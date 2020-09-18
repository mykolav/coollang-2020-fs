namespace LibCool.Driver


open System
open System.Collections.Generic
open System.IO
open LibCool.DiagnosticParts
open LibCool.SourceParts
open LibCool.Frontend
open LibCool.Frontend.SemanticParts


type IWriteLine =
    abstract member WriteLine: format:string * [<ParamArray>] args:obj[] -> unit


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
        let diags = DiagnosticBag()

        let ret_code = Driver.DoCompile(source, diags)
        Driver.RenderDiags(diags,
                           source,
                           { new IWriteLine with
                               member _.WriteLine(format: string, [<ParamArray>] args: obj[]) =
                                   Console.WriteLine(format, args)})
        ret_code


    static member DoCompile(source: Source, diags: DiagnosticBag): int =
        

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
            -1
        else
            
        // Parse
        let ast = Parser.Parse(tokens, diags)
        
        if diags.ErrorsCount <> 0
        then
            -1
        else

        let asm = SemanticStageDriver.Translate(ast, diags, source)
        if diags.ErrorsCount <> 0
        then
            -1
        else

        asm |> ignore
        0


    static member RenderDiags(diagnostic_bag: DiagnosticBag, source: Source, writer: IWriteLine): unit =
        // DIAG: SemiExpected.cool(3,35): Error: ';' expected
        // DIAG: Build failed: Errors: 1. Warnings: 0
        // DIAG: Build succeeded: Errors: 0. Warnings: 0
        
        for diag in diagnostic_bag.ToReadOnlyList() do
            let location = source.Map(diag.Span.First)
            writer.WriteLine(
                "{0}: {1}: {2}",
                location,
                (diag.Severity.ToString().Replace("Severity.", "")),
                diag.Message)

        if diagnostic_bag.ErrorsCount = 0
        then
            writer.WriteLine("Build succeeded: Errors: 0. Warnings: {0}", diagnostic_bag.WarningsCount)
        else
            writer.WriteLine("Build failed: Errors: {0}. Warnings: {1}", diagnostic_bag.ErrorsCount,
                                                                          diagnostic_bag.WarningsCount)
