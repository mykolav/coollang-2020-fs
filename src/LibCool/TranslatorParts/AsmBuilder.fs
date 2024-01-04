namespace LibCool.TranslatorParts


open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Text
open LibCool.AstParts
open LibCool.SemanticParts
open LibCool.SharedParts
open LibCool.SourceParts


[<Sealed>]
type AsmBuilder(_context: TranslationContext) =
    
    
    let _asm = StringBuilder()
    let _indent = "    "
    
    
    let _pushed_caller_saved_regs = Stack<string>()
    
    
    member val Context = _context with get
    
    
    member this.Paste(asm: string): AsmBuilder =
        _asm.Append(asm).AsUnit()
        this


    member this.Directive(directive: string, comment: string option): AsmBuilder =
        _asm.Append(_indent)
            .Append(directive)
            .AsUnit()

        this.Ln(
            ?line_len=Some(_indent.Length + directive.Length),
            ?comment=comment)


    member this.Directive(directive: string, value: obj, ?comment: string): AsmBuilder =
        this.Directive(String.Format(directive, value), comment=comment)


    // Instr[uction]
    member this.Instr(instruction: string, comment: string option): AsmBuilder =
        _asm.Append(_indent)
            .Append(instruction)
            .AsUnit()

        this.Ln(
            ?line_len=Some(_indent.Length + instruction.Length),
            ?comment=comment)

        
    // Instr[uction]
    member this.Instr(instruction: string, value: obj, ?comment: string): AsmBuilder =
        this.Instr(String.Format(instruction, value), comment=comment)

        
    // Instr[uction]
    member this.Instr(instruction: string, reg: Reg, ?comment: string): AsmBuilder =
        this.Instr(String.Format(instruction, _context.RegSet.NameOf(reg)), comment=comment)


    // Instr[uction]
    member this.Instr(instruction: string, value: obj, reg: Reg, ?comment: string): AsmBuilder =
        this.Instr(String.Format(instruction, value, _context.RegSet.NameOf(reg)), comment=comment)


    // Instr[uction]
    member this.Instr(instruction: string, value0: obj, value1: obj, ?comment: string): AsmBuilder =
        this.Instr(String.Format(instruction, value0, value1), comment=comment)


    // Instr[uction]
    member this.Instr(instruction: string, value: obj, reg0: Reg, reg1: Reg, ?comment: string): AsmBuilder =
        this.Instr(String.Format(instruction,
                              value,
                              _context.RegSet.NameOf(reg0),
                              _context.RegSet.NameOf(reg1)),
                comment)


    // Instr[uction]
    member this.Instr(instruction: string, reg0: Reg, value: obj, reg1: Reg, ?comment: string): AsmBuilder =
        this.Instr(String.Format(instruction,
                              _context.RegSet.NameOf(reg0),
                              value,
                              _context.RegSet.NameOf(reg1)),
                comment)


    // Instr[uction]
    member this.Instr(instruction: string, reg: Reg, value: obj, ?comment: string): AsmBuilder =
        this.Instr(String.Format(instruction, _context.RegSet.NameOf(reg), value), comment=comment)


    // Instr[uction]
    member this.Instr(instruction: string, src: Reg, dst: Reg, ?comment: string): AsmBuilder =
        this.Instr(String.Format(instruction, _context.RegSet.NameOf(src), _context.RegSet.NameOf(dst)),
                comment)


    // Instr[uction]
    member this.Instr(instruction: string, src: Reg, dst: AddrFragment, ?comment: string): AsmBuilder =
        if dst.Asm.IsSome
        then
            _asm.Append(dst.Asm.Value)
                .AsUnit()
        
        this.Instr(instruction, src, dst.Addr, ?comment=comment)


    // Instr[uction]
    member this.Instr(instruction: string, src: AddrFragment, dst: Reg, ?comment: string): AsmBuilder =
        if src.Asm.IsSome
        then
            _asm.Append(src.Asm.Value)
                .AsUnit()
        
        this.Instr(instruction, src.Addr, dst, ?comment=comment)


    member this.Single(instruction: string, value: obj, reg: Reg, ?comment: string): string =
        this.Instr(String.Format(instruction, value, _context.RegSet.NameOf(reg)), comment=comment).ToString()

        
    member this.Addr(instruction: string, value: obj): string =
        String.Format(instruction, value)

        
    member this.Addr(instruction: string, value: obj, reg: Reg): string =
        String.Format(instruction, value, _context.RegSet.NameOf(reg))


    member this.Jmp(jmp: string, label: Label, ?comment: string) =
        this.Instr(String.Format("{0}    {1}", jmp, _context.LabelGen.NameOf(label)), comment=comment)


    member this.Jmp(label: Label, ?comment: string) =
        this.Jmp("jmp ", label, ?comment=comment)


    member this.Je(label: Label, ?comment: string) =
        this.Jmp("je  ", label, ?comment=comment)


    member this.Jne(label: Label, ?comment: string) =
        this.Jmp("jne ", label, ?comment=comment)
        
        
    member this.Label(label: Label, ?comment: string) =
        let label_name = _context.LabelGen.NameOf(label)
        _asm.AppendFormat("{0}:", label_name)
            .AsUnit()
        this.Ln(?comment=comment, line_len=label_name.Length + 1)
        
        
    member this.Label(label: string) =
        _asm.AppendFormat("{0}:", label)
            .AsUnit()
        this.Ln()
        
        
     member this.Location(offset: uint32, ?length: uint32) =
         if offset = UInt32.MaxValue
         then
             this
         else
             let max_length = 20u
             
             let length, shortified =
                 match length with
                 | None        -> max_length, true
                 | Some length -> if length <= max_length
                                  then length, false
                                  else max_length, true
                                  
             let location = _context.Source.Map(offset)
             
             let slice_start = offset
             // A slice is a closed interval, so we subtract one
             // to make `slice_end` point to the last char instead of past it.
             let slice_end = offset + length - 1u
                            
             let code_slice = _context.Source[slice_start .. slice_end]
                                             .Replace("\r", "")
                                             .Replace("\n", " \\n ")
             let code_slice = if shortified
                              then code_slice + " ..."
                              else code_slice
 
             this.Comment(String.Format("{0}({1},{2}): {3}",
                                   location.FileName,
                                   location.Line,
                                   location.Col,
                                   code_slice))        
        
        
    member this.Location(span: Span) =
        this.Location(span.First, span.Last - span.First)


    member this.Comment(comment: string) =
        _asm.Append(_indent)
            .AppendFormat("# {0}", comment)
            .AppendLine()
            .AsUnit()
        this


    member this.RtAbortMatch(location: Location, expr_reg: Reg): AsmBuilder =
        let filename_str_const = _context.StrConsts.GetOrAdd(location.FileName)
        this.Instr("movq    ${0}, %rdi", value=filename_str_const, comment="file name")
            .Instr("movq    ${0}, %rsi", value=location.Line, comment="line")
            .Instr("movq    ${0}, %rdx", value=location.Col, comment="col")
            .Instr("movq    {0}, %rcx", expr_reg, comment="match value")
            .Instr("call    {0}", RtNames.RtAbortMatch)


    member this.RtAbortDispatch(location: Location): AsmBuilder =
        let filename_str_const = _context.StrConsts.GetOrAdd(location.FileName)
        this.Instr("movq    ${0}, %rdi", filename_str_const, comment="file name")
            .Instr("movq    ${0}, %rsi", location.Line, comment="line")
            .Instr("movq    ${0}, %rdx", location.Col, comment="col")
            .Instr("call    {0}", RtNames.RtAbortDispatch)


    member this.IntGetOrCreate(): AsmBuilder =
        this.PushCallerSavedRegs()
            .Instr("call    {0}", RtNames.IntGetOrCreate)
            .PopCallerSavedRegs()


    member this.RtCopyObject(proto_reg: Reg, copy_reg: Reg): AsmBuilder =
        this.RtCopyObject(proto=_context.RegSet.NameOf(proto_reg),
                          copy_reg=copy_reg)
    
    
    member this.RtCopyObject(proto: string, copy_reg: Reg): AsmBuilder =
        this.PushCallerSavedRegs()
            .Instr("movq    {0}, %rdi", proto)
            .Instr("call    {0}", RtNames.RtCopyObject)
            .PopCallerSavedRegs()
            .Instr("movq    %rax, {0}", copy_reg)
            
            
    member this.RtAreEqual(left_reg: Reg, right_reg: Reg): AsmBuilder =
        this.PushCallerSavedRegs()
            .Instr("movq    {0}, %rdi", left_reg, comment="left")
            .Instr("movq    {0}, %rsi", right_reg, comment="right")
            .Instr("call    {0}", RtNames.RtAreEqual)
            .PopCallerSavedRegs()
        
        
    member this.StringConcatOp(concat_span: Span,
                               left_str_reg: Reg,
                               right_str_reg: Reg,
                               result_reg: Reg)
                              : AsmBuilder =
        
        // Calling `String.concat` directly behaves as a normal method call.
        // But in Scala, `left_str + right_str` never fails, and instead
        // replaces `null` operand(s) with `"null"` string literal.
        // We choose to be consistent with Scala.
        let left_str_is_some_label = this.Context.LabelGen.Generate("LEFT_STR_IS_SOME")
        let right_str_is_some_label = this.Context.LabelGen.Generate("RIGHT_STR_IS_SOME")
        
        this.Instr("cmpq    $0, {0}", left_str_reg)
            .Jne(left_str_is_some_label)
            .Instr("movq    ${0}, {1}", _context.StrConsts.GetOrAdd("null"),
                                        left_str_reg)
            .Label(left_str_is_some_label)
            .Instr("cmpq    $0, {0}", right_str_reg)
            .Jne(right_str_is_some_label)
            .Instr("movq    ${0}, {1}", _context.StrConsts.GetOrAdd("null"),
                                        right_str_reg)
            .Label(right_str_is_some_label)
            .PushCallerSavedRegs()
            .Instr("movq    {0}, %rdi", left_str_reg, "left str")
            .Instr("movq    {0}, %rsi", right_str_reg, "right str")
            .Instr("call    {0}", RtNames.StringConcat)
            .PopCallerSavedRegs()
            .Instr("movq    %rax, {0}", result_reg)
    

    member this.GenGCHandleAssign(dest_addr_frag: AddrFragment): AsmBuilder =
        this.PushCallerSavedRegs()
            .Instr("leaq    {0}, %rdi", dest_addr_frag.Addr)
            .Instr("call    {0}", RtNames.GenGCHandleAssign)
            .PopCallerSavedRegs()


    member this.PushCallerSavedRegs(): AsmBuilder =
        for reg in SysVAmd64AbiFacts.CallerSavedRegs do
            match _context.RegSet.AllocatedBy(reg) with
            | ValueSome allocatedBy ->
                this.Instr("pushq   {0}", reg, ?comment=Some $"allocated by %s{allocatedBy}").AsUnit()
                _pushed_caller_saved_regs.Push(reg)
            | ValueNone ->
                ()
        // TODO: If the number of pushed regs is odd,
        //       should `subq $8, %rsp` to align the stack by 16 bytes?
        this
            
            
    member this.PopCallerSavedRegs(): AsmBuilder =
        // TODO: If the number of pushed regs was odd,
        //       should `addq $8, %rsp` to compensate
        //       for padding the stack to 16 bytes boundary?
        while _pushed_caller_saved_regs.Count > 0 do
            let reg = _pushed_caller_saved_regs.Pop()
            this.Instr("popq    {0}", reg, ?comment=None).AsUnit()
                
        _pushed_caller_saved_regs.Clear()
        this
        
        
    // Line end
    member this.Ln(?comment: string, ?line_len: int): AsmBuilder =
        if comment.IsSome
        then
            let left_pad = match line_len with
                           | None -> " "
                           | Some line_len ->
                               let comment_col = 50;
                               String(' ', Math.Max(comment_col - line_len, 1))
            _asm.AppendFormat("{0}# {1}", left_pad, comment.Value.ToString())
                .AsUnit()
                
        _asm.AppendLine()
            .AsUnit()
        this
        
        
    override this.ToString() : string = _asm.ToString()
    member this.AsUnit() : unit = ()


