namespace Tests.Support


open System


[<Sealed>]
type Indent(?width: int) =
    let _width = defaultArg width 2
    let mutable _level = 0
    
    let mk_value () = String(' ', count = _level * _width)
    let mutable _value = mk_value ()
    
    
    member this.Increase() =
        _level <- _level + 1
        _value <- mk_value ()
        
    
    member this.Decrease() =
        if _level = 0
        then
            invalidOp "An indent's level cannot go less than 0"
            
        _level <- _level - 1
        _value <- mk_value ()
       
    
    override this.ToString() = _value


