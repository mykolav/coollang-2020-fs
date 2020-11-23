namespace LibCool.TranslatorParts


open System.Runtime.CompilerServices
open LibCool.SemanticParts


[<IsReadOnly; Struct>]
type AsmFragment =
    { Type: ClassSymbol
      Asm: string
      Reg: Reg }


[<IsReadOnly; Struct>]
type AddrFragment =
    { Asm: string voption
      Addr: string
      Type: ClassSymbol
      Reg: Reg }    
