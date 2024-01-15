namespace LibCool.SemanticParts


open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Text.RegularExpressions


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
            _consts[value]
        else
        
        let label = $"%s{prefix}_%d{_consts.Count}"
        _consts.Add(value, label)
            
        label



[<Sealed>]
type IntConstSet(prefix: string) =


    let _valueToLabelMap = Dictionary<int, string>()


    member this.Items: seq<ConstSetItem<int>> =
        _valueToLabelMap |> Seq.map (fun it -> { Label = it.Value; Value = it.Key })


    member this.GetOrAdd(value: int): string =
        if _valueToLabelMap.ContainsKey(value)
        then
            _valueToLabelMap[value]
        else

        let suffix = if value >= 0
                     then value.ToString()
                     else $"_%d{-value}"

        let label = $"%s{prefix}_%s{suffix}"
        _valueToLabelMap.Add(value, label)

        label



[<Sealed>]
type StringConstSet(prefix: string) =


    static let RegexId = Regex("^[a-zA-Z_][0-9a-zA-Z_]*$", RegexOptions.Compiled)
    let _valueToLabelMap = Dictionary<string, string>()


    member this.Items: seq<ConstSetItem<string>> =
        _valueToLabelMap |> Seq.map (fun it -> { Label = it.Value; Value = it.Key })


    member this.GetOrAdd(value: string): string =
        if _valueToLabelMap.ContainsKey(value)
        then
            _valueToLabelMap[value]
        else

        let suffix = if value = ""
                     then "_EMPTY"
                     else if RegexId.IsMatch(value)
                          then "_" + value
                          else ""

        let label = $"%s{prefix}_%d{_valueToLabelMap.Count}%s{suffix}"
        _valueToLabelMap.Add(value, label)

        label
