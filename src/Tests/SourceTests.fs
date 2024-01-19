namespace Tests


open System
open Xunit
open LibCool.SourceParts
open Tests.Support


[<RequireQualifiedAccess>]
module private TestSource =


    [<Literal>]
    let private first_part_content =
        "class Fib() extends IO() {\n" +
        "    def fib(x: Int): Int =\n" +
        "        if (x == 0) 0\n" +
        "        else if (x == 1) 1\n" +
        "        else fib(x - 2) + fib(x - 1);\n" +
        "    \r\n" +
        "    {\r\n" +
        "        out_string(\"fib(10) = \");\n" +
        "        out_int(fib(10));\n" +
        "        out_nl()\n" +
        "    };\r\n" +
        "}"


    let first_part: SourcePart = { FileName = "first.cool"
                                   Content  = first_part_content }


    [<Literal>]
    let private second_part_content =
        "class Main() {\r\n" +
        "    { new Fib() };\n" +
        "}\r\n"


    let second_part: SourcePart = { FileName = "second.cool"
                                    Content  = second_part_content }


    let parts = [first_part; second_part]


    let combined = Source(parts)


type SourceTests() =


    [<Fact>]
    member this.``Sum of the part sizes = the source size``() =
        let sum_of_parts_size = TestSource.parts |> List.sumBy (_.Content.Length) |> uint32
        Assert.That(TestSource.combined.Size)
              .IsEqualTo(sum_of_parts_size)


    // The following facts are not grouped into a theory,
    // so that each test case can have a nicer name than what
    // would've been generated for the theory's test case.


    [<Fact>]
    member this.``UInt32.MaxValue => Virtual:0:0``() =
        Assert.That(TestSource.combined.Map(UInt32.MaxValue))
              .IsEqualTo({ FileName = "Virtual"; Line = 0u; Col = 0u })


    [<Fact>]
    member this.``0 => first.cool:1:1``() =
        Assert.That(TestSource.combined.Map(0u))
              .IsEqualTo({ FileName = "first.cool"; Line = 1u; Col = 1u })


    [<Fact>]
    member this.``53 => first.cool:2:27``() =
        Assert.That(TestSource.combined.Map(53u))
              .IsEqualTo({ FileName = "first.cool"; Line = 2u; Col = 27u })


    [<Fact>]
    member this.``54 => first.cool:3:1``() =
        Assert.That(TestSource.combined.Map(54u))
              .IsEqualTo({ FileName = "first.cool"; Line = 3u; Col = 1u })


    [<Fact>]
    member this.``239 => first.cool:12:1``() =
        Assert.That(TestSource.combined.Map(239u))
              .IsEqualTo({ FileName = "first.cool"; Line = 12u; Col = 1u })


    [<Fact>]
    member this.``240 => second.cool:12:2``() =
        Assert.That(TestSource.combined.Map(240u))
              .IsEqualTo({ FileName = "second.cool"; Line = 12u; Col = 2u })


    [<Fact>]
    member this.``277 => second.cool:14:3``() =
        Assert.That(TestSource.combined.Map(277u))
              .IsEqualTo({ FileName = "second.cool"; Line = 14u; Col = 3u })
