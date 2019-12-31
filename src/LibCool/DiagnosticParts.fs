namespace LibCool.DiagnosticParts

open System.Runtime.CompilerServices
open LibCool.SourceParts

[<Struct>]
type DiagnosticCode =
    | UnexpectedCharSeq
    | InvalidIdentifier
    | InvalidNumber
    | InvalidString
    | InvalidQqqString
    | InvalidKeyword
    
[<IsReadOnly; Struct>]
type Diagnostic =
    { Code: DiagnosticCode
      Span: HalfOpenRange }
