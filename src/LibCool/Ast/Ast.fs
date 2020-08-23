namespace rec LibCool.Ast


open System
open System.Runtime.CompilerServices
open LibCool.SourceParts


// Generating keys based on the hierarchy of Nodes is deterministic and
// seems to be a better option overall,
// but it turns out implementing it is somewhat complicated.
// At the moment I cannot come up with a nice solution for cases where:
//     A parsed expression can be an immediate child of the Node<parent>
//     or a grandchild of the same Node<parent>.
// Happens when parsing an expression by climbing precedence.
// E. g.:
//     a + b => in this case the `sum`Node is the parent and `b` is its immediate child.
//     a + b * c => in this case the `sum` is the parent of the `multiply`Node
//                  which in turn is the parent of `b`.
// Hence, to generate a hierarchical key for `b` we need to somehow look forward
// and figure out which of the cases we're dealing with.
//
// Trying to do this I end up with a load of messy code.
// So, let's give up on it for now and resort to using good old GUIDs
// and keep making progress on the compiler...
[<IsReadOnly; Struct>]
type Node<'TValue> =
    { Key: Guid
      Span: HalfOpenRange
      Value: 'TValue }


[<AbstractClass; Sealed; RequireQualifiedAccess>]
type Node private () =


    static member Of<'TValue>(span: HalfOpenRange, value: 'TValue) =
        { Key = Guid.NewGuid()
          Span = span
          Value = value }


    static member Of<'TValue>(first: uint32, last: uint32, value: 'TValue) =
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
    { ClassDecls: Node<ClassDecl> [] }


type ClassDecl =
    { TYPE_NAME: Node<TYPE_NAME>
      VarFormals: Node<VarFormal> []
      Extends: Node<Extends> option
      ClassBody: Node<Feature> [] }


[<RequireQualifiedAccess>]
type Extends =
    | Info of ExtendsInfo
    | Native


type ExtendsInfo =
    { TYPE_NAME: Node<TYPE_NAME>
      Actuals: Node<Expr> [] }


type VarFormal =
    { ID: Node<ID>
      TYPE_NAME: Node<TYPE_NAME> }


[<RequireQualifiedAccess>]
type Feature =
    | Method of MethodInfo
    | Attr of AttrInfo
    | Block of Block


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


[<RequireQualifiedAccess>]
type Block =
    | Implicit of BlockInfo
    | Braced of Node<BlockInfo> option


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
    | BracedBlock of Node<BlockInfo> option
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
      Block: Node<Block> }


[<RequireQualifiedAccess>]
type Pattern =
    | IdType of Node<ID> * Node<TYPE_NAME>
    | Null
