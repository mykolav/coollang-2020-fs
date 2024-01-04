namespace rec LibCool.SemanticParts


open System.Collections.Generic
open LibCool.AstParts
open LibCool.SourceParts


type AttrSymbol =
    { Name: ID
      Type: TYPENAME
      DeclaringClass: TYPENAME
      Index: int
      SyntaxSpan: Span }


type FormalSymbol =
    { Name: ID
      Type: TYPENAME
      Index: int
      SyntaxSpan: Span }


type MethodSymbol =
    { Name: ID
      Formals: FormalSymbol[]
      ReturnType: TYPENAME
      Override: bool
      DeclaringClass: TYPENAME
      Index: int
      SyntaxSpan: Span }
    member this.IsVirtual: bool = this.SyntaxSpan = Span.Virtual
    static member Virtual(name: ID,
                          return_type: TYPENAME,
                          ?is_override: bool,
                          ?formals: (ID * TYPENAME)[]): MethodSymbol =

        let is_override = defaultArg is_override false
        let formals = defaultArg formals [||]
        
        { MethodSymbol.Name = name
          Formals = formals |> Array.mapi (fun i (id, ty) -> { FormalSymbol.Name = id
                                                               Type = ty
                                                               Index = i+1
                                                               SyntaxSpan = Span.Virtual })
          ReturnType = return_type
          Override = is_override
          DeclaringClass = TYPENAME ""
          Index = -1
          SyntaxSpan = Span.Virtual }


type ClassSymbol =
    { Name: TYPENAME
      Super: TYPENAME
      Ctor: MethodSymbol
      Attrs: IReadOnlyDictionary<ID, AttrSymbol>
      Methods: IReadOnlyDictionary<ID, MethodSymbol>
      Tag: int
      SyntaxSpan: Span }
    
    
    member this.IsSpecial: bool = this.Tag = -1
    member this.IsVirtual: bool = this.SyntaxSpan = Span.Virtual
    
    
    member this.Is(class_sym: ClassSymbol): bool = this.Name = class_sym.Name
    
    
    static member Virtual(class_name: TYPENAME,
                          tag: int,
                          ?ctor_formals: (ID * TYPENAME)[],
                          ?super: ClassSymbol,
                          ?methods: MethodSymbol[]): ClassSymbol =
        let method_map = Dictionary<ID, MethodSymbol>()

        let (super_name, super_methods) =
            match super with
            | Some sym -> sym.Name, sym.Methods.Values
            | None -> TYPENAME "", Seq.empty
        
        super_methods |> Seq.iter (fun it -> if it.Name <> ID ".ctor"
                                             then method_map.Add(it.Name, it))        
        
        let method_syms = defaultArg methods [||]
        method_syms |> Seq.iter (fun it -> method_map.Add(it.Name, { it with Index = method_map.Count
                                                                             DeclaringClass = class_name }))
        
        let ctor = { MethodSymbol.Virtual(name=ID ".ctor",
                                          return_type=class_name,
                                          formals=defaultArg ctor_formals [||]) with Index = -1
                                                                                     DeclaringClass = class_name }
        
        // method_map.Add(ctor.Name, ctor)
        
        { Name = class_name
          Super = super_name
          Ctor = ctor
          Attrs = Map.empty
          Methods = method_map
          Tag = tag
          SyntaxSpan = Span.Virtual }


[<RequireQualifiedAccess>]
module BasicClassNames =
    let Nothing = TYPENAME "Nothing"
    let Any = TYPENAME "Any" 
    let Unit = TYPENAME "Unit" 
    let Int = TYPENAME "Int"
    let String = TYPENAME "String"
    let Boolean = TYPENAME "Boolean"
    let ArrayAny = TYPENAME "ArrayAny"
    let IO = TYPENAME "IO"
    let Symbol = TYPENAME "Symbol"
    let Null = TYPENAME "Null"


