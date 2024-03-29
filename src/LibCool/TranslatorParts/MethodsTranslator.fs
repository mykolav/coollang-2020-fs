namespace rec LibCool.TranslatorParts


open LibCool.SharedParts
open LibCool.SourceParts
open LibCool.AstParts
open LibCool.SemanticParts.AstExtensions
open LibCool.TranslatorParts
open LibCool.TranslatorParts.AsmFragments


[<Sealed>]
type private MethodsTranslator(_context: TranslationContext,
                               _class_syntax: ClassSyntax) as this =


    let _sym_table = SymbolTable(_context.ClassSymMap[_class_syntax.NAME.Syntax])
    let _expr_translator = ExprTranslator(_context, _class_syntax, _sym_table)


    let translateAttrInit (attr_node: AstNode<AttrSyntax>): LcResult<string> =
        let initial_node = attr_node.Syntax.Initial
        let expr_node =
            match initial_node.Syntax with
            | AttrInitialSyntax.Expr expr_syntax ->
                AstNode.Of(expr_syntax, initial_node.Span)
            | AttrInitialSyntax.Native ->
                invalidOp "AttrInitialSyntax.Native"

        let initial_frag = _expr_translator.Translate(expr_node)
        match initial_frag with
        | Error ->
            Error
        | Ok initial_frag ->
            let attr_sym = _sym_table.Resolve(attr_node.Syntax.ID.Syntax)
            let addr_frag = _expr_translator.AddrOf(attr_sym)
            if not (_context.TypeCmp.Conforms(ancestor=addr_frag.Type, descendant=initial_frag.Type))
            then
                _context.Diags.Error(
                    $"The initial expression's type '{initial_frag.Type.Name}' " +
                    $"does not conform to the '{attr_sym.Name}' attribute's type '{attr_sym.Type}'",
                    initial_node.Span)

                _context.RegSet.Free(initial_frag.Reg)
                _context.RegSet.Free(addr_frag.Reg)

                Error
            else

            let asm =
                this.EmitAsm()
                    .Paste(initial_frag.Asm)
                    .Location(attr_node.Span)
                    .Instr("movq    {0}, {1}", initial_frag.Reg, addr_frag, comment=attr_sym.Name.ToString())
                    .ToString()

            _context.RegSet.Free(initial_frag.Reg)
            _context.RegSet.Free(addr_frag.Reg)

            Ok asm


    let translateCtorBody (): LcResult<string> =

        let asm = this.EmitAsm()

        // .ctor's formals are varformals,
        // .ctor's body is
        //   - an invocation of the super's .ctor with actuals from the extends syntax
        //   - assign values passed as formals to attrs derived from varformals
        //   - assign initial exprs to attrs defined in the class
        //   - concatenated blocks (if any)
        //   - ExprSyntax.This is always appended to the .ctor's end
        //     (as a result, the last block's last expr's type doesn't have to match the class' type)

        _sym_table.AddFormal(Symbol.This(_class_syntax))

        // By a cruel twist of fate, you can't say `this.ID = ...` in Cool2020.
        // Gotta be creative and prefix formal names with "."
        // to avoid shadowing attr names by the ctor's formal names.
        _class_syntax.VarFormals |> Seq.iter (fun vf_node ->
            let sym = Symbol.Of(formal_node=vf_node.Map(fun vf -> vf.AsFormalSyntax(id_prefix=".")),
                                index=_sym_table.Frame.ActualsCount)
            _sym_table.AddFormal(sym))

        // We're entering .ctor's body, which is a block.
        _sym_table.EnterBlock()

        // Assign values passed as varformals to attrs derived from them.
        // We do it before invoking `super..ctor`,
        // as the code may be passing attrs derived from varformals as actuals to `super..ctor`.
        _class_syntax.VarFormals
        |> Seq.iter (fun vf_node ->
            let attr_name = vf_node.Syntax.ID.Syntax.Value
            let assign_syntax =
                ExprSyntax.Assign(id=AstNode.Virtual(ID attr_name),
                                  expr=AstNode.Virtual(ExprSyntax.Id (ID ("." + attr_name))))

            let assign_frag = _expr_translator.Translate(AstNode.Virtual(assign_syntax))
            match assign_frag with
            | Error -> ()
            | Ok assign_frag ->
                _context.RegSet.Free(assign_frag.Reg)
                asm.Paste(assign_frag.Asm).AsUnit()

            _context.RegSet.AssertAllFree()
        )

        _context.RegSet.AssertAllFree()

        // Invoke the super's .ctor with actuals from the extends syntax
        // SuperDispatch of method_id: AstNode<ID> * actuals: AstNode<ExprSyntax> []
        let extends_syntax = _class_syntax.ExtendsSyntax
        let super_dispatch_syntax = ExprSyntax.SuperDispatch (
                                        method_id=AstNode.Virtual(ID ".ctor"),
                                        actuals=extends_syntax.Actuals)

        let super_dispatch_frag = _expr_translator.Translate(AstNode.Virtual(super_dispatch_syntax))
        match super_dispatch_frag with
        | Error -> ()
        | Ok super_dispatch_frag ->
            _context.RegSet.Free(super_dispatch_frag.Reg)
            asm.Paste(super_dispatch_frag.Asm).AsUnit()

        _context.RegSet.AssertAllFree()

        // Assign initial values to attributes declared in the class.
        for feature_node in _class_syntax.Features do
            match feature_node with
            | { Syntax = FeatureSyntax.Attr attr_syntax } ->
                let attr_frag = translateAttrInit (feature_node.Map(fun _ -> attr_syntax))
                match attr_frag with
                | Error -> ()
                | Ok attr_frag ->
                   asm.Paste(attr_frag).AsUnit()
            | _ -> ()

        _context.RegSet.AssertAllFree()

        // Translate blocks.
        for feature_node in _class_syntax.Features do
            match feature_node with
            | { Syntax = FeatureSyntax.BracedBlock braced_block_syntax } ->
                let block_frag = _expr_translator.TranslateBlock(braced_block_syntax)
                match block_frag with
                | Error -> ()
                | Ok block_frag ->
                    _context.RegSet.Free(block_frag.Reg)
                    asm.Paste(block_frag.Asm).AsUnit()
            | _ -> ()

        _context.RegSet.AssertAllFree()

        // Append ExprSyntax.This to the .ctor's end.
        // (As a result, the last block's last expr's type doesn't have to match the class' type)
        let this_syntax = ExprSyntax.This
        let this_frag = _expr_translator.Translate(AstNode.Virtual(this_syntax))
                                        .Value

        asm.Paste(this_frag.Asm)
           .Instr("movq    {0}, %rax", this_frag.Reg, comment="this")
           .AsUnit()

        _context.RegSet.Free(this_frag.Reg)
        _sym_table.LeaveBlock()

        Ok (asm.ToString())


    let translateMethodBody (method_syntax: MethodSyntax): LcResult<string> =
        let mutable override_ok = true

        if method_syntax.Override
        then
            let super_sym = _context.ClassSymMap[_class_syntax.ExtendsSyntax.SUPER.Syntax]
            let overridden_method_sym = super_sym.Methods[method_syntax.ID.Syntax]

            if overridden_method_sym.Formals.Length <> method_syntax.Formals.Length
            then
                _context.Diags.Error(
                    $"The overriding '{_class_syntax.NAME.Syntax}.{method_syntax.ID.Syntax}' method's " +
                    $"number of formals %d{method_syntax.Formals.Length} does not match " +
                    $"the overridden '{super_sym.Name}.{method_syntax.ID.Syntax}' method's " +
                    $"number of formals %d{overridden_method_sym.Formals.Length}",
                    method_syntax.ID.Span)
                override_ok <- false

            overridden_method_sym.Formals |> Array.iteri (fun i overridden_formal_sym ->
                let formal = method_syntax.Formals[i].Syntax
                if overridden_formal_sym.Type <> formal.TYPE.Syntax
                then
                    _context.Diags.Error(
                        $"The overriding formals's type '{formal.TYPE.Syntax}' does not match to " +
                        $"the overridden formal's type '{overridden_formal_sym.Type}'",
                        formal.TYPE.Span)
                    override_ok <- false)

            let overridden_return_ty = _context.ClassSymMap[overridden_method_sym.ReturnType]
            let return_ty = _context.ClassSymMap[method_syntax.RETURN.Syntax]
            if not (_context.TypeCmp.Conforms(ancestor=overridden_return_ty, descendant=return_ty))
            then
                _context.Diags.Error(
                    $"The overriding '{_class_syntax.NAME.Syntax}.{method_syntax.ID.Syntax}' method's " +
                    $"return type '{method_syntax.RETURN.Syntax}' does not conform to " +
                    $"the overridden '{super_sym.Name}.{method_syntax.ID.Syntax}' method's " +
                    $"return type '{overridden_method_sym.ReturnType}'",
                    method_syntax.RETURN.Span)
                override_ok <- false

        if not override_ok
        then
            Error
        else

        // Add the method's formal parameters to the symbol table.
        _sym_table.AddFormal(Symbol.This(_class_syntax))

        method_syntax.Formals
        |> Seq.iter (fun formal_node ->
            let sym = Symbol.Of(formal_node, index=_sym_table.Frame.ActualsCount)
            _sym_table.AddFormal(sym))

        // Translate the method's body
        let body_frag = _expr_translator.Translate(method_syntax.Body)
        match body_frag with
        | Error ->
            Error
        | Ok body_frag ->
            // Make sure, the body's type conforms to the return type.
            let return_ty = _context.ClassSymMap[method_syntax.RETURN.Syntax]
            if not (_context.TypeCmp.Conforms(ancestor=return_ty, descendant=body_frag.Type))
            then
                _context.Diags.Error(
                    $"The method body's type '{body_frag.Type.Name}' does not conform to " +
                    $"the declared return type '{return_ty.Name}'",
                    method_syntax.Body.Span)
                _context.RegSet.Free(body_frag.Reg)
                Error
            else

            // Finally, all the semantic checks passed.
            // Emit assembly.

            let asm = this.EmitAsm()
                          .Paste(body_frag.Asm)

            if body_frag.Reg = Reg.Null
            then
                Ok (asm.Instr("movq    ${0}, %rax", RtNames.UnitValue)
                       .ToString())
            else
                _context.RegSet.Free(body_frag.Reg)
                Ok (asm.Instr("movq    {0}, %rax", body_frag.Reg)
                       .ToString())


    let translateMethod (method_name: string)
                        (method_span: Span)
                        (translate_body: unit -> LcResult<string>)
                        : LcResult<string> =
        _context.RegSet.AssertAllFree()
        _sym_table.EnterMethod()

        let body_frag = translate_body ()
        match body_frag with
        | Error ->
            Error
        | Ok body_frag ->
            let asm = this.EmitAsm()
                          .Method(method_name, method_span, _sym_table.Frame, body_frag)

            _sym_table.LeaveMethod()
            _context.RegSet.AssertAllFree()

            Ok asm


    member private this.EmitAsm(): AsmBuilder = AsmBuilder(_context)


    member this.Translate(): string =
        _context.RegSet.AssertAllFree()

        let asm = this.EmitAsm()

        let ctor_name = $"{_class_syntax.NAME.Syntax}..ctor"
        let ctor_span = if _class_syntax.VarFormals.Length > 0
                        then Span.Of(_class_syntax.NAME.Span.First,
                                     _class_syntax.VarFormals[_class_syntax.VarFormals.Length - 1].Span.Last)
                        else _class_syntax.NAME.Span

        let method_frag = translateMethod ctor_name ctor_span translateCtorBody
        match method_frag with
        | Error -> ()
        | Ok method_frag ->
            asm.Paste(method_frag).AsUnit()

        for feature_node in _class_syntax.Features do
            match feature_node with
            | { Syntax = FeatureSyntax.Method method_syntax } ->
                let method_node = feature_node.Map(fun _ -> method_syntax)
                let method_name = $"{_class_syntax.NAME.Syntax}.{method_node.Syntax.ID.Syntax}"
                let method_frag = translateMethod method_name
                                                   method_node.Span
                                                   (fun () -> translateMethodBody method_node.Syntax)
                match method_frag with
                | Error -> ()
                | Ok method_frag ->
                    asm.Paste(method_frag).AsUnit()
            | _ -> ()

        _context.RegSet.AssertAllFree()

        asm.ToString()
