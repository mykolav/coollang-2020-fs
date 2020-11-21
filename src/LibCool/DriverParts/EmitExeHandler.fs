namespace LibCool.DriverParts


open System
open System.Runtime.CompilerServices
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

        let result = CompileToAsmDriver.Invoke(source, diags)
        
        if result.IsError
        then
            DiagRenderer.Render(diags, source, _writer)
            -1
        else
            
        let obj_file = if args.ExeFile.EndsWith(".exe")
                       then args.ExeFile.Replace(".exe", ".o")
                       else args.ExeFile + ".o"
        let as_output = ProcessRunner.Run(file_name="as",
                                          args=sprintf "-o %s" obj_file,
                                          stdin=result.Value)
        if not (String.IsNullOrWhiteSpace(as_output))
        then
            // We treat every message from 'as' as an error,
            // which is obviously not always correct.
            // But for now it will do.
            for line in ProcessOutputParser.split_in_lines as_output do
                diags.AsError(line)
                
            DiagRenderer.Render(diags, source, _writer)
            -1
        else

        // TODO: Get rid of the hardcoded path 'C:/msys64/mingw64/x86_64-w64-mingw32/lib'.
        // TODO: Assuming 'ld' is in PATH,
        // TODO: find 'ld''s location and build path to 'x86_64-w64-mingw32/lib'
        // TODO: relative to that location.
        let ld_args =
            sprintf "-o %s -e main %s rt_windows.o -L\"C:/msys64/mingw64/x86_64-w64-mingw32/lib\" -lkernel32"
                    args.ExeFile
                    obj_file
        let ld_output = ProcessRunner.Run(file_name="ld",
                                          args=ld_args,
                                          stdin=result.Value)
        if not (String.IsNullOrWhiteSpace(ld_output))
        then
            // We treat every message from 'ld' as an error,
            // which is obviously not always correct.
            // But for now it will do.
            for line in ProcessOutputParser.split_in_lines ld_output do
                diags.LdError(line)
            DiagRenderer.Render(diags, source, _writer)
            -1
        else

        DiagRenderer.Render(diags, source, _writer)
        0
