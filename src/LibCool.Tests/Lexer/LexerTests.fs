namespace LibCool.Tests.Lexer

open System.Runtime.CompilerServices
open LibCool.DiagnosticParts
open LibCool.Frontend
open LibCool.SourceParts
open LibCool.Tests

[<IsReadOnly; Struct>]
type Snippet = Snippet of content: string
    with
    member this.ToDisplayString() =
        let (Snippet content) = this
        "\"" +
        (if content.Length > 100 
        then content.[0..100] + "..."
        else content) +
        "\""
        
    override this.ToString() =
        let (Snippet content) = this
        content

type TokenTestCase =
    { Snippet: Snippet
      Expected: Token[] }
    with
    override this.ToString() = this.Snippet.ToDisplayString()

type InvalidNumberTestCase =
    { Snippet: Snippet
      Expected: Diagnostic[] }
    with
    override this.ToString() = this.Snippet.ToDisplayString()
        
type LexerTestCaseSource() =
    static let map_token_test_cases (tuples: (string * Token[])[]) =
        tuples |> Array.map (fun (snippet, expected) ->
                                 [| { TokenTestCase.Snippet = Snippet(snippet)
                                      Expected = expected } :> obj |])

    static let map_invalid_number_test_cases (tuples: (string * Diagnostic[])[]) =
        tuples |> Array.map (fun (snippet, expected) ->
                                 [| { InvalidNumberTestCase.Snippet = Snippet(snippet)
                                      Expected = expected } :> obj |])
        
    [<Literal>]
    static let qqqStringContent1 = "This starts a string literal\r\nthat continues on for several lines\neven though it includes \"'s and \\'s and newline characters\r\nin a \"wild\" profu\\sion\\\\ of normally i\\\\egal t\"ings.\\"
    [<Literal>]
    static let qqqStringContent2 = "This starts a string literal\r\nthat /*continues*/ on for several lines\n// even though it includes \"'s and \\'s and newline characters\r\nin a \"wild\" profu\\sion\\\\ of normally i\\\\egal t\"ings.\\"

    static member TokenTestCases = map_token_test_cases [|
        //
        // Whitespace
        //
        "", [||] 
        " ", [||] 
        "  ", [||] 
        "   ", [||] 
        "    ", [||] 
        "\t", [||] 
        " \t", [||] 
        " \t ", [||] 
        "  \t  ", [||] 
        "   \t   ", [||] 
        "    \t    ", [||] 
        "\t\t", [||] 
        " \t\t", [||] 
        " \t\t ", [||] 
        "  \t\t  ", [||] 
        "   \t\t   ", [||] 
        "    \t\t    ", [||] 
        "\t\t\t", [||] 
        " \t\t\t", [||] 
        " \t\t\t ", [||] 
        "  \t\t\t  ", [||] 
        "   \t\t\t   ", [||] 
        "    \t\t\t    ", [||] 
        " \t \t \t ", [||] 
        "  \t  \t  \t  ", [||] 
        "   \t   \t   \t   ", [||] 
        "    \t    \t    \t    ", [||]
        //
        // Identifiers
        //
        "Frobnitz", [| T.ID("Frobnitz") |]
        "frobnitz", [| T.ID("frobnitz") |]
        "_frobnitz", [| T.ID("_frobnitz") |]
        "frobnitz_", [| T.ID("frobnitz_") |]
        "__frobnitz", [| T.ID("__frobnitz") |]
        "frobnitz__", [| T.ID("frobnitz__") |]
        "__f_r_o_b_n_i_t_z__", [| T.ID("__f_r_o_b_n_i_t_z__") |]
        "Corge_grault", [| T.ID("Corge_grault") |]
        "corge_grault", [| T.ID("corge_grault") |]
        "corge0grault", [| T.ID("corge0grault") |]
        "corge9001grault", [| T.ID("corge9001grault") |]
        "__corge_9001_grault__", [| T.ID("__corge_9001_grault__") |]
        "QUUX", [| T.ID("QUUX") |]
        "__QUUZ__", [| T.ID("__QUUZ__") |]
        "_9001corge", [| T.ID("_9001corge") |]
        "_9001_corge", [| T.ID("_9001_corge") |]
        "grault9001", [| T.ID("grault9001") |]
        "grault_9001", [| T.ID("grault_9001") |]
        //
        // String literals
        //
        "\"\"",                       [| T.Str "" |]
        "\"Hello, world!\"",          [| T.Str "Hello, world!" |]
        "\"He//o, world!\"",          [| T.Str "He//o, world!" |]
        "\"Hello/*, world!\"",        [| T.Str "Hello/*, world!" |]
        "\"Hello/*, world!*/\"",      [| T.Str "Hello/*, world!*/" |]
        "\"Power level: 9001\"",      [| T.Str "Power level: 9001" |]
        "\"Power \\0 level: 9001\"",  [| T.Str "Power \0 level: 9001" |]
        "\"Power \\b level: 9001\"",  [| T.Str "Power \b level: 9001" |]
        "\"Power \\t level: 9001\"",  [| T.Str "Power \t level: 9001" |]
        "\"Power \\n level: 9001\"",  [| T.Str "Power \n level: 9001" |]
        "\"Power \\r level: 9001\"",  [| T.Str "Power \r level: 9001" |]
        "\"Power \\f level: 9001\"",  [| T.Str "Power \f level: 9001" |]
        "\"Power \\\" level: 9001\"", [| T.Str "Power \" level: 9001" |]
        "\"Power \\\\ level: 9001\"", [| T.Str "Power \\ level: 9001" |]
        //
        // Triple quote string literals
        //
        (sprintf "\"\"\"%s\"\"\"" qqqStringContent1), [| T.QQQ(qqqStringContent1) |]
        (sprintf "\"\"\"%s\"\"\"" qqqStringContent2), [| T.QQQ(qqqStringContent2) |]
        //
        // Int literals
        //
        "0",            [| T.Int(0) |]
        "01",           [| T.Int(1) |]
        "1",            [| T.Int(1) |]
        "9001",         [| T.Int(9001) |]
        " 9001",        [| T.Int(9001) |]
        "  9001",       [| T.Int(9001) |]
        "\t9001",       [| T.Int(9001) |]
        " \t9001",      [| T.Int(9001) |]
        "\t 9001",      [| T.Int(9001) |]
        "\r\n9001",     [| T.Int(9001) |]
        "\r\n 9001",    [| T.Int(9001) |]
        "\r\n\t9001",   [| T.Int(9001) |]
        "\r\n\t\t9001", [| T.Int(9001) |]
        "9001 ",        [| T.Int(9001) |]
        "9001  ",       [| T.Int(9001) |]
        "9001\t",       [| T.Int(9001) |]
        "9001 \t",      [| T.Int(9001) |]
        "9001\r\n",     [| T.Int(9001) |]
        "9001\r\n ",    [| T.Int(9001) |]
        "9001\r\n\t",   [| T.Int(9001) |]
        "9001\r\n\t\t", [| T.Int(9001) |]
        //
        // Comments
        //
        "/*Power level = */9001",           [| T.Int(9001) |]
        "9001/*= Power level*/",            [| T.Int(9001) |]
        "// Power level = 9001",            [| |]
        "9001// = Power level",             [| T.Int(9001) |]
        "90/* Power level */01",            [| T.Int(90); T.Int(01) |]
        "90//01 = Power level",             [| T.Int(90) |]
        "/*Power // level = */9001 9002",   [| T.Int(9001); T.Int(9002) |]
        "9001/*= Power // level*/",         [| T.Int(9001) |]
        "// Power /* level = 9001\r\n9002", [| T.Int(9002) |]
        "9001// = Power /* level\r\n9002",  [| T.Int(9001); T.Int(9002) |]
        //
        // Punctuators
        //
        "+",   [| T.Plus |]
        "-",   [| T.Minus |]
        "/",   [| T.Slash |]
        "*",   [| T.Star |]
        "/ *", [| T.Slash; T.Star |]
        "* /", [| T.Star; T.Slash |]
        "=",   [| T.Eq |]
        "==",  [| T.EqEq |]
        "= =", [| T.Eq; T.Eq |]
        "<",   [| T.Lt |]
        ">",   [| T.Gt |]
        "<=",  [| T.LtEq |]
        "< =", [| T.Lt; T.Eq |]
        ">=",  [| T.GtEq |]
        "> =", [| T.Gt; T.Eq |]
        "=>",  [| T.EqGt |]
        "= >", [| T.Eq; T.Gt |]
        "!",   [| T.Ex |]
        "!=",  [| T.ExEq |]
        "! =", [| T.Ex; T.Eq |]
        "(",   [| T.LPar |]
        ")",   [| T.RPar |]
        "[",   [| T.LSq |]
        "]",   [| T.RSq |]
        "{",   [| T.LBr |]
        "}",   [| T.RBr |]
        ":",   [| T.Colon |]
        ";",   [| T.Semi |]
        ".",   [| T.Dot |]
        ",",   [| T.Comma |]
        
        // Keywords
        "case", [| T.Case |] 
        "Case", [| T.ID("Case") |] 

        "class", [| T.Class |] 
        "Class", [| T.ID("Class") |] 

        "def", [| T.Def |] 
        "Def", [| T.ID("Def") |] 

        "else", [| T.Else |] 
        "Else", [| T.ID("Else") |] 

        "extends", [| T.Extends |] 
        "Extends", [| T.ID("Extends") |] 

        "false", [| T.False |] 
        "False", [| T.ID("False") |] 

        "if", [| T.If |] 
        "If", [| T.ID("If") |] 

        "match", [| T.Match |] 
        "Match", [| T.ID("Match") |] 

        "native", [| T.Native |]
        "Native", [| T.ID("Native") |]

        "new", [| T.New |] 
        "New", [| T.ID("New") |] 

        "null", [| T.Null |] 
        "Null", [| T.ID("Null") |] 

        "override", [| T.Override |] 
        "Override", [| T.ID("Override") |] 

        "super", [| T.Super |] 
        "Super", [| T.ID("Super") |] 

        "this", [| T.This |] 
        "This", [| T.ID("This") |] 

        "true", [| T.True |] 
        "True", [| T.ID("True") |] 

        "var", [| T.Var |] 
        "Var", [| T.ID("Var") |] 

        "while", [| T.While |] 
        "While", [| T.ID("While") |] 

        // Illegal/reserved keywords
        "abstract", [| T.Abstract |] 
        "Abstract", [| T.ID("Abstract") |] 

        "catch", [| T.Catch |] 
        "Catch", [| T.ID("Catch") |] 

        "do", [| T.Do |] 
        "Do", [| T.ID("Do") |] 

        "final", [| T.Final |] 
        "Final", [| T.ID("Final") |] 

        "finally", [| T.Finally |] 
        "Finally", [| T.ID("Finally") |] 

        "for", [| T.For |] 
        "For", [| T.ID("For") |] 

        "forSome", [| T.ForSome |] 
        "ForSome", [| T.ID("ForSome") |] 

        "implicit", [| T.Implicit |] 
        "Implicit", [| T.ID("Implicit") |] 

        "import", [| T.Import |] 
        "Import", [| T.ID("Import") |] 

        "lazy", [| T.Lazy |] 
        "Lazy", [| T.ID("Lazy") |] 

        "object", [| T.Object |] 
        "Object", [| T.ID("Object") |] 

        "package", [| T.Package |] 
        "Package", [| T.ID("Package") |] 

        "private", [| T.Private |] 
        "Private", [| T.ID("Private") |] 

        "protected", [| T.Protected |] 
        "Protected", [| T.ID("Protected") |] 

        "requires", [| T.Requires |] 
        "Requires", [| T.ID("Requires") |] 

        "return", [| T.Return |] 
        "Return", [| T.ID("Return") |] 

        "sealed", [| T.Sealed |] 
        "Sealed", [| T.ID("Sealed") |] 

        "throw", [| T.Throw |] 
        "Throw", [| T.ID("Throw") |] 

        "trait", [| T.Trait |] 
        "Trait", [| T.ID("Trait") |] 

        "try", [| T.Try |] 
        "Try", [| T.ID("Try") |] 

        "type", [| T.Type |] 
        "Type", [| T.ID("Type") |] 

        "val", [| T.Val |] 
        "Val", [| T.ID("Val") |] 

        "with", [| T.With |] 
        "With", [| T.ID("With") |] 

        "yield", [| T.Yield |]
        "Yield", [| T.ID("Yield") |]
    |]
    
    static member InvalidNumberTestCases = map_invalid_number_test_cases [|
        "0corge",    [| { Code = InvalidNumber; Span = { First = 0u; Last = 1u } } |]
        "9001corge", [| { Code = InvalidNumber; Span = { First = 0u; Last = 4u } } |]
        "90a1",      [| { Code = InvalidNumber; Span = { First = 0u; Last = 2u } } |]
        "9.001",     [| { Code = InvalidNumber; Span = { First = 0u; Last = 1u } } |]
    |]

