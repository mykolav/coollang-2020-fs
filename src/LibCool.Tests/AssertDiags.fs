namespace LibCool.Tests

open LibCool.DiagnosticParts
open LibCool.Tests.Lexer

[<RequireQualifiedAccess>]
type AssertDiags() =
    static let str_of_diag (diag: Diagnostic) =
        sprintf "[%d..%d]: %s" diag.Span.First diag.Span.Last (diag.Code.ToString())
        
    static member Equal(expected: seq<Diagnostic>, actual: seq<Diagnostic>) =
        let expected_strs = expected |> Seq.map (fun it -> str_of_diag it)
        let actual_strs = actual |> Seq.map (fun it -> str_of_diag it)
        AssertStringSeq.Equal(expected_strs, actual_strs)


