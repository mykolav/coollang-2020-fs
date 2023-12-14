########################################
# Data
########################################

    .data

# Assignment stack's tip pointer
assign_sp:                      .quad 0

#
# TODO: Place strings and other constants in `.section .rodata` instead of `.data`.
#

########################################
# Messages for the GenGC garbage collector
########################################

.GenGC.MSG_INIT_OK_ASCII:          .ascii "GenGC: initialized"
.GenGC.MSG_INIT_OK_LEN  =                 (. - .GenGC.MSG_INIT_OK_ASCII)
.GenGC.MSG_INITED_IN_TEST_ASCII:   .ascii "GenGC: initialized in test mode"
.GenGC.MSG_INITED_IN_TEST_LEN  =          (. - .GenGC.MSG_INITED_IN_TEST_ASCII)
.GenGC.MSG_INIT_ERROR_ASCII:       .ascii "GenGC: Unable to initialize the garbage collector"
.GenGC.MSG_INIT_ERROR_LEN  =              (. - .GenGC.MSG_INIT_ERROR_ASCII)

.GenGC.MSG_COLLECTING_ASCII:       .ascii "GenGC: Garbage collecting ..."
.GenGC.MSG_COLLECTING_LEN  =              (. - .GenGC.MSG_COLLECTING_ASCII)

.GenGC.MSG_MAJOR_ASCII:            .ascii "GenGC: Major ..."
.GenGC.MSG_MAJOR_LEN  =                   (. - .GenGC.MSG_MAJOR_ASCII)
.GenGC.MSG_MAJOR_ERROR_ASCII:      .ascii "GenGC: Error during major garbage collection"
.GenGC.MSG_MAJOR_ERROR_LEN  =             (. - .GenGC.MSG_MAJOR_ERROR_ASCII)

.GenGC.MSG_MINOR_ASCII:            .ascii "GenGC: Minor ..."
.GenGC.MSG_MINOR_LEN  =                   (. - .GenGC.MSG_MINOR_ASCII)
.GenGC.MSG_MINOR_ERROR_ASCII:      .ascii "GenGC: Error during minor garbage collection"
.GenGC.MSG_MINOR_ERROR_LEN  =             (. - .GenGC.MSG_MINOR_ERROR_ASCII)

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
# GenGC Generational Garbage Collector
#
#   This is an implementation of a generational garbage collector
#   as described in "Simple Generational Garbage Collection and Fast
#   Allocation" by Andrew W. Appel [Princeton University, March 1988].
#   This is a two generation scheme which uses an assignment table
#   to handle root pointers located in the older generation objects.
#
#   When the work area is filled, a minor garbage collection takes place
#   which moves all live objects into the reserve area. These objects
#   are then incorporated into the old area.  New reserve and work areas
#   are setup and allocation can continue in the work area. If a break-
#   point is reached in the size of the old area just after a minor
#   collection, a major collection then takes place. All live objects in
#   the old area are then copied into the new area, expanding the heap if
#   necessary. The X and new areas are then block copied back L1-L0
#   bytes to form the next old area.
#
#   The assignment table is implemented as a stack growing towards the
#   allocation pointer (`alloc_ptr`) in the work area. If they cross, a minor
#   collection is then carried out. This allows the garbage collector 
#   not to have to keep a fixed table of assignments. As a result, programs
#   with many assignments will tend not to be bogged down with extra
#   garbage collections [to make space in a fixed-size assignment table?].
#
#   The unused area was implemented to help keep the garbage collector
#   from continually expanding the heap. This buffer zone allows major
#   garbage collections to happen earlier, reducing the risk of expansions
#   due to too many live objects in the old area. The histories kept by
#   the garbage collector in MAJOR0, MAJOR1, MINOR0, and MINOR1 also help
#   to prevent unnecessary expansions of the heap. If many live objects
#   were recently collected, the garbage collections will start to occur
#   sooner.
#
#   Note that during a minor collection, the work area is guaranteed to
#   fit within the reserve area. However, during a major collection, the
#   old area will not necessarily fit in the new area. If the latter occurs,
#   `.GenGC.offset_copy` will detect this and expand the heap.
#
#   The heap is expanded on two different occasions:
#
#     1) After a major collection, the old area is set to be at most
#        1/(2^GenGC_OLDRATIO) of the usable heap (L0 to L3). Note that
#        first L4 is checked to see if any of the unused memory between L3
#        and L4 is enough to satisfy this requirement. If not, then the
#        heap will be expanded.  If it is, the appropriate amount will be
#        transfered from the unused area to the work/reserve area.
#
#     2) During a major collection, if the live objects in the old area
#        do not fit within the new area, the heap is expanded and `assign_sp`
#        is updated to reflact this. This value later gets stored back
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
#     |    |              |                 |             Assignment table
#     |    |              |                 |             |
#     |    |              |                 |             |   Unused
#     |    |              |                 |             |   |
#     v    v              v                 v             v   v
#     +----+--------------+-----------------+-------------+---+---------+
#     |XXXX| Old Area     | Reserve Area    | Work Area   |XXX| Unused  |
#     +----+--------------+-----------------+-------------+---+---------+
#      ^    ^              ^                 ^    ^        ^   ^         ^
#      |    |              |                 |    |-->  <--|   |         |
#      |    L0             L1                L2   |        |   L3        L4
#      |                                          |        |
#      heap_start                            alloc_ptr    assign_sp
#
#     `alloc_ptr`: points to the next free word in the work
#         area during normal allocation.  During a minor garbage collection,
#         it points to the next free work in the reserve area.
#
#     `assign_sp`: points to the tip of the assignment stack, 
#         `alloc_ptr` cannot go past `assign_sp`.
#         Between `assign_sp` and L3 sits the assignment stack 
#         which grows towards `alloc_ptr`.
#
#   The following invariant is maintained for `alloc_ptr` and `assign_ptr` by 
#   the garbage collector's code at all times:
#
#      `alloc_ptr` is always strictly less than `assign_sp`.
#      Hence there is always enough room for at least one assignment record 
#      at the tip of assignment stack.
#
#      If the above invariant hadn't been maintained, we would've ended up
#      in a situation where at the moment we're requested to record an 
#      assignment, `alloc_ptr` == `assign_sp`. As there is no room to 
#      record the assignment a garbage collection has to run first. 
#      As the unrecorded assignment can point to a GC root, the garbage 
#      collection would've missed that root and removed a live object or 
#      multiple live objects...
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
#     ^    ^                  ^      ^   ^      ^                       ^
#     |    |                  |      |   |      |-->                    |
#     |    L0                 L1     |   L2    alloc_ptr      assign_sp, L4
#     |                              |
#     heap_start                     breakpoint
#
#     `alloc_ptr` (allocation pointer): During a major collection, this points
#         into the next free word in the new area.
#
#     `assign_sp`: During a major collection, this points to the tip of an 
#         empty assignment stack wich is the same as the limit of heap memory.
#         `alloc_ptr` is not allowed to pass this value. If the live objects 
#         in the old area cannot fit in the new area, more memory is allocated 
#         and `assign_sp` is adjusted accordingly.
#
#   See the `alloc_ptr` < `assign_sp` invariant descriptions above.
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
#        a valid address in the heap is assumed to point to an object
#        in the heap.  Even heap addresses on the stack that are actually
#        something else (e.g., raw integers) will probably cause an
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
#        to keep any non-pointers from being updated accidentally. The
#        functions `.GenGC.check_copy` and `.GenGC.offset_copy` are responsible
#        for these checks.
#
#     4) The size stored in the object does not include the word required
#        to store the eyecatcher for the object in the heap.  This allows
#        the prototype objects to not require its own eyecatcher.  Also,
#        a size of 0 is invalid because it is used as a flag by the garbage
#        collector to indicate a forwarding pointer in the `obj_disp` field.
#
#     5) Roots are contained in the following areas: the stack, registers
#        specified in the REG mask, and the assignment table.
########################################

