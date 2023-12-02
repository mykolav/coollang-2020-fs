namespace LibCool.SharedParts


open System.Runtime.CompilerServices


[<IsReadOnly; Struct>]
type LcResult<'T>
    = Error
    | Ok of 'T
    with
    static member isOk (result: LcResult<'T>): bool =
        match result with
        | Ok _ -> true
        | Error -> false
    static member isError (result: LcResult<'T>): bool = not (LcResult.isOk result)
    member this.Value: 'T =
        match this with
        | Ok value -> value
        | Error -> invalidOp $"LcResult<{typeof<'T>.Name}>"
