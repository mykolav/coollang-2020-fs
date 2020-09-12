namespace LibCool.Frontend


open LibCool.Ast
open LibCool.DiagnosticParts


[<Sealed>]
type SemanticStage private (_ast: Ast, _diags: DiagnosticBag) =


    member private _.Translate(): string =
        ""
        
    
    static member Translate(ast: Ast, diags: DiagnosticBag): string =
        let sema = SemanticStage(ast, diags)
        let asm = sema.Translate()
        asm
