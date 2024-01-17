namespace Tests.GenGC


open System.IO
open System.Runtime.CompilerServices
open Tests.Support
open Xunit
open Xunit.Abstractions
open Tests.Compiler


[<Sealed; AbstractClass; Extension>]
type GenGCStateInfoAsserts private () =


    [<Extension>]
    static member IsEqualIgnoringHistoryTo(assert_that: IAssertThat<GenGCStateInfo>,
                                           expected: GenGCStateInfo): unit =
        Assert.Equal(expected={ expected with History = GenGCHistory.Empty },
                     actual  ={ assert_that.Actual with History = GenGCHistory.Empty })


    [<Extension>]
    static member Contains(assert_that: IAssertThat<GenGCStateInfo[]>,
                           predicate: GenGCStateInfo -> bool): unit =
        Assert.True(Array.exists predicate assert_that.Actual)


type GenGCTests(test_output: ITestOutputHelper) =


    do
        // We want to change the current directory to 'Tests/CoolBuild'.
        CompilerTestCaseSource.CwdCoolBuild()


    [<Fact>]
    member this.``When no allocs/collections the heap state doesn't change``() =
        // Arrange
        // Act
        let state_infos = this.CompileAndRun("Runtime/GenGC/NoAlloc1.cool")

        // Assert
        Assert.That(state_infos[1]).IsEqualTo(state_infos[0])


    [<Fact>]
    member this.``When no allocs/collections in a loop the heap state doesn't change``() =
        // Arrange
        // Act
        let state_infos = this.CompileAndRun("Runtime/GenGC/NoAlloc3.cool")

        // Assert
        let expected_state_info = state_infos[0]
        let mutable i = 1
        while (i < state_infos.Length) do
            Assert.That(state_infos[i]).IsEqualTo(expected_state_info)
            i <- i + 1


    [<Fact>]
    member this.``When no allocs collection doesn't change the heap state``() =
        // Arrange
        // Act
        let state_infos = this.CompileAndRun("Runtime/GenGC/NoAlloc2.cool")

        // Assert
        // We don't expect the histories to be the same as
        // the history naturally changes after each collection.
        Assert.That(state_infos[1]).IsEqualIgnoringHistoryTo(state_infos[0])


    [<Fact>]
    member this.``When no allocs collection in a loop doesn't change the heap state``() =
        // Arrange
        // Act
        let state_infos = this.CompileAndRun("Runtime/GenGC/NoAlloc4.cool")

        // Assert
        let expected_state_info = state_infos[0]
        let mutable i = 1
        while (i < state_infos.Length) do
            // We don't expect the histories to be the same as
            // the history naturally changes after each collection.
            Assert.That(state_infos[i]).IsEqualIgnoringHistoryTo(expected_state_info)
            i <- i + 1


    [<Fact>]
    member this.``An unreachable Gen0 object gets collected``() =
        // Arrange
        // Act
        let state_infos = this.CompileAndRun("Runtime/GenGC/Gen0-Collected1.cool")

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
        Assert.That(collected_state).IsEqualIgnoringHistoryTo(initial_state)


    [<Fact>]
    member this.``Unreachable Gen0 objects in a loop get collected``() =
        // Arrange
        // Act
        let state_infos = this.CompileAndRun("Runtime/GenGC/Gen0-Collected2.cool")

        // Assert
        let initial_state = state_infos[0]

        let mutable i = 1
        while i < state_infos.Length do
            let allocated_state = state_infos[i + 0]
            let collected_state = state_infos[i + 1]

            // In this test case,
            // the heap is not expected to get re-sized or re-arranged in any way.
            Assert.That(allocated_state.HeapInfo)
                  .IsEqualTo(initial_state.HeapInfo)

            // The test cool code is supposed to allocate 8 * 32 bytes.
            Assert.That(allocated_state.AllocInfo.AllocPtr)
                  .IsEqualTo(initial_state.AllocInfo.AllocPtr + 8L * 32L)

            // We expect the collection to remove all the allocated objects,
            // reverting the heap to its initial state as a result.
            // We don't expect the histories to be the same as
            // the history naturally changes after each collection.
            Assert.That(collected_state).IsEqualIgnoringHistoryTo(initial_state)

            i <- i + 2


    [<Fact>]
    member this.``A reachable Gen0 object gets promoted``() =
        // Arrange
        // Act
        let state_infos = this.CompileAndRun("Runtime/GenGC/Gen0-Promoted1.cool")

        // Assert
        let initial_state = state_infos[0]
        let allocated_state = state_infos[1]
        let collected_state = state_infos[2]

        // The test cool code is supposed to allocate 32 bytes.
        Assert.That(allocated_state.AllocInfo.AllocPtr)
              .IsEqualTo(initial_state.AllocInfo.AllocPtr + 32L)

        // The allocated object resides in Work Area prior to the collection.
        Assert.That(allocated_state.HeapInfo)
              .IsEqualTo(initial_state.HeapInfo)

        // The allocated object should've been promoted.
        Assert.That(collected_state.HeapInfo.L1)
              .IsEqualTo(initial_state.HeapInfo.L1 + 32L)


    [<Fact>]
    member this.``Reachable Gen0 objects in a loop get promoted``() =
        // Arrange
        // Act
        let state_infos = this.CompileAndRun("Runtime/GenGC/Gen0-Promoted2.cool")

        // Assert
        let mutable i = 1
        while i < state_infos.Length do
            let prev_state = state_infos[i - 1]
            let allocated_state = state_infos[i + 0]
            let collected_state = state_infos[i + 1]

            // The test cool code is supposed to allocate 8 * 32 bytes every iteration.
            Assert.That(allocated_state.AllocInfo.AllocPtr)
                  .IsEqualTo(prev_state.AllocInfo.AllocPtr + 8L * 32L)

            // After each collection
            // the reachable objects get promoted to Old Area [L0; L1).
            Assert.That(collected_state.HeapInfo.L1)
                  .IsEqualTo(allocated_state.HeapInfo.L1 + 8L * 32L)

            i <- i + 2

        let initial_state = state_infos[0]
        let final_state = state_infos[state_infos.Length - 1]

        // The test cool code is supposed to allocate 256 * 32 bytes in total.
        Assert.That(final_state.HeapInfo.L1)
              .IsEqualTo(initial_state.HeapInfo.L1 + 256L * 32L)


    [<Fact>]
    member this.``Allocating Gen0 objects triggers a collection``() =
        // Arrange
        // Act
        let state_infos = this.CompileAndRun("Runtime/GenGC/Alloc-Gen0-Triggers-Collection.cool")

        // Assert
        Assert.That(state_infos).Contains(fun it -> it.History.Minor0 <> 0)


    [<Fact>]
    member this.``An unreachable cycle gets collected``() =
        // Arrange
        // Act
        let state_infos = this.CompileAndRun("Runtime/GenGC/Cycle-Unreachable.cool")

        // Assert
        let initial_state = state_infos[0]
        let allocated_state = state_infos[1]
        let collected_state = state_infos[2]

        // We haven't allocated enough to trigger a heap resize.
        Assert.That(allocated_state.HeapInfo)
              .IsEqualTo(initial_state.HeapInfo)

        // The test cool code is supposed to allocate 80 bytes.
        Assert.That(allocated_state.AllocInfo.AllocPtr)
              .IsEqualTo(initial_state.AllocInfo.AllocPtr + 80L)

        // The allocated objects should've been collected.
        Assert.That(collected_state).IsEqualIgnoringHistoryTo(initial_state)


    [<Fact>]
    member this.``A reachable cycle gets promoted``() =
        // Arrange
        // Act
        let state_infos = this.CompileAndRun("Runtime/GenGC/Cycle-Reachable.cool")

        // Assert
        let initial_state = state_infos[0]
        let allocated_state = state_infos[1]
        let collected_state = state_infos[2]

        // We haven't allocated enough to trigger a heap resize.
        Assert.That(allocated_state.HeapInfo)
              .IsEqualTo(initial_state.HeapInfo)

        // The test cool code is supposed to allocate 80 bytes.
        Assert.That(allocated_state.AllocInfo.AllocPtr)
              .IsEqualTo(initial_state.AllocInfo.AllocPtr + 80L)

        // The allocated objects should've been promoted.
        Assert.That(collected_state.HeapInfo.L1)
              .IsEqualTo(initial_state.HeapInfo.L1 + 80L)


    [<Fact>]
    member this.``Promoting objects to Gen1 triggers a major collection``() =
        // Arrange
        // Act
        let state_infos = this.CompileAndRun("Runtime/GenGC/Promoting-Triggers-Collection.cool")

        // Assert
        Assert.That(state_infos).Contains(fun it -> it.History.Major0 <> 0)


    [<Fact>]
    member this.``Unreachable Gen1 objects get collected``() =
        // Arrange
        // Act
        let state_infos = this.CompileAndRun("Runtime/GenGC/Gen1-Collected.cool")

        // Assert
        let initial_state = state_infos[0]
        let collected_state = state_infos[1]

        // We don't expect the histories to be the same as
        // the history naturally changes after each collection.
        Assert.That(collected_state).IsEqualIgnoringHistoryTo(initial_state)


    [<Theory>]
    [<InlineData("NoLeak-Fibonacci")>]
    [<InlineData("NoLeak-InsertionSort")>]
    [<InlineData("NoLeak-Life")>]
    [<InlineData("NoLeak-QuickSort")>]
    member this.``No leaks``(source_file_name: string) =
        // Arrange
        // Act
        let state_infos = this.CompileAndRun($"Runtime/GenGC/{source_file_name}.cool")

        // Assert
        let initial_state = state_infos[0]
        let collected_state = state_infos[1]

        // Make sure
        // 1) Old Area doesn't contain any objects except the `Main` instance
        Assert.That(collected_state.HeapInfo.L0).IsEqualTo(initial_state.HeapInfo.L0)
        Assert.That(collected_state.HeapInfo.L1).IsEqualTo(initial_state.HeapInfo.L1)
        // 2) Work Area doesn't contain any objects -- i.e. it's empty
        Assert.That(collected_state.AllocInfo.AllocPtr - collected_state.HeapInfo.L2)
              .IsEqualTo(0)


    member private this.CompileAndRun(path: string): GenGCStateInfo[] =
        let test_program = CoolProgram.From(path)
        let snippet = File.ReadAllText(test_program.SourcePath)

        // Compile
        let compiler_output = test_program.Compile()

        test_output.WriteLine("===== clc: =====")
        test_output.WriteLine(compiler_output.Raw)

        Assert.That(compiler_output).IsBuildSucceeded(snippet)

        // Run
        let program_output = test_program.Run(std_input=[])
        Array.ofSeq (GenGCStateInfo.Parse(program_output.Output))
