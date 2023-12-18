########################################
# Data
########################################

    .data

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
.GenGC.MSG_MAJOR_ERROR_ASCII:      .ascii "GenGC: Fatal error during major garbage collection"
.GenGC.MSG_MAJOR_ERROR_LEN  =             (. - .GenGC.MSG_MAJOR_ERROR_ASCII)

.GenGC.MSG_MINOR_ASCII:            .ascii "GenGC: Minor ..."
.GenGC.MSG_MINOR_LEN  =                   (. - .GenGC.MSG_MINOR_ASCII)
.GenGC.MSG_MINOR_ERROR_ASCII:      .ascii "GenGC: Fatal error during minor garbage collection"
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
#   allocation pointer (`.Alloc.ptr`) in the work area. If they cross, a minor
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
#        do not fit within the new area, the heap is expanded and `.Alloc.limit`
#        is updated to reflect this. This value later gets stored back
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
#      heap_start                            .Alloc.ptr    .Alloc.limit
#
#     `.Alloc.ptr`: points to the next free word in the work
#         area during normal allocation.  During a minor garbage collection,
#         it points to the next free work in the reserve area.
#
#     `.Alloc.limit`: points to the tip of the assignment stack, 
#         `.Alloc.ptr` cannot go past `.Alloc.limit`.
#         Between `.Alloc.limit` and L3 sits the assignment stack 
#         which grows towards `.Alloc.ptr`.
#
#   The following invariant is maintained for `.Alloc.ptr` and `assign_ptr` by 
#   the garbage collector's code at all times:
#
#      `.Alloc.ptr` is always strictly less than `.Alloc.limit`.
#      Hence there is always enough room for at least one assignment record 
#      at the tip of assignment stack.
#
#      If the above invariant hadn't been maintained, we would've ended up
#      in a situation where at the moment we're requested to record an 
#      assignment, `.Alloc.ptr` == `.Alloc.limit`. As there is no room to 
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
#     |    L0                 L1     |   L2    .Alloc.ptr      .Alloc.limit, L4
#     |                              |
#     heap_start                     breakpoint
#
#     `.Alloc.ptr` (allocation pointer): During a major collection, this points
#         into the next free word in the new area.
#
#     `.Alloc.limit`: During a major collection, this points to the tip of an 
#         empty assignment stack wich is the same as the limit of heap memory.
#         `.Alloc.ptr` is not allowed to pass this value. If the live objects 
#         in the old area cannot fit in the new area, more memory is allocated 
#         and `.Alloc.limit` is adjusted accordingly.
#
#   See the `.Alloc.ptr` < `.Alloc.limit` invariant descriptions above.
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
# Granularity of heap expansion
#
#   The heap is always expanded in multiples of 2^k, where
#   k is the granularity.
#

.GenGC.HEAP_PAGE     = 32768 # in bytes

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
    STKBASE_SIZE    = 8
    STKBASE         = -STKBASE_SIZE
    FRAME_SIZE      = STKBASE_SIZE

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    movq     %rdi, STKBASE(%rbp)

    movq     $.GenGC.HEAP_PAGE, %rdi                   # allocate initial heap space
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
    jz       .GenGC.init.abort                         # heap initially too small

    movq     .Platform.heap_end(%rip), %rax
    movq     %rax, .GenGC.HDR_L3(%rdi)                 # initially the end of work area is at the heap end
    movq     %rax, .Alloc.limit(%rdi)                  # initially the tip of assign stack is at the end of work area
    movq     %rax, .GenGC.HDR_L4(%rdi)                 # initially the end of unused area is at the heap end

    subq     %rsi, %rax                                # %rsi contains the work area size
                                                       # L3 - %rsi = reserve area end/work area start
    movq     %rax, .GenGC.HDR_L2(%rdi)                 # store the calculated start of work area
    movq     %rax, .Alloc.ptr(%rip)                    # initially the allocation pointer is at the start of work area

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

.GenGC.init.abort:
    movq     $.GenGC.MSG_INIT_ERROR_ASCII, %rdi
    movq     $.GenGC.MSG_INIT_ERROR_LEN, %rsi
    call     .Platform.out_string
    call     .Runtime.out_nl

    movq   $1, %rdi
    jmp    .Platform.exit_process

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
#   Registers modified:
#    %rax, %rdi, %rsi, .GenGC.collect
#

    .global .GenGC.handle_assign
.GenGC.handle_assign:
    POINTER_SIZE = 8

    pushq    %rbp
    movq     %rsp, %rbp

    # TODO: Preserve %rdi?

    movq     .Alloc.limit(%rip), %rax
    subq     $POINTER_SIZE, %rax
    movq     %rax, .Alloc.limit(%rip)      # make room in the assignment stack
    movq     %rdi, 0(%rax)                 # place pointer to the pointer being assigned to
                                           # at the tip of assignment stack
    cmpq     .Alloc.ptr(%rdi), %rax        # if `.Alloc.ptr` and `.Alloc.limit` have met
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
#   The `.Alloc.limit` and `.Alloc.ptr` pointers are then set 
#   as well as the L2 pointer.
#
#   If a major collection is not done, the X area is incorporated 
#   into the old area (i.e. the value of L2 is moved into L1) and 
#   `.Alloc.limit`, `.Alloc.ptr`, and L2 are then set.
#
#   INPUT:
#    %rdi: requested allocation size in bytes
#    %rsi: the tip of stack to start checking for roots from
#
#   OUTPUT:
#    %rdi: requested allocation size in bytes (unchanged)
#
#   GLOBALS MODIFIED:
#    L1, L2, L3, L4, .Alloc.ptr, .Alloc.limit,
#    MINOR0, MINOR1, MAJOR0, MAJOR1
#
#   Registers modified:
#    %rax, %rdi, %rsi, %rcx, %rdx, .GenGC.minor_collect, .GenGC.major_collect
#

    .global .GenGC.collect
