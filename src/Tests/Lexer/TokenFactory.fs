namespace Tests

open LibCool.SourceParts
open LibCool.Frontend

[<RequireQualifiedAccess>]
module T = 
    let EOF = { Kind = TokenKind.EOF; Span = Range.Invalid }
    let ID (value: string) = { Kind = TokenKind.Identifier value; Span = Range.Invalid }
    // Literals
    let Int (value: int) = { Kind = TokenKind.IntLiteral value; Span = Range.Invalid }
    let Str (value: string) = { Kind = TokenKind.StringLiteral value; Span = Range.Invalid }
    let QQQ (value: string) = { Kind = TokenKind.TripleQuotedStringLiteral value; Span = Range.Invalid }
    // Punctuators
    // + - / * = == < > <= >= ! != ( ) [ ] { } : ; . ,
    let Plus = { Kind = TokenKind.Plus; Span = Range.Invalid }
    let Minus = { Kind = TokenKind.Minus; Span = Range.Invalid }
    let Slash = { Kind = TokenKind.Slash; Span = Range.Invalid }
    let Star = { Kind = TokenKind.Star; Span = Range.Invalid }
    let Eq = { Kind = TokenKind.Equal; Span = Range.Invalid }
    let EqEq = { Kind = TokenKind.EqualEqual; Span = Range.Invalid }
    let Lt = { Kind = TokenKind.Less; Span = Range.Invalid }
    let Gt = { Kind = TokenKind.Greater; Span = Range.Invalid }
    let LtEq = { Kind = TokenKind.LessEqual; Span = Range.Invalid }
    let GtEq = { Kind = TokenKind.GreaterEqual; Span = Range.Invalid }
    let EqGt = { Kind = TokenKind.EqualGreater; Span = Range.Invalid }
    let Ex = { Kind = TokenKind.Exclaim; Span = Range.Invalid }
    let ExEq = { Kind = TokenKind.ExclaimEqual; Span = Range.Invalid }
    let LPar = { Kind = TokenKind.LParen; Span = Range.Invalid }
    let RPar = { Kind = TokenKind.RParen; Span = Range.Invalid }
    let LSq = { Kind = TokenKind.LSquare; Span = Range.Invalid }
    let RSq = { Kind = TokenKind.RSquare; Span = Range.Invalid }
    let LBr = { Kind = TokenKind.LBrace; Span = Range.Invalid }
    let RBr = { Kind = TokenKind.RBrace; Span = Range.Invalid }
    let Colon = { Kind = TokenKind.Colon; Span = Range.Invalid }
    let Semi = { Kind = TokenKind.Semi; Span = Range.Invalid }
    let Dot = { Kind = TokenKind.Dot; Span = Range.Invalid }
    let Comma = { Kind = TokenKind.Comma; Span = Range.Invalid }
    // Keywords
    let Case = { Kind = TokenKind.KwCase; Span = Range.Invalid } 
    let Class = { Kind = TokenKind.KwClass; Span = Range.Invalid } 
    let Def = { Kind = TokenKind.KwDef; Span = Range.Invalid } 
    let Else = { Kind = TokenKind.KwElse; Span = Range.Invalid } 
    let Extends = { Kind = TokenKind.KwExtends; Span = Range.Invalid } 
    let False = { Kind = TokenKind.KwFalse; Span = Range.Invalid } 
    let If = { Kind = TokenKind.KwIf; Span = Range.Invalid } 
    let Match = { Kind = TokenKind.KwMatch; Span = Range.Invalid } 
    let Native = { Kind = TokenKind.KwNative; Span = Range.Invalid }
    let New = { Kind = TokenKind.KwNew; Span = Range.Invalid } 
    let Null = { Kind = TokenKind.KwNull; Span = Range.Invalid } 
    let Override = { Kind = TokenKind.KwOverride; Span = Range.Invalid } 
    let Super = { Kind = TokenKind.KwSuper; Span = Range.Invalid } 
    let This = { Kind = TokenKind.KwThis; Span = Range.Invalid } 
    let True = { Kind = TokenKind.KwTrue; Span = Range.Invalid } 
    let Var = { Kind = TokenKind.KwVar; Span = Range.Invalid } 
    let While = { Kind = TokenKind.KwWhile; Span = Range.Invalid } 
    // Illegal/reserved keywords
    let Abstract = { Kind = TokenKind.KwAbstract; Span = Range.Invalid } 
    let Catch = { Kind = TokenKind.KwCatch; Span = Range.Invalid } 
    let Do = { Kind = TokenKind.KwDo; Span = Range.Invalid } 
    let Final = { Kind = TokenKind.KwFinal; Span = Range.Invalid } 
    let Finally = { Kind = TokenKind.KwFinally; Span = Range.Invalid } 
    let For = { Kind = TokenKind.KwFor; Span = Range.Invalid } 
    let ForSome = { Kind = TokenKind.KwForSome; Span = Range.Invalid } 
    let Implicit = { Kind = TokenKind.KwImplicit; Span = Range.Invalid } 
    let Import = { Kind = TokenKind.KwImport; Span = Range.Invalid } 
    let Lazy = { Kind = TokenKind.KwLazy; Span = Range.Invalid } 
    let Object = { Kind = TokenKind.KwObject; Span = Range.Invalid } 
    let Package = { Kind = TokenKind.KwPackage; Span = Range.Invalid } 
    let Private = { Kind = TokenKind.KwPrivate; Span = Range.Invalid } 
    let Protected = { Kind = TokenKind.KwProtected; Span = Range.Invalid } 
    let Requires = { Kind = TokenKind.KwRequires; Span = Range.Invalid } 
    let Return = { Kind = TokenKind.KwReturn; Span = Range.Invalid } 
    let Sealed = { Kind = TokenKind.KwSealed; Span = Range.Invalid } 
    let Throw = { Kind = TokenKind.KwThrow; Span = Range.Invalid } 
    let Trait = { Kind = TokenKind.KwTrait; Span = Range.Invalid } 
    let Try = { Kind = TokenKind.KwTry; Span = Range.Invalid } 
    let Type = { Kind = TokenKind.KwType; Span = Range.Invalid } 
    let Val = { Kind = TokenKind.KwVal; Span = Range.Invalid } 
    let With = { Kind = TokenKind.KwWith; Span = Range.Invalid } 
    let Yield = { Kind = TokenKind.KwYield; Span = Range.Invalid }
