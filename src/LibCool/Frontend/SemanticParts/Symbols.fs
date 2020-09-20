namespace LibCool.Frontend.SemanticParts


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
    member this.IsError =
        this.Name = TYPENAME ".error"


[<RequireQualifiedAccess>]
module BasicClasses =
    
    
    let private mk_empty_class_sym (name: TYPENAME) (super: TYPENAME) =
        { Name = name
          Super = super
          Ctor =
            { MethodSymbol.Name = ID ".ctor"
              Formals = [||] 
              ReturnType = name  
              Override = false
              DeclaringClass = name 
              Index = -1
              SyntaxSpan = Span.Invalid
            }
          Attrs = Map.empty
          Methods = Map.empty
          SyntaxSpan = Span.Invalid
        }
        
    
    let Any: ClassSymbol = mk_empty_class_sym (TYPENAME "Any") (TYPENAME "") 
    let Unit: ClassSymbol = mk_empty_class_sym (TYPENAME "Unit") (TYPENAME "Any") 
    let Int: ClassSymbol = mk_empty_class_sym (TYPENAME "Int") (TYPENAME "Any")
    let String: ClassSymbol = mk_empty_class_sym (TYPENAME "String") (TYPENAME "Any")
    let Boolean: ClassSymbol = mk_empty_class_sym (TYPENAME "Boolean") (TYPENAME "Any")
    let ArrayAny: ClassSymbol = mk_empty_class_sym (TYPENAME "ArrayAny") (TYPENAME "Any")
    let IO: ClassSymbol = mk_empty_class_sym (TYPENAME "IO") (TYPENAME "Any")
    let Symbol: ClassSymbol = mk_empty_class_sym (TYPENAME "Symbol") (TYPENAME "Any")

    // Null is the type of the null literal.
    // It is a subtype of every type except those of value classes. Value classes include types such as Int, Boolean, Unit.
    // Since Null is not a subtype of value types, null is not a member of any such type.
    // For instance, it is not possible to assign null to a variable of type Int. 
    let Null: ClassSymbol = mk_empty_class_sym (TYPENAME "Null") (TYPENAME "Any")

    let Error: ClassSymbol = mk_empty_class_sym (TYPENAME ".error") (TYPENAME "")
