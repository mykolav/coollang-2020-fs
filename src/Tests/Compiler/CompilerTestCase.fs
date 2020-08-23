namespace Tests.Compiler


open System.Collections.Generic
open System.IO
open System.Runtime.CompilerServices
open System.Text
open System.Text.RegularExpressions


[<Sealed>]
type CompilerTestCaseSource private () =
    
    
    [<Literal>]
    static let programs_path = @"../../../CoolPrograms/"
    
    
    static let discover_compiler_test_cases () =
        let test_cases =
            Directory.EnumerateFiles(programs_path, "*.cool", SearchOption.AllDirectories)
            |> Seq.map (fun it -> [| it.Replace(programs_path, "")
                                       .Replace("\\", "/") :> obj |])
            |> Array.ofSeq
        test_cases

    
    static member ProgramsPath = programs_path


    static member TestCases = discover_compiler_test_cases ()


[<AutoOpen>]    
module private CompilerTestCaseParser =

    
    let re_expected_diags = Regex("//\\s*DIAG:\\s*", RegexOptions.Compiled)
    let re_expected_output = Regex("//\\s*OUT:\\s*", RegexOptions.Compiled)

    
    let take_matching_lines (lines: seq<string>) (re: Regex) =
        lines
        |> Seq.filter(fun it -> re.IsMatch(it))
        |> Seq.map(fun it -> re.Replace(it, ""))


    let take_snippet (lines: seq<string>): string =
        let sb = StringBuilder()
        lines
        |> Seq.takeWhile (fun it -> not (re_expected_diags.IsMatch(it) || re_expected_output.IsMatch(it)))
        |> Seq.iteri (fun i it -> sb.AppendLine(sprintf "%d\t%s" (i + 1) it) |> ignore)
        
        sb.ToString()


[<IsReadOnly; Struct>]
type CompilerTestCase =
    { Path: string
      FileName: string
      Snippet: string
      ExpectedDiags: seq<string>
      ExpectedOutput: seq<string> }
    

    static member ReadFrom(path: string): CompilerTestCase =
        let lines = File.ReadAllLines(path)
        { Path = path
          FileName = Path.GetFileNameWithoutExtension(path)
          ExpectedDiags = take_matching_lines lines re_expected_diags
          ExpectedOutput = take_matching_lines lines re_expected_output
          Snippet = take_snippet lines }


[<AutoOpen>]
module private CompilerOutputParser =
    
    
    let private split_in_lines (clc_output: string): seq<string> =
        let lines = List<string>()

        let mutable sb_line = StringBuilder()
        let mutable i = 0
        let mutable at_line_end = false
        
        while i < clc_output.Length do
            let ch = clc_output.[i]
            i <- i + 1
            
            if ch = '\r' && (i < clc_output.Length && clc_output.[i] = '\n')
            then
                i <- i + 1
                at_line_end <- true
            else if ch = '\r' || ch = '\n'
            then
                at_line_end <- true
            else if i >= clc_output.Length
            then
                sb_line.Append(ch) |> ignore
                at_line_end <- true
            
            if at_line_end
            then
                lines.Add(sb_line.ToString())
                sb_line <- StringBuilder()
                at_line_end <- false
            else
                sb_line.Append(ch) |> ignore
                
        lines :> seq<string>


    let parse (clc_output: string): struct {| Diags: seq<string>; Output: seq<string> |} =
        let lines = split_in_lines clc_output
        
        let diags = List<string>()
        let output = List<string>()
        
        let mutable build_status_seen = false
        
        for line in lines do
            if not build_status_seen
            then
                diags.Add(line)
            else
                output.Add(line)
                
            if line.StartsWith("Build ")
            then
                build_status_seen <- true
        
        struct {| Diags = diags; Output = output |}


[<IsReadOnly; Struct>]
type CompilerOutput =
    { Diags: seq<string>
      Output: seq<string> }

    
    static member Parse(clc_output: string): CompilerOutput =
            
        let it = parse clc_output
        { Diags = it.Diags; Output = it.Output }
