#ifndef CBASE64_H
#define CBASE64_H

#include <cstddef>
#include <cstdint>

class CBase64
{
public:
    static char *encode(const uint8_t *in, size_t inLen, size_t &outLen);
    static uint8_t *decode(const char *in, size_t inLen, size_t &outLen);
};

#endif // CBASE64_H
