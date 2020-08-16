namespace Tests.Support


open System.Runtime.CompilerServices


[<IsReadOnly; Struct>]
type Mismatch =
    { Expected: string
      Actual: string
      At: int }


[<RequireQualifiedAccess>]
module StringSeq =


    let private zipAll (xs: seq<'X>) (ys: seq<'Y>) =
        seq {
            let ex = xs.GetEnumerator()
            let ey = ys.GetEnumerator()

            let mutable haveX = true
            let mutable haveY = true

            while haveX || haveY do
                haveX <- ex.MoveNext()
                haveY <- ey.MoveNext()
                if haveX && haveY then yield (Some ex.Current, Some ey.Current)
                else if haveX     then yield (Some ex.Current, None)
                else if haveY     then yield (None, Some ey.Current)
        }


    let private either defaultValue (option: 'a option) =
        match option with
        | Some value -> value
        | None       -> defaultValue

    
    let compare (xs: seq<string>) (ys: seq<string>) =
        zipAll xs ys
        |> Seq.mapi (fun i (e, a) -> { At = i
                                       Expected = either "<NONE>" e
                                       Actual = either "<NONE>" a })
        |> Seq.filter (fun it -> it.Expected <> it.Actual)
