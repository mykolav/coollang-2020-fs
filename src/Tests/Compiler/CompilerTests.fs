namespace Tests.Compiler


open System
open System.IO
open LibCool.SharedParts
open Tests.Support
open Xunit
open Xunit.Abstractions
open Tests.Compiler.ClcRunner


type CompilerTestsFixture() =

    
    do 
        // Our current directory is 'Tests/bin/Debug/netcoreapp3.1',
        // i.e. where the tests assembly gets build into.
        // We want to change to 'Tests/CoolBuild'.
        Directory.SetCurrentDirectory("../../../CoolBuild")


    interface IDisposable with
        member _.Dispose() = ()


type CompilerTests(test_output: ITestOutputHelper) =
    interface IClassFixture<CompilerTestsFixture>


    [<Theory>]
    [<MemberData("TestCases", MemberType = typeof<CompilerTestCaseSource>)>]
    member this.``Compile and run``(path: string) =
        this.CompileAndRun(path)
        
    
    [<Fact>]
    member this.``Compile and run InString.cool``() =
        this.CompileAndRun("Runtime/InString.cool", stdin="Bond, James Bond")


    member private this.CompileAndRun(path: string, ?stdin: string) =
        // Arrange
        
        // Build a program's path relative to the 'CoolBuild' folder.
        let path = Path.Combine(CompilerTestCaseSource.ProgramsPath, path).Replace("\\", "/")
        let tc = CompilerTestCase.ReadFrom(path)
        let exe_file = tc.FileName + ".exe"

        // Act
        let clc_output = run_clc_in_process ([ path; "-o"; exe_file ])
        
        test_output.WriteLine("===== clc: =====")
        test_output.WriteLine(clc_output)
        
        let co = CompilerOutput.Parse(clc_output)
        let po =
            if co.BuildSucceeded
            then
                let output = ProcessRunner.Run(exe_name=sprintf "./%s" exe_file,
                                               args="",
                                               ?stdin=stdin)
                ProgramOutput.Parse(output)
            else
                ProgramOutput.Empty

        // Assert
        AssertCompilerTestCaseOutput.Matches(tc, co, po)
