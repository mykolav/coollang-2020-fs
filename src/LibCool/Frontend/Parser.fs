namespace LibCool.Frontend


open System.Collections.Generic
open System.Text
open LibCool.SourceParts
open LibCool.Ast
open LibCool.DiagnosticParts


// SYNTAX ERROR HANDLING:
// If a syntax error
//   a) Keep parsing other parts of the syntax node
//      as to diagnose as many syntax errors in one go as possible 
//   b) Evaluate to None until "bubble up" to a syntax nodes collection
//      or a syntax node with an optional child 
// ... ERR123: File.cool:LL:CC:  An incomplete feature ...
//
// ... ERR123: File.cool:LL:CC: An incomplete var declaration
//     NOTE: 'foo' was not expected at this point of the var declaration
//     NOTE: Assuming 'foo' begins the next syntax element
//     (If 'foo' doesn't match any expression's beginning,
//        a) Skip to the first token matching any relevant syntax node 
//        b) Report the skipped tokens as invalid)
//
// ... ERR123: File.cool:LL:CC: An incomplete var declaration
//     NOTE: 'var' was not expected at this point of the var declaration
//     NOTE: Assuming 'var' is the next var declaration's begging
type Parser(_tokens: Token[], _diags: DiagnosticBag) =

    
    let mutable _offset = 0
    let mutable _token = _tokens.[_offset]

    
    static let get_id (token: Token): string =
        match token.Kind with
        | TokenKind.Id value -> value
        | _ -> invalidArg "token" "The token is not an identifier"
    
    
    static let get_int (token: Token): int =
        match token.Kind with
        | TokenKind.IntLiteral value -> value
        | _ -> invalidArg "token" "The token is not an int literal"
    
    
    static let get_string (token: Token): string =
        match token.Kind with
        | TokenKind.StringLiteral value -> value
        | _ -> invalidArg "token" "The token is not a string literal"
    
    
    static let get_qqq_string (token: Token): string =
        match token.Kind with
        | TokenKind.TripleQuotedStringLiteral value -> value
        | _ -> invalidArg "token" "The token is not a \"\"\" string literal"
        
        
    static let get_kw_spelling (token: Token): string =
        if not (token.IsKw || token.IsReservedKw)
        then
            invalidArg "token" "The token is not a keyword"
            
        token.Kind.ToString().Replace("TokenKind.Kw", "").ToLower()

    
    static let get_kw_kind_spelling (token: Token): string =        
        if not (token.IsKw || token.IsReservedKw)
        then
            invalidArg "token" "The token is not a keyword"
            
        if (token.IsKw) then "keyword" else "reserved keyword"

        
    static let get_kw_description (token: Token): string =
        if token.IsKw || token.IsReservedKw
        then
            sprintf "; '%s' is a %s" (get_kw_spelling token) (get_kw_kind_spelling token)
        else
            ""

        
    let eat_token (): unit =
        if _offset + 1 >= _tokens.Length
        then
            invalidOp (sprintf "_offset [%d] + 1 is >=f _tokens.Length [%d]" _offset _tokens.Length)
        
        _offset <- _offset + 1
        _token <- _tokens.[_offset]
    
    
    let eat (kind: TokenKind): bool =
        if _token.Is kind
        then
            eat_token()
            true
        else
            false
            
            
    let eat_until (kinds: seq<TokenKind>): unit =
        let kind_set = Set.ofSeq kinds
        while not (_token.IsEof ||
                   kind_set.Contains(_token.Kind)) do
            eat_token()
    
    
    let varformal (): Node<VarFormal> voption =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.KwVar)
        then
            _diags.Add(
                Severity.Error,
                "'var' expected. A varformal declaration must start with 'var'",
                _token.Span)
            ValueNone
        else
            
        if not (_token.IsId)
        then
            let sb_message =
                 StringBuilder()
                     .Append("A varformal name expected. Varformal name must be an identifier")
                     .Append(get_kw_description _token)
                
            _diags.Add(Severity.Error, sb_message.ToString(), _token.Span)

            ValueNone
        else
            
        let token_id = _token
        eat_token()
            
        if not (eat TokenKind.Colon)
        then
            _diags.Add(
                Severity.Error,
                "':' expected. A varformal's name and type must be delimited by ':'",
                _token.Span)
            ValueNone
        else
            
        if not (_token.IsId)
        then
            let sb_message =
                StringBuilder()
                    .Append("The varformal's type name expected. The type name must be an identifier")
                    .Append(get_kw_description _token)
                    
            _diags.Add(Severity.Error, sb_message.ToString(), _token.Span)
            
            ValueNone
        else

        let token_type = _token
        eat_token()
        
        let id_node = Node.Of(ID (get_id token_id), token_id.Span)
        let type_node = Node.Of(TYPE_NAME (get_id token_type), token_type.Span)
        
        let varformal_value =
            { VarFormal.ID = id_node
              TYPE_NAME = type_node }
            
        let varformal_span = Span.Of(span_start, _token.Span.First)
        
        ValueSome (Node.Of(varformal_value, varformal_span))
    
    
    let varformals (): Node<VarFormal>[] voption =
        if not (eat TokenKind.LParen)
        then
            _diags.Add(
                Severity.Error,
                "'(' expected. A varformals list must start with '('; an empty one is denoted as '()'",
                _token.Span)
            ValueNone
        else
            
        let varformal_nodes = List<Node<VarFormal>>()

        let mutable is_varformal_expected = not (_token.IsEof || _token.Is(TokenKind.RParen))
        while is_varformal_expected do
            match varformal() with
            | ValueSome varformal_node ->
                varformal_nodes.Add(varformal_node)
                
                if _token.IsEof || _token.Is(TokenKind.RParen)
                then
                    is_varformal_expected <- false
                else

                if (eat TokenKind.Comma)
                then
                    is_varformal_expected <- true
                else

                _diags.Add(
                    Severity.Error,
                    "',' expected. Elements of a varformal list must be delimited by ','",
                    _token.Span)
                
                // We didn't find ',' where expected.
                // Recover from this error by eating tokens
                // until we find 'var' -- the start of another varformal.
                eat_until [TokenKind.RParen; TokenKind.KwVar]
                is_varformal_expected <- _token.Is(TokenKind.KwVar)
            | ValueNone ->
                // We didn't manage to parse a varformal.
                // Recover from this error by eating tokens
                // until we find 'var' -- the start of another varformal.
                eat_until [TokenKind.RParen; TokenKind.KwVar]
                is_varformal_expected <- _token.Is(TokenKind.KwVar)
            
        if (_token.IsEof)
        then
            _diags.Add(
                Severity.Error,
                "')' expected. A varformals list must end with ')'",
                _token.Span)
            ValueNone
        else
            
        // Eat ')'
        eat_token()
        
        ValueSome (varformal_nodes.ToArray())
    
    
    let extends (): ErrorOrOption<Node<Extends>> =
        (*
        if eat TokenKind.KwExtends
        then
            if not (eat TokenKind.Identifier)
            then
                // Diag...
                None
            else
               
            let actuals_node_opt = actuals() 
            if Option.isNone actuals_node_opt
            then
                // Diag...
                None
            else
                None
        else
        *)
        Ok ValueNone
        
        
    let features (): Node<Feature>[] =
        [||]
        
    
    let class_decl (): Node<ClassDecl> voption =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.KwClass)
        then
            _diags.Add(Severity.Error, "'class' expected. Only classes can appear at the top level", _token.Span)
            ValueNone
        else
            
        if not (_token.IsId)
        then
            let sb_message =
                StringBuilder()
                    .Append("A class name expected. Class name must be an identifier")
                    .Append(get_kw_description _token)
                
            _diags.Add(Severity.Error, sb_message.ToString(), _token.Span)
            ValueNone
        else
           
        let token_id = _token
        eat_token()
        
        let varformals_node_opt = varformals()
        if ValueOption.isNone varformals_node_opt
        then
            // The callee already emitted relevant diags.
            // We aren't going to add anything else.
            ValueNone
        else
        
        let extends_node_result = extends()
        if ErrorOrOption.isError extends_node_result
        then
            // The callee already emitted relevant diags.
            // We aren't going to add anything else.
            ValueNone
        else
            
        if not (eat TokenKind.LBrace)
        then
            // Diag...
            ValueNone
        else
            
        let feature_nodes = features()
        
        if (not (eat TokenKind.RBrace))
        then
            // Probably, we don't want to emit any diagnostics here.
            // Instead `features()` should emit something along the lines:
            // "Error: Did not expect $token. Expected a feature or '}'"
            ValueNone
        else
            
        let name_node = Node.Of(TYPE_NAME (get_id token_id), token_id.Span)
        
        let class_decl_value =
            { ClassDecl.NAME = name_node
              VarFormals = varformals_node_opt.Value
              Extends = extends_node_result.Value
              ClassBody = feature_nodes }
        let class_decl_span = Span.Of(span_start, _token.Span.First)
        ValueSome (Node.Of(class_decl_value, class_decl_span))
    
    
    let class_decls (): Node<ClassDecl>[] =
        let class_decl_nodes = List<Node<ClassDecl>>()
        while not (_token.IsEof) do
            match class_decl() with
            | ValueSome class_decl_node ->
                class_decl_nodes.Add(class_decl_node)
            | ValueNone ->
                // We didn't manage to parse a class declaration.
                // We can start our next attempt to parse from only a 'class' keyword.
                // Let's skip all tokens until we find a 'class' keyword,
                // as otherwise we'd have to report every non-'class' token as unexpected,
                // and that would create a bunch of unhelpful diagnostics.
                eat_until [TokenKind.KwClass]

        class_decl_nodes.ToArray()
        
        
    let ast (): Ast =
        let span_start = _token.Span.First
        
        let class_decl_nodes = class_decls()
        let span = Span.Of((*first=*)span_start,
                           (*last=*)_token.Span.First)
        
        { Program = Node.Of({ Program.ClassDecls = class_decl_nodes }, span) }

    
    member _.Parse() : Ast =
        ast()
