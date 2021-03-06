namespace LibCool.TranslatorParts


module SysVAmd64AbiFacts =
    let ActualRegs = [| "%rdi"; "%rsi"; "%rdx"; "%rcx"; "%r8"; "%r9" |]
    let CalleeSavedRegs = [| "%rbx"; "%r12"; "%r13"; "%r14"; "%r15" |]
    let CallerSavedRegs = [| "%r10"; "%r11" |]


// All offsets and sizes are given in bytes.
module MemLayoutFacts =
    let VTableEntrySize = 8


// All offsets and sizes are given in bytes.
// We use a suffix 'Size' for sizes.
// We don't use any suffix for offsets.
module ObjLayoutFacts =
    let ElemSize = 8

    let Tag = 0
    let Size = 8
    let VTable = 16
    let Attrs = 24
        
    let StringLength = 24
    let StringContent = 32
    
    let ArrayLength = 24
    let ArrayItems = 32
    
    let BoolValue = 24
    let IntValue = 24


// All offsets and sizes are given in bytes.
// We use a suffix 'Size' for sizes.
// We don't use any suffix for offsets.
module FrameLayoutFacts =
    let ElemSize = 8
    
    let CalleeSavedRegsSize = SysVAmd64AbiFacts.CalleeSavedRegs.Length * ElemSize
    
    // skip saved %rbp, and return addr
    let ActualsInCallerFrame = 2 * ElemSize
    let Actuals = 0
    let This = -8


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
