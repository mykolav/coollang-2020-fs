namespace Tests.Parser


open System.Text
open LibCool.Ast
open Tests.Support


[<Sealed>]
type AstRenderer private () =

    
    let _indent = Indent()
    let _sb_ast = StringBuilder()
    
    
    let with_indent (label: string) (action: unit -> unit): unit =
        _indent.Increase()
        _sb_ast.Append(_indent).AppendLine(label).Nop()

        _indent.Increase()
        action()
        _indent.Decrease()

        _indent.Decrease()


    let label_with_indent (label: string): unit =
        _indent.Increase()
        _sb_ast.Append(_indent).AppendLine(label).Nop()
        _indent.Decrease()

    
    // Actuals
    let rec walk_actual (actual: Expr, index: int): unit =
        with_indent ((*label=*)"actual") (fun () -> walk_expr actual)


    and walk_actuals (actuals: Node<Expr> []): unit =
        if actuals.Length = 0
        then
            _sb_ast.Append(_indent).AppendLine("<empty>").Nop()
        else
            actuals |> Array.iteri (fun i it -> walk_actual (it.Value, i))


    // Expressions
    // Block
    and walk_var_decl (var_decl_info: VarDeclInfo): unit =
        _sb_ast.Append(_indent).AppendLine("var declaration").Nop()
           
        label_with_indent (sprintf "name %s" var_decl_info.ID.Value.Value)
        label_with_indent (sprintf "type %s" var_decl_info.TYPE_NAME.Value.Value)
        with_indent ((*label=*)"initial value") (fun () -> walk_expr var_decl_info.Expr.Value)


     and walk_stmt_expr (expr: Expr): unit =
        walk_expr expr


     and walk_block_info (block_info: BlockInfo): unit =
        block_info.Stmts
        |> Array.iteri (fun i it ->
            match it.Value with
            | Stmt.VarDecl var_decl_info -> walk_var_decl var_decl_info
            | Stmt.Expr expr -> walk_stmt_expr expr)

        walk_expr block_info.Expr.Value


    // Braced block
    and walk_braced_block (block_node: Node<BlockInfo voption>): unit =
        match block_node.Value with
        | ValueSome block_info ->
            walk_block_info block_info
        | ValueNone ->
            _sb_ast.Append(_indent).AppendLine("<empty>").Nop()


    and walk_block (block: CaseBlock) =
        match block with
        | CaseBlock.Implicit block_info ->
            with_indent ((*label=*)"caseblock implicit") (fun () -> walk_block_info block_info)
        | CaseBlock.Braced block_info_opt ->
            with_indent ((*label=*)"caseblock braced") (fun () -> walk_braced_block block_info_opt)


    // Assign
    and walk_assign (lvalue: Node<ID>, rvalue: Node<Expr>): unit =
        _sb_ast.Append(_indent).AppendLine("assign =").Nop()
           
        with_indent ((*label=*)"left") (fun () -> _sb_ast.Append(_indent).AppendFormat("ID {0}", lvalue.Value).AppendLine().Nop())
        with_indent ((*label=*)"right") (fun () -> walk_expr rvalue.Value)


    // If
    and walk_if (condition: Node<Expr>, then_branch: Node<Expr>, else_branch: Node<Expr>): unit =
        _sb_ast.Append(_indent).AppendLine("if").Nop()
            
        with_indent ((*label=*)"cond") (fun () -> walk_expr condition.Value)
        with_indent ((*label=*)"then") (fun () -> walk_expr then_branch.Value)
        with_indent ((*label=*)"else") (fun () -> walk_expr else_branch.Value)


    // While
    and walk_while (condition: Node<Expr>, body: Node<Expr>): unit =
        _sb_ast.Append(_indent).AppendLine("while").Nop()
            
        with_indent ((*label=*)"cond") (fun () -> walk_expr condition.Value)
        with_indent ((*label=*)"body") (fun () -> walk_expr body.Value)


    // Match/case
    and stringify_match_case_pattern (pattern: Pattern): string =
        match pattern with
        | Pattern.IdType(node_id, node_type_name) ->
            sprintf "%s: %s" node_id.Value.Value node_type_name.Value.Value 
        | Pattern.Null ->
            "null"


    and walk_match_case (case: Case): unit =
        _sb_ast.Append(_indent).AppendLine("case").Nop()

        label_with_indent (sprintf "pattern %s" (stringify_match_case_pattern case.Pattern.Value))
        with_indent ((*label=*)"block") (fun () -> walk_block case.Block.Value)


    and walk_match_cases (cases_hd: Node<Case>, cases_tl: Node<Case> []): unit =
       walk_match_case cases_hd.Value
       cases_tl |> Array.iter (fun it -> walk_match_case it.Value)


    // Match
    and walk_match (expr: Node<Expr>, cases_hd: Node<Case>, cases_tl: Node<Case> []): unit =
        _sb_ast.Append(_indent).AppendLine("match").Nop()
            
        with_indent ((*label=*)"expr") (fun () -> walk_expr expr.Value)

        with_indent ((*label=*)"cases") (fun () -> walk_match_cases (cases_hd, cases_tl))


    // Dispatch
    and walk_dispatch (receiver: Node<Expr>, method_id: Node<ID>, actuals: Node<Expr> []): unit =
        _sb_ast.Append(_indent).AppendLine("dispatch").Nop()
            
        with_indent ((*label=*)"receiver") (fun () -> walk_expr receiver.Value)
        
        with_indent ((*label=*)"method") (fun () -> _sb_ast.Append(_indent).AppendLine(method_id.Value.Value).Nop())

        with_indent ((*label=*)"actuals") (fun () -> walk_actuals actuals)


    // Implicit `this` dispatch
    and walk_implicit_this_dispatch (method_id: Node<ID>, actuals: Node<Expr> []): unit =
        _sb_ast.Append(_indent).AppendLine("implicit this dispatch").Nop()
            
        label_with_indent (sprintf "method %s" method_id.Value.Value)
        with_indent ((*label=*)"actuals") (fun () -> walk_actuals actuals)


    // Super dispatch
    and walk_super_dispatch (method_id: Node<ID>, actuals: Node<Expr> []): unit =
        _sb_ast.Append(_indent).AppendLine("super dispatch").Nop()
            
        label_with_indent (sprintf "method %s" method_id.Value.Value)
        with_indent ((*label=*)"actuals") (fun () -> walk_actuals actuals)


    // Object creation
    and walk_object_creation (type_name: Node<TYPE_NAME>, actuals: Node<Expr> []): unit =
        _sb_ast.Append(_indent).AppendLine("new").Nop()
            
        label_with_indent (sprintf "type %s" type_name.Value.Value)
        with_indent ((*label=*)"actuals") (fun () -> walk_actuals actuals)


    // Bool negation
    and walk_bool_negation (expr: Node<Expr>): unit =
        _sb_ast.Append("boolean !").Nop()
        _indent.Increase()
        walk_expr expr.Value
        _indent.Decrease()


    // Compare
    and walk_comparison (left: Node<Expr>, op: string, right: Node<Expr>): unit =
        _sb_ast.Append(_indent).AppendLine(op).Nop()
        
        with_indent ((*label=*)"left") (fun () -> walk_expr left.Value)
        with_indent ((*label=*)"right") (fun () -> walk_expr right.Value)


    // Unary minus
    and walk_unary_minus (expr: Node<Expr>): unit =
        with_indent ((*label=*)"unary -") (fun () -> walk_expr expr.Value)


    // Arith
    and walk_arith (left: Node<Expr>, op: string, right: Node<Expr>): unit =
        _sb_ast.Append(_indent).AppendLine(op).Nop()
        
        with_indent ((*label=*)"left") (fun () -> walk_expr left.Value)
        with_indent ((*label=*)"right") (fun () -> walk_expr right.Value)


    // Parenthesized expr
    and walk_parens_expr (expr: Node<Expr>): unit =
        with_indent ((*label=*)"(...)") (fun () -> walk_expr expr.Value)


    // Classes
    and walk_var_formal (var_formal: VarFormal, index: int): unit =
        _sb_ast.Append(_indent).AppendLine("var formal").Nop()

        label_with_indent (sprintf "name %s" var_formal.ID.Value.Value)
        label_with_indent (sprintf "type %s" var_formal.TYPE_NAME.Value.Value)


    and walk_var_formals (var_formals: Node<VarFormal> []): unit =
        if var_formals.Length = 0
        then
            _sb_ast.Append(_indent).AppendLine("<empty>").Nop()
        else
            var_formals |> Array.iteri (fun i it -> walk_var_formal(it.Value, i))


    // Method
    and walk_formal (formal: Formal, index: int): unit =
        _sb_ast.Append(_indent).AppendLine("formal").Nop()

        label_with_indent (sprintf "name %s" formal.ID.Value.Value)
        label_with_indent (sprintf "type %s" formal.TYPE_NAME.Value.Value)


    and walk_formals (formals: Node<Formal> []): unit =
        if formals.Length = 0
        then
            _sb_ast.Append(_indent).AppendLine("<empty>").Nop()
        else
            formals |> Array.iteri (fun i it -> walk_formal(it.Value, i))


    and walk_method (method_info: MethodInfo): unit =
        _sb_ast.Append(_indent).AppendLine("method").Nop()
        
        label_with_indent (sprintf "name %s" method_info.ID.Value.Value)
        label_with_indent (sprintf "type %s" method_info.TYPE_NAME.Value.Value)
        label_with_indent (sprintf "overriden? %s" (if method_info.Override then "Yes" else "No"))
        with_indent ((*label=*)"formals") (fun () -> walk_formals method_info.Formals)
        
        with_indent ((*label=*)"body") (fun () ->
            match method_info.MethodBody.Value with
            | MethodBody.Expr expr ->
                walk_expr expr
            | MethodBody.Native ->
                _sb_ast.Append(_indent).AppendLine("native").Nop()
        )


    // Attribute
    and walk_attr (attr_info: AttrInfo): unit =
        _sb_ast.Append(_indent).AppendLine("attribute").Nop()
           
        label_with_indent (sprintf "name %s" attr_info.ID.Value.Value)
        label_with_indent (sprintf "type %s" attr_info.TYPE_NAME.Value.Value)

        with_indent ((*label=*)"initial value") (fun () ->
            match attr_info.AttrBody.Value with
            | AttrBody.Expr expr ->
                walk_expr expr
            | AttrBody.Native ->
                _sb_ast.Append(_indent).AppendLine("native").Nop()
        )


    // Class
    and walk_extends (extends: Extends): unit =
        match extends with
        | Extends.Info extends_info ->
            _sb_ast.Append(_indent).AppendLine(extends_info.PARENT_NAME.Value.Value).Nop()
            walk_actuals (extends_info.Actuals)
        | Extends.Native ->
            _sb_ast.Append(_indent).AppendLine("native").Nop()


     and walk_features (features: Node<Feature> []): unit =
        let visit_feature (feature_node: Node<Feature>): unit =
            match feature_node.Value with
            | Feature.Method method_info ->
                walk_method method_info
            | Feature.Attr attr_info ->
                walk_attr attr_info
            | Feature.BracedBlock block_info_opt ->
                with_indent ((*label=*)"block") (fun () -> walk_braced_block block_info_opt)

        features |> Array.iter (fun it -> visit_feature it)


     and walk_class (klass: ClassDecl): unit =
        _sb_ast.Append(_indent).AppendLine("class").Nop()
        
        label_with_indent (sprintf "name %s" klass.NAME.Value.Value)
        with_indent ((*label=*)"varformals") (fun () -> walk_var_formals klass.VarFormals)

        with_indent ((*label=*)"extends") (fun () ->
            match klass.Extends with
            | ValueSome extends_node ->
                walk_extends extends_node.Value
            | ValueNone ->
                _sb_ast.Append(_indent).AppendLine("Any").Nop()
        )

        with_indent ((*label=*)"classbody") (fun () -> walk_features klass.ClassBody)


    // Program
    and walk_program (program: Program): unit =
        program.ClassDecls
        |> Array.iter (fun it -> walk_class it.Value
                                 _sb_ast.AppendLine().AppendLine().Nop())


    // Ast
    and walk_ast (ast: Ast): unit =
        walk_program ast.Program.Value


    // This function has to be declared as a member to support mutually recursive calls
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
            walk_comparison (left, "infix <=", right)
        | Expr.GtEq(left, right) ->
            walk_comparison (left, "infix >=", right)
        | Expr.Lt(left, right) ->
            walk_comparison (left, "infix <", right)
        | Expr.Gt(left, right) ->
            walk_comparison (left, "infix >", right)
        | Expr.EqEq(left, right) ->
            walk_comparison (left, "infix ==", right)
        | Expr.NotEq(left, right) ->
            walk_comparison (left, "infix !=", right)
        | Expr.Mul(left, right) ->
            walk_arith (left, "infix *", right)
        | Expr.Div(left, right) ->
            walk_arith (left, "infix /", right)
        | Expr.Sum(left, right) ->
            walk_arith (left, "infix +", right)
        | Expr.Sub(left, right) ->
            walk_arith (left, "infix -", right)
        | Expr.Match(expr, cases_hd, cases_tl) ->
            walk_match (expr, cases_hd, cases_tl)
        | Expr.Dispatch(obj_expr, method_id, actuals) ->
            walk_dispatch (obj_expr, method_id, actuals)
        // Primary expressions
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
            _sb_ast.Append(_indent).AppendLine(value.ToString()).Nop()
        | Expr.Int value ->
            _sb_ast.Append(_indent).AppendLine(value.ToString()).Nop()
        | Expr.Str value ->
            _sb_ast.Append(_indent).AppendLine(value.ToString()).Nop()
        | Expr.Bool value ->
            _sb_ast.Append(_indent).AppendLine(value.ToString()).Nop()
        | Expr.This ->
            _sb_ast.Append(_indent).AppendLine("this").Nop()
        | Expr.Null ->
            _sb_ast.Append(_indent).AppendLine("null").Nop()
        | Expr.Unit ->
            _sb_ast.Append(_indent).AppendLine("()").Nop()

    
    member private this.Render(ast: Ast): unit = walk_ast (ast)

    
    override this.ToString() = _sb_ast.ToString()


    static member Render(ast: Ast) =
        let renderer = AstRenderer()
        renderer.Render(ast)
        renderer.ToString()
