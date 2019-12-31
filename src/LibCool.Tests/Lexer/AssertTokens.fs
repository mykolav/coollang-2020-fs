namespace LibCool.Tests.Lexer

open LibCool.Frontend

module private Seq =
    let any source = not (Seq.isEmpty source)
    let zipAll (xs: seq<'X>) (ys: seq<'Y>) =
        seq {
            let ex = xs.GetEnumerator()
            let ey = ys.GetEnumerator()

            let mutable haveX = true
            let mutable haveY = true

            while haveX || haveY do
                haveX <- ex.MoveNext()
                haveY <- ey.MoveNext()
                if haveX && haveY then yield (Some ex.Current, Some ey.Current)
                else if haveX     then yield (Some ex.Current, None)
                else if haveY     then yield (None, Some ey.Current)
        }

module private StringBuilderExtensions =
    open System.Text
    
    type StringBuilder with
        member this.Pad(len: int, requiredLen: int) =
            if len < requiredLen 
            then let paddingLength = requiredLen - len
                 this.Append(new string(' ', paddingLength)) |> ignore
            this

open System
open System.Text
open System.IO
open System.Runtime.CompilerServices
open StringBuilderExtensions

[<IsReadOnly; Struct>]
type private Mismatch =
    { Expected: Token option
      Actual: Token option
      At: int }

type AssertTokens() =
    static let either defaultValue handleSome (option: 'a option) =
        match option with
        | Some value -> handleSome value
        | None       -> defaultValue

    static let str_of_value (kind: TokenKind) =
        match kind with
        | Invalid value                   -> value.ToString()
        | Identifier value                -> value.ToString() 
        | IntLiteral value                -> value.ToString()
        | StringLiteral value             -> value.ToString()
        | TripleQuotedStringLiteral value -> value.ToString()
        | _                               -> ""
    
    static let str_of_token (token: Token) =
        sprintf "%s" (token.Kind.ToString().Replace("Kind", ""))
        
    static let append_mismatch (expected: StringBuilder)
                               (actual: StringBuilder)
                               (mismatch: Mismatch) =
        let expected_str = sprintf "%d: %s; " mismatch.At (either "<NONE>" str_of_token mismatch.Expected)
        let actual_str   = sprintf "%d: %s; " mismatch.At (either "<NONE>" str_of_token mismatch.Actual)

        expected
            .Append(expected_str)
            .Pad(len = expected.Length, 
                 requiredLen = actual.Length)
            |> ignore
            
        actual
            .Append(actual_str)
            .Pad(len = actual.Length, 
                 requiredLen = expected.Length)
            |> ignore

    static let format_mismatches (expected_len: int)
                                 (actual_len: int)
                                 (mismatches: seq<Mismatch>) =
        let expected = StringBuilder("[")
        let actual = StringBuilder("[")
                                   
        mismatches |> Seq.iter (fun it -> append_mismatch expected actual it)

        expected.Append("]") |> ignore
        actual.Append("]") |> ignore

        let mismatch_positions = mismatches |> Seq.map (fun it -> it.At)

        use message = new StringWriter()
        fprintfn message "EXPECTED [%i]:\t%s" expected_len (expected.ToString())
        fprintfn message "ACTUAL   [%i]:\t%s" actual_len (actual.ToString())
        fprintfn message "MISMATCHES [%i]: [%s]" (Seq.length mismatches) 
                                                 (String.Join("; ", mismatch_positions))
        message.ToString()


    static let is_match (expected: Token option) (actual: Token option) =
        match expected, actual with
        | None, None                  -> invalidOp "kind and token cannot both be None"
        | Some _, None | None, Some _ -> false
        | Some e, Some a              ->
            e.Kind = a.Kind && (str_of_value e.Kind) = (str_of_value a.Kind)

    static member Equal(expected: seq<Token>, actual: seq<Token>) = 
        let mismatches = Seq.zipAll expected actual
                         |> Seq.mapi (fun i (e, a) -> { Expected = e; Actual = a; At = i })
                         |> Seq.filter (fun it -> not (is_match it.Expected it.Actual))

        if Seq.any mismatches
        then
            let message = format_mismatches (Seq.length expected) 
                                            (Seq.length actual) 
                                            mismatches
            raise (Xunit.Sdk.XunitException(message))
