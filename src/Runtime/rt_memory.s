########################################
# Data
########################################

    .data

########################################
# TODO: The following globals should be emmitted by the compiler in the program's assembly,
# TODO: but for now they are defined here.
    .global    .MemoryManager.FN_INIT
.MemoryManager.FN_INIT:         .quad .NopGC.init

    .global    .MemoryManager.FN_COLLECT
.MemoryManager.FN_COLLECT:      .quad .NopGC.collect

    .global    .MemoryManager.TEST_ENABLED
.MemoryManager.TEST_ENABLED:    .quad 0
########################################

work_area_start:                .quad 0
work_area_end:                  .quad 0
alloc_ptr:                      .quad 0

#
# TODO: Place strings and other constants in `.section .rodata` instead of `.data`.
#

########################################
# Messages for the GenGC garbage collector
########################################

.GenGC.MSG_INITERROR_ASCII:        .ascii "GenGC: Unable to initialize the garbage collector"
.GenGC.MSG_COLLECTING_ASCII:       .ascii "GenGC: Garbage collecting ..."
.GenGC.MSG_MAJOR_ASCII:            .ascii "GenGC: Major ..."
.GenGC.MSG_MINOR_ASCII:            .ascii "GenGC: Minor ..."
.GenGC.MSG_MINOR_ERROR_ASCII:      .ascii "GenGC: Error during minor garbage collection"
.GenGC.MSG_MAJOR_ERROR_ASCII:      .ascii "GenGC: Error during major garbage collection"
.GenGC.MSG_INIT_TEST_ASCII:        .ascii "GenGC: initialized in test mode"
.GenGC.MSG_INIT_ASCII:             .ascii "GenGC: initialized"

########################################
# Messages for the NoGC garabge collector
########################################

.NopGC.MSG_COLLECTING_ASCII:        .ascii "NoGC: Increasing heap..."
.NopGC.MSG_COLLECTING_LEN  =               (. - .NopGC.MSG_COLLECTING_ASCII)

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

########################################
# MemoryManager Memory Manager
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
#        %rsi: the top of the stack to start checking for pointers from.
#              (remember the stack grows down, 
#               so the top is at the lowest address)
#
########################################

#
# Initialize the Memory Manager
#
#   Call the initialization routine for the garbage collector.
#
#   INPUT:
#    %rdi: initial Register mask
#    %rsi: the bottom of stack to stop checking for pointers at.
#          (remember the stack grows down, 
#           so the bottom is at the highest address)
#
#   OUTPUT:
#    none
#
#   Registers modified:
#    initializer function
#

    .global .MemoryManager.init
.MemoryManager.init:
    callq    *.MemoryManager.FN_INIT(%rip)   # pointer to initialization procedure
                                             # defined in another compilation unit
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

    movq     alloc_ptr(%rip), %rax
    addq     %rdi, %rax                          # calc the new alloc ptr value

    cmpq     work_area_end(%rip), %rax           # check if enough free space in the work area
    jl       .MemoryManager.alloc.can_alloc      # yes, there is
 
    # Let's make enough free space.
    # %rdi contains allocation size in bytes 
    # and the collector fn preserves its value 
    movq     %rbp, %rsi                          # end of stack to collect (except the padding we added)
    callq    *.MemoryManager.FN_COLLECT(%rip)    # collect garbage

    movq     alloc_ptr(%rip), %rax
    addq     %rdi, %rax                          # calc the new alloc ptr value
.MemoryManager.alloc.can_alloc:
    movq     alloc_ptr(%rip), %rdi               # preserve the addr of allocated memory block
    movq     %rax, alloc_ptr(%rip)               # advance the allocation pointer
    movq     %rdi, %rax                          # place the addr of allocated memory block in %rax

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
    REQUESTED_SIZE_SIZE   = 8
    REQUESTED_SIZE_OFFSET = -REQUESTED_SIZE_SIZE
    PADDING_SIZE          = 8
    FRAME_SIZE            = REQUESTED_SIZE_SIZE + PADDING_SIZE

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    movq     %rdi, REQUESTED_SIZE_OFFSET(%rbp)          # preserve the allocation size in quads
    salq     $3, %rdi                                   # convert quads to bytes

    movq     alloc_ptr(%rip), %rax
    addq     %rdi, %rax                                 # calc the new alloc ptr value

    cmpq     work_area_end(%rip), %rax                  # check if enough free space in the work area
    jl       .MemoryManager.ensure_can_alloc.can_alloc  # yes, there is

    # Let's make enough free space
    # %rdi contains allocation size in bytes 
    movq     %rbp, %rsi                                 # end of stack to collect
    callq    *.MemoryManager.FN_COLLECT(%rip)           # collect garbage
.MemoryManager.ensure_can_alloc.can_alloc:
    movq     REQUESTED_SIZE_OFFSET(%rbp), %rdi          # restore the allocation size in quads

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

    movq     .MemoryManager.TEST_ENABLED(%rip), %rax    # Check if testing enabled
    testq    %rax, %rax
    jz       .MemoryManager.test.exit

    xorl     %edi, %edi                                 # 0 bytes allocation size in %rdi
    movq     %rbp, %rsi                                 # end of stack to collect
    callq    *.MemoryManager.FN_COLLECT(%rip)           # collect garbage

.MemoryManager.test.exit:
    movq    %rbp, %rsp
    popq    %rbp
    ret

########################################
# No Operation Garbage Collector
#
#   NopGC does not attempt to do any garbage collection.
#   It simply expands the heap if more memory is needed.
########################################

#
# Initialization
#    Sets work_area_start to the value of .Platform.heap_start
#    Sets work_area_end to the value of .Platform.heap_end
#    Sets alloc_ptr to the value of work_area_start
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
    movq    %rax, work_area_start(%rip)
    movq    %rax, alloc_ptr(%rip)
    movq    .Platform.heap_end(%rip), %rax
    movq    %rax, work_area_end(%rip)

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
    EXPAND_SIZE           = 0x10000               # size in bytes to expand heap (65536KB)
    REQUESTED_SIZE_SIZE   = 8
    REQUESTED_SIZE_OFFSET = -REQUESTED_SIZE_SIZE
    PADDING_SIZE          = 8
    FRAME_SIZE            = REQUESTED_SIZE_SIZE + PADDING_SIZE

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    movq     %rdi, REQUESTED_SIZE_OFFSET(%rbp)    # preserve the allocation size in bytes

    # show collection message
    # movq     $.NopGC.MSG_COLLECTING_ASCII, %rdi
    # movq     $.NopGC.MSG_COLLECTING_LEN, %rsi
    # call     .Platform.out_string

.NopGC.collect.ensure_can_alloc:
    movq     alloc_ptr(%rip), %rax
    movq     REQUESTED_SIZE_OFFSET(%rbp), %rdi     # restore the allocation size in bytes
    addq     %rdi, %rax                            # calc the new alloc ptr value 
    cmpq     work_area_end(%rip), %rax             # check if enough free space in the work area
    jl       .NopGC.collect.exit                   # yes, there is

    # Let's make enough free space
    movq     $EXPAND_SIZE, %rdi                    # size in bytes
    call     .Platform.alloc                       # expand the heap

    movq     .Platform.heap_end(%rip), %rax
    movq     %rax, work_area_end(%rip)             # update the work-area end pointer

    jmp      .NopGC.collect.ensure_can_alloc       # keep expanding?

.NopGC.collect.exit:
    movq     REQUESTED_SIZE_OFFSET(%rbp), %rdi     # restore the allocation size in bytes

    movq     %rbp, %rsp
    popq     %rbp

    ret

