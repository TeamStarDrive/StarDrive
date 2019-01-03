#include "../lodepng/lodepng.h"
#include "../soil2/image_DXT.h"

#define DLLEXPORT extern "C" __declspec(dllexport)
using byte = unsigned char;

struct Color
{
    byte b, g, r, a;
};

DLLEXPORT void __stdcall ConvertBGRAtoRGBA(int w, int h, Color* image)
{
	const int count = w * h;
	for (int i = 0; i < count; ++i)
	{
		const byte temp = image[i].r;
		image[i].r = image[i].b;
		image[i].b = temp;
	}
}

/**
 * @return Error string or null if no error happened.
 */
DLLEXPORT const char* __stdcall SaveImageAsPNG(
    const char* filename, int w, int h, const Color* rgbaImage)
{
    unsigned error = lodepng::encode(filename, (const byte*)rgbaImage, w, h, LCT_RGBA);
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
DLLEXPORT const char* __stdcall SaveImageAsDDS(
    const char* filename, int w, int h, const Color* rgbaImage)
{
    int error = save_image_as_DDS(filename, w, h, 4, (const byte*)rgbaImage);
    if (error == 1)
        return "Invalid parameters for DDS";
    if (error == 2)
        return "Failed to create DDS file. Directory not created? File already opened?";
    return nullptr;
}