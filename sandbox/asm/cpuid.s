# as -o cpuid.o cpuid.gas
# ld -o cpuid.exe cpuid.o -L"C:/msys64/mingw64/x86_64-w64-mingw32/lib" -lkernel32
# This works too! ld -o cpuid.exe cpuid.o -L"C:/Program Files (x86)/Windows Kits/10/Lib/10.0.17763.0/um/x64" -lkernel32
# ld -o cpuid.exe cpuid.o -L/c/mingw-w64/x86_64-8.1.0-posix-seh-rt_v6-rev0/mingw64/x86_64-w64-mingw32/lib -lkernel32

    .data

cpuidMsg:
    .ascii "The processor vendor ID is 'xxxxxxxxxxxxx'\n"
hStdOut: 
    .quad 0
dwWritten: 
    .quad 0

    .text
    .global main

    .set STD_INPUT_HANDLE, -10
    .set STD_OUTPUT_HANDLE, -11
    .set STD_ERROR_HANDLE, -12

main:
  movq $0, %rax
  cpuid
  movl $cpuidMsg, %edi
  movl %ebx, 28(%edi)
  movl %edx, 32(%edi)
  movl %ecx, 36(%edi)

  movl $STD_OUTPUT_HANDLE, %ecx
  call *__imp_GetStdHandle(%rip)
  movq %rax, hStdOut

# BOOL WINAPI WriteConsole(
#   _In_             HANDLE  hConsoleOutput,
#   _In_       const VOID    *lpBuffer,
#   _In_             DWORD   nNumberOfCharsToWrite,
#   _Out_opt_        LPDWORD lpNumberOfCharsWritten,
#   _Reserved_       LPVOID  lpReserved
# );
  movq hStdOut, %rcx
  movq $cpuidMsg, %rdx
  movq $43, %r8
  movq $dwWritten, %r9
  pushq $0
  call *__imp_WriteConsoleA(%rip)

  movq $0, %rcx
  call *__imp_ExitProcess(%rip)
