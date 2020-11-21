namespace LibCool.DriverParts

open LibCool.DiagnosticParts
open LibCool.SourceParts


module DiagRenderer =
    let Render(diagnostic_bag: DiagnosticBag, source: Source, writer: IWriteLine): unit =
        // DIAG: SemiExpected.cool(3,35): Error: ';' expected
        // DIAG: Build failed: Errors: 1. Warnings: 0
        // DIAG: Build succeeded: Errors: 0. Warnings: 0
        
        for diag in diagnostic_bag.Diags do
            let location = source.Map(diag.Span.First)
            writer.WriteLine(sprintf "%O: %s: %s" location
                                                  (diag.Severity.ToString().Replace("Severity.", ""))
                                                  diag.Message)

        
        for binutils_error in diagnostic_bag.BinutilsErrors do
            writer.WriteLine(binutils_error)

        if diagnostic_bag.ErrorsCount = 0
        then
            writer.WriteLine(sprintf "Build succeeded: Errors: 0. Warnings: %d"
                                     diagnostic_bag.WarningsCount)
        else
            writer.WriteLine(sprintf "Build failed: Errors: %d. Warnings: %d"
                                     diagnostic_bag.ErrorsCount
                                     diagnostic_bag.WarningsCount)

