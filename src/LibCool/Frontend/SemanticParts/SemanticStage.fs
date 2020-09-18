namespace LibCool.Frontend


open System.Collections.Generic
open System.Text
open LibCool.SourceParts
open LibCool.AstParts
open LibCool.DiagnosticParts
open LibCool.Frontend.SemanticParts
open AstExtensions


[<Sealed>]
type private SemanticStage(_program_syntax: ProgramSyntax,
                           _class_sym_map: IReadOnlyDictionary<TYPENAME, ClassSymbol>,
                           _diags: DiagnosticBag,
                           _source: Source) =
    
    
    let sb_data = StringBuilder()
    let sb_code = StringBuilder()
    
    
    let translate_attr (attr_node: AstNode<AttrSyntax>) =
        ()
    
    
    let translate_ctor (class_node: AstNode<ClassSyntax>) =
        // .ctor's formals are varformals,
        // .ctor's body is
        //   - an invocation of the super's .ctor with actuals from the extends syntax
        //   - assign values passed as formals to attrs derived from varformals
        //   - assign initial exprs to attrs defined in the class
        //   - concatenated blocks (if any)
        //   - ExprSyntax.This is always appended to the .ctor's end
        //     (as a result, the last block's last expr's type doesn't have to match the class' type)

        class_node.Syntax.Features
            |> Seq.where (fun feature_node -> feature_node.Syntax.IsAttr)
            |> Seq.iter (fun feature_node -> translate_attr (feature_node.Map(fun it -> it.AsAttrSyntax)))
    
    
    let translate_method (method_node: AstNode<MethodSyntax>) =
        ()
    
    
    let translate_class (class_node: AstNode<ClassSyntax>) =
        translate_ctor class_node
        class_node.Syntax.Features
            |> Seq.where (fun feature_node -> feature_node.Syntax.IsMethod)
            |> Seq.iter (fun feature_node -> translate_method (feature_node.Map(fun it -> it.AsMethodSyntax)))


    member this.Translate(): string =
        _program_syntax.Classes |> Array.iter translate_class
        ""


[<Sealed>]
type SemanticStageDriver private () =
        
    
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
        
        let asm = SemanticStage(program_syntax, class_sym_map, diags, source).Translate()
        asm
