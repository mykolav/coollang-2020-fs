namespace LibCool.SharedParts


open System.IO
open System.Diagnostics
open System.Text


[<Sealed>]
type StringBuilderWriter() =
    inherit TextWriter()
    
    let _sb_out = StringBuilder()
    
    override _.Encoding = stdout.Encoding
    
    override _.Write (s: string) = _sb_out.Append(s).AsUnit()
    override _.WriteLine (s: string) = _sb_out.AppendLine(s).AsUnit()
    override _.WriteLine() = _sb_out.AppendLine().AsUnit()
    
    override _.ToString() = _sb_out.ToString()


[<Sealed>]
type ProcessRunner private () =


    static member Run(exe_name: string, args: string, ?stdin_lines: seq<string>): string =
        let stdin_lines = match stdin_lines with
                          | Some stdin_lines -> stdin_lines
                          | None             -> []

        let sb_output = StringBuilder()

        use theProcess =
            new Process(StartInfo =
                            ProcessStartInfo
                                (FileName = exe_name,
                                 Arguments = args,
                                 UseShellExecute = false,
                                 RedirectStandardOutput = true,
                                 RedirectStandardError = true,
                                 RedirectStandardInput = Seq.any stdin_lines))

        theProcess.Start() |> ignore

        for stdin_line in stdin_lines do
            theProcess.StandardInput.WriteLine(stdin_line)
            theProcess.StandardInput.Close()

        theProcess.WaitForExit()
        
        sb_output
            .Append(theProcess.StandardOutput.ReadToEnd())
            .Append(theProcess.StandardError.ReadToEnd())
            .ToString()
