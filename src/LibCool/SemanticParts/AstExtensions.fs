namespace LibCool.SemanticParts.SemanticParts


open LibCool.AstParts
open LibCool.SourceParts


module AstExtensions =
    
    
    type VarFormalSyntax
        with
        member this.AsFormalSyntax(?id_prefix: string): FormalSyntax =
            let id_prefix = defaultArg id_prefix "" 
            { FormalSyntax.ID = this.ID.Map(fun it -> ID (id_prefix + it.Value))
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
            | _         -> invalidOp "FeatureSyntax.AsMethodSyntax"
        member this.IsAttr: bool =
            match this with
            | FeatureSyntax.Attr _ -> true
            | _      -> false
        member this.AsAttrSyntax: AttrSyntax =
            match this with
            | FeatureSyntax.Attr it -> it
            | _       -> invalidOp "FeatureSyntax.AsAttrSyntax"
        member this.IsBracedBlock: bool =
            match this with
            | FeatureSyntax.BracedBlock _ -> true
            | _             -> false
        member this.AsBlockSyntax: BlockSyntax voption =
            match this with
            | FeatureSyntax.BracedBlock it -> it 
            | _              -> invalidOp "FeatureSyntax.AsBlockSyntax"
            
            
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
                | InheritanceSyntax.Native  -> invalidOp "ClassSyntax.ExtendsSyntax"
                
                
    type MethodBodySyntax
        with
        member this.AsExprSyntax: ExprSyntax =
            match this with
            | MethodBodySyntax.Expr it -> it
            | MethodBodySyntax.Native -> invalidOp "MethodBodySyntax.AsExprSyntax"
