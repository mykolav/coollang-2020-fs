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


type ClassSymbol =
    { Name: TYPENAME
      Super: TYPENAME
      Ctor: MethodSymbol
      Attrs: IReadOnlyDictionary<ID, AttrSymbol>
      Methods: IReadOnlyDictionary<ID, MethodSymbol>
      SyntaxSpan: Span }
    member this.Is(class_sym: ClassSymbol): bool = this.Name = class_sym.Name
    member this.IsError: bool = this.Is(BasicClasses.Error)


[<RequireQualifiedAccess>]
module BasicClasses =
    
    
    type ClassSymbol
        with
        static member Mk(name: string, super: string, ?methods: MethodSymbol[]) =
            let methods = Dictionary<_, _>((defaultArg methods [||])
                                           |> Array.map (fun m -> KeyValuePair(m.Name, m)))
            let ctor = 
                { MethodSymbol.Name = ID ".ctor"
                  Formals = [||] 
                  ReturnType = TYPENAME name  
                  Override = false
                  DeclaringClass = TYPENAME name 
                  Index = -1
                  SyntaxSpan = Span.Invalid
                }
                
            methods.Add(ctor.Name, ctor)
            
            { Name = TYPENAME name
              Super = TYPENAME super
              Ctor = ctor
              Attrs = Map.empty
              Methods = methods
              SyntaxSpan = Span.Invalid
            }
    
    
    let private mk_empty_class_sym (name: TYPENAME) (super: TYPENAME) =
        let ctor = 
            { MethodSymbol.Name = ID ".ctor"
              Formals = [||] 
              ReturnType = name  
              Override = false
              DeclaringClass = name 
              Index = -1
              SyntaxSpan = Span.Invalid
            }
        { Name = name
          Super = super
          Ctor = ctor
          Attrs = Map.empty
          Methods = Map.ofSeq [ ID ".ctor", ctor ]
          SyntaxSpan = Span.Invalid
        }
        
    
    let Any: ClassSymbol = mk_empty_class_sym (TYPENAME "Any") (TYPENAME "") 
    let Unit: ClassSymbol = mk_empty_class_sym (TYPENAME "Unit") (TYPENAME "Any") 
    let Int: ClassSymbol = mk_empty_class_sym (TYPENAME "Int") (TYPENAME "Any")
    let String: ClassSymbol = mk_empty_class_sym (TYPENAME "String") (TYPENAME "Any")
    let Boolean: ClassSymbol = mk_empty_class_sym (TYPENAME "Boolean") (TYPENAME "Any")
    let ArrayAny: ClassSymbol = mk_empty_class_sym (TYPENAME "ArrayAny") (TYPENAME "Any")
    
    let IO: ClassSymbol =
        ClassSymbol.Mk(
            name="IO",
            super="Any",
            methods=[|
                { MethodSymbol.Name = ID "out_string"
                  Formals = [| { FormalSymbol.Name = ID "str"; Type = String.Name; Index = 1; SyntaxSpan = Span.Invalid } |]
                  ReturnType = Unit.Name
                  Override = false
                  DeclaringClass = TYPENAME "IO"
                  Index = 0
                  SyntaxSpan = Span.Invalid }
            |])
    
    let Symbol: ClassSymbol = mk_empty_class_sym (TYPENAME "Symbol") (TYPENAME "Any")

    // Null is the type of the null literal.
    // It is a subtype of every type except those of value classes. Value classes include types such as Int, Boolean, Unit.
    // Since Null is not a subtype of value types, null is not a member of any such type.
    // For instance, it is not possible to assign null to a variable of type Int. 
    let Null: ClassSymbol = mk_empty_class_sym (TYPENAME "Null") (TYPENAME "Any")

    let Error: ClassSymbol = mk_empty_class_sym (TYPENAME ".error") (TYPENAME "")
