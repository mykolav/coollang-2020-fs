namespace LibCool.Tests.Parser

open System.Runtime.CompilerServices
open Xunit
open LibCool.DiagnosticParts
open LibCool.Frontend
open LibCool.SourceParts
open LibCool.Tests


[<IsReadOnly; Struct>]
type ParsedOKTestCase =
    { Snippet: Snippet }
    with
    override this.ToString() = this.Snippet.ToDisplayString()


[<Sealed>]
type ParserTestCaseSource private () =
    static let map_parsed_ok_test_cases (snippets: string[]) =
        snippets
            |> Array.map (fun it -> [| { Snippet = Snippet(it) } :> obj |])

    
    static member ParsedOKTestCases = map_parsed_ok_test_cases [|
        ""
        CoolSnippets.Fib
        CoolSnippets.QuickSort
    |]


type ParserTests() =
    [<Theory>]
    [<MemberData("ParsedOKTestCases", MemberType=typeof<ParserTestCaseSource>)>]
    member _.``Parsed OK``(tc: ParsedOKTestCase) =
        // Arrange
        let source = Source([ { FileName = "parsed-ok-test.cool"; Content = tc.Snippet.ToString() } ])
        let diagnosticBag = DiagnosticBag()
        let lexer = Lexer(source, diagnosticBag)
        let parser = Parser(lexer, diagnosticBag)

        // Act
        let ast = parser.Parse()
        let diags = diagnosticBag.ToReadOnlyList()
        let rendered = if diags.Count = 0 then CoolRenderer.Render(ast) else ""
        
        // Assert
        Assert.Empty(diags)
        AssertSnippets.EqualIgnoringWhitespace(
            expected = tc.Snippet.ToString(),
            actual = rendered)
