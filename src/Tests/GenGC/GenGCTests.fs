namespace Tests.GenGC


open System.IO
open LibCool.SharedParts
open Tests.Support
open Xunit
open Xunit.Abstractions
open Tests.Compiler
open Tests.Compiler.ClcRunner


type GenGCTests(test_output: ITestOutputHelper) =


    [<Fact>]
    member this.``When no allocs/collections the heap state doesn't change``() =
        // Arrange
        // Act
        let program_output = this.CompileAndRun("Runtime/GenGC/NoAlloc1.cool")
        let state_infos = Array.ofSeq (GenGCStateInfo.Parse(program_output.Output))
        // Assert
        Assert.Equal(expected=state_infos[0], actual=state_infos[1])


    [<Fact>]
    member this.``When no allocs/collections in a loop the heap state doesn't change``() =
        // Arrange
        // Act
        let program_output = this.CompileAndRun("Runtime/GenGC/NoAlloc3.cool")
        let state_infos = Array.ofSeq (GenGCStateInfo.Parse(program_output.Output))
        // Assert
        let expected_state_info = state_infos[0]
        let mutable i = 1
        while (i < state_infos.Length) do
            Assert.Equal(expected=expected_state_info, actual=state_infos[i])
            i <- i + 1


    [<Fact>]
    member this.``When no allocs collection doesn't change the heap state``() =
        // Arrange
        // Act
        let program_output = this.CompileAndRun("Runtime/GenGC/NoAlloc2.cool")
        let state_infos = Array.ofSeq (GenGCStateInfo.Parse(program_output.Output))
        // Assert
        // We don't expect the histories to be the same as
        // the history naturally changes after each collection.
        Assert.Equal(expected=state_infos[0].HeapInfo, actual=state_infos[1].HeapInfo)
        Assert.Equal(expected=state_infos[0].StackBase, actual=state_infos[1].StackBase)
        Assert.Equal(expected=state_infos[0].AllocInfo, actual=state_infos[1].AllocInfo)


    [<Fact>]
    member this.``When no allocs collection in a loop doesn't change the heap state``() =
        // Arrange
        // Act
        let program_output = this.CompileAndRun("Runtime/GenGC/NoAlloc4.cool")
        let state_infos = Array.ofSeq (GenGCStateInfo.Parse(program_output.Output))
        // Assert
        let expected_state_info = state_infos[0]
        let mutable i = 1
        while (i < state_infos.Length) do
            // We don't expect the histories to be the same as
            // the history naturally changes after each collection.
            Assert.Equal(expected=expected_state_info.HeapInfo, actual=state_infos[i].HeapInfo)
            Assert.Equal(expected=expected_state_info.StackBase, actual=state_infos[i].StackBase)
            Assert.Equal(expected=expected_state_info.AllocInfo, actual=state_infos[i].AllocInfo)
            i <- i + 1


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
