namespace LibCool.Tests.Support

open System
open System.Text
open LibCool.DiagnosticParts

[<RequireQualifiedAccess>]
type AssertDiags() =
    static let either defaultValue handleSome (option: 'a option) =
        match option with
        | Some value -> handleSome value
        | None       -> defaultValue

    static let format_mismatches (expected_len: int)
                          (actual_len: int)
                          (mismatches: seq<Mismatch>) =
        let expected = StringBuilder("[")
        let actual = StringBuilder("[")
                                   

        expected.Append("]") |> ignore
        actual.Append("]") |> ignore

        let mismatch_positions = mismatches |> Seq.map (fun it -> it.At)

        let message = StringBuilder()
        message
            .AppendLine(sprintf "EXPECTED [%i]" expected_len)
            .AppendLine(sprintf "ACTUAL   [%i]" actual_len)
            .AppendLine(sprintf "MISMATCHES [%i]: [%s]" (Seq.length mismatches)
                                                        (String.Join("; ", mismatch_positions)))
            |> ignore

        mismatches |> Seq.iter (fun it -> message.AppendLine(sprintf "E [%i]: %s" it.At it.Expected)
                                                 .AppendLine(sprintf "A [%i]: %s" it.At it.Actual)
                                                 .AppendLine() |> ignore)
        
        message.ToString()

    static let str_of_diag (diag: Diagnostic) =
        sprintf "[%d..%d]: %s" diag.Span.First diag.Span.Last (diag.Severity.ToString())
        
    static member Equal(expected: seq<Diagnostic>, actual: seq<Diagnostic>, cool_snippet: string) =
        let mismatches = Seq.zipAll expected actual
                         |> Seq.mapi (fun i (e, a) -> { At = i
                                                        Expected = either "<NONE>" str_of_diag e
                                                        Actual = either "<NONE>" str_of_diag a })
                         |> Seq.filter (fun it -> it.Expected <> it.Actual)

        if Seq.any mismatches
        then
            let message = format_mismatches (Seq.length expected) 
                                            (Seq.length actual) 
                                            mismatches
            raise (Xunit.Sdk.XunitException(message))

        let expected_strs = expected |> Seq.map (fun it -> str_of_diag it)
        let actual_strs = actual |> Seq.map (fun it -> str_of_diag it)
        AssertStringSeq.Equal(expected_strs, actual_strs)


