namespace rec LibCool.SemanticParts.SemanticParts


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
    static member Virtual(declaring_class: TYPENAME,
                          name: ID,
                          return_type: TYPENAME,
                          ?is_override: bool,
                          ?formals: (ID * TYPENAME)[]): MethodSymbol =

        let is_override = defaultArg is_override false
        let formals = defaultArg formals [||]
        
        { MethodSymbol.Name = name
          Formals = formals |> Array.mapi (fun i (id, ty) -> { FormalSymbol.Name = id
                                                               Type = ty
                                                               Index = i+1
                                                               SyntaxSpan = Span.Invalid })
          ReturnType = return_type
          Override = is_override
          DeclaringClass = declaring_class
          Index = -1
          SyntaxSpan = Span.Invalid }


type ClassSymbol =
    { Name: TYPENAME
      Super: TYPENAME
      Ctor: MethodSymbol
      Attrs: IReadOnlyDictionary<ID, AttrSymbol>
      Methods: IReadOnlyDictionary<ID, MethodSymbol>
      SyntaxSpan: Span }
    
    
    member this.Is(class_sym: ClassSymbol): bool = this.Name = class_sym.Name
    
    
    static member Virtual(class_name: TYPENAME,
                          ?super: ClassSymbol,
                          ?methods: MethodSymbol[]): ClassSymbol =
        let method_map = Dictionary<ID, MethodSymbol>()

        let (super_name, super_methods) =
            match super with
            | Some sym -> sym.Name, sym.Methods.Values
            | None -> TYPENAME "", Seq.empty
        
        super_methods |> Seq.iter (fun it -> if it.Name <> ID ".ctor"
                                             then
                                                 method_map.Add(it.Name, it))        
        
        let method_syms = defaultArg methods [||]
        method_syms |> Seq.iter (fun it -> method_map.Add(it.Name, { it with Index = method_map.Count }))
        
        let ctor = { MethodSymbol.Virtual(class_name(*declaring_class*),
                                          ID ".ctor"(*name*),
                                          class_name(*return_type*)) with Index = -1 }
        
        method_map.Add(ctor.Name, ctor)
        
        { Name = class_name
          Super = super_name
          Ctor = ctor
          Attrs = Map.empty
          Methods = method_map
          SyntaxSpan = Span.Invalid }


[<RequireQualifiedAccess>]
module BasicClassNames =
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
    let Any = ClassSymbol.Virtual(class_name=BasicClassNames.Any) 
    let Unit = ClassSymbol.Virtual(class_name=BasicClassNames.Unit, super=BasicClasses.Any) 
    let Int = ClassSymbol.Virtual(class_name=BasicClassNames.Int, super=BasicClasses.Any)
    let String = ClassSymbol.Virtual(class_name=BasicClassNames.String, super=BasicClasses.Any)
    let Boolean = ClassSymbol.Virtual(class_name=BasicClassNames.Boolean, super=BasicClasses.Any)
    let ArrayAny = ClassSymbol.Virtual(class_name=BasicClassNames.ArrayAny, super=BasicClasses.Any)
    
    let IO =
        ClassSymbol.Virtual(
            class_name=BasicClassNames.IO,
            super=BasicClasses.Any,
            methods=[|
                MethodSymbol.Virtual(BasicClassNames.IO(*declaring_class*),
                                     ID "out_string"(*name*),
                                     BasicClassNames.Unit(*return_type*),
                                     formals=[| (ID "value", BasicClassNames.String) |])
                
                MethodSymbol.Virtual(BasicClassNames.IO(*declaring_class*),
                                     ID "out_int"(*name*),
                                     BasicClassNames.Unit(*return_type*),
                                     formals=[| (ID "value", BasicClassNames.Int) |])
                
                MethodSymbol.Virtual(BasicClassNames.IO(*declaring_class*),
                                     ID "out_nl"(*name*),
                                     BasicClassNames.Unit(*return_type*),
                                     formals=[||])
            |])
    
    let Symbol = ClassSymbol.Virtual(class_name=BasicClassNames.Symbol, super=BasicClasses.Any)

    // Null is the type of the null literal.
    // It is a subtype of every type except those of value classes. Value classes include types such as Int, Boolean, Unit.
    // Since Null is not a subtype of value types, null is not a member of any such type.
    // For instance, it is not possible to assign null to a variable of type Int. 
    let Null = ClassSymbol.Virtual(class_name=BasicClassNames.Null, super=BasicClasses.Any)
