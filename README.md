# The Cool 2020 language compiler implemented in F#

[![Build status](https://ci.appveyor.com/api/projects/status/h5u412oqjd4ceh7j?svg=true)](https://ci.appveyor.com/project/mykolav/coollang-2020-fs)

# Cool 2020

Cool 2020 is a subset of Scala with minor incompatibilities.

We compile Cool 2020 programs to native x64 code. The compiler runs on Windows and Linux.

[CoolAid: The Cool 2020 Reference Manual](http://pabst.cs.uwm.edu/classes/cs654/handout/cool-manual.pdf)

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

## A code sample

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

Too see more of the language's features in action take a look at [QuickSort.cool](./src/Tests/CoolPrograms/Runtime/QuickSort.cool).

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

// The expresson's syntax is split in `expr`, `assign_or_prefixop`, `primary`, and `infixop_rhs` to avoid left recursion.
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
    : ('<=' | '<' | '>=' | '>' | '==' | '*' | '/' | '+' | '-') expr
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

## Precendence

<details>
  <summary>Click to expand/collapse</summary>

The precedence of operations is given below from highest to lowest (where -num denotes unary minus).  
Keep in mind, `match`, `if`, `while` are expressions in Cool 2020.

```
.
!  -num
*  /
+  -
==
<= >=
<  >
match
if while
=
```

</details>

# Work in progress

The compiler can successfully build a Windows x64 or Linux x64 excecutable out of every [sample program](./src/Tests/CoolPrograms/Runtime).

Generational garbage collection is planned for some time in the future, but doesn't exist at the moment.
As a result we never free allocated memory.

# Build

## Dependencies

### .NET Core 3.1 SDK

The compiler is written in F#. F# is a .NET language. Rather predictably, a .NET SDK is a dependency.

Get it from [the download page](https://dotnet.microsoft.com/download/dotnet-core/3.1).  

#### _Windows_

Download and run an SDK installer.  
Keep in mind:

> If you're using Visual Studio, look for the SDK that supports the version you're using.  
> If you're not using Visual Studio, install the first SDK listed.

#### _Linux_

Follow [these instructions](https://docs.microsoft.com/en-us/dotnet/core/install/linux).  

Just to give an example. On Ubuntu installation looks something like this:

```sh
# Add the Microsoft package signing key to your list of trusted keys.
# And add the package repository.
wget https://packages.microsoft.com/config/ubuntu/20.10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

# Install the SDK.
sudo apt-get update; \
sudo apt-get install -y apt-transport-https && \
sudo apt-get update && \
sudo apt-get install -y dotnet-sdk-3.1
```

### Check installation

On both Windows and Linux, to check your installation, do the following command. If everything is OK, the output will contain one or many 3.1.xyz versions.

```sh
dotnet --list-sdks
```

### GNU Binutils

The compiler emits x86-64 assembly. It uses `as` to assemble it. And `ld` to link with the runtime.

#### _Windows_

One way of getting binutils is installing MinGW. MinGW is a project providing Windows versions of GCC, GDB, binutils, and some other tools.  
[This page](https://code.visualstudio.com/docs/cpp/config-mingw#_prerequisites) explains how to install MinGW (see item #3).

<details>
  <summary>Click to expand/collapse the installation steps</summary>

1. Download and run Download [the Windows Mingw-w64 installer](https://sourceforge.net/projects/mingw-w64/files/Toolchains%20targetting%20Win32/Personal%20Builds/mingw-builds/installer/mingw-w64-install.exe/download).
2. For **Architecture** select **x86_64** and then select **Next**.
3. **Next** again to use the default installation folder and install MinGW.
4. Add the path to your Mingw-w64 bin folder to the Windows PATH environment variable by using the following steps:  
    1. In the Windows search bar, type 'settings' to open your Windows Settings.
    2. Search for **Edit environment variables for your account**.
    3. Choose the `PATH` variable and then select **Edit**.
    4. Select **New** and add the Mingw-w64 path to the system path. The exact path depends on which version of Mingw-w64 you have installed and where you installed it. If you used the settings above to install Mingw-w64, then add this to the path: `C:\Program Files\mingw-w64\x86_64-8.1.0-posix-seh-rt_v6-rev0\mingw64\bin`.
    5. Select **OK** to save the updated `PATH`. You will need to reopen any console windows for the new `PATH` location to be available

</details>

### _Linux_

Install your distribution's binutils package. For example, on Ubuntu it looks something like this:

```sh
sudo apt install binutils
```

## Commands

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
  clc -> /home/appveyor/projects/coollang-2020-fs/src/clc/bin/Debug/netcoreapp3.1/clc.dll
  Tests -> /home/appveyor/projects/coollang-2020-fs/src/Tests/bin/Debug/netcoreapp3.1/Tests.dll

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

`cd` to `sandbox/clc`.  
The folder contains a bash script `clc`. It's a wrapper that invokes the compiler and passes through command line arguments.

## SYNOPSIS

`clc file1.cool [file2.cool, ..., fileN.cool] [-o file.exe | -S [file.asm]]`

## Examples

To compile and run QuickSort.cool follow these steps.

```sh
# Compile
$ ./clc ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool -o qs
Build succeeded: Errors: 0. Warnings: 0

# Run
$ ./qs
30 20 50 40 10
10 20 30 40 50
```

To see assembbly that the compiler emits for QuickSort.cool perform the command below. Then open qs.s in your favourite editor. 

```
$ ./clc ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool -S qs.s
Build succeeded: Errors: 0. Warnings: 0
```

### Test programs

Take a look inside [src/Tests/CoolPrograms/Runtime](./src/Tests/CoolPrograms/Runtime) for more test programs. In particular, relatively bigger programs it contains are [Fibonacci.cool](./src/Tests/CoolPrograms/Runtime/Fibonacci.cool), and [InsertionSort.cool](./src/Tests/CoolPrograms/Runtime/InsertionSort.cool).

# Implementation remarks

The implementation language is F#, but the code is imperative/OO.
That said, the code is, hopefully, consistent with the recommendations from ["F# Code I Love" by Don Syme](https://www.youtube.com/watch?v=1AZA1zoP-II).

Initially it was my ambition to practice writing functional code while learning about compilers. But trying to do both at the same time was biting off more than I could chew.

Used as an imperative/OO language, F# has many and many nice features. More mainstream languages started to catch up only recently. E.g., no null values by default, records, discriminated unions, pattern matching, primary constructors, etc.

Plus, F# is a low ceremony, low syntactic noise language. In this respect, you can think of F# as Python but statically typed.

 The code contains a notable deviation from [F# formatting guidelines](https://docs.microsoft.com/en-us/dotnet/fsharp/style-guide/formatting). The guidelines recommend camelCase naming for `let` bindings. But I use snake-case naming in `let` bindingis. It's just a personal preference for a hobby project.

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
 
 QuickSort.cool and InsertionSort.cool came from [a papaer](https://www.lume.ufrgs.br/bitstream/handle/10183/151038/001009883.pdf)
 from [LUME - the Digital Repository of the Universidade Federal do Rio Grande do Sul](https://www.lume.ufrgs.br/apresentacao).  

# License

This [project](https://github.com/mykolav/coollang-2020-fs) is licensed under the MIT license.  
See [LICENSE](./LICENSE) for details.
