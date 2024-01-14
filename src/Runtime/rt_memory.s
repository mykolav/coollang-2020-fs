########################################
# Data
########################################

    .data
########################################
# Memory Manager Globals
########################################

# The compiler emits the following globals
#   .MemoryManager.FN_INIT
#   .MemoryManager.FN_COLLECT
#   .MemoryManager.IS_TESTING

    .global .Alloc.ptr
.Alloc.ptr:                      .quad 0
    .global .Alloc.limit
.Alloc.limit:                    .quad 0

#
# TODO: Place strings and other constants in `.section .rodata` instead of `.data`.
#
########################################
# MemoryManager messages
########################################

    .global .MM.MSG_ALLOC_SIZE_EQ_ASCII
.MM.MSG_ALLOC_SIZE_EQ_ASCII:     .ascii "MM:  Alloc size    = "
.MM.MSG_ALLOC_SIZE_EQ_LEN  =             (. - .MM.MSG_ALLOC_SIZE_EQ_ASCII)

    .global .MM.MSG_ALLOC_PTR_EQ_ASCII
.MM.MSG_ALLOC_PTR_EQ_ASCII:      .ascii "MM: .Alloc.ptr     = "
.MM.MSG_ALLOC_PTR_EQ_LEN  =             (. - .MM.MSG_ALLOC_PTR_EQ_ASCII)

    .global .MM.MSG_ALLOC_LIMIT_EQ_ASCII
.MM.MSG_ALLOC_LIMIT_EQ_ASCII:    .ascii "MM: .Alloc.limit   = "
.MM.MSG_ALLOC_LIMIT_EQ_LEN  =           (. - .MM.MSG_ALLOC_LIMIT_EQ_ASCII)

########################################
# Common GC messages
########################################

    .global .GC.MSG_INTERNAL_ERROR_ASCII
.GC.MSG_INTERNAL_ERROR_ASCII:    .ascii "GC: The garbage collector encountered an internal error"
.GC.MSG_INTERNAL_ERROR_LEN  =           (. - .GC.MSG_INTERNAL_ERROR_ASCII)

########################################
# NopGC garabge collector messages
########################################

    .global .NopGC.MSG_COLLECTING_ASCII
.NopGC.MSG_COLLECTING_ASCII:           .ascii "NopGC: Increasing heap..."
.NopGC.MSG_COLLECTING_LEN  =                  (. - .NopGC.MSG_COLLECTING_ASCII)

.NopGC.MSG_HEAP_START_EQ_ASCII:        .ascii "NopGC: HEAP START  = "
.NopGC.MSG_HEAP_START_EQ_LEN =                (. - .NopGC.MSG_HEAP_START_EQ_ASCII)

.NopGC.MSG_HEAP_END_EQ_ASCII:          .ascii "NopGC: HEAP END    = "
.NopGC.MSG_HEAP_END_EQ_LEN =                  (. - .NopGC.MSG_HEAP_END_EQ_ASCII)

.NopGC.MSG_ALLOC_PTR_EQ_ASCII:         .ascii "NopGC: ALLOC PTR   = "
.NopGC.MSG_ALLOC_PTR_EQ_LEN =                 (. - .NopGC.MSG_ALLOC_PTR_EQ_ASCII)

.NopGC.MSG_ALLOC_LIMIT_EQ_ASCII:       .ascii "NopGC: ALLOC LIMIT = "
.NopGC.MSG_ALLOC_LIMIT_EQ_LEN =               (. - .NopGC.MSG_ALLOC_LIMIT_EQ_ASCII)

########################################
# Text
#
# The following memory management and garbage collection code has been
# adapted from [the Cool runtime system](https://theory.stanford.edu/~aiken/software/cooldist/lib/trap.handler).
#
# The source code in trap.handler is covered by the following copyright notice.
#
# Copyright (c) 1995,1996 The Regents of the University of California.
# All rights reserved.
#
# Permission to use, copy, modify, and distribute this software
# for any purpose, without fee, and without written agreement is
# hereby granted, provided that the above copyright notice and the following
# two paragraphs appear in all copies of this software.
#
# IN NO EVENT SHALL THE UNIVERSITY OF CALIFORNIA BE LIABLE TO ANY PARTY FOR
# DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES ARISING OUT
# OF THE USE OF THIS SOFTWARE AND ITS DOCUMENTATION, EVEN IF THE UNIVERSITY OF
# CALIFORNIA HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#
# THE UNIVERSITY OF CALIFORNIA SPECIFICALLY DISCLAIMS ANY WARRANTIES,
# INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
# AND FITNESS FOR A PARTICULAR PURPOSE.  THE SOFTWARE PROVIDED HEREUNDER IS
# ON AN "AS IS" BASIS, AND THE UNIVERSITY OF CALIFORNIA HAS NO OBLIGATION TO
# PROVIDE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.
########################################
    .text

    .include "constants.inc"

