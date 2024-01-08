namespace Tests.GenGC


open System.IO
open LibCool.SharedParts
open Tests.Support
open Xunit
open Xunit.Abstractions
open Tests.Compiler
open Tests.Compiler.ClcRunner


type GenGCTests(test_output: ITestOutputHelper) =


    do
        // We want to change the current directory to 'Tests/CoolBuild'.
        CompilerTestCaseSource.CwdCoolBuild()


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


    [<Fact>]
    member this.``An unreachable Gen0 object gets collected``() =
        // Arrange
        // Act
        let program_output = this.CompileAndRun("Runtime/GenGC/Gen0-Collected1.cool")
        let state_infos = Array.ofSeq (GenGCStateInfo.Parse(program_output.Output))

        // Assert
        let initial_state = state_infos[0]
        let allocated_state = state_infos[1]
        let collected_state = state_infos[2]

        // We haven't allocated enough to trigger a heap resize.
        Assert.Equal(expected=initial_state.HeapInfo, actual=allocated_state.HeapInfo)

        // The test cool code is supposed to allocate 32 bytes.
        Assert.Equal(expected=initial_state.AllocInfo.AllocPtr + 32L,
                     actual  =allocated_state.AllocInfo.AllocPtr)

        // We don't expect the histories to be the same as
        // the history naturally changes after each collection.
        Assert.Equal(expected={ initial_state with History = GenGCHistory.Empty },
                     actual  ={ collected_state with History = GenGCHistory.Empty })


    [<Fact>]
    member this.``Unreachable Gen0 objects in a loop get collected``() =
        // Arrange
        // Act
        let program_output = this.CompileAndRun("Runtime/GenGC/Gen0-Collected2.cool")
        let state_infos = Array.ofSeq (GenGCStateInfo.Parse(program_output.Output))

        // Assert
        let initial_state = state_infos[0]

        let mutable i = 1
        while i < state_infos.Length do
            let allocated_state = state_infos[i + 0]
            let collected_state = state_infos[i + 1]

            Assert.Equal(expected=initial_state.HeapInfo, actual=allocated_state.HeapInfo)

            // The test cool code is supposed to allocate 8 * 32 bytes.
            Assert.Equal(expected=initial_state.AllocInfo.AllocPtr + 8L * 32L,
                         actual  =allocated_state.AllocInfo.AllocPtr)

            // We don't expect the histories to be the same as
            // the history naturally changes after each collection.
            Assert.Equal(expected={ initial_state with History = GenGCHistory.Empty },
                         actual  ={ collected_state with History = GenGCHistory.Empty })

            i <- i + 2


    [<Fact>]
    member this.``A reachable Gen0 object gets promoted``() =
        // Arrange
        // Act
        let program_output = this.CompileAndRun("Runtime/GenGC/Gen0-Promoted1.cool")
        let state_infos = Array.ofSeq (GenGCStateInfo.Parse(program_output.Output))

        // Assert
        let initial_state = state_infos[0]
        let allocated_state = state_infos[1]
        let collected_state = state_infos[2]

        // The test cool code is supposed to allocate 32 bytes.
        Assert.Equal(expected=initial_state.AllocInfo.AllocPtr + 32L,
                     actual  =allocated_state.AllocInfo.AllocPtr)

        // The allocated object resides in Work Area prior to the collection.
        Assert.Equal(expected=initial_state.HeapInfo,
                     actual  =allocated_state.HeapInfo)

        // The allocated object should've been promoted.
        Assert.Equal(expected=initial_state.HeapInfo.L1 + 32L,
                     actual  =collected_state.HeapInfo.L1)


    [<Fact>]
    member this.``Reachable Gen0 objects in a loop get promoted``() =
        // Arrange
        // Act
        let program_output = this.CompileAndRun("Runtime/GenGC/Gen0-Promoted2.cool")
        let state_infos = Array.ofSeq (GenGCStateInfo.Parse(program_output.Output))

        // Assert
        let mutable i = 1
        while i < state_infos.Length do
            let prev_state = state_infos[i - 1]
            let allocated_state = state_infos[i + 0]
            let collected_state = state_infos[i + 1]

            // The test cool code is supposed to allocate 8 * 32 bytes every iteration.
            // After each collection `AllocPtr` gets reset to its original position.
            Assert.Equal(expected=prev_state.AllocInfo.AllocPtr + 8L * 32L,
                         actual  =allocated_state.AllocInfo.AllocPtr)

            Assert.Equal(expected=allocated_state.HeapInfo.L1 + 8L * 32L,
                         actual  =collected_state.HeapInfo.L1)

            i <- i + 2

        let initial_state = state_infos[0]
        let final_state = state_infos[state_infos.Length - 1]
        Assert.Equal(expected=initial_state.HeapInfo.L1 + 256L * 32L,
                     actual  =final_state.HeapInfo.L1)


    [<Fact>]
    member this.``Allocating Gen0 objects triggers a collection``() =
        // Arrange
        // Act
        let program_output = this.CompileAndRun("Runtime/GenGC/Alloc-Gen0-Triggers-Collection.cool")
        let state_infos = Array.ofSeq (GenGCStateInfo.Parse(program_output.Output))

        // Assert
        Assert.True(Array.exists (fun it -> it.History.Minor0 <> 0) state_infos)


    [<Fact>]
    member this.``An unreachable cycle gets collected``() =
        // Arrange
        // Act
        let program_output = this.CompileAndRun("Runtime/GenGC/Cycle-Unreachable.cool")
        let state_infos = Array.ofSeq (GenGCStateInfo.Parse(program_output.Output))

        // Assert
        let initial_state = state_infos[0]
        let allocated_state = state_infos[1]
        let collected_state = state_infos[2]

        // We haven't allocated enough to trigger a heap resize.
        Assert.Equal(expected=initial_state.HeapInfo, actual=allocated_state.HeapInfo)

        // The test cool code is supposed to allocate 32 bytes.
        Assert.Equal(expected=initial_state.AllocInfo.AllocPtr + 80L,
                     actual  =allocated_state.AllocInfo.AllocPtr)

        // We don't expect the histories to be the same as
        // the history naturally changes after each collection.
        Assert.Equal(expected={ initial_state with History = GenGCHistory.Empty },
                     actual  ={ collected_state with History = GenGCHistory.Empty })


    [<Fact>]
    member this.``A reachable cycle gets promoted``() =
        // Arrange
        // Act
        let program_output = this.CompileAndRun("Runtime/GenGC/Cycle-Reachable.cool")
        let state_infos = Array.ofSeq (GenGCStateInfo.Parse(program_output.Output))

        // Assert
        let initial_state = state_infos[0]
        let allocated_state = state_infos[1]
        let collected_state = state_infos[2]

        // The test cool code is supposed to allocate 32 bytes.
        Assert.Equal(expected=initial_state.AllocInfo.AllocPtr + 80L,
                     actual  =allocated_state.AllocInfo.AllocPtr)

        // The allocated objects reside in Work Area prior to the collection.
        Assert.Equal(expected=initial_state.HeapInfo,
                     actual  =allocated_state.HeapInfo)

        // The allocated objects should've been promoted.
        Assert.Equal(expected=initial_state.HeapInfo.L1 + 80L,
                     actual  =collected_state.HeapInfo.L1)


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
