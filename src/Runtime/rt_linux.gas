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
#  Calls GetProcessHeap and stores the pointer 
#  for later use by .Platform.alloc.
#
    .global .Platform.init
.Platform.init:
    pushq   %rbp
    movq    %rsp, %rbp

    # subq    $32, %rsp # allocate shadow space!

    # Initialize the heap.
    # call    GetProcessHeap
    # movq    %rax, hProcessDefaultHeap

    movq    %rbp, %rsp
    popq    %rbp

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
    pushq   %rbp
    movq    %rsp, %rb# p

    # subq    $32, %rsp # shadow space!

    # DECLSPEC_ALLOCATOR LPVOID HeapAlloc(
    #   HANDLE hHeap,
    #   DWORD  dwFlags,
    #   SIZE_T dwBytes
    # );
    # hHeap
    # movq    hProcessDefaultHeap, %rcx
    # dwFlags
    # movq    $HEAP_ZERO_MEMORY, %rdx
    # dwBytes
    # movq    %rdi, %r8 # size in quads
    # salq    $3, %r8   # convert quads to to bytes
    # call    HeapAlloc
    # cmpq    $0, %rax
    # jne     .Platform.alloc.ok

    # Allocation failed
    # movq    $ascii_out_of_memory, %rdi
    # movq    $13, %rsi
    # call    .Platform.out_string

    # movq    $0, %rdi
    # call    .Platform.exit_process

.Platform.alloc.ok:
    movq    %rbp, %rsp
    popq    %rbp

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

    # subq    $(8 + 8 + 32), %rsp # NumberOfBytesWritten + 
    #                             # fifth argument + 
    #                             # shadow space

    # movl    $STD_OUTPUT_HANDLE, %ecx
    # call    GetStdHandle
    # movq    %rax, %rcx
  
    # BOOL WriteFile(
    #   HANDLE       hFile,
    #   LPCVOID      lpBuffer,
    #   DWORD        nNumberOfBytesToWrite,
    #   LPDWORD      lpNumberOfBytesWritten,
    #   LPOVERLAPPED lpOverlapped
    # );
    # movq    %rdi, %rdx
    # movq    %rsi, %r8
    # leaq    -8(%rbp), %r9 # lpNumberOfBytesWritten
    # movq    $0, -16(%rbp) # lpOverlapped
    #                       # a fifth argument must be higher in the stack
    #                       # than the shadow space!
    # call    WriteFile

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
    # movq %rdi, %rcx
    # call ExitProcess