########################################
# Memory Manager
#
#   The MemMgr functions give a consistent view of the garbage collectors.
#   This allows multiple collectors to exist in one file and the easy
#   selection of different collectors.  It includes functions to initialize
#   the collector and to reserve memory and query its status.
#
#   The following assumptions are made:
#
#     1) The allocation of memory involves incrementing the $gp pointer.
#        The $s7 pointer serves as a limit.  The collector function is
#        called before $s7 is exceeded by $gp.
#
#     2) The initialization functions all take the same arguments as
#        defined in ".MemoryManager.init".
#
#     3) The garbage collector functions all take the following arguments.
#        %rdi: the allocation size in bytes needed by the program,
#              must be preserved across the function call.
#        %rsi: the tip of the stack to start checking for pointers from.
#              (remember the stack grows down, 
#               so the tip is at the lowest address)
#
########################################

#
# Initialize the Memory Manager
#
#   Call the initialization routine for the garbage collector.
#
#   INPUT:
#    %rdi: the base of stack to stop checking for GC roots at.
#          (remember the stack grows down, 
#           so the base is at the highest address)
#
#   OUTPUT:
#    none
#
#   Registers modified:
#    .MemoryManager.FN_INIT
#

    .global .MemoryManager.init
.MemoryManager.init:
    pushq    %rbp
    movq     %rsp, %rbp

    # pointer to the init procedure of a GC implementation
    callq    *.MemoryManager.FN_INIT(%rip)

    movq     %rbp, %rsp
    popq     %rbp
    ret

#
# Record an Assignment in the Assignment Stack
#
#   The GC's code guarantees `.Alloc.ptr` is always strictly less than `.Alloc.limit`.
#   Hence there is always enough room for at least one assignment record 
#   at the tip of assignment stack.
#
#   If the above invariant hadn't been maintained, we would've ended up
#   in a situation where at the moment we're requested to record an 
#   assignment, `.Alloc.ptr` == `.Alloc.limit`. As there is no room to 
#   record the assignment a garbage collection has to run first. 
#   As the unrecorded assignment can point to a GC root, the garbage 
#   collection would've missed that root and removed a live object or 
#   multiple live objects...
#
#   INPUT:
#    %rdi: pointer to the pointer being assigned to
#
#   OUTPUT:
#    None
#
#   Registers modified:
#    .MemoryManager.FN_ON_ASSIGN
#

    .global .MemoryManager.on_assign
.MemoryManager.on_assign:
    pushq    %rbp
    movq     %rsp, %rbp

    callq    *.MemoryManager.FN_ON_ASSIGN(%rip)

    movq     %rbp, %rsp
    popq     %rbp
    ret

#
# Memory Allocation
#
#   Allocates the requested amount of memory and returns a pointer
#   to the start of the block.
#
#   INPUT:
#    %rdi: size of allocation in quads
#
#   OUTPUT:
#    %rax: pointer to new memory block
#
#   Registers modified:
#    %rax, %rdi, %rsi, collector function
#

    .global .MemoryManager.alloc
