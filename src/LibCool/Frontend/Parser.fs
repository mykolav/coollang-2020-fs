namespace rec LibCool.Frontend


open System.Collections.Generic
open System.Text
open LibCool.SourceParts
open LibCool.Ast
open LibCool.DiagnosticParts


type Parser(_tokens: Token[], _diags: DiagnosticBag) as this =

    
    let mutable _offset = 0
    let mutable _token = _tokens.[_offset]

        
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
    
    
    let expr (): Node<Expr> voption =
        ValueNone
    

    let stmt (): Node<Stmt> voption =
        ValueNone


    let block_info (terminators: seq<TokenKind>): ErrorOrOption<BlockInfo> =
        let ts_set = Set.ofSeq terminators
        
        let recover (): unit =
            // Recover from a syntactic error by eating tokens
            // until we find ';'
            // Then eat it -- hopefully this places us at the start of a statement.
            eat_until (Seq.append [TokenKind.Semi] terminators)
            if _token.Is(TokenKind.Semi)
            then
                eat_token()
        
        let is_block_end (): bool = _token.IsEof || ts_set.Contains(_token.Kind)
        
        let stmt_nodes = this.ParseDelimitedList(
                            element=stmt,
                            delimiter=TokenKind.Semi,
                            delimiter_error_message="';' expected. Statements of a block must be delimited by ';'",
                            recover=recover,
                            is_list_end=is_block_end)
        
        if stmt_nodes.Length = 0
        then
            Ok ValueNone
        else
            
        let stmts = stmt_nodes |> Seq.take (stmt_nodes.Length - 1) |> Array.ofSeq
        
        let last_stmt = stmt_nodes.[stmt_nodes.Length - 1]
        match last_stmt.Value with
        | Stmt.VarDecl _ ->
            _diags.Error("Blocks must end with an expression", last_stmt.Span)
            Error
        | Stmt.Expr expr ->
            Ok (ValueSome { BlockInfo.Stmts = stmts
                            Expr = Node.Of(expr, last_stmt.Span) })

    
    let braced_block (): Node<BlockInfo voption> voption =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.LBrace)
        then
            _diags.Error(
                "'{' expected. A braced block must start with '{'; an empty one is denoted by '{}'",
                _token.Span)
            ValueNone
        else

        let block_info_result = block_info((*terminators*)[TokenKind.RBrace])
        if block_info_result.IsError
        then
            ValueNone
        else
            
        if _token.IsEof
        then
            _diags.Error("'}' expected. A braced block must end with '}'", _token.Span)
            ValueNone
        else
            
        // Eat '}'
        eat_token()
        
        let block_value = block_info_result.Value
        let block_span = Span.Of(span_start, _token.Span.First)
        
        ValueSome (Node.Of(block_value, block_span))


    let varformal (): Node<VarFormal> voption =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.KwVar)
        then
            _diags.Error(
                "'var' expected. A varformal declaration must start with 'var'",
                _token.Span)
            ValueNone
        else
            
        if not (_token.IsId)
        then
            let sb_message =
                 StringBuilder()
                     .Append("A varformal name expected. Varformal name must be an identifier")
                     .Append(_token.KwDescription)
                
            _diags.Error(sb_message.ToString(), _token.Span)

            ValueNone
        else
            
        let token_id = _token
        eat_token()
            
        if not (eat TokenKind.Colon)
        then
            _diags.Error(
                "':' expected. A varformal's name and type must be delimited by ':'",
                _token.Span)
            ValueNone
        else
            
        if not (_token.IsId)
        then
            let sb_message =
                StringBuilder()
                    .Append("The varformal's type name expected. The type name must be an identifier")
                    .Append(_token.KwDescription)
                    
            _diags.Error(sb_message.ToString(), _token.Span)
            
            ValueNone
        else

        let token_type = _token
        eat_token()
        
        let id_node = Node.Of(ID token_id.Id, token_id.Span)
        let type_node = Node.Of(TYPE_NAME token_type.Id, token_type.Span)
        
        let varformal_value =
            { VarFormal.ID = id_node
              TYPE_NAME = type_node }
            
        let varformal_span = Span.Of(span_start, _token.Span.First)
        
        ValueSome (Node.Of(varformal_value, varformal_span))
    
    
    let varformals (): Node<VarFormal>[] voption =
        let recover (): unit =
            // Recover from a syntactic error by eating tokens
            // until we find 'var' -- the start of another varformal.
            eat_until [TokenKind.RParen; TokenKind.KwVar]
            
        let is_varformals_end (): bool = _token.IsEof || _token.Is(TokenKind.RParen)

        this.ParseEnclosedDelimitedList(
            list_start=TokenKind.LParen,
            list_start_error_message="'(' expected. A varformals list must start with '('; an empty one is denoted by '()'",
            list_end_error_message="')' expected. A varformals list must end with ')'",
            element=varformal,
            delimiter=TokenKind.Comma,
            delimiter_error_message="',' expected. Elements of a varformal list must be delimited by ','",
            recover=recover,
            is_list_end=is_varformals_end)
        
        
    let actuals (): Node<Expr>[] voption =
        let recover (): unit =
            // Recover from a syntactic error by eating tokens
            // until we find ','
            // Then eat it -- hopefully this places us at the start of another formal.
            eat_until [TokenKind.RParen; TokenKind.Comma]
            if _token.Is(TokenKind.Comma)
            then
                eat_token()
                
        let is_actuals_end (): bool = _token.IsEof || _token.Is(TokenKind.RParen)

        this.ParseEnclosedDelimitedList(
            list_start=TokenKind.LParen,
            list_start_error_message="'(' expected. An actuals list must start with '('; an empty one is denoted by '()'",
            list_end_error_message="')' expected. An actuals list must end with ')'",
            element=expr,
            delimiter=TokenKind.Comma,
            delimiter_error_message="',' expected. Elements of an actuals list must be delimited by ','",
            recover=recover,
            is_list_end=is_actuals_end)
    
    
    let extends (): ErrorOrOption<Node<Extends>> =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.KwExtends)
        then
            Ok ValueNone
        else

        if not (_token.IsId)
        then
            let sb_message =
                StringBuilder()
                    .Append("A parent class name expected. Parent class name must be an identifier")
                    .Append(_token.KwDescription)
                    
            _diags.Error(sb_message.ToString(), _token.Span)
            
            Error
        else
            
        let token_id = _token
        eat_token()
           
        let actual_nodes_opt = actuals()
        if ValueOption.isNone actual_nodes_opt
        then
            Error
        else
        
        let extends_info =
            { ExtendsInfo.PARENT_NAME = Node.Of(TYPE_NAME token_id.Id, token_id.Span)
              Actuals = actual_nodes_opt.Value }
              
        let extends_span = Span.Of(span_start, _token.Span.First)
            
        Ok (ValueSome (Node.Of(Extends.Info extends_info, extends_span)))
        
        
    let formal (): Node<Formal> voption =
        let span_start = _token.Span.First
        
        if not (_token.IsId)
        then
            let sb_message =
                 StringBuilder()
                     .Append("A formal name expected. Formal name must be an identifier")
                     .Append(_token.KwDescription)
                
            _diags.Error(sb_message.ToString(), _token.Span)

            ValueNone
        else
            
        let token_id = _token
        eat_token()
            
        if not (eat TokenKind.Colon)
        then
            _diags.Error(
                "':' expected. A formal's name and type must be delimited by ':'",
                _token.Span)
            ValueNone
        else
            
        if not (_token.IsId)
        then
            let sb_message =
                StringBuilder()
                    .Append("The formal's type name expected. The type name must be an identifier")
                    .Append(_token.KwDescription)
                    
            _diags.Error(sb_message.ToString(), _token.Span)
            
            ValueNone
        else

        let token_type = _token
        eat_token()
        
        let id_node = Node.Of(ID token_id.Id, token_id.Span)
        let type_node = Node.Of(TYPE_NAME token_type.Id, token_type.Span)
        
        let formal_value =
            { Formal.ID = id_node
              TYPE_NAME = type_node }
            
        let formal_span = Span.Of(span_start, _token.Span.First)
        
        ValueSome (Node.Of(formal_value, formal_span))
    
    
    let formals (): Node<Formal>[] voption =
        let recover (): unit =
            // Recover from a syntactic error by eating tokens
            // until we find ','
            // Then eat it -- hopefully this places us at the start of another formal.
            eat_until [TokenKind.RParen; TokenKind.Comma]
            if _token.Is(TokenKind.Comma)
            then
                eat_token()
                
        let is_formals_end (): bool = _token.IsEof || _token.Is(TokenKind.RParen)

        this.ParseEnclosedDelimitedList(
            list_start=TokenKind.LParen,
            list_start_error_message="'(' expected. A formals list must start with '('; an empty one is denoted by '()'",
            list_end_error_message="')' expected. A formals list must end with ')'",
            element=formal,
            delimiter=TokenKind.Comma,
            delimiter_error_message="',' expected. Elements of a formals list must be delimited by ','",
            recover=recover,
            is_list_end=is_formals_end)
    
    
    let method (span_start: uint32, is_override: bool) : Node<Feature> voption =
        if not _token.IsId
        then
            let sb_message =
                StringBuilder()
                    .Append("A method name expected. Method name must be an identifier")
                    .Append(_token.KwDescription)
            _diags.Error(sb_message.ToString(), _token.Span)
            ValueNone
        else
            
        let token_id = _token
        eat_token()
        
        let formal_nodes_opt = formals()
        if formal_nodes_opt.IsNone
        then
            ValueNone
        else
            
        if not (eat TokenKind.Colon)
        then
            _diags.Error("':' expected. A method's formals and return type must be delimited by ':'", _token.Span)
            ValueNone
        else

        if not _token.IsId
        then
            let sb_message =
                StringBuilder()
                    .Append("A return type name expected. Type name must be an identifier")
                    .Append(_token.KwDescription)
            _diags.Error(sb_message.ToString(), _token.Span)
            ValueNone
        else
            
        let token_type = _token
        eat_token()
        
        if not (eat TokenKind.Equal)
        then
            _diags.Error("'=' expected. A method's return type and body must be delimited by '='", _token.Span)
            ValueNone
        else
            
        let expr_node_opt = expr()
        if expr_node_opt.IsNone
        then
            ValueNone
        else
        
        let expr_node = expr_node_opt.Value
        let method_info_value =
            { Override = is_override
              ID = Node.Of(ID token_id.Id, token_id.Span)
              Formals = formal_nodes_opt.Value
              TYPE_NAME =  Node.Of(TYPE_NAME token_type.Id, token_type.Span)
              MethodBody = Node.Of(MethodBody.Expr expr_node.Value, expr_node.Span) }

        let method_info_span = Span.Of(span_start, _token.Span.First)
        
        ValueSome (Node.Of(Feature.Method method_info_value, method_info_span))
        
        
    let attribute (): Node<Feature> voption =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.KwVar)
        then
            ValueNone
        else
            
        if not (_token.IsId)
        then
            let sb_message =
                 StringBuilder()
                     .Append("An attribute name expected. Attribute name must be an identifier")
                     .Append(_token.KwDescription)
                
            _diags.Error(sb_message.ToString(), _token.Span)

            ValueNone
        else
            
        let token_id = _token
        eat_token()
            
        if not (eat TokenKind.Colon)
        then
            _diags.Error(
                "':' expected. An attribute's name and type must be delimited by ':'",
                _token.Span)
            ValueNone
        else
            
        if not (_token.IsId)
        then
            let sb_message =
                StringBuilder()
                    .Append("The attribute's type name expected. The type name must be an identifier")
                    .Append(_token.KwDescription)
                    
            _diags.Error(sb_message.ToString(), _token.Span)
            
            ValueNone
        else

        let token_type = _token
        eat_token()
        
        if not (eat TokenKind.Equal)
        then
            _diags.Error("'=' expected. An attribute's type and initializer must be delimited by '='", _token.Span)
            ValueNone
        else
            
        let expr_node_opt = expr()
        if expr_node_opt.IsNone
        then
            ValueNone
        else
        
        let expr_node = expr_node_opt.Value
        
        let attribute_value: AttrInfo =
            { ID = Node.Of(ID token_id.Id, token_id.Span)
              TYPE_NAME = Node.Of(TYPE_NAME token_type.Id, token_type.Span)
              AttrBody = Node.Of(AttrBody.Expr expr_node.Value, expr_node.Span) }
            
        let attribute_span = Span.Of(span_start, _token.Span.First)
        
        ValueSome (Node.Of(Feature.Attr attribute_value, attribute_span))
        
        
    let feature (): Node<Feature> voption =
        let span_start = _token.Span.First
        
        if eat TokenKind.KwOverride
        then
            if not (eat TokenKind.KwDef)
            then
                _diags.Error("'def' expected. An overriden method must start with 'override def'", _token.Span)
                ValueNone
            else
                
            method(span_start, (*is_override=*)true)
        else

        if eat TokenKind.KwDef
        then
            method(span_start, (*is_override=*)false)
        else

        if _token.Is(TokenKind.KwVar)
        then
            attribute()
        else

        if _token.Is(TokenKind.LBrace)
        then
            let block_node_opt = braced_block()
            if block_node_opt.IsNone
            then
                ValueNone
            else
                
            let block_node = block_node_opt.Value
            ValueSome (Node.Of(Feature.BracedBlock block_node, block_node.Span))
        else
            
        _diags.Error(
            "'def', 'override def', 'var', or '{' expected. A class feature must be a method, attribute, or block",
            _token.Span)    
        ValueNone


    let classbody (): Node<Feature>[] voption =
        // if not (eat TokenKind.LBrace)
        // then
        //     _diags.Error(
        //         "'{' expected. A class body must start with '{'; an empty one is denoted by '{}'",
        //         _token.Span)
        //     ValueNone
        // else
            
        let feature_nodes = List<Node<Feature>>()

        let recover (): unit = 
            // Recover from a syntactic error by eating tokens
            // until we find the beginning of another feature
            eat_until [ // end of classbody
                       TokenKind.RBrace
                       // start of a block
                       TokenKind.LBrace 
                       // start of a method
                       TokenKind.KwOverride 
                       TokenKind.KwDef
                       // start of an attribute
                       TokenKind.KwVar]
            
        let is_classbody_end (): bool = _token.IsEof || _token.Is(TokenKind.RBrace)
        
        let mutable is_feature_expected = not (is_classbody_end())
        while is_feature_expected do
            match feature() with
            | ValueSome feature_node ->
                feature_nodes.Add(feature_node)
                
                if not (eat TokenKind.Semi)
                then
                    _diags.Error("';' expected. Features must be terminated by ';'", _token.Span)
                    recover()

            | ValueNone ->
                // We didn't manage to parse a feature.
                recover ()

            is_feature_expected <- not (is_classbody_end())
            
        if (_token.IsEof)
        then
            _diags.Error("'}' expected. A class body must end with '}'", _token.Span)
            ValueNone
        else
            
        // Eat '}'
        eat_token()
        
        ValueSome (feature_nodes.ToArray())
        
    
    let class_decl (): Node<ClassDecl> voption =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.KwClass)
        then
            _diags.Error("'class' expected. Only classes can appear at the top level", _token.Span)
            ValueNone
        else
            
        if not (_token.IsId)
        then
            let sb_message =
                StringBuilder()
                    .Append("A class name expected. Class name must be an identifier")
                    .Append(_token.KwDescription)
                
            _diags.Error(sb_message.ToString(), _token.Span)
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
        if extends_node_result.IsError
        then
            // The callee already emitted relevant diags.
            // We aren't going to add anything else.
            ValueNone
        else
            
        if not (eat TokenKind.LBrace)
        then
            let sb_message =
                StringBuilder()
                    .Append(if extends_node_result.IsNone then "'extends' or " else "")
                    .Append("'{' expected. A class body must start with '{'")
            _diags.Error(sb_message.ToString(), _token.Span)
            ValueNone
        else
            
        let feature_nodes_opt = classbody()
        if feature_nodes_opt.IsNone
        then
            // The callee already emitted relevant diags.
            // We aren't going to add anything else.
            ValueNone
        else
        
        if (not (eat TokenKind.RBrace))
        then
            // The callee already emitted relevant diags.
            // We aren't going to add anything else.
            ValueNone
        else
            
        let name_node = Node.Of(TYPE_NAME token_id.Id, token_id.Span)
        
        let class_decl_value =
            { ClassDecl.NAME = name_node
              VarFormals = varformals_node_opt.Value
              Extends = extends_node_result.Value
              ClassBody = feature_nodes_opt.Value }
        let class_decl_span = Span.Of(span_start, _token.Span.First)
        ValueSome (Node.Of(class_decl_value, class_decl_span))
    
    
    let class_decls (): Node<ClassDecl>[] =
        let class_decl_nodes = List<Node<ClassDecl>>()
        while not _token.IsEof do
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

    
    member this.ParseDelimitedList<'T>(element: unit -> Node<'T> voption,
                                       delimiter: TokenKind,
                                       delimiter_error_message: string,
                                       recover: unit -> unit,
                                       is_list_end: unit -> bool)
                                       : Node<'T>[] =
        
        let element_nodes = List<Node<'T>>()
        
        let mutable is_element_expected = not (is_list_end())
        while is_element_expected do
            match element() with
            | ValueSome element_node ->
                element_nodes.Add(element_node)
                
                if is_list_end()
                then
                    is_element_expected <- false
                else

                if eat delimiter
                then
                    is_element_expected <- true
                else

                _diags.Error(delimiter_error_message, _token.Span)
                
                // We didn't find `delimiter` where expected.
                recover()
                is_element_expected <- not (is_list_end())
            | ValueNone ->
                // We didn't manage to parse an element
                recover()
                is_element_expected <- not (is_list_end())
            
        element_nodes.ToArray()


    member this.ParseEnclosedDelimitedList<'T>(list_start: TokenKind,
                                               list_start_error_message: string,
                                               list_end_error_message: string,
                                               element: unit -> Node<'T> voption,
                                               delimiter: TokenKind,
                                               delimiter_error_message: string,
                                               recover: unit -> unit,
                                               is_list_end: unit -> bool)
                                               : Node<'T>[] voption =
        
        if not (eat list_start)
        then
            _diags.Error(list_start_error_message, _token.Span)
            ValueNone
        else

        let element_nodes = this.ParseDelimitedList(element, delimiter, delimiter_error_message, recover, is_list_end)

        if _token.IsEof
        then
            _diags.Error(list_end_error_message, _token.Span)
            ValueNone
        else
            
        // Eat the token that closes the list
        eat_token()
        
        ValueSome element_nodes


    member _.Parse() : Ast =
        ast()
