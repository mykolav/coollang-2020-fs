#!/bin/bash

pushd ./src/Runtime && \
as -o ./rt_linux.o ./rt_linux.s && \
as -o ./rt_windows.o ./rt_windows.s && \
as -o ./rt_gen_gc.o ./rt_gen_gc.s && \
as -o ./rt_memory.o ./rt_memory.s && \
as -o ./rt_runtime.o ./rt_runtime.s && \
as -o ./rt_common.o ./rt_common.s && \
popd
