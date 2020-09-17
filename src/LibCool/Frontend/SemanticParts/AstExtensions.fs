namespace LibCool.Frontend.SemanticParts


open LibCool.AstParts
open LibCool.SourceParts


module AstExtensions =
    
    
    type FeatureSyntax
        with
        member this.IsMethod: bool =
            match this with
            | FeatureSyntax.Method _ -> true
            | _            -> false
        member this.AsMethodInfo: MethodSyntax =
            match this with
            | FeatureSyntax.Method it -> it 
            | _         -> invalidOp "Feature.MethodInfo"
        member this.IsAttr: bool =
            match this with
            | FeatureSyntax.Attr _ -> true
            | _      -> false
        member this.AsAttrInfo: AttrSyntax =
            match this with
            | FeatureSyntax.Attr it -> it
            | _       -> invalidOp "Feature.AttrInfo"
        member this.IsBracedBlock: bool =
            match this with
            | FeatureSyntax.BracedBlock _ -> true
            | _             -> false
        member this.AsBlockInfo: BlockSyntax voption =
            match this with
            | FeatureSyntax.BracedBlock it -> it 
            | _              -> invalidOp "Feature.BracedInfo"
            
            
    type ClassSyntax
        with
        member this.ExtendsInfo: ExtendsSyntax =
            match this.Extends with
            | ValueNone ->
                { ExtendsSyntax.SUPER = AstNode.Of(TYPENAME "Any", Span.Invalid)
                  Actuals = Array.empty }
            | ValueSome extends_node ->
                match extends_node.Value with
                | InheritanceSyntax.Info it -> it
                | InheritanceSyntax.Native  -> invalidOp "ClassDecl.Extends is Extends.Native"
