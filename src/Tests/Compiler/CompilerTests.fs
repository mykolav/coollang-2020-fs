namespace Tests.Compiler


open System.IO
open LibCool.SharedParts
open Tests.Support
open Xunit
open Xunit.Abstractions
open Tests.Compiler.ClcRunner


type CompilerTests(test_output: ITestOutputHelper) =


    do
        // We want to change the current directory to 'Tests/CoolBuild'.
        CompilerTestCaseSource.CwdCoolBuild()


    [<Theory>]
    [<MemberData("TestCases", MemberType = typeof<CompilerTestCaseSource>)>]
    member this.``Compile and run``(path: string) =
        this.CompileAndRun(path)


    member private this.CompileAndRun(path: string) =
        // Build a program's path relative to the 'CoolBuild' folder.
        let path = Path.Combine(CompilerTestCaseSource.ProgramsPath, path).Replace("\\", "/")
        let test_case = CompilerTestCase.ParseFrom(path)
        let exe_file = test_case.FileName + ".exe"

        // Compile
        let clc_output = runClcInProcess [ path; "-o"; exe_file ]

        test_output.WriteLine("===== clc: =====")
        test_output.WriteLine(clc_output)

        let compiler_output = CompilerOutput.Parse(clc_output)

        // Ensure the actual compilation results match the expected
        Assert.That(compiler_output).IsExpectedBy(test_case)

        // Run
        for run in test_case.Runs do
            let std_output = ProcessRunner.Run(
                exe_name= $"./%s{exe_file}", args="", stdin_lines=run.GivenInput)
            let program_output = ProgramOutput.Parse(std_output)

            // Ensure the actual execution results match the expected
            Assert.That(program_output).IsExpectedBy(test_case, run)
