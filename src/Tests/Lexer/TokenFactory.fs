namespace Tests

open LibCool.SourceParts
open LibCool.Frontend

[<RequireQualifiedAccess>]
module T = 
    let EOF = { Kind = TokenKind.EOF; Span = Span.Invalid }
    let ID (value: string) = { Kind = TokenKind.Id value; Span = Span.Invalid }
    // Literals
    let Int (value: int) = { Kind = TokenKind.IntLiteral value; Span = Span.Invalid }
    let Str (value: string) = { Kind = TokenKind.StringLiteral value; Span = Span.Invalid }
    let QQQ (value: string) = { Kind = TokenKind.TripleQuotedStringLiteral value; Span = Span.Invalid }
    // Punctuators
    // + - / * = == < > <= >= ! != ( ) [ ] { } : ; . ,
    let Plus = { Kind = TokenKind.Plus; Span = Span.Invalid }
    let Minus = { Kind = TokenKind.Minus; Span = Span.Invalid }
    let Slash = { Kind = TokenKind.Slash; Span = Span.Invalid }
    let Star = { Kind = TokenKind.Star; Span = Span.Invalid }
    let Eq = { Kind = TokenKind.Equal; Span = Span.Invalid }
    let EqEq = { Kind = TokenKind.EqualEqual; Span = Span.Invalid }
    let Lt = { Kind = TokenKind.Less; Span = Span.Invalid }
    let Gt = { Kind = TokenKind.Greater; Span = Span.Invalid }
    let LtEq = { Kind = TokenKind.LessEqual; Span = Span.Invalid }
    let GtEq = { Kind = TokenKind.GreaterEqual; Span = Span.Invalid }
    let EqGt = { Kind = TokenKind.EqualGreater; Span = Span.Invalid }
    let Ex = { Kind = TokenKind.Exclaim; Span = Span.Invalid }
    let ExEq = { Kind = TokenKind.ExclaimEqual; Span = Span.Invalid }
    let LPar = { Kind = TokenKind.LParen; Span = Span.Invalid }
    let RPar = { Kind = TokenKind.RParen; Span = Span.Invalid }
    let LSq = { Kind = TokenKind.LSquare; Span = Span.Invalid }
    let RSq = { Kind = TokenKind.RSquare; Span = Span.Invalid }
    let LBr = { Kind = TokenKind.LBrace; Span = Span.Invalid }
    let RBr = { Kind = TokenKind.RBrace; Span = Span.Invalid }
    let Colon = { Kind = TokenKind.Colon; Span = Span.Invalid }
    let Semi = { Kind = TokenKind.Semi; Span = Span.Invalid }
    let Dot = { Kind = TokenKind.Dot; Span = Span.Invalid }
    let Comma = { Kind = TokenKind.Comma; Span = Span.Invalid }
    // Keywords
    let Case = { Kind = TokenKind.KwCase; Span = Span.Invalid } 
    let Class = { Kind = TokenKind.KwClass; Span = Span.Invalid } 
    let Def = { Kind = TokenKind.KwDef; Span = Span.Invalid } 
    let Else = { Kind = TokenKind.KwElse; Span = Span.Invalid } 
    let Extends = { Kind = TokenKind.KwExtends; Span = Span.Invalid } 
    let False = { Kind = TokenKind.KwFalse; Span = Span.Invalid } 
    let If = { Kind = TokenKind.KwIf; Span = Span.Invalid } 
    let Match = { Kind = TokenKind.KwMatch; Span = Span.Invalid } 
    let Native = { Kind = TokenKind.KwNative; Span = Span.Invalid }
    let New = { Kind = TokenKind.KwNew; Span = Span.Invalid } 
    let Null = { Kind = TokenKind.KwNull; Span = Span.Invalid } 
    let Override = { Kind = TokenKind.KwOverride; Span = Span.Invalid } 
    let Super = { Kind = TokenKind.KwSuper; Span = Span.Invalid } 
    let This = { Kind = TokenKind.KwThis; Span = Span.Invalid } 
    let True = { Kind = TokenKind.KwTrue; Span = Span.Invalid } 
    let Var = { Kind = TokenKind.KwVar; Span = Span.Invalid } 
    let While = { Kind = TokenKind.KwWhile; Span = Span.Invalid } 
    // Illegal/reserved keywords
    let Abstract = { Kind = TokenKind.KwAbstract; Span = Span.Invalid } 
    let Catch = { Kind = TokenKind.KwCatch; Span = Span.Invalid } 
    let Do = { Kind = TokenKind.KwDo; Span = Span.Invalid } 
    let Final = { Kind = TokenKind.KwFinal; Span = Span.Invalid } 
    let Finally = { Kind = TokenKind.KwFinally; Span = Span.Invalid } 
    let For = { Kind = TokenKind.KwFor; Span = Span.Invalid } 
    let ForSome = { Kind = TokenKind.KwForSome; Span = Span.Invalid } 
    let Implicit = { Kind = TokenKind.KwImplicit; Span = Span.Invalid } 
    let Import = { Kind = TokenKind.KwImport; Span = Span.Invalid } 
    let Lazy = { Kind = TokenKind.KwLazy; Span = Span.Invalid } 
    let Object = { Kind = TokenKind.KwObject; Span = Span.Invalid } 
    let Package = { Kind = TokenKind.KwPackage; Span = Span.Invalid } 
    let Private = { Kind = TokenKind.KwPrivate; Span = Span.Invalid } 
    let Protected = { Kind = TokenKind.KwProtected; Span = Span.Invalid } 
    let Requires = { Kind = TokenKind.KwRequires; Span = Span.Invalid } 
    let Return = { Kind = TokenKind.KwReturn; Span = Span.Invalid } 
    let Sealed = { Kind = TokenKind.KwSealed; Span = Span.Invalid } 
    let Throw = { Kind = TokenKind.KwThrow; Span = Span.Invalid } 
    let Trait = { Kind = TokenKind.KwTrait; Span = Span.Invalid } 
    let Try = { Kind = TokenKind.KwTry; Span = Span.Invalid } 
    let Type = { Kind = TokenKind.KwType; Span = Span.Invalid } 
    let Val = { Kind = TokenKind.KwVal; Span = Span.Invalid } 
    let With = { Kind = TokenKind.KwWith; Span = Span.Invalid } 
    let Yield = { Kind = TokenKind.KwYield; Span = Span.Invalid }