.MemoryManager.alloc:
    ALLOC_SIZE_SIZE   = 8
    ALLOC_SIZE        = -ALLOC_SIZE_SIZE
    PAD_SIZE          = 8
    FRAME_SIZE        = ALLOC_SIZE_SIZE + PAD_SIZE

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    salq     $3, %rdi                            # convert quads to bytes
    movq     %rdi, ALLOC_SIZE(%rbp)

                                                 # %rdi = alloc size in bytes
    movq     .Alloc.ptr(%rip), %rax              # %rax = .Alloc.ptr
    addq     %rdi, %rax                          # %rax = .Alloc.ptr + alloc size = the next .Alloc.ptr

    # Check if enough free space in the work area
    #
    # `.Alloc.ptr` is always strictly less than `.Alloc.limit`.
    # Hence there is always enough room for at least one assignment record 
    # at the tip of assignment stack. 
    # See the relevant comments in GenGC for more details.
    cmpq     .Alloc.limit(%rip), %rax
    jl       .MemoryManager.alloc.can_alloc      # if (the next .Alloc.ptr < .Alloc.limit) go to ...
 
    # Let's make enough free space.
                                                 # %rdi = alloc size in bytes
    movq     %rbp, %rsi                          # %rsi = tip of stack to start collecting from
    xorl     %edx, %edx                          # %rdx = %edx = 0, don't force a major collection
    callq    *.MemoryManager.FN_COLLECT(%rip)    # collect garbage

    # Assert the collector fn preserved the value of %rdi
    # cmpq     ALLOC_SIZE(%rbp), %rdi
    # jne     .GC.abort

                                                 # %rdi = alloc size in bytes
    movq     .Alloc.ptr(%rip), %rax              # %rax = .Alloc.ptr
    addq     %rdi, %rax                          # %rax = .Alloc.ptr + alloc size

.MemoryManager.alloc.can_alloc:
    movq     .Alloc.ptr(%rip), %rdi              # %rdi = .Alloc.ptr
    movq     %rax, .Alloc.ptr(%rip)              # .Alloc.ptr = %rax = .Alloc.ptr + alloc size
    movq     %rdi, %rax                          # %rax = %rdi = .Alloc.ptr

    movq     %rbp, %rsp
    popq     %rbp
    ret

#
# Ensure Enough Memory for Allocation
#
#   Makes sure that the requested amount of memory can be allocated
#   within the work area.
#
#   INPUT:
#    %rdi: size of allocation in quads
#
#   OUTPUT:
#    %rdi: size of allocation in quads (unchanged)
#
#   Registers modified:
#    %rax, %rsi, collector function
#

    .global .MemoryManager.ensure_can_alloc 
.MemoryManager.ensure_can_alloc:
    ALLOC_SIZE_SIZE   = 8
    ALLOC_SIZE        = -ALLOC_SIZE_SIZE
    PAD_SIZE          = 8
    FRAME_SIZE        = ALLOC_SIZE_SIZE + PAD_SIZE

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    movq     %rdi, ALLOC_SIZE(%rbp)                     # preserve the allocation size in quads

    salq     $3, %rdi                                   # convert quads to bytes
    movq     .Alloc.ptr(%rip), %rax
    addq     %rdi, %rax                                 # calc the new alloc ptr value

    cmpq     .Alloc.limit(%rip), %rax                   # check if enough free space in the work area
    jl       .MemoryManager.ensure_can_alloc.can_alloc  # yes, there is

    # Let's make enough free space
    # %rdi contains allocation size in bytes 
    movq     %rbp, %rsi                                 # tip of stack to start collecting from
    xorl     %edx, %edx                                 # %rdx = %edx = 0, don't force a major collection
    callq    *.MemoryManager.FN_COLLECT(%rip)           # collect garbage

.MemoryManager.ensure_can_alloc.can_alloc:
    movq     ALLOC_SIZE(%rbp), %rdi                     # restore the allocation size in quads

    movq     %rbp, %rsp
    popq     %rbp
    ret

#
# Test heap consistency
#
#   Runs the garbage collector passing zero allocation size 
#   in the hope that this will help detect garbage collection bugs earlier.
#
#   INPUT:
#    none
#
#   OUTPUT:
#    none
#
#   Registers modified:
#    %rax, %rdi, %rsi, collector function

    .global .MemoryManager.test
.MemoryManager.test:
    pushq    %rbp
    movq     %rsp, %rbp

    movq     .MemoryManager.IS_TESTING(%rip), %rax    # Check if testing enabled
    testq    %rax, %rax
    jz       .MemoryManager.test.done

    xorl     %edi, %edi                               # 0 bytes allocation size in %rdi
    movq     %rbp, %rsi                               # tip of stack to start collecting from
    xorl     %edx, %edx                               # %rdx = %edx = 0, don't force a major collection
    callq    *.MemoryManager.FN_COLLECT(%rip)         # collect garbage

.MemoryManager.test.done:
    movq    %rbp, %rsp
    popq    %rbp
    ret

#
# Print heap state
#
#   INPUT:
#    None
#
#   OUTPUT:
#    %rax: unchanged
#    %rdi: unchanged
#    %rsi: unchanged
#
#   Registers modified:
#    .Runtime.print, .Runtime.out_int, .Runtime.out_nl

    .global .MemoryManager.print_state
