namespace LibCool.Tests.Parser

open System.Text
open System.Text.RegularExpressions
open LibCool.Tests


[<Sealed>]
type AssertSnippets private () =
    static let re_ws = Regex(@"\s+", RegexOptions.Compiled)
    
    
    static let condense (input: string) : string =
        let result = re_ws.Replace(input, "")
        result
        
        
    static member EqualIgnoringWhitespace(expected: string, actual: string) : unit =
        let expected_condensed = condense expected
        let actual_condensed = condense actual
        if System.String.CompareOrdinal(expected_condensed, actual_condensed) <> 0
        then do
            let message =
                StringBuilder()
                    .AppendFormat("EXPECTED [CONDENSED LENGTH = {0}]: ", expected_condensed.Length)
                    .AppendLine()
                    .AppendLine(expected)
                    .AppendLine()
                    .AppendFormat("ACTUAL [CONDENSED LENGTH = {0}]: ", actual_condensed.Length)
                    .AppendLine()
                    .AppendLine(actual)
                    .ToString();
            AssertFail.With(message)
