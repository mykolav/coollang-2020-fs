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

        let lexer = Lexer(source, diagnostic_bag)
        let parser = Parser(lexer, diagnostic_bag)

        parser.Parse() |> ignore
        let diags = diagnostic_bag.ToReadOnlyList()
        
        // DIAG: SemiExpected.cool(3,35): Error: ';' expected
        // DIAG: Build failed: Errors: 1. Warnings: 0
        // DIAG: Build succeeded: Errors: 0. Warnings: 0
        
        for diag in diags do
            let { FileName = file_name; Line = line; Col = col } = source.Map(diag.Span.First)
            Console.WriteLine("{0}({1},{2}): {3}: {4}", file_name, line, col, (diag.Severity.ToString()), diag.Message)

        let errs_count = diags |> Seq.filter (fun it -> it.Severity = DiagnosticSeverity.Error) |> Seq.length
        let warns_count = diags |> Seq.filter (fun it -> it.Severity = DiagnosticSeverity.Warning) |> Seq.length
        
        if errs_count = 0
        then
            Console.WriteLine("Build succeeded: Errors: 0. Warnings: {0}", warns_count)
        else
            Console.WriteLine("Build failed: Errors: {0}. Warnings: {1}", errs_count, warns_count)

        0

