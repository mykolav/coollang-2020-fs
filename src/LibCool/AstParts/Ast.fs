namespace rec LibCool.AstParts


open System.Runtime.CompilerServices
open LibCool.SourceParts


[<IsReadOnly; Struct>]
type AstNode<'TSyntax> =
    { Span: Span
      Syntax: 'TSyntax }
    with
    member this.Map<'TMapped>(mapping: 'TSyntax -> 'TMapped): AstNode<'TMapped> =
        AstNode.Of(mapping this.Syntax, this.Span)
        


[<AbstractClass; Sealed; RequireQualifiedAccess>]
type AstNode private () =


    static member Of<'TSyntax>(syntax: 'TSyntax, span: Span): AstNode<'TSyntax> =
        { Span=span
          Syntax=syntax }


    static member Of<'TSyntax>(value: 'TSyntax, first: uint32, last: uint32): AstNode<'TSyntax> =
        AstNode.Of(span=Span.Of(first, last),
                   syntax=value)


type ProgramSyntax =
    { Classes: AstNode<ClassSyntax>[] }


type ClassSyntax =
    { NAME: AstNode<TYPENAME>
      VarFormals: AstNode<VarFormalSyntax>[]
      Extends: AstNode<InheritanceSyntax> voption
      Features: AstNode<FeatureSyntax>[] }


[<RequireQualifiedAccess>]
type InheritanceSyntax =
    | Info of ExtendsSyntax
    | Native


type ExtendsSyntax =
    { SUPER: AstNode<TYPENAME>
      Actuals: AstNode<ExprSyntax> [] }


type VarFormalSyntax =
    { ID: AstNode<ID>
      TYPE: AstNode<TYPENAME> }


[<RequireQualifiedAccess; DefaultAugmentation(false)>]
type FeatureSyntax =
    | Method of MethodSyntax
    | Attr of AttrSyntax
    | BracedBlock of BlockSyntax voption


type MethodSyntax =
    { Override: bool
      ID: AstNode<ID>
      Formals: AstNode<FormalSyntax> []
      RETURN: AstNode<TYPENAME>
      Body: AstNode<MethodBodySyntax> }


[<RequireQualifiedAccess>]
type MethodBodySyntax =
    | Expr of ExprSyntax
    | Native


type FormalSyntax =
    { ID: AstNode<ID>
      TYPE: AstNode<TYPENAME> }


[<RequireQualifiedAccess>]
type AttrSyntax =
    { ID: AstNode<ID>
      TYPE: AstNode<TYPENAME>
      Initial: AstNode<AttrInitialSyntax> }


[<RequireQualifiedAccess>]
type AttrInitialSyntax =
    | Expr of ExprSyntax
    | Native


type BlockSyntax =
    { Stmts: AstNode<StmtSyntax> []
      Expr: AstNode<ExprSyntax> }


[<RequireQualifiedAccess>]
type StmtSyntax =
    | Var of VarSyntax
    | Expr of ExprSyntax


type VarSyntax =
    { ID: AstNode<ID>
      TYPE: AstNode<TYPENAME>
      Expr: AstNode<ExprSyntax> }


[<RequireQualifiedAccess>]
type ExprSyntax =
    | Assign of id: AstNode<ID> * expr: AstNode<ExprSyntax>
    | BoolNegation of AstNode<ExprSyntax>
    | UnaryMinus of AstNode<ExprSyntax>
    | If of condition: AstNode<ExprSyntax> * then_branch: AstNode<ExprSyntax> * else_branch: AstNode<ExprSyntax>
    | While of condition: AstNode<ExprSyntax> * body: AstNode<ExprSyntax>
    | LtEq of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
    | GtEq of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
    | Lt of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
    | Gt of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
    | EqEq of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
    | NotEq of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
    | Mul of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
    | Div of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
    | Sum of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
    | Sub of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
    | Match of expr: AstNode<ExprSyntax> * cases_hd: AstNode<CaseSyntax> * cases_tl: AstNode<CaseSyntax> []
    | Dispatch of receiver: AstNode<ExprSyntax> * method_id: AstNode<ID> * actuals: AstNode<ExprSyntax> []
    // Primary
    | ImplicitThisDispatch of method_id: AstNode<ID> * actuals: AstNode<ExprSyntax> []
    | SuperDispatch of method_id: AstNode<ID> * actuals: AstNode<ExprSyntax> []
    | New of type_name: AstNode<TYPENAME> * actuals: AstNode<ExprSyntax> []
    | BracedBlock of BlockSyntax voption
    | ParensExpr of AstNode<ExprSyntax>
    | Id of ID
    | Int of INT
    | Str of STRING
    | Bool of BOOL
    | This
    | Null
    | Unit


type CaseSyntax =
    { Pattern: AstNode<PatternSyntax>
      Block: AstNode<CaseBlockSyntax> }


[<RequireQualifiedAccess>]
type CaseBlockSyntax =
    | Implicit of BlockSyntax
    | BracedBlock of BlockSyntax voption


[<RequireQualifiedAccess>]
type PatternSyntax =
    | IdType of id:AstNode<ID> * pattern_type:AstNode<TYPENAME>
    | Null


type ID =
    | ID of value: string
    member this.Value = let (ID value) = this in value
    override this.ToString() = this.Value


type TYPENAME =
    | TYPENAME of value: string
    member this.Value = let (TYPENAME value) = this in value
    override this.ToString() = this.Value


type INT =
    | INT of value: int
    member this.Value = let (INT value) = this in value
    override this.ToString() = this.Value.ToString()


type STRING =
    | STRING of value: string * is_qqq: bool
    member this.Value = let (STRING (value, _)) = this in value
    member this.IsQqq = let (STRING (_, is_qqq)) = this in is_qqq
    member this.ToString(escape_quotes: bool) =
        let quote = if this.IsQqq then "\"\"\"" else "\""
        let quote = if escape_quotes then quote.Replace("\"", "\\\"") else quote
        quote + this.Value + quote
    override this.ToString() = this.ToString(escape_quotes=false)


[<RequireQualifiedAccess>]
type BOOL =
    | True
    | False
    member this.Value = this = BOOL.True
    override this.ToString() = if this.Value then "true" else "false"
