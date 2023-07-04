open System.Collections.Generic
open System.IO

module Program =


    [<Literal>]
    let cool_programs_path = @"../../../CoolPrograms"
    
    
    let printCoolProgramPaths () =
        Directory.EnumerateFiles(cool_programs_path, "*.cool", SearchOption.AllDirectories)
        |> Seq.iter (fun it -> printfn "%s" (it.Replace(cool_programs_path + "\\", "").Replace("\\", "/")))
        
        
    let printCoolProgramPaths2 () =
        
        let rec enum_files (directory: string): string seq =
            Seq.concat [
                Directory.EnumerateFiles(directory, "*.cool")
                Seq.collect enum_files (Directory.EnumerateDirectories(directory)) ]
            
        enum_files cool_programs_path
        |> Seq.iter (fun it -> printfn "%s" (it.Replace(cool_programs_path + "\\", "").Replace("\\", "/")))
        
        
    let printCoolProgramPaths3 () =
        
        let rec enum_files (directory: string) (files: List<string>) =
            files.AddRange(Directory.EnumerateFiles(directory, "*.cool"))
            Directory.EnumerateDirectories(directory)
            |> Seq.iter (fun it -> enum_files it files) 
            
        let program_paths = List<string>()
        enum_files cool_programs_path program_paths
        
        program_paths
        |> Seq.iter (fun it -> printfn "%s" (it.Replace(cool_programs_path + "\\", "").Replace("\\", "/")))


    let [<EntryPoint>] main _ =
        printCoolProgramPaths3 ()
        0
