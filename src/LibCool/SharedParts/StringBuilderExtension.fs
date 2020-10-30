namespace LibCool.SharedParts


open System.Runtime.CompilerServices
open System.Text


[<Extension>]
type StringBuilderExtensions private () =
    [<Extension>] static member Nop(_: StringBuilder): unit = ()
