namespace LibCool.DiagnosticParts

open System.Collections.Generic
open System.Diagnostics

[<DebuggerDisplay("DiagnosticBag: Count = [{_diagnostics.Count}]")>]
type DiagnosticBag() =
    let _diagnostics = List<Diagnostic>()
    
    
    member _.Add(diagnostic: Diagnostic) =
        _diagnostics.Add(diagnostic)
        
        
    member _.ToReadOnlyList() : IReadOnlyList<Diagnostic> =
        _diagnostics :> IReadOnlyList<Diagnostic>
