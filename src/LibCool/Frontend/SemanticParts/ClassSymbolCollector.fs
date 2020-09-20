namespace LibCool.Frontend.SemanticParts


open System.Collections.Generic
open System.Text
open LibCool.AstParts
open LibCool.DiagnosticParts
open LibCool.SourceParts
open AstExtensions


[<Sealed>]
type private InheritanceChain() =
    let _ancestry_map = Dictionary<TYPENAME, struct {| Syntax: ClassSyntax; Distance: int |}>()
                        
                        
    member this.Add(class_syntax: ClassSyntax): bool =
        if _ancestry_map.ContainsKey(class_syntax.NAME.Syntax)
        then
            false
        else
        
        let distance = _ancestry_map.Count + 1
        _ancestry_map.Add(class_syntax.NAME.Syntax,
                       struct {| Syntax = class_syntax; Distance = distance |})
        
        true
        
        
    member this.Subchain(start: ClassSyntax) =
        let start_distance = _ancestry_map.[start.NAME.Syntax].Distance
        
        let subchain =
            _ancestry_map.Values
            |> Seq.where (fun it -> it.Distance >= start_distance)
            |> Seq.sortBy (fun it -> it.Distance)
            |> Seq.map (fun it -> it.Syntax)
        Array.ofSeq subchain


type VarFormalOrAttrSyntax =
    { ID: AstNode<ID>
      TYPE: AstNode<TYPENAME> }