.GenGC.collect:
    ALLOC_SIZE_SIZE    = 8
    ALLOC_SIZE         = -ALLOC_SIZE_SIZE
    STACK_TIP_SIZE     = 8
    STACK_TIP          = -(STACK_TIP_SIZE + ALLOC_SIZE_SIZE)
    FRAME_SIZE         = STACK_TIP_SIZE + ALLOC_SIZE_SIZE

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    movq     %rdi, ALLOC_SIZE(%rbp)
    movq     %rsi, STACK_TIP(%rbp)

    # movq     $.GenGC.MSG_COLLECTING_ASCII, %rdi
    # movq     $.GenGC.MSG_COLLECTING_LEN, %rsi
    # call     .Platform.out_string
    # call     .Runtime.out_nl

    movq     STACK_TIP(%rbp), %rdi
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
    # If no, update L1, set up the new Reserve/Work areas,
    # reset `.Alloc.ptr`, and `.Alloc.limit`, etc
    movq     .GenGC.HDR_L2(%rdi), %rcx           # %rcx = L2
    movq     .GenGC.HDR_L3(%rdi), %rdx           # %rdx = L3
    movq     %rdx, %rsi                          # %rsi = L3
    # Calculate Reserve/Work areas boundary
    subq     %rcx, %rdx                          # %rdx = L3 - L2
    sarq     $1, %rdx                            # %rdx = (L3 - L2) / 2
    andq     $(-8), %rdx                         # %rdx = the nearest smaller multiple of 8
                                                 # so Reserve Area >= Work Area by 8 bytes
    subq     %rdx, %rsi                          # %rsi = the new L2
                                                 # (L3 - round_down((L3 - L2) / 2))
    movq     %rsi, %rcx                          # %rcx = the new L2

    # Enough space to allocate the requested size?
    movq     ALLOC_SIZE(%rbp), %rax              # %rax = the requested allocation size
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
    movq     %rcx, .Alloc.ptr(%rip)              # set `.Alloc.ptr` at the start of Work Area
    movq     %rdx, .Alloc.limit(%rip)            # set `.Alloc.limit` to L3 (Work Area's end)
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

    movq     STACK_TIP(%rbp), %rdi
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
    movq     .GenGC.HDR_L1(%rdi), %rcx           # %rcx = L1
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
    addq     ALLOC_SIZE(%rbp), %rdx              # %rdx =  Work Area start + requested alloc size
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
    #   2) Old Area/Heap size ratio is preserved
    #   3) We have enough space in Work Area to
    #      accomodate the requested allocation size
    cmpq     $0, %rcx
    jle      .GenGC.collect.set_.Alloc.limit_and_L2  # if (%rcx <= 0) 
                                                     #     go to .GenGC.collect.set_.Alloc.limit_and_L2
    # %rcx: we need to expand the heap by at least this number of bytes.
    # Round up %rcx to the nearest greater multiple of .GenGC.HEAP_PAGE (e.g., 32768 bytes):
    # %rcx = (%rcx + 32767) & (-32768)
    movq    $.GenGC.HEAP_PAGE, %rax              # %rax = 32768  = 00000000_00000000_10000000_00000000
    decq    %rax                                 # %rax = 32767  = 00000000_00000000_01111111_11111111
    addq    %rax, %rcx                           # %rcx = %rcx + 32767
    notq    %rax                                 # %rax = -32768 = 11111111_11111111_10000000_00000000
    andq    %rax, %rcx                           # %rcx = %rcx & (-32768)

    # %rcx: the total  number of bytes to expand the heap by.
    #       (a multiple of .GenGC.HEAP_PAGE).
    # See how much of the total expansion is covered by Unused.
    movq     .GenGC.HDR_L4(%rdi), %rax           # %rax = L4
    subq     .GenGC.HDR_L3(%rdi), %rax           # %rax = L4 - L3 = sizeof(Unused)
    movq     %rcx, %rdx                          # %rdx = %rcx
                                                 #      = total expansion size
    subq     %rax, %rdx                          # %rdx = total expansion size - sizeof(Unused)
    jg       .GenGC.collect.platform_allocate    # if ((total expansion size) > sizeof(Unused)) 
                                                 #     go to .GenGC.collect.platform_allocate
    # We have enough Unused space to cover the required expansion
    # without allocating any additional memory from the OS.
    movq     .GenGC.HDR_L3(%rdi), %rax           # %rax      = L3
    addq     %rcx, %rax                          # %rax      = L3 + total expansion size
    movq     %rax, .GenGC.HDR_L3(%rdi)           # L3        = L3 + total expansion size
    movq     %rax, .Alloc.limit(%rip)            # .Alloc.limit = L3 + total expansion size
                                                 # (therefore, the assign stack size = 0)
    jmp      .GenGC.collect.set_L2
.GenGC.collect.platform_allocate:
    # %rdx = total expansion size - sizeof(Unused)
    movq     %rdx, %rdi                          # %rdi      = requested alloc size
    call     .Platform.alloc                     # %rax      = allocated memory block's start

    movq     .Platform.heap_start(%rip), %rdi    # %rdi      = heap_start
    movq     .Platform.heap_end(%rip), %rax      # %rax      = heap_end after the allocation
    movq     %rax, .GenGC.HDR_L3(%rdi)           # L3        = heap_end after the allocation
    movq     %rax, .Alloc.limit(%rip)            # .Alloc.limit = heap_end after the allocation
                                                 # (therefore, the assign stack size = 0)
    movq     %rax, .GenGC.HDR_L4(%rdi)           # L4        = heap_end after the allocation
                                                 # (therefore, sizeof(Unused) = 0 at this point)
    jmp      .GenGC.collect.set_L2
.GenGC.collect.set_.Alloc.limit_and_L2:
    movq     .GenGC.HDR_L3(%rdi), %rax           # %rax = L3
    movq     %rax, .Alloc.limit(%rip)            # .Alloc.limit = L3
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
    movq     %rsi, .Alloc.ptr(%rip)              # .Alloc.ptr = %rsi

.GenGC.collect.done:
    # Zero out the new generation (the new Work Area) to help catch missing pointers
    movq     .Alloc.ptr(%rip), %rax
.GenGC.collect.work_area_clear_loop:
    movq     $0, 0(%rax)                         # zero out the quad at %rax
    addq     $8, %rax
    cmpq     .Alloc.limit(%rip), %rax            # %rax < `.Alloc.limit`
    jl       .GenGC.collect.work_area_clear_loop # if yes, we haven't reached 
                                                 # the end of Work Area yet

    movq    ALLOC_SIZE(%rbp), %rdi               # restore requested allocation size in bytes
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
#   Else, the object is copied to `.Alloc.ptr` and a pointer to 
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
#    %rsi: lower bound object should be within
#    %rdx: upper bound object should be within
#
#   OUTPUT:
#    %rax: if %rdi points to a heap object 
#          then it is set to the location of copied object.
#          Else, unchanged value from %rdi.
#    %rsi: lower bound object should be within (unchanged)
#    %rdx: upper bound object should be within (unchanged)
#
#   GLOBALS MODIFIED:
#    .Alloc.ptr
#
#   Registers modified:
#    %rax, %rdi, %rcx
#

