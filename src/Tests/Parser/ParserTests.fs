namespace Tests.Parser


open Xunit
open LibCool.DiagnosticParts
open LibCool.SourceParts
open LibCool.ParserParts
open Tests.Support


type ParserTestCase =
    { Snippet: Snippet }
    with
    override this.ToString() = this.Snippet.ToDisplayString()


[<Sealed>]
type ParserTestCaseSource private () =
    
    
    static let mapParserTestCases (snippets: string[]) =
        snippets |> Array.map (fun it -> [| { ParserTestCase.Snippet = Snippet(it) } :> obj |])

    
    static member ParserTestCases = mapParserTestCases [|
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

        // Act
        let ast = Parser.Parse(TokenArray.ofLexer lexer, diagnostic_bag)
        let rendered = if diagnostic_bag.ErrorsCount = 0 then CoolRenderer.Render(ast) else ""
        
        // Assert
        Assert.Equal(expected=0, actual=diagnostic_bag.ErrorsCount)
        AssertSnippets.EqualIgnoringWhitespace(
            expected = tc.Snippet.ToString(),
            actual = rendered)
