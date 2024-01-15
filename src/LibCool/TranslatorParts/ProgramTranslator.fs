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

        let asm = AsmBuilder(_context)
                     // Eyecatch, unique id to verify any object
                    .Ln()
                    .Directive(".quad {0}", "-1")
                    .Directive(".global {0}", int_const.Label)
                    .Label(int_const.Label)
                    // Tag
                    .Directive(".quad INT_TAG", comment=Some "tag")
                    // Object size in quads
                    .Directive(".quad {0}", int_object_size_in_quads, comment="size in quads")
                    // Addr of the vtable
                    .Directive(".quad {0}_VTABLE", BasicClasses.Int.Name.Value)
                    // Value
                    .Directive(".quad {0}", int_const.Value, comment="value")

        if pad_size_in_bytes > 0
        then
             // Ensure 8 byte alignment
            asm.Ln(comment=($"payload's size in bytes = %d{int_object_size_in_bytes}, pad to an 8 byte boundary"))
               .Directive(".zero {0}", pad_size_in_bytes)
               .AsUnit()

        _sb_data.Append(asm.ToString()).AsUnit()

        
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

        let asm = AsmBuilder(_context)
                     // Eyecatch, unique id to verify any object
                    .Ln()
                    .Directive(".quad {0}", "-1")
                    .Directive(".global {0}", str_const.Label)
                    .Label(str_const.Label)
                    // Tag
                    .Directive(".quad STRING_TAG", comment=Some "tag")
                    // Object size in quads
                    .Directive(".quad {0}", str_object_size_in_quads, comment="size in quads")
                    // Addr of the vtable
                    .Directive(".quad {0}_VTABLE", BasicClasses.String.Name.Value)
                    // Addr of an int object containing the string's len in chars
                    .Directive(".quad {0}", len_const_label, comment=($"length = %d{ascii_bytes.Length}"))
                    // A comment with the string's content in human-readable form
                    .Comment(str_const.Value.Replace("\r", "").Replace("\n", "\\n"))

        // String content encoded in UTF8
        if ascii_bytes.Length > 0
        then
            asm.Directive(".byte {0}", String.Join(", ", ascii_bytes))
               .AsUnit()
                
        // String terminator
        asm.Directive(".byte {0}", "0", comment="terminator")
           .AsUnit()
            
        if pad_size_in_bytes > 0
        then
            // Ensure 8 byte alignment
            asm.Comment($"payload's size in bytes = %d{str_object_size_in_bytes}, pad to an 8 byte boundary")
               .Directive(".zero {0}", pad_size_in_bytes)
               .AsUnit()

        _sb_data.Append(asm.ToString()).AsUnit()

    
    let emitConsts (): unit =
        // Add class names to string constants -- class name table needs these.
        _context.ClassSymMap.Values
            |> Seq.where (_.IsAllowedInUserCode)
            |> Seq.iter (fun class_sym -> _context.StrConsts.GetOrAdd(class_sym.Name.Value) |> ignore)
        
        // Translate.
        _context.StrConsts.Items |> Seq.iter translateStrConst
        _context.IntConsts.Items
            |> Seq.where (fun it -> it.Value < IntConstFacts.MinPredefinedValue ||
                                    it.Value > IntConstFacts.MaxPredefinedValue)
            |> Seq.iter translateIntConst
    
    
    let emitClassNameTable(): unit =
        let asm = AsmBuilder(_context)
                    .Ln()
                    .Directive(".global {0}", "CLASS_NAME_MAP")
                    .Label("CLASS_NAME_MAP")

        _context.ClassSymMap.Values
        |> Seq.sortBy (_.Tag)
        |> Seq.where (_.IsAllowedInUserCode)
        |> Seq.iter (fun class_sym ->
               let name_const_label = _context.StrConsts.GetOrAdd(class_sym.Name.Value)
               asm.Directive(".quad {0}", name_const_label, comment=class_sym.Name.Value).AsUnit())

        _sb_data.Append(asm.ToString()).AsUnit()


    let emitClassParentTable(): unit =
        let asm = AsmBuilder(_context)
                    .Ln()
                    .Directive(".global {0}", RtNames.ClassParentMap)
                    .Label(RtNames.ClassParentMap)

        _context.ClassSymMap.Values
        |> Seq.sortBy (_.Tag)
        |> Seq.where (_.IsAllowedInUserCode)
        |> Seq.iter (fun class_sym ->
               if class_sym.Is(BasicClasses.Any)
               then
                   asm.Directive(".quad {0}", "-1", comment="Any").AsUnit()
               else

               let super_sym = _context.ClassSymMap[class_sym.Super]
               asm.Directive(
                       ".quad {0}",
                       super_sym.Tag,
                       comment=($"%s{class_sym.Name.Value} extends %s{super_sym.Name.Value}"))
                  .AsUnit())

        _sb_data.Append(asm.ToString()).AsUnit()

    
    let emitClassVTables(): unit =
        let asm = AsmBuilder(_context)

        _context.ClassSymMap.Values
        |> Seq.sortBy (fun class_sym -> class_sym.Tag)
        |> Seq.where (fun class_sym -> not class_sym.IsVirtual)
        |> Seq.iter (fun class_sym ->
            asm.Ln()
               .Directive(".global {0}_VTABLE", class_sym.Name.Value)
               .Label($"%s{class_sym.Name.Value}_VTABLE")
               .AsUnit()

            class_sym.Methods.Values
            |> Seq.sortBy (fun method_sym -> method_sym.Index)
            |> Seq.where (fun method_sym -> method_sym.Name <> ID ".ctor")
            |> Seq.iter (fun method_sym ->
                   asm.Directive(".quad {0}", $"%s{method_sym.DeclaringClass.Value}.%s{method_sym.Name.Value}",
                                 ?comment=(if method_sym.Override then Some "overrides" else None))
                      .AsUnit())
        )

        _sb_data.Append(asm.ToString()).AsUnit()


    let emitPrototypeObj(class_sym: ClassSymbol): unit =
        let header_size_in_quads = 3 // tag + size + vtable
        let proto_obj_size_in_quads = header_size_in_quads + class_sym.Attrs.Count

        let asm = AsmBuilder(_context)
                     // Eyecatch, unique id to verify any object
                    .Ln()
                    .Directive(".quad {0}", "-1")
                    .Directive(".global {0}_PROTO_OBJ", class_sym.Name.Value)
                    .Label($"%s{class_sym.Name.Value}_PROTO_OBJ")
                    // Tag
                    .Directive(".quad {0}", class_sym.Tag, comment="tag")
                    // Object size in quads
                    .Directive(".quad {0}", proto_obj_size_in_quads, comment="size in quads")
                    // Addr of the vtable
                    .Directive(".quad {0}_VTABLE", class_sym.Name.Value)

        class_sym.Attrs.Values
        |> Seq.sortBy (fun attr_sym -> attr_sym.Index)
        |> Seq.iter (fun attr_sym ->
               let default_value_ref =
                   if attr_sym.Type = TYPENAME "Unit" then RtNames.UnitValue
                   else if attr_sym.Type = TYPENAME "Int" then _context.IntConsts.GetOrAdd(0)
                   else if attr_sym.Type = TYPENAME "String" then _context.StrConsts.GetOrAdd("")
                   else if attr_sym.Type = TYPENAME "Boolean" then RtNames.BoolFalse
                   else "0"
               asm.Directive(".quad {0}", default_value_ref, comment=attr_sym.Name.Value).AsUnit())

        _sb_data.Append(asm.ToString()).AsUnit()
    
    
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
                .AppendLine("EYE_CATCH        = -1")
                .AppendLine()
                .AppendLine("# Tags")
                .AppendLine("UNIT_TAG         = 1")
                .AppendLine("INT_TAG          = 2")
                .AppendLine("STRING_TAG       = 3")
                .AppendLine("BOOLEAN_TAG      = 4")
                .AppendLine("ARRAYANY_TAG     = 5")
                .AppendLine("IO_TAG           = 6")
                .AppendLine()
                .AppendLine("# Offsets")
                .AppendLine("OBJ_EYE_CATCH    = -8")
                .AppendLine("OBJ_TAG          = 0")
                .AppendLine("OBJ_SIZE         = 8")
                .AppendLine("OBJ_VTAB         = 16")
                .AppendLine("OBJ_ATTR         = 24")
                .AppendLine("STR_LEN          = 24")
                .AppendLine("STR_VAL          = 32")
                .AppendLine("ARR_LEN          = 24")
                .AppendLine("ARR_ITEMS        = 32")
                .AppendLine("BOOL_VAL         = 24")
                .AppendLine("INT_VAL          = 24")
                .Append(_sb_data.ToString())
                .AppendLine()
                .AppendLine("    .text")
                .Append(sb_code.ToString())
                .ToString()
        asm
