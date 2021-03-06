namespace Tests.Parser


open System.Text
open LibCool.SharedParts
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
    
    
    and walk_expr (expr_syntax: ExprSyntax): unit =
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
        | ExprSyntax.BracedBlock block_syntax_opt ->
            walk_braced_block block_syntax_opt
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
        actuals |> Array.iteri (fun i it -> walk_actual (it.Syntax, i))
        _sb_cool.Append(")").Nop()


    and walk_var (var_syntax: VarSyntax): unit =
        _sb_cool
            .Append("var ")
            .Append(var_syntax.ID.Syntax)
            .Append(": ")
            .Append(var_syntax.TYPE.Syntax)
            .Append(" = ")
            .Nop()
        
        walk_expr(var_syntax.Expr.Syntax)

        _sb_cool.Append(";")
                .AppendLine().Nop()


     and walk_stmt_expr (expr: ExprSyntax): unit =
        walk_expr expr
        _sb_cool.Append(";")
                .AppendLine().Nop()


     and walk_block_syntax (block_syntax: BlockSyntax): unit =
        block_syntax.Stmts
        |> Array.iteri (fun i it ->
            if i > 0
            then
                _sb_cool.Append(_indent).Nop()

            match it.Syntax with
            | StmtSyntax.Var var_syntax -> walk_var var_syntax
            | StmtSyntax.Expr expr -> walk_stmt_expr expr)

        let block_expr = block_syntax.Expr
    
        // If it's the only expression in the block,
        // it's also the first.
        // We don't want to ident the first expression/stmt of the block
        // as the caller already added an indent.
        if block_syntax.Stmts.Length > 0
        then
            _sb_cool.Append(_indent).Nop()
            
        walk_expr block_expr.Syntax
        _sb_cool.AppendLine().Nop()


     and walk_braced_block (block_syntax: BlockSyntax voption): unit =
        _sb_cool.Append("{").Nop()
        _indent.Increase()

        let indent = 
            match block_syntax with
            | ValueSome block_syntax ->
                _sb_cool.AppendLine().Append(_indent).Nop()
                walk_block_syntax block_syntax
                true
            | ValueNone ->
                false
            
        _indent.Decrease()
        _sb_cool
            .Append(if indent then _indent.ToString() else "")
            .Append("}")
            .Nop()


    and walk_caseblock (caseblock_syntax: CaseBlockSyntax) =
       match caseblock_syntax with
       | CaseBlockSyntax.Free block_syntax ->
           walk_block_syntax block_syntax
       | CaseBlockSyntax.Braced block_syntax_opt ->
           walk_braced_block block_syntax_opt


    and walk_assign (lvalue: AstNode<ID>, rvalue: AstNode<ExprSyntax>): unit =
       _sb_cool.Append(lvalue.Syntax)
                     .Append(" = ").Nop()
       walk_expr rvalue.Syntax


    and walk_if (condition: AstNode<ExprSyntax>, then_branch: AstNode<ExprSyntax>, else_branch: AstNode<ExprSyntax>): unit =
        _sb_cool.Append("if (").Nop()
        walk_expr condition.Syntax
        _sb_cool.Append(") ").Nop()
        
        // Enter 'then' branch.
        before_braced_block_or_expr then_branch.Syntax
        walk_expr then_branch.Syntax

        match then_branch.Syntax with
        | ExprSyntax.BracedBlock _ ->
            _sb_cool.Append(" ").Nop()
        | _ ->
            _indent.Decrease()
            _sb_cool.AppendLine().Append(_indent).Nop()
        
        _sb_cool.Append("else ").Nop()
        
        // Enter 'else' branch.        
        before_braced_block_or_expr else_branch.Syntax
        walk_expr else_branch.Syntax
        after_braced_block_or_expr else_branch.Syntax


    and walk_while (condition: AstNode<ExprSyntax>, body: AstNode<ExprSyntax>): unit =
        _sb_cool.Append("while (").Nop()
        walk_expr condition.Syntax
        _sb_cool.Append(") ").Nop()
 
        before_braced_block_or_expr body.Syntax
        walk_expr body.Syntax
        after_braced_block_or_expr body.Syntax


    and walk_match_case_pattern (pattern: PatternSyntax): unit =
       match pattern with
       | PatternSyntax.IdType(node_id, node_type_name) ->
           _sb_cool
               .Append(node_id.Syntax)
               .Append(": ")
               .Append(node_type_name.Syntax)
               .Nop()
       | PatternSyntax.Null ->
           _sb_cool.Append("null").Nop()


    and walk_match_case (case: CaseSyntax): unit =
       _sb_cool.Append(_indent)
               .Append("case ").Nop()
       walk_match_case_pattern case.Pattern.Syntax
       _sb_cool.Append(" => ").Nop()
       walk_caseblock case.Block.Syntax


    and walk_match_cases (cases_hd: AstNode<CaseSyntax>, cases_tl: AstNode<CaseSyntax> []): unit =
       _sb_cool
           .Append("{")
           .AppendLine().Nop()
       _indent.Increase()

       walk_match_case cases_hd.Syntax
       cases_tl |> Array.iter (fun it -> walk_match_case it.Syntax)
       
       _indent.Decrease()
       _sb_cool
           .Append(_indent)
           .Append("}").Nop()


    and walk_match (expr: AstNode<ExprSyntax>, cases_hd: AstNode<CaseSyntax>, cases_tl: AstNode<CaseSyntax> []): unit =
        walk_expr expr.Syntax

        _sb_cool.Append(" match ").Nop()

        walk_match_cases (cases_hd, cases_tl)


    and walk_dispatch (obj_expr: AstNode<ExprSyntax>, method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        walk_expr obj_expr.Syntax
        _sb_cool.Append(".")
                .Append(method_id.Syntax).Nop()
        walk_actuals (actuals)


    and walk_implicit_this_dispatch (method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        _sb_cool.Append(method_id.Syntax).Nop()
        walk_actuals actuals


    and walk_super_dispatch (method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        _sb_cool.Append("super.")
                .Append(method_id.Syntax).Nop()
        walk_actuals actuals


    and walk_object_creation (type_name: AstNode<TYPENAME>, actuals: AstNode<ExprSyntax> []): unit =
        _sb_cool.Append("new ")
                .Append(type_name.Syntax).Nop()
        walk_actuals (actuals)


    and walk_bool_negation (expr: AstNode<ExprSyntax>): unit =
        _sb_cool.Append("!").Nop()
        walk_expr expr.Syntax


    and walk_comparison (left: AstNode<ExprSyntax>, op: CompareOp, right: AstNode<ExprSyntax>): unit =
        walk_expr left.Syntax

        (match op with
        | CompareOp.LtEq -> _sb_cool.Append(" <= ")
        | CompareOp.GtEq -> _sb_cool.Append(" >= ")
        | CompareOp.Lt -> _sb_cool.Append(" < ")
        | CompareOp.Gt -> _sb_cool.Append(" > ")
        | CompareOp.EqEq -> _sb_cool.Append(" == ")
        | CompareOp.NotEq -> _sb_cool.Append(" != ")
        ).Nop()

        walk_expr right.Syntax


    and walk_unary_minus (expr: AstNode<ExprSyntax>): unit =
       _sb_cool.Append("-").Nop()
       walk_expr expr.Syntax


    and walk_arith (left: AstNode<ExprSyntax>, op: ArithOp, right: AstNode<ExprSyntax>): unit =
       walk_expr left.Syntax

       (match op with
       | ArithOp.Mul -> _sb_cool.Append(" * ")
       | ArithOp.Div -> _sb_cool.Append(" / ")
       | ArithOp.Sum -> _sb_cool.Append(" + ")
       | ArithOp.Sub -> _sb_cool.Append(" - ")
       ).Nop()

       walk_expr right.Syntax


    and walk_parens_expr (expr: AstNode<ExprSyntax>): unit =
        _sb_cool.Append("(").Nop()
        walk_expr expr.Syntax
        _sb_cool.Append(")").Nop()


    and walk_var_formal (var_formal: VarFormalSyntax, index: int): unit =
        _sb_cool.Append(if index > 0 then ", " else "")
                .Append("var ")
                .Append(var_formal.ID.Syntax)
                .Append(": ")
                .Append(var_formal.TYPE.Syntax).Nop()


    and walk_var_formals (var_formals: AstNode<VarFormalSyntax> []): unit =
        _sb_cool.Append("(").Nop()
        var_formals |> Array.iteri (fun i it -> walk_var_formal(it.Syntax, i))
        _sb_cool.Append(")").Nop()


    and walk_formal (formal: FormalSyntax, index: int): unit =
         _sb_cool.Append(if index > 0 then ", " else "")
                 .Append(formal.ID.Syntax)
                 .Append(": ")
                 .Append(formal.TYPE.Syntax).Nop()


    and walk_formals (formals: AstNode<FormalSyntax> []): unit =
        _sb_cool.Append("(").Nop()
        formals |> Array.iteri (fun i it -> walk_formal(it.Syntax, i))
        _sb_cool.Append(")").Nop()


    and walk_method (method_syntax: MethodSyntax): unit =
        _sb_cool.Append(_indent)
                .Append(if method_syntax.Override then "override def " else "def ")
                .Append(method_syntax.ID.Syntax)
                .Nop()
        
        walk_formals method_syntax.Formals
        
        _sb_cool.Append(": ")
                .Append(method_syntax.RETURN.Syntax)
                .Append(" = ")
                .Nop()
        
        let method_body = method_syntax.Body.Syntax
        
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


     and walk_attr (attr_syntax: AttrSyntax): unit =
        _sb_cool
            .Append(_indent)
            .Append("var ")
            .Append(attr_syntax.ID.Syntax)
            .Append(": ")
            .Append(attr_syntax.TYPE.Syntax).Nop()

        let attr_body = attr_syntax.Initial.Syntax
        _sb_cool.Append(" = ").Nop()
        
        match attr_body with
        | AttrInitialSyntax.Expr expr ->
            before_braced_block_or_expr expr
            walk_expr expr
            after_braced_block_or_expr expr            
        | AttrInitialSyntax.Native ->
            _sb_cool.Append("native").Nop()


     and walk_extends (extends: InheritanceSyntax): unit =
        _sb_cool.Append(" extends ").Nop()

        match extends with
        | InheritanceSyntax.Extends extends_syntax ->
            _sb_cool.Append(extends_syntax.SUPER.Syntax).Nop()
            walk_actuals (extends_syntax.Actuals)
        | InheritanceSyntax.Native ->
            _sb_cool.Append("native").Nop()


     and walk_features (features: AstNode<FeatureSyntax> []): unit =
        _sb_cool.Append(" {")
                .AppendLine().Nop()
        _indent.Increase()

        let visit_feature (feature_node: AstNode<FeatureSyntax>): unit =
            match feature_node.Syntax with
            | FeatureSyntax.Method method_syntax ->
                walk_method method_syntax
            | FeatureSyntax.Attr attr_syntax ->
                walk_attr attr_syntax
            | FeatureSyntax.BracedBlock block_syntax_opt ->
                _sb_cool.Append(_indent).Nop()
                walk_braced_block block_syntax_opt

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

        _sb_cool.Append(klass.NAME.Syntax.Value).Nop()
        walk_var_formals klass.VarFormals

        match klass.Extends with
        | ValueSome extends_node ->
            walk_extends (extends_node.Syntax)
        | ValueNone ->
            ()

        walk_features klass.Features


    and walk_program (program: ProgramSyntax): unit =
        program.Classes
        |> Array.iter (fun it -> walk_class it.Syntax
                                 _sb_cool.AppendLine().AppendLine().Nop())


    member private this.Render(ast: ProgramSyntax): unit =
        walk_program ast

    
    override this.ToString() = _sb_cool.ToString()


    static member Render(ast: ProgramSyntax) =
        let renderer = CoolRenderer()
        renderer.Render(ast)
        renderer.ToString()
