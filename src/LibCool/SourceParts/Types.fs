namespace LibCool.SourceParts

open System.Runtime.CompilerServices

type Offset = uint32

[<IsReadOnly; Struct>]
type HalfOpenRange =
    { First: Offset
      Last: Offset }

module HalfOpenRange =
    let mk_range (first: Offset) (last: Offset) =
        { First = first
          Last  = last }

[<IsReadOnly; Struct>]
type Location =
    { FileName: string 
      Line: uint32 
      Col: uint32 }
