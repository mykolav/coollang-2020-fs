namespace LibCool.ParserParts


[<RequireQualifiedAccess>]
module Prec =
    let OfDot = 8y
    let OfExclaim = 7y
    let OfUnaryMinus = 7y
    let OfStar = 6y
    let OfSlash = 6y
    let OfPlus = 5y
    let OfMinus = 5y
    let OfEqualEqual = 4y
    let OfNotEqual = 4y
    let OfLessEqual = 3y
    let OfLess = 3y
    let OfGreaterEqual = 3y
    let OfGreater = 3y
    let OfMatch = 2y
    let OfIf = 1y
    let OfWhile = 1y
    let OfEqual = 0y
    
    let Min = OfEqual
    let Max = OfDot
    let Empty = -1y

    let Of: TokenKind -> sbyte = function
        | TokenKind.LessEqual    -> OfLessEqual
        | TokenKind.Less         -> OfLess
        | TokenKind.GreaterEqual -> OfGreaterEqual
        | TokenKind.Greater      -> OfGreater
        | TokenKind.EqualEqual   -> OfEqualEqual
        | TokenKind.ExclaimEqual -> OfNotEqual
        | TokenKind.Star         -> OfStar
        | TokenKind.Slash        -> OfSlash
        | TokenKind.Plus         -> OfPlus
        | TokenKind.Minus        -> OfMinus
        | TokenKind.KwMatch      -> OfMatch
        | TokenKind.Dot          -> OfDot
        // The TokenKind doesn't correspond to any operator.
        | _                      -> Empty


module TokenExtensions =
    type Token
        with
        
        
        member this.KwSpelling: string =
            if not (this.IsKw || this.IsReservedKw)
            then
                invalidArg "token" "The token is not a keyword"
                
            let raw_spelling = this.Kind.ToString()
            let spelling = 
                if raw_spelling.StartsWith("TokenKind.Kw")
                then raw_spelling.Substring("TokenKind.Kw".Length)
                else raw_spelling.Substring("Kw".Length)

            spelling.ToLower()

        
        member this.KwKindSpelling: string =        
            if not (this.IsKw || this.IsReservedKw)
            then
                invalidArg "token" "The token is not a keyword"
                
            if (this.IsKw) then "keyword" else "reserved keyword"
        
        
        member this.KwDescription: string =
            if this.IsKw || this.IsReservedKw
            then
                sprintf "; '%s' is a %s" (this.KwSpelling) (this.KwKindSpelling)
            else
                ""
    
    
        member this.IsInfixOp: bool =
            match this.Kind with
            | TokenKind.Plus
            | TokenKind.Minus
            | TokenKind.Slash
            | TokenKind.Star
            | TokenKind.EqualEqual
            | TokenKind.ExclaimEqual
            | TokenKind.Less
            | TokenKind.Greater
            | TokenKind.LessEqual
            | TokenKind.GreaterEqual -> true
            | _                      -> false


        member this.InfixOpSpelling: string =
            match this.Kind with            
            | TokenKind.Plus         -> "+"
            | TokenKind.Minus        -> "-"
            | TokenKind.Slash        -> "/"
            | TokenKind.Star         -> "*"
            | TokenKind.EqualEqual   -> "=="
            | TokenKind.ExclaimEqual -> "!="
            | TokenKind.Less         -> "<"
            | TokenKind.Greater      -> ">"
            | TokenKind.LessEqual    -> "<="
            | TokenKind.GreaterEqual -> ">="
            | _                      -> invalidArg "token" "The token is not an infix operator"
