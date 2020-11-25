namespace LibCool.TranslatorParts


module SysVAmd64AbiFacts =
    let ActualRegs = [| "%rdi"; "%rsi"; "%rdx"; "%rcx"; "%r8"; "%r9" |]
    let CalleeSavedRegs = [| "%rbx"; "%r12"; "%r13"; "%r14"; "%r15" |]
    let CallerSavedRegs = [| "%r10"; "%r11" |]


module MemoryLayoutFacts =
    let QuadSizeInBytes = 8


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
    let CalleeSavedRegsSizeInBytes = SysVAmd64AbiFacts.CalleeSavedRegs.Length *
                                     MemoryLayoutFacts.QuadSizeInBytes
    // skip saved %rbp, and return addr
    let ActualsOutOfFrameOffset = 2 * MemoryLayoutFacts.QuadSizeInBytes
    let ActualsOffsetInBytes = 0


module RuntimeNames =
    let BoolFalse = "Boolean_false"
    let BoolTrue = "Boolean_true"
    
    let UnitValue = "Unit_value"
    
    let RtCopyObject = ".Runtime.copy_object"
    let StringConcat = "String.concat"
