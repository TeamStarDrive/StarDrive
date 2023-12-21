#pragma once
#include <libpng/png.h>
#include <src/rpp/file_io.h>
#include <string>
#include <stdarg.h>
#include "shared_image_utils.h"

static const char* format_err(const char* fmt, ...)
{
    static thread_local char buf[512];
    va_list ap; va_start(ap, fmt);
    int len = vsnprintf(buf, sizeof(buf), fmt, ap);
    buf[sizeof(buf)-1] = '\0';
    return buf;
}

// libpng 
class PngLoader
{
    static void err_fn(png_structp self, const char* err) {
        //PngLoader* loader = reinterpret_cast<PngLoader*>(self);
        //loader->errors.push_back(err);
    }
    static void warn_fn(png_structp self, const char* err) {
        //PngLoader* loader = reinterpret_cast<PngLoader*>(self);
        //loader->errors.push_back(err);
    }
    png_structp png = png_create_read_struct(PNG_LIBPNG_VER_STRING, this, &err_fn, &warn_fn);
    png_infop  info = png_create_info_struct(png);

public:

    uint8_t* image = nullptr;
    uint32_t width = 0;
    uint32_t height = 0;
    int channels = 0;
    int stride = 0;
    //std::vector<std::string> errors;

    PngLoader() noexcept = default;

    ~PngLoader() noexcept
    {
        if (image)
            free(image);
        png_destroy_read_struct(&png, &info, 0);
    }

    uint8_t* steal_ptr() noexcept
    {
        uint8_t* img = image;
        image = nullptr;
        return img;
    }

    struct png_io_data {
        const char* ptr;
        const char* end;
    };

    static void PngMemReader(png_structp png, png_bytep dstbuf, size_t numBytes) {
        png_io_data* io = (png_io_data*)png_get_io_ptr(png);
        size_t avail = io->end - io->ptr;
        if (numBytes > avail)
            numBytes = avail;
        memcpy(dstbuf, io->ptr, numBytes);
        io->ptr += numBytes;
    }

    /**
     * @param filename PNG file to load
     * @param flipVertically if true, image rows are flipped vertically (for some graphics API-s)
     * @param bgr If true, load image as BGR/BGRA instead of RGB/RGBA
     * @return NULL on success, error message otherwise
     */
    const char* load(const char* filename, bool flipVertically = false, bool bgr = false) noexcept
    {
        rpp::load_buffer buf = rpp::file::read_all(filename);
        if (!buf.str) {
            return "png error: failed to open file";
        }

        const char* data = buf.str;
        const int size = buf.len;
        if (size <= 8 || !png_check_sig((png_bytep)data, 8)) {
            return "png error: invalid png signature";
        }

        png_io_data io { (char*)data + 8, (char*)data + size };
        png_set_read_fn(png, &io, &PngMemReader);
        png_set_sig_bytes(png, 8); // tell libpng we already read the signature
        if (bgr) {
            png_set_bgr(png);
        }
        png_read_info(png, info);

        int bitDepth = 0;
        int colorType = -1;
        int interlacing = 0; // 0: non-interlaced, 1: Adam7 interlacing
        uint32_t ret = png_get_IHDR(png, info, &width, &height, &bitDepth, &colorType, &interlacing, 0, 0);
        if (ret != 1) {
            return "png error: failed to read PNG header";
        }

        // http://www.libpng.org/pub/png/libpng-1.2.5-manual.html#section-3.7
        if (bitDepth == 16) // strip 16-bit PNG to 8-bit
            png_set_strip_16(png);
        if (colorType == PNG_COLOR_TYPE_PALETTE)
            png_set_palette_to_rgb(png);
        else if (colorType == PNG_COLOR_TYPE_GRAY && bitDepth < 8)
            png_set_expand_gray_1_2_4_to_8(png);
        if (png_get_valid(png, info, (png_uint_32)PNG_INFO_tRNS))
            png_set_tRNS_to_alpha(png);

        int num_passes = 1;
        if (interlacing == 1/*Adam7*/) {
            num_passes = png_set_interlace_handling(png);
            //png_start_read_image(png);
        }

        // now update info based on new unpacking and expansion flags:
        png_read_update_info(png, info);
        bitDepth  = png_get_bit_depth(png, info);
        colorType = png_get_color_type(png, info);
        
        //printf("png %dx%d bits:%d type:%s\n", width, height, bitDepth, strPNGColorType(colorType));

        switch (colorType) {
            default:case PNG_COLOR_TYPE_GRAY:channels = 1; break;
            case PNG_COLOR_TYPE_GRAY_ALPHA:  channels = 2; break;
            case PNG_COLOR_TYPE_PALETTE:
            case PNG_COLOR_TYPE_RGB:         channels = 3; break;
            case PNG_COLOR_TYPE_RGB_ALPHA:   channels = 4; break;
        }

        //// Do a double query on what libpng says the rowbytes are and what
        //// we are assuming. If our rowBytes is wrong, then colorType switch 
        //// has a bug and GL format is wrong. It will most likely segfault.
        //stride = AlignRowTo4(width, channels);
        //int rowBytes = (int)png_get_rowbytes(png, info);
        //if (stride != rowBytes) {
        //    return format_err("png error: PNG.rowbytes(%d) != expected stride(%d)  image: %dx%d", rowBytes, stride, width, height);
        //}

        // Ignore the stride restriction and lock us to Direct3D
        stride = (int)png_get_rowbytes(png, info);

        uint8_t* dest = (uint8_t*)malloc(stride * height);
        if (!dest) { // most likely corrupted image which causes a huge allocation
            return format_err("png error: failed to allocate image bytes=%u", stride * height);
        }

        image = dest;

        if (flipVertically)
        {
            for (int pass = 0; pass < num_passes; ++pass)
            {
                // OpenGL eats images in reverse row order, so read all rows in reverse
                for (int y = height-1; y >= 0; --y)
                {
                    uint8_t* dstRow = dest + y * stride;
                    png_read_row(png, (png_bytep)dstRow, nullptr);
                }
            }
        }
        else
        {
            for (int pass = 0; pass < num_passes; ++pass)
            {
                for (uint32_t y = 0; y < height; ++y)
                {
                    uint8_t* dstRow = dest + y * stride;
                    png_read_row(png, (png_bytep)dstRow, nullptr);
                }
            }
        }

        //if (!errors.empty()) {
        //    for (const std::string& warn : errors) {
        //        fprintf(stderr, "png error: %s  %s\n", warn.c_str(), filename);
        //    }
        //}

        return nullptr; // Success! No error message.
    }
};