.GenGC.check_copy:
    POINTER_SIZE       = 8
    POINTER            = -POINTER_SIZE
    LOWER_BOUND_SIZE   = 8
    LOWER_BOUND        = -(POINTER_SIZE + LOWER_BOUND_SIZE)
    UPPER_BOUND_SIZE   = 8
    UPPER_BOUND        = -(POINTER_SIZE + LOWER_BOUND_SIZE + UPPER_BOUND_SIZE)
    FRAME_SIZE         =  (POINTER_SIZE + LOWER_BOUND_SIZE + UPPER_BOUND_SIZE)

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    movq     %rdi, %rax                         # if a check doesn't pass
                                                # we promised %rax = %rdi
    movq     %rdi, POINTER(%rbp)
    movq     %rsi, LOWER_BOUND(%rbp)
    movq     %rdx, UPPER_BOUND(%rbp)

    # If the pointer is a muliple of 8, 
    # its least significant 3 bits are 000
    testq    $7, %rdi
    jnz      .GenGC.check_copy.done             # if (%rdi % 8 != 0)
                                                # go to .GenGC.check_copy.done

    # Check if the pointer is within [%rsi, %rdx)
    cmpq     %rsi, %rdi
    jl       .GenGC.check_copy.done             # if (%rdi < %rsi) 
                                                # go to .GenGC.check_copy.done
    cmpq     %rdx, %rdi
    jge      .GenGC.check_copy.done             # if (%rdi >= %rdx)
                                                # go to .GenGC.check_copy.done

    # Check the eye catcher is present
    cmpq     $EYE_CATCH, OBJ_EYE_CATCH(%rdi)
    jne      .GC.abort                          # if no eye catcher,
                                                # go to .GC.abort
    # Check the object's tag != EYE_CATCH
    cmpq     $EYE_CATCH, OBJ_TAG(%rdi)
    je       .GenGC.check_copy.done             # if (tag == $EYE_CATCH)
                                                # go to .GenGC.check_copy.done

    movq     OBJ_SIZE(%rdi), %rsi               # %rsi = sizeof(obj) in quads
    testq    %rsi, %rsi
    jz       .GenGC.check_copy.copy_done        # if (sizeof(obj) == 0)
                                                # the source obj has already been copied
.GenGC.check_copy.copy:
    # The checks have passed, 
    # we're going to copy the object now.
    addq     $8, .Alloc.ptr(%rip)               # reserve a quad for the eye catcher
    movq     .Alloc.ptr(%rip), %rcx             # %rcx = the start of copy obj
    movq     %rcx, %rdx                         # %rdx = the start of copy obj
    movq     $EYE_CATCH, OBJ_EYE_CATCH(%rdx)    # place the eye catcher before the copy obj
    salq     $3, %rsi                           # %rsi = sizeof(obj) in quads * 8 = sizeof(obj) in bytes
    addq     %rdi, %rsi                         # %rsi = the start of source obj + sizeof(obj) in bytes
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
    jl       .GenGC.check_copy.copy_loop        # if (%rdi < %rsi) 
                                                # go to .GenGC.check_copy.copy_loop
    # %rcx: the start of destination (copy) object
    # %rdx: the end of destination (copy) object

    movq     %rdx, .Alloc.ptr(%rip)             # .Alloc.ptr = the end of dest (copy) obj

    # Mark the source object as copied
    movq     POINTER(%rbp), %rdi                # %rdi = the start of source obj
    movq     $0, OBJ_SIZE(%rdi)                 # put 0 into the source obj's size
    movq     %rcx, OBJ_VTAB(%rdi)               # put a forwarding pointer to the copy
                                                # into the source obj's vtab slot
.GenGC.check_copy.copy_done:
    movq     POINTER(%rbp), %rdi
    movq     OBJ_VTAB(%rdi), %rax               # %rax = a pointer to the obj copy
    movq     LOWER_BOUND(%rbp), %rsi            # %rsi = the original value
    movq     UPPER_BOUND(%rbp), %rdx            # %rdx = the original value

.GenGC.check_copy.done:
    movq     %rbp, %rsp
    popq     %rbp
    ret

#
# Minor Garbage Collection
#
#   A minor garbage collector is run whenever the space in Work Area 
#   is used up by objects and the assignment table. The live
#   objects are found and copied to Reserve Area. The L2 pointer
#   is then set right past the last live object. The collector consists
#   of six phases:
#
#     1) Set `.Alloc.ptr` at the start of Reserve Area and 
#        set up the bounds for `.GenGC.check_copy`
#
#     2) Scan the stack for root pointers into the heap.  The beginning
#        of the stack is in the header and the end is an input to this
#        function.  Look for the appropriate stack flags and act
#        accordingly.  Use `.GenGC.check_copy` to validate the pointer and
#        get the new pointer, and then update the stack entry.
#
#     3) Inspect the %rbx, %r12, %r13, %r14, %r15 registers
#        for GC roots and to automatically update. 
#
#     4) The assignment stack is now inspected. `.Alloc.limit` is moved from its
#        current position until it hits the L3 pointer (the base of assignment stack).
#        Each entry is a pointer to object A's attribute itself pointing to object B.
#        If object A resides in Old Area and object B resides in Work Area,
#        Object B lives on.
#        Again, `.GenGC.check_copy` is used and object A's attribute is updated to
#        point to object B's copy in Reserve Area.
#
#     5) At this stage, all root objects are in Reserve Area.
#        This area is now traversed object by object (from L1 to `.Alloc.ptr`).
#        It results in a breadth-first search of the live objects 
#        collected in Reserve Area.
#        All attributes of objects are treated as pointers except the 
#        `Int`, `Bool`, and `String` objects. The first two are skipped
#        completely, and only the first attribute of the string object is
#        analyzed (should be a pointer to an `Int` object).
#
#     6) At this stage, L2 is set to the end of the live objects in
#        Reserve Area. This is in preparation for a major collection.
#        The size of all the live objects collected is then computed and
#        returned.
#
#   INPUT:
#    %rdi: the tip of stack to start checking for roots from
#
#   OUTPUT:
#    %rax: the size of all collected live objects
#
#   GLOBALS MODIFIED:
#    L2: the end of the live objects in Reserve area
#
#   Registers modified:
#    %rax, %rdi, %rsi, %rdx, %rcx, %r8, %r9, .GenGC.check_copy
#

.GenGC.minor_collect:
    STACK_TIP_SIZE     = 8
    STACK_TIP          = -STACK_TIP_SIZE
    RA_OBJ_SIZE_SIZE   = 8
    RA_OBJ_SIZE        = -(STACK_TIP_SIZE + RA_OBJ_SIZE_SIZE)
    FRAME_SIZE         = STACK_TIP_SIZE + RA_OBJ_SIZE_SIZE

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    movq     %rdi, STACK_TIP(%rbp)

    movq     .Platform.heap_start(%rip), %r8      # %r8 = heap_start

    # Set up the bounds for `.GenGC.check_copy`
    movq     .GenGC.HDR_L2(%r8), %rsi             # $rsi = [lower bound for .GenGC.check_copy
    movq     .Alloc.limit(%rip), %rdx             # %rdx = upper bound) for .GenGC.check_copy
    # Set up the destination for `.GenGC.check_copy`
    movq     .GenGC.HDR_L1(%r8), %rax             # %rax = L1 = the start of Reserve Area
    movq     %rax, .Alloc.ptr(%rip)               # .Alloc.ptr = the start of Reserve Area

    # Inspect the stack for GC roots within [%r9, %r10)
    movq    .GenGC.HDR_STK(%r8), %r10             # %r10 = stack base
    movq    %rdi, %r9                             # %r9 = %rdi = stack tip
    cmpq    %r10, %r9 
    jge     .GenGC.minor_collect.registers        # if (stack tip >= stack base) // stack grows down!
                                                  # go to .GenGC.minor_collect.registers
