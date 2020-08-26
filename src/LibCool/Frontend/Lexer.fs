namespace LibCool.Frontend


open System
open System.Text
open LibCool.DiagnosticParts
open LibCool.SourceParts


type Lexer(_source: Source, _diags: DiagnosticBag) =


    let mutable _token_start = 0u
    let mutable _offset: uint32 = 0u

    
    let span (): Range = Range.Of(_token_start, _offset)
            
            
    let is_letter (ch: char): bool = Char.IsLetter(ch) || ch = '_'
    let is_digit (ch: char): bool = Char.IsDigit(ch)
    let is_ws (ch: char): bool = Char.IsWhiteSpace(ch)
    

    let is_eof (): bool = _offset = _source.Size
        
    
    let is_ahead (substring: string): bool =
        if uint32 substring.Length > (_source.Size - _offset)
        then
            false
        else
        
        let mutable i = 0
        let mutable equal = true

        while equal && i < substring.Length do
            equal <- substring.[i] = _source.[_offset + uint32 i]
            i <- i + 1
        
        equal
    
    
    let eat_chars (n: uint32) : unit =
        if _offset + n > _source.Size
        then
            invalidOp (sprintf "_offset [%d] + [%d] is > _source.Size [%d]" _offset n _source.Size)
        
        _offset <- _offset + n
        
    
    let eat_char () : unit = eat_chars 1u


    let peek_char () : char = _source.[_offset]
    
    
    let eat_ws_crlf (): unit =
        while not (is_eof()) && Char.IsWhiteSpace(peek_char()) do
            eat_char()
            
            
    let try_eat_linebreak(): Range =
        if is_ahead "\r\n"
        then
            eat_chars 2u
            Range.Of(_offset - 2u, _offset)
        else if (let ch = peek_char() in ch = '\r' || ch = '\n')
        then
            eat_char()
            Range.Of(_offset - 1u, _offset)
        else
            Range.Invalid
        
        
    let id_kind (id: string): TokenKind =
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
        | _ -> TokenKind.Identifier id
        
        
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

    
    let lex_identifier_or_keyword(): Token =
        let sb_id = StringBuilder(peek_char().ToString())
        let mutable token_complete = false
        
        while not token_complete do
            eat_char()
            
            if is_eof()
            then
                token_complete <- true
            else
            
            let ch = peek_char()
            
            token_complete <- not (is_letter ch || is_digit ch)
            if not token_complete
            then
                sb_id.Append(ch.ToString()) |> ignore
            
        Token.Of(id_kind (sb_id.ToString()), span())
        
        
    let lex_integer_literal(): Token =
        let sb_int = StringBuilder(peek_char().ToString())
        let mutable token_complete = false
        
        while not token_complete do
            eat_char()
            
            if is_eof()
            then
                token_complete <- true
            else
            
            let ch = peek_char()
            
            token_complete <- not (is_digit ch)
            if not token_complete
            then
                sb_int.Append(ch.ToString()) |> ignore
                
        let mutable value = 0
        if Int32.TryParse(sb_int.ToString(), &value)
        then 
            Token.Of(TokenKind.IntLiteral value, span())
        else
            
        _diags.Add(Diagnostic.Of(DiagnosticSeverity.Error, "Invalid integer literal", span()))
        Token.Invalid(span())
        
        
    let lex_forward_slash_or_eat_comment(): Token option =
        eat_char()

        if is_eof()
        then
            Some (Token.Of(TokenKind.Slash, span()))
        else
            
        let ch1 = peek_char()
        
        if ch1 = '/'
        then
            // line comment
            eat_char()
            while not (is_eof()) && (try_eat_linebreak() = Range.Invalid) do
                eat_char()
            
            None
        else
        
        if ch1 = '*'
        then
            // multiline comment
            eat_char()
            let mutable is_comment_terminated = false
            while not (is_eof()) && not is_comment_terminated do
                is_comment_terminated <- 
                    peek_char() = '*' &&
                        (eat_char()
                         not (is_eof()) && peek_char() = '/')
                eat_char()
                
            None
        else
            
        Some (Token.Of(TokenKind.Slash, span()))
        
    
    let lex_qqq_string_literal(): Token option =
        eat_chars 3u
            
        let sb_qqq_literal = StringBuilder()
        let mutable token_opt: Token option = None
        
        while Option.isNone token_opt do
            if is_eof()
            then
                _diags.Add(Diagnostic.Of(DiagnosticSeverity.Error, "Unterminated string literal", span()))
                token_opt <- Some (Token.Of(TokenKind.StringLiteral (sb_qqq_literal.ToString()),
                                            Range.Of(_token_start + 3u, _offset)))
            else
            
            if is_ahead "\"\"\""
            then
                token_opt <- Some (Token.Of(TokenKind.TripleQuotedStringLiteral (sb_qqq_literal.ToString()),
                                            Range.Of(_token_start + 3u, _offset + 1u)))
                eat_chars 3u
            else
                
            sb_qqq_literal.Append(peek_char()) |> ignore
            eat_char()
        
        token_opt        
    
    
    let lex_simple_string_literal(): Token option =
        eat_char()
        
        let sb_literal = StringBuilder()
        let mutable token_opt: Token option = None

        while Option.isNone token_opt do
            if is_eof()
            then
                _diags.Add(Diagnostic.Of(DiagnosticSeverity.Error, "Unterminated string literal", span()))
                token_opt <- Some (Token.Of(TokenKind.StringLiteral (sb_literal.ToString()),
                                            Range.Of(_token_start + 1u, _offset)))
            else

            let ch1 = peek_char()
            if ch1 = '"'
            then
                token_opt <- Some (Token.Of(TokenKind.StringLiteral (sb_literal.ToString()),
                                            Range.Of(_token_start + 1u, _offset + 1u)))
                eat_char()
            else
                
            if ch1 = '\\'
            then
                eat_char()
                // We treat code similar to the following `\ \r\n`
                // as an escaped line-break, in spite of the fact
                // there's a space between `\` and `\r\n`.
                // This is consistent with real compilers, AFAIK.
                while not (is_eof()) &&
                      (let ch = peek_char()
                       (is_ws ch) && ch <> '\r' && ch <> '\n') do
                    eat_char()
                
                if is_eof()
                then
                    _diags.Add(Diagnostic.Of(DiagnosticSeverity.Error, "Unterminated string literal", span()))
                    token_opt <- Some (Token.Of(TokenKind.StringLiteral (sb_literal.ToString()),
                                                Range.Of(_token_start + 1u, _offset)))
                else

                let ch2 = peek_char()
                let is_linebreak = is_ahead "\r\n" || ch2 = '\r' || ch2 = '\n'

                // If a line-break is ahead of us, we do nothing.
                // A dedicated branch outside of the escaped chars processing code will deal with it.                         
                if not is_linebreak
                then
                    eat_char()
                    
                    if _escaped_char_map.ContainsKey(ch2)
                    then
                        sb_literal.Append(_escaped_char_map.[ch2]) |> ignore
                    else
                        _diags.Add(Diagnostic.Of(DiagnosticSeverity.Error,
                                                 "Invalid escaped char in the string literal",
                                                 Range.Of(_offset - 2u, _offset)))
            else
                
            let linebreak_span = try_eat_linebreak()
            if linebreak_span <> Range.Invalid
            then
                _diags.Add(Diagnostic.Of(DiagnosticSeverity.Error,
                                         "String literals cannot contain line breaks",
                                         linebreak_span))
            else

            sb_literal.Append(ch1) |> ignore
            eat_char()
            
        token_opt        


    let lex_string_literal(): Token option =
        // Lookahead isn't, strictly speaking, necessary to lex triple-quoted string literals.
        // But looking ahead makes the code much, much simpler.
        if is_ahead "\"\"\""
        then
            lex_qqq_string_literal()
        else
            lex_simple_string_literal()
    
    
    let try_lex_punctuator (ch: char): Token option =
        let token_kind_opt =
            match ch with
            | '+' -> Some TokenKind.Plus
            | '-' -> Some TokenKind.Minus
            // a dedicated function takes care of '/' -> TokenKind.Slash 
            | '*' -> Some TokenKind.Star
            | '=' ->
                eat_char()
                if is_eof()
                then
                    Some TokenKind.Equal
                else

                match peek_char() with
                | '=' -> Some TokenKind.EqualEqual // ==
                | '>' -> Some TokenKind.EqualGreater // =>
                | _ -> Some TokenKind.Equal
            | '<' -> 
                eat_char()
                if is_eof()
                then
                    Some TokenKind.Less
                else

                if peek_char() = '='
                then
                    Some TokenKind.LessEqual // <=
                else
                    Some TokenKind.Less
            | '>' ->
                eat_char()
                if is_eof()
                then
                    Some TokenKind.Greater
                else
                
                if peek_char() = '='
                then
                    Some TokenKind.GreaterEqual // >=
                else
                    Some TokenKind.Greater
            | '!' ->
                eat_char()
                if is_eof()
                then
                    Some TokenKind.Exclaim
                else

                if peek_char() = '='
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
                eat_char()
                
            Token.Of(it, span()))
        
    
    let lex_next_or_eat_ws_and_comments(): Token option =
        if is_eof()
        then
            _offset <- _offset + 1u
            Some (Token.EOF(_source.Size))
        else
        
        let ch = peek_char()
        
        if is_ws ch
        then
            eat_ws_crlf()
            None
        else
        
        if is_letter ch
        then
            Some (lex_identifier_or_keyword())
        else
            
        if is_digit ch
        then
            Some (lex_integer_literal())
        else
        
        if ch = '/'
        then
            lex_forward_slash_or_eat_comment()
        else
           
        match try_lex_punctuator ch with
        | Some punctuator_token ->
            Some punctuator_token
        | None ->
            
        if ch = '"'
        then
            lex_string_literal()
        else
            
        eat_char()

        _diags.Add(Diagnostic.Of(DiagnosticSeverity.Error, sprintf "Unexpected character '%c'" ch, span()))
        Some (Token.Invalid(span()))
    
    
    let lex_next (): Token =
        let mutable result: Token option = None
        while Option.isNone result do
            if _offset > _source.Size
            then
                invalidOp (sprintf "_offset [%d] is > _source.Size [%d]" _offset _source.Size)

            _token_start <- _offset
            result <- lex_next_or_eat_ws_and_comments()
            
        result.Value
    
    
    member _.LexNext(): Token = lex_next ()
