namespace rec LibCool.TranslatorParts


open System.Collections.Generic
open LibCool.SourceParts
open LibCool.DiagnosticParts
open LibCool.AstParts
open LibCool.TranslatorParts


type TranslationContext =
    { ClassSymMap: IReadOnlyDictionary<TYPENAME, ClassSymbol>
      TypeCmp: TypeComparer
      RegSet: RegisterSet
      LabelGen: LabelGenerator
      // Diags
      Diags: DiagnosticBag
      Source: Source
      // Accumulators
      IntConsts: ConstSet<int>
      StrConsts: ConstSet<string> }
