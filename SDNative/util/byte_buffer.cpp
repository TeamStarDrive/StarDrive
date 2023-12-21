#include <cstdlib>
#include <cstring>
#include <src/rpp/strview.h>

#define DLLEXPORT extern "C" __declspec(dllexport)
#define STDCALL(ret) DLLEXPORT ret __stdcall
using byte = unsigned char;

/**
 * This is here to speed up bulk byte operations in C#
 * without using insane amount of memory
 * 
 * All the memory here is managed manually for max performance
 */
struct ByteBuffer
{
    byte* Data;
    int Capacity;
    int Size;

    // @return &Data[oldSize]
    byte* Grow(int addedLength) noexcept
    {
        int oldSize = Size;
        int newSize = oldSize + addedLength;
        Size = newSize;

        int capacity = Capacity;
        if (capacity < newSize)
        {
            do {
                capacity *= 2;
            } while (capacity < newSize);

            if (byte* newData = (byte*)realloc(Data, capacity)) {
                Data = newData;
                Capacity = capacity;
            }
        }
        return &Data[oldSize];
    }
};

// No need to use C++ for this, since it's all C-wrapped anyways
STDCALL(ByteBuffer*) ByteBufferNew(int defaultCapacity) noexcept
{
    auto* b = (ByteBuffer*)malloc(sizeof(ByteBuffer));
    if (!b) return nullptr;
    b->Data = (byte*)malloc(defaultCapacity);
    b->Capacity = defaultCapacity;
    b->Size = 0;
    return b;
}

STDCALL(void) ByteBufferDelete(ByteBuffer* b) noexcept
{
    free(b->Data);
    free(b);
}

STDCALL(void) ByteBufferCopy(ByteBuffer* b, byte* out) noexcept
{
    memcpy(out, b->Data, b->Size);
}

STDCALL(void) ByteBufferWriteI(ByteBuffer* b, int val) noexcept
{
    char buf[32];
    int len = rpp::_tostring(buf, val);
    memcpy(b->Grow(len), buf, len);
}

STDCALL(void) ByteBufferWriteF(ByteBuffer* b, float val, int maxDecimals) noexcept
{
    char buf[32];
    int len = rpp::_tostring(buf, val, maxDecimals);
    memcpy(b->Grow(len), buf, len);
}

STDCALL(void) ByteBufferWriteD(ByteBuffer* b, double val, int maxDecimals) noexcept
{
    char buf[32];
    int len = rpp::_tostring(buf, val);
    memcpy(b->Grow(len), buf, len);
}

STDCALL(void) ByteBufferWriteC(ByteBuffer* b, wchar_t ch) noexcept
{
    byte* dst = b->Grow(1);
    *dst = (byte)ch;
}

__forceinline void Copy(byte* dst, const wchar_t* str, int len) noexcept
{
    for (int i = 0; i < len; ++i)
        dst[i] = (byte)str[i];
}

STDCALL(void) ByteBufferWriteS(ByteBuffer* b,
    const wchar_t* str, int len) noexcept
{
    byte* dst = b->Grow(len);
    Copy(dst, str, len);
}


// key=value\n
STDCALL(void) ByteBufferWriteKV(ByteBuffer* b,
    const wchar_t* key, int keylen,
    const wchar_t* val, int vallen) noexcept
{
    int len = keylen + vallen + 2;
    byte* dst = b->Grow(len);

    Copy(dst, key, keylen); dst += keylen;
    *dst++ = '=';
    Copy(dst, val, vallen); dst += vallen;
    *dst = '\n';
}

