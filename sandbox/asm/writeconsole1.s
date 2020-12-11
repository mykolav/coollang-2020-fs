	.text
.LC0:
	.ascii "Hello, Console!\0"
	.text
	.globl	main
main1:
.LFB4419:
	pushq	%rbp
	movq	%rsp, %rbp

	subq	$80, %rsp

	leaq	.LC0(%rip), %rax
	movq	%rax, -8(%rbp)
	movl	$15, -12(%rbp) # dwMsgLen

	movl	$-11, %ecx
	# movq	__imp_GetStdHandle(%rip), %rax
	# call	*%rax
    call GetStdHandle

.LVL0:
	movq	%rax, -24(%rbp) # hOut
	leaq	-28(%rbp), %r8
	movl	-12(%rbp), %ecx	# dwMsgLen
	movq	-8(%rbp), %rdx  # szMsg
	movq	-24(%rbp), %rax # hOut
	movq	$0, 32(%rsp)
	movq	%r8, %r9
	movl	%ecx, %r8d
	movq	%rax, %rcx
	#movq	__imp_WriteConsoleA(%rip), %rax	 #, tmp91
	#call	*%rax	 # tmp91
    call WriteConsoleA

.LVL1:
	movq	-24(%rbp), %rax
	movq	%rax, %rcx
	#movq	__imp_CloseHandle(%rip), %rax
	#call	*%rax
    call CloseHandle
.LVL2:
	movl	$0, %eax
	
    addq	$80, %rsp
    
    movq    %rbp, %rsp
    popq    %rbp
	ret	
.LFE4419:

main:
	pushq	%rbp
	movq	%rsp, %rbp
    
    call main1

    movq %rbp, %rsp
    popq %rbp

    ret
