namespace Tests.Compiler


open System.Collections.Generic
open System.IO
open LibCool.Ast
open LibCool.DiagnosticParts
open LibCool.Frontend
open LibCool.SourceParts
open Tests.Parser
open Xunit
open Xunit.Abstractions


type Sandbox(_test_output: ITestOutputHelper) =
    
    
    let render_diags (diagnostic_bag: DiagnosticBag) (source: Source): unit =
        for diag in diagnostic_bag.ToReadOnlyList() do
            let { FileName = file_name; Line = line; Col = col } = source.Map(diag.Span.First)
            _test_output.WriteLine(
                "{0}({1},{2}): {3}: {4}",
                file_name,
                line,
                col,
                (diag.Severity.ToString().Replace("Severity.", "")),
                diag.Message)

        if diagnostic_bag.ErrorsCount = 0
        then
            _test_output.WriteLine("Build succeeded: Errors: 0. Warnings: {0}", diagnostic_bag.WarningsCount)
        else
            _test_output.WriteLine("Build failed: Errors: {0}. Warnings: {1}", diagnostic_bag.ErrorsCount,
                                                                               diagnostic_bag.WarningsCount)
    
    
    let parse (path: string): Ast voption =
        // Prepare
        let path = Path.Combine(CompilerTestCaseSource.ProgramsPath, path).Replace("\\", "/")
        let tc = CompilerTestCase.ReadFrom(path)

        let source = Source([{ FileName = tc.FileName; Content = File.ReadAllText(path) }])
        let diagnostic_bag = DiagnosticBag()

        // Lex
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
            render_diags diagnostic_bag source
            ValueNone
        else

        let parser = Parser(tokens.ToArray(), diagnostic_bag)
        let ast = parser.Parse()
            
        if diagnostic_bag.ErrorsCount <> 0
        then
            render_diags diagnostic_bag source
            ValueNone
        else
            
        ValueSome ast
        

    [<Fact>]
    member _.PrintAst() =
        let ast = parse "Valid/ArithExprPrecedence.cool"
        let rendered = AstRenderer.Render(ast.Value)
        _test_output.WriteLine(rendered)
    