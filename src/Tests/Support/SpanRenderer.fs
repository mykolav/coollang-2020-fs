namespace Tests.Support

open LibCool.SourceParts


[<Sealed>]
type SpanRenderer private () =
    
    
    override _.ToString() : string = ""
    
    
    static member Render(cool_text: string, spans: Span[]) : string =
        let renderer = SpanRenderer()
        renderer.ToString()
