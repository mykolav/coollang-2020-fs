#!/bin/bash

# https://stackoverflow.com/a/5947802/818321
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Building F#${NC}\n" && \
dotnet build fs-coollang.sln && \
echo -e "\n${YELLOW}Assembling the runtime${NC}\n" && \
as -o ./src/Runtime/rt_linux.o ./src/Runtime/rt_linux.s && \
as -o ./src/Runtime/rt_windows.o ./src/Runtime/rt_windows.s && \
as -o ./src/Runtime/rt_common.o ./src/Runtime/rt_common.s && \
echo -e "${YELLOW}Done${NC}\n"
