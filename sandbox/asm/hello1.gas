# as -o hello1.o hello1.gas
# ld -o hello1.exe hello1.o -L"C:/msys64/mingw64/x86_64-w64-mingw32/lib" -lkernel32

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
  push %rbp
  movq %rsp, %rbp

  subq $(8 + 8 + 32), %rsp # shadow space
  
  movl $STD_OUTPUT_HANDLE, %ecx
  call GetStdHandle
  movq %rax, %rcx

# BOOL WriteFile(
#   HANDLE       hFile,
#   LPCVOID      lpBuffer,
#   DWORD        nNumberOfBytesToWrite,
#   LPDWORD      lpNumberOfBytesWritten,
#   LPOVERLAPPED lpOverlapped
# );
  # movq hStdOut, %rcx
  movq $helloMsg, %rdx
  movq $16, %r8
  leaq -8(%rbp), %r9
  pushq $0
  call WriteFile

  movq $0, %rcx
  call ExitProcess

  movq %rbp, %rsp
  popq %rbp
