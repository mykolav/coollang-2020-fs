namespace LibCool.DriverParts


open System
open System.IO
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Text
open LibCool.DiagnosticParts
open LibCool.SharedParts
open LibCool.SourceParts


[<IsReadOnly; Struct>]
type EmitExeArgs =
    { SourceParts: seq<SourcePart>
      ExeFile: string }


[<Sealed>]
type EmitExeHandler(_writer: IWriteLine) =
    
    //
    // Theoretically, a handler could combine a number of shared steps into a pipeline,
    // to achieve the handler's goal.
    // E.g., resolving dirs, assembling, linking could each be a standalone step.
    // But, for our toy project, the only shared step we really need is `CompileToAsmStep`.
    //
    member _.Invoke(args: EmitExeArgs): int =
        let source = Source(args.SourceParts)
        let diags = DiagnosticBag()

        //
        // Compile to assembly
        //
        let result = CompileToAsmStep.Invoke(source, diags)
        match result with
        | Error ->
            DiagRenderer.Render(diags, source, _writer)
            -1
        | Ok asm ->
            let obj_file = if args.ExeFile.EndsWith(".exe")
                           then args.ExeFile.Replace(".exe", ".o")
                           else if args.ExeFile.EndsWith(".out")
                           then args.ExeFile.Replace(".out", ".o")
                           else args.ExeFile + ".o"

            //
            // Assemble an object file
            //
            let as_diags = EmitExeHandler.Assemble(asm=asm, obj_file=obj_file)
            if as_diags.Length > 0
            then
                // We treat every message from 'as' as an error,
                // which is obviously not always correct.
                // But it will do for now.
                for as_diag in as_diags do
                    diags.AsError(as_diag)

                DiagRenderer.Render(diags, source, _writer)
                -1
            else

            //
            // Link an executable
            //
            let ld_diags = EmitExeHandler.Link(obj_file=obj_file, exe_file=args.ExeFile)
            if ld_diags.Length > 0
            then
                // We treat every message from 'ld' as an error,
                // which is obviously not always correct.
                // But for now it will do.
                for ld_diag in ld_diags do
                    diags.LdError(ld_diag)

                DiagRenderer.Render(diags, source, _writer)
                -1
            else

            DiagRenderer.Render(diags, source, _writer)
            0
        
    
    static member Assemble(asm: string, obj_file: string) : string[] =
        ProcessRunner.Run(exe_name="as", args= $"-o %s{obj_file}", stdin=asm)
        |> ProcessOutputParser.splitInLines
        |> Array.ofSeq


    static member Link(obj_file: string, exe_file: string): string[] =
        let rt_dir = EmitExeHandler.ResolveRtDir()
        let rt_common_path = Path.Combine(rt_dir, "rt_common.o")
        let rt_gen_gc_path = Path.Combine(rt_dir, "rt_gen_gc.o")
        let rt_memory_path = Path.Combine(rt_dir, "rt_memory.o")
        let ld_args = StringBuilder(
            $"-o %s{exe_file} " +
            $"-e main %s{obj_file} " +
            $"\"%s{rt_common_path}\" " +
            $"\"%s{rt_memory_path}\" " +
            $"\"%s{rt_gen_gc_path}\" ")

        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        then
            ld_args.Append(sprintf "\"%s\" -L\"%s\" -lkernel32" (Path.Combine(rt_dir, "rt_windows.o"))
                                                                (EmitExeHandler.ResolveLibDir()))
                   .AsUnit()
            
            // A workaround for the "relocation truncated to fit: R_X86_64_32S" problem.
            // See README.md for more details.
            let ld_version = EmitExeHandler.ResolveLdVersion()
            if ld_version > 234
            then
                ld_args.Append(" --default-image-base-low").AsUnit()
                       
        else if RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        then
            ld_args.Append(sprintf "\"%s\"" (Path.Combine(rt_dir, "rt_linux.o"))).AsUnit()
        else
            invalidOp ($"'%s{RuntimeInformation.OSDescription}' is not supported.%s{Environment.NewLine}" +
                       $"Use '-S' to emit assembly anyway.%s{Environment.NewLine}")
            
        ProcessRunner.Run(exe_name="ld",
                          args=ld_args.ToString())
        |> ProcessOutputParser.splitInLines
        |> Array.ofSeq
    
    
    static member ResolveRtDir(): string =
        let uri_prefix =
            if RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            then "file:\\\\\\"
            else if RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            then "file://"
            else invalidOp ($"'%s{RuntimeInformation.OSDescription}' is not supported.%s{Environment.NewLine}" +
                            $"Use '-S' to emit assembly anyway.%s{Environment.NewLine}")

        // We assume the 'src/Runtime' folder is 4 levels up
        // relative to where our assembly is.
        // I.e. if we are in 'src/Somewhere/bin/Debug/net8.0'
        // a relative path to 'src/Runtime' is '../../../../Runtime'
        let assembly_path = typeof<EmitExeHandler>.Assembly.CodeBase.Substring(uri_prefix.Length)
        let rt_parent_dir = assembly_path
                            |> Path.GetDirectoryName
                            |> Path.GetDirectoryName
                            |> Path.GetDirectoryName
                            |> Path.GetDirectoryName
                            |> Path.GetDirectoryName
        Path.Combine(rt_parent_dir, "Runtime")
    
    
    // This function is only relevant for Windows.
    // Resolves to an absolute path of the MinGW64 dir
    // that contains 'libkernel32.a'.
    // E.g., 'C:/msys64/mingw64/x86_64-w64-mingw32/lib'
    static member ResolveLibDir(): string =
        let ld_path = ProcessRunner.Run(exe_name="where", args="ld")
        let ld_dir = Path.GetDirectoryName(ld_path)
        let mingw64_dir = Path.GetDirectoryName(ld_dir)
        Path.Combine(mingw64_dir, "x86_64-w64-mingw32\\lib").Replace('\\', '/')

    
    static member ResolveLdVersion(): int =
        let ld_version_text = ProcessRunner.Run(exe_name="ld", args="--version")
        let ld_version_line_opt =
            ld_version_text.Split([| "\r\n"; "\r"; "\n" |], StringSplitOptions.None)
            |> Seq.tryHead
            
        match ld_version_line_opt with
        | Some ld_version_line ->
            let ld_version_str =
                ld_version_line.Replace("GNU ld (GNU Binutils) ", "")
                               .Replace(".", "")
            int ld_version_str
        | None -> 0
