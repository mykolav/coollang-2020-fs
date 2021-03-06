namespace LibCool.SharedParts


open System.IO
open System.Diagnostics
open System.Text


[<Sealed>]
type StringBuilderWriter() =
    inherit TextWriter()
    
    let _sb_out = StringBuilder()
    
    override _.Encoding = stdout.Encoding
    
    override _.Write (s: string) = _sb_out.Append(s).Nop()
    override _.WriteLine (s: string) = _sb_out.AppendLine(s).Nop()
    override _.WriteLine() = _sb_out.AppendLine().Nop()
    
    override _.ToString() = _sb_out.ToString()


[<Sealed>]
type ProcessRunner private () =


    static member Run(exe_name: string, args: string, ?stdin: string): string =
        let sb_output = StringBuilder()

        use theProcess =
            new Process(StartInfo =
                            ProcessStartInfo
                                (FileName = exe_name,
                                 Arguments = args,
                                 UseShellExecute = false,
                                 RedirectStandardOutput = true,
                                 RedirectStandardError = true,
                                 RedirectStandardInput = stdin.IsSome))

        theProcess.Start() |> ignore
        
        if stdin.IsSome
        then
            theProcess.StandardInput.WriteLine(stdin.Value)
            theProcess.StandardInput.Close()

        theProcess.WaitForExit()
        
        sb_output
            .Append(theProcess.StandardOutput.ReadToEnd())
            .Append(theProcess.StandardError.ReadToEnd())
            .ToString()
