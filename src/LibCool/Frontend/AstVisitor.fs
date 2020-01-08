namespace LibCool.Frontend

open System
open LibCool.Frontend
open LibCool.SourceParts

[<RequireQualifiedAccess>]
type CompareOp = LtEq
               | GtEq
               | Lt
               | Gt
               | EqEq
               | NotEq

[<RequireQualifiedAccess>]
type ArithOp = Mul
             | Div
             | Sum
             | Sub

[<AbstractClass>]
type AstVisitor() =
    // Ast
    abstract member VisitAst: ast:Ast -> unit
    default this.VisitAst(ast: Ast) : unit = this.WalkAst(ast)
        
    member this.WalkAst(ast: Ast) : unit =
        this.VisitProgram(ast.Program.Value, ast.Program.Key, ast.Program.Span)

    // Program
    abstract member VisitProgram: program:Program * key:Guid * span:HalfOpenRange -> unit
    default this.VisitProgram(program:Program, key:Guid, span:HalfOpenRange) : unit =
        this.WalkProgram(program, key, span)
    
    member this.WalkProgram(program:Program, _:Guid, _:HalfOpenRange) : unit =
        program.ClassDecls |> Array.iter (fun it -> this.VisitClass(it.Value, it.Key, it.Span))

    // Classes
    abstract member VisitClass: klass:ClassDecl * key:Guid * span:HalfOpenRange -> unit
    default this.VisitClass(klass:ClassDecl, key:Guid, span:HalfOpenRange) : unit =
        this.WalkClass(klass, key, span)
    
    member this.WalkClass(klass:ClassDecl, _:Guid, _:HalfOpenRange) : unit =
        this.VisitTYPE_NAME(klass.TYPE_NAME.Value, klass.TYPE_NAME.Key, klass.TYPE_NAME.Span)
        this.VisitVarFormals(klass.VarFormals)

        match klass.Extends with
        | Some extends_node ->
            this.VisitExtends(extends_node.Value, extends_node.Key, extends_node.Span)
        | None ->
            ()
            
        this.VisitFeatures(klass.ClassBody)
        

    abstract member VisitVarFormals: var_formals:Node<VarFormal>[] -> unit
    default this.VisitVarFormals(var_formals:Node<VarFormal>[]) : unit = this.WalkVarFormals(var_formals)

    member this.WalkVarFormals(var_formals:Node<VarFormal>[]) : unit =
        // TODO: Implement...
        ()
    
    abstract member VisitVarFormal: var_formal:VarFormal * key:Guid * span:HalfOpenRange -> unit
    default this.VisitVarFormal(var_formal:VarFormal, key:Guid, span:HalfOpenRange) : unit =
        this.WalkVarFormal(var_formal, key, span)
    
    member this.WalkVarFormal(var_formal:VarFormal, key:Guid, span:HalfOpenRange) : unit =
        // TODO: Implement...
        ()
    
    abstract member VisitExtends: extends:Extends * key:Guid * span:HalfOpenRange -> unit
    default this.VisitExtends(extends:Extends, key:Guid, span:HalfOpenRange) : unit =
        this.WalkExtends(extends, key, span)
        
    member this.WalkExtends(extends:Extends, key:Guid, span:HalfOpenRange) : unit =
        match extends with
        | Extends.Info extends_info ->
            this.VisitTYPE_NAME(extends_info.TYPE_NAME.Value,
                                extends_info.TYPE_NAME.Key,
                                extends_info.TYPE_NAME.Span)
            this.VisitActuals(extends_info.Actuals)
        | Extends.Native ->
            this.VisitNATIVE()
    
    abstract member VisitFeatures: features:Node<Feature>[] -> unit
    default this.VisitFeatures(features: Node<Feature>[]) : unit =
        this.WalkFeatures(features)

    member this.WalkFeatures(features: Node<Feature>[]) : unit =
        let visit_feature (feature_node: Node<Feature>) : unit =
            match feature_node.Value with
            | Feature.Method method_info ->
                this.VisitMethod(method_info, feature_node.Key, feature_node.Span)
            | Feature.Attr attr_info ->
                this.VisitAttr(attr_info, feature_node.Key, feature_node.Span)
            | Feature.Block (Block.Info block_info) ->
                this.VisitBlock(block_info, feature_node.Key, feature_node.Span)
            | Feature.Block (Block.Braced block_info_opt) ->
                this.VisitBracedBlock(block_info_opt, feature_node.Key, feature_node.Span)
            
        features |> Array.iter visit_feature

    // Method
    abstract member VisitMethod: method_info:MethodInfo * key:Guid * span:HalfOpenRange -> unit
    default this.VisitMethod(method_info:MethodInfo, key:Guid, span:HalfOpenRange) =
        this.WalkMethod(method_info, key, span)
        
    member this.WalkMethod(method_info:MethodInfo, _:Guid, _:HalfOpenRange) : unit =
        this.VisitID(method_info.ID.Value, method_info.ID.Key, method_info.ID.Span)
        this.VisitFormals(method_info.Formals)
        this.VisitCOLON()
        this.VisitTYPE_NAME(method_info.TYPE_NAME.Value, method_info.TYPE_NAME.Key, method_info.TYPE_NAME.Span)
        this.VisitEQUALS()
        match method_info.MethodBody.Value with
        | MethodBody.Expr expr ->
            this.VisitExpr(expr, method_info.MethodBody.Key, method_info.MethodBody.Span)
        | MethodBody.Native ->
            this.VisitNATIVE()
    
    abstract member VisitFormals: formals:Node<Formal>[] -> unit
    default this.VisitFormals(formals:Node<Formal>[]) : unit =
        this.WalkFormals(formals)
        
    member this.WalkFormals(formals:Node<Formal>[]) : unit =
        formals |> Array.iter (fun it -> this.VisitFormal(it.Value, it.Key, it.Span))
        
    abstract member VisitFormal: formal:Formal * key:Guid * span:HalfOpenRange -> unit
    default this.VisitFormal(formal:Formal, key:Guid, span:HalfOpenRange) : unit =
        this.WalkFormal(formal, key, span)

    member this.WalkFormal(formal:Formal, _:Guid, _:HalfOpenRange) : unit =
        this.VisitID(formal.ID.Value, formal.ID.Key, formal.ID.Span)
        this.VisitCOLON()
        this.VisitTYPE_NAME(formal.TYPE_NAME.Value, formal.TYPE_NAME.Key, formal.TYPE_NAME.Span)
    
    // Attribute
    abstract member VisitAttr: attr_info:AttrInfo * key:Guid * span:HalfOpenRange -> unit
    default this.VisitAttr(attr_info:AttrInfo, key:Guid, span:HalfOpenRange) : unit =
        this.WalkAttr(attr_info, key, span)
        
    member this.WalkAttr(attr_info:AttrInfo, _:Guid, _:HalfOpenRange) : unit =
        this.VisitID(attr_info.ID.Value, attr_info.ID.Key, attr_info.ID.Span)
        this.VisitCOLON()
        this.VisitTYPE_NAME(attr_info.TYPE_NAME.Value, attr_info.TYPE_NAME.Key, attr_info.TYPE_NAME.Span)
        this.VisitEQUALS()
        match attr_info.AttrBody.Value with
        | AttrBody.Expr expr ->
            this.VisitExpr(expr, attr_info.AttrBody.Key, attr_info.AttrBody.Span)
        | AttrBody.Native ->
            this.VisitNATIVE()

    // Block
    abstract member VisitBlock: block_info:BlockInfo * key:Guid * span:HalfOpenRange -> unit
    abstract member WalkBlock: block_info:BlockInfo * key:Guid * span:HalfOpenRange -> unit

    abstract member VisitVarDecl: var_decl_info:VarDeclInfo * key:Guid * span:HalfOpenRange -> unit
    abstract member WalkVarDecl: var_decl_info:VarDeclInfo * key:Guid * span:HalfOpenRange -> unit

    abstract member VisitStmtExpr: expr:Expr * key:Guid * span:HalfOpenRange -> unit
    abstract member WalkStmtExpr: expr:Expr * key:Guid * span:HalfOpenRange -> unit

    // Expressions
    abstract member VisitExpr: expr:Expr * key:Guid * span:HalfOpenRange -> unit
    abstract member WalkExpr: expr:Expr * key:Guid * span:HalfOpenRange -> unit

    // Assign
    abstract member VisitAssign: left:Node<ID> * right:Node<Expr> * key:Guid * span:HalfOpenRange -> unit
    abstract member WalkAssign: lvalue:Node<ID> * rvalue:Node<Expr> * key:Guid * span:HalfOpenRange -> unit

    abstract member VisitAssignLeft: left:ID * key:Guid * span:HalfOpenRange -> unit
    abstract member VisitAssignOp: span:HalfOpenRange -> unit
    abstract member VisitAssignRight: right:Expr * key:Guid * span:HalfOpenRange -> unit
    
    // If
    abstract member VisitIf: condition:Node<Expr> * then_branch:Node<Expr> * else_branch:Node<Expr> -> unit
    abstract member WalkIf: condition:Node<Expr> * then_branch:Node<Expr> * else_branch:Node<Expr> -> unit
    
    abstract member VisitIfCond: condition:Node<Expr> -> unit
    abstract member VisitThenCond: then_branch:Node<Expr> -> unit
    abstract member VisitElseCond: else_branch:Node<Expr> -> unit
    
    // While
    abstract member VisitWhile: condition:Node<Expr> * body:Node<Expr> -> unit
    abstract member WalkWhile: condition:Node<Expr> * body:Node<Expr> -> unit
    
    abstract member VisitWhileCond: condition:Node<Expr> -> unit
    abstract member VisitWhileBody: body:Node<Expr> -> unit
    
    // Match
    abstract member VisitMatch: expr:Node<Expr> * cases:Cases -> unit
    abstract member WalkMatch: expr:Node<Expr> * cases:Cases -> unit
    
    abstract member VisitMatchExpr: expr:Expr * key:Guid * span:HalfOpenRange -> unit
    abstract member VisitMatchCases: cases:Cases -> unit

    // Match/case
    abstract member VisitMatchCase: case:Case * key:Guid * span:HalfOpenRange -> unit
    abstract member WalkMatchCase: case:Case * key:Guid * span:HalfOpenRange -> unit
    
    abstract member VisitMatchCasePattern: id:Node<ID> * type_name:Node<TYPE_NAME> * key:Guid * span:HalfOpenRange -> unit
    abstract member WalkMatchCasePattern: id:Node<ID> * type_name:Node<TYPE_NAME> * key:Guid * span:HalfOpenRange -> unit

    abstract member VisitMatchCaseBlock: block:Block * key:Guid * span:HalfOpenRange -> unit
    
    // Dispatch
    abstract member VisitDispatch: obj_expr:Node<Expr> * method_id:Node<ID> * actuals:Node<Expr>[] -> unit
    abstract member WalkDispatch: obj_expr:Node<Expr> * method_id:Node<ID> * actuals:Node<Expr>[] -> unit
    
    // Implicit `this` dispatch
    abstract member VisitImplicitThisDispatch: method_id:Node<ID> * actuals:Node<Expr>[] -> unit
    abstract member WalkImplicitThisDispatch: method_id:Node<ID> * actuals:Node<Expr>[] -> unit
    
    // Super dispatch
    abstract member VisitSuperDispatch: method_id:Node<ID> * actuals:Node<Expr>[] -> unit
    abstract member WalkSuperDispatch: method_id:Node<ID> * actuals:Node<Expr>[] -> unit
    
    // Object creation
    abstract member VisitObjectCreation: Node<TYPE_NAME> * actuals:Node<Expr>[] -> unit
    abstract member WalkObjectCreation: Node<TYPE_NAME> * actuals:Node<Expr>[] -> unit

    // Bool negation
    abstract member VisitBoolNegation: expr:Node<Expr> * key:Guid * span:HalfOpenRange -> unit
    abstract member WalkBoolNegation: expr:Node<Expr> * key:Guid * span:HalfOpenRange -> unit
    
    // Compare
    abstract member VisitComparison: left:Node<Expr> * op:CompareOp * right:Node<Expr> -> unit
    abstract member WalkComparison: left:Node<Expr> * op:CompareOp * right:Node<Expr> -> unit
    
    abstract member VisitComparisonLeft: left:Expr * key:Guid * span:HalfOpenRange -> unit
    abstract member VisitComparisonOp: op:CompareOp * span:HalfOpenRange -> unit
    abstract member VisitComparisonRight: right:Expr * key:Guid * span:HalfOpenRange -> unit

    // Unary minus
    abstract member VisitUnaryMinus: expr:Node<Expr> * key:Guid * span:HalfOpenRange -> unit
    abstract member WalkUnaryMinus: expr:Node<Expr> * key:Guid * span:HalfOpenRange -> unit
    
    abstract member VisitUnaryMinusOp: span:HalfOpenRange -> unit
    abstract member VisitUnaryMinusExpr: expr:Expr * key:Guid * span:HalfOpenRange -> unit

    // Arith
    abstract member VisitArith: left:Node<Expr> * op:ArithOp * right:Node<Expr> -> unit
    abstract member WalkArith: left:Node<Expr> * op:ArithOp * right:Node<Expr> -> unit

    abstract member VisitArithLeft: left:Expr * key:Guid * span:HalfOpenRange -> unit
    abstract member VisitArithOp: op:ArithOp * span:HalfOpenRange -> unit
    abstract member VisitArithRight: right:Expr * key:Guid * span:HalfOpenRange -> unit
    
    // Braced block    
    abstract member VisitBracedBlock: BlockInfo option * key:Guid * span:HalfOpenRange -> unit
    abstract member WalkBracedBlock: BlockInfo option * key:Guid * span:HalfOpenRange -> unit
    
    // Parenthesized expr
    abstract member VisitParensExpr: Node<Expr> -> unit
    default this.VisitParensExpr(expr:Node<Expr>) : unit =
        this.WalkExpr(expr.Value, expr.Key, expr.Span)

    // Id
    abstract member VisitId: id:ID * key:Guid * span:HalfOpenRange -> unit
    default _.VisitId(_:ID, _:Guid, _:HalfOpenRange) : unit = ()
    
    // Literals    
    abstract member VisitInt: literal:INT * span:HalfOpenRange -> unit
    default _.VisitInt(_:INT, _:HalfOpenRange) : unit = ()
    
    abstract member VisitStr: literal:STRING * span:HalfOpenRange -> unit
    default _.VisitStr(_:STRING, _:HalfOpenRange) : unit = ()
    
    abstract member VisitBool: literal:BOOL * span:HalfOpenRange -> unit
    default _.VisitBool(_:BOOL, _:HalfOpenRange) : unit = ()
    
    abstract member VisitThis: span:HalfOpenRange -> unit
    default _.VisitThis(_:HalfOpenRange) : unit = ()
    
    abstract member VisitNull: span:HalfOpenRange -> unit
    default _.VisitNull(_:HalfOpenRange) : unit = ()
    
    abstract member VisitUnit: span:HalfOpenRange -> unit
    default _.VisitUnit(_:HalfOpenRange) : unit = ()
    

    // Actuals
    abstract member VisitActuals: actuals:Node<Expr>[] -> unit
    abstract member WalkActuals: actuals:Node<Expr>[] -> unit
    abstract member VisitActual: actual:Expr * key:Guid * span:HalfOpenRange -> unit
    abstract member WalkActual: actual:Expr * key:Guid * span:HalfOpenRange -> unit

    // Terminals
    abstract member VisitID: id:ID * key:Guid * span:HalfOpenRange -> unit
    default _.VisitID(_:ID, _:Guid, _:HalfOpenRange) : unit = ()
    
    abstract member VisitTYPE_NAME: type_name:TYPE_NAME * key:Guid * span:HalfOpenRange -> unit
    default _.VisitTYPE_NAME(_:TYPE_NAME, _:Guid, _:HalfOpenRange) : unit = ()

    abstract member VisitCOLON: unit -> unit
    default _.VisitCOLON() : unit = ()

    abstract member VisitEQUALS: unit -> unit
    default _.VisitEQUALS() : unit = ()

    abstract member VisitNATIVE: unit -> unit
    default _.VisitNATIVE() : unit = ()

    abstract member VisitARROW: unit -> unit
    default _.VisitARROW() : unit = ()
