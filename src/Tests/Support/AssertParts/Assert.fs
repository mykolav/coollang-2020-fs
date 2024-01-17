namespace Tests.Support

open System.Runtime.CompilerServices


type IAssertThat<'TActual> =
    abstract Actual: 'TActual


type IAssertIt = interface end


[<Sealed; AbstractClass; RequireQualifiedAccess>]
type Assert =
    static member That<'TActual>(actual: 'TActual): IAssertThat<'TActual> =
        { new IAssertThat<'TActual> with
            member _.Actual = actual }


    static let _assert_it = { new IAssertIt }
    static member It: IAssertIt = _assert_it


[<Sealed; AbstractClass; Extension>]
type FailAsserts private () =
    [<Extension>]
    static member Fails(_: IAssertIt, message: string): unit =
        raise (Xunit.Sdk.XunitException(message))


[<Sealed; AbstractClass; Extension>]
type CommonAsserts private () =
    [<Extension>]
    static member IsEqualTo(assert_that: IAssertThat<'T>, expected: 'T): unit =
        Xunit.Assert.Equal<'T>(expected=expected, actual=assert_that.Actual)
