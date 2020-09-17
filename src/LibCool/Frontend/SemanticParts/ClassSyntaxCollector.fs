namespace LibCool.Frontend.SemanticParts


open System.Collections.Generic
open LibCool.AstParts
open LibCool.DiagnosticParts
open LibCool.SourceParts


[<Sealed>]
type ClassDeclCollector(_program_syntax: ProgramSyntax, _diags: DiagnosticBag, _source: Source) =
    
    
    member this.Collect() =
        let map = Dictionary<TYPENAME, AstNode<ClassSyntax>>()
        
        let add_classdecl_syntax (classdecl_node: AstNode<ClassSyntax>): unit =
            let classdecl_syntax = classdecl_node.Value
            if map.ContainsKey(classdecl_syntax.NAME.Value)
            then
                let prev_classdecl_syntax = map.[classdecl_syntax.NAME.Value].Value
                let message = sprintf "The program already contains a class '%O' at %O"
                                      classdecl_syntax.NAME.Value
                                      (_source.Map(prev_classdecl_syntax.NAME.Span.First))
                                      
                _diags.Error(message, classdecl_syntax.NAME.Span)
            else
                map.Add(classdecl_syntax.NAME.Value, classdecl_node)
        
        _program_syntax.ClassDecls |> Seq.iter (fun it -> add_classdecl_syntax it)
        
        map :> IReadOnlyDictionary<_, _>