#
# GenGC header offsets from `.Platform.heap_start`
#

.GenGC.HDR_SIZE      = 80    # size of GenGC header

.GenGC.HDR_L0        = 0     # old area start
.GenGC.HDR_L1        = 8     # old area end/reserve area start
.GenGC.HDR_L2        = 16    # reserve area end/work area start
.GenGC.HDR_L3        = 24    # assignment table end/unused start
.GenGC.HDR_L4        = 32    # unused end

.GenGC.HDR_MAJOR0    = 40    # total size of objects in the new area 
                             # after last major collection
.GenGC.HDR_MAJOR1    = 48    # (MAJOR0 + MAJOR1) / 2

.GenGC.HDR_MINOR0    = 56    # size of all live objects collected
                             # during last minor collection
.GenGC.HDR_MINOR1    = 64    # (MINOR0 + MINOR1) / 2

.GenGC.HDR_STK       = 72    # base of stack

#
# The ROOT_REG_MASK bit mask tells the garbage collector which register(s) it
# should check for roots and automatically update on a garbage collection. 
#
# The registers are numbered 0 to 15 in the following way (in descending order).
# A register number corresponds to its bit in the mask.
#
# 15 - 08: r15 - r8
# 07 - 00: rsp, rbp, rdi, rsi, rdx, rcx, rbx, rax
#
# Only 16 lest significant bits of the mask are used.
# The other bits are padding the mask to 32 bit, their value is always 0.
#
# 3 2          1          0
# 10987654_32109876_54321098_76543210 |
# 00000000_00000000_11111111_00111111 | r15 - r8, rdi - rax
# ------++-------++-------++-------++ | (i.e., all the registers, except rsp, rbp)
#       00       00       FF       3F |
#

.GenGC.ROOT_REG_MASK = 0x0000FF3F

#
# Granularity of heap expansion
#
#   The heap is always expanded in multiples of 2^k, where
#   k is the granularity.
#

.GenGC.HEAP_EXP_SIZE = 32768 # in bytes

#
# Old to usable heap size ratio
#
#   After a major collection, the ratio of size of old area to the usable
#   size of the heap is at most 1/(2^k) where k is the value provided.
#

.GenGC.OLD_RATIO     = 2     # 1/(2^2)=.25=25%

#
# Initialization
#
#   Sets up the header information block for the garbage collector.
#   This block is located at the start of the heap (`Platform.heap_start`)
#   and includes information needed by the garbage collector.  It
#   also calculates the boundary for the reserve and work areas and
#   sets the L2 pointer accordingly, rounding off in favor of the
#   reserve area.
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
#    %rax, %rdi, %rsi, .Platform.alloc
#

    .global .GenGC.init
.GenGC.init:
    STKBASE_SIZE      = 8
    STKBASE_OFFSET    = -STKBASE_SIZE
    FRAME_SIZE        = STKBASE_SIZE

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    movq     %rdi, STKBASE_OFFSET(%rbp)

    movq     $.GenGC.HEAP_EXP_SIZE, %rdi               # allocate initial heap space
    call     .Platform.alloc
    
    movq     .Platform.heap_start(%rip), %rdi
    movq     $.GenGC.HDR_SIZE, %rax
    addq     %rdi, %rax                                # %rax contains the first addr past the header
    movq     %rax, .GenGC.HDR_L0(%rdi)                 # init the header's L0 field
    movq     %rax, .GenGC.HDR_L1(%rdi)                 # init the header's L1 field

    movq     .Platform.heap_end(%rip), %rsi
    subq     %rax, %rsi                                # heap_end - (heap_start + .GenGC.HDR_SIZE)
    sarq     $1, %rsi                                  # (heap_end - (heap_start + .GenGC.HDR_SIZE)) / 2
    andq     $(-8), %rsi                               # round down to the closest smaller multiple of 8 bytes
                                                       # since our object sizes are multiples of 8 bytes
    jz       .GenGC.init.error                         # heap initially too small

    movq     .Platform.heap_end(%rip), %rax
    movq     %rax, .GenGC.HDR_L3(%rdi)                 # initially the end of work area is at the heap end
    movq     %rax, assign_sp(%rdi)                     # initially the tip of assign stack is at the end of work area
    movq     %rax, .GenGC.HDR_L4(%rdi)                 # the end of unused area is at the heap end

    subq     %rsi, %rax                                # %rsi contains the work area size
                                                       # L3 - %rsi = reserve area end/work area start
    movq     %rax, .GenGC.HDR_L2(%rdi)                 # store the calculated start of work area
    movq     %rax, alloc_ptr(%rip)                     # initially the allocation pointer is at the start of work area

    movq     $0, .GenGC.HDR_MAJOR0(%rdi)               # init histories with zeros
    movq     $0, .GenGC.HDR_MAJOR1(%rdi)
    movq     $0, .GenGC.HDR_MINOR0(%rdi)
    movq     $0, .GenGC.HDR_MINOR1(%rdi)

    movq     STKBASE_OFFSET(%rbp), %rax
    movq     %rax, .GenGC.HDR_STK(%rdi)                # init stack base

    movq     .MemoryManager.TEST_ENABLED(%rip), %rax   # check if heap testing enabled
    testq    %rax, %rax
    jz       .GenGC.init.heap_test_disabled

    movq     $.GenGC.MSG_INITED_IN_TEST_ASCII, %rdi
    movq     $.GenGC.MSG_INITED_IN_TEST_LEN, %rsi
    call     .Platform.out_string
    call     .Runtime.out_nl
    jmp      .GenGC.init.ok

.GenGC.init.heap_test_disabled:
    movq     $.GenGC.MSG_INIT_OK_ASCII, %rdi
    movq     $.GenGC.MSG_INIT_OK_LEN, %rsi
    call     .Platform.out_string
    call     .Runtime.out_nl

.GenGC.init.ok:
    movq     %rbp, %rsp
    popq     %rbp
    ret

.GenGC.init.error:
    movq     $.GenGC.MSG_INIT_ERROR_ASCII, %rdi
    movq     $.GenGC.MSG_INIT_ERROR_LEN, %rsi
    call     .Platform.out_string
    call     .Runtime.out_nl

    movq   $1, %rdi
    jmp    .Platform.exit_process

