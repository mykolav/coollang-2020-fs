namespace Tests.Compiler


open System
open System.Collections.Generic
open System.IO
open System.Runtime.CompilerServices
open LibCool.DriverParts
open LibCool.SharedParts


[<RequireQualifiedAccess>]
module ClcRunner =


    let run (args: seq<string>): string =
        ProcessRunner.Run("../../../../clc/bin/Debug/net8.0/clc.exe", String.Join(" ", args))


    let runInProcess (driver_args: seq<string>): string =
        use output = new StringBuilderWriter()
        Driver({ new IWriteLine with
                     member _.WriteLine(line: string) =
                         output.WriteLine(line) })
            .Invoke(driver_args)
            |> ignore

        output.ToString()


[<IsReadOnly; Struct>]
type CompilerOutput =
    { Raw: string
      BuildSucceeded: bool
      Diags: seq<string>
      BinutilsDiags: seq<string> }
with
    static member Parse(clc_output: string): CompilerOutput =
        let lines = ProcessOutputParser.splitInLines clc_output

        let diags = List<string>()
        let binutils_diags = List<string>()

        let mutable build_status_seen = false
        let mutable build_succeeded = false

        for line in lines do
            if not build_status_seen
            then
                diags.Add(line)
            else
                build_succeeded <- false
                binutils_diags.Add(line)

            if line.StartsWith("Build ")
            then
                build_status_seen <- true
                build_succeeded <- line.StartsWith("Build succeeded")

        { Raw = clc_output
          BuildSucceeded = build_succeeded
          Diags = diags
          BinutilsDiags = binutils_diags }


[<IsReadOnly; Struct>]
type ProgramOutput =
    { Output: seq<string> }
with
    static member Empty: ProgramOutput = { Output = [] }
    static member Parse(program_output: string): ProgramOutput =
        { Output = ProcessOutputParser.splitInLines program_output }


type CoolProgram private (_source_path: string, _exe_file: string) =


    member _.SourcePath: string = _source_path
    member _.ExeFile: string = _exe_file


    static member From(source_path: string): CoolProgram =
        // Build a program's path relative to the 'CoolBuild' folder.
        let source_path = Path.Combine(CompilerTestCaseSource.ProgramsPath, source_path)
                              .Replace("\\", "/")
        let exe_file = Path.GetFileNameWithoutExtension(source_path) + ".exe"
        CoolProgram(source_path, exe_file)


    member _.Compile(): CompilerOutput =
        let clc_output = ClcRunner.runInProcess [ _source_path; "-o"; _exe_file ]
        let compiler_output = CompilerOutput.Parse(clc_output)
        compiler_output


    member _.Run(std_input: seq<string>): ProgramOutput =
        let std_output = ProcessRunner.Run(
            exe_name= $"./{_exe_file}",
            args="",
            stdin_lines=std_input)

        let program_output = ProgramOutput.Parse(std_output)
        program_output
