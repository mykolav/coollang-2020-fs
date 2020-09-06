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

// The expresson's syntax is split in `expr`, `exprprefix`, `primary`, and `exprsuffix` to avoid left recursion.
expr
    : exprprefix* primary exprsuffix*
    ;

exprprefix
    : ID '=' expr
    | '!' expr
    | '-' expr
    | 'if' '(' expr ')' expr 'else' expr
    | 'while' '(' expr ')' expr
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

exprsuffix
    : (('<=' | '<' | '>=' | '>' | '==' | '*' | '/' | '+' | '-') expr) 
    | ('match' cases) 
    | ('.' ID actuals)
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
