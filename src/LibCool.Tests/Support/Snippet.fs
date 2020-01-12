namespace LibCool.Tests.Support

open System.Runtime.CompilerServices

[<IsReadOnly; Struct>]
type Snippet = Snippet of content: string
    with
    member this.ToDisplayString() =
        let (Snippet content) = this
        "\"" +
        (if content.Length > 100 
        then content.[0..100] + "..."
        else content) +
        "\""
        
    override this.ToString() =
        let (Snippet content) = this
        content


