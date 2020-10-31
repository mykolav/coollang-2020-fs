namespace rec LibCool.TranslatorParts


open System
open System.Collections.Generic
open System.Text
open LibCool.SourceParts
open LibCool.DiagnosticParts
open LibCool.AstParts
open LibCool.TranslatorParts


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
        
        
    let translate_int_const (int_const: ConstSetItem<int>): unit =
        let tag_field_size_in_bytes = 8
        let size_field_size_in_bytes = 8
        let vtable_addr_field_size_in_bytes = 8
        let value_size_in_bytes = 8
        
        let int_object_size_in_bytes =
            tag_field_size_in_bytes +
            size_field_size_in_bytes +
            vtable_addr_field_size_in_bytes +
            value_size_in_bytes;
            
        let pad_size_in_bytes = if (int_object_size_in_bytes % 8) = 0
                                then 0
                                else 8 - (int_object_size_in_bytes % 8)
        
        let int_object_size_in_quads = (int_object_size_in_bytes + pad_size_in_bytes) / 8
        
        _sb_data
             // Eyecatch, unique id to verify any object
            .AppendLine(        "    .quad -1")
            .AppendLine(sprintf "%s:" int_const.Label)
            // Tag
            .AppendLine(sprintf "    .quad %d # tag" BasicClasses.String.Tag)
            // Object size in quads
            .AppendLine(sprintf "    .quad %d # size in quads" int_object_size_in_quads)
            // Addr of the vtable
            .AppendLine(sprintf "    .quad %s_vtable" BasicClasses.String.Name.Value)
            // Addr of an int object containing the string's len in chars
            .AppendLine(sprintf "    .quad %d # value" int_const.Value)
            |> ignore
        
        if pad_size_in_bytes > 0
        then
             // Ensure 8 byte alignment
            _sb_data.AppendLine(sprintf "    .zero %d # payload's size in bytes = %d, pad to an 8 byte boundary"
                                        pad_size_in_bytes
                                        int_object_size_in_bytes)
            |> ignore
        
        
    let translate_str_const (str_const: ConstSetItem<string>): unit =
        let utf8_bytes = Encoding.UTF8.GetBytes(str_const.Value)
        let len_const_label = _context.IntConsts.GetOrAdd(utf8_bytes.Length)
        
        let tag_field_size_in_bytes = 8
        let size_field_size_in_bytes = 8
        let vtable_addr_field_size_in_bytes = 8
        let terminating_zero_field_size_in_bytes = 1
        
        let str_object_size_in_bytes =
            tag_field_size_in_bytes +
            size_field_size_in_bytes +
            vtable_addr_field_size_in_bytes +
            utf8_bytes.Length +
            terminating_zero_field_size_in_bytes
            
        let pad_size_in_bytes = if (str_object_size_in_bytes % 8) = 0
                                then 0
                                else 8 - (str_object_size_in_bytes % 8)
        
        let str_object_size_in_quads = (str_object_size_in_bytes + pad_size_in_bytes) / 8
        
        _sb_data
             // Eyecatch, unique id to verify any object
            .AppendLine(        "    .quad -1")
            .AppendLine(sprintf "%s:" str_const.Label)
            // Tag
            .AppendLine(sprintf "    .quad %d # tag" BasicClasses.String.Tag)
            // Object size in quads
            .AppendLine(sprintf "    .quad %d # size in quads" str_object_size_in_quads)
            // Addr of the vtable
            .AppendLine(sprintf "    .quad %s_vtable" BasicClasses.String.Name.Value)
            // Addr of an int object containing the string's len in chars
            .AppendLine(sprintf "    .quad %s # length" len_const_label)
            // A comment with the string's content in human-readable form
            .AppendLine(sprintf "#   '%s'" (str_const.Value.Replace("\r", "").Replace("\n", "\\n")))
            |> ignore
        
        // String content encoded in UTF8
        if utf8_bytes.Length > 0
        then
            _sb_data
                .AppendLine(sprintf "    .byte %s" (String.Join(", ", utf8_bytes)))
                |> ignore
                
        // String terminator
        _sb_data
            .AppendLine(sprintf "    .byte 0 # terminator")
            |> ignore
            
        if pad_size_in_bytes > 0
        then
            // Ensure 8 byte alignment
            _sb_data.AppendLine(sprintf "    .zero %d # payload's size in bytes = %d, pad to an 8 byte boundary"
                                        pad_size_in_bytes
                                        str_object_size_in_bytes)
            |> ignore
    
    
    let emit_consts (): unit =
        _context.StrConsts.Items |> Seq.iter translate_str_const
        _context.IntConsts.Items |> Seq.iter translate_int_const
    
    
    let emit_class_name_table(): unit = 
        ()
    
    
    let emit_class_parent_table(): unit = 
        ()
    
    
    let emit_class_vtables(): unit = 
        ()


    member this.Translate(): string =
        let sb_asm = StringBuilder()

        _program_syntax.Classes |> Array.iter translate_class
        
        // TODO: Add class names to the string constant set.

        emit_consts()
        emit_class_name_table()
        emit_class_parent_table()
        emit_class_vtables()

        let asm = 
            sb_asm
                .AppendLine("    .data")
                .AppendLine("    .global class_name_table")
                .AppendLine("    .global Main_proto_obj")
                .AppendLine()
                .Append(_sb_data.ToString())
                .AppendLine()
                .AppendLine("    .text")
                .AppendLine("    ret")
                // .AppendLine("    movq $0, %rcx")
                // .AppendLine("    call ExitProcess")
                .Append(_sb_code.ToString())
                .ToString()
        asm
