; dumpbin.exe /exports "C:\Program Files (x86)\Windows Kits\10\Lib\10.0.17763.0\ucrt\x64\ucrt.lib" > ucrt.txt
; nasm -f win64 -o hello_world.obj hello_world.asm
; x64/link hello_world.obj /subsystem:console /entry:main kernel32.lib ucrt.lib

; link hello_world.obj /subsystem:console /entry:main /libpath:"C:/Program Files (x86)/Windows Kits/10/Lib/10.0.17763.0/um/x64" /libpath:"C:/Program Files (x86)/Windows Kits/10/Lib/10.0.17763.0/ucrt/x64" kernel32.lib ucrt.lib

; To use printf: link hello_world.obj /subsystem:console /entry:main kernel32.lib ucrt.lib legacy_stdio_definitions.lib

; To get rid of the warning "legacy_stdio_wide_specifiers.lib(legacy_stdio_wide_specifiers.obj) : warning LNK4210: .CRT section exists; there may be unhandled static initializers or terminators" 
; x64/link hello_world.obj /subsystem:console kernel32.lib msvcrt.lib legacy_stdio_definitions.lib

bits 64
default rel

segment .data
    msg db "Hello world!", 0xd, 0xa, 0

segment .text

global main

extern ExitProcess
; extern _cputs
extern printf

main:
    push    rbp
    mov     rbp, rsp
    sub     rsp, 32

    lea     rcx, [msg]
    call    printf
    ; call    _cputs

    xor     rax, rax
    call    ExitProcess
