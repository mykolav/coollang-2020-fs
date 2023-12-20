#!/bin/bash

# https://stackoverflow.com/a/5947802/818321
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Building F#${NC}\n" && \
dotnet build fs-coollang.sln && \
echo -e "\n${YELLOW}Assembling the runtime${NC}\n" && \
pushd ./src/Runtime && \
as -o ./rt_linux.o ./rt_linux.s && \
as -o ./rt_windows.o ./rt_windows.s && \
as -o ./rt_gen_gc.o ./rt_gen_gc.s && \
as -o ./rt_memory.o ./rt_memory.s && \
as -o ./rt_runtime.o ./rt_runtime.s && \
as -o ./rt_common.o ./rt_common.s && \
popd && \
echo -e "${YELLOW}Done${NC}\n"
