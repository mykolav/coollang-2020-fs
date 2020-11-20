namespace Tests.Compiler


open System
open LibCool.SharedParts
open LibCool.DriverParts


module ClcRunner =


    let run_clc (args: seq<string>): string =
        ProcessRunner.Run("../../../../clc/bin/Debug/netcoreapp3.1/clc.exe", String.Join(" ", args))


    let run_clc_in_process (driver_args: seq<string>): string =
        use output = new StringBuilderWriter()
        Driver({ new IWriteLine with
                     member _.WriteLine(line: string) =
                         output.WriteLine(line) })
            .Compile(driver_args)
            |> ignore
        
        output.ToString()
