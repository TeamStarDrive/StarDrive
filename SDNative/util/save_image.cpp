#include <lodepng/lodepng.h>
#include <soil2/image_DXT.h>
#include <libsquish/squish.h>
#include <stb/stb_dxt.h>
#include <cstdio>

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

enum DxtEncoders
{
	LibSquish,
	LibSoil2,
};

constexpr DxtEncoders DxtEncoder = LibSoil2;

/**
 * @return Error string or null if no error happened
 */
DLLEXPORT const char* __stdcall SaveImageAsDDS(
    const char* filename, int w, int h, const Color* rgbaImage)
{
	if constexpr (DxtEncoder == LibSquish)
	{
		unsigned dxt_size = squish::GetStorageRequirements(w, h, squish::kDxt5);;
		std::vector<uint8_t> dxt; dxt.resize(dxt_size);

		squish::CompressImage((const byte*)rgbaImage, w, h, dxt.data(),
							  squish::kColourClusterFit | squish::kDxt5);

		DDS_header header = { 0 };
		header.dwMagic = ('D' << 0) | ('D' << 8) | ('S' << 16) | (' ' << 24);
		header.dwSize = 124;
		header.dwFlags = DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT | DDSD_LINEARSIZE;
		header.dwWidth = w;
		header.dwHeight = h;
		header.dwPitchOrLinearSize = dxt_size;
		header.sPixelFormat.dwSize = 32;
		header.sPixelFormat.dwFlags = DDPF_FOURCC;
		header.sPixelFormat.dwFourCC = ('D' << 0) | ('X' << 8) | ('T' << 16) | ('5' << 24);
		header.sCaps.dwCaps1 = DDSCAPS_TEXTURE;

		if (FILE* f = fopen(filename, "wb")) {
			fwrite(&header, sizeof(DDS_header), 1, f);
			fwrite(dxt.data(), 1, dxt_size, f);
			fclose(f);
		}
		else return "Failed to create DDS file. Directory not created? File already opened?";
	}
	else
	{
		int error = save_image_as_DDS(filename, w, h, 4, (const byte*)rgbaImage);
		if (error == 1)
			return "Invalid parameters for DDS";
		if (error == 2)
			return "Failed to create DDS file. Directory not created? File already opened?";
	}
    return nullptr;
}


DLLEXPORT void __stdcall CopyPixelsRGBA(Color* dst, int dstWidth, int x, int y, const Color* src, int w, int h)
{
	Color* dstRow = dst + (dstWidth * y) + x;
	const Color* srcRow = src;
	for (int iy = 0; iy < h; ++iy)
	{
		memcpy(dstRow, srcRow, sizeof(Color) * w); // copy row
		dstRow += dstWidth; // move to next row
		srcRow += w;
	}
}
