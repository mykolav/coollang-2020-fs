namespace LibCool.Frontend

open System
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
type AstListener() =
    // Ast
    abstract member EnterAst: ast:Ast -> unit
    default _.EnterAst(ast: Ast) : unit = ()
    abstract member LeaveAst: ast:Ast -> unit
    default _.LeaveAst(ast: Ast) : unit = ()
        
    // Program
    abstract member EnterProgram: program:Program * key:Guid * span:HalfOpenRange -> unit
    default this.EnterProgram(program:Program, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveProgram: program:Program * key:Guid * span:HalfOpenRange -> unit
    default this.LeaveProgram(program:Program, key:Guid, span:HalfOpenRange) : unit = ()
    
    // Classes
    abstract member EnterClass: klass:ClassDecl * key:Guid * span:HalfOpenRange -> unit
    default this.EnterClass(klass:ClassDecl, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveClass: klass:ClassDecl * key:Guid * span:HalfOpenRange -> unit
    default this.LeaveClass(klass:ClassDecl, key:Guid, span:HalfOpenRange) : unit = ()

    abstract member EnterVarFormals: var_formals:Node<VarFormal>[] -> unit
    default this.EnterVarFormals(var_formals:Node<VarFormal>[]) : unit = ()
    abstract member LeaveVarFormals: var_formals:Node<VarFormal>[] -> unit
    default this.LeaveVarFormals(var_formals:Node<VarFormal>[]) : unit = ()

    abstract member EnterVarFormal: var_formal:VarFormal * key:Guid * span:HalfOpenRange -> unit
    default this.EnterVarFormal(var_formal:VarFormal, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveVarFormal: var_formal:VarFormal * key:Guid * span:HalfOpenRange -> unit
    default this.LeaveVarFormal(var_formal:VarFormal, key:Guid, span:HalfOpenRange) : unit = ()
    
    abstract member EnterExtends: extends:Extends * key:Guid * span:HalfOpenRange -> unit
    default this.EnterExtends(extends:Extends, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveExtends: extends:Extends * key:Guid * span:HalfOpenRange -> unit
    default this.LeaveExtends(extends:Extends, key:Guid, span:HalfOpenRange) : unit = ()
    
    abstract member EnterFeatures: features:Node<Feature>[] -> unit
    default this.EnterFeatures(features: Node<Feature>[]) : unit = ()
    abstract member LeaveFeatures: features:Node<Feature>[] -> unit
    default this.LeaveFeatures(features: Node<Feature>[]) : unit = ()

    // Method
    abstract member EnterMethod: method_info:MethodInfo * key:Guid * span:HalfOpenRange -> unit
    default this.EnterMethod(method_info:MethodInfo, key:Guid, span:HalfOpenRange) = ()
    abstract member LeaveMethod: method_info:MethodInfo * key:Guid * span:HalfOpenRange -> unit
    default this.LeaveMethod(method_info:MethodInfo, key:Guid, span:HalfOpenRange) = ()
    
    abstract member EnterFormals: formals:Node<Formal>[] -> unit
    default this.EnterFormals(formals:Node<Formal>[]) : unit = ()
    abstract member LeaveFormals: formals:Node<Formal>[] -> unit
    default this.LeaveFormals(formals:Node<Formal>[]) : unit = ()
    
    abstract member EnterFormal: formal:Formal * key:Guid * span:HalfOpenRange -> unit
    default this.EnterFormal(formal:Formal, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveFormal: formal:Formal * key:Guid * span:HalfOpenRange -> unit
    default this.LeaveFormal(formal:Formal, key:Guid, span:HalfOpenRange) : unit = ()

    // Attribute
    abstract member EnterAttr: attr_info:AttrInfo * key:Guid * span:HalfOpenRange -> unit
    default this.EnterAttr(attr_info:AttrInfo, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveAttr: attr_info:AttrInfo * key:Guid * span:HalfOpenRange -> unit
    default this.LeaveAttr(attr_info:AttrInfo, key:Guid, span:HalfOpenRange) : unit = ()

    // Block
    abstract member EnterBlock: block_info:BlockInfo * key:Guid * span:HalfOpenRange -> unit
    default _.EnterBlock(block_info:BlockInfo, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveBlock: block_info:BlockInfo * key:Guid * span:HalfOpenRange -> unit
    default _.LeaveBlock(block_info:BlockInfo, key:Guid, span:HalfOpenRange) : unit = ()
    
    abstract member EnterVarDecl: var_decl_info:VarDeclInfo * key:Guid * span:HalfOpenRange -> unit
    default _.EnterVarDecl(var_decl_info:VarDeclInfo , key:Guid , span:HalfOpenRange) : unit = ()
    abstract member LeaveVarDecl: var_decl_info:VarDeclInfo * key:Guid * span:HalfOpenRange -> unit
    default _.LeaveVarDecl(var_decl_info:VarDeclInfo , key:Guid , span:HalfOpenRange) : unit = ()

    abstract member EnterStmtExpr: expr:Expr * key:Guid * span:HalfOpenRange -> unit
    default _.EnterStmtExpr(expr:Expr, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveStmtExpr: expr:Expr * key:Guid * span:HalfOpenRange -> unit
    default _.LeaveStmtExpr(expr:Expr, key:Guid, span:HalfOpenRange) : unit = ()

    // Expressions
    abstract member EnterExpr: expr:Expr * key:Guid * span:HalfOpenRange -> unit
    default _.EnterExpr(expr:Expr, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveExpr: expr:Expr * key:Guid * span:HalfOpenRange -> unit
    default _.LeaveExpr(expr:Expr, key:Guid, span:HalfOpenRange) : unit = ()

    // Assign
    abstract member EnterAssign: left:Node<ID> * right:Node<Expr> * key:Guid * span:HalfOpenRange -> unit
    default _.EnterAssign(left:Node<ID>, right:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveAssign: left:Node<ID> * right:Node<Expr> * key:Guid * span:HalfOpenRange -> unit
    default _.LeaveAssign(left:Node<ID>, right:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = ()
    
    // If
    abstract member EnterIf: condition:Node<Expr> * then_branch:Node<Expr> * else_branch:Node<Expr> -> unit
    default _.EnterIf(condition:Node<Expr>, then_branch:Node<Expr>, else_branch:Node<Expr>) : unit = ()
    abstract member LeaveIf: condition:Node<Expr> * then_branch:Node<Expr> * else_branch:Node<Expr> -> unit
    default _.LeaveIf(condition:Node<Expr>, then_branch:Node<Expr>, else_branch:Node<Expr>) : unit = ()
    
    abstract member EnterIfCond: condition:Node<Expr> -> unit
    default _.EnterIfCond(condition:Node<Expr>) : unit = ()
    abstract member LeaveIfCond: condition:Node<Expr> -> unit
    default _.LeaveIfCond(condition:Node<Expr>) : unit = ()

    abstract member EnterThenBranch: then_branch:Node<Expr> -> unit
    default _.EnterThenBranch(then_branch:Node<Expr>) : unit = ()
    abstract member LeaveThenBranch: then_branch:Node<Expr> -> unit
    default _.LeaveThenBranch(then_branch:Node<Expr>) : unit = ()

    abstract member EnterElseBranch: else_branch:Node<Expr> -> unit
    default _.EnterElseBranch(else_branch:Node<Expr>) : unit = ()
    abstract member LeaveElseBranch: else_branch:Node<Expr> -> unit
    default _.LeaveElseBranch(else_branch:Node<Expr>) : unit = ()
    
    // While
    abstract member EnterWhile: condition:Node<Expr> * body:Node<Expr> -> unit
    default _.EnterWhile(condition:Node<Expr>, body:Node<Expr>) : unit = ()
    abstract member LeaveWhile: condition:Node<Expr> * body:Node<Expr> -> unit
    default _.LeaveWhile(condition:Node<Expr>, body:Node<Expr>) : unit = ()
    
    abstract member EnterWhileCond: condition:Node<Expr> -> unit
    default _.EnterWhileCond(condition:Node<Expr>) : unit = ()
    abstract member LeaveWhileCond: condition:Node<Expr> -> unit
    default _.LeaveWhileCond(condition:Node<Expr>) : unit = ()

    abstract member EnterWhileBody: body:Node<Expr> -> unit
    default _.EnterWhileBody(body:Node<Expr>) : unit = ()
    abstract member LeaveWhileBody: body:Node<Expr> -> unit
    default _.LeaveWhileBody(body:Node<Expr>) : unit = ()
    
    // Match
    abstract member EnterMatch: expr:Node<Expr> * cases:Cases -> unit
    default _.EnterMatch(expr:Node<Expr>, cases:Cases) : unit = ()
    abstract member LeaveMatch: expr:Node<Expr> * cases:Cases -> unit
    default _.LeaveMatch(expr:Node<Expr>, cases:Cases) : unit = ()
    
    abstract member EnterMatchExpr: expr:Expr * key:Guid * span:HalfOpenRange -> unit
    default _.EnterMatchExpr(expr:Expr, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveMatchExpr: expr:Expr * key:Guid * span:HalfOpenRange -> unit
    default _.LeaveMatchExpr(expr:Expr, key:Guid, span:HalfOpenRange) : unit = ()

    abstract member EnterMatchCases: cases:Cases -> unit
    default _.EnterMatchCases(cases:Cases) : unit = ()
    abstract member LeaveMatchCases: cases:Cases -> unit
    default _.LeaveMatchCases(cases:Cases) : unit = ()

    // Match/case
    abstract member EnterMatchCase: case:Case * key:Guid * span:HalfOpenRange -> unit
    default _.EnterMatchCase(case:Case, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveMatchCase: case:Case * key:Guid * span:HalfOpenRange -> unit
    default _.LeaveMatchCase(case:Case, key:Guid, span:HalfOpenRange) : unit = ()
    
    abstract member EnterMatchCasePattern: id:Node<ID> * type_name:Node<TYPE_NAME> * key:Guid * span:HalfOpenRange -> unit
    default _.EnterMatchCasePattern(id:Node<ID>, type_name:Node<TYPE_NAME>, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveMatchCasePattern: id:Node<ID> * type_name:Node<TYPE_NAME> * key:Guid * span:HalfOpenRange -> unit
    default _.LeaveMatchCasePattern(id:Node<ID>, type_name:Node<TYPE_NAME>, key:Guid, span:HalfOpenRange) : unit = ()

    abstract member EnterMatchCaseBlock: block:Block * key:Guid * span:HalfOpenRange -> unit
    default _.EnterMatchCaseBlock(block:Block, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveMatchCaseBlock: block:Block * key:Guid * span:HalfOpenRange -> unit
    default _.LeaveMatchCaseBlock(block:Block, key:Guid, span:HalfOpenRange) : unit = ()
    
    // Dispatch
    abstract member EnterDispatch: obj_expr:Node<Expr> * method_id:Node<ID> * actuals:Node<Expr>[] -> unit
    default _.EnterDispatch(obj_expr:Node<Expr>, method_id:Node<ID>, actuals:Node<Expr>[]) : unit = ()
    abstract member LeaveDispatch: obj_expr:Node<Expr> * method_id:Node<ID> * actuals:Node<Expr>[] -> unit
    default _.LeaveDispatch(obj_expr:Node<Expr>, method_id:Node<ID>, actuals:Node<Expr>[]) : unit = ()
    
    // Implicit `this` dispatch
    abstract member EnterImplicitThisDispatch: method_id:Node<ID> * actuals:Node<Expr>[] -> unit
    default _.EnterImplicitThisDispatch(method_id:Node<ID>, actuals:Node<Expr>[]) : unit = ()
    abstract member LeaveImplicitThisDispatch: method_id:Node<ID> * actuals:Node<Expr>[] -> unit
    default _.LeaveImplicitThisDispatch(method_id:Node<ID>, actuals:Node<Expr>[]) : unit = ()
    
    // Super dispatch
    abstract member EnterSuperDispatch: method_id:Node<ID> * actuals:Node<Expr>[] -> unit
    default _.EnterSuperDispatch(method_id:Node<ID>, actuals:Node<Expr>[]) : unit = ()
    abstract member LeaveSuperDispatch: method_id:Node<ID> * actuals:Node<Expr>[] -> unit
    default _.LeaveSuperDispatch(method_id:Node<ID>, actuals:Node<Expr>[]) : unit = ()
    
    // Object creation
    abstract member EnterObjectCreation: Node<TYPE_NAME> * actuals:Node<Expr>[] -> unit
    default _.EnterObjectCreation(type_name: Node<TYPE_NAME>, actuals:Node<Expr>[]) : unit = ()
    abstract member LeaveObjectCreation: Node<TYPE_NAME> * actuals:Node<Expr>[] -> unit
    default _.LeaveObjectCreation(type_name: Node<TYPE_NAME>, actuals:Node<Expr>[]) : unit = ()

    // Bool negation
    abstract member EnterBoolNegation: expr:Node<Expr> * key:Guid * span:HalfOpenRange -> unit
    default _.EnterBoolNegation(expr:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveBoolNegation: expr:Node<Expr> * key:Guid * span:HalfOpenRange -> unit
    default _.LeaveBoolNegation(expr:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = ()
    
    // Compare
    abstract member EnterComparison: left:Node<Expr> * op:CompareOp * right:Node<Expr> -> unit
    default _.EnterComparison(left:Node<Expr>, op:CompareOp, right:Node<Expr>) : unit = ()
    abstract member LeaveComparison: left:Node<Expr> * op:CompareOp * right:Node<Expr> -> unit
    default _.LeaveComparison(left:Node<Expr>, op:CompareOp, right:Node<Expr>) : unit = ()

    abstract member EnterComparisonOp: op:CompareOp * span:HalfOpenRange -> unit
    default _.EnterComparisonOp(op:CompareOp, span:HalfOpenRange) : unit = ()
    abstract member LeaveComparisonOp: op:CompareOp * span:HalfOpenRange -> unit
    default _.LeaveComparisonOp(op:CompareOp, span:HalfOpenRange) : unit = ()

    // Unary minus
    abstract member EnterUnaryMinus: expr:Node<Expr> * key:Guid * span:HalfOpenRange -> unit
    default _.EnterUnaryMinus(expr:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveUnaryMinus: expr:Node<Expr> * key:Guid * span:HalfOpenRange -> unit
    default _.LeaveUnaryMinus(expr:Node<Expr>, key:Guid, span:HalfOpenRange) : unit = ()

    // Arith
    abstract member EnterArith: left:Node<Expr> * op:ArithOp * right:Node<Expr> -> unit
    default _.EnterArith(left:Node<Expr>, op:ArithOp, right:Node<Expr>) : unit = ()
    abstract member LeaveArith: left:Node<Expr> * op:ArithOp * right:Node<Expr> -> unit
    default _.LeaveArith(left:Node<Expr>, op:ArithOp, right:Node<Expr>) : unit = ()

    abstract member VisitArithOp: op:ArithOp * span:HalfOpenRange -> unit
    default _.VisitArithOp(op:ArithOp, span:HalfOpenRange) : unit = ()
    
    // Braced block    
    abstract member EnterBracedBlock: BlockInfo option * key:Guid * span:HalfOpenRange -> unit
    default _.EnterBracedBlock(block_info_opt: BlockInfo option, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveBracedBlock: BlockInfo option * key:Guid * span:HalfOpenRange -> unit
    default _.LeaveBracedBlock(block_info_opt: BlockInfo option, key:Guid, span:HalfOpenRange) : unit = ()
    
    // Parenthesized expr
    abstract member EnterParensExpr: Node<Expr> -> unit
    default _.EnterParensExpr(expr:Node<Expr>) : unit = ()
    abstract member LeaveParensExpr: Node<Expr> -> unit
    default _.LeaveParensExpr(expr:Node<Expr>) : unit = ()

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
    
    abstract member VisitThis: unit -> unit
    default _.VisitThis() : unit = ()
    
    abstract member VisitNull: unit -> unit
    default _.VisitNull() : unit = ()
    
    abstract member VisitUnit: unit -> unit
    default _.VisitUnit() : unit = ()

    // Actuals
    abstract member EnterActuals: actuals:Node<Expr>[] -> unit
    default _.EnterActuals(actuals:Node<Expr>[]) : unit = ()
    abstract member LeaveActuals: actuals:Node<Expr>[] -> unit
    default _.LeaveActuals(actuals:Node<Expr>[]) : unit = ()
    
    abstract member EnterActual: actual:Expr * key:Guid * span:HalfOpenRange -> unit
    default _.EnterActual(actual:Expr, key:Guid, span:HalfOpenRange) : unit = ()
    abstract member LeaveActual: actual:Expr * key:Guid * span:HalfOpenRange -> unit
    default _.LeaveActual(actual:Expr, key:Guid, span:HalfOpenRange) : unit = ()

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
