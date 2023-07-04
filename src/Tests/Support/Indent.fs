namespace Tests.Support


open System


[<Sealed>]
type Indent(?width: int) =
    
    
    let _width = defaultArg width 2
    let mutable _level = 0
    
    
    let mkValue () =
        let count = _level * _width
        if count = 0
        then
            ""
        else
            String(' ', count = _level * _width)
            
            
    let mutable _value = mkValue ()
    
    
    member this.Increase() =
        _level <- _level + 1
        _value <- mkValue ()
        
    
    member this.Decrease() =
        if _level = 0
        then
            invalidOp "An indent's level cannot go less than 0"
            
        _level <- _level - 1
        _value <- mkValue ()
       
    
    override this.ToString() = _value