#
# Record an Assignment in the Assignment Stack
#
#   The GC's code guarantees `alloc_ptr` is always strictly less than `assign_sp`.
#   Hence there is always enough room for at least one assignment record 
#   at the tip of assignment stack.
#
#   If the above invariant hadn't been maintained, we would've ended up
#   in a situation where at the moment we're requested to record an 
#   assignment, `alloc_ptr` == `assign_sp`. As there is no room to 
#   record the assignment a garbage collection has to run first. 
#   As the unrecorded assignment can point to a GC root, the garbage 
#   collection would've missed that root and removed a live object or 
#   multiple live objects...
#
#   INPUT:
#    %rdi: pointer to the pointer being assigned to
#
#   Registers modified:
#    %rax, %rdi, %rsi, .GenGC.collect
#

    .global .GenGC.handle_assign
.GenGC.handle_assign:
    POINTER_SIZE = 8

    pushq    %rbp
    movq     %rsp, %rbp

    # TODO: Preserve %rdi?

    movq     assign_sp(%rip), %rax
    subq     $POINTER_SIZE, %rax
    movq     %rax, assign_sp(%rip)         # make room in the assignment stack
    movq     %rdi, 0(%rax)                 # place pointer to the pointer being assigned to
                                           # at the tip of assignment stack
    cmpq     alloc_ptr(%rdi), %rax         # if `alloc_ptr` and `assign_sp` have met
                                           # we'll have to collect garbage
    jg       .GenGC.handle_assign.done

    xor      %edi, %edi                    # we request to allocate 0 bytes
                                           # as we only need to collect garbage
    movq     %rbp, %rsi                    # the tip of stack to start checking for roots from
    call     .GenGC.collect

.GenGC.handle_assign.done:
    movq     %rbp, %rsp
    popq     %rbp
    ret

#
# Generational Garbage Collection
#
#   This function implements the generational garbage collection.
#
#   It first calls the minor collector, `.GenGC.minor_collect`, and then
#   updates its history in the header.
#
#   The breakpoint is then calculated. If the breakpoint is reached or 
#   there is still not enough room to allocate the requested size, 
#   a major garbage collection takes place by calling `.GenGC.major_collect`.
#
#   After the major collection, the size of the old area is analyzed.
#   If it is greater than 1/(2^GenGC_OLDRATIO) of the total usable heap
#   size (L0 to L3), the heap is expanded. 
#
#   If there is still not enough room to allocate the requested size, 
#   the heap is expanded further to make sure that the specified 
#   amount of memory can be allocated.
#
#   If there is enough room in the unused area (L3 to L4),
#   this memory is used and the heap is not expanded.
#
#   The `assign_sp` and `alloc_ptr` pointers are then set 
#   as well as the L2 pointer.
#
#   If a major collection is not done, the X area is incorporated 
#   into the old area (i.e. the value of L2 is moved into L1) and 
#   `assign_sp`, `alloc_ptr`, and L2 are then set.
#
#   INPUT:
#    %rdi: requested allocation size in bytes
#    %rsi: the tip of stack to start checking for roots from
#
#   OUTPUT:
#    %rdi: requested allocation size in bytes (unchanged)
#
#   GLOBALS MODIFIED:
#    L1, L2, L3, L4, alloc_ptr, assign_sp,
#    MINOR0, MINOR1, MAJOR0, MAJOR1
#
#   Registers modified:
#    %rax, %rdi, %rsi, %rcx, %rdx, .GenGC.minor_collect, .GenGC.major_collect
#

    .global .GenGC.collect
.GenGC.collect:
    REQUESTED_SIZE_SIZE   = 8
    REQUESTED_SIZE_OFFSET = -REQUESTED_SIZE_SIZE
    STACK_TIP_SIZE        = 8
    STACK_TIP_OFFSET      = -(STACK_TIP_SIZE + REQUESTED_SIZE_SIZE)
    FRAME_SIZE            = STACK_TIP_SIZE + REQUESTED_SIZE_SIZE

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    movq     %rdi, REQUESTED_SIZE_OFFSET(%rbp)
    movq     %rsi, STACK_TIP_OFFSET(%rbp)

    # movq     $.GenGC.MSG_COLLECTING_ASCII, %rdi
    # movq     $.GenGC.MSG_COLLECTING_LEN, %rsi
    # call     .Platform.out_string
    # call     .Runtime.out_nl

    movq     REQUESTED_SIZE_OFFSET(%rbp), %rdi 
    movq     STACK_TIP_OFFSET(%rbp), %rsi
    call     .GenGC.minor_collect                # %rax contains the size of all collected live objects

    # %rdi = heap_start
    movq     .Platform.heap_start(%rip), %rdi
    # Update MINOR0, MINOR1
    movq     %rax, .GenGC.HDR_MINOR0(%rdi)       # MINOR0 = the size of all collected live objects
    movq     .GenGC.HDR_MINOR1(%rdi), %rsi       # %rsi = MINOR1
    addq     %rax, %rsi                          # %rsi = MINOR1 + MINOR0
    sarq     $1, %rsi                            # %rsi = (MINOR0 + MINOR1) / 2
    movq     %rsi, .GenGC.HDR_MINOR1(%rdi)       # MINOR1 = (MINOR0 + MINOR1) / 2
    
    # breakpoint = MIN(L3 - MAX(MAJOR0, MAJOR1) - MAX(MINOR0, MINOR1), 
    #                  L3 - (L3 - L0) / 2)
    #
    # L3 - MAX(MAJOR0, MAJOR1) - MAX(MINOR0, MINOR1)
    # %rdx = MAX(MINOR0, MINOR1)
    movq     %rax, %rdx                          # %rdx = MINOR0
    cmpq     %rsi, %rax                          # MINOR0 >= MINOR1?
    jge      .GenGC.collect.calc_max_of_majors   # if yes, go to .GenGC.collect.calc_max_of_majors
    movq     %rsi, %rdx                          # if no, %rdx = MINOR1
.GenGC.collect.calc_max_of_majors:
    # %rcx = MAX(MAJOR0, MAJOR1)
    movq     .GenGC.HDR_MAJOR0(%rdi), %rcx       # %rcx = MAJOR0
    movq     .GenGC.HDR_MAJOR1(%rdi), %rax       # %rax = MAJOR1
    cmpq     %rax, %rcx                          # MAJOR0 >= MAJOR1?
    jge      .GenGC.collect.calc_min_breakpoint  # if yes, go to .GenGC.collect.calc_min_breakpoint
    movq     %rax, %rcx                          # if no, %rcx = MAJOR1
