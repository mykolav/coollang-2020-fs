namespace LibCool.DiagnosticParts


open System.Collections.Generic
open System.Diagnostics
open LibCool.SourceParts


[<DebuggerDisplay("DiagnosticBag: Count = [{_diagnostics.Count}]")>]
type DiagnosticBag() =
    let _diagnostics = List<Diag>()
    

    member _.ErrorsCount = _diagnostics |> Seq.filter(fun it -> it.Severity = Severity.Error) |> Seq.length
    member _.WarningsCount = _diagnostics |> Seq.filter(fun it -> it.Severity = Severity.Warning) |> Seq.length
    
    
    member this.Error(message: string, span: Span) =
        _diagnostics.Add(Diag.Of(Severity.Error, message, span))
        
        
    member this.Warn(message: string, span: Span) =
        _diagnostics.Add(Diag.Of(Severity.Warning, message, span))
        
        
    member this.Info(message: string, span: Span) =
        _diagnostics.Add(Diag.Of(Severity.Info, message, span))
        
        
    member _.ToReadOnlyList() : IReadOnlyList<Diag> =
        _diagnostics :> IReadOnlyList<Diag>
