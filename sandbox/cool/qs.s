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
    .quad 10 # size in quads
    .quad String_vtable
    .quad int_const_11 # length = 51
    # '../../src/Tests/CoolPrograms/Runtime/QuickSort.cool'
    .byte 46, 46, 47, 46, 46, 47, 115, 114, 99, 47, 84, 101, 115, 116, 115, 47, 67, 111, 111, 108, 80, 114, 111, 103, 114, 97, 109, 115, 47, 82, 117, 110, 116, 105, 109, 101, 47, 81, 117, 105, 99, 107, 83, 111, 114, 116, 46, 99, 111, 111, 108
    .byte 0 # terminator
    .zero 4 # payload's size in bytes = 76, pad to an 8 byte boundary
    .quad -1
str_const_2:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_3 # length = 1
    # ' '
    .byte 32
    .byte 0 # terminator
    .zero 6 # payload's size in bytes = 26, pad to an 8 byte boundary
    .quad -1
str_const_3:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_7 # length = 3
    # 'Any'
    .byte 65, 110, 121
    .byte 0 # terminator
    .zero 4 # payload's size in bytes = 28, pad to an 8 byte boundary
    .quad -1
str_const_4:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_9 # length = 4
    # 'Unit'
    .byte 85, 110, 105, 116
    .byte 0 # terminator
    .zero 3 # payload's size in bytes = 29, pad to an 8 byte boundary
    .quad -1
str_const_5:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_7 # length = 3
    # 'Int'
    .byte 73, 110, 116
    .byte 0 # terminator
    .zero 4 # payload's size in bytes = 28, pad to an 8 byte boundary
    .quad -1
str_const_6:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_12 # length = 6
    # 'String'
    .byte 83, 116, 114, 105, 110, 103
    .byte 0 # terminator
    .zero 1 # payload's size in bytes = 31, pad to an 8 byte boundary
    .quad -1
str_const_7:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_13 # length = 7
    # 'Boolean'
    .byte 66, 111, 111, 108, 101, 97, 110
    .byte 0 # terminator
    .quad -1
str_const_8:
    .quad 3 # tag
    .quad 5 # size in quads
    .quad String_vtable
    .quad int_const_14 # length = 8
    # 'ArrayAny'
    .byte 65, 114, 114, 97, 121, 65, 110, 121
    .byte 0 # terminator
    .zero 7 # payload's size in bytes = 33, pad to an 8 byte boundary
    .quad -1
str_const_9:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_5 # length = 2
    # 'IO'
    .byte 73, 79
    .byte 0 # terminator
    .zero 5 # payload's size in bytes = 27, pad to an 8 byte boundary
    .quad -1
str_const_10:
    .quad 3 # tag
    .quad 5 # size in quads
    .quad String_vtable
    .quad int_const_15 # length = 9
    # 'QuickSort'
    .byte 81, 117, 105, 99, 107, 83, 111, 114, 116
    .byte 0 # terminator
    .zero 6 # payload's size in bytes = 34, pad to an 8 byte boundary
    .quad -1
str_const_11:
    .quad 3 # tag
    .quad 4 # size in quads
    .quad String_vtable
    .quad int_const_9 # length = 4
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
    .quad 5 # value
    .quad -1
int_const_2:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 30 # value
    .quad -1
int_const_3:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 1 # value
    .quad -1
int_const_4:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 20 # value
    .quad -1
int_const_5:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 2 # value
    .quad -1
int_const_6:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 50 # value
    .quad -1
int_const_7:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 3 # value
    .quad -1
int_const_8:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 40 # value
    .quad -1
int_const_9:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 4 # value
    .quad -1
int_const_10:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 10 # value
    .quad -1
int_const_11:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 51 # value
    .quad -1
int_const_12:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 6 # value
    .quad -1
int_const_13:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 7 # value
    .quad -1
int_const_14:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 8 # value
    .quad -1
int_const_15:
    .quad 2 # tag
    .quad 4 # size in quads
    .quad Int_vtable
    .quad 9 # value
class_name_table:
    .quad str_const_3 # Any
    .quad str_const_4 # Unit
    .quad str_const_5 # Int
    .quad str_const_6 # String
    .quad str_const_7 # Boolean
    .quad str_const_8 # ArrayAny
    .quad str_const_9 # IO
    .quad str_const_10 # QuickSort
    .quad str_const_11 # Main
class_parent_table:
    .quad -1 # Any
    .quad 0 # Unit extends Any
    .quad 0 # Int extends Any
    .quad 0 # String extends Any
    .quad 0 # Boolean extends Any
    .quad 0 # ArrayAny extends Any
    .quad 0 # IO extends Any
    .quad 6 # QuickSort extends IO
    .quad 0 # Main extends Any
QuickSort_vtable:
    .quad Any.abort 
    .quad IO.out_string 
    .quad IO.out_int 
    .quad IO.out_nl 
    .quad IO.in_string 
    .quad IO.in_int 
    .quad QuickSort.quicksort 
    .quad QuickSort.partition 
    .quad QuickSort.array_swap 
    .quad QuickSort.out_array 
