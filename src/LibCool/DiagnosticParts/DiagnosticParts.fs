namespace LibCool.DiagnosticParts


open System.Runtime.CompilerServices
open LibCool.SourceParts


[<RequireQualifiedAccess; Struct>]
type Severity =
    | Info
    | Warning
    | Error

    
[<Struct; IsReadOnly>]
type Diag =
    { Span: Span
      Severity: Severity
      Message: string }
    with
    static member Of(severity, message, span) = { Span = span; Severity = severity; Message = message }
