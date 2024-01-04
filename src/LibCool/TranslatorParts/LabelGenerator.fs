namespace LibCool.TranslatorParts


type Label = Label of int * string


[<Sealed>]
type LabelGenerator() =
    
    
    let mutable _n: int = 0 

    
    member this.Generate(name: string): Label =
        let label = Label (_n, name)
        _n <- _n + 1
        label
        
        
    member this.NameOf(label: Label): string =
        let (Label (n, name)) = label
        $".L%i{n}_{name}"
