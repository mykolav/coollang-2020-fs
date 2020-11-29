namespace LibCool.TranslatorParts


module SysVAmd64AbiFacts =
    let ActualRegs = [| "%rdi"; "%rsi"; "%rdx"; "%rcx"; "%r8"; "%r9" |]
    let CalleeSavedRegs = [| "%rbx"; "%r12"; "%r13"; "%r14"; "%r15" |]
    let CallerSavedRegs = [| "%r10"; "%r11" |]


module MemLayoutFacts =
    let VTableEntrySizeInBytes = 8


module ObjLayoutFacts =
    let Tag = 0
    let Size = 8
    let VTable = 16
    
    let StringLength = 24
    let StringContent = 32
    
    let ArrayLength = 24
    let ArrayItems = 32
    
    let BoolValue = 24
    let IntValue = 24


module FrameLayoutFacts =
    let ElemSizeInBytes = 8
    let CalleeSavedRegsSizeInBytes = SysVAmd64AbiFacts.CalleeSavedRegs.Length * ElemSizeInBytes
    // skip saved %rbp, and return addr
    let ActualsOutOfFrameOffset = 2 * ElemSizeInBytes
    let ActualsOffsetInBytes = 0


module RtNames =
    let ClassParentTable = "class_parent_table"
    
    let BoolFalse = "Boolean_false"
    let BoolTrue = "Boolean_true"
    
    let UnitValue = "Unit_value"
    
    let RtCopyObject = ".Runtime.copy_object"
    let RtAreEqual = ".Runtime.are_equal"
    let RtAbortMatch = ".Runtime.abort_match"
    let RtAbortDispatch = ".Runtime.abort_dispatch"
    
    let StringConcat = "String.concat"
