namespace rec LibCool.Frontend

open System
open LibCool.Frontend
open LibCool.SourceParts

[<Sealed>]
type AstWalker private (_listener: AstListener) as this =
    // Actuals
    let walk_actual(actual:Expr, key:Guid, span:HalfOpenRange) : unit = ()
    let walk_actuals(actuals:Node<Expr>[]) : unit = ()

    // Expressions
    // Assign
    let walk_assign(lvalue:Node<ID>, rvalue:Node<Expr>, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterAssign(lvalue, rvalue, key, span)
        _listener.LeaveAssign(lvalue, rvalue, key, span)

    // If
    let walk_if(condition:Node<Expr>, then_branch:Node<Expr>, else_branch:Node<Expr>, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterIf(condition, then_branch, else_branch, key, span)
        _listener.LeaveIf(condition, then_branch, else_branch, key, span)
    
    // While
    let walk_while(condition:Node<Expr>, body:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterWhile(condition, body, key, span)
        _listener.LeaveWhile(condition, body, key, span)
    
    // Match/case
    let walk_match_case_pattern(id:Node<ID>, type_name:Node<TYPE_NAME>, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterMatchCasePattern(id, type_name, key, span)
        _listener.EnterMatchCasePattern(id, type_name, key, span)

    let walk_match_case(case:Case, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterMatchCase(case, key, span)
        _listener.LeaveMatchCase(case, key, span)

    // Match
    let walk_match(expr:Node<Expr>, cases:Cases, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterMatch(expr, cases, key, span)
        _listener.LeaveMatch(expr, cases, key, span)
    
    // Dispatch
    let walk_dispatch(obj_expr:Node<Expr>, method_id:Node<ID>, actuals:Node<Expr>[], key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterDispatch(obj_expr, method_id, actuals, key, span)
        _listener.LeaveDispatch(obj_expr, method_id, actuals, key, span)
    
    // Implicit `this` dispatch
    let walk_implicit_this_dispatch(method_id:Node<ID>, actuals:Node<Expr>[], key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterImplicitThisDispatch(method_id, actuals, key, span)
        _listener.EnterImplicitThisDispatch(method_id, actuals, key, span)
    
    // Super dispatch
    let walk_super_dispatch(method_id:Node<ID>, actuals:Node<Expr>[], key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterSuperDispatch(method_id, actuals, key, span)
        _listener.EnterSuperDispatch(method_id, actuals, key, span)
    
    // Object creation
    let walk_object_creation(type_name:Node<TYPE_NAME>, actuals:Node<Expr>[], key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterObjectCreation(type_name, actuals, key, span)
        _listener.EnterObjectCreation(type_name, actuals, key, span)

    // Bool negation
    let walk_bool_negation(expr:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterBoolNegation(expr, key, span)
        _listener.LeaveBoolNegation(expr, key, span)
    
    // Compare
    let walk_comparison(left:Node<Expr>, op:CompareOp, right:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterComparison(left, op, right, key, span)
        _listener.LeaveComparison(left, op, right, key, span)
    
    // Unary minus
    let walk_unary_minus(expr:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterUnaryMinus(expr, key, span)
        _listener.LeaveUnaryMinus(expr, key, span)

    // Arith
    let walk_arith(left:Node<Expr>, op:ArithOp, right:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterArith(left, op, right, key, span)
        _listener.LeaveArith(left, op, right, key, span)

    // Parenthesized expr
    let walk_parens_expr(expr: Expr, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterParensExpr(expr, key, span)
        _listener.EnterParensExpr(expr, key, span)
    
    // Braced block
    let walk_braced_block(block_info_opt: BlockInfo option, key:Guid, span:HalfOpenRange) : unit = 
        _listener.EnterBracedBlock(block_info_opt, key, span)
        _listener.LeaveBracedBlock(block_info_opt, key, span)
    
    // Classes
    let walk_var_formal(var_formal:VarFormal, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterVarFormal(var_formal, key, span)
        _listener.LeaveVarFormal(var_formal, key, span)

    let walk_var_formals(var_formals:Node<VarFormal>[]) : unit =
        _listener.EnterVarFormals(var_formals)
        _listener.LeaveVarFormals(var_formals)

    // Method
    let walk_formal(formal:Formal, _:Guid, _:HalfOpenRange) : unit =
        _listener.VisitID(formal.ID.Value, formal.ID.Key, formal.ID.Span)
        _listener.VisitCOLON()
        _listener.VisitTYPE_NAME(formal.TYPE_NAME.Value, formal.TYPE_NAME.Key, formal.TYPE_NAME.Span)
    
    let walk_formals(formals:Node<Formal>[]) : unit =
        formals |> Array.iter (fun it -> walk_formal(it.Value, it.Key, it.Span))

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
            _listener.EnterExpr(expr, method_info.MethodBody.Key, method_info.MethodBody.Span)
            this.WalkExpr(expr, method_info.MethodBody.Key, method_info.MethodBody.Span)
            _listener.LeaveExpr(expr, method_info.MethodBody.Key, method_info.MethodBody.Span)
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
            _listener.EnterExpr(expr, attr_info.AttrBody.Key, attr_info.AttrBody.Span)
            this.WalkExpr(expr, attr_info.AttrBody.Key, attr_info.AttrBody.Span)
            _listener.LeaveExpr(expr, attr_info.AttrBody.Key, attr_info.AttrBody.Span)
        | AttrBody.Native ->
            _listener.VisitNATIVE()

        _listener.LeaveAttr(attr_info, key, span)

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
    
    let walk_block(block_info:BlockInfo, key:Guid, span:HalfOpenRange) : unit =
        _listener.EnterBlock(block_info, key, span)
        block_info.Stmts |> Array.iter (fun it ->
            match it.Value with
            | Stmt.VarDecl var_decl_info -> walk_var_decl(var_decl_info, it.Key, it.Span)
            | Stmt.Expr expr -> walk_stmt_expr(expr, it.Key, it.Span))
        
        this.WalkExpr(block_info.Expr.Value, block_info.Expr.Key, block_info.Expr.Span) 
        _listener.LeaveBlock(block_info, key, span)

    let walk_extends(extends:Extends, key:Guid, span:HalfOpenRange) : unit =
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
    
    let walk_features(features: Node<Feature>[]) : unit =
        _listener.EnterFeatures(features)
        let visit_feature (feature_node: Node<Feature>) : unit =
            match feature_node.Value with
            | Feature.Method method_info ->
                walk_method(method_info, feature_node.Key, feature_node.Span)
            | Feature.Attr attr_info ->
                walk_attr(attr_info, feature_node.Key, feature_node.Span)
            | Feature.Block (Block.Info block_info) ->
                walk_block(block_info, feature_node.Key, feature_node.Span)
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
            _listener.EnterExtends(extends_node.Value, extends_node.Key, extends_node.Span)
            walk_extends(extends_node.Value, extends_node.Key, extends_node.Span)
            _listener.LeaveExtends(extends_node.Value, extends_node.Key, extends_node.Span)
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
        _listener.LeaveExpr(expr, key, span)
        
    member private __.Walk(ast: Ast) : unit =
        walk_ast(ast)
        
    static member Walk(ast: Ast, listener: AstListener) : unit =
        let ast_walker = AstWalker(listener)
        ast_walker.Walk(ast)
