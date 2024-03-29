namespace LibCool.SemanticParts


open System.Collections.Generic
open System.Text
open LibCool.AstParts
open LibCool.DiagnosticParts
open LibCool.SharedParts
open LibCool.SourceParts
open AstExtensions


[<RequireQualifiedAccess>]
module SpecialClasses =
    let Error = ClassSymbol.Virtual(class_name=TYPENAME ".Error", tag=(-1))


[<AutoOpen>]
module private ClassSymbolExtensions =
    type ClassSymbol
        with
        member this.IsError: bool = this.Is(SpecialClasses.Error)


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
        let start_distance = _ancestry_map[start.NAME.Syntax].Distance
        
        let subchain =
            _ancestry_map.Values
            |> Seq.where (fun it -> it.Distance >= start_distance)
            |> Seq.sortBy (_.Distance)
            |> Seq.map (_.Syntax)
        Array.ofSeq subchain


type VarFormalOrAttrSyntax =
    { ID: AstNode<ID>
      TYPE: AstNode<TYPENAME> }


[<Sealed>]
type ClassSymbolCollector(_program_syntax: ProgramSyntax,
                          _class_node_map: IReadOnlyDictionary<TYPENAME, AstNode<ClassSyntax>>,
                          _source: Source,
                          _diags: DiagnosticBag) =

    
    let _class_sym_map = Dictionary<TYPENAME, ClassSymbol>() 
    

    let resolveToClassNode (class_name_node: AstNode<TYPENAME>)
                           : AstNode<ClassSyntax> voption =
        let class_name = class_name_node.Syntax

        // Make sure it's not a reference to a system class that is not allowed in user code.
        if _class_sym_map.ContainsKey(class_name) &&
           (let class_sym = _class_sym_map[class_name]
            not class_sym.IsAllowedInUserCode && not class_sym.IsError)
        then
            _diags.Error(
                $"The type name '{class_name_node.Syntax}' is not allowed in user code",
                class_name_node.Span)
            
            ValueNone
        else

        if _class_node_map.ContainsKey(class_name)
        then
            ValueSome (_class_node_map[class_name])
        else

        if not (_class_sym_map.ContainsKey(class_name))
        then
            // We could not find a syntax node or symbol corresponding to `class_name`.
            _class_sym_map.Add(class_name, SpecialClasses.Error)
            _diags.Error(
                $"The type name '{class_name}' could not be found (is an input file missing?)",
                class_name_node.Span)

        // Else: it's a basic class, that only exists as a symbol and is not present in the ast.
        ValueNone


    let mkAttrSym (class_syntax: ClassSyntax)
                  (attr_node: AstNode<VarFormalOrAttrSyntax>)
                  (index: int): AttrSymbol =
        let attr_syntax = attr_node.Syntax
        { AttrSymbol.Name = attr_syntax.ID.Syntax
          Type = attr_syntax.TYPE.Syntax
          DeclaringClass = class_syntax.NAME.Syntax
          Index = index
          SyntaxSpan = attr_node.Span }
    

    let mkAttrSyms (class_syntax: ClassSyntax)
                   (super: ClassSymbol) =
        let attr_syms = Dictionary<ID, AttrSymbol>(super.Attrs)

        let addAttrSym (attr_node: AstNode<VarFormalOrAttrSyntax>) =
            let attr_syntax = attr_node.Syntax

            if attr_syms.ContainsKey(attr_syntax.ID.Syntax)
            then
                let prev_attr_sym = attr_syms[attr_syntax.ID.Syntax]
                let message =
                    $"The class '{class_syntax.NAME.Syntax}' " +
                    $"already contains an attribute '{attr_syntax.ID.Syntax}' " +
                    $"[declared in '{prev_attr_sym.DeclaringClass}' at {_source.Map(prev_attr_sym.SyntaxSpan.First)}]"

                _diags.Error(message, attr_node.Span)
            else

            resolveToClassNode attr_syntax.TYPE |> ignore

            let attr_sym = mkAttrSym class_syntax
                                       attr_node
                                       (*index=*)attr_syms.Count
            attr_syms.Add(attr_sym.Name, attr_sym)

        class_syntax.VarFormals
        |> Seq.iter (fun varformal_node ->
            addAttrSym (varformal_node.Map(fun it -> { VarFormalOrAttrSyntax.ID=it.ID
                                                       TYPE=it.TYPE
                                                       (*Initial=ValueNone*) })))

        for feature_node in class_syntax.Features do
            match feature_node with
            | { Syntax = FeatureSyntax.Attr attr_syntax } ->
                addAttrSym (feature_node.Map(fun _ ->
                    { VarFormalOrAttrSyntax.ID=attr_syntax.ID
                      TYPE=attr_syntax.TYPE
                      (*Initial=ValueSome attr_syntax.Initial*) }))
            | _ -> ()

        attr_syms :> IReadOnlyDictionary<_, _>
    
    
    let mkParamSyms (formal_syntaxes: AstNode<FormalSyntax>[])
                      (get_message: (*formal:*)AstNode<FormalSyntax> -> (*prev_formal:*)AstNode<FormalSyntax> -> string)
                      : FormalSymbol[] =
        let formal_node_map = Dictionary<ID, AstNode<FormalSyntax>>()

        formal_syntaxes
        |> Array.iter (fun formal_node ->
            let formal_syntax = formal_node.Syntax
            if formal_node_map.ContainsKey(formal_syntax.ID.Syntax)
            then
                let prev_formal_node = formal_node_map[formal_syntax.ID.Syntax]
                _diags.Error(get_message formal_node prev_formal_node, formal_node.Span)
            else
                
            resolveToClassNode formal_syntax.TYPE |> ignore
            formal_node_map.Add(formal_syntax.ID.Syntax, formal_node)
        )

        let param_syms = formal_node_map.Values
                         |> Seq.mapi (fun i it -> { FormalSymbol.Name = it.Syntax.ID.Syntax
                                                    Type = it.Syntax.TYPE.Syntax
                                                    Index = i
                                                    SyntaxSpan = it.Span })
        Array.ofSeq param_syms
    
    
    let mkMethodParamSyms (method_syntax: MethodSyntax): FormalSymbol[] =
        mkParamSyms ((*formal_syntaxes=*)method_syntax.Formals)
                      ((*get_message=*)fun formal prev_formal ->
                          $"The method '{method_syntax.ID.Syntax}' " +
                          $"already contains a formal '{formal.Syntax.ID.Syntax}' at {prev_formal.Span}")


    let mkMethodSym (class_syntax: ClassSyntax)
                    (method_node: AstNode<MethodSyntax>)
                    (index: int): MethodSymbol =
        let method_syntax = method_node.Syntax
        let param_syms = mkMethodParamSyms method_syntax

        { MethodSymbol.Name = method_syntax.ID.Syntax
          Formals = param_syms
          ReturnType = method_syntax.RETURN.Syntax
          Override = method_syntax.Override
          DeclaringClass = class_syntax.NAME.Syntax
          Index = index
          SyntaxSpan = method_node.Span }
        
        
    let mkMethodSyms (class_syntax: ClassSyntax)
                     (super: ClassSymbol) =
        let method_syms = Dictionary<ID, MethodSymbol>(super.Methods)
        
        let addMethodSym (method_node: AstNode<MethodSyntax>): unit =
            let method_syntax = method_node.Syntax

            if not method_syntax.Override && method_syms.ContainsKey(method_syntax.ID.Syntax)
            then
                let prev_method_sym = method_syms[method_syntax.ID.Syntax]
                let sb_message =
                    StringBuilder().AppendFormat(
                                        "The class '{0}' already contains a method '{1}' [declared in '{2}' at {3}]",
                                        class_syntax.NAME.Syntax,
                                        method_syntax.ID.Syntax,
                                        prev_method_sym.DeclaringClass,
                                        (_source.Map(prev_method_sym.SyntaxSpan.First)))

                if class_syntax.NAME.Syntax <> prev_method_sym.DeclaringClass
                then
                    sb_message.Append(". Use 'override def' to override it") |> ignore

                _diags.Error(sb_message.ToString(), method_node.Span)
            else

            if method_syntax.Override && not (method_syms.ContainsKey(method_syntax.ID.Syntax))
            then
                _diags.Error(
                    $"Cannot override a method '{method_syntax.ID.Syntax}' because it was not previously defined",
                    method_node.Span)
            else

            resolveToClassNode method_syntax.RETURN |> ignore

            let index = if method_syms.ContainsKey(method_syntax.ID.Syntax)
                        then method_syms[method_syntax.ID.Syntax].Index
                        else method_syms.Count
            let mi = mkMethodSym class_syntax method_node index

            method_syms[mi.Name] <- mi

        for feature_node in class_syntax.Features do
            match feature_node with
            | { Syntax = FeatureSyntax.Method method_syntax } ->
                addMethodSym (feature_node.Map(fun _ -> method_syntax))
            | _ -> ()

        method_syms :> IReadOnlyDictionary<_, _>


    let mkCtorParamSyms (class_syntax: ClassSyntax): FormalSymbol[] =
        let formal_syntaxes = class_syntax.VarFormals
                              |> Array.map (fun vf_node -> vf_node.Map(fun vf -> vf.AsFormalSyntax()))
        mkParamSyms ((*formal_syntaxes=*)formal_syntaxes)
                      ((*get_message=*)fun formal prev_formal ->
                          $"The constructor of class '{class_syntax.NAME.Syntax}' " +
                          $"already contains a var formal '{formal.Syntax.ID.Syntax}' at {prev_formal.Span}")


    let mkCtorSym (class_syntax: ClassSyntax): MethodSymbol =
        let formal_syms = mkCtorParamSyms class_syntax
        
        { MethodSymbol.Name = ID ".ctor"
          Formals = formal_syms
          ReturnType = class_syntax.NAME.Syntax
          Override = false
          DeclaringClass = class_syntax.NAME.Syntax
          Index = -1
          SyntaxSpan = Span.Virtual }
    

    let mkClassSym (class_node: AstNode<ClassSyntax>)
                     (super: ClassSymbol) : ClassSymbol =
        let class_syntax = class_node.Syntax
        let attr_syms = mkAttrSyms class_syntax super
        let method_syms = mkMethodSyms class_syntax super
        
        { ClassSymbol.Name = class_syntax.NAME.Syntax
          Super = super.Name
          Ctor = mkCtorSym class_syntax
          Attrs = attr_syms
          Methods = method_syms
          Tag = _class_sym_map.Values |> Seq.where (_.IsAllowedInUserCode) |> Seq.length
          SyntaxSpan = class_node.Span }
        
        
    let addClassSymToMap (classsym: ClassSymbol): unit =
        _class_sym_map.Add(classsym.Name, classsym)


    let addToInheritanceChain (inheritance_chain: InheritanceChain)
                              (class_syntax: ClassSyntax)
                              : LcResult<unit> =
        if inheritance_chain.Add(class_syntax)
        then
            Ok ()
        else

        // An inheritance cycle detected
        let sb_message = StringBuilder("A circular superclass dependency detected: '")

        let cycle = inheritance_chain.Subchain(class_syntax)
        cycle |> Seq.iter (fun it -> sb_message.AppendFormat("{0} -> ", it.NAME.Syntax) |> ignore)

        sb_message.AppendFormat("{0}'", cycle[0].NAME.Syntax) |> ignore

        _diags.Error(sb_message.ToString(), class_syntax.ExtendsSyntax.SUPER.Span)
        Error


    let rec doCollectClassSym (inheritance_chain: InheritanceChain)
                                 (class_name_node: AstNode<TYPENAME>)
                                 : ClassSymbol =
        let class_name = class_name_node.Syntax
 
        // See if we collected a symbol for this class earlier or it's a basic class.
        // If so, don't try to collect again.
        if _class_sym_map.ContainsKey(class_name)
        then
            let class_sym = _class_sym_map[class_name_node.Syntax]
            if not class_sym.IsAllowedInUserCode && not class_sym.IsError
            then
                _diags.Error(
                    $"The type name '{class_name_node.Syntax}' is not allowed in user code",
                    class_name_node.Span)
            
            class_sym
        else
        
        // See if a class with the name supplied to us exists in user code.
        let class_node_opt = resolveToClassNode class_name_node
        if class_node_opt.IsNone
        then 
            SpecialClasses.Error
        else
            
        let class_node = class_node_opt.Value 
        
        // See if we're in an inheritance cycle.
        let result = addToInheritanceChain inheritance_chain
                                              class_node.Syntax
        match result with
        | Error ->
            SpecialClasses.Error
        | Ok _ ->

        // We didn't collect a symbol for this class previously.
        // And we aren't in an inheritance cycle.
        // To collect a symbol for this class we need to merge the class' syntax and its super's symbol.
        
        // Collect the super's symbol.
        let super_name_node = class_node.Syntax.ExtendsSyntax.SUPER
        let super_sym = doCollectClassSym inheritance_chain super_name_node
                                             
        if super_sym.IsError
        then
            // Remember we failed to collect a symbol for the current class.
            // So the next time we need it, we don't try to collect it again
            // and don't emit duplicate diags.
            _class_sym_map.Add(class_name, SpecialClasses.Error)
            SpecialClasses.Error
        else
            
        if super_sym.Is(BasicClasses.Int) || super_sym.Is(BasicClasses.Unit) ||
           super_sym.Is(BasicClasses.Boolean) || super_sym.Is(BasicClasses.String)
        then
            _diags.Error(
                $"Extending '{super_name_node.Syntax}' is not allowed",
                super_name_node.Span)
            SpecialClasses.Error
        else

        // Merge the current class' syntax and its super's symbol.
        let class_sym = mkClassSym class_node super_sym
        
        // Add the collected symbol to the map,
        // so the next time we need it, we can reuse it instead of collecting from scratch.
        addClassSymToMap class_sym
        class_sym
        
        
    let collectClassSym (class_name_node: AstNode<TYPENAME>): unit =
        if not (_class_sym_map.ContainsKey(class_name_node.Syntax))
        then
            doCollectClassSym (InheritanceChain()) class_name_node |> ignore
        else

        let class_sym = _class_sym_map[class_name_node.Syntax]
        if class_sym.IsError
        then
            ()
        else

        if not class_sym.IsAllowedInUserCode
        then
            _diags.Error(
                $"The type name '{class_name_node.Syntax}' is not allowed in user code",
                class_name_node.Span)
        else

        if class_sym.IsVirtual
        then
            _diags.Error(
                $"The type name '{class_name_node.Syntax}' conflicts with the name of built-in type",
                class_name_node.Span)


    member this.Collect(): IReadOnlyDictionary<TYPENAME, ClassSymbol> =
        // The following three aren't allowed in user code.
        addClassSymToMap BasicClasses.Nothing
        addClassSymToMap BasicClasses.Null
        addClassSymToMap BasicClasses.Symbol
        
        // Just normal basic classes.
        addClassSymToMap BasicClasses.Any
        addClassSymToMap BasicClasses.Unit
        addClassSymToMap BasicClasses.Int
        addClassSymToMap BasicClasses.String
        addClassSymToMap BasicClasses.Boolean
        addClassSymToMap BasicClasses.ArrayAny
        addClassSymToMap BasicClasses.IO
        
        _program_syntax.Classes |> Array.iter (fun class_node ->
            collectClassSym class_node.Syntax.NAME)
        
        _class_sym_map :> IReadOnlyDictionary<_, _>
