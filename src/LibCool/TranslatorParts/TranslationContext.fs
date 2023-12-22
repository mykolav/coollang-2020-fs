namespace rec LibCool.TranslatorParts


open System.Collections.Generic
open System.Runtime.CompilerServices
open LibCool.SourceParts
open LibCool.DiagnosticParts
open LibCool.AstParts
open LibCool.SemanticParts
open LibCool.TranslatorParts


[<IsReadOnly; Struct>]
type GarbageCollectorKind
    = Nop          // No op garbage collector
    | Generational // Generational garbage collector


[<IsReadOnly; Struct>]
type CodeGenOptions =
    { GC: GarbageCollectorKind }


type TranslationContext =
    { CodeGenOptions: CodeGenOptions
      ClassSymMap: IReadOnlyDictionary<TYPENAME, ClassSymbol>
      TypeCmp: TypeComparer
      RegSet: RegisterSet
      LabelGen: LabelGenerator
      // Diags
      Diags: DiagnosticBag
      Source: Source
      // Accumulators
      IntConsts: IntConstSet
      StrConsts: StringConstSet }
