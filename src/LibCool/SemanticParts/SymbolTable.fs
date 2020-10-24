namespace LibCool.SemanticParts


open System.Collections.Generic
open LibCool.SourceParts
open LibCool.AstParts
open LibCool.SemanticParts


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
    static member This(class_syntax: ClassSyntax) =
        { Symbol.Name = ID "this"
          Type = class_syntax.NAME.Syntax
          Index = 0
          SyntaxSpan = Span.Invalid
          Kind = SymbolKind.Formal }        

[<Sealed>]
type Scope() =    
    
    
    let _syms = Dictionary<ID, Symbol>()
    
    
    member this.Add(sym: Symbol): unit =
        _syms.Add(sym.Name, sym)
    
    
    member this.Contains(name: ID): bool = _syms.ContainsKey(name)
    
    
    member this.Resolve(name: ID): Symbol = _syms.[name]
    member this.TryResolve(name: ID): Symbol voption =
        if _syms.ContainsKey(name)
        then ValueSome (_syms.[name])
        else ValueNone


[<Sealed>]
type SymbolTable(_class_sym: ClassSymbol) =


    let _method_sym_counts = List<int>()
    let _scopes = List<Scope>()
    
    
    member this.MethodSymCount
        with get() = _method_sym_counts.[_method_sym_counts.Count - 1]
        and private set count = _method_sym_counts.[_method_sym_counts.Count - 1] <- count


    member private this.CurrentScopeLevel = _scopes.Count - 1
    member this.CurrentScope = _scopes.[this.CurrentScopeLevel]
    
    
    member this.EnterMethod(): unit =
        _method_sym_counts.Add(0)
        _scopes.Add(Scope())
    
    
    member this.EnterBlock(): unit =
        _scopes.Add(Scope())
    

    member private this.EnterScope(): unit =
        _scopes.Add(Scope())
    
    
    member private this.LeaveScope(): unit =
        _scopes.RemoveAt(this.CurrentScopeLevel)
    
    
    member this.LeaveBlock(): unit =
        _scopes.RemoveAt(this.CurrentScopeLevel)
    
    
    member this.LeaveMethod(): unit =
        _method_sym_counts.RemoveAt(this.CurrentScopeLevel)
        _scopes.RemoveAt(this.CurrentScopeLevel)
        
        
    member this.Add(sym: Symbol): unit =
        this.CurrentScope.Add(sym)
        // Multiple match expression branches can bind an id
        // to the matched expression's value.
        // But at runtime, only one branch will execute,
        // as a result we need only one temporary on stack.
        // That's why we don't increase `MethodSymCount`,
        // if `sym.Index < this.MethodSymCount`.
        if sym.Index >= this.MethodSymCount
        then
            this.MethodSymCount <- this.MethodSymCount + 1
    
    
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
