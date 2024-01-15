namespace rec LibCool.TranslatorParts


open System
open System.Text
open LibCool.SemanticParts
open LibCool.SharedParts
open LibCool.AstParts
open LibCool.TranslatorParts
open LibCool.TranslatorParts.AsmFragments


module private IntObjLayout =
    // In bytes
    let PayloadSize =
        ObjLayoutFacts.TagSlotSize +
        ObjLayoutFacts.SizeSlotSize +
        ObjLayoutFacts.VTableAddrSlotSize +
        ObjLayoutFacts.IntValueSlotSize

    // In bytes
    let PadSize = if (PayloadSize % 8) = 0
                  then 0
                  else 8 - (PayloadSize % 8)

    let SizeInQuads = (PayloadSize + PadSize) / 8


module private StringObjLayout =
    let TerminatingZeroSize = 1


    // In bytes
    let PayloadSize (ascii_bytes_len: int): int =
        ObjLayoutFacts.TagSlotSize +
        ObjLayoutFacts.SizeSlotSize +
        ObjLayoutFacts.VTableAddrSlotSize +
        ascii_bytes_len +
        TerminatingZeroSize


    // In bytes
    let PadSize (string_obj_size: int): int =
       if (string_obj_size % 8) = 0
       then 0
       else 8 - (string_obj_size % 8)


    let SizeInQuads (string_obj_size: int): int =
        (string_obj_size + (PadSize string_obj_size)) / 8