.GenGC.collect.calc_min_breakpoint:
    movq     .GenGC.HDR_L3(%rdi), %rsi           # %rsi = L3
    subq     %rcx, %rsi                          # %rsi = L3 - MAX(MAJOR0, MAJOR1)
    subq     %rdx, %rsi                          # %rsi = L3 - MAX(MAJOR0, MAJOR1) - MAX(MINOR0, MINOR1)

    # L3 - (L3 - L0) / 2
    movq     .GenGC.HDR_L3(%rdi), %rax           # %rax = L3
    movq     %rax, %rcx                          # %rcx = L3
    subq     .GenGC.HDR_L0(%rdi), %rax           # %rax = L3 - L0
    sarq     $1, %rax                            # %rax = (L3 - L0) / 2
    subq     %rax, %rcx                          # %rcx = L3 - (L3 - L0) / 2

    # %rcx = MIN(%rcx, %rsi) = breakpoint
    cmpq     %rsi, %rcx                          # %rcx <= %rsi?
    jle      .GenGC.collect.check_breakpoint     # if yes, go to .GenGC.collect.check_breakpoint
    movq     %rsi, %rcx                          # if no, %rcx = %rsi
.GenGC.collect.check_breakpoint:
    # %rcx contains the breakpoint value
    movq     .GenGC.HDR_L1(%rdi), %rax
    cmpq     %rcx, %rax                          # Has L1 (the end of Old Area) crossed the breakpoint?
                                                 # (%rax >= %rcx?)
    jge      .GenGC.collect.do_major             # if yes, perform a major collection
    #. if no, update L1, set up the new Reserve/Work areas,
    #  reset `alloc_ptr`, and `assign_sp`, etc
    movq     .GenGC.HDR_L2(%rdi), %rcx           # %rcx = L2
    movq     .GenGC.HDR_L3(%rdi), %rdx           # %rdx = L3
    movq     %rdx, %rsi                          # %rsi = L3
    # find Reserve/Work areas boundary
    subq     %rcx, %rdx                          # %rdx = L3 - L2
    sarq     $1, %rdx                            # %rdx = (L3 - L2) / 2
    andq     $(-8), %rdx                         # %rdx = the nearest smaller multiple of 8
                                                 # so Reserve Area >= Work Area by 8 bytes
    subq     %rdx, %rsi                          # %rsi = the new L2
                                                 # (L3 - round_down((L3 - L2) / 2))
    movq     %rsi, %rcx                          # %rcx = the new L2

    # Enough space to allocate the requested size?
    movq     REQUESTED_SIZE_OFFSET(%rbp), %rax   # %rax = the requested allocation size
    addq     %rax, %rsi                          # %rsi = (the new L2) + alloc size
    movq     .GenGC.HDR_L3(%rdi), %rdx           # %rdx = L3
    cmpq     %rdx, %rsi                          # %rsi >= L3?
    jge      .GenGC.collect.do_major             # if yes, there's not engough space,
                                                 # we'll have to preform a major collection
    # No major collection required
    movq     .GenGC.HDR_L2(%rdi), %rax
    movq     %rax, .GenGC.HDR_L1(%rdi)           # include the live object collected
                                                 # by `.GenGC.minor_collect` into Old Area
    # %rcx contains the new L2 value
    movq     %rcx, .GenGC.HDR_L2(%rdi)           # set up the new Reserver/Work boundary
    movq     %rcx, alloc_ptr(%rip)               # set `alloc_ptr` at the start of Work Area
    movq     %rdx, assign_sp(%rip)               # set `assign_sp` to L3 (Work Area's end)
                                                 # effectively clearing the assignment stack.
                                                 # Garbage collecting the young gen results in
                                                 # no old gen objects pointing to young gen
                                                 # objects anymore, so we can clear the stack.
    jmp      .GenGC.collect.done
.GenGC.collect.do_major:
    
    # movq     $.GenGC.MSG_MAJOR_ASCII, %rdi
    # movq     $.GenGC.MSG_MAJOR_LEN, %rsi
    # call     .Platform.out_string
    # call     .Runtime.out_nl

    movq     REQUESTED_SIZE_OFFSET(%rbp), %rdi 
    movq     STACK_TIP_OFFSET(%rbp), %rsi
    call     .GenGC.major_collect                # %rax: the size of all collected live objects
                                                 # L1:   the new Old Area's end

    # %rdi = heap_start
    movq     .Platform.heap_start(%rip), %rdi

    # Update MAJOR0, MAJOR1
    movq     %rax, .GenGC.HDR_MAJOR0(%rdi)       # MAJOR0 = total size of objects in the new area 
                                                 #          after the major collection.
    movq     .GenGC.HDR_MAJOR1(%rdi), %rsi       # %rsi = MAJOR1
    addq     %rax, %rsi                          # %rsi = MAJOR1 + MAJOR0
    sarq     $1, %rsi                            # %rsi = (MAJOR0 + MAJOR1) / 2
    movq     %rsi, .GenGC.HDR_MAJOR1(%rdi)       # MAJOR1 = (MAJOR0 + MAJOR1) / 2

    # Calculate how much we need to expand the heap (if at all),
    # to preserve the chosen Old Area/Heap size ratio.

    # Calculate the max Old Area boundary that 
    # still stays within the chosen ratio to the total heap size.
    # Place the value in %rdx
    movq     .GenGC.HDR_L0(%rdi), %rdx           # %rdx = L0
    movq     .GenGC.HDR_L3(%rdi), %rcx           # %rcx = L3
    movq     %rcx, %rax                          # %rax = L3
    subq     %rdx, %rax                          # %rax = L3 - L0
    sarq     $.GenGC.OLD_RATIO, %rax             # %rax = (L3 - L0) / 2^.GenGC.OLD_RATIO
    addq     %rax, %rdx                          # %rdx = L0 + (L3 - L0) / 2^.GenGC.OLD_RATIO
                                                 #      = sizeof(max Old Area)

    # Calculate the difference between the new Old Area size and the max Old Area size.
    # Place the value in %rcx
    #
    # If the difference <= 0, although we don't branch physically,
    # further calculations on %rcx can be logically ignored.
    # We'll check the memory to allocate's size is not <= 0 later on.
    #
    # If the difference > 0 we need to allocate %rcx * 2^.GenGC.OLD_RATIO memory
    # to restore the Old Area/Heap size ratio.
    #
    # Keep in mind,
    #     L1 - (L0 + (L3 - L0) / 2^.GenGC.OLD_RATIO) =
    #     (L0 + sizeof(the new Old Area)) - (L0 + sizeof(max Old Area)) =
    #     sizeof(the new Old Area) - sizeof(max Old Area)
    movq    .GenGC.HDR_L1(%rdi), %rcx            # %rcx = L1
                                                 # (`.GenGC.major_collect` places
                                                 #  the new Old Area's end into L1)
    subq     %rdx, %rcx                          # %rcx = L1 - (L0 + (L3 - L0) / 2^.GenGC.OLD_RATIO)
    salq     $.GenGC.OLD_RATIO, %rcx             # %rcx = %rcx * 2^.GenGC.OLD_RATIO

    # Calculate how much we need to expand the heap (if at all),
    # to accomodate the requested allocation size in the Reserve/Work areas

    # Calculate the new Reserve/Work areas boundary's position.
    # Which is the same as Work Area's start position.
    # Place the value into %rdx
    movq     .GenGC.HDR_L3(%rdi), %rsi           # %rsi =  L3
    subq     .GenGC.HDR_L0(%rdi), %rsi           # %rsi =  L3 - L0
    sarq     $1, %rsi                            # %rsi =  (L3 - L0) / 2
    andq     $(-8), %rsi                         # %rsi =  ((L3 - L1) / 2) & (-8)
    movq     .GenGC.HDR_L3(%rdi), %rdx           # %rdx =  L3
    subq     %rsi, %rdx                          # %rdx =  L3 - ((L3 - L1) / 2) & (-8)
                                                 #      =  Reserve/Work areas boundary
                                                 #      =  Work Area start

    # Now, see whether the requested allocation size fits withing Work Area's boundaries.
    # Calculate the difference between (Work Area start + requested alloc size)
    # and Work Area's end position (L3).
    # Place the value into %rdx.
    #
    # If the difference <= 0, although we don't branch physically,
    # further calculations on %rdx can be logically ignored.
    # We'll check the memory to allocate's size is not <= 0 later on.
    #
    # If the difference > 0 we need to allocate %rdx * 2 memory
    # as we guarantee Reserve Area to be >= the size of Work Area we need
    # %rdx bytes for Work Area + %rdx bytes for Reserve Area.
    addq     REQUESTED_SIZE_OFFSET(%rbp), %rdx   # %rdx =  Work Area start + requested alloc size
    subq     .GenGC.HDR_L3(%rdi), %rdx           # %rdx =  (Work Area's start + requested alloc size) - L3
    addq     $8, %rdx                            # %rdx += 8 -- adjust for round off errors 
                                                 #              (interger division by 2, etc)
    salq     $1, %rdx                            # %rdx *= 2 -- need to allocate this much memory
                                                 # %rcx = max(%rcx, %rdx)
    cmpq     %rdx, %rcx
    jge      .GenGC.collect.ensure_heap_size     # if (%rcx >= %rdx) go to .GenGC.collect.ensure_heap_size
    movq     %rdx, %rcx                          # else              %rcx = %rdx
