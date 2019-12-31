namespace LibCool.Frontend

open System
open LibCool.SourceParts

type private Ch = Ch of char | EOF

type Lexer(_source: Source) =
    let mutable _offset: uint32 = 0u
    
    let eat_char() : Ch =
        if _offset = _source.Size
        then EOF
        else
        
        let result = Ch _source.[_offset]    
        _offset <- _offset + 1u
        result
        
    let peek_char() : Ch =
        if _offset < _source.Size
        then Ch _source.[_offset]
        else EOF
    
    let move_next() =
        ()

    let mutable _current = { Kind = Invalid ""; Span = HalfOpenRange.Invalid }
    
    member _.Current with get() = _current
    member _.MoveNext() = false
