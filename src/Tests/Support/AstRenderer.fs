namespace Tests.Parser


open System.Text
open LibCool.SharedParts
open LibCool.AstParts
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


    let rec walk_expr (expr_syntax: ExprSyntax): unit =
        match expr_syntax with
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
            walk_comparison (left, "<=", right)
        | ExprSyntax.GtEq(left, right) ->
            walk_comparison (left, ">=", right)
        | ExprSyntax.Lt(left, right) ->
            walk_comparison (left, "<", right)
        | ExprSyntax.Gt(left, right) ->
            walk_comparison (left, ">", right)
        | ExprSyntax.EqEq(left, right) ->
            walk_comparison (left, "==", right)
        | ExprSyntax.NotEq(left, right) ->
            walk_comparison (left, "!=", right)
        | ExprSyntax.Mul(left, right) ->
            walk_arith (left, "*", right)
        | ExprSyntax.Div(left, right) ->
            walk_arith (left, "/", right)
        | ExprSyntax.Sum(left, right) ->
            walk_arith (left, "+", right)
        | ExprSyntax.Sub(left, right) ->
            walk_arith (left, "-", right)
        | ExprSyntax.Match(expr, cases_hd, cases_tl) ->
            walk_match (expr, cases_hd, cases_tl)
        | ExprSyntax.Dispatch(obj_expr, method_id, actuals) ->
            walk_dispatch (obj_expr, method_id, actuals)
        // Primary expressions
        | ExprSyntax.ImplicitThisDispatch(method_id, actuals) ->
            walk_implicit_this_dispatch (method_id, actuals)
        | ExprSyntax.SuperDispatch(method_id, actuals) ->
            walk_super_dispatch (method_id, actuals)
        | ExprSyntax.New(class_name, actuals) ->
            walk_object_creation (class_name, actuals)
        | ExprSyntax.BracedBlock block_syntax_opt ->
            walk_braced_block block_syntax_opt
        | ExprSyntax.ParensExpr node_expr ->
            walk_parens_expr node_expr
        | ExprSyntax.Id value ->
            text (sprintf "\"%s\"" (value.ToString()))
        | ExprSyntax.Int value ->
            text (value.ToString())
        | ExprSyntax.Str value ->
            text (sprintf "\"%s\"" (value.ToString(escape_quotes=true)))
        | ExprSyntax.Bool value ->
            text (sprintf "%s" (value.ToString()))
        | ExprSyntax.This ->
            text ("\"this\"")
        | ExprSyntax.Null ->
            text ("\"null\"")
        | ExprSyntax.Unit ->
            text ("\"()\"")
        
    
    and walk_var (var_syntax: VarSyntax): unit =
        begin_with "{"
        
        end_line_with "\"kind\": \"var\", "
           
        end_line_with (sprintf "\"name\": \"%s\", " var_syntax.ID.Syntax.Value)
        end_line_with (sprintf "\"type\": \"%s\", " var_syntax.TYPE.Syntax.Value)
        
        text "\"value\": "; walk_expr var_syntax.Expr.Syntax; end_line()
        
        end_with "}"


     and walk_stmt_expr (expr_syntax: ExprSyntax): unit =
        begin_with "{"
        
        end_line_with "\"kind\": \"stmt\", "
        text "\"expr\": "; walk_expr expr_syntax; end_line()
        
        end_with "}"


     and walk_block_syntax (block_syntax: BlockSyntax): unit =
        begin_with "[ "
        
        block_syntax.Stmts
        |> Array.iter (fun it ->
            match it.Syntax with
            | StmtSyntax.Var var_syntax -> walk_var var_syntax
            | StmtSyntax.Expr expr -> walk_stmt_expr expr
            end_line_with ", ")

        begin_with "{" 

        end_line_with "\"kind\": \"expr\", "

        text "\"expr\": "
        walk_expr block_syntax.Expr.Syntax; end_line()

        end_with "}"; end_line()
        
        end_with "]"


    and walk_braced_block (block_syntax: BlockSyntax voption): unit =
        match block_syntax with
        | ValueSome block_syntax ->
            walk_block_syntax block_syntax
        | ValueNone ->
            text ("[]")


    and walk_caseblock (block: CaseBlockSyntax) =
        begin_with "{"
        
        match block with
        | CaseBlockSyntax.Free block_syntax ->
            end_line_with "\"kind\": \"implicit\", "
            text "\"statements\": "
            walk_block_syntax block_syntax; end_line()
        | CaseBlockSyntax.Braced block_syntax_opt ->
            end_line_with "\"kind\": \"braced\", "
            text "\"statements\": "
            walk_braced_block block_syntax_opt; end_line()
            
        end_with "}"


    and walk_assign (lvalue: AstNode<ID>, rvalue: AstNode<ExprSyntax>): unit =
        begin_with "{"

        end_line_with "\"kind\": \"=\", "
           
        end_line_with (sprintf "\"left\": \"%s\", " lvalue.Syntax.Value)
        
        text "\"right\": "
        walk_expr rvalue.Syntax; end_line ()

        end_with "}"


    and walk_if (condition: AstNode<ExprSyntax>, then_branch: AstNode<ExprSyntax>, else_branch: AstNode<ExprSyntax>): unit =
        begin_with "{"

        end_line_with "\"kind\": \"if\", "
           
        text "\"cond\": "
        walk_expr condition.Syntax; end_line_with ", "
        
        text "\"then\": "
        walk_expr then_branch.Syntax; end_line_with ", "

        text "\"else\": "
        walk_expr else_branch.Syntax; end_line()

        end_with "}"


    and walk_while (condition: AstNode<ExprSyntax>, body: AstNode<ExprSyntax>): unit =
        begin_with "{"

        end_line_with "\"kind\": \"while\", "
           
        text "\"cond\": "
        walk_expr condition.Syntax; end_line_with ", "
        
        text "\"body\": "
        walk_expr body.Syntax; end_line()

        end_with "}"


    and stringify_match_case_pattern (pattern: PatternSyntax): string =
        match pattern with
        | PatternSyntax.IdType(node_id, node_type_name) ->
            sprintf "%s: %s" node_id.Syntax.Value node_type_name.Syntax.Value 
        | PatternSyntax.Null ->
            "null"


    and walk_match_case (case: CaseSyntax): unit =
        begin_with "{"

        end_line_with (sprintf "\"pattern\": \"%s\", " (stringify_match_case_pattern case.Pattern.Syntax))
        
        text "\"block\": "
        walk_caseblock case.Block.Syntax; end_line()
        
        end_with "}"


    and walk_match_cases (cases_hd: AstNode<CaseSyntax>, cases_tl: AstNode<CaseSyntax> []): unit =
        begin_with "["
       
        walk_match_case cases_hd.Syntax;
        cases_tl |> Array.iter (fun it ->
            end_line_with ","
            walk_match_case it.Syntax
        )
        end_line()
       
        end_with "]"


    and walk_match (expr: AstNode<ExprSyntax>, cases_hd: AstNode<CaseSyntax>, cases_tl: AstNode<CaseSyntax> []): unit =
        begin_with "{"

        end_line_with "\"kind\": \"match\", "
           
        text "\"expr\": "
        walk_expr expr.Syntax; end_line_with ", "
        
        text "\"cases\": "
        walk_match_cases (cases_hd, cases_tl); end_line()

        end_with "}"


    and walk_actual (actual: ExprSyntax, index: int): unit =
        if index > 0
        then
            end_line_with ", "
            
        walk_expr actual


    and walk_actuals (actuals: AstNode<ExprSyntax> []): unit =
        if actuals.Length = 0
        then
            text "[]"
        else
            begin_with "[ "
            actuals |> Array.iteri (fun i it -> walk_actual (it.Syntax, i)); end_line()
            end_with "]"


    and walk_dispatch (receiver: AstNode<ExprSyntax>, method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        begin_with "{"

        end_line_with "\"kind\": \"dispatch\", "
           
        text "\"receiver\": "
        walk_expr receiver.Syntax; end_line_with ", "
        
        end_line_with (sprintf "\"method\": \"%s\", " method_id.Syntax.Value)

        text "\"actuals\": "
        walk_actuals actuals; end_line()

        end_with "}"


    and walk_implicit_this_dispatch (method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        begin_with "{"

        end_line_with "\"kind\": \"implicit_this_dispatch\", "
        end_line_with (sprintf "\"method\": \"%s\", " method_id.Syntax.Value)

        text "\"actuals\": "
        walk_actuals actuals; end_line()

        end_with "}"


    and walk_super_dispatch (method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        begin_with "{"

        end_line_with "\"kind\": \"super_dispatch\", "
        end_line_with (sprintf "\"method\": \"%s\", " method_id.Syntax.Value)

        text "\"actuals\": "
        walk_actuals actuals; end_line()

        end_with "}"


    // Object creation
    and walk_object_creation (type_name: AstNode<TYPENAME>, actuals: AstNode<ExprSyntax> []): unit =
        begin_with "{"

        end_line_with "\"kind\": \"new\", "
        end_line_with (sprintf "\"type\": \"%s\", " type_name.Syntax.Value)

        text "\"actuals\": "
        walk_actuals actuals; end_line()

        end_with "}"


    // Bool negation
    and walk_bool_negation (expr: AstNode<ExprSyntax>): unit =
        begin_with "{"

        end_line_with "\"kind\": \"!\", "
           
        text "\"expr\": "
        walk_expr expr.Syntax; end_line()

        end_with "}"


    // Compare
    and walk_comparison (left: AstNode<ExprSyntax>, op: string, right: AstNode<ExprSyntax>): unit =
        begin_with "{"

        end_line_with (sprintf "\"kind\": \"%s\", " op)
           
        end_line_with "\"left\": "
        walk_expr left.Syntax; end_line_with ", "

        end_line_with "\"right\": "
        walk_expr right.Syntax; end_line()

        end_with "}"


    // Unary minus
    and walk_unary_minus (expr: AstNode<ExprSyntax>): unit =
        begin_with "{"

        end_line_with "\"kind\": \"-\", "
           
        text "\"expr\": "
        walk_expr expr.Syntax; end_line()

        end_with "}"


    // Arith
    and walk_arith (left: AstNode<ExprSyntax>, op: string, right: AstNode<ExprSyntax>): unit =
        begin_with "{"

        end_line_with (sprintf "\"kind\": \"%s\", " op)
           
        text "\"left\": "
        walk_expr left.Syntax; end_line_with ", "

        text "\"right\": "
        walk_expr right.Syntax; end_line()

        end_with "}"


    // Parenthesized expr
    and walk_parens_expr (expr: AstNode<ExprSyntax>): unit =
        begin_with "{"

        end_line_with "\"kind\": \"parenthesized\", "
           
        text "\"expr\": "
        walk_expr expr.Syntax; end_line()

        end_with "}"


    and walk_var_formal (var_formal: VarFormalSyntax, index: int): unit =
        if index > 0
        then
            end_line_with ", "
            
        begin_with "{"

        end_line_with (sprintf "\"name\": \"%s\", " var_formal.ID.Syntax.Value)
        end_line_with (sprintf "\"type\": \"%s\"" var_formal.TYPE.Syntax.Value)

        end_with "}"


    and walk_var_formals (var_formals: AstNode<VarFormalSyntax> []): unit =
        if var_formals.Length = 0
        then
            text "[]"
        else
            begin_with "[ "
            var_formals |> Array.iteri (fun i it -> walk_var_formal(it.Syntax, i)); end_line()
            end_with "]"


    and walk_formal (formal: FormalSyntax, index: int): unit =
        if index > 0
        then
            end_line_with ", "
            
        begin_with "{"

        end_line_with (sprintf "\"name\": \"%s\", " formal.ID.Syntax.Value)
        end_line_with (sprintf "\"type\": \"%s\"" formal.TYPE.Syntax.Value)
        
        end_with "}"


    and walk_formals (formals: AstNode<FormalSyntax> []): unit =
        if formals.Length = 0
        then
            text "[]"
        else
            begin_with "[ "
            formals |> Array.iteri (fun i it -> walk_formal(it.Syntax, i)); end_line()
            end_with "]"


    and walk_method (method_syntax: MethodSyntax): unit =
        begin_with "{"
        
        end_line_with "\"kind\": \"method\", "
        end_line_with (sprintf "\"name\": \"%s\", " method_syntax.ID.Syntax.Value)
        end_line_with (sprintf "\"type\": \"%s\", " method_syntax.RETURN.Syntax.Value)
        end_line_with (sprintf "\"overriden\": %b, " method_syntax.Override)
        
        end_line_with "\"formals\": "
        walk_formals method_syntax.Formals; end_line_with ", "
        
        text "\"body\": "
        match method_syntax.Body.Syntax with
        | MethodBodySyntax.Expr expr ->
            walk_expr expr
        | MethodBodySyntax.Native ->
            text ("\"native\"")
        end_line()
        
        end_with "}"


    and walk_attr (attr_syntax: AttrSyntax): unit =
        begin_with "{"
           
        end_line_with "\"kind\": \"attribute\", " 
        end_line_with (sprintf "\"name\": \"%s\", " attr_syntax.ID.Syntax.Value)
        end_line_with (sprintf "\"type\": \"%s\", " attr_syntax.TYPE.Syntax.Value)

        text "\"value\": "
        match attr_syntax.Initial.Syntax with
        | AttrInitialSyntax.Expr expr ->
            walk_expr expr
        | AttrInitialSyntax.Native ->
            end_line_with ("\"native\"")
        end_line()
        
        end_with "}"


    and walk_extends (inheritance_syntax: InheritanceSyntax voption): unit =
        match inheritance_syntax with
        | ValueSome (InheritanceSyntax.Extends extends_syntax) ->
            begin_with "{"
            end_line_with (sprintf "\"type\": \"%s\", " extends_syntax.SUPER.Syntax.Value)
            text "\"actuals\": "
            walk_actuals (extends_syntax.Actuals); end_line()
            end_with "}"
        | ValueNone ->
            begin_with "{"
            end_line_with "\"type\": \"Any\", "
            end_line_with "\"actuals\": []"
            end_line()
            end_with "}"
        | ValueSome InheritanceSyntax.Native ->
            end_line_with "\"native\""


     and walk_features (features: AstNode<FeatureSyntax> []): unit =
        let visit_feature (feature_node: AstNode<FeatureSyntax>) (index: int): unit =
            if index > 0
            then
                end_line_with ", "
                
            match feature_node.Syntax with
            | FeatureSyntax.Method method_syntax ->
                walk_method method_syntax
            | FeatureSyntax.Attr attr_syntax ->
                walk_attr attr_syntax
            | FeatureSyntax.BracedBlock block_syntax_opt ->
                begin_with "{"
                
                end_line_with "\"kind\": \"block\", "
                text "\"statements\": "
                walk_braced_block block_syntax_opt; end_line()
                
                end_with "}"

        begin_with "["
        features |> Array.iteri (fun i it -> visit_feature it i); end_line()
        end_with "]"


     and walk_class (class_syntax: ClassSyntax): unit =
        begin_with "{"
        
        end_line_with (sprintf "\"name\": \"%s\", " class_syntax.NAME.Syntax.Value)
        
        text "\"varformals\": "
        walk_var_formals class_syntax.VarFormals; end_line_with ", "

        text "\"extends\": "
        walk_extends (class_syntax.Extends |> ValueOption.map (fun it -> it.Syntax)); end_line_with ", "

        text "\"body\": "
        walk_features class_syntax.Features; end_line()

        end_with "}"


    // Program
    and walk_program (program_syntax: ProgramSyntax): unit =
        end_line_with "{"
        end_line_with "\"classes\": ["
        
        program_syntax.Classes
        |> Array.iteri (fun i it ->
            if i > 0
            then
                end_line_with ", "; end_line()
                
            walk_class it.Syntax
        )
        end_line()
        
        end_line_with "]"
        end_line_with "}"


    member private this.Render(program_syntax: ProgramSyntax): unit =
        walk_program program_syntax

    
    override this.ToString() = _sb_ast.ToString()


    static member Render(ast: ProgramSyntax) =
        let renderer = AstRenderer()
        renderer.Render(ast)
        renderer.ToString()
