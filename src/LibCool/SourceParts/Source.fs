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
type Source(partSeq: SourcePart seq) =
    
    
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
        parts |> Array.mapFold (fun offset it -> (offset, (*next offset: *) offset + uint32 it.Content.Length))
                               (*first offset: *) 0u

    // Calculate the line offsets
    let _line_offsets =
        let acc_offsets = List<uint32>()
        
        // The first line's offset is by definition 0,
        // all the other offsets we'll calculate
        acc_offsets.Add(0u)
        
        let mutable i = 0
        while uint32 i < _size do
           // Did we find a line's end?
           if _content.[i] = '\r' || _content.[i] = '\n'
           then do
               // If we found "\r\n" or "\n\r",
               // make sure we consider both chars as a single line-end combination
               if (uint32 i + 1u < _size) && (_content.[i + 1] = '\r' || _content.[i + 1] = '\n')
               then do
                   i <- i + 1
               
               // OK, we moved past the current line's end.
               // If we haven't reached the source's end, i + 1 is the next line's start offset 
               if (uint32 i + 1u < _size)
               then do
                   acc_offsets.Add(uint32 i + 1u)
           // Move to the next char
           i <- i + 1
        Array.ofSeq acc_offsets

    
    let ensure_in_range (offset: uint32) =
        if offset >= _size
        then raise (ArgumentOutOfRangeException(
                        "offset",
                        sprintf "Expected offset >= 0 and < [%d] but it was [%d]" _size offset))
            
    
    // Binary search the exact or closest left index of an offset in an array of offsets
    let index_of offset offsets =
        ensure_in_range offset

        let search_result = System.Array.BinarySearch(offsets, offset)
        let index = if search_result >= 0
                    then search_result
                    else ~~~search_result - 1
        
        if index >= offsets.Length
        then invalidOp (sprintf "index [%d] >= offsets.Length [%d]"
                                index
                                offsets.Length)
        
        index
        
    
    // Map a global offset to the source part's index
    let part_index_of offset =
        index_of offset _part_offsets
        
    
    // Map a global offset to the line and col numbers  
    let line_col_of offset =
        let line = index_of offset _line_offsets
        // We add 1 to the line and col, as lines and columns numbering starts from 1:1
        // (and not 0:0)
        struct {| Line = uint32 line + 1u; Col = offset - _line_offsets.[line] + 1u |}
        
    
    member _.Size with get() = _size
    
    
    member _.Item with get(offset: uint32) =
        ensure_in_range offset
        _content.[int offset]
        
    
    member this.Map(offset: uint32) =
        let part = part_index_of offset
        let lc = line_col_of offset
        { FileName = _file_names.[part]; Line = lc.Line; Col = lc.Col }
