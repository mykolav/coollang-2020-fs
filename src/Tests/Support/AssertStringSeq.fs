namespace Tests.Support


open LibCool.SharedParts
open Tests.Support


type FormatMessageFn = ((*expected_len=*)int) -> ((*actual_len=*)int) -> ((*mismatches=*)seq<Mismatch>) -> string


[<Sealed>]
type AssertStringSeq private () =


    static member EmptyMismatches(mismatches: seq<Mismatch>,
                                  expected_len: int,
                                  actual_len: int,
                                  format_message: FormatMessageFn): unit =
        if Seq.any mismatches
        then
            let message = format_message expected_len actual_len mismatches
            raise (Xunit.Sdk.XunitException(message))
