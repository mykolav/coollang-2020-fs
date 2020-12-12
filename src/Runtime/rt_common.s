
########################################
# Data
########################################
    .data

########################################
# Strings
########################################
ascii_aborted_from:             .ascii "Aborted from "

ascii_lparen:                   .ascii "("
ascii_comma:                    .ascii ","
ascii_rparen_colon_space:       .ascii "): "

ascii_dispatch_to_null:         .ascii "Dispatch to null"

ascii_no_match:                 .ascii "No match for "
ascii_null:                     .ascii "null"

ascii_index:                    .ascii ": Index "
ascii_is_out_of_range:          .ascii " is out of range"

ascii_colon_actual_apostrophe:  .ascii ": Actual '"
ascii_apostrophe_is_null:       .ascii "' is null"

ascii_string_substring_start_index:
                                .ascii "String.substring: start_index "
ascii_gt_end_index:             .ascii " > end_index "

ascii_arrayany_get:             .ascii "ArrayAny.get"
ascii_arrayany_set:             .ascii "ArrayAny.set"

ascii_string_concat:            .ascii "String.concat"
ascii_suffix:                   .ascii "suffix"

ascii_string_substring:         .ascii "String.substring"

    .global ascii_out_of_memory
ascii_out_of_memory:            .ascii "Out of memory"

ascii_input_too_long:           .ascii "IO.in_int: Input string is too long"
ascii_input_not_digit:          .ascii "IO.in_int: Input string contains a char that is not a digit"

########################################
# Virtual tables
########################################
    .global Any_vtable
Any_vtable:
    .quad Any.abort

    .global Unit_vtable
Unit_vtable:
    .quad Any.abort

    .global Int_vtable
Int_vtable:
    .quad Any.abort

    .global String_vtable
String_vtable:
    .quad Any.abort
    .quad String.length
    .quad String.concat
    .quad String.substring

    .global Boolean_vtable
Boolean_vtable:
    .quad Any.abort

    .global ArrayAny_vtable
ArrayAny_vtable:
    .quad Any.abort
    .quad ArrayAny.get
    .quad ArrayAny.set
    .quad ArrayAny.length

    .global IO_vtable
IO_vtable:
    .quad Any.abort
    .quad IO.out_string
    .quad IO.out_int
    .quad IO.out_nl
    .quad IO.in_string
    .quad IO.in_int

########################################
# Tags
########################################
    .set Unit_tag,      1
    .set Int_tag,       2
    .set String_tag,    3
    .set Boolean_tag,   4
    .set ArrayAny_tag,  5
    .set IO_tag,        6

########################################
# Prototype objects
########################################
    .quad -1
    .global Int_proto_obj
Int_proto_obj:
    .quad Int_tag    # tag
    .quad 4          # size in quads
    .quad Int_vtable
    .quad 0          # value

    .quad -1
    .global Int_0
Int_0:
    .quad Int_tag    # tag
    .quad 4          # size in quads
    .quad Int_vtable
    .quad 0          # value

    .quad -1
    .global Unit_value
Unit_value:
    .quad Unit_tag    # tag
    .quad 3           # size in quads
    .quad Unit_vtable
    
    .quad -1
    .global String_empty
String_empty:
    .quad String_tag    # tag
    .quad 5             # size in quads
    .quad String_vtable
    .quad Int_0         # length
    .quad 0             # terminating 0 and 
                        # 16 bytes boundary padding
    
    .quad -1
    .global Boolean_true
Boolean_true:
    .quad Boolean_tag    # tag
    .quad 4              # size in quads
    .quad Boolean_vtable
    .quad 1              # value

    .quad -1
    .global Boolean_false
Boolean_false:
    .quad Boolean_tag    # tag
    .quad 4              # size in quads
    .quad Boolean_vtable
    .quad 0              # value
    
    .quad -1
    .global IO_proto_obj
IO_proto_obj:
    .quad IO_tag         # tag
    .quad 3              # size in quads
    .quad IO_vtable

########################################
# Text
########################################
    .text

    .set OBJ_TAG,   0
    .set OBJ_SIZE,  8
    .set OBJ_VTAB,  16
    
    .set STR_LEN,   24
    .set STR_VAL,   32

    .set ARR_LEN,   24
    .set ARR_ITEMS, 32

    .set BOOL_VAL,  24

    .set INT_VAL,   24

########################################
# .Runtime
########################################

#
#  .Runtime.abort_dispatch
#
#      file   in %rdi
#.     line   in %rsi
#      column in %rdx
#  
#  Prints "{file}({line},{column}): Dispatch to null" and exits process.
#  Does not return.
#
    .global .Runtime.abort_dispatch
.Runtime.abort_dispatch:
    # pushq %rbp
    # movq %rsp, %rbp

    # Print "{file}({line},{column}): "
    call    .Runtime.out_location
    # Print "Dispatch to null"
    movq    $ascii_dispatch_to_null, %rdi
    movq    $16, %rsi
    call    .Platform.out_string
    # Print new line
    call    .Runtime.out_nl

    # Exit the process
    call    .Platform.exit_process

