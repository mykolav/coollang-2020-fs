
########################################
# Data
########################################
    .data

########################################
# Strings
########################################
ascii_aborted_from:             .ascii "Aborted from "

ascii_string_substring_start_index:
                                .ascii "String.substring: start_index "
ascii_gt_end_index:             .ascii " > end_index "

ascii_arrayany_get:             .ascii "ArrayAny.get"
ascii_arrayany_set:             .ascii "ArrayAny.set"

ascii_string_concat:            .ascii "String.concat"
ascii_suffix:                   .ascii "suffix"

ascii_string_substring:         .ascii "String.substring"

ascii_input_too_long:           .ascii "IO.in_int: Input string is too long"
ascii_input_not_digit:          .ascii "IO.in_int: Input string contains a char that is not a digit"

########################################
# Virtual tables
########################################
    .global Any_VTABLE
Any_VTABLE:
    .quad Any.abort
    .quad Any.equals
    .quad Any.GC_collect
    .quad Any.GC_print_state

    .global Unit_VTABLE
Unit_VTABLE:
    .quad Any.abort
    .quad Any.equals
    .quad Any.GC_collect
    .quad Any.GC_print_state

    .global Int_VTABLE
Int_VTABLE:
    .quad Any.abort
    .quad Any.equals
    .quad Any.GC_collect
    .quad Any.GC_print_state

    .global String_VTABLE
String_VTABLE:
    .quad Any.abort
    .quad Any.equals
    .quad Any.GC_collect
    .quad Any.GC_print_state
    .quad String.length
    .quad String.concat
    .quad String.substring

    .global Boolean_VTABLE
Boolean_VTABLE:
    .quad Any.abort
    .quad Any.equals
    .quad Any.GC_collect
    .quad Any.GC_print_state

    .global ArrayAny_VTABLE
ArrayAny_VTABLE:
    .quad Any.abort
    .quad Any.equals
    .quad Any.GC_collect
    .quad Any.GC_print_state
    .quad ArrayAny.get
    .quad ArrayAny.set
    .quad ArrayAny.length

    .global IO_VTABLE
IO_VTABLE:
    .quad Any.abort
    .quad Any.equals
    .quad Any.GC_collect
    .quad Any.GC_print_state
    .quad IO.out_string
    .quad IO.out_int
    .quad IO.out_nl
    .quad IO.in_string
    .quad IO.in_int

    .include "constants.inc"

########################################
# Prototype objects
########################################
    .quad -1
    .global Unit_VALUE
Unit_VALUE:
    .quad UNIT_TAG    # tag
    .quad 3           # size in quads
    .quad Unit_VTABLE
    
    .quad -1
    .global String_EMPTY
String_EMPTY:
    .quad STRING_TAG    # tag
    .quad 5             # size in quads
    .quad String_VTABLE
    .quad INT_0         # length
    .quad 0             # terminating 0 and 
                        # 16 bytes boundary padding
    
    .quad -1
    .global Boolean_TRUE
Boolean_TRUE:
    .quad BOOLEAN_TAG    # tag
    .quad 4              # size in quads
    .quad Boolean_VTABLE
    .quad 1              # value

    .quad -1
    .global Boolean_FALSE
Boolean_FALSE:
    .quad BOOLEAN_TAG    # tag
    .quad 4              # size in quads
    .quad Boolean_VTABLE
    .quad 0              # value
    
    .quad -1
    .global IO_PROTO_OBJ
IO_PROTO_OBJ:
    .quad IO_TAG         # tag
    .quad 3              # size in quads
    .quad IO_VTABLE

########################################
# Text
########################################
    .text

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
#  Prints "Aborted from {class}" and exits the process.
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


#
#  Any.equals
#  
#  Compares 'this' object to 'other' object.
#  Two objects are equal if they
#    - are identical (pointer equality)
#    - have same tag and are of type BOOL, STRING, INT, UNIT 
#      and contain the same data.
#
#  INPUT: 
#      'this'   in %rdi
#      'other'  in %rsi
#  OUTPUT: 
#      $Boolean_TRUE  in %rax, if the objects are equal.
#      $Boolean_FALSE in %rax, if the objects are unequal.
#
    .global Any.equals
