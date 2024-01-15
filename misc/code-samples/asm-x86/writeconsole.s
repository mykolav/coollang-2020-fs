	.file	"writeconsole.c"
	.text
.Ltext0:
	.cfi_sections	.debug_frame
	.def	__main;	.scl	2;	.type	32;	.endef
	.section .rdata,"dr"
.LC0:
	.ascii "Hello, Console!\0"
	.text
	.globl	main
	.def	main;	.scl	2;	.type	32;	.endef
	.seh_proc	main
main:
.LFB4419:
	.file 1 "writeconsole.c"
	.loc 1 4 1
	.cfi_startproc
	pushq	%rbp
	.seh_pushreg	%rbp
	.cfi_def_cfa_offset 16
	.cfi_offset 6, -16
	movq	%rsp, %rbp
	.seh_setframe	%rbp, 0
	.cfi_def_cfa_register 6
	subq	$80, %rsp
	.seh_stackalloc	80
	.seh_endprologue
	movl	%ecx, 16(%rbp)
	movq	%rdx, 24(%rbp)
	.loc 1 4 1
	call	__main
	.loc 1 5 11
	leaq	.LC0(%rip), %rax
	movq	%rax, -8(%rbp)
	.loc 1 6 11
	movl	$15, -12(%rbp)
	.loc 1 9 19
	movl	$-11, %ecx
	movq	__imp_GetStdHandle(%rip), %rax
	call	*%rax
.LVL0:
	movq	%rax, -24(%rbp)
	.loc 1 10 5
	leaq	-28(%rbp), %r8
	movl	-12(%rbp), %ecx
	movq	-8(%rbp), %rdx
	movq	-24(%rbp), %rax
	movq	$0, 32(%rsp)
	movq	%r8, %r9
	movl	%ecx, %r8d
	movq	%rax, %rcx
	movq	__imp_WriteConsoleA(%rip), %rax
	call	*%rax
.LVL1:
	.loc 1 11 5
	movq	-24(%rbp), %rax
	movq	%rax, %rcx
	movq	__imp_CloseHandle(%rip), %rax
	call	*%rax
.LVL2:
	.loc 1 13 12
	movl	$0, %eax
	.loc 1 14 1
	addq	$80, %rsp
	popq	%rbp
	.cfi_restore 6
	.cfi_def_cfa 7, 8
	ret
	.cfi_endproc
.LFE4419:
	.seh_endproc
.Letext0:
	.file 2 "C:/msys64/mingw64/x86_64-w64-mingw32/include/minwindef.h"
	.file 3 "C:/msys64/mingw64/x86_64-w64-mingw32/include/winnt.h"
	.file 4 "C:/msys64/mingw64/x86_64-w64-mingw32/include/processenv.h"
	.file 5 "C:/msys64/mingw64/x86_64-w64-mingw32/include/wincon.h"
	.file 6 "C:/msys64/mingw64/x86_64-w64-mingw32/include/handleapi.h"
	.section	.debug_info,"dr"
