namespace LibCool.Tests.Semantics

open System
open System.IO
open System.Runtime.CompilerServices
open Xunit
open LibCool.DiagnosticParts
open LibCool.Driver
open LibCool.Tests.Support


[<IsReadOnly; Struct>]
type CompilerTestCase =
    { InputFilePath: string }
    with
    override this.ToString() =
        Path.GetFileNameWithoutExtension(this.InputFilePath)
    


[<Sealed>]
type CompilerTestCaseSource private () =
    static let discover_compiler_test_cases () =
        [| [| { InputFilePath = "foo.cool"  } :> obj |] |]
    
    static member TestCases = discover_compiler_test_cases ()

type CompilerTests() =
    [<Theory>]
    [<MemberData("TestCases", MemberType=typeof<CompilerTestCaseSource>)>]
    member _.``Compiling a program produces the expected results``(tc: CompilerTestCase) =
        // Arrange
        let input_file = Path.GetFileNameWithoutExtension(tc.InputFilePath)
        
        use output = new StringWriter()
        Console.SetError(output)
        Console.SetOut(output)
        // TODO: Extract expected diagnostics from `input_file`
        let expected_diags: Diagnostic[] = [||]
        
        // Act
        let exit_code = Driver.Compile([ tc.InputFilePath; "-o"; input_file + ".exe" ])
        // TODO: Extract actual diagnostics from `output`
        let actual_diags: Diagnostic[] = [||]
        
        // Assert
        // TODO: Assert `exit_code` is success
        AssertDiags.Equal(
            expected=expected_diags,
            actual=actual_diags,
            // TODO: Probably we can extract `cool_snippet` from `input_file`
            // TODO: along with extracting the expected diagnostics 
            cool_snippet="")
