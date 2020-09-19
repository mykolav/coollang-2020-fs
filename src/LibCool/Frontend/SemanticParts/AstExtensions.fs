namespace LibCool.Frontend.SemanticParts


open LibCool.AstParts
open LibCool.SourceParts


module AstExtensions =
    
    
    type VarFormalSyntax
        with
        member this.AsFormalSyntax: FormalSyntax =
            { FormalSyntax.ID = this.ID
              TYPE = this.TYPE }
    
    
    type FeatureSyntax
        with
        member this.IsMethod: bool =
            match this with
            | FeatureSyntax.Method _ -> true
            | _            -> false
        member this.AsMethodSyntax: MethodSyntax =
            match this with
            | FeatureSyntax.Method it -> it 
            | _         -> invalidOp "Feature.MethodInfo"
        member this.IsAttr: bool =
            match this with
            | FeatureSyntax.Attr _ -> true
            | _      -> false
        member this.AsAttrSyntax: AttrSyntax =
            match this with
            | FeatureSyntax.Attr it -> it
            | _       -> invalidOp "Feature.AttrInfo"
        member this.IsBracedBlock: bool =
            match this with
            | FeatureSyntax.BracedBlock _ -> true
            | _             -> false
        member this.AsBlockSyntax: BlockSyntax voption =
            match this with
            | FeatureSyntax.BracedBlock it -> it 
            | _              -> invalidOp "Feature.BracedInfo"
            
            
    type ClassSyntax
        with
        member this.ExtendsSyntax: ExtendsSyntax =
            match this.Extends with
            | ValueNone ->
                { ExtendsSyntax.SUPER = AstNode.Of(TYPENAME "Any", Span.Invalid)
                  Actuals = Array.empty }
            | ValueSome extends_node ->
                match extends_node.Syntax with
                | InheritanceSyntax.Info it -> it
                | InheritanceSyntax.Native  -> invalidOp "ClassDecl.Extends is Extends.Native"