.GenGC.collect.ensure_heap_size:
    # If max(%rcx, %rdx) <= 0, that means:
    #   1) both %rcx and %rdx are <= 0
    #   2) Old Area/Heap size ration is preserved
    #   3) We have enough space in Work Area to
    #      accomodate the requested allocation size
    cmpq     $0, %rcx
    jle      .GenGC.collect.set_assign_sp_and_L2    # if (%rcx <= 0) 
                                                    #     go to .GenGC.collect.set_assign_sp_and_L2
    # %rcx: we need to expand the heap by at least this number of bytes.
    # Round up %rcx to the nearest greater multiple of .GenGC.HEAP_EXP_SIZE (e.g., 32768 bytes):
    # %rcx = (%rcx + 32767) & (-32768)
    movq    $.GenGC.HEAP_EXP_SIZE, %rax          # %rax = 32768  = 00000000_00000000_10000000_00000000
    decq    %rax                                 # %rax = 32767  = 00000000_00000000_01111111_11111111
    addq    %rax, %rcx                           # %rcx = %rcx + 32767
    notq    %rax                                 # %rax = -32768 = 11111111_11111111_10000000_00000000
    andq    %rax, %rcx                           # %rcx = %rcx & (-32768)

    # %rcx: the total  number of bytes to expand the heap by.
    #       (a multiple of .GenGC.HEAP_EXP_SIZE).
    # See how much of the total expansion is covered by Unused.
    movq     .GenGC.HDR_L4(%rdi), %rax           # %rax = L4
    subq     .GenGC.HDR_L3(%rdi), %rax           # %rax = L4 - L3 = sizeof(Unused)
    movq     %rcx, %rdx                          # %rdx = %rcx = 
                                                 #        total expansion size
    subq     %rax, %rdx                          # %rdx = total expansion size - sizeof(Unused)
    jg       .GenGC.collect.platform_allocate    # if ((total expansion size) > sizeof(Unused)) 
                                                 #     go to .GenGC.collect.platform_allocate
    # We have enough Unused space to cover the required expansion
    # without allocating any additional memory from the OS.
    movq     .GenGC.HDR_L3(%rdi), %rax           # %rax      = L3
    addq     %rcx, %rax                          # %rax      = L3 + total expansion size
    movq     %rax, .GenGC.HDR_L3(%rdi)           # L3        = L3 + total expansion size
    movq     %rax, assign_sp(%rip)               # assign_sp = L3 + total expansion size
                                                 # (therefore, the assign stack size = 0)
    jmp      .GenGC.collect.set_L2
.GenGC.collect.platform_allocate:
    movq     %rcx, %rdi                          # %rdi = %rcx = total expansion size
    call     .Platform.alloc                     # %rax: allocated memory block's start

    movq     .Platform.heap_start(%rip), %rdi    # %rdi      = heap_start
    movq     .Platform.heap_end(%rip), %rax      # %rax      = heap_end after the allocation
    movq     %rax, .GenGC.HDR_L3(%rdi)           # L3        = heap_end after the allocation
    movq     %rax, assign_sp(%rip)               # assign_sp = heap_end after the allocation
                                                 # (therefore, the assign stack size = 0)
    movq     %rax, .GenGC.HDR_L4(%rdi)           # L4        = heap_end after the allocation
                                                 # (therefore, sizeof(Unused) = 0 at this point)
    jmp      .GenGC.collect.set_L2
.GenGC.collect.set_assign_sp_and_L2:
    movq     .GenGC.HDR_L3(%rdi), %rax           # %rax = L3
    movq     %rax, assign_sp(%rip)               # assign_sp = L3
                                                 # (therefore, the assign stack size = 0)
.GenGC.collect.set_L2:
    # %rax must be equal to L3 at this point
    movq     %rax, %rsi                          # %rsi = %rax = L3
    subq     .GenGC.HDR_L1(%rdi), %rax           # %rax = L3 - L1
    sarq     %rax                                # %rax = (L3 - L1) / 2
    andq     $(-8), %rax                         # %rax = ((L3 - L1) / 2) & (-8)
                                                 #      = sizeof(Work Area)
    subq     %rax, %rsi                          # %rsi = L3 - sizeof(Work Area)
                                                 #      = Reserve/Work areas boundary
    movq     %rsi, .GenGC.HDR_L2(%rdi)           # L2 = %rsi
    movq     %rsi, alloc_ptr(%rip)               # alloc_ptr = %rsi

.GenGC.collect.done:
    # Zero out the new generation (the new Work Area) to help catch missing pointers
    movq     alloc_ptr(%rip), %rax
.GenGC.collect.work_area_clear_loop:
    movq     $0, 0(%rax)                         # zero out the quad at %rax
    addq     $8, %rax
    cmpq     assign_sp(%rip), %rax               # %rax < `assign_sp`
    jl       .GenGC.collect.work_area_clear_loop # if yes, we haven't reached 
                                                 # the end of Work Area yet

    movq    REQUESTED_SIZE_OFFSET(%rbp), %rdi    # restore requested allocation size in bytes
    movq    %rbp, %rsp
    popq    %rbp
    ret

