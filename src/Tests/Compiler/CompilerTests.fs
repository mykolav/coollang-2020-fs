namespace Tests.Compiler


open Tests.Support
open Xunit
open Xunit.Abstractions


type CompilerTests(test_output: ITestOutputHelper) =


    do
        // We want to change the current directory to 'Tests/CoolBuild'.
        CompilerTestCaseSource.CwdCoolBuild()


    [<Theory>]
    [<MemberData("TestCases", MemberType = typeof<CompilerTestCaseSource>)>]
    member this.``Compile and run``(path: string) =
        let test_program = CoolProgram.From(path)
        let test_case = CompilerTestCase.ParseFrom(test_program.SourcePath)

        // Compile
        let compiler_output = test_program.Compile()

        test_output.WriteLine("===== clc: =====")
        test_output.WriteLine(compiler_output.Raw)

        Assert.That(compiler_output).IsExpectedBy(test_case)

        // Run
        for run in test_case.Runs do
            let program_output = test_program.Run(std_input=run.GivenInput)
            Assert.That(program_output).IsExpectedBy(test_case, run)
