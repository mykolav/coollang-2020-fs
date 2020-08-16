namespace Tests.Compiler

module ProcessRunner =


    open System.Diagnostics
    open System.Text


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


    let run_clc (args: string): string =
        run "../../../../clc/bin/Debug/netcoreapp3.1/clc.exe" args