#
# Check and Copy an Object
#
#   Checks that the input pointer points to a heap object.
#
#   If so, it then checks the object for a forwarding pointer by
#   checking the object's size for 0.
#
#   If found, the forwarding pointer is returned.
#   Else, the object is copied to `alloc_ptr` and a pointer to 
#   this copy is returned.
#
#   The following tests are done to determine if the object is 
#   a heap object:
#
#     1) The pointer is a multiple of 8 (a quad is our chosen minimal granularity)
#     2) The pointer is within the specified limits
#     3) The word before the pointer is the eye catcher 0xFFFF_FFFF
#     4) The word at the pointer is a valid tag (i.e. not equal to
#        0xFFFF_FFFF)
#
#   INPUT:
#    %rdi: pointer to check and copy
#    %rsi: lower bound object should be within.
#    %rdx: upper bound object should be within.
#
#   OUTPUT:
#    %rax: if %rdi points to a heap object 
#          then it is set to the location of copied object.
#          Else, unchanged value from %rdi.
#    %rsi: lower bound object should be within. (unchanged)
#    %rdx: upper bound object should be within. (unchanged)
#
#   GLOBALS MODIFIED:
#    alloc_ptr
#
#   Registers modified:
#    %rax, %rdi, %rcx
#

.GenGC.check_copy:
    POINTER_SIZE       = 8
    POINTER_OFFSET     = -POINTER_SIZE
    LOWER_BOUND_SIZE   = 8
    LOWER_BOUND_OFFSET = -(POINTER_SIZE + LOWER_BOUND_SIZE)
    UPPER_BOUND_SIZE   = 8
    UPPER_BOUND_OFFSET = -(POINTER_SIZE + LOWER_BOUND_SIZE + UPPER_BOUND_SIZE)
    FRAME_SIZE         =  (POINTER_SIZE + LOWER_BOUND_SIZE + UPPER_BOUND_SIZE)

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    movq     %rdi, %rax                                # if a check doesn't pass
                                                       # we promised %rax = %rdi
    movq     %rdi, POINTER_OFFSET(%rbp)
    movq     %rsi, LOWER_BOUND_OFFSET(%rbp)
    movq     %rdx, UPPER_BOUND_OFFSET(%rbp)

    # If the pointer is a muliple of 8, 
    # its least significant 3 bits are 000
    testq    $7, %rdi
    jnz      .GenGC.check_copy.check_fail              # if (%rdi % 8 != 0)
                                                       # go to .GenGC.check_copy.check_fail

    # Check if the pointer is within [%rsi, %rdx)
    cmpq     %rsi, %rdi
    jl       .GenGC.check_copy.check_fail              # if (%rdi < %rsi) 
                                                       # go to .GenGC.check_copy.check_fail
    cmpq     %rdx, %rdi
    jge      .GenGC.check_copy.check_fail              # if (%rdi >= %rdx)
                                                       # go to .GenGC.check_copy.check_fail

    # Check the eye catcher is present
    cmpq     $EYE_CATCH, OBJ_EYE_CATCH(%rdi)
    jne      .GC.abort                                 # if no eye catcher,
                                                       # go to .GC.abort
    # Check the object's tag != EYE_CATCH
    cmpq     $EYE_CATCH, OBJ_TAG(%rdi)
    je       .GenGC.check_copy.check_fail              # if (tag == $EYE_CATCH)
                                                       # go to .GenGC.check_copy.check_fail

    movq     OBJ_SIZE(%rdi), %rsi                      # %rsi = sizeof(obj) in quads
    testq    %rsi, %rsi
    jz       .GenGC.check_copy.copy_done               # if (sizeof(obj) == 0)
                                                       # the source obj has already been copied
.GenGC.check_copy.copy:
    # The checks have passed, 
    # we're going to copy the object now.
    addq     $8, alloc_ptr(%rip)                       # reserve a quad for the eye catcher
    movq     alloc_ptr(%rip), %rcx                     # %rcx = the start of copy obj
    movq     %rcx, %rdx                                # %rdx = the start of copy obj
    movq     $EYE_CATCH, OBJ_EYE_CATCH(%rdx)           # place the eye catcher before the copy obj
    salq     $3, %rsi                                  # %rsi = sizeof(obj) in quads * 8 = sizeof(obj) in bytes
    addq     %rdi, %rsi                                # %rsi = the start of source obj + sizeof(obj) in bytes
                                                       #      = the end of source obj
    # %rdi: the start of source object
    # %rsi: the end of source obj
    # %rcx: the start of destination (copy) object
    # %rdx: the start of destination (copy) object

.GenGC.check_copy.copy_loop:
    movq     0(%rdi), %rax
    movq     %rax, 0(%rdx)
    addq     $8, %rdi
    addq     $8, %rdx
    cmpq     %rsi, %rdi
    jl       .GenGC.check_copy.copy_loop               # if (%rdi < %rsi) 
                                                       # go to .GenGC.check_copy.copy_loop
    # %rcx: the start of destination (copy) object
    # %rdx: the end of destination (copy) object

    movq     %rdx, alloc_ptr(%rip)                     # alloc_ptr = the end of dest (copy) obj

    # Mark the source object as copied
    movq     POINTER_OFFSET(%rbp), %rdi                # %rdi = the start of source obj
    movq     $0, OBJ_SIZE(%rdi)                        # put 0 into the source obj's size
    movq     %rcx, OBJ_VTAB(%rdi)                      # put a forwarding pointer to the copy
                                                       # into the source obj's vtab slot
.GenGC.check_copy.copy_done:
    movq     POINTER_OFFSET(%rbp), %rdi
    movq     OBJ_VTAB(%rdi), %rax                      # %rax = a pointer to the obj copy
    movq     LOWER_BOUND_OFFSET(%rbp), %rsi            # %rsi = the original value
    movq     UPPER_BOUND_OFFSET(%rbp), %rdx            # %rdx = the original value

.GenGC.check_copy.check_fail:
    movq     %rbp, %rsp
    popq     %rbp
    ret

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
#        accordingly.  Use ".GenGC.check_copy" to validate the pointer and
#        get the new pointer, and then update the stack entry.
#
#     3) Check the registers specified in the .GenGC.ROOT_REG_MASK mask 
#        for GC roots and to automatically update. 
#        This mask is stored in the header.  If bit #n in the mask is set, 
#        register #n will be passed to ".GenGC.check_copy" and updated with 
#        its result.
#
#     4) The assignemnt table is now checked.  $s7 is moved from its
#        current position until it hits the L3 pointer.  Each entry is a
#        pointer to the pointer that must be checked.  Again,
#        ".GenGC.check_copy" is used and the pointer updated.
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
#    %rsi: the tip of stack to start checking for roots from
#
#    $a0: end of stack
#    $s7: limit pointer of this area of storage
#    $gp: current allocation pointer
#    heap_start: start of heap
#
#   OUTPUT:
#    %rax: the size of all collected live objects
#
#    $a0: size of all live objects collected
#
#   GLOBALS MODIFIED:
#    L2: the end of the live objects in Reserve area
#
#   Registers modified:
#
#    $t0, $t1, $t2, $t3, $t4, $v0, $v1, $a0, $a1, $a2, $gp, $s7
#

