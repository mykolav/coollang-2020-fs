namespace LibCool.Driver


open System
open System.Collections.Generic
open System.IO
open LibCool.DiagnosticParts
open LibCool.Frontend
open LibCool.SourceParts


[<Sealed>]
type Driver private () =
    static member Compile(args: seq<string>): int =
        
        let source_parts = List<SourcePart>()
        
        let mutable minus_o_seen = false
        let mutable i = 0
        
        let arg_array = Array.ofSeq args
        while not minus_o_seen && i < arg_array.Length do
            let arg = arg_array.[i]
            if (arg = "-o")
            then
                minus_o_seen <- true
            else
                
            source_parts.Add({ FileName = arg; Content = File.ReadAllText(arg) })
            i <- i + 1
            
        let source = Source(source_parts)
        let diagnostic_bag = DiagnosticBag()

        // ERROR HANDLING:
        // 1) If any lexical errors, report and stop
        // 2) If any syntax errors, report and stop
        // 3) Semantic analysis should also get performed in stages,
        //    see Eric Lippert's corresponding post for inspiration.
        //    E.g.: detecting circular base class dependencies should be its own stage?
        //    ...

        // Lex
        let lexer = Lexer(source, diagnostic_bag)

        let tokens = List<Token>()

        let mutable token = lexer.GetNext()
        tokens.Add(token)
            
        while token.Kind <> TokenKind.EOF do
            token <- lexer.GetNext()
            tokens.Add(token)
    
        if diagnostic_bag.ErrorsCount = 0
        then
            // Parse
            let parser = Parser(tokens.ToArray(), diagnostic_bag)

            parser.Parse() |> ignore
        
        // DIAG: SemiExpected.cool(3,35): Error: ';' expected
        // DIAG: Build failed: Errors: 1. Warnings: 0
        // DIAG: Build succeeded: Errors: 0. Warnings: 0
        
        for diag in diagnostic_bag.ToReadOnlyList() do
            let { FileName = file_name; Line = line; Col = col } = source.Map(diag.Span.First)
            Console.WriteLine(
                "{0}({1},{2}): {3}: {4}",
                file_name,
                line,
                col,
                (diag.Severity.ToString().Replace("Severity.", "")),
                diag.Message)

        if diagnostic_bag.ErrorsCount = 0
        then
            Console.WriteLine("Build succeeded: Errors: 0. Warnings: {0}", diagnostic_bag.WarningsCount)
        else
            Console.WriteLine("Build failed: Errors: {0}. Warnings: {1}", diagnostic_bag.ErrorsCount,
                                                                          diagnostic_bag.WarningsCount)

        0

