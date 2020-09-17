namespace LibCool.Frontend.SemanticParts


open System.Collections.Generic
open System.Text
open LibCool.AstParts
open LibCool.DiagnosticParts
open LibCool.SourceParts
open AstExtensions
open Symbols
    

[<Sealed>]
type private InheritanceChain() =
    let _super_map = Dictionary<
                        Ast.TYPENAME,
                        struct {| Syntax: Ast.ClassDecl; Distance: int |}>()
                        
                        
    member this.AddSuper(super_syntax: Ast.ClassDecl): bool =
        if _super_map.ContainsKey(super_syntax.NAME.Value)
        then
            false
        else
        
        let distance = _super_map.Count + 1
        _super_map.Add(super_syntax.NAME.Value,
                       struct {| Syntax = super_syntax; Distance = distance |})
        
        true
        
        
    member this.Subchain(start: Ast.ClassDecl) =
        let start_distance = _super_map.[start.NAME.Value].Distance
        
        let subchain =
            _super_map.Values
            |> Seq.where (fun it -> it.Distance >= start_distance)
            |> Seq.sortBy (fun it -> it.Distance)
            |> Seq.map (fun it -> it.Syntax)
        Array.ofSeq subchain


type VarFormalOrAttrSyntax =
    { ID: Ast.Node<Ast.ID>
      TYPE: Ast.Node<Ast.TYPENAME>
      Initial: Ast.Node<Ast.AttrInitial> voption }