.GenGC.minor_collect.stack_loop:
    movq     0(%r9), %rdi                         # %rdi = stack entry at %r9
    call    .GenGC.check_copy
    movq    %rax, 0(%r9)                          # replace the stack entry pointing to old object 
                                                  # with the pointer to copy
    addq    $8, %r9                               # move up one stack entry closer to the base
    cmpq    %r10, %r9
    jl      .GenGC.minor_collect.stack_loop       # if (%rdi < stack base) // stack grows down!
                                                  # go to .GenGC.minor_collect.stack_loop
.GenGC.minor_collect.registers:
    # Inspect %rbx, %r12, %r13, %r14, %r15 for GC roots
    #
    # We follow the SYSV AMD64 ABI convention.
    #
    # The callee must preserve the value of %rbx, %r12, %r13, %r14, %r15.
    # So the calling code is free to store GC roots in these registers and
    # rely on them to be preserved across a procedure call. Hence we must
    # inspect these registers.
    #
    # On the other hand, the calling code is responsible for placing 
    # %rdi, %rsi, %rdx, %rcx, %r8, %r9 if it wants the registers preserved across 
    # a procedure call. Therefore, when we inspect the stack for GC roots it 
    # includes any GC roots from the caller-saved registers.

    # %rbx
    movq    %rbx, %rdi                            # %rdi = %rdx = a potential pointer to an object 
    call    .GenGC.check_copy                     # if $rdx indeed points to an object,
                                                  # we make a copy and the object lives on.
                                                  # %rax = the start of copy or %rdi's original value.
    movq    %rax, %rbx                            # %rbx = the start of copy or unchanges.
    # %r12
    movq    %r12, %rdi
    call    .GenGC.check_copy
    movq    %rax, %r12
    # %r13:
    movq    %r13, %rdi
    call    .GenGC.check_copy
    movq    %rax, %r13
    # %r14:
    movq    %r14, %rdi
    call    .GenGC.check_copy
    movq    %rax, %r14
    # %r15:
    movq    %r15, %rdi
    call    .GenGC.check_copy
    movq    %rax, %r15

    # Inspect the assignment stack for GC roots
    movq    .Alloc.limit(%rip), %r9               # %r9  = .Alloc.limit
    # See if the assignment stack is empty
    cmpq    .GenGC.HDR_L3(%r8), %r9
    jge     .GenGC.minor_collect.reserve_area     # if (.Alloc.limit >= L3)
                                                  # go to .GenGC.minor_collect.reserve_area
.GenGC.minor_collect.assign_loop:
    movq    0(%r9), %rdi                          # %rdi = current assignment stack entry
    # If %rdi points to an attribute of an Old-Area object,
    # the address will be within [L0, L1).
    cmpq    .GenGC.HDR_L0(%r8), %rdi
    jl      .GenGC.minor_collect.assign_continue  # if (%rdi < L0) continue
    cmpq    .GenGC.HDR_L1(%r8), %rdi
    jge     .GenGC.minor_collect.assign_continue  # if (%rdi >= L1) continue
    # Check the object pointed to by the attribute
    movq    0(%rdi), %rdi                         # %rdi = obj A's attr = a pointer to obj B
    call    .GenGC.check_copy
    
    movq    0(%r9), %rdi                          # %rdi = current assignment stack entry
    movq    %rax, 0(%rdi)                         # obj A's attr = a pointer to obj B's copy
    addq    $8, %r9                               # move up one entry closer to L3
.GenGC.minor_collect.assign_continue:
    cmpq    .GenGC.HDR_L3(%r8), %r9
    jl      .GenGC.minor_collect.assign_loop      # if (%r9 < L3) 
                                                  # go to .GenGC.minor_collect.assign_loop
    movq    %r9, .Alloc.limit(%rip)               # .Alloc.limit = L3
.GenGC.minor_collect.reserve_area:
    # The objects we've copied to Reserve Area so far
    # are GC roots. Now traverse them in breadth-first order
    # and append any objects they point to at the end of Reserve Area.
    # Keep traversing the appended objects. This process stops when we
    # can't find any more objects to append to Reserve Area.
    movq    .GenGC.HDR_L1(%r8), %r9               # %r9 = the first obj in Reserve Area's eye catcher
    # See if Reserve Area is empty
    # `.Alloc.ptr` points right past the last obj 
    # copied by `.GenGC.check_copy` into Reserve Area
    cmpq    .Alloc.ptr(%rip), %r9
    jge     .GenGC.minor_collect.done             # if (%r9 >= .Alloc.ptr) go to ...
.GenGC.minor_collect.ra_loop:
    # Check the current obj is prefixed by $EYE_CATCH
    addq    $8, %r9                               # %r9 = the first obj in Reserve Area
                                                  #       (skip the eye-catcher)
    cmpq    $EYE_CATCH, OBJ_EYE_CATCH(%r9)
    jnz     .GenGC.minor_collect.abort

    # Calculate the current obj's size in bytes
    movq    OBJ_SIZE(%r9), %r11                   # %r11 = sizeof(obj) in quads
    salq    $3, %r11                              # quads to bytes
    movq    %r11, RA_OBJ_SIZE(%rbp)
    
    # Switch on the obj's type 
    movq    OBJ_TAG(%r9), %rax                    # %rax = the obj's tag
    cmpq    $INT_TAG, %rax
    je      .GenGC.minor_collect.ra_int
    cmpq    $BOOL_TAG, %rax
    je      .GenGC.minor_collect.ra_bool
    cmpq    $STRING_TAG, %rax
    je      .GenGC.minor_collect.ra_string
    cmpq    $ARRAYANY_TAG, %rax
    je      .GenGC.minor_collect.ra_array

    # Handle an object of any other type
    # As we handle objects of special types individually,
    # we know every attribute of this object points to another object.
    # Just check and copy them one by one, updating each attribute with the copy's address.
    leaq    OBJ_ATTR(%r9), %r10                   # %r10 = a ptr to the obj's first attribute
    addq    %r9, %r11                             # %r11 = sizeof(obj) + the start of obj 
                                                  #      = the limit of attrs
    # See if the object has not attributes
    cmpq    %r11, %r10
    jge     .GenGC.minor_collect.ra_continue      # if (%r10 >= %r11) continue
