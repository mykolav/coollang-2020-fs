namespace rec LibCool.SemanticParts


open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Text
open LibCool.SourceParts
open LibCool.DiagnosticParts
open LibCool.AstParts
open LibCool.SemanticParts.SemanticParts
open AstExtensions


[<Sealed>]
type TypeComparer(_class_sym_map: IReadOnlyDictionary<TYPENAME, ClassSymbol>) =


    member private this.Resolve(typename: TYPENAME): ClassSymbol =
        _class_sym_map.[typename]


    member this.Conforms(ancestor: ClassSymbol, descendant: ClassSymbol): bool =
        if ancestor.Is(BasicClasses.Any)
        then
            true
        else
        
        if descendant.Is(BasicClasses.Null)
        then
            not (
                ancestor.Is(BasicClasses.Boolean) ||
                ancestor.Is(BasicClasses.Int) ||
                ancestor.Is(BasicClasses.Unit))
        else
        
        let rec conforms (descendant: ClassSymbol): bool =
            if descendant.Name = BasicClasses.Any.Name
            then
                false
            else
            
            if ancestor.Name = descendant.Name
            then
                true
            else
                
            conforms (this.Resolve(descendant.Super))
            
        conforms descendant
        
        
    member this.LeastUpperBound(type1: ClassSymbol, type2: ClassSymbol): ClassSymbol =
        if this.Conforms(ancestor=type1, descendant=type2)
        then type1
        else this.LeastUpperBound(type1=this.Resolve(type1.Super), type2=type2)
        
        
[<IsReadOnly; Struct; DefaultAugmentation(false)>]
type Result<'T>
    = Error
    | Ok of 'T
    with
    member this.IsError: bool =
        match this with
        | Error -> true
        | Ok _  -> false
    member this.IsOk: bool = not this.IsError
    member this.Value: 'T =
        match this with
        | Ok value -> value
        | _ -> invalidOp "Result<'T>.Value"


[<IsReadOnly; Struct>]
type AsmFragment =
    { Type: ClassSymbol
      Asm: string
      Reg: Reg }
    with
    static member Unit =
        { Type=BasicClasses.Unit; Asm=""; Reg=Reg.Null }