.GenGC.minor_collect:
    ret

/*
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
    jal        .GenGC.check_copy                     # check and copy
    lw         $t0 12($sp)                        # load stack index
    sw         $a0 4($t0)                         # replace stack item pointing to old object with pointer to new one
    lw         $t1 16($sp)                        # restore stack end
    bgt        $t0 $t1 .GenGC_MinorC_stackloop    # loop
.GenGC_MinorC_stackend:
    li         $t0 .GenGC.ROOT_REG_MASK           # get Register mask
    sw         $t0 16($sp)                        # save Register mask
.GenGC_MinorC_reg16:
    srl        $t0 $t0 16                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg17         # check if set
    move       $a0 $16                            # set test pointer to potentially old object address
    jal        .GenGC.check_copy                     # check and copy
    move       $16 $a0                            # update register with potentilly new object address
.GenGC_MinorC_reg17:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 17                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg18         # check if set
    move       $a0 $17                            # set test pointer
    jal        .GenGC.check_copy                     # check and copy
    move       $17 $a0                            # update register
.GenGC_MinorC_reg18:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 18                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg19         # check if set
    move       $a0 $18                            # set test pointer
    jal        .GenGC.check_copy                     # check and copy
    move       $18 $a0                            # update register
.GenGC_MinorC_reg19:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 19                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg20         # check if set
    move       $a0 $19                            # set test pointer
    jal        .GenGC.check_copy                     # check and copy
    move       $19 $a0                            # update register
.GenGC_MinorC_reg20:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 20                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg21         # check if set
    move       $a0 $20                            # set test pointer
    jal        .GenGC.check_copy                     # check and copy
    move       $20 $a0                            # update register
.GenGC_MinorC_reg21:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 21                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg22         # check if set
    move       $a0 $21                            # set test pointer
    jal        .GenGC.check_copy                     # check and copy
    move       $21 $a0                            # update register
.GenGC_MinorC_reg22:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 22                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg24         # check if set
    move       $a0 $22                            # set test pointer
    jal        .GenGC.check_copy                     # check and copy
    move       $22 $a0                            # update register
.GenGC_MinorC_reg24:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 24                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg25         # check if set
    move       $a0 $24                            # set test pointer
    jal        .GenGC.check_copy                     # check and copy
    move       $24 $a0                            # update register
.GenGC_MinorC_reg25:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 25                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg30         # check if set
    move       $a0 $25                            # set test pointer
    jal        .GenGC.check_copy                     # check and copy
    move       $25 $a0                            # update register
.GenGC_MinorC_reg30:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 30                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_reg31         # check if set
    move       $a0 $30                            # set test pointer
    jal        .GenGC.check_copy                     # check and copy
    move       $30 $a0                            # update register
.GenGC_MinorC_reg31:
    lw         $t0 16($sp)                        # restore mask
    srl        $t0 $t0 31                         # shift to proper bit
    andi       $t1 $t0 1
    beq        $t1 $0 .GenGC_MinorC_regend        # check if set
    move       $a0 $31                            # set test pointer
    jal        .GenGC.check_copy                     # check and copy
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
    jal        .GenGC.check_copy                     # check and copy
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
    jal        .GenGC.check_copy                     # check and copy
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
    jal        .GenGC.check_copy                     # check and copy
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
*/

#
# Check and Copy an Object with an Offset
#
#   Checks that the input pointer points to a heap object:
#     1) The pointer is a multiple of 8 (a quad is our chosen minimal granularity)
#     2) The pointer is within the specified limits
#     3) The word before the pointer is the eye catcher 0xFFFF_FFFF
#     4) The word at the pointer is a valid tag (i.e. not equal to
#        0xFFFF_FFFF)
#
#   If so, the pointer is checked to be in one of two areas.
#
#   If the pointer is in the X area, L0-L1 is added to the pointer, 
#   and the new pointer is returned.
#
#   If the pointer points to an object within the Old Area, 
#   it then checks the object for a forwarding pointer by checking the 
#   object's size for 0.
#
#   If found, the forwarding pointer is returned.
#   Else, the heap is analyzed to make sure the object can be copied.
#
#   The heap is expanded if necessary (updating only `assign_sp`),
#   and the object gets copied to `alloc_ptr`.
#
#   After copying is complete, the procedure places 
#       - 0 into the source object's OBJ_SIZE slot
#       - (the address of copy object + (L0 - L1)) 
#         into the source object's OBJ_VTAB slot
#
#   Finally, the return value (the address of copy object + (L0 - L1)) is placed in %rax
#   Note that %rax does not actually point to the copy object initially.
#   The entire area will later be block copied. Once that is complete, the address in %rax 
#   will become valid.
#
#   INPUT:
#    %rdi: pointer to check and copy with an offset
#
#   OUTPUT:
#    %rax: if %rdi points to a heap object 
#          then it is set to the location of copied object.
#          Else, unchanged value from %rdi.
#
#   GLOBALS MODIFIED:
#    alloc_ptr, assign_sp
#
#   Registers modified:
#    %rax, %rdi, %rsi, %rdx, %rcx
#

.GenGC.offset_copy:
    POINTER_SIZE       = 8
    POINTER_OFFSET     = -POINTER_SIZE
    FRAME_SIZE         =  POINTER_SIZE

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    movq     %rdi, %rax                                # if a check doesn't pass
                                                       # we promised %rax = %rdi
    movq     %rdi, POINTER_OFFSET(%rbp)

    # If the pointer is a muliple of 8, 
    # its least significant 3 bits are 000
    testq    $7, %rdi
    jnz      .GenGC.offset_copy.done                   # if (%rdi % 8 != 0)
                                                       # go to .GenGC.offset_copy.done

    movq     .Platform.heap_start(%rip), %rsi          # %rsi = heap_start

    # Check if the pointer is within [L0, L2)
    cmpq     .GenGC.HDR_L0(%rsi), %rdi
    jl       .GenGC.offset_copy.done                   # if (%rdi < L0) 
                                                       # go to .GenGC.offset_copy.done
    cmpq     .GenGC.HDR_L2(%rsi), %rdi
    jge      .GenGC.offset_copy.done                   # if (%rdi >= L2)
                                                       # go to .GenGC.offset_copy.done

    # Check the eye catcher is present
    cmpq     $EYE_CATCH, OBJ_EYE_CATCH(%rdi)
    jne      .GC.abort                                 # if no eye catcher,
                                                       # go to .GC.abort
    # Check the object's tag != $EYE_CATCH
    cmpq     $EYE_CATCH, OBJ_TAG(%rdi)
    je       .GenGC.offset_copy.done                   # if (tag == $EYE_CATCH)
                                                       # go to .GenGC.offset_copy.done
    cmpq     .GenGC.HDR_L1(%rsi), %rdi
    jl       .GenGC.offset_copy.ensure_copied          # if (L0 <= %rdi < L1)
                                                       # go to .GenGC.offset_copy.ensure_copied
    # %rdi points to an object in the X area.
    # Add (L0 - L1) to the pointer and return it.
    movq     .GenGC.HDR_L0(%rsi), %rax                 # %rax = L0
    subq     .GenGC.HDR_L1(%rsi), %rax                 # %rax = L0 - L1
    addq     %rdi, %rax                                # %rax = %rdi + (L0 - L1)
    jmp      .GenGC.offset_copy.done

