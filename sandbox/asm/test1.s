    .data
int_const0:
    .quad 0 # tag
    .quad 0 # size
    .quad 0 # vtable
    .quad 0 # value

    .text
    movq $int_const0, %rax
    xorq $0xFFFFFFFFFFFFFFFF, 24(%rax)
    negq 24(%rax)
    cmpq $0, 24(%rax)
    # addq 24(%rax), 24(%rbx)
    idivq 24(%rdi)
    setl %r8b
    movzbq %r8b, %rax
    movq    $11, %r8
    subq    -48(%rbp), %r8 # 11 - digit_pos
    addq    %rax, -48(%rbp)
    # movq    -16(%rbp), 8(%rax)
    # idivq   $10
    # end if
    retq
