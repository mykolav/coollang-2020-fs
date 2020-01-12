namespace LibCool.Frontend

open System.Collections.Generic
open LibCool.Ast
open LibCool.DiagnosticParts

[<Sealed>]
type SemanticAnalyzer private () =
    static member Analyze(ast: Ast) : IReadOnlyList<Diagnostic> =
        List<Diagnostic>() :> IReadOnlyList<Diagnostic>
