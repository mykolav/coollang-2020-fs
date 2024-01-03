namespace LibCool.SharedParts


[<RequireQualifiedAccess>]
module public Seq =
    let any<'T> (source: seq<'T>): bool = not (Seq.isEmpty source)