open Xunit

type LexerTests() =
    let get_tokens (lexer: Lexer) =
        seq {
            yield lexer.Current
            while lexer.MoveNext() do
                yield lexer.Current
        }
    
    [<Theory>]
    [<MemberData("TokenTestCases", MemberType=typeof<LexerTestCaseSource>)>]
    member _.``Lex``(tc: TokenTestCase) =
        // Arrange
        let expected_tokens = Array.append tc.Expected [| T.EOF |]
        let source = Source([ { FileName = "lexer-test.cool"; Content = tc.Snippet.ToString() } ])
        let lexer = Lexer(source)

        // Act
        let actual_tokens = lexer |> get_tokens |> Array.ofSeq
        
        // Assert
        AssertTokens.Equal(expected = expected_tokens, actual = actual_tokens)

    [<Theory>]
    [<MemberData("InvalidNumberTestCases", MemberType=typeof<LexerTestCaseSource>)>]
    member _.``Lex invalid number``(tc: InvalidNumberTestCase) =
        // Arrange
        let source = Source([ { FileName = "lexer-test.cool"; Content = tc.Snippet.ToString() } ])
        let lexer = Lexer(source)

        // Act
        lexer |> get_tokens |> ignore
        
        // Assert
        AssertDiags.Equal(expected = tc.Expected, actual = lexer.Diagnostics)
