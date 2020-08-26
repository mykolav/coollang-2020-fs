namespace LibCool.DiagnosticParts


open System.Runtime.CompilerServices
open LibCool.SourceParts


[<Struct>]
type DiagnosticSeverity =
    | Info
    | Warning
    | Error

    
[<IsReadOnly; Struct>]
type Diagnostic =
    { Span: Range
      Severity: DiagnosticSeverity
      Message: string }
    with
    static member Of(severity, message, span) = { Span = span; Severity = severity; Message = message }
