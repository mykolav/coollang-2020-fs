open System.Collections.Generic
open System.IO

module Program =


    [<Literal>]
    let cool_programs_path = @"../../../CoolPrograms"
    
    
    let printCoolProgramPaths () =
        Directory.EnumerateFiles(cool_programs_path, "*.cool", SearchOption.AllDirectories)
        |> Seq.iter (fun it -> printfn "%s" (it.Replace(cool_programs_path + "\\", "").Replace("\\", "/")))
        
        
    let printCoolProgramPaths2 () =
        
        let rec enumFiles (directory: string): string seq =
            Seq.concat [
                Directory.EnumerateFiles(directory, "*.cool")
                Seq.collect enumFiles (Directory.EnumerateDirectories(directory)) ]

        enumFiles cool_programs_path
        |> Seq.iter (fun it -> printfn "%s" (it.Replace(cool_programs_path + "\\", "").Replace("\\", "/")))
        
        
    let printCoolProgramPaths3 () =
        
        let rec enumFiles (directory: string) (files: List<string>) =
            files.AddRange(Directory.EnumerateFiles(directory, "*.cool"))
            Directory.EnumerateDirectories(directory)
            |> Seq.iter (fun it -> enumFiles it files)

        let program_paths = List<string>()
        enumFiles cool_programs_path program_paths
        
        program_paths
        |> Seq.iter (fun it -> printfn "%s" (it.Replace(cool_programs_path + "\\", "").Replace("\\", "/")))


    let [<EntryPoint>] main _ =
        printCoolProgramPaths3 ()
        0
