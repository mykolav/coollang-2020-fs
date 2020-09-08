namespace rec LibCool.Frontend


open System.Collections.Generic
open System.Linq
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
            
            
    let eat (kind: TokenKind)
            (error_message: string): bool =
        if _token.Is kind
        then
            eat_token()
            true
        else
            _diags.Error(error_message, _token.Span)
            false


    let try_eat (kind: TokenKind): bool =
        if _token.Is kind
        then
            eat_token()
            true
        else
            false
            
            
    let try_eat_when (is_match: bool): bool =
        if is_match
        then
            eat_token()
            true
        else
            false

                
    let eat_when (is_match: bool)
                 (error_message: string): bool =
        if is_match
        then
            eat_token()
            true
        else
            _diags.Error(error_message, _token.Span)
            false
            
            
    let eat_until (kinds: seq<TokenKind>): unit =
        let kind_set = Set.ofSeq kinds
        while not (_token.IsEof ||
                   kind_set.Contains(_token.Kind)) do
            eat_token()

    
    // To parse expressions we effectively use the recursive productions below.
    // The recursive productions are in contrast with the iterative productions,
    // defined in the grammar file, e.g.:
    // ```
    // expr
    // : prefix* primary infixop_and_rhs*
    // ;
    // ```
    //
    // The reason is, it's trivial to implement, if we can create partially initialized Ast nodes
    // and later mutate them. E.g., in pseudocode:
    // ```
    // let assign = Assign (id=..., expr=NULL)
    // ...
    // assign.expr <- prefix()
    // ```
    //
    // As we use immutable data structures to represent Ast,
    // it's not immediately obvious how to go about implementing the iterative productions.
    //
    // So we resort to the "recursive" productions,
    // as they make working with the immutable Ast data structures possible.
    //
    // expr
    //     // Assign
    //     : ID '=' expr
    //     // Two primary expressions starting with ID.
    //     // Included in `expr` so that parsing without lookahead is possible.
    //     | ID infixop_rhs?
    //     | ID actuals infixop_rhs?
    //     // Prefix ops
    //     | '!' expr
    //     | '-' expr
    //     | 'if' '(' expr ')' expr 'else' expr
    //     | 'while' '(' expr ')' expr
    //     | primary infixop_rhs?
    //     ;
    // 
    // primary
    //     : 'super' '.' ID actuals
    //     | 'new' ID actuals
    //     | '{' block? '}'
    //     | '(' expr ')'
    //     | 'null'
    //     | '(' ')'
    //     | INTEGER
    //     | STRING
    //     | BOOLEAN
    //     | 'this'
    //     ;
    // 
    // infixop_rhs
    //     : ('<=' | '<' | '>=' | '>' | '==' | '*' | '/' | '+' | '-') expr infixop_rhs?
    //     | 'match' cases infixop_rhs?
    //     | '.' ID actuals infixop_rhs?
    //     ;
    let rec expr (): ErrorOrOption<Node<Expr>> =
        let span_start = _token.Span.First
        
        let first_token = _token
        if try_eat_when _token.IsId
        then
            //
            // Assign
            //
            // 'ID = ' is an expression prefix.
            if try_eat TokenKind.Equal
            then
                // 'ID =' can be followed by:
                // - expressions starting with an expression prefix;
                // - a primary expression;
                // - a primary expression (expression suffix)+.
                // In other words, by any expression.
                let init_node_opt = required_expr ("An expression expected. When declaring a var," +
                                                   " '=' must be followed by an initializer expression")
                if init_node_opt.IsNone
                then
                    Error
                else
                    
                let id_node = Node.Of(ID first_token.Id, first_token.Span)
                let init_node = init_node_opt.Value

                let assign_value = Expr.Assign (id=id_node, expr=init_node)
                let assing_span = Span.Of(span_start, init_node.Span.Last)
                
                let assign_node = Node.Of(assign_value, assing_span)
                Ok (ValueSome assign_node)
            else
                
            //
            //  Two primary expressions starting with ID
            //
            // "ID actuals" is a primary expression.
            // A primary expression cannot be followed by another primary expression,
            // only by an infix op, i.e.: (infixop rhs)*
            if _token.Is(TokenKind.LParen)
            then
                let actual_nodes_opt = actuals()
                if actual_nodes_opt.IsNone
                then
                    Error
                else
                
                let actual_nodes = actual_nodes_opt.Value
                let id_node = Node.Of(ID first_token.Id, first_token.Span)
                
                let expr_dispatch_span = Span.Of(span_start, actual_nodes.Span.Last)
                let expr_dispatch_value = Expr.ImplicitThisDispatch (method_id=id_node, actuals=actual_nodes.Value)
                
                // The 'ID actuals' dispatch can be followed by any infix op except '='.
                match infixop_rhs (Node.Of(expr_dispatch_value, expr_dispatch_span)) with
                | ValueSome it -> Ok (ValueSome it)
                | ValueNone -> Error
            else

            // "ID" is a primary expression.
            // A primary expression cannot be followed by another primary expression,
            // only by an infix op, i.e.: (infixop rhs)*
            match infixop_rhs (Node.Of(Expr.Id (ID first_token.Id), first_token.Span)) with
            | ValueSome it -> Ok (ValueSome it)
            | ValueNone -> Error
        else
        
        //
        // Prefix ops.
        //     
        if try_eat TokenKind.Exclaim
        then
            let expr_node_opt = required_expr "An expression expected. '!' must be followed by an expression"
            if expr_node_opt.IsNone
            then
                Error
            else
                
            let expr_node = expr_node_opt.Value
            
            let expr_bool_neg_span = Span.Of(span_start, expr_node.Span.Last)
            let expr_bool_neg_value = Expr.BoolNegation expr_node 
            Ok (ValueSome (Node.Of(expr_bool_neg_value, expr_bool_neg_span)))
        else
            
        if try_eat TokenKind.Minus
        then
            let expr_node_opt = required_expr "An expression expected. '-' must be followed by an expression"
            if expr_node_opt.IsNone
            then
                Error
            else
                
            let expr_node = expr_node_opt.Value
            
            let expr_unary_minus_span = Span.Of(span_start, expr_node.Span.Last)
            let expr_unary_minus_value = Expr.UnaryMinus expr_node
            Ok (ValueSome (Node.Of(expr_unary_minus_value, expr_unary_minus_span)))
        else
            
        if try_eat TokenKind.KwIf
        then
            if not (eat TokenKind.LParen "'(' expected. The condition must be enclosed in '(' and ')'")
            then
                Error
            else

            let condition_opt = required_expr "An expression expected. 'if (' must be followed by a boolean expression"
            if condition_opt.IsNone
            then
                Error
            else
            
            if not (eat TokenKind.RParen "')' expected. The conditional expression must be enclosed in '(' and ')'")
            then
                Error
            else
                
            let then_branch_opt = required_expr "An expression expected. A conditional expression must have a then branch"
            if then_branch_opt.IsNone
            then
                Error
            else
                
            if not (eat TokenKind.KwElse "'else' expected. A conditional expression must have an else branch")
            then
                Error
            else
                
            let else_branch_opt = required_expr "An expression required. A conditional expression must have an else branch"
            if else_branch_opt.IsNone
            then
                Error
            else
                
            let expr_if_span = Span.Of(span_start, else_branch_opt.Value.Span.Last)
            let expr_if_value = Expr.If (condition=condition_opt.Value,
                                         then_branch=then_branch_opt.Value,
                                         else_branch=else_branch_opt.Value)
            Ok (ValueSome (Node.Of(expr_if_value, expr_if_span)))
        else
        
        if try_eat TokenKind.KwWhile
        then
            if not (eat TokenKind.LParen "'(' expected. The condition must be enclosed in '(' and ')'")
            then
                Error
            else

            let condition_opt = required_expr "An expression expected. 'while (' must be followed by a boolean expression"
            if condition_opt.IsNone
            then
                Error
            else
            
            if not (eat TokenKind.RParen "')' expected. The conditional expression must be enclosed in '(' and ')'")
            then
                Error
            else
                
            let body_opt = required_expr "An expression expected. A while loop must have a body"
            if body_opt.IsNone
            then
                Error
            else
                
            let expr_while_span = Span.Of(span_start, body_opt.Value.Span.Last)
            let expr_while_value = Expr.While (condition=condition_opt.Value,
                                               body=body_opt.Value)
            Ok (ValueSome (Node.Of(expr_while_value, expr_while_span)))
        else

        //
        // No prefix op specified
        //
        // primary infixop*
        match primary() with
        | Error ->
            Error
        | Ok ValueNone ->
            // Actually, the syntax doesn't contain any productions with an optional expression.
            // So, we know, we encountered a syntax error here.
            // But the caller can report a more specific error message, so we return control to the caller.
            Ok ValueNone
        | Ok (ValueSome primary_node) ->            
            match infixop_rhs(primary_node) with
            | ValueSome it -> Ok (ValueSome it)
            | ValueNone -> Error
        
    
    // A primary expression cannot be followed by another primary expression,
    // only by an infix op, i.e.: (expression suffix)*    
    and primary (): ErrorOrOption<Node<Expr>> =
        let span_start = _token.Span.First
        let first_token = _token

        // ('super' '.')? ID actuals
        if try_eat TokenKind.KwSuper
        then
            if not (eat TokenKind.Dot "'.' expected. 'super' can only be used in a super dispatch expression")
            then
                Error
            else
                
            let token_id = _token
            if not (eat_when _token.IsId ("A method name expected. Method name must be an identifier" +
                                          _token.KwDescription))
            then
                Error
            else
                
            let actual_nodes_opt = actuals()
            if actual_nodes_opt.IsNone
            then
                Error
            else
            
            let actual_nodes = actual_nodes_opt.Value
            let id_node = Node.Of(ID token_id.Id, token_id.Span)
            
            let expr_super_dispatch_span = Span.Of(span_start, actual_nodes.Span.Last)
            let expr_super_dispatch_value = Expr.SuperDispatch (method_id=id_node, actuals=actual_nodes.Value)
            
            Ok (ValueSome (Node.Of(expr_super_dispatch_value, expr_super_dispatch_span)))
        else

        // 'new' ID actuals
        if try_eat TokenKind.KwNew
        then
            let token_id = _token 
            if not (eat_when _token.IsId ("A type name expected. Type name must be an identifier" +
                                          _token.KwDescription))
            then
                Error
            else
                
            let actual_nodes_opt = actuals()
            if actual_nodes_opt.IsNone
            then
                Error
            else
            
            let actual_nodes = actual_nodes_opt.Value
            let type_name_node = Node.Of(TYPE_NAME token_id.Id, token_id.Span)
            
            let expr_new_span = Span.Of(span_start, actual_nodes.Span.Last)
            let expr_new_value = Expr.New (type_name=type_name_node, actuals=actual_nodes.Value)
            Ok (ValueSome (Node.Of(expr_new_value, expr_new_span)))
        else
            
        // '{' block '}'
        if _token.Is(TokenKind.LBrace)
        then
            let block_node_opt = braced_block()
            if block_node_opt.IsNone
            then
                Error
            else
                
            let block_node = block_node_opt.Value
            Ok (ValueSome (Node.Of(Expr.BracedBlock block_node, block_node.Span)))
        else
            
        // '(' expr ')'
        // '(' ')'
        if try_eat TokenKind.LParen
        then
            let token_rparen = _token
            if try_eat TokenKind.RParen
            then
                let expr_unit_span = Span.Of(span_start, token_rparen.Span.Last)
                Ok (ValueSome (Node.Of(Expr.Unit, expr_unit_span)))
            else
                
            let expr_node_opt = required_expr "A parenthesized expression expected"
            if expr_node_opt.IsNone
            then
                Error
            else

            let token_rparen = _token                
            if not (eat TokenKind.RParen "')' expected. '(expression' must be followed by ')'")
            then
                Error
            else
                
            let expr_node = expr_node_opt.Value
            
            let expr_parens_value = Expr.ParensExpr expr_node
            let expr_parens_span = Span.Of(span_start, token_rparen.Span.Last)
            
            Ok (ValueSome (Node.Of(expr_parens_value, expr_parens_span)))
        else
            
        // 'null'
        if try_eat TokenKind.KwNull
        then
            Ok (ValueSome (Node.Of(Expr.Null, first_token.Span)))
        else
            
        if try_eat TokenKind.KwThis
        then
            Ok (ValueSome (Node.Of(Expr.This, first_token.Span)))
        else

        // INTEGER
        if try_eat_when _token.IsInt
        then
            Ok (ValueSome (Node.Of(Expr.Int (INT first_token.Int), first_token.Span)))
        else

        // STRING
        if try_eat_when (_token.IsString || _token.IsQqqString)
        then
            Ok (ValueSome (Node.Of(Expr.Str (STRING (value=first_token.String,
                                                     is_qqq=first_token.IsQqqString)),
                                   first_token.Span)))
        else

        // BOOLEAN
        if try_eat TokenKind.KwTrue || try_eat TokenKind.KwFalse
        then
            let expr_bool_value =
                Expr.Bool (match first_token.Kind with
                           | TokenKind.KwTrue -> BOOL.True
                           | TokenKind.KwFalse -> BOOL.False
                           | _ -> invalidOp "Unreachable")
            
            Ok (ValueSome (Node.Of(expr_bool_value, first_token.Span)))
        else
        
        // Actually, the syntax doesn't contain any productions with an optional expression.
        // So, we know, we encountered a syntax error here.
        // But the caller can report a more specific error message, so we return control to the caller.
        Ok ValueNone
        
        
    // infixop_rhs
    //     : ('<=' | '<' | '>=' | '>' | '==' | '*' | '/' | '+' | '-') expr infixop_rhs?
    //     | 'match' cases infixop_rhs?
    //     | '.' ID actuals infixop_rhs?
    //     ;
    and infixop_rhs (lhs: Node<Expr>): Node<Expr> voption =
        let span_start = _token.Span.First
        let first_token = _token
        
        if try_eat_when (_token.Is(TokenKind.LessEqual) || _token.Is(TokenKind.Less) ||
                         _token.Is(TokenKind.GreaterEqual) || _token.Is(TokenKind.Greater) ||
                         _token.Is(TokenKind.EqualEqual) ||
                         _token.Is(TokenKind.Star) || _token.Is(TokenKind.Slash) ||
                         _token.Is(TokenKind.Plus) || _token.Is(TokenKind.Minus))
        then
            let rhs_opt = required_expr (sprintf "An expression expected. '%s' must be followed by an expression"
                                                 first_token.InfixOpSpelling)
            if rhs_opt.IsNone
            then
                ValueNone
            else

            let rhs_opt = infixop_rhs (*lhs=*)rhs_opt.Value
            if rhs_opt.IsNone
            then
                ValueNone
            else

            let rhs = rhs_opt.Value
            let expr_span = Span.Of(span_start, rhs.Span.Last)
            
            let expr_value =                
                match first_token.Kind with
                | TokenKind.LessEqual    -> Expr.LtEq (left=lhs, right=rhs)
                | TokenKind.Less         -> Expr.Lt (left=rhs, right=rhs)
                | TokenKind.GreaterEqual -> Expr.GtEq (left=rhs, right=rhs)
                | TokenKind.Greater      -> Expr.Gt (left=rhs, right=rhs)
                | TokenKind.EqualEqual   -> Expr.EqEq (left=rhs, right=rhs)
                | TokenKind.Star         -> Expr.Mul (left=rhs, right=rhs)
                | TokenKind.Slash        -> Expr.Div (left=rhs, right=rhs)
                | TokenKind.Plus         -> Expr.Sum (left=rhs, right=rhs)
                | TokenKind.Minus        -> Expr.Sub (left=rhs, right=rhs)
                | _                      -> invalidOp "Unreachable"
                    
            ValueSome (Node.Of(expr_value, expr_span))
        else

        // 'match' cases infixop_rhs?
        if try_eat TokenKind.KwMatch
        then
            let case_nodes_opt = cases()
            if case_nodes_opt.IsNone
            then
                ValueNone
            else
            
            let case_nodes = case_nodes_opt.Value
            if case_nodes.Value.Length = 0
            then
                _diags.Error("A match expression must contain at least one case",
                             Span.Of(span_start, _token.Span.First))
                ValueNone
            else

            let expr_match_span = Span.Of(span_start, case_nodes.Span.Last)
            let expr_match_value = Expr.Match (expr=lhs,
                                               cases_hd=case_nodes.Value.[0],
                                               cases_tl=case_nodes.Value.[1..])
            
            let expr_match_node = Node.Of(expr_match_value, expr_match_span)
            infixop_rhs (*lhs=*)expr_match_node
        else
            
        // '.' ID actuals infixop_rhs?
        if try_eat TokenKind.Dot
        then
            let token_id = _token
            if not (eat_when _token.IsId ("An identifier expected. '.' must be followed by a method name" +
                                          _token.KwDescription))
            then
                ValueNone
            else
                
            let actual_nodes_opt = actuals()
            if actual_nodes_opt.IsNone
            then
                ValueNone
            else
            
            let actual_nodes = actual_nodes_opt.Value
            
            let expr_dispatch_span = Span.Of(span_start, actual_nodes.Span.Last)
            let expr_dispatch_value = Expr.Dispatch (receiver=lhs,
                                                     method_id=Node.Of(ID token_id.Id, token_id.Span),
                                                     actuals=actual_nodes.Value)
           
            let expr_dispatch_node = Node.Of(expr_dispatch_value, expr_dispatch_span) 
            infixop_rhs (*lhs=*)expr_dispatch_node
        else
        
        ValueSome lhs

            
    // cases
    //     : '{' ('case' casepattern '=>' caseblock)+ '}'
    //     ;
    and cases (): Node<Node<Case>[]> voption =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.LBrace
                    "'{' expected. Cases must be enclosed in '{' and '}'")
        then
            ValueNone
        else
            
        let case_nodes = List<Node<Case>>()

        let recover (): unit = 
            // Recover from a syntactic error by eating tokens
            // until we find the beginning of another feature
            eat_until [TokenKind.KwCase; TokenKind.RBrace]
            
        let is_cases_end (): bool = _token.IsEof || _token.Is(TokenKind.RBrace)

        let mutable is_case_expected = not (is_cases_end())
        while is_case_expected do
            match case() with
            | ValueSome case_node ->
                case_nodes.Add(case_node)
            | ValueNone ->
                // We didn't manage to parse a feature.
                recover ()

            is_case_expected <- not (is_cases_end())
            
        if (_token.IsEof)
        then
            _diags.Error("'}' expected. Cases must be enclosed in '{' and '}'", _token.Span)
            ValueNone
        else
            
        // Eat '}'
        let token_rbrace = _token
        eat_token()
        
        let case_nodes_span = Span.Of(span_start, token_rbrace.Span.Last)
        
        ValueSome (Node.Of(case_nodes.ToArray(), case_nodes_span))
    
    
    and case (): Node<Case> voption =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.KwCase
                    "'case' expected. A case in 'match' expression must start with the 'case' keyword")
        then
            ValueNone
        else
        
        let pattern_node_opt = casepattern()
        if pattern_node_opt.IsNone
        then
            ValueNone
        else
            
        if not (eat TokenKind.EqualGreater
                    "'=>' expected. A case's pattern and block must be delimited by '=>'")
        then
            ValueNone
        else
            
        let block_node_opt = caseblock()
        if (block_node_opt.IsNone)
        then
            ValueNone
        else
            
        let block_node = block_node_opt.Value
        
        let case_span = Span.Of(span_start, block_node.Span.Last)
        let case_value: Case = { Pattern = pattern_node_opt.Value; Block = block_node }
        
        ValueSome (Node.Of(case_value, case_span))
    
    
    // casepattern
    //     : ID ':' ID
    //     | 'null'
    //     ;
    and casepattern (): Node<Pattern> voption =
        let span_start = _token.Span.First
        let first_token = _token
        
        if try_eat_when _token.IsId
        then
            if not (eat TokenKind.Colon
                        "':' expected. In a pattern, an identifier must be followed by ':'")
            then
                ValueNone
            else
                
            let token_type = _token
            if not (eat_when _token.IsId
                             ("The pattern's type name expected. The type name must be an identifier" +
                              _token.KwDescription))
            then
                ValueNone
            else
            
            let pattern_span = Span.Of(span_start, token_type.Span.Last)
            let pattern_value = Pattern.IdType (id=Node.Of(ID first_token.Id, first_token.Span),
                                                pattern_type=Node.Of(TYPE_NAME token_type.Id, token_type.Span))
            
            ValueSome (Node.Of(pattern_value, pattern_span))
        else
            
        if try_eat TokenKind.KwNull
        then
            ValueSome (Node.Of(Pattern.Null, first_token.Span))
        else
            
        _diags.Error("An identifier or 'null' expected. A pattern must start from an identifier or 'null'", _token.Span)
        ValueNone
    

    // caseblock
    //     : block
    //     | '{' block? '}'
    //     ;
    and caseblock (): Node<CaseBlock> voption =
        if _token.Is(TokenKind.LBrace)
        then
            let braced_block_opt = braced_block()
            if braced_block_opt.IsNone
            then
                ValueNone
            else
                
            let braced_block = braced_block_opt.Value
            ValueSome (Node.Of(CaseBlock.Braced braced_block, braced_block.Span))
        else
        
        let span_start = _token.Span.First
        
        let block_info_result = block_info((*terminators=*)[TokenKind.KwCase; TokenKind.RBrace])
        if block_info_result.IsError
        then
            ValueNone
        else
        
        if block_info_result.IsNone
        then
            _diags.Error("A block expected. 'pattern =>' must be followed by a non-empty block",
                         _token.Span)
            ValueNone
        else
            
        let block_info_node = block_info_result.Value
            
        let caseblock_span = Span.Of(span_start, block_info_node.Span.Last)
        let caseblock_value = CaseBlock.Implicit block_info_node.Value
        ValueSome (Node.Of(caseblock_value, caseblock_span))
    
    
    and required_expr (expr_required_error_message: string): Node<Expr> voption =
        let expr_node_result = expr()
        if expr_node_result.IsError
        then
            ValueNone
        else
            
        if expr_node_result.IsNone
        then
            _diags.Error(expr_required_error_message, _token.Span)
            ValueNone
        else
            ValueSome expr_node_result.Value


    and stmt (): Node<Stmt> voption =
        let span_start = _token.Span.First

        if try_eat TokenKind.KwVar
        then
            
            let token_id = _token
            if not (eat_when _token.IsId
                             ("A var name expected. Var name must be an identifier" +
                              _token.KwDescription))
            then
                ValueNone
            else
                
            if not (eat TokenKind.Colon
                        "':' expected. A var's name and type must be delimited by ':'")
            then
                ValueNone
            else
                
            let token_type = _token
            if not (eat_when _token.IsId
                             ("The var's type name expected. The type name must be an identifier" +
                              _token.KwDescription))
            then
                ValueNone
            else
            
            if not (eat TokenKind.Equal
                        "'=' expected. A var's type and initializer must be delimited by '='")
            then
                ValueNone
            else
                
            let expr_node_opt = expr()
            if expr_node_opt.IsNone
            then
                ValueNone
            else
            
            let expr_node = expr_node_opt.Value
            
            let vardecl_span = Span.Of(span_start, expr_node.Span.Last)
            let vardecl_value: VarDeclInfo =
                { ID = Node.Of(ID token_id.Id, token_id.Span)
                  TYPE_NAME = Node.Of(TYPE_NAME token_type.Id, token_type.Span)
                  Expr = expr_node }
            
            ValueSome (Node.Of(Stmt.VarDecl vardecl_value, vardecl_span))
        else
            
        let expr_node_opt = expr()
        if expr_node_opt.IsNone
        then
            ValueNone
        else
        
        ValueSome (expr_node_opt.Value.Map(fun it -> Stmt.Expr it))


    and block_info (terminators: seq<TokenKind>): ErrorOrOption<Node<BlockInfo>> =
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
        
        let span_start = _token.Span.First
        let stmt_nodes = this.DelimitedList(
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
            let block_info_span = Span.Of(span_start, last_stmt.Span.Last)
            let block_info_value: BlockInfo = { Stmts = stmts
                                                Expr = Node.Of(expr, last_stmt.Span) }
            
            Ok (ValueSome (Node.Of(block_info_value, block_info_span)))

    
    and braced_block (): Node<BlockInfo voption> voption =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.LBrace
                    "'{' expected. A braced block must start with '{'; an empty one is denoted by '{}'")
        then
            ValueNone
        else

        let block_info_result = block_info((*terminators=*)[TokenKind.RBrace])
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
        let token_rbrace = _token
        eat_token()
        
        let block_span = Span.Of(span_start, token_rbrace.Span.Last)
        let block_value = block_info_result.Option |> ValueOption.map (fun it -> it.Value)
        
        ValueSome (Node.Of(block_value, block_span))


    and varformal (): Node<VarFormal> voption =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.KwVar
                    "'var' expected. A varformal declaration must start with 'var'")
        then
            ValueNone
        else

        let token_id = _token
        if not (eat_when _token.IsId
                         ("A varformal name expected. Varformal name must be an identifier" +
                          _token.KwDescription))
        then
            ValueNone
        else
            
        if not (eat TokenKind.Colon
                    "':' expected. A varformal's name and type must be delimited by ':'")
        then
            ValueNone
        else
            
        let token_type = _token
        if not (eat_when _token.IsId
                         ("The varformal's type name expected. The type name must be an identifier" +
                          _token.KwDescription))
        then
            ValueNone
        else

        let id_node = Node.Of(ID token_id.Id, token_id.Span)
        let type_node = Node.Of(TYPE_NAME token_type.Id, token_type.Span)
        
        let varformal_span = Span.Of(span_start, type_node.Span.Last)
        let varformal_value =
            { VarFormal.ID = id_node
              TYPE_NAME = type_node }
            
        ValueSome (Node.Of(varformal_value, varformal_span))
    
    
    and varformals (): Node<Node<VarFormal>[]> voption =
        let recover (): unit =
            // Recover from a syntactic error by eating tokens
            // until we find 'var' -- the start of another varformal.
            eat_until [TokenKind.RParen; TokenKind.KwVar]
            
        let is_varformals_end (): bool = _token.IsEof || _token.Is(TokenKind.RParen)

        this.EnclosedDelimitedList(
            list_start=TokenKind.LParen,
            list_start_error_message="'(' expected. A varformals list must start with '('; an empty one is denoted by '()'",
            list_end_error_message="')' expected. A varformals list must end with ')'",
            element=varformal,
            delimiter=TokenKind.Comma,
            delimiter_error_message="',' expected. Elements of a varformal list must be delimited by ','",
            recover=recover,
            is_list_end=is_varformals_end)
        
        
    and actual (): Node<Expr> voption =
        required_expr "An expression expected. Actuals must be an expression"
    
    
    and actuals (): Node<Node<Expr>[]> voption =
        let recover (): unit =
            // Recover from a syntactic error by eating tokens
            // until we find ','
            // Then eat it -- hopefully this places us at the start of another formal.
            eat_until [TokenKind.RParen; TokenKind.Comma]
            if _token.Is(TokenKind.Comma)
            then
                eat_token()
                
        let is_actuals_end (): bool = _token.IsEof || _token.Is(TokenKind.RParen)

        this.EnclosedDelimitedList(
            list_start=TokenKind.LParen,
            list_start_error_message="'(' expected. An actuals list must start with '('; an empty one is denoted by '()'",
            list_end_error_message="')' expected. An actuals list must end with ')'",
            element=actual,
            delimiter=TokenKind.Comma,
            delimiter_error_message="',' expected. Elements of an actuals list must be delimited by ','",
            recover=recover,
            is_list_end=is_actuals_end)
    
    
    and extends (): ErrorOrOption<Node<Extends>> =
        let span_start = _token.Span.First
        
        if not (try_eat TokenKind.KwExtends)
        then
            Ok ValueNone
        else

        let token_id = _token
        if not (eat_when _token.IsId
                         ("A parent class name expected. Parent class name must be an identifier" +
                          _token.KwDescription))
        then
            Error
        else
            
        let actual_nodes_opt = actuals()
        if ValueOption.isNone actual_nodes_opt
        then
            Error
        else
        
        let actual_nodes = actual_nodes_opt.Value
        let extends_span = Span.Of(span_start, actual_nodes.Span.Last)
        let extends_info: ExtendsInfo =
            { PARENT_NAME = Node.Of(TYPE_NAME token_id.Id, token_id.Span)
              Actuals = actual_nodes.Value }
              
        Ok (ValueSome (Node.Of(Extends.Info extends_info, extends_span)))
        
        
    and formal (): Node<Formal> voption =
        let span_start = _token.Span.First
        
        let token_id = _token
        if not (eat_when _token.IsId
                         ("A formal name expected. Formal name must be an identifier" +
                          _token.KwDescription))
        then
            ValueNone
        else
            
        if not (eat TokenKind.Colon
                    "':' expected. A formal's name and type must be delimited by ':'")
        then
            ValueNone
        else
            
        let token_type = _token
        if not (eat_when _token.IsId
                         ("The formal's type name expected. The type name must be an identifier" +
                          _token.KwDescription))
        then
            ValueNone
        else
        
        let id_node = Node.Of(ID token_id.Id, token_id.Span)
        let type_node = Node.Of(TYPE_NAME token_type.Id, token_type.Span)
        
        let formal_span = Span.Of(span_start, type_node.Span.Last)
        let formal_value =
            { Formal.ID = id_node
              TYPE_NAME = type_node }
        
        ValueSome (Node.Of(formal_value, formal_span))
    
    
    and formals (): Node<Node<Formal>[]> voption =
        let recover (): unit =
            // Recover from a syntactic error by eating tokens
            // until we find ','
            // Then eat it -- hopefully this places us at the start of another formal.
            eat_until [TokenKind.RParen; TokenKind.Comma]
            if _token.Is(TokenKind.Comma)
            then
                eat_token()
                
        let is_formals_end (): bool = _token.IsEof || _token.Is(TokenKind.RParen)

        this.EnclosedDelimitedList(
            list_start=TokenKind.LParen,
            list_start_error_message="'(' expected. A formals list must start with '('; an empty one is denoted by '()'",
            list_end_error_message="')' expected. A formals list must end with ')'",
            element=formal,
            delimiter=TokenKind.Comma,
            delimiter_error_message="',' expected. Elements of a formals list must be delimited by ','",
            recover=recover,
            is_list_end=is_formals_end)
    
    
    and method (span_start: uint32, is_override: bool) : Node<Feature> voption =
        let token_id = _token
        if not (eat_when _token.IsId
                         ("A method name expected. Method name must be an identifier" +
                          _token.KwDescription))
        then
            ValueNone
        else
            
        let formal_nodes_opt = formals()
        if formal_nodes_opt.IsNone
        then
            ValueNone
        else
            
        if not (eat TokenKind.Colon
                    "':' expected. A method's formals and return type must be delimited by ':'")
        then
            ValueNone
        else

        let token_type = _token
        if not (eat_when _token.IsId
                         ("A return type name expected. Type name must be an identifier" +
                          _token.KwDescription))
        then
            ValueNone
        else
        
        if not (eat TokenKind.Equal
                    "'=' expected. A method's return type and body must be delimited by '='")
        then
            ValueNone
        else
            
        let expr_node_opt = expr()
        if expr_node_opt.IsNone
        then
            ValueNone
        else
        
        let expr_node = expr_node_opt.Value

        let method_info_span = Span.Of(span_start, expr_node.Span.Last)
        let method_info_value =
            { Override = is_override
              ID = Node.Of(ID token_id.Id, token_id.Span)
              Formals = formal_nodes_opt.Value.Value
              TYPE_NAME =  Node.Of(TYPE_NAME token_type.Id, token_type.Span)
              MethodBody = expr_node.Map(fun it -> MethodBody.Expr it) }
        
        ValueSome (Node.Of(Feature.Method method_info_value, method_info_span))
        
        
    and attribute (): Node<Feature> voption =
        let span_start = _token.Span.First
        
        if not (try_eat TokenKind.KwVar)
        then
            ValueNone
        else
            
        let token_id = _token
        if not (eat_when _token.IsId
                         ("An attribute name expected. Attribute name must be an identifier" +
                          _token.KwDescription))
        then
            ValueNone
        else
            
        if not (eat TokenKind.Colon
                    "':' expected. An attribute's name and type must be delimited by ':'")
        then
            ValueNone
        else
            
        let token_type = _token
        if not (eat_when _token.IsId
                         ("The attribute's type name expected. The type name must be an identifier" +
                          _token.KwDescription))
        then
            ValueNone
        else
        
        if not (eat TokenKind.Equal
                    "'=' expected. An attribute's type and initializer must be delimited by '='")
        then
            ValueNone
        else
            
        let expr_node_opt = expr()
        if expr_node_opt.IsNone
        then
            ValueNone
        else
        
        let expr_node = expr_node_opt.Value
        
        let attribute_span = Span.Of(span_start, expr_node.Span.Last)
        let attribute_value: AttrInfo =
            { ID = Node.Of(ID token_id.Id, token_id.Span)
              TYPE_NAME = Node.Of(TYPE_NAME token_type.Id, token_type.Span)
              AttrBody = expr_node.Map(fun it -> AttrBody.Expr it) }
        
        ValueSome (Node.Of(Feature.Attr attribute_value, attribute_span))
        
        
    and feature (): Node<Feature> voption =
        let span_start = _token.Span.First
        
        if try_eat TokenKind.KwOverride
        then
            if not (eat TokenKind.KwDef
                        "'def' expected. An overriden method must start with 'override def'")
            then
                ValueNone
            else
                
            method(span_start, (*is_override=*)true)
        else

        if try_eat TokenKind.KwDef
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


    and classbody (span_start: uint): Node<Node<Feature>[]> voption =
        // The caller must eat '{' or emit a diagnostic if it's empty
        
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
                
                if not (eat TokenKind.Semi
                            "';' expected. Features must be terminated by ';'")
                then
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
        let token_rbrace = _token
        eat_token()
        
        let classbody_span = Span.Of(span_start, token_rbrace.Span.Last)
        
        ValueSome (Node.Of(feature_nodes.ToArray(), classbody_span))
        
    
    and class_decl (): Node<ClassDecl> voption =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.KwClass
                    "'class' expected. Only classes can appear at the top level")
        then
            ValueNone
        else
            
        let token_id = _token
        if not (eat_when _token.IsId
                         ("A class name expected. Class name must be an identifier" +
                          _token.KwDescription))
        then
            ValueNone
        else
           
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
            
        if not (eat TokenKind.LBrace
                    ((if extends_node_result.IsNone then "'extends' or " else "") +
                     "'{' expected. A class body must start with '{'"))
        then
            ValueNone
        else
            
        let feature_nodes_opt = classbody(span_start)
        if feature_nodes_opt.IsNone
        then
            // The callee already emitted relevant diags.
            // We aren't going to add anything else.
            ValueNone
        else

        // classbody() eats '}'        
            
        let feature_nodes = feature_nodes_opt.Value
        let name_node = Node.Of(TYPE_NAME token_id.Id, token_id.Span)
        
        let class_decl_span = Span.Of(span_start, feature_nodes.Span.Last)
        let class_decl_value =
            { ClassDecl.NAME = name_node
              VarFormals = varformals_node_opt.Value.Value
              Extends = extends_node_result.Option
              ClassBody = feature_nodes.Value }

        ValueSome (Node.Of(class_decl_value, class_decl_span))
    
    
    and class_decls (): Node<Node<ClassDecl>[]> =
        let span_start = _token.Span.First
        
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

        let class_decls_span = Span.Of(span_start, _token.Span.Last)
        Node.Of(class_decl_nodes.ToArray(), class_decls_span)
        
        
    and ast (): Ast =
        let span_start = _token.Span.First
        
        let class_decl_nodes = class_decls()
        let span = Span.Of((*first=*)span_start,
                           (*last=*)class_decl_nodes.Span.Last)
        
        { Program = Node.Of({ Program.ClassDecls = class_decl_nodes.Value }, span) }

    
    member this.DelimitedList<'T>(element: unit -> Node<'T> voption,
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

                if try_eat delimiter
                then
                    is_element_expected <- true
                else

                // We want to diagnose a missing delimiter at the last found element's end.
                // In contrast to diagnosing it a the beginning of a token that we expected to be the delimiter.
                // As there can be whitespace/line-breaks before this token,
                // diagnosing a missing delimiter at its beginning is confusing.
                let last_element_span_end = element_nodes.Last().Span.Last
                _diags.Error(delimiter_error_message, Span.Of(last_element_span_end, last_element_span_end + 1u))
                
                // We didn't find `delimiter` where expected.
                recover()
                is_element_expected <- not (is_list_end())
            | ValueNone ->
                // We didn't manage to parse an element
                recover()
                is_element_expected <- not (is_list_end())
            
        element_nodes.ToArray()


    member this.EnclosedDelimitedList<'T>(list_start: TokenKind,
                                          list_start_error_message: string,
                                          list_end_error_message: string,
                                          element: unit -> Node<'T> voption,
                                          delimiter: TokenKind,
                                          delimiter_error_message: string,
                                          recover: unit -> unit,
                                          is_list_end: unit -> bool)
                                          : Node<Node<'T>[]> voption =
        
        let span_start = _token.Span.First
        
        if not (eat list_start
                    list_start_error_message)
        then
            ValueNone
        else

        let element_nodes = this.DelimitedList(element, delimiter, delimiter_error_message, recover, is_list_end)

        if _token.IsEof
        then
            _diags.Error(list_end_error_message, _token.Span)
            ValueNone
        else
            
        // Eat the token that closes the list
        let span_end = _token.Span.Last
        eat_token()
        
        let list_span = Span.Of(span_start, span_end)
        
        ValueSome (Node.Of(element_nodes, list_span))


    member _.Parse() : Ast =
        ast()
