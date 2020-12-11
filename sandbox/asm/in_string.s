	.file	"in_string.c"
 # GNU C17 (Rev4, Built by MSYS2 project) version 10.2.0 (x86_64-w64-mingw32)
 #	compiled by GNU C version 10.2.0, GMP version 6.2.0, MPFR version 4.1.0, MPC version 1.2.0, isl version isl-0.22.1-GMP

 # GGC heuristics: --param ggc-min-expand=100 --param ggc-min-heapsize=131072
 # options passed: 
 # -iprefix C:/msys64/mingw64/bin/../lib/gcc/x86_64-w64-mingw32/10.2.0/
 # -D_REENTRANT in_string.c -mtune=generic -march=x86-64 -g -Wall -Wextra
 # -fno-asynchronous-unwind-tables -fno-exceptions -fno-rtti -fverbose-asm
 # options enabled:  -faggressive-loop-optimizations -fallocation-dce
 # -fauto-inc-dec -fdelete-null-pointer-checks -fdwarf2-cfi-asm
 # -fearly-inlining -feliminate-unused-debug-symbols
 # -feliminate-unused-debug-types -ffp-int-builtin-inexact -ffunction-cse
 # -fgcse-lm -fgnu-unique -fident -finline-atomics -fipa-stack-alignment
 # -fira-hoist-pressure -fira-share-save-slots -fira-share-spill-slots
 # -fivopts -fkeep-inline-dllexport -fkeep-static-consts
 # -fleading-underscore -flifetime-dse -fmath-errno -fmerge-debug-strings
 # -fpeephole -fpic -fplt -fprefetch-loop-arrays -freg-struct-return
 # -fsched-critical-path-heuristic -fsched-dep-count-heuristic
 # -fsched-group-heuristic -fsched-interblock -fsched-last-insn-heuristic
 # -fsched-rank-heuristic -fsched-spec -fsched-spec-insn-heuristic
 # -fsched-stalled-insns-dep -fschedule-fusion -fsemantic-interposition
 # -fset-stack-executable -fshow-column -fshrink-wrap-separate
 # -fsigned-zeros -fsplit-ivs-in-unroller -fssa-backprop -fstdarg-opt
 # -fstrict-volatile-bitfields -fsync-libcalls -ftrapping-math
 # -ftree-cselim -ftree-forwprop -ftree-loop-if-convert -ftree-loop-im
 # -ftree-loop-ivcanon -ftree-loop-optimize -ftree-parallelize-loops=
 # -ftree-phiprop -ftree-reassoc -ftree-scev-cprop -funit-at-a-time
 # -fverbose-asm -fzero-initialized-in-bss -m128bit-long-double -m64
 # -m80387 -maccumulate-outgoing-args -malign-double -malign-stringops
 # -mavx256-split-unaligned-load -mavx256-split-unaligned-store
 # -mfancy-math-387 -mfp-ret-in-387 -mfxsr -mieee-fp -mlong-double-80 -mmmx
 # -mms-bitfields -mno-sse4 -mpush-args -mred-zone -msse -msse2
 # -mstack-arg-probe -mstackrealign -mvzeroupper

	.text
.Ltext0:
	.cfi_sections	.debug_frame
	.def	__main;	.scl	2;	.type	32;	.endef
	.section .rdata,"dr"
.LC0:
	.ascii "Type a string:>\0"
	.text
	.globl	main
	.def	main;	.scl	2;	.type	32;	.endef
main:
.LFB4419:
	.file 1 "in_string.c"
	.loc 1 4 1
	.cfi_startproc
	pushq	%rbp	 #
	.cfi_def_cfa_offset 16
	.cfi_offset 6, -16
	movq	%rsp, %rbp	 #,
	.cfi_def_cfa_register 6
	pushq	%rdi	 #
	subq	$1144, %rsp	 #,
	.cfi_offset 5, -24
	movl	%ecx, 16(%rbp)	 # argc, argc
	movq	%rdx, 24(%rbp)	 # argv, argv
 # in_string.c:4: {
	.loc 1 4 1
	call	__main	 #
 # in_string.c:6:     HANDLE hOut = GetStdHandle(STD_OUTPUT_HANDLE);
	.loc 1 6 19
	movl	$-11, %ecx	 #,
	movq	__imp_GetStdHandle(%rip), %rax	 #, tmp85
	call	*%rax	 # tmp85
