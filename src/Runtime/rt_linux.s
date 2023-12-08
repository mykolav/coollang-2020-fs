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
    .global .Platform.heap_start
.Platform.heap_start:    .quad 0

    .global .Platform.heap_end
.Platform.heap_end:      .quad 0

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
    # The program break is the first location 
    # after the end of the uninitialized data segment (.bss)
    #
    # If the program started with a pre-allocated heap,
    # the break would be defined as the first location after the pre-allocated heap 
    # instead of the .bss.
    # In other words, a process starts with a heap of size 0.
    # Therefore, finding the current break's value at this moment 
    # gives us the start of the heap's location.
    #
    # On failure, the system call returns the current break.
    # We want to find out the current break's value,
    # so we deliberately pass an invalid value as the first arg.
    movq    $-1, %rdi
    movq    $12, %rax # brk
    syscall
    movq    %rax, curr_break
    movq    %rax, .Platform.heap_start
    movq    %rax, .Platform.heap_end

    ret

#
#  .Platform.alloc
#
#  INPUT:
#      %rdi: the requested size in bytes
#  OUTPUT:
#      %rax: a pointer to the start of allocated memory block.
#      If allocation fails, prints a message and exits the process.
#  
#  Allocates additional %rdi bytes of memory on heap.
#
    .global .Platform.alloc
.Platform.alloc:
    addq    curr_break, %rdi            # calculate the new break
    movq    $12, %rax                   # brk
    syscall

    # On failure, the system call returns the current break.
    cmpq    curr_break, %rax
    jne     .Platform.alloc.ok

    # Allocation failed
    jmp     .Runtime.abort_out_of_mem

.Platform.alloc.ok:
    movq    curr_break, %rdi            # ptr to the start of allocated memory
    movq    %rax, curr_break            # store the new break
    movq    %rax, .Platform.heap_end

    # Return ptr to the start of allocated memory
    movq    %rdi, %rax

    ret

#
#  .Platform.out_string
#
#      buffer ptr in %rdi
#      size in bytes in %rsi
#
#  Prints out the content of the string object argument.
#
    .global .Platform.out_string
.Platform.out_string:
    movq    %rsi, %rdx  # length
    movq    %rdi, %rsi  # buffer
    movq    $1, %rdi    # fd      = stdout
    movq    $1, %rax    # syscall = write
    syscall

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

    # -14        16 byte boundary padding
    # -1040      buffer 
    subq    $1040, %rsp

    movq    $0, %rdi          # fd      = stdin
    leaq    -1040(%rbp), %rsi # buffer
    movq    $1026, %rdx       # buffer size
    movq    $0, %rax          # syscall = read
    syscall

    leaq    -1040(%rbp), %rdi # buffer
    movq    %rax, %rsi        # num of bytes read
    call    String.from_buffer

    # The string object ptr is already in %rax

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
