namespace LibCool.Frontend


[<RequireQualifiedAccess>]
module TokenArray =

    let ofLexer (lexer: Lexer): Token[] =
        seq {
            let mutable is_token_expected = true            
            while is_token_expected do
                let token = lexer.GetNext()
                yield token
                is_token_expected <- not token.IsEof
        } |> Array.ofSeq
    
    


