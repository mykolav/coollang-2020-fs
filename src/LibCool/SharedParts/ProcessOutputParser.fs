namespace LibCool.SharedParts


open System
open System.Collections.Generic
open System.Text


module ProcessOutputParser =
    
    
    let split_in_lines (output: string): seq<string> =
        let lines = List<string>()

        let mutable sb_line = StringBuilder()
        let mutable i = 0
        let mutable at_line_end = false
        
        while i < output.Length do
            let ch = output.[i]
            i <- i + 1
            
            if ch = '\r' && (i < output.Length && output.[i] = '\n')
            then
                i <- i + 1
                at_line_end <- true
            else if ch = '\r' || ch = '\n'
            then
                at_line_end <- true
            else if i >= output.Length
            then
                sb_line.Append(ch).Nop()
                at_line_end <- true
            
            if at_line_end
            then
                let line = sb_line.ToString().Trim()
                if not (String.IsNullOrWhiteSpace(line))
                then
                    lines.Add(line)
                    
                sb_line <- StringBuilder()
                at_line_end <- false
            else
                sb_line.Append(ch).Nop()
                
        lines :> seq<string>


