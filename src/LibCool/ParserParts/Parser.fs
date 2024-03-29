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
    let mutable _token = _tokens[_offset]
    let mutable _prev_token_span_last = 0u

        
    let eatToken (): unit =
        if _offset + 1 >= _tokens.Length
        then
            invalidOp $"_offset [%d{_offset}] + 1 is >= _tokens.Length [%d{_tokens.Length}]"
        
        _prev_token_span_last <- _token.Span.Last
        _offset <- _offset + 1
        _token <- _tokens[_offset]
            
            
    let eat (kind: TokenKind)
            (error_message: string): bool =
        if _token.Is kind
        then
            eatToken()
            true
        else
            _diags.Error(error_message, Span.Of(_prev_token_span_last, _token.Span.First))
            false


    let tryEat (kind: TokenKind): bool =
        if _token.Is kind
        then
            eatToken()
            true
        else
            false
            
            
    let tryEatWhen (is_match: bool): bool =
        if is_match
        then
            eatToken()
            true
        else
            false

                
    let eatWhen (is_match: bool)
                (error_message: string): bool =
        if is_match
        then
            eatToken()
            true
        else
            _diags.Error(error_message, Span.Of(_prev_token_span_last, _token.Span.First))
            false
            
            
    let eatUntil (kinds: seq<TokenKind>): unit =
        let kind_set = Set.ofSeq kinds
        while not (_token.IsEof ||
                   kind_set.Contains(_token.Kind)) do
            eatToken()

    
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
    let rec expr (prec_threshold: sbyte): LcResult<AstNode<ExprSyntax> voption> =
        let span_start = _token.Span.First

        let first_token = _token
        if tryEatWhen _token.IsId
        then
            //
            // Assign
            //
            // 'ID = ' is an expression prefix.
            if tryEat TokenKind.Equal
            then
                // 'ID =' can be followed by any expression.
                // Everything to the right of '=' is treated as a "standalone" expression.
                // I.e., we pass prec_threshold=Prec.Empty
                let init_node_res = requiredExpr
                                        (*prec_threshold=*)Prec.Empty
                                        "An expression expected. '=' must be followed by an expression"
                match init_node_res with
                | Error ->
                    Error
                | Ok init_node ->
                    let id_node = AstNode.Of(ID first_token.Id, first_token.Span)

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
                match actual_nodes_res with
                | Error ->
                    Error
                | Ok actual_nodes ->
                    let id_node = AstNode.Of(ID first_token.Id, first_token.Span)

                    let expr_dispatch_span = Span.Of(span_start, actual_nodes.Span.Last)
                    let expr_dispatch_syntax = ExprSyntax.ImplicitThisDispatch (method_id=id_node, actuals=actual_nodes.Syntax)

                    // The 'ID actuals' dispatch can be followed by any infix op except '='.
                    LcResult.map ValueSome
                                 (infixOpRhs prec_threshold (AstNode.Of(expr_dispatch_syntax,
                                                                        expr_dispatch_span)))
            else

            // "ID" is a primary expression.
            // A primary expression cannot be followed by another primary expression,
            // only by an infix op, i.e.: (infixop_rhs)*
            LcResult.map ValueSome
                         (infixOpRhs prec_threshold (AstNode.Of(ExprSyntax.Id (ID first_token.Id),
                                                                first_token.Span)))
        else

        //
        // Prefix ops (Atom modifiers).
        //
        if tryEat TokenKind.Exclaim
        then
            // Exclaim applies boolean negation to the following:
            // - atom (e.g.: ID) or
            // - dispatch op (e.g.: receiver.method(...))
            let expr_node_res = requiredExpr ((*prec_threshold=*)Prec.OfExclaim + 1y)
                                              "An expression expected. '!' must be followed by an expression"
            match expr_node_res with
            | Error ->
                Error
            | Ok expr_node ->
                let expr_bool_neg_span = Span.Of(span_start, expr_node.Span.Last)
                let expr_bool_neg_syntax = ExprSyntax.BoolNegation expr_node

                // Boolean negation applied to an atom or subexpression
                // can then be followed by an infix op, i.e.: (infixop_rhs)*
                LcResult.map ValueSome
                             (infixOpRhs prec_threshold (AstNode.Of(expr_bool_neg_syntax,
                                                                    expr_bool_neg_span)))
        else

        if tryEat TokenKind.Minus
        then
            // Here, minus applies arithmetical negation to the following:
            // - atom (e.g.: ID) or
            // - dispatch op (e.g.: receiver.method(...))
            let expr_node_res = requiredExpr ((*prec_threshold=*)Prec.OfUnaryMinus + 1y)
                                              "An expression expected. '-' must be followed by an expression"
            match expr_node_res with
            | Error ->
                Error
            | Ok expr_node ->
                let expr_unary_minus_span = Span.Of(span_start, expr_node.Span.Last)
                let expr_unary_minus_syntax = ExprSyntax.UnaryMinus expr_node

                // Arithmetical negation applied to an atom or subexpression
                // can then be followed by an infix op, i.e.: (infixop_rhs)*
                LcResult.map ValueSome
                             (infixOpRhs prec_threshold (AstNode.Of(expr_unary_minus_syntax,
                                                                    expr_unary_minus_span)))
        else

        if tryEat TokenKind.KwIf
        then
            if not (eat TokenKind.LParen "'(' expected. The condition must be enclosed in '(' and ')'")
            then
                Error
            else

            let condition_res = requiredExpr (*prec_threshold=*)Prec.Empty
                                              "An expression expected. 'if (' must be followed by a boolean expression"
            match condition_res with
            | Error ->
                Error
            | Ok condition ->
                if not (eat TokenKind.RParen "')' expected. The conditional expression must be enclosed in '(' and ')'")
                then
                    Error
                else

                let then_branch_res = requiredExpr (*prec_threshold=*)Prec.Empty
                                                    "An expression expected. A conditional expression must have a then branch"
                match then_branch_res with
                | Error ->
                    Error
                | Ok then_branch ->
                    if not (eat TokenKind.KwElse "'else' expected. A conditional expression must have an else branch")
                    then
                        Error
                    else

                    // Everything to the right of 'else' is treated as a "standalone" expression.
                    // I.e., we pass prec_threshold=Prec.Empty
                    let else_branch_res = requiredExpr
                                            (*prec_threshold=*)Prec.Empty
                                            "An expression required. A conditional expression must have an else branch"
                    match else_branch_res with
                    | Error ->
                        Error
                    | Ok else_branch ->
                        let expr_if_span = Span.Of(span_start, else_branch.Span.Last)
                        let expr_if_syntax = ExprSyntax.If (condition=condition,
                                                            then_branch=then_branch,
                                                            else_branch=else_branch)
                        Ok (ValueSome (AstNode.Of(expr_if_syntax, expr_if_span)))
        else

        if tryEat TokenKind.KwWhile
        then
            if not (eat TokenKind.LParen "'(' expected. The condition must be enclosed in '(' and ')'")
            then
                Error
            else

            let condition_res = requiredExpr
                                    (*prec_threshold=*)Prec.Empty
                                    "An expression expected. 'while (' must be followed by a boolean expression"
            match condition_res with
            | Error ->
                Error
            | Ok condition_res ->

            if not (eat TokenKind.RParen "')' expected. The conditional expression must be enclosed in '(' and ')'")
            then
                Error
            else

            // The while's body is treated as a "standalone" expression.
            // I.e., we pass prec_threshold=Prec.Empty
            let body_res = requiredExpr
                               (*prec_threshold=*)Prec.Empty
                               "An expression expected. A while loop must have a body"
            match body_res with
            | Error ->
                Error
            | Ok body ->
                let expr_while_span = Span.Of(span_start, body.Span.Last)
                let expr_while_syntax = ExprSyntax.While (condition=condition_res,
                                                          body=body)
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
            LcResult.map ValueSome (infixOpRhs prec_threshold primary_node)
        
    
    // A primary expression cannot be followed by another primary expression,
    // only by an infix op, i.e.: (expression suffix)*    
    and primary (): LcResult<AstNode<ExprSyntax> voption> =
        let span_start = _token.Span.First
        let first_token = _token

        // ('super' '.')? ID actuals
        if tryEat TokenKind.KwSuper
        then
            if not (eat TokenKind.Dot "'.' expected. 'super' can only be used in a super dispatch expression")
            then
                Error
            else
                
            let token_id = _token
            if not (eatWhen _token.IsId ("A method name expected. Method name must be an identifier" +
                                          _token.KwDescription))
            then
                Error
            else
                
            let actual_nodes_res = actuals ()
            match actual_nodes_res with
            | Error ->
                Error
            | Ok actual_nodes ->
                let id_node = AstNode.Of(ID token_id.Id, token_id.Span)

                let expr_super_dispatch_span = Span.Of(span_start, actual_nodes.Span.Last)
                let expr_super_dispatch_syntax = ExprSyntax.SuperDispatch (method_id=id_node, actuals=actual_nodes.Syntax)

                Ok (ValueSome (AstNode.Of(expr_super_dispatch_syntax, expr_super_dispatch_span)))
        else

        // 'new' ID actuals
        if tryEat TokenKind.KwNew
        then
            let token_id = _token 
            if not (eatWhen _token.IsId ("A type name expected. Type name must be an identifier" +
                                          _token.KwDescription))
            then
                Error
            else
                
            let actual_nodes_res = actuals ()
            match actual_nodes_res with
            | Error ->
                Error
            | Ok actual_nodes ->
                let type_name_node = AstNode.Of(TYPENAME token_id.Id, token_id.Span)

                let expr_new_span = Span.Of(span_start, actual_nodes.Span.Last)
                let expr_new_syntax = ExprSyntax.New (type_name=type_name_node, actuals=actual_nodes.Syntax)
                Ok (ValueSome (AstNode.Of(expr_new_syntax, expr_new_span)))
        else
            
        // '{' block '}'
        if _token.Is(TokenKind.LBrace)
        then
            let block_node_res = bracedBlock()
            match block_node_res with
            | Error ->
                Error
            | Ok block_node ->
                Ok (ValueSome (AstNode.Of(ExprSyntax.BracedBlock block_node.Syntax, block_node.Span)))
        else
            
        // '(' expr ')'
        // '(' ')'
        if tryEat TokenKind.LParen
        then
            let token_rparen = _token
            if tryEat TokenKind.RParen
            then
                let expr_unit_span = Span.Of(span_start, token_rparen.Span.Last)
                Ok (ValueSome (AstNode.Of(ExprSyntax.Unit, expr_unit_span)))
            else
                
            let expr_node_res = requiredExpr (*prec_threshold=*)Prec.Empty
                                             "A parenthesized expression expected"
            match expr_node_res with
            | Error ->
                Error
            | Ok expr_node ->
                let token_rparen = _token
                if not (eat TokenKind.RParen "')' expected. '(expression' must be followed by ')'")
                then
                    Error
                else

                let expr_parens_syntax = ExprSyntax.ParensExpr expr_node
                let expr_parens_span = Span.Of(span_start, token_rparen.Span.Last)

                Ok (ValueSome (AstNode.Of(expr_parens_syntax, expr_parens_span)))
        else
            
        // 'null'
        if tryEat TokenKind.KwNull
        then
            Ok (ValueSome (AstNode.Of(ExprSyntax.Null, first_token.Span)))
        else
            
        if tryEat TokenKind.KwThis
        then
            Ok (ValueSome (AstNode.Of(ExprSyntax.This, first_token.Span)))
        else

        // INTEGER
        if tryEatWhen _token.IsInt
        then
            Ok (ValueSome (AstNode.Of(ExprSyntax.Int (INT first_token.Int), first_token.Span)))
        else

        // STRING
        if tryEatWhen (_token.IsString || _token.IsQqqString)
        then
            Ok (ValueSome (AstNode.Of(ExprSyntax.Str (STRING (value=first_token.String,
                                                              is_qqq=first_token.IsQqqString)),
                                      first_token.Span)))
        else

        // BOOLEAN
        if tryEat TokenKind.KwTrue || tryEat TokenKind.KwFalse
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
    and infixOpRhs (prec_threshold: sbyte)
                    (atom: AstNode<ExprSyntax>)
                    : LcResult<AstNode<ExprSyntax>> =
        let span_start = atom.Span.First
        
        let mutable have_errors = false
        let mutable lhs = atom

        let mutable infixop_expected = true
        
        while infixop_expected && Prec.Of(_token.Kind) >= prec_threshold do
        
            let token_op = _token
            if tryEatWhen _token.IsInfixOp
            then
                let rhs_res = requiredExpr ((*prec_threshold=*)Prec.Of(token_op.Kind) + 1y)
                                            ("An expression expected. " +
                                             $"'%s{token_op.InfixOpSpelling}' must be followed by an expression")
                match rhs_res with
                | Error ->
                    have_errors <- true
                | Ok rhs ->
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
            if tryEat TokenKind.KwMatch
            then
                let case_nodes_res = cases ()
                match case_nodes_res with
                | Error ->
                    have_errors <- true
                | Ok case_nodes ->
                    if case_nodes.Syntax.Length = 0
                    then
                        _diags.Error("A match expression must contain at least one case",
                                     Span.Of(span_start, _token.Span.First))
                        have_errors <- true
                    else

                    let expr_match_span = Span.Of(span_start, case_nodes.Span.Last)
                    let expr_match_syntax = ExprSyntax.Match (expr=lhs,
                                                              cases_hd=case_nodes.Syntax[0],
                                                              cases_tl=case_nodes.Syntax[1..])

                    lhs <- AstNode.Of(expr_match_syntax, expr_match_span)
            else
                
            // '.' ID actuals infixop_rhs?
            if tryEat TokenKind.Dot
            then
                let token_id = _token
                if not (eatWhen _token.IsId ("An identifier expected. '.' must be followed by a method name" +
                                              _token.KwDescription))
                then
                    have_errors <- true
                else
                    
                let actual_nodes_res = actuals()
                match actual_nodes_res with
                | Error ->
                    have_errors <- true
                | Ok actual_nodes ->
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
    and cases (): LcResult<AstNode<AstNode<CaseSyntax>[]>> =
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
            eatUntil [TokenKind.KwCase; TokenKind.RBrace]
            
        let isCasesEnd (): bool = _token.IsEof || _token.Is(TokenKind.RBrace)

        let mutable is_case_expected = not (isCasesEnd())
        while is_case_expected do
            match case () with
            | Ok case_node ->
                case_nodes.Add(case_node)
            | Error ->
                // We didn't manage to parse a feature.
                recover ()

            is_case_expected <- not (isCasesEnd())
            
        if _token.IsEof
        then
            _diags.Error("'}' expected. Cases must be enclosed in '{' and '}'", _token.Span)
            Error
        else
            
        // Eat '}'
        let token_rbrace = _token
        eatToken()
        
        Ok (AstNode.Of(case_nodes.ToArray(),
                       Span.Of(span_start, token_rbrace.Span.Last)))
    
    
    and case (): LcResult<AstNode<CaseSyntax>> =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.KwCase
                    "'case' expected. A case in 'match' expression must start with the 'case' keyword")
        then
            Error
        else
        
        let pattern_node_res = casePattern ()
        match pattern_node_res with
        | Error ->
            Error
        | Ok pattern_node ->
            if not (eat TokenKind.EqualGreater
                        "'=>' expected. A case's pattern and block must be delimited by '=>'")
            then
                Error
            else

            let block_node_res = caseBlock ()
            match block_node_res with
            | Error ->
                Error
            | Ok block_node ->
                let case_span = Span.Of(span_start, block_node.Span.Last)
                let case_syntax: CaseSyntax = { Pattern = pattern_node; Block = block_node }

                Ok (AstNode.Of(case_syntax, case_span))
    
    
    // casepattern
    //     : ID ':' ID
    //     | 'null'
    //     ;
    and casePattern (): LcResult<AstNode<PatternSyntax>> =
        let span_start = _token.Span.First
        let first_token = _token
        
        if tryEatWhen _token.IsId
        then
            if not (eat TokenKind.Colon
                        "':' expected. In a pattern, an identifier must be followed by ':'")
            then
                Error
            else
                
            let token_type = _token
            if not (eatWhen _token.IsId
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
            
        if tryEat TokenKind.KwNull
        then
            Ok (AstNode.Of(PatternSyntax.Null, first_token.Span))
        else
            
        _diags.Error(
            "An identifier or 'null' expected. A pattern must start from an identifier or 'null'",
            _token.Span)
        Error
    

    // caseblock
    //     : block
    //     | '{' block? '}'
    //     ;
    and caseBlock (): LcResult<AstNode<CaseBlockSyntax>> =
        if _token.Is(TokenKind.LBrace)
        then
            let braced_block_node_res = bracedBlock ()
            match braced_block_node_res with
            | Error ->
                Error
            | Ok braced_block_node ->
                
            Ok (braced_block_node.Map(fun it -> CaseBlockSyntax.Braced it))
        else
        
        let span_start = _token.Span.First
        
        let block_syntax_res = blockSyntax((*terminators=*)[TokenKind.KwCase; TokenKind.RBrace])
        match block_syntax_res with
        | Error ->
            Error
        | Ok ValueNone ->
            _diags.Error(
                "A block expected. 'pattern =>' must be followed by a non-empty block",
                _token.Span)
            Error
        | Ok (ValueSome block_node) ->
            let case_block_span = Span.Of(span_start, block_node.Span.Last)
            let case_block_syntax = CaseBlockSyntax.Free block_node.Syntax
            Ok (AstNode.Of(case_block_syntax, case_block_span))
    
    
    and requiredExpr (prec_threshold: sbyte)
                      (expr_required_error_message: string)
                      : LcResult<AstNode<ExprSyntax>> =
        let expr_node_res = expr prec_threshold
        match expr_node_res with
        | Error ->
            Error
        | Ok ValueNone ->
            _diags.Error(expr_required_error_message, _token.Span)
            Error
        | Ok (ValueSome expr_node) ->
            Ok expr_node


    and stmt (): LcResult<AstNode<StmtSyntax>> =
        let span_start = _token.Span.First

        if tryEat TokenKind.KwVar
        then
            
            let token_id = _token
            if not (eatWhen _token.IsId
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
            if not (eatWhen _token.IsId
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
                
            let expr_node_res = requiredExpr (*prec_threshold=*)Prec.Empty
                                              "An expression expected. Variables must be initialized"
            match expr_node_res with
            | Error ->
                Error
            | Ok expr_node ->
                let var_span = Span.Of(span_start, expr_node.Span.Last)
                let var_syntax: VarSyntax =
                    { ID = AstNode.Of(ID token_id.Id, token_id.Span)
                      TYPE = AstNode.Of(TYPENAME token_type.Id, token_type.Span)
                      Expr = expr_node }

                Ok (AstNode.Of(StmtSyntax.Var var_syntax, var_span))
        else
            
        let expr_node_res =
            requiredExpr (*prec_threshold=*)Prec.Empty
                         "'var' or an expression expected. Did you put ';' after the block's last expression?"
        match expr_node_res with
        | Error        -> Error
        | Ok expr_node -> Ok (expr_node.Map(fun it -> StmtSyntax.Expr it))


    and blockSyntax (terminators: seq<TokenKind>): LcResult<AstNode<BlockSyntax> voption> =
        let ts_set = Set.ofSeq terminators
        
        let recover (): unit =
            // Recover from a syntactic error by eating tokens
            // until we find ';'
            // Then eat it -- hopefully this places us at the start of a statement.
            eatUntil (Seq.append [TokenKind.Semi] terminators)
            if _token.Is(TokenKind.Semi)
            then
                eatToken()
        
        let isBlockEnd (): bool = _token.IsEof || ts_set.Contains(_token.Kind)

        let span_start = _token.Span.First
        let stmt_nodes = this.DelimitedList(
                            element=stmt,
                            delimiter=TokenKind.Semi,
                            delimiter_error_message="';' expected. Statements of a block must be delimited by ';'",
                            recover=recover,
                            is_list_end=isBlockEnd)
        
        if stmt_nodes.Length = 0
        then
            Ok ValueNone
        else
            
        let stmts = stmt_nodes |> Seq.take (stmt_nodes.Length - 1) |> Array.ofSeq
        
        let last_stmt = stmt_nodes[stmt_nodes.Length - 1]
        match last_stmt.Syntax with
        | StmtSyntax.Var _ ->
            _diags.Error("Blocks must end with an expression", last_stmt.Span)
            Error
        | StmtSyntax.Expr expr ->
            let block_span = Span.Of(span_start, last_stmt.Span.Last)
            let block_syntax: BlockSyntax = { Stmts = stmts
                                              Expr = AstNode.Of(expr, last_stmt.Span) }
            
            Ok (ValueSome (AstNode.Of(block_syntax, block_span)))

    
    and bracedBlock (): LcResult<AstNode<BlockSyntax voption>> =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.LBrace
                    "'{' expected. A braced block must start with '{'; an empty one is denoted by '{}'")
        then
            Error
        else

        let block_syntax_res = blockSyntax((*terminators=*)[TokenKind.RBrace])
        match block_syntax_res with
        | Error ->
            // There was an error parsing block_syntax,
            // Try to eat the braced block's remaining tokens,
            // we seem to produce better diagnostic messages this way.
            eatUntil [TokenKind.RBrace]
            if _token.Is(TokenKind.RBrace)
            then
                eatToken()

            Error
        | Ok block_syntax_node ->
            if _token.IsEof
            then
                _diags.Error("'}' expected. A braced block must end with '}'", _token.Span)
                Error
            else

            // Eat '}'
            let token_rbrace = _token
            eatToken()

            let block_span = Span.Of(span_start, token_rbrace.Span.Last)
            let block_syntax = block_syntax_node |> ValueOption.map (fun it -> it.Syntax)

            Ok (AstNode.Of(block_syntax, block_span))


    and varFormal (): LcResult<AstNode<VarFormalSyntax>> =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.KwVar
                    "'var' expected. A varformal declaration must start with 'var'")
        then
            Error
        else

        let token_id = _token
        if not (eatWhen _token.IsId
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
        if not (eatWhen _token.IsId
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
    
    
    and varFormals (): LcResult<AstNode<AstNode<VarFormalSyntax>[]>> =
        let recover (): unit =
            // Recover from a syntactic error by eating tokens
            // until we find 'var' -- the start of another varformal.
            eatUntil [TokenKind.RParen; TokenKind.KwVar]
            
        let isVarFormalsEnd (): bool = _token.IsEof || _token.Is(TokenKind.RParen)

        this.EnclosedDelimitedList(
            list_start=TokenKind.LParen,
            list_start_error_message="'(' expected. A varformals list must start with '('; an empty one is denoted by '()'",
            list_end_error_message="')' expected. A varformals list must end with ')'",
            element=varFormal,
            delimiter=TokenKind.Comma,
            delimiter_error_message="',' expected. Elements of a varformal list must be delimited by ','",
            recover=recover,
            is_list_end=isVarFormalsEnd)
        
        
    and actual (): LcResult<AstNode<ExprSyntax>> =
        requiredExpr (*prec_threshold=*)Prec.Empty
                      "An expression expected. Actuals must be an expression"
    
    
    and actuals (): LcResult<AstNode<AstNode<ExprSyntax>[]>> =
        let recover (): unit =
            // Recover from a syntactic error by eating tokens
            // until we find ','
            // Then eat it -- hopefully this places us at the start of another formal.
            eatUntil [TokenKind.RParen; TokenKind.Comma]
            if _token.Is(TokenKind.Comma)
            then
                eatToken()
                
        let isActualsEnd (): bool = _token.IsEof || _token.Is(TokenKind.RParen)

        this.EnclosedDelimitedList(
            list_start=TokenKind.LParen,
            list_start_error_message="'(' expected. An actuals list must start with '('; an empty one is denoted by '()'",
            list_end_error_message="')' expected. An actuals list must end with ')'",
            element=actual,
            delimiter=TokenKind.Comma,
            delimiter_error_message="',' expected. Elements of an actuals list must be delimited by ','",
            recover=recover,
            is_list_end=isActualsEnd)
    
    
    and extends (): LcResult<AstNode<InheritanceSyntax> voption> =
        let span_start = _token.Span.First
        
        if not (tryEat TokenKind.KwExtends)
        then
            Ok ValueNone
        else

        let token_id = _token
        if not (eatWhen _token.IsId
                         ("A parent class name expected. Parent class name must be an identifier" +
                          _token.KwDescription))
        then
            Error
        else
            
        let actual_nodes_res = actuals ()
        match actual_nodes_res with
        | Error ->
            Error
        | Ok actual_nodes ->
            let extends_span = Span.Of(span_start, actual_nodes.Span.Last)
            let extends_syntax: ExtendsSyntax =
                { SUPER = AstNode.Of(TYPENAME token_id.Id, token_id.Span)
                  Actuals = actual_nodes.Syntax }

            Ok (ValueSome (AstNode.Of(InheritanceSyntax.Extends extends_syntax, extends_span)))
        
        
    and formal (): LcResult<AstNode<FormalSyntax>> =
        let span_start = _token.Span.First
        
        let token_id = _token
        if not (eatWhen _token.IsId
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
        if not (eatWhen _token.IsId
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
    
    
    and formals (): LcResult<AstNode<AstNode<FormalSyntax>[]>> =
        let recover (): unit =
            // Recover from a syntactic error by eating tokens
            // until we find ','
            // Then eat it -- hopefully this places us at the start of another formal.
            eatUntil [TokenKind.RParen; TokenKind.Comma]
            if _token.Is(TokenKind.Comma)
            then
                eatToken()
                
        let isFormalsEnd (): bool = _token.IsEof || _token.Is(TokenKind.RParen)

        this.EnclosedDelimitedList(
            list_start=TokenKind.LParen,
            list_start_error_message="'(' expected. A formals list must start with '('; an empty one is denoted by '()'",
            list_end_error_message="')' expected. A formals list must end with ')'",
            element=formal,
            delimiter=TokenKind.Comma,
            delimiter_error_message="',' expected. Elements of a formals list must be delimited by ','",
            recover=recover,
            is_list_end=isFormalsEnd)
    
    
    and method (span_start: uint32)
               (is_override: bool)
               : LcResult<AstNode<FeatureSyntax>> =
        let token_id = _token
        if not (eatWhen _token.IsId
                         ("A method name expected. Method name must be an identifier" +
                          _token.KwDescription))
        then
            Error
        else
            
        let formal_nodes_res = formals()
        match formal_nodes_res with
        | Error ->
            Error
        | Ok formal_nodes ->
            if not (eat TokenKind.Colon
                        "':' expected. A method's formals and return type must be delimited by ':'")
            then
                Error
            else

            let token_type = _token
            if not (eatWhen _token.IsId
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

            let expr_node_res = requiredExpr (*prec_threshold=*)Prec.Empty
                                              "An expression expected. Methods must have a body"
            match expr_node_res with
            | Error ->
                Error
            | Ok expr_node ->
                let method_span = Span.Of(span_start, expr_node.Span.Last)
                let method_syntax =
                    { Override = is_override
                      ID = AstNode.Of(ID token_id.Id, token_id.Span)
                      Formals = formal_nodes.Syntax
                      RETURN =  AstNode.Of(TYPENAME token_type.Id, token_type.Span)
                      Body = expr_node.Map(fun it -> MethodBodySyntax.Expr it) }

                Ok (AstNode.Of(FeatureSyntax.Method method_syntax, method_span))
        
        
    and attribute (): LcResult<AstNode<FeatureSyntax>> =
        let span_start = _token.Span.First
        
        if not (tryEat TokenKind.KwVar)
        then
            Error
        else
            
        let token_id = _token
        if not (eatWhen _token.IsId
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
        if not (eatWhen _token.IsId
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
            
        let expr_node_res = requiredExpr (*prec_threshold=*)Prec.Empty
                                          "An expression expected. Attributes must be initialized"
        match expr_node_res with
        | Error ->
            Error
        | Ok expr_node ->
            let attribute_span = Span.Of(span_start, expr_node.Span.Last)
            let attribute_syntax: AttrSyntax =
                { ID = AstNode.Of(ID token_id.Id, token_id.Span)
                  TYPE = AstNode.Of(TYPENAME token_type.Id, token_type.Span)
                  Initial = expr_node.Map(fun it -> AttrInitialSyntax.Expr it) }

            Ok (AstNode.Of(FeatureSyntax.Attr attribute_syntax, attribute_span))
        
        
    and feature (): LcResult<AstNode<FeatureSyntax>> =
        let span_start = _token.Span.First
        
        if tryEat TokenKind.KwOverride
        then
            if not (eat TokenKind.KwDef
                        "'def' expected. An overriden method must start with 'override def'")
            then
                Error
            else
                
            method span_start (*is_override=*)true
        else

        if tryEat TokenKind.KwDef
        then
            method span_start (*is_override=*)false
        else

        if _token.Is(TokenKind.KwVar)
        then
            attribute()
        else

        if _token.Is(TokenKind.LBrace)
        then
            let block_node_res = bracedBlock()
            match block_node_res with
            | Error ->
                Error
            | Ok block_node ->
                Ok (AstNode.Of(FeatureSyntax.BracedBlock block_node.Syntax, block_node.Span))
        else
            
        _diags.Error(
            "'def', 'override def', 'var', or '{' expected. Only a method, attribute, or block can appear at the class level",
            _token.Span)    
        Error


    and classBody (span_start: uint): LcResult<AstNode<AstNode<FeatureSyntax>[]>> =
        // The caller must eat '{' or emit a diagnostic if it's empty
        
        let feature_nodes = List<AstNode<FeatureSyntax>>()

        let recover (): unit = 
            // Recover from a syntactic error by eating tokens
            // until we find the beginning of another feature
            eatUntil [ // end of classbody
                       TokenKind.RBrace
                       // start of a block
                       TokenKind.LBrace 
                       // start of a method
                       TokenKind.KwOverride 
                       TokenKind.KwDef
                       // start of an attribute
                       TokenKind.KwVar]
            
        let isClassbodyEnd (): bool = _token.IsEof || _token.Is(TokenKind.RBrace)

        let mutable is_feature_expected = not (isClassbodyEnd())
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

            is_feature_expected <- not (isClassbodyEnd())
            
        if _token.IsEof
        then
            _diags.Error("'}' expected. A class body must end with '}'", _token.Span)
            Error
        else
            
        // Eat '}'
        let token_rbrace = _token
        eatToken()
        
        let classbody_span = Span.Of(span_start, token_rbrace.Span.Last)
        
        Ok (AstNode.Of(feature_nodes.ToArray(), classbody_span))
        
    
    and classDecl (): LcResult<AstNode<ClassSyntax>> =
        let span_start = _token.Span.First
        
        if not (eat TokenKind.KwClass
                    "'class' expected. Only classes can appear at the top level")
        then
            Error
        else
            
        let token_id = _token
        if not (eatWhen _token.IsId
                         ("A class name expected. Class name must be an identifier" +
                          _token.KwDescription))
        then
            Error
        else
           
        let varformals_node_res = varFormals ()
        match varformals_node_res with
        | Error ->
            // The callee already emitted relevant diags.
            // We aren't going to add anything else.
            Error
        | Ok varformals_node ->
            let extends_node_res = extends ()
            match extends_node_res with
            | Error ->
                // The callee already emitted relevant diags.
                // We aren't going to add anything else.
                Error
            | Ok extends_node ->
                if not (eat TokenKind.LBrace
                            ((if extends_node.IsNone then "'extends' or " else "") +
                             "'{' expected. A class body must start with '{'"))
                then
                    Error
                else

                let feature_nodes_res = classBody(span_start)
                match feature_nodes_res with
                | Error ->
                    // The callee already emitted relevant diags.
                    // We aren't going to add anything else.
                    Error
                | Ok feature_nodes ->
                    // classbody() eats '}'

                    let name_node = AstNode.Of(TYPENAME token_id.Id, token_id.Span)

                    let class_decl_span = Span.Of(span_start, feature_nodes.Span.Last)
                    let class_decl_syntax =
                        { ClassSyntax.NAME = name_node
                          VarFormals = varformals_node.Syntax
                          Extends = extends_node
                          Features = feature_nodes.Syntax }

                    Ok (AstNode.Of(class_decl_syntax, class_decl_span))
    
    
    and classDecls (): AstNode<AstNode<ClassSyntax>[]> =
        let span_start = _token.Span.First
        
        let class_decl_nodes = List<AstNode<ClassSyntax>>()
        
        while not _token.IsEof do
            match classDecl () with
            | Ok class_decl_node ->
                class_decl_nodes.Add(class_decl_node)
            | Error ->
                // We didn't manage to parse a class declaration.
                // We can start our next attempt to parse from only a 'class' keyword.
                // Let's skip all tokens until we find a 'class' keyword,
                // as otherwise we'd have to report every non-'class' token as unexpected,
                // and that would create a bunch of unhelpful diagnostics.
                eatUntil [TokenKind.KwClass]

        let class_decls_span = Span.Of(span_start, _token.Span.Last)
        AstNode.Of(class_decl_nodes.ToArray(), class_decls_span)
        
        
    and ast (): ProgramSyntax =
        let class_decl_nodes = classDecls()
        { ProgramSyntax.Classes = class_decl_nodes.Syntax }

    
    member this.DelimitedList<'T>(element: unit -> LcResult<AstNode<'T>>,
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

                if tryEat delimiter
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
                                          element: unit -> LcResult<AstNode<'T>>,
                                          delimiter: TokenKind,
                                          delimiter_error_message: string,
                                          recover: unit -> unit,
                                          is_list_end: unit -> bool)
                                          : LcResult<AstNode<AstNode<'T>[]>> =
        
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
        eatToken()
        
        let list_span = Span.Of(span_start, span_end)
        
        Ok (AstNode.Of(element_nodes, list_span))


    member private _.Parse() : ProgramSyntax =
        ast()
        
        
    static member Parse(tokens: Token[], diags: DiagnosticBag) =
        let parser = Parser(tokens, diags)
        let ast = parser.Parse()
        ast