#
#  .Runtime.abort_match
#
#      file     %rdi
#.     line     %rsi
#      column   %rdx
#      object   %rcx
#  
#  Prints "{file}({line},{column}): No match for {class}" and exits process.
#  Does not return.
#
    .global .Runtime.abort_match
.Runtime.abort_match:
    pushq %rbp
    movq %rsp, %rbp

    subq    $(8 + 8), %rsp # 16 bytes boundary padding + object
    movq    %rcx, -16(%rbp) # store %rcx, as it's volatile under MS x64

    # Print "{file}({line},{column}): "
    call    .Runtime.out_location
    # Print "No match for "
    movq    $ascii_no_match, %rdi
    movq    $13, %rsi
    call    .Platform.out_string
    # Print "{class}"
    movq    -16(%rbp), %rdi
    call    .Runtime.out_type_name
    # Print new line
    call    .Runtime.out_nl

    # Exit the process
    call    .Platform.exit_process

#
#  .Runtime.abort_index
#
#      method_name      %rdi
#      method_name_len  %rsi
#      index            %rdx
#  
#  Prints "{method_name}: Index {index} is out of range" and exits process.
#  Does not return.
#
    .global .Runtime.abort_index
.Runtime.abort_index:
    pushq   %rbp
    movq    %rsp, %rbp

    subq    $(8 + 8), %rsp # 16 bytes boundary padding + index
    movq    %rdx, -16(%rbp) # index

    # Print "{method_name}"
    call    .Platform.out_string
    # Print ": Index "
    movq    $ascii_index, %rdi
    movq    $8, %rsi
    call    .Platform.out_string
    # Print "{index}"
    movq    -16(%rbp), %rdi # index
    call    .Runtime.out_int
    # Print " is out of range"
    movq    $ascii_is_out_of_range, %rdi
    movq    $16, %rsi
    call    .Platform.out_string
    # Print new line
    call    .Runtime.out_nl

    # Exit the process
    call    .Platform.exit_process

#
#  .Runtime.abort_arg_null
#
#      method name     %rdi
#      method_name_len %rsi
#      arg_name        %rdx
#      arg_name_len    %rcx
#  
#  Prints "{method_name}: Actual '{arg_name}' is null" and exits process.
#  Does not return.
#
    .global .Runtime.abort_arg_null
.Runtime.abort_arg_null:
    pushq   %rbp
    movq    %rsp, %rbp

    subq    $(8 + 8), %rsp # arg_name +
                           # arg_name_len

    movq    %rdx, -8(%rbp)  # arg_name
    movq    %rcx, -16(%rbp) # arg_name_len

    # Print "{method_name}"
    call    .Platform.out_string
    # Print ": Actual '"
    movq    $ascii_colon_actual_apostrophe, %rdi
    movq    $10, %rsi
    call    .Platform.out_string
    # Print "{arg_name}"
    movq    -8(%rbp), %rdi  # arg_name
    movq    -16(%rbp), %rsi # arg_name_len
    call    .Platform.out_string
    # Print "' is null"
    movq    $ascii_apostrophe_is_null, %rdi
    movq    $9, %rsi
    call    .Platform.out_string
    # Print new line
    call    .Runtime.out_nl

    # Exit the process
    call    .Platform.exit_process

#
#  .Runtime.out_location
#
#      file   in %rdi
#.     line   in %rsi
#      column in %rdx
#  
#  Prints "{file}({line},{column}): ".
#
    .global .Runtime.out_location
.Runtime.out_location:
    pushq %rbp
    movq %rsp, %rbp

    subq    $(8 + 8 + 8 + 8), %rsp # 16 bytes boundary padding +
                                   # file +
                                   #.line + 
                                   # colum

    movq    %rdi, -16(%rbp) # file
    movq    %rsi, -24(%rbp) # line
    movq    %rdx, -32(%rbp) # column
  
    # Print "{file}" 
    movq    -16(%rbp), %rdi # file
    movq    STR_LEN(%rdi), %rsi
    movq    INT_VAL(%rsi), %rsi 
    leaq    STR_VAL(%rdi), %rdi
    call    .Platform.out_string

    # Print "(" 
    movq    $ascii_lparen, %rdi
    movq    $1, %rsi
    call    .Platform.out_string

    # Print "{line}" 
    movq    -24(%rbp), %rdi # line
    call    .Runtime.out_int

    # Print ","
    movq    $ascii_comma, %rdi
    movq    $1, %rsi
    call    .Platform.out_string

    # Print "{column}" 
    movq    -32(%rbp), %rdi # line
    call    .Runtime.out_int

    # Print "): "
    movq    $ascii_rparen_colon_space, %rdi
    movq    $3, %rsi
    call    .Platform.out_string

    movq %rbp, %rsp
    popq %rbp

    ret

#
#  .Runtime.out_type_name
#
#      object   %rdi
#  
#  Prints "{class}" and exits.
#
    .global .Runtime.out_type_name
.Runtime.out_type_name:
    pushq   %rbp
    movq    %rsp, %rbp

    # handle null
    cmpq    $0, %rdi
    jne     .Runtime.out_type_name.is_some

    movq    $ascii_null, %rdi
    movq    $4, %rsi
    jmp     .Runtime.out_type_name.out_string

