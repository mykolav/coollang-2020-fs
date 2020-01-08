namespace LibCool.Tests.Lexer

open LibCool.Frontend

[<RequireQualifiedAccess>]
type AssertTokens() =
    static let str_of_token (token: Token) =
        sprintf "%s" (token.Kind.ToString().Replace("Kind", ""))

    static member Equal(expected: seq<Token>, actual: seq<Token>) = 
        let expected_strs = expected |> Seq.map (fun it -> str_of_token it)
        let actual_strs = actual |> Seq.map (fun it -> str_of_token it)
        AssertStringSeq.Equal(expected_strs, actual_strs)
