    .data
    .global class_name_table
    .global Main_proto_obj
    .global Main..ctor

    .quad -1
str_const_0:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_0 # length = 0
    # ''
    .byte 0 # terminator
    .zero 7 # payload's size in bytes = 25, pad to an 8 byte boundary
    .quad -1
str_const_1:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_4 # length = 4
    # 'fib('
    .byte 102, 105, 98, 40
    .byte 0 # terminator
    .zero 3 # payload's size in bytes = 29, pad to an 8 byte boundary
    .quad -1
str_const_2:
    .quad 3 # tag
    .quad 10 # size in quads
    .quad String_vtable
    .quad int_const_5 # length = 51
    # '../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool'
    .byte 46, 46, 47, 46, 46, 47, 115, 114, 99, 47, 84, 101, 115, 116, 115, 47, 67, 111, 111, 108, 80, 114, 111, 103, 114, 97, 109, 115, 47, 82, 117, 110, 116, 105, 109, 101, 47, 70, 105, 98, 111, 110, 97, 99, 99, 105, 46, 99, 111, 111, 108
    .byte 0 # terminator
    .zero 4 # payload's size in bytes = 76, pad to an 8 byte boundary
    .quad -1
str_const_3:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_4 # length = 4
    # ') = '
    .byte 41, 32, 61, 32
    .byte 0 # terminator
    .zero 3 # payload's size in bytes = 29, pad to an 8 byte boundary
    .quad -1
str_const_4:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_6 # length = 3
    # 'Any'
    .byte 65, 110, 121
    .byte 0 # terminator
    .zero 4 # payload's size in bytes = 28, pad to an 8 byte boundary
    .quad -1
str_const_5:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_4 # length = 4
    # 'Unit'
    .byte 85, 110, 105, 116
    .byte 0 # terminator
    .zero 3 # payload's size in bytes = 29, pad to an 8 byte boundary
    .quad -1
str_const_6:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_6 # length = 3
    # 'Int'
    .byte 73, 110, 116
    .byte 0 # terminator
    .zero 4 # payload's size in bytes = 28, pad to an 8 byte boundary
    .quad -1
str_const_7:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_7 # length = 6
    # 'String'
    .byte 83, 116, 114, 105, 110, 103
    .byte 0 # terminator
    .zero 1 # payload's size in bytes = 31, pad to an 8 byte boundary
    .quad -1
str_const_8:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_8 # length = 7
    # 'Boolean'
    .byte 66, 111, 111, 108, 101, 97, 110
    .byte 0 # terminator
    .quad -1
str_const_9:
    .quad 3 # tag
    .quad 5 # size in quads
    .quad String_vtable
    .quad int_const_9 # length = 8
    # 'ArrayAny'
    .byte 65, 114, 114, 97, 121, 65, 110, 121
    .byte 0 # terminator
    .zero 7 # payload's size in bytes = 33, pad to an 8 byte boundary
    .quad -1
str_const_10:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_3 # length = 2
    # 'IO'
    .byte 73, 79
    .byte 0 # terminator
    .zero 5 # payload's size in bytes = 27, pad to an 8 byte boundary
    .quad -1
str_const_11:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_6 # length = 3
    # 'Fib'
    .byte 70, 105, 98
    .byte 0 # terminator
    .zero 4 # payload's size in bytes = 28, pad to an 8 byte boundary
    .quad -1
str_const_12:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_4 # length = 4
    # 'Main'
    .byte 77, 97, 105, 110
    .byte 0 # terminator
    .zero 3 # payload's size in bytes = 29, pad to an 8 byte boundary
    .quad -1
int_const_0:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 0 # value
    .quad -1
int_const_1:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 1 # value
    .quad -1
int_const_2:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 10 # value
    .quad -1
int_const_3:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 2 # value
    .quad -1
int_const_4:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 4 # value
    .quad -1
int_const_5:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 51 # value
    .quad -1
int_const_6:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 3 # value
    .quad -1
int_const_7:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 6 # value
    .quad -1
int_const_8:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 7 # value
    .quad -1
