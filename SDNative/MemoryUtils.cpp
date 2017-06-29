#include "util/apex_memmove.h"

extern "C"
{
    /**
     * To do an even faster block copy of C# Arrays, we have this fine little beast:
     */
    __declspec(dllexport) void __stdcall MemCopy(char* dst, const char* src, int numBytes)
    {
        // Around ~20% faster than platform memcpy :O
        apex::memcpy(dst, src, numBytes);
    }
}