.LVL0:
	movq	%rax, -24(%rbp)	 # tmp86, hOut
 # in_string.c:7:     HANDLE hIn = GetStdHandle(STD_INPUT_HANDLE);
	.loc 1 7 18
	movl	$-10, %ecx	 #,
	movq	__imp_GetStdHandle(%rip), %rax	 #, tmp87
	call	*%rax	 # tmp87
.LVL1:
	movq	%rax, -32(%rbp)	 # tmp88, hIn
 # in_string.c:9:     char *szMsg = "Type a string:>";
	.loc 1 9 11
	leaq	.LC0(%rip), %rax	 #, tmp89
	movq	%rax, -40(%rbp)	 # tmp89, szMsg
 # in_string.c:10:     DWORD dwMsgLen = 15;
	.loc 1 10 11
	movl	$15, -44(%rbp)	 #, dwMsgLen
 # in_string.c:13:     WriteConsoleA(hOut, szMsg, dwMsgLen, &dwWritten, NULL);
	.loc 1 13 5
	leaq	-52(%rbp), %r8	 #, tmp90
	movl	-44(%rbp), %ecx	 # dwMsgLen, tmp91
	movq	-40(%rbp), %rdx	 # szMsg, tmp92
	movq	-24(%rbp), %rax	 # hOut, tmp93
	movq	$0, 32(%rsp)	 #,
	movq	%r8, %r9	 # tmp90,
	movl	%ecx, %r8d	 # tmp91,
	movq	%rax, %rcx	 # tmp93,
	movq	__imp_WriteConsoleA(%rip), %rax	 #, tmp94
	call	*%rax	 # tmp94
.LVL2:
 # in_string.c:15:     char buffer[1024] = { 0 };
	.loc 1 15 10
	movq	$0, -1088(%rbp)	 #, buffer
	movq	$0, -1080(%rbp)	 #, buffer
	leaq	-1072(%rbp), %rdx	 #, tmp95
	movl	$0, %eax	 #, tmp96
	movl	$126, %ecx	 #, tmp97
	movq	%rdx, %rdi	 # tmp95, tmp95
	rep stosq
 # in_string.c:16:     DWORD nNumberOfBytesToRead = 1024;
	.loc 1 16 11
	movl	$1024, -48(%rbp)	 #, nNumberOfBytesToRead
 # in_string.c:17:     DWORD dwNumberOfBytesRead = 0;
	.loc 1 17 11
	movl	$0, -1092(%rbp)	 #, dwNumberOfBytesRead
 # in_string.c:19:     ReadFile(hIn, buffer, nNumberOfBytesToRead, &dwNumberOfBytesRead, NULL);
	.loc 1 19 5
	leaq	-1092(%rbp), %r8	 #, tmp98
	movl	-48(%rbp), %ecx	 # nNumberOfBytesToRead, tmp99
	leaq	-1088(%rbp), %rdx	 #, tmp100
	movq	-32(%rbp), %rax	 # hIn, tmp101
	movq	$0, 32(%rsp)	 #,
	movq	%r8, %r9	 # tmp98,
	movl	%ecx, %r8d	 # tmp99,
	movq	%rax, %rcx	 # tmp101,
	movq	__imp_ReadFile(%rip), %rax	 #, tmp102
	call	*%rax	 # tmp102