.GenGC.minor_collect.ra_obj_loop:
    # Check and copy the object pointed to by the attribute
    movq    0(%r10), %rdi                         # %rdi = the current attr's value
                                                  #      = a pointer to object
    call    .GenGC.check_copy
    movq    %rax, 0(%r10)                         # update the attr's value with
                                                  # a pointer to the obj's copy
    addq    $8, %r10                              # %r10 = a ptr to the next attribute
    cmpq    %r11, %r10
    jl      .GenGC.minor_collect.ra_obj_loop      # if (%r10 < %r11) go to ...
    # Done checking and copying objects pointed to 
    # by the current object's attributes
    jmp     .GenGC.minor_collect.ra_continue      # next object

.GenGC.minor_collect.ra_array:
    # `ArrayAny` contains one attr pointing to another obj.
    # Namely, `ArrayAny.length` points to an `Int` obj.
    # Plus each array item points to an obj.
    movq    ARR_LEN(%r9), %rdi                    # %rdi = a pointer to an Int obj
    call    .GenGC.check_copy
    movq    %rax, ARR_LEN(%r9)                    # Update the attr with the copy address

    # Check and copy array items one by one, updating each item with the copy's address.
    leaq    ARR_ITEMS(%r9), %r10                  # %r10 = a ptr to the first array item
    addq    %r9, %r11                             # %r11 = sizeof(array obj) + the start of array obj
                                                  #      = the limit of array items
    # See if it's an empty array
    cmpq    %r11, %r10
    jge     .GenGC.minor_collect.ra_continue      # if (%r10 >= %r11) continue
.GenGC.minor_collect.ra_array_loop:
    # Check and copy the object pointed to by the attribute
    movq    0(%r10), %rdi                         # %rdi = the current item's value
                                                  #      = a pointer to object
    call    .GenGC.check_copy
    movq    %rax, 0(%r10)                         # update the item's value with
                                                  # a pointer to the obj's copy
    addq    $8, %r10                              # %r10 = a ptr to the next item
    cmpq    %r11, %r10
    jl      .GenGC.minor_collect.ra_array_loop    # if (%r10 < %r11) go to ...
    # Done checking and copying objects pointed to 
    # by the array items
    jmp     .GenGC.minor_collect.ra_continue      # next object
.GenGC.minor_collect.ra_string:
    # `String` contains one attr pointing to another obj.
    # Namely, `String.length` points to an `Int` obj.
    movq    STR_LEN(%r9), %rdi                    # %rdi = a pointer to an Int obj
    call    .GenGC.check_copy
    movq    %rax, STR_LEN(%r9)                    # Update the attr with the copy address
    # From here, we'll just fall through to `.GenGC.minor_collect.ra_continue`.
    # So, no need to jump.
.GenGC.minor_collect.ra_int:
.GenGC.minor_collect.ra_bool:
    # Int and Bool don't contain any pointers to other objects.
    # So, there's nothing to do for them.
.GenGC.minor_collect.ra_continue:
    # Move %r9 past the current obj's last attribute
    # and see if it has reached `.Alloc.ptr`. As during a minor collection
    # `.Alloc.ptr` points to the start of free space in Reserve Area,
    # reaching it means we've inspected all the objs in Reserve Area.
    # `.GenGC.check_copy` advances `.Alloc.ptr` whenever it copies an obj.
    addq    RA_OBJ_SIZE(%rbp), %r9                # %r9 = a pointer to the next Reserve Area obj's eye-catcher
    cmpq    .Alloc.ptr(%rip), %r9
    jl      .GenGC.minor_collect.ra_loop          # if (%r9 < .Alloc.ptr) go to ...

.GenGC.minor_collect.done:
    # The minor collection is complete. We are going to:
    # 1) Set L2 to the end of collected live objects 
    #    (`.GenGC.collect` will take care of actually extending Old Area over 
    #     the collected live objs residing in Reserve Area).
    # 2) %rax = the size of collected live objects
    movq     .Platform.heap_start(%rip), %rdi
    movq     .Alloc.ptr(%rip), %rax               # %rax = the end of collected live objects
    movq     %rax, .GenGC.HDR_L2(%rdi)            # L2 = the end of collected live objects
    subq     .GenGC.HDR_L1(%rdi), %rax            # %rax = L2 - L1 = sizeof(the collected live objects)

    movq     %rbp, %rsp
    popq     %rbp
    ret

.GenGC.minor_collect.abort:
    movq     $.GenGC.MSG_MINOR_ERROR_ASCII, %rdi
    movq     $.GenGC.MSG_MINOR_ERROR_LEN, %rsi
    call     .Platform.out_string
    call     .Runtime.out_nl

    movq   $1, %rdi
    jmp    .Platform.exit_process


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
#   If the pointer is in the X area, the future back copy offset (L1 - L0) 
#   is subtracted from the pointer and the new pointer is returned.
#
#   If the pointer points to an object within the Old Area, 
#   it then checks the object for a forwarding pointer by checking the 
#   object's size for 0.
#
#   If found, the forwarding pointer is returned.
#   Else, the heap is analyzed to make sure the object can be copied.
#
#   The heap is expanded if necessary (updating only `.Alloc.limit`),
#   and the object gets copied to `.Alloc.ptr`.
#
#   After copying is complete, the procedure places 
#       - 0 into the source object's OBJ_SIZE slot
#       - (the address of copy object - (L1 - L0)) 
#         into the source object's OBJ_VTAB slot
#
#   Finally, the return value (the address of copy object - (L1 - L0)) is placed in %rax
#   Note that %rax does not actually point to the copy object initially.
#   The entire area will later be block copied (L1 - L0) bytes back. Once that is complete, 
#   the address in %rax will become valid.
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
#    .Alloc.ptr, .Alloc.limit
#
#   Registers modified:
#    %rax, %rdi, %rsi, %rdx, %rcx, .Platform.alloc
#

