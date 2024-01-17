namespace Tests.Lexer


open System
open System.Runtime.CompilerServices
open System.Text
open LibCool.SharedParts
open LibCool.ParserParts
open Tests.Support


[<Sealed; AbstractClass; Extension>]
type private StringBuilderExtensions private () =
    [<Extension>]
    static member Pad(this: StringBuilder, len: int, requiredLen: int): unit =
        if len < requiredLen
        then
             let paddingLength = requiredLen - len
             this.Append(new string(' ', paddingLength))
                 .AsUnit()


[<Sealed; AbstractClass; Extension>]
type TokenAsserts private() =


    static let appendMismatch (expected: StringBuilder)
                              (actual: StringBuilder)
                              (mismatch: Mismatch) =
        let expected_str = $"%d{mismatch.At}: %s{mismatch.Expected}; "
        let actual_str   = $"%d{mismatch.At}: %s{mismatch.Actual}; "

        expected
            .Append(expected_str)
            .Pad(len=expected.Length, requiredLen=actual.Length)

        actual
            .Append(actual_str)
            .Pad(len=actual.Length, requiredLen=expected.Length)


    static let formatMismatches (expected_len: int)
                                (actual_len: int)
                                (mismatches: seq<Mismatch>)
                                : string =
        let expected = StringBuilder("[")
        let actual = StringBuilder("[")

        mismatches |> Seq.iter (fun it -> appendMismatch expected actual it)

        expected.Append("]") |> ignore
        actual.Append("]") |> ignore

        let mismatch_positions = mismatches |> Seq.map (_.At)

        let message =
            StringBuilder()
                .AppendLine($"EXPECTED   [%i{expected_len}]:\t%s{expected.ToString()}")
                .AppendLine($"ACTUAL     [%i{actual_len}]:\t%s{actual.ToString()}")
                .AppendLine(sprintf "MISMATCHES [%i]: [%s]" (Seq.length mismatches)
                                                            (String.Join("; ", mismatch_positions)))
                .ToString()
        message


    [<Extension>]
    static member Match(assert_that: IAssertThat<Token[]>, expected: Token[]) =
        let expected_strs = expected |> Seq.map (_.Kind.ToString().Replace("Kind", ""))
        let actual_strs = assert_that.Actual |> Seq.map (_.Kind.ToString().Replace("Kind", ""))

        Assert.That(actual_strs).Match(expected_strs, formatMismatches)
