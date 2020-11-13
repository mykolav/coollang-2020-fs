namespace rec LibCool.TranslatorParts


open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Text
open LibCool.SharedParts
open LibCool.SourceParts
open LibCool.AstParts
open LibCool.TranslatorParts
open AstExtensions


[<IsReadOnly; Struct>]
type AsmFragment =
    { Type: ClassSymbol
      Asm: StringBuilder
      Reg: Reg }


[<Sealed>]
type private ExprTranslator(_context: TranslationContext,
                            _class_syntax: ClassSyntax,
                            _sym_table: SymbolTable) as this =
    
    
    let rec translate_expr (expr_node: AstNode<ExprSyntax>): Res<AsmFragment> =
        match expr_node.Syntax with
        | ExprSyntax.Assign (id, expr) ->
            translate_assign id expr

        | ExprSyntax.BoolNegation expr ->
            let expr_frag = translate_unaryop_operand expr (*op=*)"!" (*expected_ty=*)BasicClasses.Boolean
            if expr_frag.IsError
            then
                Error

            else

            let expr_frag = expr_frag.Value
            
            let false_label = _context.LabelGen.Generate()
            let done_label = _context.LabelGen.Generate()
            
            expr_frag.Asm
                .AppendLine(sprintf "    cmpq $0, 24(%s)" (_context.RegSet.NameOf(expr_frag.Reg)))
                .AppendLine(sprintf "    je %s" (_context.LabelGen.NameOf(false_label)))
                .AppendLine(sprintf "    movq $Boolean_false, %s" (_context.RegSet.NameOf(expr_frag.Reg)))
                .AppendLine(sprintf "    jmp %s" (_context.LabelGen.NameOf(done_label)))
                .AppendLine(sprintf "%s:" (_context.LabelGen.NameOf(false_label)))
                .AppendLine(sprintf "    movq $Boolean_true, %s" (_context.RegSet.NameOf(expr_frag.Reg)))
                .AppendLine(sprintf "%s:" (_context.LabelGen.NameOf(done_label)))
                .Nop()
            Ok expr_frag
        
        | ExprSyntax.UnaryMinus expr ->
            let expr_frag = translate_unaryop_operand expr (*op=*)"-" (*expected_ty=*)BasicClasses.Int
            if expr_frag.IsError
            then
                Error
            else
            
            let expr_frag = expr_frag.Value    
            expr_frag.Asm
                .AppendLine("    pushq %r10")
                .AppendLine("    pushq %r11")
                .AppendLine(sprintf "    movq %s, %%rdi" (_context.RegSet.NameOf(expr_frag.Reg)))
                .AppendLine("    call .Runtime.copy_object")
                .AppendLine("    popq %r11")
                .AppendLine("    popq %r10")
                .AppendLine(sprintf "    movq %%rax, %s" (_context.RegSet.NameOf(expr_frag.Reg)))
                .AppendLine(sprintf "    negq 24(%s)" (_context.RegSet.NameOf(expr_frag.Reg)))
                .Nop()
            Ok expr_frag
            
        | ExprSyntax.If (condition, then_branch, else_branch) ->
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
                then_frag.Value.Asm
                    .AppendLine(sprintf "    movq %s, %s" (_context.RegSet.NameOf(then_frag.Value.Reg))
                                                          (_context.RegSet.NameOf(result_reg)))
                    .ToString()
            
            let else_asm =
                else_frag.Value.Asm
                    .AppendLine(sprintf "    movq %s, %s" (_context.RegSet.NameOf(else_frag.Value.Reg))
                                                          (_context.RegSet.NameOf(result_reg)))
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
                let left, right, equal_branch, not_equal_branch =
                    match condition.Syntax with
                    | ExprSyntax.EqEq (left, right)  -> left, right, then_asm, else_asm
                    | ExprSyntax.NotEq (left, right) -> left, right, else_asm, then_asm
                    | _                              -> invalidOp "Unreachable"
                let operands = translate_eqop_operands left right "=="
                if operands.IsError
                then
                    Error
                else
                    
                let left_frag, right_frag = operands.Value
                let asm = 
                    emit_eqop_with_branches left_frag
                                            right_frag
                                            not_equal_branch
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

            let asm = StringBuilder()
            
            asm.AppendLine("    # if_expr")
               .Append(condition_frag.Value.Asm.ToString())
               .AppendLine(sprintf "    cmpq $0, 24(%s)" (_context.RegSet.NameOf(condition_frag.Value.Reg)))
               .AppendLine(sprintf "    je %s" (_context.LabelGen.NameOf(else_label)))
               .AppendLine("    # then")
               .Append(then_asm)
               .AppendLine(sprintf "    jmp %s" (_context.LabelGen.NameOf(done_label)))
               .AppendLine(sprintf "%s: # else" (_context.LabelGen.NameOf(else_label)))
               .Append(else_asm)
               .AppendLine(sprintf "%s: # done" (_context.LabelGen.NameOf(done_label)))
               .Nop()
            
            _context.RegSet.Free(condition_frag.Value.Reg)
            
            Ok { AsmFragment.Asm = asm
                 Type = _context.TypeCmp.LeastUpperBound(then_frag.Value.Type, else_frag.Value.Type)
                 Reg = result_reg }
        
        | ExprSyntax.While (condition, body) ->
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
            let loop_label = _context.LabelGen.Generate()
            let done_label = _context.LabelGen.Generate()

            let asm =
                StringBuilder()
                    .AppendLine("    # while")
                    .AppendLine(sprintf "%s: # condition" (_context.LabelGen.NameOf(loop_label)))
                    .Append(condition_frag.Value.Asm.ToString())
                    .AppendLine(sprintf "    cmpq $0, 24(%s)" (_context.RegSet.NameOf(condition_frag.Value.Reg)))
                    .AppendLine(sprintf "    je %s" (_context.LabelGen.NameOf(done_label)))
                    .AppendLine("    # body")
                    .Append(body_frag.Value.Asm)
                    .AppendLine(sprintf "    jmp %s" (_context.LabelGen.NameOf(loop_label)))
                    .AppendLine(sprintf "%s: # done" (_context.LabelGen.NameOf(done_label)))
                    .AppendLine(sprintf "    movq $Unit_value, %s" (_context.RegSet.NameOf(result_reg)))
                
            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Unit
                 Reg = result_reg }

        | ExprSyntax.LtEq (left, right) ->
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
        
        | ExprSyntax.GtEq (left, right) ->
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
        
        | ExprSyntax.Lt (left, right) ->
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
        
        | ExprSyntax.Gt (left, right) ->
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
        
        | ExprSyntax.EqEq (left, right) ->
            let operands = translate_eqop_operands left right "=="
            if operands.IsError
            then
                Error
            else
                
            let left_frag, right_frag = operands.Value
            let result_reg = _context.RegSet.Allocate()
            let asm =
                StringBuilder()
                    .Append(left_frag.Asm.ToString())
                    .Append(right_frag.Asm.ToString())
                    .AppendLine("    pushq %r10")
                    .AppendLine("    pushq %r11")
                    .AppendLine(sprintf "    movq %s, %%rdi" (_context.RegSet.NameOf(left_frag.Reg)))
                    .AppendLine(sprintf "    movq %s, %%rsi" (_context.RegSet.NameOf(right_frag.Reg)))
                    .AppendLine("    call .Runtime.are_equal")
                    .AppendLine(sprintf "movq %%rax, %s" (_context.RegSet.NameOf(result_reg)))
                    .AppendLine("    popq %r11")
                    .AppendLine("    popq %r10")
                
            _context.RegSet.Free(left_frag.Reg)
            _context.RegSet.Free(right_frag.Reg)
            
            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Boolean
                 Reg = result_reg }
        
        | ExprSyntax.NotEq (left, right) ->
            let operands = translate_eqop_operands left right "!="
            if operands.IsError
            then
                Error
            else
                
            let result_reg = _context.RegSet.Allocate()
            let not_equal_branch = sprintf "    movq $Boolean_true, %s\n" (_context.RegSet.NameOf(result_reg))
            let equal_branch = sprintf "    movq $Boolean_false, %s\n" (_context.RegSet.NameOf(result_reg))
            let left_frag, right_frag = operands.Value
            let asm = 
                emit_eqop_with_branches left_frag
                                        right_frag
                                        not_equal_branch
                                        equal_branch
                                        
            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Boolean
                 Reg = result_reg }
        
        | ExprSyntax.Mul (left, right) ->
            let operands = translate_infixop_int_operands left right "*"
            if operands.IsError
            then
                Error
            else
            
            let left_frag, right_frag = operands.Value    

            let result_reg = _context.RegSet.Allocate()
            
            let asm = StringBuilder()
            asm.AppendLine("    # multiply")
               .Append(left_frag.Asm)
               .Append(right_frag.Asm)
               .AppendLine(sprintf "    movq %s, %%rax" (_context.RegSet.NameOf(left_frag.Reg)))
               .AppendLine(sprintf "    imul %s" (_context.RegSet.NameOf(right_frag.Reg)))
               .AppendLine(sprintf "    movq %%rax, %s" (_context.RegSet.NameOf(result_reg)))
               .Nop()

            _context.RegSet.Free(left_frag.Reg)
            _context.RegSet.Free(right_frag.Reg)
            
            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Int
                 Reg = result_reg }
        
        | ExprSyntax.Div (left, right) ->
            let operands = translate_infixop_int_operands left right "/"
            if operands.IsError
            then
                Error
            else
            
            let left_frag, right_frag = operands.Value    

            let result_reg = _context.RegSet.Allocate()
            
            let asm = StringBuilder()
            asm.AppendLine("    # divide")
               .Append(left_frag.Asm)
               .Append(right_frag.Asm)
               // left / right
               .AppendLine("    xorq %rdx, %rdx")
               .AppendLine(sprintf "    movq %s, %%rax" (_context.RegSet.NameOf(left_frag.Reg)))
               .AppendLine(sprintf "    idiv %s" (_context.RegSet.NameOf(right_frag.Reg)))
               .AppendLine(sprintf "    movq %%rax, %s" (_context.RegSet.NameOf(result_reg)))
               .Nop()

            _context.RegSet.Free(left_frag.Reg)
            _context.RegSet.Free(right_frag.Reg)
            
            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Int
                 Reg = result_reg }
        
        | ExprSyntax.Sum (left, right) ->
            let typecheck left_frag right_frag: bool =
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

            let operands = translate_infixop_operands left right typecheck
            if operands.IsError
            then
                Error
            else
            
            let left_frag, right_frag = operands.Value
            
            let result_reg = _context.RegSet.Allocate()
            
            let asm = StringBuilder()
            asm.AppendLine("    # add / concatenate")
               .Append(left_frag.Asm)
               .Append(right_frag.Asm)
               .Nop()
            
            if left_frag.Type.Is(BasicClasses.Int) &&
               right_frag.Type.Is(BasicClasses.Int)
            then
                asm.AppendLine(sprintf "    addq %s, %s" (_context.RegSet.NameOf(left_frag.Reg))
                                                         (_context.RegSet.NameOf(right_frag.Reg)))
                   .AppendLine(sprintf "    movq %s, %s" (_context.RegSet.NameOf(right_frag.Reg))
                                                         (_context.RegSet.NameOf(result_reg)))
                   .Nop()
            else // string concatenation
                asm.AppendLine("    pushq %r10")
                   .AppendLine("    pushq %r11")
                   .AppendLine(sprintf "    movq %s, %%rdi" (_context.RegSet.NameOf(left_frag.Reg)))
                   .AppendLine(sprintf "    movq %s, %%rsi" (_context.RegSet.NameOf(right_frag.Reg)))
                   .AppendLine("    call String.concat")
                   .AppendLine(sprintf "    movq %%rax, %s" (_context.RegSet.NameOf(result_reg)))
                   .AppendLine("    popq %r11")
                   .AppendLine("    popq %r10")
                   .Nop()

            _context.RegSet.Free(left_frag.Reg)
            _context.RegSet.Free(right_frag.Reg)
            
            Ok { AsmFragment.Asm = asm
                 Type = left_frag.Type
                 Reg = result_reg }
        
        | ExprSyntax.Sub (left, right) ->
            let operands = translate_infixop_int_operands left right "-"
            if operands.IsError
            then
                Error
            else
            
            let left_frag, right_frag = operands.Value    

            let result_reg = _context.RegSet.Allocate()
            
            let asm = StringBuilder()
            asm.AppendLine("    # subtract")
               .Append(left_frag.Asm)
               .Append(right_frag.Asm)
               // left - right
               .AppendLine(sprintf "    subq %s, %s" (_context.RegSet.NameOf(right_frag.Reg))
                                                     (_context.RegSet.NameOf(left_frag.Reg)))
               .AppendLine(sprintf "    movq %s, %s" (_context.RegSet.NameOf(left_frag.Reg))
                                                     (_context.RegSet.NameOf(result_reg)))
               .Nop()

            _context.RegSet.Free(left_frag.Reg)
            _context.RegSet.Free(right_frag.Reg)
            
            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Int
                 Reg = result_reg }
        
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
                
                let pattern_ty = _context.ClassSymMap.[pattern.Syntax]    
                if not (_context.TypeCmp.Conforms(pattern_ty, expr_frag.Value.Type) ||
                        _context.TypeCmp.Conforms(expr_frag.Value.Type, pattern_ty))
                then
                    _context.Diags.Error(
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
                    if _context.ClassSymMap.ContainsKey(prev_pattern.Syntax)
                    then
                        let prev_pattern_ty = _context.ClassSymMap.[prev_pattern.Syntax]    
                        if _context.TypeCmp.Conforms(ancestor=prev_pattern_ty, descendant=pattern_ty)
                        then
                            _context.Diags.Error(
                                sprintf "This case is shadowed by an earlier case at %O"
                                        (_context.Source.Map(prev_pattern.Span.First)),
                                pattern.Span)
                            pattern_error <- true
            
            let block_frags =
                cases |> Array.map (fun case ->
                    _sym_table.EnterBlock()
                    
                    match case.Syntax.Pattern.Syntax with
                    | PatternSyntax.IdType (id, ty) ->
                        _sym_table.AddVar({ Symbol.Name = id.Syntax
                                            Type = ty.Syntax
                                            Index = _sym_table.MethodFrame.VarsCount
                                            SyntaxSpan = case.Syntax.Pattern.Span
                                            Kind = SymbolKind.Var })
                    | PatternSyntax.Null ->
                        ()
                        
                    let block_frag = this.TranslateBlock(case.Syntax.Block.Syntax.AsBlockSyntax)
                    if block_frag.IsOk then _context.RegSet.Free(block_frag.Value.Reg)
                    _sym_table.LeaveBlock()
                    
                    block_frag)
            
            if pattern_error || (block_frags |> Seq.exists (fun it -> it.IsError))
            then
                _context.RegSet.Free(expr_frag.Value.Reg)
                Error
            else
                
            let block_types = block_frags |> Array.map (fun it -> it.Value.Type)
            
            _context.RegSet.Free(expr_frag.Value.Reg)
            
            Ok { AsmFragment.Asm = StringBuilder()
                 Type = _context.TypeCmp.LeastUpperBound(block_types)
                 Reg = Reg.Null }

        | ExprSyntax.Dispatch (receiver, method_id, actuals) ->
            translate_dispatch receiver method_id actuals
            
        | ExprSyntax.ImplicitThisDispatch (method_id, actuals) ->
            translate_dispatch (AstNode.Virtual(ExprSyntax.This)) method_id actuals
            
        | ExprSyntax.SuperDispatch (method_id, actuals) ->
            let super_sym = _context.ClassSymMap.[_class_syntax.ExtendsSyntax.SUPER.Syntax] 
            if not (super_sym.Methods.ContainsKey(method_id.Syntax))
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

            let asm = StringBuilder()
            asm.AppendLine(sprintf "    # super dispatch %O.%O" super_sym.Name
                                                                method_id.Syntax)
               .AppendLine("    pushq %r10")
               .AppendLine("    pushq %r11")
               .Nop()
                
            let method_sym = super_sym.Methods.[method_id.Syntax]
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
                
            let result_reg = _context.RegSet.Allocate()
            
            asm.Append(actuals_frag.Value)
               .AppendLine(sprintf "    call %O.%O" method_sym.DeclaringClass method_sym.Name)
               .AppendLine(sprintf "    movq %%rax, %s # store return value" (_context.RegSet.NameOf(result_reg)))
               .Nop()
            
            // We only store 5 actuals in registers,
            // as we always use %rdi to store `this`.
            let actual_on_stack_count = actuals.Length - 5
            if actual_on_stack_count > 0
            then
                asm.AppendLine(sprintf "    addq %d, %%rsp" (actual_on_stack_count * 8))
                   .Nop()
            
            asm.AppendLine("    popq %r11")
               .AppendLine("    popq %r10")
               .Nop()

            Ok { AsmFragment.Asm = asm
                 Type = _context.ClassSymMap.[method_sym.ReturnType]
                 Reg = result_reg }
            
        | ExprSyntax.New (type_name, actuals) ->
            if (typecheck_type type_name) = Error
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

            let asm = StringBuilder()
            asm.AppendLine(sprintf "    # new %O" ty.Name)
               .Nop()
               
            let this_reg = _context.RegSet.Allocate()
            let this_frag = {
                AsmFragment.Asm = StringBuilder()
                                      .AppendLine("    # make a copy of the prototype object")
                                      .AppendLine(sprintf "    movq $%O_proto_obj, %%rdi" ty.Name)
                                      .AppendLine("    pushq %r10")
                                      .AppendLine("    pushq %r11")
                                      .AppendLine("    call .Runtime.copy_object")
                                      .AppendLine(sprintf "    movq %%rax, %s" (_context.RegSet.NameOf(this_reg)))
                                      .AppendLine("    popq %r11")
                                      .AppendLine("    popq %r10")
                Reg = this_reg
                Type = ty
            }
            
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
            
            asm.AppendLine("    pushq %r10")
               .AppendLine("    pushq %r11")
               .Append(actuals_frag.Value)
               .AppendLine(sprintf "    call %O..ctor" ty.Name)
               .AppendLine(sprintf "    movq %%rax, %s # store return value" (_context.RegSet.NameOf(result_reg)))
               .Nop()
            
            // We only store 5 actuals in registers,
            // as we always use %rdi to store `this`.
            let actual_on_stack_count = actuals.Length - 5
            if actual_on_stack_count > 0
            then
                asm.AppendLine(sprintf "    addq %d, %%rsp" (actual_on_stack_count * 8))
                   .Nop()
            
            asm.AppendLine("    popq %r11")
               .AppendLine("    popq %r10")
               .Nop()

            Ok { AsmFragment.Asm = asm
                 Type = ty
                 Reg = result_reg }
                
        | ExprSyntax.BracedBlock block ->
            this.TranslateBlock(block)
                
        | ExprSyntax.ParensExpr expr ->
            translate_expr expr

        | ExprSyntax.Id id ->
            let sym = _sym_table.TryResolve(id)
            match sym with
            | ValueNone ->
                _context.Diags.Error(
                    sprintf "The name '%O' does not exist in the current context" id,
                    expr_node.Span)
                Error
            | ValueSome sym ->
                let ty = _context.ClassSymMap.[sym.Type]
                
                let asm = StringBuilder()
                let addr_frag = this.AddrOf(sym)
                let result_reg = _context.RegSet.Allocate()
                
                if addr_frag.Asm.IsSome
                then
                    asm.Append(addr_frag.Asm.Value).Nop()
                
                asm.AppendLine(sprintf "    movq %s, %s" addr_frag.Addr
                                                         (_context.RegSet.NameOf(result_reg)))
                   .Nop()
                
                _context.RegSet.Free(addr_frag.Reg)
                
                Ok { AsmFragment.Asm = asm
                     Type = ty
                     Reg = result_reg }
            
        | ExprSyntax.Int int_syntax ->
            let const_label = _context.IntConsts.GetOrAdd(int_syntax.Value)
            let reg = _context.RegSet.Allocate()
            
            let asm = StringBuilder()
            asm.AppendLine(sprintf "    movq $%s, %s" const_label (_context.RegSet.NameOf(reg)))
               .Nop()
            
            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Int
                 Reg = reg }
            
        | ExprSyntax.Str str_syntax ->
            let const_label = _context.StrConsts.GetOrAdd(str_syntax.Value)
            let reg = _context.RegSet.Allocate()
            
            let asm = StringBuilder()
            asm.AppendLine(sprintf "    movq $%s, %s\n" const_label (_context.RegSet.NameOf(reg)))
               .Nop()
            
            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.String
                 Reg = reg }

        | ExprSyntax.Bool bool_syntax ->
            let const_label = if bool_syntax = BOOL.True
                              then "Boolean_true"
                              else "Boolean_false"
            let reg = _context.RegSet.Allocate()
            
            let asm = StringBuilder()
            asm.AppendLine(sprintf "    movq $%s, %s\n" const_label (_context.RegSet.NameOf(reg)))
               .Nop()
                              
            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Boolean
                 Reg = reg }

        | ExprSyntax.This ->
            let sym = _sym_table.Resolve(ID "this")
            let ty = _context.ClassSymMap.[sym.Type]

            let asm = StringBuilder()
            let addr_frag = this.AddrOf(sym)
            let result_reg = _context.RegSet.Allocate()
            
            if addr_frag.Asm.IsSome
            then
                asm.Append(addr_frag.Asm.Value).Nop()
            
            asm.AppendLine(sprintf "    movq %s, %s" addr_frag.Addr (_context.RegSet.NameOf(result_reg)))
               .Nop()
               
            _context.RegSet.Free(addr_frag.Reg)
            
            Ok { AsmFragment.Asm = asm
                 Type = ty
                 Reg = result_reg }

        | ExprSyntax.Null ->
            let result_reg = _context.RegSet.Allocate()
            let asm = StringBuilder()
            asm.AppendLine(sprintf "    xorq %s, %s" (_context.RegSet.NameOf(result_reg))
                                                     (_context.RegSet.NameOf(result_reg)))
               .Nop()
            
            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Null 
                 Reg = result_reg }

        | ExprSyntax.Unit ->
            let result_reg = _context.RegSet.Allocate()
            let asm = StringBuilder()
            asm.AppendLine(sprintf "    movq $Unit_value, %s" (_context.RegSet.NameOf(result_reg)))
               .Nop()
            
            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Unit
                 Reg = result_reg }
        
        
    and translate_assign (id: AstNode<ID>) (expr: AstNode<ExprSyntax>): Res<AsmFragment> =
        
        let expr_frag = translate_expr expr
        if expr_frag.IsError
        then
            Error
        else
        
        let addr_frag = this.AddrOf(_sym_table.Resolve(id.Syntax))

        if not (_context.TypeCmp.Conforms(ancestor=addr_frag.Type, descendant=expr_frag.Value.Type))
        then
            _context.Diags.Error(
                sprintf "The expression's type '%O' does not conform to the type '%O' of '%O'"
                        expr_frag.Value.Type.Name
                        addr_frag.Type.Name
                        id.Syntax,
                expr.Span)
            
            _context.RegSet.Free(addr_frag.Reg)
            _context.RegSet.Free(expr_frag.Value.Reg)
            Error
        else

        let asm = expr_frag.Value.Asm
        
        if addr_frag.Asm.IsSome
        then
            asm.AppendLine(addr_frag.Asm.Value).Nop()
            
        asm.AppendLine(sprintf "    movq %s, %s"
                               (_context.RegSet.NameOf(expr_frag.Value.Reg))
                               addr_frag.Addr)
           .Nop()
        
        _context.RegSet.Free(addr_frag.Reg)

        // We do not free up expr_frag.Value.Reg,
        // to support assignments of the form `ID = ID = ...`

        Ok { AsmFragment.Asm = asm
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
        let typecheck left_frag right_frag: bool =
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

        translate_infixop_operands left right typecheck
        
        
    and translate_eqop_operands (left: AstNode<ExprSyntax>)
                                (right: AstNode<ExprSyntax>)
                                (op: string)
                                : Res<(AsmFragment * AsmFragment)> =
        let typecheck left_frag right_frag: bool =
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

        translate_infixop_operands left right typecheck
        
        
    and translate_infixop_operands (left: AstNode<ExprSyntax>)
                                   (right: AstNode<ExprSyntax>)
                                   (typecheck: AsmFragment -> AsmFragment -> bool)
                                   : Res<(AsmFragment * AsmFragment)> =
        let left_frag = translate_expr left
        let right_frag = translate_expr right
        
        if left_frag.IsError || right_frag.IsError
        then
            if left_frag.IsOk then _context.RegSet.Free(left_frag.Value.Reg)
            if right_frag.IsOk then _context.RegSet.Free(right_frag.Value.Reg)
            Error
        else
            
        if not (typecheck left_frag.Value right_frag.Value)
        then
            _context.RegSet.Free(left_frag.Value.Reg)
            _context.RegSet.Free(right_frag.Value.Reg)
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
            _context.Diags.Error(
                sprintf "'%O' does not contain a definition for '%O'"
                        receiver_frag.Value.Type.Name
                        method_id.Syntax,
                method_id.Span)
            
            _context.RegSet.Free(receiver_frag.Value.Reg)
            Error
        else
            
        let receiver_location = _context.Source.Map(receiver.Span.First)
        let not_null_label = _context.LabelGen.Generate()
        
        let asm = StringBuilder()
        asm.AppendLine(sprintf "    # dispatch %O.%O" receiver_frag.Value.Type.Name
                                                      method_id.Syntax)
           .AppendLine("    pushq %r10")
           .AppendLine("    pushq %r11")
           .AppendLine(sprintf "    cmpq $0, %s" (_context.RegSet.NameOf(receiver_frag.Value.Reg)))
           .AppendLine(sprintf "    jne %s" (_context.LabelGen.NameOf(not_null_label)))
           .AppendLine("    pushq %r10")
           .AppendLine("    pushq %r11")
           .AppendLine(sprintf "    movq %s, %%rdi" (_context.StrConsts.GetOrAdd(receiver_location.FileName)))
           .AppendLine(sprintf "    movq %d, %%rsi" receiver_location.Line)
           .AppendLine(sprintf "    movq %d, %%rdx" receiver_location.Col)
           .AppendLine("    call .Runtime.abort_dispatch")
           .AppendLine("    popq %r11")
           .AppendLine("    popq %r10")
           .AppendLine(sprintf "%s: # the receiver is not null" (_context.LabelGen.NameOf(not_null_label)))
           .Nop()

        let method_sym = receiver_frag.Value.Type.Methods.[method_id.Syntax]
        let method_name = sprintf "'%O.%O'" receiver_frag.Value.Type.Name method_sym.Name
        
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
        let result_reg = _context.RegSet.Allocate()
        
        asm.Append(actuals_frag.Value)
           .AppendLine(sprintf "    movq 16(%s), %s # load vtable" (_context.RegSet.NameOf(receiver_frag.Value.Reg))
                                                                   (_context.RegSet.NameOf(method_reg)))
           .AppendLine(sprintf "    movq %d(%s), %s # load method addr" (method_sym.Index * 8)
                                                                        (_context.RegSet.NameOf(method_reg))
                                                                        (_context.RegSet.NameOf(method_reg)))
           .AppendLine(sprintf "    call %s" (_context.RegSet.NameOf(method_reg)))
           .AppendLine(sprintf "    movq %%rax, %s # store return value" (_context.RegSet.NameOf(result_reg)))
           .Nop()
        
        // We only store 5 actuals in registers,
        // as we always use %rdi to store `this`.
        let actual_on_stack_count = actuals.Length - 5
        if actual_on_stack_count > 0
        then
            asm.AppendLine(sprintf "    addq %d, %%rsp" (actual_on_stack_count * 8))
               .Nop()
        
        asm.AppendLine("    popq %r11")
           .AppendLine("    popq %r10")
           .Nop()

        _context.RegSet.Free(method_reg)
    
        Ok { AsmFragment.Asm = asm
             Type = _context.ClassSymMap.[method_sym.ReturnType]
             Reg = result_reg }

    
    and translate_actuals (method_name: string)
                          (method_id_span: Span)
                          (method_sym: MethodSymbol)
                          (formal_name: string)
                          (this_frag: AsmFragment)
                          (actual_nodes: AstNode<ExprSyntax>[])
                          : Res<string> =
        let asm = StringBuilder()
        asm.AppendLine(sprintf "    subq %d, %%rsp" ((actual_nodes.Length + 1) * 8))
            // Calculate and store 'this'.
           .AppendLine("    # actual 0 is 'this'")
           .Append(this_frag.Asm.ToString())
           .AppendLine(sprintf "    movq %s, 0(%%rsp)" (_context.RegSet.NameOf(this_frag.Reg)))
           .Nop()
           
        _context.RegSet.Free(this_frag.Reg)
            
        let actual_frags = List<Res<AsmFragment>>()
        actual_nodes |> Array.iteri (fun actual_index actual_node ->
            let actual_frag = translate_expr actual_node
            if actual_frag.IsOk
            then
                asm.AppendLine(sprintf "    # actual %d" actual_index)
                   .Append(actual_frag.Value.Asm.ToString())
                   .AppendLine(sprintf "    movq %s, %d(%%rsp)" (_context.RegSet.NameOf(actual_frag.Value.Reg))
                                                                ((actual_index + 1) * 8))
                   .Nop()
                   
                _context.RegSet.Free(actual_frag.Value.Reg)

            actual_frags.Add(actual_frag)
        )
        
        asm.AppendLine("    # load first 6 actuals into registers")
           .Nop()
           
        let actual_regs = [| "%rdi"; "%rsi"; "%rdx"; "%rcx"; "%r8"; "%r9" |]
        // We store `this` in %rdi, and as a result can only pass 5 actuals in registers. 
        let actual_in_reg_count = if (actual_frags.Count + 1) > actual_regs.Length
                                  then actual_regs.Length
                                  else actual_frags.Count + 1 // Add one, to account for passing 'this' as the actual 0. 
                                  
        for actual_index in 0 .. (actual_in_reg_count - 1) do
            asm.AppendLine(sprintf "    movq %d(%%rsp), %s" (actual_index * 8)
                                                            (actual_regs.[actual_index]))
               .Nop()
        
        asm.AppendLine("    # remove actuals loaded into registers from stack")
           .AppendLine(sprintf "    addq %d, %%rsp" (actual_in_reg_count * 8))
           .Nop()
            
        if actual_frags |> Seq.exists (fun it -> it.IsError)
        then
            Error
        else
        
        if method_sym.Formals.Length <> actual_frags.Count
        then
            _context.Diags.Error(
                sprintf "%s takes %d actual(s) but was passed %d"
                        method_name
                        method_sym.Formals.Length
                        actual_frags.Count,
                method_id_span)

            Error
        else
            
        let mutable formal_actual_mismatch = false
        
        for i in 0 .. method_sym.Formals.Length - 1 do
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
        
    
    and translate_var (var_node: AstNode<VarSyntax>): Res<AsmFragment> =
        if (typecheck_type var_node.Syntax.TYPE) = Error
        then
            Error
        else
            
        // We place a symbol for this variable in the symbol table
        // before translating the init expression.
        // As a result, the var will be visible to the init expression.
        // TODO: Does this correspond to Cool2020's operational semantics?
        _sym_table.AddVar(Symbol.Of(var_node, _sym_table.MethodFrame.VarsCount))
        let assign_frag = translate_assign var_node.Syntax.ID var_node.Syntax.Expr
        if assign_frag.IsError
        then
            Error
        else
            
        _context.RegSet.Free(assign_frag.Value.Reg)
        
        // The var declaration is not an expression, so `Reg = Reg.Null` 
        Ok { assign_frag.Value with Reg = Reg.Null }
            
            
    and typecheck_type (ty_node: AstNode<TYPENAME>): Res<Unit> =
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
                   : struct {| Asm: StringBuilder; Reg: Reg |} =
        
        let result_reg = _context.RegSet.Allocate()
        let asm =
            emit_cmpop_with_branches left_frag
                                     right_frag
                                     jmp
                                     (sprintf "    movq $Boolean_false, %s" (_context.RegSet.NameOf(result_reg)))
                                     (sprintf "    movq $Boolean_true, %s" (_context.RegSet.NameOf(result_reg)))
        {| Asm=asm; Reg=result_reg |}
    
    
    and emit_cmpop_with_branches (left_frag: AsmFragment)
                                 (right_frag: AsmFragment)
                                 (jmp: string)
                                 (false_branch: string)
                                 (true_branch: string)
                                 : StringBuilder =
        
        let true_label = _context.LabelGen.Generate()
        let done_label = _context.LabelGen.Generate()
        
        let asm = StringBuilder()
        
        asm.AppendLine()
           .AppendLine("    # cmp op / if")
           .Append(left_frag.Asm.ToString())
           .Append(right_frag.Asm.ToString())
           .AppendLine(sprintf "    movq 24(%s), %s" (_context.RegSet.NameOf(left_frag.Reg))
                                                     (_context.RegSet.NameOf(left_frag.Reg)))
           .AppendLine(sprintf "    movq 24(%s), %s" (_context.RegSet.NameOf(right_frag.Reg))
                                                     (_context.RegSet.NameOf(right_frag.Reg)))
           .AppendLine(sprintf "    cmpq %s, %s" (_context.RegSet.NameOf(left_frag.Reg))
                                                 (_context.RegSet.NameOf(right_frag.Reg)))
           .AppendLine(sprintf "    %s %s" jmp (_context.LabelGen.NameOf(true_label)))
           .AppendLine("    # false / else")
           .Append(false_branch)
           .AppendLine(sprintf "    jmp %s" (_context.LabelGen.NameOf(done_label)))
           .AppendLine(sprintf "%s: # true / then" (_context.LabelGen.NameOf(true_label)))
           .Append(true_branch)
           .AppendLine(sprintf "%s: # done" (_context.LabelGen.NameOf(done_label)))
           .Nop()
        
        _context.RegSet.Free(left_frag.Reg)
        _context.RegSet.Free(right_frag.Reg)
        
        asm
    
    
    and emit_eqop_with_branches (left_frag: AsmFragment)
                                (right_frag: AsmFragment)
                                (not_equal_branch: string)
                                (equal_branch: string)
                                : StringBuilder =

        let not_equal_label = _context.LabelGen.Generate()
        let done_label = _context.LabelGen.Generate()
        
        let asm =
            StringBuilder()
                .AppendLine("    # eq op / if")
                .Append(left_frag.Asm.ToString())
                .Append(right_frag.Asm.ToString())
                .AppendLine("    pushq %r10")
                .AppendLine("    pushq %r11")
                .AppendLine(sprintf "    movq %s, %%rdi" (_context.RegSet.NameOf(left_frag.Reg)))
                .AppendLine(sprintf "    movq %s, %%rsi" (_context.RegSet.NameOf(right_frag.Reg)))
                .AppendLine("    call .Runtime.are_equal")
                .AppendLine("    popq %r11")
                .AppendLine("    popq %r10")
                .AppendLine("    cmpq $0, 24(%rax)")
                .AppendLine(sprintf "    je %s" (_context.LabelGen.NameOf(not_equal_label)))
                .AppendLine("# equal")
                .Append(equal_branch)
                .AppendLine(sprintf "    jmp %s" (_context.LabelGen.NameOf(done_label)))
                .AppendLine(sprintf "%s: # not equal" (_context.LabelGen.NameOf(not_equal_label)))
                .Append(not_equal_branch)
                .AppendLine(sprintf "%s: # done" (_context.LabelGen.NameOf(done_label)))
            
        _context.RegSet.Free(left_frag.Reg)
        _context.RegSet.Free(right_frag.Reg)
        
        asm
    
    
    member this.TranslateBlock(block_syntax_opt: BlockSyntax voption): Res<AsmFragment> =
        match block_syntax_opt with
        | ValueNone ->
            let result_reg = _context.RegSet.Allocate()
            let asm =
                StringBuilder()
                    .AppendLine(sprintf "    movq $Unit_value, %s" (_context.RegSet.NameOf(result_reg)))
            
            Ok { AsmFragment.Asm = asm
                 Type = BasicClasses.Unit
                 Reg = result_reg }
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
                        _context.RegSet.Free(var_frag.Value.Reg)
                        sb_asm.Append(var_frag.Value.Asm).Nop()
                        
                | StmtSyntax.Expr expr_syntax ->
                    let expr_frag = translate_expr (stmt_node.Map(fun _ -> expr_syntax))
                    if expr_frag.IsOk
                    then
                        _context.RegSet.Free(expr_frag.Value.Reg)
                        sb_asm.Append(expr_frag.Value.Asm).Nop()
            )
            
            let expr_frag = translate_expr block_syntax.Expr
            
            _sym_table.LeaveBlock()
            
            if expr_frag.IsError
            then
                Error
            else
                
            sb_asm.Append(expr_frag.Value.Asm).Nop()
            Ok { AsmFragment.Asm = sb_asm
                 Type = expr_frag.Value.Type
                 Reg = expr_frag.Value.Reg }
        
        
    member this.AddrOf(sym: Symbol)
        : struct {| Asm: string voption
                    Addr: string
                    Type: ClassSymbol
                    Reg: Reg |} =
        match sym.Kind with
        | SymbolKind.Formal ->
            if sym.Index <= 6
            then
                {| Addr = sprintf "-%d(%%rbp)" ((sym.Index + 1) * 8)
                   Asm = ValueNone
                   Type = _context.ClassSymMap.[sym.Type]
                   Reg = Reg.Null |}
            else
                let actuals_out_of_frame_offset = 16 // skip saved %rbp, and return addr
                {| Addr = sprintf "%d(%%rbp)" (actuals_out_of_frame_offset + (sym.Index - 7) * 8)
                   Asm = ValueNone
                   Type = _context.ClassSymMap.[sym.Type]
                   Reg = Reg.Null |}
                
        | SymbolKind.Var ->
            let actuals_in_frame_count = if _sym_table.MethodFrame.ActualsCount >= 6
                                         then 6
                                         else _sym_table.MethodFrame.ActualsCount
            let vars_offset = actuals_in_frame_count * 8
            {| Addr = sprintf "-%d(%%rbp)" (vars_offset + (sym.Index + 1) * 8)
               Asm = ValueNone
               Type = _context.ClassSymMap.[sym.Type]
               Reg = Reg.Null |}
            
        | SymbolKind.Attr ->
            let attrs_offset_in_quads = 24 // skip tag, obj_size, and vtable_ptr
            let this_reg = _context.RegSet.Allocate()
            {| Addr = sprintf "%d(%s)"
                              ((attrs_offset_in_quads + sym.Index) * 8)
                              (_context.RegSet.NameOf(this_reg))
               Asm = ValueSome (sprintf "    movq -8(%%rbp), %s\n" (_context.RegSet.NameOf(this_reg)))
               Type = _context.ClassSymMap.[sym.Type]
               Reg = this_reg |}


    member this.Translate(expr_node: AstNode<ExprSyntax>): Res<AsmFragment> =
        translate_expr expr_node
