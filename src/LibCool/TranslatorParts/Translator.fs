namespace rec LibCool.TranslatorParts


open System.Collections.Generic
open System.Text
open LibCool.SemanticParts
open LibCool.SharedParts
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


    static member Translate(
        program_syntax: ProgramSyntax,
        diags: DiagnosticBag,
        source: Source,
        code_gen_options: CodeGenOptions
    ): string =
        // First off, build a class name to class AST node map.
        let class_node_map = collectClassNodes program_syntax diags source

        // In case of any errors, we don't proceed to the next translation stage.
        if diags.ErrorsCount <> 0
        then
            ""
        else

        // Now, build a class name to class symbol map.
        let class_sym_map = ClassSymbolCollector(
            program_syntax, class_node_map, source, diags).Collect()

        // In case of any errors, we don't proceed to the next translation stage.
        if diags.ErrorsCount <> 0
        then
            ""
        else

        // We've collected everything we need to actually start emitting assembly code.
        let context = { TranslationContext.CodeGenOptions = code_gen_options
                        ClassSymMap = class_sym_map
                        TypeCmp = TypeComparer(class_sym_map)
                        RegSet = RegisterSet()
                        LabelGen = LabelGenerator()
                        // Diags
                        Diags = diags
                        Source = source
                        // Accumulators
                        IntConsts = IntConstSet("INT")
                        StrConsts = StringConstSet("STR") }

        // Add default values here so that their indexes are 0.
        // Prototype objs need these.
        context.IntConsts.GetOrAdd(0) |> ignore
        context.StrConsts.GetOrAdd("") |> ignore

        // Translate the code section.
        // This must happen before translating the data section,
        // so that, for example, we've seen all the predefined objects
        // we'll need to emit into the data section, etc.
        let sb_code_asm = StringBuilder()
        for class_node in program_syntax.Classes do
            let class_methods_frag = MethodsTranslator(context, class_node.Syntax).Translate()
            sb_code_asm.Append(class_methods_frag)
                       .AsUnit()

        // Translate the data section.
        let data_asm = DataTranslator(context).Translate()

        // Combine the data and code.
        let asm =
            StringBuilder()
                .AppendLine("    .section .rodata")
                .AppendLine()
                .Append(data_asm)
                .AppendLine()
                .AppendLine("    .text")
                .Append(sb_code_asm.ToString())
                .ToString()
        asm
