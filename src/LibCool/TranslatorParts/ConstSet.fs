namespace LibCool.TranslatorParts


open System.Collections.Generic
open System.Runtime.CompilerServices


[<Struct; IsReadOnly>]
type ConstSetItem<'T> =
    { Label: string
      Value: 'T }



[<Sealed>]
type ConstSet<'T when 'T :equality>(prefix: string) =
    
    
    let _consts = Dictionary<'T, string>()
        
        
    member this.Items: seq<ConstSetItem<'T>> =
        _consts |> Seq.map (fun it -> { Label = it.Value; Value = it.Key })
    
    
    member this.GetOrAdd(value: 'T): string =
        if _consts.ContainsKey(value)
        then
            _consts.[value]
        else
        
        let label = sprintf "%s_%d" prefix _consts.Count
        _consts.Add(value, label)
            
        label
