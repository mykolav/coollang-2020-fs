namespace Tests.Compiler


open System.IO
open System.Runtime.CompilerServices
open System.Text
open System.Text.RegularExpressions


[<Sealed>]
type CompilerTestCaseSource private () =
    
    
    [<Literal>]
    static let programs_path = @"../../../CoolPrograms"
    
    
    static let discover_compiler_test_cases () =
        let test_cases =
            Directory.EnumerateFiles(programs_path, "*.cool", SearchOption.AllDirectories)
            |> Seq.map (fun it -> [| it.Replace(programs_path + "\\", "")
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
module private ClcOutputParser =
    let parse_diags (clc_output: string): seq<string> = Seq.empty
    let parse_output (clc_output: string) = Seq.empty


[<IsReadOnly; Struct>]
type CompilerOutput =
    { Diags: seq<string>
      Output: seq<string> }
    

    static member Parse(clc_output: string): CompilerOutput =
        { Diags = parse_diags clc_output
          Output = parse_output clc_output }
