namespace LibCool.SourceParts


open System
open System.Collections.Generic
open System.Diagnostics
open System.Runtime.CompilerServices
open System.Text


[<IsReadOnly; Struct>]
type SourcePart =
    { FileName: string
      Content: string }


[<DebuggerDisplay("FL: [{_file_names.Length}] - S: [{_size}]")>]
type Source(partSeq: seq<SourcePart>) =
    
    
    let parts = Array.ofSeq partSeq
    do
       if Array.isEmpty parts
       then invalidArg "partSeq" "Expected at least one SourcePart but partSeq is empty"
    
    // Combine the parts' content    
    let _content =
        let sb = StringBuilder()
        parts |> Array.iter (fun it -> sb.Append(it.Content) |> ignore)
        sb.ToString()
        
    // Extract the parts' file names
    let _file_names = parts |> Array.map (fun it -> it.FileName)
    
    // Calculate the part offsets
    let (_part_offsets, _size) =
        parts |> Array.mapFold (fun offset it -> (offset, (*next offset=*) offset + uint32 it.Content.Length))
                               (*first offset=*) 0u

    // Calculate the line offsets
    let _line_offsets =
        let acc_offsets = List<uint32>()
        
        // The first line's offset is by definition 0,
        // all the other offsets we'll calculate
        acc_offsets.Add(0u)
        
        let mutable i = 0
        while uint32 i < _size do
           // Did we find a line's end?
           if _content[i] = '\r' || _content[i] = '\n'
           then do
               // If we found "\r\n" or "\n\r",
               // make sure we consider both chars as a single line-end combination
               if (uint32 i + 1u < _size) && (_content[i + 1] = '\r' || _content[i + 1] = '\n')
               then do
                   i <- i + 1
               
               // OK, we moved past the current line's end.
               // If we haven't reached past the source's end, i + 1 is the next line's start offset.
               // We treat (i + 1u = _size) as a valid case to accomodate
               // diagnostics that have their location at EOF
               // (see the comments in `ensure_in_range` for more details).
               if (uint32 i + 1u <= _size)
               then do
                   acc_offsets.Add(uint32 i + 1u)
           // Move to the next char
           i <- i + 1
        Array.ofSeq acc_offsets

    
    let ensure_in_range (offset: uint32) =
        // We treat (offset = _size) as a valid case to accomodate
        // diagnostics that have their location at EOF.
        // E.g., if the parsers goes to the end of file looking for ')'
        // and still cannot find it. The corresponding diag's location is at EOF.
        if offset > _size
        then raise (ArgumentOutOfRangeException(
                        "offset",
                        $"Expected offset >= 0 and <= [%d{_size}] but it was [%d{offset}]"))
            
    
    // Binary search the exact or closest left index of an offset in an array of offsets
    let index_of offset offsets =
        ensure_in_range offset

        let search_result = Array.BinarySearch(offsets, offset)
        let index = if search_result >= 0
                    then search_result
                    else ~~~search_result - 1
        
        if index >= offsets.Length
        then invalidOp $"index [%d{index}] >= offsets.Length [%d{offsets.Length}]"
        
        index
        
    
    // Map a global offset to the source part's index
    let part_index_of offset =
        index_of offset _part_offsets
        
    
    // Map a global offset to the line and col numbers  
    let line_col_of offset =
        let line = index_of offset _line_offsets
        // We add 1 to the line and col, as lines and columns numbering starts from 1:1
        // (and not 0:0)
        struct {| Line = uint32 line + 1u; Col = offset - _line_offsets[line] + 1u |}
        
    
    member _.Size with get(): uint32 = _size
    
    
    member _.Item with get(offset: uint32): char =
        ensure_in_range offset
        _content[int offset]
        
    
    member _.GetSlice(start: uint32 option, finish: uint32 option): string =
        let start = defaultArg start 0u
        
        let content_finish = uint32 _content.Length - 1u
        let finish = defaultArg finish content_finish
        let finish = if finish <= content_finish
                     then finish
                     else content_finish
        
        // Wny do we add 1u to get the slice's length?
        // [first, start] is a closed interval (includes its endpoints).
        // So, the length of [0, 0] is 1 = (0 - 0) + 1
        //     the length of [0, 1] is 2 = (1 - 0) + 1
        //     etc ...
        let length = (finish - start) + 1u
                     
        _content.Substring(startIndex=int start, length=int length)


    member this.Map(offset: uint32): Location =
        if offset = UInt32.MaxValue
        then
            { FileName = "Virtual"; Line = 0u; Col = 0u }
        else
            
        let part = part_index_of offset
        let lc = line_col_of offset
        { FileName = _file_names[part]; Line = lc.Line; Col = lc.Col }
