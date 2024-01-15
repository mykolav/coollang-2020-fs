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


    let rec translateExpr (expr_node: AstNode<ExprSyntax>): LcResult<AsmFragment> =
        match expr_node.Syntax with
        | ExprSyntax.Assign (id, expr)                         -> translateAssign expr_node.Span id expr
        | ExprSyntax.BoolNegation expr                         -> translateBoolNegation expr_node expr
        | ExprSyntax.UnaryMinus expr                           -> translateUnaryMinus expr_node expr
        | ExprSyntax.If (condition,
                         then_branch,
                         else_branch)                          -> translateIf expr_node condition
                                                                                        then_branch
                                                                                        else_branch
        | ExprSyntax.While (condition, body)                   -> translateWhile expr_node condition body
        | ExprSyntax.LtEq (left, right)                        -> translateLtEq expr_node left right
        | ExprSyntax.GtEq (left, right)                        -> translateGtEq expr_node left right
        | ExprSyntax.Lt (left, right)                          -> translateLt expr_node left right
        | ExprSyntax.Gt (left, right)                          -> translateGt expr_node left right
        | ExprSyntax.EqEq (left, right)                        -> translateEqEq expr_node left right
        | ExprSyntax.NotEq (left, right)                       -> translateNotEq expr_node left right
        | ExprSyntax.Mul (left, right)                         -> translateMul expr_node left right
        | ExprSyntax.Div (left, right)                         -> translateDiv expr_node left right
        | ExprSyntax.Sum (left, right)                         -> translateSum expr_node left right
        | ExprSyntax.Sub (left, right)                         -> translateSub expr_node left right
        | ExprSyntax.Match (expr, cases_hd, cases_tl)          -> translateMatch expr_node expr cases_hd cases_tl
        | ExprSyntax.Dispatch (receiver, method_id, actuals)   -> translateDispatch expr_node receiver
                                                                                              method_id
                                                                                              actuals
        | ExprSyntax.ImplicitThisDispatch (method_id, actuals) -> translateDispatch expr_node
                                                                                     (AstNode.Virtual(ExprSyntax.This))
                                                                                     method_id
                                                                                     actuals
        | ExprSyntax.SuperDispatch (method_id, actuals)        -> translateSuperDispatch expr_node method_id actuals
        | ExprSyntax.New (type_name, actuals)                  -> translateNew expr_node type_name actuals
        | ExprSyntax.BracedBlock block                         -> this.TranslateBlock(block)
        | ExprSyntax.ParensExpr expr                           -> translateExpr expr
        | ExprSyntax.Id id                                     -> translateId expr_node id
        | ExprSyntax.Int int_syntax                            -> translateInt expr_node int_syntax
        | ExprSyntax.Str str_syntax                            -> translateStr expr_node str_syntax
        | ExprSyntax.Bool bool_syntax                          -> translateBool expr_node bool_syntax
        | ExprSyntax.This                                      -> translateThis expr_node
        | ExprSyntax.Null                                      -> translateNull expr_node
        | ExprSyntax.Unit                                      -> translateUnit expr_node


    and translateAssign (assign_node_span: Span)
                        (id: AstNode<ID>)
                        (rvalue_expr: AstNode<ExprSyntax>)
                        : LcResult<AsmFragment> =
        let expr_frag = translateExpr rvalue_expr
        match expr_frag with
        | Error ->
            Error
        | Ok expr_frag ->
            let id_sym_opt = _sym_table.TryResolve(id.Syntax)
            match id_sym_opt with
            | ValueNone ->
                _context.Diags.Error(
                    $"The name '{id.Syntax}' does not exist in the current context",
                    id.Span)

                _context.RegSet.Free(expr_frag.Reg)
                Error
            | ValueSome id_sym ->
                let addr_frag = this.AddrOf(id_sym)

                if not (_context.TypeCmp.Conforms(ancestor=addr_frag.Type, descendant=expr_frag.Type))
                then
                    _context.Diags.Error(
                        $"The expression's type '{expr_frag.Type.Name}' does not conform to " +
                        $"the type '{addr_frag.Type.Name}' of '{id.Syntax}'",
                        rvalue_expr.Span)

                    _context.RegSet.Free(addr_frag.Reg)
                    _context.RegSet.Free(expr_frag.Reg)
                    Error
                else

                let asm =
                    this.EmitAsm()
                        .Paste(expr_frag.Asm)
                        .Location(assign_node_span)
                        .Instr("movq    {0}, {1}", expr_frag.Reg, addr_frag, comment=id.Syntax.ToString())

                // If we are assigning a value to an attribute of an object
                // residing in the Generational GC's Old Area, this object keeps
                // the object pointed to by its attribute alive.
                if id_sym.Kind = SymbolKind.Attr &&
                   _context.CodeGenOptions.GC = GarbageCollectorKind.Generational
                then
                    asm.GenGCHandleAssign(addr_frag)
                       .AsUnit()

                _context.RegSet.Free(addr_frag.Reg)

                // We do not free up expr_frag.Value.Reg,
                // to support assignments of the form `ID = ID = ...`

                Ok { AsmFragment.Asm = asm.ToString()
                     Reg = expr_frag.Reg
                     Type = addr_frag.Type }


    and translateBoolNegation (bool_negation_node: AstNode<ExprSyntax>)
                              (negated_node: AstNode<ExprSyntax>)
                              : LcResult<AsmFragment> =
        let negated_frag = translateUnaryOpOperand negated_node (*op=*)"!" (*expected_ty=*)BasicClasses.Boolean
        match negated_frag with
        | Error           -> Error
        | Ok negated_frag ->
            Ok { negated_frag with
                   Asm = this.EmitAsm()
                             .BoolNegation(bool_negation_node.Span, negated_frag) }


    and translateUnaryMinus (unary_minus_node: AstNode<ExprSyntax>)
                            (negated_node: AstNode<ExprSyntax>)
                            : LcResult<AsmFragment> =
        match negated_node.Syntax with
        | ExprSyntax.Int int_syntax ->
            translateInt unary_minus_node (INT -int_syntax.Value)

        | _ ->
            let negated_frag = translateUnaryOpOperand negated_node
                                                       (*op=*)"-"
                                                       (*expected_ty=*)BasicClasses.Int
            match negated_frag with
            | Error           -> Error
            | Ok negated_frag ->
                Ok { negated_frag with
                       Asm = this.EmitAsm()
                                 .UnaryMinus(unary_minus_node.Span, negated_frag) }


    and translateIf (if_node: AstNode<ExprSyntax>)
                    (cond_node: AstNode<ExprSyntax>)
                    (then_branch: AstNode<ExprSyntax>)
                    (else_branch: AstNode<ExprSyntax>)
                    : LcResult<AsmFragment> =
        let then_frag = translateExpr then_branch
        let else_frag = translateExpr else_branch

        match then_frag, else_frag with
        | Error, Error ->
            Error
        | Ok then_frag, Error ->
            _context.RegSet.Free(then_frag.Reg)
            Error
        | Error, Ok else_frag ->
            _context.RegSet.Free(else_frag.Reg)
            Error
        | Ok then_frag, Ok else_frag ->
            let result_reg = _context.RegSet.Allocate("translate_if.result_reg")

            let then_asm =
                this.EmitAsm()
                    .Paste(then_frag.Asm)
                    .Instr("movq    {0}, {1}", then_frag.Reg, result_reg)
                    .ToString()

            let else_asm =
                this.EmitAsm()
                    .Paste(else_frag.Asm)
                    .Instr("movq    {0}, {1}", else_frag.Reg, result_reg)
                    .ToString()

            // It's OK to free up these registers early,
            // as the the condition's code cannot overwrite them
            // (it's located before the branches).
            _context.RegSet.Free(then_frag.Reg)
            _context.RegSet.Free(else_frag.Reg)

            let asm = emitCond {| Name="if"; Span=if_node.Span |} cond_node then_asm else_asm
            match asm with
            | Error  ->
                _context.RegSet.Free(result_reg)
                Error
            | Ok asm ->
                Ok { AsmFragment.Asm = asm
                     Type = _context.TypeCmp.LeastUpperBound(then_frag.Type, else_frag.Type)
                     Reg = result_reg }


    and translateWhile while_node cond_node body =
        let body_frag = translateExpr body
        match body_frag with
        | Error ->
            Error
        | Ok body_frag ->
            let while_cond_label = _context.LabelGen.Generate("WHILE_COND")

            let body_asm =
                this.EmitAsm()
                    .Paste(body_frag.Asm)
                    .Jmp(while_cond_label)
                    .ToString()

            _context.RegSet.Free(body_frag.Reg)

            let result_reg = _context.RegSet.Allocate("translate_while.result_reg")
            let done_asm = this.EmitAsm()
                               .Single("movq    ${0}, {1}", RtNames.UnitValue, result_reg)

            let cond_asm = emitCond {| Name="while"; Span=cond_node.Span |}
                                     cond_node
                                     (*true_branch_asm*)body_asm
                                     (*false_branch_asm*)done_asm
            match cond_asm with
            | Error ->
                _context.RegSet.Free(result_reg)
                Error
            | Ok cond_asm ->
                let asm = this.EmitAsm()
                              .Label(while_cond_label)
                              .Paste(cond_asm)
                              .Location(while_node.Span)
                              .ToString()

                Ok { AsmFragment.Asm = asm
                     Type = BasicClasses.Unit
                     Reg = result_reg }


    and emitCond (expr_info: struct {| Name: string; Span: Span |})
                 (cond_node: AstNode<ExprSyntax>)
                 (true_branch_asm: string)
                 (false_branch_asm: string)
                 : LcResult<string> =
        // We can end up with two conditional structures in the assembly code.
        // The first conditional computes the result of `x > y` and places that in a register.
        // The second conditional compares the register against zero
        // and jumps to the true or false branch of the expression.

        // To avoid two conditionals in the assembly, we plug `then` and `else` branches
        // directly into the conditional generated for `x > y`
        // and don't generate the second conditional at all.
        if ExprSyntax.isComparison cond_node.Syntax
        then
            let left, right, op, jmp =
                match cond_node.Syntax with
                | ExprSyntax.Lt (left, right)   -> left, right, "<",  "jl "
                | ExprSyntax.LtEq (left, right) -> left, right, "<=", "jle"
                | ExprSyntax.Gt (left, right)   -> left, right, ">",  "jg "
                | ExprSyntax.GtEq (left, right) -> left, right, ">=", "jge"
                | _                             -> invalidOp "Unreachable"
            let operands = translateInfixOpIntOperands left right op
            match operands with
            | Error ->
                Error
            | Ok (left_frag, right_frag) ->
                Ok (emitCmpOpWithBranches expr_info.Span
                                          left_frag
                                          right_frag
                                          jmp
                                          (*false_branch*)false_branch_asm
                                          (*true_branch*)true_branch_asm)
        else

        // To avoid two conditionals in the assembly, we plug `then` and `else` branches
        // directly into the conditional generated for `x == y`
        // and don't generate the second conditional at all.
        if ExprSyntax.isEquality cond_node.Syntax
        then
            let left, right, op, equal_branch, unequal_branch =
                match cond_node.Syntax with
                | ExprSyntax.EqEq (left, right)  -> left, right, "==", true_branch_asm, false_branch_asm
                | ExprSyntax.NotEq (left, right) -> left, right, "!=", false_branch_asm, true_branch_asm
                | _                              -> invalidOp "Unreachable"
            let operands = translateEqOpOperands left right op
            match operands with
            | Error ->
                Error
            | Ok (left_frag, right_frag) ->
                Ok (emitEqopWithBranches expr_info.Span
                                         left_frag
                                         right_frag
                                         unequal_branch
                                         equal_branch)
        else

        let cond_frag = translateExpr cond_node
        match cond_frag with
        | Error ->
            Error
        | Ok cond_frag ->
            // Free up the register right away, it's OK if it gets re-used in a branch.
            _context.RegSet.Free(cond_frag.Reg)

            if not (cond_frag.Type.Is(BasicClasses.Boolean))
            then
                _context.Diags.Error(
                    $"'%s{expr_info.Name}' expects a 'Boolean' condition but found '{cond_frag.Type.Name}'",
                    cond_node.Span)

                Error
            else

            // We have `(flag) ...` or `(is_satisfied()) ...` as our condition.
            // Instead of plugging `then` and `else` branches in an existing conditional,
            // generate the conditional ourselves.
            Ok (this.EmitAsm().Cond(cond_frag,
                                    true_branch_asm=true_branch_asm,
                                    false_branch_asm=false_branch_asm))


    and translateLtEq lt_eq_node left right =
        let operands = translateInfixOpIntOperands left right (*op=*)"<="
        match operands with
        | Error ->
            Error
        | Ok (left_frag, right_frag) ->
            let asm_frag = emitCmpOp lt_eq_node left_frag right_frag "jle "

            Ok { AsmFragment.Asm = asm_frag.Asm
                 Type = BasicClasses.Boolean
                 Reg = asm_frag.Reg }


    and translateGtEq gt_eq_node left right =
        let operands = translateInfixOpIntOperands left right (*op=*)">="
        match operands with
        | Error ->
            Error
        | Ok (left_frag, right_frag) ->
            let asm_frag = emitCmpOp gt_eq_node left_frag right_frag "jge "

            Ok { AsmFragment.Asm = asm_frag.Asm
                 Type = BasicClasses.Boolean
                 Reg = asm_frag.Reg }


    and translateLt lt_node left right =
        let operands = translateInfixOpIntOperands left right (*op=*)"<"
        match operands with
        | Error ->
            Error
        | Ok (left_frag, right_frag) ->
            let asm_frag = emitCmpOp lt_node left_frag right_frag "jl  "
            Ok { AsmFragment.Asm = asm_frag.Asm
                 Type = BasicClasses.Boolean
                 Reg = asm_frag.Reg }


    and translateGt gt_node left right =
        let operands = translateInfixOpIntOperands left right (*op=*)">"
        match operands with
        | Error ->
            Error
        | Ok (left_frag, right_frag) ->
            let asm_frag = emitCmpOp gt_node left_frag right_frag "jg  "
            Ok { AsmFragment.Asm = asm_frag.Asm
                 Type = BasicClasses.Boolean
                 Reg = asm_frag.Reg }


    and translateEqEq eq_eq_node left right =
        let operands = translateEqOpOperands left right "=="
        match operands with
        | Error ->
            Error
        | Ok (left_frag, right_frag) ->
            let result_reg = _context.RegSet.Allocate("translate_eq_eq.result_reg")
            let equal_branch = this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolTrue, result_reg, "true")
            let unequal_branch = this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolFalse, result_reg, "false")
            let asm = emitEqopWithBranches eq_eq_node.Span
                                           left_frag
                                           right_frag
                                           unequal_branch
                                           equal_branch

            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Boolean
                 Reg = result_reg }


    and translateNotEq not_eq_node left right =
        let operands = translateEqOpOperands left right "!="
        match operands with
        | Error ->
            Error
        | Ok (left_frag, right_frag) ->
            let result_reg = _context.RegSet.Allocate("translate_not_eq.result_reg")
            let unequal_branch = this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolTrue, result_reg, "true")
            let equal_branch = this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolFalse, result_reg, "false")
            let asm = emitEqopWithBranches not_eq_node.Span
                                           left_frag
                                           right_frag
                                           unequal_branch
                                           equal_branch

            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Boolean
                 Reg = result_reg }


    and translateMul mul_node left right =
        let operands = translateInfixOpIntOperands left right "*"
        match operands with
        | Error ->
            Error
        | Ok (left_frag, right_frag) ->
            let asm = this.EmitAsm().Mul(mul_node.Span, left_frag, right_frag)
            _context.RegSet.Free(right_frag.Reg)

            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Int
                 Reg = left_frag.Reg }


    and translateDiv div_node left right =
        let operands = translateInfixOpIntOperands left right "/"
        match operands with
        | Error ->
            Error
        | Ok (left_frag, right_frag) ->
            let asm = this.EmitAsm().Div(div_node.Span, left_frag, right_frag)
            _context.RegSet.Free(right_frag.Reg)

            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Int
                 Reg = left_frag.Reg }


    and translateSum sum_node left right =
        let checkOperands (left_frag: AsmFragment) (right_frag: AsmFragment): bool =
            if not ((left_frag.Type.Is(BasicClasses.Int) &&
                     right_frag.Type.Is(BasicClasses.Int)) ||
                    (left_frag.Type.Is(BasicClasses.String) &&
                     right_frag.Type.Is(BasicClasses.String)))
            then
                _context.Diags.Error(
                    $"'+' cannot be applied to operands of type '{left_frag.Type.Name}' and '{right_frag.Type.Name}'; " +
                    "only to 'Int' and 'Int' or 'String' and 'String'",
                    left.Span)
                false
            else
                true

        let operands = translateInfixOpOperands left right checkOperands
        match operands with
        | Error ->
            Error
        | Ok (left_frag, right_frag) ->
            // left = left + right
            let asm = this.EmitAsm().Sum(sum_node.Span, left_frag, right_frag)
            _context.RegSet.Free(right_frag.Reg)

            Ok { AsmFragment.Asm = asm.ToString()
                 Type = left_frag.Type
                 Reg = left_frag.Reg }


    and translateSub sub_node left right =
        let operands = translateInfixOpIntOperands left right "-"
        match operands with
        | Error ->
            Error
        | Ok (left_frag, right_frag) ->
            let asm = this.EmitAsm().Sub(sub_node.Span, left_frag, right_frag)
            _context.RegSet.Free(right_frag.Reg)

            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Int
                 Reg = left_frag.Reg }


    and translateMatch match_node expr cases_hd cases_tl =
        let expr_frag = translateExpr expr
        match expr_frag with
        | Error ->
            Error
        | Ok expr_frag ->
            let cases = Array.concat [[| cases_hd |]; cases_tl]
            let patterns = cases |> Array.map (fun case ->
                match case.Syntax.Pattern.Syntax with
                | PatternSyntax.IdType (_, ty) -> ty
                | PatternSyntax.Null -> AstNode.Virtual(BasicClassNames.Null))

            let pattern_asm_infos = Dictionary<TYPENAME, PatternAsmInfo>()
            for pattern in patterns do
                let pattern_ty = _context.ClassSymMap[pattern.Syntax]
                pattern_asm_infos.Add(
                    pattern.Syntax,
                    { PatternAsmInfo.Label = _context.LabelGen.Generate($"CASE_%s{pattern_ty.Name.Value}")
                      Tag = pattern_ty.Tag })

            let tag_reg = _context.RegSet.Allocate("translate_match.tag_reg")
            let expr_location = _context.Source.Map(expr.Span.First)

            let asm = this.EmitAsm()
                          .Match(match_node.Span,
                                 expr_frag,
                                 expr_location,
                                 _sym_table.Frame,
                                 tag_reg,
                                 pattern_asm_infos)

            _context.RegSet.Free(expr_frag.Reg)
            _context.RegSet.Free(tag_reg)

            let mutable pattern_error = false
            for i in 0 .. (patterns.Length - 1) do
                let pattern = patterns[i]
                if pattern.Syntax <> BasicClassNames.Null &&
                   (checkTypename pattern) = Error
                then
                    pattern_error <- true
                else

                let pattern_ty = _context.ClassSymMap[pattern.Syntax]
                if not (_context.TypeCmp.Conforms(pattern_ty, expr_frag.Type) ||
                        _context.TypeCmp.Conforms(expr_frag.Type, pattern_ty))
                then
                    _context.Diags.Error(
                        $"'{expr_frag.Type.Name}' and '{pattern_ty.Name}' are not parts of " +
                        "the same inheritance chain. As a result this case is unreachable",
                        pattern.Span)
                    pattern_error <- true
                else

                // if `i` = 0, we'll have `for j in 0 .. -1 do`
                // that will not perform a single iteration.
                for j in 0 .. (i - 1) do
                    let prev_pattern = patterns[j]
                    if _context.ClassSymMap.ContainsKey(prev_pattern.Syntax)
                    then
                        let prev_pattern_ty = _context.ClassSymMap[prev_pattern.Syntax]
                        // Null conforms to Any and other non-primitive types,
                        // but we still allowed `case null => ...` to be the last branch.
                        if not (pattern_ty.Is(BasicClasses.Null)) &&
                           _context.TypeCmp.Conforms(ancestor=prev_pattern_ty, descendant=pattern_ty)
                        then
                            _context.Diags.Error(
                                $"This case is shadowed by an earlier case at {_context.Source.Map(prev_pattern.Span.First)}",
                                pattern.Span)
                            pattern_error <- true

            let endmatch_label = _context.LabelGen.Generate("ENDMATCH")
            let result_reg = _context.RegSet.Allocate("translate_match.result_reg")

            let pattern_var_index = _sym_table.Frame.VarsCount
            let block_frags = cases |> Array.map (fun case ->
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

                let block_frag = this.TranslateCaseBlock(case.Syntax.Block.Syntax)
                match block_frag with
                | Error -> ()
                | Ok block_frag ->
                    let pattern_asm_info = pattern_asm_infos[pattern_ty]
                    asm.MatchCase(case.Span,
                                  pattern_asm_info.Label,
                                  block_frag,
                                  result_reg,
                                  endmatch_label)

                    _context.RegSet.Free(block_frag.Reg)

                _sym_table.LeaveBlock()

                block_frag)

            asm.Label(endmatch_label)
               .AsUnit()

            if pattern_error || (block_frags |> Seq.exists (fun it -> LcResult.isError it))
            then
                _context.RegSet.Free(result_reg)
                Error
            else

            let block_types = block_frags |> Array.map (fun it -> it.Value.Type)

            Ok { AsmFragment.Asm = asm.ToString()
                 Type = _context.TypeCmp.LeastUpperBound(block_types)
                 Reg = result_reg }


    and translateUnaryOpOperand (expr: AstNode<ExprSyntax>)
                                (op: string)
                                (expected_ty: ClassSymbol)
                                : LcResult<AsmFragment> =
        let expr_frag = translateExpr expr
        match expr_frag with
        | Error ->
            Error
        | Ok expr_frag ->
            if not (expr_frag.Type.Is(expected_ty))
            then
                _context.Diags.Error(
                    $"Unary '%s{op}' expects an operand of type '{expected_ty.Name}' but found '{expr_frag.Type.Name}'",
                    expr.Span)

                _context.RegSet.Free(expr_frag.Reg)
                Error
            else

            Ok expr_frag


    and translateInfixOpIntOperands (left: AstNode<ExprSyntax>)
                                    (right: AstNode<ExprSyntax>)
                                    (op: string)
                                    : LcResult<AsmFragment * AsmFragment> =
        let check_operands (left_frag: AsmFragment) (right_frag: AsmFragment): bool =
            if not (left_frag.Type.Is(BasicClasses.Int) &&
                    right_frag.Type.Is(BasicClasses.Int))
            then
                _context.Diags.Error(
                    $"'%s{op}' cannot be applied to operands of type '{left_frag.Type.Name}' " +
                    $"and '{right_frag.Type.Name}'; only to 'Int' and 'Int'",
                    left.Span)
                false
            else
                true

        translateInfixOpOperands left right check_operands


    and translateEqOpOperands (left: AstNode<ExprSyntax>)
                              (right: AstNode<ExprSyntax>)
                              (op: string)
                              : LcResult<AsmFragment * AsmFragment> =
        let checkOperands (left_frag: AsmFragment) (right_frag: AsmFragment): bool =
            if not (_context.TypeCmp.Conforms(left_frag.Type, right_frag.Type) ||
                    _context.TypeCmp.Conforms(right_frag.Type, left_frag.Type))
            then
                _context.Diags.Error(
                    $"'%s{op}' cannot be applied to operands of type '{left_frag.Type.Name}' and '{right_frag.Type.Name}'",
                    left.Span)
                false
            else
                true

        translateInfixOpOperands left right checkOperands


    and translateInfixOpOperands (left: AstNode<ExprSyntax>)
                                 (right: AstNode<ExprSyntax>)
                                 (check_operands: AsmFragment -> AsmFragment -> bool)
                                 : LcResult<AsmFragment * AsmFragment> =
        let left_frag = translateExpr left
        let right_frag = translateExpr right

        match left_frag, right_frag with
        | Error, Error ->
            Error
        | Ok left_frag, Error ->
            _context.RegSet.Free(left_frag.Reg)
            Error
        | Error, Ok right_frag ->
            _context.RegSet.Free(right_frag.Reg)
            Error
        | Ok left_frag, Ok right_frag ->
            if not (check_operands left_frag right_frag)
            then
                _context.RegSet.Free(left_frag.Reg)
                _context.RegSet.Free(right_frag.Reg)
                Error
            else

            Ok (left_frag, right_frag)


    and translateDispatch (dispatch_node: AstNode<ExprSyntax>)
                          (receiver: AstNode<ExprSyntax>)
                          (method_id: AstNode<ID>)
                          (actuals: AstNode<ExprSyntax>[])
                          : LcResult<AsmFragment> =
        let receiver_frag = translateExpr receiver
        match receiver_frag with
        | Error ->
            Error
        | Ok receiver_frag ->
            if not (receiver_frag.Type.Methods.ContainsKey(method_id.Syntax))
            then
                _context.Diags.Error(
                    $"'{receiver_frag.Type.Name}' does not contain a definition for '{method_id.Syntax}'",
                    method_id.Span)

                _context.RegSet.Free(receiver_frag.Reg)
                Error
            else

            let method_sym = receiver_frag.Type.Methods[method_id.Syntax]
            let method_name = $"'{receiver_frag.Type.Name}.{method_sym.Name}'"

            let actuals_asm = translateActuals method_name
                                               method_id.Span
                                               method_sym
                                               (*formal_name=*)"formal"
                                               receiver_frag.Reg
                                               actuals
            match actuals_asm with
            | Error ->
                Error
            | Ok actuals_asm ->
                let asm = this.EmitAsm().BeginDispatch(dispatch_node.Span)

                let method_reg = _context.RegSet.Allocate("translate_dispatch.method_reg")
                let result_reg = _context.RegSet.Allocate("translate_dispatch.result_reg")

                asm.CompleteDispatch(dispatch_node.Span,
                                     receiver_frag,
                                     actuals_asm,
                                     method_reg,
                                     method_sym,
                                     actuals.Length,
                                     result_reg)

                _context.RegSet.Free(method_reg)

                Ok { AsmFragment.Asm = asm.ToString()
                     Type = _context.ClassSymMap[method_sym.ReturnType]
                     Reg = result_reg }


    and translateSuperDispatch (super_dispatch_node: AstNode<ExprSyntax>)
                               (method_id: AstNode<ID>)
                               (actuals: AstNode<ExprSyntax>[])
                               : LcResult<AsmFragment> =
        let super_sym = _context.ClassSymMap[_class_syntax.ExtendsSyntax.SUPER.Syntax]
        if not (method_id.Syntax = ID ".ctor" || super_sym.Methods.ContainsKey(method_id.Syntax))
        then
            _context.Diags.Error(
                $"'{super_sym.Name}' does not contain a definition for '{method_id.Syntax}'",
                method_id.Span)
            Error
        else

        let this_frag = translateExpr (AstNode.Virtual(ExprSyntax.This))
        match this_frag with
        | Error ->
            Error
        | Ok this_frag ->

            let method_sym = if method_id.Syntax = ID ".ctor"
                             then super_sym.Ctor
                             else super_sym.Methods[method_id.Syntax]
            let method_name = $"'{super_sym.Name}.{method_sym.Name}'"

            let actuals_asm = translateActuals method_name
                                               method_id.Span
                                               method_sym
                                               (*formal_name=*)"formal"
                                               this_frag.Reg
                                               actuals
            match actuals_asm with
            | Error ->
                Error
            | Ok actuals_asm ->
                let asm = this.EmitAsm()
                              .BeginSuperDispatch(super_dispatch_node.Span)

                let result_reg = _context.RegSet.Allocate("translate_super_dispatch.result_reg")

                asm.CompleteSuperDispatch(super_dispatch_node.Span,
                                          this_frag,
                                          actuals_asm,
                                          method_sym,
                                          result_reg,
                                          actuals.Length)

                Ok { AsmFragment.Asm = asm.ToString()
                     Type = _context.ClassSymMap[method_sym.ReturnType]
                     Reg = result_reg }


    and translateNew (new_node: AstNode<ExprSyntax>)
                     (type_name: AstNode<TYPENAME>)
                     (actuals: AstNode<ExprSyntax>[])
                     : LcResult<AsmFragment> =
        if (checkTypename type_name) = Error
        then
            Error
        else

        let ty = _context.ClassSymMap[type_name.Syntax]

        if ty.Is(BasicClasses.Any) || ty.Is(BasicClasses.Int) ||
           ty.Is(BasicClasses.Unit) || ty.Is(BasicClasses.Boolean) ||
           ty.Is(BasicClasses.String)
        then
            _context.Diags.Error(
                $"'new {type_name.Syntax}' is not allowed",
                type_name.Span)
            Error
        else

        let actuals_asm =
            translateCtorActuals type_name
                                 ty
                                 actuals
        match actuals_asm with
        | Error ->
            Error
        | Ok actuals_asm ->
            let asm = this.EmitAsm()
                          .BeginNew(new_node.Span)

            let result_reg = _context.RegSet.Allocate("translate_new.result_reg")

            asm.CompleteNew(ty, actuals_asm, actuals.Length, result_reg)

            Ok { AsmFragment.Asm = asm.ToString()
                 Type = ty
                 Reg = result_reg }


    and translateCtorActuals (type_name: AstNode<TYPENAME>)
                             (ty: ClassSymbol)
                             (actual_nodes: AstNode<ExprSyntax>[])
                             : LcResult<string> =

        let asm, actual_frags = beginTranslateActuals type_name.Span
                                                      (*this_reg=*)Reg.Null
                                                      actual_nodes

        if ty.Is(BasicClasses.ArrayAny)
        then
            // Pass 0 as `this` pointer.
            // As the size of array is passed to the ctor of 'ArrayAny',
            // it doesn't use an object copied from a prototype.
            // Instead it will allocate memory and create an 'ArrayAny' object there itself.
            asm.Comment("ArrayAny..ctor does not take a this pointer as it allocates memory itself")
               .Instr("movq    $0, 0(%rsp)", comment=None)
               .AsUnit()
        else
            // Copy the relevant prototype and pass a pointer to the copy as `this` pointer.
            asm.RtCopyObject(proto=($"${ty.Name}_PROTO_OBJ"))
               .Instr("movq    %rax, 0(%rsp)", comment=Some "actual #0: this")
               .AsUnit()

        asm.LoadActualsIntoRegs(actual_frags.Count)

        let check_res = checkActuals (*method_name=*)    $"Constructor of '{ty.Name}'"
                                     (*method_id_span=*) type_name.Span
                                     (*method_sym=*)     ty.Ctor
                                     (*formal_name=*)    "varformal"
                                     actual_nodes
                                     actual_frags
        match check_res with
        | Error ->
            Error
        | Ok () ->
            Ok (asm.ToString())


    and translateActuals (method_name: string)
                         (method_id_span: Span)
                         (method_sym: MethodSymbol)
                         (formal_name: string)
                         (this_reg: Reg)
                         (actual_nodes: AstNode<ExprSyntax>[])
                         : LcResult<string> =

        let asm, actual_frags = beginTranslateActuals method_id_span this_reg actual_nodes

        asm.LoadActualsIntoRegs(actual_frags.Count)

        let check_res = checkActuals method_name
                                     method_id_span
                                     method_sym
                                     formal_name
                                     actual_nodes
                                     actual_frags
        match check_res with
        | Error ->
            Error
        | Ok () ->
            Ok (asm.ToString())


    and beginTranslateActuals (method_id_span: Span)
                              (this_reg: Reg)
                              (actual_nodes: AstNode<ExprSyntax>[])
                              : AsmBuilder * List<LcResult<AsmFragment>> =

        let asm = this.EmitAsm()
                      .BeginActuals(method_id_span, actual_nodes.Length, this_reg)

        _context.RegSet.Free(this_reg)

        let actual_frags = List<LcResult<AsmFragment>>()
        for actual_index = 0 to (actual_nodes.Length - 1) do
            let actual_frag = translateExpr actual_nodes[actual_index]
            match actual_frag with
            | Error -> ()
            | Ok actual_frag ->
                asm.Actual(actual_index, actual_frag)
                _context.RegSet.Free(actual_frag.Reg)

            actual_frags.Add(actual_frag)

        asm, actual_frags


    and checkActuals (method_name: string)
                     (method_id_span: Span)
                     (method_sym: MethodSymbol)
                     (formal_name: string)
                     (actual_nodes: AstNode<ExprSyntax>[])
                     (actual_frags: List<LcResult<AsmFragment>>)
                     : LcResult<Unit> =

        if actual_frags |> Seq.exists (fun it -> LcResult.isError it)
        then
            Error
        else

        if method_sym.Formals.Length <> actual_frags.Count
        then
            _context.Diags.Error(
                $"%s{method_name} takes %d{method_sym.Formals.Length} formal(s) " +
                $"but was passed %d{actual_frags.Count} actual(s)",
                method_id_span)

            Error
        else

        let mutable formal_actual_mismatch = false

        for i = 0 to method_sym.Formals.Length - 1 do
            let formal = method_sym.Formals[i]
            let formal_ty = _context.ClassSymMap[formal.Type]
            let actual = actual_frags[i].Value
            if not (_context.TypeCmp.Conforms(ancestor=formal_ty, descendant=actual.Type))
            then
                formal_actual_mismatch <- true
                _context.Diags.Error(
                    $"The actual's type '{actual.Type.Name}' does not conform to " +
                    $"the %s{formal_name}'s type '{formal_ty.Name}'",
                    actual_nodes[i].Span)

        if formal_actual_mismatch
        then
            Error
        else

        Ok ()


    and translateId id_node id =
        let sym = _sym_table.TryResolve(id)
        match sym with
        | ValueNone ->
            _context.Diags.Error(
                $"The name '{id}' does not exist in the current context",
                id_node.Span)
            Error
        | ValueSome sym ->
            let ty = _context.ClassSymMap[sym.Type]

            let addr_frag = this.AddrOf(sym)
            let result_reg = _context.RegSet.Allocate("translate_id.result_reg")

            let asm =
                this.EmitAsm()
                    .Location(id_node.Span)
                    .Instr("movq    {0}, {1}", addr_frag, result_reg, comment=sym.Name.ToString())
                    .ToString()

            _context.RegSet.Free(addr_frag.Reg)

            Ok { AsmFragment.Asm = asm
                 Type = ty
                 Reg = result_reg }


    and translateInt int_node int_syntax =
        let const_label = _context.IntConsts.GetOrAdd(int_syntax.Value)
        let reg = _context.RegSet.Allocate("translate_int.reg")
        let asm =
            this.EmitAsm()
                .Location(int_node.Span)
                .Instr("movq    ${0}, {1}", const_label, reg)
                .ToString()

        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Int
             Reg = reg }


    and translateStr str_node str_syntax =
        let const_label = _context.StrConsts.GetOrAdd(str_syntax.Value)
        let reg = _context.RegSet.Allocate("translate_str.reg")
        let asm =
            this.EmitAsm()
                .Location(str_node.Span)
                .Instr("movq    ${0}, {1}", const_label, reg)
                .ToString()

        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.String
             Reg = reg }


    and translateBool bool_node bool_syntax =
        let const_label = if bool_syntax = BOOL.True
                          then RtNames.BoolTrue
                          else RtNames.BoolFalse
        let reg = _context.RegSet.Allocate("translate_bool.reg")
        let asm =
            this.EmitAsm()
                .Location(bool_node.Span)
                .Instr("movq    ${0}, {1}", const_label, reg)
                .ToString()

        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Boolean
             Reg = reg }


    and translateThis this_node =
        let sym = _sym_table.Resolve(ID "this")
        let ty = _context.ClassSymMap[sym.Type]

        let addr_frag = this.AddrOf(sym)
        let result_reg = _context.RegSet.Allocate("translate_this.result_reg")

        let asm =
            this.EmitAsm()
                .Location(this_node.Span)
                .Instr("movq    {0}, {1}", addr_frag, result_reg)
                .ToString()

        _context.RegSet.Free(addr_frag.Reg)

        Ok { AsmFragment.Asm = asm
             Type = ty
             Reg = result_reg }


    and translateNull null_node =
        let result_reg = _context.RegSet.Allocate("translate_null.result_reg")
        let asm = this.EmitAsm()
                      .Location(null_node.Span)
                      .Instr("xorq    {0}, {1}", result_reg, result_reg)
                      .ToString()

        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Null
             Reg = result_reg }


    and translateUnit unit_node =
        let result_reg = _context.RegSet.Allocate("translate_unit.result_reg")
        let asm = this.EmitAsm()
                      .Location(unit_node.Span)
                      .Instr("movq    ${0}, {1}", RtNames.UnitValue, result_reg)
                      .ToString()

        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Unit
             Reg = result_reg }


    and translateVar (var_node: AstNode<VarSyntax>): LcResult<AsmFragment> =
        if (checkTypename var_node.Syntax.TYPE) = Error
        then
            Error
        else

        // We place a symbol for this variable in the symbol table
        // before translating the init expression.
        // As a result, the var will be visible to the init expression.
        // TODO: Does this correspond to Cool2020's operational semantics?
        _sym_table.AddVar(Symbol.Of(var_node, _sym_table.Frame.VarsCount))
        let assign_frag = translateAssign var_node.Span var_node.Syntax.ID var_node.Syntax.Expr
        match assign_frag with
        | Error ->
            Error
        | Ok assign_frag ->
            _context.RegSet.Free(assign_frag.Reg)

            // The var declaration is not an expression, so `Reg = Reg.Null`
            Ok { assign_frag with Reg = Reg.Null }


    and checkTypename (ty_node: AstNode<TYPENAME>): LcResult<Unit> =
        // Make sure it's not a reference to a system class that is not allowed in user code.
        if _context.ClassSymMap.ContainsKey(ty_node.Syntax)
        then
            let class_sym = _context.ClassSymMap[ty_node.Syntax]
            if class_sym.IsSpecial
            then
                _context.Diags.Error(
                    $"The type name '{ty_node.Syntax}' is not allowed in user code",
                    ty_node.Span)
                Error
            else

            Ok ()
        else

        // We could not find a symbol corresponding to `ty_node.Syntax`.
        _context.Diags.Error(
            $"The type name '{ty_node.Syntax}' could not be found (is an input file missing?)",
            ty_node.Span)
        Error


    and emitCmpOp (cmpop_node: AstNode<ExprSyntax>)
                  (left_frag: AsmFragment)
                  (right_frag: AsmFragment)
                  (jmp: string)
                  : struct {| Asm: string; Reg: Reg |} =

        let result_reg = _context.RegSet.Allocate("emit_cmpop.result_reg")
        let asm =
            emitCmpOpWithBranches cmpop_node.Span
                                     left_frag
                                     right_frag
                                     jmp
                                     (*false_branch*)(this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolFalse, result_reg))
                                     (*true_branch*)(this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolTrue, result_reg))
        {| Asm=asm; Reg=result_reg |}


    and emitCmpOpWithBranches (cmpop_span: Span)
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


    and emitEqopWithBranches (eqop_span: Span)
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

    member this.TranslateCaseBlock(block_syntax: CaseBlockSyntax): LcResult<AsmFragment> =
        match block_syntax with
        | CaseBlockSyntax.Free block_syntax -> this.TranslateBlock(block_syntax)
        | CaseBlockSyntax.Braced block_syntax_opt -> this.TranslateBlock(block_syntax_opt)


    member this.TranslateBlock(block_syntax_opt: BlockSyntax voption): LcResult<AsmFragment> =
        match block_syntax_opt with
        | ValueNone ->
            let result_reg = _context.RegSet.Allocate("TranslateBlock.result_reg")
            let asm = this.EmitAsm().Single("movq    ${0}, {1}", RtNames.UnitValue, result_reg)

            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Unit
                 Reg = result_reg }
        | ValueSome block_syntax ->
            this.TranslateBlock(block_syntax)


    member this.TranslateBlock(block_syntax: BlockSyntax): LcResult<AsmFragment> =
        _sym_table.EnterBlock()

        let asm = this.EmitAsm()

        for stmt_node in block_syntax.Stmts do
            let stmt_frag =
                match stmt_node.Syntax with
                | StmtSyntax.Var var_syntax ->
                    translateVar (stmt_node.Map(fun _ -> var_syntax))

                | StmtSyntax.Expr expr_syntax ->
                    translateExpr (stmt_node.Map(fun _ -> expr_syntax))

            match stmt_frag with
            | Error -> ()
            | Ok stmt_frag ->
                _context.RegSet.Free(stmt_frag.Reg)
                asm.Paste(stmt_frag.Asm).AsUnit()

        let expr_frag = translateExpr block_syntax.Expr

        _sym_table.LeaveBlock()

        match expr_frag with
        | Error ->
            Error
        | Ok expr_frag ->
            Ok { AsmFragment.Asm = asm.Paste(expr_frag.Asm)
                                      .ToString()
                 Type = expr_frag.Type
                 Reg = expr_frag.Reg }


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
              Type = _context.ClassSymMap[sym.Type]
              Reg = Reg.Null }

        | SymbolKind.Var ->
            // The index 0 corresponds to -(8 + _sym_table.Frame.VarsOffset)(%rbp),
            // to account for it we add 1 to `sym.Index`.
            { Addr = this.EmitAsm().Addr("-{0}(%rbp)", _sym_table.Frame.Vars +
                                                       (sym.Index + 1) *
                                                       FrameLayoutFacts.ElemSize)
              Asm = ValueNone
              Type = _context.ClassSymMap[sym.Type]
              Reg = Reg.Null }

        | SymbolKind.Attr ->
            let this_reg = _context.RegSet.Allocate("AddrOf.this_reg")
            { Addr = this.EmitAsm()
                         .Addr("{0}({1})", ObjLayoutFacts.Attrs + (sym.Index * ObjLayoutFacts.ElemSize),
                                           this_reg)
              Asm = ValueSome (this.EmitAsm().Single("movq    {0}(%rbp), {1}", FrameLayoutFacts.This, this_reg))
              Type = _context.ClassSymMap[sym.Type]
              Reg = this_reg }


    member this.EmitAsm(): AsmBuilder = AsmBuilder(_context)


    member this.Translate(expr_node: AstNode<ExprSyntax>): LcResult<AsmFragment> =
        translateExpr expr_node


    member this.Translate(method_body_node: AstNode<MethodBodySyntax>): LcResult<AsmFragment> =
        match method_body_node.Syntax with
        | MethodBodySyntax.Native -> invalidOp "MethodBodySyntax.AsExprSyntax"
        | MethodBodySyntax.Expr it -> this.Translate(method_body_node.Map(fun _ -> it))


module private ExprSyntax =
    let isComparison (expr_syntax: ExprSyntax): bool =
        match expr_syntax with
        | ExprSyntax.Lt _   -> true
        | ExprSyntax.LtEq _ -> true
        | ExprSyntax.Gt _   -> true
        | ExprSyntax.GtEq _ -> true
        | _                 -> false

    let isEquality (expr_syntax: ExprSyntax): bool =
        match expr_syntax with
        | ExprSyntax.EqEq _  -> true
        | ExprSyntax.NotEq _ -> true
        | _                  -> false
