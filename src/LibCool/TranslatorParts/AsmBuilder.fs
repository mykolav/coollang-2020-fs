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
        _asm.Append(asm).Nop()
        this

        
    // In[struction]
    member this.In(instruction: string, comment: string option): AsmBuilder =
        _asm.Append(_indent)
            .Append(instruction)
            .Nop()
        this.Ln(?comment=comment)

        
    // In[struction]
    member this.In(instruction: string, value: obj, ?comment: string): AsmBuilder =
        this.In(String.Format(instruction, value), comment=comment)

        
    // In[struction]
    member this.In(instruction: string, reg: Reg, ?comment: string): AsmBuilder =
        this.In(String.Format(instruction, _context.RegSet.NameOf(reg)), comment=comment)


    // In[struction]
    member this.In(instruction: string, value: obj, reg: Reg, ?comment: string): AsmBuilder =
        this.In(String.Format(instruction, value, _context.RegSet.NameOf(reg)), comment=comment)


    // In[struction]
    member this.In(instruction: string, value0: obj, value1: obj, ?comment: string): AsmBuilder =
        this.In(String.Format(instruction, value0, value1), comment=comment)


    // In[struction]
    member this.In(instruction: string, value: obj, reg0: Reg, reg1: Reg, ?comment: string): AsmBuilder =
        this.In(String.Format(instruction,
                              value,
                              _context.RegSet.NameOf(reg0),
                              _context.RegSet.NameOf(reg1)),
                comment)


    // In[struction]
    member this.In(instruction: string, reg0: Reg, value: obj, reg1: Reg, ?comment: string): AsmBuilder =
        this.In(String.Format(instruction,
                              _context.RegSet.NameOf(reg0),
                              value,
                              _context.RegSet.NameOf(reg1)),
                comment)


    // In[struction]
    member this.In(instruction: string, reg: Reg, value: obj, ?comment: string): AsmBuilder =
        this.In(String.Format(instruction, _context.RegSet.NameOf(reg), value), comment=comment)


    // In[struction]
    member this.In(instruction: string, src: Reg, dst: Reg, ?comment: string): AsmBuilder =
        this.In(String.Format(instruction, _context.RegSet.NameOf(src), _context.RegSet.NameOf(dst)),
                comment)


    // In[struction]
    member this.In(instruction: string, src: Reg, dst: AddrFragment, ?comment: string): AsmBuilder =
        if dst.Asm.IsSome
        then
            _asm.Append(dst.Asm.Value)
                .Nop()
        
        this.In(instruction, src, dst.Addr, ?comment=comment)    


    // In[struction]
    member this.In(instruction: string, src: AddrFragment, dst: Reg, ?comment: string): AsmBuilder =
        if src.Asm.IsSome
        then
            _asm.Append(src.Asm.Value)
                .Nop()
        
        this.In(instruction, src.Addr, dst, ?comment=comment)    


    member this.Single(instruction: string, value: obj, reg: Reg, ?comment: string): string =
        this.In(String.Format(instruction, value, _context.RegSet.NameOf(reg)), comment=comment).ToString()

        
    member this.Addr(instruction: string, value: obj): string =
        String.Format(instruction, value)

        
    member this.Addr(instruction: string, value: obj, reg: Reg): string =
        String.Format(instruction, value, _context.RegSet.NameOf(reg))


    member this.Jmp(jmp: string, label: Label, comment: string) =
        this.In(String.Format("{0}    {1}", jmp, _context.LabelGen.NameOf(label)), comment=Some comment)


    member this.Jmp(label: Label, comment: string) =
        this.Jmp("jmp ", label, comment)


    member this.Je(label: Label, comment: string) =
        this.Jmp("je  ", label, comment)


    member this.Jne(label: Label, comment: string) =
        this.Jmp("jne ", label, comment)
        
        
    member this.Label(label: Label, comment: string) =
        _asm.AppendFormat("{0}:", _context.LabelGen.NameOf(label))
            .Nop()
        this.Ln(comment)
        
        
    member this.Label(label: string) =
        _asm.AppendFormat("{0}:", label)
            .Nop()
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
                            
             let code_slice = _context.Source.[slice_start .. slice_end]
                                             .Replace("\r", "")
                                             .Replace("\n", " \\n ")
             let code_slice = if shortified
                              then code_slice + " ..."
                              else code_slice
 
             this.Comment(String.Format("# {0}({1},{2}): {3}",
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
            .Nop()
        this
        
        
    member this.RtAbortMatch(location: Location, expr_reg: Reg): AsmBuilder =
        let filename_label = _context.StrConsts.GetOrAdd(location.FileName)
        this.In("movq    ${0}, %rdi", value=filename_label)
            .In("movq    ${0}, %rsi", value=location.Line)
            .In("movq    ${0}, %rdx", value=location.Col)
            .In("movq    {0}, %rcx", expr_reg)
            .In("call    {0}", RtNames.RtAbortMatch)


    member this.RtAbortDispatch(location: Location): AsmBuilder =
        let filename_label = _context.StrConsts.GetOrAdd(location.FileName)
        this.In("movq    ${0}, %rdi", filename_label)
            .In("movq    ${0}, %rsi", location.Line)
            .In("movq    ${0}, %rdx", location.Col)
            .In("call    {0}", RtNames.RtAbortDispatch)
    
    
    member this.RtCopyObject(proto_reg: Reg, copy_reg: Reg): AsmBuilder =
        this.RtCopyObject(proto=_context.RegSet.NameOf(proto_reg),
                          copy_reg=copy_reg)
    
    
    member this.RtCopyObject(proto: string, copy_reg: Reg): AsmBuilder =
        this.PushCallerSavedRegs()
            .In("movq    {0}, %rdi", proto)
            .In("call    {0}", RtNames.RtCopyObject)
            .PopCallerSavedRegs()
            .In("movq    %rax, {0}", copy_reg)
            
            
    member this.RtAreEqual(left_reg: Reg, right_reg: Reg): AsmBuilder =
        this.PushCallerSavedRegs()
            .In("movq    {0}, %rdi", left_reg)
            .In("movq    {0}, %rsi", right_reg)
            .In("call    {0}", RtNames.RtAreEqual)
            .PopCallerSavedRegs()
        
        
    member this.StringConcat(str0_reg: Reg, str1_reg: Reg, result_reg: Reg): AsmBuilder =
        this.PushCallerSavedRegs()
            .In("movq    {0}, %rdi", str0_reg)
            .In("movq    {0}, %rsi", str1_reg)
            .In("call    {0}", RtNames.StringConcat)
            .PopCallerSavedRegs()
            .In("movq    %rax, {0}", result_reg)
    

    member this.PushCallerSavedRegs(): AsmBuilder =
        for reg in SysVAmd64AbiFacts.CallerSavedRegs do
            if _context.RegSet.IsAllocated(reg)
            then
                this.In("pushq   {0}", reg, ?comment=None).AsUnit()
                _pushed_caller_saved_regs.Push(reg)
        this
            
            
    member this.PopCallerSavedRegs(): AsmBuilder =
        while _pushed_caller_saved_regs.Count > 0 do
            let reg = _pushed_caller_saved_regs.Pop()
            this.In("popq    {0}", reg, ?comment=None).AsUnit()
                
        _pushed_caller_saved_regs.Clear()
        this
        
        
    // Line end
    member private this.Ln(?comment: string): AsmBuilder =
        if comment.IsSome
        then
            _asm.AppendFormat(" # {0}", comment.Value.ToString())
                .Nop()
                
        _asm.AppendLine()
            .Nop()
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
        this.In("pushq   %rbp", None)
            .In("movq    %rsp, %rbp", None)
            .In("subq    ${0}, %rsp", frame.FrameSize + frame.PadSize)
            .Comment("store actuals on the stack")
            .AsUnit()
        
        for i = 0 to (frame.ActualsInFrameCount - 1) do
            this.In("movq    {0}, -{1}(%rbp)", SysVAmd64AbiFacts.ActualRegs.[i],
                                               FrameLayoutFacts.Actuals + (i + 1) * FrameLayoutFacts.ElemSize)
                .AsUnit()
        
        this.Comment("store callee-saved regs on the stack")
            .AsUnit()

        for i = 0 to (SysVAmd64AbiFacts.CalleeSavedRegs.Length - 1) do
            this.In("movq    {0}, -{1}(%rbp)", SysVAmd64AbiFacts.CalleeSavedRegs.[i],
                                               frame.CalleeSavedRegs + (i + 1) * FrameLayoutFacts.ElemSize)
                .AsUnit()
               
        this
    
    
    member private this.MethodEpilogue(frame: FrameInfo): AsmBuilder =
        this.Comment("restore callee-saved regs")
            .AsUnit()

        for i = 0 to (SysVAmd64AbiFacts.CalleeSavedRegs.Length - 1) do
            this.In("movq    -{0}(%rbp), {1}", value0=frame.CalleeSavedRegs + (i + 1) * FrameLayoutFacts.ElemSize,
                                               value1=SysVAmd64AbiFacts.CalleeSavedRegs.[i])
                .AsUnit()

        this.Comment("restore the caller's frame")
            .In("movq    %rbp, %rsp", None)
            .In("popq    %rbp", None)
            .In("ret", None)


    member this.Method(method_name: string,
                       method_span: Span,
                       frame: FrameInfo,
                       body_frag: string): string =
        this.Location(method_span)
            .Label(method_name)
            .MethodPrologue(frame)
            .Paste(body_frag)
            .MethodEpilogue(frame)
            .ToString()


    member this.BoolNegation(bool_negation_span: Span, negated_frag: AsmFragment): string =
        let false_label = this.Context.LabelGen.Generate()
        let done_label = this.Context.LabelGen.Generate()
        
        this.Location(bool_negation_span)
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
    
    
    member this.UnaryMinus(unary_minus_span: Span, negated_frag: AsmFragment): string =
        this.Location(unary_minus_span)
            .Paste(negated_frag.Asm)
            .RtCopyObject(proto_reg=negated_frag.Reg, copy_reg=negated_frag.Reg)
            .In("negq    {0}({1})", ObjLayoutFacts.IntValue, negated_frag.Reg)
            .ToString()


    member this.If(if_span: Span,
                   condition_frag: AsmFragment,
                   then_asm: string,
                   else_asm: string): string =
        let else_label = this.Context.LabelGen.Generate()
        let done_label = this.Context.LabelGen.Generate()

        this.Location(if_span)
            .Paste(condition_frag.Asm)
            .In("cmpq    $0, {0}({1})", ObjLayoutFacts.BoolValue, condition_frag.Reg)
            .Je(else_label, "else")
            .Comment("then")
            .Paste(then_asm)
            .Jmp(done_label, "end if")
            .Label(else_label, "else")
            .Paste(else_asm)
            .Label(done_label, "end if")
            .ToString()


    member this.While(while_span: Span,
                      condition_frag: AsmFragment,
                      body_frag: AsmFragment,
                      result_reg: Reg): string =
        let while_cond_label = this.Context.LabelGen.Generate()
        let done_label = this.Context.LabelGen.Generate()

        this.Location(while_span)
            .Label(while_cond_label, "while cond")
            .Paste(condition_frag.Asm)
            .In("cmpq    $0, {0}({1})", ObjLayoutFacts.BoolValue, condition_frag.Reg)
            .Je(done_label, "end while")
            .Paste(body_frag.Asm)
            .Jmp(while_cond_label, "while cond")
            .Label(done_label, "end while")
            .In("movq    ${0}, {1}", RtNames.UnitValue, result_reg, "unit")
            .ToString()


    member this.Cond(cond_frag: AsmFragment,
                     true_branch_asm: string,
                     false_branch_asm: string): string =
        let cond_false_label = this.Context.LabelGen.Generate()
        let done_label = this.Context.LabelGen.Generate()

        this.Paste(cond_frag.Asm)
            .In("cmpq    $0, {0}({1})", ObjLayoutFacts.BoolValue, cond_frag.Reg)
            .Je(cond_false_label, "condition is false")
            .Comment("condition is true")
            .Paste(true_branch_asm)
            .Jmp(done_label, "done")
            .Label(cond_false_label, "condition is false")
            .Paste(false_branch_asm)
            .Label(done_label, "done")
            .ToString()


    member this.Mul(mul_span: Span, left_frag: AsmFragment, right_frag: AsmFragment): string =
        this.Location(mul_span)
            .Paste(left_frag.Asm)
            .RtCopyObject(proto_reg=left_frag.Reg, copy_reg=left_frag.Reg)
            .Paste(right_frag.Asm)
            .In("movq    {0}({1}), %rax", ObjLayoutFacts.IntValue, left_frag.Reg)
            .In("imulq   {0}({1})", ObjLayoutFacts.IntValue, right_frag.Reg)
            .In("movq    %rax, {0}({1})", ObjLayoutFacts.IntValue, left_frag.Reg)
            .ToString()


    member this.Div(div_span: Span, left_frag: AsmFragment, right_frag: AsmFragment): string =
        this.Location(div_span)
            .Paste(left_frag.Asm)
            .RtCopyObject(proto_reg=left_frag.Reg, copy_reg=left_frag.Reg)
            .Paste(right_frag.Asm)
            // left / right
            .In("movq    {0}({1}), %rax", ObjLayoutFacts.IntValue, left_frag.Reg)
            .In("cqto", comment=Some "sign-extend %rax to %rdx:%rax")
            .In("idivq    {0}({1})", ObjLayoutFacts.IntValue, right_frag.Reg)
            .In("movq    %rax, {0}({1})", ObjLayoutFacts.IntValue, left_frag.Reg)
            .ToString()


    member this.Sum(sum_span: Span, left_frag: AsmFragment, right_frag: AsmFragment): string =
        this.Location(sum_span)
            .Paste(left_frag.Asm)
            .Paste(right_frag.Asm)
            .AsUnit()
        
        if left_frag.Type.Is(BasicClasses.Int) &&
           right_frag.Type.Is(BasicClasses.Int)
        then
            this.RtCopyObject(proto_reg=right_frag.Reg, copy_reg=right_frag.Reg)
                .In("movq    {0}({1}), {2}", ObjLayoutFacts.IntValue, left_frag.Reg, left_frag.Reg)
                .In("addq    {0}, {1}({2})", left_frag.Reg, ObjLayoutFacts.IntValue, right_frag.Reg)
                .ToString()
        else // string concatenation
            this.StringConcat(left_frag.Reg, right_frag.Reg, result_reg=right_frag.Reg)
                .ToString()


    member this.Sub(sub_span: Span, left_frag: AsmFragment, right_frag: AsmFragment): string =
        this.Location(sub_span)
            .Paste(left_frag.Asm)
            .Paste(right_frag.Asm)
            // left - right
            .RtCopyObject(proto_reg=left_frag.Reg, copy_reg=left_frag.Reg)
            .In("movq    {0}({1}), {2}", ObjLayoutFacts.IntValue, right_frag.Reg, right_frag.Reg)
            .In("subq    {0}, {1}({2})", right_frag.Reg, ObjLayoutFacts.IntValue, left_frag.Reg)
            .ToString()


    member this.Match(match_span: Span,
                      expr_frag: AsmFragment,
                      expr_location: Location,
                      frame: FrameInfo,
                      tag_reg: Reg,
                      pattern_asm_infos: IReadOnlyDictionary<TYPENAME, PatternAsmInfo>)
                     : AsmBuilder =
        
        let match_init_label = this.Context.LabelGen.Generate()
        let is_tag_valid_label = this.Context.LabelGen.Generate()
        let try_match_label = this.Context.LabelGen.Generate()
        
        this.Location(match_span)
            .Paste(expr_frag.Asm)
            .Comment("handle null")
            .In("cmpq    $0, {0}", expr_frag.Reg)
            .Jne(match_init_label, "match init")
            .AsUnit()
           
        if pattern_asm_infos.ContainsKey(BasicClassNames.Null)
        then
            let null_pattern_asm_info = pattern_asm_infos.[BasicClassNames.Null]
            this.Jmp(null_pattern_asm_info.Label, "case null => ...")
                .AsUnit()
        else
            this.RtAbortMatch(expr_location, expr_reg=expr_frag.Reg)
                .AsUnit()

        this.Label(match_init_label, "match init")
            .AsUnit()
           
        if pattern_asm_infos |> Seq.exists (fun it -> it.Key <> BasicClassNames.Null)
        then
            // Store the expression's value on stack,
            // such that a var introduced by a matched case would pick it up.
            this.In("movq    {0}, -{1}(%rbp)",
                    expr_frag.Reg,
                    frame.Vars + (frame.VarsCount + 1) * 8,
                    "the expression's value")
                .AsUnit()
              
        this.In("movq    ({0}), {1}", expr_frag.Reg, tag_reg, "tag")
            .Label(is_tag_valid_label, "no match?")
            .In("cmpq    $-1, {0}", tag_reg)
            .Jne(try_match_label, "try match")
            .RtAbortMatch(expr_location, expr_reg=expr_frag.Reg)
            .Label(try_match_label, "try match")
            .AsUnit()
           
        for pattern_asm_info in pattern_asm_infos do
            // We already emitted asm for 'null'. Don't try to do it again.
            if pattern_asm_info.Key <> BasicClassNames.Null
            then
                this.In("cmpq    ${0}, {1}", pattern_asm_info.Value.Tag, tag_reg)
                    .Je(pattern_asm_info.Value.Label, comment=pattern_asm_info.Key.ToString())
                    .AsUnit()
        
        this.In("salq    $3, {0}", tag_reg, "multiply by 8")
            .In("movq    {0}({1}), {2}", RtNames.ClassParentTable, tag_reg, tag_reg, "the parent's tag")
            .Jmp(is_tag_valid_label, "no match?")


    member this.MatchCase(case_span: Span,
                          case_label: Label,
                          pattern_ty: TYPENAME,
                          block_frag: AsmFragment,
                          result_reg: Reg,
                          done_label: Label): unit =
        this.Location(case_span)
            .Label(case_label, comment="case " + pattern_ty.ToString())
            .Paste(block_frag.Asm)
            .In("movq    {0}, {1}", block_frag.Reg, result_reg)
            .Jmp(done_label, "end match")
            .AsUnit()


    member this.BeginDispatch(dispatch_span: Span): AsmBuilder =
        this.Location(dispatch_span)
            .PushCallerSavedRegs()


    member this.CompleteDispatch(dispatch_span: Span,
                                 receiver_frag: AsmFragment,
                                 receiver_is_some_label: Label,
                                 actuals_asm: string,
                                 method_reg: Reg,
                                 method_sym: MethodSymbol,
                                 actuals_count: int,
                                 result_reg: Reg): unit =
        this.Comment("actual #0")
            .Paste(receiver_frag.Asm)
            .In("cmpq    $0, {0}", receiver_frag.Reg)
            .Jne(receiver_is_some_label, "the receiver is some")
            .RtAbortDispatch(this.Context.Source.Map(dispatch_span.First))
            .Label(receiver_is_some_label, "the receiver is some")
            .Paste(actuals_asm)
            .In("movq    {0}(%rdi), {1}", ObjLayoutFacts.VTable,
                                          method_reg,
                                          comment=receiver_frag.Type.Name.ToString() + "_vtable")
            .In("movq    {0}({1}), {2}", method_sym.Index * MemLayoutFacts.VTableEntrySize,
                                         method_reg,
                                         method_reg,
                                         comment=receiver_frag.Type.Name.ToString() + "." + method_sym.Name.ToString())
            .In("call    *{0}", method_reg)
            .RemoveActualsFromStack(actuals_count)
            .PopCallerSavedRegs()
            .In("movq    %rax, {0}", result_reg, "returned value")
            .AsUnit()


    member this.BeginSuperDispatch(super_dispatch_span: Span): AsmBuilder =
        this.BeginDispatch(super_dispatch_span)
        
        
    member this.CompleteSuperDispatch(this_frag: AsmFragment,
                                      actuals_asm: string,
                                      method_sym: MethodSymbol,
                                      result_reg: Reg,
                                      actuals_count: int): unit =
        this.Comment("actual #0")
            .Paste(this_frag.Asm)
            .Paste(actuals_asm)
            .In("call    {0}.{1}", method_sym.DeclaringClass,
                                   method_sym.Name,
                                   comment="super." + method_sym.Name.ToString())
            .In("movq    %rax, {0}", result_reg, comment="returned value")
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
                .In("xorq    {0}, {1}", this_reg, this_reg)
                .AsUnit()
        else
            // Copy the relevant prototype and place a pointer to the copy in 'this_reg'.
            this.RtCopyObject(proto="$" + ty.Name.ToString() + "_proto_obj",
                              copy_reg=this_reg)
               .AsUnit()
        
        // `actuals_asm` contains `movq    $this_reg, %rdi`
        this.Paste(actuals_asm)
            .In("call    {0}..ctor", ty.Name)
            .RemoveActualsFromStack(actuals_count)
            .PopCallerSavedRegs()
            .In("movq    %rax, {0}", result_reg, comment="the new object")
            .AsUnit()


    member private this.RemoveActualsFromStack(actuals_count: int): AsmBuilder =
        // We only have (ActualRegs.Length - 1) registers to store actuals,
        // as we always use %rdi to store `this`.
        let actual_on_stack_count = actuals_count - (SysVAmd64AbiFacts.ActualRegs.Length - 1)
        if actual_on_stack_count <= 0
        then
            this
        else
            
        this.In("addq    ${0}, %rsp", actual_on_stack_count * FrameLayoutFacts.ElemSize,
                                      comment="remove " +
                                              actual_on_stack_count.ToString() +
                                              " actual(s) from stack")


    member this.BeginActuals(method_id_span: Span, actuals_count, this_reg: Reg): AsmBuilder =
        this.Location(method_id_span.Last)
            .In("subq    ${0}, %rsp", (actuals_count + (*this*)1) * FrameLayoutFacts.ElemSize)
            .In("movq    {0}, 0(%rsp)", this_reg, comment="actual #0")


    member this.Actual(actual_index: int, actual_frag: AsmFragment): unit =
        let comment = String.Format("actual #{0}", actual_index + 1)
        this.Comment(comment)
            .Paste(actual_frag.Asm)
            .In("movq    {0}, {1}(%rsp)", actual_frag.Reg,
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
            this.In("movq    {0}(%rsp), {1}", value0=actual_index * FrameLayoutFacts.ElemSize,
                                              value1=SysVAmd64AbiFacts.ActualRegs.[actual_index])
                .AsUnit()
        
        this.Comment("remove the register-loaded actuals from stack")
            .In("addq    ${0}, %rsp", actual_in_reg_count * FrameLayoutFacts.ElemSize)
            .AsUnit()
    