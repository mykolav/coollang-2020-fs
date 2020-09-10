namespace Tests.Compiler


open System
open System.Collections.Generic
open System.IO
open LibCool.Ast
open LibCool.DiagnosticParts
open LibCool.Driver
open LibCool.Frontend
open LibCool.SourceParts
open Tests.Parser
open Xunit
open Xunit.Abstractions


type Sandbox(_test_output: ITestOutputHelper) =
    
    
    let parse (path: string): Ast voption =
        // Prepare
        let path = Path.Combine(CompilerTestCaseSource.ProgramsPath, path).Replace("\\", "/")
        let tc = CompilerTestCase.ReadFrom(path)

        let source = Source([{ FileName = tc.FileName; Content = File.ReadAllText(path) }])
        let diagnostic_bag = DiagnosticBag()

        // Lex
        let do_parse () =
            let tokens = List<Token>()

            let lexer = Lexer(source, diagnostic_bag)
            let mutable token = lexer.GetNext()
            tokens.Add(token)
                
            while token.Kind <> TokenKind.EOF do
                token <- lexer.GetNext()
                tokens.Add(token)

            // Parse
            if diagnostic_bag.ErrorsCount <> 0
            then
                ValueNone
            else

            let parser = Parser(tokens.ToArray(), diagnostic_bag)
            let ast = parser.Parse()
                
            if diagnostic_bag.ErrorsCount <> 0
            then
                ValueNone
            else
                
            ValueSome ast
        
        let ast_opt = do_parse()
        
        Driver.RenderDiags(diagnostic_bag,
                           source,
                           { new IWriteLine with
                               member _.WriteLine(format: string, [<ParamArray>] args: obj[]) =
                                   _test_output.WriteLine(format, args)})
        ast_opt
        

    [<Fact>]
    member _.PrintAst() =
        let ast = parse "Valid/ArithExprPrecedence.cool"
        let rendered = AstRenderer.Render(ast.Value)
        _test_output.WriteLine(rendered)
    