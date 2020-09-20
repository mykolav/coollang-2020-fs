namespace rec LibCool.Frontend


open System.Collections.Generic
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


    member private this.TryResolve(typename: TYPENAME): ClassSymbol voption =
        if _class_sym_map.ContainsKey(typename)
        then ValueSome (_class_sym_map.[typename])
        else ValueNone
        
        
    member this.Conforms(ancestor: ClassSymbol, descendant: ClassSymbol): bool =
        if ancestor.Name = BasicClasses.Any.Name
        then
            true
        else
        
        // let mutable super = descendant
        // let mutable conforms = ValueNone
        // while conforms.IsNone do
        //     if super.Name = BasicClassSymbols.Any.Name
        //     then
        //         conforms <- ValueSome false
        //     else
        //         
        //     if ancestor.Name = super.Name
        //     then
        //         conforms <- ValueSome true
        //     else
        //         
        //     super <- this.Resolve(super.Super)
        // 
        // conforms.Value
        
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


[<Sealed>]
type private ProgramTranslator(_program_syntax: ProgramSyntax,
                               _class_sym_map: IReadOnlyDictionary<TYPENAME, ClassSymbol>,
                               _diags: DiagnosticBag,
                               _source: Source) =
    
    
    let _type_cmp = TypeComparer(_class_sym_map)
    
    
    let _sb_data = StringBuilder()
    let _sb_code = StringBuilder()
    
    
    let translate_expr (expr_node: AstNode<ExprSyntax>): (*type:*)ClassSymbol * (*reg:*)string =
        BasicClasses.Error, ""
    
    
    let translate_ctor (class_node: AstNode<ClassSyntax>) (sym_table: SymbolTable) =
        
        let translate_attr (attr_node: AstNode<AttrSyntax>): unit =
            let initial_node = attr_node.Syntax.Initial
            let expr_node =
                match initial_node.Syntax with
                | AttrInitialSyntax.Expr expr_syntax ->
                    AstNode.Of(expr_syntax, initial_node.Span)
                | AttrInitialSyntax.Native ->
                    invalidOp "AttrInitialSyntax.Native"
                    
            let initial_ty, _ = translate_expr expr_node
            if not initial_ty.IsError
            then
                let attr_ty = _class_sym_map.[attr_node.Syntax.TYPE.Syntax]
                if not (_type_cmp.Conforms(ancestor=attr_ty, descendant=initial_ty))
                then
                    _diags.Error(
                        sprintf "The initial expression's type '%O' must conform to the attribute's type '%O'"
                                initial_ty.Name
                                attr_ty.Name,
                        initial_node.Span)
    
        // .ctor's formals are varformals,
        // .ctor's body is
        //   - an invocation of the super's .ctor with actuals from the extends syntax
        //   - assign values passed as formals to attrs derived from varformals
        //   - assign initial exprs to attrs defined in the class
        //   - concatenated blocks (if any)
        //   - ExprSyntax.This is always appended to the .ctor's end
        //     (as a result, the last block's last expr's type doesn't have to match the class' type)

        let mutable sym_count = 0
        
        sym_table.EnterScope()

        class_node.Syntax.VarFormals
        |> Array.iter (fun vf_node ->
            let sym = Symbol.Of(formal_node=vf_node.Map(fun vf -> vf.AsFormalSyntax), index=sym_count)
            sym_table.CurrentScope.AddVisible(sym)
            sym_count <- sym_count + 1)
    
        sym_table.EnterScope()

        class_node.Syntax.Features
            |> Seq.where (fun feature_node -> feature_node.Syntax.IsAttr)
            |> Seq.iter (fun feature_node -> translate_attr (feature_node.Map(fun it -> it.AsAttrSyntax)))
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
