namespace LibCool.Frontend

open LibCool.SourceParts
open System

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
type Node<'TValue>(span: HalfOpenRange, value: 'TValue) =
    let key = Guid.NewGuid()
    member _.Key = key
    member _.Span = span
    member _.Value = value
    
    new(first: uint32, last: uint32, value: 'TValue) =
       Node<'TValue>(span = { First = first; Last = last }, value = value)

module rec AST =
    type ID = ID of value: string
    type TYPE_NAME = TYPE_NAME of value: string
    type INTEGER = INTEGER of value: int
    type STRING = STRING of value: string
    type BOOL = True | False

    type Program = { ClassDecls: Node<ClassDecl> list }

    type ClassDecl = { TYPE_NAME: Node<TYPE_NAME>
                       VarFormals: Node<VarFormal> list
                       Extends: Node<Extends> option
                       ClassBody: Node<Feature> list }

    type Extends = Extends of {| TYPE_NAME: Node<TYPE_NAME>
                                 Actuals: Node<Expr> list |}
                 | ExtendsNative

    type VarFormal = { ID: Node<ID>; TYPE_NAME: Node<TYPE_NAME> }

    type Feature = Method of {| Override: bool
                                ID: Node<ID>
                                Formals: Node<Formal> list
                                TYPE_NAME: Node<TYPE_NAME>
                                MethodBody: Node<MethodBody> |}
                 | Attr of Attr
                 | Block of Block
    type MethodBody = ExprMethodBody of Expr
                    | NativeMethodBody

    type Formal = { ID: Node<ID>; TYPE_NAME: Node<TYPE_NAME> }

    type Attr = Attr of {| ID: Node<ID>; TYPE_NAME: Node<TYPE_NAME>; Expr: Node<Expr> |}
              | Native of Node<ID>

    type Block = JustBlock of BlockInfo
               | BracedBlock of BlockInfo option
    type BlockInfo = { Stmts: Node<Stmt> list; Expr: Node<Expr> }
    
    type Stmt = VarDecl of {| ID: Node<ID>; TYPE_NAME: Node<TYPE_NAME>; Expr: Node<Expr> |}
              | ExprStmt of Expr

    [<RequireQualifiedAccess>]
    type Expr = Assign of Node<ID> * Node<Expr>
              | BoolNegation of Node<Expr>
              | UnaryMinus of Node<Expr>
              | If of Node<Cond> * Node<Then> * Node<Else>
              | While of Node<Cond> * Node<WhileBody>
              | LtEq of Node<Expr> * Node<Expr>
              | GtEq of Node<Expr> * Node<Expr>
              | Lt of Node<Expr> * Node<Expr>
              | Gt of Node<Expr> * Node<Expr>
              | EqEq of Node<Expr> * Node<Expr>
              | NotEq of Node<Expr> * Node<Expr>
              | Mul of Node<Expr> * Node<Expr>
              | Div of Node<Expr> * Node<Expr>
              | Sum of Node<Expr> * Node<Expr>
              | Sub of Node<Expr> * Node<Expr>
              | Match of Node<Expr> * Node<Cases>
              | Dispatch of Node<Expr> * Node<ID> * actuals: Node<Expr> list
              // Primary
              | ImplicitThisDispatch of Node<ID> * actuals: Node<Expr> list
              | SuperDispatch of Node<ID> * actuals: Node<Expr> list
              | ObjectCreation of Node<TYPE_NAME> * actuals: Node<Expr> list
              | BracedBlock of BlockInfo option
              | ParensExpr of Node<Expr>
              | Null
              | Unit
              | Id of Node<ID>
              | Int of INTEGER
              | Str of STRING
              | Bool of BOOL
              | This

    type Cond = Expr
    type Then = Expr
    type Else = Expr

    type WhileBody = Expr

    type Case = Case of Node<Pattern> * Node<Block>
    type Pattern = IdType of Node<ID> * Node<TYPE_NAME>
                 | Null
    type Cases = Node<Case> * Node<Case> list
