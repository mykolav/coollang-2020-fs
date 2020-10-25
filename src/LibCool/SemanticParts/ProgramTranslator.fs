namespace rec LibCool.SemanticParts


open System.Collections.Generic
open System.Text
open LibCool.SourceParts
open LibCool.DiagnosticParts
open LibCool.AstParts
open LibCool.SemanticParts


[<Sealed>]
type private ProgramTranslator(_program_syntax: ProgramSyntax,
                               _class_sym_map: IReadOnlyDictionary<TYPENAME, ClassSymbol>,
                               _diags: DiagnosticBag,
                               _source: Source) =
    
    
    let _sb_code = StringBuilder()
    let _sb_data = StringBuilder()
    let _context = { TranslationContext.ClassSymMap = _class_sym_map
                     TypeCmp = TypeComparer(_class_sym_map)
                     RegSet = RegisterSet()
                     LabelGen = LabelGenerator()
                     // Diags
                     Diags = _diags
                     Source = _source
                     // Accumulators
                     IntConsts = ConstSet<int>("int_const")
                     StrConsts = ConstSet<string>("str_const") }

    
    let translate_class (class_node: AstNode<ClassSyntax>): unit =
        ClassTranslator(_context, class_node.Syntax, _sb_code).Translate()
        
        
    let translate_consts (): unit =
        ()
    
    
    let emit_class_name_table(): unit = 
        ()
    
    
    let emit_class_parent_table(): unit = 
        ()
    
    
    let emit_class_vtables(): unit = 
        ()


    member this.Translate(): string =
        let sb_asm = StringBuilder()

        _program_syntax.Classes |> Array.iter translate_class

        translate_consts()
        emit_class_name_table()
        emit_class_parent_table()
        emit_class_vtables()

        let asm = 
            sb_asm
                .AppendLine(".data")
                .AppendLine(".global class_name_table")
                .AppendLine(".global Main_proto_obj")
                .Append(_sb_data.ToString())
                .AppendLine()
                .AppendLine(".code")
                .Append(_sb_code.ToString())
                .ToString()
        asm
