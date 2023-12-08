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

MSG_INIT_FAILED_ASCII:          .ascii "Init failed"
MSG_INIT_FAILED_LEN  =                 (. - MSG_INIT_FAILED_ASCII)


########################################
# Global vars
########################################
    .global .Platform.heap_start
.Platform.heap_start:    .quad 0

    .global .Platform.heap_end
.Platform.heap_end:      .quad 0

# `.Platform.alloc` uses a multiple of this size to reserve and commit 
# additional memory from the OS.
# The value is in bytes and we assume is always a power of 2.
pageSize:                .quad 0

########################################
# Text
#
# Under the Microsoft x64 calling convention:
# %rax, %rcx, %rdx, %r8, %r9, %r10, and %r11 are volatile, 
# %rbx, %rbp, %rdi, %rsi, %rsp, and %r12 through %r15 are non-volatile and must be saved be the callee if used.
########################################
    .text

    STD_INPUT_HANDLE  = -10
    STD_OUTPUT_HANDLE = -11
    STD_ERROR_HANDLE  = -12

    MEM_RESERVE       = 0x00002000
    MEM_COMMIT        = 0x00001000
    PAGE_READWRITE    = 0x00000004

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
#  Calls `GetSystemInfo` and calculates `pageSize` 
#  for later use by `.Platform.alloc`. 
#  Reserve `RESERVED_PAGES x pageSize` contiguous virtual addresses range.
#  Initializes `.Platform.heap_start`, `.Platform.heap_end` with
#  the base address of the reserved range.
#
    .global .Platform.init
.Platform.init:

    SHADOW_SPACE_SIZE                = 32
    SYSTEM_INFO_SIZE                 = 48
    FRAME_SIZE                       = SYSTEM_INFO_SIZE + SHADOW_SPACE_SIZE
    SYSTEM_INFO_OFFSET               = -SYSTEM_INFO_SIZE
    DW_PAGE_SIZE_OFFSET              = SYSTEM_INFO_OFFSET + 4

    pushq   %rbp
    movq    %rsp, %rbp

    subq    $FRAME_SIZE, %rsp

    # void GetSystemInfo(
    #   [out] LPSYSTEM_INFO lpSystemInfo
    # );
    leaq    SYSTEM_INFO_OFFSET(%rbp), %rcx      # lpSystemInfo
    call    GetSystemInfo

    # 32-bit operands generate a 32-bit result, 
    # zero-extended to a 64-bit result in the destination general-purpose register.
    movl    DW_PAGE_SIZE_OFFSET(%rbp), %eax
    movq    %rax, pageSize

    # We reserve a 655360-pages-long (2.5GB for 4KB page) contiguous virtual addresses range
    # and set `.Platform.heap_start`, `.Platform.heap_end` to
    # the base address of the reserved range -- this is our heap.
    # `.Platform.alloc` commits memory withing the reserved range
    # whenever expanding the heap is necessary.

    # LPVOID VirtualAlloc(
    #   [in, optional] LPVOID lpAddress,
    #   [in]           SIZE_T dwSize,
    #   [in]           DWORD  flAllocationType,
    #   [in]           DWORD  flProtect
    # );

    xor     %ecx, %ecx                          # lpAddress = NULL
    
    # dwSize
    # 655360 * pageSize
    # 655360 = 524288 + 131072 = 2**19 + 2**17
    movq    pageSize, %rdx
    movq    %rdx, %rax
    salq    $19, %rdx
    salq    $17, %rax
    addq    %rax, %rdx
    
    movq    $MEM_RESERVE, %r8                   # flAllocationType
    movq    $PAGE_READWRITE, %r9                # flProtect
    call    VirtualAlloc

    testq   %rax, %rax
    jnz     .Platform.init.ok
 
    # Allocation failed
    call    GetLastError

    movq    $MSG_INIT_FAILED_ASCII, %rdi
    movq    $MSG_INIT_FAILED_LEN, %rsi
    call    .Platform.out_string

    movq    $.Platform.ascii_new_line, %rdi
    movq    $.Platform.new_line_len, %rsi
    call    .Platform.out_string

    jmp     .Runtime.abort_out_of_mem
 
