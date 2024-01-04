
########################################
# Data
########################################
    .data

########################################
# Strings
########################################

ascii_out_of_memory:            .ascii "Out of memory"

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

    .include "constants.inc"

########################################
# Text
########################################
    .text

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
#  Prints "{file}({line},{column}): Dispatch to null" and exits the process.
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
#  Prints "{file}({line},{column}): No match for {class}" and exits the process.
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
#  Prints "{method_name}: Index {index} is out of range" and exits the process.
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
#  Prints "{method_name}: Actual '{arg_name}' is null" and exits the process.
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
#  .Runtime.abort_out_of_mem
#
#      Doesn't take any args
#
#  Prints "Out of memory" and exits the process.
#  Does not return.
#
    .global .Runtime.abort_out_of_mem
.Runtime.abort_out_of_mem:
    movq    $ascii_out_of_memory, %rdi
    movq    $13, %rsi
    call    .Platform.out_string

    movq    $1, %rdi
    jmp    .Platform.exit_process

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
                                   # File +
                                   # Line + 
                                   # Colum

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
                     # to get the offset in 'CLASS_NAME_MAP'
    addq    $CLASS_NAME_MAP, %rdi
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

    subq    $(8 + 8), %rsp          # 16 bytes boundary padding + prototype
    movq    %rdi, -16(%rbp)         # store the prototype

    movq    OBJ_SIZE(%rdi), %rdi    # size in quads
    incq    %rdi                    # add a quad for the eyecatch
    call    .MemoryManager.alloc

    movq    $EYE_CATCH, (%rax)      # write the eyecatch value
    addq    $8, %rax                # move ptr to the beginning of object

    # %rdi - src
    # %rdx - src_end
    # %rsi - dst
    # %rcx - tmp

    # dst
    movq    %rax, %rsi

    # src
    movq    -16(%rbp), %rdi         # prototype

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
#    - have the same tag and are of type BOOL, STRING, INT, UNIT 
#      and contain the same data.
#
#  INPUT: 
#      obj1 in %rdi
#      obj2 in %rsi
#  OUTPUT: 
#      $Boolean_TRUE  in %rax, if the objects are equal.
#      $Boolean_FALSE in %rax, if the objects are unequal.
#
    .global .Runtime.are_equal
.Runtime.are_equal:
    # pointer equality
    cmpq    %rdi, %rsi
    je      .Runtime.are_equal.true

    # get tags
    movq    (%rdi), %rdx
    movq    (%rsi), %rcx

    #. if the tags are not equal, the objects are unequal
    cmpq    %rdx, %rcx
    jne     .Runtime.are_equal.false

    cmpq    $INT_TAG, %rdx
    je      .Runtime.are_equal.int

    cmpq    $BOOLEAN_TAG, %rdx
    je      .Runtime.are_equal.int

    cmpq    $UNIT_TAG, %rdx
    je      .Runtime.are_equal.unit

    cmpq    $STRING_TAG, %rdx
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
    movq    $Boolean_TRUE, %rax
    jmp     .Runtime.are_equal.return

.Runtime.are_equal.false:
    movq    $Boolean_FALSE, %rax

.Runtime.are_equal.return:
    ret

#
#  .Runtime.out_int
#
#  Prints out the value of an int object argument.
#
#  INPUT
#   %rdi: an int value (*not* an Int object)
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

#
#  Prints out the content of the buffer.
#
#  INPUT
#   %rdi: buffer pointer
#   %rsi: buffer size in bytes
#
#  OUTPUT
#   None
#
    .global .Runtime.print
.Runtime.print:
    jmp    .Platform.out_string

#
#  Prints out the content of the buffer and appends a new line.
#
#  INPUT
#   %rdi: buffer pointer
#   %rsi: buffer size in bytes
#
#  OUTPUT
#   None
#
    .global .Runtime.print_ln
.Runtime.print_ln:
    pushq   %rbp
    movq    %rsp, %rbp

    call    .Platform.out_string
    call    .Runtime.out_nl

    movq    %rsp, %rbp
    popq    %rbp
    ret
