namespace LibCool.TranslatorParts


module SysVAmd64AbiFacts =
    // If the caller wants to preserve the values of `ActualRegs` across the call,
    // it's responsible for storing them.
    // (I.e., the callee is free to clobber the registers from `ActualRegs`).
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
    let TagConst = "OBJ_TAG"

    let Size = 8
    let SizeConst = "OBJ_SIZE"

    let VTable = 16
    let VTableConst = "OBJ_VTAB"

    let Attrs = 24
    let AttrsConst = "OBJ_ATTR"
        
    let StringLength = 24
    let StringLengthConst = "STR_LEN"

    let StringContent = 32
    let StringContentConst = "STR_VAL"
    
    let ArrayLength = 24
    let ArrayLengthConst = "ARR_LEN"

    let ArrayItems = 32
    let ArrayItemsConst = "ARR_ITEMS"
    
    let BoolValue = 24
    let BoolValueConst = "BOOL_VAL"

    let IntValue = 24
    let IntValueConst = "INT_VAL"


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


module IntConstFacts =
    let MinPredefinedValue = -500
    let MaxPredefinedValue = 500


module RtNames =
    let ClassParentMap = "CLASS_PARENT_MAP"
    
    let BoolFalse = "Boolean_FALSE"
    let BoolTrue = "Boolean_TRUE"
    
    let UnitValue = "Unit_VALUE"
    
    let RtCopyObject = ".Runtime.copy_object"
    let RtAreEqual = ".Runtime.are_equal"
    let RtAbortMatch = ".Runtime.abort_match"
    let RtAbortDispatch = ".Runtime.abort_dispatch"
    
    let StringConcat = "String.concat"

    let IntGetOrCreate = "Int.get_or_create"

    let GenGCHandleAssign = ".GenGC.on_assign"
