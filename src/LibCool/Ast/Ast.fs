namespace rec LibCool.Ast


open System
open System.Runtime.CompilerServices
open LibCool.SourceParts


[<IsReadOnly; Struct>]
type Node<'TValue> =
    { Key: Guid
      Span: Span
      Value: 'TValue }


[<AbstractClass; Sealed; RequireQualifiedAccess>]
type Node private () =


    static member Of<'TValue>(value: 'TValue, span: Span) =
        { Key = Guid.NewGuid()
          Span = span
          Value = value }


    static member Of<'TValue>(value: 'TValue, first: uint32, last: uint32) =
        Node.Of
            (span =
                { First = first
                  Last = last }, value = value)


type Ast =
    { Program: Node<Program> }


type ID =
    | ID of value: string
    member this.Value =
        let (ID value) = this in value


type TYPE_NAME =
    | TYPE_NAME of value: string
    member this.Value =
        let (TYPE_NAME value) = this in value


type INT =
    | INT of value: int
    member this.Value =
        let (INT value) = this in value


type STRING =
    | STRING of value: string
    member this.Value =
        let (STRING value) = this in value


type BOOL =
    | True
    | False


type Program =
    { ClassDecls: Node<ClassDecl>[] }


type ClassDecl =
    { NAME: Node<TYPE_NAME>
      VarFormals: Node<VarFormal>[]
      Extends: Node<Extends> voption
      ClassBody: Node<Feature>[] }


[<RequireQualifiedAccess>]
type Extends =
    | Info of ExtendsInfo
    | Native


type ExtendsInfo =
    { PARENT_NAME: Node<TYPE_NAME>
      Actuals: Node<Expr> [] }


type VarFormal =
    { ID: Node<ID>
      TYPE_NAME: Node<TYPE_NAME> }


[<RequireQualifiedAccess>]
type Feature =
    | Method of MethodInfo
    | Attr of AttrInfo
    | BracedBlock of Node<BlockInfo voption>


type MethodInfo =
    { Override: bool
      ID: Node<ID>
      Formals: Node<Formal> []
      TYPE_NAME: Node<TYPE_NAME>
      MethodBody: Node<MethodBody> }


[<RequireQualifiedAccess>]
type MethodBody =
    | Expr of Expr
    | Native


type Formal =
    { ID: Node<ID>
      TYPE_NAME: Node<TYPE_NAME> }


[<RequireQualifiedAccess>]
type AttrInfo =
    { ID: Node<ID>
      TYPE_NAME: Node<TYPE_NAME>
      AttrBody: Node<AttrBody> }


[<RequireQualifiedAccess>]
type AttrBody =
    | Expr of Expr
    | Native


type BlockInfo =
    { Stmts: Node<Stmt> []
      Expr: Node<Expr> }


[<RequireQualifiedAccess>]
type Stmt =
    | VarDecl of VarDeclInfo
    | Expr of Expr


type VarDeclInfo =
    { ID: Node<ID>
      TYPE_NAME: Node<TYPE_NAME>
      Expr: Node<Expr> }


[<RequireQualifiedAccess>]
type Expr =
    | Assign of left: Node<ID> * right: Node<Expr>
    | BoolNegation of Node<Expr>
    | UnaryMinus of Node<Expr>
    | If of condition: Node<Expr> * then_branch: Node<Expr> * else_branch: Node<Expr>
    | While of condition: Node<Expr> * body: Node<Expr>
    | LtEq of left: Node<Expr> * right: Node<Expr>
    | GtEq of left: Node<Expr> * right: Node<Expr>
    | Lt of left: Node<Expr> * right: Node<Expr>
    | Gt of left: Node<Expr> * right: Node<Expr>
    | EqEq of left: Node<Expr> * right: Node<Expr>
    | NotEq of left: Node<Expr> * right: Node<Expr>
    | Mul of left: Node<Expr> * right: Node<Expr>
    | Div of left: Node<Expr> * right: Node<Expr>
    | Sum of left: Node<Expr> * right: Node<Expr>
    | Sub of left: Node<Expr> * right: Node<Expr>
    | Match of expr: Node<Expr> * cases_hd: Node<Case> * cases_tl: Node<Case> []
    | Dispatch of receiver: Node<Expr> * method_id: Node<ID> * actuals: Node<Expr> []
    // Primary
    | ImplicitThisDispatch of method_id: Node<ID> * actuals: Node<Expr> []
    | SuperDispatch of method_id: Node<ID> * actuals: Node<Expr> []
    | ObjectCreation of class_name: Node<TYPE_NAME> * actuals: Node<Expr> []
    | BracedBlock of Node<BlockInfo voption>
    | ParensExpr of Node<Expr>
    | Id of ID
    | Int of INT
    | Str of STRING
    | Bool of BOOL
    | This
    | Null
    | Unit


type Case =
    { Pattern: Node<Pattern>
      Block: Node<CaseBlock> }


[<RequireQualifiedAccess>]
type CaseBlock =
    | Implicit of BlockInfo
    | Braced of Node<BlockInfo voption>


[<RequireQualifiedAccess>]
type Pattern =
    | IdType of Node<ID> * Node<TYPE_NAME>
    | Null
