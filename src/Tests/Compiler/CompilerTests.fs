namespace Tests.Compiler


open System
open System.IO
open Tests.Support
open Xunit
open Xunit.Abstractions
open Tests.Compiler.ProcessRunner


type CompilerTests(test_output: ITestOutputHelper) =


    [<Theory>]
    [<MemberData("TestCases", MemberType = typeof<CompilerTestCaseSource>)>]
    member _.``A program ``(path: string) =
        // Arrange
        let path = Path.Combine(CompilerTestCaseSource.ProgramsPath, path)
        let tc = CompilerTestCase.ReadFrom(path)

        // Act
        let clc_output = run_clc (String.Join(" ", [ path; "-o"; tc.FileName + ".exe" ]))
        
        test_output.WriteLine("===== clc: =====")
        test_output.WriteLine(clc_output)
        
        let co = CompilerOutput.Parse(clc_output)

        // Assert
        AssertCompilerOutput.Matches(tc, co)
