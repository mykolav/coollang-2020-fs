namespace LibCool.DriverParts


open System.IO
open System.Runtime.CompilerServices
open LibCool.DiagnosticParts
open LibCool.SharedParts
open LibCool.SourceParts
open LibCool.TranslatorParts


[<IsReadOnly; Struct>]
type EmitAsmArgs =
    { SourceParts: seq<SourcePart>
      AsmFile: string voption
      CodeGenOptions: CodeGenOptions }


[<Sealed>]
type EmitAsmHandler(_writer: IWriteLine) =
    member this.Invoke(args: EmitAsmArgs): int =
        let source = Source(args.SourceParts)
        let diags = DiagnosticBag()

        let result = CompileToAsmStep.Invoke(source, diags, args.CodeGenOptions)
        DiagRenderer.Render(diags, source, _writer)
        
        match result with
        | Error ->
            -1
        | Ok asm ->
            match args.AsmFile with
            | ValueSome asm_file -> File.WriteAllText(asm_file, asm)
            | ValueNone          -> _writer.WriteLine(asm)

            0
