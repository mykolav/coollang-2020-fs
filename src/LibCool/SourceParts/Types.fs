namespace LibCool.SourceParts

open System.Runtime.CompilerServices

[<IsReadOnly; Struct>]
type HalfOpenRange =
    { First: uint32
      Last: uint32 }
    with
    static member Invalid = { First = 0u; Last = 0u }
    static member Mk first last = { First = first; Last = last }

[<IsReadOnly; Struct>]
type Location =
    { FileName: string 
      Line: uint32 
      Col: uint32 }


