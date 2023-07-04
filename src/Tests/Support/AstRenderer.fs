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


    let rec walkExpr (expr_syntax: ExprSyntax): unit =
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
            walkComparison (left, "<=", right)
        | ExprSyntax.GtEq(left, right) ->
            walkComparison (left, ">=", right)
        | ExprSyntax.Lt(left, right) ->
            walkComparison (left, "<", right)
        | ExprSyntax.Gt(left, right) ->
            walkComparison (left, ">", right)
        | ExprSyntax.EqEq(left, right) ->
            walkComparison (left, "==", right)
        | ExprSyntax.NotEq(left, right) ->
            walkComparison (left, "!=", right)
        | ExprSyntax.Mul(left, right) ->
            walkArith (left, "*", right)
        | ExprSyntax.Div(left, right) ->
            walkArith (left, "/", right)
        | ExprSyntax.Sum(left, right) ->
            walkArith (left, "+", right)
        | ExprSyntax.Sub(left, right) ->
            walkArith (left, "-", right)
        | ExprSyntax.Match(expr, cases_hd, cases_tl) ->
            walkMatch (expr, cases_hd, cases_tl)
        | ExprSyntax.Dispatch(obj_expr, method_id, actuals) ->
            walkDispatch (obj_expr, method_id, actuals)
        // Primary expressions
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
        
    
    and walkVar (var_syntax: VarSyntax): unit =
        beginWith "{"
        
        endLineWith "\"kind\": \"var\", "
           
        endLineWith $"\"name\": \"%s{var_syntax.ID.Syntax.Value}\", "
        endLineWith $"\"type\": \"%s{var_syntax.TYPE.Syntax.Value}\", "
        
        text "\"value\": "; walkExpr var_syntax.Expr.Syntax; endLine()
        
        endWith "}"


     and walkStmtExpr (expr_syntax: ExprSyntax): unit =
        beginWith "{"
        
        endLineWith "\"kind\": \"stmt\", "
        text "\"expr\": "; walkExpr expr_syntax; endLine()
        
        endWith "}"


     and walkBlockSyntax (block_syntax: BlockSyntax): unit =
        beginWith "[ "
        
        block_syntax.Stmts
        |> Array.iter (fun it ->
            match it.Syntax with
            | StmtSyntax.Var var_syntax -> walkVar var_syntax
            | StmtSyntax.Expr expr -> walkStmtExpr expr
            endLineWith ", ")

        beginWith "{"

        endLineWith "\"kind\": \"expr\", "

        text "\"expr\": "
        walkExpr block_syntax.Expr.Syntax; endLine()

        endWith "}"; endLine()
        
        endWith "]"


    and walkBracedBlock (block_syntax: BlockSyntax voption): unit =
        match block_syntax with
        | ValueSome block_syntax ->
            walkBlockSyntax block_syntax
        | ValueNone ->
            text ("[]")


    and walkCaseBlock (block: CaseBlockSyntax) =
        beginWith "{"
        
        match block with
        | CaseBlockSyntax.Free block_syntax ->
            endLineWith "\"kind\": \"implicit\", "
            text "\"statements\": "
            walkBlockSyntax block_syntax; endLine()
        | CaseBlockSyntax.Braced block_syntax_opt ->
            endLineWith "\"kind\": \"braced\", "
            text "\"statements\": "
            walkBracedBlock block_syntax_opt; endLine()
            
        endWith "}"


    and walkAssign (lvalue: AstNode<ID>, rvalue: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith "\"kind\": \"=\", "
           
        endLineWith $"\"left\": \"%s{lvalue.Syntax.Value}\", "
        
        text "\"right\": "
        walkExpr rvalue.Syntax; endLine ()

        endWith "}"


    and walkIf (condition: AstNode<ExprSyntax>, then_branch: AstNode<ExprSyntax>, else_branch: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith "\"kind\": \"if\", "
           
        text "\"cond\": "
        walkExpr condition.Syntax; endLineWith ", "
        
        text "\"then\": "
        walkExpr then_branch.Syntax; endLineWith ", "

        text "\"else\": "
        walkExpr else_branch.Syntax; endLine()

        endWith "}"


    and walkWhile (condition: AstNode<ExprSyntax>, body: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith "\"kind\": \"while\", "
           
        text "\"cond\": "
        walkExpr condition.Syntax; endLineWith ", "
        
        text "\"body\": "
        walkExpr body.Syntax; endLine()

        endWith "}"


    and stringifyMatchCasePattern (pattern: PatternSyntax): string =
        match pattern with
        | PatternSyntax.IdType(node_id, node_type_name) ->
            $"%s{node_id.Syntax.Value}: %s{node_type_name.Syntax.Value}"
        | PatternSyntax.Null ->
            "null"


    and walkMatchCase (case: CaseSyntax): unit =
        beginWith "{"

        endLineWith $"\"pattern\": \"%s{stringifyMatchCasePattern case.Pattern.Syntax}\", "
        
        text "\"block\": "
        walkCaseBlock case.Block.Syntax; endLine()
        
        endWith "}"


    and walkMatchCases (cases_hd: AstNode<CaseSyntax>, cases_tl: AstNode<CaseSyntax> []): unit =
        beginWith "["
       
        walkMatchCase cases_hd.Syntax;
        cases_tl |> Array.iter (fun it ->
            endLineWith ","
            walkMatchCase it.Syntax
        )
        endLine()
       
        endWith "]"


    and walkMatch (expr: AstNode<ExprSyntax>, cases_hd: AstNode<CaseSyntax>, cases_tl: AstNode<CaseSyntax> []): unit =
        beginWith "{"

        endLineWith "\"kind\": \"match\", "
           
        text "\"expr\": "
        walkExpr expr.Syntax; endLineWith ", "
        
        text "\"cases\": "
        walkMatchCases (cases_hd, cases_tl); endLine()

        endWith "}"


    and walkActual (actual: ExprSyntax, index: int): unit =
        if index > 0
        then
            endLineWith ", "
            
        walkExpr actual


    and walkActuals (actuals: AstNode<ExprSyntax> []): unit =
        if actuals.Length = 0
        then
            text "[]"
        else
            beginWith "[ "
            actuals |> Array.iteri (fun i it -> walkActual (it.Syntax, i)); endLine()
            endWith "]"


    and walkDispatch (receiver: AstNode<ExprSyntax>, method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        beginWith "{"

        endLineWith "\"kind\": \"dispatch\", "
           
        text "\"receiver\": "
        walkExpr receiver.Syntax; endLineWith ", "
        
        endLineWith $"\"method\": \"%s{method_id.Syntax.Value}\", "

        text "\"actuals\": "
        walkActuals actuals; endLine()

        endWith "}"


    and walkImplicitThisDispatch (method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        beginWith "{"

        endLineWith "\"kind\": \"implicit_this_dispatch\", "
        endLineWith $"\"method\": \"%s{method_id.Syntax.Value}\", "

        text "\"actuals\": "
        walkActuals actuals; endLine()

        endWith "}"


    and walkSuperDispatch (method_id: AstNode<ID>, actuals: AstNode<ExprSyntax> []): unit =
        beginWith "{"

        endLineWith "\"kind\": \"super_dispatch\", "
        endLineWith $"\"method\": \"%s{method_id.Syntax.Value}\", "

        text "\"actuals\": "
        walkActuals actuals; endLine()

        endWith "}"


    // Object creation
    and walkObjectCreation (type_name: AstNode<TYPENAME>, actuals: AstNode<ExprSyntax> []): unit =
        beginWith "{"

        endLineWith "\"kind\": \"new\", "
        endLineWith $"\"type\": \"%s{type_name.Syntax.Value}\", "

        text "\"actuals\": "
        walkActuals actuals; endLine()

        endWith "}"


    // Bool negation
    and walkBoolNegation (expr: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith "\"kind\": \"!\", "
           
        text "\"expr\": "
        walkExpr expr.Syntax; endLine()

        endWith "}"


    // Compare
    and walkComparison (left: AstNode<ExprSyntax>, op: string, right: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith $"\"kind\": \"%s{op}\", "
           
        endLineWith "\"left\": "
        walkExpr left.Syntax; endLineWith ", "

        endLineWith "\"right\": "
        walkExpr right.Syntax; endLine()

        endWith "}"


    // Unary minus
    and walkUnaryMinus (expr: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith "\"kind\": \"-\", "
           
        text "\"expr\": "
        walkExpr expr.Syntax; endLine()

        endWith "}"


    // Arith
    and walkArith (left: AstNode<ExprSyntax>, op: string, right: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith $"\"kind\": \"%s{op}\", "
           
        text "\"left\": "
        walkExpr left.Syntax; endLineWith ", "

        text "\"right\": "
        walkExpr right.Syntax; endLine()

        endWith "}"


    // Parenthesized expr
    and walkParensExpr (expr: AstNode<ExprSyntax>): unit =
        beginWith "{"

        endLineWith "\"kind\": \"parenthesized\", "
           
        text "\"expr\": "
        walkExpr expr.Syntax; endLine()

        endWith "}"


    and walkVarFormal (var_formal: VarFormalSyntax, index: int): unit =
        if index > 0
        then
            endLineWith ", "
            
        beginWith "{"

        endLineWith $"\"name\": \"%s{var_formal.ID.Syntax.Value}\", "
        endLineWith $"\"type\": \"%s{var_formal.TYPE.Syntax.Value}\""

        endWith "}"


    and walkVarFormals (var_formals: AstNode<VarFormalSyntax> []): unit =
        if var_formals.Length = 0
        then
            text "[]"
        else
            beginWith "[ "
            var_formals |> Array.iteri (fun i it -> walkVarFormal(it.Syntax, i)); endLine()
            endWith "]"


    and walkFormal (formal: FormalSyntax, index: int): unit =
        if index > 0
        then
            endLineWith ", "
            
        beginWith "{"

        endLineWith $"\"name\": \"%s{formal.ID.Syntax.Value}\", "
        endLineWith $"\"type\": \"%s{formal.TYPE.Syntax.Value}\""
        
        endWith "}"


    and walkFormals (formals: AstNode<FormalSyntax> []): unit =
        if formals.Length = 0
        then
            text "[]"
        else
            beginWith "[ "
            formals |> Array.iteri (fun i it -> walkFormal(it.Syntax, i)); endLine()
            endWith "]"


    and walkMethod (method_syntax: MethodSyntax): unit =
        beginWith "{"
        
        endLineWith "\"kind\": \"method\", "
        endLineWith $"\"name\": \"%s{method_syntax.ID.Syntax.Value}\", "
        endLineWith $"\"type\": \"%s{method_syntax.RETURN.Syntax.Value}\", "
        endLineWith $"\"overriden\": %b{method_syntax.Override}, "
        
        endLineWith "\"formals\": "
        walkFormals method_syntax.Formals; endLineWith ", "
        
        text "\"body\": "
        match method_syntax.Body.Syntax with
        | MethodBodySyntax.Expr expr ->
            walkExpr expr
        | MethodBodySyntax.Native ->
            text ("\"native\"")
        endLine()
        
        endWith "}"


    and walkAttr (attr_syntax: AttrSyntax): unit =
        beginWith "{"
           
        endLineWith "\"kind\": \"attribute\", "
        endLineWith $"\"name\": \"%s{attr_syntax.ID.Syntax.Value}\", "
        endLineWith $"\"type\": \"%s{attr_syntax.TYPE.Syntax.Value}\", "

        text "\"value\": "
        match attr_syntax.Initial.Syntax with
        | AttrInitialSyntax.Expr expr ->
            walkExpr expr
        | AttrInitialSyntax.Native ->
            endLineWith ("\"native\"")
        endLine()
        
        endWith "}"


    and walkExtends (inheritance_syntax: InheritanceSyntax voption): unit =
        match inheritance_syntax with
        | ValueSome (InheritanceSyntax.Extends extends_syntax) ->
            beginWith "{"
            endLineWith $"\"type\": \"%s{extends_syntax.SUPER.Syntax.Value}\", "
            text "\"actuals\": "
            walkActuals (extends_syntax.Actuals); endLine()
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
                walkMethod method_syntax
            | FeatureSyntax.Attr attr_syntax ->
                walkAttr attr_syntax
            | FeatureSyntax.BracedBlock block_syntax_opt ->
                beginWith "{"

                endLineWith "\"kind\": \"block\", "
                text "\"statements\": "
                walkBracedBlock block_syntax_opt; endLine()

                endWith "}"

        beginWith "["
        features |> Array.iteri (fun i it -> visitFeature it i); endLine()
        endWith "]"


     and walkClass (class_syntax: ClassSyntax): unit =
        beginWith "{"
        
        endLineWith $"\"name\": \"%s{class_syntax.NAME.Syntax.Value}\", "
        
        text "\"varformals\": "
        walkVarFormals class_syntax.VarFormals; endLineWith ", "

        text "\"extends\": "
        walkExtends (class_syntax.Extends |> ValueOption.map (fun it -> it.Syntax)); endLineWith ", "

        text "\"body\": "
        walkFeatures class_syntax.Features; endLine()

        endWith "}"


    // Program
    and walkProgram (program_syntax: ProgramSyntax): unit =
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
        walkProgram program_syntax

    
    override this.ToString() = _sb_ast.ToString()


    static member Render(ast: ProgramSyntax) =
        let renderer = AstRenderer()
        renderer.Render(ast)
        renderer.ToString()
