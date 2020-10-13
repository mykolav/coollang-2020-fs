namespace LibCool.SemanticParts


type Label = Label of int


[<Sealed>]
type LabelGenerator() =
    
    
    let mutable _n: int = 0 

    
    member this.Generate(): Label =
        let label = Label _n
        _n <- _n + 1
        label
        
        
    member this.NameOf(label: Label): string =
        let (Label n) = label
        sprintf ".label_%i" n
