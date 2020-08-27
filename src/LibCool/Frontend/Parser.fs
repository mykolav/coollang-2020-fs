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

    
    let get_id (token: Token): string =
        match token.Kind with
        | TokenKind.Id value -> value
        | _ -> invalidArg "token" "The token is not an identifier"
    
    
    let get_int (token: Token): int =
        match token.Kind with
        | TokenKind.IntLiteral value -> value
        | _ -> invalidArg "token" "The token is not an int literal"
    
    
    let get_string (token: Token): string =
        match token.Kind with
        | TokenKind.StringLiteral value -> value
        | _ -> invalidArg "token" "The token is not a string literal"
    
    
    let get_qqq_string (token: Token): string =
        match token.Kind with
        | TokenKind.TripleQuotedStringLiteral value -> value
        | _ -> invalidArg "token" "The token is not a \"\"\" string literal"
        
        
    let get_kw_spelling (token: Token): string =
        if not (token.IsKw || token.IsReservedKw)
        then
            invalidArg "token" "The token is not a keyword"
            
        _token.Kind.ToString().Replace("TokenKind.Kw", "").ToLower()

    
    let get_kw_kind_spelling (token: Token): string =        
        if not (token.IsKw || token.IsReservedKw)
        then
            invalidArg "token" "The token is not a keyword"
            
        if (_token.IsKw) then "keyword" else "reserved keyword"

        
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
    
    
    let varformal (): Node<VarFormal> voption =
        ValueNone
    
    
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
        while not (_token.IsEof) && not (_token.Is(TokenKind.RParen)) do
            eat_token()
            
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
            let sb_message = StringBuilder("A class name expected. Class name must be an identifier")
            if (_token.IsKw || _token.IsReservedKw)
            then
                sb_message.AppendFormat("; '{0}' is a {1}", get_kw_spelling _token, get_kw_kind_spelling _token) |> ignore;
                
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
                // We can only start our next attempt to parse from a 'class' keyword.
                // Let's skip all tokens until we find a 'class' keyword,
                // as otherwise we'd have to report every non-'class' token as unexpected,
                // and that would create a bunch of unhelpful diagnostics.
                //
                // In a specific case, where we have `class class`,
                // `class_decl()` will complain the 'class' keyword is not a valid class name,
                // and will not eat the second 'class' token.
                // We need to eat the second 'class' unconditionally here,
                // otherwise next invocation of `class_decl()` will start parsing from it,
                // and report confusing syntax errors as a result.
                let mutable found_kw_class = false
                while not (_token.IsEof) && not found_kw_class do
                    eat_token()
                    found_kw_class <- _token.Is(TokenKind.KwClass)

        class_decl_nodes.ToArray()
        
        
    let ast (): Ast =
        let span_start = _token.Span.First
        
        let class_decl_nodes = class_decls()
        let span = Span.Of((*first=*)span_start,
                           (*last=*)_token.Span.First)
        
        { Program = Node.Of({ Program.ClassDecls = class_decl_nodes }, span) }

    
    member _.Parse() : Ast =
        ast()
