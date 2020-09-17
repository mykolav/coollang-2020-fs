namespace Tests.Parser


open System.Text
open LibCool.AstParts
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

    let rec before_braced_block_or_expr (it: ExprSyntax) : unit =
        match it with
        | ExprSyntax.BracedBlock _ ->
            ()
        | _ ->
            _indent.Increase()
            _sb_cool.AppendLine().Append(_indent).Nop()
   

    and after_braced_block_or_expr (it: ExprSyntax) : unit =
        match it with
        | ExprSyntax.BracedBlock _ -> ()
        | _                  -> _indent.Decrease()
    
    
    and walk_expr (expr: ExprSyntax): unit =
        match expr with
        | ExprSyntax.Assign(left, right) ->
            walk_assign (left, right)
        | ExprSyntax.BoolNegation negated_expr ->
            walk_bool_negation (negated_expr)
        | ExprSyntax.UnaryMinus expr ->
            walk_unary_minus (expr)
        | ExprSyntax.If(condition, then_branch, else_branch) ->
            walk_if (condition, then_branch, else_branch)
        | ExprSyntax.While(condition, body) ->
            walk_while (condition, body)
        | ExprSyntax.LtEq(left, right) ->
            walk_comparison (left, CompareOp.LtEq, right)
        | ExprSyntax.GtEq(left, right) ->
            walk_comparison (left, CompareOp.GtEq, right)
        | ExprSyntax.Lt(left, right) ->
            walk_comparison (left, CompareOp.Lt, right)
        | ExprSyntax.Gt(left, right) ->
            walk_comparison (left, CompareOp.Gt, right)
        | ExprSyntax.EqEq(left, right) ->
            walk_comparison (left, CompareOp.EqEq, right)
        | ExprSyntax.NotEq(left, right) ->
            walk_comparison (left, CompareOp.NotEq, right)
        | ExprSyntax.Mul(left, right) ->
            walk_arith (left, ArithOp.Mul, right)
        | ExprSyntax.Div(left, right) ->
            walk_arith (left, ArithOp.Div, right)
        | ExprSyntax.Sum(left, right) ->
            walk_arith (left, ArithOp.Sum, right)
        | ExprSyntax.Sub(left, right) ->
            walk_arith (left, ArithOp.Sub, right)
        | ExprSyntax.Match(expr, cases_hd, cases_tl) ->
            walk_match (expr, cases_hd, cases_tl)
        | ExprSyntax.Dispatch(obj_expr, method_id, actuals) ->
            walk_dispatch (obj_expr, method_id, actuals)
        | ExprSyntax.ImplicitThisDispatch(method_id, actuals) ->
            walk_implicit_this_dispatch (method_id, actuals)
        | ExprSyntax.SuperDispatch(method_id, actuals) ->
            walk_super_dispatch (method_id, actuals)
        | ExprSyntax.New(class_name, actuals) ->
            walk_object_creation (class_name, actuals)
        | ExprSyntax.BracedBlock block_info_opt ->
            walk_braced_block block_info_opt
        | ExprSyntax.ParensExpr node_expr ->
            walk_parens_expr node_expr
        | ExprSyntax.Id value ->
            _sb_cool.Append(value).Nop()
        | ExprSyntax.Int value ->
            _sb_cool.Append(value).Nop()
        | ExprSyntax.Str value ->
            _sb_cool.Append(value).Nop()
        | ExprSyntax.Bool value ->
            _sb_cool.Append(value).Nop()
        | ExprSyntax.This ->
            _sb_cool.Append("this").Nop()
        | ExprSyntax.Null ->
            _sb_cool.Append("null").Nop()
        | ExprSyntax.Unit ->
            _sb_cool.Append("()").Nop()

    
    and walk_actual (actual: ExprSyntax, index: int): unit =
        if index > 0
        then
            _sb_cool.Append(", ").Nop()

        walk_expr actual


    and walk_actuals (actuals: AstNode<ExprSyntax> []): unit =
        _sb_cool.Append("(").Nop()
        actuals |> Array.iteri (fun i it -> walk_actual (it.Value, i))
        _sb_cool.Append(")").Nop()


    // Expressions
    // Block
    and walk_var_decl (var_decl_info: VarDeclSyntax): unit =
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


     and walk_stmt_expr (expr: ExprSyntax): unit =
        walk_expr expr
        _sb_cool.Append(";")
                .AppendLine().Nop()


     and walk_block_info (block_info: BlockSyntax): unit =
        block_info.Stmts
        |> Array.iteri (fun i it ->
            if i > 0
            then
                _sb_cool.Append(_indent).Nop()

            match it.Value with
            | StmtSyntax.VarDecl var_decl_info -> walk_var_decl var_decl_info
            | StmtSyntax.Expr expr -> walk_stmt_expr expr)

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


     and walk_braced_block (block_info: BlockSyntax voption): unit =
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


    and walk_block (block: CaseBlockSyntax) =
       match block with
       | CaseBlockSyntax.Implicit block_info ->
           walk_block_info block_info
       | CaseBlockSyntax.BracedBlock block_info_opt ->
           walk_braced_block block_info_opt


    // Assign
    and walk_assign (lvalue: AstNode<ID>, rvalue: AstNode<ExprSyntax>): unit =
       _sb_cool.Append(lvalue.Value)
                     .Append(" = ").Nop()
       walk_expr rvalue.Value


    // If
    and walk_if (condition: AstNode<ExprSyntax>, then_branch: AstNode<ExprSyntax>, else_branch: AstNode<ExprSyntax>): unit =
        _sb_cool.Append("if (").Nop()
        walk_expr condition.Value
        _sb_cool.Append(") ").Nop()
        
        // Enter 'then' branch.
        before_braced_block_or_expr then_branch.Value
        walk_expr then_branch.Value

        match then_branch.Value with
        | ExprSyntax.BracedBlock _ ->
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
    and walk_while (condition: AstNode<ExprSyntax>, body: AstNode<ExprSyntax>): unit =
        _sb_cool.Append("while (").Nop()
        walk_expr condition.Value
        _sb_cool.Append(") ").Nop()
 
        before_braced_block_or_expr body.Value
        walk_expr body.Value
        after_braced_block_or_expr body.Value


    // Match/case
    and walk_match_case_pattern (pattern: PatternSyntax): unit =
       match pattern with
       | PatternSyntax.IdType(node_id, node_type_name) ->
           _sb_cool
               .Append(node_id.Value)
               .Append(": ")
               .Append(node_type_name.Value)
               .Nop()
       | PatternSyntax.Null ->
           _sb_cool.Append("null").Nop()


    and walk_match_case (case: CaseSyntax): unit =
       _sb_cool.Append(_indent)
               .Append("case ").Nop()
       walk_match_case_pattern case.Pattern.Value
       _sb_cool.Append(" => ").Nop()
       walk_block case.Block.Value


    and walk_match_cases (cases_hd: AstNode<CaseSyntax>, cases_tl: AstNode<CaseSyntax> []): unit =
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
    and walk_match (expr: AstNode<ExprSyntax>, cases_hd: AstNode<CaseSyntax>, cases_tl: AstNode<CaseSyntax> []): unit =
        walk_expr expr.Value

        _sb_cool.Append(" match ").Nop()

        walk_match_cases (cases_hd, cases_tl)


    // Dispatch
    and walk_dispatch (obj_expr: AstNode<ExprSyntax>, method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        walk_expr obj_expr.Value
        _sb_cool.Append(".")
                .Append(method_id.Value).Nop()
        walk_actuals (actuals)


    // Implicit `this` dispatch
    and walk_implicit_this_dispatch (method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        _sb_cool.Append(method_id.Value).Nop()
        walk_actuals actuals


    // Super dispatch
    and walk_super_dispatch (method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        _sb_cool.Append("super.")
                .Append(method_id.Value).Nop()
        walk_actuals actuals


    // Object creation
    and walk_object_creation (type_name: AstNode<TYPENAME>, actuals: AstNode<ExprSyntax> []): unit =
        _sb_cool.Append("new ")
                .Append(type_name.Value).Nop()
        walk_actuals (actuals)


    // Bool negation
    and walk_bool_negation (expr: AstNode<ExprSyntax>): unit =
        _sb_cool.Append("!").Nop()
        walk_expr expr.Value


    // Compare
    and walk_comparison (left: AstNode<ExprSyntax>, op: CompareOp, right: AstNode<ExprSyntax>): unit =
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
    and walk_unary_minus (expr: AstNode<ExprSyntax>): unit =
       _sb_cool.Append("-").Nop()
       walk_expr expr.Value


    // Arith
    and walk_arith (left: AstNode<ExprSyntax>, op: ArithOp, right: AstNode<ExprSyntax>): unit =
       walk_expr left.Value

       (match op with
       | ArithOp.Mul -> _sb_cool.Append(" * ")
       | ArithOp.Div -> _sb_cool.Append(" / ")
       | ArithOp.Sum -> _sb_cool.Append(" + ")
       | ArithOp.Sub -> _sb_cool.Append(" - ")
       ).Nop()

       walk_expr right.Value


    // Parenthesized expr
    and walk_parens_expr (expr: AstNode<ExprSyntax>): unit =
        _sb_cool.Append("(").Nop()
        walk_expr expr.Value
        _sb_cool.Append(")").Nop()


    // Classes
    and walk_var_formal (var_formal: VarFormalSyntax, index: int): unit =
        _sb_cool.Append(if index > 0 then ", " else "")
                .Append("var ")
                .Append(var_formal.ID.Value)
                .Append(": ")
                .Append(var_formal.TYPE.Value).Nop()


    and walk_var_formals (var_formals: AstNode<VarFormalSyntax> []): unit =
        _sb_cool.Append("(").Nop()
        var_formals |> Array.iteri (fun i it -> walk_var_formal(it.Value, i))
        _sb_cool.Append(")").Nop()


    // Method
    and walk_formal (formal: FormalSyntax, index: int): unit =
         _sb_cool.Append(if index > 0 then ", " else "")
                 .Append(formal.ID.Value)
                 .Append(": ")
                 .Append(formal.TYPE.Value).Nop()


    and walk_formals (formals: AstNode<FormalSyntax> []): unit =
        _sb_cool.Append("(").Nop()
        formals |> Array.iteri (fun i it -> walk_formal(it.Value, i))
        _sb_cool.Append(")").Nop()


    and walk_method (method_info: MethodSyntax): unit =
        _sb_cool.Append(_indent)
                .Append(if method_info.Override then "override def " else "def ")
                .Append(method_info.ID.Value)
                .Nop()
        
        walk_formals method_info.Formals
        
        _sb_cool.Append(": ")
                .Append(method_info.RETURN.Value)
                .Append(" = ")
                .Nop()
        
        let method_body = method_info.Body.Value
        
        match method_body with
        | MethodBodySyntax.Expr expr ->
            before_braced_block_or_expr expr
        | _ ->
            ()
        
        match method_body with
        | MethodBodySyntax.Expr expr ->
            walk_expr expr
        | MethodBodySyntax.Native ->
            _sb_cool.Append("native").Nop()
        
        match method_body with
        | MethodBodySyntax.Expr expr ->
           match expr with
           | ExprSyntax.BracedBlock _ -> ()
           | _                  -> _indent.Decrease()
        | _ ->
            ()


    // Attribute
     and walk_attr (attr_info: AttrSyntax): unit =
        _sb_cool
            .Append(_indent)
            .Append("var ")
            .Append(attr_info.ID.Value)
            .Append(": ")
            .Append(attr_info.TYPE.Value).Nop()

        let attr_body = attr_info.Initial.Value
        _sb_cool.Append(" = ").Nop()
        
        match attr_body with
        | AttrInitialSyntax.Expr expr ->
            before_braced_block_or_expr expr
            walk_expr expr
            after_braced_block_or_expr expr            
        | AttrInitialSyntax.Native ->
            _sb_cool.Append("native").Nop()


    // Class
     and walk_extends (extends: InheritanceSyntax): unit =
        _sb_cool.Append(" extends ").Nop()

        match extends with
        | InheritanceSyntax.Info extends_info ->
            _sb_cool.Append(extends_info.SUPER.Value).Nop()
            walk_actuals (extends_info.Actuals)
        | InheritanceSyntax.Native ->
            _sb_cool.Append("native").Nop()


     and walk_features (features: AstNode<FeatureSyntax> []): unit =
        _sb_cool.Append(" {")
                .AppendLine().Nop()
        _indent.Increase()

        let visit_feature (feature_node: AstNode<FeatureSyntax>): unit =
            match feature_node.Value with
            | FeatureSyntax.Method method_info ->
                walk_method method_info
            | FeatureSyntax.Attr attr_info ->
                walk_attr attr_info
            | FeatureSyntax.BracedBlock block_info_opt ->
                _sb_cool.Append(_indent).Nop()
                walk_braced_block block_info_opt

        features |> Array.iter (fun it -> _sb_cool.AppendLine().Nop()
                                          visit_feature it
                                          _sb_cool.Append(";").AppendLine().Nop())
        
        _indent.Decrease()
        _sb_cool.Append(_indent)
                .Append("}").Nop()


     and walk_class (klass: ClassSyntax): unit =
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


    and walk_program (program: ProgramSyntax): unit =
        program.ClassDecls
        |> Array.iter (fun it -> walk_class it.Value
                                 _sb_cool.AppendLine().AppendLine().Nop())


    member private this.Render(ast: ProgramSyntax): unit =
        walk_program ast

    
    override this.ToString() = _sb_cool.ToString()


    static member Render(ast: ProgramSyntax) =
        let renderer = CoolRenderer()
        renderer.Render(ast)
        renderer.ToString()
