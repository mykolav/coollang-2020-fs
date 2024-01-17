namespace Tests.Parser

open System.Runtime.CompilerServices
open System.Text
open System.Text.RegularExpressions
open Tests.Support


[<Sealed; AbstractClass; Extension>]
type CodeSnippetAsserts private () =


    static let re_ws = Regex(@"\s+", RegexOptions.Compiled)


    [<Extension>]
    static member IsEqualIgnoringWhitespaceTo(assert_that: IAssertThat<string>,
                                              expected: string) : unit =
        let expected_condensed = re_ws.Replace(expected, "")
        let actual_condensed = re_ws.Replace(assert_that.Actual, "")

        if System.String.CompareOrdinal(expected_condensed, actual_condensed) <> 0
        then
            let message =
                StringBuilder()
                    .AppendFormat("EXPECTED [CONDENSED LENGTH = {0}]: ", expected_condensed.Length)
                    .AppendLine()
                    .AppendLine(expected)
                    .AppendLine()
                    .AppendFormat("ACTUAL [CONDENSED LENGTH = {0}]: ", actual_condensed.Length)
                    .AppendLine()
                    .AppendLine(assert_that.Actual)
                    .ToString()

            Assert.It.Fails(message)