.Ldebug_info0:
	.long	0x268
	.word	0x4
	.secrel32	.Ldebug_abbrev0
	.byte	0x8
	.uleb128 0x1
	.ascii "GNU C17 10.2.0 -mtune=generic -march=x86-64 -g\0"
	.byte	0xc
	.ascii "writeconsole.c\0"
	.ascii "c:\\Users\\Mykola\\Documents\\src\\fs-coollang\\sandbox\\asm\0"
	.quad	.Ltext0
	.quad	.Letext0-.Ltext0
	.secrel32	.Ldebug_line0
	.uleb128 0x2
	.byte	0x1
	.byte	0x6
	.ascii "char\0"
	.uleb128 0x2
	.byte	0x8
	.byte	0x7
	.ascii "long long unsigned int\0"
	.uleb128 0x2
	.byte	0x8
	.byte	0x5
	.ascii "long long int\0"
	.uleb128 0x2
	.byte	0x2
	.byte	0x7
	.ascii "short unsigned int\0"
	.uleb128 0x2
	.byte	0x4
	.byte	0x5
	.ascii "int\0"
	.uleb128 0x2
	.byte	0x4
	.byte	0x5
	.ascii "long int\0"
	.uleb128 0x3
	.byte	0x8
	.long	0x95
	.uleb128 0x2
	.byte	0x4
	.byte	0x7
	.ascii "unsigned int\0"
	.uleb128 0x2
	.byte	0x4
	.byte	0x7
	.ascii "long unsigned int\0"
	.uleb128 0x2
	.byte	0x1
	.byte	0x8
	.ascii "unsigned char\0"
	.uleb128 0x4
	.byte	0x8
	.uleb128 0x5
	.ascii "DWORD\0"
	.byte	0x2
	.byte	0x8d
	.byte	0x1d
	.long	0x107
	.uleb128 0x2
	.byte	0x4
	.byte	0x4
	.ascii "float\0"
	.uleb128 0x2
	.byte	0x1
	.byte	0x6
	.ascii "signed char\0"
	.uleb128 0x2
	.byte	0x2
	.byte	0x5
	.ascii "short int\0"
	.uleb128 0x6
	.ascii "HANDLE\0"
	.byte	0x3
	.word	0x195
	.byte	0x11
	.long	0x12d
	.uleb128 0x2
	.byte	0x8
	.byte	0x4
	.ascii "double\0"
	.uleb128 0x2
	.byte	0x10
	.byte	0x4
	.ascii "long double\0"
	.uleb128 0x3
	.byte	0x8
	.long	0xf1
	.uleb128 0x7
	.ascii "main\0"
	.byte	0x1
	.byte	0x3
	.byte	0x5
	.long	0xde
	.quad	.LFB4419
	.quad	.LFE4419-.LFB4419
	.uleb128 0x1
	.byte	0x9c
	.long	0x246
	.uleb128 0x8
	.ascii "argc\0"
	.byte	0x1
	.byte	0x3
	.byte	0xe
	.long	0xde
	.uleb128 0x2
	.byte	0x91
	.sleb128 0
	.uleb128 0x8
	.ascii "argv\0"
	.byte	0x1
	.byte	0x3
	.byte	0x1a
	.long	0x18b
	.uleb128 0x2
	.byte	0x91
	.sleb128 8
	.uleb128 0x9
	.ascii "szMsg\0"
	.byte	0x1
	.byte	0x5
	.byte	0xb
	.long	0xf1
	.uleb128 0x2
	.byte	0x91
	.sleb128 -24
	.uleb128 0x9
	.ascii "dwMsgLen\0"
	.byte	0x1
	.byte	0x6
	.byte	0xb
	.long	0x12f
	.uleb128 0x2
	.byte	0x91
	.sleb128 -28
	.uleb128 0x9
	.ascii "dwWritten\0"
	.byte	0x1
	.byte	0x7
	.byte	0xb
	.long	0x12f
	.uleb128 0x2
	.byte	0x91
	.sleb128 -44
	.uleb128 0x9
	.ascii "hOut\0"
	.byte	0x1
	.byte	0x9
	.byte	0xc
	.long	0x162
	.uleb128 0x2
	.byte	0x91
	.sleb128 -40
	.uleb128 0xa
	.quad	.LVL0
	.long	0x246
	.uleb128 0xa
	.quad	.LVL1
	.long	0x252
	.uleb128 0xa
	.quad	.LVL2
	.long	0x25f
	.byte	0
	.uleb128 0xb
	.secrel32	.LASF0
	.secrel32	.LASF0
	.byte	0x4
	.byte	0x39
	.byte	0x1c
	.uleb128 0xc
	.secrel32	.LASF1
	.secrel32	.LASF1
	.byte	0x5
	.word	0x10b
	.byte	0x1d
	.uleb128 0xb
	.secrel32	.LASF2
	.secrel32	.LASF2
	.byte	0x6
	.byte	0x13
	.byte	0x1d
	.byte	0
	.section	.debug_abbrev,"dr"
