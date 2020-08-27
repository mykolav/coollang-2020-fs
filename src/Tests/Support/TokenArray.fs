namespace Tests.Support


open LibCool.Frontend


[<RequireQualifiedAccess>]
module TokenArray =

    let ofLexer (lexer: Lexer): Token[] =
        seq {
            let mutable token = lexer.LexNext()
            yield token
            
            while token.Kind <> TokenKind.EOF do
                token <- lexer.LexNext()
                yield token
        } |> Array.ofSeq
    
    


