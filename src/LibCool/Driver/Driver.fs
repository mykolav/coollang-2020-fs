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
        
        let arg_array = Array.ofSeq args

        let source_parts = List<SourcePart>()
        
        let mutable source_file_expected = true
        let mutable i = 0
        
        while source_file_expected && i < arg_array.Length do
            let arg = arg_array.[i]
            if arg = "-o"
            then
                source_file_expected <- false
            else
                
            source_parts.Add({ FileName = arg; Content = File.ReadAllText(arg) })
            i <- i + 1
            
        let source = Source(source_parts)
        let diagnostic_bag = DiagnosticBag()

        let ret_code = Driver.DoCompile(source, diagnostic_bag)
        Driver.RenderDiags(diagnostic_bag, source)
        ret_code


    static member DoCompile(source: Source, diagnostic_bag: DiagnosticBag): int =
        

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

        let mutable token_expected = true            
        while token_expected do
            let token = lexer.GetNext()
            tokens.Add(token)
            token_expected <- not token.IsEof
    
        if diagnostic_bag.ErrorsCount <> 0
        then
            -1
        else
            
        // Parse
        let parser = Parser(tokens.ToArray(), diagnostic_bag)
        let ast = parser.Parse()
        
        if diagnostic_bag.ErrorsCount <> 0
        then
            -1
        else

        ast |> ignore
        0


    static member RenderDiags(diagnostic_bag: DiagnosticBag, source: Source): unit =
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
