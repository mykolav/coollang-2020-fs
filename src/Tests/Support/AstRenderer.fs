namespace Tests.Parser


open System.Text
open LibCool.AstParts.Ast
open Tests.Support


[<Sealed>]
type AstRenderer private () =

    
    let _indent = Indent()
    let _sb_ast = StringBuilder()

    
    let mutable _is_new_line = true
    let indent (): unit =
        if (_is_new_line)
        then
            _sb_ast.Append(_indent).Nop()
            _is_new_line <- false
            

    let end_line_with (content: string): unit =
        indent()
        _sb_ast.AppendLine(content).Nop()
        _is_new_line <- true
    
    
    let end_line () =
        end_line_with ""
        
        
    let text (content: string): unit =
        indent()
        _sb_ast.Append(content).Nop()
        _is_new_line <- false
        
        
    let begin_with (left: string) =
        end_line_with left
        _indent.Increase()

    
    let end_with (right: string) =
        _indent.Decrease()
        text right


    // Expressions
    let rec walk_expr (expr: Expr): unit =
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
            walk_comparison (left, "<=", right)
        | Expr.GtEq(left, right) ->
            walk_comparison (left, ">=", right)
        | Expr.Lt(left, right) ->
            walk_comparison (left, "<", right)
        | Expr.Gt(left, right) ->
            walk_comparison (left, ">", right)
        | Expr.EqEq(left, right) ->
            walk_comparison (left, "==", right)
        | Expr.NotEq(left, right) ->
            walk_comparison (left, "!=", right)
        | Expr.Mul(left, right) ->
            walk_arith (left, "*", right)
        | Expr.Div(left, right) ->
            walk_arith (left, "/", right)
        | Expr.Sum(left, right) ->
            walk_arith (left, "+", right)
        | Expr.Sub(left, right) ->
            walk_arith (left, "-", right)
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
            text (sprintf "\"%s\"" (value.ToString()))
        | Expr.Int value ->
            text (value.ToString())
        | Expr.Str value ->
            text (sprintf "\"%s\"" (value.ToString(escape_quotes=true)))
        | Expr.Bool value ->
            text (sprintf "%s" (value.ToString()))
        | Expr.This ->
            text ("\"this\"")
        | Expr.Null ->
            text ("\"null\"")
        | Expr.Unit ->
            text ("\"()\"")
        
    
    // Block
    and walk_var_decl (var_decl_info: VarDeclInfo): unit =
        begin_with "{"
        
        end_line_with "\"kind\": \"var\", "
           
        end_line_with (sprintf "\"name\": \"%s\", " var_decl_info.ID.Value.Value)
        end_line_with (sprintf "\"type\": \"%s\", " var_decl_info.TYPE_NAME.Value.Value)
        
        text "\"value\": "; walk_expr var_decl_info.Expr.Value; end_line()
        
        end_with "}"


     and walk_stmt_expr (expr: Expr): unit =
        begin_with "{"
        
        end_line_with "\"kind\": \"stmt\", "
        text "\"expr\": "; walk_expr expr; end_line()
        
        end_with "}"


     and walk_block_info (block_info: BlockInfo): unit =
        begin_with "[ "
        
        block_info.Stmts
        |> Array.iter (fun it ->
            match it.Value with
            | Stmt.VarDecl var_decl_info -> walk_var_decl var_decl_info
            | Stmt.Expr expr -> walk_stmt_expr expr
            end_line_with ", ")

        begin_with "{" 

        end_line_with "\"kind\": \"expr\", "

        text "\"expr\": "
        walk_expr block_info.Expr.Value; end_line()

        end_with "}"; end_line()
        
        end_with "]"


    // Braced block
    and walk_braced_block (block_node: Node<BlockInfo voption>): unit =
        match block_node.Value with
        | ValueSome block_info ->
            walk_block_info block_info
        | ValueNone ->
            text ("[]")


    and walk_block (block: CaseBlock) =
        begin_with "{"
        
        match block with
        | CaseBlock.Implicit block_info ->
            end_line_with "\"kind\": \"implicit\", "
            text "\"statements\": "
            walk_block_info block_info; end_line()
        | CaseBlock.Braced block_info_opt ->
            end_line_with "\"kind\": \"braced\", "
            text "\"statements\": "
            walk_braced_block block_info_opt; end_line()
            
        end_with "}"


    // Assign
    and walk_assign (lvalue: Node<ID>, rvalue: Node<Expr>): unit =
        begin_with "{"

        end_line_with "\"kind\": \"=\", "
           
        end_line_with (sprintf "\"left\": \"%s\", " lvalue.Value.Value)
        
        text "\"right\": "
        walk_expr rvalue.Value; end_line ()

        end_with "}"


    // If
    and walk_if (condition: Node<Expr>, then_branch: Node<Expr>, else_branch: Node<Expr>): unit =
        begin_with "{"

        end_line_with "\"kind\": \"if\", "
           
        text "\"cond\": "
        walk_expr condition.Value; end_line_with ", "
        
        text "\"then\": "
        walk_expr then_branch.Value; end_line_with ", "

        text "\"else\": "
        walk_expr else_branch.Value; end_line()

        end_with "}"


    // While
    and walk_while (condition: Node<Expr>, body: Node<Expr>): unit =
        begin_with "{"

        end_line_with "\"kind\": \"while\", "
           
        text "\"cond\": "
        walk_expr condition.Value; end_line_with ", "
        
        text "\"body\": "
        walk_expr body.Value; end_line()

        end_with "}"


    // Match/case
    and stringify_match_case_pattern (pattern: Pattern): string =
        match pattern with
        | Pattern.IdType(node_id, node_type_name) ->
            sprintf "%s: %s" node_id.Value.Value node_type_name.Value.Value 
        | Pattern.Null ->
            "null"


    and walk_match_case (case: Case): unit =
        begin_with "{"

        end_line_with (sprintf "\"pattern\": \"%s\", " (stringify_match_case_pattern case.Pattern.Value))
        
        text "\"block\": "
        walk_block case.Block.Value; end_line()
        
        end_with "}"


    and walk_match_cases (cases_hd: Node<Case>, cases_tl: Node<Case> []): unit =
        begin_with "["
       
        walk_match_case cases_hd.Value;
        cases_tl |> Array.iter (fun it ->
            end_line_with ","
            walk_match_case it.Value
        )
        end_line()
       
        end_with "]"


    // Match
    and walk_match (expr: Node<Expr>, cases_hd: Node<Case>, cases_tl: Node<Case> []): unit =
        begin_with "{"

        end_line_with "\"kind\": \"match\", "
           
        text "\"expr\": "
        walk_expr expr.Value; end_line_with ", "
        
        text "\"cases\": "
        walk_match_cases (cases_hd, cases_tl); end_line()

        end_with "}"


    // Dispatch
    // Actuals
    and walk_actual (actual: Expr, index: int): unit =
        if index > 0
        then
            end_line_with ", "
            
        walk_expr actual


    and walk_actuals (actuals: Node<Expr> []): unit =
        if actuals.Length = 0
        then
            text "[]"
        else
            begin_with "[ "
            actuals |> Array.iteri (fun i it -> walk_actual (it.Value, i)); end_line()
            end_with "]"


    and walk_dispatch (receiver: Node<Expr>, method_id: Node<ID>, actuals: Node<Expr> []): unit =
        begin_with "{"

        end_line_with "\"kind\": \"dispatch\", "
           
        text "\"receiver\": "
        walk_expr receiver.Value; end_line_with ", "
        
        end_line_with (sprintf "\"method\": \"%s\", " method_id.Value.Value)

        text "\"actuals\": "
        walk_actuals actuals; end_line()

        end_with "}"


    and walk_implicit_this_dispatch (method_id: Node<ID>, actuals: Node<Expr> []): unit =
        begin_with "{"

        end_line_with "\"kind\": \"implicit_this_dispatch\", "
        end_line_with (sprintf "\"method\": \"%s\", " method_id.Value.Value)

        text "\"actuals\": "
        walk_actuals actuals; end_line()

        end_with "}"


    and walk_super_dispatch (method_id: Node<ID>, actuals: Node<Expr> []): unit =
        begin_with "{"

        end_line_with "\"kind\": \"super_dispatch\", "
        end_line_with (sprintf "\"method\": \"%s\", " method_id.Value.Value)

        text "\"actuals\": "
        walk_actuals actuals; end_line()

        end_with "}"


    // Object creation
    and walk_object_creation (type_name: Node<TYPE_NAME>, actuals: Node<Expr> []): unit =
        begin_with "{"

        end_line_with "\"kind\": \"new\", "
        end_line_with (sprintf "\"type\": \"%s\", " type_name.Value.Value)

        text "\"actuals\": "
        walk_actuals actuals; end_line()

        end_with "}"


    // Bool negation
    and walk_bool_negation (expr: Node<Expr>): unit =
        begin_with "{"

        end_line_with "\"kind\": \"!\", "
           
        text "\"expr\": "
        walk_expr expr.Value; end_line()

        end_with "}"


    // Compare
    and walk_comparison (left: Node<Expr>, op: string, right: Node<Expr>): unit =
        begin_with "{"

        end_line_with (sprintf "\"kind\": \"%s\", " op)
           
        end_line_with "\"left\": "
        walk_expr left.Value; end_line_with ", "

        end_line_with "\"right\": "
        walk_expr right.Value; end_line()

        end_with "}"


    // Unary minus
    and walk_unary_minus (expr: Node<Expr>): unit =
        begin_with "{"

        end_line_with "\"kind\": \"-\", "
           
        text "\"expr\": "
        walk_expr expr.Value; end_line()

        end_with "}"


    // Arith
    and walk_arith (left: Node<Expr>, op: string, right: Node<Expr>): unit =
        begin_with "{"

        end_line_with (sprintf "\"kind\": \"%s\", " op)
           
        text "\"left\": "
        walk_expr left.Value; end_line_with ", "

        text "\"right\": "
        walk_expr right.Value; end_line()

        end_with "}"


    // Parenthesized expr
    and walk_parens_expr (expr: Node<Expr>): unit =
        begin_with "{"

        end_line_with "\"kind\": \"parenthesized\", "
           
        text "\"expr\": "
        walk_expr expr.Value; end_line()

        end_with "}"


    and walk_var_formal (var_formal: VarFormal, index: int): unit =
        if index > 0
        then
            end_line_with ", "
            
        begin_with "{"

        end_line_with (sprintf "\"name\": \"%s\", " var_formal.ID.Value.Value)
        end_line_with (sprintf "\"type\": \"%s\"" var_formal.TYPE_NAME.Value.Value)

        end_with "}"


    and walk_var_formals (var_formals: Node<VarFormal> []): unit =
        if var_formals.Length = 0
        then
            text "[]"
        else
            begin_with "[ "
            var_formals |> Array.iteri (fun i it -> walk_var_formal(it.Value, i)); end_line()
            end_with "]"


    and walk_formal (formal: Formal, index: int): unit =
        if index > 0
        then
            end_line_with ", "
            
        begin_with "{"

        end_line_with (sprintf "\"name\": \"%s\", " formal.ID.Value.Value)
        end_line_with (sprintf "\"type\": \"%s\"" formal.TYPE_NAME.Value.Value)
        
        end_with "}"


    and walk_formals (formals: Node<Formal> []): unit =
        if formals.Length = 0
        then
            text "[]"
        else
            begin_with "[ "
            formals |> Array.iteri (fun i it -> walk_formal(it.Value, i)); end_line()
            end_with "]"


    and walk_method (method_info: MethodInfo): unit =
        begin_with "{"
        
        end_line_with "\"kind\": \"method\", "
        end_line_with (sprintf "\"name\": \"%s\", " method_info.ID.Value.Value)
        end_line_with (sprintf "\"type\": \"%s\", " method_info.TYPE_NAME.Value.Value)
        end_line_with (sprintf "\"overriden\": %b, " method_info.Override)
        
        end_line_with "\"formals\": "
        walk_formals method_info.Formals; end_line_with ", "
        
        text "\"body\": "
        match method_info.MethodBody.Value with
        | MethodBody.Expr expr ->
            walk_expr expr
        | MethodBody.Native ->
            text ("\"native\"")
        end_line()
        
        end_with "}"


    and walk_attr (attr_info: AttrInfo): unit =
        begin_with "{"
           
        end_line_with "\"kind\": \"attribute\", " 
        end_line_with (sprintf "\"name\": \"%s\", " attr_info.ID.Value.Value)
        end_line_with (sprintf "\"type\": \"%s\", " attr_info.TYPE_NAME.Value.Value)

        text "\"value\": "
        match attr_info.AttrBody.Value with
        | AttrBody.Expr expr ->
            walk_expr expr
        | AttrBody.Native ->
            end_line_with ("\"native\"")
        end_line()
        
        end_with "}"


    and walk_extends (extends: Extends voption): unit =
        match extends with
        | ValueSome (Extends.Info extends_info) ->
            begin_with "{"
            end_line_with (sprintf "\"type\": \"%s\", " extends_info.PARENT_NAME.Value.Value)
            text "\"actuals\": "
            walk_actuals (extends_info.Actuals); end_line()
            end_with "}"
        | ValueNone ->
            begin_with "{"
            end_line_with "\"type\": \"Any\", "
            end_line_with "\"actuals\": []"
            end_line()
            end_with "}"
        | ValueSome Extends.Native ->
            end_line_with "\"native\""


     and walk_features (features: Node<Feature> []): unit =
        let visit_feature (feature_node: Node<Feature>) (index: int): unit =
            if index > 0
            then
                end_line_with ", "
                
            match feature_node.Value with
            | Feature.Method method_info ->
                walk_method method_info
            | Feature.Attr attr_info ->
                walk_attr attr_info
            | Feature.BracedBlock block_info_opt ->
                begin_with "{"
                
                end_line_with "\"kind\": \"block\", "
                text "\"statements\": "
                walk_braced_block block_info_opt; end_line()
                
                end_with "}"

        begin_with "["
        features |> Array.iteri (fun i it -> visit_feature it i); end_line()
        end_with "]"


     and walk_class (klass: ClassDecl): unit =
        begin_with "{"
        
        end_line_with (sprintf "\"name\": \"%s\", " klass.NAME.Value.Value)
        
        text "\"varformals\": "
        walk_var_formals klass.VarFormals; end_line_with ", "

        text "\"extends\": "
        walk_extends (klass.Extends |> ValueOption.map (fun it -> it.Value)); end_line_with ", "

        text "\"body\": "
        walk_features klass.ClassBody; end_line()

        end_with "}"


    // Program
    and walk_program (program: Program): unit =
        end_line_with "{"
        end_line_with "\"classes\": ["
        
        program.ClassDecls
        |> Array.iteri (fun i it ->
            if i > 0
            then
                end_line_with ", "; end_line()
                
            walk_class it.Value
        )
        end_line()
        
        end_line_with "]"
        end_line_with "}"


    // Ast
    and walk_ast (ast: Ast): unit =
        walk_program ast.Program.Value

    
    member private this.Render(ast: Ast): unit = walk_ast (ast)

    
    override this.ToString() = _sb_ast.ToString()


    static member Render(ast: Ast) =
        let renderer = AstRenderer()
        renderer.Render(ast)
        renderer.ToString()
