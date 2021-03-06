# as -o hello.o hello.gas
# ld -o hello.exe hello.o -L"C:/msys64/mingw64/x86_64-w64-mingw32/lib" -lkernel32

    .data

helloMsg:
    .ascii "Hello, MinGW64!\n"
hStdOut: 
    .quad 0
dwWritten: 
    .quad 0

    .text
    .global main

    .set STD_INPUT_HANDLE, -10
    .set STD_OUTPUT_HANDLE, -11
    .set STD_ERROR_HANDLE, -12

    .global _start
_start:
  movl $STD_OUTPUT_HANDLE, %ecx
  call GetStdHandle
  movq %rax, hStdOut

# BOOL WINAPI WriteConsole(
#   _In_             HANDLE  hConsoleOutput,
#   _In_       const VOID    *lpBuffer,
#   _In_             DWORD   nNumberOfCharsToWrite,
#   _Out_opt_        LPDWORD lpNumberOfCharsWritten,
#   _Reserved_       LPVOID  lpReserved
# );
  movq hStdOut, %rcx
  movq $helloMsg, %rdx
  movq $16, %r8
  movq $dwWritten, %r9
  pushq $0
  call WriteConsoleA

  movq $0, %rcx
  call ExitProcess
