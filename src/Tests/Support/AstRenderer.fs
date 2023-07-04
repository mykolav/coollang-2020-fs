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
            _sb_ast.Append(_indent).AsUnit()
            _is_new_line <- false
            

    let endLineWith (content: string): unit =
        indent()
        _sb_ast.AppendLine(content).AsUnit()
        _is_new_line <- true
    
    
    let endLine () =
        endLineWith ""
        
        
    let text (content: string): unit =
        indent()
        _sb_ast.Append(content).AsUnit()
        _is_new_line <- false
        
        
    let beginWith (left: string) =
        endLineWith left
        _indent.Increase()

    
    let endWith (right: string) =
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
            text $"\"%s{value.ToString()}\""
        | ExprSyntax.Int value ->
            text (value.ToString())
        | ExprSyntax.Str value ->
            text $"\"%s{value.ToString(escape_quotes=true)}\""
        | ExprSyntax.Bool value ->
            text $"%s{value.ToString()}"
        | ExprSyntax.This ->
            text ("\"this\"")
        | ExprSyntax.Null ->
            text ("\"null\"")
        | ExprSyntax.Unit ->
            text ("\"()\"")
        
    
    and walk_var (var_syntax: VarSyntax): unit =
        beginWith "{"
        
        endLineWith "\"kind\": \"var\", "
           
        endLineWith $"\"name\": \"%s{var_syntax.ID.Syntax.Value}\", "
        endLineWith $"\"type\": \"%s{var_syntax.TYPE.Syntax.Value}\", "
        
        text "\"value\": "; walk_expr var_syntax.Expr.Syntax; endLine()
        
        endWith "}"


     and walk_stmt_expr (expr_syntax: ExprSyntax): unit =
        beginWith "{"
        
        endLineWith "\"kind\": \"stmt\", "
        text "\"expr\": "; walk_expr expr_syntax; endLine()
        
        endWith "}"


     and walk_block_syntax (block_syntax: BlockSyntax): unit =
        beginWith "[ "
        
        block_syntax.Stmts
        |> Array.iter (fun it ->
            match it.Syntax with
            | StmtSyntax.Var var_syntax -> walk_var var_syntax
            | StmtSyntax.Expr expr -> walk_stmt_expr expr
            endLineWith ", ")

        beginWith "{"

        endLineWith "\"kind\": \"expr\", "

        text "\"expr\": "
        walk_expr block_syntax.Expr.Syntax; endLine()

        endWith "}"; endLine()
        
        endWith "]"


    and walk_braced_block (block_syntax: BlockSyntax voption): unit =
        match block_syntax with
        | ValueSome block_syntax ->
            walk_block_syntax block_syntax
        | ValueNone ->
            text ("[]")


    and walk_caseblock (block: CaseBlockSyntax) =
        beginWith "{"
        
        match block with
        | CaseBlockSyntax.Free block_syntax ->
            endLineWith "\"kind\": \"implicit\", "
            text "\"statements\": "
            walk_block_syntax block_syntax; endLine()
        | CaseBlockSyntax.Braced block_syntax_opt ->
            endLineWith "\"kind\": \"braced\", "
            text "\"statements\": "
            walk_braced_block block_syntax_opt; endLine()
            
        endWith "}"


    and walk_assign (lvalue: AstNode<ID>, rvalue: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith "\"kind\": \"=\", "
           
        endLineWith $"\"left\": \"%s{lvalue.Syntax.Value}\", "
        
        text "\"right\": "
        walk_expr rvalue.Syntax; endLine ()

        endWith "}"


    and walk_if (condition: AstNode<ExprSyntax>, then_branch: AstNode<ExprSyntax>, else_branch: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith "\"kind\": \"if\", "
           
        text "\"cond\": "
        walk_expr condition.Syntax; endLineWith ", "
        
        text "\"then\": "
        walk_expr then_branch.Syntax; endLineWith ", "

        text "\"else\": "
        walk_expr else_branch.Syntax; endLine()

        endWith "}"


    and walk_while (condition: AstNode<ExprSyntax>, body: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith "\"kind\": \"while\", "
           
        text "\"cond\": "
        walk_expr condition.Syntax; endLineWith ", "
        
        text "\"body\": "
        walk_expr body.Syntax; endLine()

        endWith "}"


    and stringify_match_case_pattern (pattern: PatternSyntax): string =
        match pattern with
        | PatternSyntax.IdType(node_id, node_type_name) ->
            $"%s{node_id.Syntax.Value}: %s{node_type_name.Syntax.Value}"
        | PatternSyntax.Null ->
            "null"


    and walk_match_case (case: CaseSyntax): unit =
        beginWith "{"

        endLineWith $"\"pattern\": \"%s{stringify_match_case_pattern case.Pattern.Syntax}\", "
        
        text "\"block\": "
        walk_caseblock case.Block.Syntax; endLine()
        
        endWith "}"


    and walk_match_cases (cases_hd: AstNode<CaseSyntax>, cases_tl: AstNode<CaseSyntax> []): unit =
        beginWith "["
       
        walk_match_case cases_hd.Syntax;
        cases_tl |> Array.iter (fun it ->
            endLineWith ","
            walk_match_case it.Syntax
        )
        endLine()
       
        endWith "]"


    and walk_match (expr: AstNode<ExprSyntax>, cases_hd: AstNode<CaseSyntax>, cases_tl: AstNode<CaseSyntax> []): unit =
        beginWith "{"

        endLineWith "\"kind\": \"match\", "
           
        text "\"expr\": "
        walk_expr expr.Syntax; endLineWith ", "
        
        text "\"cases\": "
        walk_match_cases (cases_hd, cases_tl); endLine()

        endWith "}"


    and walk_actual (actual: ExprSyntax, index: int): unit =
        if index > 0
        then
            endLineWith ", "
            
        walk_expr actual


    and walk_actuals (actuals: AstNode<ExprSyntax> []): unit =
        if actuals.Length = 0
        then
            text "[]"
        else
            beginWith "[ "
            actuals |> Array.iteri (fun i it -> walk_actual (it.Syntax, i)); endLine()
            endWith "]"


    and walk_dispatch (receiver: AstNode<ExprSyntax>, method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        beginWith "{"

        endLineWith "\"kind\": \"dispatch\", "
           
        text "\"receiver\": "
        walk_expr receiver.Syntax; endLineWith ", "
        
        endLineWith $"\"method\": \"%s{method_id.Syntax.Value}\", "

        text "\"actuals\": "
        walk_actuals actuals; endLine()

        endWith "}"


    and walk_implicit_this_dispatch (method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        beginWith "{"

        endLineWith "\"kind\": \"implicit_this_dispatch\", "
        endLineWith $"\"method\": \"%s{method_id.Syntax.Value}\", "

        text "\"actuals\": "
        walk_actuals actuals; endLine()

        endWith "}"


    and walk_super_dispatch (method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        beginWith "{"

        endLineWith "\"kind\": \"super_dispatch\", "
        endLineWith $"\"method\": \"%s{method_id.Syntax.Value}\", "

        text "\"actuals\": "
        walk_actuals actuals; endLine()

        endWith "}"


    // Object creation
    and walk_object_creation (type_name: AstNode<TYPENAME>, actuals: AstNode<ExprSyntax> []): unit =
        beginWith "{"

        endLineWith "\"kind\": \"new\", "
        endLineWith $"\"type\": \"%s{type_name.Syntax.Value}\", "

        text "\"actuals\": "
        walk_actuals actuals; endLine()

        endWith "}"


    // Bool negation
    and walk_bool_negation (expr: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith "\"kind\": \"!\", "
           
        text "\"expr\": "
        walk_expr expr.Syntax; endLine()

        endWith "}"


    // Compare
    and walk_comparison (left: AstNode<ExprSyntax>, op: string, right: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith $"\"kind\": \"%s{op}\", "
           
        endLineWith "\"left\": "
        walk_expr left.Syntax; endLineWith ", "

        endLineWith "\"right\": "
        walk_expr right.Syntax; endLine()

        endWith "}"


    // Unary minus
    and walk_unary_minus (expr: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith "\"kind\": \"-\", "
           
        text "\"expr\": "
        walk_expr expr.Syntax; endLine()

        endWith "}"


    // Arith
    and walk_arith (left: AstNode<ExprSyntax>, op: string, right: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith $"\"kind\": \"%s{op}\", "
           
        text "\"left\": "
        walk_expr left.Syntax; endLineWith ", "

        text "\"right\": "
        walk_expr right.Syntax; endLine()

        endWith "}"


    // Parenthesized expr
    and walk_parens_expr (expr: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith "\"kind\": \"parenthesized\", "
           
        text "\"expr\": "
        walk_expr expr.Syntax; endLine()

        endWith "}"


    and walk_var_formal (var_formal: VarFormalSyntax, index: int): unit =
        if index > 0
        then
            endLineWith ", "
            
        beginWith "{"

        endLineWith $"\"name\": \"%s{var_formal.ID.Syntax.Value}\", "
        endLineWith $"\"type\": \"%s{var_formal.TYPE.Syntax.Value}\""

        endWith "}"


    and walk_var_formals (var_formals: AstNode<VarFormalSyntax> []): unit =
        if var_formals.Length = 0
        then
            text "[]"
        else
            beginWith "[ "
            var_formals |> Array.iteri (fun i it -> walk_var_formal(it.Syntax, i)); endLine()
            endWith "]"


    and walk_formal (formal: FormalSyntax, index: int): unit =
        if index > 0
        then
            endLineWith ", "
            
        beginWith "{"

        endLineWith $"\"name\": \"%s{formal.ID.Syntax.Value}\", "
        endLineWith $"\"type\": \"%s{formal.TYPE.Syntax.Value}\""
        
        endWith "}"


    and walk_formals (formals: AstNode<FormalSyntax> []): unit =
        if formals.Length = 0
        then
            text "[]"
        else
            beginWith "[ "
            formals |> Array.iteri (fun i it -> walk_formal(it.Syntax, i)); endLine()
            endWith "]"


    and walk_method (method_syntax: MethodSyntax): unit =
        beginWith "{"
        
        endLineWith "\"kind\": \"method\", "
        endLineWith $"\"name\": \"%s{method_syntax.ID.Syntax.Value}\", "
        endLineWith $"\"type\": \"%s{method_syntax.RETURN.Syntax.Value}\", "
        endLineWith $"\"overriden\": %b{method_syntax.Override}, "
        
        endLineWith "\"formals\": "
        walk_formals method_syntax.Formals; endLineWith ", "
        
        text "\"body\": "
        match method_syntax.Body.Syntax with
        | MethodBodySyntax.Expr expr ->
            walk_expr expr
        | MethodBodySyntax.Native ->
            text ("\"native\"")
        endLine()
        
        endWith "}"


    and walk_attr (attr_syntax: AttrSyntax): unit =
        beginWith "{"
           
        endLineWith "\"kind\": \"attribute\", "
        endLineWith $"\"name\": \"%s{attr_syntax.ID.Syntax.Value}\", "
        endLineWith $"\"type\": \"%s{attr_syntax.TYPE.Syntax.Value}\", "

        text "\"value\": "
        match attr_syntax.Initial.Syntax with
        | AttrInitialSyntax.Expr expr ->
            walk_expr expr
        | AttrInitialSyntax.Native ->
            endLineWith ("\"native\"")
        endLine()
        
        endWith "}"


    and walk_extends (inheritance_syntax: InheritanceSyntax voption): unit =
        match inheritance_syntax with
        | ValueSome (InheritanceSyntax.Extends extends_syntax) ->
            beginWith "{"
            endLineWith $"\"type\": \"%s{extends_syntax.SUPER.Syntax.Value}\", "
            text "\"actuals\": "
            walk_actuals (extends_syntax.Actuals); endLine()
            endWith "}"
        | ValueNone ->
            beginWith "{"
            endLineWith "\"type\": \"Any\", "
            endLineWith "\"actuals\": []"
            endLine()
            endWith "}"
        | ValueSome InheritanceSyntax.Native ->
            endLineWith "\"native\""


     and walkFeatures (features: AstNode<FeatureSyntax> []): unit =
        let visitFeature (feature_node: AstNode<FeatureSyntax>) (index: int): unit =
            if index > 0
            then
                endLineWith ", "

            match feature_node.Syntax with
            | FeatureSyntax.Method method_syntax ->
                walk_method method_syntax
            | FeatureSyntax.Attr attr_syntax ->
                walk_attr attr_syntax
            | FeatureSyntax.BracedBlock block_syntax_opt ->
                beginWith "{"

                endLineWith "\"kind\": \"block\", "
                text "\"statements\": "
                walk_braced_block block_syntax_opt; endLine()

                endWith "}"

        beginWith "["
        features |> Array.iteri (fun i it -> visitFeature it i); endLine()
        endWith "]"


     and walkClass (class_syntax: ClassSyntax): unit =
        beginWith "{"
        
        endLineWith $"\"name\": \"%s{class_syntax.NAME.Syntax.Value}\", "
        
        text "\"varformals\": "
        walk_var_formals class_syntax.VarFormals; endLineWith ", "

        text "\"extends\": "
        walk_extends (class_syntax.Extends |> ValueOption.map (fun it -> it.Syntax)); endLineWith ", "

        text "\"body\": "
        walkFeatures class_syntax.Features; endLine()

        endWith "}"


    // Program
    and walk_program (program_syntax: ProgramSyntax): unit =
        endLineWith "{"
        endLineWith "\"classes\": ["
        
        program_syntax.Classes
        |> Array.iteri (fun i it ->
            if i > 0
            then
                endLineWith ", "; endLine()
                
            walkClass it.Syntax
        )
        endLine()
        
        endLineWith "]"
        endLineWith "}"


    member private this.Render(program_syntax: ProgramSyntax): unit =
        walk_program program_syntax

    
    override this.ToString() = _sb_ast.ToString()


    static member Render(ast: ProgramSyntax) =
        let renderer = AstRenderer()
        renderer.Render(ast)
        renderer.ToString()
