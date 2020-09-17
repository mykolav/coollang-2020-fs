namespace LibCool.Frontend.SemanticParts


open System.Collections.Generic
open LibCool.AstParts
open LibCool.SourceParts


module Symbols =


    type AttrSymbol =
        { Name: Ast.ID
          Type: Ast.TYPENAME
          DeclaringClass: Ast.TYPENAME
          Index: int
          SyntaxSpan: Span }


    type ParamSymbol =
        { Name: Ast.ID
          Type: Ast.TYPENAME
          Index: int
          SyntaxSpan: Span }


    type MethodSymbol =
        { Name: Ast.ID
          Params: ParamSymbol[]
          ReturnType: Ast.TYPENAME
          Override: bool
          DeclaringClass: Ast.TYPENAME
          Index: int
          SyntaxSpan: Span }


    type ClassSymbol =
        { Name: Ast.TYPENAME
          Super: Ast.TYPENAME
          Ctor: MethodSymbol
          Attrs: IReadOnlyDictionary<Ast.ID, AttrSymbol>
          Methods: IReadOnlyDictionary<Ast.ID, MethodSymbol>
          SyntaxSpan: Span }
        member this.IsError =
            this.Name = Ast.TYPENAME ".error"


[<RequireQualifiedAccess>]
module BasicClassSymbols =
    
    
    open Symbols
    
    
    let private mk_class_sym (name: Ast.TYPENAME) (super: Ast.TYPENAME) =
        { Name = name
          Super = super
          Ctor =
            { MethodSymbol.Name = Ast.ID ".ctor"
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
        
    
    let Any: ClassSymbol = mk_class_sym (Ast.TYPENAME "Any") (Ast.TYPENAME "") 
    let Unit: ClassSymbol = mk_class_sym (Ast.TYPENAME "Unit") (Ast.TYPENAME "Any") 
    let Int: ClassSymbol = mk_class_sym (Ast.TYPENAME "Int") (Ast.TYPENAME "Any")
    let String: ClassSymbol = mk_class_sym (Ast.TYPENAME "String") (Ast.TYPENAME "Any")
    let Boolean: ClassSymbol = mk_class_sym (Ast.TYPENAME "Boolean") (Ast.TYPENAME "Any")
    let ArrayAny: ClassSymbol = mk_class_sym (Ast.TYPENAME "ArrayAny") (Ast.TYPENAME "Any")
    let IO: ClassSymbol = mk_class_sym (Ast.TYPENAME "IO") (Ast.TYPENAME "Any")
    let Symbol: ClassSymbol = mk_class_sym (Ast.TYPENAME "Symbol") (Ast.TYPENAME "Any")

    let Error: ClassSymbol = mk_class_sym (Ast.TYPENAME ".error") (Ast.TYPENAME "")
