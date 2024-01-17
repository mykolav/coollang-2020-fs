namespace Tests.Support


open System.Runtime.CompilerServices
open LibCool.SharedParts


[<IsReadOnly; Struct>]
type Mismatch =
    { Expected: string
      Actual: string
      At: int }


[<RequireQualifiedAccess>]
module private Seq =


    let private zipAll (xs: seq<'X>) (ys: seq<'Y>): seq<'X option * 'Y option> =
        seq {
            use ex = xs.GetEnumerator()
            use ey = ys.GetEnumerator()

            let mutable haveX = true
            let mutable haveY = true

            while haveX || haveY do
                haveX <- ex.MoveNext()
                haveY <- ey.MoveNext()
                if haveX && haveY then yield (Some ex.Current, Some ey.Current)
                else if haveX     then yield (Some ex.Current, None)
                else if haveY     then yield (None, Some ey.Current)
        }


    let private stringify (defaultString: string) (option: 'T option): string =
        match option with
        | Some value -> value.ToString()
        | None       -> defaultString


    let compare (expecteds: seq<'T>) (actuals: seq<'T>): seq<Mismatch> =
        zipAll expecteds actuals
        |> Seq.mapi (fun i (e, a) -> (i, e, a))
        |> Seq.filter (fun (_, e, a) -> e <> a)
        |> Seq.map (fun (i, e, a) -> { At = i
                                       Expected = stringify "<NONE>" e
                                       Actual = stringify "<NONE>" a })


type FormatMismatchesFn = ((*expected_len=*)int) ->
                           ((*actual_len=*)int) ->
                           ((*mismatches=*)seq<Mismatch>) -> string


[<Sealed; AbstractClass; Extension>]
type SeqAsserts private () =


    [<Extension>]
    static member Match(assert_that: IAssertThat<seq<'T>>,
                        expected: seq<'T>,
                        format: FormatMismatchesFn)
                        : unit =
        let mismatches = Seq.compare expected assert_that.Actual
        if Seq.any mismatches
        then
            let message = format (Seq.length expected)
                                 (Seq.length assert_that.Actual)
                                 mismatches
            Assert.It.Fails(message)


    [<Extension>]
    static member AreEmpty(assert_that: IAssertThat<seq<'T>>,
                           format: FormatMismatchesFn)
                          : unit =
        let mismatches = Seq.compare [] assert_that.Actual
        if Seq.any mismatches
        then
            let message = format 0 (Seq.length assert_that.Actual) mismatches
            Assert.It.Fails(message)