Any.equals:
    jmp     .Runtime.are_equal


#
# Collect garbage
#  
#   Triggers a garbage collection by invoking .MemoryManager.FN_COLLECT
#   Requests additional allocation of 0 bytes, 
#   as we only want to collect the garbage.
#
#   INPUT: 
#    None
#   OUTPUT: 
#    None
#
#   Registers modified:
#    %rdi, %rsi, .MemoryManager.FN_COLLECT
#
    .global Any.GC_collect
Any.GC_collect:
    pushq    %rbp
    movq     %rsp, %rbp

    xorl     %edi, %edi                          # %rdi = alloc size = 0
    movq     %rbp, %rsi                          # %rsi = tip of stack to start collecting from
    callq    *.MemoryManager.FN_COLLECT(%rip)    # collect garbage

    movq     %rbp, %rsp
    popq     %rbp

    ret

#
#  Print GC's state
#  
#  Makes the configured garbage collector dump its current state to STDOUT 
#  by invoking .MemoryManager.FN_PRINT_STATE
#
#  INPUT: 
#   None
#  OUTPUT: 
#   None
#
#   Registers modified:
#    .MemoryManager.FN_PRINT_STATE
#
    .global Any.GC_print_state
Any.GC_print_state:
    pushq    %rbp
    movq     %rsp, %rbp

    callq    *.MemoryManager.FN_PRINT_STATE(%rip)

    movq     %rbp, %rsp
    popq     %rbp

    ret

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

    movq    %rdi, -8(%rbp)               # length

    # Calc size in quads from length
    addq    $(1 + 7), %rdi               # + 1 to account for null terminator
                                         # + 7 to align
    andq    $0xFFFFFFFFFFFFFFF8, %rdi    # align to a quad boundary
    sarq    $3, %rdi                     # div by 8
    addq    $4, %rdi                     # + (tag + size + vtable + length)
    movq    %rdi, -16(%rbp)              # size in quads

    # Allocate the result string
    incq    %rdi                         # add a quad for the eyecatch
    call    .MemoryManager.alloc

    movq    $EYE_CATCH, (%rax)           # write the eyecatch value
    addq    $8, %rax                     # move ptr to the beginning of object
    movq    %rax, -24(%rbp)              # result string object

    # tag
    movq    $STRING_TAG, OBJ_TAG(%rax)
    
    # size in quads
    movq    -16(%rbp), %rcx              # size in quads
    movq    %rcx, OBJ_SIZE(%rax)

    # vtable
    movq    $String_VTABLE, OBJ_VTAB(%rax)

    # length (an int object)

    movq    -8(%rbp), %rdi               # length (an integer value)
    call    Int.get_or_create
    movq    -24(%rbp), %rcx              # string object
    movq    %rax, STR_LEN(%rcx)          # length (an Int object)

    # return string object
    movq    %rcx, %rax

    movq    %rbp, %rsp
    popq    %rbp
    
    ret

########################################
# Int
########################################

#
# Get a predefined Int object or create a new one
#
#   Find a predefined Int object corresponding to the value of %rdi.
#   If not found, create a new one.
#
#   INPUT:
#    %rdi: an int value (not an Int object)
#
#   OUTPUT:
#    %rax: pointer to the Int object
#
#   Registers modified:
#    %rdi, %rsi, %rax
#
    .global Int.get_or_create
