namespace Tests.Support


open LibCool.Frontend


[<RequireQualifiedAccess>]
module TokenArray =

    let ofLexer (lexer: Lexer): Token[] =
        seq {
            let mutable token = lexer.GetNext()
            yield token
            
            while token.Kind <> TokenKind.EOF do
                token <- lexer.GetNext()
                yield token
        } |> Array.ofSeq
    
    