int_const_9:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 8 # value
class_name_table:
    .quad str_const_4 # Any
    .quad str_const_5 # Unit
    .quad str_const_6 # Int
    .quad str_const_7 # String
    .quad str_const_8 # Boolean
    .quad str_const_9 # ArrayAny
    .quad str_const_10 # IO
    .quad str_const_11 # Fib
    .quad str_const_12 # Main
class_parent_table:
    .quad -1 # Any
    .quad 0 # Unit extends Any
    .quad 0 # Int extends Any
    .quad 0 # String extends Any
    .quad 0 # Boolean extends Any
    .quad 0 # ArrayAny extends Any
    .quad 0 # IO extends Any
    .quad 6 # Fib extends IO
    .quad 0 # Main extends Any
Fib_vtable:
    .quad Any.abort 
    .quad IO.out_string 
    .quad IO.out_int 
    .quad IO.out_nl 
    .quad IO.in_string 
    .quad IO.in_int 
    .quad Fib.fib 
Main_vtable:
    .quad Any.abort 
    .quad -1
Fib_proto_obj:
    .quad 7 # tag
    .quad 3 # size in quads
    .quad Fib_vtable
    .quad -1
Main_proto_obj:
    .quad 8 # tag
    .quad 3 # size in quads
    .quad Main_vtable

    .text
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(1,7): Fib
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
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(8,5): var i: Int = 0
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(8,18): 0
    movq    $int_const_0, %rbx
    movq    %rbx, -16(%rbp) # i
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(9,5): while (i <= 10) { \n   ...
.label_6: # while cond
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(9,12): i <= 10
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(9,12): i
    movq    -16(%rbp), %r10 # i
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(9,17): 10
    movq    $int_const_2, %r11
    movq    24(%r10), %r10
    movq    24(%r11), %r11
    cmpq    %r11, %r10
    jle    .label_7 # true branch
    # false branch
    movq    $Unit_value, %rbx # unit
    jmp     .label_8 # done
.label_7: # true branch
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(10,7): out_string("fib(")
    # actual #0
    movq    -8(%rbp), %rbx
    cmpq    $0, %rbx
    jne     .label_0 # the receiver is some
    movq    $str_const_2, %rdi
    movq    $10, %rsi
    movq    $7, %rdx
    call    .Runtime.abort_dispatch
.label_0: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(10,17): ("fib("); out_int(i) ...
    subq    $16, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(10,18): "fib("
    movq    $str_const_1, %rbx
    movq    %rbx, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %rbx # Fib_vtable
    movq    8(%rbx), %rbx # Fib.out_string
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(10,27): out_int(i)
    # actual #0
    movq    -8(%rbp), %rbx
    cmpq    $0, %rbx
    jne     .label_1 # the receiver is some
    movq    $str_const_2, %rdi
    movq    $10, %rsi
    movq    $27, %rdx
    call    .Runtime.abort_dispatch
.label_1: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(10,34): (i); out_string(") = ...
    subq    $16, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(10,35): i
    movq    -16(%rbp), %rbx # i
    movq    %rbx, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %rbx # Fib_vtable
    movq    16(%rbx), %rbx # Fib.out_int
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(10,39): out_string(") = ")
    # actual #0
    movq    -8(%rbp), %rbx
    cmpq    $0, %rbx
    jne     .label_2 # the receiver is some
    movq    $str_const_2, %rdi
    movq    $10, %rsi
    movq    $39, %rdx
    call    .Runtime.abort_dispatch
.label_2: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(10,49): (") = "); \n       out ...
    subq    $16, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(10,50): ") = "
    movq    $str_const_3, %rbx
    movq    %rbx, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %rbx # Fib_vtable
    movq    8(%rbx), %rbx # Fib.out_string
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(11,7): out_int(fib(i))
    # actual #0
    movq    -8(%rbp), %rbx
    cmpq    $0, %rbx
    jne     .label_4 # the receiver is some
    movq    $str_const_2, %rdi
    movq    $11, %rsi
    movq    $7, %rdx
    call    .Runtime.abort_dispatch
.label_4: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(11,14): (fib(i)); \n       out ...
    subq    $16, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(11,15): fib(i)
    # actual #0
    movq    -8(%rbp), %rbx
    cmpq    $0, %rbx
    jne     .label_3 # the receiver is some
    movq    $str_const_2, %rdi
    movq    $11, %rsi
    movq    $15, %rdx
    call    .Runtime.abort_dispatch
.label_3: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(11,18): (i)); \n       out_nl( ...
    subq    $16, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(11,19): i
    movq    -16(%rbp), %rbx # i
    movq    %rbx, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %rbx # Fib_vtable
    movq    48(%rbx), %rbx # Fib.fib
    call    *%rbx
    movq    %rax, %r10 # returned value
    movq    %r10, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %rbx # Fib_vtable
    movq    16(%rbx), %rbx # Fib.out_int
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(12,7): out_nl()
    # actual #0
    movq    -8(%rbp), %rbx
    cmpq    $0, %rbx
    jne     .label_5 # the receiver is some
    movq    $str_const_2, %rdi
    movq    $12, %rsi
    movq    $7, %rdx
    call    .Runtime.abort_dispatch
.label_5: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(12,13): (); \n        \n       i ...
    subq    $8, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    # remove the register-loaded actuals from stack
    addq    $8, %rsp
    movq    16(%rdi), %rbx # Fib_vtable
    movq    24(%rbx), %rbx # Fib.out_nl
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(14,7): i = i + 1
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(14,11): i + 1
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(14,11): i
    movq    -16(%rbp), %rbx # i
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(14,15): 1
    movq    $int_const_1, %r10
    pushq   %r10
    movq    %r10, %rdi
    call    .Runtime.copy_object
    popq    %r10
    movq    %rax, %r10
    movq    24(%rbx), %rbx
    addq    %rbx, 24(%r10)
    movq    %r10, -16(%rbp) # i
    jmp     .label_6 # while cond
.label_8: # done
    movq    -8(%rbp), %rbx
    movq    %rbx, %rax # this
    # restore callee-saved regs
    movq    -24(%rbp), %rbx
    movq    -32(%rbp), %r12
    movq    -40(%rbp), %r13
    movq    -48(%rbp), %r14
    movq    -56(%rbp), %r15
    # restore the caller's frame
    movq    %rbp, %rsp
    popq    %rbp
    ret
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(2,3): def fib(x: Int): Int ...
Fib.fib:
    pushq   %rbp
    movq    %rsp, %rbp
    subq    $64, %rsp
    # store actuals on the stack
    movq    %rdi, -8(%rbp)
    movq    %rsi, -16(%rbp)
    # store callee-saved regs on the stack
    movq    %rbx, -24(%rbp)
    movq    %r12, -32(%rbp)
    movq    %r13, -40(%rbp)
    movq    %r14, -48(%rbp)
    movq    %r15, -56(%rbp)
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(3,5): if (x == 0) 0 \n     e ...
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(3,9): x
    movq    -16(%rbp), %rbx # x
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(3,14): 0
    movq    $int_const_0, %r11
    # are pointers equal?
    cmpq    %r11, %rbx
    je      .label_13 # equal
    pushq   %r10
    pushq   %r11
    movq    %rbx, %rdi
    movq    %r11, %rsi
    call    .Runtime.are_equal
    popq    %r11
    popq    %r10
    movq    24(%rax), %rax
    cmpq    $0, %rax
    jne     .label_13 # equal
    # unequal
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(4,10): if (x == 1) 1 \n     e ...
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(4,14): x
    movq    -16(%rbp), %r10 # x
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(4,19): 1
    movq    $int_const_1, %r12
    # are pointers equal?
    cmpq    %r12, %r10
    je      .label_11 # equal
    pushq   %r10
    pushq   %r11
    movq    %r10, %rdi
    movq    %r12, %rsi
    call    .Runtime.are_equal
    popq    %r11
    popq    %r10
    movq    24(%rax), %rax
    cmpq    $0, %rax
    jne     .label_11 # equal
    # unequal
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(5,10): fib(x - 2) + fib(x - ...
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(5,10): fib(x - 2)
    pushq   %r10
    # actual #0
    movq    -8(%rbp), %r11
    cmpq    $0, %r11
    jne     .label_9 # the receiver is some
    movq    $str_const_2, %rdi
    movq    $5, %rsi
    movq    $10, %rdx
    call    .Runtime.abort_dispatch
.label_9: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(5,13): (x - 2) + fib(x - 1) ...
    subq    $16, %rsp
    movq    %r11, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(5,14): x - 2
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(5,14): x
    movq    -16(%rbp), %r11 # x
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(5,18): 2
    movq    $int_const_3, %r12
    pushq   %r10
    pushq   %r11
    movq    %r11, %rdi
    call    .Runtime.copy_object
    popq    %r11
    popq    %r10
    movq    %rax, %r11
    movq    24(%r12), %r12
    subq    %r12, 24(%r11)
    movq    %r11, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %r11 # Fib_vtable
    movq    48(%r11), %r11 # Fib.fib
    call    *%r11
    popq    %r10
    movq    %rax, %r12 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(5,23): fib(x - 1)
    pushq   %r10
    # actual #0
    movq    -8(%rbp), %r11
    cmpq    $0, %r11
    jne     .label_10 # the receiver is some
    movq    $str_const_2, %rdi
    movq    $5, %rsi
    movq    $23, %rdx
    call    .Runtime.abort_dispatch
.label_10: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(5,26): (x - 1); \n  \n   { \n     ...
    subq    $16, %rsp
    movq    %r11, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(5,27): x - 1
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(5,27): x
    movq    -16(%rbp), %r11 # x
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(5,31): 1
    movq    $int_const_1, %r13
    pushq   %r10
    pushq   %r11
    movq    %r11, %rdi
    call    .Runtime.copy_object
    popq    %r11
    popq    %r10
    movq    %rax, %r11
    movq    24(%r13), %r13
    subq    %r13, 24(%r11)
    movq    %r11, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %r11 # Fib_vtable
    movq    48(%r11), %r11 # Fib.fib
    call    *%r11
    popq    %r10
    movq    %rax, %r13 # returned value
    pushq   %r10
    movq    %r13, %rdi
    call    .Runtime.copy_object
    popq    %r10
    movq    %rax, %r13
    movq    24(%r12), %r12
    addq    %r12, 24(%r13)
    movq    %r13, %r11
    jmp     .label_12 # done
.label_11: # equal
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(4,22): 1
    movq    $int_const_1, %r10
    movq    %r10, %r11
.label_12: # done
    movq    %r11, %r10
    jmp     .label_14 # done
.label_13: # equal
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(3,17): 0
    movq    $int_const_0, %rbx
    movq    %rbx, %r10
.label_14: # done
    movq    %r10, %rax
    # restore callee-saved regs
    movq    -24(%rbp), %rbx
    movq    -32(%rbp), %r12
    movq    -40(%rbp), %r13
    movq    -48(%rbp), %r14
    movq    -56(%rbp), %r15
    # restore the caller's frame
    movq    %rbp, %rsp
    popq    %rbp
    ret
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(19,7): Main
Main..ctor:
    pushq   %rbp
    movq    %rsp, %rbp
    subq    $48, %rsp
    # store actuals on the stack
    movq    %rdi, -8(%rbp)
    # store callee-saved regs on the stack
    movq    %rbx, -16(%rbp)
    movq    %r12, -24(%rbp)
    movq    %r13, -32(%rbp)
    movq    %r14, -40(%rbp)
    movq    %r15, -48(%rbp)
    # actual #0
    movq    -8(%rbp), %rbx
    subq    $8, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    # remove the register-loaded actuals from stack
    addq    $8, %rsp
    call    Any..ctor # super..ctor
    movq    %rax, %rbx # returned value
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(20,5): new Fib()
    movq    $Fib_proto_obj, %rdi
    call    .Runtime.copy_object
    movq    %rax, %rbx
    # ../../src/Tests/CoolPrograms/Runtime/Fibonacci.cool(20,12): () }; \n } \n // DIAG: B ...
    subq    $8, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    # remove the register-loaded actuals from stack
    addq    $8, %rsp
    call    Fib..ctor
    movq    %rax, %rbx # the new object
    movq    -8(%rbp), %rbx
    movq    %rbx, %rax # this
    # restore callee-saved regs
    movq    -16(%rbp), %rbx
    movq    -24(%rbp), %r12
    movq    -32(%rbp), %r13
    movq    -40(%rbp), %r14
    movq    -48(%rbp), %r15
    # restore the caller's frame
    movq    %rbp, %rsp
    popq    %rbp
    ret
