> 🐌  
> Yours truly is not an expert in compilers but does enjoy diving into this topic and learning something new from time to time.

Why write another toy compiler? First and foremost, because it's a lot of fun. Also learning a great deal along the way comes with the package. Learning not only in terms of technical skills but also in terms of managing a project's scope, complexity, and ultimately delivering a working program implementing the original idea. 

As someone who finds these reasons compelling, [I developed a hobby compiler too](https://github.com/mykolav/coollang-2020-fs). Read on to find out about things that might set this one apart.

![Not exactly the Dragon Book's cover...](./images/complexity-of-toy-compiler-design.png)

- [A taste of the language](#a-taste-of-the-language)
  - [The language is concise but retains a degree of expressivity](#the-language-is-concise-but-retains-a-degree-of-expressivity)
- [A couple words about the implementation](#a-couple-words-about-the-implementation)
  - [It is cross-platform](#it-is-cross-platform)
  - [The test suite](#the-test-suite)
  - [Maybe the only hobby-compiler that has a demo video :)](#maybe-the-only-hobby-compiler-that-has-a-demo-video-)
- [A crucial design decision: compile time errors handling](#a-crucial-design-decision-compile-time-errors-handling)
  - [Stop on the first error](#stop-on-the-first-error)
  - [Try to discover the maximum number of errors](#try-to-discover-the-maximum-number-of-errors)
  - [A hybrid approach](#a-hybrid-approach)
- [Parsing](#parsing)
- [Emitting assembly](#emitting-assembly)
  - [Human-readable assembly](#human-readable-assembly)
  - [Register allocation](#register-allocation)
- [Runtime library](#runtime-library)
  - [Runtime library source code structure](#runtime-library-source-code-structure)
  - [The entry point](#the-entry-point)
- [Learning resources](#learning-resources)
  - [Crafting Interpreters](#crafting-interpreters)
  - [Introduction to Compilers and Language Design](#introduction-to-compilers-and-language-design)
  - [Building a Compiler video series by Immo Landwerth](#building-a-compiler-video-series-by-immo-landwerth)
  - [CS 6120: Advanced Compilers: The Self-Guided Online Course](#cs-6120-advanced-compilers-the-self-guided-online-course)
  - [LLVM IR Tutorial - Phis, GEPs ...](#llvm-ir-tutorial---phis-geps-)
- [Looking to implement a compiler yourself?](#looking-to-implement-a-compiler-yourself)
  - [Do not start with classics textbooks](#do-not-start-with-classics-textbooks)
  - [Don’t try to come up with your own language just yet](#dont-try-to-come-up-with-your-own-language-just-yet)
  - [Stay focused](#stay-focused)


# A taste of the language

Code calculating a portion of the Fibonacci sequence looks like this:

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

Or take a look at the sources of [Conway’s Game of Life](https://github.com/mykolav/coollang-2020-fs/blob/35d603c54392d94efef41540a48f50fe25c51ba5/src/Tests/CoolPrograms/Runtime/Life.cool)

## The language is concise but retains a degree of expressivity

[Cool 2020](https://web.archive.org/web/20210823043833/http://www.cs.uwm.edu/classes/cs654/handout/cool-manual.pdf) is a small Scala subset which can be implemented in a time-frame reasonable for a hobby compiler. Although concise, the language is quite expressive — the features include:

  - Classes
  - Inheritance
  - Virtual dispatch
  - And even a very simple form of pattern matching

These represent exciting compiler design and implementation challenges. Cool 2020 is statically typed, which again leads to interesting implications for design and implementation. 

The language being a Scala subset, tools for Scala code syntax highlighting, formatting, and editing work out of the box. A Cool 2020 program can be (with just a little bit of [scaffolding code](https://mykolam.net/posts/toy-compiler-of-scala-subset/5-cool2020-scala-adapter/)) compiled by a Scala compiler or executed in [an online playground](https://scastie.scala-lang.org/).

# A couple words about the implementation

## It is cross-platform

The two supported hosts are x86-64 Windows and Linux. And in general, the project is based on cross-platform technologies. The compiler itself runs on .NET. The GNU tools `as` and `ld` are available on Linux and Windows (and many, many other, of course). The language's runtime is implemented in assembly and is split up into three parts: the majority of code is in a common source file, Windows-specific bits, and Linux-specific bits that are responsible for calling Windows API and Linux system functions reside in their respective source files.

## The test suite

Approximately 288 automated tests make up [the test suite](https://github.com/mykolav/coollang-2020-fs/tree/master/src/Tests). One nice consequence of generating native executables is an automated test can compile a program, run it, and compare the program's output to the expected values. Out of the 288 tests, there are [39 that do exactly that](https://github.com/mykolav/coollang-2020-fs/tree/master/src/Tests/CoolPrograms/Runtime).

## Maybe the only hobby-compiler that has a demo video :)

Here's how compiling a hello world looks like.

![A compilation session demo](./images/compilation-session-demo.gif#center)

# A crucial design decision: compile time errors handling

Reporting errors encountered in the user’s code is an essential function of a compiler. Spending some time upfront to think though this aspects of compiler design is well worth it. As the decision has a significant impact on the compiler's mechanics and code. So, it's great to have a set of explicit, coherent assumptions in mind when working on the implementation.

## Stop on the first error

The compiler can display an error message and stop the translation once it runs into the first error. This is arguably the simplest error-handling strategy to implement. It has an obvious downside. If a program contains multiple errors, this strategy makes the user go through an equal number of correct the error / restart compilation iterations to catch them all. The user experience might not necessarily be ideal in this case.

## Try to discover the maximum number of errors

At the other end of the spectrum, a compiler can attempt to identify and report as many errors in the code as possible within a single run. When such a compiler detects an error it informs the user but, instead of stopping, it keeps going over the code. With the aim of keeping the number of times the user has to restart compilation to a minimum &mdash; only one restart is required in the best-case scenario. While this strategy sounds a lot nicer it's not without problems either.

### Cascading errors

It leads to a notable increase in the compiler's code complexity, as the compiler has to employ heuristics reducing cascading errors. What’s a cascading error? In the Scala code sample below, there's *one* error &mdash; the closing parenthesis is missing in line 2.

```scala{linenos=true}
class CascadingError {
  def force(: Unit = {
  };  
}
```

But the Scala compiler [diagnoses *two* errors](https://scastie.scala-lang.org/sBJyZ6WWSHeYWzx2daD9XA). After seeing the first, real one it gets "confused" and reports the second error in line 3 where there is no actual problem with the code. This is an example of cascading error.

```
CascadingError.scala:2: error: identifier expected but ':' found.  
    def speak(: Unit = {  
              ^
CascadingError.scala:3: error: ':' expected but ';' found.  
    };  
     ^
```

In some cases, cascading errors make diagnostic messages so noisy, [users start looking for a way to make the compiler stop after the first encountered error](https://stackoverflow.com/questions/27493895/make-scala-compiler-stop-on-first-error). The effort necessary to tell real errors from cascading errors can degrade the user experience to such a degree as to defy the entire point of reporting maximum number of error in one go.

## A hybrid approach

Some compilers aim to integrate the strong sides of both approaches by following a middle-of-the-road error handling strategy. Translation of a program from the input language into the target one typically happens over a number of stages. For an educational compiler these stages can look like this.

- Lexical analysis, or simply lexing
- Syntactic analysis, or parsing
- Semantic analysis
- Emitting x86-64 assembly

(What about a production-grade compiler? C# compiler has [more than 20 compilation stages](https://docs.microsoft.com/en-us/archive/blogs/ericlippert/how-many-passes))

The idea is to diagnose as many errors in the input code as possible within one compilation stage. The key difference with the approach of detecting maximum errors per a compilation is that the next compilation stage will only start if no errors are found at the current one. 

At the lexical analysis stage, if the lexer encounters a portion of the source that doesn't correspond to any valid token, an error is reported to the users and lexing continues. But parsing isn’t going to start, the compiler will stop once lexing is complete. 

Similarly, if the parser encounters a syntactic error, it's reported to the user and parsing continues. Once syntactic analysis is complete, the compiler will stop instead of moving on to the semantic analysis stage.

In contrast, the C# compiler performs lexing and parsing at the same time. If the lexer doesn't recognize a token, the parser handles this scenario and keeps going anyway. But semantic analysis will not start if lexical or syntactic errors have been encountered in the input code. In such a case, translation terminates.

And so on.

### C# compiler as an example

A great example of going this way is the C# compiler. Here’s [a short description](https://stackoverflow.com/a/4698306/818321) Eric Lippert gave on StackOverflow:

> Briefly, the way the compiler works is it tries to get the program through a series of stages [...]
> 
> The idea is that if an early stage gets an error, we might not be able to successfully complete a later stage without (1) going into an infinite loop, (2) crashing, or (3) reporting crazy "cascading" errors. So what happens is, you get one error, you fix it, and then suddenly the next stage of compilation can run, and it finds a bunch more errors.
>
> [...]
>
> The up side of this design is that (1) you get the errors that are the most "fundamental" first, without a lot of noisy, crazy cascading errors, and (2) the compiler is more robust because it doesn't have to try to do analysis on programs where the basic invariants of the language are broken. The down side is of course your scenario: that you have fifty errors, you fix them all, and suddenly fifty more appear.

Finally, let’s see how this idea is represented in [the C# compiler’s source code](https://github.com/dotnet/roslyn/blob/80e9227e49088d53eda930ecdee87a82c665ffd1/src/Compilers/Core/Portable/CommandLine/CommonCompiler.cs#L929). The method `CompileAndEmit` of the class `Microsoft.CodeAnalysis.CommonCompiler` coordinates compilation of C# and VB.NET source code. 

```csharp
/// <summary>
/// Perform all the work associated with actual compilation
/// (parsing, binding, compile, emit), resulting in diagnostics
/// and analyzer output.
/// </summary>
private void CompileAndEmit(/*[...]*/)
{
    analyzerCts = null;
    reportAnalyzer = false;
    analyzerDriver = null;

    // Print the diagnostics produced during the parsing stage and exit if there were any errors.
    compilation.GetDiagnostics(CompilationStage.Parse, includeEarlierStages: false, diagnostics, cancellationToken);
    if (HasUnsuppressableErrors(diagnostics))
    {
        return;
    }

    // [...]
}
```

Right away, we notice the invocation of `compilation.GetDiagnostics` with `CompilationStage.Parse`. The invocation is followed by a check `HasUnsuppressableErrors` to determine whether the compilation stage complete successfully and should the compilation move on to the next stage or stop. If you keep looking through the code of `CompileAndEmit` you'll find more spots that perform a call to `HasUnsuppressableErrors(diagnostics)` and based on the result decide to stop or carry on with the compilation.

# Parsing

It's easy to get hung up on parsing theory as it's a fascinating area of computer science. But try and resist the temptation as it gets real complex really fast. And the thing is, coding up a **recursive descent parser** takes very little knowledge of the theory. Of course, you can always come back later and study parsing in great details. Actually, first getting a feel for it by doing is going to be a great help in grasping the theory later.

Using a parser generator tool or a parser combinator library might sounds like a good way to save time and effort vs writing a parser by hand. But writing a hand-crafted parser might turn out easier than it seems from the outset. Once you get into the groove, you may realize that while it does take more typing, than using a tool wood, in return you get straightforward handwritten code that is easy to reason about and debug. And if you hit a special case, you just add a branch to handle it instead of fighting with a tool which might or might not be well documented.

Again, I cannot recommend ["Crafting Interpreters"](https://craftinginterpreters.com/contents.html) enough for a guide on implementing a parser.

It's hard to underestimate the importance of having a working example to look at and fully comprehend. Here goes a couple:

- [8cc C Compiler](https://github.com/rui314/8cc)
- [chibicc: A Small C Compiler](https://github.com/rui314/chibicc)
- [Tiny C Compiler](https://github.com/TinyCC/tinycc)
- [lcc](https://github.com/drh/lcc)

Please, keep in mind, the last two are much bigger than `8cc` and `chibicc` and don't have easy of exploring their sources as an explicit goal.

An impressive thing to note is production-grade, major-league language implementations like `GCC`, `Clang`, `Roslyn (C# and VB.NET compiler)`, `D`, `Rust`, `Swift`, `Go`, `Java`, `V8`, and `TypeScript` use hand-crafted recursive descent parsers too. So, if you decide to go down this route, your experience will actually be closer to that of professional compiler developers :)

# Emitting assembly

The compiler translates the source code down to x86-64 assembly. Then GNU `as` and `ld` come into play to build a native executable from the assembly. 

Many educational compilers emit MIPS assembly, and although it's possible to run it using an emulator, to me, running a native executable produced by your own compiler feels much more rewarding.

I also prefer emitting assembly instead of, for instance, C. It helps understand the inner workings of various things we often take for granted when coding in high-level languages:

- Managing a function's stack frame
- Calling conventions
- Finding the memory address of an identifier
- Converting expressions into assembly
- And so on, and so forth

On the other hand, someone enthusiastic about compilers isn't unlikely to be interested in going even deeper, down to the level of binary machine code and linking. I decided to stop short of working on these undeniably enticing subjects, to focus on building a working compiler first.

All in all, assembly code native to your platform is a sweet spot for translation. It gets you close-enough to the metal to learn how high-level languages do their magic. But is abstract enough to avoid getting distracted by somewhat orthogonal subjects.

[Chapter 11 – Code Generation](https://www3.nd.edu/~dthain/compilerbook/chapter11.pdf) of [Introduction to Compilers and Language Design](https://www3.nd.edu/~dthain/compilerbook/) is a great discussion of a basic approach to assembly generation.

Examining the sources of [chibicc: A Small C Compiler](https://github.com/rui314/chibicc) or [8cc C Compiler](https://github.com/rui314/8cc) is an excellent opportunity to see assembly-generating code in action.

## Human-readable assembly

The emitted assembly code contains a lot of hopefully useful comments. The idea here is to help understand the assembly and relate it back to the source code as much as possible. 

```asm
    .text
    # ../CoolPrograms/Runtime/Fibonacci.cool(1,7): Fib
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
    movq    %rbx, 0(%rsp) # actual #0
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    # remove the register-loaded actuals from stack
    addq    $8, %rsp
    call    IO..ctor # super..ctor
    movq    %rax, %rbx # returned value
    # ../CoolPrograms/Runtime/Fibonacci.cool(8,5): var i: Int = 0
    # ../CoolPrograms/Runtime/Fibonacci.cool(8,18): 0
    movq    $int_const_0, %rbx
    movq    %rbx, -16(%rbp) # i
    # ../CoolPrograms/Runtime/Fibonacci.cool(9,5): while (i <= 10) { \n   ...
.label_6: # while cond
    # ../CoolPrograms/Runtime/Fibonacci.cool(9,12): i <= 10
    # ../CoolPrograms/Runtime/Fibonacci.cool(9,12): i
    movq    -16(%rbp), %r10 # i
    # ../CoolPrograms/Runtime/Fibonacci.cool(9,17): 10
    movq    $int_const_2, %r11
```

## Register allocation

In short, programs use processor registers to temporarily store variable and expression values. This post won't dig any deeper than that, as by the time you get to implementing [register allocation](https://en.wikipedia.org/wiki/Register_allocation), you'll in all likelihood have read longer descriptions by people who are actually good at explaining things.

Anyway, something to keep in mind, coding up register allocation can be a rather involved exercise. Register allocation in production compilers is [a matter](http://www.rw.cdl.uni-saarland.de/~grund/papers/cc06-ra_ssa.pdf) [of scientific](https://www.info.uni-karlsruhe.de/uploads/publikationen/braun13cc.pdf) [research](http://web.cs.ucla.edu/~palsberg/course/cs232/papers/HackGoos-ipl06.pdf). But there's an extremely simple approach that is great to get a compiler up and running and can get it surprisingly far. It's laid out in that same "Chapter 11 – Code Generation":

> [Y]ou can see that we set aside each register for a purpose: some are for function arguments, some for stack frame management, and some are available for scratch values. Take those scratch registers and put them into a table [...]
> Then, write `scratch_alloc` to find an unused register in the table, mark it as in use, and return the register number `r`. `scratch_free` should mark the indicated register as available. `scratch_name` should return the name of a register, given its number `r`. Running out of scratch registers is possible, but unlikely, as we will see below. For now, if `scratch_alloc` cannot find a free register, just emit an error message and halt.
>
> &mdash; <cite>[11.2 Supporting Functions](https://www3.nd.edu/~dthain/compilerbook/chapter11.pdf)</cite>

# Runtime library

An implementation of educational language relies on a number of built-in functions, classes, and objects. Some of these are available to the user directly, such as ChocoPy's `print` function or Cool 2020's `IO` class. References to other built-ins can only be injected into a program by the compiler &mdash; for example, to allocate the memory for a new object or abort the execution in case things have gone off the rails.

The only readily available [runtime implementation](https://theory.stanford.edu/~aiken/software/cooldist/lib/trap.handler) for an educational language I'm aware of is written in MIPS assembly for the language [COOL](https://theory.stanford.edu/~aiken/software/cool/cool.html) designed by [Alex Aiken](https://profiles.stanford.edu/alex-aiken) used in his famous [online course](https://online.stanford.edu/courses/soe-ycscs1-compilers). 

(Notice, while the names Cool 2020 and COOL sound confusingly similar, the languages themselves are distinct: Cool 2020 is a Scala subset and COOL is an invention of Alex Aiken. The name Cool 2020 is a tribute to Aiken's COOL.)

Examining COOL runtime's MIPS code provides enough insights to implement a similar one for a different "client" language and dialect of assembly. This is how I came up with a [runtime](https://github.com/mykolav/coollang-2020-fs/tree/master/src/Runtime) for Cool 2020 in x86-64 assembly. It's written in an extremely naive and unoptimized way, but gets the job done. One serious omission though, there's no garbage collection code (One day...) 

## Runtime library source code structure

The runtime is made up of three assembly source files: 

- [rt_common.s](https://github.com/mykolav/coollang-2020-fs/blob/6174d03148e7e395d1e5777c370000bf47e1578f/src/Runtime/rt_common.s) &mdash; the common code that doesn't depend on a platform's specifics resides in this file
- [rt_linux.s](https://github.com/mykolav/coollang-2020-fs/blob/6174d03148e7e395d1e5777c370000bf47e1578f/src/Runtime/rt_linux.s)  &mdash; Linux-specific bits
- [rt_windows.s](https://github.com/mykolav/coollang-2020-fs/blob/6174d03148e7e395d1e5777c370000bf47e1578f/src/Runtime/rt_windows.s) &mdash; Windows-specific bits

## The entry point

In particular, `rt_common.s` contains a compiled program's process entry point responsible for 

- Invoking the platform-dependent initialization routine
- Allocating memory for an instance of the class `Main` and invoking its constructor. 
- Exiting the process once the user's code have finished executing

The process entry point's logic expressed in pseudocode follows.

```scala
 def main () = {
 . Platform .init();

 new Main();

 .Platform.exit_process(0)
}
```

And here is the actual assembly code.

```asm
    .global main
main:
    call    .Platform.init

    # A class 'Main' must be present in every Cool2020 program.
    # Create a new instance of 'Main'.
    movq    $Main_proto_obj, %rdi
    call    .Runtime.copy_object

    # 'Main..ctor' is a Cool2020 program's entry point.
    # Pass a reference to the newly created 'Main' instance in %rdi.
    # Invoke the constructor.
    movq    %rax, %rdi
    call    Main..ctor

    xorq    %rdi, %rdi
    jmp     .Platform.exit_process
```

# Learning resources

## Crafting Interpreters

Yes, yes, you read that right, interpreters. [Crafting Interpreters](https://craftinginterpreters.com/contents.html) by Robert Nystrom is simply a brilliant book, which despite its name can serve as a great introduction to compilers. Compilers and interpreters have a lot in common and by the time you get to the interpreters-specific topics, you’ll gain a ton of useful knowledge. In my opinion, the explanations on general programming language implementation concepts, lexing, and parsing are downright the best in class. 

## Introduction to Compilers and Language Design

[Introduction to Compilers and Language Design](https://www3.nd.edu/~dthain/compilerbook/) by Douglas Thain is really accessible and doesn't assume any preexisting compilers knowledge. It teaches all the basics necessary to build a compiler: from lexing and parsing to a program's memory organization and stack management to assembly generation. It's self-contained and takes a hands-on approach: you'll end up with a working compiler if you choose to following the book through. If later you feel like digging into more advanced compiling techniques, you'll have a solid foundation to build upon.

Both are freely available online. (I encourage you to consider buying the paid versions of the books though, if you can, to support the authors).

## Building a Compiler video series by Immo Landwerth

Who’s gonna watch a Fortnite stream on Twitch, when Immo Landwerth is [coding up a compiler](https://www.youtube.com/watch?v=wgHIkdUQbp0&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y) live on YouTube?! Apart from all the usual compiling topics, the series touch upon adding a basic debugger support, so that it's possible to step through the source of a compiled program. Immo is an awesome educator and their code is accessible and simply beautiful. You can explore it in [the project’s github repo](https://github.com/terrajobst/minsk).

## CS 6120: Advanced Compilers: The Self-Guided Online Course

A free self-guided online course from Cornell University. Goes in depth on IR and optimizations but is very accessible at the same time. Again, cannot recommend highly enough. [CS 6120: Advanced Compilers: The Self-Guided Online Course](https://www.cs.cornell.edu/courses/cs6120/2020fa/self-guided/).

## LLVM IR Tutorial - Phis, GEPs ...

A talk on intermediate representations (IR). Previous knowledge is not required. What’s so cool about this talk is it’s built around one of the most widely used IR — LLVM IR. [2019 EuroLLVM Developers’ Meeting: V. Bridgers & F. Piovezan "LLVM IR Tutorial - Phis, GEPs ..."](https://www.youtube.com/watch?v=m8G_S5LwlTo).

# Looking to implement a compiler yourself?

## Do not start with classics textbooks

While there’s no way around studying a bit of theory before getting down to coding, **do not** start with classics textbooks like the Dragon Book, or “Modern Compiler Implementation”. These are great, and you can always come back to them later, when you’re better prepared to take the most out of them. 

That said, it’s way more productive to read ones geared toward beginners first. The ones I'd opt for are listed in the [Learning resources](#learning-resources) section.

## Don’t try to come up with your own language just yet

Go with an existing educational language instead, and focus on learning about compilers.

## Stay focused

Compiling is a huge field. You'll encounter seemingly endless choices, techniques, and opinions each step of the way. At times it's hard not to give in to the temptation of going down the rabbit hole of researching all kind of ways of implementing a compilation stage or algorithm. I believe keeping the project's scope as narrow as possible is a recipe for actually getting through to a working compiler. 

Stick to straightforward simple ways of doing things at first. Consider writing down all the interesting links and nuggets of information you come across and setting them aside for a time. Once you finally make it to the other side, you can start going through your notes and replacing portions of the code with more advanced algorithms or extending it with additional functionality.

Good luck!