Int.get_or_create:
    MIN_PREDEFINED              = -500
    MAX_PREDEFINED              = 500
    # An `Int` object size in quads is 4:
    # TAG slot + OBJ SIZE slot + VTAB slot + VALUE slot
    INT_OBJ_SIZE_IN_QUADS       = 4

    VAR_VALUE_SIZE              = 8
    VAR_VALUE                   = -VAR_VALUE_SIZE
    PAD_SIZE                    = 8
    FRAME_SIZE                  = VAR_VALUE_SIZE + PAD_SIZE

    pushq   %rbp
    movq    %rsp, %rbp
    subq    $FRAME_SIZE, %rsp

    cmpq    $MIN_PREDEFINED, %rdi
    jl      Int.get_or_create.create                   # if (%rdi < -500) go to ...
    cmpq    $MAX_PREDEFINED, %rdi
    jg      Int.get_or_create.create                   # if (%rdi > 500) go to ...

    # %rdi contains the requested value, which basically is
    # an element index in the predefined Int objects table.
    # The element's offset in quads = index * (INT_OBJ_SIZE_IN_QUADS + 1 for eye catcher)
    # The offset in bytes = the offset in quads * 8
    movq    %rdi, %rax                                 # %rax = %rdi = index
    salq    $2, %rax                                   # %rax = index * 4
    addq    %rdi, %rax                                 # %rax = index * 4 + index = index * 5 
                                                       #      = index * (INT_OBJ_SIZE_IN_QUADS + 1)
                                                       #      = offset in quads
    salq    $3, %rax                                   # %rax = offset in quads * 8 = offset in bytes
    addq    $INT_0, %rax                               # %rax = the table base + offset in bytes
                                                       #      = the address of the Int object
    jmp     Int.get_or_create.done

Int.get_or_create.create:
    movq    %rdi, VAR_VALUE(%rbp)                      # preserve the requested int value

    movq    $(INT_OBJ_SIZE_IN_QUADS + 1), %rdi         # %rdi = eye catcher + an Int obj size in quads
    call    .MemoryManager.alloc                       # %rax = the start of allocated memory block

    addq    $8, %rax                                   # %rax = the start of Int obj (moved past the eye-catcher)

    movq    $EYE_CATCH, OBJ_EYE_CATCH(%rax)
    movq    $INT_TAG, OBJ_TAG(%rax)
    movq    $INT_OBJ_SIZE_IN_QUADS, OBJ_SIZE(%rax)
    movq    $Int_VTABLE, OBJ_VTAB(%rax)

    movq    VAR_VALUE(%rbp), %rdi
    movq    %rdi, INT_VAL(%rax)

Int.get_or_create.done:
    # %rax = the pointer to Int object

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

    movq    $String_EMPTY, %rax
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
#      length (an int object) in %rsi
#
#  Allocates memory for %rsi elements array.
#  Initializes the array's attributes.
#

    .global ArrayAny..ctor
ArrayAny..ctor:
    VAR_ARR_LEN_SIZE            = 8
    VAR_ARR_LEN                 = -VAR_ARR_LEN_SIZE
    VAR_OBJ_SIZE_IN_QUADS_SIZE  = 8
    VAR_OBJ_SIZE_IN_QUADS       = -(VAR_ARR_LEN_SIZE + VAR_OBJ_SIZE_IN_QUADS_SIZE)
    PAD_SIZE                    = 0
    FRAME_SIZE                  =   VAR_ARR_LEN_SIZE + VAR_OBJ_SIZE_IN_QUADS_SIZE + PAD_SIZE

    pushq   %rbp
    movq    %rsp, %rbp
    subq    $FRAME_SIZE, %rsp

    movq    %rsi, VAR_ARR_LEN(%rbp)              # preserve the array length as an Int object
    movq    INT_VAL(%rsi), %rsi                  # %rsi = array length (number of items)

    # An `ArrayAny` object size in quads is 
    # %rsi + a quad for each of the
    #     TAG slot + 
    #     OBJ SIZE slot + 
    #     VTAB slot + 
    #     LENGTH slot
    addq    $4, %rsi                             # %rsi = the array object size in quads
    movq    %rsi, VAR_OBJ_SIZE_IN_QUADS(%rbp)    # preserve the size in quads

    movq    %rsi, %rdi                           # %rdi = %rsi = the array object size in quads
    incq    %rdi                                 # %rdi = eye catcher + the array obj size in quads
    call    .MemoryManager.alloc                 # %rax = the start of allocated memory block

    addq    $8, %rax                             # %rax = the start of array obj

    movq    $EYE_CATCH, OBJ_EYE_CATCH(%rax)
    movq    $ARRAYANY_TAG, OBJ_TAG(%rax)

    movq    VAR_OBJ_SIZE_IN_QUADS(%rbp), %rsi    # %rsi = size in quads
    movq    %rsi, OBJ_SIZE(%rax)

    movq    $ArrayAny_VTABLE, OBJ_VTAB(%rax)

    movq    VAR_ARR_LEN(%rbp), %rsi              # %rsi = array length (an Int object)
    movq    %rsi, ARR_LEN(%rax)

    # %rdi - current element
    # %rsi - elements end

    movq    INT_VAL(%rsi), %rsi                  # %rsi = array length (number of items)
    salq    $3, %rsi                             # %rsi = arary length * 8 (sizeof(an array item)) 
                                                 #      = sizeof(all array items) in bytes
    leaq    ARR_ITEMS(%rax), %rdi                # %rdi = the start of array items mem region
    addq    %rdi, %rsi                           # %rsi = the start of array items + sizeof(all array items)
                                                 #        the limit of array items mem region

    jmp     .ArrayAny..ctor.loop_cond

    # init the array's elements to $0
