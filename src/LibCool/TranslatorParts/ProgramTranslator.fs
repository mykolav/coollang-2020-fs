namespace rec LibCool.TranslatorParts


open System
open System.Collections.Generic
open System.Text
open LibCool.SemanticParts
open LibCool.SharedParts
open LibCool.SourceParts
open LibCool.DiagnosticParts
open LibCool.AstParts
open LibCool.TranslatorParts


[<Sealed>]
type private ProgramTranslator(_program_syntax: ProgramSyntax,
                               _class_sym_map: IReadOnlyDictionary<TYPENAME, ClassSymbol>,
                               _diags: DiagnosticBag,
                               _source: Source,
                               _code_gen_options: CodeGenOptions) =


    let _sb_data = StringBuilder()
    let _context = { TranslationContext.CodeGenOptions = _code_gen_options
                     ClassSymMap = _class_sym_map
                     TypeCmp = TypeComparer(_class_sym_map)
                     RegSet = RegisterSet()
                     LabelGen = LabelGenerator()
                     // Diags
                     Diags = _diags
                     Source = _source
                     // Accumulators
                     IntConsts = IntConstSet("INT")
                     StrConsts = StringConstSet("STR") }
    
    // Add default values here so that their indexes are 0,
    // and const labels look similar to `int_const_0`, `str_const_0`.
    // Prototype objs need these.
    do _context.IntConsts.GetOrAdd(0) |> ignore
    do _context.StrConsts.GetOrAdd("") |> ignore

    
    let translateClass (class_node: AstNode<ClassSyntax>): string =
        ClassMethodsTranslator(_context, class_node.Syntax).Translate()
        
        
    let translateIntConst (int_const: ConstSetItem<int>): unit =
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
            .AppendLine()
            .AppendLine( "    .quad -1")
            .AppendLine($"    .global %s{int_const.Label}")
            .AppendLine($"%s{int_const.Label}:")
            // Tag
            .AppendLine($"    .quad %d{BasicClasses.Int.Tag} # tag")
            // Object size in quads
            .AppendLine($"    .quad %d{int_object_size_in_quads} # size in quads")
            // Addr of the vtable
            .AppendLine($"    .quad %s{BasicClasses.Int.Name.Value}_vtable")
            // Value
            .AppendLine($"    .quad %d{int_const.Value} # value")
            .AsUnit()
        
        if pad_size_in_bytes > 0
        then
             // Ensure 8 byte alignment
            _sb_data
                .Append($"    .zero %d{pad_size_in_bytes} ")
                .AppendLine($"# payload's size in bytes = %d{int_object_size_in_bytes}, pad to an 8 byte boundary")
                .AsUnit()
        
        
    let translateStrConst (str_const: ConstSetItem<string>): unit =
        let ascii_bytes = Encoding.ASCII.GetBytes(str_const.Value)
        let len_const_label = _context.IntConsts.GetOrAdd(ascii_bytes.Length)
        
        let tag_field_size_in_bytes = 8
        let size_field_size_in_bytes = 8
        let vtable_addr_field_size_in_bytes = 8
        let terminating_zero_field_size_in_bytes = 1
        
        let str_object_size_in_bytes =
            tag_field_size_in_bytes +
            size_field_size_in_bytes +
            vtable_addr_field_size_in_bytes +
            ascii_bytes.Length +
            terminating_zero_field_size_in_bytes
            
        let pad_size_in_bytes = if (str_object_size_in_bytes % 8) = 0
                                then 0
                                else 8 - (str_object_size_in_bytes % 8)
        
        let str_object_size_in_quads = (str_object_size_in_bytes + pad_size_in_bytes) / 8
        
        _sb_data
             // Eyecatch, unique id to verify any object
            .AppendLine()
            .AppendLine( "    .quad -1")
            .AppendLine($"    .global %s{str_const.Label}")
            .AppendLine($"%s{str_const.Label}:")
            // Tag
            .AppendLine($"    .quad %d{BasicClasses.String.Tag} # tag")
            // Object size in quads
            .AppendLine($"    .quad %d{str_object_size_in_quads} # size in quads")
            // Addr of the vtable
            .AppendLine($"    .quad %s{BasicClasses.String.Name.Value}_vtable")
            // Addr of an int object containing the string's len in chars
            .AppendLine($"    .quad %s{len_const_label} # length = %d{ascii_bytes.Length}")
            // A comment with the string's content in human-readable form
            .AppendLine(sprintf "    # '%s'" (str_const.Value.Replace("\r", "").Replace("\n", "\\n")))
            .AsUnit()
        
        // String content encoded in UTF8
        if ascii_bytes.Length > 0
        then
            _sb_data
                .AppendLine(sprintf "    .byte %s" (String.Join(", ", ascii_bytes)))
                .AsUnit()
                
        // String terminator
        _sb_data
            .AppendLine("    .byte 0 # terminator")
            .AsUnit()
            
        if pad_size_in_bytes > 0
        then
            // Ensure 8 byte alignment
            _sb_data
                .Append($"    .zero %d{pad_size_in_bytes} ")
                .AppendLine($"# payload's size in bytes = %d{str_object_size_in_bytes}, pad to an 8 byte boundary")
                .AsUnit()
    
    
    let emitConsts (): unit =
        // Add class names to string constants -- class name table needs these.
        _context.ClassSymMap.Values
        |> Seq.where (fun class_sym -> not class_sym.IsSpecial)
        |> Seq.iter (fun class_sym -> _context.StrConsts.GetOrAdd(class_sym.Name.Value) |> ignore)
        
        // Translate.
        _context.StrConsts.Items |> Seq.iter translateStrConst
        _context.IntConsts.Items |> Seq.iter translateIntConst
    
    
    let emitClassNameTable(): unit =
        _sb_data
            .AppendLine()
            .AppendLine("    .global class_name_table")
            .AppendLine("class_name_table:")
            .AsUnit()
        
        _context.ClassSymMap.Values
        |> Seq.sortBy (fun class_sym -> class_sym.Tag)
        |> Seq.where (fun class_sym -> not class_sym.IsSpecial)
        |> Seq.iter (fun class_sym ->
            let name_const_label = _context.StrConsts.GetOrAdd(class_sym.Name.Value)
            _sb_data.AppendLine($"    .quad %s{name_const_label} # %s{class_sym.Name.Value}").AsUnit())
    
    
    let emitClassParentTable(): unit =
        _sb_data
            .AppendLine()
            .AppendLine("    .global class_parent_table")
            .AppendLine("class_parent_table:")
            .AsUnit()
        
        _context.ClassSymMap.Values
        |> Seq.sortBy (fun class_sym -> class_sym.Tag)
        |> Seq.where (fun class_sym -> not class_sym.IsSpecial)
        |> Seq.iter (fun class_sym ->
            if class_sym.Is(BasicClasses.Any)
            then
                _sb_data.AppendLine("    .quad -1 # Any").AsUnit()
            else
                
            let super_sym = _context.ClassSymMap[class_sym.Super]
            _sb_data.Append($"    .quad %d{super_sym.Tag} ")
                    .AppendLine($"# %s{class_sym.Name.Value} extends %s{super_sym.Name.Value}")
                    .AsUnit())
    
    
    let emitClassVTables(): unit =
        _context.ClassSymMap.Values
        |> Seq.sortBy (fun class_sym -> class_sym.Tag)
        |> Seq.where (fun class_sym -> not class_sym.IsVirtual)
        |> Seq.iter (fun class_sym ->
            _sb_data
                .AppendLine()
                .AppendLine($"    .global %s{class_sym.Name.Value}_vtable")
                .AppendLine($"%s{class_sym.Name.Value}_vtable:")
                .AsUnit()
            
            class_sym.Methods.Values
            |> Seq.sortBy (fun method_sym -> method_sym.Index)
            |> Seq.where (fun method_sym -> method_sym.Name <> ID ".ctor")
            |> Seq.iter (fun method_sym ->
                _sb_data.AppendLine(sprintf "    .quad %s.%s %s"
                                            method_sym.DeclaringClass.Value
                                            method_sym.Name.Value
                                            (if method_sym.Override then "# overrides" else ""))
                        .AsUnit()
                )
        )


    let emitPrototypeObj(class_sym: ClassSymbol): unit =
        let header_size_in_quads = 3 // tag + size + vtable
        let proto_obj_size_in_quads = header_size_in_quads + class_sym.Attrs.Count
        _sb_data
             // Eyecatch, unique id to verify any object
            .AppendLine()
            .AppendLine( "    .quad -1")
            .AppendLine($"    .global %s{class_sym.Name.Value}_proto_obj")
            .AppendLine($"%s{class_sym.Name.Value}_proto_obj:")
            // Tag
            .AppendLine($"    .quad %d{class_sym.Tag} # tag")
            // Object size in quads
            .AppendLine($"    .quad %d{proto_obj_size_in_quads} # size in quads")
            // Addr of the vtable
            .AppendLine($"    .quad %s{class_sym.Name.Value}_vtable")
            .AsUnit()
            
        class_sym.Attrs.Values
        |> Seq.sortBy (fun attr_sym -> attr_sym.Index)
        |> Seq.iter (fun attr_sym ->
            let default_value_ref =
                if attr_sym.Type = TYPENAME "Unit" then "Unit_value"
                else if attr_sym.Type = TYPENAME "Int" then _context.IntConsts.GetOrAdd(0)
                else if attr_sym.Type = TYPENAME "String" then _context.StrConsts.GetOrAdd("")
                else if attr_sym.Type = TYPENAME "Boolean" then "Boolean_false"
                else "0"
            _sb_data.AppendLine($"    .quad %s{default_value_ref} # %s{attr_sym.Name.Value}").AsUnit())
    
    
    let emitPrototypeObjs(): unit =
        _context.ClassSymMap.Values
        |> Seq.where (fun class_sym -> not class_sym.IsVirtual)
        |> Seq.iter emitPrototypeObj
    
    
    member this.Translate(): string =
        let sb_code = StringBuilder()
        for class_syntax in _program_syntax.Classes do
            let class_methods_frag = translateClass class_syntax
            sb_code.Append(class_methods_frag)
                   .AsUnit()
        
        emitConsts()
        emitClassNameTable()
        emitClassParentTable()
        emitClassVTables()
        emitPrototypeObjs()

        let gc_name =
            match _context.CodeGenOptions.GC with
            | Nop          -> ".NopGC"
            | Generational -> ".GenGC"

        let asm = 
            StringBuilder()
                .AppendLine("    .data")
                .AppendLine()
                .AppendLine( "    .global .MemoryManager.FN_INIT")
                .AppendLine($".MemoryManager.FN_INIT:         .quad {gc_name}.init")
                .AppendLine()
                .AppendLine( "    .global .MemoryManager.FN_ON_ASSIGN")
                .AppendLine($".MemoryManager.FN_ON_ASSIGN:    .quad {gc_name}.on_assign")
                .AppendLine()
                .AppendLine( "    .global .MemoryManager.FN_COLLECT")
                .AppendLine($".MemoryManager.FN_COLLECT:      .quad {gc_name}.collect")
                .AppendLine()
                .AppendLine( "    .global .MemoryManager.FN_PRINT_STATE")
                .AppendLine($".MemoryManager.FN_PRINT_STATE:  .quad {gc_name}.print_state")
                .AppendLine()
                .AppendLine( "    .global .MemoryManager.IS_TESTING")
                .AppendLine( ".MemoryManager.IS_TESTING:      .quad 0")
                .AppendLine()
                .Append(_sb_data.ToString())
                .AppendLine()
                .AppendLine("    .text")
                .Append(sb_code.ToString())
                .ToString()
        asm
