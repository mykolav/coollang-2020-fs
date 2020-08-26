namespace Tests.Parser


open System
open System.Text
open LibCool.SourceParts
open LibCool.Ast


[<RequireQualifiedAccess>]
type CompareOp =
    | LtEq
    | GtEq
    | Lt
    | Gt
    | EqEq
    | NotEq


[<RequireQualifiedAccess>]
type ArithOp =
    | Mul
    | Div
    | Sum
    | Sub
    

[<Sealed>]
type private Indent(?width: int) =
    let _width = defaultArg width 2
    let mutable _level = 0
    
    let mk_value () = String(' ', count = _level * _width)
    let mutable _value = mk_value ()
    
    
    member this.Increase() =
        _level <- _level + 1
        _value <- mk_value ()
        
    
    member this.Decrease() =
        if _level = 0
        then
            invalidOp "An indent's level cannot go less than 0"
            
        _level <- _level - 1
        _value <- mk_value ()
       
    
    override this.ToString() = _value


[<Sealed>]
type CoolRenderer1 private () =

    
    let _indent = Indent()
    let _acc_cool_text = StringBuilder()

        
    // Classes
    let rec EnterClass(_: ClassDecl, _: Guid, _: Range) : unit =
        _acc_cool_text
            .Append(_indent)
            .Append("class ") |> ignore
        
    
    and EnterVarFormals(_: Node<VarFormal>[]) : unit =
        _acc_cool_text.Append("(") |> ignore
    
    and LeaveVarFormals(_: Node<VarFormal>[]) : unit =
        _acc_cool_text.Append(")") |> ignore

    
    and EnterVarFormal(_: VarFormal, index: int, _: Guid, _: Range) : unit =
        if index > 0
        then do
            _acc_cool_text.Append(", ") |> ignore
        
    
    and EnterExtends(_: Extends, _: Guid, _: Range) : unit =
        _acc_cool_text.Append(" extends ") |> ignore
    
    
    and EnterFeatures(_: Node<Feature>[]) : unit =
        _acc_cool_text
            .Append(" {")
            .AppendLine() |> ignore
        _indent.Increase()
            
    and LeaveFeatures(_: Node<Feature>[]) : unit =
        _indent.Decrease()
        _acc_cool_text
            .Append(_indent)
            .Append("}")
            .AppendLine() |> ignore

    // Method
    and EnterMethod(method_info: MethodInfo, _: Guid, _: Range) =
        _acc_cool_text
            .Append(_indent)
            .Append(if method_info.Override then "override def " else "def ") |> ignore
    
    
     and EnterFormals(_:Node<Formal>[]) : unit =
        _acc_cool_text.Append("(") |> ignore
    
     and LeaveFormals(_:Node<Formal>[]) : unit =
        _acc_cool_text.Append(")") |> ignore
    
    
     and EnterFormal(_: Formal, index: int, _: Guid, _: Range) : unit =
        if index > 0
        then do
            _acc_cool_text.Append(", ") |> ignore
        
    
    and EnterMethodBody(method_body: MethodBody, _: Guid, _: Range) = 
        _acc_cool_text.Append(" = ") |> ignore
        match method_body with
        | MethodBody.Expr expr ->
            match expr with
            | Expr.BracedBlock _ -> ()
            | _ ->
                _acc_cool_text.AppendLine() |> ignore
                _indent.Increase()
        | _ -> ()
    
    and LeaveMethodBody(method_body: MethodBody, _: Guid, _: Range) = 
        match method_body with
        | MethodBody.Expr expr ->
            match expr with
            | Expr.BracedBlock _ -> ()
            | _ ->
                _indent.Decrease()
        | _ -> ()

    
    // Attribute
     and EnterAttr(_: AttrInfo, _: Guid, _: Range) : unit =
        _acc_cool_text
            .Append(_indent)
            .Append("var ") |> ignore
    
    
     and EnterAttrBody(attr_body: AttrBody, _: Guid, _: Range) = 
        _acc_cool_text.Append(" = ") |> ignore
        match attr_body with
        | AttrBody.Expr expr ->
            match expr with
            | Expr.BracedBlock _ -> ()
            | _ ->
                _acc_cool_text.AppendLine() |> ignore
                _indent.Increase()
        | _ -> ()
    
     and LeaveAttrBody(attr_body: AttrBody, _: Guid, _: Range) = 
        match attr_body with
        | AttrBody.Expr expr ->
            match expr with
            | Expr.BracedBlock _ -> ()
            | _ ->
                _indent.Decrease()
        | _ -> ()


    // Block
     and EnterBlock(_: BlockInfo, _: Guid, _: Range) : unit =
        _acc_cool_text
            .Append("{")
            .AppendLine() |> ignore
        _indent.Increase()
    
     and LeaveBlock(_: BlockInfo, _: Guid, _: Range) : unit =
        _indent.Decrease()
        _acc_cool_text
            .Append(_indent)
            .Append("}")
            .AppendLine() |> ignore
    
    
     and EnterVarDecl(_: VarDeclInfo , _: Guid , _: Range) : unit =
        _acc_cool_text
            .Append(_indent)
            .Append("var ") |> ignore
    
     and LeaveVarDecl(_: VarDeclInfo , _: Guid , _: Range) : unit =
        _acc_cool_text
            .Append(";")
            .AppendLine() |> ignore

    
     and EnterStmtExpr(_: Expr, _: Guid, _: Range) : unit =
        _acc_cool_text.Append(_indent) |> ignore

     and LeaveStmtExpr(_: Expr, _: Guid, _: Range) : unit =
        _acc_cool_text
            .Append(";")
            .AppendLine() |> ignore
            
    
     and EnterBlockLastExpr(_: Expr, _: Guid, _: Range) : unit =
        _acc_cool_text.Append(_indent) |> ignore

     and LeaveBlockLastExpr(_: Expr, _: Guid, _: Range) : unit =
        _acc_cool_text.AppendLine() |> ignore

    
    // Expressions
    // Assign
    // ...    
    
    // If
     and EnterThenBranch(then_branch: Expr, _: Guid, _: Range) : unit =
        match then_branch with
        | Expr.BracedBlock _ -> ()
        | _ ->
            _acc_cool_text.AppendLine() |> ignore
            _indent.Increase()
    
     and LeaveThenBranch(then_branch: Expr, _: Guid, _: Range) : unit =
        match then_branch with
        | Expr.BracedBlock _ -> ()
        | _ ->
            _indent.Decrease()

    
     and EnterElseBranch(else_branch: Expr, _: Guid, _: Range) : unit =
        _acc_cool_text.Append("else") |> ignore
        
        match else_branch with
        | Expr.BracedBlock _ -> ()
        | _ ->
            _acc_cool_text.AppendLine() |> ignore
            _indent.Increase()
    
     and LeaveElseBranch(else_branch: Expr, _: Guid, _: Range) : unit =
        match else_branch with
        | Expr.BracedBlock _ -> ()
        | _ ->
            _indent.Decrease()
    
    
    // While
     and EnterWhileBody(body: Expr, _: Guid, _: Range) : unit =
        match body with
        | Expr.BracedBlock _ -> ()
        | _ ->
            _acc_cool_text.AppendLine() |> ignore
            _indent.Increase()
    
     and LeaveWhileBody(body: Expr, _: Guid, _: Range) : unit =
        match body with
        | Expr.BracedBlock _ -> ()
        | _ ->
            _indent.Decrease()
    
    
    // Match
     and VisitMATCH() : unit = _acc_cool_text.Append(" match ") |> ignore


     and EnterMatchCases(_: Node<Case>, _: Node<Case>[]) : unit =
        _acc_cool_text
            .Append("{")
            .AppendLine() |> ignore
        _indent.Increase()
    
     and LeaveMatchCases(_: Node<Case>, _: Node<Case>[]) : unit =
        _indent.Decrease()
        _acc_cool_text
            .Append(_indent)
            .Append("}")
            .AppendLine() |> ignore

    
    // Match/case
     and EnterMatchCase(_: Case, _: Guid, _: Range) : unit =
        _acc_cool_text
            .Append(_indent)
            .Append("case ") |> ignore
    
    
    // Dispatch
    // Implicit `this` dispatch
    // Super dispatch
     and EnterSuperDispatch(_: Node<ID>, _: Node<Expr>[], _: Guid, _: Range) : unit =
        _acc_cool_text.Append("super.") |> ignore
    
    
    // Object creation
     and EnterObjectCreation(_: Node<TYPE_NAME>, _: Node<Expr>[], _: Guid, _: Range) : unit =
        _acc_cool_text.Append("new ") |> ignore

    
    // Bool negation
     and EnterBoolNegation(_: Node<Expr>, _: Guid, _: Range) : unit =
        _acc_cool_text.Append("!") |> ignore
    
    
    // Compare
     and VisitCOMPARISON_OP(op: CompareOp) : unit =
        match op with
        | CompareOp.LtEq -> _acc_cool_text.Append(" <= ")
        | CompareOp.GtEq -> _acc_cool_text.Append(" >= ")
        | CompareOp.Lt -> _acc_cool_text.Append(" < ")
        | CompareOp.Gt -> _acc_cool_text.Append(" > ")
        | CompareOp.EqEq -> _acc_cool_text.Append(" == ")
        | CompareOp.NotEq -> _acc_cool_text.Append(" != ")
        |> ignore

    
    // Unary minus
     and EnterUnaryMinus(_: Node<Expr>, _: Guid, _: Range) : unit =
        _acc_cool_text.Append("-") |> ignore

    
    // Arith
     and VisitARITH_OP(op: ArithOp) : unit =
        match op with
        | ArithOp.Mul -> _acc_cool_text.Append(" * ")
        | ArithOp.Div -> _acc_cool_text.Append(" / ")
        | ArithOp.Sum -> _acc_cool_text.Append(" + ")
        | ArithOp.Sub -> _acc_cool_text.Append(" - ")
        |> ignore
    
    
    // Braced block    
     and EnterBracedBlock(_: Node<BlockInfo> option, _: Guid, _: Range) : unit =
        _acc_cool_text
            .Append("{")
            .AppendLine() |> ignore
        _indent.Increase()
    
     and LeaveBracedBlock(_: Node<BlockInfo> option, _: Guid, _: Range) : unit =
        _indent.Decrease()
        _acc_cool_text
            .Append("}")
            .AppendLine() |> ignore
    
    
    // Parenthesized expr
     and EnterParensExpr(_:Node<Expr>, _: Guid, _: Range) : unit =
        _acc_cool_text.Append("(") |> ignore
    
     and LeaveParensExpr(_:Node<Expr>, _: Guid, _: Range) : unit =
        _acc_cool_text.Append(")") |> ignore

    
    // Actuals
     and EnterActuals(_: Node<Expr>[]) : unit =
        _acc_cool_text.Append("(") |> ignore
    
     and LeaveActuals(_: Node<Expr>[]) : unit =
        _acc_cool_text.Append(")") |> ignore

    
     and EnterActual(_: Expr, index: int, _: Guid, _: Range) : unit =
        if index > 0
        then do
            _acc_cool_text.Append(", ") |> ignore

    
    // Id
     and VisitId(id: ID, _: Guid, _: Range) : unit =
        _acc_cool_text.Append(id.Value) |> ignore
    
    
    // Literals    
     and VisitInt(literal: INT, _: Guid, _: Range) : unit =
        _acc_cool_text.Append(literal.Value) |> ignore
    

     and VisitStr(literal: STRING, _: Guid, _: Range) : unit =
        _acc_cool_text.Append(literal.Value) |> ignore
    

     and VisitBool(literal: BOOL, _: Guid, _: Range) : unit =
        match literal with
        | BOOL.True -> _acc_cool_text.Append("true")
        | BOOL.False -> _acc_cool_text.Append("false")
        |> ignore
    
     and VisitThis(_: Guid, _: Range) : unit = _acc_cool_text.Append("this") |> ignore
    
     and VisitNull(_: Guid, _: Range) : unit = _acc_cool_text.Append("null") |> ignore
    
     and VisitUnit(_: Guid, _: Range) : unit = _acc_cool_text.Append("()") |> ignore

    
    // Terminals
     and VisitID(id: ID, _: Guid, _: Range) : unit =
        _acc_cool_text.Append(id.Value) |> ignore 
    
    
     and VisitTYPE_NAME(type_name: TYPE_NAME, _: Guid, _: Range) : unit =
        _acc_cool_text.Append(type_name.Value) |> ignore

    
     and VisitCOLON() : unit = _acc_cool_text.Append(": ") |> ignore

     and VisitEQUALS() : unit = _acc_cool_text.Append(" = ") |> ignore

     and VisitNATIVE() : unit = _acc_cool_text.Append("native") |> ignore

     and VisitARROW() : unit = _acc_cool_text.Append(" => ") |> ignore
        
     and VisitDOT() : unit = _acc_cool_text.Append(".") |> ignore

     and VisitNULL() : unit = _acc_cool_text.Append("null") |> ignore

    
    // Actuals
     and walk_actual (actual: Expr, index: int, key: Guid, span: Range): unit =
        EnterActual(actual, index, key, span)
        walk_expr(actual, key, span)


     and walk_actuals (actuals: Node<Expr> []): unit =
        EnterActuals(actuals)
        actuals |> Array.iteri (fun i it -> walk_actual (it.Value, i, it.Key, it.Span))
        LeaveActuals(actuals)


    // Expressions
    // Block
     and walk_var_decl (var_decl_info: VarDeclInfo, key: Guid, span: Range): unit =
        EnterVarDecl(var_decl_info, key, span)
        VisitID(var_decl_info.ID.Value, var_decl_info.ID.Key, var_decl_info.ID.Span)
        VisitCOLON()
        VisitTYPE_NAME
            (var_decl_info.TYPE_NAME.Value, var_decl_info.TYPE_NAME.Key, var_decl_info.TYPE_NAME.Span)
        VisitEQUALS()
        walk_expr(var_decl_info.Expr.Value, var_decl_info.Expr.Key, var_decl_info.Expr.Span)
        LeaveVarDecl(var_decl_info, key, span)


     and walk_stmt_expr (expr: Expr, key: Guid, span: Range): unit =
        EnterStmtExpr(expr, key, span)
        walk_expr(expr, key, span)
        LeaveStmtExpr(expr, key, span)


     and walk_block_info (block_info: BlockInfo, key: Guid, span: Range): unit =
        EnterBlock(block_info, key, span)
        block_info.Stmts
        |> Array.iter (fun it ->
            match it.Value with
            | Stmt.VarDecl var_decl_info -> walk_var_decl (var_decl_info, it.Key, it.Span)
            | Stmt.Expr expr -> walk_stmt_expr (expr, it.Key, it.Span))

        let block_expr = block_info.Expr
    
        EnterBlockLastExpr(block_expr.Value, block_expr.Key, block_expr.Span)
        walk_expr(block_expr.Value, block_expr.Key, block_expr.Span)
        LeaveBlockLastExpr(block_expr.Value, block_expr.Key, block_expr.Span)
        
        LeaveBlock(block_info, key, span)


    // Braced block
     and walk_braced_block (block_info_opt: Node<BlockInfo> option, key: Guid, span: Range): unit =
        EnterBracedBlock(block_info_opt, key, span)
        match block_info_opt with
        | Some block_info ->
            walk_block_info (block_info.Value, block_info.Key, block_info.Span)
        | None ->
            ()
        LeaveBracedBlock(block_info_opt, key, span)


     and walk_block (block: Block, key: Guid, span: Range) =
        match block with
        | (Block.Implicit block_info) ->
            walk_block_info (block_info, key, span)
        | (Block.Braced block_info_opt) ->
            walk_braced_block (block_info_opt, key, span)


    // Assign
     and walk_assign (lvalue: Node<ID>, rvalue: Node<Expr>, key: Guid, span: Range): unit =
        VisitID(lvalue.Value, lvalue.Key, lvalue.Span)
        VisitEQUALS()
        walk_expr(rvalue.Value, rvalue.Key, rvalue.Span)


    // If
    and walk_if (condition: Node<Expr>, then_branch: Node<Expr>, else_branch: Node<Expr>, key: Guid, span: Range): unit =
        // EnterIf(condition, then_branch, else_branch, key, span)

        // EnterIfCond(condition.Value, condition.Key, condition.Span)
        walk_expr(condition.Value, condition.Key, condition.Span)
        // LeaveIfCond(condition.Value, condition.Key, condition.Span)

        EnterThenBranch(then_branch.Value, then_branch.Key, then_branch.Span)
        walk_expr(then_branch.Value, then_branch.Key, then_branch.Span)
        LeaveThenBranch(then_branch.Value, then_branch.Key, then_branch.Span)

        EnterElseBranch(else_branch.Value, else_branch.Key, else_branch.Span)
        walk_expr(else_branch.Value, else_branch.Key, else_branch.Span)
        LeaveElseBranch(else_branch.Value, else_branch.Key, else_branch.Span)


    // While
     and walk_while (condition: Node<Expr>, body: Node<Expr>, key: Guid, span: Range): unit =
        // EnterWhile(condition, body, key, span)

        // EnterWhileCond(condition.Value, condition.Key, condition.Span)
        walk_expr(condition.Value, condition.Key, condition.Span)
        // LeaveWhileCond(condition.Value, condition.Key, condition.Span)

        EnterWhileBody(body.Value, body.Key, body.Span)
        walk_expr(body.Value, body.Key, body.Span)
        LeaveWhileBody(body.Value, body.Key, body.Span)


    // Match/case
     and walk_match_case_pattern (pattern: Pattern, key: Guid, span: Range): unit =
        // EnterMatchCasePattern(pattern, key, span)

        match pattern with
        | Pattern.IdType(node_id, node_type_name) ->
            VisitID(node_id.Value, node_id.Key, node_id.Span)
            VisitCOLON()
            VisitTYPE_NAME(node_type_name.Value, node_type_name.Key, node_type_name.Span)
        | Pattern.Null ->
            VisitNULL()

        // LeaveMatchCasePattern(pattern, key, span)


     and walk_match_case (case: Case, key: Guid, span: Range): unit =
        EnterMatchCase(case, key, span)
        walk_match_case_pattern (case.Pattern.Value, case.Pattern.Key, case.Pattern.Span)
        VisitARROW()
        walk_block (case.Block.Value, case.Block.Key, case.Block.Span)
        // LeaveMatchCase(case, key, span)


     and walk_match_cases (cases_hd: Node<Case>, cases_tl: Node<Case> []): unit =
        EnterMatchCases(cases_hd, cases_tl)
        walk_match_case (cases_hd.Value, cases_hd.Key, cases_hd.Span)
        cases_tl |> Array.iter (fun it -> walk_match_case (it.Value, it.Key, it.Span))
        LeaveMatchCases(cases_hd, cases_tl)


    // Match
     and walk_match (expr: Node<Expr>, cases_hd: Node<Case>, cases_tl: Node<Case> [], key: Guid, span: Range): unit =
        // EnterMatch(expr, cases_hd, cases_tl, key, span)

        // EnterMatchExpr(expr.Value, expr.Key, expr.Span)
        walk_expr(expr.Value, expr.Key, expr.Span)
        // LeaveMatchExpr(expr.Value, expr.Key, expr.Span)

        VisitMATCH()

        walk_match_cases (cases_hd, cases_tl)
        // LeaveMatch(expr, cases_hd, cases_tl, key, span)


    // Dispatch
     and walk_dispatch (obj_expr: Node<Expr>, method_id: Node<ID>, actuals: Node<Expr> [], key: Guid, span: Range): unit =
        // EnterDispatch(obj_expr, method_id, actuals, key, span)
        walk_expr(obj_expr.Value, obj_expr.Key, obj_expr.Span)
        VisitDOT()
        VisitID(method_id.Value, method_id.Key, method_id.Span)
        walk_actuals (actuals)
        // LeaveDispatch(obj_expr, method_id, actuals, key, span)


    // Implicit `this` dispatch
     and walk_implicit_this_dispatch (method_id: Node<ID>, actuals: Node<Expr> [], key: Guid, span: Range): unit =
        // EnterImplicitThisDispatch(method_id, actuals, key, span)
        VisitID(method_id.Value, method_id.Key, method_id.Span)
        walk_actuals (actuals)
        // LeaveImplicitThisDispatch(method_id, actuals, key, span)


    // Super dispatch
     and walk_super_dispatch (method_id: Node<ID>, actuals: Node<Expr> [], key: Guid, span: Range): unit =
        EnterSuperDispatch(method_id, actuals, key, span)
        VisitID(method_id.Value, method_id.Key, method_id.Span)
        walk_actuals (actuals)
        // LeaveSuperDispatch(method_id, actuals, key, span)


    // Object creation
     and walk_object_creation (type_name: Node<TYPE_NAME>, actuals: Node<Expr> [], key: Guid, span: Range): unit =
        EnterObjectCreation(type_name, actuals, key, span)
        VisitTYPE_NAME(type_name.Value, type_name.Key, type_name.Span)
        walk_actuals (actuals)
        // LeaveObjectCreation(type_name, actuals, key, span)


    // Bool negation
     and walk_bool_negation (expr: Node<Expr>, key: Guid, span: Range): unit =
        EnterBoolNegation(expr, key, span)
        walk_expr(expr.Value, expr.Key, expr.Span)
        // LeaveBoolNegation(expr, key, span)


    // Compare
     and walk_comparison (left: Node<Expr>, op: CompareOp, right: Node<Expr>, key: Guid, span: Range): unit =
        // EnterComparison(left, op, right, key, span)
        walk_expr(left.Value, left.Key, left.Span)
        VisitCOMPARISON_OP(op)
        walk_expr(right.Value, right.Key, right.Span)
        // LeaveComparison(left, op, right, key, span)


    // Unary minus
     and walk_unary_minus (expr: Node<Expr>, key: Guid, span: Range): unit =
        EnterUnaryMinus(expr, key, span)
        walk_expr(expr.Value, expr.Key, expr.Span)
        // LeaveUnaryMinus(expr, key, span)


    // Arith
     and walk_arith (left: Node<Expr>, op: ArithOp, right: Node<Expr>, key: Guid, span: Range): unit =
        walk_expr(left.Value, left.Key, left.Span)
        VisitARITH_OP(op)
        walk_expr(right.Value, right.Key, right.Span)


    // Parenthesized expr
     and walk_parens_expr (expr: Node<Expr>, key: Guid, span: Range): unit =
        EnterParensExpr(expr, key, span)
        walk_expr(expr.Value, expr.Key, expr.Span)
        LeaveParensExpr(expr, key, span)


    // Classes
     and walk_var_formal (var_formal: VarFormal, index: int, key: Guid, span: Range): unit =
        EnterVarFormal(var_formal, index, key, span)
        VisitID(var_formal.ID.Value, var_formal.ID.Key, var_formal.ID.Span)
        VisitCOLON()
        VisitTYPE_NAME(var_formal.TYPE_NAME.Value, var_formal.TYPE_NAME.Key, var_formal.TYPE_NAME.Span)
        // LeaveVarFormal


     and walk_var_formals (var_formals: Node<VarFormal> []): unit =
        EnterVarFormals(var_formals)
        var_formals |> Array.iteri (fun i it -> walk_var_formal (it.Value, i, it.Key, it.Span))
        LeaveVarFormals(var_formals)


    // Method
     and walk_formal (formal: Formal, index: int, key: Guid, span: Range): unit =
        EnterFormal(formal, index, key, span)
        VisitID(formal.ID.Value, formal.ID.Key, formal.ID.Span)
        VisitCOLON()
        VisitTYPE_NAME(formal.TYPE_NAME.Value, formal.TYPE_NAME.Key, formal.TYPE_NAME.Span)
        // LeaveFormal


     and walk_formals (formals: Node<Formal> []): unit =
        EnterFormals(formals)
        formals |> Array.iteri (fun i it -> walk_formal (it.Value, i, it.Key, it.Span))
        LeaveFormals(formals)


     and walk_method (method_info: MethodInfo, key: Guid, span: Range): unit =
        EnterMethod(method_info, key, span)
        VisitID(method_info.ID.Value, method_info.ID.Key, method_info.ID.Span)
        walk_formals (method_info.Formals)
        VisitCOLON()
        VisitTYPE_NAME(method_info.TYPE_NAME.Value, method_info.TYPE_NAME.Key, method_info.TYPE_NAME.Span)

        let node_body = method_info.MethodBody
        EnterMethodBody(node_body.Value, node_body.Key, node_body.Span)
        
        match method_info.MethodBody.Value with
        | MethodBody.Expr expr ->
            walk_expr(expr, method_info.MethodBody.Key, method_info.MethodBody.Span)
        | MethodBody.Native ->
            VisitNATIVE()
        
        LeaveMethodBody(node_body.Value, node_body.Key, node_body.Span)
        // LeaveMethod(method_info, key, span)


    // Attribute
     and walk_attr (attr_info: AttrInfo, key: Guid, span: Range): unit =
        EnterAttr(attr_info, key, span)
        VisitID(attr_info.ID.Value, attr_info.ID.Key, attr_info.ID.Span)
        VisitCOLON()
        VisitTYPE_NAME(attr_info.TYPE_NAME.Value, attr_info.TYPE_NAME.Key, attr_info.TYPE_NAME.Span)

        let node_body = attr_info.AttrBody
        EnterAttrBody(node_body.Value, node_body.Key, node_body.Span)
        
        match attr_info.AttrBody.Value with
        | AttrBody.Expr expr ->
            walk_expr(expr, attr_info.AttrBody.Key, attr_info.AttrBody.Span)
        | AttrBody.Native ->
            VisitNATIVE()

        LeaveAttrBody(node_body.Value, node_body.Key, node_body.Span)
        // LeaveAttr(attr_info, key, span)


    // Class
     and walk_extends (extends: Extends, key: Guid, span: Range): unit =
        EnterExtends(extends, key, span)

        match extends with
        | Extends.Info extends_info ->
            VisitTYPE_NAME
                (extends_info.TYPE_NAME.Value, extends_info.TYPE_NAME.Key, extends_info.TYPE_NAME.Span)
            EnterActuals(extends_info.Actuals)
            walk_actuals (extends_info.Actuals)
            LeaveActuals(extends_info.Actuals)
        | Extends.Native ->
            VisitNATIVE()

        // LeaveExtends(extends, key, span)


     and walk_features (features: Node<Feature> []): unit =
        EnterFeatures(features)
        let visit_feature (feature_node: Node<Feature>): unit =
            match feature_node.Value with
            | Feature.Method method_info ->
                walk_method (method_info, feature_node.Key, feature_node.Span)
            | Feature.Attr attr_info ->
                walk_attr (attr_info, feature_node.Key, feature_node.Span)
            | Feature.Block (Block.Implicit block_info) ->
                walk_block_info (block_info, feature_node.Key, feature_node.Span)
            | Feature.Block (Block.Braced block_info_opt) ->
                walk_braced_block (block_info_opt, feature_node.Key, feature_node.Span)

        features |> Array.iter visit_feature
        LeaveFeatures(features)


     and walk_class (klass: ClassDecl, key: Guid, span: Range): unit =
        EnterClass(klass, key, span)
        VisitTYPE_NAME(klass.NAME.Value, klass.NAME.Key, klass.NAME.Span)
        walk_var_formals (klass.VarFormals)

        match klass.Extends with
        | Some extends_node ->
            walk_extends (extends_node.Value, extends_node.Key, extends_node.Span)
        | None ->
            ()

        walk_features (klass.ClassBody)
        // LeaveClass(klass, key, span)


    // Program
    and walk_program (program: Program, key: Guid, span: Range): unit =
        // EnterProgram(program, key, span)

        program.ClassDecls
        |> Array.iter (fun it ->
            EnterClass(it.Value, it.Key, it.Span)
            walk_class (it.Value, it.Key, it.Span)
            (*LeaveClass(it.Value, it.Key, it.Span)*))

        // LeaveProgram(program, key, span)


    // Ast
    and walk_ast (ast: Ast): unit =
        // EnterAst(ast)
        walk_program (ast.Program.Value, ast.Program.Key, ast.Program.Span)
        // LeaveAst(ast)


    // This function has to be declared as a member to support mutually recursive calls
    and walk_expr (expr: Expr, key: Guid, span: Range): unit =
        match expr with
        | Expr.Assign(left, right) ->
            walk_assign (left, right, key, span)
        | Expr.BoolNegation negated_expr ->
            walk_bool_negation (negated_expr, key, span)
        | Expr.UnaryMinus expr ->
            walk_unary_minus (expr, key, span)
        | Expr.If(condition, then_branch, else_branch) ->
            walk_if (condition, then_branch, else_branch, key, span)
        | Expr.While(condition, body) ->
            walk_while (condition, body, key, span)
        | Expr.LtEq(left, right) ->
            walk_comparison (left, CompareOp.LtEq, right, key, span)
        | Expr.GtEq(left, right) ->
            walk_comparison (left, CompareOp.GtEq, right, key, span)
        | Expr.Lt(left, right) ->
            walk_comparison (left, CompareOp.Lt, right, key, span)
        | Expr.Gt(left, right) ->
            walk_comparison (left, CompareOp.Gt, right, key, span)
        | Expr.EqEq(left, right) ->
            walk_comparison (left, CompareOp.EqEq, right, key, span)
        | Expr.NotEq(left, right) ->
            walk_comparison (left, CompareOp.NotEq, right, key, span)
        | Expr.Mul(left, right) ->
            walk_arith (left, ArithOp.Mul, right, key, span)
        | Expr.Div(left, right) ->
            walk_arith (left, ArithOp.Div, right, key, span)
        | Expr.Sum(left, right) ->
            walk_arith (left, ArithOp.Sum, right, key, span)
        | Expr.Sub(left, right) ->
            walk_arith (left, ArithOp.Sub, right, key, span)
        | Expr.Match(expr, cases_hd, cases_tl) ->
            walk_match (expr, cases_hd, cases_tl, key, span)
        | Expr.Dispatch(obj_expr, method_id, actuals) ->
            walk_dispatch (obj_expr, method_id, actuals, key, span)
        // Primary expressions
        | Expr.ImplicitThisDispatch(method_id, actuals) ->
            walk_implicit_this_dispatch (method_id, actuals, key, span)
        | Expr.SuperDispatch(method_id, actuals) ->
            walk_super_dispatch (method_id, actuals, key, span)
        | Expr.ObjectCreation(class_name, actuals) ->
            walk_object_creation (class_name, actuals, key, span)
        | Expr.BracedBlock block_info_opt ->
            walk_braced_block (block_info_opt, key, span)
        | Expr.ParensExpr node_expr ->
            walk_parens_expr (node_expr, key, span)
        | Expr.Id value ->
            VisitId(value, key, span)
        | Expr.Int value ->
            VisitInt(value, key, span)
        | Expr.Str value ->
            VisitStr(value, key, span)
        | Expr.Bool value ->
            VisitBool(value, key, span)
        | Expr.This ->
            VisitThis(key, span)
        | Expr.Null ->
            VisitNull(key, span)
        | Expr.Unit ->
            VisitUnit(key, span)

    
    member private this.Render(ast: Ast): unit = walk_ast (ast)

    
    override this.ToString() = _acc_cool_text.ToString()


    static member Render(ast: Ast) =
        let renderer = CoolRenderer1()
        renderer.Render(ast)
        renderer.ToString()
