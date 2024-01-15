#include <stdio.h>

void itoa(long i) {
    char digits[12] = { 0 };

    const signed char is_negative = i < 0;
    if (is_negative) {
        i = -i;
    }

    signed char digit_pos = 9;
    do {
        signed char remainder = i % 10;
        digits[digit_pos] = remainder + '0';
        --digit_pos;

        i = i / 10;
    } while (i > 0);

    if (is_negative) {
        digits[digit_pos] = '-';
        --digit_pos;
    }

    const char * ascii = digits + (digit_pos + 1);
    printf("itoa: %s\r\n", ascii);
}

int main() {
    itoa(0);
    
    itoa(1);
    itoa(2);
    itoa(3);

    itoa(10);
    itoa(20);
    itoa(30);

    itoa(9001);

    itoa(123456789);
    
    itoa(-0);
    
    itoa(-1);
    itoa(-2);
    itoa(-3);

    itoa(-10);
    itoa(-20);
    itoa(-30);

    itoa(-9001);

    itoa(-123456789);
    
    return 0;
}
