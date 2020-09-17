namespace LibCool.Frontend


open LibCool.AstParts
open LibCool.DiagnosticParts
open LibCool.SourceParts
open LibCool.Frontend.SemanticParts


[<Sealed>]
type SemanticStage private () =
        
    
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
        
        class_sym_map |> ignore
        ""