.ArrayAny..ctor.loop_body:
    movq    $0, 0(%rdi) 
    addq    $8, %rdi

.ArrayAny..ctor.loop_cond:
    cmpq    %rsi, %rdi
    jl     .ArrayAny..ctor.loop_body             # if (%rdi < the limit of array items) go to ...

    # %rax = the start of array object

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
#  Sets the array's element with the given index to the supplied value.
#
#  INPUT
#   %rdi: array object
#   %rsi: element index (an int object)
#   %rdx: value to set
#
#  OUTPUT
#   None
#
#  Registers modified
#   %rdi, %rsi, %rcx, .MemoryManager.on_assign
#

    .global ArrayAny.set
ArrayAny.set:
    pushq   %rbp
    movq    %rsp, %rbp

    movq    INT_VAL(%rsi), %rsi         # %rsi = index

    # Ensure index >= 0
    cmpq    $0, %rsi
    jl      ArrayAny.set.abort_index    # if (%rsi < 0) go to ...

    # Ensure index < length
    movq    ARR_LEN(%rdi), %rcx
    movq    INT_VAL(%rcx), %rcx         # %rcx = the array's length
    cmpq    %rcx, %rsi
    jge     ArrayAny.set.abort_index    # if (index >= length) go to ...

    # The index is in range
    addq    $ARR_ITEMS, %rdi            # %rdi = a pointer to the array's items memory region
    leaq    (%rdi, %rsi, 8), %rdi       # %rdi = a pointer to the element at index
    movq    %rdx, 0(%rdi)
    call    .MemoryManager.on_assign

    movq    %rbp, %rsp
    popq    %rbp
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

#
#  IO.out_string
#
#      'this' in %rdi
#      a string object in %rsi
#
#  Prints out the content of a string object argument.
#
    .global IO.out_string
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
    # %rdi = the integer value parsed from STDIN
    call    Int.get_or_create
    # %rax = the pointer to Int object

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
    MAIN_OBJ_SIZE    = 8
    MAIN_OBJ         = -MAIN_OBJ_SIZE
    PAD_SIZE         = 8
    FRAME_SIZE       =  MAIN_OBJ_SIZE + PAD_SIZE

    pushq   %rbp
    movq    %rsp, %rbp
    subq    $FRAME_SIZE, %rsp

    call    .Platform.init

    # The base of stack -- to stop checking for stack GC roots at.
    movq    %rbp, %rdi
    call    .MemoryManager.init

    # A class 'Main' must be present in every Cool2020 program.
    # Create a new instance of 'Main'.
    movq    $Main_PROTO_OBJ, %rdi
    call    .Runtime.copy_object

    # Place the created `Main` object on the stack
    # to let the GC know it's not garbage.
    movq    %rax, MAIN_OBJ(%rbp)

    # 'Main..ctor' is a Cool2020 program's entry point.
    # Pass a reference to the newly created 'Main' instance in %rdi.
    # Invoke the constructor.
    movq    %rax, %rdi
    call    Main..ctor

    xorq    %rdi, %rdi
    jmp     .Platform.exit_process