[<Sealed>]
type private DataTranslator(_context: TranslationContext) =


    let emitPredefinedInt (int_const: ConstSetItem<int>): string =
        let asm = AsmBuilder(_context)
                      .BeginPredefinedObj(label=int_const.Label,
                                          tag_name="INT_TAG",
                                          size_in_quads=IntObjLayout.SizeInQuads,
                                          class_name=BasicClasses.Int.Name)
                      // Value
                      .Directive(".quad {0}", int_const.Value, comment="value")
                      .CompletePredefinedObj(IntObjLayout.PadSize)

        asm.ToString()


    let emitPredefinedStr (str_const: ConstSetItem<string>): string =
        let ascii_bytes = Encoding.ASCII.GetBytes(str_const.Value)
        let len_const_label = _context.IntConsts.GetOrAdd(ascii_bytes.Length)

        let payload_size = StringObjLayout.PayloadSize ascii_bytes.Length
        let pad_size = StringObjLayout.PadSize payload_size
        let size_in_quads = StringObjLayout.SizeInQuads payload_size

        let asm = AsmBuilder(_context)
                      .BeginPredefinedObj(label=str_const.Label,
                                          tag_name="STRING_TAG",
                                          size_in_quads=size_in_quads,
                                          class_name=BasicClasses.String.Name)
                      // Addr of an Int object containing the string's len in chars
                      .Directive(".quad {0}", len_const_label, comment=($"length = %d{ascii_bytes.Length}"))
                      // A comment with the string's content in human-readable form
                      .Comment(str_const.Value.Replace("\r", "").Replace("\n", "\\n"))

        // String content encoded in ASCII
        if ascii_bytes.Length > 0
        then
            asm.Directive(".byte {0}", String.Join(", ", ascii_bytes))
               .AsUnit()

        asm
           // String terminator
           .Directive(".byte {0}", "0", comment="terminator")
           .CompletePredefinedObj(pad_size)
           .ToString()


    let emitPredefinedObjs (): string =
        // Add class names to string constants -- class name table needs these.
        _context.ClassSymMap.Values
            |> Seq.where (_.IsAllowedInUserCode)
            |> Seq.iter (fun class_sym -> _context.StrConsts.GetOrAdd(class_sym.Name.Value) |> ignore)

        // Translate.
        let sb_predefined_objs = StringBuilder()

        _context.StrConsts.Items
            |> Seq.iter (fun it -> sb_predefined_objs.Append(emitPredefinedStr it).AsUnit())

        _context.IntConsts.Items
            |> Seq.where (fun it -> it.Value < IntConstFacts.MinPredefinedValue ||
                                    it.Value > IntConstFacts.MaxPredefinedValue)
            |> Seq.iter (fun it -> sb_predefined_objs.Append(emitPredefinedInt it).AsUnit())

        sb_predefined_objs.ToString()


    let emitClassNameTable(): string =
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

        asm.ToString()


    let emitClassParentTable(): string =
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
                   asm.Directive(".quad {0}", "-1", comment="Any")
                      .AsUnit()
               else

               let super_sym = _context.ClassSymMap[class_sym.Super]
               let comment = $"%s{class_sym.Name.Value} extends %s{super_sym.Name.Value}"
               asm.Directive(".quad {0}", super_sym.Tag, comment=comment)
                  .AsUnit())

        asm.ToString()


    let emitClassVTables(): string =
        let asm = AsmBuilder(_context)

        _context.ClassSymMap.Values
        |> Seq.sortBy (_.Tag)
        |> Seq.where (fun class_sym -> not class_sym.IsVirtual)
        |> Seq.iter (fun class_sym ->
            asm.Ln()
               .Directive(".global {0}_VTABLE", class_sym.Name.Value)
               .Label($"%s{class_sym.Name.Value}_VTABLE")
               .AsUnit()

            class_sym.Methods.Values
            |> Seq.sortBy (_.Index)
            |> Seq.where (fun method_sym -> method_sym.Name <> ID ".ctor")
            |> Seq.iter (fun method_sym ->
                   asm.Directive(".quad {0}", $"%s{method_sym.DeclaringClass.Value}.%s{method_sym.Name.Value}",
                                 ?comment=(if method_sym.Override then Some "overrides" else None))
                      .AsUnit())
        )

        asm.ToString()


    let emitPrototypeObj(class_sym: ClassSymbol): string =
        let size_in_quads =
            ObjLayoutFacts.HeaderSizeInQuads +
            class_sym.Attrs.Count

        let asm = AsmBuilder(_context)
                      .BeginPredefinedObj(label=($"{class_sym.Name}_PROTO_OBJ"),
                                          tag_name=class_sym.Tag.ToString(),
                                          size_in_quads=size_in_quads,
                                          class_name=class_sym.Name)

        class_sym.Attrs.Values
        |> Seq.sortBy (_.Index)
        |> Seq.iter (fun attr_sym ->
               let default_value_ref =
                   if attr_sym.Type = TYPENAME "Unit" then RtNames.UnitValue
                   else if attr_sym.Type = TYPENAME "Int" then _context.IntConsts.GetOrAdd(0)
                   else if attr_sym.Type = TYPENAME "String" then _context.StrConsts.GetOrAdd("")
                   else if attr_sym.Type = TYPENAME "Boolean" then RtNames.BoolFalse
                   else "0"
               asm.Directive(".quad {0}", default_value_ref, comment=attr_sym.Name.Value).AsUnit())

        asm.ToString()


    let emitPrototypeObjs(): string =
        let sb_proto_objs = StringBuilder()

        _context.ClassSymMap.Values
        |> Seq.where (fun class_sym -> not class_sym.IsVirtual)
        |> Seq.iter (fun it -> sb_proto_objs.Append(emitPrototypeObj it).AsUnit())

        sb_proto_objs.ToString()


    let emitMemoryManagerSetup(): string =
        let gc_name =
            match _context.CodeGenOptions.GC with
            | Nop          -> ".NopGC"
            | Generational -> ".GenGC"

        StringBuilder()
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
            .ToString()


    static let assemblyConsts = String.concat Environment.NewLine [
        ""
        "EYE_CATCH        = -1"
        ""
        "# Tags"
        "UNIT_TAG         = 1"
        "INT_TAG          = 2"
        "STRING_TAG       = 3"
        "BOOLEAN_TAG      = 4"
        "ARRAYANY_TAG     = 5"
        "IO_TAG           = 6"
        ""
        "# Offsets"
        "OBJ_EYE_CATCH    = -8"
        "OBJ_TAG          = 0"
        "OBJ_SIZE         = 8"
        "OBJ_VTAB         = 16"
        "OBJ_ATTR         = 24"
        "STR_LEN          = 24"
        "STR_VAL          = 32"
        "ARR_LEN          = 24"
        "ARR_ITEMS        = 32"
        "BOOL_VAL         = 24"
        "INT_VAL          = 24"
        ""
    ]


    member this.Translate(): string =
        StringBuilder()
            .Append(emitMemoryManagerSetup())
            .Append(assemblyConsts)
            .Append(emitPredefinedObjs())
            .Append(emitClassNameTable())
            .Append(emitClassParentTable())
            .Append(emitClassVTables())
            .Append(emitPrototypeObjs())
            .ToString()
