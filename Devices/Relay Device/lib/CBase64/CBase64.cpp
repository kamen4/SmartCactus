#include "CBase64.h"
#include <cstdlib>
#include <cctype>

static const char encoding_table[] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

static unsigned char decoding_table[256];
static bool table_built = false;

static void build_decoding_table()
{
    for (int i = 0; i < 64; ++i)
    {
        decoding_table[(unsigned char)encoding_table[i]] = i;
    }
    table_built = true;
}

char *CBase64::encode(const uint8_t *data, size_t input_length, size_t &output_length)
{
    static const int mod_table[] = {0, 2, 1};
    output_length = 4 * ((input_length + 2) / 3);

    char *encoded_data = (char *)malloc(output_length + 1);
    if (!encoded_data)
        return nullptr;

    for (size_t i = 0, j = 0; i < input_length;)
    {
        uint32_t octet_a = i < input_length ? data[i++] : 0;
        uint32_t octet_b = i < input_length ? data[i++] : 0;
        uint32_t octet_c = i < input_length ? data[i++] : 0;

        uint32_t triple = (octet_a << 16) + (octet_b << 8) + octet_c;

        encoded_data[j++] = encoding_table[(triple >> 18) & 0x3F];
        encoded_data[j++] = encoding_table[(triple >> 12) & 0x3F];
        encoded_data[j++] = encoding_table[(triple >> 6) & 0x3F];
        encoded_data[j++] = encoding_table[(triple >> 0) & 0x3F];
    }

    for (int i = 0; i < mod_table[input_length % 3]; i++)
    {
        encoded_data[output_length - 1 - i] = '=';
    }
    encoded_data[output_length] = '\0';
    return encoded_data;
}

uint8_t *CBase64::decode(const char *data, size_t input_length, size_t &output_length)
{
    if (input_length % 4 != 0)
        return nullptr;

    if (!table_built)
        build_decoding_table();

    output_length = input_length / 4 * 3;
    if (data[input_length - 1] == '=')
        output_length--;
    if (data[input_length - 2] == '=')
        output_length--;

    uint8_t *decoded_data = (uint8_t *)malloc(output_length);
    if (!decoded_data)
        return nullptr;

    for (size_t i = 0, j = 0; i < input_length;)
    {
        uint32_t sextet_a = data[i] == '=' ? 0 & i++ : decoding_table[(unsigned char)data[i++]];
        uint32_t sextet_b = data[i] == '=' ? 0 & i++ : decoding_table[(unsigned char)data[i++]];
        uint32_t sextet_c = data[i] == '=' ? 0 & i++ : decoding_table[(unsigned char)data[i++]];
        uint32_t sextet_d = data[i] == '=' ? 0 & i++ : decoding_table[(unsigned char)data[i++]];

        uint32_t triple = (sextet_a << 18) + (sextet_b << 12) + (sextet_c << 6) + sextet_d;

        if (j < output_length)
            decoded_data[j++] = (triple >> 16) & 0xFF;
        if (j < output_length)
            decoded_data[j++] = (triple >> 8) & 0xFF;
        if (j < output_length)
            decoded_data[j++] = (triple >> 0) & 0xFF;
    }
    return decoded_data;
}
