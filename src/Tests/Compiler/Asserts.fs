namespace Tests.Support


open System
open System.Runtime.CompilerServices
open System.Text
open LibCool.SharedParts
open Tests.Compiler


[<RequireQualifiedAccess>]
module private Mismatches =


    let format (title: string)
               (cool_snippet: string)
               (expected_len: int)
               (actual_len: int)
               (mismatches: seq<Mismatch>): string =

        let mismatch_positions = mismatches |> Seq.map (_.At)

        let message =
            StringBuilder()
                .AppendLine(title)
                .AppendLine($"EXPECTED   [%i{expected_len}]")
                .AppendLine($"ACTUAL     [%i{actual_len}]")
                .AppendLine(sprintf "MISMATCHES [%i]: [%s]" (Seq.length mismatches)
                                                            (String.Join("; ", mismatch_positions)))
                .AppendLine()

        mismatches |> Seq.iter (fun it -> message.AppendLine($"E [%i{it.At}]: %s{it.Expected}")
                                                 .AppendLine($"A [%i{it.At}]: %s{it.Actual}")
                                                 .AppendLine()
                                                 .AsUnit())

        message.AppendLine()
               .AppendLine(cool_snippet)
               .ToString()


[<Sealed; AbstractClass; Extension>]
type CompilerOutputAsserts private () =


    [<Extension>]
    static member Match(assert_that: IAssertThat<CompilerOutput>,
                        expected_diags: seq<string>,
                        snippet: string): unit =
        let actual_diags = assert_that.Actual.Diags
                           |> Seq.map (_.Replace(CompilerTestCaseSource.ProgramsPath, ""))

        Assert.That(actual_diags)
              .Match(expected_diags, Mismatches.format "DIAGS:" snippet)

        Assert.That(assert_that.Actual.BinutilsDiags)
              .AreEmpty(Mismatches.format "BINUTILS DIAGS:" snippet)


    [<Extension>]
    static member IsExpectedBy(assert_that: IAssertThat<CompilerOutput>,
                               test_case: CompilerTestCase): unit =
        let actual_diags = assert_that.Actual.Diags
                           |> Seq.map (_.Replace(CompilerTestCaseSource.ProgramsPath, ""))

        Assert.That(actual_diags)
              .Match(test_case.ExpectedDiags, Mismatches.format "DIAGS:" test_case.Snippet)

        Assert.That(assert_that.Actual.BinutilsDiags)
              .AreEmpty(Mismatches.format "BINUTILS DIAGS:" test_case.Snippet)


    [<Extension>]
    static member IsBuildSucceeded(assert_that: IAssertThat<CompilerOutput>,
                                   snippet: string): unit =
        Assert.That(assert_that.Actual)
              .Match([ "Build succeeded: Errors: 0. Warnings: 0" ], snippet)


[<Sealed; AbstractClass; Extension>]
type ProgramOutputAsserts private () =


    [<Extension>]
    static member Matches(assert_that: IAssertThat<ProgramOutput>,
                          expected_output: seq<string>,
                          snippet: string) =
        Assert.That(assert_that.Actual.Output)
              .Match(expected_output, Mismatches.format "OUTPUT" snippet)


    [<Extension>]
    static member IsExpectedBy(assert_that: IAssertThat<ProgramOutput>,
                               test_case: CompilerTestCase,
                               run: CompilerTestCaseRun) =
        Assert.That(assert_that.Actual.Output)
              .Match(run.ExpectedOutput, Mismatches.format "OUTPUT" test_case.Snippet)
