namespace LibCool.Tests.Support

module StringBuilderExtensions =
    open System.Text
    
    type StringBuilder with
        member this.Pad(len: int, requiredLen: int) =
            if len < requiredLen 
            then let paddingLength = requiredLen - len
                 this.Append(new string(' ', paddingLength)) |> ignore
            this