.LVL3:
 # in_string.c:21:     WriteConsoleA(hOut, buffer, dwNumberOfBytesRead, &dwWritten, NULL);
	.loc 1 21 5
	movl	-1092(%rbp), %ecx	 # dwNumberOfBytesRead, dwNumberOfBytesRead.0_1
	leaq	-52(%rbp), %r8	 #, tmp103
	leaq	-1088(%rbp), %rdx	 #, tmp104
	movq	-24(%rbp), %rax	 # hOut, tmp105
	movq	$0, 32(%rsp)	 #,
	movq	%r8, %r9	 # tmp103,
	movl	%ecx, %r8d	 # dwNumberOfBytesRead.0_1,
	movq	%rax, %rcx	 # tmp105,
	movq	__imp_WriteConsoleA(%rip), %rax	 #, tmp106
	call	*%rax	 # tmp106
.LVL4:
 # in_string.c:23:     CloseHandle(hOut);
	.loc 1 23 5
	movq	-24(%rbp), %rax	 # hOut, tmp107
	movq	%rax, %rcx	 # tmp107,
	movq	__imp_CloseHandle(%rip), %rax	 #, tmp108
	call	*%rax	 # tmp108
.LVL5:
 # in_string.c:24:     CloseHandle(hIn);
	.loc 1 24 5
	movq	-32(%rbp), %rax	 # hIn, tmp109
	movq	%rax, %rcx	 # tmp109,
	movq	__imp_CloseHandle(%rip), %rax	 #, tmp110
	call	*%rax	 # tmp110
.LVL6:
 # in_string.c:26:     return 0;
	.loc 1 26 12
	movl	$0, %eax	 #, _17
 # in_string.c:27: }
	.loc 1 27 1
	movq	-8(%rbp), %rdi	 #,
	leave	
	.cfi_restore 6
	.cfi_restore 5
	.cfi_def_cfa 7, 8
	ret	
	.cfi_endproc
.LFE4419:
.Letext0:
	.file 2 "C:/msys64/mingw64/x86_64-w64-mingw32/include/minwindef.h"
	.file 3 "C:/msys64/mingw64/x86_64-w64-mingw32/include/winnt.h"
	.file 4 "C:/msys64/mingw64/x86_64-w64-mingw32/include/processenv.h"
	.file 5 "C:/msys64/mingw64/x86_64-w64-mingw32/include/wincon.h"
	.file 6 "C:/msys64/mingw64/x86_64-w64-mingw32/include/fileapi.h"
	.file 7 "C:/msys64/mingw64/x86_64-w64-mingw32/include/handleapi.h"
	.section	.debug_info,"dr"
