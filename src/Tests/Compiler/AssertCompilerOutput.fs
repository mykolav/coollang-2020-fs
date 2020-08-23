namespace Tests.Support


open System
open System.Text
open Tests.Compiler


[<Sealed>]
type AssertCompilerOutput private () =


    static let format_mismatches (title: string)
                                 (cool_snippet: string)
                                 (expected_len: int)
                                 (actual_len: int)
                                 (mismatches: seq<Mismatch>): string =
        let mismatch_positions = mismatches |> Seq.map (fun it -> it.At)

        let message = StringBuilder()
        message
            .AppendLine(title)
            .AppendLine(sprintf "EXPECTED   [%i]" expected_len)
            .AppendLine(sprintf "ACTUAL     [%i]" actual_len)
            .AppendLine(sprintf "MISMATCHES [%i]: [%s]" (Seq.length mismatches)
                                                        (String.Join("; ", mismatch_positions)))
            .AppendLine()
            |> ignore

        mismatches |> Seq.iter (fun it -> message.AppendLine(sprintf "E [%i]: %s" it.At it.Expected)
                                                 .AppendLine(sprintf "A [%i]: %s" it.At it.Actual)
                                                 .AppendLine() |> ignore)
        
        message
            .AppendLine()
            .AppendLine(cool_snippet)
            .ToString()


    static member Matches(tc: CompilerTestCase, co: CompilerOutput) =
        let actual_diags = co.Diags
                           |> Seq.map (fun it -> if it.StartsWith(CompilerTestCaseSource.ProgramsPath)
                                                 then it.Replace(CompilerTestCaseSource.ProgramsPath, "")
                                                 else it)
                           
        let diag_mismatches = StringSeq.compare tc.ExpectedDiags actual_diags
        AssertStringSeq.EmptyMismatches(diag_mismatches,
                                        Seq.length tc.ExpectedDiags,
                                        Seq.length co.Diags,
                                        format_mismatches "DIAGS:" tc.Snippet)

        let output_mismatches = StringSeq.compare tc.ExpectedOutput co.Output
        AssertStringSeq.EmptyMismatches(output_mismatches,
                                        Seq.length tc.ExpectedOutput,
                                        Seq.length co.Output,
                                        format_mismatches "OUTPUT" tc.Snippet)
