namespace Tests.GenGC

open System
open System.Collections.Generic
open System.Runtime.CompilerServices


type private Iterator<'T>(_enumerator: IEnumerator<'T>) =


    let mutable _has_current = _enumerator.MoveNext()


    member this.HasCurrent: bool = _has_current
    member this.Current: 'T = _enumerator.Current
    member this.MoveNext(): bool =
        _has_current <- _enumerator.MoveNext()
        _has_current


    interface IDisposable with
        member _.Dispose(): unit = _enumerator.Dispose()


(*
GenGC: HEAP START  = 1950022500352
GenGC: HEAP END    = 1950022533120
GenGC: L0          = 1950022500432
GenGC: L1          = 1950022500432
GenGC: L2          = 1950022516776
GenGC: L3          = 1950022533120
GenGC: L4          = 1950022533120
GenGC: MINOR0      = 0
GenGC: MINOR1      = 0
GenGC: MAJOR0      = 0
GenGC: MAJOR1      = 0
GenGC: STACK BASE  = 532657731408
GenGC: ALLOC PTR   = 1950022516808
GenGC: ALLOC LIMIT = 1950022533120
*)
[<IsReadOnly; Struct>]
type HeapInfo =
    { HeapStart: int64
      HeapEnd: int64
      // GenGC's heap header
      L0: int64
      L1: int64
      L2: int64
      L3: int64
      L4: int64 }


    static member Empty: HeapInfo =
        { HeapInfo.HeapStart = 0
          HeapEnd = 0
          L0 = 0
          L1 = 0
          L2 = 0
          L3 = 0
          L4 = 0 }


[<IsReadOnly; Struct>]
type GenGCHistory =
    { Minor0: int64
      Minor1: int64
      Major0: int64
      Major1: int64 }


    static member Empty: GenGCHistory =
        { Minor0 = 0
          Minor1 = 0
          Major0 = 0
          Major1 = 0 }


[<IsReadOnly; Struct>]
type AllocInfo =
    { AllocPtr: int64
      AllocLimit: int64 }


    static member Empty: AllocInfo =
        { AllocPtr = 0
          AllocLimit = 0 }


[<IsReadOnly; Struct>]
type GenGCStateInfo =
    { HeapInfo: HeapInfo
      History: GenGCHistory
      StackBase: int64
      AllocInfo: AllocInfo }


    static member Empty: GenGCStateInfo =
        { HeapInfo = HeapInfo.Empty
          History = GenGCHistory.Empty
          StackBase = 0
          AllocInfo = AllocInfo.Empty }


    static member Parse(lines: seq<string>): seq<GenGCStateInfo> =
        let state_infos = List<GenGCStateInfo>()

        use lit = new Iterator<_>(lines.GetEnumerator())

        let mutable have_state_info = GenGCStateInfo.MoveToNext(lit)
        while have_state_info do
            let state_info =
                { GenGCStateInfo.HeapInfo =
                    { HeapInfo.HeapStart = GenGCStateInfo.TakeInt64("GenGC: HEAP START", lit)
                      HeapEnd = GenGCStateInfo.TakeInt64("GenGC: HEAP END", lit)
                      L0 = GenGCStateInfo.TakeInt64("GenGC: L0", lit)
                      L1 = GenGCStateInfo.TakeInt64("GenGC: L1", lit)
                      L2 = GenGCStateInfo.TakeInt64("GenGC: L2", lit)
                      L3 = GenGCStateInfo.TakeInt64("GenGC: L3", lit)
                      L4 = GenGCStateInfo.TakeInt64("GenGC: L4", lit) }
                  History =
                      { Minor0 = GenGCStateInfo.TakeInt64("GenGC: MINOR0", lit)
                        Minor1 = GenGCStateInfo.TakeInt64("GenGC: MINOR1", lit)
                        Major0 = GenGCStateInfo.TakeInt64("GenGC: MAJOR0", lit)
                        Major1 = GenGCStateInfo.TakeInt64("GenGC: MAJOR1", lit) }
                  StackBase = GenGCStateInfo.TakeInt64("GenGC: STACK BASE", lit)
                  AllocInfo =
                      { AllocPtr = GenGCStateInfo.TakeInt64("GenGC: ALLOC PTR", lit)
                        AllocLimit = GenGCStateInfo.TakeInt64("GenGC: ALLOC LIMIT", lit) } }
            state_infos.Add(state_info)

            have_state_info <- GenGCStateInfo.MoveToNext(lit)

        state_infos


    static member private MoveToNext(lit: Iterator<string>): bool =
        while lit.HasCurrent && not (lit.Current.StartsWith("GenGC: HEAP START")) do
            lit.MoveNext() |> ignore

        lit.HasCurrent


    static member private TakeInt64(prefix: string, lit: Iterator<string>): int64 =
        if not lit.HasCurrent
        then
            invalidOp "Enumeration already finished"

        let line = lit.Current
        lit.MoveNext() |> ignore

        if not (line.StartsWith(prefix))
        then
            invalidOp $"'{line}' does not start with '{prefix}'"

        let eq_index = line.IndexOf('=')
        let stringified_value = line.Substring(eq_index + 1).Trim()
        Int64.Parse(stringified_value)