Main_vtable:
    .quad Any.abort 
    .quad -1
QuickSort_proto_obj:
    .quad 7 # tag
    .quad 3 # size in quads
    .quad QuickSort_vtable
    .quad -1
Main_proto_obj:
    .quad 8 # tag
    .quad 3 # size in quads
    .quad Main_vtable

    .text
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(3,7): QuickSort
QuickSort..ctor:
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
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(48,5): var array: ArrayAny  ...
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(48,27): new ArrayAny(5)
    # ArrayAny..ctor will allocate memory for N items
    xorq    %rbx, %rbx
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(48,39): (5); \n     array.set( ...
    subq    $16, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(48,40): 5
    movq    $int_const_1, %rbx
    movq    %rbx, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    call    ArrayAny..ctor
    movq    %rax, %rbx # the new object
    movq    %rbx, -16(%rbp) # array
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(49,5): array.set(0, 30)
    # actual #0
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(49,5): array
    movq    -16(%rbp), %rbx # array
    cmpq    $0, %rbx
    jne     .label_0 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $49, %rsi
    movq    $5, %rdx
    call    .Runtime.abort_dispatch
.label_0: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(49,14): (0, 30); \n     array. ...
    subq    $24, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(49,15): 0
    movq    $int_const_0, %rbx
    movq    %rbx, 8(%rsp) # actual #1
    # actual #2
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(49,18): 30
    movq    $int_const_2, %rbx
    movq    %rbx, 16(%rsp) # actual #2
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    movq    16(%rsp), %rdx
    # remove the register-loaded actuals from stack
    addq    $24, %rsp
    movq    16(%rdi), %rbx # ArrayAny_vtable
    movq    16(%rbx), %rbx # ArrayAny.set
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(50,5): array.set(1, 20)
    # actual #0
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(50,5): array
    movq    -16(%rbp), %rbx # array
    cmpq    $0, %rbx
    jne     .label_1 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $50, %rsi
    movq    $5, %rdx
    call    .Runtime.abort_dispatch
.label_1: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(50,14): (1, 20); \n     array. ...
    subq    $24, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(50,15): 1
    movq    $int_const_3, %rbx
    movq    %rbx, 8(%rsp) # actual #1
    # actual #2
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(50,18): 20
    movq    $int_const_4, %rbx
    movq    %rbx, 16(%rsp) # actual #2
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    movq    16(%rsp), %rdx
    # remove the register-loaded actuals from stack
    addq    $24, %rsp
    movq    16(%rdi), %rbx # ArrayAny_vtable
    movq    16(%rbx), %rbx # ArrayAny.set
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(51,5): array.set(2, 50)
    # actual #0
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(51,5): array
    movq    -16(%rbp), %rbx # array
    cmpq    $0, %rbx
    jne     .label_2 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $51, %rsi
    movq    $5, %rdx
    call    .Runtime.abort_dispatch
.label_2: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(51,14): (2, 50); \n     array. ...
    subq    $24, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(51,15): 2
    movq    $int_const_5, %rbx
    movq    %rbx, 8(%rsp) # actual #1
    # actual #2
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(51,18): 50
    movq    $int_const_6, %rbx
    movq    %rbx, 16(%rsp) # actual #2
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    movq    16(%rsp), %rdx
    # remove the register-loaded actuals from stack
    addq    $24, %rsp
    movq    16(%rdi), %rbx # ArrayAny_vtable
    movq    16(%rbx), %rbx # ArrayAny.set
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(52,5): array.set(3, 40)
    # actual #0
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(52,5): array
    movq    -16(%rbp), %rbx # array
    cmpq    $0, %rbx
    jne     .label_3 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $52, %rsi
    movq    $5, %rdx
    call    .Runtime.abort_dispatch
.label_3: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(52,14): (3, 40); \n     array. ...
    subq    $24, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(52,15): 3
    movq    $int_const_7, %rbx
    movq    %rbx, 8(%rsp) # actual #1
    # actual #2
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(52,18): 40
    movq    $int_const_8, %rbx
    movq    %rbx, 16(%rsp) # actual #2
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    movq    16(%rsp), %rdx
    # remove the register-loaded actuals from stack
    addq    $24, %rsp
    movq    16(%rdi), %rbx # ArrayAny_vtable
    movq    16(%rbx), %rbx # ArrayAny.set
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(53,5): array.set(4, 10)
    # actual #0
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(53,5): array
    movq    -16(%rbp), %rbx # array
    cmpq    $0, %rbx
    jne     .label_4 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $53, %rsi
    movq    $5, %rdx
    call    .Runtime.abort_dispatch
.label_4: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(53,14): (4, 10); \n      \n      ...
    subq    $24, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(53,15): 4
    movq    $int_const_9, %rbx
    movq    %rbx, 8(%rsp) # actual #1
    # actual #2
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(53,18): 10
    movq    $int_const_10, %rbx
    movq    %rbx, 16(%rsp) # actual #2
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    movq    16(%rsp), %rdx
    # remove the register-loaded actuals from stack
    addq    $24, %rsp
    movq    16(%rdi), %rbx # ArrayAny_vtable
    movq    16(%rbx), %rbx # ArrayAny.set
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(55,5): out_array(array)
    # actual #0
    movq    -8(%rbp), %rbx
    cmpq    $0, %rbx
    jne     .label_5 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $55, %rsi
    movq    $5, %rdx
    call    .Runtime.abort_dispatch
.label_5: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(55,14): (array); \n      \n      ...
    subq    $16, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(55,15): array
    movq    -16(%rbp), %rbx # array
    movq    %rbx, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %rbx # QuickSort_vtable
    movq    72(%rbx), %rbx # QuickSort.out_array
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(57,5): quicksort(array, 0,  ...
    # actual #0
    movq    -8(%rbp), %rbx
    cmpq    $0, %rbx
    jne     .label_7 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $57, %rsi
    movq    $5, %rdx
    call    .Runtime.abort_dispatch
.label_7: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(57,14): (array, 0, array.len ...
    subq    $32, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(57,15): array
    movq    -16(%rbp), %rbx # array
    movq    %rbx, 8(%rsp) # actual #1
    # actual #2
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(57,22): 0
    movq    $int_const_0, %rbx
    movq    %rbx, 16(%rsp) # actual #2
    # actual #3
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(57,25): array.length() - 1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(57,25): array.length()
    # actual #0
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(57,25): array
    movq    -16(%rbp), %rbx # array
    cmpq    $0, %rbx
    jne     .label_6 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $57, %rsi
    movq    $25, %rdx
    call    .Runtime.abort_dispatch
.label_6: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(57,37): () - 1); \n      \n      ...
    subq    $8, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    # remove the register-loaded actuals from stack
    addq    $8, %rsp
    movq    16(%rdi), %rbx # ArrayAny_vtable
    movq    24(%rbx), %rbx # ArrayAny.length
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(57,42): 1
    movq    $int_const_3, %rbx
    pushq   %r10
    movq    %r10, %rdi
    call    .Runtime.copy_object
    popq    %r10
    movq    %rax, %r10
    movq    24(%rbx), %rbx
    subq    %rbx, 24(%r10)
    movq    %r10, 24(%rsp) # actual #3
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    movq    16(%rsp), %rdx
    movq    24(%rsp), %rcx
    # remove the register-loaded actuals from stack
    addq    $32, %rsp
    movq    16(%rdi), %rbx # QuickSort_vtable
    movq    48(%rbx), %rbx # QuickSort.quicksort
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(59,5): out_array(array)
    # actual #0
    movq    -8(%rbp), %rbx
    cmpq    $0, %rbx
    jne     .label_8 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $59, %rsi
    movq    $5, %rdx
    call    .Runtime.abort_dispatch
.label_8: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(59,14): (array) \n   }; \n } \n  \n  ...
    subq    $16, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(59,15): array
    movq    -16(%rbp), %rbx # array
    movq    %rbx, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %rbx # QuickSort_vtable
    movq    72(%rbx), %rbx # QuickSort.out_array
    call    *%rbx
    movq    %rax, %r10 # returned value
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
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(4,3): def quicksort(array: ...
QuickSort.quicksort:
    pushq   %rbp
    movq    %rsp, %rbp
    subq    $80, %rsp
    # store actuals on the stack
    movq    %rdi, -8(%rbp)
    movq    %rsi, -16(%rbp)
    movq    %rdx, -24(%rbp)
    movq    %rcx, -32(%rbp)
    # store callee-saved regs on the stack
    movq    %rbx, -48(%rbp)
    movq    %r12, -56(%rbp)
    movq    %r13, -64(%rbp)
    movq    %r14, -72(%rbp)
    movq    %r15, -80(%rbp)
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(5,5): if (lo < hi) { \n      ...
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(5,9): lo
    movq    -24(%rbp), %rbx # lo
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(5,14): hi
    movq    -32(%rbp), %r10 # hi
    movq    24(%rbx), %rbx
    movq    24(%r10), %r10
    cmpq    %r10, %rbx
    jl    .label_12 # true branch
    # false branch
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(9,12): ()
    movq    $Unit_value, %rbx
    movq    %rbx, %r11
    jmp     .label_13 # done
.label_12: # true branch
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(6,7): var p: Int = partiti ...
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(6,20): partition(array, lo, ...
    # actual #0
    movq    -8(%rbp), %rbx
    cmpq    $0, %rbx
    jne     .label_9 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $6, %rsi
    movq    $20, %rdx
    call    .Runtime.abort_dispatch
.label_9: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(6,29): (array, lo, hi); \n    ...
    subq    $32, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(6,30): array
    movq    -16(%rbp), %rbx # array
    movq    %rbx, 8(%rsp) # actual #1
    # actual #2
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(6,37): lo
    movq    -24(%rbp), %rbx # lo
    movq    %rbx, 16(%rsp) # actual #2
    # actual #3
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(6,41): hi
    movq    -32(%rbp), %rbx # hi
    movq    %rbx, 24(%rsp) # actual #3
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    movq    16(%rsp), %rdx
    movq    24(%rsp), %rcx
    # remove the register-loaded actuals from stack
    addq    $32, %rsp
    movq    16(%rdi), %rbx # QuickSort_vtable
    movq    56(%rbx), %rbx # QuickSort.partition
    call    *%rbx
    movq    %rax, %r10 # returned value
    movq    %r10, -40(%rbp) # p
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(7,7): quicksort(array, lo, ...
    # actual #0
    movq    -8(%rbp), %rbx
    cmpq    $0, %rbx
    jne     .label_10 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $7, %rsi
    movq    $7, %rdx
    call    .Runtime.abort_dispatch
.label_10: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(7,16): (array, lo, p - 1); ...
    subq    $32, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(7,17): array
    movq    -16(%rbp), %rbx # array
    movq    %rbx, 8(%rsp) # actual #1
    # actual #2
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(7,24): lo
    movq    -24(%rbp), %rbx # lo
    movq    %rbx, 16(%rsp) # actual #2
    # actual #3
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(7,28): p - 1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(7,28): p
    movq    -40(%rbp), %rbx # p
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(7,32): 1
    movq    $int_const_3, %r10
    pushq   %r10
    movq    %rbx, %rdi
    call    .Runtime.copy_object
    popq    %r10
    movq    %rax, %rbx
    movq    24(%r10), %r10
    subq    %r10, 24(%rbx)
    movq    %rbx, 24(%rsp) # actual #3
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    movq    16(%rsp), %rdx
    movq    24(%rsp), %rcx
    # remove the register-loaded actuals from stack
    addq    $32, %rsp
    movq    16(%rdi), %rbx # QuickSort_vtable
    movq    48(%rbx), %rbx # QuickSort.quicksort
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(8,7): quicksort(array, p + ...
    # actual #0
    movq    -8(%rbp), %rbx
    cmpq    $0, %rbx
    jne     .label_11 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $8, %rsi
    movq    $7, %rdx
    call    .Runtime.abort_dispatch
.label_11: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(8,16): (array, p + 1, hi) \n  ...
    subq    $32, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(8,17): array
    movq    -16(%rbp), %rbx # array
    movq    %rbx, 8(%rsp) # actual #1
    # actual #2
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(8,24): p + 1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(8,24): p
    movq    -40(%rbp), %rbx # p
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(8,28): 1
    movq    $int_const_3, %r10
    pushq   %r10
    movq    %r10, %rdi
    call    .Runtime.copy_object
    popq    %r10
    movq    %rax, %r10
    movq    24(%rbx), %rbx
    addq    %rbx, 24(%r10)
    movq    %r10, 16(%rsp) # actual #2
    # actual #3
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(8,31): hi
    movq    -32(%rbp), %rbx # hi
    movq    %rbx, 24(%rsp) # actual #3
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    movq    16(%rsp), %rdx
    movq    24(%rsp), %rcx
    # remove the register-loaded actuals from stack
    addq    $32, %rsp
    movq    16(%rdi), %rbx # QuickSort_vtable
    movq    48(%rbx), %rbx # QuickSort.quicksort
    call    *%rbx
    movq    %rax, %r10 # returned value
    movq    %r10, %r11
.label_13: # done
    movq    %r11, %rax
    # restore callee-saved regs
    movq    -48(%rbp), %rbx
    movq    -56(%rbp), %r12
    movq    -64(%rbp), %r13
    movq    -72(%rbp), %r14
    movq    -80(%rbp), %r15
    # restore the caller's frame
    movq    %rbp, %rsp
    popq    %rbp
    ret
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(12,3): def partition(array: ...
QuickSort.partition:
    pushq   %rbp
    movq    %rsp, %rbp
    subq    $112, %rsp
    # store actuals on the stack
    movq    %rdi, -8(%rbp)
    movq    %rsi, -16(%rbp)
    movq    %rdx, -24(%rbp)
    movq    %rcx, -32(%rbp)
    # store callee-saved regs on the stack
    movq    %rbx, -80(%rbp)
    movq    %r12, -88(%rbp)
    movq    %r13, -96(%rbp)
    movq    %r14, -104(%rbp)
    movq    %r15, -112(%rbp)
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(13,5): var pivot: Int = arr ...
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(13,22): array.get(lo) match  ...
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(13,22): array.get(lo)
    # actual #0
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(13,22): array
    movq    -16(%rbp), %rbx # array
    cmpq    $0, %rbx
    jne     .label_14 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $13, %rsi
    movq    $22, %rdx
    call    .Runtime.abort_dispatch
.label_14: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(13,31): (lo) match { case i: ...
    subq    $16, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(13,32): lo
    movq    -24(%rbp), %rbx # lo
    movq    %rbx, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %rbx # ArrayAny_vtable
    movq    8(%rbx), %rbx # ArrayAny.get
    call    *%rbx
    movq    %rax, %r10 # returned value
    # handle null
    cmpq    $0, %r10
    jne     .label_16 # match init
    movq    $str_const_1, %rdi
    movq    $13, %rsi
    movq    $22, %rdx
    movq    %r10, %rcx
    call    .Runtime.abort_match
.label_16: # match init
    movq    %r10, -48(%rbp) # the expression's value
    movq    (%r10), %rbx # tag
.label_17: # no match?
    cmpq    $-1, %rbx
    jne     .label_18 # try match
    movq    $str_const_1, %rdi
    movq    $13, %rsi
    movq    $22, %rdx
    movq    %r10, %rcx
    call    .Runtime.abort_match
.label_18: # try match
    cmpq    $2, %rbx
    je      .label_15 # Int
    salq    $3, %rbx # multiply by 8
    movq    class_parent_table(%rbx), %rbx # the parent's tag
    jmp     .label_17 # no match?
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(13,44): case i: Int => i
.label_15: # case Int
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(13,59): i
    movq    -48(%rbp), %r10 # i
    movq    %r10, %rbx
    jmp     .label_19 # end match
.label_19: # end match
    movq    %rbx, -40(%rbp) # pivot
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(14,5): var p: Int = lo
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(14,18): lo
    movq    -24(%rbp), %rbx # lo
    movq    %rbx, -56(%rbp) # p
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(15,5): var i: Int = lo + 1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(15,18): lo + 1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(15,18): lo
    movq    -24(%rbp), %rbx # lo
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(15,23): 1
    movq    $int_const_3, %r10
    pushq   %r10
    movq    %r10, %rdi
    call    .Runtime.copy_object
    popq    %r10
    movq    %rax, %r10
    movq    24(%rbx), %rbx
    addq    %rbx, 24(%r10)
    movq    %r10, -64(%rbp) # i
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(16,5): while (i <= hi) { \n   ...
.label_29: # while cond
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(16,12): i <= hi
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(16,12): i
    movq    -64(%rbp), %r10 # i
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(16,17): hi
    movq    -32(%rbp), %r11 # hi
    movq    24(%r10), %r10
    movq    24(%r11), %r11
    cmpq    %r11, %r10
    jle    .label_30 # true branch
    # false branch
    movq    $Unit_value, %rbx # unit
    jmp     .label_31 # done
.label_30: # true branch
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(17,7): if (((array.get(i))  ...
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(17,12): (array.get(i)) match ...
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(17,13): array.get(i)
    pushq   %r11
    # actual #0
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(17,13): array
    movq    -16(%rbp), %rbx # array
    cmpq    $0, %rbx
    jne     .label_21 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $17, %rsi
    movq    $13, %rdx
    call    .Runtime.abort_dispatch
.label_21: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(17,22): (i)) match { case i: ...
    subq    $16, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(17,23): i
    movq    -64(%rbp), %rbx # i
    movq    %rbx, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %rbx # ArrayAny_vtable
    movq    8(%rbx), %rbx # ArrayAny.get
    call    *%rbx
    popq    %r11
    movq    %rax, %r10 # returned value
    # handle null
    cmpq    $0, %r10
    jne     .label_23 # match init
    movq    $str_const_1, %rdi
    movq    $17, %rsi
    movq    $12, %rdx
    movq    %r10, %rcx
    call    .Runtime.abort_match
.label_23: # match init
    movq    %r10, -72(%rbp) # the expression's value
    movq    (%r10), %rbx # tag
.label_24: # no match?
    cmpq    $-1, %rbx
    jne     .label_25 # try match
    movq    $str_const_1, %rdi
    movq    $17, %rsi
    movq    $12, %rdx
    movq    %r10, %rcx
    call    .Runtime.abort_match
.label_25: # try match
    cmpq    $2, %rbx
    je      .label_22 # Int
    salq    $3, %rbx # multiply by 8
    movq    class_parent_table(%rbx), %rbx # the parent's tag
    jmp     .label_24 # no match?
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(17,35): case i: Int => i
.label_22: # case Int
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(17,50): i
    movq    -72(%rbp), %r10 # i
    movq    %r10, %rbx
    jmp     .label_26 # end match
.label_26: # end match
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(17,58): pivot
    movq    -40(%rbp), %r10 # pivot
    movq    24(%rbx), %rbx
    movq    24(%r10), %r10
    cmpq    %r10, %rbx
    jle    .label_27 # true branch
    # false branch
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(20,9): ()
    movq    $Unit_value, %rbx
    movq    %rbx, %r11
    jmp     .label_28 # done
.label_27: # true branch
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(18,9): array_swap(array, i, ...
    # actual #0
    movq    -8(%rbp), %rbx
    cmpq    $0, %rbx
    jne     .label_20 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $18, %rsi
    movq    $9, %rdx
    call    .Runtime.abort_dispatch
.label_20: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(18,19): (array, i, { p = p + ...
    subq    $32, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(18,20): array
    movq    -16(%rbp), %rbx # array
    movq    %rbx, 8(%rsp) # actual #1
    # actual #2
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(18,27): i
    movq    -64(%rbp), %rbx # i
    movq    %rbx, 16(%rsp) # actual #2
    # actual #3
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(18,32): p = p + 1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(18,36): p + 1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(18,36): p
    movq    -56(%rbp), %rbx # p
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(18,40): 1
    movq    $int_const_3, %r10
    pushq   %r10
    movq    %r10, %rdi
    call    .Runtime.copy_object
    popq    %r10
    movq    %rax, %r10
    movq    24(%rbx), %rbx
    addq    %rbx, 24(%r10)
    movq    %r10, -56(%rbp) # p
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(18,43): p
    movq    -56(%rbp), %rbx # p
    movq    %rbx, 24(%rsp) # actual #3
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    movq    16(%rsp), %rdx
    movq    24(%rsp), %rcx
    # remove the register-loaded actuals from stack
    addq    $32, %rsp
    movq    16(%rdi), %rbx # QuickSort_vtable
    movq    64(%rbx), %rbx # QuickSort.array_swap
    call    *%rbx
    movq    %rax, %r10 # returned value
    movq    %r10, %r11
.label_28: # done
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(21,7): i = i + 1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(21,11): i + 1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(21,11): i
    movq    -64(%rbp), %rbx # i
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(21,15): 1
    movq    $int_const_3, %r10
    pushq   %r10
    movq    %r10, %rdi
    call    .Runtime.copy_object
    popq    %r10
    movq    %rax, %r10
    movq    24(%rbx), %rbx
    addq    %rbx, 24(%r10)
    movq    %r10, -64(%rbp) # i
    jmp     .label_29 # while cond
.label_31: # done
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(24,5): array_swap(array, p, ...
    # actual #0
    movq    -8(%rbp), %rbx
    cmpq    $0, %rbx
    jne     .label_32 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $24, %rsi
    movq    $5, %rdx
    call    .Runtime.abort_dispatch
.label_32: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(24,15): (array, p, lo); \n     ...
    subq    $32, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(24,16): array
    movq    -16(%rbp), %rbx # array
    movq    %rbx, 8(%rsp) # actual #1
    # actual #2
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(24,23): p
    movq    -56(%rbp), %rbx # p
    movq    %rbx, 16(%rsp) # actual #2
    # actual #3
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(24,26): lo
    movq    -24(%rbp), %rbx # lo
    movq    %rbx, 24(%rsp) # actual #3
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    movq    16(%rsp), %rdx
    movq    24(%rsp), %rcx
    # remove the register-loaded actuals from stack
    addq    $32, %rsp
    movq    16(%rdi), %rbx # QuickSort_vtable
    movq    64(%rbx), %rbx # QuickSort.array_swap
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(25,5): p
    movq    -56(%rbp), %rbx # p
    movq    %rbx, %rax
    # restore callee-saved regs
    movq    -80(%rbp), %rbx
    movq    -88(%rbp), %r12
    movq    -96(%rbp), %r13
    movq    -104(%rbp), %r14
    movq    -112(%rbp), %r15
    # restore the caller's frame
    movq    %rbp, %rsp
    popq    %rbp
    ret
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(28,3): def array_swap(array ...
QuickSort.array_swap:
    pushq   %rbp
    movq    %rsp, %rbp
    subq    $80, %rsp
    # store actuals on the stack
    movq    %rdi, -8(%rbp)
    movq    %rsi, -16(%rbp)
    movq    %rdx, -24(%rbp)
    movq    %rcx, -32(%rbp)
    # store callee-saved regs on the stack
    movq    %rbx, -48(%rbp)
    movq    %r12, -56(%rbp)
    movq    %r13, -64(%rbp)
    movq    %r14, -72(%rbp)
    movq    %r15, -80(%rbp)
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(29,5): var tmp: Any = array ...
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(29,20): array.get(p)
    # actual #0
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(29,20): array
    movq    -16(%rbp), %rbx # array
    cmpq    $0, %rbx
    jne     .label_33 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $29, %rsi
    movq    $20, %rdx
    call    .Runtime.abort_dispatch
.label_33: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(29,29): (p); \n     array.set( ...
    subq    $16, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(29,30): p
    movq    -24(%rbp), %rbx # p
    movq    %rbx, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %rbx # ArrayAny_vtable
    movq    8(%rbx), %rbx # ArrayAny.get
    call    *%rbx
    movq    %rax, %r10 # returned value
    movq    %r10, -40(%rbp) # tmp
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(30,5): array.set(p, array.g ...
    # actual #0
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(30,5): array
    movq    -16(%rbp), %rbx # array
    cmpq    $0, %rbx
    jne     .label_35 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $30, %rsi
    movq    $5, %rdx
    call    .Runtime.abort_dispatch
.label_35: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(30,14): (p, array.get(q)); \n  ...
    subq    $24, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(30,15): p
    movq    -24(%rbp), %rbx # p
    movq    %rbx, 8(%rsp) # actual #1
    # actual #2
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(30,18): array.get(q)
    # actual #0
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(30,18): array
    movq    -16(%rbp), %rbx # array
    cmpq    $0, %rbx
    jne     .label_34 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $30, %rsi
    movq    $18, %rdx
    call    .Runtime.abort_dispatch
.label_34: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(30,27): (q)); \n     array.set ...
    subq    $16, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(30,28): q
    movq    -32(%rbp), %rbx # q
    movq    %rbx, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %rbx # ArrayAny_vtable
    movq    8(%rbx), %rbx # ArrayAny.get
    call    *%rbx
    movq    %rax, %r10 # returned value
    movq    %r10, 16(%rsp) # actual #2
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    movq    16(%rsp), %rdx
    # remove the register-loaded actuals from stack
    addq    $24, %rsp
    movq    16(%rdi), %rbx # ArrayAny_vtable
    movq    16(%rbx), %rbx # ArrayAny.set
    call    *%rbx
    movq    %rax, %r10 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(31,5): array.set(q, tmp)
    # actual #0
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(31,5): array
    movq    -16(%rbp), %rbx # array
    cmpq    $0, %rbx
    jne     .label_36 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $31, %rsi
    movq    $5, %rdx
    call    .Runtime.abort_dispatch
.label_36: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(31,14): (q, tmp) \n   }; \n      ...
    subq    $24, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(31,15): q
    movq    -32(%rbp), %rbx # q
    movq    %rbx, 8(%rsp) # actual #1
    # actual #2
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(31,18): tmp
    movq    -40(%rbp), %rbx # tmp
    movq    %rbx, 16(%rsp) # actual #2
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    movq    16(%rsp), %rdx
    # remove the register-loaded actuals from stack
    addq    $24, %rsp
    movq    16(%rdi), %rbx # ArrayAny_vtable
    movq    16(%rbx), %rbx # ArrayAny.set
    call    *%rbx
    movq    %rax, %r10 # returned value
    movq    %r10, %rax
    # restore callee-saved regs
    movq    -48(%rbp), %rbx
    movq    -56(%rbp), %r12
    movq    -64(%rbp), %r13
    movq    -72(%rbp), %r14
    movq    -80(%rbp), %r15
    # restore the caller's frame
    movq    %rbp, %rsp
    popq    %rbp
    ret
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(34,3): def out_array(array: ...
QuickSort.out_array:
    pushq   %rbp
    movq    %rsp, %rbp
    subq    $80, %rsp
    # store actuals on the stack
    movq    %rdi, -8(%rbp)
    movq    %rsi, -16(%rbp)
    # store callee-saved regs on the stack
    movq    %rbx, -40(%rbp)
    movq    %r12, -48(%rbp)
    movq    %r13, -56(%rbp)
    movq    %r14, -64(%rbp)
    movq    %r15, -72(%rbp)
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(35,5): var i: Int = 0
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(35,18): 0
    movq    $int_const_0, %rbx
    movq    %rbx, -24(%rbp) # i
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(36,5): while (i < array.len ...
.label_45: # while cond
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(36,12): i < array.length()
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(36,12): i
    movq    -24(%rbp), %r10 # i
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(36,16): array.length()
    pushq   %r10
    # actual #0
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(36,16): array
    movq    -16(%rbp), %r11 # array
    cmpq    $0, %r11
    jne     .label_46 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $36, %rsi
    movq    $16, %rdx
    call    .Runtime.abort_dispatch
.label_46: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(36,28): ()) { \n       array.g ...
    subq    $8, %rsp
    movq    %r11, 0(%rsp) # actual #0
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    # remove the register-loaded actuals from stack
    addq    $8, %rsp
    movq    16(%rdi), %r11 # ArrayAny_vtable
    movq    24(%r11), %r11 # ArrayAny.length
    call    *%r11
    popq    %r10
    movq    %rax, %r12 # returned value
    movq    24(%r10), %r10
    movq    24(%r12), %r12
    cmpq    %r12, %r10
    jl    .label_47 # true branch
    # false branch
    movq    $Unit_value, %rbx # unit
    jmp     .label_48 # done
.label_47: # true branch
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(37,7): array.get(i) match { ...
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(37,7): array.get(i)
    # actual #0
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(37,7): array
    movq    -16(%rbp), %rbx # array
    cmpq    $0, %rbx
    jne     .label_37 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $37, %rsi
    movq    $7, %rdx
    call    .Runtime.abort_dispatch
.label_37: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(37,16): (i) match { \n         ...
    subq    $16, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(37,17): i
    movq    -24(%rbp), %rbx # i
    movq    %rbx, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %rbx # ArrayAny_vtable
    movq    8(%rbx), %rbx # ArrayAny.get
    call    *%rbx
    movq    %rax, %r10 # returned value
    # handle null
    cmpq    $0, %r10
    jne     .label_39 # match init
    movq    $str_const_1, %rdi
    movq    $37, %rsi
    movq    $7, %rdx
    movq    %r10, %rcx
    call    .Runtime.abort_match
.label_39: # match init
    movq    %r10, -32(%rbp) # the expression's value
    movq    (%r10), %rbx # tag
.label_40: # no match?
    cmpq    $-1, %rbx
    jne     .label_41 # try match
    movq    $str_const_1, %rdi
    movq    $37, %rsi
    movq    $7, %rdx
    movq    %r10, %rcx
    call    .Runtime.abort_match
.label_41: # try match
    cmpq    $2, %rbx
    je      .label_38 # Int
    salq    $3, %rbx # multiply by 8
    movq    class_parent_table(%rbx), %rbx # the parent's tag
    jmp     .label_40 # no match?
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(38,9): case i: Int => out_i ...
.label_38: # case Int
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(38,24): out_int(i)
    # actual #0
    movq    -8(%rbp), %r10
    cmpq    $0, %r10
    jne     .label_43 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $38, %rsi
    movq    $24, %rdx
    call    .Runtime.abort_dispatch
.label_43: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(38,31): (i); out_string(" ") ...
    subq    $16, %rsp
    movq    %r10, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(38,32): i
    movq    -32(%rbp), %r10 # i
    movq    %r10, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %r10 # QuickSort_vtable
    movq    16(%r10), %r10 # QuickSort.out_int
    call    *%r10
    movq    %rax, %r11 # returned value
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(38,36): out_string(" ")
    # actual #0
    movq    -8(%rbp), %r10
    cmpq    $0, %r10
    jne     .label_44 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $38, %rsi
    movq    $36, %rdx
    call    .Runtime.abort_dispatch
.label_44: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(38,46): (" ") \n       }; \n     ...
    subq    $16, %rsp
    movq    %r10, 0(%rsp) # actual #0
    # actual #1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(38,47): " "
    movq    $str_const_2, %r10
    movq    %r10, 8(%rsp) # actual #1
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    movq    8(%rsp), %rsi
    # remove the register-loaded actuals from stack
    addq    $16, %rsp
    movq    16(%rdi), %r10 # QuickSort_vtable
    movq    8(%r10), %r10 # QuickSort.out_string
    call    *%r10
    movq    %rax, %r11 # returned value
    movq    %r11, %rbx
    jmp     .label_42 # end match
.label_42: # end match
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(41,7): i = i + 1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(41,11): i + 1
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(41,11): i
    movq    -24(%rbp), %rbx # i
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(41,15): 1
    movq    $int_const_3, %r10
    pushq   %r10
    movq    %r10, %rdi
    call    .Runtime.copy_object
    popq    %r10
    movq    %rax, %r10
    movq    24(%rbx), %rbx
    addq    %rbx, 24(%r10)
    movq    %r10, -24(%rbp) # i
    jmp     .label_45 # while cond
.label_48: # done
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(44,5): out_nl()
    # actual #0
    movq    -8(%rbp), %rbx
    cmpq    $0, %rbx
    jne     .label_49 # the receiver is some
    movq    $str_const_1, %rdi
    movq    $44, %rsi
    movq    $5, %rdx
    call    .Runtime.abort_dispatch
.label_49: # the receiver is some
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(44,11): () \n   }; \n      \n   { ...
    subq    $8, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    # remove the register-loaded actuals from stack
    addq    $8, %rsp
    movq    16(%rdi), %rbx # QuickSort_vtable
    movq    24(%rbx), %rbx # QuickSort.out_nl
    call    *%rbx
    movq    %rax, %r10 # returned value
    movq    %r10, %rax
    # restore callee-saved regs
    movq    -40(%rbp), %rbx
    movq    -48(%rbp), %r12
    movq    -56(%rbp), %r13
    movq    -64(%rbp), %r14
    movq    -72(%rbp), %r15
    # restore the caller's frame
    movq    %rbp, %rsp
    popq    %rbp
    ret
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(63,7): Main
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
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(65,5): new QuickSort()
    movq    $QuickSort_proto_obj, %rdi
    call    .Runtime.copy_object
    movq    %rax, %rbx
    # ../../src/Tests/CoolPrograms/Runtime/QuickSort.cool(65,18): () \n   }; \n } \n  \n // DI ...
    subq    $8, %rsp
    movq    %rbx, 0(%rsp) # actual #0
    # load up to 6 first actuals into regs
    movq    0(%rsp), %rdi
    # remove the register-loaded actuals from stack
    addq    $8, %rsp
    call    QuickSort..ctor
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