.Runtime.out_type_name.is_some:
    movq    OBJ_TAG(%rdi), %rdi
    salq    $3, %rdi # multiply the tag by 8
                     # to get the offset in 'class_name_table'
    addq    $class_name_table, %rdi
    movq    (%rdi), %rdi

    movq    STR_LEN(%rdi), %rsi
    movq    INT_VAL(%rsi), %rsi
    leaq    STR_VAL(%rdi), %rdi

.Runtime.out_type_name.out_string:
    call    .Platform.out_string
 
    movq    %rbp, %rsp
    popq    %rbp
 
    ret

#
#  .Runtime.copy_object
#
#      a prototype in %rdi
#  
#  Allocates memory on heap and copies the prototype.
#  Returns a pointer to the copy in %rax.
#
    .global .Runtime.copy_object
.Runtime.copy_object:
    pushq   %rbp
    movq    %rsp, %rbp

    subq    $(8 + 8), %rsp  # 16 bytes boundary padding + prototype
    movq    %rdi, -16(%rbp) # store the prototype

    movq    OBJ_SIZE(%rdi), %rdi # size in quads
    incq    %rdi                 # add a quad for the eyecatch
    call    .Platform.alloc

    movq    $-1, (%rax) # write the eyecatch value
    addq    $8, %rax    # move ptr to the beginning of object

    # %rdi - src
    # %rdx - src_end
    # %rsi - dst
    # %rcx - tmp

    # dst
    movq    %rax, %rsi

    # src
    movq    -16(%rbp), %rdi # prototype

    # src_end
    movq    OBJ_SIZE(%rdi), %rdx
    salq    $3, %rdx
    addq    %rdi, %rdx

    jmp     .Runtime.copy_object.loop_cond

.Runtime.copy_object.loop_body:
    movq    (%rdi), %rcx
    movq    %rcx, (%rsi)
    addq    $8, %rdi
    addq    $8, %rsi

.Runtime.copy_object.loop_cond:
    cmpq    %rdi, %rdx
    jne     .Runtime.copy_object.loop_body

    movq    %rbp, %rsp
    popq    %rbp

    ret

#
#  Polymorphic equality testing function:
#  Two objects are equal if they
#    - are both null (pointer equality)
#    - are identical (pointer equality)
#    - have same tag and are of type BOOL, STRING, INT, UNIT 
#      and contain the same data.
#
#  INPUT: 
#      obj1 in %rdi
#      obj2 in %rsi
#  OUTPUT: 
#      $1 in %rax, if the objects are equal.
#      $0 in %rax, if the objects are unequal.
#
    .global .Runtime.are_equal
.Runtime.are_equal:
    # pushq   %rbp
    # movq    %rsp, %rbp

    # pointer equality
    cmpq    %rdi, %rsi
    je      .Runtime.are_equal.true

    # get tags
    movq    (%rdi), %rdx
    movq    (%rsi), %rcx

    #. if the tags are not equal, the objects are unequal
    cmpq    %rdx, %rcx
    jne     .Runtime.are_equal.false

    cmpq    $Int_tag, %rdx
    je      .Runtime.are_equal.int

    cmpq    $Boolean_tag, %rdx
    je      .Runtime.are_equal.int

    cmpq    $Unit_tag, %rdx
    je      .Runtime.are_equal.unit

    cmpq    $String_tag, %rdx
    jne     .Runtime.are_equal.false    # Not a primitive type, return false

#.Runtime.are_equal.string:
    movq    STR_LEN(%rdi), %rdx
    movq    INT_VAL(%rdx), %rdx
    movq    STR_LEN(%rsi), %rcx
    movq    INT_VAL(%rcx), %rcx
    
    # If strings have different lengths, they cannot be equal
    cmpq    %rdx, %rcx
    jne     .Runtime.are_equal.false
    
    # If both strings' lengths are 0, they are equal
    cmpq    $0, %rdx
    je      .Runtime.are_equal.true

    leaq    STR_VAL(%rdi), %rdi
    leaq    STR_VAL(%rsi), %rsi
    # - %rdx contains strings' length,
    #   we'll use it as a counter of remaining chars to compare
    # - string1's current char ptr will be in %rdi
    #             current char will be in %rcx
    # - string2's current char ptr will be in %rsi
    #             current char will be accessed indirectly

.Runtime.are_equal.cmp_string_content:
    movb    (%rdi), %cl
    cmpb    %cl, (%rsi)
    # We found a pair of unequal chars, the strings are unequal
    jne     .Runtime.are_equal.false
    incq    %rdi # move string1's ptr to next char
    incq    %rsi # move string2's ptr to next char
    
    decq    %rdx # decrease the number of chars remaining to compare
    jnz     .Runtime.are_equal.cmp_string_content
    # We didn't find a pair of unequal chars, the strings are equal
    jmp     .Runtime.are_equal.true

.Runtime.are_equal.int: # Handles int and bool values
    movq    INT_VAL(%rdi), %rdx
    cmpq    %rdx, INT_VAL(%rsi)
    jne      .Runtime.are_equal.false
    # fall through to .Runtime.are_equal.true

