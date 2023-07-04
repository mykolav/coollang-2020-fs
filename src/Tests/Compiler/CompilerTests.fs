namespace Tests.Compiler


open System.IO
open LibCool.SharedParts
open Tests.Support
open Xunit
open Xunit.Abstractions
open Tests.Compiler.ClcRunner


[<Collection("Compiler collection")>]
type CompilerTests(test_output: ITestOutputHelper) =


    [<Theory>]
    [<MemberData("TestCases", MemberType = typeof<CompilerTestCaseSource>)>]
    member this.``Compile and run``(path: string) =
        this.CompileAndRun(path)
        
    
    [<Fact>]
    member this.``Compile and run InString.cool``() =
        this.CompileAndRun("Runtime/InString.cool", stdin="Bond, James Bond")
        
    
    [<Fact>]
    member this.``Compile and run InString1.cool``() =
        this.CompileAndRun("Runtime/InString1.cool", stdin="Elizabeth Alexandra Mary Windsor")
        
    
    [<Fact>]
    member this.``Compile and run InInt.cool``() =
        this.CompileAndRun("Runtime/InInt.cool", stdin="9001")
        
    
    [<Fact>]
    member this.``Compile and run InInt1.cool``() =
        this.CompileAndRun("Runtime/InInt1.cool", stdin="+9001")
        
    
    [<Fact>]
    member this.``Compile and run InInt2.cool``() =
        this.CompileAndRun("Runtime/InInt2.cool", stdin="-9001")
        
    
    [<Fact>]
    member this.``Compile and run InInt3.cool``() =
        this.CompileAndRun("Runtime/InInt3.cool", stdin="a9001")
        
    
    [<Fact>]
    member this.``Compile and run InInt4.cool``() =
        this.CompileAndRun("Runtime/InInt4.cool", stdin="9001a")
        
    
    [<Fact>]
    member this.``Compile and run InInt5.cool``() =
        this.CompileAndRun("Runtime/InInt5.cool", stdin="12345678912")
        
    
    [<Fact>]
    member this.``Compile and run Life.cool``() =
        this.CompileAndRun("Runtime/Life.cool", stdin="...................>...................>....xxx...xxx......>...................>..x....x.x....x....>..x....x.x....x....>..x....x.x....x....>....xxx...xxx......>...................>....xxx...xxx......>..x....x.x....x....>..x....x.x....x....>..x....x.x....x....>...................>....xxx...xxx......>...................>...................>...................>...................>...................>")
        

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
                let output = ProcessRunner.Run(exe_name= $"./%s{exe_file}",
                                               args="",
                                               ?stdin=stdin)
                ProgramOutput.Parse(output)
            else
                ProgramOutput.Empty

        // Assert
        AssertCompilerTestCaseOutput.Matches(tc, co, po)
