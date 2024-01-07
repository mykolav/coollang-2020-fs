namespace Tests.Compiler


open System
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open LibCool.SharedParts


[<Sealed>]
type CompilerTestCaseSource private () =
    
    
    // Test cases discovery runs from 'Tests/bin/Debug/net8.0'
    // before we have a chance to change the working directory to 'Tests/CoolBuild'.
    [<Literal>]
    static let programs_discovery_path = @"../../../CoolPrograms/"
    
    
    static let excluded_files: string[] = [||]
    
    
    static let isExcludedPath (path: string): bool =
        path.Contains("Runtime/GenGC/") ||
        excluded_files |> Seq.exists (fun it -> path.EndsWith(it))
    
    
    static let discoverCompilerTestCases () =
        let test_cases =
            Directory.EnumerateFiles(programs_discovery_path, "*.cool", SearchOption.AllDirectories)
            |> Seq.map (_.Replace(programs_discovery_path, "").Replace("\\", "/"))
            |> Seq.where (fun it -> not (isExcludedPath it))
            |> Seq.map (fun it -> [| it :> obj |])
            |> Array.ofSeq
        test_cases

    
    // In contrast to discovery, all the other code works
    // with programs paths relative to the 'CoolBuild' folder.
    static member ProgramsPath = "../CoolPrograms/"


    static member TestCases = discoverCompilerTestCases ()


    static member CwdCoolBuild() =
        // The test assembly's location is 'Tests/bin/Debug/net8.0',
        // We want to change to 'Tests/CoolBuild'.
        let assemblyLocationUri = UriBuilder(Assembly.GetExecutingAssembly().Location)
        let assemblyLocationPath = Uri.UnescapeDataString(assemblyLocationUri.Path)
        let assemblyLocationDirectoryName = Path.GetDirectoryName(assemblyLocationPath)
        Directory.SetCurrentDirectory(Path.Combine(assemblyLocationDirectoryName, "../../../CoolBuild"))


[<AutoOpen>]    
module private CompilerTestCaseParser =

    
    let re_expected_diags = Regex("//\\s*DIAG:\\s*", RegexOptions.Compiled)
    let re_expected_input = Regex("//\\s*IN:\\s*", RegexOptions.Compiled)
    let re_expected_output = Regex("//\\s*OUT:\\s*", RegexOptions.Compiled)

    
    let takeMatchingLines (lines: seq<string>) (re: Regex) =
        lines
        |> Seq.filter(fun it -> re.IsMatch(it))
        |> Seq.map(fun it -> re.Replace(it, ""))

    let isSnippetLine (line: string): bool =
        not (re_expected_diags.IsMatch(line) || re_expected_output.IsMatch(line))


[<IsReadOnly; Struct>]
type CompilerTestCaseRun =
    { GivenInput: seq<string>
      ExpectedOutput: seq<string> }


[<IsReadOnly; Struct>]
type CompilerTestCase =
    { Path: string
      FileName: string
      Snippet: string
      ExpectedDiags: seq<string>
      Runs: CompilerTestCaseRun[] }


    static member ParseFrom(path: string): CompilerTestCase =
        let file_lines = File.ReadAllLines(path) |> Array.map (_.Trim())
        let mutable line_index = 0

        let snippet_lines = List<string>()
        while line_index < file_lines.Length && isSnippetLine file_lines[line_index] do
            snippet_lines.Add(file_lines[line_index])
            line_index <- line_index + 1

        let inputs = List<seq<string>>()
        let outputs = List<seq<string>>()
        while line_index < file_lines.Length do
            let multiline_input = List<string>()
            while line_index < file_lines.Length && re_expected_input.IsMatch(file_lines[line_index]) do
                multiline_input.Add(re_expected_input.Replace(file_lines[line_index], ""))
                line_index <- line_index + 1

            if multiline_input.Count > 0
            then
                inputs.Add(multiline_input)

            let multiline_output = List<string>()
            while line_index < file_lines.Length && re_expected_output.IsMatch(file_lines[line_index]) do
                multiline_output.Add(re_expected_output.Replace(file_lines[line_index], ""))
                line_index <- line_index + 1

            if multiline_output.Count > 0
            then
                outputs.Add(multiline_output)

            while line_index < file_lines.Length &&
                  not (re_expected_input.IsMatch(file_lines[line_index])) &&
                  not (re_expected_output.IsMatch(file_lines[line_index])) do
                line_index <- line_index + 1

        let mutable run_index = 0
        let runs = List<CompilerTestCaseRun>()
        while run_index < outputs.Count do
            runs.Add({
                GivenInput = if run_index < inputs.Count
                             then inputs[run_index]
                             else []
                ExpectedOutput = outputs[run_index]
            })
            run_index <- run_index + 1

        { Path = path
          FileName = Path.GetFileNameWithoutExtension(path)
          Snippet = String.Join(Environment.NewLine, snippet_lines)
          ExpectedDiags = takeMatchingLines file_lines re_expected_diags
          Runs = runs.ToArray() }


[<IsReadOnly; Struct>]
type CompilerOutput =
    { BuildSucceeded: bool
      Diags: seq<string>
      BinutilsDiags: seq<string> }


type ProgramOutput =
    { Output: seq<string> }


[<AutoOpen>]
module private CompilerTestCaseOutputParser =
    
    
    let parseClcOutput (clc_output: string): CompilerOutput =
        let lines = ProcessOutputParser.splitInLines clc_output
        
        let diags = List<string>()
        let binutils_diags = List<string>()
        
        let mutable build_status_seen = false
        let mutable build_succeeded = false
        
        for line in lines do
            if not build_status_seen
            then
                diags.Add(line)
            else
                build_succeeded <- false
                binutils_diags.Add(line)
                
            if line.StartsWith("Build ")
            then
                build_status_seen <- true
                build_succeeded <- line.StartsWith("Build succeeded")
        
        { BuildSucceeded = build_succeeded
          Diags = diags
          BinutilsDiags = binutils_diags }


    let parseProgramOutput (program_output: string): ProgramOutput =
        { Output = ProcessOutputParser.splitInLines program_output }

    
type CompilerOutput
with
    static member Parse(clc_output: string): CompilerOutput =
        parseClcOutput clc_output

    
type ProgramOutput
with
    static member Empty: ProgramOutput = { Output = [] }
    static member Parse(program_output: string): ProgramOutput =
        parseProgramOutput program_output
