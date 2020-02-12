namespace LibCool.Tests.Support

open System.Runtime.CompilerServices

[<IsReadOnly; Struct>]
type Mismatch =
    { Expected: string
      Actual: string
      At: int }


