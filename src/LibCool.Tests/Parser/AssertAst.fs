namespace LibCool.Tests.Parser

open LibCool.Frontend

[<RequireQualifiedAccess>]
type AssertAst() =
    static member Equal(expected: Ast, actual: Ast) =
        ()

