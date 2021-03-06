namespace rec LibCool.ParserParts


open System.Collections.Generic
open System.Linq
open LibCool.SharedParts
open LibCool.SourceParts
open LibCool.AstParts
open LibCool.DiagnosticParts
open LibCool.ParserParts.TokenExtensions


type Parser private (_tokens: Token[], _diags: DiagnosticBag) as this =

    
    let mutable _offset = 0
    let mutable _token = _tokens.[_offset]
    let mutable _prev_token_span_last = 0u

        
    let eat_token (): unit =
        if _offset + 1 >= _tokens.Length
        then
            invalidOp (sprintf "_offset [%d] + 1 is >= _tokens.Length [%d]" _offset _tokens.Length)
        
        _prev_token_span_last <- _token.Span.Last
        _offset <- _offset + 1
        _token <- _tokens.[_offset]
            
            
    let eat (kind: TokenKind)
            (error_message: string): bool =
        if _token.Is kind
        then
            eat_token()
            true
        else
            _diags.Error(error_message, Span.Of(_prev_token_span_last, _token.Span.First))
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
            _diags.Error(error_message, Span.Of(_prev_token_span_last, _token.Span.First))
            false
            
            
    let eat_until (kinds: seq<TokenKind>): unit =
        let kind_set = Set.ofSeq kinds
        while not (_token.IsEof ||
                   kind_set.Contains(_token.Kind)) do
            eat_token()

    
    // To parse expressions we effectively use the productions below.
    // They are somewhat different from the grammar's productions, e.g.:
    // ```
    // expr
    // : prefix* primary infixop_rhs*
    // ;
    //
    // expr
    //     // Assign
    //     : ID '=' expr
    //     // Two primary expressions starting with ID.
    //     // Included in `expr` to avoid having to call `primary` and
    //     // pass the ID token as an argument, based on whether the next token is '=' or not. 
    //     | ID infixop_rhs?
    //     | ID actuals infixop_rhs?
    //     // Prefix ops
    //     | '!' expr infixop_rhs?
    //     | '-' expr infixop_rhs?
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
    //     : ('<=' | '<' | '>=' | '>' | '==' | '*' | '/' | '+' | '-') expr infixop_rhs*
    //     | 'match' cases infixop_rhs*
    //     | '.' ID actuals infixop_rhs*
    //     ;
    // ```
    let rec expr (prec_threshold: sbyte): Res<AstNode<ExprSyntax> voption> =
        let res_of (res: Res<AstNode<ExprSyntax>>): Res<AstNode<ExprSyntax> voption> =
            match res with
            | Error    -> Error
            | Ok value -> Ok (ValueSome value)
        
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
                // 'ID =' can be followed by any expression.
                // Everything to the right of '=' is treated as a "standalone" expression.
                // I.e., we pass prec_threshold=Prec.Empty
                let init_node_res = required_expr
                                        (*prec_threshold=*)Prec.Empty
                                        "An expression expected. '=' must be followed by an expression"
                if init_node_res.IsError
                then
                    Error
                else
                    
                let id_node = AstNode.Of(ID first_token.Id, first_token.Span)
                let init_node = init_node_res.Value

                let assign_syntax = ExprSyntax.Assign (id=id_node, expr=init_node)
                let assing_span = Span.Of(span_start, init_node.Span.Last)
                
                let assign_node = AstNode.Of(assign_syntax, assing_span)
                Ok (ValueSome assign_node)
            else
                
            //
            //  Two primary expressions starting with ID
            //
            // "ID actuals" is a primary expression.
            // A primary expression cannot be followed by another primary expression,
            // only by an infix op, i.e.: (infixop_rhs)*
            if _token.Is(TokenKind.LParen)
            then
                let actual_nodes_res = actuals ()
                if actual_nodes_res.IsError
                then
                    Error
                else
                
                let actual_nodes = actual_nodes_res.Value
                let id_node = AstNode.Of(ID first_token.Id, first_token.Span)
                
                let expr_dispatch_span = Span.Of(span_start, actual_nodes.Span.Last)
                let expr_dispatch_syntax = ExprSyntax.ImplicitThisDispatch (method_id=id_node, actuals=actual_nodes.Syntax)
                
                // The 'ID actuals' dispatch can be followed by any infix op except '='.
                res_of (infixop_rhs prec_threshold
                                    (AstNode.Of(expr_dispatch_syntax, expr_dispatch_span)))
            else

            // "ID" is a primary expression.
            // A primary expression cannot be followed by another primary expression,
            // only by an infix op, i.e.: (infixop_rhs)*
            res_of (infixop_rhs prec_threshold
                                (AstNode.Of(ExprSyntax.Id (ID first_token.Id), first_token.Span)))
        else
        
        //
        // Prefix ops (Atom modifiers).
        //     
        if try_eat TokenKind.Exclaim
        then
            // Exclaim applies boolean negation to the following:
            // - atom (e.g.: ID) or
            // - dispatch op (e.g.: receiver.method(...))
            let expr_node_res = required_expr ((*prec_threshold=*)Prec.OfExclaim + 1y)
                                              "An expression expected. '!' must be followed by an expression"
            if expr_node_res.IsError
            then
                Error
            else
                
            let expr_node = expr_node_res.Value
            
            let expr_bool_neg_span = Span.Of(span_start, expr_node.Span.Last)
            let expr_bool_neg_syntax = ExprSyntax.BoolNegation expr_node 
            
            // Boolean negation applied to an atom or subexpression
            // can then be followed by an infix op, i.e.: (infixop_rhs)*
            res_of (infixop_rhs prec_threshold
                                (AstNode.Of(expr_bool_neg_syntax, expr_bool_neg_span)))
        else
            
        if try_eat TokenKind.Minus
        then
            // Here, minus applies arithmetical negation to the following:
            // - atom (e.g.: ID) or
            // - dispatch op (e.g.: receiver.method(...))
            let expr_node_res = required_expr ((*prec_threshold=*)Prec.OfUnaryMinus + 1y)
                                              "An expression expected. '-' must be followed by an expression"
            if expr_node_res.IsError
            then
                Error
            else
                
            let expr_node = expr_node_res.Value
            
            let expr_unary_minus_span = Span.Of(span_start, expr_node.Span.Last)
            let expr_unary_minus_syntax = ExprSyntax.UnaryMinus expr_node

            // Arithmetical negation applied to an atom or subexpression
            // can then be followed by an infix op, i.e.: (infixop_rhs)*
            res_of (infixop_rhs prec_threshold
                                (AstNode.Of(expr_unary_minus_syntax, expr_unary_minus_span)))
        else
            
        if try_eat TokenKind.KwIf
        then
            if not (eat TokenKind.LParen "'(' expected. The condition must be enclosed in '(' and ')'")
            then
                Error
            else

            let condition_res = required_expr (*prec_threshold=*)Prec.Empty
                                              "An expression expected. 'if (' must be followed by a boolean expression"
            if condition_res.IsError
            then
                Error
            else
            
            if not (eat TokenKind.RParen "')' expected. The conditional expression must be enclosed in '(' and ')'")
            then
                Error
            else
                
            let then_branch_res = required_expr (*prec_threshold=*)Prec.Empty
                                                "An expression expected. A conditional expression must have a then branch"
            if then_branch_res.IsError
            then
                Error
            else
                
            if not (eat TokenKind.KwElse "'else' expected. A conditional expression must have an else branch")
            then
                Error
            else
                
            // Everything to the right of 'else' is treated as a "standalone" expression.
            // I.e., we pass prec_threshold=Prec.Empty
            let else_branch_res = required_expr
                                    (*prec_threshold=*)Prec.Empty
                                    "An expression required. A conditional expression must have an else branch"
            if else_branch_res.IsError
            then
                Error
            else
                
            let expr_if_span = Span.Of(span_start, else_branch_res.Value.Span.Last)
            let expr_if_syntax = ExprSyntax.If (condition=condition_res.Value,
                                                then_branch=then_branch_res.Value,
                                                else_branch=else_branch_res.Value)
            Ok (ValueSome (AstNode.Of(expr_if_syntax, expr_if_span)))
        else
        
        if try_eat TokenKind.KwWhile
        then
            if not (eat TokenKind.LParen "'(' expected. The condition must be enclosed in '(' and ')'")
            then
                Error
            else

            let condition_res = required_expr
                                    (*prec_threshold=*)Prec.Empty
                                    "An expression expected. 'while (' must be followed by a boolean expression"
            if condition_res.IsError
            then
                Error
            else
            
            if not (eat TokenKind.RParen "')' expected. The conditional expression must be enclosed in '(' and ')'")
            then
                Error
            else
                
            // The while's body is treated as a "standalone" expression.
            // I.e., we pass prec_threshold=Prec.Empty
            let body_res = required_expr
                               (*prec_threshold=*)Prec.Empty
                               "An expression expected. A while loop must have a body"
            if body_res.IsError
            then
                Error
            else
                
            let expr_while_span = Span.Of(span_start, body_res.Value.Span.Last)
            let expr_while_syntax = ExprSyntax.While (condition=condition_res.Value,
                                               body=body_res.Value)
            Ok (ValueSome (AstNode.Of(expr_while_syntax, expr_while_span)))
        else

        //
        // No prefix op specified
        //
        // primary infixop_rhs*
        match primary() with
        | Error ->
            Error
        | Ok ValueNone ->
            // Actually, the syntax doesn't contain any productions with an optional expression.
            // So, we know, we encountered a syntax error here.
            // But the caller can report a more specific error message, so we return control to the caller.
            Ok ValueNone
        | Ok (ValueSome primary_node) ->
            res_of (infixop_rhs prec_threshold primary_node)
        
    
    // A primary expression cannot be followed by another primary expression,
    // only by an infix op, i.e.: (expression suffix)*    
    and primary (): Res<AstNode<ExprSyntax> voption> =
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
                
            let actual_nodes_res = actuals ()
            if actual_nodes_res.IsError
            then
                Error
            else
            
            let actual_nodes = actual_nodes_res.Value
            let id_node = AstNode.Of(ID token_id.Id, token_id.Span)
            
            let expr_super_dispatch_span = Span.Of(span_start, actual_nodes.Span.Last)
            let expr_super_dispatch_syntax = ExprSyntax.SuperDispatch (method_id=id_node, actuals=actual_nodes.Syntax)
            
            Ok (ValueSome (AstNode.Of(expr_super_dispatch_syntax, expr_super_dispatch_span)))
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
                
            let actual_nodes_res = actuals ()
            if actual_nodes_res.IsError
            then
                Error
            else
            
            let actual_nodes = actual_nodes_res.Value
            let type_name_node = AstNode.Of(TYPENAME token_id.Id, token_id.Span)
            
            let expr_new_span = Span.Of(span_start, actual_nodes.Span.Last)
            let expr_new_syntax = ExprSyntax.New (type_name=type_name_node, actuals=actual_nodes.Syntax)
            Ok (ValueSome (AstNode.Of(expr_new_syntax, expr_new_span)))
        else
            
        // '{' block '}'
        if _token.Is(TokenKind.LBrace)
        then
            let block_node_res = braced_block()
            if block_node_res.IsError
            then
                Error
            else
                
            let block_node = block_node_res.Value
            Ok (ValueSome (AstNode.Of(ExprSyntax.BracedBlock block_node.Syntax, block_node.Span)))
        else
            
        // '(' expr ')'
        // '(' ')'
        if try_eat TokenKind.LParen
        then
            let token_rparen = _token
            if try_eat TokenKind.RParen
            then
                let expr_unit_span = Span.Of(span_start, token_rparen.Span.Last)
                Ok (ValueSome (AstNode.Of(ExprSyntax.Unit, expr_unit_span)))
            else
                
            let expr_node_res = required_expr ((*prec_threshold=*)Prec.Empty)
                                              "A parenthesized expression expected"
            if expr_node_res.IsError
            then
                Error
            else

            let token_rparen = _token                
            if not (eat TokenKind.RParen "')' expected. '(expression' must be followed by ')'")
            then
                Error
            else
                
            let expr_node = expr_node_res.Value
            
            let expr_parens_syntax = ExprSyntax.ParensExpr expr_node
            let expr_parens_span = Span.Of(span_start, token_rparen.Span.Last)
            
            Ok (ValueSome (AstNode.Of(expr_parens_syntax, expr_parens_span)))
        else
            
        // 'null'
        if try_eat TokenKind.KwNull
        then
            Ok (ValueSome (AstNode.Of(ExprSyntax.Null, first_token.Span)))
        else
            
        if try_eat TokenKind.KwThis
        then
            Ok (ValueSome (AstNode.Of(ExprSyntax.This, first_token.Span)))
        else

        // INTEGER
        if try_eat_when _token.IsInt
        then
            Ok (ValueSome (AstNode.Of(ExprSyntax.Int (INT first_token.Int), first_token.Span)))
        else

        // STRING
        if try_eat_when (_token.IsString || _token.IsQqqString)
        then
            Ok (ValueSome (AstNode.Of(ExprSyntax.Str (STRING (value=first_token.String,
                                                              is_qqq=first_token.IsQqqString)),
                                      first_token.Span)))
        else

        // BOOLEAN
        if try_eat TokenKind.KwTrue || try_eat TokenKind.KwFalse
        then
            let expr_bool_syntax =
                ExprSyntax.Bool (match first_token.Kind with
                                 | TokenKind.KwTrue -> BOOL.True
                                 | TokenKind.KwFalse -> BOOL.False
                                 | _ -> invalidOp "Unreachable")
            
            Ok (ValueSome (AstNode.Of(expr_bool_syntax, first_token.Span)))
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
    and infixop_rhs (prec_threshold: sbyte)
                    (atom: AstNode<ExprSyntax>)
                    : Res<AstNode<ExprSyntax>> =
        let span_start = atom.Span.First
        
        let mutable have_errors = false
        let mutable lhs = atom

        let mutable infixop_expected = true
        
        while infixop_expected && Prec.Of(_token.Kind) >= prec_threshold do
        
            let token_op = _token
            if try_eat_when _token.IsInfixOp
            then
                let rhs_res = required_expr ((*prec_threshold=*)Prec.Of(token_op.Kind) + 1y)
                                            (sprintf "An expression expected. '%s' must be followed by an expression"
                                                     token_op.InfixOpSpelling)
                if rhs_res.IsError
                then
                    have_errors <- true
                else

                let rhs = rhs_res.Value
                let expr_span = Span.Of(span_start, rhs.Span.Last)
                
                let expr_syntax =                
                    match token_op.Kind with
                    | TokenKind.LessEqual    -> ExprSyntax.LtEq (left=lhs, right=rhs)
                    | TokenKind.Less         -> ExprSyntax.Lt (left=lhs, right=rhs)
                    | TokenKind.GreaterEqual -> ExprSyntax.GtEq (left=lhs, right=rhs)
                    | TokenKind.Greater      -> ExprSyntax.Gt (left=lhs, right=rhs)
                    | TokenKind.EqualEqual   -> ExprSyntax.EqEq (left=lhs, right=rhs)
                    | TokenKind.ExclaimEqual -> ExprSyntax.NotEq (left=lhs, right=rhs)
                    | TokenKind.Star         -> ExprSyntax.Mul (left=lhs, right=rhs)
                    | TokenKind.Slash        -> ExprSyntax.Div (left=lhs, right=rhs)
                    | TokenKind.Plus         -> ExprSyntax.Sum (left=lhs, right=rhs)
                    | TokenKind.Minus        -> ExprSyntax.Sub (left=lhs, right=rhs)
                    | _                      -> invalidOp "Unreachable"
                        
                lhs <- AstNode.Of(expr_syntax, expr_span)
            else

            // 'match' cases infixop_rhs?
            if try_eat TokenKind.KwMatch
            then
                let case_nodes_res = cases ()
                if case_nodes_res.IsError
                then
                    have_errors <- true
                else
                
                let case_nodes = case_nodes_res.Value
                if case_nodes.Syntax.Length = 0
                then
                    _diags.Error("A match expression must contain at least one case",
                                 Span.Of(span_start, _token.Span.First))
                    have_errors <- true
                else

                let expr_match_span = Span.Of(span_start, case_nodes.Span.Last)
                let expr_match_syntax = ExprSyntax.Match (expr=lhs,
                                                          cases_hd=case_nodes.Syntax.[0],
                                                          cases_tl=case_nodes.Syntax.[1..])
                
                lhs <- AstNode.Of(expr_match_syntax, expr_match_span)
            else
                
            // '.' ID actuals infixop_rhs?
            if try_eat TokenKind.Dot
            then
                let token_id = _token
                if not (eat_when _token.IsId ("An identifier expected. '.' must be followed by a method name" +
                                              _token.KwDescription))
                then
                    have_errors <- true
                else
                    
                let actual_nodes_res = actuals()
                if actual_nodes_res.IsError
                then
                    have_errors <- true
                else
                
                let actual_nodes = actual_nodes_res.Value
                
                let expr_dispatch_span = Span.Of(span_start, actual_nodes.Span.Last)
                let expr_dispatch_syntax =
                    ExprSyntax.Dispatch (receiver=lhs,
                                         method_id=AstNode.Of(ID token_id.Id, token_id.Span),
                                         actuals=actual_nodes.Syntax)
               
                lhs <- AstNode.Of(expr_dispatch_syntax, expr_dispatch_span) 
            else
            
            infixop_expected <- false

        if have_errors
        then
            Error
        else
            Ok lhs

            
    // cases
    //     : '{' ('case' casepattern '=>' caseblock)+ '}'
    //     ;
    and cases (): Res<AstNode<AstNode<CaseSyntax>[]>> =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.LBrace
                    "'{' expected. Cases must be enclosed in '{' and '}'")
        then
            Error
        else
            
        let case_nodes = List<AstNode<CaseSyntax>>()

        let recover (): unit = 
            // Recover from a syntactic error by eating tokens
            // until we find the beginning of another feature
            eat_until [TokenKind.KwCase; TokenKind.RBrace]
            
        let is_cases_end (): bool = _token.IsEof || _token.Is(TokenKind.RBrace)

        let mutable is_case_expected = not (is_cases_end())
        while is_case_expected do
            match case () with
            | Ok case_node ->
                case_nodes.Add(case_node)
            | Error ->
                // We didn't manage to parse a feature.
                recover ()

            is_case_expected <- not (is_cases_end())
            
        if _token.IsEof
        then
            _diags.Error("'}' expected. Cases must be enclosed in '{' and '}'", _token.Span)
            Error
        else
            
        // Eat '}'
        let token_rbrace = _token
        eat_token()
        
        let case_nodes_span = Span.Of(span_start, token_rbrace.Span.Last)
        
        Ok (AstNode.Of(case_nodes.ToArray(), case_nodes_span))
    
    
    and case (): Res<AstNode<CaseSyntax>> =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.KwCase
                    "'case' expected. A case in 'match' expression must start with the 'case' keyword")
        then
            Error
        else
        
        let pattern_node_res = casepattern ()
        if pattern_node_res.IsError
        then
            Error
        else
            
        if not (eat TokenKind.EqualGreater
                    "'=>' expected. A case's pattern and block must be delimited by '=>'")
        then
            Error
        else
            
        let block_node_res = caseblock ()
        if (block_node_res.IsError)
        then
            Error
        else
            
        let block_node = block_node_res.Value
        
        let case_span = Span.Of(span_start, block_node.Span.Last)
        let case_syntax: CaseSyntax = { Pattern = pattern_node_res.Value; Block = block_node }
        
        Ok (AstNode.Of(case_syntax, case_span))
    
    
    // casepattern
    //     : ID ':' ID
    //     | 'null'
    //     ;
    and casepattern (): Res<AstNode<PatternSyntax>> =
        let span_start = _token.Span.First
        let first_token = _token
        
        if try_eat_when _token.IsId
        then
            if not (eat TokenKind.Colon
                        "':' expected. In a pattern, an identifier must be followed by ':'")
            then
                Error
            else
                
            let token_type = _token
            if not (eat_when _token.IsId
                             ("The pattern's type name expected. The type name must be an identifier" +
                              _token.KwDescription))
            then
                Error
            else
            
            let pattern_span = Span.Of(span_start, token_type.Span.Last)
            let pattern_syntax = PatternSyntax.IdType (id=AstNode.Of(ID first_token.Id, first_token.Span),
                                                pattern_type=AstNode.Of(TYPENAME token_type.Id, token_type.Span))
            
            Ok (AstNode.Of(pattern_syntax, pattern_span))
        else
            
        if try_eat TokenKind.KwNull
        then
            Ok (AstNode.Of(PatternSyntax.Null, first_token.Span))
        else
            
        _diags.Error("An identifier or 'null' expected. A pattern must start from an identifier or 'null'", _token.Span)
        Error
    

    // caseblock
    //     : block
    //     | '{' block? '}'
    //     ;
    and caseblock (): Res<AstNode<CaseBlockSyntax>> =
        if _token.Is(TokenKind.LBrace)
        then
            let braced_block_node_res = braced_block ()
            if braced_block_node_res.IsError
            then
                Error
            else
                
            Ok (braced_block_node_res.Value.Map(fun it -> CaseBlockSyntax.Braced it))
        else
        
        let span_start = _token.Span.First
        
        let block_syntax_result = block_syntax((*terminators=*)[TokenKind.KwCase; TokenKind.RBrace])
        if block_syntax_result.IsError
        then
            Error
        else
        
        if block_syntax_result.IsError
        then
            _diags.Error("A block expected. 'pattern =>' must be followed by a non-empty block",
                         _token.Span)
            Error
        else
            
        let block_node = block_syntax_result.Value
            
        let caseblock_span = Span.Of(span_start, block_node.Value.Span.Last)
        let caseblock_syntax = CaseBlockSyntax.Free block_node.Value.Syntax
        Ok (AstNode.Of(caseblock_syntax, caseblock_span))
    
    
    and required_expr (prec_threshold: sbyte)
                      (expr_required_error_message: string)
                      : Res<AstNode<ExprSyntax>> =
        let expr_node_res = expr prec_threshold
        if expr_node_res.IsError
        then
            Error
        else
        
        match expr_node_res.Value with
        | ValueNone ->
            _diags.Error(expr_required_error_message, _token.Span)
            Error
        | ValueSome expr_node ->
            Ok expr_node


    and stmt (): Res<AstNode<StmtSyntax>> =
        let span_start = _token.Span.First

        if try_eat TokenKind.KwVar
        then
            
            let token_id = _token
            if not (eat_when _token.IsId
                             ("A var name expected. Var name must be an identifier" +
                              _token.KwDescription))
            then
                Error
            else
                
            if not (eat TokenKind.Colon
                        "':' expected. A var's name and type must be delimited by ':'")
            then
                Error
            else
                
            let token_type = _token
            if not (eat_when _token.IsId
                             ("The var's type name expected. The type name must be an identifier" +
                              _token.KwDescription))
            then
                Error
            else
            
            if not (eat TokenKind.Equal
                        "'=' expected. A var's type and initializer must be delimited by '='")
            then
                Error
            else
                
            let expr_node_res = required_expr (*prec_threshold=*)Prec.Empty
                                              "An expression expected. Variables must be initialized"
            if expr_node_res.IsError
            then
                Error
            else
            
            let expr_node = expr_node_res.Value
            
            let var_span = Span.Of(span_start, expr_node.Span.Last)
            let var_syntax: VarSyntax =
                { ID = AstNode.Of(ID token_id.Id, token_id.Span)
                  TYPE = AstNode.Of(TYPENAME token_type.Id, token_type.Span)
                  Expr = expr_node }
            
            Ok (AstNode.Of(StmtSyntax.Var var_syntax, var_span))
        else
            
        let expr_node_res =
            required_expr (*prec_threshold=*)Prec.Empty
                          "'var' or an expression expected. Did you put ';' after the block's last expression?"
        match expr_node_res with
        | Error        -> Error
        | Ok expr_node -> Ok (expr_node.Map(fun it -> StmtSyntax.Expr it))


    and block_syntax (terminators: seq<TokenKind>): Res<AstNode<BlockSyntax> voption> =
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
        match last_stmt.Syntax with
        | StmtSyntax.Var _ ->
            _diags.Error("Blocks must end with an expression", last_stmt.Span)
            Error
        | StmtSyntax.Expr expr ->
            let block_span = Span.Of(span_start, last_stmt.Span.Last)
            let block_syntax: BlockSyntax = { Stmts = stmts
                                              Expr = AstNode.Of(expr, last_stmt.Span) }
            
            Ok (ValueSome (AstNode.Of(block_syntax, block_span)))

    
    and braced_block (): Res<AstNode<BlockSyntax voption>> =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.LBrace
                    "'{' expected. A braced block must start with '{'; an empty one is denoted by '{}'")
        then
            Error
        else

        let block_syntax_result = block_syntax((*terminators=*)[TokenKind.RBrace])
        if block_syntax_result.IsError
        then
            // There was an error parsing block_syntax,
            // Try to eat the braced block's remaining tokens,
            // we seem to produce better diagnostic messages this way.
            eat_until [TokenKind.RBrace]
            if _token.Is(TokenKind.RBrace)
            then
                eat_token()
                
            Error
        else
            
        if _token.IsEof
        then
            _diags.Error("'}' expected. A braced block must end with '}'", _token.Span)
            Error
        else
                
        // Eat '}'
        let token_rbrace = _token
        eat_token()
        
        let block_span = Span.Of(span_start, token_rbrace.Span.Last)
        let block_syntax = block_syntax_result.Value |> ValueOption.map (fun it -> it.Syntax)
        
        Ok (AstNode.Of(block_syntax, block_span))


    and varformal (): Res<AstNode<VarFormalSyntax>> =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.KwVar
                    "'var' expected. A varformal declaration must start with 'var'")
        then
            Error
        else

        let token_id = _token
        if not (eat_when _token.IsId
                         ("A varformal name expected. Varformal name must be an identifier" +
                          _token.KwDescription))
        then
            Error
        else
            
        if not (eat TokenKind.Colon
                    "':' expected. A varformal's name and type must be delimited by ':'")
        then
            Error
        else
            
        let token_type = _token
        if not (eat_when _token.IsId
                         ("The varformal's type name expected. The type name must be an identifier" +
                          _token.KwDescription))
        then
            Error
        else

        let id_node = AstNode.Of(ID token_id.Id, token_id.Span)
        let type_node = AstNode.Of(TYPENAME token_type.Id, token_type.Span)
        
        let varformal_span = Span.Of(span_start, type_node.Span.Last)
        let varformal_syntax =
            { VarFormalSyntax.ID = id_node
              TYPE = type_node }
            
        Ok (AstNode.Of(varformal_syntax, varformal_span))
    
    
    and varformals (): Res<AstNode<AstNode<VarFormalSyntax>[]>> =
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
        
        
    and actual (): Res<AstNode<ExprSyntax>> =
        required_expr ((*prec_threshold=*)Prec.Empty)
                      "An expression expected. Actuals must be an expression"
    
    
    and actuals (): Res<AstNode<AstNode<ExprSyntax>[]>> =
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
    
    
    and extends (): Res<AstNode<InheritanceSyntax> voption> =
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
            
        let actual_nodes_res = actuals ()
        if actual_nodes_res.IsError
        then
            Error
        else
        
        let actual_nodes = actual_nodes_res.Value
        let extends_span = Span.Of(span_start, actual_nodes.Span.Last)
        let extends_syntax: ExtendsSyntax =
            { SUPER = AstNode.Of(TYPENAME token_id.Id, token_id.Span)
              Actuals = actual_nodes.Syntax }
              
        Ok (ValueSome (AstNode.Of(InheritanceSyntax.Extends extends_syntax, extends_span)))
        
        
    and formal (): Res<AstNode<FormalSyntax>> =
        let span_start = _token.Span.First
        
        let token_id = _token
        if not (eat_when _token.IsId
                         ("A formal name expected. Formal name must be an identifier" +
                          _token.KwDescription))
        then
            Error
        else
            
        if not (eat TokenKind.Colon
                    "':' expected. A formal's name and type must be delimited by ':'")
        then
            Error
        else
            
        let token_type = _token
        if not (eat_when _token.IsId
                         ("The formal's type name expected. The type name must be an identifier" +
                          _token.KwDescription))
        then
            Error
        else
        
        let id_node = AstNode.Of(ID token_id.Id, token_id.Span)
        let type_node = AstNode.Of(TYPENAME token_type.Id, token_type.Span)
        
        let formal_span = Span.Of(span_start, type_node.Span.Last)
        let formal_syntax =
            { FormalSyntax.ID = id_node
              TYPE = type_node }
        
        Ok (AstNode.Of(formal_syntax, formal_span))
    
    
    and formals (): Res<AstNode<AstNode<FormalSyntax>[]>> =
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
    
    
    and method (span_start: uint32)
               (is_override: bool)
               : Res<AstNode<FeatureSyntax>> =
        let token_id = _token
        if not (eat_when _token.IsId
                         ("A method name expected. Method name must be an identifier" +
                          _token.KwDescription))
        then
            Error
        else
            
        let formal_nodes_res = formals()
        if formal_nodes_res.IsError
        then
            Error
        else
            
        if not (eat TokenKind.Colon
                    "':' expected. A method's formals and return type must be delimited by ':'")
        then
            Error
        else

        let token_type = _token
        if not (eat_when _token.IsId
                         ("A return type name expected. Type name must be an identifier" +
                          _token.KwDescription))
        then
            Error
        else
        
        if not (eat TokenKind.Equal
                    "'=' expected. A method's return type and body must be delimited by '='")
        then
            Error
        else
            
        let expr_node_res = required_expr (*prec_threshold=*)Prec.Empty
                                          "An expression expected. Methods must have a body"
        if expr_node_res.IsError
        then
            Error
        else
        
        let expr_node = expr_node_res.Value

        let method_span = Span.Of(span_start, expr_node.Span.Last)
        let method_syntax =
            { Override = is_override
              ID = AstNode.Of(ID token_id.Id, token_id.Span)
              Formals = formal_nodes_res.Value.Syntax
              RETURN =  AstNode.Of(TYPENAME token_type.Id, token_type.Span)
              Body = expr_node.Map(fun it -> MethodBodySyntax.Expr it) }
        
        Ok (AstNode.Of(FeatureSyntax.Method method_syntax, method_span))
        
        
    and attribute (): Res<AstNode<FeatureSyntax>> =
        let span_start = _token.Span.First
        
        if not (try_eat TokenKind.KwVar)
        then
            Error
        else
            
        let token_id = _token
        if not (eat_when _token.IsId
                         ("An attribute name expected. Attribute name must be an identifier" +
                          _token.KwDescription))
        then
            Error
        else
            
        if not (eat TokenKind.Colon
                    "':' expected. An attribute's name and type must be delimited by ':'")
        then
            Error
        else
            
        let token_type = _token
        if not (eat_when _token.IsId
                         ("The attribute's type name expected. The type name must be an identifier" +
                          _token.KwDescription))
        then
            Error
        else
        
        if not (eat TokenKind.Equal
                    "'=' expected. An attribute's type and initializer must be delimited by '='")
        then
            Error
        else
            
        let expr_node_res = required_expr (*prec_threshold=*)Prec.Empty
                                          "An expression expected. Attributes must be initialized"
        if expr_node_res.IsError
        then
            Error
        else
        
        let expr_node = expr_node_res.Value
        
        let attribute_span = Span.Of(span_start, expr_node.Span.Last)
        let attribute_syntax: AttrSyntax =
            { ID = AstNode.Of(ID token_id.Id, token_id.Span)
              TYPE = AstNode.Of(TYPENAME token_type.Id, token_type.Span)
              Initial = expr_node.Map(fun it -> AttrInitialSyntax.Expr it) }
        
        Ok (AstNode.Of(FeatureSyntax.Attr attribute_syntax, attribute_span))
        
        
    and feature (): Res<AstNode<FeatureSyntax>> =
        let span_start = _token.Span.First
        
        if try_eat TokenKind.KwOverride
        then
            if not (eat TokenKind.KwDef
                        "'def' expected. An overriden method must start with 'override def'")
            then
                Error
            else
                
            method span_start (*is_override=*)true
        else

        if try_eat TokenKind.KwDef
        then
            method span_start (*is_override=*)false
        else

        if _token.Is(TokenKind.KwVar)
        then
            attribute()
        else

        if _token.Is(TokenKind.LBrace)
        then
            let block_node_res = braced_block()
            if block_node_res.IsError
            then
                Error
            else
                
            let block_node = block_node_res.Value
            Ok (AstNode.Of(FeatureSyntax.BracedBlock block_node.Syntax, block_node.Span))
        else
            
        _diags.Error(
            "'def', 'override def', 'var', or '{' expected. Only a method, attribute, or block can appear at the class level",
            _token.Span)    
        Error


    and classbody (span_start: uint): Res<AstNode<AstNode<FeatureSyntax>[]>> =
        // The caller must eat '{' or emit a diagnostic if it's empty
        
        let feature_nodes = List<AstNode<FeatureSyntax>>()

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
            | Ok feature_node ->
                feature_nodes.Add(feature_node)
                
                if not (eat TokenKind.Semi
                            "';' expected. Features must be terminated by ';'")
                then
                    recover()

            | Error ->
                // We didn't manage to parse a feature.
                recover ()

            is_feature_expected <- not (is_classbody_end())
            
        if (_token.IsEof)
        then
            _diags.Error("'}' expected. A class body must end with '}'", _token.Span)
            Error
        else
            
        // Eat '}'
        let token_rbrace = _token
        eat_token()
        
        let classbody_span = Span.Of(span_start, token_rbrace.Span.Last)
        
        Ok (AstNode.Of(feature_nodes.ToArray(), classbody_span))
        
    
    and class_decl (): Res<AstNode<ClassSyntax>> =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.KwClass
                    "'class' expected. Only classes can appear at the top level")
        then
            Error
        else
            
        let token_id = _token
        if not (eat_when _token.IsId
                         ("A class name expected. Class name must be an identifier" +
                          _token.KwDescription))
        then
            Error
        else
           
        let varformals_node_res = varformals ()
        if varformals_node_res.IsError
        then
            // The callee already emitted relevant diags.
            // We aren't going to add anything else.
            Error
        else
        
        let extends_node_res = extends ()
        if extends_node_res.IsError
        then
            // The callee already emitted relevant diags.
            // We aren't going to add anything else.
            Error
        else
            
        if not (eat TokenKind.LBrace
                    ((if extends_node_res.Value.IsNone then "'extends' or " else "") +
                     "'{' expected. A class body must start with '{'"))
        then
            Error
        else
            
        let feature_nodes_res = classbody(span_start)
        if feature_nodes_res.IsError
        then
            // The callee already emitted relevant diags.
            // We aren't going to add anything else.
            Error
        else

        // classbody() eats '}'        
            
        let feature_nodes = feature_nodes_res.Value
        let name_node = AstNode.Of(TYPENAME token_id.Id, token_id.Span)
        
        let class_decl_span = Span.Of(span_start, feature_nodes.Span.Last)
        let class_decl_syntax =
            { ClassSyntax.NAME = name_node
              VarFormals = varformals_node_res.Value.Syntax
              Extends = extends_node_res.Value
              Features = feature_nodes.Syntax }

        Ok (AstNode.Of(class_decl_syntax, class_decl_span))
    
    
    and class_decls (): AstNode<AstNode<ClassSyntax>[]> =
        let span_start = _token.Span.First
        
        let class_decl_nodes = List<AstNode<ClassSyntax>>()
        
        while not _token.IsEof do
            match class_decl () with
            | Ok class_decl_node ->
                class_decl_nodes.Add(class_decl_node)
            | Error ->
                // We didn't manage to parse a class declaration.
                // We can start our next attempt to parse from only a 'class' keyword.
                // Let's skip all tokens until we find a 'class' keyword,
                // as otherwise we'd have to report every non-'class' token as unexpected,
                // and that would create a bunch of unhelpful diagnostics.
                eat_until [TokenKind.KwClass]

        let class_decls_span = Span.Of(span_start, _token.Span.Last)
        AstNode.Of(class_decl_nodes.ToArray(), class_decls_span)
        
        
    and ast (): ProgramSyntax =
        let class_decl_nodes = class_decls()
        { ProgramSyntax.Classes = class_decl_nodes.Syntax }

    
    member this.DelimitedList<'T>(element: unit -> Res<AstNode<'T>>,
                                  delimiter: TokenKind,
                                  delimiter_error_message: string,
                                  recover: unit -> unit,
                                  is_list_end: unit -> bool)
                                  : AstNode<'T>[] =
        
        let element_nodes = List<AstNode<'T>>()
        
        let mutable is_element_expected = not (is_list_end())
        while is_element_expected do
            match element() with
            | Ok element_node ->
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
            | Error ->
                // We didn't manage to parse an element
                recover()
                is_element_expected <- not (is_list_end())
            
        element_nodes.ToArray()


    member this.EnclosedDelimitedList<'T>(list_start: TokenKind,
                                          list_start_error_message: string,
                                          list_end_error_message: string,
                                          element: unit -> Res<AstNode<'T>>,
                                          delimiter: TokenKind,
                                          delimiter_error_message: string,
                                          recover: unit -> unit,
                                          is_list_end: unit -> bool)
                                          : Res<AstNode<AstNode<'T>[]>> =
        
        let span_start = _token.Span.First
        
        if not (eat list_start
                    list_start_error_message)
        then
            Error
        else

        let element_nodes = this.DelimitedList(element, delimiter, delimiter_error_message, recover, is_list_end)

        if _token.IsEof
        then
            _diags.Error(list_end_error_message, _token.Span)
            Error
        else
            
        // Eat the token that closes the list
        let span_end = _token.Span.Last
        eat_token()
        
        let list_span = Span.Of(span_start, span_end)
        
        Ok (AstNode.Of(element_nodes, list_span))


    member private _.Parse() : ProgramSyntax =
        ast()
        
        
    static member Parse(tokens: Token[], diags: DiagnosticBag) =
        let parser = Parser(tokens, diags)
        let ast = parser.Parse()
        ast
