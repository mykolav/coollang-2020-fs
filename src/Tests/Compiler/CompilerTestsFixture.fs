namespace Tests.Compiler


open System
open System.IO
open Xunit


type CompilerTestsFixture() =
    do 
        // Our current directory is 'Tests/bin/Debug/netcoreapp3.1',
        // i.e. where the tests assembly gets build into.
        // We want to change to 'Tests/CoolBuild'.
        Directory.SetCurrentDirectory("../../../CoolBuild")


    interface IDisposable with
        member _.Dispose() = ()


[<CollectionDefinition("Compiler collection"); AbstractClass>]
type DatabaseCollection =
    interface ICollectionFixture<CompilerTestsFixture>
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
