namespace rec LibCool.TranslatorParts


open System.Text
open LibCool.AstParts
open LibCool.TranslatorParts
open AstExtensions
open LibCool.SharedParts


[<Sealed>]
type private ClassTranslator(_context: TranslationContext,
                             _class_syntax: ClassSyntax,
                             _sb_code: StringBuilder) =
    
    
    let _sym_table = SymbolTable(_context.ClassSymMap.[_class_syntax.NAME.Syntax])
    let _expr_translator = ExprTranslator(_context, _class_syntax, _sym_table)

    
    let translate_attr (attr_node: AstNode<AttrSyntax>): Res<string> =
        let initial_node = attr_node.Syntax.Initial
        let expr_node =
            match initial_node.Syntax with
            | AttrInitialSyntax.Expr expr_syntax ->
                AstNode.Of(expr_syntax, initial_node.Span)
            | AttrInitialSyntax.Native ->
                invalidOp "AttrInitialSyntax.Native"
                
        let initial_frag = _expr_translator.Translate(expr_node)
        if initial_frag.IsError
        then
            Error
        else
            
        let attr_sym = _sym_table.Resolve(attr_node.Syntax.ID.Syntax)
        let addr_frag = _expr_translator.AddrOf(attr_sym)
        if not (_context.TypeCmp.Conforms(ancestor=addr_frag.Type, descendant=initial_frag.Value.Type))
        then
            _context.Diags.Error(
                sprintf "The initial expression's type '%O' does not conform to the '%O' attribute's type '%O'"
                        initial_frag.Value.Type.Name
                        attr_sym.Name
                        attr_sym.Type,
                initial_node.Span)

            _context.RegSet.Free(initial_frag.Value.Reg)
            _context.RegSet.Free(addr_frag.Reg)

            Error
        else
            
        let asm = initial_frag.Value.Asm
        if addr_frag.Asm.IsSome
        then
            asm.Append(addr_frag.Asm.Value).Nop()
            
        asm.AppendLine(sprintf "    movq %s, %s" (_context.RegSet.NameOf(initial_frag.Value.Reg))
                                                 (addr_frag.Asm.ToString()))
           .Nop()
            
        _context.RegSet.Free(initial_frag.Value.Reg)
        _context.RegSet.Free(addr_frag.Reg)

        Ok (asm.ToString())


    let emit_method_prologue (): unit =
        let actuals_in_frame_count = if _sym_table.MethodFrame.ActualsCount >= 6
                                     then 6
                                     else _sym_table.MethodFrame.ActualsCount
        let actuals_size_in_bytes = actuals_in_frame_count * 8

        let vars_size_in_bytes = _sym_table.MethodFrame.VarsCount * 8
        let callee_saved_regs_size_in_bytes = 5 (* number of the callee-saved regs *) * 8

        let frame_size_in_bytes =
            actuals_size_in_bytes +
            vars_size_in_bytes +
            callee_saved_regs_size_in_bytes

        let pad_size_in_bytes = if (frame_size_in_bytes % 16) = 0
                                then 0
                                else 16 - (frame_size_in_bytes % 16)
                                
        let actuals_offset_in_bytes = 0
        // let vars_offset_in_bytes = actuals_size_in_bytes
        let callee_saved_regs_offset_in_bytes = actuals_size_in_bytes + vars_size_in_bytes

        _sb_code
            .AppendLine("    pushq %rbp # save the base pointer")
            .AppendLine("    movq %rsp, %rbp # set new base pointer to rsp")
            .AppendLine(sprintf "    subq $%d, %%rsp" (frame_size_in_bytes + pad_size_in_bytes))
            .AppendLine("    # save actuals on the stack")
            .Nop()
        
        let actual_regs = [| "%rdi"; "%rsi"; "%rdx"; "%rcx"; "%r8"; "%r9" |]
        for i in 0 .. (actuals_in_frame_count - 1) do
            _sb_code
                .AppendLine(sprintf "    movq %s, -%d(%%rbp)"
                                    actual_regs.[i]
                                    (actuals_offset_in_bytes + 8 * (i + 1)))
                .Nop()
        
        _sb_code.AppendLine("    # save callee-saved registers").Nop()

        let callee_saved_regs = [| "%rbx"; "%r12"; "%r13"; "%r14"; "%r15" |]
        for i in 0..(callee_saved_regs.Length-1) do
            _sb_code
                .AppendLine(sprintf "    movq %s, -%d(%%rbp)"
                                    callee_saved_regs.[i]
                                    (callee_saved_regs_offset_in_bytes + 8 * (i + 1)))
                .Nop()
    
    
    let emit_method_epilogue (): unit =
        _sb_code
            .AppendLine("    # restore callee-saved registers")
            .AppendLine("    popq %r15 ")
            .AppendLine("    popq %r14")
            .AppendLine("    popq %r13")
            .AppendLine("    popq %r12")
            .AppendLine("    popq %rbx")
            .AppendLine("    movq %rbp, %rsp # reset stack to base pointer")
            .AppendLine("    popq %rbp # restore the old base pointer")
            .AppendLine("    ret")
            .Nop()
    
    
    let translate_ctor (): unit =
    
        _context.RegSet.AssertAllFree()

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
        _sym_table.AddFormal(Symbol.This(_class_syntax))
        
        // By a cruel twist of fate, you can't say `this.ID = ...` in Cool2020.
        // Gotta be creative and prefix formal names with "."
        // to avoid shadowing attr names by the ctor's formal names.
        _class_syntax.VarFormals
        |> Seq.iter (fun vf_node ->
            let sym = Symbol.Of(formal_node=vf_node.Map(fun vf -> vf.AsFormalSyntax(id_prefix=".")),
                                index=_sym_table.MethodFrame.ActualsCount)
            _sym_table.AddFormal(sym))
        
        // We're entering .ctor's body, which is a block.
        _sym_table.EnterBlock()
        
        // Invoke the super's .ctor with actuals from the extends syntax
        // SuperDispatch of method_id: AstNode<ID> * actuals: AstNode<ExprSyntax> []
        let extends_syntax = _class_syntax.ExtendsSyntax
        let super_dispatch_syntax = ExprSyntax.SuperDispatch (
                                        method_id=AstNode.Virtual(ID ".ctor"),
                                        actuals=extends_syntax.Actuals)
        
        let super_dispatch_frag = _expr_translator.Translate(AstNode.Virtual(super_dispatch_syntax))
        if super_dispatch_frag.IsOk
        then
            _context.RegSet.Free(super_dispatch_frag.Value.Reg)
            sb_ctor_body.Append(super_dispatch_frag.Value.Asm).Nop()
            
        _context.RegSet.AssertAllFree()

        // Assign values passed as formals to attrs derived from varformals.
        _class_syntax.VarFormals
        |> Seq.iter (fun vf_node ->
            let attr_name = vf_node.Syntax.ID.Syntax.Value
            let assign_syntax =
                ExprSyntax.Assign(id=AstNode.Virtual(ID attr_name),
                                  expr=AstNode.Virtual(ExprSyntax.Id (ID ("." + attr_name))))

            let assign_frag = _expr_translator.Translate(AstNode.Virtual(assign_syntax))
            if assign_frag.IsOk
            then
                _context.RegSet.Free(assign_frag.Value.Reg)
                sb_ctor_body.Append(assign_frag.Value.Asm).Nop()
        
            _context.RegSet.AssertAllFree()
        )
        
        _context.RegSet.AssertAllFree()

        // Assign initial values to attributes declared in the class.
        _class_syntax.Features
        |> Seq.where (fun feature_node -> feature_node.Syntax.IsAttr)
        |> Seq.iter (fun feature_node ->
            let attr_frag = translate_attr (feature_node.Map(fun it -> it.AsAttrSyntax))
            if attr_frag.IsOk
            then
                sb_ctor_body.Append(attr_frag.Value).Nop()
        )
        
        _context.RegSet.AssertAllFree()

        // Translate blocks.
        _class_syntax.Features
        |> Seq.where (fun feature_node -> feature_node.Syntax.IsBracedBlock)
        |> Seq.iter (fun feature_node ->
            let block_frag = _expr_translator.TranslateBlock(feature_node.Syntax.AsBlockSyntax)
            if block_frag.IsOk
            then
                _context.RegSet.Free(block_frag.Value.Reg)
                sb_ctor_body.Append(block_frag.Value.Asm).Nop()
        )
        
        _context.RegSet.AssertAllFree()

        // Append ExprSyntax.This to the .ctor's end.
        // (As a result, the last block's last expr's type doesn't have to match the class' type.)
        let this_syntax = ExprSyntax.This
        let this_frag = _expr_translator.Translate(AstNode.Virtual(this_syntax))
        sb_ctor_body.AppendLine("    # return 'this'")
                    .Append(this_frag.Value.Asm.ToString())
                    .AppendLine(sprintf "    movq %s, %%rax" (_context.RegSet.NameOf(this_frag.Value.Reg)))
                    .Nop()

        _context.RegSet.Free(this_frag.Value.Reg)
        _context.RegSet.AssertAllFree()

        // Finally, emit assembly.
        _sb_code
            .AppendLine(sprintf "%O..ctor:" _class_syntax.NAME.Syntax)
            .Nop()
                        
        emit_method_prologue ()
        _sb_code
            .AppendLine("    # begin body")
            .Append(sb_ctor_body.ToString())
            .AppendLine("    # end body")
            .Nop()
        emit_method_epilogue ()

        _sym_table.LeaveBlock()
        _sym_table.LeaveMethod()
    
    
    let translate_method (method_node: AstNode<MethodSyntax>) =
        _context.RegSet.AssertAllFree()

        _sym_table.EnterMethod()
        
        let mutable override_ok = true

        if method_node.Syntax.Override
        then
            let super_sym = _context.ClassSymMap.[_class_syntax.ExtendsSyntax.SUPER.Syntax]
            let overridden_method_sym = super_sym.Methods.[method_node.Syntax.ID.Syntax]
            
            if overridden_method_sym.Formals.Length <> method_node.Syntax.Formals.Length
            then
                _context.Diags.Error(
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
                    _context.Diags.Error(
                        sprintf "The overriding formals's type '%O' does not match to the overridden formal's type '%O'"
                                formal.TYPE.Syntax
                                overridden_formal_sym.Type,
                        formal.TYPE.Span)
                    override_ok <- false)

            let overridden_return_ty = _context.ClassSymMap.[overridden_method_sym.ReturnType]
            let return_ty = _context.ClassSymMap.[method_node.Syntax.RETURN.Syntax]
            if not (_context.TypeCmp.Conforms(ancestor=overridden_return_ty, descendant=return_ty))
            then
                _context.Diags.Error(
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
            _sym_table.AddFormal(Symbol.This(_class_syntax))
            
            method_node.Syntax.Formals
            |> Seq.iter (fun formal_node ->
                let sym = Symbol.Of(formal_node, index=_sym_table.MethodFrame.ActualsCount)
                _sym_table.AddFormal(sym))
            
            // Translate the method's body
            let body_frag = _expr_translator.Translate(method_node.Syntax.Body.Map(fun it -> it.AsExprSyntax))
            if body_frag.IsOk
            then
                // Make sure, the body's type conforms to the return type.
                let return_ty = _context.ClassSymMap.[method_node.Syntax.RETURN.Syntax]
                if not (_context.TypeCmp.Conforms(ancestor=return_ty, descendant=body_frag.Value.Type))
                then
                    _context.Diags.Error(
                        sprintf "The method body's type '%O' does not conform to the declared return type '%O'"
                                body_frag.Value.Type.Name
                                return_ty.Name,
                        method_node.Syntax.Body.Span)
                    _context.RegSet.Free(body_frag.Value.Reg)
                else
                    // Finally, all the semantic checks passed.
                    // Emit assembly.
                    _sb_code
                        .AppendLine(sprintf "%O.%O:"
                                            _class_syntax.NAME.Syntax
                                            method_node.Syntax.ID.Syntax)
                        .Nop()
                        
                    emit_method_prologue ()
                    _sb_code
                        .AppendLine("    # begin body")
                        .Append(body_frag.Value.Asm.ToString())
                        .AppendLine(sprintf "    movq %s, %%rax"
                                            (if body_frag.Value.Reg = Reg.Null
                                             then "$0"
                                             else _context.RegSet.NameOf(body_frag.Value.Reg)))
                        .AppendLine("    # end body")
                        .Nop()
                    _context.RegSet.Free(body_frag.Value.Reg)
                    emit_method_epilogue ()

        _sym_table.LeaveMethod()
        _context.RegSet.AssertAllFree()
    
    
    member this.Translate(): unit =
        _context.RegSet.AssertAllFree()
            
        translate_ctor ()
        _class_syntax.Features
            |> Seq.where (fun feature_node -> feature_node.Syntax.IsMethod)
            |> Seq.iter (fun feature_node -> translate_method (feature_node.Map(fun it -> it.AsMethodSyntax)))

        _context.RegSet.AssertAllFree()
