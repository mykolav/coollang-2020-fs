namespace LibCool.Tests.Parser

module CoolSnippets =
    [<Literal>]
    let Fib =
      "class Fib() extends IO() {\n\
         def fib(x: Int): Int =\n\
           if (x == 0) 0\n\
           else if (x == 1) 1\n\
           else fib(x - 2) + fib(x - 1);\n\
       \n\
         {\n\
           out_string(\"fib(10) = \");\n\
           out_int(fib(10));\n\
           out_nl()\n\
         };\n\
       }\n\
       class Main() {\n\
         { new Fib() };\n\
       }\n\
      "

    
    [<Literal>]
    let FibRendered =
      "class Fib() extends IO() {\n\
         def fib(x: Int): Int =\n\
           if (x == 0) 0\n\
           else if (x == 1) 1\n\
           else fib(x - 2) + fib(x - 1);\n\
       \n\
         {\n\
           out_string(\"fib(10) = \");\n\
           out_int(fib(10));\n\
           out_nl()\n\
         };\n\
       }\n\
       class Main() {\n\
         { new Fib() };\n\
       }\n\
      "


    // The source of this snippet is [a paper](https://www.lume.ufrgs.br/bitstream/handle/10183/151038/001009883.pdf)
    // from [LUME - the Digital Repository of the Universidade Federal do Rio Grande do Sul](https://www.lume.ufrgs.br/apresentacao)
    [<Literal>]
    let QuickSort =
      "class QuickSort() extends IO() {\n\
         def quicksort(array: ArrayAny, lo: Int, hi: Int): Unit = {\n\
           if (lo < hi) {\n\
             var p: Int = partition(array, lo, hi);\n\
             quicksort(array, lo, p - 1);\n\
             quicksort(array, p + 1, hi)\n\
           } else ()\n\
         };\n\
         \n\
         def partition(array: ArrayAny, lo: Int, hi: Int): Int = {\n\
           var pivot: Int = array.get(lo) match { case i: Int => i };\n\
           var p: Int = lo;\n\
           var i: Int = lo + 1;\n\
           while (i <= hi) {\n\
             if (((array.get(i)) match { case i: Int => i }) <= pivot)\n\
               array_swap(array, i, { p = p + 1; p })\n\
             else\n\
               ();\n\
             i = i + 1\n\
           };\n\
           \n\
           array_swap(array, p, lo);\n\
           p\n\
         };\n\
         \n\
         def array_swap(array: ArrayAny, p: Int, q: Int): Unit = {\n\
           var tmp: Any = array.get(p);\n\
           array.set(p, array.get(q));\n\
           array.set(q, tmp)\n\
         };\n\
         \n\
         def out_array(array: ArrayAny): Unit = {\n\
           var i: Int = 0;\n\
           while (i < array.length()) {\n\
             array.get(i) match {\n\
               case i: Int => out_int(i); out_string(\" \")\n\
             };\n\
             \n\
             i = i + 1\n\
           };\n\
           \n\
           out_nl()\n\
         };\n\
         \n\
         {\n\
           var array: ArrayAny = new ArrayAny(5);\n\
           array.set(0, 30);\n\
           array.set(1, 20);\n\
           array.set(2, 50);\n\
           array.set(3, 40);\n\
           array.set(4, 10);\n\
           \n\
           out_array(array);\n\
           \n\
           quicksort(array, 0, array.length() - 1);\n\
           \n\
           out_array(array)\n\
         };\n\
       }\n\
       \n\
       class Main() {\n\
         {\n\
           new QuickSort()\n\
         };\n\
       }\n
      "
    
    
    [<Literal>]
    let QuickSortRendered =
      "class QuickSort() extends IO() {\n\
         def quicksort(array: ArrayAny, lo: Int, hi: Int): Unit = {\n\
           if (lo < hi) {\n\
             var p: Int = partition(array, lo, hi);\n\
             quicksort(array, lo, p - 1);\n\
             quicksort(array, p + 1, hi)\n\
           } else ()\n\
         };\n\
         \n\
         def partition(array: ArrayAny, lo: Int, hi: Int): Int = {\n\
           var pivot: Int = array.get(lo) match { case i: Int => i };\n\
           var p: Int = lo;\n\
           var i: Int = lo + 1;\n\
           while (i <= hi) {\n\
             if (((array.get(i)) match { case i: Int => i }) <= pivot)\n\
               array_swap(array, i, { p = p + 1; p })\n\
             else\n\
               ();\n\
             i = i + 1\n\
           };\n\
           \n\
           array_swap(array, p, lo);\n\
           p\n\
         };\n\
         \n\
         def array_swap(array: ArrayAny, p: Int, q: Int): Unit = {\n\
           var tmp: Any = array.get(p);\n\
           array.set(p, array.get(q));\n\
           array.set(q, tmp)\n\
         };\n\
         \n\
         def out_array(array: ArrayAny): Unit = {\n\
           var i: Int = 0;\n\
           while (i < array.length()) {\n\
             array.get(i) match {\n\
               case i: Int => out_int(i); out_string(\" \")\n\
             };\n\
             \n\
             i = i + 1\n\
           };\n\
           \n\
           out_nl()\n\
         };\n\
         \n\
         {\n\
           var array: ArrayAny = new ArrayAny(5);\n\
           array.set(0, 30);\n\
           array.set(1, 20);\n\
           array.set(2, 50);\n\
           array.set(3, 40);\n\
           array.set(4, 10);\n\
           \n\
           out_array(array);\n\
           \n\
           quicksort(array, 0, array.length() - 1);\n\
           \n\
           out_array(array)\n\
         };\n\
       }\n\
       \n\
       class Main() {\n\
         {\n\
           new QuickSort()\n\
         };\n\
       }\n
      "