.GenGC.offset_copy:
    POINTER_SIZE    = 8
    POINTER         = -POINTER_SIZE
    FRAME_SIZE      =  POINTER_SIZE

    pushq    %rbp
    movq     %rsp, %rbp
    subq     $FRAME_SIZE, %rsp

    movq     %rdi, %rax                                # if a check doesn't pass
                                                       # we promised %rax = %rdi
    movq     %rdi, POINTER(%rbp)

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
    # %rdi already points to an object in the X area.
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
    addq     .Alloc.ptr(%rip), %rcx                    # %rcx = .Alloc.ptr + sizeof(obj)
    addq     $8, %rcx                                  # %rcx = .Alloc.ptr + sizeof(obj) + sizeof(eye catcher)
    subq     .Alloc.limit(%rip), %rcx                  # %rcx = %rcx - .Alloc.limit
    jl       .GenGC.offset_copy.copy                   # if (%rcx < 0)
                                                       # go to .GenGC.offset_copy.copy
    # There is not engough heap space to copy the source obj.
    # %rcx contains the amount of required additional memory in bytes.
    # We add 8 bytes to %rcx, to maintain `.Alloc.ptr` < `.Alloc.limit` after the allocation.
    # So, there is room to record an assignment before a garbage collection has to run (if any).
    # See the comments at `.GenGC.handle_assignment` for details.
    addq $8, %rcx

    # Now, as usual, round up %rcx to the nearest greater multiple of 
    # .GenGC.HEAP_PAGE (e.g., 32768 bytes):
    # %rcx = (%rcx + 32767) & (-32768)
    movq     $.GenGC.HEAP_PAGE, %rax                   # %rax = 32768  = 00000000_00000000_10000000_00000000
    decq     %rax                                      # %rax = 32767  = 00000000_00000000_01111111_11111111
    addq     %rax, %rcx                                # %rcx = %rcx + 32767
    notq     %rax                                      # %rax = -32768 = 11111111_11111111_10000000_00000000
    andq     %rax, %rcx                                # %rcx = %rcx & (-32768)
    movq     $.GenGC.HEAP_PAGE, %rax                   # %rax = 32768  = 00000000_00000000_10000000_00000000
    decq     %rax                                      # %rax = 32767  = 00000000_00000000_01111111_11111111
    addq     %rax, %rcx                                # %rcx = %rcx + 32767
    notq     %rax                                      # %rax = -32768 = 11111111_11111111_10000000_00000000
    andq     %rax, %rcx                                # %rcx = %rcx & (-32768)

    movq     %rcx, %rdi                                # %rdi = %rcx = total expansion size
    call     .Platform.alloc                           # %rax: allocated memory block's start

    movq     .Platform.heap_end(%rip), %rax            # %rax      = heap_end after the allocation
    movq     %rax, .Alloc.limit(%rip)                  # .Alloc.limit = heap_end

    movq     POINTER(%rbp), %rdi                       # %rdi = the start of source obj

.GenGC.offset_copy.copy:
    # The checks have passed, 
    # we're going to copy the object now.
    addq     $8, .Alloc.ptr(%rip)                      # reserve a quad for the eye catcher
    movq     .Alloc.ptr(%rip), %rcx                    # %rcx = the start of copy obj
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

    movq     %rdx, .Alloc.ptr(%rip)                    # .Alloc.ptr = the end of dest (copy) obj

    # Subtract (L1 - L0) from the pointer and return it.
    movq     .Platform.heap_start(%rip), %rsi          # %rsi = heap_start
    addq     .GenGC.HDR_L0(%rsi), %rcx                 # %rcx = %rcx + L0
    subq     .GenGC.HDR_L1(%rsi), %rcx                 # %rcx = %rcx + L0 - L1
                                                       #      = %rcx - (L1 - L0)
                                                       #      = the start of dest (copy) object - 
                                                       #        the future back copy offset
    # Mark the source object as copied
    movq     POINTER(%rbp), %rdi                       # %rdi = the start of source obj
    movq     $0, OBJ_SIZE(%rdi)                        # put 0 into the source obj's size
    movq     %rcx, OBJ_VTAB(%rdi)                      # put a forwarding pointer to the copy - offset
                                                       # into the source obj's VTAB slot
.GenGC.offset_copy.copy_done:
    movq     POINTER(%rbp), %rdi
    movq     OBJ_VTAB(%rdi), %rax                      # %rax = a pointer to the obj copy

.GenGC.offset_copy.done:
    movq     %rbp, %rsp
    popq     %rbp
    ret

#
# Major Garbage Collection
#
#   A major collection is carried out whenever either 
#     - Old Area grows beyond the breakpoint
#     - After a minor collection there's still not enough space in 
#       Work Area to satisfy the allocation request
#   `.GenGC.minor_collect` sets up the Old, X, and New areas for us. additionaly,
#   `.GenGC.minor_collect` empties out the assignment stack (see the comments there
#   for details). Consequently, a major collection is free to set `.Alloc.limit` = L4.
#   In particular, once a minor collection completes, 
#     - [L0; L1) are the boundaries of Old Area (unchanged) 
#     - [L1; L2) (ex-Reserve Area) contains all the live objects 
#       collected by the minor collection.
#     - L2 points right past the last live object.
#   `.GenGC.major_collect` then collects all the live objects in 
#   Old Area [L0; L1) into New Area [L2; L3).
#
#   A major collection consists of five phases:
#
#     1) Set `.Alloc.ptr` into New Area (L2), and `.Alloc.limit` to L4. Also set the
#        inputs for `.GenGC.offset_copy`.
#
#     2) Traverse the stack (see the minor collector) using `.GenGC.offset_copy`.
#
#     3) Check the registers (see the minor collector) using `.GenGC.offset_copy`.
#
#     4) Traverse the heap from L1 to `.Alloc.ptr` using `.GenGC.offset_copy`. 
#        Note that this includes X area. (See the minor collector)
#
#     5) Block copy the region [L1; .Alloc.ptr) back L1-L0 bytes to create the
#        next Old Area. Save the next Old Area's end in L1. 
#        Calculate the size of the live objects collected from Old Area 
#        and return this value.
#
#   Note that the pointers returned by `.GenGC.offset_copy` are not valid
#   until the block copy is done.
#
#   A bug? Consider the following.
#   An object O from Old Area points to an object W from Work Area. 
#   A minor collection finds the corresponding record in the assignment stack 
#   and (properly) detects the object W as alive.
#   Now, during the major collection immediately following this minor collection,
#   the object O is detected as garbage and hence not copied over into [L2; L3).
#   Therefore, the object W doesn't have any references to it anymore, and is
#   garbage itself. The major collection still copies it from [L1; .Alloc.ptr)
#   at the step 5 above, but it must not. The object W (errorneously) 
#   lives on until the next major collection.
#
#   INPUT:
#    %rdi: the tip of stack to start checking for roots from
#
#    $a0: end of stack
#    heap_start: start of heap
#
#   OUTPUT:
#    %rax: the size of objects collected into New Area [L2; L3)
#
#   GLOBALS MODIFIED:
#    L1: the new Old Area's end
#
#   Registers modified:
#    All the caller-saved registers
#

