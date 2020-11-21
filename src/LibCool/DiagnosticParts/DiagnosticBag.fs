namespace LibCool.DiagnosticParts


open System.Collections.Generic
open System.Diagnostics
open LibCool.SourceParts


[<DebuggerDisplay("DiagnosticBag: Count = [{_diagnostics.Count}]")>]
type DiagnosticBag() =
    let _diagnostics = List<Diag>()
    let _binutils_errors = List<string>()
        
        
    member _.Diags : IReadOnlyList<Diag> =
        _diagnostics :> IReadOnlyList<Diag>
        
        
    member _.BinutilsErrors : IReadOnlyList<string> =
        _binutils_errors :> IReadOnlyList<string>
    

    member _.ErrorsCount =
        _binutils_errors.Count +
        (_diagnostics |> Seq.filter(fun it -> it.Severity = Severity.Error) |> Seq.length)
        
    member _.WarningsCount =
        _diagnostics |> Seq.filter(fun it -> it.Severity = Severity.Warning) |> Seq.length
    
    
    member this.Error(message: string, span: Span) =
        _diagnostics.Add(Diag.Of(Severity.Error, message, span))
    
    
    member this.AsError(message: string) =
        _binutils_errors.Add(message)
    
    
    member this.LdError(message: string) =
        _binutils_errors.Add(message)
        
        
    member this.Warn(message: string, span: Span) =
        _diagnostics.Add(Diag.Of(Severity.Warning, message, span))
        
        
    member this.Info(message: string, span: Span) =
        _diagnostics.Add(Diag.Of(Severity.Info, message, span))