[<Sealed>]
type ClassSymbolCollector(_program_syntax: Ast.Program,
                                  _classdecl_node_map: IReadOnlyDictionary<Ast.TYPENAME, Ast.Node<Ast.ClassDecl>>,
                                  _source: Source,
                                  _diags: DiagnosticBag) =

    
    let _class_sym_map = Dictionary<Ast.TYPENAME, ClassSymbol>() 
    

    let resolve_to_class_syntax (class_name_node: Ast.Node<Ast.TYPENAME>)
                                : Ast.Node<Ast.ClassDecl> voption =
        let class_name = class_name_node.Value
        
        if _classdecl_node_map.ContainsKey(class_name)
        then
            ValueSome (_classdecl_node_map.[class_name])
        else

        if not (_class_sym_map.ContainsKey(class_name))
        then
            _class_sym_map.Add(class_name, BasicClassSymbols.Error)
            _diags.Error(
                sprintf "The type name '%O' could not be found (is an input file missing?)" class_name,
                class_name_node.Span)

        ValueNone


    let mk_attr_sym (classdecl_syntax: Ast.ClassDecl)
                    (attr_node: Ast.Node<VarFormalOrAttrSyntax>)
                    (index: int): AttrSymbol =
        let attr_syntax = attr_node.Value
        { AttrSymbol.Name = attr_syntax.ID.Value
          Type = attr_syntax.TYPE.Value
          DeclaringClass = classdecl_syntax.NAME.Value
          Index = index
          SyntaxSpan = attr_node.Span }
    

    let mk_attr_syms (classdecl_syntax: Ast.ClassDecl)
                     (super: ClassSymbol) =
        let attr_syms = Dictionary<Ast.ID, AttrSymbol>(super.Attrs)

        let add_attr_sym (attr_node: Ast.Node<VarFormalOrAttrSyntax>) =
            let attr_syntax = attr_node.Value
            
            if attr_syms.ContainsKey(attr_syntax.ID.Value)
            then
                let prev_attr_sym = attr_syms.[attr_syntax.ID.Value]
                let message =
                    sprintf "The class '%O' already contains an attribute '%O' [declared in '%O' at %O]"
                            classdecl_syntax.NAME.Value
                            attr_syntax.ID.Value
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
                                                         Initial = ValueNone })))
        
        classdecl_syntax.ClassBody
        |> Seq.where (fun it -> it.Value.IsAttr)
        |> Seq.iter (fun feature_node ->
            add_attr_sym (feature_node.Map(fun it -> let attr_syntax = it.AsAttrInfo
                                                     { VarFormalOrAttrSyntax.ID=attr_syntax.ID
                                                       TYPE=attr_syntax.TYPE
                                                       Initial=ValueSome attr_syntax.Initial })))
        
        attr_syms :> IReadOnlyDictionary<_, _>
    
    
    let mk_param_syms (formal_syntaxes: Ast.Node<Ast.Formal>[])
                      (get_message: (*formal:*)Ast.Node<Ast.Formal> -> (*prev_formal:*)Ast.Node<Ast.Formal> -> string)
                      : ParamSymbol[] =
        let formal_node_map = Dictionary<Ast.ID, Ast.Node<Ast.Formal>>()

        formal_syntaxes
        |> Array.iter (fun formal_node ->
            let formal_syntax = formal_node.Value
            if formal_node_map.ContainsKey(formal_syntax.ID.Value)
            then
                let prev_formal_node = formal_node_map.[formal_syntax.ID.Value]
                _diags.Error(get_message formal_node prev_formal_node, formal_node.Span)
            else
                
            resolve_to_class_syntax formal_syntax.TYPE |> ignore
            formal_node_map.Add(formal_syntax.ID.Value, formal_node)
        )

        let param_syms = formal_node_map.Values
                         |> Seq.mapi (fun i it -> { ParamSymbol.Name = it.Value.ID.Value
                                                    Type = it.Value.TYPE.Value
                                                    Index = i
                                                    SyntaxSpan = it.Span })
        Array.ofSeq param_syms
    
    
    let mk_method_param_syms (method_syntax: Ast.MethodInfo) =
        mk_param_syms ((*formal_syntaxes=*)method_syntax.Formals)
                      ((*get_message=*)fun formal prev_formal ->
                          sprintf "The method '%O' already contains a formal '%O' at %O"
                                   method_syntax.ID.Value
                                   formal.Value.ID.Value
                                   prev_formal.Span)


    let mk_method_sym (classdecl_syntax: Ast.ClassDecl)
                      (method_node: Ast.Node<Ast.MethodInfo>)
                      (index: int): MethodSymbol =
        let method_syntax = method_node.Value
        let param_syms = mk_method_param_syms method_syntax

        { MethodSymbol.Name = method_syntax.ID.Value
          Params = param_syms
          ReturnType = method_syntax.RETURN.Value
          Override = method_syntax.Override
          DeclaringClass = classdecl_syntax.NAME.Value
          Index = index
          SyntaxSpan = method_node.Span }
        
        
    let mk_method_syms (classdecl_syntax: Ast.ClassDecl)
                       (super: ClassSymbol) =
        let method_syms = Dictionary<Ast.ID, MethodSymbol>(super.Methods)
        
        let add_method_sym (method_node: Ast.Node<Ast.MethodInfo>): unit =
            let method_syntax = method_node.Value
            
            if not method_syntax.Override && method_syms.ContainsKey(method_syntax.ID.Value)
            then
                let prev_method_sym = method_syms.[method_syntax.ID.Value]
                let sb_message =
                    StringBuilder().AppendFormat(
                                        "The class '{0}' already contains a method '{1}' [declared in '{2}' at {3}]",
                                        classdecl_syntax.NAME.Value,
                                        method_syntax.ID.Value,
                                        prev_method_sym.DeclaringClass,
                                        (_source.Map(prev_method_sym.SyntaxSpan.First)))
                
                if classdecl_syntax.NAME.Value <> prev_method_sym.DeclaringClass
                then
                    sb_message.Append(". Use 'override def' to override it") |> ignore
                
                _diags.Error(sb_message.ToString(), method_node.Span)
            else
                
            if method_syntax.Override && not (method_syms.ContainsKey(method_syntax.ID.Value))
            then
                _diags.Error(
                    sprintf "Cannot override a method '%O' because it was not previously defined" method_syntax.ID.Value,
                    method_node.Span)
            else
                
            resolve_to_class_syntax method_syntax.RETURN |> ignore
            
            let index = if method_syms.ContainsKey(method_syntax.ID.Value)
                        then method_syms.[method_syntax.ID.Value].Index
                        else method_syms.Count
            let mi = mk_method_sym classdecl_syntax method_node index
            
            method_syms.[mi.Name] <- mi

        classdecl_syntax.ClassBody
        |> Seq.where (fun feature_node -> feature_node.Value.IsMethod)
        |> Seq.iter (fun feature_node -> add_method_sym (feature_node.Map(fun it -> it.AsMethodInfo))) 

        method_syms :> IReadOnlyDictionary<_, _>


    let mk_ctor_param_syms (classdecl_syntax: Ast.ClassDecl) =
        let formal_syntaxes = classdecl_syntax.VarFormals
                              |> Array.map (fun vf_node ->
                                  vf_node.Map(fun vf -> { Ast.Formal.ID = vf.ID
                                                          Ast.Formal.TYPE = vf.TYPE }))
        mk_param_syms ((*formal_syntaxes=*)formal_syntaxes)
                      ((*get_message=*)fun formal prev_formal ->
                          sprintf "The constructor of class '%O' already contains a var formal '%O' at %O"
                                   classdecl_syntax.NAME.Value
                                   formal.Value.ID.Value
                                   prev_formal.Span)


    let mk_ctor_sym (classdecl_syntax: Ast.ClassDecl): MethodSymbol =
        let param_syms = mk_ctor_param_syms classdecl_syntax
        
        { MethodSymbol.Name = Ast.ID ".ctor"
          Params = param_syms
          ReturnType = classdecl_syntax.NAME.Value
          Override = false
          DeclaringClass = classdecl_syntax.NAME.Value
          Index = -1
          SyntaxSpan = Span.Invalid }
    

    let mk_class_sym (classdecl_node: Ast.Node<Ast.ClassDecl>)
                     (super: ClassSymbol) : ClassSymbol =
        let classdecl_syntax = classdecl_node.Value
        let attr_syms = mk_attr_syms classdecl_syntax super 
        let method_syms = mk_method_syms classdecl_syntax super
        
        { ClassSymbol.Name = classdecl_syntax.NAME.Value
          Super = super.Name
          Ctor = mk_ctor_sym classdecl_syntax
          Attrs = attr_syms
          Methods = method_syms
          SyntaxSpan = classdecl_node.Span }
        
        
    let add_class_sym_to_map (classsym: ClassSymbol): unit =
        _class_sym_map.Add(classsym.Name, classsym)


    let collect_class_sym (class_name_node: Ast.Node<Ast.TYPENAME>): unit =
        let inheritance_chain = InheritanceChain()
        
        let rec do_collect_class_sym (class_name_node: Ast.Node<Ast.TYPENAME>)
                                     (is_super: bool): ClassSymbol =
            let class_name = class_name_node.Value
     
            if _class_sym_map.ContainsKey(class_name)
            then
                _class_sym_map.[class_name]
            else
            
            let classdecl_node_opt = resolve_to_class_syntax class_name_node
            if classdecl_node_opt.IsNone
            then 
                BasicClassSymbols.Error
            else
                
            let classdecl_node = classdecl_node_opt.Value 
            
            let cycle_detected =
                if not is_super
                then
                    false
                else
                
                let classdecl_syntax = classdecl_node.Value
                if inheritance_chain.AddSuper(classdecl_syntax)
                then
                    false
                else

                // An inheritance cycle detected
                let sb_message = StringBuilder("A circular superclass dependency detected: '")

                let cycle = inheritance_chain.Subchain(classdecl_syntax)
                
                cycle |> Seq.iter (fun it -> sb_message.AppendFormat("{0} -> ", it.NAME.Value) |> ignore)
                sb_message.AppendFormat("{0}'", cycle.[0].NAME.Value) |> ignore

                _diags.Error(sb_message.ToString(), classdecl_syntax.ExtendsInfo.SUPER.Span)
                true
            
            if cycle_detected
            then
                BasicClassSymbols.Error
            else

            let super_sym = do_collect_class_sym classdecl_node.Value.ExtendsInfo.SUPER ((*is_super=*)true)
            if super_sym.IsError
            then
                BasicClassSymbols.Error
            else

            let class_sym = mk_class_sym classdecl_node super_sym
            
            add_class_sym_to_map class_sym
            class_sym
            
        do_collect_class_sym class_name_node ((*is_super=*)false) |> ignore
    
    
    member this.Collect(): IReadOnlyDictionary<Ast.TYPENAME, ClassSymbol> =
        add_class_sym_to_map BasicClassSymbols.Any
        add_class_sym_to_map BasicClassSymbols.Unit
        add_class_sym_to_map BasicClassSymbols.Int
        add_class_sym_to_map BasicClassSymbols.String
        add_class_sym_to_map BasicClassSymbols.Boolean
        add_class_sym_to_map BasicClassSymbols.ArrayAny
        add_class_sym_to_map BasicClassSymbols.IO
        add_class_sym_to_map BasicClassSymbols.Symbol
        
        _program_syntax.ClassDecls |> Array.iter (fun classdecl_node ->
            let classdecl_syntax = classdecl_node.Value
            collect_class_sym classdecl_syntax.NAME)
        
        _class_sym_map :> IReadOnlyDictionary<_, _>
