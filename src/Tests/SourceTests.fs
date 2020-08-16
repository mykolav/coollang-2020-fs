namespace Tests

module SourceTestFixture =

    [<Literal>]
    let first_cool =
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

    [<Literal>]
    let second_cool =
        "class Main() {\r\n" +
        "    { new Fib() };\n" +
        "}\r\n"


open Xunit
open SourceTestFixture
open LibCool.SourceParts

type SourceTests() =
    let translates_to location (offset, snippets: (string * string) list) = 
        // Arrange
        let parts = snippets
                    |> List.map (fun (name, content) ->
                                    { FileName = (sprintf "%s.cool" name) 
                                      Content  = content })
        let source = Source(parts)

        // Act
        let actual = source.Map offset

        // Assert
        Assert.Equal(expected=location, actual=actual)

    [<Fact>]
    member this.``Sum of the part sizes = the source size``() =
        // Arrange
        let parts = [ { FileName = "first.cool"; Content = first_cool }
                      { FileName = "second.cool"; Content = second_cool } ]
        let expected_size = parts |> List.sumBy (fun it -> it.Content.Length) |> uint32

        // Act
        let source = Source(parts)

        // Assert
        Assert.Equal(expected = expected_size, actual = source.Size)

    [<Fact>]
    member this.``0 => first.cool:1:1``() =
        (0u, [("first", first_cool)
              ("second", second_cool)]) 
        |> translates_to { FileName = "first.cool"; Line = 1u; Col = 1u }

    [<Fact>]
    member this.``53 => first.cool:2:27``() =
        (53u, [("first", first_cool)
               ("second", second_cool)]) 
        |> translates_to { FileName = "first.cool"; Line = 2u; Col = 27u }

    [<Fact>]
    member this.``54 => first.cool:3:1``() =
        (54u, [("first", first_cool)
               ("second", second_cool)]) 
        |> translates_to { FileName = "first.cool"; Line = 3u; Col = 1u }

    [<Fact>]
    member this.``239 => first.cool:12:1``() =
        (239u, [("first", first_cool)
                ("second", second_cool)]) 
        |> translates_to { FileName = "first.cool"; Line = 12u; Col = 1u }

    [<Fact>]
    member this.``240 => second.cool:12:2``() =
        (240u, [("first", first_cool)
                ("second", second_cool)]) 
        |> translates_to { FileName = "second.cool"; Line = 12u; Col = 2u }

    [<Fact>]
    member this.``277 => second.cool:14:3``() =
        (277u, [("first", first_cool)
                ("second", second_cool)]) 
        |> translates_to { FileName = "second.cool"; Line = 14u; Col = 3u }
