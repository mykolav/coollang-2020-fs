namespace LibCool.Tests

open LibCool.SourceParts
open LibCool.Frontend

[<RequireQualifiedAccess>]
module T = 
    let EOF = { Kind = TokenKind.EOF; Span = HalfOpenRange.Invalid }
    let ID (value: string) = { Kind = TokenKind.Identifier value; Span = HalfOpenRange.Invalid }
    // Literals
    let Int (value: int) = { Kind = TokenKind.IntLiteral value; Span = HalfOpenRange.Invalid }
    let Str (value: string) = { Kind = TokenKind.StringLiteral value; Span = HalfOpenRange.Invalid }
    let QQQ (value: string) = { Kind = TokenKind.TripleQuotedStringLiteral value; Span = HalfOpenRange.Invalid }
    // Punctuators
    // + - / * = == < > <= >= ! != ( ) [ ] { } : ; . ,
    let Plus = { Kind = TokenKind.Plus; Span = HalfOpenRange.Invalid }
    let Minus = { Kind = TokenKind.Minus; Span = HalfOpenRange.Invalid }
    let Slash = { Kind = TokenKind.Slash; Span = HalfOpenRange.Invalid }
    let Star = { Kind = TokenKind.Star; Span = HalfOpenRange.Invalid }
    let Eq = { Kind = TokenKind.Equal; Span = HalfOpenRange.Invalid }
    let EqEq = { Kind = TokenKind.EqualEqual; Span = HalfOpenRange.Invalid }
    let Lt = { Kind = TokenKind.Less; Span = HalfOpenRange.Invalid }
    let Gt = { Kind = TokenKind.Greater; Span = HalfOpenRange.Invalid }
    let LtEq = { Kind = TokenKind.LessEqual; Span = HalfOpenRange.Invalid }
    let GtEq = { Kind = TokenKind.GreaterEqual; Span = HalfOpenRange.Invalid }
    let EqGt = { Kind = TokenKind.EqualGreater; Span = HalfOpenRange.Invalid }
    let Ex = { Kind = TokenKind.Exclaim; Span = HalfOpenRange.Invalid }
    let ExEq = { Kind = TokenKind.ExclaimEqual; Span = HalfOpenRange.Invalid }
    let LPar = { Kind = TokenKind.LParen; Span = HalfOpenRange.Invalid }
    let RPar = { Kind = TokenKind.RParen; Span = HalfOpenRange.Invalid }
    let LSq = { Kind = TokenKind.LSquare; Span = HalfOpenRange.Invalid }
    let RSq = { Kind = TokenKind.RSquare; Span = HalfOpenRange.Invalid }
    let LBr = { Kind = TokenKind.LBrace; Span = HalfOpenRange.Invalid }
    let RBr = { Kind = TokenKind.RBrace; Span = HalfOpenRange.Invalid }
    let Colon = { Kind = TokenKind.Colon; Span = HalfOpenRange.Invalid }
    let Semi = { Kind = TokenKind.Semi; Span = HalfOpenRange.Invalid }
    let Dot = { Kind = TokenKind.Dot; Span = HalfOpenRange.Invalid }
    let Comma = { Kind = TokenKind.Comma; Span = HalfOpenRange.Invalid }
    // Keywords
    let Case = { Kind = TokenKind.KwCase; Span = HalfOpenRange.Invalid } 
    let Class = { Kind = TokenKind.KwClass; Span = HalfOpenRange.Invalid } 
    let Def = { Kind = TokenKind.KwDef; Span = HalfOpenRange.Invalid } 
    let Else = { Kind = TokenKind.KwElse; Span = HalfOpenRange.Invalid } 
    let Extends = { Kind = TokenKind.KwExtends; Span = HalfOpenRange.Invalid } 
    let False = { Kind = TokenKind.KwFalse; Span = HalfOpenRange.Invalid } 
    let If = { Kind = TokenKind.KwIf; Span = HalfOpenRange.Invalid } 
    let Match = { Kind = TokenKind.KwMatch; Span = HalfOpenRange.Invalid } 
    let Native = { Kind = TokenKind.KwNative; Span = HalfOpenRange.Invalid }
    let New = { Kind = TokenKind.KwNew; Span = HalfOpenRange.Invalid } 
    let Null = { Kind = TokenKind.KwNull; Span = HalfOpenRange.Invalid } 
    let Override = { Kind = TokenKind.KwOverride; Span = HalfOpenRange.Invalid } 
    let Super = { Kind = TokenKind.KwSuper; Span = HalfOpenRange.Invalid } 
    let This = { Kind = TokenKind.KwThis; Span = HalfOpenRange.Invalid } 
    let True = { Kind = TokenKind.KwTrue; Span = HalfOpenRange.Invalid } 
    let Var = { Kind = TokenKind.KwVar; Span = HalfOpenRange.Invalid } 
    let While = { Kind = TokenKind.KwWhile; Span = HalfOpenRange.Invalid } 
    // Illegal/reserved keywords
    let Abstract = { Kind = TokenKind.KwAbstract; Span = HalfOpenRange.Invalid } 
    let Catch = { Kind = TokenKind.KwCatch; Span = HalfOpenRange.Invalid } 
    let Do = { Kind = TokenKind.KwDo; Span = HalfOpenRange.Invalid } 
    let Final = { Kind = TokenKind.KwFinal; Span = HalfOpenRange.Invalid } 
    let Finally = { Kind = TokenKind.KwFinally; Span = HalfOpenRange.Invalid } 
    let For = { Kind = TokenKind.KwFor; Span = HalfOpenRange.Invalid } 
    let ForSome = { Kind = TokenKind.KwForSome; Span = HalfOpenRange.Invalid } 
    let Implicit = { Kind = TokenKind.KwImplicit; Span = HalfOpenRange.Invalid } 
    let Import = { Kind = TokenKind.KwImport; Span = HalfOpenRange.Invalid } 
    let Lazy = { Kind = TokenKind.KwLazy; Span = HalfOpenRange.Invalid } 
    let Object = { Kind = TokenKind.KwObject; Span = HalfOpenRange.Invalid } 
    let Package = { Kind = TokenKind.KwPackage; Span = HalfOpenRange.Invalid } 
    let Private = { Kind = TokenKind.KwPrivate; Span = HalfOpenRange.Invalid } 
    let Protected = { Kind = TokenKind.KwProtected; Span = HalfOpenRange.Invalid } 
    let Requires = { Kind = TokenKind.KwRequires; Span = HalfOpenRange.Invalid } 
    let Return = { Kind = TokenKind.KwReturn; Span = HalfOpenRange.Invalid } 
    let Sealed = { Kind = TokenKind.KwSealed; Span = HalfOpenRange.Invalid } 
    let Throw = { Kind = TokenKind.KwThrow; Span = HalfOpenRange.Invalid } 
    let Trait = { Kind = TokenKind.KwTrait; Span = HalfOpenRange.Invalid } 
    let Try = { Kind = TokenKind.KwTry; Span = HalfOpenRange.Invalid } 
    let Type = { Kind = TokenKind.KwType; Span = HalfOpenRange.Invalid } 
    let Val = { Kind = TokenKind.KwVal; Span = HalfOpenRange.Invalid } 
    let With = { Kind = TokenKind.KwWith; Span = HalfOpenRange.Invalid } 
    let Yield = { Kind = TokenKind.KwYield; Span = HalfOpenRange.Invalid }
