void bar(int i) {}
int foo(int p0,
        int p1,
        int p2,
        int p3,
        int p4,
        int p5,
        int p6) {

    bar(9001);
    int blah = 100;
    int sum = p0 + p1 + p2 + p3 + p4 + p5 + p6;
    return sum + blah;
}
int main() {
    return foo(0, 1, 2, 3, 4, 5, 6);
}

//
// gcc -fno-asynchronous-unwind-tables -fno-exceptions -fno-rtti -fverbose-asm -Wall -Wextra -g -S call1.c
//
