namespace LibCool.Frontend.SemanticParts


open System.Collections.Generic
open LibCool.SourceParts
open LibCool.AstParts
open LibCool.Frontend.SemanticParts


[<Sealed>]
type TypeTable(_class_sym_map: IReadOnlyDictionary<TYPENAME, ClassSymbol>) =


    member this.Resolve(typename: TYPENAME): ClassSymbol =
        _class_sym_map.[typename]


    member this.TryResolve(typename: TYPENAME): ClassSymbol voption =
        if _class_sym_map.ContainsKey(typename)
        then ValueSome (_class_sym_map.[typename])
        else ValueNone
        
        
    member this.Conforms(ancestor: ClassSymbol, descendant: ClassSymbol): bool =
        if ancestor.Name = BasicClassSymbols.Any.Name
        then
            true
        else
        
        // let mutable super = descendant
        // let mutable conforms = ValueNone
        // while conforms.IsNone do
        //     if super.Name = BasicClassSymbols.Any.Name
        //     then
        //         conforms <- ValueSome false
        //     else
        //         
        //     if ancestor.Name = super.Name
        //     then
        //         conforms <- ValueSome true
        //     else
        //         
        //     super <- this.Resolve(super.Super)
        // 
        // conforms.Value
        
        let rec conforms (descendant: ClassSymbol): bool =
            if descendant.Name = BasicClassSymbols.Any.Name
            then
                false
            else
            
            if ancestor.Name = descendant.Name
            then
                true
            else
                
            conforms (this.Resolve(descendant.Super))
            
        conforms descendant


type SymbolKind
    = Attr
    | Formal
    | Var


type Symbol =
    { Name: ID
      Type: TYPENAME
      Index: int
      SyntaxSpan: Span
      Kind: SymbolKind }
    with
    member this.Is(kind: SymbolKind): bool =
        this.Kind = kind
    static member Of(var_node: AstNode<VarSyntax>, index: int): Symbol =
        { Symbol.Name = var_node.Syntax.ID.Syntax
          Type = var_node.Syntax.TYPE.Syntax
          Index = index
          SyntaxSpan = var_node.Span
          Kind = SymbolKind.Var }
    static member Of(formal_node: AstNode<FormalSyntax>, index: int): Symbol =
        { Symbol.Name = formal_node.Syntax.ID.Syntax
          Type = formal_node.Syntax.TYPE.Syntax
          Index = index
          SyntaxSpan = formal_node.Span
          Kind = SymbolKind.Formal }
    static member Of(attr_sym: AttrSymbol): Symbol =
        { Symbol.Name = attr_sym.Name
          Type = attr_sym.Type
          Index = attr_sym.Index
          SyntaxSpan = attr_sym.SyntaxSpan
          Kind = SymbolKind.Attr }
        

[<Sealed>]
type Scope() =    
    
    
    let _visible_syms = Dictionary<ID, Symbol>()
    
    
    member this.AddVisible(sym: Symbol): unit =
        _visible_syms.Add(sym.Name, sym)
    
    
    member this.IsVisible(name: ID): bool = _visible_syms.ContainsKey(name)
    
    
    member this.Resolve(name: ID): Symbol = _visible_syms.[name]
    member this.TryResolve(name: ID): Symbol voption =
        if _visible_syms.ContainsKey(name)
        then ValueSome (_visible_syms.[name])
        else ValueNone


[<Sealed>]
type SymbolTable(_class_sym: ClassSymbol) =


    let _scopes = List<Scope>()
    
    member private this.LastScopeIndex = _scopes.Count - 1
    member this.CurrentScope = _scopes.[this.LastScopeIndex]
    
    
    member this.EnterScope(): unit =
        _scopes.Add(Scope())
    
    
    member this.LeaveScope(): unit =
        _scopes.RemoveAt(this.LastScopeIndex)
    
    
    member this.Resolve(name: ID): Symbol =
        match this.TryResolve(name) with
        | ValueSome sym -> sym
        | ValueNone     -> invalidOp (sprintf "Could not resolve a symbol '%O'" name)
        
        
    member this.TryResolve(name: ID): Symbol voption =
        
        let rec try_resolve (scope_level: int): Symbol voption =
            if scope_level < 0
            then
                ValueNone
            else
                
            let scope = _scopes.[scope_level]
            let sym_opt = scope.TryResolve(name)
            if sym_opt.IsSome
            then
                sym_opt
            else
            
            try_resolve (scope_level - 1)
            
            
        let sym_opt = try_resolve ((*scope_level=*)this.LastScopeIndex)
        if sym_opt.IsSome
        then
            sym_opt
        else
            
        if _class_sym.Attrs.ContainsKey(name)
        then
            ValueSome (Symbol.Of(_class_sym.Attrs.[name]))
        else
            
        ValueNone
