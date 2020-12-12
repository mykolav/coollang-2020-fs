########################################
# Data
########################################
    .data

########################################
# Strings
########################################
    .global .Platform.ascii_new_line
.Platform.ascii_new_line:   .ascii "\r\n"
    .global .Platform.new_line_len
.Platform.new_line_len:     .quad  2

########################################
# Global vars
########################################

hProcessDefaultHeap:
    .quad 0

########################################
# Text
#
# Under the Microsoft x64 calling convention:
# %rax, %rcx, %rdx, %r8, %r9, %r10, and %r11 are volatile, 
# %rbx, %rbp, %rdi, %rsi, %rsp, and %r12 through %r15 are non-volatile and must be saved be the callee if used.
########################################
    .text

    .set HEAP_GENERATE_EXCEPTIONS, 0x00000004
    .set HEAP_NO_SERIALIZE, 0x00000001
    .set HEAP_ZERO_MEMORY, 0x00000008

    .set STD_INPUT_HANDLE, -10
    .set STD_OUTPUT_HANDLE, -11
    .set STD_ERROR_HANDLE, -12

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

    subq    $32, %rsp # allocate shadow space!

    # Initialize the heap.
    call    GetProcessHeap
    movq    %rax, hProcessDefaultHeap

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
    movq    %rsp, %rbp

    subq    $32, %rsp # shadow space!

    # DECLSPEC_ALLOCATOR LPVOID HeapAlloc(
    #   HANDLE hHeap,
    #   DWORD  dwFlags,
    #   SIZE_T dwBytes
    # );
    # hHeap
    movq    hProcessDefaultHeap, %rcx
    # dwFlags
    movq    $HEAP_ZERO_MEMORY, %rdx
    # dwBytes
    movq    %rdi, %r8 # size in quads
    salq    $3, %r8   # convert quads to to bytes
    call    HeapAlloc
    cmpq    $0, %rax
    jne     .Platform.alloc.ok

    # Allocation failed
    jmp     .Runtime.abort_out_of_mem

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

    subq    $(8 + 8 + 32), %rsp # NumberOfBytesWritten + 
                                # fifth argument + 
                                # shadow space

    movl    $STD_OUTPUT_HANDLE, %ecx
    call    GetStdHandle
    movq    %rax, %rcx
  
    # BOOL WriteFile(
    #   HANDLE       hFile,
    #   LPCVOID      lpBuffer,
    #   DWORD        nNumberOfBytesToWrite,
    #   LPDWORD      lpNumberOfBytesWritten,
    #   LPOVERLAPPED lpOverlapped
    # );
    movq    %rdi, %rdx
    movq    %rsi, %r8
    leaq    -8(%rbp), %r9 # lpNumberOfBytesWritten
    movq    $0, -16(%rbp) # lpOverlapped
                          # a fifth argument must be higher in the stack
                          # than the shadow space!
    call    WriteFile

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

    # -6        16 byte boundary padding
    # -1032     buffer 
    # -1040     numberOfBytesRead
    # -1048     fifth argument (lpOverlapped)
    # -1056     shadow space
    subq    $1056, %rsp

    movl    $STD_INPUT_HANDLE, %ecx
    call    GetStdHandle
    movq    %rax, %rcx
  
    # BOOL ReadFile(
    #   HANDLE       hFile,
    #   LPVOID       lpBuffer,
    #   DWORD        nNumberOfBytesToRead,
    #   LPDWORD      lpNumberOfBytesRead,
    #   LPOVERLAPPED lpOverlapped
    # );
    # stdin handle is already in %rcx
    leaq    -1032(%rbp), %rdx   # lpBuffer
    movq    $1026, %r8           # nNumberOfBytesToRead
    leaq    -1040(%rbp), %r9    # lpNumberOfBytesRead
    movq    $0, -1048(%rbp)     # lpOverlapped
                                # a fifth argument must be higher in the stack
                                # than the shadow space!
    call    ReadFile

    leaq    -1032(%rbp), %rdi # lpBuffer
    movq    -1040(%rbp), %rsi # numberOfBytesRead
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
    movq %rdi, %rcx
    call ExitProcess
