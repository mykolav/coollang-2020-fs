namespace Tests.Parser

open LibCool.AstParts.Ast

[<RequireQualifiedAccess>]
type AssertAst() =
    static member Equal(expected: Ast, actual: Ast) =
        ()

