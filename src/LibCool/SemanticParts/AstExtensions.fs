namespace LibCool.SemanticParts


open LibCool.AstParts


module AstExtensions =
    
    
    type VarFormalSyntax
        with
        member this.AsFormalSyntax(?id_prefix: string): FormalSyntax =
            let id_prefix = defaultArg id_prefix "" 
            { FormalSyntax.ID = this.ID.Map(fun it -> ID (id_prefix + it.Value))
              TYPE = this.TYPE }
    
    
    type ClassSyntax
        with
        member this.ExtendsSyntax: ExtendsSyntax =
            match this.Extends with
            | ValueNone ->
                { ExtendsSyntax.SUPER = AstNode.Virtual(TYPENAME "Any")
                  Actuals = Array.empty }
            | ValueSome extends_node ->
                match extends_node.Syntax with
                | InheritanceSyntax.Extends it -> it
                | InheritanceSyntax.Native  -> invalidOp "ClassSyntax.ExtendsSyntax"
