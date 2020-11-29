namespace LibCool.SourceParts


open System
open System.Runtime.CompilerServices


/// It's a half-open range!
/// A half-open range is one which includes the first element, but excludes the last one.
/// The range [1,5) is half-open, and consists of the values 1, 2, 3, and 4.
/// https://stackoverflow.com/a/13067115
/// So, given the `First` and `Last` elements, the corresponding range is [First,Last)
[<IsReadOnly; Struct>]
type Span =
    { First: uint32
      Last: uint32 }
    with
    static member Virtual = { First = UInt32.MaxValue; Last = UInt32.MaxValue }
    static member Of(first, last) = { First = first; Last = last }
    member this.IsPhysical: bool = this <> Span.Virtual
    member this.IsVirtual: bool = this = Span.Virtual


[<IsReadOnly; Struct>]
type Location =
    { FileName: string 
      Line: uint32 
      Col: uint32 }
    with
    override this.ToString(): string =
        sprintf "%s(%d,%d)" this.FileName this.Line this.Col