[<Sealed>]
type ClassSymbolCollector(_program_syntax: ProgramSyntax,
                          _classdecl_node_map: IReadOnlyDictionary<TYPENAME, AstNode<ClassSyntax>>,
                          _source: Source,
                          _diags: DiagnosticBag) =

    
    let _class_sym_map = Dictionary<TYPENAME, ClassSymbol>() 
    

    let resolve_to_class_syntax (class_name_node: AstNode<TYPENAME>)
                                : AstNode<ClassSyntax> voption =
        let class_name = class_name_node.Syntax
        
        if _classdecl_node_map.ContainsKey(class_name)
        then
            ValueSome (_classdecl_node_map.[class_name])
        else

        if not (_class_sym_map.ContainsKey(class_name))
        then
            _class_sym_map.Add(class_name, BasicClasses.Error)
            _diags.Error(
                sprintf "The type name '%O' could not be found (is an input file missing?)" class_name,
                class_name_node.Span)

        ValueNone


    let mk_attr_sym (classdecl_syntax: ClassSyntax)
                    (attr_node: AstNode<VarFormalOrAttrSyntax>)
                    (index: int): AttrSymbol =
        let attr_syntax = attr_node.Syntax
        { AttrSymbol.Name = attr_syntax.ID.Syntax
          Type = attr_syntax.TYPE.Syntax
          DeclaringClass = classdecl_syntax.NAME.Syntax
          Index = index
          SyntaxSpan = attr_node.Span }
    

    let mk_attr_syms (classdecl_syntax: ClassSyntax)
                     (super: ClassSymbol) =
        let attr_syms = Dictionary<ID, AttrSymbol>(super.Attrs)

        let add_attr_sym (attr_node: AstNode<VarFormalOrAttrSyntax>) =
            let attr_syntax = attr_node.Syntax
            
            if attr_syms.ContainsKey(attr_syntax.ID.Syntax)
            then
                let prev_attr_sym = attr_syms.[attr_syntax.ID.Syntax]
                let message =
                    sprintf "The class '%O' already contains an attribute '%O' [declared in '%O' at %O]"
                            classdecl_syntax.NAME.Syntax
                            attr_syntax.ID.Syntax
                            prev_attr_sym.DeclaringClass
                            (_source.Map(prev_attr_sym.SyntaxSpan.First))
                    
                _diags.Error(message, attr_node.Span)
            else
                
            resolve_to_class_syntax attr_syntax.TYPE |> ignore

            let ai = mk_attr_sym classdecl_syntax
                                 attr_node
                                 (*index=*)attr_syms.Count
            attr_syms.Add(ai.Name, ai)
            
        classdecl_syntax.VarFormals
        |> Seq.iter (fun varformal_node ->
            add_attr_sym (varformal_node.Map(fun it -> { VarFormalOrAttrSyntax.ID=it.ID
                                                         TYPE=it.TYPE
                                                         (*Initial=ValueNone*) })))
        
        classdecl_syntax.Features
        |> Seq.where (fun it -> it.Syntax.IsAttr)
        |> Seq.iter (fun feature_node ->
            add_attr_sym (feature_node.Map(fun it -> let attr_syntax = it.AsAttrSyntax
                                                     { VarFormalOrAttrSyntax.ID=attr_syntax.ID
                                                       TYPE=attr_syntax.TYPE
                                                       (*Initial=ValueSome attr_syntax.Initial*) })))
        
        attr_syms :> IReadOnlyDictionary<_, _>
    
    
    let mk_param_syms (formal_syntaxes: AstNode<FormalSyntax>[])
                      (get_message: (*formal:*)AstNode<FormalSyntax> -> (*prev_formal:*)AstNode<FormalSyntax> -> string)
                      : FormalSymbol[] =
        let formal_node_map = Dictionary<ID, AstNode<FormalSyntax>>()

        formal_syntaxes
        |> Array.iter (fun formal_node ->
            let formal_syntax = formal_node.Syntax
            if formal_node_map.ContainsKey(formal_syntax.ID.Syntax)
            then
                let prev_formal_node = formal_node_map.[formal_syntax.ID.Syntax]
                _diags.Error(get_message formal_node prev_formal_node, formal_node.Span)
            else
                
            resolve_to_class_syntax formal_syntax.TYPE |> ignore
            formal_node_map.Add(formal_syntax.ID.Syntax, formal_node)
        )

        let param_syms = formal_node_map.Values
                         |> Seq.mapi (fun i it -> { FormalSymbol.Name = it.Syntax.ID.Syntax
                                                    Type = it.Syntax.TYPE.Syntax
                                                    Index = i
                                                    SyntaxSpan = it.Span })
        Array.ofSeq param_syms
    
    
    let mk_method_param_syms (method_syntax: MethodSyntax) =
        mk_param_syms ((*formal_syntaxes=*)method_syntax.Formals)
                      ((*get_message=*)fun formal prev_formal ->
                          sprintf "The method '%O' already contains a formal '%O' at %O"
                                   method_syntax.ID.Syntax
                                   formal.Syntax.ID.Syntax
                                   prev_formal.Span)


    let mk_method_sym (classdecl_syntax: ClassSyntax)
                      (method_node: AstNode<MethodSyntax>)
                      (index: int): MethodSymbol =
        let method_syntax = method_node.Syntax
        let param_syms = mk_method_param_syms method_syntax

        { MethodSymbol.Name = method_syntax.ID.Syntax
          Formals = param_syms
          ReturnType = method_syntax.RETURN.Syntax
          Override = method_syntax.Override
          DeclaringClass = classdecl_syntax.NAME.Syntax
          Index = index
          SyntaxSpan = method_node.Span }
        
        
    let mk_method_syms (classdecl_syntax: ClassSyntax)
                       (super: ClassSymbol) =
        let method_syms = Dictionary<ID, MethodSymbol>(super.Methods)
        
        let add_method_sym (method_node: AstNode<MethodSyntax>): unit =
            let method_syntax = method_node.Syntax
            
            if not method_syntax.Override && method_syms.ContainsKey(method_syntax.ID.Syntax)
            then
                let prev_method_sym = method_syms.[method_syntax.ID.Syntax]
                let sb_message =
                    StringBuilder().AppendFormat(
                                        "The class '{0}' already contains a method '{1}' [declared in '{2}' at {3}]",
                                        classdecl_syntax.NAME.Syntax,
                                        method_syntax.ID.Syntax,
                                        prev_method_sym.DeclaringClass,
                                        (_source.Map(prev_method_sym.SyntaxSpan.First)))
                
                if classdecl_syntax.NAME.Syntax <> prev_method_sym.DeclaringClass
                then
                    sb_message.Append(". Use 'override def' to override it") |> ignore
                
                _diags.Error(sb_message.ToString(), method_node.Span)
            else
                
            if method_syntax.Override && not (method_syms.ContainsKey(method_syntax.ID.Syntax))
            then
                _diags.Error(
                    sprintf "Cannot override a method '%O' because it was not previously defined" method_syntax.ID.Syntax,
                    method_node.Span)
            else
                
            resolve_to_class_syntax method_syntax.RETURN |> ignore
            
            let index = if method_syms.ContainsKey(method_syntax.ID.Syntax)
                        then method_syms.[method_syntax.ID.Syntax].Index
                        else method_syms.Count
            let mi = mk_method_sym classdecl_syntax method_node index
            
            method_syms.[mi.Name] <- mi

        classdecl_syntax.Features
        |> Seq.where (fun feature_node -> feature_node.Syntax.IsMethod)
        |> Seq.iter (fun feature_node -> add_method_sym (feature_node.Map(fun it -> it.AsMethodSyntax))) 

        method_syms :> IReadOnlyDictionary<_, _>


    let mk_ctor_param_syms (classdecl_syntax: ClassSyntax) =
        let formal_syntaxes = classdecl_syntax.VarFormals
                              |> Array.map (fun vf_node -> vf_node.Map(fun vf -> vf.AsFormalSyntax))
        mk_param_syms ((*formal_syntaxes=*)formal_syntaxes)
                      ((*get_message=*)fun formal prev_formal ->
                          sprintf "The constructor of class '%O' already contains a var formal '%O' at %O"
                                   classdecl_syntax.NAME.Syntax
                                   formal.Syntax.ID.Syntax
                                   prev_formal.Span)


    let mk_ctor_sym (classdecl_syntax: ClassSyntax): MethodSymbol =
        let param_syms = mk_ctor_param_syms classdecl_syntax
        
        { MethodSymbol.Name = ID ".ctor"
          Formals = param_syms
          ReturnType = classdecl_syntax.NAME.Syntax
          Override = false
          DeclaringClass = classdecl_syntax.NAME.Syntax
          Index = -1
          SyntaxSpan = Span.Invalid }
    

    let mk_class_sym (classdecl_node: AstNode<ClassSyntax>)
                     (super: ClassSymbol) : ClassSymbol =
        let classdecl_syntax = classdecl_node.Syntax
        let attr_syms = mk_attr_syms classdecl_syntax super 
        let method_syms = mk_method_syms classdecl_syntax super
        
        { ClassSymbol.Name = classdecl_syntax.NAME.Syntax
          Super = super.Name
          Ctor = mk_ctor_sym classdecl_syntax
          Attrs = attr_syms
          Methods = method_syms
          SyntaxSpan = classdecl_node.Span }
        
        
    let add_class_sym_to_map (classsym: ClassSymbol): unit =
        _class_sym_map.Add(classsym.Name, classsym)


    let collect_class_sym (class_name_node: AstNode<TYPENAME>): unit =
        let inheritance_chain = InheritanceChain()
        
        let rec do_collect_class_sym (class_name_node: AstNode<TYPENAME>): ClassSymbol =
            let class_name = class_name_node.Syntax
     
            // See if we collected a symbol for this class earlier.
            // If we did, don't try to collect again.
            if _class_sym_map.ContainsKey(class_name)
            then
                _class_sym_map.[class_name]
            else
            
            // See if a class with the name supplied to us exists.
            let class_node_opt = resolve_to_class_syntax class_name_node
            if class_node_opt.IsNone
            then 
                BasicClasses.Error
            else
                
            let class_node = class_node_opt.Value 
            
            // See if we're in an inheritance cycle.
            let cycle_detected =
                let class_syntax = class_node.Syntax
                if inheritance_chain.Add(class_syntax)
                then
                    false
                else

                // An inheritance cycle detected
                
                let sb_message = StringBuilder("A circular superclass dependency detected: '")

                let cycle = inheritance_chain.Subchain(class_syntax)
                
                cycle |> Seq.iter (fun it -> sb_message.AppendFormat("{0} -> ", it.NAME.Syntax) |> ignore)
                sb_message.AppendFormat("{0}'", cycle.[0].NAME.Syntax) |> ignore

                _diags.Error(sb_message.ToString(), class_syntax.ExtendsSyntax.SUPER.Span)
                true
            
            if cycle_detected
            then
                BasicClasses.Error
            else

            // We didn't collect a symbol for this class previously.
            // And we aren't in an inheritance cycle.
            // To collect a symbol for this class we need to merge the class' syntax and its super's symbol.
            
            // Collect the super's symbol.
            let super_sym = do_collect_class_sym class_node.Syntax.ExtendsSyntax.SUPER
            if super_sym.IsError
            then
                // Remember we failed to collect a symbol for the current class.
                // So the next time we need it, we don't try to collect it again
                // and don't emit duplicate diags.
                _class_sym_map.Add(class_name, BasicClasses.Error)
                BasicClasses.Error
            else

            // Merge the current class' syntax and its super's symbol.
            let class_sym = mk_class_sym class_node super_sym
            
            // Add the collected symbol to the map,
            // so the next time we need it, we can reuse it instead of collecting from scratch.
            add_class_sym_to_map class_sym
            class_sym
            
        do_collect_class_sym class_name_node |> ignore
    
    
    member this.Collect(): IReadOnlyDictionary<TYPENAME, ClassSymbol> =
        add_class_sym_to_map BasicClasses.Any
        add_class_sym_to_map BasicClasses.Unit
        add_class_sym_to_map BasicClasses.Int
        add_class_sym_to_map BasicClasses.String
        add_class_sym_to_map BasicClasses.Boolean
        add_class_sym_to_map BasicClasses.ArrayAny
        add_class_sym_to_map BasicClasses.IO
        
        // User code cannot directly reference the class Null.
        // So we don't add it to the map before collecting class symbols from the program's ast. 
        
        _program_syntax.Classes |> Array.iter (fun class_node ->
            let class_syntax = class_node.Syntax
            collect_class_sym class_syntax.NAME)
        
        _class_sym_map :> IReadOnlyDictionary<_, _>
