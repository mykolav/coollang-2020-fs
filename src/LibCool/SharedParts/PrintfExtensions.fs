namespace LibCool.SharedParts


open System


[<AutoOpen>]
module PrintfExtensions =
    let sprintfn format =
        Printf.ksprintf (fun it -> it + Environment.NewLine) format