.GenGC.offset_copy.ensure_copied:
    # %rdi points to an object in Old Area, make sure we either 
    # already copied it to the X area or copy now.
    movq     OBJ_SIZE(%rdi), %rcx                      # %rcx = sizeof(obj) in quads
    testq    %rcx, %rcx
    jz       .GenGC.offset_copy.copy_done              # if (sizeof(obj) == 0)
                                                       # the source obj has already been copied

    salq     $3, %rcx                                  # %rcx = sizeof(obj) in bytes
    addq     alloc_ptr(%rip), %rcx                     # %rcx = alloc_ptr + sizeof(obj)
    addq     $8, %rcx                                  # %rcx = alloc_ptr + sizeof(obj) + sizeof(eye catcher)
    subq     assign_sp(%rip), %rcx                     # %rcx = %rcx - assign_sp
    jl       .GenGC.offset_copy.copy                   # if (%rcx < 0)
                                                       # go to .GenGC.offset_copy.copy
    # There is not engough heap space to copy the source obj.
    # %rcx contains the amount of required additional memory in bytes.
    # We add 8 bytes to %rcx, to maintain `alloc_ptr` < `assign_sp` after the allocation.
    # So, there is room to record an assignment before a garbage collection has to run (if any).
    # See the comments at `.GenGC.handle_assignment` for details.
    addq $8, %rcx

    # Now, as usual, round up %rcx to the nearest greater multiple of 
    # .GenGC.HEAP_EXP_SIZE (e.g., 32768 bytes):
    # %rcx = (%rcx + 32767) & (-32768)
    movq     $.GenGC.HEAP_EXP_SIZE, %rax               # %rax = 32768  = 00000000_00000000_10000000_00000000
    decq     %rax                                      # %rax = 32767  = 00000000_00000000_01111111_11111111
    addq     %rax, %rcx                                # %rcx = %rcx + 32767
    notq     %rax                                      # %rax = -32768 = 11111111_11111111_10000000_00000000
    andq     %rax, %rcx                                # %rcx = %rcx & (-32768)
    movq     $.GenGC.HEAP_EXP_SIZE, %rax               # %rax = 32768  = 00000000_00000000_10000000_00000000
    decq     %rax                                      # %rax = 32767  = 00000000_00000000_01111111_11111111
    addq     %rax, %rcx                                # %rcx = %rcx + 32767
    notq     %rax                                      # %rax = -32768 = 11111111_11111111_10000000_00000000
    andq     %rax, %rcx                                # %rcx = %rcx & (-32768)

    movq     %rcx, %rdi                                # %rdi = %rcx = total expansion size
    call     .Platform.alloc                           # %rax: allocated memory block's start

    movq     .Platform.heap_end(%rip), %rax            # %rax      = heap_end after the allocation
    movq     %rax, assign_sp(%rip)                     # assign_sp = heap_end

    movq     POINTER_OFFSET(%rbp), %rdi                # %rdi = the start of source obj

.GenGC.offset_copy.copy:
    # The checks have passed, 
    # we're going to copy the object now.
    addq     $8, alloc_ptr(%rip)                       # reserve a quad for the eye catcher
    movq     alloc_ptr(%rip), %rcx                     # %rcx = the start of copy obj
    movq     %rcx, %rdx                                # %rdx = the start of copy obj
    movq     $EYE_CATCH, OBJ_EYE_CATCH(%rdx)           # place the eye catcher before the copy obj
    movq     OBJ_SIZE(%rdi), %rsi                      # %rsi = sizeof(obj) in quads
    salq     $3, %rsi                                  # %rsi = sizeof(obj) in quads * 8 = sizeof(obj) in bytes
    addq     %rdi, %rsi                                # %rsi = the start of source obj + sizeof(obj) in bytes
                                                       #      = the end of source obj
    # %rdi: the start of source object
    # %rsi: the end of source obj
    # %rcx: the start of destination (copy) object
    # %rdx: the start of destination (copy) object

.GenGC.offset_copy.copy_loop:
    movq     0(%rdi), %rax
    movq     %rax, 0(%rdx)
    addq     $8, %rdi
    addq     $8, %rdx
    cmpq     %rsi, %rdi
    jl       .GenGC.offset_copy.copy_loop              # if (%rdi < %rsi) 
                                                       # go to .GenGC.offset_copy.copy_loop
    # %rcx: the start of destination (copy) object
    # %rdx: the end of destination (copy) object

    movq     %rdx, alloc_ptr(%rip)                     # alloc_ptr = the end of dest (copy) obj

    # Add (L0 - L1) to the pointer and return it.
    movq     .Platform.heap_start(%rip), %rsi          # %rsi = heap_start
    movq     .GenGC.HDR_L0(%rsi), %rax                 # %rax = L0
    subq     .GenGC.HDR_L1(%rsi), %rax                 # %rax = L0 - L1
    addq     %rax, %rcx                                # %rcx = %rcx + (L0 - L1)
                                                       #      = the start of dest (copy) object + (L0 - L1)
    # Mark the source object as copied
    movq     POINTER_OFFSET(%rbp), %rdi                # %rdi = the start of source obj
    movq     $0, OBJ_SIZE(%rdi)                        # put 0 into the source obj's size
    movq     %rcx, OBJ_VTAB(%rdi)                      # put a forwarding pointer to the copy + (L0 - L1) 
                                                       # into the source obj's VTAB slot
.GenGC.offset_copy.copy_done:
    movq     POINTER_OFFSET(%rbp), %rdi
    movq     OBJ_VTAB(%rdi), %rax                      # %rax = a pointer to the obj copy

.GenGC.offset_copy.done:
    movq     %rbp, %rsp
    popq     %rbp
    ret

#
# Major Garbage Collection
#
#   This collection occurs when ever the old area grows beyond a specified
#   point.  `.GenGC.minor_collect` sets up the Old, X, and New areas for
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
#    %rsi: the tip of stack to start checking for roots from
#
#    $a0: end of stack
#    heap_start: start of heap
#
#   OUTPUT:
#    %rax: the size of all collected live objects
#
#    $a0: size of all live objects collected
#
#   GLOBALS MODIFIED:
#    L1: the new Old Area's end
#
#   Registers modified:
#    $t0, $t1, $t2, $v0, $v1, $a0, $a1, $a2, $gp, $s7
#

.GenGC.major_collect:
    ret

/*
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
*/
