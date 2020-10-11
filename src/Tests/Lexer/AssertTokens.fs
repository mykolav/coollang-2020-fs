namespace Tests.Lexer


open System
open System.Text
open LibCool.ParserParts
open Tests.Support


[<AutoOpen>]
module private StringBuilderExtensions =
    
    
    type StringBuilder with
        member this.Pad(len: int, requiredLen: int) =
            if len < requiredLen 
            then let paddingLength = requiredLen - len
                 this.Append(new string(' ', paddingLength)) |> ignore
            this


[<Sealed>]
type AssertTokens private() =
    
    
    static let append_mismatch (expected: StringBuilder)
                               (actual: StringBuilder)
                               (mismatch: Mismatch) =
        let expected_str = sprintf "%d: %s; " mismatch.At mismatch.Expected
        let actual_str   = sprintf "%d: %s; " mismatch.At mismatch.Actual

        expected
            .Append(expected_str)
            .Pad(len=expected.Length, requiredLen=actual.Length)
            |> ignore
            
        actual
            .Append(actual_str)
            .Pad(len=actual.Length, requiredLen=expected.Length)
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

        let message =
            StringBuilder()
                .AppendLine(sprintf "EXPECTED   [%i]:\t%s" expected_len (expected.ToString()))
                .AppendLine(sprintf "ACTUAL     [%i]:\t%s" actual_len (actual.ToString()))
                .AppendLine(sprintf "MISMATCHES [%i]: [%s]" (Seq.length mismatches) 
                                                            (String.Join("; ", mismatch_positions)))
                .ToString()
        message


    static let str_of_token (token: Token) =
        sprintf "%s" (token.Kind.ToString().Replace("Kind", ""))

    
    static member Equal(expected: seq<Token>, actual: seq<Token>) = 
        let expected_strs = expected |> Seq.map (fun it -> str_of_token it)
        let actual_strs = actual |> Seq.map (fun it -> str_of_token it)
        
        let mismatches = StringSeq.compare expected_strs actual_strs
        AssertStringSeq.EmptyMismatches(mismatches,
                                        Seq.length expected_strs,
                                        Seq.length actual_strs,
                                        format_mismatches)