.Ldebug_info0:
	.long	0x353
	.word	0x4
	.secrel32	.Ldebug_abbrev0
	.byte	0x8
	.uleb128 0x1
	.ascii "GNU C17 10.2.0 -mtune=generic -march=x86-64 -g -fno-asynchronous-unwind-tables -fno-exceptions -fno-rtti\0"
	.byte	0xc
	.ascii "in_string.c\0"
	.ascii "C:\\Users\\Mykola\\Documents\\src\\fs-coollang\\sandbox\\asm\0"
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
	.long	0xcc
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
	.long	0x13e
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
	.long	0x164
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
	.long	0x128
	.uleb128 0x7
	.ascii "main\0"
	.byte	0x1
	.byte	0x3
	.byte	0x5
	.long	0x115
	.quad	.LFB4419
	.quad	.LFE4419-.LFB4419
	.uleb128 0x1
	.byte	0x9c
	.long	0x314
	.uleb128 0x8
	.ascii "argc\0"
	.byte	0x1
	.byte	0x3
	.byte	0xe
	.long	0x115
	.uleb128 0x2
	.byte	0x91
	.sleb128 0
	.uleb128 0x8
	.ascii "argv\0"
	.byte	0x1
	.byte	0x3
	.byte	0x1a
	.long	0x1c2
	.uleb128 0x2
	.byte	0x91
	.sleb128 8
	.uleb128 0x9
	.ascii "hOut\0"
	.byte	0x1
	.byte	0x6
	.byte	0xc
	.long	0x199
	.uleb128 0x2
	.byte	0x91
	.sleb128 -40
	.uleb128 0x9
	.ascii "hIn\0"
	.byte	0x1
	.byte	0x7
	.byte	0xc
	.long	0x199
	.uleb128 0x2
	.byte	0x91
	.sleb128 -48
	.uleb128 0x9
	.ascii "szMsg\0"
	.byte	0x1
	.byte	0x9
	.byte	0xb
	.long	0x128
	.uleb128 0x2
	.byte	0x91
	.sleb128 -56
	.uleb128 0x9
	.ascii "dwMsgLen\0"
	.byte	0x1
	.byte	0xa
	.byte	0xb
	.long	0x166
	.uleb128 0x2
	.byte	0x91
	.sleb128 -60
	.uleb128 0x9
	.ascii "dwWritten\0"
	.byte	0x1
	.byte	0xb
	.byte	0xb
	.long	0x166
	.uleb128 0x3
	.byte	0x91
	.sleb128 -68
	.uleb128 0x9
	.ascii "buffer\0"
	.byte	0x1
	.byte	0xf
	.byte	0xa
	.long	0x314
	.uleb128 0x3
	.byte	0x91
	.sleb128 -1104
	.uleb128 0x9
	.ascii "nNumberOfBytesToRead\0"
	.byte	0x1
	.byte	0x10
	.byte	0xb
	.long	0x166
	.uleb128 0x2
	.byte	0x91
	.sleb128 -64
	.uleb128 0x9
	.ascii "dwNumberOfBytesRead\0"
	.byte	0x1
	.byte	0x11
	.byte	0xb
	.long	0x166
	.uleb128 0x3
	.byte	0x91
	.sleb128 -1108
	.uleb128 0xa
	.quad	.LVL0
	.long	0x325
	.uleb128 0xa
	.quad	.LVL1
	.long	0x325
	.uleb128 0xa
	.quad	.LVL2
	.long	0x331
	.uleb128 0xa
	.quad	.LVL3
	.long	0x33e
	.uleb128 0xa
	.quad	.LVL4
	.long	0x331
	.uleb128 0xa
	.quad	.LVL5
	.long	0x34a
	.uleb128 0xa
	.quad	.LVL6
	.long	0x34a
	.byte	0
	.uleb128 0xb
	.long	0xcc
	.long	0x325
	.uleb128 0xc
	.long	0xd4
	.word	0x3ff
	.byte	0
	.uleb128 0xd
	.secrel32	.LASF0
	.secrel32	.LASF0
	.byte	0x4
	.byte	0x39
	.byte	0x1c
	.uleb128 0xe
	.secrel32	.LASF1
	.secrel32	.LASF1
	.byte	0x5
	.word	0x10b
	.byte	0x1d
	.uleb128 0xd
	.secrel32	.LASF2
	.secrel32	.LASF2
	.byte	0x6
	.byte	0xb2
	.byte	0x1d
	.uleb128 0xd
	.secrel32	.LASF3
	.secrel32	.LASF3
	.byte	0x7
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
	.uleb128 0x1
	.byte	0x1
	.uleb128 0x49
	.uleb128 0x13
	.uleb128 0x1
	.uleb128 0x13
	.byte	0
	.byte	0
	.uleb128 0xc
	.uleb128 0x21
	.byte	0
	.uleb128 0x49
	.uleb128 0x13
	.uleb128 0x2f
	.uleb128 0x5
	.byte	0
	.byte	0
	.uleb128 0xd
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
	.uleb128 0xe
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
.LASF3:
	.ascii "CloseHandle\0"
.LASF2:
	.ascii "ReadFile\0"
.LASF0:
	.ascii "GetStdHandle\0"
.LASF1:
	.ascii "WriteConsoleA\0"
	.ident	"GCC: (Rev4, Built by MSYS2 project) 10.2.0"