.Runtime.are_equal.unit:
    # fall through to .Runtime.are_equal.true

.Runtime.are_equal.true:
    movq    $Boolean_true, %rax
    jmp     .Runtime.are_equal.return

.Runtime.are_equal.false:
    movq    $Boolean_false, %rax

.Runtime.are_equal.return:
    # movq    %rbp, %rsp
    # popq    %rbp

    ret

#
#  .Runtime.out_int
#
#      an int value in %rdi
#
#  Prints out the value of an int object argument.
#
    .global .Runtime.out_int
.Runtime.out_int:
    pushq   %rbp
    movq    %rsp, %rbp

    subq    $16, %rsp
    # -8
    # -12 - digits
    # -16 - 16 bytes boundary padding

    # i            - %rdi
    # is_negative  - %rcx
    # digit_pos    - %rsi

    # Int32.MaxValue = 2147483647 -- (minus sign)? + 10 digits + 0 terminator
    # char digits[12] = { 0 };
    movq    $0, -8(%rbp)
    movl    $0, -12(%rbp)

    xor     %rcx, %rcx
    #. if (is_negative) { i = -i; }
    cmpq    $0, %rdi
    jge     .Runtime.out_int.loop_init

    movq    $1, %rcx # is_negative
    negq    %rdi     # i = -i;

.Runtime.out_int.loop_init:
    # digit_pos = 10
    # terminating 0 is at index 11
    movq    $10, %rsi # digit_pos

.Runtime.out_int.loop_body:
    # i = i / 10
    movq    %rdi, %rax      # i
    cqto                    # sign-extend %rax to %rdx:%rax
    movq    $10, %r8        # divisor
    idivq   %r8             # quotient is in %rax
                            # remainder is in %rdx

    movq    %rax, %rdi      # i

    addq    $48, %rdx            # remainder + '0'
    movb    %dl, -12(%rbp, %rsi) # digits[digit_pos] = remainder + '0'

    decq    %rsi                 # --digit_pos

    # } while (i > 0);
    cmpq    $0, %rdi # i
    jg      .Runtime.out_int.loop_body

    ## if (is_negative) {
    #     digits[digit_pos] = '-';
    #     --digit_pos;
    # }
    cmpq    $0, %rcx             # is_negative
    je      .Runtime.out_int.print
    
    movb    $45, -12(%rbp, %rsi) # digits[digit_pos] = '-';
    decq    %rsi                 # --digit_pos;

.Runtime.out_int.print:
    # digit_pos is pointing to a vacant digit slot, 
    # move it to the leftmost digit (or '-')
    incq    %rsi # digit_pos

    leaq    -12(%rbp, %rsi), %rdi # digits

    movq    $11, %r8
    subq    %rsi, %r8 # 11 - digit_pos
    movq    %r8, %rsi # length

    call    .Platform.out_string

    movq    %rbp, %rsp
    popq    %rbp

    ret

#
#  .Runtime.out_nl
#
#      Doesn't take any args
#
#  Prints out new line according to the platform.
#
    .global .Runtime.out_nl
.Runtime.out_nl:
    movq    $.Platform.ascii_new_line, %rdi
    movq    .Platform.new_line_len, %rsi
    jmp     .Platform.out_string

########################################
# Any
########################################
    .global Any..ctor
Any..ctor:
    movq %rdi, %rax
    ret

#
#  Any.abort
#
#      'this' in %rdi
#  
#  Prints "Aborted from {class}" and exits process.
#  Does not return.
#
    .global Any.abort
Any.abort:
    pushq   %rbp
    movq    %rsp, %rbp

    subq    $(8 + 8), %rsp  # 16 bytes boundary pad + 'this'
    movq    %rdi, -16(%rbp) # 'this'

    # Print "Aborted from " 
    movq    $ascii_aborted_from, %rdi
    movq    $13, %rsi
    call    .Platform.out_string
    # Print "{class}"
    movq    -16(%rbp), %rdi
    call    .Runtime.out_type_name
    # Print new line
    call    .Runtime.out_nl

    # Exit the process
    movq    $0, %rdi
    call    .Platform.exit_process

########################################
# String
########################################

#
#  String.create
#
#      length in     %rdi
#
#  This function is meant for internal usage.
#
#  Creates a string object and returns a pointer to it in %rax.
#  Can't use a prototype object to create string,
#  as string objects have variable size.
#  And currently, once an object is allocated we cannot change its size.
#
    .global String.create
