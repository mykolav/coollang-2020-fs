namespace LibCool.Frontend


[<Struct; DefaultAugmentation(false)>]
type ErrorOrOption<'TValue>
    = Error
    | Ok of ('TValue voption)
    with
    
    
    member this.IsError: bool =
        match this with
        | Error -> true
        | _ -> false
    
    
    member this.IsSome: bool =
        match this with
        | Ok (ValueSome _) -> true
        | _ -> false
        
    
    member this.IsNone: bool = not this.IsSome 
        
    
    member this.Value: 'TValue voption =
        match this with
        | Ok value -> value
        | _ -> invalidOp "ErrorOrOption.Value"
