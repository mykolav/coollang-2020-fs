namespace Tests.Support


open System
open System.Text
open Tests.Compiler


[<Sealed>]
type AssertCompilerTestCaseOutput private () =


    static let format_mismatches (title: string)
                                 (cool_snippet: string)
                                 (expected_len: int)
                                 (actual_len: int)
                                 (mismatches: seq<Mismatch>): string =
        let mismatch_positions = mismatches |> Seq.map (fun it -> it.At)

        let message = StringBuilder()
        message
            .AppendLine(title)
            .AppendLine($"EXPECTED   [%i{expected_len}]")
            .AppendLine($"ACTUAL     [%i{actual_len}]")
            .AppendLine(sprintf "MISMATCHES [%i]: [%s]" (Seq.length mismatches)
                                                        (String.Join("; ", mismatch_positions)))
            .AppendLine()
            |> ignore

        mismatches |> Seq.iter (fun it -> message.AppendLine($"E [%i{it.At}]: %s{it.Expected}")
                                                 .AppendLine($"A [%i{it.At}]: %s{it.Actual}")
                                                 .AppendLine() |> ignore)
        
        message
            .AppendLine()
            .AppendLine(cool_snippet)
            .ToString()


    static member Matches(tc: CompilerTestCase, co: CompilerOutput, po: ProgramOutput) =
        let actual_diags =
            co.Diags
            |> Seq.map (fun it -> if it.StartsWith(CompilerTestCaseSource.ProgramsPath)
                                  then it.Replace(CompilerTestCaseSource.ProgramsPath, "")
                                  else it)
                           
        let diag_mismatches = StringSeq.compare tc.ExpectedDiags actual_diags
        AssertStringSeq.EmptyMismatches(diag_mismatches,
                                        Seq.length tc.ExpectedDiags,
                                        Seq.length co.Diags,
                                        format_mismatches "DIAGS:" tc.Snippet)

        let expected_binutils_diags = []
        let actual_binutils_diags = co.BinutilsDiags
        let binutils_diag_mismatches = StringSeq.compare expected_binutils_diags actual_binutils_diags
        AssertStringSeq.EmptyMismatches(binutils_diag_mismatches,
                                        Seq.length expected_binutils_diags,
                                        Seq.length actual_binutils_diags,
                                        format_mismatches "BINUTILS DIAGS:" tc.Snippet)

        let output_mismatches = StringSeq.compare tc.ExpectedOutput po.Output
        AssertStringSeq.EmptyMismatches(output_mismatches,
                                        Seq.length tc.ExpectedOutput,
                                        Seq.length po.Output,
                                        format_mismatches "OUTPUT" tc.Snippet)
