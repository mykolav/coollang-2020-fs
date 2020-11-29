namespace Tests

open LibCool.SourceParts
open LibCool.ParserParts

[<RequireQualifiedAccess>]
module T = 
    let EOF = { Kind = TokenKind.EOF; Span = Span.Virtual }
    let ID (value: string) = { Kind = TokenKind.Id value; Span = Span.Virtual }
    // Literals
    let Int (value: int) = { Kind = TokenKind.IntLiteral value; Span = Span.Virtual }
    let Str (value: string) = { Kind = TokenKind.StringLiteral value; Span = Span.Virtual }
    let QQQ (value: string) = { Kind = TokenKind.TripleQuotedStringLiteral value; Span = Span.Virtual }
    // Punctuators
    // + - / * = == < > <= >= ! != ( ) [ ] { } : ; . ,
    let Plus = { Kind = TokenKind.Plus; Span = Span.Virtual }
    let Minus = { Kind = TokenKind.Minus; Span = Span.Virtual }
    let Slash = { Kind = TokenKind.Slash; Span = Span.Virtual }
    let Star = { Kind = TokenKind.Star; Span = Span.Virtual }
    let Eq = { Kind = TokenKind.Equal; Span = Span.Virtual }
    let EqEq = { Kind = TokenKind.EqualEqual; Span = Span.Virtual }
    let Lt = { Kind = TokenKind.Less; Span = Span.Virtual }
    let Gt = { Kind = TokenKind.Greater; Span = Span.Virtual }
    let LtEq = { Kind = TokenKind.LessEqual; Span = Span.Virtual }
    let GtEq = { Kind = TokenKind.GreaterEqual; Span = Span.Virtual }
    let EqGt = { Kind = TokenKind.EqualGreater; Span = Span.Virtual }
    let Ex = { Kind = TokenKind.Exclaim; Span = Span.Virtual }
    let ExEq = { Kind = TokenKind.ExclaimEqual; Span = Span.Virtual }
    let LPar = { Kind = TokenKind.LParen; Span = Span.Virtual }
    let RPar = { Kind = TokenKind.RParen; Span = Span.Virtual }
    let LSq = { Kind = TokenKind.LSquare; Span = Span.Virtual }
    let RSq = { Kind = TokenKind.RSquare; Span = Span.Virtual }
    let LBr = { Kind = TokenKind.LBrace; Span = Span.Virtual }
    let RBr = { Kind = TokenKind.RBrace; Span = Span.Virtual }
    let Colon = { Kind = TokenKind.Colon; Span = Span.Virtual }
    let Semi = { Kind = TokenKind.Semi; Span = Span.Virtual }
    let Dot = { Kind = TokenKind.Dot; Span = Span.Virtual }
    let Comma = { Kind = TokenKind.Comma; Span = Span.Virtual }
    // Keywords
    let Case = { Kind = TokenKind.KwCase; Span = Span.Virtual } 
    let Class = { Kind = TokenKind.KwClass; Span = Span.Virtual } 
    let Def = { Kind = TokenKind.KwDef; Span = Span.Virtual } 
    let Else = { Kind = TokenKind.KwElse; Span = Span.Virtual } 
    let Extends = { Kind = TokenKind.KwExtends; Span = Span.Virtual } 
    let False = { Kind = TokenKind.KwFalse; Span = Span.Virtual } 
    let If = { Kind = TokenKind.KwIf; Span = Span.Virtual } 
    let Match = { Kind = TokenKind.KwMatch; Span = Span.Virtual } 
    let Native = { Kind = TokenKind.KwNative; Span = Span.Virtual }
    let New = { Kind = TokenKind.KwNew; Span = Span.Virtual } 
    let Null = { Kind = TokenKind.KwNull; Span = Span.Virtual } 
    let Override = { Kind = TokenKind.KwOverride; Span = Span.Virtual } 
    let Super = { Kind = TokenKind.KwSuper; Span = Span.Virtual } 
    let This = { Kind = TokenKind.KwThis; Span = Span.Virtual } 
    let True = { Kind = TokenKind.KwTrue; Span = Span.Virtual } 
    let Var = { Kind = TokenKind.KwVar; Span = Span.Virtual } 
    let While = { Kind = TokenKind.KwWhile; Span = Span.Virtual } 
    // Illegal/reserved keywords
    let Abstract = { Kind = TokenKind.KwAbstract; Span = Span.Virtual } 
    let Catch = { Kind = TokenKind.KwCatch; Span = Span.Virtual } 
    let Do = { Kind = TokenKind.KwDo; Span = Span.Virtual } 
    let Final = { Kind = TokenKind.KwFinal; Span = Span.Virtual } 
    let Finally = { Kind = TokenKind.KwFinally; Span = Span.Virtual } 
    let For = { Kind = TokenKind.KwFor; Span = Span.Virtual } 
    let ForSome = { Kind = TokenKind.KwForSome; Span = Span.Virtual } 
    let Implicit = { Kind = TokenKind.KwImplicit; Span = Span.Virtual } 
    let Import = { Kind = TokenKind.KwImport; Span = Span.Virtual } 
    let Lazy = { Kind = TokenKind.KwLazy; Span = Span.Virtual } 
    let Object = { Kind = TokenKind.KwObject; Span = Span.Virtual } 
    let Package = { Kind = TokenKind.KwPackage; Span = Span.Virtual } 
    let Private = { Kind = TokenKind.KwPrivate; Span = Span.Virtual } 
    let Protected = { Kind = TokenKind.KwProtected; Span = Span.Virtual } 
    let Requires = { Kind = TokenKind.KwRequires; Span = Span.Virtual } 
    let Return = { Kind = TokenKind.KwReturn; Span = Span.Virtual } 
    let Sealed = { Kind = TokenKind.KwSealed; Span = Span.Virtual } 
    let Throw = { Kind = TokenKind.KwThrow; Span = Span.Virtual } 
    let Trait = { Kind = TokenKind.KwTrait; Span = Span.Virtual } 
    let Try = { Kind = TokenKind.KwTry; Span = Span.Virtual } 
    let Type = { Kind = TokenKind.KwType; Span = Span.Virtual } 
    let Val = { Kind = TokenKind.KwVal; Span = Span.Virtual } 
    let With = { Kind = TokenKind.KwWith; Span = Span.Virtual } 
    let Yield = { Kind = TokenKind.KwYield; Span = Span.Virtual }
