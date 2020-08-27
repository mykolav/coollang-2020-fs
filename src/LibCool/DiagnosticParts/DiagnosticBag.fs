namespace LibCool.DiagnosticParts

open System.Collections.Generic
open System.Diagnostics
open LibCool.SourceParts

[<DebuggerDisplay("DiagnosticBag: Count = [{_diagnostics.Count}]")>]
type DiagnosticBag() =
    let _diagnostics = List<Diag>()
    

    member _.ErrorsCount = _diagnostics |> Seq.filter(fun it -> it.Severity = Severity.Error) |> Seq.length
    member _.WarningsCount = _diagnostics |> Seq.filter(fun it -> it.Severity = Severity.Warning) |> Seq.length
    
    
    member _.Add(diagnostic: Diag) =
        _diagnostics.Add(diagnostic)
        
        
    member this.Add(severity: Severity, message: string, span: Span) =
        _diagnostics.Add(Diag.Of(severity, message, span))
        
        
    member _.ToReadOnlyList() : IReadOnlyList<Diag> =
        _diagnostics :> IReadOnlyList<Diag>
