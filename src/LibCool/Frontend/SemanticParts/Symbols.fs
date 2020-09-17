namespace LibCool.Frontend.SemanticParts


open System.Collections.Generic
open LibCool.AstParts
open LibCool.SourceParts


module Symbols =


    type AttrSymbol =
        { Name: ID
          Type: TYPENAME
          DeclaringClass: TYPENAME
          Index: int
          SyntaxSpan: Span }


    type ParamSymbol =
        { Name: ID
          Type: TYPENAME
          Index: int
          SyntaxSpan: Span }


    type MethodSymbol =
        { Name: ID
          Params: ParamSymbol[]
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
module BasicClassSymbols =
    
    
    open Symbols
    
    
    let private mk_class_sym (name: TYPENAME) (super: TYPENAME) =
        { Name = name
          Super = super
          Ctor =
            { MethodSymbol.Name = ID ".ctor"
              Params = [||] 
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
        
    
    let Any: ClassSymbol = mk_class_sym (TYPENAME "Any") (TYPENAME "") 
    let Unit: ClassSymbol = mk_class_sym (TYPENAME "Unit") (TYPENAME "Any") 
    let Int: ClassSymbol = mk_class_sym (TYPENAME "Int") (TYPENAME "Any")
    let String: ClassSymbol = mk_class_sym (TYPENAME "String") (TYPENAME "Any")
    let Boolean: ClassSymbol = mk_class_sym (TYPENAME "Boolean") (TYPENAME "Any")
    let ArrayAny: ClassSymbol = mk_class_sym (TYPENAME "ArrayAny") (TYPENAME "Any")
    let IO: ClassSymbol = mk_class_sym (TYPENAME "IO") (TYPENAME "Any")
    let Symbol: ClassSymbol = mk_class_sym (TYPENAME "Symbol") (TYPENAME "Any")

    let Error: ClassSymbol = mk_class_sym (TYPENAME ".error") (TYPENAME "")
