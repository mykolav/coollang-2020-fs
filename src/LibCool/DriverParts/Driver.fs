namespace LibCool.DriverParts


open System
open System.Collections.Generic
open System.IO
open System.Text
open LibCool.DiagnosticParts
open LibCool.SharedParts
open LibCool.SourceParts
open LibCool.ParserParts
open LibCool.TranslatorParts


type Cmd
    = PrintUsage of struct {| Message: string |}
    | EmitAsm of struct {| SourceParts: seq<SourcePart>; AsmFile: string voption |}
    | EmitExe of struct {| SourceParts: seq<SourcePart>; ExeFile: string |}


type IWriteLine =
    abstract member WriteLine: line:string -> unit


[<Sealed>]
type Driver(?_writer: IWriteLine) =
    
    let _writer = defaultArg _writer
                             ({ new IWriteLine with
                                    member _.WriteLine(line: string) =
                                        Console.WriteLine(line)})
    
    
    member this.Invoke(args: seq<string>): int =
        match Driver.ParseArgs(args) with
        | Cmd.PrintUsage it -> this.PrintUsage(it.Message)
        | Cmd.EmitAsm args -> this.EmitAsm(args)
        | Cmd.EmitExe args -> this.EmitExe(args)
        
        
    member this.PrintUsage(message: string): int =
        _writer.WriteLine("Cool2020 Compiler version 0.1")
        _writer.WriteLine("Usage: clc file1.cool [file2.cool, ..., fileN.cool] [-o file.exe | -S file.asm]")
        _writer.WriteLine("")
        _writer.WriteLine("Error(s):")
        _writer.WriteLine(message)
        0
    
    
    member this.EmitAsm(args: struct {| SourceParts: seq<SourcePart>; AsmFile: string voption |}): int =
        let source = Source(args.SourceParts)
        let diags = DiagnosticBag()

        let result = Driver.CompileToAsm(source, diags)
        Driver.RenderDiags(diags, source, _writer)
        
        if result.IsError
        then
            -1
        else

        match args.AsmFile with
        | ValueSome asm_file -> File.WriteAllText(asm_file, result.Value)
        | ValueNone          -> _writer.WriteLine(result.Value) 
            
        0


    member this.EmitExe(args: struct {| SourceParts: seq<SourcePart>; ExeFile: string |}): int =
        let source = Source(args.SourceParts)
        let diags = DiagnosticBag()

        let result = Driver.CompileToAsm(source, diags)
        
        if result.IsError
        then
            Driver.RenderDiags(diags, source, _writer)
            -1
        else
            
        let obj_file = if args.ExeFile.EndsWith(".exe")
                       then args.ExeFile.Replace(".exe", ".o")
                       else args.ExeFile + ".o"
        let as_output = ProcessRunner.Run(file_name="as",
                                          args=sprintf "-o %s" obj_file,
                                          stdin=result.Value)
        if not (String.IsNullOrWhiteSpace(as_output))
        then
            // We treat every message from 'as' as an error,
            // which is obviously not always correct.
            // But for now it will do.
            for line in ProcessOutputParser.split_in_lines as_output do
                diags.Error(line, Span.Invalid)
                
            Driver.RenderDiags(diags, source, _writer)
            -1
        else

        let ld_args =
            sprintf "-o %s -e main %s rt_windows.o -L\"C:/msys64/mingw64/x86_64-w64-mingw32/lib\" -lkernel32"
                    args.ExeFile
                    obj_file
        let ld_output = ProcessRunner.Run(file_name="ld",
                                          args=ld_args,
                                          stdin=result.Value)
        if not (String.IsNullOrWhiteSpace(ld_output))
        then
            // We treat every message from 'ld' as an error,
            // which is obviously not always correct.
            // But for now it will do.
            for line in ProcessOutputParser.split_in_lines ld_output do
                diags.Error(line, Span.Invalid)
            Driver.RenderDiags(diags, source, _writer)
            -1
        else

        Driver.RenderDiags(diags, source, _writer)
        0


    static member CompileToAsm(source: Source, diags: DiagnosticBag): Res<string> =
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
        
        for diag in diagnostic_bag.Diags do
            let location = source.Map(diag.Span.First)
            writer.WriteLine(sprintf "%O: %s: %s" location
                                                  (diag.Severity.ToString().Replace("Severity.", ""))
                                                  diag.Message)

        
        for binutils_error in diagnostic_bag.BinutilsErrors do
            writer.WriteLine(binutils_error)

        if diagnostic_bag.ErrorsCount = 0
        then
            writer.WriteLine(sprintf "Build succeeded: Errors: 0. Warnings: %d"
                                     diagnostic_bag.WarningsCount)
        else
            writer.WriteLine(sprintf "Build failed: Errors: %d. Warnings: %d"
                                     diagnostic_bag.ErrorsCount
                                     diagnostic_bag.WarningsCount)
            
            
    static member ParseArgs(args: seq<string>): Cmd =
        
        let arg_array = Array.ofSeq args
        
        if arg_array.Length = 0
        then
            Cmd.PrintUsage {| Message = "At least one Cool source file name expected" |}
        else
        
        let mutable message = ""

        let source_parts = List<SourcePart>()
        
        let mutable o_seen = 0
        let mutable exe_file: string voption = ValueNone

        let mutable S_seen = 0
        let mutable asm_file: string voption = ValueNone
        
        let mutable parsing_complete = false
        let mutable i = 0
        
        while not parsing_complete && i < arg_array.Length do
            let arg = arg_array.[i]
            if arg = "-o"
            then
                o_seen <- o_seen + 1
                
                if i + 1 >= arg_array.Length
                then
                    parsing_complete <- true
                    message <- "'-o' must be followed by an output file name"
                else
                    exe_file <- ValueSome (arg_array.[i + 1])
                    i <- i + 1
            else if arg = "-S"
            then
                S_seen <- S_seen + 1
                
                if i + 1 < arg_array.Length
                then
                    asm_file <- ValueSome (arg_array.[i + 1])
                    i <- i + 1
            else
                source_parts.Add({ FileName = arg; Content = File.ReadAllText(arg) })
            i <- i + 1
        
        let sb_message = StringBuilder()
        
        let mutable have_message = false
        
        if o_seen > 0 && S_seen > 0
        then
            have_message <- true
            sb_message.AppendLine("'-o' and '-S' cannot both be used at the same time").Nop()
        if o_seen > 1
        then
            have_message <- true
            sb_message.AppendLine("'-o' can be used only once").Nop()
        if S_seen > 1
        then
            have_message <- true
            sb_message.AppendLine("'-S' can be used only once").Nop()
        if message <> ""
        then
            have_message <- true
            sb_message.AppendLine(message).Nop()
            
        if have_message
        then
            Cmd.PrintUsage {| Message = sb_message.ToString() |}
        else
        
        if S_seen > 0
        then
            Cmd.EmitAsm {| SourceParts = source_parts; AsmFile = asm_file |}
        else
            
        Cmd.EmitExe {| SourceParts = source_parts; ExeFile = exe_file.Value |}
