#include "../lodepng/lodepng.h"
#include "../soil2/image_DXT.h"

#define DLLEXPORT extern "C" __declspec(dllexport)
using byte = unsigned char;

struct Color
{
    byte b, g, r, a;
};

static std::vector<Color> BGRAtoRGBA(int w, int h, const Color* bgraImage)
{
    const int count = w * h;
    std::vector<Color> rgbaImage;
    rgbaImage.resize(count);

    Color* rgba = rgbaImage.data();
    const Color* bgra = bgraImage;

    for (int i = 0; i < count; ++i)
    {
        Color c = bgra[i];
        byte b = c.b;
        c.b = c.r;
        c.r = b;
        rgba[i] = c;
    }
    return rgbaImage;
}

/**
 * @return Error string or null if no error happened.
 */
DLLEXPORT const char* __stdcall SaveBGRAImageAsPNG(
    const char* filename, int w, int h, const Color* bgraImage)
{
    std::vector<Color> rgbaImage = BGRAtoRGBA(w, h, bgraImage);
    unsigned error = lodepng::encode(filename, (const byte*)rgbaImage.data(), w, h, LCT_RGBA);
    if (error)
    {
        fprintf(stderr, "SaveImage failed: %s %dx%d %s\n",
                filename, w, h, lodepng_error_text(error));
    }
    return error ? lodepng_error_text(error) : nullptr;
}


/**
 * @return Error string or null if no error happened
 */
DLLEXPORT const char* __stdcall SaveBGRAImageAsDDS(
    const char* filename, int w, int h, const Color* bgraImage)
{
    std::vector<Color> rgbaImage = BGRAtoRGBA(w, h, bgraImage);

    int error = save_image_as_DDS(filename, w, h, 4, (const byte*)rgbaImage.data());
    if (error == 1)
        return "Invalid parameters for DDS";
    if (error == 2)
        return "Failed to create DDS file. Directory not created? File already opened?";
    return nullptr;
}