namespace LibCool.Tests.Parser

open LibCool.Ast

[<RequireQualifiedAccess>]
type AssertAst() =
    static member Equal(expected: Ast, actual: Ast) =
        ()