.MemoryManager.print_state:
    RAX_SIZE      = 8
    RAX           = -RAX_SIZE
    RDI_SIZE      = 8
    RDI           = -(RAX_SIZE + RDI_SIZE)
    RSI_SIZE      = 8
    RSI           = -(RAX_SIZE + RDI_SIZE + RSI_SIZE)
    PAD_SIZE      = 8
    FRAME_SIZE    =   RAX_SIZE + RDI_SIZE + RSI_SIZE + PAD_SIZE

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    movq     %rax, RAX(%rbp)
    movq     %rdi, RDI(%rbp)
    movq     %rsi, RSI(%rbp)

    movq     $.MM.MSG_ALLOC_PTR_EQ_ASCII, %rdi
    movq     $.MM.MSG_ALLOC_PTR_EQ_LEN, %rsi
    call     .Runtime.print
    movq     .Alloc.ptr(%rip), %rdi
    call     .Runtime.out_int
    call     .Runtime.out_nl

    # movq     $.MM.MSG_ALLOC_LIMIT_EQ_ASCII, %rdi
    # movq     $.MM.MSG_ALLOC_LIMIT_EQ_LEN, %rsi
    # call     .Runtime.print
    # movq     .Alloc.limit(%rip), %rdi
    # call     .Runtime.out_int
    # call     .Runtime.out_nl

    movq     RAX(%rbp), %rax
    movq     RDI(%rbp), %rdi
    movq     RSI(%rbp), %rsi

    movq     %rbp, %rsp
    popq     %rbp
    ret

########################################
# Shared GC procedures
########################################

#
# Check a pointer contains an address of an object.
# If the check fails, abort.
#
#   INPUT:
#    %rdi: a pointer to an object
#
#   OUTPUT:
#    %rdi: unchanged
#
#   Registers modified:
#    %rax
#

    .global .GC.validate_ptr
.GC.validate_ptr:
    testq    %rdi, %rdi
    jz       .GC.validate_ptr.ok                # null is valid
    cmpq     $EYE_CATCH, OBJ_EYE_CATCH(%rdi)
    je       .GC.validate_ptr.ok                # found the eye-catch just before the object
    call     .GC.abort                          # this is not a pointer to an object
.GC.validate_ptr.ok:
    ret

#
# Reports an internal error in the GC code and aborts
#
#   INPUT:
#    none
#
#   Registers modified:
#    %rdi, %rsi
#

    .global .GC.abort
.GC.abort:
    movq     $.GC.MSG_INTERNAL_ERROR_ASCII, %rdi
    movq     $.GC.MSG_INTERNAL_ERROR_LEN, %rsi
    call     .Runtime.print_ln

    movq   $1, %rdi
    jmp    .Platform.exit_process

########################################
# No Operation Garbage Collector
#
#   NopGC does not attempt to do any garbage collection.
#   It simply expands the heap if more memory is needed.
########################################

#
# Initialization
#    Sets `.Alloc.ptr`   = `.Platform.heap_start`
#    Sets `.Alloc.limit` = `.Platform.heap_end`
#    In the case of NopGC, `.Alloc.limit` is simply 
#    the same as `.Platform.heap_end` 
#
#   INPUT:
#    none
#
#   OUTPUT:
#    none
#
#   Registers modified:
#    %rax
#
    .global .NopGC.init
.NopGC.init:
    movq    .Platform.heap_start(%rip), %rax
    movq    %rax, .Alloc.ptr(%rip)
    movq    .Platform.heap_end(%rip), %rax
    movq    %rax, .Alloc.limit(%rip)

    ret
#
# `on_assign` is called to notify the GC an assignment just happened.
# In the case of `.NopGC` this procedure does nothing.
#
#   INPUT:
#    %rdi: pointer to the pointer being assigned to
#
#   OUTPUT:
#    None
#
#   Registers modified:
#    None
#
    .global .NopGC.on_assign
.NopGC.on_assign:
    ret


#
# Collection
#
#   Does not collect any garbage just expands the heap as necessary.
#
#   INPUT:
#    %rdi: size to allocate in bytes
#
#   OUTPUT:
#    %rdi: size to allocate in bytes (unchanged)
#
#   Registers modified:
#    %rax, %rsi, .Platform.alloc
#
    .global .NopGC.collect
