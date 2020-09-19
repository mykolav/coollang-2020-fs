namespace rec LibCool.Frontend


open System.Text
open LibCool.SourceParts
open LibCool.DiagnosticParts
open LibCool.AstParts
open LibCool.Frontend.SemanticParts
open AstExtensions


[<Sealed>]
type private ProgramTranslator(_program_syntax: ProgramSyntax,
                               _type_table: TypeTable,
                               _diags: DiagnosticBag,
                               _source: Source) =
    
    
    let sb_data = StringBuilder()
    let sb_code = StringBuilder()
    
    
    let translate_expr (expr_node: AstNode<ExprSyntax>): (*type:*)ClassSymbol * (*reg:*)string =
        BasicClassSymbols.Error, ""
    
    
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
                let attr_ty = _type_table.Resolve(attr_node.Syntax.TYPE.Syntax)
                if not (_type_table.Conforms(ancestor=attr_ty, descendant=initial_ty))
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
        let sym_table = SymbolTable(_type_table.Resolve(class_node.Syntax.NAME.Syntax))
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
        let classdecl_node_map = ClassDeclCollector(program_syntax, diags, source).Collect()
        if diags.ErrorsCount <> 0
        then
            ""
        else
            
        let class_sym_map = ClassSymbolCollector(program_syntax,
                                                 classdecl_node_map,
                                                 source,
                                                 diags).Collect()
        if diags.ErrorsCount <> 0
        then
            ""
        else
        
        let asm = ProgramTranslator(program_syntax, TypeTable(class_sym_map), diags, source).Translate()
        asm
