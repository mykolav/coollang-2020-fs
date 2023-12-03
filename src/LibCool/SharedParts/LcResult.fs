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
    static member map<'U> (mapping: 'T -> 'U) (result: LcResult<'T>): LcResult<'U> =
        match result with
        | Ok value -> Ok (mapping value)
        | Error -> Error
    member this.Value: 'T =
        match this with
        | Ok value -> value
        | Error -> invalidOp $"LcResult<{typeof<'T>.Name}>"
