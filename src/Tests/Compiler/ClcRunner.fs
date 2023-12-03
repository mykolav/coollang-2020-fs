namespace Tests.Compiler


open System
open LibCool.SharedParts
open LibCool.DriverParts


module ClcRunner =


    let runClc (args: seq<string>): string =
        ProcessRunner.Run("../../../../clc/bin/Debug/net8.0/clc.exe", String.Join(" ", args))


    let runClcInProcess (driver_args: seq<string>): string =
        use output = new StringBuilderWriter()
        Driver({ new IWriteLine with
                     member _.WriteLine(line: string) =
                         output.WriteLine(line) })
            .Invoke(driver_args)
            |> ignore
        
        output.ToString()
