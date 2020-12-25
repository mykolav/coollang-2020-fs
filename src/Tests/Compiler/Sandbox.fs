namespace Tests.Compiler


open System.IO
open Xunit
open Xunit.Abstractions
open LibCool.AstParts
open LibCool.DiagnosticParts
open LibCool.DriverParts
open LibCool.SourceParts
open LibCool.ParserParts
open Tests.Parser
open Tests.Compiler.ClcRunner 


[<Collection("Compiler collection")>]
type Sandbox(_test_output: ITestOutputHelper) =
    
    
    let parse (path: string): ProgramSyntax voption =
        // Prepare
        let path = Path.Combine(CompilerTestCaseSource.ProgramsPath, path).Replace("\\", "/")
        let tc = CompilerTestCase.ReadFrom(path)

        let source = Source([{ FileName = tc.FileName; Content = File.ReadAllText(path) }])
        let diagnostic_bag = DiagnosticBag()

        // Lex
        let do_parse () =

            let lexer = Lexer(source, diagnostic_bag)
            let tokens = TokenArray.ofLexer lexer

            // Parse
            if diagnostic_bag.ErrorsCount <> 0
            then
                ValueNone
            else

            let ast = Parser.Parse(tokens, diagnostic_bag)
                
            if diagnostic_bag.ErrorsCount <> 0
            then
                ValueNone
            else
                
            ValueSome ast
        
        let ast_opt = do_parse()
        
        DiagRenderer.Render(diagnostic_bag,
                            source,
                            { new IWriteLine with
                                member _.WriteLine(line: string) =
                                    _test_output.WriteLine(line)})
        ast_opt
        

    [<Fact>]
    member _.``Print ast``() =
        let ast = parse "Runtime/ArithExprPrecedence.cool"
        //let ast = parse "Runtime/IfElseExprPrecedence.cool"
        //let ast = parse "../../../sandbox/cool/HelloWorld.cool"
        let rendered = AstRenderer.Render(ast.Value)
        _test_output.WriteLine(rendered)


    [<Fact>]
    member _.``Print asm``() =
        // Arrange
        //let path = Path.Combine(CompilerTestCaseSource.ProgramsPath, "Runtime/ArithExprPrecedence.cool")
        //let path = Path.Combine(CompilerTestCaseSource.ProgramsPath, "Runtime/IfElseExprPrecedence.cool")
        //let path = Path.Combine(CompilerTestCaseSource.ProgramsPath, "Runtime/QuickSort.cool")
        //let path = Path.Combine(CompilerTestCaseSource.ProgramsPath, "Runtime/InsertionSort.cool")
        //let path = Path.Combine(CompilerTestCaseSource.ProgramsPath, "Runtime/HelloCool1.cool")
        //let path = Path.Combine(CompilerTestCaseSource.ProgramsPath, "Runtime/HelloCool2.cool")
        let path = Path.Combine(CompilerTestCaseSource.ProgramsPath, "Runtime/Abort1.cool")
        //let path = Path.Combine(CompilerTestCaseSource.ProgramsPath, "Runtime/Heap1.cool")

        // Act
        let clc_output = run_clc_in_process ([ path; "-S" ])
        
        _test_output.WriteLine("===== clc: =====")
        _test_output.WriteLine(clc_output)
