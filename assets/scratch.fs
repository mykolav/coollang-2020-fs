    let eat_expected (expected_token_infos: struct {| Matches: Token -> bool
                                                      GetErrorMessage: Token -> string |} [])
                     : Token[] =
        let eaten_tokens = List<Token>()
        
        let mutable is_expected_token = true
        let mutable i = 0
        while is_expected_token &&
              not _token.IsEof &&
              (i < expected_token_infos.Length) do
            let expected = expected_token_infos.[i]
            if expected.Matches(_token)
            then
                eaten_tokens.Add(_token)
                eat_token()
            else
                is_expected_token <- false
                _diags.Error(expected.GetErrorMessage(_token), _token.Span)
            i <- i + 1
            
        eaten_tokens.ToArray()
