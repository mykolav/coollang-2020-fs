namespace LibCool.DriverParts


open System.IO
open System.Runtime.CompilerServices
open LibCool.DiagnosticParts
open LibCool.SourceParts


[<IsReadOnly; Struct>]
type EmitAsmArgs =
    { SourceParts: seq<SourcePart>
      AsmFile: string voption }


[<Sealed>]
type EmitAsmHandler(_writer: IWriteLine) =
    member this.Invoke(args: EmitAsmArgs): int =
        let source = Source(args.SourceParts)
        let diags = DiagnosticBag()

        let result = CompileToAsmStep.Invoke(source, diags)
        DiagRenderer.Render(diags, source, _writer)
        
        if result.IsError
        then
            -1
        else

        match args.AsmFile with
        | ValueSome asm_file -> File.WriteAllText(asm_file, result.Value)
        | ValueNone          -> _writer.WriteLine(result.Value) 
            
        0
