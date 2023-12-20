########################################
# Data
########################################

    .data

########################################
# TODO: The following globals should be emmitted by the compiler in the program's assembly,
# TODO: but for now they are defined here.
    .global .MemoryManager.FN_INIT
.MemoryManager.FN_INIT:         .quad .GenGC.init

    .global .MemoryManager.FN_COLLECT
.MemoryManager.FN_COLLECT:      .quad .GenGC.collect

    .global .MemoryManager.IS_TESTING
.MemoryManager.IS_TESTING:      .quad 0
########################################

    .global .Alloc.ptr
.Alloc.ptr:                      .quad 0
    .global .Alloc.limit
.Alloc.limit:                    .quad 0

#
# TODO: Place strings and other constants in `.section .rodata` instead of `.data`.
#
########################################
# Common GC messages
########################################

.GC.MSG_INTERNAL_ERROR_ASCII:    .ascii "The garbage collector encountered an internal error"
.GC.MSG_INTERNAL_ERROR_LEN  =           (. - .GC.MSG_INTERNAL_ERROR_ASCII)

########################################
# Messages for the NoGC garabge collector
########################################

.NopGC.MSG_COLLECTING_ASCII:    .ascii "NoGC: Increasing heap..."
.NopGC.MSG_COLLECTING_LEN  =           (. - .NopGC.MSG_COLLECTING_ASCII)

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
#    initializer function
#

    .global .MemoryManager.init
.MemoryManager.init:
    # pointer to init procedure of a GC implementation
    callq    *.MemoryManager.FN_INIT(%rip)
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
    pushq    %rbp
    movq     %rsp, %rbp

    salq     $3, %rdi                            # convert quads to bytes

    movq     .Alloc.ptr(%rip), %rax
    addq     %rdi, %rax                          # calc the new alloc ptr value

    cmpq     .Alloc.limit(%rip), %rax            # check if enough free space in the work area
    jl       .MemoryManager.alloc.can_alloc      # yes, there is
 
    # Let's make enough free space.
    # %rdi contains allocation size in bytes 
    # and the collector fn preserves its value 
    movq     %rbp, %rsi                          # tip of stack to start collecting from
    callq    *.MemoryManager.FN_COLLECT(%rip)    # collect garbage

    movq     .Alloc.ptr(%rip), %rax
    addq     %rdi, %rax                          # calc the new alloc ptr value
.MemoryManager.alloc.can_alloc:
    movq     .Alloc.ptr(%rip), %rdi              # preserve the start addr of allocated memory block
    movq     %rax, .Alloc.ptr(%rip)              # advance the allocation pointer
    movq     %rdi, %rax                          # place the start addr of allocated memory block in %rax

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
    callq    *.MemoryManager.FN_COLLECT(%rip)         # collect garbage

.MemoryManager.test.done:
    movq    %rbp, %rsp
    popq    %rbp
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
    jmp      .GC.abort                          # this is not a pointer to an object
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
    call     .Platform.out_string
    call     .Runtime.out_nl

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
