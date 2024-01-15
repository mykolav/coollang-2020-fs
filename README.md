# Compiler of Cool 2020 (a small Scala subset) into x86-64 assembly, in F#

[![Build status](https://ci.appveyor.com/api/projects/status/h5u412oqjd4ceh7j?svg=true)](https://ci.appveyor.com/project/mykolav/coollang-2020-fs)

> :snail:  
> Hey, check out [my blog posts](https://mykolam.net/tags/compilers/) on compilers in general and this one in particular.

The project is purely for fun and it's, honestly, just another toy compiler. Here's a couple things that might set it apart.

## The [language](#cool-2020) is concise but retains a degree of expressivity

A lot of mini-compilers have a language consisting of functions, primitive values and pretty much nothing else. Whereas [this project's language](#cool-2020) is a small Scala subset &mdash; it has classes, inheritance, virtual dispatch, and even a very simple form of pattern matching. Implementing all of these features yourself, making them function at the assembly level provides a lot of insights into how production-grade programming languages work.

## It is garbage-collected

The language's runtime library implements [a simple generational garbage collector](./src/Runtime/rt_gen_gc.s).

## It compiles down to x86-64 assembly

Then GNU `as` and `ld` come into play to build a native executable from the assembly. Many educational compilers emit MIPS assembly, and although it's possible to run it using an emulator, to me, running a native executable produced by your own compiler feels much more rewarding.

I also prefer emitting assembly instead of, for instance, C. It helps you understand the inner workings of various things that developers often take for granted:
- Managing a function's stack frame
- Calling conventions
- Finding the memory address of an identifier
- Converting expressions into assembly
- And so on, and so forth

High-level languages are a bit too... well, high-level for someone who wants to grasp how these things work under the hood. However, if you're more interested in designing a programming language than the nitty-gritty of compilers, targeting a high-level language like C might actually be a great idea.

### Human-readable assembly

The emitted assembly code contains a lot of hopefully useful comments. The idea here is to help understand the assembly and relate it back to the source code as much as possible. 

<details>
  <summary>Click to expand/collapse a sample assembly listing</summary>

```assembly
    .text

    # ../CoolPrograms/Runtime/Fibonacci.cool(1,7): Fib
    .global Fib..ctor
Fib..ctor:
    pushq   %rbp
    movq    %rsp, %rbp
    subq    $64, %rsp
    # store actuals on the stack
    movq    %rdi, -8(%rbp)
    # store callee-saved regs on the stack
    movq    %rbx, -24(%rbp)
    movq    %r12, -32(%rbp)
    movq    %r13, -40(%rbp)
    movq    %r14, -48(%rbp)
    movq    %r15, -56(%rbp)
    # actual #0
    movq    -8(%rbp), %rbx
    subq    $8, %rsp
    movq    %rbx, 0(%rsp)                         # actual #0
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    # remove the register-loaded actuals from stack
    addq    $8, %rsp
    call    IO..ctor                              # super..ctor
    movq    %rax, %rbx                            # returned value
    # ../CoolPrograms/Runtime/Fibonacci.cool(8,18): 0
    movq    $INT_0, %rbx
    # ../CoolPrograms/Runtime/Fibonacci.cool(8,5): var i: Int = 0
    movq    %rbx, -16(%rbp)                       # i
.L6_WHILE_COND:
    # ../CoolPrograms/Runtime/Fibonacci.cool(9,12): i
    movq    -16(%rbp), %r10                       # i
    # ../CoolPrograms/Runtime/Fibonacci.cool(9,17): 10
    movq    $INT_10, %r11
```

</details>

## It is cross-platform

The two supported hosts are x86-64 Windows and Linux. And in general, the project is based on cross-platform technologies. The compiler itself runs on .NET. The GNU tools `as` and `ld` are available on Linux and Windows (and many, many other, of course). The language's runtime is implemented in assembly and is split up into three parts: common code, Windows-specific bits, and Linux-specific bits that are responsible for calling Windows API and Linux system functions respectively.

## The test suite

Approximately 288 automated tests make up [the test suite](https://github.com/mykolav/coollang-2020-fs/tree/master/src/Tests). One nice consequence of generating native executables is an automated test can compile a program, run it, and compare the program's output to the expected values. Out of the 288 tests, there are [39 that do exactly that](https://github.com/mykolav/coollang-2020-fs/tree/master/src/Tests/CoolPrograms/Runtime).

## Maybe the only hobby-compiler that has a demo video :)

![A sample compilation session](./compilation-session-demo.gif)

## There's a story behind it

This [page](https://mykolav.github.io/coollang-2020-fs/) tries to give a bit of background on the project and describe it in more detail. 

## Contents

- [Cool 2020](#cool-2020)
  - [CoolAid: The Cool 2020 Reference Manual](#coolaid-the-cool-2020-reference-manual)
  - [An antlr4 grammar for Cool 2020](#an-antlr4-grammar-for-cool-2020)
  - [Precedence](#precedence)
- [Work in progress](#work-in-progress)
- [Build](#build)
  - [Install .NET 8 SDK](#install-net-8-sdk)
    - [_Windows_](#windows)
    - [_Linux_](#linux)
    - [Check installation](#check-installation)
  - [Install GNU Binutils](#install-gnu-binutils)
    - [_Windows_](#windows-1)
      - [Troubleshooting **... relocation truncated to fit: R\_X86\_64\_32S ...**](#troubleshooting--relocation-truncated-to-fit-r_x86_64_32s-)
    - [_Linux_](#linux-1)
  - [Build the compiler](#build-the-compiler)
- [Compiler usage](#compiler-usage)
  - [Synopsis](#synopsis)
  - [Examples](#examples)
    - [Test programs](#test-programs)
- [Implementation remarks](#implementation-remarks)
- [Useful links](#useful-links)
- [Credits](#credits)
- [License](#license)

---

# Cool 2020

[Cool 2020](https://web.archive.org/web/20210823043833/http://www.cs.uwm.edu/classes/cs654/handout/cool-manual.pdf) is a subset of Scala with minor incompatibilities. Let's first have a taste of the language and then move on to discussing the language in more details.

```scala
class Fib() extends IO() {
  def fib(x: Int): Int =
    if (x == 0) 0
    else if (x == 1) 1
    else fib(x - 2) + fib(x - 1);

  {
    var i: Int = 0;
    while (i <= 10) {
      out_string("fib("); out_int(i); out_string(") = ");
      out_int(fib(i));
      out_nl();
      
      i = i + 1
    }
  };
}

class Main() {
  { new Fib() };
}
```

Too see more of the language's features in action take a look at [Life.cool](./src/Tests/CoolPrograms/Runtime/Life.cool), [QuickSort.cool](./src/Tests/CoolPrograms/Runtime/QuickSort.cool), and [InsertionSort.cool](./src/Tests/CoolPrograms/Runtime/InsertionSort.cool).

## [CoolAid: The Cool 2020 Reference Manual](https://web.archive.org/web/20210823043833/http://www.cs.uwm.edu/classes/cs654/handout/cool-manual.pdf)

> ... the Classroom Object-Oriented Language ... retains many of
the features of modern programming languages including objects, static typing, and automatic memory
management...  

> Cool programs are sets of classes. A class encapsulates the variables and procedures of a data type.
Instances of a class are objects. In Cool, classes and types are identified; i.e., every class defines a type.
Classes permit programmers to define new types and associated procedures (or methods) specific to those
types. Inheritance allows new types to extend the behavior of existing types.  

> Cool is an expression language. Most Cool constructs are expressions, and every expression has a
value and a type. Cool is type safe: procedures are guaranteed to be applied to data of the correct type.
While static typing imposes a strong discipline on programming in Cool, it guarantees that no runtime
type errors can arise in the execution of Cool programs.

## An antlr4 grammar for Cool 2020

<details>
  <summary>Click to expand/collapse</summary>
  
(This grammar ignores the precedence of operations.)

``` ANTLR
grammar Cool_2020;

program 
    : classdecl+
    ;

classdecl
    : 'class' ID varformals ('extends' ID actuals)? classbody
    ;

varformals
    : '(' (varformal (',' varformal)*)? ')'
    ;

varformal
    : 'var' ID ':' ID
    ;

classbody
    : '{' (feature ';')* '}'
    ;

feature
    : 'override'? 'def' ID formals ':' ID '=' expr
    | 'var' ID ':' ID '=' expr
    | '{' block? '}'
    ;

formals
    : '(' (formal (',' formal)*)? ')'
    ;

formal
    : ID ':' ID
    ;

actuals
    : '(' (expr (',' expr)*)? ')'
    ;

block
    : (('var' ID ':' ID '=')? expr ';')* expr
    ;

// The expression's syntax is split in `expr`, `assign_or_prefixop`, `primary`, and `infixop_rhs` to avoid left recursion.
expr
    : prefix* primary infixop_rhs*
    ;

prefix
    : ID '='
    | '!'
    | '-'
    | 'if' '(' expr ')' expr 'else'
    | 'while' '(' expr ')'
    ;


primary
    : ('super' '.')? ID actuals
    | 'new' ID actuals
    | '{' block? '}'
    | '(' expr ')'
    | 'null'
    | '(' ')'
    | ID
    | INTEGER
    | STRING
    | BOOLEAN
    | 'this'
    ;

infixop_rhs
    : ('<=' | '<' | '>=' | '>' | '==' | '!=' | '*' | '/' | '+' | '-') expr
    | 'match' cases
    | '.' ID actuals
    ;

cases
    : '{' ('case' casepattern '=>' caseblock)+ '}'
    ;

casepattern
    : ID ':' ID
    | 'null'
    ;

caseblock
    : block
    | '{' block? '}'
    ;


ID
    : [a-zA-Z$_][a-zA-Z0-9$_]*
    ;

INTEGER
    : [0-9]+
    ;

STRING
    : '"' (~["\\] | '\\' [0btnrf"\\])*? '"'
    | '"""' .*? '"""'
    ;

BOOLEAN
    : 'true'
    | 'false'
    ;

BLOCK_COMMENT 
    : '/*' .*? '*/' -> skip
    ;

LINE_COMMENT 
    : '//' .*? ('\r\n' | '\r' | '\n') -> skip
    ;

WS
    : [ \r\n\t]+ -> skip
    ;
```

</details>

## Precedence

<details>
  <summary>Click to expand/collapse</summary>

The precedence of operations is given below from highest to lowest (where -num denotes unary minus).  
Keep in mind, `match`, `if`, `while` are expressions in Cool 2020.

```
.
!  -num
*  /
+  -
== !=
<= >=
<  >
match
if while
=
```

</details>

# Work in progress

The compiler can successfully build a Windows x64 or Linux x64 executable out of every [sample program](./src/Tests/CoolPrograms/Runtime).

## TODO

Introduce the concept of a companion object limited to objects predefined by the runtime. In particular, it will allow a Cool 2020 program to invoke GC related methods like this:

```scala
GC.collect(-1);
GC.print_state()
```

Instead of the current approach:

```scala
GC_collect(-1);
GC_print_state()
```

# Build

## Install .NET 8 SDK

The compiler is written in F#. F# is a .NET language. Rather predictably, a .NET SDK is a dependency.

Get it from [the download page](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).  

### _Windows_

Download and run an SDK installer.  
Keep in mind:

> If you're using Visual Studio, look for the SDK that supports the version you're using.  
> If you're not using Visual Studio, install the first SDK listed.

### _Linux_

Follow [these instructions](https://docs.microsoft.com/en-us/dotnet/core/install/linux).  

Just to give an example. On Ubuntu 22.04 installation looks something like this:

```sh
# Install the SDK.
# (If you install the .NET SDK, you don't need to install the corresponding runtime.)
sudo apt-get update && \
sudo apt-get install -y dotnet-sdk-8.0
```

### Check installation

On both Windows and Linux, to check your installation, do the following command. If everything is OK, the output will contain one or many 8.0.xyz versions.

```sh
dotnet --list-sdks
```

## Install GNU Binutils

The compiler emits x86-64 assembly. It uses `as` to assemble it. And `ld` to link with the runtime.

### _Windows_

You can get binutils as part of [MSYS2](https://www.msys2.org/), which provides up-to-date native builds of GNU Binutils, Mingw-w64, etc.
Follow the steps outlined on [this page](https://code.visualstudio.com/docs/cpp/config-mingw#_prerequisites) starting from the step #3.

<details>
  <summary>Click to expand/collapse the installation steps</summary>

1. Get the latest version of Mingw-w64 via [MSYS2](https://www.msys2.org/). You can download the latest installer from the MSYS2 page.
2. Follow the Installation instructions on the [MSYS2 website](https://www.msys2.org/) to install Mingw-w64. 
Take care to run each required Start menu and pacman command.
3. Install the Mingw-w64 toolchain (`pacman -S --needed base-devel mingw-w64-x86_64-toolchain`). 
Run the pacman command in a MSYS2 MINGW64 terminal (you'll find it in the Start menu). 
Accept the default to install all the members in the toolchain group.
4. Add the path to your Mingw-w64 bin folder to the Windows `PATH` environment variable by using the following steps:
    1. In the Windows search bar, type 'settings' to open your Windows Settings.
    2. Search for Edit environment variables for your account.
    3. Choose the Path variable in your User variables and then select Edit.
    4. Select New and add the Mingw-w64 destination folder path to the system path.
    The exact path depends on which version of Mingw-w64 you have installed and where you installed it. 
    If you used the settings above to install Mingw-w64, then add this to the path: `C:\msys64\mingw64\bin`.
    5. Select OK to save the updated `PATH`. You will need to reopen any console windows for the new PATH location to be available
</details>
   

#### Troubleshooting **... relocation truncated to fit: R_X86_64_32S ...**
In the case you use MSYS2 to install their MinGW packages, please keep in mind the following. The compiler links an executable using a command similar to

```sh
ld -o a.exe -e main a.o ../../src/Runtime/rt_common.o ../../src/Runtime/rt_windows.o -L"C:/msys64/mingw64/x86_64-w64-mingw32/lib" -lkernel32
```

Starting at least from GNU Binutils 2.36.1 (and maybe an earlier version), the command above produces an error along these lines:

```
a.o:fake:(.text+0x3f): relocation truncated to fit: R_X86_64_32S against symbol `IO_proto_obj' defined in .data section in ../../src/Runtime/rt_common.o
a.o:fake:(.text+0x7c): relocation truncated to fit: R_X86_64_32S against `.data'
a.o:fake:(.text+0x9e): relocation truncated to fit: R_X86_64_32S against `.data'
a.o:fake:(.text+0xd2): relocation truncated to fit: R_X86_64_32S against `.data'
../../src/Runtime/rt_common.o:fake:(.text+0x8): relocation truncated to fit: R_X86_64_32S against `.data'
../../src/Runtime/rt_common.o:fake:(.text+0x36): relocation truncated to fit: R_X86_64_32S against `.data'
../../src/Runtime/rt_common.o:fake:(.text+0x6d): relocation truncated to fit: R_X86_64_32S against `.data'
../../src/Runtime/rt_common.o:fake:(.text+0x89): relocation truncated to fit: R_X86_64_32S against `.data'
../../src/Runtime/rt_common.o:fake:(.text+0xbb): relocation truncated to fit: R_X86_64_32S against `.data'
../../src/Runtime/rt_common.o:fake:(.text+0xdb): relocation truncated to fit: R_X86_64_32S against `.data'
../../src/Runtime/rt_common.o:fake:(.text+0xf8): additional relocation overflows omitted from the output
```

A [news report](https://www.msys2.org/news/#2021-01-31-aslr-enabled-by-default) on the MSYS2 page and an [ld bug report](https://sourceware.org/bugzilla/show_bug.cgi?id=26659) seem to discuss a very similar issue though not exactly the same.  

I don't really understand what causes the issue with linking object files produced from compiler-generated assembly. But a workaround suggested on the MSYS2 page solves it. The workaround is to pass `--default-image-base-low` to ld.   

The compiler's code includes this flag into the linker command line to work around the problem. If you still encounter similar error messages, you might try downgrading GNU Binutils to 2.34 to make the problem go away.

```sh
pacman -U http://repo.msys2.org/mingw/x86_64/mingw-w64-x86_64-binutils-2.34-3-any.pkg.tar.zst
```

### _Linux_

Install your distribution's binutils package. For example, on Ubuntu it looks something like this:

```sh
sudo apt install binutils
```

## Build the compiler

On Windows, use Bash to perform these commands. As a git user, you likely have Git Bash installed and know how to use it. If not, take a look at [this nice tutorial](https://www.atlassian.com/git/tutorials/git-bash).

```sh
# Clone the repo.
git clone https://github.com/mykolav/coollang-2020-fs.git
cd coollang-2020-fs/

# Build F# code.
# And assemble the runtime's code.
./build.sh
```

<details>
  <summary>A successful build should look similar to the following.</summary>

```
Building F#

Microsoft (R) Build Engine version 16.8.0+126527ff1 for .NET
Copyright (C) Microsoft Corporation. All rights reserved.

  Determining projects to restore...
  Restored /home/appveyor/projects/coollang-2020-fs/src/clc/clc.fsproj (in 5.41 sec).
  Restored /home/appveyor/projects/coollang-2020-fs/src/LibCool/LibCool.fsproj (in 20 ms).
  Restored /home/appveyor/projects/coollang-2020-fs/src/Tests/Tests.fsproj (in 10.66 sec).
  LibCool -> /home/appveyor/projects/coollang-2020-fs/src/LibCool/bin/Debug/netstandard2.1/LibCool.dll
  clc -> /home/appveyor/projects/coollang-2020-fs/src/clc/bin/Debug/net6.0/clc.dll
  Tests -> /home/appveyor/projects/coollang-2020-fs/src/Tests/bin/Debug/net6.0/Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:50.56

Assembling the runtime

Done

```
</details>

# Compiler usage

On Windows, use Bash to follow the examples.  
One option is using Git Bash. To get more details, take a look at [this nice tutorial](https://www.atlassian.com/git/tutorials/git-bash).

Change directory to `sandbox/clc`.  
The folder contains a bash script `clc`. It's a wrapper that invokes the compiler and passes through command line arguments.

## Synopsis

`clc file1.cool [file2.cool, ..., fileN.cool] [-o file.exe | -S [file.asm]]`

## Examples

To compile and run QuickSort.cool, follow these steps.

```sh
# Compile
$ ./clc ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool -o qs
Build succeeded: Errors: 0. Warnings: 0

# Run
$ ./qs
30 20 50 40 10
10 20 30 40 50
```

To see the assembly the compiler emits for QuickSort.cool, perform the command below. Then open qs.s in your favorite editor. 

```
$ ./clc ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool -S qs.s
Build succeeded: Errors: 0. Warnings: 0
```

### Test programs

Take a look inside [src/Tests/CoolPrograms/Runtime](./src/Tests/CoolPrograms/Runtime) for more test programs. In particular, relatively bigger programs it contains are [Life.cool](./src/Tests/CoolPrograms/Runtime/Life.cool), [InsertionSort.cool](./src/Tests/CoolPrograms/Runtime/InsertionSort.cool), [QuickSort.cool](./src/Tests/CoolPrograms/Runtime/QuickSort.cool), and [Fibonacci.cool](./src/Tests/CoolPrograms/Runtime/Fibonacci.cool).

# Implementation remarks

The implementation language is F#, but the code is imperative/OO.
That said, the code is, hopefully, consistent with the recommendations from ["F# Code I Love" by Don Syme](https://www.youtube.com/watch?v=1AZA1zoP-II).

Initially it was my ambition to practice writing functional code while learning about compilers. But trying to do both at the same time was biting off more than I could chew.

Used as an imperative/OO language, F# has many and many nice features. More mainstream languages started to catch up only recently. E.g., no null values by default, records, discriminated unions, pattern matching, primary constructors, etc.

Plus, F# is a low ceremony, low syntactic noise language. In this respect, you can think of F# as Python but statically typed.

 The code contains a notable deviation from [F# formatting guidelines](https://docs.microsoft.com/en-us/dotnet/fsharp/style-guide/formatting). The guidelines recommend camelCase naming for all `let` bindings. But I use camelCase for function bindings and snake-case for value bindings. This way function and value binding are easier to tell apart at a glance.

# Useful links

- [Douglas Thain, Introduction to Compilers and Language Design](https://www3.nd.edu/~dthain/compilerbook/)
- [Bob Nystrom, Crafting Interpreters](https://craftinginterpreters.com/)
- [Alex Aiken, Compilers](https://www.edx.org/course/compilers)
- [Eli Bendersky, Parsing expressions by precedence climbing](https://eli.thegreenplace.net/2012/08/02/parsing-expressions-by-precedence-climbing)
- [Gabrijel Boduljak, Coursework project for the Stanford Compilers MOOC course](https://github.com/gboduljak/stanford-compilers-coursework/tree/master/examples)
- [Arek Holko, MIPS assembler files generated by my COOL compiler](https://github.com/fastred/cool-compiler-examples)
- [Kuang Han, Dan Deng, Ang Li, Compiler-SML An implementation of a compiler for the Tiger Language](https://github.com/kh156/Tiger-Compiler-SML)
- [Guide to x86-64](http://web.stanford.edu/class/cs107/guide/x86-64.html)
- [Siew Yi Liang, Understanding Windows x64 Assembly](https://sonictk.github.io/asm_tutorial/)
- [System V ABI](https://wiki.osdev.org/System_V_ABI)
- Lots and lots of other blog posts and github repos

# Credits

The original Cool language was designed by [Alex Aiken](https://theory.stanford.edu/~aiken/).  
 
The Cool 2020 version was designed by [John Boyland](https://uwm.edu/engineering/people/boyland-ph-d-john/).  
 
QuickSort.cool and InsertionSort.cool came from [a paper](https://www.lume.ufrgs.br/bitstream/handle/10183/151038/001009883.pdf)
from [LUME - the Digital Repository of the Universidade Federal do Rio Grande do Sul](https://www.lume.ufrgs.br/apresentacao).  

The web page uses [GitHub Corners](https://github.com/tholman/github-corners) by [Tim Holman](https://github.com/tholman).

# License

[This project](https://github.com/mykolav/coollang-2020-fs) is licensed under the MIT license. See [LICENSE](./LICENSE) for details.

[GitHub Corners](https://github.com/tholman/github-corners) is licensed under the MIT license. See [LICENSE.github-corners](./LICENSE.github-corners) for details.
