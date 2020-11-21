namespace LibCool.DriverParts


open LibCool.DiagnosticParts
open LibCool.ParserParts
open LibCool.SharedParts
open LibCool.SourceParts
open LibCool.TranslatorParts


module CompileToAsmDriver =
    let Invoke(source: Source, diags: DiagnosticBag): Res<string> =
        // ERROR HANDLING:
        // 1) If any lexical errors, report and stop
        // 2) If any syntax errors, report and stop
        // 3) Semantic analysis is also performed in stages,
        //    see Eric Lippert's corresponding post for inspiration.
        //    E.g.: detecting circular base class dependencies is in its own stage.
        //    ...

        // Lex
        let lexer = Lexer(source, diags)
        let tokens = TokenArray.ofLexer lexer
    
        if diags.ErrorsCount <> 0
        then
            Res.Error
        else
            
        // Parse
        let ast = Parser.Parse(tokens, diags)
        
        if diags.ErrorsCount <> 0
        then
            Res.Error
        else

        let asm = Translator.Translate(ast, diags, source)
        if diags.ErrorsCount <> 0
        then
            Res.Error
        else

        Res.Ok asm
