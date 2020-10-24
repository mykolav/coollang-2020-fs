namespace rec LibCool.SemanticParts


open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Text
open LibCool.SourceParts
open LibCool.DiagnosticParts
open LibCool.AstParts
open LibCool.SemanticParts
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
            
        if descendant.Is(BasicClasses.Nothing)
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
        
        
    // member this.LeastUpperBound(types: seq<ClassSymbol>): ClassSymbol =
    //     let rec least_upper_bound types =
    //         match List.ofSeq types with
    //         | [] -> invalidOp "types.Length = 0"
    //         | [ t1 ] -> t1
    //         | t1::t2::types_tl -> least_upper_bound (this.LeastUpperBound(t1, t2)::types_tl)
    //         
    //     least_upper_bound (List.ofSeq types)
        
        
    member this.LeastUpperBound(types: ClassSymbol[]): ClassSymbol =
        if types.Length = 0
        then
            invalidOp "types.Length = 0"
            
        let mutable least_upper_bound = types.[0]
        let mutable i = 1
        while i < types.Length do
            least_upper_bound <- this.LeastUpperBound(least_upper_bound, types.[i])
            i <- i + 1
            
        least_upper_bound


[<IsReadOnly; Struct; DefaultAugmentation(false)>]
type Res<'T>
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
      Asm: StringBuilder
      Reg: Reg }
    with
    static member Unit =
        { Type=BasicClasses.Unit; Asm=StringBuilder(); Reg=Reg.Null }


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
        { AsmFragment.Asm = StringBuilder()
          Type = _class_sym_map.[sym.Type]
          Reg = Reg.Null }
    
    
    let rec translate_expr (expr_node: AstNode<ExprSyntax>): Res<AsmFragment> =
        match expr_node.Syntax with
        | ExprSyntax.Assign (id, expr) ->
            translate_assign id expr

        | ExprSyntax.BoolNegation expr ->
            let result = translate_unaryop_operand expr (*op=*)"!" (*expected_ty=*)BasicClasses.Boolean
            if result.IsError
            then
                Error

            else
                
            let expr_frag = result.Value
                
            let asm = sprintf "xorq $0xFFFFFFFFFFFFFFFF %s" (_reg_set.NameOf(expr_frag.Reg))
            Ok { expr_frag with
                    Asm = expr_frag.Asm.AppendLine(asm) }
        
        | ExprSyntax.UnaryMinus expr ->
            let result = translate_unaryop_operand expr (*op=*)"-" (*expected_ty=*)BasicClasses.Int
            if result.IsError
            then
                Error
            else
                
            let expr_frag = result.Value
            
            let asm = sprintf "negq %s" (_reg_set.NameOf(expr_frag.Reg))
            Ok { expr_frag with
                    Asm = expr_frag.Asm.AppendLine(asm) }
            
        | ExprSyntax.If (condition, then_branch, else_branch) ->
            let condition_frag = translate_expr condition
            if condition_frag.IsError
            then
                Error
            else
                
            if not (condition_frag.Value.Type.Is(BasicClasses.Boolean))
            then
                _diags.Error(
                    sprintf "'if' expects a 'Boolean' condition but found '%O'"
                            condition_frag.Value.Type.Name,
                    condition.Span)
                
                Error
            else
            
            let sb_asm = condition_frag.Value.Asm
            
            // TODO: Emit comparison asm

            _reg_set.Free(condition_frag.Value.Reg)
            
            let then_frag = translate_expr then_branch
            let else_frag = translate_expr else_branch

            if then_frag.IsError || else_frag.IsError
            then
                Error
            else
                
            let reg = _reg_set.Allocate()
            
            // TODO: Emit branches asm, move each branch result into `reg`
            
            _reg_set.Free(then_frag.Value.Reg)
            _reg_set.Free(else_frag.Value.Reg)
                
            Ok { AsmFragment.Asm = sb_asm
                 Type = _type_cmp.LeastUpperBound(then_frag.Value.Type, else_frag.Value.Type)
                 Reg = reg }
        
        | ExprSyntax.While (condition, body) ->
            let condition_frag = translate_expr condition
            if condition_frag.IsError
            then
                Error
            else
                
            if not (condition_frag.Value.Type.Is(BasicClasses.Boolean))
            then
                _diags.Error(
                    sprintf "'while' expects a 'Boolean' condition but found '%O'"
                            condition_frag.Value.Type.Name,
                    condition.Span)
                
                Error
            else
            
            let sb_asm = condition_frag.Value.Asm
            
            // TODO: Emit comparison asm

            _reg_set.Free(condition_frag.Value.Reg)
            
            let body_frag = translate_expr body
            if body_frag.IsError
            then
                Error
            else
                
            // TODO: Emit the body asm
            
            // The loop's type is always Unit, so we free up `body_frag.Value.Reg`
            _reg_set.Free(body_frag.Value.Reg)
                
            Ok { AsmFragment.Asm = sb_asm
                 Type = BasicClasses.Unit
                 Reg = Reg.Null }

        | ExprSyntax.LtEq (left, right) ->
            let operands = translate_infixop_int_operands left right (*op=*)"<="
            if operands.IsError
            then
                Error
            else
            
            let left_frag, right_frag = operands.Value
            
            _reg_set.Free(left_frag.Reg)
            _reg_set.Free(right_frag.Reg)
            
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = BasicClasses.Boolean
                 Reg = Reg.Null }
        
        | ExprSyntax.GtEq (left, right) ->
            let operands = translate_infixop_int_operands left right (*op=*)">="
            if operands.IsError
            then
                Error
            else
            
            let left_frag, right_frag = operands.Value

            _reg_set.Free(left_frag.Reg)
            _reg_set.Free(right_frag.Reg)
            
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = BasicClasses.Boolean
                 Reg = Reg.Null }
        
        | ExprSyntax.Lt (left, right) ->
            let operands = translate_infixop_int_operands left right (*op=*)"<"
            if operands.IsError
            then
                Error
            else
            
            let left_frag, right_frag = operands.Value
                
            _reg_set.Free(left_frag.Reg)
            _reg_set.Free(right_frag.Reg)
            
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = BasicClasses.Boolean
                 Reg = Reg.Null }
        
        | ExprSyntax.Gt (left, right) ->
            let operands = translate_infixop_int_operands left right (*op=*)">"
            if operands.IsError
            then
                Error
            else
            
            let left_frag, right_frag = operands.Value

            _reg_set.Free(left_frag.Reg)
            _reg_set.Free(right_frag.Reg)
            
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = BasicClasses.Boolean
                 Reg = Reg.Null }
        
        | ExprSyntax.EqEq (left, right) ->
            let operands = translate_eqop_operands left right "=="
            if operands.IsError
            then
                Error
            else
                
            let left_frag, right_frag = operands.Value
                
            _reg_set.Free(left_frag.Reg)
            _reg_set.Free(right_frag.Reg)
            
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = BasicClasses.Boolean
                 Reg = Reg.Null }
        
        | ExprSyntax.NotEq (left, right) ->
            let operands = translate_eqop_operands left right "!="
            if operands.IsError
            then
                Error
            else
                
            let left_frag, right_frag = operands.Value
                
            _reg_set.Free(left_frag.Reg)
            _reg_set.Free(right_frag.Reg)
            
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = BasicClasses.Boolean
                 Reg = Reg.Null }
        
        | ExprSyntax.Mul (left, right) ->
            let operands = translate_infixop_int_operands left right "*"
            if operands.IsError
            then
                Error
            else
            
            let left_frag, right_frag = operands.Value    

            _reg_set.Free(left_frag.Reg)
            _reg_set.Free(right_frag.Reg)
            
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = BasicClasses.Int
                 Reg = Reg.Null }
        
        | ExprSyntax.Div (left, right) ->
            let operands = translate_infixop_int_operands left right "/"
            if operands.IsError
            then
                Error
            else
            
            let left_frag, right_frag = operands.Value    

            _reg_set.Free(left_frag.Reg)
            _reg_set.Free(right_frag.Reg)
            
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = BasicClasses.Int
                 Reg = Reg.Null }
        
        | ExprSyntax.Sum (left, right) ->
            let typecheck left_frag right_frag: bool =
                if not ((left_frag.Type.Is(BasicClasses.Int) &&
                         right_frag.Type.Is(BasicClasses.Int)) ||
                        (left_frag.Type.Is(BasicClasses.String) &&
                         right_frag.Type.Is(BasicClasses.String)))
                then
                    _diags.Error(
                        sprintf "'+' cannot be applied to operands of type '%O' and '%O'; only to 'Int' and 'Int' or 'String' and 'String'"
                                left_frag.Type.Name
                                right_frag.Type.Name,
                        left.Span)
                    false
                else
                    true

            let operands = translate_infixop_operands left right typecheck
            if operands.IsError
            then
                Error
            else
            
            let left_frag, right_frag = operands.Value    

            _reg_set.Free(left_frag.Reg)
            _reg_set.Free(right_frag.Reg)
            
            let ty = left_frag.Type
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = ty
                 Reg = Reg.Null }
        
        | ExprSyntax.Sub (left, right) ->
            let operands = translate_infixop_int_operands left right "-"
            if operands.IsError
            then
                Error
            else
            
            let left_frag, right_frag = operands.Value    

            _reg_set.Free(left_frag.Reg)
            _reg_set.Free(right_frag.Reg)
            
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = BasicClasses.Int
                 Reg = Reg.Null }
        
        | ExprSyntax.Match (expr, cases_hd, cases_tl) ->
            let expr_frag = translate_expr expr
            if expr_frag.IsError
            then
                Error
            else
            
            let cases = Array.concat [[| cases_hd |]; cases_tl]    
            let patterns = cases |> Array.map (fun case ->
                match case.Syntax.Pattern.Syntax with
                | PatternSyntax.IdType (_, ty) -> ty
                | PatternSyntax.Null -> AstNode.Virtual(BasicClassNames.Null))
            
            let mutable pattern_error = false
            for i in 0 .. patterns.Length-1 do
                let pattern = patterns.[i]
                if pattern.Syntax <> BasicClassNames.Null &&
                   (typecheck_type pattern) = Error
                then
                    pattern_error <- true
                else
                
                let pattern_ty = _class_sym_map.[pattern.Syntax]    
                if not (_type_cmp.Conforms(pattern_ty, expr_frag.Value.Type) ||
                        _type_cmp.Conforms(expr_frag.Value.Type, pattern_ty))
                then
                    _diags.Error(
                        sprintf "'%O' and '%O' are not parts of the same inheritance chain. As a result this case is unreachable"
                                expr_frag.Value.Type
                                pattern_ty,
                        pattern.Span)
                    pattern_error <- true
                else
                
                // if `i` = 0, we'll have `for j in 0 .. -1 do`
                // that will not perform a single iteration.
                for j in 0 .. i-1 do
                    let prev_pattern = patterns.[j]
                    if _class_sym_map.ContainsKey(prev_pattern.Syntax)
                    then
                        let prev_pattern_ty = _class_sym_map.[prev_pattern.Syntax]    
                        if _type_cmp.Conforms(ancestor=prev_pattern_ty, descendant=pattern_ty)
                        then
                            _diags.Error(
                                sprintf "This case is shadowed by an earlier case at %O"
                                        (_source.Map(prev_pattern.Span.First)),
                                pattern.Span)
                            pattern_error <- true
            
            let sym_index = _sym_table.MethodSymCount
            let block_frags =
                cases |> Array.map (fun case ->
                    _sym_table.EnterBlock()
                    
                    match case.Syntax.Pattern.Syntax with
                    | PatternSyntax.IdType (id, ty) ->
                        _sym_table.Add({ Symbol.Name = id.Syntax
                                         Type = ty.Syntax
                                         Index = sym_index
                                         SyntaxSpan = case.Syntax.Pattern.Span
                                         Kind = SymbolKind.Var })
                    | PatternSyntax.Null ->
                        ()
                        
                    let block_frag = translate_block case.Syntax.Block.Syntax.AsBlockSyntax
                    _sym_table.LeaveBlock()
                    
                    block_frag)
            
            if pattern_error || (block_frags |> Seq.exists (fun it -> it.IsError))
            then
                Error
            else
                
            let block_types = block_frags |> Array.map (fun it -> it.Value.Type)
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = _type_cmp.LeastUpperBound(block_types)
                 Reg = Reg.Null }

        | ExprSyntax.Dispatch (receiver, method_id, actuals) ->
            translate_dispatch receiver method_id actuals
            
        | ExprSyntax.ImplicitThisDispatch (method_id, actuals) ->
            translate_dispatch (AstNode.Virtual(ExprSyntax.This)) method_id actuals
            
        | ExprSyntax.SuperDispatch (method_id, actuals) ->
            let super_sym = _class_sym_map.[_class_syntax.ExtendsSyntax.SUPER.Syntax] 
            if not (super_sym.Methods.ContainsKey(method_id.Syntax))
            then
                _diags.Error(
                    sprintf "'%O' does not contain a definition for '%O'"
                            super_sym.Name
                            method_id.Syntax,
                    method_id.Span)
                Error
            else
                
            let this_frag = translate_expr (AstNode.Virtual(ExprSyntax.This))
            if this_frag.IsError
            then
                Error
            else
                
            let method_sym = super_sym.Methods.[method_id.Syntax]
            let method_name = sprintf "'%O.%O'" super_sym.Name method_sym.Name

            let actuals_frag = translate_actuals method_name method_id.Span method_sym (*formal_name=*)"formal" actuals
            if actuals_frag.IsError
            then
                Error
            else

            Ok { AsmFragment.Asm = StringBuilder()
                 Type = _class_sym_map.[method_sym.ReturnType]
                 Reg = Reg.Null }
            
        | ExprSyntax.New (type_name, actuals) ->
            if (typecheck_type type_name) = Error
            then
                Error
            else
                
            let ty = _class_sym_map.[type_name.Syntax]
            
            if ty.Is(BasicClasses.Any) || ty.Is(BasicClasses.Int) ||
               ty.Is(BasicClasses.Unit) || ty.Is(BasicClasses.Boolean)
            then
                _diags.Error(
                    sprintf "'new %O' is not allowed" type_name.Syntax,
                    type_name.Span)
                Error
            else
            
            let method_sym = ty.Ctor
            let method_name = sprintf "Constructor of '%O'" ty.Name

            let actuals_frag =
                translate_actuals method_name
                                  type_name.Span
                                  method_sym
                                  (*formal_name=*)"varformal"
                                  actuals
            if actuals_frag.IsError
            then
                Error
            else

            Ok { AsmFragment.Asm = StringBuilder()
                 Type = ty
                 Reg = Reg.Null }
                
        | ExprSyntax.BracedBlock block ->
            translate_block block
                
        | ExprSyntax.ParensExpr expr ->
            translate_expr expr

        | ExprSyntax.Id id ->
            let sym = _sym_table.TryResolve(id)
            match sym with
            | ValueNone ->
                _diags.Error(
                    sprintf "The name '%O' does not exist in the current context" id,
                    expr_node.Span)
                Error
            | ValueSome sym ->
                let ty = _class_sym_map.[sym.Type]
                Ok { AsmFragment.Asm = StringBuilder()
                     Type = ty
                     Reg = Reg.Null }
            
        | ExprSyntax.Int value ->
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = BasicClasses.Int
                 Reg = Reg.Null }
            
        | ExprSyntax.Str value ->
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = BasicClasses.String
                 Reg = Reg.Null }

        | ExprSyntax.Bool value ->
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = BasicClasses.Boolean
                 Reg = Reg.Null }

        | ExprSyntax.This ->
            let sym = _sym_table.Resolve(ID "this")
            let ty = _class_sym_map.[sym.Type]
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = ty 
                 Reg = Reg.Null }

        | ExprSyntax.Null ->
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = BasicClasses.Null 
                 Reg = Reg.Null }

        | ExprSyntax.Unit ->
            Ok AsmFragment.Unit
        
        
    and translate_assign (id: AstNode<ID>) (expr: AstNode<ExprSyntax>): Res<AsmFragment> =
        let addr_frag = addr_of (_sym_table.Resolve(id.Syntax))
        let expr_frag = translate_expr expr
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
                        addr_frag.Type.Name
                        id.Syntax,
                expr.Span)
            
            Error
        else

        let asm = sprintf "movq %s, %s" (_reg_set.NameOf(expr_frag.Value.Reg)) (addr_frag.Asm.ToString())
        _reg_set.Free(addr_frag.Reg)

        // We do not free up expr_frag.Value.Reg,
        // to support assignments of the form `ID = ID = ...`

        Ok { AsmFragment.Asm = expr_frag.Value.Asm.AppendLine(asm)
             Reg = expr_frag.Value.Reg
             Type = addr_frag.Type }
        
        
    and translate_unaryop_operand (expr: AstNode<ExprSyntax>)
                                  (op: string)
                                  (expected_ty: ClassSymbol)
                                  : Res<AsmFragment> =
        let expr_frag = translate_expr expr
        if expr_frag.IsError
        then
            Error
        else
        
        if not (expr_frag.Value.Type.Is(expected_ty))
        then
            _diags.Error(
                sprintf "Unary '%s' expects an operand of type '%O' but found '%O'"
                        op
                        expected_ty.Name
                        expr_frag.Value.Type.Name,
                expr.Span)
            
            Error
        else
            
        expr_frag
        
        
    and translate_infixop_int_operands (left: AstNode<ExprSyntax>)
                                       (right: AstNode<ExprSyntax>)
                                       (op: string)
                                       : Res<(AsmFragment * AsmFragment)> =
        let typecheck left_frag right_frag: bool =
            if not (left_frag.Type.Is(BasicClasses.Int) &&
                    right_frag.Type.Is(BasicClasses.Int))
            then
                _diags.Error(
                    sprintf "'%s' cannot be applied to operands of type '%O' and '%O'; only to 'Int' and 'Int'"
                            op
                            left_frag.Type.Name
                            right_frag.Type.Name,
                    left.Span)
                false
            else
                true

        translate_infixop_operands left right typecheck
        
        
    and translate_eqop_operands (left: AstNode<ExprSyntax>)
                                (right: AstNode<ExprSyntax>)
                                (op: string)
                                : Res<(AsmFragment * AsmFragment)> =
        let typecheck left_frag right_frag: bool =
            if not (_type_cmp.Conforms(left_frag.Type, right_frag.Type) ||
                    _type_cmp.Conforms(right_frag.Type, left_frag.Type))
            then
                _diags.Error(
                    sprintf "'%s' cannot be applied to operands of type '%O' and '%O'"
                            op
                            left_frag.Type.Name
                            right_frag.Type.Name,
                    left.Span)
                false
            else
                true

        translate_infixop_operands left right typecheck
        
        
    and translate_infixop_operands (left: AstNode<ExprSyntax>)
                                   (right: AstNode<ExprSyntax>)
                                   (typecheck: AsmFragment -> AsmFragment -> bool)
                                   : Res<(AsmFragment * AsmFragment)> =
        let left_frag = translate_expr left
        let right_frag = translate_expr right
        
        if left_frag.IsError || right_frag.IsError
        then
            Error
        else
            
        if not (typecheck left_frag.Value right_frag.Value)
        then
            Error
        else

        Ok (left_frag.Value, right_frag.Value)
    
    
    and translate_dispatch (receiver: AstNode<ExprSyntax>)
                           (method_id: AstNode<ID>)
                           (actuals: AstNode<ExprSyntax>[])
                           : Res<AsmFragment> =
        let receiver_frag = translate_expr receiver
        if receiver_frag.IsError
        then
            Error
        else
        
        if not (receiver_frag.Value.Type.Methods.ContainsKey(method_id.Syntax))
        then
            _diags.Error(
                sprintf "'%O' does not contain a definition for '%O'"
                        receiver_frag.Value.Type.Name
                        method_id.Syntax,
                method_id.Span)
            Error
        else
            
        let method_sym = receiver_frag.Value.Type.Methods.[method_id.Syntax]
        let method_name = sprintf "'%O.%O'" receiver_frag.Value.Type.Name method_sym.Name
        
        let actuals_frag = translate_actuals method_name method_id.Span method_sym (*formal_name=*)"formal" actuals
        if actuals_frag.IsError
        then
            Error
        else

        Ok { AsmFragment.Asm = StringBuilder()
             Type = _class_sym_map.[method_sym.ReturnType]
             Reg = Reg.Null }

    
    and translate_actuals (method_name: string)
                          (method_id_span: Span)
                          (method_sym: MethodSymbol)
                          (formal_name: string)
                          (actuals: AstNode<ExprSyntax>[])
                          : Res<AsmFragment> =
        let actual_frags = actuals |> Array.map (fun it -> translate_expr it)
        if actual_frags |> Array.exists (fun it -> it.IsError)
        then
            Error
        else
        
        if method_sym.Formals.Length <> actual_frags.Length
        then
            _diags.Error(
                sprintf "%s takes %d actual(s) but was passed %d"
                        method_name
                        method_sym.Formals.Length
                        actual_frags.Length,
                method_id_span)
            Error
        else
            
        let mutable formal_actual_mismatch = false
        
        for i in 0 .. method_sym.Formals.Length - 1 do
            let formal = method_sym.Formals.[i]
            let formal_ty = _class_sym_map.[formal.Type]
            let actual = actual_frags.[i].Value
            if not (_type_cmp.Conforms(ancestor=formal_ty, descendant=actual.Type))
            then
                formal_actual_mismatch <- true
                _diags.Error(
                    sprintf "The actual's type '%O' does not conform to the %s's type '%O'"
                            actual.Type.Name
                            formal_name
                            formal_ty.Name,
                    actuals.[i].Span)

        Ok { AsmFragment.Asm = StringBuilder()
             Type = _class_sym_map.[method_sym.ReturnType]
             Reg = Reg.Null }
        
    
    and translate_var (var_node: AstNode<VarSyntax>): Res<AsmFragment> =
        if (typecheck_type var_node.Syntax.TYPE) = Error
        then
            Error
        else
            
        _sym_table.Add(Symbol.Of(var_node, _sym_table.MethodSymCount))
        let assign_frag = translate_assign var_node.Syntax.ID var_node.Syntax.Expr
        if assign_frag.IsError
        then
            Error
        else
            
        _reg_set.Free(assign_frag.Value.Reg)
        
        // The var declaration is not an expression, so `Reg = Reg.Null` 
        Ok { assign_frag.Value with Reg = Reg.Null }

    
    and translate_block (block_syntax_opt: BlockSyntax voption): Res<AsmFragment> =
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
                        sb_asm.Append(var_frag.Value.Asm) |> ignore
                | StmtSyntax.Expr expr_syntax ->
                    let expr_frag = translate_expr (stmt_node.Map(fun _ -> expr_syntax))
                    if expr_frag.IsOk
                    then
                        _reg_set.Free(expr_frag.Value.Reg)
                        sb_asm.Append(expr_frag.Value.Asm) |> ignore)
            
            let expr_frag = translate_expr block_syntax.Expr
            
            _sym_table.LeaveBlock()
            
            if expr_frag.IsError
            then
                Error
            else
                
            sb_asm.Append(expr_frag.Value.Asm) |> ignore
            Ok { AsmFragment.Asm = sb_asm
                 Type = expr_frag.Value.Type
                 Reg = expr_frag.Value.Reg }
            
            
    and typecheck_type (ty_node: AstNode<TYPENAME>): Res<Unit> =
        // Make sure it's not a reference to a system class that is not allowed in user code.
        if _class_sym_map.ContainsKey(ty_node.Syntax)
        then
            let class_sym = _class_sym_map.[ty_node.Syntax]
            if class_sym.IsSpecial
            then
                _diags.Error(
                    sprintf "The type name '%O' is not allowed in user code" ty_node.Syntax,
                    ty_node.Span)
                Error
            else
            
            Ok ()
        else

        // We could not find a symbol corresponding to `ty_node.Syntax`.
        _diags.Error(
            sprintf "The type name '%O' could not be found (is an input file missing?)" ty_node.Syntax,
            ty_node.Span)
        Error


    let translate_attr (attr_node: AstNode<AttrSyntax>): Res<string> =
        let initial_node = attr_node.Syntax.Initial
        let expr_node =
            match initial_node.Syntax with
            | AttrInitialSyntax.Expr expr_syntax ->
                AstNode.Of(expr_syntax, initial_node.Span)
            | AttrInitialSyntax.Native ->
                invalidOp "AttrInitialSyntax.Native"
                
        let initial_frag = translate_expr expr_node
        if initial_frag.IsError
        then
            Error
        else
            
        let attr_sym = _sym_table.Resolve(attr_node.Syntax.ID.Syntax)
        let addr_frag = addr_of attr_sym
        if not (_type_cmp.Conforms(ancestor=addr_frag.Type, descendant=initial_frag.Value.Type))
        then
            _diags.Error(
                sprintf "The initial expression's type '%O' does not conform to the '%O' attribute's type '%O'"
                        initial_frag.Value.Type.Name
                        attr_sym.Name
                        attr_sym.Type,
                initial_node.Span)
            Error
        else
            
        let asm =
            initial_frag.Value.Asm
                .AppendLine(sprintf "movq %s, %s" (_reg_set.NameOf(initial_frag.Value.Reg)) (addr_frag.Asm.ToString()))
                .ToString()
            
        _reg_set.Free(initial_frag.Value.Reg)
        _reg_set.Free(addr_frag.Reg)

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
        
        let super_dispatch_frag = translate_expr (AstNode.Virtual(super_dispatch_syntax))
        if super_dispatch_frag.IsOk
        then
            sb_ctor_body.Append(super_dispatch_frag.Value.Asm) |> ignore
            
        // Assign values passed as formals to attrs derived from varformals.
        _class_syntax.VarFormals
        |> Seq.iter (fun vf_node ->
            let attr_name = vf_node.Syntax.ID.Syntax.Value
            let assign_syntax =
                ExprSyntax.Assign(id=AstNode.Virtual(ID attr_name),
                                  expr=AstNode.Virtual(ExprSyntax.Id (ID ("." + attr_name))))

            let assign_frag = translate_expr (AstNode.Virtual(assign_syntax))
            if assign_frag.IsOk
            then
                sb_ctor_body.Append(assign_frag.Value.Asm) |> ignore)
        
        // Assign initial values to attributes declared in the class.
        _class_syntax.Features
        |> Seq.where (fun feature_node -> feature_node.Syntax.IsAttr)
        |> Seq.iter (fun feature_node ->
            let attr_frag = translate_attr (feature_node.Map(fun it -> it.AsAttrSyntax))
            if attr_frag.IsOk
            then
                sb_ctor_body.Append(attr_frag.Value) |> ignore)
        
        // Translate blocks.
        _class_syntax.Features
        |> Seq.where (fun feature_node -> feature_node.Syntax.IsBracedBlock)
        |> Seq.iter (fun feature_node ->
            let block_frag = translate_block feature_node.Syntax.AsBlockSyntax
            if block_frag.IsOk
            then
                sb_ctor_body.Append(block_frag.Value.Asm) |> ignore)
        
        // Append ExprSyntax.This to the .ctor's end.
        // (As a result, the last block's last expr's type doesn't have to match the class' type.)
        let this_syntax = ExprSyntax.This
        let this_frag = translate_expr (AstNode.Virtual(this_syntax))
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
        
        let mutable override_ok = true

        if method_node.Syntax.Override
        then
            let super_sym = _class_sym_map.[_class_syntax.ExtendsSyntax.SUPER.Syntax]
            let overridden_method_sym = super_sym.Methods.[method_node.Syntax.ID.Syntax]
            
            if overridden_method_sym.Formals.Length <> method_node.Syntax.Formals.Length
            then
                _diags.Error(
                    sprintf "The overriding '%O.%O' method's number of formals %d does not match the overridden '%O.%O' method's number of formals %d"
                            _class_syntax.NAME.Syntax
                            method_node.Syntax.ID.Syntax
                            method_node.Syntax.Formals.Length
                            super_sym.Name
                            method_node.Syntax.ID.Syntax
                            overridden_method_sym.Formals.Length,
                    method_node.Syntax.ID.Span)
                override_ok <- false
                
            overridden_method_sym.Formals |> Array.iteri (fun i overridden_formal_sym ->
                let formal = method_node.Syntax.Formals.[i].Syntax
                if overridden_formal_sym.Type <> formal.TYPE.Syntax
                then
                    _diags.Error(
                        sprintf "The overriding formals's type '%O' does not match to the overridden formal's type '%O'"
                                formal.TYPE.Syntax
                                overridden_formal_sym.Type,
                        formal.TYPE.Span)
                    override_ok <- false)

            let overridden_return_ty = _class_sym_map.[overridden_method_sym.ReturnType]
            let return_ty = _class_sym_map.[method_node.Syntax.RETURN.Syntax]
            if not (_type_cmp.Conforms(ancestor=overridden_return_ty, descendant=return_ty))
            then
                _diags.Error(
                    sprintf "The overriding '%O.%O' method's return type '%O' does not conform to the overridden '%O.%O' method's return type '%O'"
                            _class_syntax.NAME.Syntax
                            method_node.Syntax.ID.Syntax
                            method_node.Syntax.RETURN.Syntax
                            super_sym.Name
                            method_node.Syntax.ID.Syntax
                            overridden_method_sym.ReturnType,
                    method_node.Syntax.RETURN.Span)
                override_ok <- false

        if override_ok
        then
            // Add the method's formal parameters to the symbol table.
            _sym_table.Add(Symbol.This(_class_syntax))
            
            method_node.Syntax.Formals
            |> Seq.iter (fun formal_node ->
                let sym = Symbol.Of(formal_node, index=_sym_table.MethodSymCount)
                _sym_table.Add(sym))
            
            // Translate the method's body
            let body_frag = translate_expr (method_node.Syntax.Body.Map(fun it -> it.AsExprSyntax))
            if body_frag.IsOk
            then
                // Make sure, the body's type conforms to the return type.
                let return_ty = _class_sym_map.[method_node.Syntax.RETURN.Syntax]
                if not (_type_cmp.Conforms(ancestor=return_ty, descendant=body_frag.Value.Type))
                then
                    _diags.Error(
                        sprintf "The method body's type '%O' does not conform to the declared return type '%O'"
                                body_frag.Value.Type.Name
                                return_ty.Name,
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
