namespace rec LibCool.TranslatorParts


open System
open System.Collections.Generic
open System.Text
open LibCool.SharedParts
open LibCool.SourceParts
open LibCool.AstParts
open LibCool.SemanticParts
open LibCool.SemanticParts.AstExtensions
open LibCool.TranslatorParts


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
                .Location(assign_node_span.First)
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
        if negated_frag.IsError
        then
            Error

        else

        let negated_frag = negated_frag.Value
        
        let false_label = _context.LabelGen.Generate()
        let done_label = _context.LabelGen.Generate()
        
        let asm =
            this.EmitAsm()
                .Location(bool_negation_node.Span.First)
                .Paste(negated_frag.Asm)
                .In("cmpq    $0, {0}({1})", ObjLayoutFacts.BoolValue, negated_frag.Reg)
                .Je(false_label, "false")
                .Comment("true:")
                .In("movq    ${0}, {1}", RtNames.BoolFalse, negated_frag.Reg)
                .Jmp(done_label, "done")
                .Label(false_label, "false")
                .In("movq    ${0}, {1}", RtNames.BoolTrue, negated_frag.Reg)
                .Label(done_label, "done")
                .ToString()
                
        Ok { negated_frag with Asm = asm }
        
        
    and translate_unary_minus (unary_minus_node: AstNode<ExprSyntax>)
                              (negated_node: AstNode<ExprSyntax>)
                              : Res<AsmFragment> =
        let negated_frag = translate_unaryop_operand negated_node (*op=*)"-" (*expected_ty=*)BasicClasses.Int
        if negated_frag.IsError
        then
            Error
        else
        
        let negated_frag = negated_frag.Value
        let asm =
            this.EmitAsm()
                .Location(unary_minus_node.Span.First)
                .Paste(negated_frag.Asm)
                .RtCopyObject(proto_reg=negated_frag.Reg, copy_reg=negated_frag.Reg)
                .In("negq    {0}({1})", ObjLayoutFacts.IntValue, negated_frag.Reg)
                .ToString()

        Ok { negated_frag with Asm = asm }
        
        
    and translate_if (if_node: AstNode<ExprSyntax>)
                     (condition: AstNode<ExprSyntax>)
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

        let result_reg = _context.RegSet.Allocate()
        
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
            
        if condition.Syntax.IsComparison
        then
            let left, right, op, jmp =
                match condition.Syntax with
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
            let asm = emit_cmpop_with_branches left_frag
                                               right_frag
                                               jmp
                                               (*false_branch*)else_asm
                                               (*true_branch*)then_asm

            Ok { AsmFragment.Asm = asm
                 Type = _context.TypeCmp.LeastUpperBound(then_frag.Value.Type, else_frag.Value.Type)
                 Reg = result_reg }
        else
            
        if condition.Syntax.IsEquality
        then
            let left, right, op, equal_branch, unequal_branch =
                match condition.Syntax with
                | ExprSyntax.EqEq (left, right)  -> left, right, "==", then_asm, else_asm
                | ExprSyntax.NotEq (left, right) -> left, right, "!=", else_asm, then_asm
                | _                              -> invalidOp "Unreachable"
            let operands = translate_eqop_operands left right op
            if operands.IsError
            then
                Error
            else
                
            let left_frag, right_frag = operands.Value
            let asm = 
                emit_eqop_with_branches left_frag
                                        right_frag
                                        unequal_branch
                                        equal_branch

            Ok { AsmFragment.Asm = asm
                 Type = _context.TypeCmp.LeastUpperBound(then_frag.Value.Type, else_frag.Value.Type)
                 Reg = result_reg }
        else
            
        let condition_frag = translate_expr condition
        if condition_frag.IsError
        then
            Error
        else
            
        if not (condition_frag.Value.Type.Is(BasicClasses.Boolean))
        then
            _context.Diags.Error(
                sprintf "'if' expects a 'Boolean' condition but found '%O'"
                        condition_frag.Value.Type.Name,
                condition.Span)
            
            _context.RegSet.Free(condition_frag.Value.Reg)
            Error
        else
        
        let else_label = _context.LabelGen.Generate()
        let done_label = _context.LabelGen.Generate()

        let asm =
            this.EmitAsm()
                .Location(if_node.Span.First)
                .Paste(condition_frag.Value.Asm)
                .In("cmpq    $0, {0}({1})", ObjLayoutFacts.BoolValue, condition_frag.Value.Reg)
                .Je(else_label, "else")
                .Comment("then")
                .Paste(then_asm)
                .Jmp(done_label, "end if")
                .Label(else_label, "else")
                .Paste(else_asm)
                .Label(done_label, "end if")
                .ToString()
        
        _context.RegSet.Free(condition_frag.Value.Reg)
        
        Ok { AsmFragment.Asm = asm
             Type = _context.TypeCmp.LeastUpperBound(then_frag.Value.Type, else_frag.Value.Type)
             Reg = result_reg }
        
        
    and translate_while while_node condition body =
        let condition_frag = translate_expr condition
        if condition_frag.IsError
        then
            Error
        else

        // Free up the register right away, it's OK if the body re-uses it.
        _context.RegSet.Free(condition_frag.Value.Reg)
            
        if not (condition_frag.Value.Type.Is(BasicClasses.Boolean))
        then
            _context.Diags.Error(
                sprintf "'while' expects a 'Boolean' condition but found '%O'"
                        condition_frag.Value.Type.Name,
                condition.Span)

            Error
        else
        
        let body_frag = translate_expr body
        if body_frag.IsError
        then
            Error
        else
        
        // Free up the register right away, it's OK if the condition uses the same register.
        _context.RegSet.Free(body_frag.Value.Reg)

        let result_reg = _context.RegSet.Allocate()
        let while_cond_label = _context.LabelGen.Generate()
        let done_label = _context.LabelGen.Generate()

        let asm =
            this.EmitAsm()
                .Location(while_node.Span.First)
                .Label(while_cond_label, "while cond")
                .Paste(condition_frag.Value.Asm)
                .In("cmpq    $0, {0}({1})", ObjLayoutFacts.BoolValue, condition_frag.Value.Reg)
                .Je(done_label, "end while")
                .Paste(body_frag.Value.Asm)
                .Jmp(while_cond_label, "while cond")
                .Label(done_label, "end while")
                .In("movq    ${0}, {1}", RtNames.UnitValue, result_reg, "unit")
                .ToString()
            
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Unit
             Reg = result_reg }
        
        
    and translate_lt_eq lt_eq_node left right =
        let operands = translate_infixop_int_operands left right (*op=*)"<="
        if operands.IsError
        then
            Error
        else
        
        let left_frag, right_frag = operands.Value
        let asm_frag = emit_cmpop left_frag right_frag "jle"
        
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
        let asm_frag = emit_cmpop left_frag right_frag "jge"
        
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
        let asm_frag = emit_cmpop left_frag right_frag "jl"
        
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
        let asm_frag = emit_cmpop left_frag right_frag "jg"
        
        Ok { AsmFragment.Asm = asm_frag.Asm
             Type = BasicClasses.Boolean
             Reg = asm_frag.Reg }
        
        
    and translate_eq_eq eq_eq_node left right =
        let operands = translate_eqop_operands left right "=="
        if operands.IsError
        then
            Error
        else
            
        let result_reg = _context.RegSet.Allocate()
        let equal_branch = this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolTrue, result_reg, "true")
        let unequal_branch = this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolFalse, result_reg, "false")
        let left_frag, right_frag = operands.Value
        let asm = 
            emit_eqop_with_branches left_frag
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
            
        let result_reg = _context.RegSet.Allocate()
        let unequal_branch = this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolTrue, result_reg, "true")
        let equal_branch = this.EmitAsm().Single("movq    ${0}, {1}", RtNames.BoolFalse, result_reg, "false")
        let left_frag, right_frag = operands.Value
        let asm = 
            emit_eqop_with_branches left_frag
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

        let asm =
            this.EmitAsm()
                .Location(mul_node.Span.First)
                .Paste(left_frag.Asm)
                .RtCopyObject(proto_reg=left_frag.Reg, copy_reg=left_frag.Reg)
                .Paste(right_frag.Asm)
                .In("movq    {0}({1}), %rax", ObjLayoutFacts.IntValue, left_frag.Reg)
                .In("imulq   {0}({1})", ObjLayoutFacts.IntValue, right_frag.Reg)
                .In("movq    %rax, {0}({1})", ObjLayoutFacts.IntValue, left_frag.Reg)
                .ToString()

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

        let asm =
            this.EmitAsm()
                .Location(div_node.Span.First)
                .Paste(left_frag.Asm)
                .RtCopyObject(proto_reg=left_frag.Reg, copy_reg=left_frag.Reg)
                .Paste(right_frag.Asm)
                // left / right
                .In("movq    {0}({1}), %rax", ObjLayoutFacts.IntValue, left_frag.Reg)
                .In("cqto", comment=Some "sign-extend %rax to %rdx:%rax")
                .In("idivq    {0}({1})", ObjLayoutFacts.IntValue, right_frag.Reg)
                .In("movq    %rax, {0}({1})", ObjLayoutFacts.IntValue, left_frag.Reg)
                .ToString()

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
  
        let asm =
            this.EmitAsm()
                .Location(sum_node.Span.First)
                .Paste(left_frag.Asm)
                .Paste(right_frag.Asm)
        
        if left_frag.Type.Is(BasicClasses.Int) &&
           right_frag.Type.Is(BasicClasses.Int)
        then
            asm.RtCopyObject(proto_reg=right_frag.Reg, copy_reg=right_frag.Reg)
               .In("movq    {0}({1}), {2}", ObjLayoutFacts.IntValue, left_frag.Reg, left_frag.Reg)
               .In("addq    {0}, {1}({2})", left_frag.Reg, ObjLayoutFacts.IntValue, right_frag.Reg)
               .AsUnit()
        else // string concatenation
            asm.StringConcat(left_frag.Reg, right_frag.Reg, result_reg=right_frag.Reg)
               .AsUnit()

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

        let asm =
            this.EmitAsm()
                .Location(sub_node.Span.First)
                .Paste(left_frag.Asm)
                .Paste(right_frag.Asm)
                // left - right
                .RtCopyObject(proto_reg=left_frag.Reg, copy_reg=left_frag.Reg)
                .In("movq    {0}({1}), {2}", ObjLayoutFacts.IntValue, right_frag.Reg, right_frag.Reg)
                .In("subq    {0}, {1}({2})", right_frag.Reg, ObjLayoutFacts.IntValue, left_frag.Reg)
                .ToString()

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

        let asm =
            this.EmitAsm()
                .Location(match_node.Span.First)
                .Paste(expr_frag.Value.Asm)
           
        let tag_reg = _context.RegSet.Allocate()
        
        let cases = Array.concat [[| cases_hd |]; cases_tl]    
        let patterns = cases |> Array.map (fun case ->
            match case.Syntax.Pattern.Syntax with
            | PatternSyntax.IdType (_, ty) -> ty
            | PatternSyntax.Null -> AstNode.Virtual(BasicClassNames.Null))

        let pattern_asm_infos = Dictionary<TYPENAME, struct {| Label: Label; Tag: int |}>()
        for pattern in patterns do
            let pattern_ty = _context.ClassSymMap.[pattern.Syntax]    
            pattern_asm_infos.Add(pattern.Syntax,
                                  {| Label = _context.LabelGen.Generate()
                                     Tag = pattern_ty.Tag |})
            
        let expr_location = _context.Source.Map(expr.Span.First)
        let match_init_label = _context.LabelGen.Generate()
        let is_tag_valid_label = _context.LabelGen.Generate()
        let try_match_label = _context.LabelGen.Generate()
        
        asm.Comment("handle null")
           .In("cmpq    $0, {0}", expr_frag.Value.Reg)
           .Jne(match_init_label, "match init")
           .AsUnit()
           
        if pattern_asm_infos.ContainsKey(BasicClassNames.Null)
        then
            let null_pattern_asm_info = pattern_asm_infos.[BasicClassNames.Null]
            asm.Jmp(null_pattern_asm_info.Label, "case null => ...").AsUnit()
        else
            asm.RtAbortMatch(filename_label=_context.StrConsts.GetOrAdd(expr_location.FileName),
                             line=expr_location.Line,
                             col=expr_location.Col,
                             expr_reg=expr_frag.Value.Reg)
               .AsUnit()

        asm.Label(match_init_label, "match init")
           .AsUnit()
           
        if pattern_asm_infos |> Seq.exists (fun it -> it.Key <> BasicClassNames.Null)
        then
            // Store the expression's value on stack,
            // such that a var introduced by a matched case would pick it up.
            asm.In("movq    {0}, -{1}(%rbp)",
                   expr_frag.Value.Reg,
                   _sym_table.Frame.VarsOffset + (_sym_table.Frame.VarsCount + 1) * 8,
                   "the expression's value")
               .AsUnit()
              
        asm.In("movq    ({0}), {1}", expr_frag.Value.Reg, tag_reg, "tag")
           .Label(is_tag_valid_label, "no match?")
           .In("cmpq    $-1, {0}", tag_reg)
           .Jne(try_match_label, "try match")
           .RtAbortMatch(filename_label=_context.StrConsts.GetOrAdd(expr_location.FileName),
                         line=expr_location.Line,
                         col=expr_location.Col,
                         expr_reg=expr_frag.Value.Reg)
           .Label(try_match_label, "try match")
           .AsUnit()
           
        for pattern_asm_info in pattern_asm_infos do
            // We already emitted asm for 'null'. Don't try to do it again.
            if pattern_asm_info.Key <> BasicClassNames.Null
            then
                asm.In("cmpq    ${0}, {1}", pattern_asm_info.Value.Tag, tag_reg)
                   .Je(pattern_asm_info.Value.Label, comment=pattern_asm_info.Key.ToString())
                   .AsUnit()
        
        asm.In("salq    $3, {0}", tag_reg, "multiply by 8")
           .In("movq    {0}({1}), {2}", RtNames.ClassParentTable, tag_reg, tag_reg, "the parent's tag")
           .Jmp(is_tag_valid_label, "no match?")
           .AsUnit()
        
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
        let result_reg = _context.RegSet.Allocate()
        
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
                    asm.Location(case.Span.First)
                       .Label(pattern_asm_info.Label, comment="case " + pattern_ty.ToString())
                       .Paste(block_frag.Value.Asm)
                       .In("movq    {0}, {1}", block_frag.Value.Reg, result_reg)
                       .Jmp(done_label, "end match")
                       .AsUnit()
                       
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
        
        let receiver_ty = receiver_frag.Value.Type
        
        if not (receiver_ty.Methods.ContainsKey(method_id.Syntax))
        then
            _context.Diags.Error(
                sprintf "'%O' does not contain a definition for '%O'"
                        receiver_frag.Value.Type.Name
                        method_id.Syntax,
                method_id.Span)
            
            _context.RegSet.Free(receiver_frag.Value.Reg)
            Error
        else
            
        let receiver_location = _context.Source.Map(receiver.Span.First)
        let receiver_is_some_label = _context.LabelGen.Generate()
        
        let asm =
            this.EmitAsm()
                .Location(dispatch_node.Span.First)
                .PushCallerSavedRegs()
                .Comment("actual #0")
                .Paste(receiver_frag.Value.Asm)
                .In("cmpq    $0, {0}", receiver_frag.Value.Reg)
                .Jne(receiver_is_some_label, "the receiver is some")
                .RtAbortDispatch(filename_label=_context.StrConsts.GetOrAdd(receiver_location.FileName),
                                 line=receiver_location.Line,
                                 col=receiver_location.Col)
                .Label(receiver_is_some_label, "the receiver is some")

        let method_sym = receiver_ty.Methods.[method_id.Syntax]
        let method_name = sprintf "'%O.%O'" receiver_ty.Name method_sym.Name
        
        let actuals_frag = translate_actuals method_name
                                             method_id.Span
                                             method_sym
                                             (*formal_name=*)"formal"
                                             receiver_frag.Value
                                             actuals
        if actuals_frag.IsError
        then
            Error
        else
            
        let method_reg = _context.RegSet.Allocate()
        
        asm.Paste(actuals_frag.Value)
           .In("movq    {0}(%rdi), {1}", ObjLayoutFacts.VTable,
                                         method_reg,
                                         comment=receiver_ty.Name.ToString() + "_vtable")
           .In("movq    {0}({1}), {2}", method_sym.Index * MemLayoutFacts.VTableEntrySizeInBytes,
                                        method_reg,
                                        method_reg,
                                        comment=receiver_ty.Name.ToString() + "." + method_sym.Name.ToString())
           .In("call    *{0}", method_reg)
           .AsUnit()
        
        // We only have (ActualRegs.Length - 1) registers to store actuals,
        // as we always use %rdi to store `this`.
        let actual_on_stack_count = actuals.Length - (SysVAmd64AbiFacts.ActualRegs.Length - 1)
        if actual_on_stack_count > 0
        then
            asm.In("addq    ${0}, %rsp", actual_on_stack_count * FrameLayoutFacts.ElemSizeInBytes,
                                         comment="remove " +
                                                 actual_on_stack_count.ToString() +
                                                 " actual(s) from stack")
               .AsUnit()
        
        let result_reg = _context.RegSet.Allocate()

        asm.PopCallerSavedRegs()
           .In("movq    %rax, {0}", result_reg, "returned value")
           .AsUnit()

        _context.RegSet.Free(method_reg)

        Ok { AsmFragment.Asm = asm.ToString()
             Type = _context.ClassSymMap.[method_sym.ReturnType]
             Reg = result_reg }
        
        
    and translate_super_dispatch super_dispatch_node method_id actuals =
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

        let actuals_frag = translate_actuals method_name
                                             method_id.Span
                                             method_sym
                                             (*formal_name=*)"formal"
                                             this_frag.Value
                                             actuals
        if actuals_frag.IsError
        then
            Error
        else
            
        let asm =
            this.EmitAsm()
                .Location(super_dispatch_node.Span.First)
                .PushCallerSavedRegs()
                .Comment("actual #0")
                .Paste(this_frag.Value.Asm)

        let result_reg = _context.RegSet.Allocate()

        asm.Paste(actuals_frag.Value)
           .In("call    {0}.{1}", method_sym.DeclaringClass,
                                  method_sym.Name,
                                  comment="super." + method_sym.Name.ToString())
           .In("movq    %rax, {0}", result_reg, comment="returned value")
           .AsUnit()
        
        // We only have (ActualRegs.Length - 1) registers to store actuals,
        // as we always use %rdi to store `this`.
        let actual_on_stack_count = actuals.Length - (SysVAmd64AbiFacts.ActualRegs.Length - 1)
        if actual_on_stack_count > 0
        then
            asm.In("addq    ${0}, %rsp", actual_on_stack_count * FrameLayoutFacts.ElemSizeInBytes,
                                         comment="remove " +
                                                 actual_on_stack_count.ToString() +
                                                 " actual(s) from stack")
               .AsUnit()

        asm.PopCallerSavedRegs()
           .AsUnit()

        Ok { AsmFragment.Asm = asm.ToString()
             Type = _context.ClassSymMap.[method_sym.ReturnType]
             Reg = result_reg }
        
        
    and translate_new new_node type_name actuals =
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

        let asm =
            this.EmitAsm()
                .Location(new_node.Span.First)
                .PushCallerSavedRegs()
        
        // The actual's type '%O' does not conform to the %s's type '%O'
        let this_reg = _context.RegSet.Allocate()
        
        if ty.Is(BasicClasses.ArrayAny)
        then
            asm.Comment("ArrayAny..ctor will allocate memory for N items")
               .In("xorq    {0}, {1}", this_reg, this_reg)
               .AsUnit()
        else
            asm.RtCopyObject(proto="$" + ty.Name.ToString() + "_proto_obj",
                             copy_reg=this_reg)
               .AsUnit()
        
        let this_frag =
            { AsmFragment.Asm = ""
              Reg = this_reg
              Type = ty }
        
        let method_sym = ty.Ctor
        let method_name = sprintf "Constructor of '%O'" ty.Name

        let actuals_frag =
            translate_actuals method_name
                              type_name.Span
                              method_sym
                              (*formal_name=*)"varformal"
                              this_frag
                              actuals
        if actuals_frag.IsError
        then
            Error
        else

        let result_reg = _context.RegSet.Allocate()
        
        asm.Paste(actuals_frag.Value)
           .In("call    {0}..ctor", ty.Name)
           .AsUnit()
        
        // We only have (ActualRegs.Length - 1) registers to store actuals,
        // as we always use %rdi to store `this`.
        let actual_on_stack_count = actuals.Length - (SysVAmd64AbiFacts.ActualRegs.Length - 1)
        if actual_on_stack_count > 0
        then
            asm.In("addq    ${0}, %rsp", actual_on_stack_count * FrameLayoutFacts.ElemSizeInBytes,
                                         comment="remove " +
                                                 actual_on_stack_count.ToString() +
                                                 " actual(s) from stack")
               .AsUnit()
        
        asm.PopCallerSavedRegs()
           .In("movq    %rax, {0}", result_reg, comment="the new object")
           .AsUnit()

        Ok { AsmFragment.Asm = asm.ToString()
             Type = ty
             Reg = result_reg }

    
    and translate_actuals (method_name: string)
                          (method_id_span: Span)
                          (method_sym: MethodSymbol)
                          (formal_name: string)
                          (this_frag: AsmFragment)
                          (actual_nodes: AstNode<ExprSyntax>[])
                          : Res<string> =
        let asm =
            this.EmitAsm()
                .Location(method_id_span.Last)
                .In("subq    ${0}, %rsp", (actual_nodes.Length + (*this*)1) * FrameLayoutFacts.ElemSizeInBytes)
                .In("movq    {0}, 0(%rsp)", this_frag.Reg, comment="actual #0")
           
        _context.RegSet.Free(this_frag.Reg)
            
        let actual_frags = List<Res<AsmFragment>>()
        for actual_index = 0 to (actual_nodes.Length - 1) do
            let actual_frag = translate_expr actual_nodes.[actual_index]
            if actual_frag.IsOk
            then
                let comment = String.Format("actual #{0}", actual_index + 1)
                asm.Comment(comment)
                   .Paste(actual_frag.Value.Asm)
                   .In("movq    {0}, {1}(%rsp)", actual_frag.Value.Reg,
                                                 ((actual_index + 1) * FrameLayoutFacts.ElemSizeInBytes) :> obj,
                                                 comment=comment)
                   .AsUnit()
                   
                _context.RegSet.Free(actual_frag.Value.Reg)

            actual_frags.Add(actual_frag)
        
        asm.Comment("load up to 6 first actuals into regs")
           .AsUnit()
           
        // We store `this` in %rdi, and as a result can only pass 5 actuals in registers.
        let actual_in_reg_count = if (actual_frags.Count + 1) > SysVAmd64AbiFacts.ActualRegs.Length
                                  then SysVAmd64AbiFacts.ActualRegs.Length
                                  else actual_frags.Count + 1 // Add one, to account for passing 'this' as the actual #0. 
                                  
        for actual_index = 0 to (actual_in_reg_count - 1) do
            asm.In("movq    {0}(%rsp), {1}", value0=actual_index * FrameLayoutFacts.ElemSizeInBytes,
                                             value1=SysVAmd64AbiFacts.ActualRegs.[actual_index])
               .AsUnit()
        
        asm.Comment("remove the register-loaded actuals from stack")
           .In("addq    ${0}, %rsp", actual_in_reg_count * FrameLayoutFacts.ElemSizeInBytes)
           .AsUnit()
            
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
            
            let asm = StringBuilder()
            let addr_frag = this.AddrOf(sym)
            let result_reg = _context.RegSet.Allocate()
            
            if addr_frag.Asm.IsSome
            then
                asm.Append(addr_frag.Asm.Value).Nop()
            
            asm.AppendLine(sprintf "    movq %s, %s # %O" addr_frag.Addr
                                                          (_context.RegSet.NameOf(result_reg))
                                                          sym.Name)
               .Nop()
            
            _context.RegSet.Free(addr_frag.Reg)
            
            Ok { AsmFragment.Asm = asm.ToString()
                 Type = ty
                 Reg = result_reg }
            
            
    and translate_int int_node int_syntax =
        let const_label = _context.IntConsts.GetOrAdd(int_syntax.Value)
        let reg = _context.RegSet.Allocate()
        let asm = sprintfn "    movq $%s, %s" const_label (_context.RegSet.NameOf(reg))
        
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Int
             Reg = reg }
        
        
    and translate_str str_node str_syntax =
        let const_label = _context.StrConsts.GetOrAdd(str_syntax.Value)
        let reg = _context.RegSet.Allocate()
        let asm = sprintfn "    movq $%s, %s" const_label (_context.RegSet.NameOf(reg))
        
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.String
             Reg = reg }
        
        
    and translate_bool bool_node bool_syntax =
        let const_label = if bool_syntax = BOOL.True
                          then "Boolean_true"
                          else "Boolean_false"
        let reg = _context.RegSet.Allocate()
        let asm = sprintfn "    movq $%s, %s" const_label (_context.RegSet.NameOf(reg))
                          
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Boolean
             Reg = reg }
        
        
    and translate_this this_node =
        let sym = _sym_table.Resolve(ID "this")
        let ty = _context.ClassSymMap.[sym.Type]

        let asm = StringBuilder()
        let addr_frag = this.AddrOf(sym)
        let result_reg = _context.RegSet.Allocate()
        
        if addr_frag.Asm.IsSome
        then
            asm.Append(addr_frag.Asm.Value).Nop()
        
        asm.AppendLine(sprintf "    movq %s, %s # this" addr_frag.Addr
                                                        (_context.RegSet.NameOf(result_reg)))
           .Nop()
           
        _context.RegSet.Free(addr_frag.Reg)
        
        Ok { AsmFragment.Asm = asm.ToString()
             Type = ty
             Reg = result_reg }
        
        
    and translate_null this_node =
        let result_reg = _context.RegSet.Allocate()
        let asm = sprintfn "    xorq %s, %s" (_context.RegSet.NameOf(result_reg))
                                             (_context.RegSet.NameOf(result_reg))
        
        Ok { AsmFragment.Asm = asm
             Type = BasicClasses.Null 
             Reg = result_reg }
        
        
    and translate_unit unit_node =
        let result_reg = _context.RegSet.Allocate()
        let asm = sprintfn "    movq $Unit_value, %s" (_context.RegSet.NameOf(result_reg))
        
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

    
    and emit_cmpop (left_frag: AsmFragment)
                   (right_frag: AsmFragment)
                   (jmp: string)
                   : struct {| Asm: string; Reg: Reg |} =
        
        let result_reg = _context.RegSet.Allocate()
        let asm =
            emit_cmpop_with_branches left_frag
                                     right_frag
                                     jmp
                                     (sprintfn "    movq $Boolean_false, %s" (_context.RegSet.NameOf(result_reg)))
                                     (sprintfn "    movq $Boolean_true, %s" (_context.RegSet.NameOf(result_reg)))
        {| Asm=asm; Reg=result_reg |}
    
    
    and emit_cmpop_with_branches (left_frag: AsmFragment)
                                 (right_frag: AsmFragment)
                                 (jmp: string)
                                 (false_branch: string)
                                 (true_branch: string)
                                 : string =
        
        let true_label = _context.LabelGen.Generate()
        let done_label = _context.LabelGen.Generate()
        
        let asm = StringBuilder()
        
        asm.AppendLine("    #. cmp op / if")
           .Append(left_frag.Asm.ToString())
           .Append(right_frag.Asm.ToString())
           .AppendLine(sprintf "    movq 24(%s), %s" (_context.RegSet.NameOf(left_frag.Reg))
                                                     (_context.RegSet.NameOf(left_frag.Reg)))
           .AppendLine(sprintf "    movq 24(%s), %s" (_context.RegSet.NameOf(right_frag.Reg))
                                                     (_context.RegSet.NameOf(right_frag.Reg)))
           .AppendLine(sprintf "    cmpq %s, %s" (_context.RegSet.NameOf(right_frag.Reg))
                                                 (_context.RegSet.NameOf(left_frag.Reg)))
           .AppendLine(sprintf "    %s %s # true / then" jmp (_context.LabelGen.NameOf(true_label)))
           .AppendLine("    # false / else")
           .Append(false_branch)
           .AppendLine(sprintf "    jmp %s # done" (_context.LabelGen.NameOf(done_label)))
           .AppendLine(sprintf "%s: # true / then" (_context.LabelGen.NameOf(true_label)))
           .Append(true_branch)
           .AppendLine(sprintf "%s: #. end cmp op / if" (_context.LabelGen.NameOf(done_label)))
           .Nop()
        
        _context.RegSet.Free(left_frag.Reg)
        _context.RegSet.Free(right_frag.Reg)
        
        asm.ToString()
    
    
    and emit_eqop_with_branches (left_frag: AsmFragment)
                                (right_frag: AsmFragment)
                                (unequal_branch: string)
                                (equal_branch: string)
                                : string =

        let equal_label = _context.LabelGen.Generate()
        let done_label = _context.LabelGen.Generate()
        
        let asm =
            StringBuilder()
                .AppendLine("    # eq op / if")
                .Append(left_frag.Asm.ToString())
                .Append(right_frag.Asm.ToString())
                .AppendLine("    # try ptr equality first")
                .AppendLine(sprintf "    cmpq %s, %s" (_context.RegSet.NameOf(left_frag.Reg))
                                                      (_context.RegSet.NameOf(right_frag.Reg)))
                .AppendLine(sprintf "    je %s # equal" (_context.LabelGen.NameOf(equal_label)))
                .AppendLine("    pushq %r10")
                .AppendLine("    pushq %r11")
                .AppendLine(sprintf "    movq %s, %%rdi" (_context.RegSet.NameOf(left_frag.Reg)))
                .AppendLine(sprintf "    movq %s, %%rsi" (_context.RegSet.NameOf(right_frag.Reg)))
                .AppendLine("    call .Runtime.are_equal")
                .AppendLine("    popq %r11")
                .AppendLine("    popq %r10")
                .AppendLine("    cmpq $Boolean_true, %rax")
                .AppendLine(sprintf "    je %s # equal" (_context.LabelGen.NameOf(equal_label)))
                .AppendLine("    # unequal")
                .Append(unequal_branch)
                .AppendLine(sprintf "    jmp %s # done" (_context.LabelGen.NameOf(done_label)))
                .AppendLine("    # equal")
                .AppendLine(sprintf "%s:" (_context.LabelGen.NameOf(equal_label)))
                .Append(equal_branch)
                .AppendLine("    # done")
                .AppendLine(sprintf "%s: # end eq_op / if" (_context.LabelGen.NameOf(done_label)))
                .ToString()
            
        _context.RegSet.Free(left_frag.Reg)
        _context.RegSet.Free(right_frag.Reg)
        
        asm
    
    
    member this.TranslateBlock(block_syntax_opt: BlockSyntax voption): Res<AsmFragment> =
        match block_syntax_opt with
        | ValueNone ->
            let result_reg = _context.RegSet.Allocate()
            let asm = sprintfn "    movq $Unit_value, %s" (_context.RegSet.NameOf(result_reg))
            
            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Unit
                 Reg = result_reg }
        | ValueSome block_syntax ->
            _sym_table.EnterBlock()
        
            let asm = StringBuilder()
            
            block_syntax.Stmts
            |> Seq.iter (fun stmt_node ->
                match stmt_node.Syntax with
                | StmtSyntax.Var var_syntax ->
                    let var_frag = translate_var (stmt_node.Map(fun _ -> var_syntax))
                    if var_frag.IsOk
                    then
                        _context.RegSet.Free(var_frag.Value.Reg)
                        asm.Append(var_frag.Value.Asm).Nop()
                        
                | StmtSyntax.Expr expr_syntax ->
                    let expr_frag = translate_expr (stmt_node.Map(fun _ -> expr_syntax))
                    if expr_frag.IsOk
                    then
                        _context.RegSet.Free(expr_frag.Value.Reg)
                        asm.Append(expr_frag.Value.Asm).Nop()
            )
            
            let expr_frag = translate_expr block_syntax.Expr
            
            _sym_table.LeaveBlock()
            
            if expr_frag.IsError
            then
                Error
            else
                
            asm.Append(expr_frag.Value.Asm)
               .Nop()
            
            Ok { AsmFragment.Asm = asm.ToString()
                 Type = expr_frag.Value.Type
                 Reg = expr_frag.Value.Reg }
        
        
    member this.AddrOf(sym: Symbol) : AddrFragment =
        match sym.Kind with
        | SymbolKind.Formal ->
            if sym.Index <= 6
            then
                { Addr = sprintf "-%d(%%rbp)" ((sym.Index + 1) * 8)
                  Asm = ValueNone
                  Type = _context.ClassSymMap.[sym.Type]
                  Reg = Reg.Null }
            else
                { Addr = sprintf "%d(%%rbp)" (FrameLayoutFacts.ActualsOutOfFrameOffset + (sym.Index - 7) * 8)
                  Asm = ValueNone
                  Type = _context.ClassSymMap.[sym.Type]
                  Reg = Reg.Null }
                
        | SymbolKind.Var ->
            { Addr = sprintf "-%d(%%rbp)" (_sym_table.Frame.VarsOffset + (sym.Index + 1) * 8)
              Asm = ValueNone
              Type = _context.ClassSymMap.[sym.Type]
              Reg = Reg.Null }
            
        | SymbolKind.Attr ->
            let attrs_offset_in_quads = 3 // skip tag, obj_size, and vtable_ptr
            let this_reg = _context.RegSet.Allocate()
            { Addr = sprintf "%d(%s)"
                             ((attrs_offset_in_quads + sym.Index) * 8)
                             (_context.RegSet.NameOf(this_reg))
              Asm = ValueSome (sprintfn "    movq -8(%%rbp), %s" (_context.RegSet.NameOf(this_reg)))
              Type = _context.ClassSymMap.[sym.Type]
              Reg = this_reg }
               
               
    member this.EmitAsm(): AsmBuilder = AsmBuilder(_context)


    member this.Translate(expr_node: AstNode<ExprSyntax>): Res<AsmFragment> =
        translate_expr expr_node
