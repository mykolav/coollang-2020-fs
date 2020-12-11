#include <Windows.h>

int main(int argc, char *argv[])
{

    HANDLE hOut = GetStdHandle(STD_OUTPUT_HANDLE);
    HANDLE hIn = GetStdHandle(STD_INPUT_HANDLE);

    char *szMsg = "Type a string:>";
    DWORD dwMsgLen = 15;
    DWORD dwWritten;

    WriteConsoleA(hOut, szMsg, dwMsgLen, &dwWritten, NULL);

    char buffer[1024] = { 0 };
    DWORD nNumberOfBytesToRead = 1024;
    DWORD dwNumberOfBytesRead = 0;

    ReadFile(hIn, buffer, nNumberOfBytesToRead, &dwNumberOfBytesRead, NULL);

    WriteConsoleA(hOut, buffer, dwNumberOfBytesRead, &dwWritten, NULL);

    CloseHandle(hOut);
    CloseHandle(hIn);

    return 0;
}

//
// gcc -fno-asynchronous-unwind-tables -fno-exceptions -fno-rtti -fverbose-asm -Wall -Wextra -g -S in_string.c
//
