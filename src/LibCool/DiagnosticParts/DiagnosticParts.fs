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
    { Span: HalfOpenRange
      Severity: DiagnosticSeverity
      Message: string }
