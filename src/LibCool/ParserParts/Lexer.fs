namespace LibCool.ParserParts


open System
open System.Text
open LibCool.SharedParts
open LibCool.DiagnosticParts
open LibCool.SourceParts


type Lexer(_source: Source, _diags: DiagnosticBag) =


    let mutable _token_start = 0u
    let mutable _offset: uint32 = 0u

    
    let span (): Span = Span.Of(_token_start, _offset)
            
            
    let peekChar () : char = _source[_offset]
    
    
    let isLetter (ch: char): bool = Char.IsLetter(ch) || ch = '_'
    let isDigit (ch: char): bool = Char.IsDigit(ch)
    

    let isEof (): bool = _offset = _source.Size
        
    
    let isAhead (substring: string): bool =
        if uint32 substring.Length > (_source.Size - _offset)
        then
            false
        else
        
        let mutable i = 0
        let mutable equal = true

        while equal && i < substring.Length do
            equal <- substring[i] = _source[_offset + uint32 i]
            i <- i + 1
        
        equal
    
    
    let eatChars (n: uint32) : unit =
        if _offset + n > _source.Size
        then
            invalidOp $"_offset [%d{_offset}] + [%d{n}] is > _source.Size [%d{_source.Size}]"
        
        _offset <- _offset + n
        
    
    let eatChar () : unit = eatChars 1u


    let eatWsCrlf (): unit =
        while not (isEof()) && Char.IsWhiteSpace(peekChar()) do
            eatChar()
            
            
    let tryEatLinebreak(): Span voption =
        let ch = peekChar()
        if ch = '\r' || ch = '\n'
        then
            eatChar()
            let linebreak_len = 
                if ch = '\r' && peekChar() = '\n'
                then
                    eatChar()
                    2u
                else
                    1u
                
            ValueSome (Span.Of(_offset - linebreak_len, _offset))
        else
            
        ValueNone
        
        
    let idKind (id: string): TokenKind =
        match id with
        // Keywords
        | "case" -> TokenKind.KwCase 
        | "class" -> TokenKind.KwClass 
        | "def" -> TokenKind.KwDef 
        | "else" -> TokenKind.KwElse 
        | "extends" -> TokenKind.KwExtends 
        | "false" -> TokenKind.KwFalse 
        | "if" -> TokenKind.KwIf 
        | "match" -> TokenKind.KwMatch 
        | "native" -> TokenKind.KwNative
        | "new" -> TokenKind.KwNew 
        | "null" -> TokenKind.KwNull 
        | "override" -> TokenKind.KwOverride 
        | "super" -> TokenKind.KwSuper 
        | "this" -> TokenKind.KwThis 
        | "true" -> TokenKind.KwTrue 
        | "var" -> TokenKind.KwVar 
        | "while" -> TokenKind.KwWhile 
        // Illegal/reserved keywords
        | "abstract" -> TokenKind.KwAbstract 
        | "catch" -> TokenKind.KwCatch 
        | "do" -> TokenKind.KwDo 
        | "final" -> TokenKind.KwFinal 
        | "finally" -> TokenKind.KwFinally 
        | "for" -> TokenKind.KwFor 
        | "forSome" -> TokenKind.KwForSome 
        | "implicit" -> TokenKind.KwImplicit 
        | "import" -> TokenKind.KwImport 
        | "lazy" -> TokenKind.KwLazy 
        | "object" -> TokenKind.KwObject 
        | "package" -> TokenKind.KwPackage 
        | "private" -> TokenKind.KwPrivate 
        | "protected" -> TokenKind.KwProtected 
        | "requires" -> TokenKind.KwRequires
        | "return" -> TokenKind.KwReturn
        | "sealed" -> TokenKind.KwSealed 
        | "throw" -> TokenKind.KwThrow 
        | "trait" -> TokenKind.KwTrait 
        | "try" -> TokenKind.KwTry 
        | "type" -> TokenKind.KwType 
        | "val" -> TokenKind.KwVal 
        | "with" -> TokenKind.KwWith 
        | "yield" -> TokenKind.KwYield
        | _ -> TokenKind.Id id
        
        
    static let _escaped_char_map: Map<char, char> = Map.ofList [
        ( '0', char 0 )
        ( 'b', '\b' )
        ( 't', '\t' )
        ( 'n', '\n' )
        ( 'r', '\r' )
        ( 'f', '\f' )
        ( '"', '"' )
        ( '\\', '\\' )
    ]

    
    let lexIdentifierOrKeyword(): Token =
        let sb_id = StringBuilder(peekChar().ToString())
        let mutable token_complete = false
        
        while not token_complete do
            eatChar()
            
            if isEof()
            then
                token_complete <- true
            else
            
            let ch = peekChar()
            
            token_complete <- not (isLetter ch || isDigit ch)
            if not token_complete
            then
                sb_id.Append(ch.ToString()) |> ignore
            
        Token.Of(idKind (sb_id.ToString()), span())
        
        
    let lexIntegerLiteral(): Token =
        let sb_int = StringBuilder(peekChar().ToString())
        let mutable token_complete = false
        
        while not token_complete do
            eatChar()
            
            if isEof()
            then
                token_complete <- true
            else
            
            let ch = peekChar()
            
            token_complete <- not (isDigit ch)
            if not token_complete
            then
                sb_int.Append(ch.ToString()) |> ignore
                
        let mutable value = 0
        if Int32.TryParse(sb_int.ToString(), &value)
        then 
            Token.Of(TokenKind.IntLiteral value, span())
        else
            
        _diags.Error("Invalid integer literal", span())
        Token.Invalid(span())
        
        
    let lexForwardSlashOrEatComment(): Token option =
        eatChar()

        if isEof()
        then
            Some (Token.Of(TokenKind.Slash, span()))
        else
            
        let ch1 = peekChar()
        
        if ch1 = '/'
        then
            // line comment
            eatChar()
            while not (isEof()) && tryEatLinebreak().IsNone do
                eatChar()
            
            None
        else
        
        if ch1 = '*'
        then
            // multiline comment
            eatChar()
            let mutable is_comment_terminated = false
            while not (isEof()) && not is_comment_terminated do
                is_comment_terminated <- 
                    peekChar() = '*' &&
                        (eatChar()
                         not (isEof()) && peekChar() = '/')
                eatChar()
                
            None
        else
            
        Some (Token.Of(TokenKind.Slash, span()))
        
    
    let lexQqqStringLiteral(): Token option =
        eatChars 3u
            
        let sb_qqq_literal = StringBuilder()
        let mutable token_opt: Token option = None
        
        while token_opt.IsNone do
            if isEof()
            then
                _diags.Error("Unterminated string literal", span())
                token_opt <- Some (Token.Of(TokenKind.StringLiteral (sb_qqq_literal.ToString()),
                                            Span.Of(_token_start, _offset)))
            else
            
            if isAhead "\"\"\""
            then
                token_opt <- Some (Token.Of(TokenKind.TripleQuotedStringLiteral (sb_qqq_literal.ToString()),
                                            Span.Of(_token_start, _offset + 3u)))
                eatChars 3u
            else
                
            sb_qqq_literal.Append(peekChar()) |> ignore
            eatChar()
        
        token_opt        
    
    
    let lexSimpleStringLiteral(): Token option =
        eatChar()
        
        let sb_literal = StringBuilder()
        let mutable token_opt: Token option = None

        while token_opt.IsNone do
            if isEof()
            then
                _diags.Error("Unterminated string literal", span())
                token_opt <- Some (Token.Of(TokenKind.StringLiteral (sb_literal.ToString()),
                                            Span.Of(_token_start, _offset)))
            else

            let ch1 = peekChar()
            if ch1 = '"'
            then
                token_opt <- Some (Token.Of(TokenKind.StringLiteral (sb_literal.ToString()),
                                            Span.Of(_token_start, _offset + 1u)))
                eatChar()
            else
                
            if ch1 = '\\'
            then
                eatChar()
                // We treat code similar to the following `\ \r\n`
                // as an escaped line-break, in spite of the fact
                // there's a space between `\` and `\r\n`.
                // This is consistent with real compilers, AFAIK.
                while not (isEof()) &&
                      (let ch = peekChar()
                       Char.IsWhiteSpace(ch) && ch <> '\r' && ch <> '\n') do
                    eatChar()
                
                if isEof()
                then
                    _diags.Error("Unterminated string literal", span())
                    token_opt <- Some (Token.Of(TokenKind.StringLiteral (sb_literal.ToString()),
                                                Span.Of(_token_start, _offset)))
                else

                let ch2 = peekChar()
                let is_linebreak = ch2 = '\r' || ch2 = '\n'

                // If a line-break is ahead of us, we do nothing.
                // A dedicated branch outside of the escaped chars processing code will deal with it.                         
                if not is_linebreak
                then
                    eatChar()
                    
                    if _escaped_char_map.ContainsKey(ch2)
                    then
                        sb_literal.Append(_escaped_char_map[ch2]) |> ignore
                    else
                        _diags.Error("Invalid escaped char in the string literal", Span.Of(_offset - 2u, _offset))
            else
                
            let linebreak_span = tryEatLinebreak()
            if linebreak_span.IsSome
            then
                _diags.Error("String literals cannot contain line breaks", linebreak_span.Value)
            else

            sb_literal.Append(ch1).AsUnit()
            eatChar()
            
        token_opt        


    let lexStringLiteral(): Token option =
        // Lookahead isn't, strictly speaking, necessary to lex triple-quoted string literals.
        // But looking ahead makes the code much, much simpler.
        if isAhead "\"\"\""
        then
            lexQqqStringLiteral()
        else
            lexSimpleStringLiteral()
    
    
    let tryLexPunctuator (ch: char): Token option =
        let token_kind_opt =
            match ch with
            | '+' -> Some TokenKind.Plus
            | '-' -> Some TokenKind.Minus
            // a dedicated function takes care of '/' -> TokenKind.Slash 
            | '*' -> Some TokenKind.Star
            | '=' ->
                eatChar()
                if isEof()
                then
                    Some TokenKind.Equal
                else

                match peekChar() with
                | '=' -> Some TokenKind.EqualEqual // ==
                | '>' -> Some TokenKind.EqualGreater // =>
                | _ -> Some TokenKind.Equal
            | '<' -> 
                eatChar()
                if isEof()
                then
                    Some TokenKind.Less
                else

                if peekChar() = '='
                then
                    Some TokenKind.LessEqual // <=
                else
                    Some TokenKind.Less
            | '>' ->
                eatChar()
                if isEof()
                then
                    Some TokenKind.Greater
                else
                
                if peekChar() = '='
                then
                    Some TokenKind.GreaterEqual // >=
                else
                    Some TokenKind.Greater
            | '!' ->
                eatChar()
                if isEof()
                then
                    Some TokenKind.Exclaim
                else

                if peekChar() = '='
                then
                    Some TokenKind.ExclaimEqual // !=
                else
                    Some TokenKind.Exclaim
            | '(' -> Some TokenKind.LParen
            | ')' -> Some TokenKind.RParen
            | '[' -> Some TokenKind.LSquare
            | ']' -> Some TokenKind.RSquare
            | '{' -> Some TokenKind.LBrace
            | '}' -> Some TokenKind.RBrace
            | ':' -> Some TokenKind.Colon
            | ';' -> Some TokenKind.Semi
            | '.' -> Some TokenKind.Dot
            | ',' -> Some TokenKind.Comma
            | _ -> None
            
        token_kind_opt
        |> Option.map (fun it ->
            // We already ate '=', '<', '>', or '!', don't try to do it again.
            if not (it = TokenKind.Equal || it = TokenKind.Less ||
                    it = TokenKind.Greater || it = TokenKind.Exclaim)
            then
                eatChar()
                
            Token.Of(it, span()))
        
    
    let get_next_or_eat_comment(): Token option =
        if isEof()
        then
            _offset <- _offset + 1u
            Some (Token.EOF(_source.Size))
        else
        
        let ch = peekChar()
        
        if isLetter ch
        then
            Some (lexIdentifierOrKeyword())
        else
            
        if isDigit ch
        then
            Some (lexIntegerLiteral())
        else
        
        if ch = '/'
        then
            lexForwardSlashOrEatComment()
        else
           
        match tryLexPunctuator ch with
        | Some punctuator_token ->
            Some punctuator_token
        | None ->
            
        if ch = '"'
        then
            lexStringLiteral()
        else
            
        eatChar()

        _diags.Error($"Unexpected character '%c{ch}'", span())
        Some (Token.Invalid(span()))
    
    
    let getNext (): Token =
        let mutable next_opt: Token option = None
        while next_opt.IsNone do
            if _offset > _source.Size
            then
                invalidOp $"_offset [%d{_offset}] is > _source.Size [%d{_source.Size}]"

            eatWsCrlf()

            _token_start <- _offset
            next_opt <- get_next_or_eat_comment()
            
        next_opt.Value
    
    
    member _.GetNext(): Token = getNext ()
