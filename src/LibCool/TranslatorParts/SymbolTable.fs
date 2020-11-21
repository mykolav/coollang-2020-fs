namespace LibCool.TranslatorParts


open System.Collections.Generic
open LibCool.SourceParts
open LibCool.AstParts
open LibCool.TranslatorParts


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


type FrameInfo() =
    
    
    member val ActualsCount: int = 0 with get, set
    member val VarsCount: int = 0 with get, set

    
    // We pass these actuals in regs,
    // but still allocate space and store them in frame.
    // Otherwise non-leaf functions could not re-use regs
    // to pass actuals to functions called by them. 
    member this.ActualsInFrameCount: int =
        if this.ActualsCount >= SysVAmd64AbiFacts.ActualRegs.Length
        then SysVAmd64AbiFacts.ActualRegs.Length
        else this.ActualsCount

    
    member this.ActualsSizeInBytes: int = this.ActualsInFrameCount * 8
    member this.VarsSizeInBytes: int = this.VarsCount * 8
    member this.CalleeSavedRegsSizeInBytes: int = 5 (* number of the callee-saved regs *) * 8


    member this.FrameSizeInBytes: int =
        this.ActualsSizeInBytes +
        this.VarsSizeInBytes +
        this.CalleeSavedRegsSizeInBytes


    member this.PadSizeInBytes: int =
        if (this.FrameSizeInBytes % 16) = 0
        then 0
        else 16 - (this.FrameSizeInBytes % 16)
                            

    member val ActualsOutOfFrameOffset: int = 16 // skip saved %rbp, and return addr
    member val ActualsOffsetInBytes: int = 0
    member this.CalleeSavedRegsOffsetInBytes: int = this.ActualsSizeInBytes + this.VarsSizeInBytes
    member this.VarsOffset: int = this.ActualsInFrameCount * 8


[<Sealed>]
type SymbolTable(_class_sym: ClassSymbol) =


    let _method_frame = List<FrameInfo>()
    let _scopes = List<Scope>()
    
    
    member this.Frame
        with get() = _method_frame.[_method_frame.Count - 1]
        and private set count = _method_frame.[_method_frame.Count - 1] <- count


    member private this.CurrentScopeLevel = _scopes.Count - 1
    member this.CurrentScope = _scopes.[this.CurrentScopeLevel]
    
    
    member this.EnterMethod(): unit =
        _method_frame.Add(FrameInfo())
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
        _method_frame.RemoveAt(this.CurrentScopeLevel)
        _scopes.RemoveAt(this.CurrentScopeLevel)
        
        
    member this.AddFormal(sym: Symbol): unit =
        this.CurrentScope.Add(sym)
        this.Frame.ActualsCount <- this.Frame.ActualsCount + 1
        
        
    member this.AddVar(sym: Symbol): unit =
        this.CurrentScope.Add(sym)
        // Multiple match expression branches can bind an id
        // to the matched expression's value.
        // But at runtime, only one branch will execute,
        // as a result we need only one temporary on stack.
        // That's why we don't increase `MethodSyms.VarsCount`,
        // if `sym.Index < this.MethodSym.VarCount`.
        if sym.Index >= this.Frame.VarsCount
        then
            this.Frame.VarsCount <- this.Frame.VarsCount + 1
    
    
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
