namespace Tests.GenGC


open System.IO
open LibCool.SharedParts
open Tests.Support
open Xunit
open Xunit.Abstractions
open Tests.Compiler
open Tests.Compiler.ClcRunner


[<Collection("Compiler collection")>]
type GenGCTests(test_output: ITestOutputHelper) =


    [<Fact>]
    member this.``When no allocs/collections the heap state doesn't change``() =
        // Arrange
        // Act
        let program_output = this.CompileAndRun("Runtime/GenGC/NoAlloc1.cool")
        let state_infos = Array.ofSeq (GenGCStateInfo.Parse(program_output.Output))
        // Assert
        Assert.Equal(expected=state_infos[0], actual=state_infos[1])


    member private this.CompileAndRun(path: string): ProgramOutput =
        // Build a program's path relative to the 'CoolBuild' folder.
        let path = Path.Combine(CompilerTestCaseSource.ProgramsPath, path).Replace("\\", "/")

        let snippet = File.ReadAllText(path)
        let exe_file = Path.GetFileNameWithoutExtension(path) + ".exe"

        // Compile
        let clc_output = runClcInProcess [ path; "-o"; exe_file ]

        test_output.WriteLine("===== clc: =====")
        test_output.WriteLine(clc_output)

        let compiler_output = CompilerOutput.Parse(clc_output)

        // Ensure the compilation has succeeded.
        CompilerTestAssert.Match(
            compiler_output,
            [ "Build succeeded: Errors: 0. Warnings: 0" ],
            snippet)

        let std_output = ProcessRunner.Run(exe_name= $"./%s{exe_file}", args="")
        let program_output = ProgramOutput.Parse(std_output)
        program_output