module AsmFragments =
    
    
    [<Struct; IsReadOnly>]
    type PatternAsmInfo =
        { Label: Label
          Tag: int }


    type AsmBuilder with


        member private this.MethodPrologue(frame: FrameInfo): AsmBuilder =
            this.Instr("pushq   %rbp", None)
                .Instr("movq    %rsp, %rbp", None)
                .Instr("subq    ${0}, %rsp", frame.FrameSize + frame.PadSize)
                .Comment("store actuals on the stack")
                .AsUnit()

            for i = 0 to (frame.ActualsInFrameCount - 1) do
                this.Instr("movq    {0}, -{1}(%rbp)", SysVAmd64AbiFacts.ActualRegs[i],
                                                      FrameLayoutFacts.Actuals + (i + 1) * FrameLayoutFacts.ElemSize)
                    .AsUnit()

            this.Comment("store callee-saved regs on the stack")
                .AsUnit()

            for i = 0 to (SysVAmd64AbiFacts.CalleeSavedRegs.Length - 1) do
                this.Instr("movq    {0}, -{1}(%rbp)", SysVAmd64AbiFacts.CalleeSavedRegs[i],
                                                      frame.CalleeSavedRegs + (i + 1) * FrameLayoutFacts.ElemSize)
                    .AsUnit()

            this


        member private this.MethodEpilogue(frame: FrameInfo): AsmBuilder =
            this.Comment("restore callee-saved regs")
                .AsUnit()

            for i = 0 to (SysVAmd64AbiFacts.CalleeSavedRegs.Length - 1) do
                this.Instr("movq    -{0}(%rbp), {1}", value0=frame.CalleeSavedRegs + (i + 1) * FrameLayoutFacts.ElemSize,
                                                      value1=SysVAmd64AbiFacts.CalleeSavedRegs[i])
                    .AsUnit()

            this.Comment("restore the caller's frame")
                .Instr("movq    %rbp, %rsp", None)
                .Instr("popq    %rbp", None)
                .Instr("ret", None)


        member this.Method(method_name: string,
                           method_span: Span,
                           frame: FrameInfo,
                           body_frag: string): string =
            this.Ln()
                .Location(method_span)
                .Directive(".global {0}", method_name)
                .Label(method_name)
                .MethodPrologue(frame)
                .Paste(body_frag)
                .MethodEpilogue(frame)
                .ToString()


        member this.BoolNegation(bool_negation_span: Span, negated_frag: AsmFragment): string =
            let false_label = this.Context.LabelGen.Generate("NEG_FALSE")
            let endneg_label = this.Context.LabelGen.Generate("ENDNEG")

            this.Paste(negated_frag.Asm)
                .Location(bool_negation_span)
                .Instr("cmpq    $0, {0}({1})", ObjLayoutFacts.BoolValue, negated_frag.Reg)
                .Je(false_label, "false")
                .Comment("true:")
                .Instr("movq    ${0}, {1}", RtNames.BoolFalse, negated_frag.Reg)
                .Jmp(endneg_label, "done")
                .Label(false_label, "false")
                .Instr("movq    ${0}, {1}", RtNames.BoolTrue, negated_frag.Reg)
                .Label(endneg_label, "done")
                .ToString()


        member this.UnaryMinus(unary_minus_span: Span, negated_frag: AsmFragment): string =
            this.Paste(negated_frag.Asm)
                .Instr("movq    {0}({1}), %rdi", ObjLayoutFacts.IntValue, negated_frag.Reg)
                .Location(unary_minus_span)
                .Instr("negq    %rdi", comment=None)
                .IntGetOrCreate()
                .Instr("movq    %rax, {0}", negated_frag.Reg)
                .ToString()


        member this.If(if_span: Span,
                       condition_frag: AsmFragment,
                       then_asm: string,
                       else_asm: string): string =
            let else_label = this.Context.LabelGen.Generate("ELSE")
            let endif_label = this.Context.LabelGen.Generate("ENDIF")

            this.Paste(condition_frag.Asm)
                .Location(if_span)
                .Instr("cmpq    $0, {0}({1})", ObjLayoutFacts.BoolValue, condition_frag.Reg)
                .Je(else_label, "else")
                .Comment("then")
                .Paste(then_asm)
                .Jmp(endif_label)
                .Label(else_label, "else")
                .Paste(else_asm)
                .Label(endif_label)
                .ToString()


        member this.While(while_span: Span,
                          condition_frag: AsmFragment,
                          body_frag: AsmFragment,
                          result_reg: Reg): string =
            let while_cond_label = this.Context.LabelGen.Generate("WHILE_COND")
            let endwhile_label = this.Context.LabelGen.Generate("ENDWHILE")

            this.Label(while_cond_label)
                .Paste(condition_frag.Asm)
                .Location(while_span)
                .Instr("cmpq    $0, {0}({1})", ObjLayoutFacts.BoolValue, condition_frag.Reg)
                .Je(endwhile_label)
                .Paste(body_frag.Asm)
                .Jmp(while_cond_label)
                .Label(endwhile_label)
                .Instr("movq    ${0}, {1}", RtNames.UnitValue, result_reg)
                .ToString()


        member this.Cond(cond_frag: AsmFragment,
                         true_branch_asm: string,
                         false_branch_asm: string): string =
            let cond_false_label = this.Context.LabelGen.Generate("COND_IS_FALSE")
            let endcond_label = this.Context.LabelGen.Generate("ENDCOND")

            this.Paste(cond_frag.Asm)
                .Instr("cmpq    $0, {0}({1})", ObjLayoutFacts.BoolValue, cond_frag.Reg)
                .Je(cond_false_label)
                .Comment("condition is true")
                .Paste(true_branch_asm)
                .Jmp(endcond_label)
                .Label(cond_false_label)
                .Paste(false_branch_asm)
                .Label(endcond_label)
                .ToString()


        member this.Mul(mul_span: Span, left_frag: AsmFragment, right_frag: AsmFragment): string =
            this.Paste(left_frag.Asm)
                .Paste(right_frag.Asm)
                // left = left * right
                .Instr("movq    {0}({1}), %rax", ObjLayoutFacts.IntValue, left_frag.Reg)
                .Location(mul_span)
                .Instr("imulq   {0}({1})", ObjLayoutFacts.IntValue, right_frag.Reg)
                .Instr("movq    %rax, %rdi", comment=None)
                .IntGetOrCreate()
                .Instr("movq    %rax, {0}", left_frag.Reg)
                .ToString()


        member this.Div(div_span: Span, left_frag: AsmFragment, right_frag: AsmFragment): string =
            this.Paste(left_frag.Asm)
                .Paste(right_frag.Asm)
                // left = left / right
                .Instr("movq    {0}({1}), %rax", ObjLayoutFacts.IntValue, left_frag.Reg)
                .Instr("cqto", comment=Some "sign-extend %rax to %rdx:%rax")
                .Location(div_span)
                .Instr("idivq    {0}({1})", ObjLayoutFacts.IntValue, right_frag.Reg)
                .Instr("movq    %rax, %rdi", comment=None)
                .IntGetOrCreate()
                .Instr("movq    %rax, {0}", left_frag.Reg)
                .ToString()


        member this.Sum(sum_span: Span, left_frag: AsmFragment, right_frag: AsmFragment): string =
            this.Paste(left_frag.Asm)
                .Paste(right_frag.Asm)
                .AsUnit()

            if left_frag.Type.Is(BasicClasses.Int) &&
               right_frag.Type.Is(BasicClasses.Int)
            then // left = left + right
                this.Instr("movq    {0}({1}), %rdi", ObjLayoutFacts.IntValue, left_frag.Reg)
                    .Location(sum_span)
                    .Instr("addq    {0}({1}), %rdi", ObjLayoutFacts.IntValue, right_frag.Reg)
                    .IntGetOrCreate()
                    .Instr("movq    %rax, {0}", left_frag.Reg)
                    .ToString()
            else // string concatenation
                this.StringConcatOp(sum_span, left_frag.Reg, right_frag.Reg, result_reg=left_frag.Reg)
                    .ToString()


        member this.Sub(sub_span: Span, left_frag: AsmFragment, right_frag: AsmFragment): string =
            this.Paste(left_frag.Asm)
                .Paste(right_frag.Asm)
                // left = left - right
                .Instr("movq    {0}({1}), %rdi", ObjLayoutFacts.IntValue, left_frag.Reg)
                .Location(sub_span)
                .Instr("subq    {0}({1}), %rdi", ObjLayoutFacts.IntValue, right_frag.Reg)
                .IntGetOrCreate()
                .Instr("movq    %rax, {0}", left_frag.Reg)
                .ToString()


        member this.Match(match_span: Span,
                          expr_frag: AsmFragment,
                          expr_location: Location,
                          frame: FrameInfo,
                          tag_reg: Reg,
                          pattern_asm_infos: IReadOnlyDictionary<TYPENAME, PatternAsmInfo>)
                         : AsmBuilder =

            let match_init_label = this.Context.LabelGen.Generate("MATCH_INIT")
            let is_tag_valid_label = this.Context.LabelGen.Generate("IS_TAG_VALID")
            let try_match_label = this.Context.LabelGen.Generate("TRY_MATCH")

            this.Paste(expr_frag.Asm)
                .Comment("handle null")
                .Location(match_span)
                .Instr("cmpq    $0, {0}", expr_frag.Reg)
                .Jne(match_init_label)
                .AsUnit()

            if pattern_asm_infos.ContainsKey(BasicClassNames.Null)
            then
                let null_pattern_asm_info = pattern_asm_infos[BasicClassNames.Null]
                this.Jmp(null_pattern_asm_info.Label)
                    .AsUnit()
            else
                this.Comment("abort if the match value is null")
                    .RtAbortMatch(expr_location, expr_reg=expr_frag.Reg)
                    .AsUnit()

            this.Label(match_init_label)
                .AsUnit()

            if pattern_asm_infos |> Seq.exists (fun it -> it.Key <> BasicClassNames.Null)
            then
                // Store the expression's value on stack,
                // such that a var introduced by a matched case would pick it up.
                this.Instr("movq    {0}, -{1}(%rbp)",
                        expr_frag.Reg,
                        frame.Vars + (frame.VarsCount + 1) * 8,
                        comment="the match expression's value")
                    .AsUnit()

            this.Instr("movq    ({0}), {1}", expr_frag.Reg, tag_reg, comment="the match expression type's tag")
                .Label(is_tag_valid_label, "is the inheritance chain's end reached and still no match?")
                .Instr("cmpq    $-1, {0}", tag_reg)
                .Jne(try_match_label)
                .RtAbortMatch(expr_location, expr_reg=expr_frag.Reg)
                .Label(try_match_label)
                .AsUnit()

            for pattern_asm_info in pattern_asm_infos do
                // We already emitted asm for 'null'. Don't try to do it again.
                if pattern_asm_info.Key <> BasicClassNames.Null
                then
                    this.Instr("cmpq    ${0}, {1}", pattern_asm_info.Value.Tag, tag_reg)
                        .Je(pattern_asm_info.Value.Label)
                        .AsUnit()

            this.Instr("salq    $3, {0}", tag_reg, "multiply by 8")
                .Instr("movq    {0}({1}), {2}", RtNames.ClassParentMap, tag_reg, tag_reg, "the parent's tag")
                .Jmp(is_tag_valid_label, "check if the inheritance chain's end is reached")


        member this.MatchCase(case_span: Span,
                              case_label: Label,
                              block_frag: AsmFragment,
                              result_reg: Reg,
                              done_label: Label): unit =
            this.Label(case_label)
                .Paste(block_frag.Asm)
                .Location(case_span)
                .Instr("movq    {0}, {1}", block_frag.Reg, result_reg)
                .Jmp(done_label)
                .AsUnit()


        member this.BeginDispatch(dispatch_span: Span): AsmBuilder =
            this.Location(dispatch_span)
                .PushCallerSavedRegs()


        member this.CompleteDispatch(dispatch_span: Span,
                                     receiver_frag: AsmFragment,
                                     actuals_asm: string,
                                     method_reg: Reg,
                                     method_sym: MethodSymbol,
                                     actuals_count: int,
                                     result_reg: Reg): unit =

            let receiver_is_some_label = this.Context.LabelGen.Generate("RECEIVER_IS_SOME")

            this.Comment("actual #0")
                .Paste(receiver_frag.Asm)
                .Instr("cmpq    $0, {0}", receiver_frag.Reg)
                .Jne(receiver_is_some_label)
                .Comment("abort if the receiver is null")
                .RtAbortDispatch(this.Context.Source.Map(dispatch_span.First))
                .Label(receiver_is_some_label)
                .Paste(actuals_asm)
                .Instr("movq    {0}(%rdi), {1}", ObjLayoutFacts.VTable,
                                              method_reg,
                                              comment=receiver_frag.Type.Name.ToString() + "_VTABLE")
                .Instr("movq    {0}({1}), {2}", method_sym.Index * MemLayoutFacts.VTableEntrySize,
                                             method_reg,
                                             method_reg,
                                             comment=receiver_frag.Type.Name.ToString() + "." + method_sym.Name.ToString())
                .Location(dispatch_span)
                .Instr("call    *{0}", method_reg)
                .RemoveActualsFromStack(actuals_count)
                .PopCallerSavedRegs()
                .Instr("movq    %rax, {0}", result_reg, "returned value")
                .AsUnit()


        member this.BeginSuperDispatch(super_dispatch_span: Span): AsmBuilder =
            this.BeginDispatch(super_dispatch_span)


        member this.CompleteSuperDispatch(super_dispatch_span: Span,
                                          this_frag: AsmFragment,
                                          actuals_asm: string,
                                          method_sym: MethodSymbol,
                                          result_reg: Reg,
                                          actuals_count: int): unit =
            this.Comment("actual #0")
                .Paste(this_frag.Asm)
                .Paste(actuals_asm)
                .Location(super_dispatch_span)
                .Instr("call    {0}.{1}", method_sym.DeclaringClass,
                                       method_sym.Name,
                                       comment="super." + method_sym.Name.ToString())
                .Instr("movq    %rax, {0}", result_reg, comment="returned value")
                .RemoveActualsFromStack(actuals_count)
                .PopCallerSavedRegs()
                .AsUnit()


        member this.BeginNew(new_span: Span): AsmBuilder =
            this.BeginDispatch(new_span)


        member this.CompleteNew(ty: ClassSymbol,
                                this_reg: Reg,
                                actuals_asm: string,
                                actuals_count: int,
                                result_reg: Reg): unit =
            if ty.Is(BasicClasses.ArrayAny)
            then
                // Set 'this_reg' to 0.
                // As the size of array in passed to the ctor of 'ArrayAny',
                // it doesn't use an object copied from a prototype.
                // Instead it will allocate memory and create an 'ArrayAny' object there itself.
                this.Comment("ArrayAny..ctor will allocate memory for N items")
                    .Instr("xorq    {0}, {1}", this_reg, this_reg)
                    .AsUnit()
            else
                // Copy the relevant prototype and place a pointer to the copy in 'this_reg'.
                this.RtCopyObject(proto="$" + ty.Name.ToString() + "_PROTO_OBJ",
                                  copy_reg=this_reg)
                   .AsUnit()

            // `actuals_asm` contains `movq    $this_reg, %rdi`
            this.Paste(actuals_asm)
                .Instr("call    {0}..ctor", ty.Name)
                .RemoveActualsFromStack(actuals_count)
                .PopCallerSavedRegs()
                .Instr("movq    %rax, {0}", result_reg, comment="the new object")
                .AsUnit()


        member private this.RemoveActualsFromStack(actuals_count: int): AsmBuilder =
            // We only have (ActualRegs.Length - 1) registers to store actuals,
            // as we always use %rdi to store `this`.
            let actual_on_stack_count = actuals_count - (SysVAmd64AbiFacts.ActualRegs.Length - 1)
            if actual_on_stack_count <= 0
            then
                this
            else

            this.Instr("addq    ${0}, %rsp", actual_on_stack_count * FrameLayoutFacts.ElemSize,
                                             comment="remove " +
                                                     actual_on_stack_count.ToString() +
                                                     " actual(s) from stack")


        member this.BeginActuals(method_id_span: Span, actuals_count, this_reg: Reg): AsmBuilder =
            this.Location(method_id_span.Last)
                .Instr("subq    ${0}, %rsp", (actuals_count + (*this*)1) * FrameLayoutFacts.ElemSize)
                .Instr("movq    {0}, 0(%rsp)", this_reg, comment="actual #0")


        member this.Actual(actual_index: int, actual_frag: AsmFragment): unit =
            let comment = String.Format("actual #{0}", actual_index + 1)
            this.Comment(comment)
                .Paste(actual_frag.Asm)
                .Instr("movq    {0}, {1}(%rsp)", actual_frag.Reg,
                                              ((actual_index + 1) * FrameLayoutFacts.ElemSize),
                                              comment=comment)
                .AsUnit()


        member this.LoadActualsIntoRegs(actuals_count: int): unit =
            this.Comment("load up to 6 first actuals into regs")
                .AsUnit()

            // We store `this` in %rdi, and as a result can only pass 5 actuals in registers.
            let actual_in_reg_count = if (actuals_count + 1) > SysVAmd64AbiFacts.ActualRegs.Length
                                      then SysVAmd64AbiFacts.ActualRegs.Length
                                      else actuals_count + 1 // Add one, to account for passing 'this' as the actual #0.

            for actual_index = 0 to (actual_in_reg_count - 1) do
                this.Instr("movq    {0}(%rsp), {1}", value0=actual_index * FrameLayoutFacts.ElemSize,
                                                  value1=SysVAmd64AbiFacts.ActualRegs[actual_index])
                    .AsUnit()

            this.Comment("remove the register-loaded actuals from stack")
                .Instr("addq    ${0}, %rsp", actual_in_reg_count * FrameLayoutFacts.ElemSize)
                .AsUnit()


        member this.CmpOp(cmpop_span: Span,
                          left_frag: AsmFragment,
                          right_frag: AsmFragment,
                          jmp: string,
                          false_branch_asm: string,
                          true_branch_asm: string)
                          : string =

            let true_label = this.Context.LabelGen.Generate("CMP_TRUE")
            let done_label = this.Context.LabelGen.Generate("ENDCMP")

            this.Paste(left_frag.Asm)
                .Paste(right_frag.Asm)
                .Instr("movq    {0}({1}), {2}", ObjLayoutFacts.IntValue, left_frag.Reg, left_frag.Reg)
                .Instr("movq    {0}({1}), {2}", ObjLayoutFacts.IntValue, right_frag.Reg, right_frag.Reg)
                .Location(cmpop_span)
                .Instr("cmpq    {0}, {1}", right_frag.Reg, left_frag.Reg)
                .Jmp(jmp, true_label, "true branch")
                .Comment("false branch")
                .Paste(false_branch_asm)
                .Jmp(done_label, "done")
                .Label(true_label, "true branch")
                .Paste(true_branch_asm)
                .Label(done_label, "done")
                .ToString()


        member this.EqOp(eqop_span: Span,
                         left_frag: AsmFragment,
                         right_frag: AsmFragment,
                         unequal_branch_asm: string,
                         equal_branch_asm: string)
                         : string =

            let equal_label = this.Context.LabelGen.Generate("EQUAL")
            let done_label = this.Context.LabelGen.Generate("ENDEQ")

            this.Paste(left_frag.Asm)
                .Paste(right_frag.Asm)
                .Comment("are pointers equal?")
                .Instr("cmpq    {0}, {1}", right_frag.Reg, left_frag.Reg)
                .Je(equal_label, "equal")
                .RtAreEqual(left_reg=left_frag.Reg, right_reg=right_frag.Reg)
                .Instr("movq    {0}(%rax), %rax", ObjLayoutFacts.BoolValue)
                .Location(eqop_span)
                .Instr("cmpq    $0, %rax", comment=None)
                .Jne(equal_label, "equal")
                .Comment("unequal")
                .Paste(unequal_branch_asm)
                .Jmp(done_label, "done")
                .Label(equal_label, "equal")
                .Paste(equal_branch_asm)
                .Label(done_label, "done")
                .ToString()
