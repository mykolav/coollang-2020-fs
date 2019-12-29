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

[<DebuggerDisplay("PL: [{parts.Length}] - S: [{Size}]")>]
type Source(partSeq: SourcePart seq) =
    let parts = Array.ofSeq partSeq
    do
       if Array.isEmpty parts
       then invalidArg "partSeq" "Expected at least one SourcePart but partSeq is empty"
    
    // Combine the parts' content    
    let content =
        let sb = StringBuilder()
        parts |> Array.iter (fun it -> sb.Append(it.Content) |> ignore)
        sb.ToString()
        
    // Extract the parts' file names
    let file_names = parts |> Array.map (fun it -> it.FileName)
    
    // Calculate the part offsets
    let (part_offsets, size) =
        parts |> Array.mapFold (fun offset it -> (offset, (*next offset: *) offset + uint32 it.Content.Length))
                               (*first offset: *) 0u

    // Calculate the line offsets
    let line_offsets =
        let acc_offsets = List<Offset>()
        
        // The first line's offset is by definition 0,
        // all the other offsets we'll calculate
        acc_offsets.Add(0u)
        
        let mutable i = 0
        while uint32 i < size do
           // Did we find a line's end?
           if content.[i] = '\r' || content.[i] = '\n'
           then do
               // If we found "\r\n" or "\n\r",
               // make sure we consider both chars as a single line end combination
               if (uint32 i + 1u < size) && (content.[i + 1] = '\r' || content.[i + 1] = '\n')
               then do
                   i <- i + 1
               
               // OK, we moved past the current line's end.
               // If we haven't reached the source's end, add the next line's start offset 
               if (uint32 i + 1u < size)
               then do
                   acc_offsets.Add(uint32 i + 1u)
           // Move to the next char
           i <- i + 1
        Array.ofSeq acc_offsets

    let ensure_in_range (offset: Offset) =
        if offset >= size
        then raise (ArgumentOutOfRangeException(
                        "offset",
                        sprintf "Expected offset >= 0 and < [%d] but it was [%d]" size offset))
            
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
        
    // Translate a global offset to the source part's index
    let part_index_from offset =
        index_of offset part_offsets
        
    // Translate a global offset to the line and col numbers  
    let line_col_from offset =
        let line = index_of offset line_offsets
        // We add 1 to the line and col, as lines and columns numbering starts from 1:1
        // (and not 0:0)
        struct {| Line = uint32 line + 1u; Col = offset - line_offsets.[line] + 1u |}
        
    member val Size = size
    
    member _.Item with get(offset: Offset) =
        ensure_in_range offset
        content.[int offset]
        
    member this.Translate(offset: Offset) =
        let part = part_index_from offset
        let lc = line_col_from offset
        { FileName = file_names.[part]; Line = lc.Line; Col = lc.Col }
