namespace LibCool.DriverParts


open System
open System.Collections.Generic
open System.IO
open System.Text
open LibCool.SharedParts
open LibCool.SourceParts


type Cmd
    = PrintUsage of struct {| Message: string |}
    | EmitAsm of EmitAsmArgs
    | EmitExe of EmitExeArgs


[<Sealed>]
type Driver(?_writer: IWriteLine) =
    
    let _writer = defaultArg _writer
                             ({ new IWriteLine with
                                    member _.WriteLine(line: string) =
                                        Console.WriteLine(line)})
    let _emit_asm_handler = EmitAsmHandler(_writer)
    let _emit_exe_handler = EmitExeHandler(_writer)
    
    
    member this.Invoke(args: seq<string>): int =
        match Driver.ParseArgs(args) with
        | Cmd.PrintUsage it -> this.PrintUsage(it.Message)
        | Cmd.EmitAsm args -> _emit_asm_handler.Invoke(args)
        | Cmd.EmitExe args -> _emit_exe_handler.Invoke(args)
        
        
    member this.PrintUsage(message: string): int =
        _writer.WriteLine("Cool2020 Compiler version 0.1")
        _writer.WriteLine("Usage: clc file1.cool [file2.cool, ..., fileN.cool] [-o file.exe | -S [file.asm]]")
        _writer.WriteLine("")
        _writer.WriteLine("Error(s):")
        _writer.WriteLine(message)
        0


    static member ParseArgs(args: seq<string>): Cmd =
        
        let arg_array = Array.ofSeq args
        
        let mutable message = ""

        let source_parts = List<SourcePart>()
        
        let mutable o_seen = 0
        let mutable exe_file = "a.exe"

        let mutable S_seen = 0
        let mutable asm_file: string voption = ValueNone
        
        let mutable have_error = false
        let mutable i = 0
        
        while not have_error && i < arg_array.Length do
            let arg = arg_array.[i]
            if arg = "-o"
            then
                o_seen <- o_seen + 1
                
                if i + 1 >= arg_array.Length ||
                   arg_array.[i + 1].StartsWith('-')
                then
                    have_error <- true
                    message <- "'-o' must be followed by an output file name"
                else
                    exe_file <- arg_array.[i + 1]
                    i <- i + 1
            else if arg = "-S"
            then
                S_seen <- S_seen + 1
                
                if i + 1 < arg_array.Length
                then
                    if arg_array.[i + 1].StartsWith('-')
                    then
                        have_error <- true
                        message <- "'-S' must be followed by an assembly file name"
                    else
                        asm_file <- ValueSome (arg_array.[i + 1])
                        i <- i + 1
            else
                source_parts.Add({ FileName = arg; Content = File.ReadAllText(arg) })
            i <- i + 1
        
        let sb_message = StringBuilder()
        
        if source_parts.Count = 0
        then
            have_error <- true
            sb_message.AppendLine("At least one Cool2020 source file name expected").Nop()
        if o_seen > 0 && S_seen > 0
        then
            have_error <- true
            sb_message.AppendLine("'-o' and '-S' cannot both be used at the same time").Nop()
        if o_seen > 1
        then
            have_error <- true
            sb_message.AppendLine("'-o' can be used only once").Nop()
        if S_seen > 1
        then
            have_error <- true
            sb_message.AppendLine("'-S' can be used only once").Nop()
        if message <> ""
        then
            have_error <- true
            sb_message.AppendLine(message).Nop()
            
        if have_error
        then
            Cmd.PrintUsage {| Message = sb_message.ToString() |}
        else
        
        if S_seen > 0
        then
            Cmd.EmitAsm { SourceParts = source_parts; AsmFile = asm_file }
        else
            
        Cmd.EmitExe { SourceParts = source_parts; ExeFile = exe_file }
