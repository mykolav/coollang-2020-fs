namespace LibCool.DiagnosticParts

open System.Runtime.CompilerServices
open LibCool.SourceParts

[<Struct>]
type DiagnosticSeverity =
    | Info
    | Warning
    | Error
//    | UnexpectedCharSeq
//    | InvalidIdentifier
//    | InvalidNumber
//    | InvalidString
//    | InvalidQqqString
//    | InvalidKeyword
    
[<IsReadOnly; Struct>]
type Diagnostic =
    { Span: HalfOpenRange
      Severity: DiagnosticSeverity
      Message: string }
