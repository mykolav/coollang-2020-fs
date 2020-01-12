namespace LibCool.Tests.Semantics

open System.Runtime.CompilerServices
open Xunit
open LibCool.DiagnosticParts
open LibCool.Frontend
open LibCool.SourceParts
open LibCool.Tests.Support


[<IsReadOnly; Struct>]
type SemanticTestCase =
    { Snippet: Snippet
      ExpectedDiags: Diagnostic[] }
    with
    override this.ToString() = this.Snippet.ToDisplayString()


[<Sealed>]
type SemanticTestCaseSource private () =
    static let map_semantic_test_cases (tuples: (string * Diagnostic[]) []) =
        tuples
            |> Array.map (fun (snippet, diags) -> [| { Snippet = Snippet(snippet)
                                                       ExpectedDiags = diags } :> obj |])

    
    static member TestCases = map_semantic_test_cases [|
        "",
        [|  |]
        
        CoolWithSemanticDiagsSnippets.Snippet1,
        CoolWithSemanticDiagsSnippets.Snippet1Diags
        
        CoolWithSemanticDiagsSnippets.Snippet2,
        CoolWithSemanticDiagsSnippets.Snippet2Diags
    |]


type SemanticTests() =
    [<Theory>]
    [<MemberData("TestCases", MemberType=typeof<SemanticTestCaseSource>)>]
    member _.``Parsed OK``(tc: SemanticTestCase) =
        // Arrange
        let source = Source([ { FileName = "semantic-diags.cool"; Content = tc.Snippet.ToString() } ])
        let diagnostic_bag = DiagnosticBag()
        let lexer = Lexer(source, diagnostic_bag)
        let ast = Parser(lexer, diagnostic_bag).Parse()
        let parse_diags = diagnostic_bag.ToReadOnlyList()

        // Act
        let sema_diags = SemanticAnalyzer.Analyze(ast)
        
        // Assert
        Assert.Empty(parse_diags)
        AssertDiags.Equal(
            expected = tc.ExpectedDiags,
            actual = sema_diags)
