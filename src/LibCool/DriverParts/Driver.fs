namespace LibCool.DriverParts


open System
open System.Collections.Generic
open System.IO
open LibCool.DiagnosticParts
open LibCool.SharedParts
open LibCool.SourceParts
open LibCool.ParserParts
open LibCool.TranslatorParts


type IWriteLine =
    abstract member WriteLine: line:string -> unit


[<Sealed>]
type Driver(?_writer: IWriteLine) =
    
    let _writer = defaultArg _writer
                             ({ new IWriteLine with
                                    member _.WriteLine(line: string) =
                                        Console.WriteLine(line)})
    
    
    member this.Compile(args: seq<string>): int =
        
        let arg_array = Array.ofSeq args

        let source_parts = List<SourcePart>()
        
        let mutable source_file_expected = true
        let mutable i = 0
        
        let mutable out_file = "a.exe"
        let mutable output_asm = false
        
        while source_file_expected && i < arg_array.Length do
            let arg = arg_array.[i]
            if arg = "-o"
            then
                source_file_expected <- false
                if i + 1 >= arg_array.Length
                then
                    invalidOp "'-o' must be followed by an output file name"
                    
                out_file <- arg_array.[i + 1]
            else if arg = "-S"
            then
                output_asm <- true
            else
                source_parts.Add({ FileName = arg; Content = File.ReadAllText(arg) })
            i <- i + 1
            
        let source = Source(source_parts)
        let diags = DiagnosticBag()

        let result = Driver.DoCompile(source, diags)
        Driver.RenderDiags(diags, source, _writer)
        
        if result.IsOk
        then
            if output_asm
            then
                _writer.WriteLine(result.Value)
            0
        else
            -1


    static member DoCompile(source: Source, diags: DiagnosticBag): Res<string> =
        // ERROR HANDLING:
        // 1) If any lexical errors, report and stop
        // 2) If any syntax errors, report and stop
        // 3) Semantic analysis should also get performed in stages,
        //    see Eric Lippert's corresponding post for inspiration.
        //    E.g.: detecting circular base class dependencies should be its own stage?
        //    ...

        // Lex
        let lexer = Lexer(source, diags)
        let tokens = TokenArray.ofLexer lexer
    
        if diags.ErrorsCount <> 0
        then
            Res.Error
        else
            
        // Parse
        let ast = Parser.Parse(tokens, diags)
        
        if diags.ErrorsCount <> 0
        then
            Res.Error
        else

        let asm = Translator.Translate(ast, diags, source)
        if diags.ErrorsCount <> 0
        then
            Res.Error
        else

        Res.Ok asm


    static member RenderDiags(diagnostic_bag: DiagnosticBag, source: Source, writer: IWriteLine): unit =
        // DIAG: SemiExpected.cool(3,35): Error: ';' expected
        // DIAG: Build failed: Errors: 1. Warnings: 0
        // DIAG: Build succeeded: Errors: 0. Warnings: 0
        
        for diag in diagnostic_bag.ToReadOnlyList() do
            let location = source.Map(diag.Span.First)
            writer.WriteLine(sprintf "%O: %s: %s"
                                     location
                                     (diag.Severity.ToString().Replace("Severity.", ""))
                                     diag.Message)

        if diagnostic_bag.ErrorsCount = 0
        then
            writer.WriteLine(sprintf "Build succeeded: Errors: 0. Warnings: %d"
                                     diagnostic_bag.WarningsCount)
        else
            writer.WriteLine(sprintf "Build failed: Errors: %d. Warnings: %d"
                                     diagnostic_bag.ErrorsCount
                                     diagnostic_bag.WarningsCount)
