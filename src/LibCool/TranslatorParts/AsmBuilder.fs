namespace LibCool.TranslatorParts


open System
open System.Collections.Generic
open System.Text
open LibCool.SharedParts
open LibCool.SourceParts


[<Sealed>]
type AsmBuilder(_context: TranslationContext) =
    
    
    let _asm = StringBuilder()
    let _indent = "    "
    
    
    let _pushed_caller_saved_regs = Stack<string>()
    
    
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
        this.In(String.Format(instruction, value), comment)

        
    // In[struction]
    member this.In(instruction: string, reg: Reg, ?comment: string): AsmBuilder =
        this.In(String.Format(instruction, _context.RegSet.NameOf(reg)), comment)


    // In[struction]
    member this.In(instruction: string, value: obj, reg: Reg, ?comment: string): AsmBuilder =
        this.In(String.Format(instruction, value, _context.RegSet.NameOf(reg)), comment)


    // In[struction]
    member this.In(instruction: string, value0: obj, value1: obj, ?comment: string): AsmBuilder =
        this.In(String.Format(instruction, value0, value1), comment)


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
        this.In(String.Format(instruction, _context.RegSet.NameOf(reg), value), comment)


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


    // In[struction]
    member this.Single(instruction: string, value: obj, reg: Reg, ?comment: string): string =
        this.In(String.Format(instruction, value, _context.RegSet.NameOf(reg)), comment).ToString()

        
    member this.Jmp(jmp: string, label: Label, comment: string) =
        this.In(String.Format("{0}    {1}", jmp, _context.LabelGen.NameOf(label)), comment)


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
             let slice_end = offset + length
                            
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
        
        
    member this.RtAbortMatch(filename_label: string, line: uint32, col: uint32, expr_reg: Reg): AsmBuilder =
        this.In("movq    ${0}, %rdi", value=filename_label)
            .In("movq    ${0}, %rsi", value=line)
            .In("movq    ${0}, %rdx", value=col)
            .In("movq    {0}, %rcx", expr_reg)
            .In("call    {0}", RtNames.RtAbortMatch)


    member this.RtAbortDispatch(filename_label: string, line: uint32, col: uint32): AsmBuilder =
        this.In("movq    ${0}, %rdi", filename_label)
            .In("movq    ${0}, %rsi", line)
            .In("movq    ${0}, %rdx", col)
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


