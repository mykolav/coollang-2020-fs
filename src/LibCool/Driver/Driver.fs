namespace LibCool.Driver

open System

[<Sealed>]
type Driver private () =
    static member Compile(args: seq<string>): int =
        Console.WriteLine(String.Join(" ", args))
        0

