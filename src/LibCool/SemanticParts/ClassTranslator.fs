namespace rec LibCool.SemanticParts


open System.Text
open LibCool.AstParts
open LibCool.SemanticParts
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
            Error
        else
            
        let asm =
            initial_frag.Value.Asm
                .AppendLine(sprintf "movq %s, %s" (_context.RegSet.NameOf(initial_frag.Value.Reg)) (addr_frag.Asm.ToString()))
                .ToString()
            
        _context.RegSet.Free(initial_frag.Value.Reg)
        _context.RegSet.Free(addr_frag.Reg)

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
        
        let super_dispatch_frag = _expr_translator.Translate(AstNode.Virtual(super_dispatch_syntax))
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

            let assign_frag = _expr_translator.Translate(AstNode.Virtual(assign_syntax))
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
            let block_frag = _expr_translator.TranslateBlock(feature_node.Syntax.AsBlockSyntax)
            if block_frag.IsOk
            then
                sb_ctor_body.Append(block_frag.Value.Asm) |> ignore)
        
        // Append ExprSyntax.This to the .ctor's end.
        // (As a result, the last block's last expr's type doesn't have to match the class' type.)
        let this_syntax = ExprSyntax.This
        let this_frag = _expr_translator.Translate(AstNode.Virtual(this_syntax))
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
            _sym_table.Add(Symbol.This(_class_syntax))
            
            method_node.Syntax.Formals
            |> Seq.iter (fun formal_node ->
                let sym = Symbol.Of(formal_node, index=_sym_table.MethodSymCount)
                _sym_table.Add(sym))
            
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

        _sym_table.LeaveMethod()
    
    
    member this.Translate(): unit =
        translate_ctor ()
        _class_syntax.Features
            |> Seq.where (fun feature_node -> feature_node.Syntax.IsMethod)
            |> Seq.iter (fun feature_node -> translate_method (feature_node.Map(fun it -> it.AsMethodSyntax)))
