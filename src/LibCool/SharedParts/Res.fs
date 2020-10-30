namespace LibCool.SharedParts


open System.Runtime.CompilerServices


[<IsReadOnly; Struct; DefaultAugmentation(false)>]
type Res<'T>
    = Error
    | Ok of 'T
    with
    member this.IsError: bool =
        match this with
        | Error -> true
        | Ok _  -> false
    member this.IsOk: bool = not this.IsError
    member this.Value: 'T =
        match this with
        | Ok value -> value
        | _ -> invalidOp "Res<'T>.Value"


