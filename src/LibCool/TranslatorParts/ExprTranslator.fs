namespace rec LibCool.TranslatorParts


open System.Collections.Generic
open LibCool.SharedParts
open LibCool.SourceParts
open LibCool.AstParts
open LibCool.SemanticParts
open LibCool.SemanticParts.AstExtensions
open LibCool.TranslatorParts
open LibCool.TranslatorParts.AsmFragments


[<Sealed>]
type private ExprTranslator(_context: TranslationContext,
                            _class_syntax: ClassSyntax,
                            _sym_table: SymbolTable) as this =
    
    
    let rec translate_expr (expr_node: AstNode<ExprSyntax>): Res<AsmFragment> =
        match expr_node.Syntax with
        | ExprSyntax.Assign (id, expr)                         -> translate_assign expr_node.Span id expr
        | ExprSyntax.BoolNegation expr                         -> translate_bool_negation expr_node expr
        | ExprSyntax.UnaryMinus expr                           -> translate_unary_minus expr_node expr
        | ExprSyntax.If (condition,
                         then_branch,
                         else_branch)                          -> translate_if expr_node condition
                                                                                         then_branch
                                                                                         else_branch
        | ExprSyntax.While (condition, body)                   -> translate_while expr_node condition body
        | ExprSyntax.LtEq (left, right)                        -> translate_lt_eq expr_node left right
        | ExprSyntax.GtEq (left, right)                        -> translate_gt_eq expr_node left right
        | ExprSyntax.Lt (left, right)                          -> translate_lt expr_node left right
        | ExprSyntax.Gt (left, right)                          -> translate_gt expr_node left right
        | ExprSyntax.EqEq (left, right)                        -> translate_eq_eq expr_node left right
        | ExprSyntax.NotEq (left, right)                       -> translate_not_eq expr_node left right
        | ExprSyntax.Mul (left, right)                         -> translate_mul expr_node left right
        | ExprSyntax.Div (left, right)                         -> translate_div expr_node left right
        | ExprSyntax.Sum (left, right)                         -> translate_sum expr_node left right
        | ExprSyntax.Sub (left, right)                         -> translate_sub expr_node left right
        | ExprSyntax.Match (expr, cases_hd, cases_tl)          -> translate_match expr_node expr cases_hd cases_tl
        | ExprSyntax.Dispatch (receiver, method_id, actuals)   -> translate_dispatch expr_node
                                                                                     receiver
                                                                                     method_id
                                                                                     actuals
        | ExprSyntax.ImplicitThisDispatch (method_id, actuals) -> translate_dispatch expr_node
                                                                                     (AstNode.Virtual(ExprSyntax.This))
                                                                                     method_id
                                                                                     actuals
        | ExprSyntax.SuperDispatch (method_id, actuals)        -> translate_super_dispatch expr_node method_id actuals
        | ExprSyntax.New (type_name, actuals)                  -> translate_new expr_node type_name actuals
        | ExprSyntax.BracedBlock block                         -> this.TranslateBlock(block)
        | ExprSyntax.ParensExpr expr                           -> translate_expr expr
        | ExprSyntax.Id id                                     -> translate_id expr_node id
        | ExprSyntax.Int int_syntax                            -> translate_int expr_node int_syntax
        | ExprSyntax.Str str_syntax                            -> translate_str expr_node str_syntax
        | ExprSyntax.Bool bool_syntax                          -> translate_bool expr_node bool_syntax
        | ExprSyntax.This                                      -> translate_this expr_node
        | ExprSyntax.Null                                      -> translate_null expr_node
        | ExprSyntax.Unit                                      -> translate_unit expr_node
        
        
    and translate_assign (assign_node_span: Span)
                         (id: AstNode<ID>)
                         (rvalue_expr: AstNode<ExprSyntax>)
                         : Res<AsmFragment> =
        let expr_frag = translate_expr rvalue_expr
        if expr_frag.IsError
        then
            Error
        else
        
        let id_sym = _sym_table.TryResolve(id.Syntax)
        if id_sym.IsNone
        then
            _context.Diags.Error(
                sprintf "The name '%O' does not exist in the current context" id.Syntax,
                id.Span)
            
            _context.RegSet.Free(expr_frag.Value.Reg)
            Error
        else
            
        let addr_frag = this.AddrOf(id_sym.Value)

        if not (_context.TypeCmp.Conforms(ancestor=addr_frag.Type, descendant=expr_frag.Value.Type))
        then
            _context.Diags.Error(
                sprintf "The expression's type '%O' does not conform to the type '%O' of '%O'"
                        expr_frag.Value.Type.Name
                        addr_frag.Type.Name
                        id.Syntax,
                rvalue_expr.Span)
            
            _context.RegSet.Free(addr_frag.Reg)
            _context.RegSet.Free(expr_frag.Value.Reg)
            Error
        else

        let asm =
            this.EmitAsm()
                .Location(assign_node_span)
                .Paste(expr_frag.Value.Asm)
                .In("movq    {0}, {1}", expr_frag.Value.Reg, addr_frag, comment=id.Syntax.ToString())
                .ToString()
        
        _context.RegSet.Free(addr_frag.Reg)

        // We do not free up expr_frag.Value.Reg,
        // to support assignments of the form `ID = ID = ...`

        Ok { AsmFragment.Asm = asm.ToString()
             Reg = expr_frag.Value.Reg
             Type = addr_frag.Type }
        
        
    and translate_bool_negation (bool_negation_node: AstNode<ExprSyntax>)
                                (negated_node: AstNode<ExprSyntax>)
                                : Res<AsmFragment> =
        let negated_frag = translate_unaryop_operand negated_node (*op=*)"!" (*expected_ty=*)BasicClasses.Boolean
        match negated_frag with
        | Error           -> Error
        | Ok negated_frag -> 
            Ok { negated_frag with
                   Asm = this.EmitAsm()
                             .BoolNegation(bool_negation_node.Span, negated_frag) }
        
        
    and translate_unary_minus (unary_minus_node: AstNode<ExprSyntax>)
                              (negated_node: AstNode<ExprSyntax>)
                              : Res<AsmFragment> =
        let negated_frag = translate_unaryop_operand negated_node (*op=*)"-" (*expected_ty=*)BasicClasses.Int
        match negated_frag with
        | Error           -> Error
        | Ok negated_frag ->
            Ok { negated_frag with
                   Asm = this.EmitAsm()
                             .UnaryMinus(unary_minus_node.Span, negated_frag) }
        
        
    and translate_if (if_node: AstNode<ExprSyntax>)
                     (cond_node: AstNode<ExprSyntax>)
                     (then_branch: AstNode<ExprSyntax>)
                     (else_branch: AstNode<ExprSyntax>)
                     : Res<AsmFragment> =
        let then_frag = translate_expr then_branch
        let else_frag = translate_expr else_branch

        if then_frag.IsError || else_frag.IsError
        then
            if then_frag.IsOk then _context.RegSet.Free(then_frag.Value.Reg)
            if else_frag.IsOk then _context.RegSet.Free(else_frag.Value.Reg)
            Error
        else

        let result_reg = _context.RegSet.Allocate("translate_if.result_reg")
        
        let then_asm =
            this.EmitAsm()
                .Paste(then_frag.Value.Asm)
                .In("movq    {0}, {1}", then_frag.Value.Reg, result_reg)
                .ToString()
        
        let else_asm =
            this.EmitAsm()
                .Paste(else_frag.Value.Asm)
                .In("movq    {0}, {1}", else_frag.Value.Reg, result_reg)
                .ToString()

        // It's OK to free up these registers early,
        // as the the condition's code cannot overwrite them
        // (it's located before the branches).
        _context.RegSet.Free(then_frag.Value.Reg)
        _context.RegSet.Free(else_frag.Value.Reg)
            
        let asm = emit_cond {| Name="if"; Span=if_node.Span |} cond_node then_asm else_asm
        match asm with
        | Error  -> Error
        | Ok asm -> 
            Ok { AsmFragment.Asm = asm
                 Type = _context.TypeCmp.LeastUpperBound(then_frag.Value.Type, else_frag.Value.Type)
                 Reg = result_reg }
            
        
    and translate_while while_node cond_node body =
        let body_frag = translate_expr body
        if body_frag.IsError
        then
            Error
        else
        
        let while_cond_label = _context.LabelGen.Generate()

        let body_asm =
            this.EmitAsm()
                .Paste(body_frag.Value.Asm)
                .Jmp(while_cond_label, "while cond")
                .ToString()

        _context.RegSet.Free(body_frag.Value.Reg)
            
        let result_reg = _context.RegSet.Allocate("translate_while.result_reg")
        let done_asm = this.EmitAsm()
                           .Single("movq    ${0}, {1}", RtNames.UnitValue, result_reg, "unit")

        let cond_asm = emit_cond {| Name="while"; Span=cond_node.Span |}
                                 cond_node
                                 (*true_branch_asm*)body_asm
                                 (*false_branch_asm*)done_asm
        if cond_asm.IsError
        then
            _context.RegSet.Free(result_reg)
            Error
        else
            
        let asm =
            this.EmitAsm()
                .Location(while_node.Span)
                .Label(while_cond_label, "while cond")
                .Paste(cond_asm.Value)
                .ToString()
            
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Unit
             Reg = result_reg }

    
    and emit_cond (expr_info: struct {| Name: string; Span: Span |})
                  (cond_node: AstNode<ExprSyntax>)
                  (true_branch_asm: string)
                  (false_branch_asm: string)
                  : Res<string> =
        // We can end up with two conditional structures in the assembly code.
        // The first conditional computes the result of `x > y` and places that in a register.
        // The second conditional compares the register against zero
        // and jumps to the true or false branch of the expression.
        
        // To avoid two conditionals in the assembly, we plug `then` and `else` branches
        // directly into the conditional generated for `x > y`
        // and don't generate the second conditional at all.        
        if cond_node.Syntax.IsComparison
        then
            let left, right, op, jmp =
                match cond_node.Syntax with
                | ExprSyntax.Lt (left, right)   -> left, right, "<", "jl"
                | ExprSyntax.LtEq (left, right) -> left, right, "<=", "jle"
                | ExprSyntax.Gt (left, right)   -> left, right, ">", "jg"
                | ExprSyntax.GtEq (left, right) -> left, right, ">=", "jge"
                | _                             -> invalidOp "Unreachable"
            let operands = translate_infixop_int_operands left right op
            if operands.IsError
            then
                Error
            else
                
            let left_frag, right_frag = operands.Value
            Ok (emit_cmpop_with_branches expr_info.Span
                                         left_frag
                                         right_frag
                                         jmp
                                         (*false_branch*)false_branch_asm
                                         (*true_branch*)true_branch_asm)
        else
            
        // To avoid two conditionals in the assembly, we plug `then` and `else` branches
        // directly into the conditional generated for `x == y`
        // and don't generate the second conditional at all.        
        if cond_node.Syntax.IsEquality
        then
            let left, right, op, equal_branch, unequal_branch =
                match cond_node.Syntax with
                | ExprSyntax.EqEq (left, right)  -> left, right, "==", true_branch_asm, false_branch_asm
                | ExprSyntax.NotEq (left, right) -> left, right, "!=", false_branch_asm, true_branch_asm
                | _                              -> invalidOp "Unreachable"
            let operands = translate_eqop_operands left right op
            if operands.IsError
            then
                Error
            else
                
            let left_frag, right_frag = operands.Value
            Ok (emit_eqop_with_branches expr_info.Span
                                        left_frag
                                        right_frag
                                        unequal_branch
                                        equal_branch)
        else
            
        let cond_frag = translate_expr cond_node
        if cond_frag.IsError
        then
            Error
        else
            
        // Free up the register right away, it's OK if it gets re-used in a branch.
        _context.RegSet.Free(cond_frag.Value.Reg)

        if not (cond_frag.Value.Type.Is(BasicClasses.Boolean))
        then
            _context.Diags.Error(
                sprintf "'%s' expects a 'Boolean' condition but found '%O'"
                        expr_info.Name
                        cond_frag.Value.Type.Name,
                cond_node.Span)

            Error
        else
        
        // We have `(flag) ...` or `(is_satisfied()) ...` as our condition.
        // Instead of plugging `then` and `else` branches in an existing conditional,
        // generate the conditional ourselves.
        Ok (this.EmitAsm().Cond(cond_frag.Value,
                                true_branch_asm,
                                false_branch_asm))
        
        
    and translate_lt_eq lt_eq_node left right =
        let operands = translate_infixop_int_operands left right (*op=*)"<="
        if operands.IsError
        then
            Error
        else
        
        let left_frag, right_frag = operands.Value
        let asm_frag = emit_cmpop lt_eq_node left_frag right_frag "jle "
        
        Ok { AsmFragment.Asm = asm_frag.Asm
             Type = BasicClasses.Boolean
             Reg = asm_frag.Reg }
        
        
    and translate_gt_eq gt_eq_node left right =
        let operands = translate_infixop_int_operands left right (*op=*)">="
        if operands.IsError
        then
            Error
        else
        
        let left_frag, right_frag = operands.Value
        let asm_frag = emit_cmpop gt_eq_node left_frag right_frag "jge "
        
        Ok { AsmFragment.Asm = asm_frag.Asm
             Type = BasicClasses.Boolean
             Reg = asm_frag.Reg }
        
        
    and translate_lt lt_node left right =
        let operands = translate_infixop_int_operands left right (*op=*)"<"
        if operands.IsError
        then
            Error
        else
        
        let left_frag, right_frag = operands.Value
        let asm_frag = emit_cmpop lt_node left_frag right_frag "jl  "
        
        Ok { AsmFragment.Asm = asm_frag.Asm
             Type = BasicClasses.Boolean
             Reg = asm_frag.Reg }
        
        
    and translate_gt gt_node left right =
        let operands = translate_infixop_int_operands left right (*op=*)">"
        if operands.IsError
        then
            Error
        else
        
        let left_frag, right_frag = operands.Value
        let asm_frag = emit_cmpop gt_node left_frag right_frag "jg  "
        
        Ok { AsmFragment.Asm = asm_frag.Asm
             Type = BasicClasses.Boolean
             Reg = asm_frag.Reg }
        
        
    and translate_eq_eq eq_eq_node left right =
        let operands = translate_eqop_operands left right "=="
        if operands.IsError
        then
            Error
        else
            
        let result_reg = _context.RegSet.Allocate("translate_eq_eq.result_reg")
        let equal_branch = this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolTrue, result_reg, "true")
        let unequal_branch = this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolFalse, result_reg, "false")
        let left_frag, right_frag = operands.Value
        let asm = 
            emit_eqop_with_branches eq_eq_node.Span
                                    left_frag
                                    right_frag
                                    unequal_branch
                                    equal_branch
        
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Boolean
             Reg = result_reg }
        
        
    and translate_not_eq not_eq_node left right =
        let operands = translate_eqop_operands left right "!="
        if operands.IsError
        then
            Error
        else
            
        let result_reg = _context.RegSet.Allocate("translate_not_eq.result_reg")
        let unequal_branch = this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolTrue, result_reg, "true")
        let equal_branch = this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolFalse, result_reg, "false")
        let left_frag, right_frag = operands.Value
        let asm = 
            emit_eqop_with_branches not_eq_node.Span
                                    left_frag
                                    right_frag
                                    unequal_branch
                                    equal_branch
                                    
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Boolean
             Reg = result_reg }
        
        
    and translate_mul mul_node left right =
        let operands = translate_infixop_int_operands left right "*"
        if operands.IsError
        then
            Error
        else
        
        let left_frag, right_frag = operands.Value    
        let asm = this.EmitAsm().Mul(mul_node.Span, left_frag, right_frag)

        _context.RegSet.Free(right_frag.Reg)
        
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Int
             Reg = left_frag.Reg }
        
        
    and translate_div div_node left right =
        let operands = translate_infixop_int_operands left right "/"
        if operands.IsError
        then
            Error
        else
        
        let left_frag, right_frag = operands.Value    
        let asm = this.EmitAsm().Div(div_node.Span, left_frag, right_frag)

        _context.RegSet.Free(right_frag.Reg)
        
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Int
             Reg = left_frag.Reg }
        
        
    and translate_sum sum_node left right =
        let check_operands (left_frag: AsmFragment) (right_frag: AsmFragment): bool =
            if not ((left_frag.Type.Is(BasicClasses.Int) &&
                     right_frag.Type.Is(BasicClasses.Int)) ||
                    (left_frag.Type.Is(BasicClasses.String) &&
                     right_frag.Type.Is(BasicClasses.String)))
            then
                _context.Diags.Error(
                    sprintf "'+' cannot be applied to operands of type '%O' and '%O'; only to 'Int' and 'Int' or 'String' and 'String'"
                            left_frag.Type.Name
                            right_frag.Type.Name,
                    left.Span)
                false
            else
                true

        let operands = translate_infixop_operands left right check_operands
        if operands.IsError
        then
            Error
        else
        
        let left_frag, right_frag = operands.Value
  
        let asm = this.EmitAsm().Sum(sum_node.Span, left_frag, right_frag)
        _context.RegSet.Free(left_frag.Reg)
        
        Ok { AsmFragment.Asm = asm.ToString()
             Type = right_frag.Type
             Reg = right_frag.Reg }
        
        
    and translate_sub sub_node left right =
        let operands = translate_infixop_int_operands left right "-"
        if operands.IsError
        then
            Error
        else
        
        let left_frag, right_frag = operands.Value    

        let asm = this.EmitAsm().Sub(sub_node.Span, left_frag, right_frag)
        _context.RegSet.Free(right_frag.Reg)
        
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Int
             Reg = left_frag.Reg }
        
        
    and translate_match match_node expr cases_hd cases_tl =
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

        let pattern_asm_infos = Dictionary<TYPENAME, PatternAsmInfo>()
        for pattern in patterns do
            let pattern_ty = _context.ClassSymMap.[pattern.Syntax]    
            pattern_asm_infos.Add(pattern.Syntax,
                                  { PatternAsmInfo.Label = _context.LabelGen.Generate()
                                    Tag = pattern_ty.Tag })
            
        let tag_reg = _context.RegSet.Allocate("translate_match.tag_reg")
        let expr_location = _context.Source.Map(expr.Span.First)
        
        let asm = this.EmitAsm()
                      .Match(match_node.Span,
                             expr_frag.Value,
                             expr_location,
                             _sym_table.Frame,
                             tag_reg,
                             pattern_asm_infos)
        
        _context.RegSet.Free(expr_frag.Value.Reg)
        _context.RegSet.Free(tag_reg)

        let mutable pattern_error = false
        for i in 0 .. (patterns.Length - 1) do
            let pattern = patterns.[i]
            if pattern.Syntax <> BasicClassNames.Null &&
               (check_typename pattern) = Error
            then
                pattern_error <- true
            else
            
            let pattern_ty = _context.ClassSymMap.[pattern.Syntax]    
            if not (_context.TypeCmp.Conforms(pattern_ty, expr_frag.Value.Type) ||
                    _context.TypeCmp.Conforms(expr_frag.Value.Type, pattern_ty))
            then
                _context.Diags.Error(
                    sprintf "'%O' and '%O' are not parts of the same inheritance chain. As a result this case is unreachable"
                            expr_frag.Value.Type.Name
                            pattern_ty.Name,
                    pattern.Span)
                pattern_error <- true
            else
            
            // if `i` = 0, we'll have `for j in 0 .. -1 do`
            // that will not perform a single iteration.
            for j in 0 .. (i - 1) do
                let prev_pattern = patterns.[j]
                if _context.ClassSymMap.ContainsKey(prev_pattern.Syntax)
                then
                    let prev_pattern_ty = _context.ClassSymMap.[prev_pattern.Syntax]
                    // Null conforms to Any and other non-primitive types,
                    // but we still allowed `case null => ...` to be the last branch.
                    if not (pattern_ty.Is(BasicClasses.Null)) &&
                       _context.TypeCmp.Conforms(ancestor=prev_pattern_ty, descendant=pattern_ty)
                    then
                        _context.Diags.Error(
                            sprintf "This case is shadowed by an earlier case at %O"
                                    (_context.Source.Map(prev_pattern.Span.First)),
                            pattern.Span)
                        pattern_error <- true
        
        let done_label = _context.LabelGen.Generate()
        let result_reg = _context.RegSet.Allocate("translate_match.result_reg")
        
        let pattern_var_index = _sym_table.Frame.VarsCount
        let block_frags =
            cases |> Array.map (fun case ->
                _sym_table.EnterBlock()
                
                let pattern_ty =
                    match case.Syntax.Pattern.Syntax with
                    | PatternSyntax.IdType (id, ty) ->
                        _sym_table.AddVar({ Symbol.Name = id.Syntax
                                            Type = ty.Syntax
                                            Index = pattern_var_index
                                            SyntaxSpan = case.Syntax.Pattern.Span
                                            Kind = SymbolKind.Var })
                        ty.Syntax
                    | PatternSyntax.Null ->
                        BasicClassNames.Null
                    
                let block_frag = this.TranslateBlock(case.Syntax.Block.Syntax.AsBlockSyntax)
                if block_frag.IsOk
                then
                    let pattern_asm_info = pattern_asm_infos.[pattern_ty]
                    asm.MatchCase(case.Span,
                                  pattern_asm_info.Label,
                                  pattern_ty,
                                  block_frag.Value,
                                  result_reg,
                                  done_label)
                       
                    _context.RegSet.Free(block_frag.Value.Reg)
                
                _sym_table.LeaveBlock()
                
                block_frag)
        
        asm.Label(done_label, "end match")
           .AsUnit()
        
        if pattern_error || (block_frags |> Seq.exists (fun it -> it.IsError))
        then
            _context.RegSet.Free(result_reg)
            Error
        else
            
        let block_types = block_frags |> Array.map (fun it -> it.Value.Type)
        
        Ok { AsmFragment.Asm = asm.ToString()
             Type = _context.TypeCmp.LeastUpperBound(block_types)
             Reg = result_reg }


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
            _context.Diags.Error(
                sprintf "Unary '%s' expects an operand of type '%O' but found '%O'"
                        op
                        expected_ty.Name
                        expr_frag.Value.Type.Name,
                expr.Span)
            
            _context.RegSet.Free(expr_frag.Value.Reg)
            Error
        else
            
        expr_frag
        
        
    and translate_infixop_int_operands (left: AstNode<ExprSyntax>)
                                       (right: AstNode<ExprSyntax>)
                                       (op: string)
                                       : Res<(AsmFragment * AsmFragment)> =
        let check_operands (left_frag: AsmFragment) (right_frag: AsmFragment): bool =
            if not (left_frag.Type.Is(BasicClasses.Int) &&
                    right_frag.Type.Is(BasicClasses.Int))
            then
                _context.Diags.Error(
                    sprintf "'%s' cannot be applied to operands of type '%O' and '%O'; only to 'Int' and 'Int'"
                            op
                            left_frag.Type.Name
                            right_frag.Type.Name,
                    left.Span)
                false
            else
                true

        translate_infixop_operands left right check_operands
        
        
    and translate_eqop_operands (left: AstNode<ExprSyntax>)
                                (right: AstNode<ExprSyntax>)
                                (op: string)
                                : Res<(AsmFragment * AsmFragment)> =
        let check_operands (left_frag: AsmFragment) (right_frag: AsmFragment): bool =
            if not (_context.TypeCmp.Conforms(left_frag.Type, right_frag.Type) ||
                    _context.TypeCmp.Conforms(right_frag.Type, left_frag.Type))
            then
                _context.Diags.Error(
                    sprintf "'%s' cannot be applied to operands of type '%O' and '%O'"
                            op
                            left_frag.Type.Name
                            right_frag.Type.Name,
                    left.Span)
                false
            else
                true

        translate_infixop_operands left right check_operands
        
        
    and translate_infixop_operands (left: AstNode<ExprSyntax>)
                                   (right: AstNode<ExprSyntax>)
                                   (check_operands: AsmFragment -> AsmFragment -> bool)
                                   : Res<(AsmFragment * AsmFragment)> =
        let left_frag = translate_expr left
        let right_frag = translate_expr right
        
        if left_frag.IsError || right_frag.IsError
        then
            if left_frag.IsOk then _context.RegSet.Free(left_frag.Value.Reg)
            if right_frag.IsOk then _context.RegSet.Free(right_frag.Value.Reg)
            Error
        else
            
        if not (check_operands left_frag.Value right_frag.Value)
        then
            _context.RegSet.Free(left_frag.Value.Reg)
            _context.RegSet.Free(right_frag.Value.Reg)
            Error
        else

        Ok (left_frag.Value, right_frag.Value)
    
    
    and translate_dispatch (dispatch_node: AstNode<ExprSyntax>)
                           (receiver: AstNode<ExprSyntax>)
                           (method_id: AstNode<ID>)
                           (actuals: AstNode<ExprSyntax>[])
                           : Res<AsmFragment> =
        let receiver_frag = translate_expr receiver
        if receiver_frag.IsError
        then
            Error
        else
        
        let receiver_frag = receiver_frag.Value
        
        if not (receiver_frag.Type.Methods.ContainsKey(method_id.Syntax))
        then
            _context.Diags.Error(
                sprintf "'%O' does not contain a definition for '%O'"
                        receiver_frag.Type.Name
                        method_id.Syntax,
                method_id.Span)
            
            _context.RegSet.Free(receiver_frag.Reg)
            Error
        else
            
        let method_sym = receiver_frag.Type.Methods.[method_id.Syntax]
        let method_name = sprintf "'%O.%O'" receiver_frag.Type.Name method_sym.Name
        
        let actuals_asm = translate_actuals method_name
                                             method_id.Span
                                             method_sym
                                             (*formal_name=*)"formal"
                                             receiver_frag.Reg
                                             actuals
        if actuals_asm.IsError
        then
            Error
        else
        
        let receiver_is_some_label = _context.LabelGen.Generate()
        
        let asm = this.EmitAsm().BeginDispatch(dispatch_node.Span)
        
        let method_reg = _context.RegSet.Allocate("translate_dispatch.method_reg")
        let result_reg = _context.RegSet.Allocate("translate_dispatch.result_reg")

        asm.CompleteDispatch(dispatch_node.Span,
                             receiver_frag,
                             receiver_is_some_label,
                             actuals_asm.Value,
                             method_reg,
                             method_sym,
                             actuals.Length,
                             result_reg)

        _context.RegSet.Free(method_reg)

        Ok { AsmFragment.Asm = asm.ToString()
             Type = _context.ClassSymMap.[method_sym.ReturnType]
             Reg = result_reg }
        
        
    and translate_super_dispatch (super_dispatch_node: AstNode<ExprSyntax>)
                                 (method_id: AstNode<ID>)
                                 (actuals: AstNode<ExprSyntax>[])
                                 : Res<AsmFragment> =
        let super_sym = _context.ClassSymMap.[_class_syntax.ExtendsSyntax.SUPER.Syntax] 
        if not (method_id.Syntax = ID ".ctor" || super_sym.Methods.ContainsKey(method_id.Syntax))
        then
            _context.Diags.Error(
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

        let method_sym = if method_id.Syntax = ID ".ctor"
                         then super_sym.Ctor
                         else super_sym.Methods.[method_id.Syntax]
        let method_name = sprintf "'%O.%O'" super_sym.Name method_sym.Name

        let actuals_asm = translate_actuals method_name
                                            method_id.Span
                                            method_sym
                                            (*formal_name=*)"formal"
                                            this_frag.Value.Reg
                                            actuals
        if actuals_asm.IsError
        then
            Error
        else
            
        let asm = this.EmitAsm()
                      .BeginSuperDispatch(super_dispatch_node.Span)

        let result_reg = _context.RegSet.Allocate("translate_super_dispatch.result_reg")

        asm.CompleteSuperDispatch(this_frag.Value,
                                  actuals_asm.Value,
                                  method_sym,
                                  result_reg,
                                  actuals.Length)

        Ok { AsmFragment.Asm = asm.ToString()
             Type = _context.ClassSymMap.[method_sym.ReturnType]
             Reg = result_reg }
        
        
    and translate_new (new_node: AstNode<ExprSyntax>)
                      (type_name: AstNode<TYPENAME>)
                      (actuals: AstNode<ExprSyntax>[])
                      : Res<AsmFragment> =
        if (check_typename type_name) = Error
        then
            Error
        else
            
        let ty = _context.ClassSymMap.[type_name.Syntax]
        
        if ty.Is(BasicClasses.Any) || ty.Is(BasicClasses.Int) ||
           ty.Is(BasicClasses.Unit) || ty.Is(BasicClasses.Boolean)
        then
            _context.Diags.Error(
                sprintf "'new %O' is not allowed" type_name.Syntax,
                type_name.Span)
            Error
        else

        let asm = this.EmitAsm()
                      .BeginNew(new_node.Span)
        
        let this_reg = _context.RegSet.Allocate("translate_new.this_reg")
        
        let this_frag =
            { AsmFragment.Asm = ""
              Reg = this_reg
              Type = ty }
        
        let method_sym = ty.Ctor
        let method_name = sprintf "Constructor of '%O'" ty.Name

        let actuals_asm =
            translate_actuals method_name
                              type_name.Span
                              method_sym
                              (*formal_name=*)"varformal"
                              this_frag.Reg
                              actuals
        if actuals_asm.IsError
        then
            Error
        else

        let result_reg = _context.RegSet.Allocate("translate_new.result_reg")
        
        asm.CompleteNew(ty, this_reg, actuals_asm.Value, actuals.Length, result_reg)

        Ok { AsmFragment.Asm = asm.ToString()
             Type = ty
             Reg = result_reg }

    
    and translate_actuals (method_name: string)
                          (method_id_span: Span)
                          (method_sym: MethodSymbol)
                          (formal_name: string)
                          (this_reg: Reg)
                          (actual_nodes: AstNode<ExprSyntax>[])
                          : Res<string> =
                              
        let asm = this.EmitAsm()
                      .BeginActuals(method_id_span, actual_nodes.Length, this_reg)
           
        _context.RegSet.Free(this_reg)
            
        let actual_frags = List<Res<AsmFragment>>()
        for actual_index = 0 to (actual_nodes.Length - 1) do
            let actual_frag = translate_expr actual_nodes.[actual_index]
            if actual_frag.IsOk
            then
                asm.Actual(actual_index, actual_frag.Value)                  
                _context.RegSet.Free(actual_frag.Value.Reg)

            actual_frags.Add(actual_frag)

        asm.LoadActualsIntoRegs(actual_frags.Count)        
            
        if actual_frags |> Seq.exists (fun it -> it.IsError)
        then
            Error
        else
        
        if method_sym.Formals.Length <> actual_frags.Count
        then
            _context.Diags.Error(
                sprintf "%s takes %d formal(s) but was passed %d actual(s)"
                        method_name
                        method_sym.Formals.Length
                        actual_frags.Count,
                method_id_span)

            Error
        else
            
        let mutable formal_actual_mismatch = false
        
        for i = 0 to method_sym.Formals.Length - 1 do
            let formal = method_sym.Formals.[i]
            let formal_ty = _context.ClassSymMap.[formal.Type]
            let actual = actual_frags.[i].Value
            if not (_context.TypeCmp.Conforms(ancestor=formal_ty, descendant=actual.Type))
            then
                formal_actual_mismatch <- true
                _context.Diags.Error(
                    sprintf "The actual's type '%O' does not conform to the %s's type '%O'"
                            actual.Type.Name
                            formal_name
                            formal_ty.Name,
                    actual_nodes.[i].Span)

        if formal_actual_mismatch
        then
            Error
        else
        
        Ok (asm.ToString())
        
        
    and translate_id id_node id =
        let sym = _sym_table.TryResolve(id)
        match sym with
        | ValueNone ->
            _context.Diags.Error(
                sprintf "The name '%O' does not exist in the current context" id,
                id_node.Span)
            Error
        | ValueSome sym ->
            let ty = _context.ClassSymMap.[sym.Type]
            
            let addr_frag = this.AddrOf(sym)
            let result_reg = _context.RegSet.Allocate("translate_id.result_reg")
            
            let asm =
                this.EmitAsm()
                    .Location(id_node.Span)
                    .In("movq    {0}, {1}", addr_frag, result_reg, comment=sym.Name.ToString())
                    .ToString()
                    
            _context.RegSet.Free(addr_frag.Reg)
            
            Ok { AsmFragment.Asm = asm
                 Type = ty
                 Reg = result_reg }
            
            
    and translate_int int_node int_syntax =
        let const_label = _context.IntConsts.GetOrAdd(int_syntax.Value)
        let reg = _context.RegSet.Allocate("translate_int.reg")
        let asm =
            this.EmitAsm()
                .Location(int_node.Span)
                .In("movq    ${0}, {1}", const_label, reg)
                .ToString()
        
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Int
             Reg = reg }
        
        
    and translate_str str_node str_syntax =
        let const_label = _context.StrConsts.GetOrAdd(str_syntax.Value)
        let reg = _context.RegSet.Allocate("translate_str.reg")
        let asm =
            this.EmitAsm()
                .Location(str_node.Span)
                .In("movq    ${0}, {1}", const_label, reg)
                .ToString()
        
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.String
             Reg = reg }
        
        
    and translate_bool bool_node bool_syntax =
        let const_label = if bool_syntax = BOOL.True
                          then RtNames.BoolTrue
                          else RtNames.BoolFalse
        let reg = _context.RegSet.Allocate("translate_bool.reg")
        let asm =
            this.EmitAsm()
                .Location(bool_node.Span)
                .In("movq    ${0}, {1}", const_label, reg)
                .ToString()
                          
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Boolean
             Reg = reg }
        
        
    and translate_this this_node =
        let sym = _sym_table.Resolve(ID "this")
        let ty = _context.ClassSymMap.[sym.Type]

        let addr_frag = this.AddrOf(sym)
        let result_reg = _context.RegSet.Allocate("translate_this.result_reg")
        
        let asm =
            this.EmitAsm()
                .Location(this_node.Span)
                .In("movq    {0}, {1}", addr_frag, result_reg)
                .ToString()
           
        _context.RegSet.Free(addr_frag.Reg)
        
        Ok { AsmFragment.Asm = asm
             Type = ty
             Reg = result_reg }
        
        
    and translate_null null_node =
        let result_reg = _context.RegSet.Allocate("translate_null.result_reg")
        let asm = this.EmitAsm()
                      .Location(null_node.Span)
                      .In("xorq    {0}, {1}", result_reg, result_reg)
                      .ToString()
        
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Null 
             Reg = result_reg }
        
        
    and translate_unit unit_node =
        let result_reg = _context.RegSet.Allocate("translate_unit.result_reg")
        let asm = this.EmitAsm()
                      .Location(unit_node.Span)
                      .In("movq    ${0}, {1}", RtNames.UnitValue, result_reg)
                      .ToString()
        
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Unit
             Reg = result_reg }
    
    
    and translate_var (var_node: AstNode<VarSyntax>): Res<AsmFragment> =
        if (check_typename var_node.Syntax.TYPE) = Error
        then
            Error
        else
            
        // We place a symbol for this variable in the symbol table
        // before translating the init expression.
        // As a result, the var will be visible to the init expression.
        // TODO: Does this correspond to Cool2020's operational semantics?
        _sym_table.AddVar(Symbol.Of(var_node, _sym_table.Frame.VarsCount))
        let assign_frag = translate_assign var_node.Span var_node.Syntax.ID var_node.Syntax.Expr
        if assign_frag.IsError
        then
            Error
        else
            
        _context.RegSet.Free(assign_frag.Value.Reg)
        
        // The var declaration is not an expression, so `Reg = Reg.Null` 
        Ok { assign_frag.Value with Reg = Reg.Null }
            
            
    and check_typename (ty_node: AstNode<TYPENAME>): Res<Unit> =
        // Make sure it's not a reference to a system class that is not allowed in user code.
        if _context.ClassSymMap.ContainsKey(ty_node.Syntax)
        then
            let class_sym = _context.ClassSymMap.[ty_node.Syntax]
            if class_sym.IsSpecial
            then
                _context.Diags.Error(
                    sprintf "The type name '%O' is not allowed in user code" ty_node.Syntax,
                    ty_node.Span)
                Error
            else
            
            Ok ()
        else

        // We could not find a symbol corresponding to `ty_node.Syntax`.
        _context.Diags.Error(
            sprintf "The type name '%O' could not be found (is an input file missing?)" ty_node.Syntax,
            ty_node.Span)
        Error

    
    and emit_cmpop (cmpop_node: AstNode<ExprSyntax>)
                   (left_frag: AsmFragment)
                   (right_frag: AsmFragment)
                   (jmp: string)
                   : struct {| Asm: string; Reg: Reg |} =
        
        let result_reg = _context.RegSet.Allocate("emit_cmpop.result_reg")
        let asm =
            emit_cmpop_with_branches cmpop_node.Span
                                     left_frag
                                     right_frag
                                     jmp
                                     (*false_branch*)(this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolFalse, result_reg))
                                     (*true_branch*)(this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolTrue, result_reg))
        {| Asm=asm; Reg=result_reg |}
    
    
    and emit_cmpop_with_branches (cmpop_span: Span)
                                 (left_frag: AsmFragment)
                                 (right_frag: AsmFragment)
                                 (jmp: string)
                                 (false_branch_asm: string)
                                 (true_branch_asm: string)
                                 : string =
        
        let asm =
            this.EmitAsm()
                .CmpOp(cmpop_span,
                       left_frag=left_frag,
                       right_frag=right_frag,
                       jmp=jmp,
                       false_branch_asm=false_branch_asm,
                       true_branch_asm=true_branch_asm)
                
        _context.RegSet.Free(left_frag.Reg)
        _context.RegSet.Free(right_frag.Reg)
        
        asm
    
    
    and emit_eqop_with_branches (eqop_span: Span)
                                (left_frag: AsmFragment)
                                (right_frag: AsmFragment)
                                (unequal_branch_asm: string)
                                (equal_branch_asm: string)
                                : string =

        let asm =
            this.EmitAsm()
                .EqOp(eqop_span,
                      left_frag=left_frag,
                      right_frag=right_frag,
                      unequal_branch_asm=unequal_branch_asm,
                      equal_branch_asm=equal_branch_asm)
            
        _context.RegSet.Free(left_frag.Reg)
        _context.RegSet.Free(right_frag.Reg)
        
        asm
    
    
    member this.TranslateBlock(block_syntax_opt: BlockSyntax voption): Res<AsmFragment> =
        match block_syntax_opt with
        | ValueNone ->
            let result_reg = _context.RegSet.Allocate("TranslateBlock.result_reg")
            let asm = this.EmitAsm().Single("movq    ${0}, {1}", RtNames.UnitValue, result_reg)
            
            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Unit
                 Reg = result_reg }
        | ValueSome block_syntax ->
            _sym_table.EnterBlock()
        
            let asm = this.EmitAsm()
            
            for stmt_node in block_syntax.Stmts do
                let stmt_frag = 
                    match stmt_node.Syntax with
                    | StmtSyntax.Var var_syntax ->
                        translate_var (stmt_node.Map(fun _ -> var_syntax))
                            
                    | StmtSyntax.Expr expr_syntax ->
                        translate_expr (stmt_node.Map(fun _ -> expr_syntax))
                
                if stmt_frag.IsOk
                then
                    _context.RegSet.Free(stmt_frag.Value.Reg)
                    asm.Paste(stmt_frag.Value.Asm)
                       .AsUnit()
            
            let expr_frag = translate_expr block_syntax.Expr
            
            _sym_table.LeaveBlock()
            
            if expr_frag.IsError
            then
                Error
            else
                
            asm.Paste(expr_frag.Value.Asm)
               .AsUnit()
            
            Ok { AsmFragment.Asm = asm.ToString()
                 Type = expr_frag.Value.Type
                 Reg = expr_frag.Value.Reg }
        
        
    member this.AddrOf(sym: Symbol) : AddrFragment =
        match sym.Kind with
        | SymbolKind.Formal ->
            let addr =
                if sym.Index <= SysVAmd64AbiFacts.ActualRegs.Length
                then
                    // ActualRegs.Length actuals are passed in regs,
                    // and then the callee stores them in its frame.
                    //
                    // The index 0 corresponds to -8(%rbp), to account for it we add 1 to `sym.Index`. 
                    this.EmitAsm().Addr("-{0}(%rbp)", (sym.Index + 1) * FrameLayoutFacts.ElemSize)
                else
                    // A caller passes actuals beyond ActualRegs.Length
                    // in the caller's own frame pushing the last actual first.
                    // These actuals are stored immediately before the return addr,
                    // that the `call` instruction pushes onto the stack.
                    //
                    // Assuming `SysVAmd64AbiFacts.ActualRegs.Length` = 6,
                    // The index 7 corresponds to (0 + FrameLayoutFacts.ActualsInCallerFrameOffset)(%rbp).
                    // To account for it, we subtract (SysVAmd64AbiFacts.ActualRegs.Length + 1) from `sym.Index`,
                    this.EmitAsm().Addr("{0}(%rbp)", FrameLayoutFacts.ActualsInCallerFrame +
                                                     (sym.Index - (SysVAmd64AbiFacts.ActualRegs.Length + 1)) *
                                                     FrameLayoutFacts.ElemSize)
            
            { Addr = addr
              Asm = ValueNone
              Type = _context.ClassSymMap.[sym.Type]
              Reg = Reg.Null }
                
        | SymbolKind.Var ->
            // The index 0 corresponds to -(8 + _sym_table.Frame.VarsOffset)(%rbp),
            // to account for it we add 1 to `sym.Index`. 
            { Addr = this.EmitAsm().Addr("-{0}(%rbp)", _sym_table.Frame.Vars +
                                                       (sym.Index + 1) *
                                                       FrameLayoutFacts.ElemSize)
              Asm = ValueNone
              Type = _context.ClassSymMap.[sym.Type]
              Reg = Reg.Null }
            
        | SymbolKind.Attr ->
            let this_reg = _context.RegSet.Allocate("AddrOf.this_reg")
            { Addr = this.EmitAsm()
                         .Addr("{0}({1})", ObjLayoutFacts.Attrs + (sym.Index * ObjLayoutFacts.ElemSize),
                                           this_reg)
              Asm = ValueSome (this.EmitAsm().Single("movq {0}(%rbp), {1}", FrameLayoutFacts.This, this_reg))
              Type = _context.ClassSymMap.[sym.Type]
              Reg = this_reg }
               
               
    member this.EmitAsm(): AsmBuilder = AsmBuilder(_context)


    member this.Translate(expr_node: AstNode<ExprSyntax>): Res<AsmFragment> =
        translate_expr expr_node