.Ldebug_abbrev0:
	.uleb128 0x1
	.uleb128 0x11
	.byte	0x1
	.uleb128 0x25
	.uleb128 0x8
	.uleb128 0x13
	.uleb128 0xb
	.uleb128 0x3
	.uleb128 0x8
	.uleb128 0x1b
	.uleb128 0x8
	.uleb128 0x11
	.uleb128 0x1
	.uleb128 0x12
	.uleb128 0x7
	.uleb128 0x10
	.uleb128 0x17
	.byte	0
	.byte	0
	.uleb128 0x2
	.uleb128 0x24
	.byte	0
	.uleb128 0xb
	.uleb128 0xb
	.uleb128 0x3e
	.uleb128 0xb
	.uleb128 0x3
	.uleb128 0x8
	.byte	0
	.byte	0
	.uleb128 0x3
	.uleb128 0xf
	.byte	0
	.uleb128 0xb
	.uleb128 0xb
	.uleb128 0x49
	.uleb128 0x13
	.byte	0
	.byte	0
	.uleb128 0x4
	.uleb128 0xf
	.byte	0
	.uleb128 0xb
	.uleb128 0xb
	.byte	0
	.byte	0
	.uleb128 0x5
	.uleb128 0x16
	.byte	0
	.uleb128 0x3
	.uleb128 0x8
	.uleb128 0x3a
	.uleb128 0xb
	.uleb128 0x3b
	.uleb128 0xb
	.uleb128 0x39
	.uleb128 0xb
	.uleb128 0x49
	.uleb128 0x13
	.byte	0
	.byte	0
	.uleb128 0x6
	.uleb128 0x16
	.byte	0
	.uleb128 0x3
	.uleb128 0x8
	.uleb128 0x3a
	.uleb128 0xb
	.uleb128 0x3b
	.uleb128 0x5
	.uleb128 0x39
	.uleb128 0xb
	.uleb128 0x49
	.uleb128 0x13
	.byte	0
	.byte	0
	.uleb128 0x7
	.uleb128 0x2e
	.byte	0x1
	.uleb128 0x3f
	.uleb128 0x19
	.uleb128 0x3
	.uleb128 0x8
	.uleb128 0x3a
	.uleb128 0xb
	.uleb128 0x3b
	.uleb128 0xb
	.uleb128 0x39
	.uleb128 0xb
	.uleb128 0x27
	.uleb128 0x19
	.uleb128 0x49
	.uleb128 0x13
	.uleb128 0x11
	.uleb128 0x1
	.uleb128 0x12
	.uleb128 0x7
	.uleb128 0x40
	.uleb128 0x18
	.uleb128 0x2116
	.uleb128 0x19
	.uleb128 0x1
	.uleb128 0x13
	.byte	0
	.byte	0
	.uleb128 0x8
	.uleb128 0x5
	.byte	0
	.uleb128 0x3
	.uleb128 0x8
	.uleb128 0x3a
	.uleb128 0xb
	.uleb128 0x3b
	.uleb128 0xb
	.uleb128 0x39
	.uleb128 0xb
	.uleb128 0x49
	.uleb128 0x13
	.uleb128 0x2
	.uleb128 0x18
	.byte	0
	.byte	0
	.uleb128 0x9
	.uleb128 0x34
	.byte	0
	.uleb128 0x3
	.uleb128 0x8
	.uleb128 0x3a
	.uleb128 0xb
	.uleb128 0x3b
	.uleb128 0xb
	.uleb128 0x39
	.uleb128 0xb
	.uleb128 0x49
	.uleb128 0x13
	.uleb128 0x2
	.uleb128 0x18
	.byte	0
	.byte	0
	.uleb128 0xa
	.uleb128 0x4109
	.byte	0
	.uleb128 0x11
	.uleb128 0x1
	.uleb128 0x31
	.uleb128 0x13
	.byte	0
	.byte	0
	.uleb128 0xb
	.uleb128 0x2e
	.byte	0
	.uleb128 0x3f
	.uleb128 0x19
	.uleb128 0x3c
	.uleb128 0x19
	.uleb128 0x6e
	.uleb128 0xe
	.uleb128 0x3
	.uleb128 0xe
	.uleb128 0x3a
	.uleb128 0xb
	.uleb128 0x3b
	.uleb128 0xb
	.uleb128 0x39
	.uleb128 0xb
	.byte	0
	.byte	0
	.uleb128 0xc
	.uleb128 0x2e
	.byte	0
	.uleb128 0x3f
	.uleb128 0x19
	.uleb128 0x3c
	.uleb128 0x19
	.uleb128 0x6e
	.uleb128 0xe
	.uleb128 0x3
	.uleb128 0xe
	.uleb128 0x3a
	.uleb128 0xb
	.uleb128 0x3b
	.uleb128 0x5
	.uleb128 0x39
	.uleb128 0xb
	.byte	0
	.byte	0
	.byte	0
	.section	.debug_aranges,"dr"
	.long	0x2c
	.word	0x2
	.secrel32	.Ldebug_info0
	.byte	0x8
	.byte	0
	.word	0
	.word	0
	.quad	.Ltext0
	.quad	.Letext0-.Ltext0
	.quad	0
	.quad	0
	.section	.debug_line,"dr"
.Ldebug_line0:
	.section	.debug_str,"dr"
.LASF0:
	.ascii "GetStdHandle\0"
.LASF1:
	.ascii "WriteConsoleA\0"
.LASF2:
	.ascii "CloseHandle\0"
	.ident	"GCC: (Rev4, Built by MSYS2 project) 10.2.0"
