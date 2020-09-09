namespace Tests.Parser


open System.Runtime.CompilerServices
open Xunit
open LibCool.DiagnosticParts
open LibCool.Frontend
open LibCool.SourceParts
open Tests.Support


type ParserTestCase =
    { Snippet: Snippet }
    with
    override this.ToString() = this.Snippet.ToDisplayString()


[<Sealed>]
type ParserTestCaseSource private () =
    
    
    static let map_parser_test_cases (snippets: string[]) =
        snippets |> Array.map (fun it -> [| { ParserTestCase.Snippet = Snippet(it) } :> obj |])

    
    static member ParserTestCases = map_parser_test_cases [|
        ""
        CoolSnippets.Fib
        CoolSnippets.QuickSort
        CoolSnippets.InsertionSort
    |]


type ParserTests() =
   
   
    [<Theory>]
    [<MemberData("ParserTestCases", MemberType=typeof<ParserTestCaseSource>)>]
    member _.Parse(tc: ParserTestCase) =
        // Arrange
        let source = Source([ { FileName = "parser-test-case.cool"; Content = tc.Snippet.ToString() } ])
        let diagnostic_bag = DiagnosticBag()

        let lexer = Lexer(source, diagnostic_bag)
        let parser = Parser(TokenArray.ofLexer lexer, diagnostic_bag)

        // Act
        let ast = parser.Parse()
        let diags = diagnostic_bag.ToReadOnlyList()
        let rendered = if diags.Count = 0 then CoolRenderer.Render(ast) else ""
        
        // Assert
        Assert.Empty(diags)
        AssertSnippets.EqualIgnoringWhitespace(
            expected = tc.Snippet.ToString(),
            actual = rendered)
