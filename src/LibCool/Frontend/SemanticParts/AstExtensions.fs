namespace LibCool.Frontend.SemanticParts


open LibCool.AstParts
open LibCool.SourceParts


module AstExtensions =
    
    
    open Ast
    
    
    type Feature
        with
        member this.IsMethod: bool =
            match this with
            | Feature.Method _ -> true
            | _            -> false
        member this.AsMethodInfo: MethodInfo =
            match this with
            | Feature.Method it -> it 
            | _         -> invalidOp "Feature.MethodInfo"
        member this.IsAttr: bool =
            match this with
            | Feature.Attr _ -> true
            | _      -> false
        member this.AsAttrInfo: AttrInfo =
            match this with
            | Feature.Attr it -> it
            | _       -> invalidOp "Feature.AttrInfo"
        member this.IsBracedBlock: bool =
            match this with
            | Feature.BracedBlock _ -> true
            | _             -> false
        member this.AsBlockInfo: Block voption =
            match this with
            | Feature.BracedBlock it -> it 
            | _              -> invalidOp "Feature.BracedInfo"
            
            
    type ClassDecl
        with
        member this.ExtendsInfo: ExtendsInfo =
            match this.Extends with
            | ValueNone ->
                { ExtendsInfo.SUPER = Node.Of(TYPENAME "Any", Span.Invalid)
                  Actuals = Array.empty }
            | ValueSome extends_node ->
                match extends_node.Value with
                | Extends.Info it -> it
                | Extends.Native  -> invalidOp "ClassDecl.Extends is Extends.Native"