[<Sealed>]
type private ClassTranslator(_class_syntax: ClassSyntax,
                             _class_sym_map: IReadOnlyDictionary<TYPENAME, ClassSymbol>,
                             _diags: DiagnosticBag,
                             _source: Source) =
    
    
    let _type_cmp = TypeComparer(_class_sym_map)
    let _reg_set = RegisterSet()
    let _label_gen = LabelGenerator()
    
    
    // Make _sb_data, _sb_code the ctor's parameters?
    let _sb_data = StringBuilder()
    let _sb_code = StringBuilder()
    
    
    let _sym_table = SymbolTable(_class_sym_map.[_class_syntax.NAME.Syntax])
    
    
    let addr_of (sym: Symbol): AsmFragment =
        { AsmFragment.Asm = ""
          Type = _class_sym_map.[sym.Type]
          Reg = Reg.Null }
    
    
    let rec translate_expr: (*expr_syntax:*) ExprSyntax -> Result<AsmFragment> = function
        | ExprSyntax.Assign (id, expr) ->
            translate_assign id expr

        | ExprSyntax.BoolNegation expr ->
            let expr_frag = translate_expr expr.Syntax
            if expr_frag.IsError
            then
                Error
            else
                
            if not (expr_frag.Value.Type.Is(BasicClasses.Int))
            then
                _diags.Error(
                    sprintf "Expected an 'Int' expression but found '%O'"
                            expr_frag.Value.Type.Name,
                    expr.Span)
                
                Error
            else
                
            Ok { AsmFragment.Asm = sprintf "xorq $0xFFFFFFFFFFFFFFFF %s" (_reg_set.NameOf(expr_frag.Value.Reg))
                 Type = expr_frag.Value.Type
                 Reg = expr_frag.Value.Reg }
        
        | ExprSyntax.UnaryMinus expr ->
            let expr_frag = translate_expr expr.Syntax
            if expr_frag.IsError
            then
                Error
            else
                
            if not (expr_frag.Value.Type.Is(BasicClasses.Int))
            then
                _diags.Error(
                    sprintf "Expected an 'Int' expression but found '%O'"
                            expr_frag.Value.Type.Name,
                    expr.Span)
                
                Error
            else
                
            Ok { AsmFragment.Asm = sprintf "negq %s" (_reg_set.NameOf(expr_frag.Value.Reg))
                 Type = expr_frag.Value.Type
                 Reg = expr_frag.Value.Reg }
            
        | ExprSyntax.If (condition, then_branch, else_branch) ->
            let condition_frag = translate_expr condition.Syntax
            if condition_frag.IsError
            then
                Error
            else
                
            if not (condition_frag.Value.Type.Is(BasicClasses.Boolean))
            then
                _diags.Error(
                    sprintf "Expected a 'Boolean' expression bug found '%O'"
                            condition_frag.Value.Type.Name,
                    condition.Span)
                
                Error
            else
            
            let sb_asm = StringBuilder()
            
            // TODO: Emit comparison asm

            _reg_set.Free(condition_frag.Value.Reg)
            
            let then_frag = translate_expr then_branch.Syntax
            let else_frag = translate_expr else_branch.Syntax

            if then_frag.IsError || else_frag.IsError
            then
                Error
            else
                
            let reg = _reg_set.Allocate()
            
            // TODO: Emit branches asm, move each branch result into `reg`
            
            _reg_set.Free(then_frag.Value.Reg)
            _reg_set.Free(else_frag.Value.Reg)
                
            Ok { AsmFragment.Asm = sb_asm.ToString()
                 Type = _type_cmp.LeastUpperBound(then_frag.Value.Type, else_frag.Value.Type)
                 Reg = reg }
        
        | ExprSyntax.While (condition, body) ->
            let condition_frag = translate_expr condition.Syntax
            if condition_frag.IsError
            then
                Error
            else
                
            if not (condition_frag.Value.Type.Is(BasicClasses.Boolean))
            then
                _diags.Error(
                    sprintf "Expected a 'Boolean' expression bug found '%O'"
                            condition_frag.Value.Type.Name,
                    condition.Span)
                
                Error
            else
            
            let sb_asm = StringBuilder()
            
            // TODO: Emit comparison asm

            _reg_set.Free(condition_frag.Value.Reg)
            
            let body_frag = translate_expr body.Syntax
            if body_frag.IsError
            then
                Error
            else
                
            // TODO: Emit the body asm
            
            // The loop's type is always Unit, so we free up `body_frag.Value.Reg`
            _reg_set.Free(body_frag.Value.Reg)
                
            Ok { AsmFragment.Asm = sb_asm.ToString()
                 Type = BasicClasses.Unit
                 Reg = Reg.Null }

        // | ExprSyntax.LtEq of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
        // | ExprSyntax.GtEq of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
        // | ExprSyntax.Lt of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
        // | ExprSyntax.Gt of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
        // | ExprSyntax.EqEq of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
        // | ExprSyntax.NotEq of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
        // | ExprSyntax.Mul of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
        // | ExprSyntax.Div of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
        // | ExprSyntax.Sum of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
        // | ExprSyntax.Sub of left: AstNode<ExprSyntax> * right: AstNode<ExprSyntax>
        // | ExprSyntax.Match of expr: AstNode<ExprSyntax> * cases_hd: AstNode<CaseSyntax> * cases_tl: AstNode<CaseSyntax> []
        // | ExprSyntax.Dispatch of receiver: AstNode<ExprSyntax> * method_id: AstNode<ID> * actuals: AstNode<ExprSyntax> []
        // | ExprSyntax.ImplicitThisDispatch of method_id: AstNode<ID> * actuals: AstNode<ExprSyntax> []
        // | ExprSyntax.SuperDispatch of method_id: AstNode<ID> * actuals: AstNode<ExprSyntax> []
        // | ExprSyntax.New of type_name: AstNode<TYPENAME> * actuals: AstNode<ExprSyntax> []
        // | ExprSyntax.BracedBlock of BlockSyntax voption
        // | ExprSyntax.ParensExpr of AstNode<ExprSyntax>
        // | ExprSyntax.Id of ID
        // | ExprSyntax.Int of INT
        // | ExprSyntax.Str of STRING
        // | ExprSyntax.Bool of BOOL
        // | ExprSyntax.This
        // | ExprSyntax.Null
        // | ExprSyntax.Unit
        | _ ->  Error
        
        
    and translate_assign (id: AstNode<ID>) (expr: AstNode<ExprSyntax>): Result<AsmFragment> =
        let addr_frag = addr_of (_sym_table.Resolve(id.Syntax))
        let expr_frag = translate_expr expr.Syntax
        if expr_frag.IsError
        then
            _reg_set.Free(addr_frag.Reg)
            Error
        else
        
        if not (_type_cmp.Conforms(ancestor=addr_frag.Type, descendant=expr_frag.Value.Type))
        then
            _reg_set.Free(addr_frag.Reg)
            _reg_set.Free(expr_frag.Value.Reg)

            _diags.Error(
                sprintf "The expression's type '%O' does not conform to the type '%O' of '%O'"
                        expr_frag.Value.Type.Name
                        addr_frag.Type
                        id.Syntax,
                expr.Span)
            
            Error
        else

        let asm = sprintf "movq %s, %s" (_reg_set.NameOf(expr_frag.Value.Reg)) addr_frag.Asm

        _reg_set.Free(addr_frag.Reg)

        // We do not free up expr_frag.Value.Reg,
        // to support assignments of the form `ID = ID = ...`

        Ok { AsmFragment.Asm = asm
             Reg = expr_frag.Value.Reg
             Type = addr_frag.Type }
        
        
    let translate_var (var_node: AstNode<VarSyntax>): Result<AsmFragment> =
        _sym_table.Add (Symbol.Of(var_node, _sym_table.MethodSymCount))
        let assign_frag = translate_assign var_node.Syntax.ID var_node.Syntax.Expr
        if assign_frag.IsError
        then
            Error
        else
            
        _reg_set.Free(assign_frag.Value.Reg)
        
        // The var declaration is not an expression, so `Reg = Reg.Null` 
        Ok { assign_frag.Value with Reg = Reg.Null }

    
    let translate_block (block_syntax_opt: BlockSyntax voption): Result<AsmFragment> =
        match block_syntax_opt with
        | ValueNone ->
            Ok AsmFragment.Unit
        | ValueSome block_syntax ->
            _sym_table.EnterBlock()
        
            let sb_asm = StringBuilder()
            block_syntax.Stmts
            |> Seq.iter (fun stmt_node ->
                match stmt_node.Syntax with
                | StmtSyntax.Var var_syntax ->
                    let var_frag = translate_var (stmt_node.Map(fun _ -> var_syntax))
                    if var_frag.IsOk
                    then
                        sb_asm.AppendLine(var_frag.Value.Asm) |> ignore
                | StmtSyntax.Expr expr_syntax ->
                    let expr_frag = translate_expr expr_syntax
                    if expr_frag.IsOk
                    then
                        _reg_set.Free(expr_frag.Value.Reg)
                        sb_asm.AppendLine(expr_frag.Value.Asm) |> ignore)
            
            let expr_frag = translate_expr block_syntax.Expr.Syntax
            
            _sym_table.LeaveBlock()
            
            if expr_frag.IsError
            then
                Error
            else
                
            sb_asm.AppendLine(expr_frag.Value.Asm) |> ignore
            Ok { AsmFragment.Asm = sb_asm.ToString()
                 Type = expr_frag.Value.Type
                 Reg = expr_frag.Value.Reg }


    let translate_attr (attr_node: AstNode<AttrSyntax>): Result<string> =
        let initial_node = attr_node.Syntax.Initial
        let expr_node =
            match initial_node.Syntax with
            | AttrInitialSyntax.Expr expr_syntax ->
                AstNode.Of(expr_syntax, initial_node.Span)
            | AttrInitialSyntax.Native ->
                invalidOp "AttrInitialSyntax.Native"
                
        let initial_frag = translate_expr expr_node.Syntax
        if initial_frag.IsError
        then
            Error
        else
            
        let attr_ty = _class_sym_map.[attr_node.Syntax.TYPE.Syntax]
        if not (_type_cmp.Conforms(ancestor=attr_ty, descendant=initial_frag.Value.Type))
        then
            _diags.Error(
                sprintf "The initial expression's type '%O' does not conform to the attribute's type '%O'"
                        initial_frag.Value.Type.Name
                        attr_ty.Name,
                initial_node.Span)
            Error
        else
            
        let asm = ""
        Ok asm


    let translate_ctor () =
    
        // .ctor's formals are varformals,
        // .ctor's body is
        //   - an invocation of the super's .ctor with actuals from the extends syntax
        //   - assign values passed as formals to attrs derived from varformals
        //   - assign initial exprs to attrs defined in the class
        //   - concatenated blocks (if any)
        //   - ExprSyntax.This is always appended to the .ctor's end
        //     (as a result, the last block's last expr's type doesn't have to match the class' type)

        let sb_ctor_body = StringBuilder()
        
        _sym_table.EnterMethod()
        _sym_table.Add(Symbol.This(_class_syntax))
        
        // By a cruel twist of fate, you can't say `this.ID = ...` in Cool2020.
        // Gotta be creative and prefix formal names with "."
        // to avoid shadowing attr names by the ctor's formal names.
        _class_syntax.VarFormals
        |> Seq.iter (fun vf_node ->
            let sym = Symbol.Of(formal_node=vf_node.Map(fun vf -> vf.AsFormalSyntax(id_prefix=".")),
                                index=_sym_table.MethodSymCount)
            _sym_table.Add(sym))
        
        // We're entering .ctor's body, which is a block.
        _sym_table.EnterBlock()
        
        // Invoke the super's .ctor with actuals from the extends syntax
        // SuperDispatch of method_id: AstNode<ID> * actuals: AstNode<ExprSyntax> []
        let extends_syntax = _class_syntax.ExtendsSyntax
        let super_dispatch_syntax = ExprSyntax.SuperDispatch (
                                        method_id=AstNode.Virtual(ID ".ctor"),
                                        actuals=extends_syntax.Actuals)
        
        let super_dispatch_frag = translate_expr super_dispatch_syntax
        if super_dispatch_frag.IsOk
        then
            sb_ctor_body.AppendLine(super_dispatch_frag.Value.Asm) |> ignore
            
        // Assign values passed as formals to attrs derived from varformals.
        _class_syntax.VarFormals
        |> Seq.iter (fun vf_node ->
            let attr_name = vf_node.Syntax.ID.Syntax.Value
            let assign_syntax =
                ExprSyntax.Assign(id=AstNode.Virtual(ID attr_name),
                                  expr=AstNode.Virtual(ExprSyntax.Id (ID ("." + attr_name))))

            let assign_frag = translate_expr assign_syntax
            if assign_frag.IsOk
            then
                sb_ctor_body.AppendLine(assign_frag.Value.Asm) |> ignore)
        
        // Assign initial values to attributes declared in the class.
        _class_syntax.Features
        |> Seq.where (fun feature_node -> feature_node.Syntax.IsAttr)
        |> Seq.iter (fun feature_node ->
            let attr_frag = translate_attr (feature_node.Map(fun it -> it.AsAttrSyntax))
            if attr_frag.IsOk
            then
                sb_ctor_body.AppendLine(attr_frag.Value) |> ignore)
        
        // Translate blocks.
        _class_syntax.Features
        |> Seq.where (fun feature_node -> feature_node.Syntax.IsBracedBlock)
        |> Seq.iter (fun feature_node ->
            let block_frag = translate_block feature_node.Syntax.AsBlockSyntax
            if block_frag.IsOk
            then
                sb_ctor_body.AppendLine(block_frag.Value.Asm) |> ignore)
        
        // Append ExprSyntax.This to the .ctor's end.
        // (As a result, the last block's last expr's type doesn't have to match the class' type.)
        let this_syntax = ExprSyntax.This
        let this_frag = translate_expr this_syntax
        if this_frag.IsOk
        then
            // TODO: Here we need to emit assembly moving this_frag.Reg into the return value register
            //       Keep in mind, returning a value is more relevant for regular method
            //       as a ctor return value is the one we passed it in `this`.
            //       So, maybe, we aren't going to return anything at all from ctors.
            this_frag.Value.Reg |> ignore
            
        // TODO: Generate the method header and footer, insert `sb_ctor_body` in between them.

        _sym_table.LeaveBlock()
        _sym_table.LeaveMethod()
    
    
    let translate_method (method_node: AstNode<MethodSyntax>) =
        _sym_table.EnterMethod()

        // Add the method's formal parameters to the symbol table.
        _sym_table.Add(Symbol.This(_class_syntax))
        
        method_node.Syntax.Formals
        |> Seq.iter (fun formal_node ->
            let sym = Symbol.Of(formal_node, index=_sym_table.MethodSymCount)
            _sym_table.Add(sym))
        
        // Translate the method's body
        let body_frag = translate_expr method_node.Syntax.Body.Syntax.AsExprSyntax
        if body_frag.IsOk
        then
            // Make sure, the body's type conforms to the return type.
            let return_type = _class_sym_map.[method_node.Syntax.RETURN.Syntax]
            if not (_type_cmp.Conforms(ancestor=return_type, descendant=body_frag.Value.Type))
            then
                _diags.Error(
                    sprintf "The method's body type '%O' does not conform to the declared return type '%O'"
                            body_frag.Value.Type.Name
                            return_type.Name,
                    method_node.Syntax.Body.Span)

        _sym_table.LeaveMethod()
    
    
    member this.Translate(): unit =
        translate_ctor ()
        _class_syntax.Features
            |> Seq.where (fun feature_node -> feature_node.Syntax.IsMethod)
            |> Seq.iter (fun feature_node -> translate_method (feature_node.Map(fun it -> it.AsMethodSyntax)))


[<Sealed>]
type private ProgramTranslator(_program_syntax: ProgramSyntax,
                               _class_sym_map: IReadOnlyDictionary<TYPENAME, ClassSymbol>,
                               _diags: DiagnosticBag,
                               _source: Source) =
    
    
    let _sb_data = StringBuilder()
    let _sb_code = StringBuilder()
    
    
    let translate_class (class_node: AstNode<ClassSyntax>) =
        ClassTranslator(class_node.Syntax, _class_sym_map, _diags, _source).Translate()


    member this.Translate(): string =
        _program_syntax.Classes |> Array.iter translate_class
        ""


[<Sealed>]
type Translator private () =
        
    
    static member Translate(program_syntax: ProgramSyntax, diags: DiagnosticBag, source: Source): string =
        let class_node_map = ClassDeclCollector(program_syntax, diags, source).Collect()
        if diags.ErrorsCount <> 0
        then
            ""
        else
            
        let class_sym_map = ClassSymbolCollector(program_syntax,
                                                 class_node_map,
                                                 source,
                                                 diags).Collect()
        if diags.ErrorsCount <> 0
        then
            ""
        else
        
        let asm = ProgramTranslator(program_syntax, class_sym_map, diags, source).Translate()
        asm
