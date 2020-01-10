namespace rec LibCool.Frontend

open System
open LibCool.Frontend
open LibCool.SourceParts

[<Sealed>]
type AstWalker private (_listener: AstListener) as this =
    // Actuals
    let walk_actual(actual:Expr, index:int, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterActual(actual, index, key, span)
        _listener.LeaveActual(actual, index, key, span)
        
    let walk_actuals(actuals:Node<Expr>[]) : unit =
        _listener.EnterActuals(actuals)
        actuals |> Array.iteri (fun i it -> walk_actual(it.Value, i, it.Key, it.Span))
        _listener.LeaveActuals(actuals)

    // Expressions
    // Block
    let walk_var_decl(var_decl_info:VarDeclInfo, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterVarDecl(var_decl_info, key, span)
        _listener.VisitID(var_decl_info.ID.Value, var_decl_info.ID.Key, var_decl_info.ID.Span)
        _listener.VisitCOLON()
        _listener.VisitTYPE_NAME(var_decl_info.TYPE_NAME.Value,
                                 var_decl_info.TYPE_NAME.Key,
                                 var_decl_info.TYPE_NAME.Span)
        _listener.VisitEQUALS()
        this.WalkExpr(var_decl_info.Expr.Value, var_decl_info.Expr.Key, var_decl_info.Expr.Span)
        _listener.LeaveVarDecl(var_decl_info, key, span)

    let walk_stmt_expr(expr:Expr, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterStmtExpr(expr, key, span)
        this.WalkExpr(expr, key, span)
        _listener.LeaveStmtExpr(expr, key, span)
    
    let walk_block_info(block_info:BlockInfo, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterBlock(block_info, key, span)
        block_info.Stmts |> Array.iter (fun it ->
            match it.Value with
            | Stmt.VarDecl var_decl_info -> walk_var_decl(var_decl_info, it.Key, it.Span)
            | Stmt.Expr expr -> walk_stmt_expr(expr, it.Key, it.Span))
        
        this.WalkExpr(block_info.Expr.Value, block_info.Expr.Key, block_info.Expr.Span) 
        _listener.LeaveBlock(block_info, key, span)
        
    // Braced block
    let walk_braced_block(block_info_opt: Node<BlockInfo> option, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterBracedBlock(block_info_opt, key, span)
        match block_info_opt with
        | Some block_info ->
            walk_block_info(block_info.Value, block_info.Key, block_info.Span)
        | None ->
            ()
        _listener.LeaveBracedBlock(block_info_opt, key, span)
        
    let walk_block(block:Block, key:Guid, span:HalfOpenRange) =
        match block with
        | (Block.Info block_info) ->
            walk_block_info(block_info, key, span)
        | (Block.Braced block_info_opt) ->
            walk_braced_block(block_info_opt, key, span)
    
    // Assign
    let walk_assign(lvalue:Node<ID>, rvalue:Node<Expr>, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterAssign(lvalue, rvalue, key, span)
        _listener.VisitID(lvalue.Value, lvalue.Key, lvalue.Span)
        _listener.VisitEQUALS()
        this.WalkExpr(rvalue.Value, rvalue.Key, rvalue.Span)
        _listener.LeaveAssign(lvalue, rvalue, key, span)

    // If
    let walk_if(condition:Node<Expr>, then_branch:Node<Expr>, else_branch:Node<Expr>, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterIf(condition, then_branch, else_branch, key, span)
        
        _listener.EnterIfCond(condition.Value, condition.Key, condition.Span)
        this.WalkExpr(condition.Value, condition.Key, condition.Span)
        _listener.LeaveIfCond(condition.Value, condition.Key, condition.Span)
        
        _listener.EnterThenBranch(then_branch.Value, then_branch.Key, then_branch.Span)
        this.WalkExpr(then_branch.Value, then_branch.Key, then_branch.Span)
        _listener.LeaveThenBranch(then_branch.Value, then_branch.Key, then_branch.Span)
        
        _listener.EnterElseBranch(else_branch.Value, else_branch.Key, else_branch.Span)
        this.WalkExpr(else_branch.Value, else_branch.Key, else_branch.Span)
        _listener.LeaveElseBranch(else_branch.Value, else_branch.Key, else_branch.Span)
        
        _listener.LeaveIf(condition, then_branch, else_branch, key, span)
    
    // While
    let walk_while(condition:Node<Expr>, body:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterWhile(condition, body, key, span)
        
        _listener.EnterWhileCond(condition.Value, condition.Key, condition.Span)
        this.WalkExpr(condition.Value, condition.Key, condition.Span)
        _listener.LeaveWhileCond(condition.Value, condition.Key, condition.Span)
        
        _listener.EnterWhileBody(body.Value, body.Key, body.Span)
        this.WalkExpr(body.Value, body.Key, body.Span)
        _listener.LeaveWhileBody(body.Value, body.Key, body.Span)
        
        _listener.LeaveWhile(condition, body, key, span)
    
    // Match/case
    let walk_match_case_pattern(pattern:Pattern, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterMatchCasePattern(pattern, key, span)
        
        match pattern with
        | Pattern.IdType (node_id, node_type_name) ->
            _listener.VisitID(node_id.Value, node_id.Key, node_id.Span)
            _listener.VisitCOLON()
            _listener.VisitTYPE_NAME(node_type_name.Value, node_type_name.Key, node_type_name.Span)
        | Pattern.Null ->
            _listener.VisitNULL()
        
        _listener.EnterMatchCasePattern(pattern, key, span)

    let walk_match_case(case:Case, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterMatchCase(case, key, span)
        walk_match_case_pattern(case.Pattern.Value, case.Pattern.Key, case.Pattern.Span)
        _listener.VisitARROW()
        walk_block(case.Block.Value, case.Block.Key, case.Block.Span)
        _listener.LeaveMatchCase(case, key, span)

    let walk_match_cases(cases_hd:Node<Case>, cases_tl:Node<Case>[]) : unit = 
        _listener.EnterMatchCases(cases_hd, cases_tl)
        walk_match_case(cases_hd.Value, cases_hd.Key, cases_hd.Span)
        cases_tl |> Array.iter (fun it -> walk_match_case(it.Value, it.Key, it.Span)) 
        _listener.LeaveMatchCases(cases_hd, cases_tl)

    // Match
    let walk_match(expr:Node<Expr>, cases_hd:Node<Case>, cases_tl:Node<Case>[], key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterMatch(expr, cases_hd, cases_tl, key, span)
        this.WalkExpr(expr.Value, expr.Key, expr.Span)
        _listener.VisitWITH()
        walk_match_cases(cases_hd, cases_tl)
        _listener.LeaveMatch(expr, cases_hd, cases_tl, key, span)
    
    // Dispatch
    let walk_dispatch(obj_expr:Node<Expr>, method_id:Node<ID>, actuals:Node<Expr>[], key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterDispatch(obj_expr, method_id, actuals, key, span)
        this.WalkExpr(obj_expr.Value, obj_expr.Key, obj_expr.Span)
        _listener.VisitDOT()
        _listener.VisitID(method_id.Value, method_id.Key, method_id.Span)
        walk_actuals(actuals)
        _listener.LeaveDispatch(obj_expr, method_id, actuals, key, span)
    
    // Implicit `this` dispatch
    let walk_implicit_this_dispatch(method_id:Node<ID>, actuals:Node<Expr>[], key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterImplicitThisDispatch(method_id, actuals, key, span)
        _listener.VisitID(method_id.Value, method_id.Key, method_id.Span)
        walk_actuals(actuals)
        _listener.EnterImplicitThisDispatch(method_id, actuals, key, span)
    
    // Super dispatch
    let walk_super_dispatch(method_id:Node<ID>, actuals:Node<Expr>[], key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterSuperDispatch(method_id, actuals, key, span)
        _listener.VisitID(method_id.Value, method_id.Key, method_id.Span)
        walk_actuals(actuals)
        _listener.EnterSuperDispatch(method_id, actuals, key, span)
    
    // Object creation
    let walk_object_creation(type_name:Node<TYPE_NAME>, actuals:Node<Expr>[], key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterObjectCreation(type_name, actuals, key, span)
        _listener.VisitTYPE_NAME(type_name.Value, type_name.Key, type_name.Span)
        walk_actuals(actuals)
        _listener.EnterObjectCreation(type_name, actuals, key, span)

    // Bool negation
    let walk_bool_negation(expr:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterBoolNegation(expr, key, span)
        this.WalkExpr(expr.Value, expr.Key, expr.Span)
        _listener.LeaveBoolNegation(expr, key, span)
    
    // Compare
    let walk_comparison(left:Node<Expr>, op:CompareOp, right:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterComparison(left, op, right, key, span)
        this.WalkExpr(left.Value, left.Key, left.Span)
        _listener.VisitCOMPARISON_OP(op)
        this.WalkExpr(right.Value, right.Key, right.Span)
        _listener.LeaveComparison(left, op, right, key, span)
    
    // Unary minus
    let walk_unary_minus(expr:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterUnaryMinus(expr, key, span)
        this.WalkExpr(expr.Value, expr.Key, expr.Span)
        _listener.LeaveUnaryMinus(expr, key, span)

    // Arith
    let walk_arith(left:Node<Expr>, op:ArithOp, right:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterArith(left, op, right, key, span)
        this.WalkExpr(left.Value, left.Key, left.Span)
        _listener.VisitARITH_OP(op)
        this.WalkExpr(right.Value, right.Key, right.Span)
        _listener.LeaveArith(left, op, right, key, span)

    // Parenthesized expr
    let walk_parens_expr(expr: Node<Expr>, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterParensExpr(expr, key, span)
        this.WalkExpr(expr.Value, expr.Key, expr.Span)
        _listener.EnterParensExpr(expr, key, span)
    
    // Classes
    let walk_var_formal(var_formal:VarFormal, index:int, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterVarFormal(var_formal, index, key, span)
        _listener.VisitID(var_formal.ID.Value, var_formal.ID.Key, var_formal.ID.Span)
        _listener.VisitCOLON()
        _listener.VisitTYPE_NAME(var_formal.TYPE_NAME.Value, var_formal.TYPE_NAME.Key, var_formal.TYPE_NAME.Span)
        _listener.LeaveVarFormal(var_formal, index, key, span)

    let walk_var_formals(var_formals:Node<VarFormal>[]) : unit =
        _listener.EnterVarFormals(var_formals)
        var_formals |> Array.iteri (fun i it -> walk_var_formal(it.Value, i, it.Key, it.Span))
        _listener.LeaveVarFormals(var_formals)

    // Method
    let walk_formal(formal:Formal, index:int, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterFormal(formal, index, key, span)
        _listener.VisitID(formal.ID.Value, formal.ID.Key, formal.ID.Span)
        _listener.VisitCOLON()
        _listener.VisitTYPE_NAME(formal.TYPE_NAME.Value, formal.TYPE_NAME.Key, formal.TYPE_NAME.Span)
        _listener.LeaveFormal(formal, index, key, span)
    
    let walk_formals(formals:Node<Formal>[]) : unit =
        _listener.EnterFormals(formals)
        formals |> Array.iteri (fun i it -> walk_formal(it.Value, i, it.Key, it.Span))
        _listener.LeaveFormals(formals)

    let walk_method(method_info:MethodInfo, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterMethod(method_info, key, span)
        _listener.VisitID(method_info.ID.Value, method_info.ID.Key, method_info.ID.Span)
        walk_formals(method_info.Formals)
        _listener.VisitCOLON()
        _listener.VisitTYPE_NAME(method_info.TYPE_NAME.Value, method_info.TYPE_NAME.Key, method_info.TYPE_NAME.Span)
        _listener.VisitEQUALS()
        _listener.LeaveMethod(method_info, key, span)

        match method_info.MethodBody.Value with
        | MethodBody.Expr expr ->
            this.WalkExpr(expr, method_info.MethodBody.Key, method_info.MethodBody.Span)
        | MethodBody.Native ->
            _listener.VisitNATIVE()
    
    // Attribute
    let walk_attr(attr_info:AttrInfo, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterAttr(attr_info, key, span)
        _listener.VisitID(attr_info.ID.Value, attr_info.ID.Key, attr_info.ID.Span)
        _listener.VisitCOLON()
        _listener.VisitTYPE_NAME(attr_info.TYPE_NAME.Value, attr_info.TYPE_NAME.Key, attr_info.TYPE_NAME.Span)
        _listener.VisitEQUALS()

        match attr_info.AttrBody.Value with
        | AttrBody.Expr expr ->
            this.WalkExpr(expr, attr_info.AttrBody.Key, attr_info.AttrBody.Span)
        | AttrBody.Native ->
            _listener.VisitNATIVE()

        _listener.LeaveAttr(attr_info, key, span)

    // Class
    let walk_extends(extends:Extends, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterExtends(extends, key, span)
        
        match extends with
        | Extends.Info extends_info ->
            _listener.VisitTYPE_NAME(extends_info.TYPE_NAME.Value,
                                extends_info.TYPE_NAME.Key,
                                extends_info.TYPE_NAME.Span)
            _listener.EnterActuals(extends_info.Actuals)
            walk_actuals(extends_info.Actuals)
            _listener.LeaveActuals(extends_info.Actuals)
        | Extends.Native ->
            _listener.VisitNATIVE()
        
        _listener.LeaveExtends(extends, key, span)
    
    let walk_features(features: Node<Feature>[]) : unit =
        _listener.EnterFeatures(features)
        let visit_feature (feature_node: Node<Feature>) : unit =
            match feature_node.Value with
            | Feature.Method method_info ->
                walk_method(method_info, feature_node.Key, feature_node.Span)
            | Feature.Attr attr_info ->
                walk_attr(attr_info, feature_node.Key, feature_node.Span)
            | Feature.Block (Block.Info block_info) ->
                walk_block_info(block_info, feature_node.Key, feature_node.Span)
            | Feature.Block (Block.Braced block_info_opt) ->
                walk_braced_block(block_info_opt, feature_node.Key, feature_node.Span)
            
        features |> Array.iter visit_feature
        _listener.LeaveFeatures(features)
    
    let walk_class(klass:ClassDecl, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterClass(klass, key, span)
        _listener.VisitTYPE_NAME(klass.TYPE_NAME.Value, klass.TYPE_NAME.Key, klass.TYPE_NAME.Span)
        walk_var_formals(klass.VarFormals)

        match klass.Extends with
        | Some extends_node ->
            walk_extends(extends_node.Value, extends_node.Key, extends_node.Span)
        | None ->
            ()
            
        walk_features(klass.ClassBody)
        _listener.LeaveClass(klass, key, span)

    // Program
    let walk_program(program:Program, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterProgram(program, key, span)
        
        program.ClassDecls |> Array.iter (fun it ->
            _listener.EnterClass(it.Value, it.Key, it.Span)
            walk_class(it.Value, it.Key, it.Span)
            _listener.LeaveClass(it.Value, it.Key, it.Span))
        
        _listener.LeaveProgram(program, key, span)

    // Ast
    let walk_ast(ast: Ast) : unit =
        _listener.EnterAst(ast)
        walk_program(ast.Program.Value, ast.Program.Key, ast.Program.Span)
        _listener.LeaveAst(ast)
        
    // This function has to be declared as a member to support mutually recursive calls
    member private __.WalkExpr(expr:Expr, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterExpr(expr, key, span)
        
        match expr with
        | Expr.Assign (left, right) ->
            walk_assign(left, right, key, span)
        | Expr.BoolNegation negated_expr ->
            walk_bool_negation(negated_expr, key, span)
        | Expr.UnaryMinus expr ->
            walk_unary_minus(expr, key, span)
        | Expr.If (condition, then_branch, else_branch) ->
            walk_if(condition, then_branch, else_branch, key, span)
        | Expr.While (condition, body) ->
            walk_while(condition, body, key, span)
        | Expr.LtEq (left, right) ->
            walk_comparison(left, CompareOp.LtEq, right, key, span)
        | Expr.GtEq (left, right) ->
            walk_comparison(left, CompareOp.GtEq, right, key, span)
        | Expr.Lt (left, right) ->
            walk_comparison(left, CompareOp.Lt, right, key, span)
        | Expr.Gt (left, right) ->
            walk_comparison(left, CompareOp.Gt, right, key, span)
        | Expr.EqEq (left, right) ->
            walk_comparison(left, CompareOp.EqEq, right, key, span)
        | Expr.NotEq (left, right) ->
            walk_comparison(left, CompareOp.NotEq, right, key, span)
        | Expr.Mul (left, right) ->
            walk_arith(left, ArithOp.Mul, right, key, span)
        | Expr.Div (left, right) ->
            walk_arith(left, ArithOp.Div, right, key, span)
        | Expr.Sum (left, right) ->
            walk_arith(left, ArithOp.Sum, right, key, span)
        | Expr.Sub (left, right) ->
            walk_arith(left, ArithOp.Sub, right, key, span)
        | Expr.Match (expr, cases_hd, cases_tl) ->
            walk_match(expr, cases_hd, cases_tl, key, span)
        | Expr.Dispatch (obj_expr, method_id, actuals) ->
            walk_dispatch(obj_expr, method_id, actuals, key, span)
        // Primary expressions
        | Expr.ImplicitThisDispatch (method_id, actuals) ->
            walk_implicit_this_dispatch(method_id, actuals, key, span)
        | Expr.SuperDispatch (method_id, actuals) ->
            walk_super_dispatch(method_id, actuals, key, span)
        | Expr.ObjectCreation (class_name, actuals) ->
            walk_object_creation(class_name, actuals, key, span)
        | Expr.BracedBlock block_info_opt ->
            walk_braced_block(block_info_opt, key, span)
        | Expr.ParensExpr node_expr ->
            walk_parens_expr(node_expr, key, span)
        | Expr.Id value ->
            _listener.VisitId(value)
        | Expr.Int value ->
            _listener.VisitInt(value)
        | Expr.Str value ->
            _listener.VisitStr(value)
        | Expr.Bool value ->
            _listener.VisitBool(value)
        | Expr.This ->
            _listener.VisitThis()
        | Expr.Null ->
            _listener.VisitNull()
        | Expr.Unit ->
            _listener.VisitUnit()
        
        _listener.LeaveExpr(expr, key, span)
        
    member private __.Walk(ast: Ast) : unit =
        walk_ast(ast)
        
    static member Walk(ast: Ast, listener: AstListener) : unit =
        let ast_walker = AstWalker(listener)
        ast_walker.Walk(ast)
