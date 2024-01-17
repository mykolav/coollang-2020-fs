namespace LibCool.SharedParts


open System.Runtime.CompilerServices
open System.Text


[<Sealed; AbstractClass; Extension>]
type StringBuilderExtensions private () =
    [<Extension>]
    static member AsUnit(_: StringBuilder): unit = ()


[<RequireQualifiedAccess>]
module public Seq =
    let any<'T> (source: seq<'T>): bool = not (Seq.isEmpty source)
