#include <Windows.h>

int main(int argc, char *argv[])
{
    char *szMsg = "Hello, Console!";
    DWORD dwMsgLen = 15;
    DWORD dwWritten;

    HANDLE hOut = GetStdHandle(STD_OUTPUT_HANDLE);
    WriteConsoleA(hOut, szMsg, dwMsgLen, &dwWritten, NULL);
    CloseHandle(hOut);

    return 0;
}

//
// gcc -fno-asynchronous-unwind-tables -fno-exceptions -fno-rtti -fverbose-asm -Wall -Wextra -g -S writeconsole.c
//
