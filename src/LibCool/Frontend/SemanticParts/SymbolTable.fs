namespace LibCool.Frontend.SemanticParts


open System.Collections.Generic
open LibCool.SourceParts
open LibCool.AstParts
open LibCool.Frontend.SemanticParts


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
    static member ThisOf(class_syntax: ClassSyntax) =
        { Symbol.Name = ID "this"
          Type = class_syntax.NAME.Syntax
          Index = 0
          SyntaxSpan = Span.Invalid
          Kind = SymbolKind.Formal }        

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
    
    
    member private this.CurrentScopeLevel = _scopes.Count - 1
    member this.CurrentScope = _scopes.[this.CurrentScopeLevel]
    
    
    member this.EnterScope(): unit =
        _scopes.Add(Scope())
    
    
    member this.LeaveScope(): unit =
        _scopes.RemoveAt(this.CurrentScopeLevel)
    
    
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
            
            
        let sym_opt = try_resolve ((*scope_level=*)this.CurrentScopeLevel)
        if sym_opt.IsSome
        then
            sym_opt
        else
            
        if _class_sym.Attrs.ContainsKey(name)
        then
            ValueSome (Symbol.Of(_class_sym.Attrs.[name]))
        else
            
        ValueNone
