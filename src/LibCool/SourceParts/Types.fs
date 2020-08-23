namespace LibCool.SourceParts


open System.Runtime.CompilerServices


/// A half-open range is one which includes the first element, but excludes the last one.
/// The range [1,5) is half-open, and consists of the values 1, 2, 3, and 4.
/// https://stackoverflow.com/a/13067115
/// So, given the `First` and `Last` elements, the corresponding range is [First,Last)
[<IsReadOnly; Struct>]
type HalfOpenRange =
    { First: uint32
      Last: uint32 }
    with
    static member Invalid = { First = 0u; Last = 0u }
    static member Of(first, last) = { First = first; Last = last }


[<IsReadOnly; Struct>]
type Location =
    { FileName: string 
      Line: uint32 
      Col: uint32 }