String.create:
    pushq   %rbp
    movq    %rsp, %rbp

    # -8    length
    # -16   size in quads
    # -24   string object
    # -32   16 bytes boundary pad
    subq    $32, %rsp

    movq    %rdi, -8(%rbp)  # length

    # Calc size in quads from length
    addq    $(1 + 7), %rdi            # + 1 to account for null terminator
                                      # + 7 to align
    andq    $0xFFFFFFFFFFFFFFF8, %rdi # align to a quad boundary
    sarq    $3, %rdi                  # div by 8
    addq    $4, %rdi                  # + (tag + size + vtable + length)
    movq    %rdi, -16(%rbp)           # size in quads

    # Allocate the result string
    incq    %rdi            # add a quad for the eyecatch
    call    .Platform.alloc

    movq    $-1, (%rax)     # write the eyecatch value
    addq    $8, %rax        # move ptr to the beginning of object
    movq    %rax, -24(%rbp) # result string object

    # tag
    movq    $String_tag, OBJ_TAG(%rax)
    
    # size in quads
    movq    -16(%rbp), %rcx # size in quads
    movq    %rcx, OBJ_SIZE(%rax)

    # vtable
    movq    $String_vtable, OBJ_VTAB(%rax)

    # length (an int object)
    movq    $Int_proto_obj, %rdi
    call    .Runtime.copy_object
    movq    -8(%rbp), %rcx # length
    movq    %rcx, INT_VAL(%rax)
    movq    -24(%rbp), %rcx # string object
    movq    %rax, STR_LEN(%rcx)

    # return string object
    movq    %rcx, %rax

    movq    %rbp, %rsp
    popq    %rbp
    
    ret

#
#  String.from_buffer
#
#      buffer ptr               in %rdi
#      num of bytes in buffer   in %rsi
#
#  This function is meant for internal usage.
#
#  The buffer must be terminated with 0, '\n', or '\r\n'.
#  Calculates the buffer's length excluding the terminator.
#  Creates a string object of the calculated length
#  and copies the buffer's content into it (excluding the terminator).
#
#  Returns a pointer to the string object in %rax.
#
    .global String.from_buffer
String.from_buffer:
    pushq   %rbp
    movq    %rsp, %rbp

    # -8    buffer_start
    # -16   length
    subq    $16, %rsp

    movq    %rdi, -8(%rbp)  # buffer_start

    # Calculate the string's length.
    # We don't count '\r' and '\n' 
    # as we arent' going to copy them 
    # into the resulting string object.

    # %rsi - num of bytes in buffer
    # %rbx - buffer_start
    # %rcx - string length
    # %rax - current char

    movq    %rdi, %rbx # buffer_start
    xorq    %rcx, %rcx # string length

String.from_buffer.calc_len:
    cmpq    %rsi, %rcx # length < num of bytes in buffer
    jge     String.from_buffer.calc_len_done

    movb    (%rbx, %rcx, 1), %al

    cmpb    $13, %al # '\r'
    je      String.from_buffer.calc_len_done 
    
    cmpb    $10, %al # '\n' 
    je      String.from_buffer.calc_len_done 
    
    cmpb    $0, %al  # 0 terminator
                     # we should not ever encounter it. 
    je      String.from_buffer.calc_len_done 

    incq    %rcx     # ++length
    jmp     String.from_buffer.calc_len

String.from_buffer.calc_len_done:
    movq    %rcx, -16(%rbp) # length

    # Create the string object
    movq    %rcx, %rdi
    call    String.create

    #
    # Copy chars from the buffer to the string object
    #

    # %rdi - src
    # %rdx - src_end
    # %rsi - dst
    # %rcx - tmp

    # src
    movq    -8(%rbp), %rdi       # buffer_start

    # src_end
    movq    -16(%rbp), %rcx      # length
    leaq    (%rdi, %rcx, 1), %rdx

    # dst
    leaq    STR_VAL(%rax), %rsi  # the string object's buffer

    jmp     String.from_buffer.copy_loop_cond

String.from_buffer.copy_loop_body:
    movb    (%rdi), %cl
    movb    %cl, (%rsi)
    incq    %rdi
    incq    %rsi

String.from_buffer.copy_loop_cond:
    cmpq    %rdi, %rdx
    jne     String.from_buffer.copy_loop_body

    # The string object ptr is already in %rax

    movq    %rbp, %rsp
    popq    %rbp

    ret

#
#  String.length
#
#      string object in %rdi
#
#  Returns the string's length (an int object) in %rax.
#
    .global String.length
String.length:
    movq    STR_LEN(%rdi), %rax
    ret

#
#  String.concat(suffix)
#
#    INPUT: 
#        The first string object (this) in      %rdi
#        The second string object (suffix) in   %rsi
#
#    OUTPUT:
#        The new string object in               %rax
#
#  Concatenates suffix onto the end of self and returns a pointer
#  to the new object.
#
    .global String.concat
String.concat:
    # If suffix == null, abort
    cmpq    $0, %rsi
    jne     String.concat.suffix_ok
    movq    $ascii_string_concat, %rdi
    movq    $13, %rsi
    movq    $ascii_suffix, %rdx
    movq    $6, %rcx
    jmp     .Runtime.abort_arg_null

String.concat.suffix_ok:
    # If suffix.length == 0, just return 'this'
    movq    STR_LEN(%rsi), %rcx
    movq    INT_VAL(%rcx), %rcx
    cmpq    $0, %rcx
    jne     String.concat.do_concat
    movq    %rdi, %rax
    ret

