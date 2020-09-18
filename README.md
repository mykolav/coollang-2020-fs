# The Cool 2020 language compiler implemented in F#

## This is a work in progress and at a very early stage.

# Cool 2020

[CoolAid: The Cool 2020 Reference Manual](http://pabst.cs.uwm.edu/classes/cs654/handout/cool-manual.pdf)

> Cool 2020 is a subset of Scala; ...  

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

## An antlr4 grammar for Cool 2020

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

# Implementation details

The implementation language is F#, but the code is imperative/OO.
That said, the code is, hopefully, consistent with the recommendations from ["F# Code I Love" by Don Syme](https://www.youtube.com/watch?v=1AZA1zoP-II).

Initially it was my ambition to practice writing functional code while learning about compilers. But trying to do both at the same time was biting off more than I could chew.

Used as an imperative/OO language, F# has many and many nice features that more mainstream languages started to catch up with only not that long ago. E.g., no null values by default, records, discriminated unions, pattern matching, primary constructors, etc.

Plus, F# is a low ceremony, low syntactic noise language. In this respect, you can think of F# as Python but statically typed.

# References

- [Douglas Thain,
Introduction to Compilers and Language Design](https://www3.nd.edu/~dthain/compilerbook/)
- [Bob Nystrom, Crafting Interpreters](https://craftinginterpreters.com/)
- [Alex Aiken, Compilers](https://www.edx.org/course/compilers)
- [nicebyte, Let's Learn x86-64 Assembly!](https://gpfault.net/posts/asm-tut-0.txt.html)
- Lots and lots of blog posts and github repos

# Acknowledgements

 The original Cool language was designed by [Alex Aiken](https://theory.stanford.edu/~aiken/).  
 
 The Cool 2020 version was designed by [John Boyland](https://uwm.edu/engineering/people/boyland-ph-d-john/).  
 
 QuickSort.cool and InsertionSort.cool came from [a papaer](https://www.lume.ufrgs.br/bitstream/handle/10183/151038/001009883.pdf)
 from [LUME - the Digital Repository of the Universidade Federal do Rio Grande do Sul](https://www.lume.ufrgs.br/apresentacao).  

# License

This [project](https://github.com/mykolav/coollang-2020-fs) is licensed under the MIT license.  
See [LICENSE](./LICENSE) for details.