[<RequireQualifiedAccess>]
module BasicClasses =
    let Nothing = ClassSymbol.Virtual(class_name=BasicClassNames.Nothing, tag=(-1))
    
    
    let Symbol = ClassSymbol.Virtual(class_name=BasicClassNames.Symbol, tag=(-1))
    
    
    let Any =
        ClassSymbol.Virtual(
            class_name=BasicClassNames.Any,
            tag=0,
            methods=[|
                MethodSymbol.Virtual(ID "abort"(*name*),
                                     BasicClassNames.Nothing(*return_type*))

                MethodSymbol.Virtual(ID "equals"(*name*),
                                     BasicClassNames.Boolean(*return_type*),
                                     formals=[| (ID "other", BasicClassNames.Any) |])

                MethodSymbol.Virtual(ID "GC_collect"(*name*),
                                     BasicClassNames.Unit(*return_type*))

                MethodSymbol.Virtual(ID "GC_print_state"(*name*),
                                     BasicClassNames.Unit(*return_type*))
            |])
    
    
    let Unit = ClassSymbol.Virtual(class_name=BasicClassNames.Unit, tag=1, super=BasicClasses.Any) 
    
    
    let Int = ClassSymbol.Virtual(class_name=BasicClassNames.Int, tag=2, super=BasicClasses.Any)
    
    
    let String =
        ClassSymbol.Virtual(
            class_name=BasicClassNames.String,
            tag=3,
            super=BasicClasses.Any,
            methods=[|
                MethodSymbol.Virtual(ID "length"(*name*),
                                     BasicClassNames.Int(*return_type*))
                
                MethodSymbol.Virtual(ID "concat"(*name*),
                                     BasicClassNames.String(*return_type*),
                                     formals=[| (ID "suffix", BasicClassNames.String) |])
                
                MethodSymbol.Virtual(ID "substring"(*name*),
                                     BasicClassNames.String(*return_type*),
                                     formals=[| (ID "begin_index", BasicClassNames.Int)
                                                (ID "end_index", BasicClassNames.Int) |])
            |])
    
    
    let Boolean = ClassSymbol.Virtual(class_name=BasicClassNames.Boolean, tag=4, super=BasicClasses.Any)
    
    
    let ArrayAny =
        ClassSymbol.Virtual(
            class_name=BasicClassNames.ArrayAny,
            tag=5,
            ctor_formals=[| (ID "length", BasicClassNames.Int) |],
            super=BasicClasses.Any,
            methods=[|
                MethodSymbol.Virtual(ID "get"(*name*),
                                     BasicClassNames.Any(*return_type*),
                                     formals=[| (ID "index", BasicClassNames.Int) |])
                
                MethodSymbol.Virtual(ID "set"(*name*),
                                     BasicClassNames.Unit(*return_type*),
                                     formals=[| (ID "index", BasicClassNames.Int); (ID "value", BasicClassNames.Any) |])
                
                MethodSymbol.Virtual(ID "length"(*name*),
                                     BasicClassNames.Int(*return_type*))
            |])
    
    
    let IO =
        ClassSymbol.Virtual(
            class_name=BasicClassNames.IO,
            tag=6,
            super=BasicClasses.Any,
            methods=[|
                MethodSymbol.Virtual(ID "out_string"(*name*),
                                     BasicClassNames.Unit(*return_type*),
                                     formals=[| (ID "value", BasicClassNames.String) |])
                
                MethodSymbol.Virtual(ID "out_int"(*name*),
                                     BasicClassNames.Unit(*return_type*),
                                     formals=[| (ID "value", BasicClassNames.Int) |])
                
                MethodSymbol.Virtual(ID "out_nl"(*name*),
                                     BasicClassNames.Unit(*return_type*))
                
                MethodSymbol.Virtual(ID "in_string"(*name*),
                                     BasicClassNames.String(*return_type*))
                
                MethodSymbol.Virtual(ID "in_int"(*name*),
                                     BasicClassNames.Int(*return_type*))
            |])

    
    // Null is the type of the null literal.
    // It is a subtype of every type except those of value classes. Value classes include types such as Int, Boolean, Unit.
    // Since Null is not a subtype of value types, null is not a member of any such type.
    // For instance, it is not possible to assign null to a variable of type Int. 
    let Null = ClassSymbol.Virtual(class_name=BasicClassNames.Null, tag=(-1), super=BasicClasses.Any)