.GenGC.major_collect:
    STACK_TIP_SZ    = 8
    STACK_TIP       = -STACK_TIP_SZ
    LOOP_INDEX_SZ   = 8
    LOOP_INDEX      = -(STACK_TIP_SZ + LOOP_INDEX_SZ)
    LOOP_LIMIT_SZ   = 8
    LOOP_LIMIT      = -(STACK_TIP_SZ + LOOP_INDEX_SZ + LOOP_LIMIT_SZ)
    XA_OBJ_SZ       = 8
    XA_OBJ          = -(STACK_TIP_SZ + LOOP_INDEX_SZ + LOOP_LIMIT_SZ + XA_OBJ_SZ)
    XA_OBJ_SIZE_SZ  = 8
    XA_OBJ_SIZE     = -(STACK_TIP_SZ + LOOP_INDEX_SZ + LOOP_LIMIT_SZ + XA_OBJ_SZ + XA_OBJ_SIZE_SZ)
    FRAM_SZ         =   STACK_TIP_SZ + LOOP_INDEX_SZ + LOOP_LIMIT_SZ + XA_OBJ_SZ + XA_OBJ_SIZE_SZ

    push    %rbp
    movq    %rsp, %rbp
    subq    $FRAME_SIZE, %rsp

    movq    %rdi, STACK_TIP(%rbp)

    movq    .Platform.heap_start(%rip), %r8      # %r8 = heap_start

    # Set up the inputs for `.GenGC.offset_copy`
    # At this stage, L2 points right past the last live object 
    # collected by `.GenGC.minor_collect`
    movq    .GenGC.HDR_L2(%r8), %rax
    movq    %rax, .Alloc.ptr(%rip)                # .Alloc.ptr = L2 = the end of collected live objs
    movq    .GenGC.HDR_L4(%r8), %rax
    movq    %rax, .Alloc.limit(%rip)              # .Alloc.limit = L4 = the end of heap (including Unused Area)

    # Inspect the stack for GC roots within [%r9, %r10)
    movq    %rdi, %r9                             # %r9 = %rdi = stack tip
    movq    .GenGC.HDR_STK(%r8), %r10             # %r10       = stack base
    cmpq    %r10, %r9
    # See if the stack is empty
    jge     .GenGC.major_collect.registers        # if (stack tip >= stack base) // stack grows down!
                                                  # go to .GenGC.major_collect.registers
    movq    %r9, LOOP_INDEX(%rbp)
    movq    %r10, LOOP_LIMIT(%rbp)

.GenGC.major_collect.stack_loop:
    movq     0(%r9), %rdi                         # %rdi = stack entry at %r9
    call    .GenGC.offset_copy

    movq    LOOP_INDEX(%rbp), %r9

    movq    %rax, 0(%r9)                          # replace the stack entry pointing to old object 
                                                  # with the pointer to copy
    addq    $8, %r9                               # move up one stack entry closer to the base
    movq    %r9, LOOP_INDEX(%rbp)
    cmpq    LOOP_LIMIT(%rbp), %r9
    jl      .GenGC.major_collect.stack_loop       # if (%r9 < stack base) // stack grows down!
                                                  # go to .GenGC.major_collect.stack_loop
.GenGC.major_collect.registers:
    # Inspect %rbx, %r12, %r13, %r14, %r15 for GC roots
    #
    # We follow the SYSV AMD64 ABI convention.
    #
    # The callee must preserve the value of %rbx, %r12, %r13, %r14, %r15.
    # So the calling code is free to store GC roots in these registers and
    # rely on them to be preserved across a procedure call. Hence we must
    # inspect these registers.
    #
    # On the other hand, the calling code is responsible for placing 
    # %rdi, %rsi, %rdx, %rcx, %r8, %r9 if it wants the registers preserved across 
    # a procedure call. Therefore, when we inspect the stack for GC roots it 
    # includes any GC roots from the caller-saved registers.

    # %rbx
    movq    %rbx, %rdi                            # %rdi = %rdx = a potential pointer to an object 
    call    .GenGC.offset_copy                    # if %rdx indeed points to an object,
                                                  # we make a copy and the object lives on.
                                                  # %rax = the start of copy or %rdi's original value.
    movq    %rax, %rbx                            # %rbx = the start of copy or unchanged.
    # %r12
    movq    %r12, %rdi
    call    .GenGC.offset_copy
    movq    %rax, %r12
    # %r13:
    movq    %r13, %rdi
    call    .GenGC.offset_copy
    movq    %rax, %r13
    # %r14:
    movq    %r14, %rdi
    call    .GenGC.offset_copy
    movq    %rax, %r14
    # %r15:
    movq    %r15, %rdi
    call    .GenGC.offset_copy
    movq    %rax, %r15

    # The objects we've copied to X Area [L1; .Alloc.ptr) so far
    # (including the live objects copied by minor collection and 
    # the live objects copied by us from Old Area) are GC roots. 
    # Now traverse them in breadth-first order and append any objects 
    # they point to at the end of X Area.
    # Keep traversing the appended objects. This process stops when we
    # can't find any more objects to append to X Area.
    movq    .Platform.heap_start(%rip), %r9
    movq    .GenGC.HDR_L1(%r9), %r9               # %r9 = L1 = the first obj's eye-catcher

    # See if we have any live objects at all.
    # `.Alloc.ptr` points right past the last obj copied by `.GenGC.offset_copy`
    # into X Area. `.Alloc.ptr` <= L1 means no objects have been copied.
    cmpq    .Alloc.ptr(%rip), %r9
    jge     .GenGC.major_collect.copy_back             # if (L1 >= .Alloc.ptr) go to ...

    # X Area loop
    # %r9 = L1 = the first obj's eye-catcher
.GenGC.major_collect.xa_loop:
    # Calculate the current object
    addq    $8, %r9                               # skip the eye-catcher
    movq    %r9, XA_OBJ(%rbp)                     # %r9 = the current obj

    # Check the current obj is prefixed by $EYE_CATCH
    cmpq    $EYE_CATCH, OBJ_EYE_CATCH(%r9)
    jne     .GenGC.major_collect.abort

    # Calculate the current obj's size in bytes
    movq    OBJ_SIZE(%r9), %r11                   # %r11 = sizeof(obj) in quads
    salq    $3, %r11                              # quads to bytes
    movq    %r11, XA_OBJ_SIZE(%rbp)

    # Switch on the obj's type 
    movq    OBJ_TAG(%r9), %rax                    # %rax = the obj's tag
    cmpq    $INT_TAG, %rax
    je      .GenGC.major_collect.xa_int
    cmpq    $BOOL_TAG, %rax
    je      .GenGC.major_collect.xa_bool
    cmpq    $STRING_TAG, %rax
    je      .GenGC.major_collect.xa_string
    cmpq    $ARRAYANY_TAG, %rax
    je      .GenGC.major_collect.xa_array

    # Handle an object of any other type
    # As we handle objects of special types individually,
    # we know every attribute of this object points to another object.
    # Just check and copy them one by one, updating each attribute with the copy's address.
    leaq    OBJ_ATTR(%r9), %r10                   # %r10 = a ptr to the obj's first attribute
    addq    %r9, %r11                             # %r11 = sizeof(obj) + the start of obj 
                                                  #      = the limit of attrs
    # See if the object has not attributes
    cmpq    %r11, %r10
    jge     .GenGC.major_collect.xa_continue      # if (%r10 >= %r11) continue

    movq    %r10, LOOP_INDEX(%rbp)
    movq    %r11, LOOP_LIMIT(%rbp)
