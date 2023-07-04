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

    let rec beforeBracedBlockOrExpr (it: ExprSyntax) : unit =
        match it with
        | ExprSyntax.BracedBlock _ ->
            ()
        | _ ->
            _indent.Increase()
            _sb_cool.AppendLine().Append(_indent).AsUnit()
   

    and afterBracedBlockOrExpr (it: ExprSyntax) : unit =
        match it with
        | ExprSyntax.BracedBlock _ -> ()
        | _                  -> _indent.Decrease()
    
    
    and walkExpr (expr_syntax: ExprSyntax): unit =
        match expr_syntax with
        | ExprSyntax.Assign(left, right) ->
            walkAssign (left, right)
        | ExprSyntax.BoolNegation negated_expr ->
            walkBoolNegation (negated_expr)
        | ExprSyntax.UnaryMinus expr ->
            walkUnaryMinus (expr)
        | ExprSyntax.If(condition, then_branch, else_branch) ->
            walkIf (condition, then_branch, else_branch)
        | ExprSyntax.While(condition, body) ->
            walkWhile (condition, body)
        | ExprSyntax.LtEq(left, right) ->
            walkComparison (left, CompareOp.LtEq, right)
        | ExprSyntax.GtEq(left, right) ->
            walkComparison (left, CompareOp.GtEq, right)
        | ExprSyntax.Lt(left, right) ->
            walkComparison (left, CompareOp.Lt, right)
        | ExprSyntax.Gt(left, right) ->
            walkComparison (left, CompareOp.Gt, right)
        | ExprSyntax.EqEq(left, right) ->
            walkComparison (left, CompareOp.EqEq, right)
        | ExprSyntax.NotEq(left, right) ->
            walkComparison (left, CompareOp.NotEq, right)
        | ExprSyntax.Mul(left, right) ->
            walkArith (left, ArithOp.Mul, right)
        | ExprSyntax.Div(left, right) ->
            walkArith (left, ArithOp.Div, right)
        | ExprSyntax.Sum(left, right) ->
            walkArith (left, ArithOp.Sum, right)
        | ExprSyntax.Sub(left, right) ->
            walkArith (left, ArithOp.Sub, right)
        | ExprSyntax.Match(expr, cases_hd, cases_tl) ->
            walkMatch (expr, cases_hd, cases_tl)
        | ExprSyntax.Dispatch(obj_expr, method_id, actuals) ->
            walkDispatch (obj_expr, method_id, actuals)
        | ExprSyntax.ImplicitThisDispatch(method_id, actuals) ->
            walkImplicitThisDispatch (method_id, actuals)
        | ExprSyntax.SuperDispatch(method_id, actuals) ->
            walkSuperDispatch (method_id, actuals)
        | ExprSyntax.New(class_name, actuals) ->
            walkObjectCreation (class_name, actuals)
        | ExprSyntax.BracedBlock block_syntax_opt ->
            walkBracedBlock block_syntax_opt
        | ExprSyntax.ParensExpr node_expr ->
            walkParensExpr node_expr
        | ExprSyntax.Id value ->
            _sb_cool.Append(value).AsUnit()
        | ExprSyntax.Int value ->
            _sb_cool.Append(value).AsUnit()
        | ExprSyntax.Str value ->
            _sb_cool.Append(value).AsUnit()
        | ExprSyntax.Bool value ->
            _sb_cool.Append(value).AsUnit()
        | ExprSyntax.This ->
            _sb_cool.Append("this").AsUnit()
        | ExprSyntax.Null ->
            _sb_cool.Append("null").AsUnit()
        | ExprSyntax.Unit ->
            _sb_cool.Append("()").AsUnit()

    
    and walkActual (actual: ExprSyntax, index: int): unit =
        if index > 0
        then
            _sb_cool.Append(", ").AsUnit()

        walkExpr actual


    and walkActuals (actuals: AstNode<ExprSyntax> []): unit =
        _sb_cool.Append("(").AsUnit()
        actuals |> Array.iteri (fun i it -> walkActual (it.Syntax, i))
        _sb_cool.Append(")").AsUnit()


    and walkVar (var_syntax: VarSyntax): unit =
        _sb_cool
            .Append("var ")
            .Append(var_syntax.ID.Syntax)
            .Append(": ")
            .Append(var_syntax.TYPE.Syntax)
            .Append(" = ")
            .AsUnit()
        
        walkExpr(var_syntax.Expr.Syntax)

        _sb_cool.Append(";")
                .AppendLine().AsUnit()


     and walkStmtExpr (expr: ExprSyntax): unit =
        walkExpr expr
        _sb_cool.Append(";")
                .AppendLine().AsUnit()


     and walkBlockSyntax (block_syntax: BlockSyntax): unit =
        block_syntax.Stmts
        |> Array.iteri (fun i it ->
            if i > 0
            then
                _sb_cool.Append(_indent).AsUnit()

            match it.Syntax with
            | StmtSyntax.Var var_syntax -> walkVar var_syntax
            | StmtSyntax.Expr expr -> walkStmtExpr expr)

        let block_expr = block_syntax.Expr
    
        // If it's the only expression in the block,
        // it's also the first.
        // We don't want to ident the first expression/stmt of the block
        // as the caller already added an indent.
        if block_syntax.Stmts.Length > 0
        then
            _sb_cool.Append(_indent).AsUnit()
            
        walkExpr block_expr.Syntax
        _sb_cool.AppendLine().AsUnit()


     and walkBracedBlock (block_syntax: BlockSyntax voption): unit =
        _sb_cool.Append("{").AsUnit()
        _indent.Increase()

        let indent = 
            match block_syntax with
            | ValueSome block_syntax ->
                _sb_cool.AppendLine().Append(_indent).AsUnit()
                walkBlockSyntax block_syntax
                true
            | ValueNone ->
                false
            
        _indent.Decrease()
        _sb_cool
            .Append(if indent then _indent.ToString() else "")
            .Append("}")
            .AsUnit()


    and walkCaseBlock (caseblock_syntax: CaseBlockSyntax) =
       match caseblock_syntax with
       | CaseBlockSyntax.Free block_syntax ->
           walkBlockSyntax block_syntax
       | CaseBlockSyntax.Braced block_syntax_opt ->
           walkBracedBlock block_syntax_opt


    and walkAssign (lvalue: AstNode<ID>, rvalue: AstNode<ExprSyntax>): unit =
       _sb_cool.Append(lvalue.Syntax)
                     .Append(" = ").AsUnit()
       walkExpr rvalue.Syntax


    and walkIf (condition: AstNode<ExprSyntax>, then_branch: AstNode<ExprSyntax>, else_branch: AstNode<ExprSyntax>): unit =
        _sb_cool.Append("if (").AsUnit()
        walkExpr condition.Syntax
        _sb_cool.Append(") ").AsUnit()
        
        // Enter 'then' branch.
        beforeBracedBlockOrExpr then_branch.Syntax
        walkExpr then_branch.Syntax

        match then_branch.Syntax with
        | ExprSyntax.BracedBlock _ ->
            _sb_cool.Append(" ").AsUnit()
        | _ ->
            _indent.Decrease()
            _sb_cool.AppendLine().Append(_indent).AsUnit()
        
        _sb_cool.Append("else ").AsUnit()
        
        // Enter 'else' branch.        
        beforeBracedBlockOrExpr else_branch.Syntax
        walkExpr else_branch.Syntax
        afterBracedBlockOrExpr else_branch.Syntax


    and walkWhile (condition: AstNode<ExprSyntax>, body: AstNode<ExprSyntax>): unit =
        _sb_cool.Append("while (").AsUnit()
        walkExpr condition.Syntax
        _sb_cool.Append(") ").AsUnit()
 
        beforeBracedBlockOrExpr body.Syntax
        walkExpr body.Syntax
        afterBracedBlockOrExpr body.Syntax


    and walkMatchCasePattern (pattern: PatternSyntax): unit =
       match pattern with
       | PatternSyntax.IdType(node_id, node_type_name) ->
           _sb_cool
               .Append(node_id.Syntax)
               .Append(": ")
               .Append(node_type_name.Syntax)
               .AsUnit()
       | PatternSyntax.Null ->
           _sb_cool.Append("null").AsUnit()


    and walkMatchCase (case: CaseSyntax): unit =
       _sb_cool.Append(_indent)
               .Append("case ").AsUnit()
       walkMatchCasePattern case.Pattern.Syntax
       _sb_cool.Append(" => ").AsUnit()
       walkCaseBlock case.Block.Syntax


    and walkMatchCases (cases_hd: AstNode<CaseSyntax>, cases_tl: AstNode<CaseSyntax> []): unit =
       _sb_cool
           .Append("{")
           .AppendLine().AsUnit()
       _indent.Increase()

       walkMatchCase cases_hd.Syntax
       cases_tl |> Array.iter (fun it -> walkMatchCase it.Syntax)
       
       _indent.Decrease()
       _sb_cool
           .Append(_indent)
           .Append("}").AsUnit()


    and walkMatch (expr: AstNode<ExprSyntax>, cases_hd: AstNode<CaseSyntax>, cases_tl: AstNode<CaseSyntax> []): unit =
        walkExpr expr.Syntax

        _sb_cool.Append(" match ").AsUnit()

        walkMatchCases (cases_hd, cases_tl)


    and walkDispatch (obj_expr: AstNode<ExprSyntax>, method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        walkExpr obj_expr.Syntax
        _sb_cool.Append(".")
                .Append(method_id.Syntax).AsUnit()
        walkActuals (actuals)


    and walkImplicitThisDispatch (method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        _sb_cool.Append(method_id.Syntax).AsUnit()
        walkActuals actuals


    and walkSuperDispatch (method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        _sb_cool.Append("super.")
                .Append(method_id.Syntax).AsUnit()
        walkActuals actuals


    and walkObjectCreation (type_name: AstNode<TYPENAME>, actuals: AstNode<ExprSyntax> []): unit =
        _sb_cool.Append("new ")
                .Append(type_name.Syntax).AsUnit()
        walkActuals (actuals)


    and walkBoolNegation (expr: AstNode<ExprSyntax>): unit =
        _sb_cool.Append("!").AsUnit()
        walkExpr expr.Syntax


    and walkComparison (left: AstNode<ExprSyntax>, op: CompareOp, right: AstNode<ExprSyntax>): unit =
        walkExpr left.Syntax

        (match op with
        | CompareOp.LtEq -> _sb_cool.Append(" <= ")
        | CompareOp.GtEq -> _sb_cool.Append(" >= ")
        | CompareOp.Lt -> _sb_cool.Append(" < ")
        | CompareOp.Gt -> _sb_cool.Append(" > ")
        | CompareOp.EqEq -> _sb_cool.Append(" == ")
        | CompareOp.NotEq -> _sb_cool.Append(" != ")
        ).AsUnit()

        walkExpr right.Syntax


    and walkUnaryMinus (expr: AstNode<ExprSyntax>): unit =
       _sb_cool.Append("-").AsUnit()
       walkExpr expr.Syntax


    and walkArith (left: AstNode<ExprSyntax>, op: ArithOp, right: AstNode<ExprSyntax>): unit =
       walkExpr left.Syntax

       (match op with
       | ArithOp.Mul -> _sb_cool.Append(" * ")
       | ArithOp.Div -> _sb_cool.Append(" / ")
       | ArithOp.Sum -> _sb_cool.Append(" + ")
       | ArithOp.Sub -> _sb_cool.Append(" - ")
       ).AsUnit()

       walkExpr right.Syntax


    and walkParensExpr (expr: AstNode<ExprSyntax>): unit =
        _sb_cool.Append("(").AsUnit()
        walkExpr expr.Syntax
        _sb_cool.Append(")").AsUnit()


    and walkVarFormal (var_formal: VarFormalSyntax, index: int): unit =
        _sb_cool.Append(if index > 0 then ", " else "")
                .Append("var ")
                .Append(var_formal.ID.Syntax)
                .Append(": ")
                .Append(var_formal.TYPE.Syntax).AsUnit()


    and walkVarFormals (var_formals: AstNode<VarFormalSyntax> []): unit =
        _sb_cool.Append("(").AsUnit()
        var_formals |> Array.iteri (fun i it -> walkVarFormal(it.Syntax, i))
        _sb_cool.Append(")").AsUnit()


    and walkFormal (formal: FormalSyntax, index: int): unit =
         _sb_cool.Append(if index > 0 then ", " else "")
                 .Append(formal.ID.Syntax)
                 .Append(": ")
                 .Append(formal.TYPE.Syntax).AsUnit()


    and walkFormals (formals: AstNode<FormalSyntax> []): unit =
        _sb_cool.Append("(").AsUnit()
        formals |> Array.iteri (fun i it -> walkFormal(it.Syntax, i))
        _sb_cool.Append(")").AsUnit()


    and walkMethod (method_syntax: MethodSyntax): unit =
        _sb_cool.Append(_indent)
                .Append(if method_syntax.Override then "override def " else "def ")
                .Append(method_syntax.ID.Syntax)
                .AsUnit()
        
        walkFormals method_syntax.Formals
        
        _sb_cool.Append(": ")
                .Append(method_syntax.RETURN.Syntax)
                .Append(" = ")
                .AsUnit()
        
        let method_body = method_syntax.Body.Syntax
        
        match method_body with
        | MethodBodySyntax.Expr expr ->
            beforeBracedBlockOrExpr expr
        | _ ->
            ()
        
        match method_body with
        | MethodBodySyntax.Expr expr ->
            walkExpr expr
        | MethodBodySyntax.Native ->
            _sb_cool.Append("native").AsUnit()
        
        match method_body with
        | MethodBodySyntax.Expr expr ->
           match expr with
           | ExprSyntax.BracedBlock _ -> ()
           | _                  -> _indent.Decrease()
        | _ ->
            ()


     and walkAttr (attr_syntax: AttrSyntax): unit =
        _sb_cool
            .Append(_indent)
            .Append("var ")
            .Append(attr_syntax.ID.Syntax)
            .Append(": ")
            .Append(attr_syntax.TYPE.Syntax).AsUnit()

        let attr_body = attr_syntax.Initial.Syntax
        _sb_cool.Append(" = ").AsUnit()
        
        match attr_body with
        | AttrInitialSyntax.Expr expr ->
            beforeBracedBlockOrExpr expr
            walkExpr expr
            afterBracedBlockOrExpr expr
        | AttrInitialSyntax.Native ->
            _sb_cool.Append("native").AsUnit()


     and walkExtends (extends: InheritanceSyntax): unit =
        _sb_cool.Append(" extends ").AsUnit()

        match extends with
        | InheritanceSyntax.Extends extends_syntax ->
            _sb_cool.Append(extends_syntax.SUPER.Syntax).AsUnit()
            walkActuals (extends_syntax.Actuals)
        | InheritanceSyntax.Native ->
            _sb_cool.Append("native").AsUnit()


     and walkFeatures (features: AstNode<FeatureSyntax> []): unit =
        _sb_cool.Append(" {")
                .AppendLine().AsUnit()
        _indent.Increase()

        let visitFeature (feature_node: AstNode<FeatureSyntax>): unit =
            match feature_node.Syntax with
            | FeatureSyntax.Method method_syntax ->
                walkMethod method_syntax
            | FeatureSyntax.Attr attr_syntax ->
                walkAttr attr_syntax
            | FeatureSyntax.BracedBlock block_syntax_opt ->
                _sb_cool.Append(_indent).AsUnit()
                walkBracedBlock block_syntax_opt

        features |> Array.iter (fun it -> _sb_cool.AppendLine().AsUnit()
                                          visitFeature it
                                          _sb_cool.Append(";").AppendLine().AsUnit())
        
        _indent.Decrease()
        _sb_cool.Append(_indent)
                .Append("}").AsUnit()


     and walkClass (klass: ClassSyntax): unit =
        _sb_cool
            .Append(_indent)
            .Append("class ")
            .AsUnit()

        _sb_cool.Append(klass.NAME.Syntax.Value).AsUnit()
        walkVarFormals klass.VarFormals

        match klass.Extends with
        | ValueSome extends_node ->
            walkExtends (extends_node.Syntax)
        | ValueNone ->
            ()

        walkFeatures klass.Features


    and walkProgram (program: ProgramSyntax): unit =
        program.Classes
        |> Array.iter (fun it -> walkClass it.Syntax
                                 _sb_cool.AppendLine().AppendLine().AsUnit())


    member private this.Render(ast: ProgramSyntax): unit =
        walkProgram ast

    
    override this.ToString() = _sb_cool.ToString()


    static member Render(ast: ProgramSyntax) =
        let renderer = CoolRenderer()
        renderer.Render(ast)
        renderer.ToString()
