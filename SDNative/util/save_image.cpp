#include <lodepng/lodepng.h>
#include <soil2/image_DXT.h>
#include <stb/stb_dxt.h>
#include <cstdio>
#include <memory>
#include <rpp/debugging.h>

#define DLLEXPORT extern "C" __declspec(dllexport)
using byte = unsigned char;

struct Point
{
    int x, y;
};

struct Color // BGRA
{
    byte b, g, r, a;
};

struct RGB
{
	byte r, g, b;
};
struct RGBA
{
    byte r, g, b, a;
};

enum DDSFlags
{
    //! Use DXT1 compression.
    Dxt1 = ( 1 << 0 ),
    //! Use DXT5 compression.
    Dxt5 = ( 1 << 1 ),
    //! Source is BGRA rather than RGBA
    SourceBGRA = ( 1 << 2 )
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

std::unique_ptr<RGBA[]> CopyBGRAtoRGBA(int w, int h, const Color* src)
{
    const int count = w * h;
	std::unique_ptr<RGBA[]> storage { new RGBA[count] };
	RGBA* dst = storage.get();
    for (int i = 0; i < count; ++i)
    {
    	dst[i].r = src[i].r;
    	dst[i].g = src[i].g;
    	dst[i].b = src[i].b;
    	dst[i].a = src[i].a;
    }
	return storage;
}

std::unique_ptr<RGB[]> CopyBGRAtoRGB(int w, int h, const Color* src)
{
    const int count = w * h;
	std::unique_ptr<RGB[]> storage { new RGB[count] };
	RGB* dst = storage.get();
    for (int i = 0; i < count; ++i)
    {
    	dst[i].r = src[i].r;
    	dst[i].g = src[i].g;
    	dst[i].b = src[i].b;
    }
	return storage;
}

std::unique_ptr<RGB[]> CopyRGBAtoRGB(int w, int h, const RGBA* src)
{
    const int count = w * h;
	std::unique_ptr<RGB[]> storage { new RGB[count] };
	RGB* dst = storage.get();
    for (int i = 0; i < count; ++i)
    {
    	dst[i].r = src[i].r;
    	dst[i].g = src[i].g;
    	dst[i].b = src[i].b;
    }
	return storage;
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
    const char* filename, int w, int h, Color* rgbaImage, DDSFlags flags)
{
    int error;
	if (flags & Dxt1)
	{
		std::unique_ptr<RGB[]> temp;
	    if (flags & SourceBGRA)
	    	temp = CopyBGRAtoRGB(w, h, rgbaImage);
        else
	        temp = CopyRGBAtoRGB(w, h, (const RGBA*)rgbaImage);
		
		const byte* img = (const byte*)temp.get();
		error = save_image_as_DDS(filename, w, h, 3, img);
    }
    else if (flags & Dxt5)
    {
		const byte* img = (const byte*)rgbaImage;
		std::unique_ptr<RGBA[]> temp;
	    if (flags & SourceBGRA)
		{
	    	temp = CopyBGRAtoRGBA(w, h, rgbaImage);
	    	img = (const byte*)temp.get();
		}
        else
        {
        	// RGBA: no additional work required
        }
    	error = save_image_as_DDS(filename, w, h, 4, img);
    }
    else
    {
	    return "Require at least Dxt1 or Dxt5 flags to be set!";
    }

    if (error == 1)
        return "Invalid parameters for DDS";
    if (error == 2)
        return "Failed to create DDS file. Directory not created? File already opened?";
    return nullptr;
}

struct Image
{
    Color* data;
    int width;
    int height;
};

struct ImageCopy final
{
    Color* dst;
    Color* src;
    Color* end;
    int dst_stride;
    int src_stride;
    __forceinline void next_row()
    {
        dst += dst_stride;
        src += src_stride;
    }
    __forceinline void fill_rows()
    {
        for (; dst < end; next_row())
            memcpy(dst, src, sizeof(Color) * src_stride);
    }
    __forceinline void row()
    {
        memcpy(dst, src, sizeof(Color) * src_stride);
    }
    __forceinline void column()
    {
        for (; dst < end; next_row()) *dst = *src;
    }
    __forceinline void pixel()
    {
        *dst = *src;
    }
};

ImageCopy select_region(const Image& d, const Image& s, 
                        int destX, int destY, int srcX, int srcY)
{
    ImageCopy ic {};
    ic.dst = d.data + (d.width * destY) + destX;
    ic.src = s.data + (s.width * srcY) + srcX;
    ic.end = ic.dst + (d.width * s.height);
    ic.dst_stride = d.width;
    ic.src_stride = s.width;
    return ic;
}



// Applies 1px padding while copying {src} to {dst:x,y}
DLLEXPORT void __stdcall CopyPixelsPadded(Image dst, int x, int y, Image src)
{
    #define RangeCheck(error_condition) \
    if (error_condition) { \
        LogError("src {%dx%d} does not fit to dst {%dx%d: %d,%d}: " #error_condition, \
                 src.width, src.height, dst.width, dst.height, x, y); \
    }
    RangeCheck(x < 0);
    RangeCheck(y < 0);
    RangeCheck((x + src.width)  > dst.width);
    RangeCheck((y + src.height) > dst.height);

    select_region(dst, src, x, y, 0, 0).fill_rows(); // main image rect

    // o-------o {p}px Padding
    // |o-----o|
    // || src ||
    // |o-----S|
    // o-------D
    const Point S { src.width - 1, src.height - 1 }; // lower-right point inside src image (last pixel of the src image)
    const Point D { x + src.width, y + src.height }; // lower-right point of padding in dst image (one pixel outside the dst rect)
    const bool left = x > 0;
    const bool top  = y > 0;
    const bool right  = D.x < dst.width;  // in image bounds? 
    const bool bottom = D.y < dst.height;
    // padding rect around the image
    if (top)    { select_region(dst, src, x, y-1, /*dst*/0,   0).row(); } // copy rows and cols
    if (bottom) { select_region(dst, src, x, D.y, /*dst*/0, S.y).row(); }
    if (left)   { select_region(dst, src, x-1, y, /*dst*/0,   0).column(); }
    if (right)  { select_region(dst, src, D.x, y, /*dst*/S.x, 0).column(); }
    // also fill corners
    if (top && left)     { select_region(dst, src, x-1, y-1, /*dst*/0,   0  ).pixel(); }
    if (top && right)    { select_region(dst, src, D.x, y-1, /*dst*/S.x, 0  ).pixel(); }
    if (bottom && left)  { select_region(dst, src, x-1, D.y, /*dst*/0,   S.y).pixel(); }
    if (bottom && right) { select_region(dst, src, D.x, D.y, /*dst*/S.x, S.y).pixel(); }
}

DLLEXPORT void __stdcall FillPixels(Image dst, int x, int y, Color color, int w, int h)
{
    int endX = x + (w - 1);
    if (endX >= dst.width) endX = dst.width - 1;
    int endY = y + (h - 1);
    if (endY >= dst.height) endY = dst.height - 1;

    Color* image = dst.data;
    for (int iy = y; iy <= endY; ++iy)
    {
        Color* row = (image + dst.width*iy);
        for (int ix = x; ix <= endX; ++ix)
        {
            row[ix] = color;
        }
    }
}

DLLEXPORT int __stdcall HasTransparentPixels(Image img)
{
	for (int y = 0; y < img.height; ++y)
	{
		Color* row = &img.data[img.width * y];
		for (int x = 0; x < img.width; ++x)
		{
            if (row[x].a != byte{255})
                return true;
		}
	}
	return false; // all pixel Alphas were 255
}