String.concat.do_concat:
    pushq   %rbp
    movq    %rsp, %rbp

    # -8    this
    # -16   suffix
    # -24   result string's length in bytes
    # -32   result string object's size in quads
    # -40   result string object
    # -48   16 bytes boundary padding
    subq    $48, %rsp

    movq    %rdi, -8(%rbp)  # this
    movq    %rsi, -16(%rbp) # suffix

    movq    STR_LEN(%rdi), %rax
    movq    INT_VAL(%rax), %rax
    addq    %rcx, %rax      # + suffix.length
    movq    %rax, -24(%rbp) # result string's length in bytes

    # Allocate the result string
    movq    %rax, %rdi      # length
    movq    -24(%rbp), %rcx # result string's length in bytes
    call    String.create
    movq    %rax, -40(%rbp) # result string object

    # %rdi - src
    # %rdx - src_end
    # %rsi - dst
    # %rcx - tmp

    #
    # Copy chars from 'this'
    #
    movq    -8(%rbp), %rdi # 'this'
    movq    STR_LEN(%rdi), %rax
    movq    INT_VAL(%rax), %rax

    # src
    addq    $STR_VAL, %rdi

    # src_end
    movq    %rdi, %rdx
    addq    %rax, %rdx

    # dst 
    movq    -40(%rbp), %rsi # result string object
    addq    $STR_VAL, %rsi

    jmp     String.concat.loop_cond0

String.concat.loop_body0:
    movb    (%rdi), %cl
    movb    %cl, (%rsi)
    incq    %rdi
    incq    %rsi

String.concat.loop_cond0:
    cmpq    %rdi, %rdx
    jne     String.concat.loop_body0

    #
    # Copy chars from 'suffix'
    #
    movq    -16(%rbp), %rdi # suffix
    movq    STR_LEN(%rdi), %rax
    movq    INT_VAL(%rax), %rax

    # src
    addq    $STR_VAL, %rdi

    # src_end
    movq    %rdi, %rdx
    addq    %rax, %rdx

    # dst 
    movq    -8(%rbp), %rax      # 'this' string object
    movq    STR_LEN(%rax), %rax
    movq    INT_VAL(%rax), %rax
    movq    -40(%rbp), %rsi     # result string object
    addq    $STR_VAL, %rsi
    addq    %rax, %rsi          # move ptr beyond the last char 
                                # copied from 'this'

    jmp     String.concat.loop_cond1

String.concat.loop_body1:
    movb    (%rdi), %cl
    movb    %cl, (%rsi)
    incq    %rdi
    incq    %rsi

String.concat.loop_cond1:
    cmpq    %rdi, %rdx
    jne     String.concat.loop_body1

    # terminating 0
    movb    $0, (%rsi)

    # return the result string object
    movq    -40(%rbp), %rax

    movq    %rbp, %rsp
    popq    %rbp

    ret

#
#  String.substring(start_index, end_index)
#
#   INPUT:
#       string object in    %rdi
#       start_index in      %rsi
#       end_index in        %rdx
#
#   OUTPUT:
#       The substring object in %rax
#
#  Returns the substring of 'this' [start_index, end_index).
#  Indexing starts at 0.
#
    .global String.substring
String.substring:
    # we need length to validate indices
    movq    STR_LEN(%rdi), %rcx
    movq    INT_VAL(%rcx), %rcx

    #
    # Validate start_index
    #
    movq    INT_VAL(%rsi), %rsi

    # Ensure start_index >= 0
    cmpq    $0, %rsi
    jl      String.substring.abort_start_index

    # Ensure start_index < length
    cmpq    %rcx, %rsi
    jge     String.substring.abort_start_index

    #
    # Validate end_index
    #
    movq    INT_VAL(%rdx), %rdx

    # Ensure end_index >= 0
    cmpq    $0, %rdx
    jl      String.substring.abort_end_index

    # Ensure end_index <= length
    cmpq    %rcx, %rdx
    jg      String.substring.abort_end_index

    pushq   %rbp
    movq    %rsp, %rbp

    # -8    string object
    # -16   start_index
    # -24   end_index
    # -32   16 bytes boundary pad
    subq    $32, %rsp

    movq    %rdi, -8(%rbp)  # string object
    movq    %rsi, -16(%rbp) # start_index
    movq    %rdx, -24(%rbp) # end_index

    #
    # validate start_index <= end_index
    #
    cmpq    %rdx, %rsi
    jle     String.substring.indices_ok

    # Print "String.substring: start_index "
    movq    $ascii_string_substring_start_index, %rdi
    movq    $30, %rsi
    call    .Platform.out_string
    # Print "{start_index}"
    movq    -16(%rbp), %rdi # start_index
    call    .Runtime.out_int
    # Print " > end_index "
    movq    $ascii_gt_end_index, %rdi
    movq    $13, %rsi
    call    .Platform.out_string
    # Print "{end_index}"
    movq    -24(%rbp), %rdi # end_index
    call    .Runtime.out_int
    # Print new line
    call    .Runtime.out_nl

    # Exit the process
    call    .Platform.exit_process

String.substring.indices_ok:
    movq    -24(%rbp), %rdi # end_index
    subq    -16(%rbp), %rdi # end_index - start_index

    cmpq    $0, %rdi
    jne     String.substring.copy_substring

    movq    $String_empty, %rax
    jmp     String.substring.ret

