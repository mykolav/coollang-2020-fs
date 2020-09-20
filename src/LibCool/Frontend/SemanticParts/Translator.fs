namespace rec LibCool.Frontend


open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Text
open LibCool.SourceParts
open LibCool.DiagnosticParts
open LibCool.AstParts
open LibCool.Frontend.SemanticParts
open AstExtensions


[<Sealed>]
type TypeComparer(_class_sym_map: IReadOnlyDictionary<TYPENAME, ClassSymbol>) =


    member private this.Resolve(typename: TYPENAME): ClassSymbol =
        _class_sym_map.[typename]


    member this.Conforms(ancestor: ClassSymbol, descendant: ClassSymbol): bool =
        if ancestor.Is(BasicClasses.Any)
        then
            true
        else
        
        if descendant.Is(BasicClasses.Null)
        then
            not (
                ancestor.Is(BasicClasses.Boolean) ||
                ancestor.Is(BasicClasses.Int) ||
                ancestor.Is(BasicClasses.Unit))
        else
        
        let rec conforms (descendant: ClassSymbol): bool =
            if descendant.Name = BasicClasses.Any.Name
            then
                false
            else
            
            if ancestor.Name = descendant.Name
            then
                true
            else
                
            conforms (this.Resolve(descendant.Super))
            
        conforms descendant


[<IsReadOnly; Struct>]
type AsmFragment =
    { Type: ClassSymbol
      Asm: string
      Reg: string }
    with
    member this.IsError: bool = this.Type.IsError
    member this.IsOk: bool = not (this.Type.IsError)
    static member Error =
        { Type=BasicClasses.Error; Asm=""; Reg="" }


