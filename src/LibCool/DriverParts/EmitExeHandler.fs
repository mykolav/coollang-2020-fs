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
    
    
    member _.Invoke(args: EmitExeArgs): int =
        let source = Source(args.SourceParts)
        let diags = DiagnosticBag()

        //
        // Compile to assembly
        //
        let result = CompileToAsmDriver.Invoke(source, diags)
        if result.IsError
        then
            DiagRenderer.Render(diags, source, _writer)
            -1
        else
            
        let obj_file = if args.ExeFile.EndsWith(".exe")
                       then args.ExeFile.Replace(".exe", ".o")
                       else args.ExeFile + ".o"

        //
        // Assemble an object file
        //
        let as_diags = EmitExeHandler.Assemble(asm=result.Value, obj_file=obj_file)    
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
        ProcessRunner.Run(exe_name="as", args=sprintf "-o %s" obj_file, stdin=asm)
        |> ProcessOutputParser.split_in_lines
        |> Array.ofSeq


    static member Link(obj_file: string, exe_file: string): string[] =
        let rt_dir = EmitExeHandler.ResolveRtDir()
        let rt_common_path = Path.Combine(rt_dir, "rt_common.o")
        let ld_args = StringBuilder(sprintf "-o %s -e main %s \"%s\" " exe_file obj_file rt_common_path)
        
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        then
            ld_args.Append(sprintf "\"%s\" -L\"%s\" -lkernel32" (Path.Combine(rt_dir, "rt_windows.o"))
                                                                (EmitExeHandler.ResolveLibDir()))
                   .Nop()
        else if RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        then
            ld_args.Append(sprintf "\"%s\"" (Path.Combine(rt_dir, "rt_linux.o"))).Nop()
        else
            invalidOp (sprintf "'%s' is not supported.%sUse '-S' to emit assembly anyway.%s"
                               RuntimeInformation.OSDescription
                               Environment.NewLine
                               Environment.NewLine)
            
        ProcessRunner.Run(exe_name="ld",
                          args=ld_args.ToString())
        |> ProcessOutputParser.split_in_lines
        |> Array.ofSeq
    
    
    static member ResolveRtDir(): string =
        // We assume the 'src/Runtime' folder is 4 levels up
        // relative to where our assembly is.
        // I.e. if we are in 'src/Somewhere/bin/Debug/netcoreapp3.1'
        // a relative path to 'src/Runtime' is '../../../../Runtime'
        let assembly_path = typeof<EmitExeHandler>.Assembly.CodeBase.Substring("file:\\\\\\".Length)
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
