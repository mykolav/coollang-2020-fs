#!/bin/bash

as -o ./src/Runtime/rt_linux.o ./src/Runtime/rt_linux.s && \
as -o ./src/Runtime/rt_windows.o ./src/Runtime/rt_windows.s && \
as -o ./src/Runtime/rt_memory.o ./src/Runtime/rt_memory.s && \
as -o ./src/Runtime/rt_common.o ./src/Runtime/rt_common.s
