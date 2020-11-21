namespace Tests.Compiler


open System
open System.Collections.Generic
open System.IO
open System.Runtime.CompilerServices
open System.Text
open System.Text.RegularExpressions
open LibCool.SharedParts


[<Sealed>]
type CompilerTestCaseSource private () =
    
    
    // Test cases discovery runs from 'Tests/bin/Debug/netcoreapp3.1'
    // before we have a chance to change the working directory to 'Tests/CoolBuild'.
    [<Literal>]
    static let programs_discovery_path = @"../../../CoolPrograms/"
    
    
    static let discover_compiler_test_cases () =
        let test_cases =
            Directory.EnumerateFiles(programs_discovery_path, "*.cool", SearchOption.AllDirectories)
            |> Seq.map (fun it -> [| it.Replace(programs_discovery_path, "")
                                       .Replace("\\", "/") :> obj |])
            |> Array.ofSeq
        test_cases

    
    // In contrast to discovery, all the other code works
    // with programs paths relative to the 'CoolBuild' folder.
    static member ProgramsPath = "../CoolPrograms/"


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
        |> Seq.iteri (fun i it -> sb.AppendLine(sprintf "%d\t%s" (i + 1) it).Nop())
        
        sb.ToString()


[<IsReadOnly; Struct>]
type CompilerTestCase =
    { Path: string
      FileName: string
      Snippet: string
      ExpectedDiags: seq<string>
      ExpectedOutput: seq<string> }
    

    static member ReadFrom(path: string): CompilerTestCase =
        let lines = File.ReadAllLines(path) |> Seq.map (fun it -> it.Trim())
        { Path = path
          FileName = Path.GetFileNameWithoutExtension(path)
          ExpectedDiags = take_matching_lines lines re_expected_diags
          ExpectedOutput = take_matching_lines lines re_expected_output
          Snippet = take_snippet lines }


[<IsReadOnly; Struct>]
type CompilerOutput =
    { BuildSucceeded: bool
      Diags: seq<string>
      BinutilsDiags: seq<string> }


type ProgramOutput =
    { Output: seq<string> }


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
                sb_line.Append(ch).Nop()
                at_line_end <- true
            
            if at_line_end
            then
                lines.Add(sb_line.ToString().Trim())
                sb_line <- StringBuilder()
                at_line_end <- false
            else
                sb_line.Append(ch).Nop()
                
        lines :> seq<string>


    let parse_clc_output (clc_output: string): CompilerOutput =
        let lines = split_in_lines clc_output
        
        let diags = List<string>()
        let binutils_diags = List<string>()
        
        let mutable build_status_seen = false
        let mutable build_succeeded = false
        
        for line in lines do
            if not build_status_seen
            then
                diags.Add(line)
            else
                if not (String.IsNullOrWhiteSpace(line))
                then 
                    build_succeeded <- false
                    binutils_diags.Add(line)
                
            if line.StartsWith("Build ")
            then
                build_status_seen <- true
                build_succeeded <- line.StartsWith("Build succeeded")
        
        { BuildSucceeded = build_succeeded
          Diags = diags
          BinutilsDiags = binutils_diags }


    let parse_program_output (program_output: string): ProgramOutput =
        { Output = split_in_lines program_output
                   |> Seq.where (fun it -> not (String.IsNullOrWhiteSpace(it)))
                   |> Seq.map (fun it -> it.TrimEnd()) }

    
type CompilerOutput
with
    static member Parse(clc_output: string): CompilerOutput =
        parse_clc_output clc_output

    
type ProgramOutput
with
    static member Empty: ProgramOutput = { Output = [] }
    static member Parse(program_output: string): ProgramOutput =
        parse_program_output program_output
