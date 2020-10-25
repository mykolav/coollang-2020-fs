namespace LibCool.SemanticParts


open System.Collections.Generic


[<Sealed>]
type ConstSet<'T when 'T :equality>(prefix: string) =
    
    
    let _consts = Dictionary<'T, string>()
        
        
    member this.Consts: seq<((*label:*)string*(*value:*)'T)> =
        _consts |> Seq.map (fun it -> (it.Value, it.Key))
    
    
    member this.GetOrAdd(value: 'T): string =
        if _consts.ContainsKey(value)
        then
            _consts.[value]
        else
        
        let label = sprintf "%s_%d" prefix _consts.Count
        _consts.Add(value, label)
            
        label