[<Sealed>]
type private ProgramTranslator(_program_syntax: ProgramSyntax,
                               _class_sym_map: IReadOnlyDictionary<TYPENAME, ClassSymbol>,
                               _diags: DiagnosticBag,
                               _source: Source) =
    
    
    let _type_cmp = TypeComparer(_class_sym_map)
    
    
    let _sb_data = StringBuilder()
    let _sb_code = StringBuilder()
    
    
    let translate_expr (expr_node: AstNode<ExprSyntax>): AsmFragment =
        AsmFragment.Error
    
    
    let translate_block (block_syntax_opt: BlockSyntax voption): AsmFragment =
        AsmFragment.Error


    let translate_attr (attr_node: AstNode<AttrSyntax>): string =
        let initial_node = attr_node.Syntax.Initial
        let expr_node =
            match initial_node.Syntax with
            | AttrInitialSyntax.Expr expr_syntax ->
                AstNode.Of(expr_syntax, initial_node.Span)
            | AttrInitialSyntax.Native ->
                invalidOp "AttrInitialSyntax.Native"
                
        let initial_frag = translate_expr expr_node
        if initial_frag.IsError
        then
            ""
        else
            
        let attr_ty = _class_sym_map.[attr_node.Syntax.TYPE.Syntax]
        if not (_type_cmp.Conforms(ancestor=attr_ty, descendant=initial_frag.Type))
        then
            _diags.Error(
                sprintf "The initial expression's type '%O' must conform to the attribute's type '%O'"
                        initial_frag.Type.Name
                        attr_ty.Name,
                initial_node.Span)
            ""
        else
            
        let asm = ""
        asm


    let translate_ctor (class_node: AstNode<ClassSyntax>) (sym_table: SymbolTable) =
    
        // .ctor's formals are varformals,
        // .ctor's body is
        //   - an invocation of the super's .ctor with actuals from the extends syntax
        //   - assign values passed as formals to attrs derived from varformals
        //   - assign initial exprs to attrs defined in the class
        //   - concatenated blocks (if any)
        //   - ExprSyntax.This is always appended to the .ctor's end
        //     (as a result, the last block's last expr's type doesn't have to match the class' type)

        let sb_ctor_body = StringBuilder()
        
        // 'this' is always at index 0
        let mutable sym_count = 1
        
        sym_table.EnterScope()

        sym_table.CurrentScope.AddVisible(Symbol.This(class_node.Syntax))
        
        // By a cruel twist of fate, you can't say `this.ID = ...` in Cool2020.
        // Gotta be creative and prefix formal names with "."
        // to avoid shadowing attr names by the ctor's param names.
        class_node.Syntax.VarFormals
        |> Seq.iter (fun vf_node ->
            let sym = Symbol.Of(formal_node=vf_node.Map(fun vf -> vf.AsFormalSyntax(id_prefix=".")),
                                index=sym_count)
            sym_table.CurrentScope.AddVisible(sym)
            sym_count <- sym_count + 1)
        
        sym_table.EnterScope()
        
        // Invoke the super's .ctor with actuals from the extends syntax
        // SuperDispatch of method_id: AstNode<ID> * actuals: AstNode<ExprSyntax> []
        let extends_syntax = class_node.Syntax.ExtendsSyntax
        let super_dispatch_syntax = ExprSyntax.SuperDispatch (
                                        method_id=AstNode.Virtual(ID ".ctor"),
                                        actuals=extends_syntax.Actuals)
        
        let super_dispatch_frag = translate_expr (AstNode.Virtual(super_dispatch_syntax))
        if super_dispatch_frag.IsOk
        then
            sb_ctor_body.AppendLine(super_dispatch_frag.Asm) |> ignore
            
        // Assign values passed as formals to attrs derived from varformals.
        class_node.Syntax.VarFormals
        |> Seq.iter (fun vf_node ->
            let attr_name = vf_node.Syntax.ID.Syntax.Value
            let assign_syntax =
                ExprSyntax.Assign(id=AstNode.Virtual(ID attr_name),
                                  expr=AstNode.Virtual(ExprSyntax.Id (ID ("." + attr_name))))
            let assign_frag = translate_expr (AstNode.Virtual(assign_syntax))

            if assign_frag.IsOk
            then
                sb_ctor_body.AppendLine(assign_frag.Asm) |> ignore)
        
        // Assign initial values to attributes declared in the class.
        class_node.Syntax.Features
        |> Seq.where (fun feature_node -> feature_node.Syntax.IsAttr)
        |> Seq.iter (fun feature_node ->
            let attr_assign_initial_asm = translate_attr (feature_node.Map(fun it -> it.AsAttrSyntax))
            sb_ctor_body.AppendLine(attr_assign_initial_asm) |> ignore)
        
        // Translate blocks.
        class_node.Syntax.Features
        |> Seq.where (fun feature_node -> feature_node.Syntax.IsBracedBlock)
        |> Seq.iter (fun feature_node ->
            let block_frag = translate_block feature_node.Syntax.AsBlockSyntax
            if block_frag.IsOk
            then
                sb_ctor_body.AppendLine(block_frag.Asm) |> ignore)
        
        // Append ExprSyntax.This to the .ctor's end.
        // (As a result, the last block's last expr's type doesn't have to match the class' type.)
        let this_syntax = ExprSyntax.This
        let this_frag = translate_expr (AstNode.Virtual(this_syntax))
        if this_frag.IsOk
        then
            // TODO: Here we need to emit assembly moving this_frag.Reg into the return value register
            //       Keep in mind, returning a value is more relevant for regular method
            //       as a ctor return value is the one we passed it in `this`.
            //       So, maybe, we aren't going to return anything at all from ctors.
            this_frag.Reg |> ignore
            
        // TODO: Generate the method header and footer, insert `sb_ctor_body` in between them.

        sym_table.LeaveScope()
        sym_table.LeaveScope()
    
    
    let translate_method (method_node: AstNode<MethodSyntax>) (sym_table: SymbolTable) =
        ()
    
    
    let translate_class (class_node: AstNode<ClassSyntax>) =
        let sym_table = SymbolTable(_class_sym_map.[class_node.Syntax.NAME.Syntax])
        translate_ctor class_node sym_table
        class_node.Syntax.Features
            |> Seq.where (fun feature_node -> feature_node.Syntax.IsMethod)
            |> Seq.iter (fun feature_node -> translate_method (feature_node.Map(fun it -> it.AsMethodSyntax))
                                                              sym_table)


    member this.Translate(): string =
        _program_syntax.Classes |> Array.iter translate_class
        ""


[<Sealed>]
type Translator private () =
        
    
    static member Translate(program_syntax: ProgramSyntax, diags: DiagnosticBag, source: Source): string =
        let class_node_map = ClassDeclCollector(program_syntax, diags, source).Collect()
        if diags.ErrorsCount <> 0
        then
            ""
        else
            
        let class_sym_map = ClassSymbolCollector(program_syntax,
                                                 class_node_map,
                                                 source,
                                                 diags).Collect()
        if diags.ErrorsCount <> 0
        then
            ""
        else
        
        let asm = ProgramTranslator(program_syntax, class_sym_map, diags, source).Translate()
        asm