String.substring.copy_substring:
    # Allocate the result string
    # length is already in %rdi
    call    String.create
    movq    %rax, %rsi # returned string object

    # %rdi - src
    # %rdx - src_end
    # %rsi - dst
    # %rcx - tmp

    #
    # Copy chars from 'this'
    #
    movq    -8(%rbp), %rdi # string object
    addq    $STR_VAL, %rdi
    movq    %rdi, %rdx

    # src
    addq    -16(%rbp), %rdi # start_index

    # src_end
    addq    -24(%rbp), %rdx # end_index

    # dst 
    # substring object is already in %rsi
    addq    $STR_VAL, %rsi

    jmp     String.substring.loop_cond

String.substring.loop_body:
    movb    (%rdi), %cl
    movb    %cl, (%rsi)
    incq    %rdi
    incq    %rsi

String.substring.loop_cond:
    cmpq    %rdi, %rdx
    jne     String.substring.loop_body

    # substring object is already in %rax

String.substring.ret:
    movq    %rbp, %rsp
    popq    %rbp

    ret

String.substring.abort_start_index:
    movq    %rsi, %rdx

String.substring.abort_end_index:
    # end_index is already in %rdx
    movq    $ascii_string_substring, %rdi
    movq    $16, %rsi
    jmp     .Runtime.abort_index

########################################
# ArrayAny
########################################

#
#  ArrayAny..ctor
#
#      'null' in %rdi
#      length in %rsi
#
#  Allocates memory for %rsi elements array.
#  Initializes the array's attributes.
#

    .global ArrayAny..ctor
ArrayAny..ctor:
    pushq   %rbp
    movq    %rsp, %rbp

    subq    $(8 + 8 + 8 + 8), %rsp  # length + 
                                    # size in quads + 
                                    # object ptr + 
                                    # 16 byte boundary padding

    movq    %rsi, -8(%rbp)  # length

    addq    $5, %rsi        # eyecatch + tag + size + vtab + length
    movq    %rsi, -16(%rbp) # size in quads

    movq    %rsi, %rdi      # size in quads
    call    .Platform.alloc

    movq    $-1, (%rax)     # write the eyecatch value
    addq    $8, %rax        # move ptr to the beginning of object

    movq    $ArrayAny_tag, OBJ_TAG(%rax)

    movq    -16(%rbp), %rsi     # size in quads
    movq    %rsi, OBJ_SIZE(%rax)

    movq    $ArrayAny_vtable, OBJ_VTAB(%rax)

    movq    -8(%rbp), %rsi      # length (an int object)
    movq    %rsi, ARR_LEN(%rax)

    # %rdi - current element
    # %rsi - elements end

    movq    INT_VAL(%rsi), %rsi
    salq    $3, %rsi   # length * 8
    leaq    ARR_ITEMS(%rax), %rdi
    addq    %rdi, %rsi # elements end 

    jmp     .ArrayAny..ctor.loop_cond

    # init the array's elements to $0
.ArrayAny..ctor.loop_body:
    movq    $0, (%rdi)
    addq    $8, %rdi

.ArrayAny..ctor.loop_cond:
    cmpq    %rdi, %rsi
    jne     .ArrayAny..ctor.loop_body

    # %rax points to array object

    movq    %rbp, %rsp
    popq    %rbp

    ret

#
#  ArrayAny.get
#
#      array object in %rdi
#      element index (an int object) in %rsi
#
#  Returns value of the array's element with the given index in %rax.
#

    .global ArrayAny.get
ArrayAny.get:
    movq    INT_VAL(%rsi), %rsi

    # Ensure index >= 0
    cmpq    $0, %rsi
    jl      ArrayAny.get.abort_index

    # Ensure index < length
    movq    ARR_LEN(%rdi), %rcx
    movq    INT_VAL(%rcx), %rcx
    cmpq    %rcx, %rsi
    jge     ArrayAny.get.abort_index

    # The index is in range
    addq    $ARR_ITEMS, %rdi # elements ptr
    movq    (%rdi, %rsi, 8), %rax

    ret

ArrayAny.get.abort_index:
    movq    %rsi, %rdx
    movq    $ascii_arrayany_get, %rdi
    movq    $12, %rsi
    jmp     .Runtime.abort_index

#
#  ArrayAny.set
#
#      array object in %rdi
#      element index (an int object) in %rsi
#      value to set in %rdx
#
#  Sets the array's element with the given index to the supplied value.
#

    .global ArrayAny.set
ArrayAny.set:
    movq    INT_VAL(%rsi), %rsi

    # Ensure index >= 0
    cmpq    $0, %rsi
    jl      ArrayAny.set.abort_index

    # Ensure index < length
    movq    ARR_LEN(%rdi), %rcx
    movq    INT_VAL(%rcx), %rcx
    cmpq    %rcx, %rsi
    jge     ArrayAny.set.abort_index

    # The index is in range
    addq    $ARR_ITEMS, %rdi # elements ptr
    movq    %rdx, (%rdi, %rsi, 8)

    ret

ArrayAny.set.abort_index:
    movq    %rsi, %rdx
    movq    $ascii_arrayany_set, %rdi
    movq    $12, %rsi
    jmp     .Runtime.abort_index

