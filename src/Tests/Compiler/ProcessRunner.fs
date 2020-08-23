namespace Tests.Compiler

open System
open System.IO
open LibCool.Driver

module ProcessRunner =


    open System.Diagnostics
    open System.Text
    
    
    [<Sealed>]
    type private StringBuilderWriter() =
        inherit TextWriter()
        
        let _sb_out = StringBuilder()
        
        override _.Encoding = stdout.Encoding
        
        override _.Write (s: string) = _sb_out.Append(s) |> ignore
        override _.WriteLine (s: string) = _sb_out.AppendLine(s) |> ignore
        override _.WriteLine() = _sb_out.AppendLine() |> ignore
        
        override _.ToString() = _sb_out.ToString()


    let run (file_name: string) (args: string): string =
        let sb_output = StringBuilder()

        use theProcess =
            new Process(StartInfo =
                            ProcessStartInfo
                                (FileName = file_name,
                                 Arguments = args,
                                 UseShellExecute = false,
                                 RedirectStandardOutput = true,
                                 RedirectStandardError = true))

        theProcess.OutputDataReceived.AddHandler(fun sender args -> sb_output.Append(args.Data) |> ignore)
        theProcess.ErrorDataReceived.AddHandler(fun sender args -> sb_output.Append(args.Data) |> ignore)

        theProcess.Start() |> ignore
        theProcess.BeginOutputReadLine()
        theProcess.BeginErrorReadLine()

        theProcess.WaitForExit()

        sb_output.ToString()


    let run_clc (args: seq<string>): string =
        run "../../../../clc/bin/Debug/netcoreapp3.1/clc.exe" (String.Join(" ", args))


    let run_clc_in_process (args: seq<string>): string =
        use output = new StringBuilderWriter()
        
        let console_out = Console.Out
        Console.SetOut(output)
        
        Driver.Compile(args) |> ignore
        
        Console.SetOut(console_out)
        
        output.ToString()
