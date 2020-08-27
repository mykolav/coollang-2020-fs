namespace LibCool.Frontend


open System.Collections.Generic
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

    
    let token (): Token = _tokens.[_offset]
    let is (kind: TokenKind): bool = token().Kind = kind
    let is_eof (): bool = is TokenKind.EOF
    let is_id (): bool =
        match token().Kind with
        | TokenKind.Identifier _ -> true
        | _ -> false
    let is_int (): bool =
        match token().Kind with
        | TokenKind.IntLiteral _ -> true
        | _ -> false
    let is_string (): bool =
        match token().Kind with
        | TokenKind.StringLiteral _ -> true
        | _ -> false
    let is_qqq_string (): bool =
        match token().Kind with
        | TokenKind.TripleQuotedStringLiteral _ -> true
        | _ -> false

    
    let get_id (token: Token): string =
        match token.Kind with
        | TokenKind.Identifier value -> value
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
        
    
    let eat_token (): unit =
        if _offset + 1 >= _tokens.Length
        then
            invalidOp (sprintf "_offset [%d] + 1 is > _tokens.Length [%d]" _offset _tokens.Length)
        
        _offset <- _offset + 1
    
    
    let eat (kind: TokenKind): bool =
        if is kind
        then
            eat_token()
            true
        else
            false
            
            
    let varformals (): Node<VarFormal>[] voption =
        ValueNone
    
    
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
        Ok None
        
        
    let features (): Node<Feature>[] =
        [||]
        
    
    let class_decl (): Node<ClassDecl> voption =
        let span_start = token().Span.First
        
        if not (eat TokenKind.KwClass)
        then
            _diags.Add(Severity.Error, "Expected 'class'. Only classes can appear at the top level", token().Span)
            ValueNone
        else
            
        if not (is_id())
        then
            // TODO: Identifier expected; '{1}' is a keyword
            _diags.Add(Severity.Error, "Expected a class name. Class name must be an identifier", token().Span)
            ValueNone
        else
           
        let token_id = token()
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
        let class_decl_span = Span.Of(span_start, token().Span.First)
        ValueSome (Node.Of(class_decl_value, class_decl_span))
    
    
    let class_decls (): Node<ClassDecl>[] =
        let class_decl_nodes = List<Node<ClassDecl>>()
        while not (is_eof()) do
            match class_decl() with
            | ValueSome class_decl_node ->
                class_decl_nodes.Add(class_decl_node)
            | ValueNone ->
                while not (is_eof()) && not (is TokenKind.KwClass) do
                    eat_token()

        class_decl_nodes.ToArray()
        
        
    let ast (): Ast =
        let span_start = token().Span.First
        
        let class_decl_nodes = class_decls()
        let span = Span.Of((*first=*)span_start,
                           (*last=*)token().Span.First)
        
        { Program = Node.Of({ Program.ClassDecls = class_decl_nodes }, span) }

    
    member _.Parse() : Ast =
        ast()