#
#  ArrayAny.length
#
#      array object in %rdi
#
#  Returns the array's length (an int object) in %rax.
#

    .global ArrayAny.length
ArrayAny.length:
    movq    ARR_LEN(%rdi), %rax
    ret

########################################
# IO
########################################
    .global IO..ctor
IO..ctor:
    movq %rdi, %rax
    ret

    .global IO.out_string

#
#  IO.out_string
#
#      'this' in %rdi
#      a string object in %rsi
#
#  Prints out the content of a string object argument.
#
IO.out_string:
    leaq    STR_VAL(%rsi), %rdi
    movq    STR_LEN(%rsi), %rsi
    movq    INT_VAL(%rsi), %rsi
    jmp     .Platform.out_string

#
#  IO.out_int
#
#      'this' in %rdi
#      an int object in %rsi
#
#  Prints out the value of an int object argument.
#
    .global IO.out_int
IO.out_int:
    movq    INT_VAL(%rsi), %rdi
    jmp     .Runtime.out_int

#
#  IO.out_nl
#
#      'this' in %rdi
#
#  Prints out "\r\n"
#
    .global IO.out_nl
IO.out_nl:
    jmp     .Runtime.out_nl

#
#  IO.in_string
#
#      Doesn't take any args
#
#  Reads a line from stdin.
#  Copies the line into a string object.
#  And returns a pointer to the object in %rax.
#
    .global IO.in_string
IO.in_string:
    jmp     .Platform.in_string

#
#  IO.in_int
#
#      Doesn't take any args
#
#  Reads a line from stdin.
#  Tries to convert the line into an int object.
#  And returns a pointer to the object in %rax.
#
    .global IO.in_int
IO.in_int:
    pushq   %rbp
    movq    %rsp, %rbp

    # -8    int object
    # -16   16 bytes boundary padding
    subq    $16, %rsp

    # Create an int object,
    # that will hold the int value 
    # converted from a string read from stdin.
    movq    $Int_proto_obj, %rdi
    call    .Runtime.copy_object
    movq    %rax, -8(%rbp)

    # %rsi - length
    # %rdx - buffer
    # %rcx - buffer_end
    # %rdi - int value
    # %r8  - is_negative
    # %rax - tmp

    # string object
    call    .Platform.in_string
    movq    %rax, %rdi

    # length
    movq    STR_LEN(%rdi), %rsi
    movq    INT_VAL(%rsi), %rsi

    # buffer
    leaq    STR_VAL(%rdi), %rdx
    # buufer_end
    leaq    (%rdx, %rsi, 1), %rcx

    # int value
    xorq    %rdi, %rdi

    # See if the first char is '-' or '+' 
    xorq    %r8, %r8     # is_negative = false
    # '+' - 43
    # '-' - 45
    movzbq  (%rdx), %rax # first char
    cmpq    $45, %rax
    je      IO.in_int.leading_minus
    cmpq    $43, %rax
    je      IO.in_int.leading_plus
    jmp     IO.in_int.validate_len

IO.in_int.leading_minus:
    movq    $1, %r8      # is_negative = true
IO.in_int.leading_plus:
    # skip the sign char
    incq    %rdx         # ++buffer
    # skip sign and count only digits
    decq    %rsi         # --length

IO.in_int.validate_len:
    # Int32.MaxValue = 2147483647 -- 10 digits
    cmpq    $10, %rsi
    jle     IO.in_int.convert

    # Print "IO.in_int: Input string is too long".
    # Then exit the process.
    movq    $ascii_input_too_long, %rdi
    movq    $35, %rsi
    call    .Platform.out_string
    jmp     .Platform.exit_process

IO.in_int.convert:
    jmp     IO.in_int.loop_cond

IO.in_int.loop_body:
    movzbq  (%rdx), %rax          # digit_char
    subq    $48, %rax             # digit_char -'0' = digit

    # validate digit
    # If the char is ['0' - '9'], 
    # the value in %rax must be `>= 0` and `<= 9`
    cmpq    $0, %rax
    jl      IO.in_int.input_not_digit
    cmpq    $9, %rax
    jg      IO.in_int.input_not_digit

    leaq    (%rdi, %rdi, 4), %rdi # int value = int value + int value * 4
    leaq    (%rax, %rdi, 2), %rdi # int value = digit + (int value * 2)
                                  # as a result of the prev two instructions,
                                  # we did: int value = (int value * 10) + digit
    
    incq    %rdx                  # next char

IO.in_int.loop_cond:
    cmpq    %rcx, %rdx
    jne     IO.in_int.loop_body

    cmpq    $0, %r8              # is_negative?
    je      IO.in_int.ret

    # The input string's first char is '-'.
    # The resulting int value must be negative.
    negq    %rdi                # int value = -(int value)

IO.in_int.ret:
    movq    -8(%rbp), %rax       # int object
    movq    %rdi, INT_VAL(%rax)

    movq    %rbp, %rsp
    popq    %rbp

    ret

IO.in_int.input_not_digit:
    movq    $ascii_input_not_digit, %rdi
    movq    $59, %rsi
    call    .Platform.out_string
    jmp     .Platform.exit_process

########################################
# Process entry point
########################################
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