.Platform.init.ok:
    movq    %rax, .Platform.heap_start
    movq    %rax, .Platform.heap_end

    movq    %rbp, %rsp
    popq    %rbp

    ret

#
#  .Platform.alloc
#
#  INPUT:
#      %rdi: the requested size in bytes
#  OUTPUT:
#      %rax: a pointer to the start of allocated memory block.
#.     if allocation fails, prints a message and exits the process.
#  
#  Rounds up (%rdi * 8) bytes to the nearest greater multiple of pageSize.
#  Allocates that amount of physical memory 
#  adding it to the end of contiguous region [.Platform.heap_start, .Platform.heap_end]. 
#
    .global .Platform.alloc
.Platform.alloc:
    DW_SIZE_SIZE        = 8
    DW_SIZE_OFFSET      = -DW_SIZE_SIZE
    PADDING_SIZE        = 8
    SHADOW_SPACE_SIZE   = 32
    FRAME_SIZE          = DW_SIZE_SIZE + PADDING_SIZE + SHADOW_SPACE_SIZE

    pushq   %rbp
    movq    %rsp, %rbp

    subq    $FRAME_SIZE, %rsp

    # LPVOID VirtualAlloc(
    #   [in, optional] LPVOID lpAddress,
    #   [in]           SIZE_T dwSize,
    #   [in]           DWORD  flAllocationType,
    #   [in]           DWORD  flProtect
    # );
    movq    .Platform.heap_end, %rcx            # lpAddress

    # dwSize
    movq    %rdi, %rdx                          # requested size in bytes

    # round up the requested size in bytes to the nearest greater multiple of pageSize
    # e.g., for pageSize = 4KB = 4096, we effectively do
    # %rdx = (%rdx + 4095) & (-4096)
    movq    pageSize, %rax                      # 4096 = ..._0000_0001_0000_0000_0000
    decq    %rax                                # 4095 = ..._0000_0000_1111_1111_1111
    addq    %rax, %rdx                          # %rdx + 4095
    notq    %rax                                # -4096 = ..._1111_1111_0000_0000_0000
    andq    %rax, %rdx                          # & (-4096)
    movq    %rdx, DW_SIZE_OFFSET(%rbp)          # preserve dwSize
    
    movq    $MEM_COMMIT, %r8                    # flAllocationType
    movq    $PAGE_READWRITE, %r9                # flProtect
    call    VirtualAlloc

    testq   %rax, %rax
    jnz     .Platform.alloc.ok
 
    # Allocation failed
    call    GetLastError
    jmp     .Runtime.abort_out_of_mem
 
.Platform.alloc.ok:
    movq    DW_SIZE_OFFSET(%rbp), %rdx          # load dwSize
    addq    %rax, %rdx                          # %rdx = dwSize + the allocation's base address
    movq    %rdx, .Platform.heap_end            # advance the heap end pointer

    movq    %rbp, %rsp
    popq    %rbp

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

    # -14       16 byte boundary padding
    # -1040     buffer 
    # -1048     numberOfBytesRead
    # -1056     fifth argument (lpOverlapped)
    # -1088     shadow space
    subq    $1088, %rsp

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
    leaq    -1040(%rbp), %rdx   # lpBuffer
    movq    $1026, %r8          # nNumberOfBytesToRead
    leaq    -1048(%rbp), %r9    # lpNumberOfBytesRead
    movq    $0, -1056(%rbp)     # lpOverlapped
                                # a fifth argument must be higher in the stack
                                # than the shadow space!
    call    ReadFile

    leaq    -1040(%rbp), %rdi # lpBuffer
    movq    -1048(%rbp), %rsi # numberOfBytesRead
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