.GenGC.major_collect.xa_obj_loop:
    # Check and copy the object pointed to by the attribute
    movq    0(%r10), %rdi                         # %rdi = the current attr's value
                                                  #      = a pointer to object
    call    .GenGC.offset_copy
    movq    LOOP_INDEX(%rbp), %r10

    movq    %rax, 0(%r10)                         # update the attr's value with
                                                  # a pointer to the obj's copy
    addq    $8, %r10                              # %r10 = a ptr to the next attribute
    movq    %r10, LOOP_INDEX(%rbp)
    cmpq    LOOP_LIMIT(%rbp), %r10
    jl      .GenGC.major_collect.xa_obj_loop      # if (%r10 < the limit of attrs) go to ...

    # Done checking and copying objects pointed to 
    # by the current object's attributes
    jmp     .GenGC.major_collect.xa_continue      # next object

.GenGC.major_collect.xa_array:
    # `ArrayAny` contains one attr pointing to another obj.
    # Namely, `ArrayAny.length` points to an `Int` obj.
    # Plus each array item points to an obj.

    # Calculate the array item loop variables.
    leaq    ARR_ITEMS(%r9), %r10                  # %r10 = a ptr to the first array item
    addq    %r9, %r11                             # %r11 = sizeof(array obj) + the start of array obj
    movq    %r10, LOOP_INDEX(%rbp)
    movq    %r11, LOOP_LIMIT(%rbp)

    # Check and copy the array's length
    movq    ARR_LEN(%r9), %rdi                    # %rdi = a pointer to an Int obj
    call    .GenGC.offset_copy
    movq    XA_OBJ(%rbp), %r9
    movq    %rax, ARR_LEN(%r9)                    # Update the attr with the copy address

    # Check and copy the array items one by one, 
    # updating each item with the copy's address.
    movq    LOOP_INDEX(%rbp), %r10                # %r10 = a ptr to the first array item
    movq    LOOP_LIMIT(%rbp), %r11                # %r11 = sizeof(array obj) + the start of array obj
                                                  #      = the limit of array items
    # See if it's an empty array
    cmpq    %r11, %r10
    jge     .GenGC.major_collect.xa_continue      # if (%r10 >= %r11) continue
.GenGC.major_collect.xa_array_loop:
    # Check and copy the object pointed to by the attribute
    movq    0(%r10), %rdi                         # %rdi = the current item's value
                                                  #      = a pointer to object
    call    .GenGC.offset_copy
    movq    LOOP_INDEX(%rbp), %r10

    movq    %rax, 0(%r10)                         # update the item's value with
                                                  # a pointer to the obj's copy
    addq    $8, %r10                              # %r10 = a ptr to the next item
    movq    %r10, LOOP_INDEX(%rbp)
    cmpq    LOOP_LIMIT(%rbp), %r10
    jl      .GenGC.major_collect.xa_array_loop    # if (%r10 < the limit of array items) go to ...
    # Done checking and copying objects pointed to 
    # by the array items
    jmp     .GenGC.major_collect.xa_continue      # next object

.GenGC.major_collect.xa_string:
    # `String` contains one attr pointing to another obj.
    # Namely, `String.length` points to an `Int` obj.
    movq    STR_LEN(%r9), %rdi                    # %rdi = a pointer to an Int obj
    call    .GenGC.offset_copy
    movq    XA_OBJ(%rbp), %r9
    movq    %rax, STR_LEN(%r9)                    # Update the attr with the copy address
    # From here, we'll just fall through to `.GenGC.major_collect.xa_continue`.
    # So, no need to jump.
.GenGC.major_collect.xa_int:
.GenGC.major_collect.xa_bool:
    # Int and Bool don't contain any pointers to other objects.
    # So, there's nothing to do for them.
.GenGC.major_collect.xa_continue:
    # Move %r9 past the current obj's last attribute
    # and see if it has reached `.Alloc.ptr`. As during a minor collection
    # `.Alloc.ptr` points to the start of free space in Reserve Area,
    # reaching it means we've inspected all the objs in Reserve Area.
    # `.GenGC.offset_copy` advances `.Alloc.ptr` whenever it copies an obj.
    movq    XA_OBJ(%rbp), %r9                     # %r0 = a ptr to the curr obj
    addq    XA_OBJ_SIZE(%rbp), %r9                # %r9 = a ptr to the next obj's eye-catcher
    cmpq    .Alloc.ptr(%rip), %r9
    jl      .GenGC.major_collect.xa_loop          # if (%r9 < .Alloc.ptr) go to ...

.GenGC.major_collect.copy_back:
    # We've collected all the live objects into [L1; .Alloc.ptr).
    # Now copy them from [L1; .Alloc.ptr) back to L0.
    # This is going to be the new Old Area.
    movq    .Platform.heap_start(%rip), %r8       # %r8  = heap_start

    movq    .GenGC.HDR_L0(%r8), %r9               # %r9  = L0 = dest
    movq    .GenGC.HDR_L1(%r8), %r10              # %r10 = L1 = source
    cmpq    .Alloc.ptr(%rip), %r10
    jge     .GenGC.major_collect.done             # if (L1 >= .Alloc.ptr) go to ...

.GenGC.major_collect.copy_back_loop:
    movq    0(%r10), %rax
    movq    %rax, 0(%r9)
    addq    $8, %r9
    addq    $8, %r10
    cmpq    .Alloc.ptr(%rip), %r10
    jl      .GenGC.major_collect.copy_back_loop   # if (%r10 < .Alloc.ptr) go to ...

.GenGC.major_collect.done:
    # This is the only place, where we potentially increase the size of Unused Area [L3; L4)
    # As L3 stays the same but `.Alloc.limit` initialized to L4 at the beginning of 
    # the procedure, increases if `.GenGC.offset_copy` allocates additional memory.
    movq    .Alloc.limit(%rip), %rax
    movq    %rax, .GenGC.HDR_L4(%r8)              # L4 = .Alloc.limit

    movq    .Alloc.ptr(%rip), %rax
    subq    .GenGC.HDR_L2(%r8), %rax              # %rax      = the size of objects collected into New Area [L2; .Alloc.ptr)

    movq    .Alloc.ptr(%rip), %r9                 # %r10      = .Alloc.ptr 
                                                  #           = the end of collected objects
    # (L1 - L0) is the number of bytes we back copied the live object.
    addq    .GenGC.HDR_L0(%r8), %r9
    subq    .GenGC.HDR_L1(%r8), %r9               # %r9       = .Alloc.ptr - (L1 - L0) 
                                                  #           = the new end of Old Area
    movq    %r9, .Alloc.ptr(%rip)                 # .Alloc.ptr = the new end of Old Area
    movq    %r9, .GenGC.HDR_L1(%r8)               # L1        = the new end of Old Area

    movq    %rbp, %rsp
    popq    %rbp
    ret

.GenGC.major_collect.abort:
    movq     $.GenGC.MSG_MAJOR_ERROR_ASCII, %rdi
    movq     $.GenGC.MSG_MAJOR_ERROR_LEN, %rsi
    call     .Platform.out_string
    call     .Runtime.out_nl

    movq   $1, %rdi
    jmp    .Platform.exit_process
