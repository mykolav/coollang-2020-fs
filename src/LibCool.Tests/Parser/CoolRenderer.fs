namespace LibCool.Tests.Parser

open System
open System.Text
open LibCool.SourceParts
open LibCool.Ast

[<Sealed>]
type private Indent(?width: int) =
    let _width = defaultArg width 2
    let mutable _level = 0
    
    let mk_value () = String(' ', count = _level * _width)
    let mutable _value = mk_value ()
    
    
    member _.Increase() =
        _level <- _level + 1
        _value <- mk_value ()
        
    
    member _.Decrease() =
        if _level = 0
        then
            invalidOp "An indent's level cannot go less than 0"
            
        _level <- _level - 1
        _value <- mk_value ()
       
    
    override _.ToString() = _value


[<Sealed>]
type CoolRenderer private () =
    inherit AstListener()
    

    let _indent = Indent()
    let _acc_cool_text = StringBuilder()

        
    // Classes
    override _.EnterClass(_: ClassDecl, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text
            .Append(_indent)
            .Append("class ") |> ignore
        
    
    override _.EnterVarFormals(_: Node<VarFormal>[]) : unit =
        _acc_cool_text.Append("(") |> ignore
    
    override _.LeaveVarFormals(_: Node<VarFormal>[]) : unit =
        _acc_cool_text.Append(")") |> ignore

    
    override _.EnterVarFormal(_: VarFormal, index: int, _: Guid, _: HalfOpenRange) : unit =
        if index > 0
        then do
            _acc_cool_text.Append(", ") |> ignore
        
    
    override _.EnterExtends(_: Extends, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.Append(" extends ") |> ignore
    
    
    override _.EnterFeatures(_: Node<Feature>[]) : unit =
        _acc_cool_text
            .Append(" {")
            .AppendLine() |> ignore
        _indent.Increase()
            
    override _.LeaveFeatures(_: Node<Feature>[]) : unit =
        _indent.Decrease()
        _acc_cool_text
            .Append(_indent)
            .Append("}")
            .AppendLine() |> ignore

    // Method
    override _.EnterMethod(method_info: MethodInfo, _: Guid, _: HalfOpenRange) =
        _acc_cool_text
            .Append(_indent)
            .Append(if method_info.Override then "override def " else "def ") |> ignore
    
    
    override _.EnterFormals(_:Node<Formal>[]) : unit =
        _acc_cool_text.Append("(") |> ignore
    
    override _.LeaveFormals(_:Node<Formal>[]) : unit =
        _acc_cool_text.Append(")") |> ignore
    
    
    override _.EnterFormal(_: Formal, index: int, _: Guid, _: HalfOpenRange) : unit =
        if index > 0
        then do
            _acc_cool_text.Append(", ") |> ignore
        
    
    override _.EnterMethodBody(method_body: MethodBody, _: Guid, _: HalfOpenRange) = 
        _acc_cool_text.Append(" = ") |> ignore
        match method_body with
        | MethodBody.Expr expr ->
            match expr with
            | Expr.BracedBlock _ -> ()
            | _ ->
                _acc_cool_text.AppendLine() |> ignore
                _indent.Increase()
        | _ -> ()
    
    override _.LeaveMethodBody(method_body: MethodBody, _: Guid, _: HalfOpenRange) = 
        match method_body with
        | MethodBody.Expr expr ->
            match expr with
            | Expr.BracedBlock _ -> ()
            | _ ->
                _indent.Decrease()
        | _ -> ()

    
    // Attribute
    override _.EnterAttr(_: AttrInfo, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text
            .Append(_indent)
            .Append("var ") |> ignore
    
    
    override _.EnterAttrBody(attr_body: AttrBody, _: Guid, _: HalfOpenRange) = 
        _acc_cool_text.Append(" = ") |> ignore
        match attr_body with
        | AttrBody.Expr expr ->
            match expr with
            | Expr.BracedBlock _ -> ()
            | _ ->
                _acc_cool_text.AppendLine() |> ignore
                _indent.Increase()
        | _ -> ()
    
    override _.LeaveAttrBody(attr_body: AttrBody, _: Guid, _: HalfOpenRange) = 
        match attr_body with
        | AttrBody.Expr expr ->
            match expr with
            | Expr.BracedBlock _ -> ()
            | _ ->
                _indent.Decrease()
        | _ -> ()


    // Block
    override _.EnterBlock(_: BlockInfo, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text
            .Append("{")
            .AppendLine() |> ignore
        _indent.Increase()
    
    override _.LeaveBlock(_: BlockInfo, _: Guid, _: HalfOpenRange) : unit =
        _indent.Decrease()
        _acc_cool_text
            .Append(_indent)
            .Append("}")
            .AppendLine() |> ignore
    
    
    override _.EnterVarDecl(_: VarDeclInfo , _: Guid , _: HalfOpenRange) : unit =
        _acc_cool_text
            .Append(_indent)
            .Append("var ") |> ignore
    
    override _.LeaveVarDecl(_: VarDeclInfo , _: Guid , _: HalfOpenRange) : unit =
        _acc_cool_text
            .Append(";")
            .AppendLine() |> ignore

    
    override _.EnterStmtExpr(_: Expr, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.Append(_indent) |> ignore

    override _.LeaveStmtExpr(_: Expr, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text
            .Append(";")
            .AppendLine() |> ignore
            
    
    override _.EnterBlockLastExpr(_: Expr, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.Append(_indent) |> ignore

    override _.LeaveBlockLastExpr(_: Expr, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.AppendLine() |> ignore

    
    // Expressions
    // Assign
    // ...    
    
    // If
    override _.EnterThenBranch(then_branch: Expr, _: Guid, _: HalfOpenRange) : unit =
        match then_branch with
        | Expr.BracedBlock _ -> ()
        | _ ->
            _acc_cool_text.AppendLine() |> ignore
            _indent.Increase()
    
    override _.LeaveThenBranch(then_branch: Expr, _: Guid, _: HalfOpenRange) : unit =
        match then_branch with
        | Expr.BracedBlock _ -> ()
        | _ ->
            _indent.Decrease()

    
    override _.EnterElseBranch(else_branch: Expr, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.Append("else") |> ignore
        
        match else_branch with
        | Expr.BracedBlock _ -> ()
        | _ ->
            _acc_cool_text.AppendLine() |> ignore
            _indent.Increase()
    
    override _.LeaveElseBranch(else_branch: Expr, _: Guid, _: HalfOpenRange) : unit =
        match else_branch with
        | Expr.BracedBlock _ -> ()
        | _ ->
            _indent.Decrease()
    
    
    // While
    override _.EnterWhileBody(body: Expr, _: Guid, _: HalfOpenRange) : unit =
        match body with
        | Expr.BracedBlock _ -> ()
        | _ ->
            _acc_cool_text.AppendLine() |> ignore
            _indent.Increase()
    
    override _.LeaveWhileBody(body: Expr, _: Guid, _: HalfOpenRange) : unit =
        match body with
        | Expr.BracedBlock _ -> ()
        | _ ->
            _indent.Decrease()
    
    
    // Match
    override _.VisitMATCH() : unit = _acc_cool_text.Append(" match ") |> ignore


    override _.EnterMatchCases(_: Node<Case>, _: Node<Case>[]) : unit =
        _acc_cool_text
            .Append("{")
            .AppendLine() |> ignore
        _indent.Increase()
    
    override _.LeaveMatchCases(_: Node<Case>, _: Node<Case>[]) : unit =
        _indent.Decrease()
        _acc_cool_text
            .Append(_indent)
            .Append("}")
            .AppendLine() |> ignore

    
    // Match/case
    override _.EnterMatchCase(_: Case, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text
            .Append(_indent)
            .Append("case ") |> ignore
    
    
    // Dispatch
    // Implicit `this` dispatch
    // Super dispatch
    override _.EnterSuperDispatch(_: Node<ID>, _: Node<Expr>[], _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.Append("super.") |> ignore
    
    
    // Object creation
    override _.EnterObjectCreation(_: Node<TYPE_NAME>, _: Node<Expr>[], _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.Append("new ") |> ignore

    
    // Bool negation
    override _.EnterBoolNegation(_: Node<Expr>, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.Append("!") |> ignore
    
    
    // Compare
    override _.VisitCOMPARISON_OP(op: CompareOp) : unit =
        match op with
        | CompareOp.LtEq -> _acc_cool_text.Append(" <= ")
        | CompareOp.GtEq -> _acc_cool_text.Append(" >= ")
        | CompareOp.Lt -> _acc_cool_text.Append(" < ")
        | CompareOp.Gt -> _acc_cool_text.Append(" > ")
        | CompareOp.EqEq -> _acc_cool_text.Append(" == ")
        | CompareOp.NotEq -> _acc_cool_text.Append(" != ")
        |> ignore

    
    // Unary minus
    override _.EnterUnaryMinus(_: Node<Expr>, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.Append("-") |> ignore

    
    // Arith
    override _.VisitARITH_OP(op: ArithOp) : unit =
        match op with
        | ArithOp.Mul -> _acc_cool_text.Append(" * ")
        | ArithOp.Div -> _acc_cool_text.Append(" / ")
        | ArithOp.Sum -> _acc_cool_text.Append(" + ")
        | ArithOp.Sub -> _acc_cool_text.Append(" - ")
        |> ignore
    
    
    // Braced block    
    override _.EnterBracedBlock(_: Node<BlockInfo> option, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text
            .Append("{")
            .AppendLine() |> ignore
        _indent.Increase()
    
    override _.LeaveBracedBlock(_: Node<BlockInfo> option, _: Guid, _: HalfOpenRange) : unit =
        _indent.Decrease()
        _acc_cool_text
            .Append("}")
            .AppendLine() |> ignore
    
    
    // Parenthesized expr
    override _.EnterParensExpr(_:Node<Expr>, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.Append("(") |> ignore
    
    override _.LeaveParensExpr(_:Node<Expr>, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.Append(")") |> ignore

    
    // Actuals
    override _.EnterActuals(_: Node<Expr>[]) : unit =
        _acc_cool_text.Append("(") |> ignore
    
    override _.LeaveActuals(_: Node<Expr>[]) : unit =
        _acc_cool_text.Append(")") |> ignore

    
    override _.EnterActual(_: Expr, index: int, _: Guid, _: HalfOpenRange) : unit =
        if index > 0
        then do
            _acc_cool_text.Append(", ") |> ignore

    
    // Id
    override _.VisitId(id: ID, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.Append(id.Value) |> ignore
    
    
    // Literals    
    override _.VisitInt(literal: INT, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.Append(literal.Value) |> ignore
    

    override _.VisitStr(literal: STRING, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.Append(literal.Value) |> ignore
    

    override _.VisitBool(literal: BOOL, _: Guid, _: HalfOpenRange) : unit =
        match literal with
        | BOOL.True -> _acc_cool_text.Append("true")
        | BOOL.False -> _acc_cool_text.Append("false")
        |> ignore
    
    override _.VisitThis(_: Guid, _: HalfOpenRange) : unit = _acc_cool_text.Append("this") |> ignore
    
    override _.VisitNull(_: Guid, _: HalfOpenRange) : unit = _acc_cool_text.Append("null") |> ignore
    
    override _.VisitUnit(_: Guid, _: HalfOpenRange) : unit = _acc_cool_text.Append("()") |> ignore

    
    // Terminals
    override _.VisitID(id: ID, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.Append(id.Value) |> ignore 
    
    
    override _.VisitTYPE_NAME(type_name: TYPE_NAME, _: Guid, _: HalfOpenRange) : unit =
        _acc_cool_text.Append(type_name.Value) |> ignore

    
    override _.VisitCOLON() : unit = _acc_cool_text.Append(": ") |> ignore

    override _.VisitEQUALS() : unit = _acc_cool_text.Append(" = ") |> ignore

    override _.VisitNATIVE() : unit = _acc_cool_text.Append("native") |> ignore

    override _.VisitARROW() : unit = _acc_cool_text.Append(" => ") |> ignore
        
    override _.VisitDOT() : unit = _acc_cool_text.Append(".") |> ignore

    override _.VisitNULL() : unit = _acc_cool_text.Append("null") |> ignore

    
    override _.ToString() = _acc_cool_text.ToString()

    
    static member Render(ast: Ast) =
        let renderer = CoolRenderer()
        AstWalker.Walk(ast, renderer)
        renderer.ToString()
