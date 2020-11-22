namespace LibCool.TranslatorParts


module SysVAmd64AbiFacts =
    let ActualRegs = [| "%rdi"; "%rsi"; "%rdx"; "%rcx"; "%r8"; "%r9" |]
    let CalleeSavedRegs = [| "%rbx"; "%r12"; "%r13"; "%r14"; "%r15" |]
    let CallerSavedRegs = [| "%r10"; "%r11" |]


module MemoryLayoutFacts =
    let QuadSizeInBytes = 8


module FrameLayoutFacts =
    let CalleeSavedRegsSizeInBytes = SysVAmd64AbiFacts.CalleeSavedRegs.Length *
                                     MemoryLayoutFacts.QuadSizeInBytes
    // skip saved %rbp, and return addr
    let ActualsOutOfFrameOffset = 2 * MemoryLayoutFacts.QuadSizeInBytes
    let ActualsOffsetInBytes = 0