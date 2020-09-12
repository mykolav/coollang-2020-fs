namespace LibCool.Frontend


open LibCool.AstParts
open LibCool.DiagnosticParts


[<Sealed>]
type SemanticStage private (_ast: Ast.Program, _diags: DiagnosticBag) =


    member private _.Translate(): string =
        ""
        
    
    static member Translate(ast: Ast.Program, diags: DiagnosticBag): string =
        let sema = SemanticStage(ast, diags)
        let asm = sema.Translate()
        asm
