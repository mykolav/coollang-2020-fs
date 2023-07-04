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
    
    
    static let appendMismatch (expected: StringBuilder)
                              (actual: StringBuilder)
                              (mismatch: Mismatch) =
        let expected_str = $"%d{mismatch.At}: %s{mismatch.Expected}; "
        let actual_str   = $"%d{mismatch.At}: %s{mismatch.Actual}; "

        expected
            .Append(expected_str)
            .Pad(len=expected.Length, requiredLen=actual.Length)
            |> ignore
            
        actual
            .Append(actual_str)
            .Pad(len=actual.Length, requiredLen=expected.Length)
            |> ignore

    
    static let formatMismatches (expected_len: int)
                                (actual_len: int)
                                (mismatches: seq<Mismatch>) =
        let expected = StringBuilder("[")
        let actual = StringBuilder("[")
                                   
        mismatches |> Seq.iter (fun it -> appendMismatch expected actual it)

        expected.Append("]") |> ignore
        actual.Append("]") |> ignore

        let mismatch_positions = mismatches |> Seq.map (fun it -> it.At)

        let message =
            StringBuilder()
                .AppendLine($"EXPECTED   [%i{expected_len}]:\t%s{expected.ToString()}")
                .AppendLine($"ACTUAL     [%i{actual_len}]:\t%s{actual.ToString()}")
                .AppendLine(sprintf "MISMATCHES [%i]: [%s]" (Seq.length mismatches) 
                                                            (String.Join("; ", mismatch_positions)))
                .ToString()
        message


    static let strOfToken (token: Token) =
        sprintf "%s" (token.Kind.ToString().Replace("Kind", ""))

    
    static member Equal(expected: seq<Token>, actual: seq<Token>) = 
        let expected_strs = expected |> Seq.map (fun it -> strOfToken it)
        let actual_strs = actual |> Seq.map (fun it -> strOfToken it)
        
        let mismatches = StringSeq.compare expected_strs actual_strs
        AssertStringSeq.EmptyMismatches(mismatches,
                                        Seq.length expected_strs,
                                        Seq.length actual_strs,
                                        formatMismatches)