.NopGC.collect:
    .NopGC.HEAP_PAGE    = 0x10000               # size in bytes to expand heap (65536B)

    ALLOC_SIZE_SIZE     = 8
    ALLOC_SIZE          = -ALLOC_SIZE_SIZE
    PAD_SIZE            = 8
    FRAME_SIZE          = ALLOC_SIZE_SIZE + PAD_SIZE

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    movq     %rdi, ALLOC_SIZE(%rbp)             # preserve the allocation size in bytes

    # show collection message
    # movq     $.NopGC.MSG_COLLECTING_ASCII, %rdi
    # movq     $.NopGC.MSG_COLLECTING_LEN, %rsi
    # call     .Platform.out_string

.NopGC.collect.ensure_can_alloc:
    movq     .Alloc.ptr(%rip), %rax
    movq     ALLOC_SIZE(%rbp), %rdi             # restore the allocation size in bytes
    addq     %rdi, %rax                         # calc the new alloc ptr value 
    cmpq     .Alloc.limit(%rip), %rax           # check if enough free space in the work area
    jl       .NopGC.collect.done                # yes, there is

    # Let's make enough free space
    movq     $.NopGC.HEAP_PAGE, %rdi            # size in bytes
    call     .Platform.alloc                    # expand the heap
    # In the case of NopGC, `.Alloc.limit` is simply 
    # the same as `.Platform.heap_end` 
    movq     .Platform.heap_end(%rip), %rax
    movq     %rax, .Alloc.limit(%rip)

    jmp      .NopGC.collect.ensure_can_alloc    # keep expanding?

.NopGC.collect.done:
    movq     ALLOC_SIZE(%rbp), %rdi             # restore the allocation size in bytes

    movq     %rbp, %rsp
    popq     %rbp

    ret

#
# Print NopGC's current state
#
#   Preserves %rax, %rdi, %rsi for convinience of the caller.
#   Prints the heap's start and end addresses, .Alloc.ptr, .Alloc.limit
#
#   INPUT:
#    None
#
#   OUTPUT:
#    %rax: unchanged
#    %rdi: unchanged
#    %rsi: unchanged
#
#   GLOBALS MODIFIED:
#    None
#
#   Registers modified:
#    .Runtime.print, .Runtime.out_int, .Runtime.out_nl
#

    .global .NopGC.print_state
.NopGC.print_state:
    RAX_SIZE      = 8
    RAX           = -RAX_SIZE
    RDI_SIZE      = 8
    RDI           = -(RAX_SIZE + RDI_SIZE)
    RSI_SIZE      = 8
    RSI           = -(RAX_SIZE + RDI_SIZE + RSI_SIZE)
    PAD_SIZE      = 8
    FRAME_SIZE    =   RAX_SIZE + RDI_SIZE + RSI_SIZE + PAD_SIZE

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    movq     %rax, RAX(%rbp)
    movq     %rdi, RDI(%rbp)
    movq     %rsi, RSI(%rbp)

    movq     $.NopGC.MSG_HEAP_START_EQ_ASCII, %rdi
    movq     $.NopGC.MSG_HEAP_START_EQ_LEN, %rsi
    call     .Runtime.print
    movq     .Platform.heap_start(%rip), %rdi
    call     .Runtime.out_int
    call     .Runtime.out_nl

    movq     $.NopGC.MSG_HEAP_END_EQ_ASCII, %rdi
    movq     $.NopGC.MSG_HEAP_END_EQ_LEN, %rsi
    call     .Runtime.print
    movq     .Platform.heap_end(%rip), %rdi
    call     .Runtime.out_int
    call     .Runtime.out_nl

    movq     $.NopGC.MSG_ALLOC_PTR_EQ_ASCII, %rdi
    movq     $.NopGC.MSG_ALLOC_PTR_EQ_LEN, %rsi
    call     .Runtime.print
    movq     .Alloc.ptr(%rip), %rdi
    call     .Runtime.out_int
    call     .Runtime.out_nl

    movq     $.NopGC.MSG_ALLOC_LIMIT_EQ_ASCII, %rdi
    movq     $.NopGC.MSG_ALLOC_LIMIT_EQ_LEN, %rsi
    call     .Runtime.print
    movq     .Alloc.limit(%rip), %rdi
    call     .Runtime.out_int
    call     .Runtime.out_nl

    call     .Runtime.out_nl

    movq     RAX(%rbp), %rax
    movq     RDI(%rbp), %rdi
    movq     RSI(%rbp), %rsi

    movq     %rbp, %rsp
    popq     %rbp
    ret
