namespace LibCool.SemanticParts.SemanticParts


open System.Collections.Generic
open LibCool.AstParts
open LibCool.DiagnosticParts
open LibCool.SourceParts


[<Sealed>]
type ClassDeclCollector(_program_syntax: ProgramSyntax, _diags: DiagnosticBag, _source: Source) =
    
    
    member this.Collect() =
        let map = Dictionary<TYPENAME, AstNode<ClassSyntax>>()
        
        let add_class_syntax (class_node: AstNode<ClassSyntax>): unit =
            let class_syntax = class_node.Syntax
            if map.ContainsKey(class_syntax.NAME.Syntax)
            then
                let prev_class_syntax = map.[class_syntax.NAME.Syntax].Syntax
                let message = sprintf "The program already contains a class '%O' at %O"
                                      class_syntax.NAME.Syntax
                                      (_source.Map(prev_class_syntax.NAME.Span.First))
                                      
                _diags.Error(message, class_syntax.NAME.Span)
            else
                map.Add(class_syntax.NAME.Syntax, class_node)
        
        _program_syntax.Classes |> Seq.iter (fun it -> add_class_syntax it)
        
        map :> IReadOnlyDictionary<_, _>
