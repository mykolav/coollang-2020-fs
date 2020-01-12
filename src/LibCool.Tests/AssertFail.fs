namespace LibCool.Tests


[<RequireQualifiedAccess>]
module AssertFail =
    let With(message: string) : unit =
        raise (Xunit.Sdk.XunitException(message))
