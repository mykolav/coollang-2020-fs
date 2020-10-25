namespace rec LibCool.SemanticParts


open System.Collections.Generic
open LibCool.SourceParts
open LibCool.DiagnosticParts
open LibCool.AstParts
open LibCool.SemanticParts


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
