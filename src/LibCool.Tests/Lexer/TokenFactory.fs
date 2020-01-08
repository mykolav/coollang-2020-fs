namespace LibCool.Tests

open LibCool.SourceParts
open LibCool.Frontend

[<RequireQualifiedAccess>]
module T = 
    let EOF = { Kind = EOF; Span = HalfOpenRange.Invalid }
    let ID (value: string) = { Kind = Identifier value; Span = HalfOpenRange.Invalid }
    // Literals
    let Int (value: int) = { Kind = IntLiteral value; Span = HalfOpenRange.Invalid }
    let Str (value: string) = { Kind = StringLiteral value; Span = HalfOpenRange.Invalid }
    let QQQ (value: string) = { Kind = TripleQuotedStringLiteral value; Span = HalfOpenRange.Invalid }
    // Punctuators
    // + - / * = == < > <= >= ! != ( ) [ ] { } : ; . ,
    let Plus = { Kind = Plus; Span = HalfOpenRange.Invalid }
    let Minus = { Kind = Minus; Span = HalfOpenRange.Invalid }
    let Slash = { Kind = Slash; Span = HalfOpenRange.Invalid }
    let Star = { Kind = Star; Span = HalfOpenRange.Invalid }
    let Eq = { Kind = Equal; Span = HalfOpenRange.Invalid }
    let EqEq = { Kind = EqualEqual; Span = HalfOpenRange.Invalid }
    let Lt = { Kind = Less; Span = HalfOpenRange.Invalid }
    let Gt = { Kind = Greater; Span = HalfOpenRange.Invalid }
    let LtEq = { Kind = LessEqual; Span = HalfOpenRange.Invalid }
    let GtEq = { Kind = GreaterEqual; Span = HalfOpenRange.Invalid }
    let EqGt = { Kind = EqualGreater; Span = HalfOpenRange.Invalid }
    let Ex = { Kind = Exclaim; Span = HalfOpenRange.Invalid }
    let ExEq = { Kind = ExclaimEqual; Span = HalfOpenRange.Invalid }
    let LPar = { Kind = LParen; Span = HalfOpenRange.Invalid }
    let RPar = { Kind = RParen; Span = HalfOpenRange.Invalid }
    let LSq = { Kind = LSquare; Span = HalfOpenRange.Invalid }
    let RSq = { Kind = RSquare; Span = HalfOpenRange.Invalid }
    let LBr = { Kind = LBrace; Span = HalfOpenRange.Invalid }
    let RBr = { Kind = RBrace; Span = HalfOpenRange.Invalid }
    let Colon = { Kind = Colon; Span = HalfOpenRange.Invalid }
    let Semi = { Kind = Semi; Span = HalfOpenRange.Invalid }
    let Dot = { Kind = Dot; Span = HalfOpenRange.Invalid }
    let Comma = { Kind = Comma; Span = HalfOpenRange.Invalid }
    // Keywords
    let Case = { Kind = Case; Span = HalfOpenRange.Invalid } 
    let Class = { Kind = Class; Span = HalfOpenRange.Invalid } 
    let Def = { Kind = Def; Span = HalfOpenRange.Invalid } 
    let Else = { Kind = Else; Span = HalfOpenRange.Invalid } 
    let Extends = { Kind = ExtendsInfoExtends; Span = HalfOpenRange.Invalid } 
    let False = { Kind = False; Span = HalfOpenRange.Invalid } 
    let If = { Kind = If; Span = HalfOpenRange.Invalid } 
    let Match = { Kind = Match; Span = HalfOpenRange.Invalid } 
    let Native = { Kind = Native; Span = HalfOpenRange.Invalid }
    let New = { Kind = New; Span = HalfOpenRange.Invalid } 
    let Null = { Kind = Null; Span = HalfOpenRange.Invalid } 
    let Override = { Kind = Override; Span = HalfOpenRange.Invalid } 
    let Super = { Kind = Super; Span = HalfOpenRange.Invalid } 
    let This = { Kind = This; Span = HalfOpenRange.Invalid } 
    let True = { Kind = True; Span = HalfOpenRange.Invalid } 
    let Var = { Kind = Var; Span = HalfOpenRange.Invalid } 
    let While = { Kind = While; Span = HalfOpenRange.Invalid } 
    // Illegal/reserved keywords
    let Abstract = { Kind = Abstract; Span = HalfOpenRange.Invalid } 
    let Catch = { Kind = Catch; Span = HalfOpenRange.Invalid } 
    let Do = { Kind = Do; Span = HalfOpenRange.Invalid } 
    let Final = { Kind = Final; Span = HalfOpenRange.Invalid } 
    let Finally = { Kind = Finally; Span = HalfOpenRange.Invalid } 
    let For = { Kind = For; Span = HalfOpenRange.Invalid } 
    let ForSome = { Kind = ForSome; Span = HalfOpenRange.Invalid } 
    let Implicit = { Kind = Implicit; Span = HalfOpenRange.Invalid } 
    let Import = { Kind = Import; Span = HalfOpenRange.Invalid } 
    let Lazy = { Kind = Lazy; Span = HalfOpenRange.Invalid } 
    let Object = { Kind = Object; Span = HalfOpenRange.Invalid } 
    let Package = { Kind = Package; Span = HalfOpenRange.Invalid } 
    let Private = { Kind = Private; Span = HalfOpenRange.Invalid } 
    let Protected = { Kind = Protected; Span = HalfOpenRange.Invalid } 
    let Requires = { Kind = Requires; Span = HalfOpenRange.Invalid } 
    let Return = { Kind = Return; Span = HalfOpenRange.Invalid } 
    let Sealed = { Kind = Sealed; Span = HalfOpenRange.Invalid } 
    let Throw = { Kind = Throw; Span = HalfOpenRange.Invalid } 
    let Trait = { Kind = Trait; Span = HalfOpenRange.Invalid } 
    let Try = { Kind = Try; Span = HalfOpenRange.Invalid } 
    let Type = { Kind = Type; Span = HalfOpenRange.Invalid } 
    let Val = { Kind = Val; Span = HalfOpenRange.Invalid } 
    let With = { Kind = With; Span = HalfOpenRange.Invalid } 
    let Yield = { Kind = Yield; Span = HalfOpenRange.Invalid }
