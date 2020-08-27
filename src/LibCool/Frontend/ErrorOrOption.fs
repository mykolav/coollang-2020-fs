namespace LibCool.Frontend


[<Struct>]
type ErrorOrOption<'TValue>
    = Error
    | Ok of ('TValue option)
    with
    member this.Value: 'TValue option =
        match this with
        | Ok value -> value
        | _ -> invalidOp "ErrorOrOption.Value"


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ErrorOrOption =
    
    
    let isError: (ErrorOrOption<'T> -> bool) = function
        | Error -> true
        | _ -> false
