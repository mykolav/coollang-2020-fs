namespace LibCool.Tests.Parser

module CoolSnippets =
    [<Literal>]
    let Fib =
      "class Fib() extends IO() {\n" +
      "  def fib(x: Int): Int =\n" +
      "    if (x == 0) 0\n" +
      "    else if (x == 1) 1\n" +
      "    else fib(x - 2) + fib(x - 1);\n" +
      "\n" +
      "  {\n" +
      "    out_string(\"fib(10) = \");\n" +
      "    out_int(fib(10));\n" +
      "    out_nl()\n" +
      "  };\n" +
      "}\n" +
      "class Main() {\n" +
      "  { new Fib() };\n" +
      "}\n"


    // The source of this snippet is [a paper](https://www.lume.ufrgs.br/bitstream/handle/10183/151038/001009883.pdf)
    // from [LUME - the Digital Repository of the Universidade Federal do Rio Grande do Sul](https://www.lume.ufrgs.br/apresentacao)
    [<Literal>]
    let QuickSort =
      "class QuickSort() extends IO() {\r\n" +
      "  def quicksort(array: ArrayAny, lo: Int, hi: Int): Unit = {\r\n" +
      "    if (lo < hi) {\r\n" +
      "      var p: Int = partition(array, lo, hi);\r\n" +
      "      quicksort(array, lo, p - 1);\r\n" +
      "      quicksort(array, p + 1, hi)\r\n" +
      "    } else ()\r\n" +
      "  };\r\n" +
      "  \r\n" +
      "  def partition(array: ArrayAny, lo: Int, hi: Int): Int = {\r\n" +
      "    var pivot: Int = array.get(lo) match { case i: Int => i };\r\n" +
      "    var p: Int = lo;\r\n" +
      "    var i: Int = lo + 1;\r\n" +
      "    while (i <= hi) {\r\n" +
      "      if (((array.get(i)) match { case i: Int => i }) <= pivot)\r\n" +
      "        array_swap(array, i, { p = p + 1; p })\r\n" +
      "      else\r\n" +
      "        ();\r\n" +
      "      i = i + 1\r\n" +
      "    };\r\n" +
      "    \r\n" +
      "    array_swap(array, p, lo);\r\n" +
      "    p\r\n" +
      "  };\r\n" +
      "  \r\n" +
      "  def array_swap(array: ArrayAny, p: Int, q: Int): Unit = {\r\n" +
      "    var tmp: Any = array.get(p);\r\n" +
      "    array.set(p, array.get(q));\r\n" +
      "    array.set(q, tmp)\r\n" +
      "  };\r\n" +
      "  \r\n" +
      "  def out_array(array: ArrayAny): Unit = {\r\n" +
      "    var i: Int = 0;\r\n" +
      "    while (i < array.length()) {\r\n" +
      "      array.get(i) match {\r\n" +
      "        case i: Int => out_int(i); out_string(\" \")\r\n" +
      "      };\r\n" +
      "      \r\n" +
      "      i = i + 1\r\n" +
      "    };\r\n" +
      "    \r\n" +
      "    out_nl()\r\n" +
      "  };\r\n" +
      "  \r\n" +
      "  {\r\n" +
      "    var array: ArrayAny = new ArrayAny(5);\r\n" +
      "    array.set(0, 30);\r\n" +
      "    array.set(1, 20);\r\n" +
      "    array.set(2, 50);\r\n" +
      "    array.set(3, 40);\r\n" +
      "    array.set(4, 10);\r\n" +
      "    \r\n" +
      "    out_array(array);\r\n" +
      "    \r\n" +
      "    quicksort(array, 0, array.length() - 1);\r\n" +
      "    \r\n" +
      "    out_array(array)\r\n" +
      "  };\r\n" +
      "}\r\n" +
      "\r\n" +
      "class Main() {\r\n" +
      "  {\r\n" +
      "    new QuickSort()\r\n" +
      "  };\r\n" +
      "}"
