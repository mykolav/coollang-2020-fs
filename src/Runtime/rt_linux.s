#################################################
# THIS IS JUST A SCAFFOLDING OF A FUTURE RUNTIME!
#################################################

########################################
# Data
########################################
    .data

########################################
# Strings
########################################
    .global .Platform.ascii_new_line
.Platform.ascii_new_line:   .ascii "\n"
    .global .Platform.new_line_len
.Platform.new_line_len:     .quad  1

########################################
# Global vars
########################################

curr_break:
    .quad 0

########################################
# Text
########################################
    .text

########################################
# .Platform
########################################

#
#  .Platform.init
#
#  INPUT:
#      None
#  OUTPUT:
#      None
#  
#  Calls brk(-1) and stores the process break 
#  for later use by .Platform.alloc.
#
    .global .Platform.init
.Platform.init:
    #pushq   %rbp
    #movq    %rsp, %rbp

    # On failure, the system call returns the current break.
    # We want to find out the current break's value,
    # so we deliberately pass an invalid value as the first arg.
    movq    $-1, %rdi
    movq    $12, %rax # brk
    syscall
    movq    %rax, curr_break

    #movq    %rbp, %rsp
    #popq    %rbp

    ret

#
#  .Platform.alloc
#
#  INPUT:
#      a size in quads in %rdi
#  OUTPUT:
#      a pointer to the start of allocated memory block in %rax.
#.     if allocation fails, prints a message and exits process.
#  
#  Allocates (%rdi * 8) bytes of memory on heap.
#
    .global .Platform.alloc
.Platform.alloc:
    # pushq   %rbp
    # movq    %rsp, %rbp

    sal     $3, %rdi         # convert quads to bytes
    addq    curr_break, %rdi # calculate the new break
    movq    $12, %rax        # brk
    syscall
    movq    %rax, curr_break

    # a pointer to the start of 
    # allocated memory block is already in %rax

    # movq    %rbp, %rsp
    # popq    %rbp

    ret

#
#  .Platform.out_string
#
#      buffer ptr in %rdi
#      size in bytes in %rsi
#
#  Prints out the content of a string object argument.
#
    .global .Platform.out_string
.Platform.out_string:
    pushq   %rbp
    movq    %rsp, %rbp

    movq    %rsi, %rdx  # length
    movq    %rdi, %rsi  # buffer
    movq    $1, %rdi    # fd      = stdout
    movq    $1, %rax    # syscall = write
    syscall

    movq    %rbp, %rsp
    popq    %rbp

    ret

#
#  .Platform.in_string
#
#      Doesn't take any args
#
#  Reads a line from stdin.
#  Copies the line into a string object.
#  And returns a pointer to the object in %rax.
#
    .global .Platform.in_string
.Platform.in_string:
    pushq   %rbp
    movq    %rsp, %rbp

    movq    %rbp, %rsp
    popq    %rbp

    ret

#
#  .Platform.exit_process
#
#      exit code   in %rdi
#
#  Makes the process exit.
#  Does not return.
#
    .global .Platform.exit_process
.Platform.exit_process:
    # exit code is already in %rdi
    movq    $0x3c, %rax
    syscall
