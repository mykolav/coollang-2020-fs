namespace LibCool.Frontend

open System
open LibCool.Frontend
open LibCool.SourceParts

[<Sealed>]
type AstWalker(_listener: AstListener) =
    // Actuals
    let walk_actuals(actuals:Node<Expr>[]) : unit = ()
    let walk_actual(actual:Expr, key:Guid, span:HalfOpenRange) : unit = ()

    // Expressions
    let walk_expr(expr:Expr, key:Guid, span:HalfOpenRange) : unit =
        ()

    // Assign
    let walk_assign(lvalue:Node<ID>, rvalue:Node<Expr>, key:Guid, span:HalfOpenRange) : unit =
        ()

    // If
    let walk_if(condition:Node<Expr>, then_branch:Node<Expr>, else_branch:Node<Expr>) : unit =
        ()
    
    // While
    let walk_while(condition:Node<Expr>, body:Node<Expr>) : unit = 
        ()
    
    // Match
    let walk_match(expr:Node<Expr>, cases:Cases) : unit = 
        ()

    // Match/case
    let walk_match_case(case:Case, key:Guid, span:HalfOpenRange) : unit = 
        ()
    let walk_match_case_pattern(id:Node<ID>, type_name:Node<TYPE_NAME>, key:Guid, span:HalfOpenRange) : unit = 
        ()
    
    // Dispatch
    let walk_dispatch(obj_expr:Node<Expr>, method_id:Node<ID>, actuals:Node<Expr>[]) : unit = 
        ()
    
    // Implicit `this` dispatch
    let walk_implicit_this_dispatch(method_id:Node<ID>, actuals:Node<Expr>[]) : unit = 
        ()
    
    // Super dispatch
    let walk_super_dispatch(method_id:Node<ID>, actuals:Node<Expr>[]) : unit = 
        ()
    
    // Object creation
    let walk_object_creation(type_name:Node<TYPE_NAME>, actuals:Node<Expr>[]) : unit = 
        ()

    // Bool negation
    let walk_bool_negation(expr:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = 
        ()
    
    // Compare
    let walk_comparison(left:Node<Expr>, op:CompareOp, right:Node<Expr>) : unit = 
        ()
    
    // Unary minus
    let walk_unary_minus(expr:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = 
        ()

    // Arith
    let walk_arith(left:Node<Expr>, op:ArithOp, right:Node<Expr>) : unit = 
        ()

    // Parenthesized expr
    let walk_parens_expr(expr: Expr, key:Guid, span:HalfOpenRange) : unit = 
        ()
    
    // Braced block
    let walk_braced_block(block_info_opt: BlockInfo option, key:Guid, span:HalfOpenRange) : unit = 
        ()
    
    // Classes
    let walk_var_formals(var_formals:Node<VarFormal>[]) : unit =
        // TODO: Implement...
        ()
    
    let walk_var_formal(var_formal:VarFormal, key:Guid, span:HalfOpenRange) : unit =
        // TODO: Implement...
        ()

    // Method
    let walk_formal(formal:Formal, _:Guid, _:HalfOpenRange) : unit =
        _listener.VisitID(formal.ID.Value, formal.ID.Key, formal.ID.Span)
        _listener.VisitCOLON()
        _listener.VisitTYPE_NAME(formal.TYPE_NAME.Value, formal.TYPE_NAME.Key, formal.TYPE_NAME.Span)
    
    let walk_formals(formals:Node<Formal>[]) : unit =
        formals |> Array.iter (fun it -> walk_formal(it.Value, it.Key, it.Span))

    let walk_method(method_info:MethodInfo, _:Guid, _:HalfOpenRange) : unit =
        _listener.VisitID(method_info.ID.Value, method_info.ID.Key, method_info.ID.Span)
        walk_formals(method_info.Formals)
        _listener.VisitCOLON()
        _listener.VisitTYPE_NAME(method_info.TYPE_NAME.Value, method_info.TYPE_NAME.Key, method_info.TYPE_NAME.Span)
        _listener.VisitEQUALS()
        match method_info.MethodBody.Value with
        | MethodBody.Expr expr ->
            _listener.EnterExpr(expr, method_info.MethodBody.Key, method_info.MethodBody.Span)
            walk_expr(expr, method_info.MethodBody.Key, method_info.MethodBody.Span)
            _listener.LeaveExpr(expr, method_info.MethodBody.Key, method_info.MethodBody.Span)
        | MethodBody.Native ->
            _listener.VisitNATIVE()
    
    // Attribute
    let walk_attr(attr_info:AttrInfo, _:Guid, _:HalfOpenRange) : unit =
        _listener.VisitID(attr_info.ID.Value, attr_info.ID.Key, attr_info.ID.Span)
        _listener.VisitCOLON()
        _listener.VisitTYPE_NAME(attr_info.TYPE_NAME.Value, attr_info.TYPE_NAME.Key, attr_info.TYPE_NAME.Span)
        _listener.VisitEQUALS()
        match attr_info.AttrBody.Value with
        | AttrBody.Expr expr ->
            _listener.EnterExpr(expr, attr_info.AttrBody.Key, attr_info.AttrBody.Span)
            walk_expr(expr, attr_info.AttrBody.Key, attr_info.AttrBody.Span)
            _listener.LeaveExpr(expr, attr_info.AttrBody.Key, attr_info.AttrBody.Span)
        | AttrBody.Native ->
            _listener.VisitNATIVE()

    // Block
    let walk_var_decl(var_decl_info:VarDeclInfo, key:Guid, span:HalfOpenRange) : unit =
        ()

    let walk_stmt_expr(expr:Expr, key:Guid, span:HalfOpenRange) : unit =
        ()
    
    let walk_block(block_info:BlockInfo, key:Guid, span:HalfOpenRange) : unit =
        ()

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
        let visit_feature (feature_node: Node<Feature>) : unit =
            match feature_node.Value with
            | Feature.Method method_info ->
                _listener.EnterMethod(method_info, feature_node.Key, feature_node.Span)
                walk_method(method_info, feature_node.Key, feature_node.Span)
                _listener.LeaveMethod(method_info, feature_node.Key, feature_node.Span)
            | Feature.Attr attr_info ->
                _listener.EnterAttr(attr_info, feature_node.Key, feature_node.Span)
                walk_attr(attr_info, feature_node.Key, feature_node.Span)
                _listener.LeaveAttr(attr_info, feature_node.Key, feature_node.Span)
            | Feature.Block (Block.Info block_info) ->
                _listener.EnterBlock(block_info, feature_node.Key, feature_node.Span)
                walk_block(block_info, feature_node.Key, feature_node.Span)
                _listener.LeaveBlock(block_info, feature_node.Key, feature_node.Span)
            | Feature.Block (Block.Braced block_info_opt) ->
                _listener.EnterBracedBlock(block_info_opt, feature_node.Key, feature_node.Span)
                walk_braced_block(block_info_opt, feature_node.Key, feature_node.Span)
                _listener.LeaveBracedBlock(block_info_opt, feature_node.Key, feature_node.Span)
            
        features |> Array.iter visit_feature
    
    let walk_class(klass:ClassDecl, _:Guid, _:HalfOpenRange) : unit =
        _listener.VisitTYPE_NAME(klass.TYPE_NAME.Value, klass.TYPE_NAME.Key, klass.TYPE_NAME.Span)
        walk_var_formals(klass.VarFormals)

        match klass.Extends with
        | Some extends_node ->
            _listener.EnterExtends(extends_node.Value, extends_node.Key, extends_node.Span)
            walk_extends(extends_node.Value, extends_node.Key, extends_node.Span)
            _listener.LeaveExtends(extends_node.Value, extends_node.Key, extends_node.Span)
        | None ->
            ()
            
        _listener.EnterFeatures(klass.ClassBody)
        walk_features(klass.ClassBody)
        _listener.LeaveFeatures(klass.ClassBody)

    // Program
    let walk_program(program:Program, _:Guid, _:HalfOpenRange) : unit =
        program.ClassDecls |> Array.iter (fun it ->
            _listener.EnterClass(it.Value, it.Key, it.Span)
            walk_class(it.Value, it.Key, it.Span)
            _listener.LeaveClass(it.Value, it.Key, it.Span))

    // Ast
    let walk_ast(ast: Ast) : unit =
        _listener.EnterAst(ast)
        walk_program(ast.Program.Value, ast.Program.Key, ast.Program.Span)
        _listener.LeaveAst(ast)
        
    member _.Walk(ast: Ast) : unit =
        walk_ast(ast)
        
    static member Walk(ast: Ast, listener: AstListener) : unit =
        let ast_walker = AstWalker(listener)
        ast_walker.Walk(ast)