/*
########################################
# GenGC Generational Garbage Collector
#
#   This is an implementation of a generational garbage collector
#   as described in "Simple Generational Garbage Collection and Fast
#   Allocation" by Andrew W. Appel [Princeton University, March 1988].
#   This is a two generation scheme which uses an assignment table
#   to handle root pointers located in the older generation objects.
#
#   When the work area is filled, a minor garbage collection takes place
#   which moves all live objects into the reserve area.  These objects
#   are then incorporated into the old area.  New reserve and work areas
#   are setup and allocation can continue in the work area.  If a break-
#   point is reached in the size of the old area just after a minor
#   collection, a major collection then takes place.  All live objects in
#   the old area are then copied into the new area, expanding the heap if
#   necessary.  The X and new areas are then block copied back L1-L0
#   bytes to form the next old area.
#
#   The assignment table is implemented as a stack growing towards the
#   allocation pointer ($gp) in the work area.  If they cross, a minor
#   collection is then carried out.  This allows the garbage collector to
#   to have to keep a fixed table of assignments.  As a result, programs
#   with many assignments will tend not to be bogged down with extra
#   garbage collections.
#
#   The unused area was implemented to help keep the garbage collector
#   from continually expanding the heap.  This buffer zone allows major
#   garbage collections to happen earlier, reducing the risk of expansions
#   due to too many live objects in the old area.  The histories kept by
#   the garbage collector in MAJOR0, MAJOR1, MINOR0, and MINOR1 also help
#   to prevent unnecessary expansions of the heap.  If many live objects
#   were recently collected, the garbage collections will start to occur
#   sooner.
#
#   Note that during a minor collection, the work area is guaranteed to
#   fit within the reserve area.  However, during a major collection, the
#   old area will not necessarily fit in the new area.  If the latter occurs,
#   ".GenGC_OfsCopy" will detect this and expand the heap.
#
#   The heap is expanded on two different occasions:
#
#     1) After a major collection, the old area is set to be at most
#        1/(2^GenGC_OLDRATIO) of the usable heap (L0 to L3).  Note that
#        first L4 is checked to see if any of the unused memory between L3
#        and L4 is enough to satisfy this requirement.  If not, then the
#        heap will be expanded.  If it is, the appropriate amount will be
#        transfered from the unused area to the work/reserve area.
#
#     2) During a major collection, if the live objects in the old area
#        do not fit within the new area, the heap is expanded and $s7
#        is updated to reflact this.  This value later gets stored back
#        into L4.
#
#   During a normal allocation and minor collections, the heap has the
#   following form:
#
#      Header
#     |
#     |     Older generation objects
#     |    |
#     |    |              Minor garbage collection area
#     |    |              |
#     |    |              |                 Allocation area
#     |    |              |                 |
#     |    |              |                 |            Assignment table
#     |    |              |                 |            |
#     |    |              |                 |            |    Unused
#     |    |              |                 |            |    |
#     v    v              v                 v            v    v
#     +----+--------------+-----------------+-------------+---+---------+
#     |XXXX| Old Area     | Reserve Area    | Work Area   |XXX| Unused  |
#     +----+--------------+-----------------+-------------+---+---------+
#     ^    ^              ^                 ^    ^        ^   ^         ^
#     |    |              |                 |    |-->  <--|   |         |
#     |    L0             L1                L2  $gp      $s7  L3        L4
#     |
#     heap_start
#
#     $gp (allocation pointer): points to the next free word in the work
#         area during normal allocation.  During a minor garbage collection,
#         it points to the next free work in the reserve area.
#
#     $s7 (limit pointer): points to the limit that $gp can traverse.  Between
#         it and L3 sits the assignment table which grows towards $gp.
#
#   During a Major collection, the heap has the following form:
#
#      Header
#       |
#       |   Older generation objects
#       |    |
#       |    |                 Objects surviving last minor garbage collection
#       |    |                  |
#       |    |                  |         Major garbage collection area
#       |    |                  |          |
#       v    v                  v          v
#     +----+------------------+----------+------------------------------+
#     |XXXX| Old Area         | X        | New Area                     |
#     +----+------------------+----------+------------------------------+
#      ^    ^                  ^      ^   ^      ^                       ^
#      |    |                  |      |   |      |-->                    |
#      |    L0                 L1     |   L2    $gp                   L4, $s7
#      |                              |
#     heap_start                     breakpoint
#
#     $gp (allocation pointer): During a major collection, this points
#         into the next free word in the new area.
#
#     $s7 (limit pointer): During a major collection, the points to the
#         limit of heap memory.  $gp is not allowed to pass this value.
#         If the objects in the live old area cannot fit in the new area,
#         more memory is allocated and $s7 is adjusted accordingly.
#
#     breakpoint: Point where a major collection will occur.  It is
#         calculated by the following formula:
#
#         breakpoint = MIN(L3-MAX(MAJOR0,MAJOR1)-MAX(MINOR0,MINOR1),
#                          L3-(L3-L0)/2)
#
#         where (variables stored in the header):
#           MAJOR0 = total size of objects in the new area after last major
#                    collection.
#           MAJOR1 = (MAJOR0+MAJOR1)/2
#           MINOR0 = total size of objects in the reserve area after last
#                    minor collection.
#           MINOR1 = (MINOR0+MINOR1)/2
#
#   The following assumptions are made in the garbage collection
#   process:
#
#     1) Pointers on the Stack:
#        Every word on the stack that ends in 0 (i.e., is even) and is
#     a valid address in the heap is assumed to point to an object
#        in the heap.  Even heap addresses on the stack that are actually
#     something else (e.g., raw integers) will probably cause an
#        garbage collection error.
#
#     2) Object Layout:
#        Besides the Int, String, and Bool objects (which are handled
#        separately), the garbage collector assumes that each attribute
#        in an object is a pointer to another object.  It, however,
#        still does as much as possible to verify this before actually
#        updating any fields.
#
#     3) Pointer tests:
#        In order to be verified as an object, a pointer must undergo
#        certain tests:
#
#          a) The pointer must point within the correct storage area.
#          b) The word before the pointer (obj_eyecatch) must be the
#             word 0xFFFF FFFF
#          c) The word at the pointer must not be 0xFFFF FFFF (i.e.
#             -1 cannot be a class tag)
#
#        These tests are performed whenever any data could be a pointer
#        to keep any non-pointers from being updated accidentally.  The
#        functions ".GenGC_ChkCopy" and ".GenGC_OfsCopy" are responsible
#        for these checks.
#
#     4) The size stored in the object does not include the word required
#        to store the eyecatcher for the object in the heap.  This allows
#        the prototype objects to not require its own eyecatcher.  Also,
#        a size of 0 is invalid because it is used as a flag by the garbage
#        collector to indicate a forwarding pointer in the "obj_disp" field.
#
#     5) Roots are contained in the following areas: the stack, registers
#        specified in the REG mask, and the assignment table.
########################################

#
# Constants
#

#
# GenGC header offsets from "heap_start"
#

    GenGC_HDRSIZE     = 44    # size of GenGC header
    GenGC_HDRL0       = 0     # pointers to GenGC areas
    GenGC_HDRL1       = 4
    GenGC_HDRL2       = 8
    GenGC_HDRL3       = 12
    GenGC_HDRL4       = 16
    GenGC_HDRMAJOR0   = 20    # history of major collections
    GenGC_HDRMAJOR1   = 24
    GenGC_HDRMINOR0   = 28    # history of minor collections
    GenGC_HDRMINOR1   = 32
    GenGC_HDRSTK      = 36    # start of stack
    GenGC_HDRREG      = 40    # current REG mask

#
# Granularity of heap expansion
#
#   The heap is always expanded in multiples of 2^k, where
#   k is the granularity.
#

    GenGC_HEAPEXPGRAN = 14    # 2^14=16K

#
# Old to usable heap size ratio
#
#   After a major collection, the ratio of size of old area to the usable
#   size of the heap is at most 1/(2^k) where k is the value provided.
#

    GenGC_OLDRATIO    = 2    # 1/(2^2)=.25=25%

#
# Mask to speficy which registers can be automatically updated
# when a garbage collection occurs.  The Automatic Register Update
# (ARU) mask has a bit set for all possible registers the
# garbage collector is able to handle.  The Register (REG) mask
# determines which register(s) are actually updated.
#
# BITS----------------------------
# 3 2         1         0
# 10987654321098765432109876543210
# --------------------------------
#
# 11000011011111110000000000000000  <-  Auto Register Update (ARU) mask
# +--++--++--++--++--++--++--++--+      $s0-$s6, $t8-$t9, $s8, $ra
#    C   3   7   F   0   0   0   0     ($16-$22, $24-$25, $30, $31)
#

    GenGC_ARU_MASK    = 0xC37F0000

#
# Functions
#

#
# Initialization
#
#   Sets up the header information block for the garbage collector.
#   This block is located at the start of the heap ("heap_start")
#   and includes information needed by the garbage collector.  It
#   also calculates the barrier for the reserve and work areas and
#   sets the L2 pointer accordingly, rounding off in favor of the
#   reserve area.
#
#   INPUT:
#    $a0: start of stack
#    $a1: initial Register mask
#    $a2: end of heap
#    heap_start: start of the heap
#
#   OUTPUT:
#    $gp: lower bound of the work area
#    $s7: upper bound of the work area
#
#   Registers modified:
#    $t0, $t1, $v0, $a0
#

    .global .GenGC_Init
.GenGC_Init:
    la         $t0 heap_start
    addiu      $t1 $t0 GenGC_HDRSIZE
    sw         $t1 GenGC_HDRL0($t0)         # save start of old area
    sw         $t1 GenGC_HDRL1($t0)         # save start of reserve area
    sub        $t1 $a2 $t1                  # find reserve/work area barrier ($t1 = end of heap - (heap_start + GenGC_HDRSIZE))
    srl        $t1 $t1 1                    # $t1 = $t1 / 2
    la         $v0 0xfffffffc
    and        $t1 $t1 $v0                  # floor $t1 to the closest multiple of 4
    blez       $t1 .GenGC_Init_error        # heap initially to small
    sub        $gp $a2 $t1                  # initial work area size is half the heap size excluding the GC header aligned on a 4-byte boundary
    sw         $gp GenGC_HDRL2($t0)         # save start of work area
    sw         $a2 GenGC_HDRL3($t0)         # save end of work area
    move       $s7 $a2                      # set limit pointer
    sw         $0 GenGC_HDRMAJOR0($t0)      # clear histories
    sw         $0 GenGC_HDRMAJOR1($t0)
    sw         $0 GenGC_HDRMINOR0($t0)
    sw         $0 GenGC_HDRMINOR1($t0)
    sw         $a0 GenGC_HDRSTK($t0)        # save stack start
    sw         $a1 GenGC_HDRREG($t0)        # save register mask
    li         $v0 9                        # get heap end
    move       $a0 $zero
    syscall                                 # sbrk
    sw         $v0 GenGC_HDRL4($t0)         # save heap limit
    la         $t0 .MemoryManager.TEST_ENABLED             # Check if testing enabled
    lw         $t0 0($t0)
    beqz       $t0 .MemoryManager.test_disabled
    la         $a0 .GenGC_Init_test_msg     # tell user GC is in test mode
    li         $v0 4
    syscall
    j          .GenGC_Init_end
.MemoryManager.test_disabled:
    la         $a0 .GenGC_Init_msg          # tell user GC NOT in test mode
    li         $v0 4
    syscall
.GenGC_Init_end:
    jr         $ra                          # return

.GenGC_Init_error:
    la         $a0 .GenGC_INITERROR     # show error message
    li         $v0 4
    syscall
    li         $v0 10                   # exit
    syscall

#
# Record Assignment
#
#   Records an assignment in the assignment table.  Note that because
#   $s7 is always greater than $gp, an assignment can always be
#   recorded.
#
#   INPUT:
#    $a1: pointer to the pointer being modified
#    $s7: limit pointer of the work area
#    $gp: current allocation pointer
#    heap_start: start of heap
#
#   Registers modified:
#    $t0, $t1, $t2, $v0, $v1, $a1, $a2, $gp, $s7
#
#   sm: $a0 is explicitly saved in the GC case so that in the normal
#   case the caller need not save/restore $a0
#
#   sm: Apparently .GenGC_Collect wants $a0 to be the last+1 word
#   of the stack, rather than the last word; I've therefore changed
#     addiu   $a0 $sp 4
#   to
#     addiu   $a0 $sp 0     (i.e. move $a0 $sp)
#   Just in case this isn't exactly right, I've also put 0 into that
#   last spot so it will definitely be safely ignored.
#

    .global .GenGC_Assign
.GenGC_Assign:
    addiu    $s7 $s7 -4
    sw       $a1 0($s7)                    # save pointer to assignment
    bgt      $s7 $gp .GenGC_Assign_done
    addiu    $sp $sp -8
    sw       $ra 8($sp)                    # save return address
    sw       $a0 4($sp)                    # sm: save $a0
    move     $a1 $0                        # size
    addiu    $a0 $sp 0                     # end of stack to collect
    sw       $0 0($sp)                     # play it safe with off-by-1
    jal      .GenGC_Collect
    lw       $ra 8($sp)                    # restore return address
    lw       $a0 4($sp)                    # restore $a0
    addiu    $sp $sp 8
.GenGC_Assign_done:
    jr       $ra                           # return

    .global _gc_check
_gc_check:
    beqz     $a1, _gc_ok               # void is ok
    lw       $a2 obj_eyecatch($a1)     # and check if it is valid
    addiu    $a2 $a2 1
    bnez     $a2 _gc_abort
_gc_ok:
    jr       $ra

_gc_abort:
    la         $a0 _gc_abort_msg
    li         $v0 4
    syscall                          # print gc message
    li         $v0 10
    syscall                          # exit


#
# Generational Garbage Collection
#
#   This function implements the generational garbage collection.
#   It first calls the minor collector, ".GenGC_MinorC", and then
#   updates its history in the header.  The breakpoint is then
#   calculated.  If the breakpoint is reached or there is still not
#   enough room to allocate the requested size, a major garbage
#   collection then takes place by calling ".GenGC_MajorC".  After
#   the major collection, the size of the old area is analyzed.  If
#   it is greater than 1/(2^GenGC_OLDRATIO) of the total usable heap
#   size (L0 to L3), the heap is expanded.  Also, if there is still not
#   enough room to allocate the requested size, the heap is expanded
#   further to make sure that the specified amount of memory can be
#   allocated. If there is enough room in the unused area (L3 to L4),
#   this memory is used and the heap is not expanded.  The $s7 and $gp
#   pointers are then set as well as the L2 pointer.  If a major collection
#   is not done, the X area is incorporated into the old area
#   (i.e. the L2 pointer is moved into L1) and $s7, $gp, and L2 are
#   then set.
#
#   INPUT:
#    $a0: end of stack (%rsi)
#    $a1: size will need to allocate in bytes (%rdi)
#    $s7: limit pointer of the work area
#    $gp: current allocation pointer
#    heap_start: start of heap
#
#   OUTPUT:
#    $a1: size will need to allocate in bytes (unchanged)
#
#   Registers modified:
#    $t0, $t1, $t2, $t3, $t4, $v0, $v1, $a0, $a2, $gp, $s7
#

    .global    .GenGC_Collect
.GenGC_Collect:
    addiu      $sp $sp -12
    sw         $ra 12($sp)                       # save return address
    sw         $a0 8($sp)                        # save stack end
    sw         $a1 4($sp)                        # save size
    la         $a0 .GenGC_COLLECT                # print collection message
    li         $v0 4
    syscall
    lw         $a0 8($sp)                        # restore stack end
    jal        .GenGC_MinorC                     # minor collection
    la         $a1 heap_start
    lw         $t1 GenGC_HDRMINOR1($a1)
    addu       $t1 $t1 $a0
    srl        $t1 $t1 1
    sw         $t1 GenGC_HDRMINOR1($a1)          # update histories
    sw         $a0 GenGC_HDRMINOR0($a1)
    move       $t0 $t1                           # set $t0 to max of minor
    bgt        $t1 $a0 .GenGC_Collect_maxmaj
    move       $t0 $a0
.GenGC_Collect_maxmaj:
    lw         $t1 GenGC_HDRMAJOR0($a1)          # set $t1 to max of major
    lw         $t2 GenGC_HDRMAJOR1($a1)
    bgt        $t1 $t2 .GenGC_Collect_maxdef
    move       $t1 $t2
.GenGC_Collect_maxdef:
    lw         $t2 GenGC_HDRL3($a1)
    sub        $t0 $t2 $t0                       # set $t0 to L3-$t0-$t1
    sub        $t0 $t0 $t1
    lw         $t1 GenGC_HDRL0($a1)              # set $t1 to L3-(L3-L0)/2
    sub        $t1 $t2 $t1
    srl        $t1 $t1 1
    sub        $t1 $t2 $t1
    blt        $t0 $t1 .GenGC_Collect_breakpt    # set $t0 to minimum of above
    move       $t0 $t1
.GenGC_Collect_breakpt:
    lw         $t1 GenGC_HDRL1($a1)              # get end of old area
    bge        $t1 $t0 .GenGC_Collect_major
    lw         $t0 GenGC_HDRL2($a1)
    lw         $t1 GenGC_HDRL3($a1)
    lw         $t2 4($sp)                        # load requested size into $t2
    sub        $t0 $t1 $t0                       # find reserve/work area barrier
    srl        $t0 $t0 1
    la         $t3 0xfffffffc
    and        $t0 $t0 $t3
    sub        $t0 $t1 $t0                       # reserve/work barrier
    addu       $t2 $t0 $t2                       # test allocation
    bge        $t2 $t1 .GenGC_Collect_major      # check if work area too small
.GenGC_Collect_nomajor:
    lw         $t1 GenGC_HDRL2($a1)
    sw         $t1 GenGC_HDRL1($a1)              # expand old area
    sw         $t0 GenGC_HDRL2($a1)              # set new reserve/work barrier
    move       $gp $t0                           # set $gp
    lw         $s7 GenGC_HDRL3($a1)              # load limit into $s7
    b          .GenGC_Collect_done
.GenGC_Collect_major:
    la         $a0 .GenGC_Major                  # print collection message
    li         $v0 4
    syscall
    lw         $a0 8($sp)                        # restore stack end
    jal        .GenGC_MajorC                     # major collection
    la         $a1 heap_start
    lw         $t1 GenGC_HDRMAJOR1($a1)
    addu       $t1 $t1 $a0
    srl        $t1 $t1 1
    sw         $t1 GenGC_HDRMAJOR1($a1)          # update histories
    sw         $a0 GenGC_HDRMAJOR0($a1)
    lw         $t1 GenGC_HDRL3($a1)              # find ratio of the old area
    lw         $t0 GenGC_HDRL0($a1)
    sub        $t1 $t1 $t0                       # $t1 = L3-L0
    srl        $t1 $t1 GenGC_OLDRATIO            # $t1 = (L3-L0)/2^GenGC_OLDRATIO
    addu       $t1 $t0 $t1                       # $t1 = L0+(L3-L0)/2^GenGC_OLDRATIO
    lw         $t0 GenGC_HDRL1($a1)              # $t0 = L1
    sub        $t0 $t0 $t1                       # $t0 = L1-(L0+(L3-L0)/2^GenGC_OLDRATIO) -> $t0 > 0?
    sll        $t0 $t0 GenGC_OLDRATIO            # ??? amount to expand in $t0
    lw         $t1 GenGC_HDRL3($a1)              # load L3
    lw         $t2 GenGC_HDRL1($a1)              # load L1
    sub        $t2 $t1 $t2
    srl        $t2 $t2 1
    la         $t3 0xfffffffc
    and        $t2 $t2 $t3
    sub        $t1 $t1 $t2                       # reserve/work barrier
    lw         $t2 4($sp)                        # restore size
    addu       $t1 $t1 $t2
    lw         $t2 GenGC_HDRL3($a1)              # load L3
    sub        $t1 $t1 $t2                       # test allocation
    addiu      $t1 $t1 4                         # adjust for round off errors
    sll        $t1 $t1 1                         # need to allocate $t1 memory
    blt        $t1 $t0 .GenGC_Collect_enough     # put max of $t0, $t1 in $t0
    move       $t0 $t1
.GenGC_Collect_enough:
    blez       $t0 .GenGC_Collect_setL2          # no need to expand
    addiu      $t1 $0 1                          # put 1 in $t1
    sll        $t1 $t1 GenGC_HEAPEXPGRAN         # get granularity of expansion
    addiu      $t1 $t1 -1                        # align to granularity
    addu       $t0 $t0 $t1
    nor        $t1 $t1 $t1                       # ???
    and        $t0 $t0 $t1                       # total memory needed
    lw         $t1 GenGC_HDRL3($a1)              # load L3
    lw         $t2 GenGC_HDRL4($a1)              # load L4
    sub        $t1 $t2 $t1
    sub        $t2 $t0 $t1                       # actual amount to allocate
    bgtz       $t2 .GenGC_Collect_getmem         # check if really need to allocate
.GenGC_Collect_xfermem:
    lw         $s7 GenGC_HDRL3($a1)              # load L3
    addu       $s7 $s7 $t0                       # expand by $t0, set $s7
    sw         $s7 GenGC_HDRL3($a1)              # save L3
    b          .GenGC_Collect_findL2
.GenGC_Collect_getmem:
    li         $v0 9                             # sbrk
    move       $a0 $t2                           # set the size to expand the heap
    syscall
    li         $v0 9
    move       $a0 $zero
    syscall                                      # get new end of heap in $v0
    sw         $v0 GenGC_HDRL4($a1)              # save L4
    sw         $v0 GenGC_HDRL3($a1)              # save L3
    move       $s7 $v0                           # set $s7
    b          .GenGC_Collect_findL2
.GenGC_Collect_setL2:
    lw         $s7 GenGC_HDRL3($a1)              # load L3
.GenGC_Collect_findL2:
    lw         $t1 GenGC_HDRL1($a1)              # load L1
    sub        $t1 $s7 $t1
    srl        $t1 $t1 1
    la         $t0 0xfffffffc
    and        $t1 $t1 $t0
    sub        $gp $s7 $t1                       # reserve/work barrier
    sw         $gp GenGC_HDRL2($a1)              # save L2
.GenGC_Collect_done:

# Clear new generation to catch missing pointers
    move     $t0 $gp
.GenGC_Clear_loop:
    sw       $zero 0($t0)
    addiu    $t0 $t0 4
    blt      $t0 $s7 .GenGC_Clear_loop

    lw       $a1 4($sp)      # restore size
    lw       $ra 12($sp)     # restore return address
    addiu    $sp $sp 12
    jr       $ra             # return

#
# Check and Copy an Object
#
#   Checks that the input pointer points to an object is a heap
#   object.  If so, it then checks for a forwarding pointer by
#   checking for an object size of 0.  If found, the forwarding
#   pointer is returned.  If not found, the object is copied to $gp
#   and a pointer to it is returned.  The following tests are done to
#   determine if the object is a heap object:
#
#     1) The pointer is within the specified limits
#     2) The pointer is even
#     3) The word before the pointer is the eye catcher 0xFFFF FFFF
#     4) The word at the pointer is a valid tag (i.e. not equal to
#        0xFFFF FFFF)
#
#   INPUT:
#    $a0: pointer to check and copy
#    $a1: lower bound object should be within.
#    $a2: upper bound object should be within.
#    $gp: current allocation pointer
#
#   OUTPUT:
#    $a0: if input points to a heap object then it is set to the
#            new location of object.  If not, it is unchanged.
#    $a1: lower bound object should be within. (unchanged)
#    $a2: upper bound object should be within. (unchanged)
#
#   Registers modified:
#    $t0, $t1, $t2, $v0, $a0, $gp
#

    .global    .GenGC_ChkCopy
.GenGC_ChkCopy:
    blt      $a0 $a1 .GenGC_ChkCopy_done    # check bounds
    bge      $a0 $a2 .GenGC_ChkCopy_done
    andi     $t2 $a0 1                      # check if odd
    bnez     $t2 .GenGC_ChkCopy_done
    addiu    $t2 $0 -1
    lw       $t1 obj_eyecatch($a0)          # check eyecatcher
    bne      $t2 $t1 _gc_abort
    lw       $t1 obj_tag($a0)               # check object tag
    beq      $t2 $t1 .GenGC_ChkCopy_done
    lw       $t1 obj_size($a0)              # get size of object
    beqz     $t1 .GenGC_ChkCopy_forward     # if size = 0, get forwarding pointer
    move     $t0 $a0                        # save pointer to old object in $t0
    addiu    $gp $gp 4                      # allocate memory for eyecatcher
    move     $a0 $gp                        # get address of new object
    sw       $t2 obj_eyecatch($a0)          # save eye catcher
    sll      $t1 $t1 2                      # convert words to bytes
    addu     $t1 $t0 $t1                    # set $t1 to limit of copy
    move     $t2 $t0                        # set $t2 to old object
.GenGC_ChkCopy_loop:
    lw       $v0 0($t0)                     # copy
    sw       $v0 0($gp)
    addiu    $t0 $t0 4                      # update each index
    addiu    $gp $gp 4
    bne      $t0 $t1 .GenGC_ChkCopy_loop    # check for limit of copy
    sw       $0 obj_size($t2)               # set size to 0
    sw       $a0 obj_disp($t2)              # save forwarding pointer
.GenGC_ChkCopy_done:
    jr       $ra                            # return
.GenGC_ChkCopy_forward:
    lw       $a0 obj_disp($a0)              # get forwarding pointer
    jr       $ra                            # return


#
# Minor Garbage Collection
#
#   This garbage collector is run when ever the space in the work
#   area is used up by objects and the assignment table.  The live
#   objects are found and copied to the reserve area.  The L2 pointer
#   is then set to the end of the live objects.  The collector consists
#   of six phases:
#
#     1) Set $gp into the reserve area and set the inputs for ChkCopy
#
#     2) Scan the stack for root pointers into the heap.  The beginning
#        of the stack is in the header and the end is an input to this
#        function.  Look for the appropriate stack flags and act
#        accordingly.  Use ".GenGC_ChkCopy" to validate the pointer and
#        get the new pointer, and then update the stack entry.
#
#     3) Check the registers specified in the Register (REG) mask to
#        automatically update.  This mask is stored in the header.  If
#        bit #n in the mask is set, register #n will be passed to
#        ".GenGC_ChkCopy" and updated with its result.  ".GenGC_SetRegMask"
#        can be used to update this mask.
#
#     4) The assignemnt table is now checked.  $s7 is moved from its
#        current position until it hits the L3 pointer.  Each entry is a
#        pointer to the pointer that must be checked.  Again,
#        ".GenGC_ChkCopy" is used and the pointer updated.
#
#     5) At this point, all root objects are in the reserve area.  This
#        area is now traversed object by object (from L1 to $gp).  It
#        results in a breadth first search of the live objects collected.
#        All attributes of objects are treated as pointers except the
#        "Int", "Bool", and "String" objects.  The first two are skipped
#        completely, and the first attribute of the string object is
#        analyzed (should be a pointer to an "Int" object).
#
#     6) At this point, L2 is set to the end of the live objects in the
#        reserve area.  This is in preparation for a major collection.
#        The size of all the live objects collected is then computed and
#        returned.
#
#   INPUT:
#    $a0: end of stack
#    $s7: limit pointer of this area of storage
#    $gp: current allocation pointer
#    heap_start: start of heap
#
#   OUTPUT:
#    $a0: size of all live objects collected
#
#   Registers modified:
#    $t0, $t1, $t2, $t3, $t4, $v0, $v1, $a0, $a1, $a2, $gp, $s7
#

    .global    .GenGC_MinorC
.GenGC_MinorC:
    addiu      $sp $sp -20
    sw         $ra 20($sp)                        # save return address
    la         $t0 heap_start
    lw         $a1 GenGC_HDRL2($t0)               # set lower bound to work area
    move       $a2 $s7                            # set upper bound for ChkCopy
    lw         $gp GenGC_HDRL1($t0)               # set $gp into reserve area
    sw         $a0 16($sp)                        # save stack end
    lw         $t0 GenGC_HDRSTK($t0)              # set $t0 to stack start
    move       $t1 $a0                            # set $t1 to stack end
    ble        $t0 $t1 .GenGC_MinorC_stackend     # check for empty stack
                                                  # (stack grows from higher to lower addresses
                                                  # Hence, stack start <= stack end when stack is empty)
.GenGC_MinorC_stackloop:                          # $t1 stack end, $t0 index
    addiu      $t0 $t0 -4                         # update index
    sw         $t0 12($sp)                        # save stack index
    lw         $a0 4($t0)                         # get stack item
    jal        .GenGC_ChkCopy                     # check and copy
    lw         $t0 12($sp)                        # load stack index
    sw         $a0 4($t0)                         # replace stack item pointing to old object with pointer to new one
    lw         $t1 16($sp)                        # restore stack end
    bgt        $t0 $t1 .GenGC_MinorC_stackloop    # loop
.GenGC_MinorC_stackend:
    la         $t0 heap_start
    lw         $t0 GenGC_HDRREG($t0)              # get Register mask
    sw         $t0 16($sp)                        # save Register mask
.GenGC_MinorC_reg16:
    srl        $t0 $t0 16                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg17         # check if set
    move       $a0 $16                            # set test pointer to potentially old object address
    jal        .GenGC_ChkCopy                     # check and copy
    move       $16 $a0                            # update register with potentilly new object address
.GenGC_MinorC_reg17:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 17                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg18         # check if set
    move       $a0 $17                            # set test pointer
    jal        .GenGC_ChkCopy                     # check and copy
    move       $17 $a0                            # update register
.GenGC_MinorC_reg18:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 18                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg19         # check if set
    move       $a0 $18                            # set test pointer
    jal        .GenGC_ChkCopy                     # check and copy
    move       $18 $a0                            # update register
.GenGC_MinorC_reg19:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 19                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg20         # check if set
    move       $a0 $19                            # set test pointer
    jal        .GenGC_ChkCopy                     # check and copy
    move       $19 $a0                            # update register
.GenGC_MinorC_reg20:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 20                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg21         # check if set
    move       $a0 $20                            # set test pointer
    jal        .GenGC_ChkCopy                     # check and copy
    move       $20 $a0                            # update register
.GenGC_MinorC_reg21:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 21                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg22         # check if set
    move       $a0 $21                            # set test pointer
    jal        .GenGC_ChkCopy                     # check and copy
    move       $21 $a0                            # update register
.GenGC_MinorC_reg22:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 22                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg24         # check if set
    move       $a0 $22                            # set test pointer
    jal        .GenGC_ChkCopy                     # check and copy
    move       $22 $a0                            # update register
.GenGC_MinorC_reg24:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 24                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg25         # check if set
    move       $a0 $24                            # set test pointer
    jal        .GenGC_ChkCopy                     # check and copy
    move       $24 $a0                            # update register
.GenGC_MinorC_reg25:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 25                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg30         # check if set
    move       $a0 $25                            # set test pointer
    jal        .GenGC_ChkCopy                     # check and copy
    move       $25 $a0                            # update register
.GenGC_MinorC_reg30:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 30                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg31         # check if set
    move       $a0 $30                            # set test pointer
    jal        .GenGC_ChkCopy                     # check and copy
    move       $30 $a0                            # update register
.GenGC_MinorC_reg31:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 31                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_regend        # check if set
    move       $a0 $31                            # set test pointer
    jal        .GenGC_ChkCopy                     # check and copy
    move       $31 $a0                            # update register
.GenGC_MinorC_regend:
    la         $t0 heap_start
    lw         $t3 GenGC_HDRL0($t0)               # lower limit of old area
    lw         $t4 GenGC_HDRL1($t0)               # upper limit of old area
    lw         $t0 GenGC_HDRL3($t0)               # get L3
    sw         $t0 16($sp)                        # save index limit
    bge        $s7 $t0 .GenGC_MinorC_assnend      # check for no assignments
.GenGC_MinorC_assnloop:                           # $s7 index, $t0 limit
    lw         $a0 0($s7)                         # get table entry
    blt        $a0 $t3 .GenGC_MinorC_assnnext     # must point into old area
    bge        $a0 $t4 .GenGC_MinorC_assnnext
    lw         $a0 0($a0)                         # get pointer to check
    jal        .GenGC_ChkCopy                     # check and copy
    lw         $t0 0($s7)
    sw         $a0 0($t0)                         # update pointer
    lw         $t0 16($sp)                        # restore index limit
.GenGC_MinorC_assnnext:
    addiu      $s7 $s7 4                          # update index
    blt        $s7 $t0 .GenGC_MinorC_assnloop     # loop
.GenGC_MinorC_assnend:
    la         $t0 heap_start
    lw         $t0 GenGC_HDRL1($t0)               # start of reserve area
    bge        $t0 $gp .GenGC_MinorC_heapend      # check for no objects
.GenGC_MinorC_heaploop:                           # $t0: index, $gp: limit
    addiu      $t0 $t0 4                          # skip over eyecatcher
    addiu      $t1 $0 -1                          # check for eyecatcher
    lw         $t2 obj_eyecatch($t0)
    bne        $t1 $t2 .GenGC_MinorC_error        # eyecatcher not found
    lw         $a0 obj_size($t0)                  # get object size
    sll        $a0 $a0 2                          # words to bytes
    lw         $t1 obj_tag($t0)                   # get the object's tag
    lw         $t2 _int_tag                       # test for int object
    beq        $t1 $t2 .GenGC_MinorC_int
    lw         $t2 _bool_tag                      # test for bool object
    beq        $t1 $t2 .GenGC_MinorC_bool
    lw         $t2 _string_tag                    # test for string object
    beq        $t1 $t2 .GenGC_MinorC_string
.GenGC_MinorC_other:
    addi       $t1 $t0 obj_attr                   # start at first attribute
    add        $t2 $t0 $a0                        # limit of attributes
    bge        $t1 $t2 .GenGC_MinorC_nextobj      # check for no attributes
    sw         $t0 16($sp)                        # save pointer to object
    sw         $a0 12($sp)                        # save object size
    sw         $t2 4($sp)                         # save limit
.GenGC_MinorC_objloop:                            # $t1: index, $t2: limit
    sw         $t1 8($sp)                         # save index
    lw         $a0 0($t1)                         # set pointer to check
    jal        .GenGC_ChkCopy                     # check and copy
    lw         $t1 8($sp)                         # restore index
    sw         $a0 0($t1)                         # update object pointer
    lw         $t2 4($sp)                         # restore limit
    addiu      $t1 $t1 4
    blt        $t1 $t2 .GenGC_MinorC_objloop      # loop
.GenGC_MinorC_objend:
    lw         $t0 16($sp)                        # restore pointer to object
    lw         $a0 12($sp)                        # restore object size
    b          .GenGC_MinorC_nextobj              # next object
.GenGC_MinorC_string:
    sw         $t0 16($sp)                        # save pointer to object
    sw         $a0 12($sp)                        # save object size
    lw         $a0 str_size($t0)                  # set test pointer to an Int object representing the string's size
    jal        .GenGC_ChkCopy                     # check and copy
    lw         $t0 16($sp)                        # restore pointer to object
    sw         $a0 str_size($t0)                  # update size pointer
    lw         $a0 12($sp)                        # restore object size
.GenGC_MinorC_int:
.GenGC_MinorC_bool:
.GenGC_MinorC_nextobj:
    add        $t0 $t0 $a0                        # find next object
    blt        $t0 $gp .GenGC_MinorC_heaploop     # loop
.GenGC_MinorC_heapend:
    la         $t0 heap_start
    sw         $gp GenGC_HDRL2($t0)               # set L2 to $gp
    lw         $a0 GenGC_HDRL1($t0)
    sub        $a0 $gp $a0                        # find size after collection
    lw         $ra 20($sp)                        # restore return address
    addiu      $sp $sp 20
    jr         $ra                                # return
.GenGC_MinorC_error:
    la         $a0 .GenGC_MINORERROR              # show error message
    li         $v0 4
    syscall
    li         $v0 10                             # exit
    syscall

#
# Check and Copy an Object with an Offset
#
#   Checks that the input pointer points to an object is a heap object.
#   If so, the pointer is checked to be in one of two areas.  If the
#   pointer is in the X area, L0-L1 is added to the pointer, and the
#   new pointer is returned.  If the pointer points within the old area,
#   it then checks for a forwarding pointer by checking for an object
#   size of 0.  If found, the forwarding pointer is returned.  If not
#   found, the heap is then analyzed to make sure the object can be
#   copied.  It then expands the heap if necessary (updating only $s7),
#   and the copies the object to the $gp pointer.  It takes the new
#   pointer, adds L0-L1 to it, then saves this modified new pointer in
#   the forwarding (obj_disp) field and sets the flag (obj_size to 0).
#   Finally, it returns this pointer.  Note that this pointer does not
#   actually point to the object at this time.  This entire area will
#   later be block copied.  After that, this pointer will be valid.
#   The same tests are done here as in ".GenGC_ChkCopy" to verify that
#   this is a heap object.
#
#   INPUT:
#    $a0: pointer to check and copy with an offset
#    $a1: L0 pointer
#    $a2: L1 pointer
#    $v1: L2 pointer
#    $gp: current allocation pointer
#    $s7: L4 pointer
#
#   OUTPUT:
#    $a0: if input points to a heap object then it is set to the
#            new location of object.  If not, it is unchanged.
#    $a1: L0 pointer (unchanged)
#    $a2: L1 pointer (unchanged)
#    $v1: L2 pointer (unchanged)
#
#   Registers modified:
#    $t0, $t1, $t2, $v0, $a0, $gp, $s7
#

    .global    .GenGC_OfsCopy
.GenGC_OfsCopy:
    blt        $a0 $a1 .GenGC_OfsCopy_done     # check lower bound
    bge        $a0 $v1 .GenGC_OfsCopy_done     # check upper bound
    andi       $t2 $a0 1                       # check if odd
    bnez       $t2 .GenGC_OfsCopy_done
    addiu      $t2 $0 -1
    lw         $t1 obj_eyecatch($a0)           # check eyecatcher
    bne        $t2 $t1 _gc_abort
    lw         $t1 obj_tag($a0)                # check object tag
    beq        $t2 $t1 .GenGC_OfsCopy_done
    blt        $a0 $a2 .GenGC_OfsCopy_old      # check if old, X object
    sub        $v0 $a1 $a2                     # compute offset
    add        $a0 $a0 $v0                     # apply pointer offset
    jr         $ra                             # return
.GenGC_OfsCopy_old:
    lw         $t1 obj_size($a0)               # get size of object
    sll        $t1 $t1 2                       # convert words to bytes
    beqz       $t1 .GenGC_OfsCopy_forward      # if size = 0, get forwarding pointer
    move       $t0 $a0                         # save pointer to old object in $t0
    addu       $v0 $gp $t1                     # test allocation
    addiu      $v0 $v0 4
    blt        $v0 $s7 .GenGC_OfsCopy_memok    # check if enoguh room for object
    sub        $a0 $v0 $s7                     # amount to expand minus 1
    addiu      $v0 $0 1
    sll        $v0 $v0 GenGC_HEAPEXPGRAN
    add        $a0 $a0 $v0
    addiu      $v0 $v0 -1
    nor        $v0 $v0 $v0                     # get grain mask
    and        $a0 $a0 $v0                     # align to grain size
    li         $v0 9
    syscall                                    # expand heap
    li         $v0 9
    move       $a0 $0
    syscall                                    # get end of heap in $v0
    move       $s7 $v0                         # save heap end in $s7
    move       $a0 $t0                         # restore pointer to old object in $a0
.GenGC_OfsCopy_memok:
    addiu      $gp $gp 4                       # allocate memory for eyecatcher
    move       $a0 $gp                         # get address of new object
    sw         $t2 obj_eyecatch($a0)           # save eye catcher
    addu       $t1 $t0 $t1                     # set $t1 to limit of copy
    move       $t2 $t0                         # set $t2 to old object
.GenGC_OfsCopy_loop:
    lw         $v0 0($t0)                      # copy
    sw         $v0 0($gp)
    addiu      $t0 $t0 4                       # update each index
    addiu      $gp $gp 4
    bne        $t0 $t1 .GenGC_OfsCopy_loop     # check for limit of copy
    sw         $0 obj_size($t2)                # set size to 0
    sub        $v0 $a1 $a2                     # compute offset
    add        $a0 $a0 $v0                     # apply pointer offset
    sw         $a0 obj_disp($t2)               # save forwarding pointer
.GenGC_OfsCopy_done:
    jr         $ra                             # return
.GenGC_OfsCopy_forward:
    lw         $a0 obj_disp($a0)               # get forwarding pointer
    jr         $ra                             # return

#
# Major Garbage Collection
#
#   This collection occurs when ever the old area grows beyond a specified
#   point.  The minor collector sets up the Old, X, and New areas for
#   this collector.  It then collects all the live objects in the old
#   area (L0 to L1) into the new area (L2 to L3).  This collection consists
#   of five phases:
#
#     1) Set $gp into the new area (L2), and $s7 to L4.  Also set the
#        inputs for ".GenGC_OfsCopy".
#
#     2) Traverse the stack (see the minor collector) using ".GenGC_OfsCopy".
#
#     3) Check the registers (see the minor collector) using ".GenGC_OfsCopy".
#
#     4) Traverse the heap from L1 to $gp using ".GenGC_OfsCopy".  Note
#        that this includes the X area.  (see the minor collector)
#
#     5) Block copy the region L1 to $gp back L1-L0 bytes to create the
#        next old area.  Save the end in L1.  Calculate the size of the
#        live objects collected from the old area and return this value.
#
#   Note that the pointers returned by ".GenGC_OfsCopy" are not valid
#   until the block copy is done.
#
#   INPUT:
#    $a0: end of stack
#    heap_start: start of heap
#
#   OUTPUT:
#    $a0: size of all live objects collected
#
#   Registers modified:
#    $t0, $t1, $t2, $v0, $v1, $a0, $a1, $a2, $gp, $s7
#

    .global    .GenGC_MajorC
.GenGC_MajorC:
    addiu      $sp $sp -20
    sw         $ra 20($sp)                        # save return address
    la         $t0 heap_start
    lw         $s7 GenGC_HDRL4($t0)               # limit pointer for collection
    lw         $gp GenGC_HDRL2($t0)               # allocation pointer for collection
    lw         $a1 GenGC_HDRL0($t0)               # set inputs for OfsCopy
    lw         $a2 GenGC_HDRL1($t0)
    lw         $v1 GenGC_HDRL2($t0)
    sw         $a0 16($sp)                        # save stack end
    lw         $t0 GenGC_HDRSTK($t0)              # set $t0 to stack start
    move       $t1 $a0                            # set $t1 to stack end
    ble        $t0 $t1 .GenGC_MajorC_stackend     # check for empty stack
.GenGC_MajorC_stackloop:                          # $t1 stack end, $t0 index
    addiu      $t0 $t0 -4                         # update index
    sw         $t0 12($sp)                        # save stack index
    lw         $a0 4($t0)                         # get stack item
    jal        .GenGC_OfsCopy                     # check and copy
    lw         $t0 12($sp)                        # load stack index
    sw         $a0 4($t0)
    lw         $t1 16($sp)                        # restore stack end
    bgt        $t0 $t1 .GenGC_MajorC_stackloop    # loop
.GenGC_MajorC_stackend:
    la         $t0 heap_start
    lw         $t0 GenGC_HDRREG($t0)              # get Register mask
    sw         $t0 16($sp)                        # save Register mask
.GenGC_MajorC_reg16:
    srl        $t0 $t0 16                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MajorC_reg17         # check if set
    move       $a0 $16                            # set test pointer
    jal        .GenGC_OfsCopy                     # check and copy
    move       $16 $a0                            # update register
.GenGC_MajorC_reg17:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 17                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MajorC_reg18         # check if set
    move       $a0 $17                            # set test pointer
    jal        .GenGC_OfsCopy                     # check and copy
    move       $17 $a0                            # update register
.GenGC_MajorC_reg18:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 18                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MajorC_reg19         # check if set
    move       $a0 $18                            # set test pointer
    jal        .GenGC_OfsCopy                     # check and copy
    move       $18 $a0                            # update register
.GenGC_MajorC_reg19:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 19                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MajorC_reg20         # check if set
    move       $a0 $19                            # set test pointer
    jal        .GenGC_OfsCopy                     # check and copy
    move       $19 $a0                            # update register
.GenGC_MajorC_reg20:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 20                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MajorC_reg21         # check if set
    move       $a0 $20                            # set test pointer
    jal        .GenGC_OfsCopy                     # check and copy
    move       $20 $a0                            # update register
.GenGC_MajorC_reg21:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 21                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MajorC_reg22         # check if set
    move       $a0 $21                            # set test pointer
    jal        .GenGC_OfsCopy                     # check and copy
    move       $21 $a0                            # update register
.GenGC_MajorC_reg22:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 22                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MajorC_reg24         # check if set
    move       $a0 $22                            # set test pointer
    jal        .GenGC_OfsCopy                     # check and copy
    move       $22 $a0                            # update register
.GenGC_MajorC_reg24:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 24                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MajorC_reg25         # check if set
    move       $a0 $24                            # set test pointer
    jal        .GenGC_OfsCopy                     # check and copy
    move       $24 $a0                            # update register
.GenGC_MajorC_reg25:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 25                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MajorC_reg30         # check if set
    move       $a0 $25                            # set test pointer
    jal        .GenGC_OfsCopy                     # check and copy
    move       $25 $a0                            # update register
.GenGC_MajorC_reg30:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 30                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MajorC_reg31         # check if set
    move       $a0 $30                            # set test pointer
    jal        .GenGC_OfsCopy                     # check and copy
    move       $30 $a0                            # update register
.GenGC_MajorC_reg31:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 31                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MajorC_regend        # check if set
    move       $a0 $31                            # set test pointer
    jal        .GenGC_OfsCopy                     # check and copy
    move       $31 $a0                            # update register
.GenGC_MajorC_regend:
    la         $t0 heap_start
    lw         $t0 GenGC_HDRL1($t0)               # start of X area
    bge        $t0 $gp .GenGC_MajorC_heapend      # check for no objects
.GenGC_MajorC_heaploop:                           # $t0: index, $gp: limit
    addiu      $t0 $t0 4                          # skip over eyecatcher
    addiu      $t1 $0 -1                          # check for eyecatcher
    lw         $t2 obj_eyecatch($t0)
    bne        $t1 $t2 .GenGC_MajorC_error        # eyecatcher not found
    lw         $a0 obj_size($t0)                  # get object size
    sll        $a0 $a0 2                          # words to bytes
    lw         $t1 obj_tag($t0)                   # get the object's tag
    lw         $t2 _int_tag                       # test for int object
    beq        $t1 $t2 .GenGC_MajorC_int
    lw         $t2 _bool_tag                      # test for bool object
    beq        $t1 $t2 .GenGC_MajorC_bool
    lw         $t2 _string_tag                    # test for string object
    beq        $t1 $t2 .GenGC_MajorC_string
.GenGC_MajorC_other:
    addi       $t1 $t0 obj_attr                   # start at first attribute
    add        $t2 $t0 $a0                        # limit of attributes
    bge        $t1 $t2 .GenGC_MajorC_nextobj      # check for no attributes
    sw         $t0 16($sp)                        # save pointer to object
    sw         $a0 12($sp)                        # save object size
    sw         $t2 4($sp)                         # save limit
.GenGC_MajorC_objloop:                            # $t1: index, $t2: limit
    sw         $t1 8($sp)                         # save index
    lw         $a0 0($t1)                         # set pointer to check
    jal        .GenGC_OfsCopy                     # check and copy
    lw         $t1 8($sp)                         # restore index
    sw         $a0 0($t1)                         # update object pointer
    lw         $t2 4($sp)                         # restore limit
    addiu      $t1 $t1 4
    blt        $t1 $t2 .GenGC_MajorC_objloop      # loop
.GenGC_MajorC_objend:
    lw         $t0 16($sp)                        # restore pointer to object
    lw         $a0 12($sp)                        # restore object size
    b          .GenGC_MajorC_nextobj              # next object
.GenGC_MajorC_string:
    sw         $t0 16($sp)                        # save pointer to object
    sw         $a0 12($sp)                        # save object size
    lw         $a0 str_size($t0)                  # set test pointer
    jal        .GenGC_OfsCopy                     # check and copy
    lw         $t0 16($sp)                        # restore pointer to object
    sw         $a0 str_size($t0)                  # update size pointer
    lw         $a0 12($sp)                        # restore object size
.GenGC_MajorC_int:
.GenGC_MajorC_bool:
.GenGC_MajorC_nextobj:
    add        $t0 $t0 $a0                        # find next object
    blt        $t0 $gp .GenGC_MajorC_heaploop     # loop
.GenGC_MajorC_heapend:
    la         $t0 heap_start
    lw         $a0 GenGC_HDRL2($t0)               # get end of collection
    sub        $a0 $gp $a0                        # get length after collection
    lw         $t1 GenGC_HDRL0($t0)               # get L0
    lw         $t2 GenGC_HDRL1($t0)               # get L1
    bge        $t2 $gp .GenGC_MajorC_bcpyend      # test for empty copy
.GenGC_MajorC_bcpyloop:                           # $t2 index, $gp limit, $t1 dest
    lw         $v0 0($t2)                         # copy
    sw         $v0 0($t1)
    addiu      $t2 $t2 4                          # update each index
    addiu      $t1 $t1 4
    bne        $t2 $gp .GenGC_MajorC_bcpyloop     # loop
.GenGC_MajorC_bcpyend:
    sw         $s7 GenGC_HDRL4($t0)               # save end of heap
    lw         $t1 GenGC_HDRL0($t0)               # get L0
    lw         $t2 GenGC_HDRL1($t0)               # get L1
    sub        $t1 $t2 $t1                        # find offset of block copy
    sub        $gp $gp $t1                        # find end of old area
    sw         $gp GenGC_HDRL1($t0)               # save end of old area
    lw         $ra 20($sp)                        # restore return address
    addiu      $sp $sp 20
    jr         $ra                                # return
.GenGC_MajorC_error:
    la         $a0 .GenGC_MAJORERROR              # show error message
    li         $v0 4
    syscall
    li         $v0 10                             # exit
    syscall

#
# Set the Register (REG) mask
#
#   If bit #n is set in the Register mask, register #n will be
#   automatically updated by the garbage collector.  Note that
#   this mask is masked (ANDed) with the ARU mask.  Only those
#   registers in the ARU mask can be updated automatically.
#
#   INPUT:
#    $a0: new Register (REG) mask
#    heap_start: start of the heap
#
#   Registers modified:
#    $t0
#

    .global    .GenGC_SetRegMask
.GenGC_SetRegMask:
    li     $t0 GenGC_ARU_MASK        # apply Automatic Register Mask (ARU)
    and    $a0 $a0 $t0
    la     $t0 heap_start            # set $t0 to the start of the heap
    sw     $a0 GenGC_HDRREG($t0)     # save the Register mask
    jr     $ra                       # return

#
# Query the Register (REG) mask
#
#   INPUT:
#    heap_start: start of the heap
#
#   OUTPUT:
#    $a0: current Register (REG) mask
#
#   Registers modified:
#    none
#

    .global    .GenGC_QRegMask
.GenGC_QRegMask:
    la    $a0 heap_start            # set $a0 to the start of the heap
    lw    $a0 GenGC_HDRREG($a0)     # get the Register mask
    jr    $ra                       # return
# */