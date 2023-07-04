namespace rec LibCool.TranslatorParts


open System.Collections.Generic
open LibCool.SemanticParts
open LibCool.SourceParts
open LibCool.DiagnosticParts
open LibCool.AstParts
open LibCool.TranslatorParts


[<Sealed>]
type Translator private () =
        
    
    static let collectClassNodes (program_syntax: ProgramSyntax)
                                   (diags: DiagnosticBag)
                                   (source: Source)
                                   : IReadOnlyDictionary<TYPENAME, AstNode<ClassSyntax>> =
        let map = Dictionary<TYPENAME, AstNode<ClassSyntax>>()
        
        let addClassSyntax (class_node: AstNode<ClassSyntax>): unit =
            let class_syntax = class_node.Syntax
            if map.ContainsKey(class_syntax.NAME.Syntax)
            then
                let prev_class_syntax = map[class_syntax.NAME.Syntax].Syntax
                let message = $"The program already contains a class '{class_syntax.NAME.Syntax}' " +
                              $"at {source.Map(prev_class_syntax.NAME.Span.First)}"

                diags.Error(message, class_syntax.NAME.Span)
            else
                map.Add(class_syntax.NAME.Syntax, class_node)

        program_syntax.Classes |> Seq.iter addClassSyntax
        
        map :> IReadOnlyDictionary<_, _>
    
    
    static member Translate(program_syntax: ProgramSyntax, diags: DiagnosticBag, source: Source): string =
        let class_node_map = collectClassNodes program_syntax diags source
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
