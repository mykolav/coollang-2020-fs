namespace Tests.Parser


open System.Text
open LibCool.AstParts.Ast
open Tests.Support


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
type CoolRenderer private () =

    
    let _indent = Indent()
    let _sb_cool = StringBuilder()

    let rec before_braced_block_or_expr (it: Expr) : unit =
        match it with
        | Expr.BracedBlock _ ->
            ()
        | _ ->
            _indent.Increase()
            _sb_cool.AppendLine().Append(_indent).Nop()
   

    and after_braced_block_or_expr (it: Expr) : unit =
        match it with
        | Expr.BracedBlock _ -> ()
        | _                  -> _indent.Decrease()
    
    
    and walk_expr (expr: Expr): unit =
        match expr with
        | Expr.Assign(left, right) ->
            walk_assign (left, right)
        | Expr.BoolNegation negated_expr ->
            walk_bool_negation (negated_expr)
        | Expr.UnaryMinus expr ->
            walk_unary_minus (expr)
        | Expr.If(condition, then_branch, else_branch) ->
            walk_if (condition, then_branch, else_branch)
        | Expr.While(condition, body) ->
            walk_while (condition, body)
        | Expr.LtEq(left, right) ->
            walk_comparison (left, CompareOp.LtEq, right)
        | Expr.GtEq(left, right) ->
            walk_comparison (left, CompareOp.GtEq, right)
        | Expr.Lt(left, right) ->
            walk_comparison (left, CompareOp.Lt, right)
        | Expr.Gt(left, right) ->
            walk_comparison (left, CompareOp.Gt, right)
        | Expr.EqEq(left, right) ->
            walk_comparison (left, CompareOp.EqEq, right)
        | Expr.NotEq(left, right) ->
            walk_comparison (left, CompareOp.NotEq, right)
        | Expr.Mul(left, right) ->
            walk_arith (left, ArithOp.Mul, right)
        | Expr.Div(left, right) ->
            walk_arith (left, ArithOp.Div, right)
        | Expr.Sum(left, right) ->
            walk_arith (left, ArithOp.Sum, right)
        | Expr.Sub(left, right) ->
            walk_arith (left, ArithOp.Sub, right)
        | Expr.Match(expr, cases_hd, cases_tl) ->
            walk_match (expr, cases_hd, cases_tl)
        | Expr.Dispatch(obj_expr, method_id, actuals) ->
            walk_dispatch (obj_expr, method_id, actuals)
        | Expr.ImplicitThisDispatch(method_id, actuals) ->
            walk_implicit_this_dispatch (method_id, actuals)
        | Expr.SuperDispatch(method_id, actuals) ->
            walk_super_dispatch (method_id, actuals)
        | Expr.New(class_name, actuals) ->
            walk_object_creation (class_name, actuals)
        | Expr.BracedBlock block_info_opt ->
            walk_braced_block block_info_opt
        | Expr.ParensExpr node_expr ->
            walk_parens_expr node_expr
        | Expr.Id value ->
            _sb_cool.Append(value).Nop()
        | Expr.Int value ->
            _sb_cool.Append(value).Nop()
        | Expr.Str value ->
            _sb_cool.Append(value).Nop()
        | Expr.Bool value ->
            _sb_cool.Append(value).Nop()
        | Expr.This ->
            _sb_cool.Append("this").Nop()
        | Expr.Null ->
            _sb_cool.Append("null").Nop()
        | Expr.Unit ->
            _sb_cool.Append("()").Nop()

    
    and walk_actual (actual: Expr, index: int): unit =
        if index > 0
        then
            _sb_cool.Append(", ").Nop()

        walk_expr actual


    and walk_actuals (actuals: Node<Expr> []): unit =
        _sb_cool.Append("(").Nop()
        actuals |> Array.iteri (fun i it -> walk_actual (it.Value, i))
        _sb_cool.Append(")").Nop()


    // Expressions
    // Block
    and walk_var_decl (var_decl_info: VarDeclInfo): unit =
        _sb_cool
            .Append("var ")
            .Append(var_decl_info.ID.Value)
            .Append(": ")
            .Append(var_decl_info.TYPE_NAME.Value)
            .Append(" = ")
            .Nop()
        
        walk_expr(var_decl_info.Expr.Value)

        _sb_cool.Append(";")
                .AppendLine().Nop()


     and walk_stmt_expr (expr: Expr): unit =
        walk_expr expr
        _sb_cool.Append(";")
                .AppendLine().Nop()


     and walk_block_info (block_info: BlockInfo): unit =
        block_info.Stmts
        |> Array.iteri (fun i it ->
            if i > 0
            then
                _sb_cool.Append(_indent).Nop()

            match it.Value with
            | Stmt.VarDecl var_decl_info -> walk_var_decl var_decl_info
            | Stmt.Expr expr -> walk_stmt_expr expr)

        let block_expr = block_info.Expr
    
        // If it's the only expression in the block,
        // it's also the first.
        // We don't want to ident the first expression/stmt of the block
        // as the caller already added an indent.
        if block_info.Stmts.Length > 0
        then
            _sb_cool.Append(_indent).Nop()
            
        walk_expr block_expr.Value
        _sb_cool.AppendLine().Nop()


     and walk_braced_block (block_info: BlockInfo voption): unit =
        _sb_cool.Append("{").Nop()
        _indent.Increase()

        let indent = 
            match block_info with
            | ValueSome block_info ->
                _sb_cool.AppendLine().Append(_indent).Nop()
                walk_block_info block_info
                true
            | ValueNone ->
                false
            
        _indent.Decrease()
        _sb_cool
            .Append(if indent then _indent.ToString() else "")
            .Append("}")
            .Nop()


    and walk_block (block: CaseBlock) =
       match block with
       | CaseBlock.Implicit block_info ->
           walk_block_info block_info
       | CaseBlock.Braced block_info_opt ->
           walk_braced_block block_info_opt


    // Assign
    and walk_assign (lvalue: Node<ID>, rvalue: Node<Expr>): unit =
       _sb_cool.Append(lvalue.Value)
                     .Append(" = ").Nop()
       walk_expr rvalue.Value


    // If
    and walk_if (condition: Node<Expr>, then_branch: Node<Expr>, else_branch: Node<Expr>): unit =
        _sb_cool.Append("if (").Nop()
        walk_expr condition.Value
        _sb_cool.Append(") ").Nop()
        
        // Enter 'then' branch.
        before_braced_block_or_expr then_branch.Value
        walk_expr then_branch.Value

        match then_branch.Value with
        | Expr.BracedBlock _ ->
            _sb_cool.Append(" ").Nop()
        | _ ->
            _indent.Decrease()
            _sb_cool.AppendLine().Append(_indent).Nop()
        
        _sb_cool.Append("else ").Nop()
        
        // Enter 'else' branch.        
        before_braced_block_or_expr else_branch.Value
        walk_expr else_branch.Value
        after_braced_block_or_expr else_branch.Value


    // While
    and walk_while (condition: Node<Expr>, body: Node<Expr>): unit =
        _sb_cool.Append("while (").Nop()
        walk_expr condition.Value
        _sb_cool.Append(") ").Nop()
 
        before_braced_block_or_expr body.Value
        walk_expr body.Value
        after_braced_block_or_expr body.Value


    // Match/case
    and walk_match_case_pattern (pattern: Pattern): unit =
       match pattern with
       | Pattern.IdType(node_id, node_type_name) ->
           _sb_cool
               .Append(node_id.Value)
               .Append(": ")
               .Append(node_type_name.Value)
               .Nop()
       | Pattern.Null ->
           _sb_cool.Append("null").Nop()


    and walk_match_case (case: Case): unit =
       _sb_cool.Append(_indent)
               .Append("case ").Nop()
       walk_match_case_pattern case.Pattern.Value
       _sb_cool.Append(" => ").Nop()
       walk_block case.Block.Value


    and walk_match_cases (cases_hd: Node<Case>, cases_tl: Node<Case> []): unit =
       _sb_cool
           .Append("{")
           .AppendLine().Nop()
       _indent.Increase()

       walk_match_case cases_hd.Value
       cases_tl |> Array.iter (fun it -> walk_match_case it.Value)
       
       _indent.Decrease()
       _sb_cool
           .Append(_indent)
           .Append("}").Nop()


    // Match
    and walk_match (expr: Node<Expr>, cases_hd: Node<Case>, cases_tl: Node<Case> []): unit =
        walk_expr expr.Value

        _sb_cool.Append(" match ").Nop()

        walk_match_cases (cases_hd, cases_tl)


    // Dispatch
    and walk_dispatch (obj_expr: Node<Expr>, method_id: Node<ID>, actuals: Node<Expr> []): unit =
        walk_expr obj_expr.Value
        _sb_cool.Append(".")
                .Append(method_id.Value).Nop()
        walk_actuals (actuals)


    // Implicit `this` dispatch
    and walk_implicit_this_dispatch (method_id: Node<ID>, actuals: Node<Expr> []): unit =
        _sb_cool.Append(method_id.Value).Nop()
        walk_actuals actuals


    // Super dispatch
    and walk_super_dispatch (method_id: Node<ID>, actuals: Node<Expr> []): unit =
        _sb_cool.Append("super.")
                .Append(method_id.Value).Nop()
        walk_actuals actuals


    // Object creation
    and walk_object_creation (type_name: Node<TYPE_NAME>, actuals: Node<Expr> []): unit =
        _sb_cool.Append("new ")
                .Append(type_name.Value).Nop()
        walk_actuals (actuals)


    // Bool negation
    and walk_bool_negation (expr: Node<Expr>): unit =
        _sb_cool.Append("!").Nop()
        walk_expr expr.Value


    // Compare
    and walk_comparison (left: Node<Expr>, op: CompareOp, right: Node<Expr>): unit =
        walk_expr left.Value

        (match op with
        | CompareOp.LtEq -> _sb_cool.Append(" <= ")
        | CompareOp.GtEq -> _sb_cool.Append(" >= ")
        | CompareOp.Lt -> _sb_cool.Append(" < ")
        | CompareOp.Gt -> _sb_cool.Append(" > ")
        | CompareOp.EqEq -> _sb_cool.Append(" == ")
        | CompareOp.NotEq -> _sb_cool.Append(" != ")
        ).Nop()

        walk_expr right.Value


    // Unary minus
    and walk_unary_minus (expr: Node<Expr>): unit =
       _sb_cool.Append("-").Nop()
       walk_expr expr.Value


    // Arith
    and walk_arith (left: Node<Expr>, op: ArithOp, right: Node<Expr>): unit =
       walk_expr left.Value

       (match op with
       | ArithOp.Mul -> _sb_cool.Append(" * ")
       | ArithOp.Div -> _sb_cool.Append(" / ")
       | ArithOp.Sum -> _sb_cool.Append(" + ")
       | ArithOp.Sub -> _sb_cool.Append(" - ")
       ).Nop()

       walk_expr right.Value


    // Parenthesized expr
    and walk_parens_expr (expr: Node<Expr>): unit =
        _sb_cool.Append("(").Nop()
        walk_expr expr.Value
        _sb_cool.Append(")").Nop()


    // Classes
    and walk_var_formal (var_formal: VarFormal, index: int): unit =
        _sb_cool.Append(if index > 0 then ", " else "")
                .Append("var ")
                .Append(var_formal.ID.Value)
                .Append(": ")
                .Append(var_formal.TYPE_NAME.Value).Nop()


    and walk_var_formals (var_formals: Node<VarFormal> []): unit =
        _sb_cool.Append("(").Nop()
        var_formals |> Array.iteri (fun i it -> walk_var_formal(it.Value, i))
        _sb_cool.Append(")").Nop()


    // Method
    and walk_formal (formal: Formal, index: int): unit =
         _sb_cool.Append(if index > 0 then ", " else "")
                 .Append(formal.ID.Value)
                 .Append(": ")
                 .Append(formal.TYPE_NAME.Value).Nop()


    and walk_formals (formals: Node<Formal> []): unit =
        _sb_cool.Append("(").Nop()
        formals |> Array.iteri (fun i it -> walk_formal(it.Value, i))
        _sb_cool.Append(")").Nop()


    and walk_method (method_info: MethodInfo): unit =
        _sb_cool.Append(_indent)
                .Append(if method_info.Override then "override def " else "def ")
                .Append(method_info.ID.Value)
                .Nop()
        
        walk_formals method_info.Formals
        
        _sb_cool.Append(": ")
                .Append(method_info.TYPE_NAME.Value)
                .Append(" = ")
                .Nop()
        
        let method_body = method_info.MethodBody.Value
        
        match method_body with
        | MethodBody.Expr expr ->
            before_braced_block_or_expr expr
        | _ ->
            ()
        
        match method_body with
        | MethodBody.Expr expr ->
            walk_expr expr
        | MethodBody.Native ->
            _sb_cool.Append("native").Nop()
        
        match method_body with
        | MethodBody.Expr expr ->
           match expr with
           | Expr.BracedBlock _ -> ()
           | _                  -> _indent.Decrease()
        | _ ->
            ()


    // Attribute
     and walk_attr (attr_info: AttrInfo): unit =
        _sb_cool
            .Append(_indent)
            .Append("var ")
            .Append(attr_info.ID.Value)
            .Append(": ")
            .Append(attr_info.TYPE_NAME.Value).Nop()

        let attr_body = attr_info.AttrBody.Value
        _sb_cool.Append(" = ").Nop()
        
        match attr_body with
        | AttrBody.Expr expr ->
            before_braced_block_or_expr expr
            walk_expr expr
            after_braced_block_or_expr expr            
        | AttrBody.Native ->
            _sb_cool.Append("native").Nop()


    // Class
     and walk_extends (extends: Extends): unit =
        _sb_cool.Append(" extends ").Nop()

        match extends with
        | Extends.Info extends_info ->
            _sb_cool.Append(extends_info.PARENT_NAME.Value).Nop()
            walk_actuals (extends_info.Actuals)
        | Extends.Native ->
            _sb_cool.Append("native").Nop()


     and walk_features (features: Node<Feature> []): unit =
        _sb_cool.Append(" {")
                .AppendLine().Nop()
        _indent.Increase()

        let visit_feature (feature_node: Node<Feature>): unit =
            match feature_node.Value with
            | Feature.Method method_info ->
                walk_method method_info
            | Feature.Attr attr_info ->
                walk_attr attr_info
            | Feature.BracedBlock block_info_opt ->
                _sb_cool.Append(_indent).Nop()
                walk_braced_block block_info_opt

        features |> Array.iter (fun it -> _sb_cool.AppendLine().Nop()
                                          visit_feature it
                                          _sb_cool.Append(";").AppendLine().Nop())
        
        _indent.Decrease()
        _sb_cool.Append(_indent)
                .Append("}").Nop()


     and walk_class (klass: ClassDecl): unit =
        _sb_cool
            .Append(_indent)
            .Append("class ")
            .Nop()

        _sb_cool.Append(klass.NAME.Value.Value).Nop()
        walk_var_formals klass.VarFormals

        match klass.Extends with
        | ValueSome extends_node ->
            walk_extends (extends_node.Value)
        | ValueNone ->
            ()

        walk_features klass.ClassBody


    and walk_program (program: Program): unit =
        program.ClassDecls
        |> Array.iter (fun it -> walk_class it.Value
                                 _sb_cool.AppendLine().AppendLine().Nop())


    member private this.Render(ast: Program): unit =
        walk_program ast

    
    override this.ToString() = _sb_cool.ToString()


    static member Render(ast: Program) =
        let renderer = CoolRenderer()
        renderer.Render(ast)
        renderer.ToString()
