namespace Tests.Support


open System.Runtime.CompilerServices


[<IsReadOnly; Struct>]
type Snippet = Snippet of content: string
    with
    member this.ToDisplayString() =
        let (Snippet content) = this
        "\"" +
        (if content.Length > 50 
        then content.[0..49].Replace("\r", "").Replace("\n", " ") + "..."
        else content) +
        "\""

            
    override this.ToString() =
        let (Snippet content) = this
        content


