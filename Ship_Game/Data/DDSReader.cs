using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
// ReSharper disable IdentifierTypo
// ReSharper disable UnusedMember.Local
// ReSharper disable CommentTypo
// ReSharper disable ArrangeTypeMemberModifiers

namespace Ship_Game
{
    public class DDSReader
    {
        private DDSReader(byte[] ddsImage)
        {
            if (ddsImage == null) return;
            if (ddsImage.Length == 0) return;

            using (var stream = new MemoryStream(ddsImage.Length))
            {
                stream.Write(ddsImage, 0, ddsImage.Length);
                stream.Seek(0, SeekOrigin.Begin);

                using (var reader = new BinaryReader(stream))
                {
                    Parse(reader);
                }
            }
        }

        private DDSReader(Stream ddsImage)
        {
            if (ddsImage == null) return;
            if (!ddsImage.CanRead) return;

            using (var reader = new BinaryReader(ddsImage))
            {
                Parse(reader);
            }
        }

        void Parse(BinaryReader reader)
        {
            var header = new DDSHeader();

            if (!ReadHeader(reader, ref header))
                return;

            if (header.depth == 0) header.depth = 1;

            PixelFormat format = GetFormat(header, out uint _);
            if (format == PixelFormat.UNKNOWN)
            {
                throw new InvalidFileHeaderException();
            }

            byte[] data = ReadData(reader, header);
            if (data != null)
            {
                byte[] rawData = DecompressData(header, data, format);
                // TODO??
            }
        }

        static byte[] ReadData(BinaryReader reader, DDSHeader header)
        {
            byte[] compdata;
            uint compsize;

            if ((header.flags & DDSD_LINEARSIZE) > 1)
            {
                compdata = reader.ReadBytes((int)header.sizeorpitch);
                compsize = (uint)compdata.Length;
            }
            else
            {
                uint bps = header.width * header.pixelformat.rgbbitcount / 8;
                compsize = bps * header.height * header.depth;
                compdata = new byte[compsize];

                var mem = new MemoryStream((int)compsize);

                for (int z = 0; z < header.depth; z++)
                {
                    for (int y = 0; y < header.height; y++)
                    {
                        byte[] temp = reader.ReadBytes((int)bps);
                        mem.Write(temp, 0, temp.Length);
                    }
                }
                mem.Seek(0, SeekOrigin.Begin);

                mem.Read(compdata, 0, compdata.Length);
                mem.Close();
            }

            return compdata;
        }

        static bool ReadHeader(BinaryReader reader, ref DDSHeader header)
        {
            byte[] signature = reader.ReadBytes(4);
            if (!(signature[0] == 'D' && signature[1] == 'D' && signature[2] == 'S' && signature[3] == ' '))
                return false;

            header.size = reader.ReadUInt32();
            if (header.size != 124)
                return false;

            //convert the data
            header.flags = reader.ReadUInt32();
            header.height = reader.ReadUInt32();
            header.width = reader.ReadUInt32();
            header.sizeorpitch = reader.ReadUInt32();
            header.depth = reader.ReadUInt32();
            header.mipmapcount = reader.ReadUInt32();
            header.alphabitdepth = reader.ReadUInt32();

            header.reserved = new uint[10];
            for (int i = 0; i < 10; i++)
            {
                header.reserved[i] = reader.ReadUInt32();
            }

            //pixelfromat
            header.pixelformat.size = reader.ReadUInt32();
            header.pixelformat.flags = reader.ReadUInt32();
            header.pixelformat.fourcc = reader.ReadUInt32();
            header.pixelformat.rgbbitcount = reader.ReadUInt32();
            header.pixelformat.rbitmask = reader.ReadUInt32();
            header.pixelformat.gbitmask = reader.ReadUInt32();
            header.pixelformat.bbitmask = reader.ReadUInt32();
            header.pixelformat.alphabitmask = reader.ReadUInt32();

            //caps
            header.ddscaps.caps1 = reader.ReadUInt32();
            header.ddscaps.caps2 = reader.ReadUInt32();
            header.ddscaps.caps3 = reader.ReadUInt32();
            header.ddscaps.caps4 = reader.ReadUInt32();
            header.texturestage = reader.ReadUInt32();

            return true;
        }

        static PixelFormat GetFormat(in DDSHeader header, out uint blocksize)
        {
            var format = PixelFormat.UNKNOWN;
            if ((header.pixelformat.flags & DDPF_FOURCC) == DDPF_FOURCC)
            {
                blocksize = ((header.width + 3) / 4) * ((header.height + 3) / 4) * header.depth;

                switch (header.pixelformat.fourcc)
                {
                    case FOURCC_DXT1:
                        format = PixelFormat.DXT1;
                        blocksize *= 8;
                        break;
                    case FOURCC_DXT2:
                        format = PixelFormat.DXT2;
                        blocksize *= 16;
                        break;
                    case FOURCC_DXT3:
                        format = PixelFormat.DXT3;
                        blocksize *= 16;
                        break;
                    case FOURCC_DXT4:
                        format = PixelFormat.DXT4;
                        blocksize *= 16;
                        break;
                    case FOURCC_DXT5:
                        format = PixelFormat.DXT5;
                        blocksize *= 16;
                        break;
                    case FOURCC_ATI1:
                        format = PixelFormat.ATI1N;
                        blocksize *= 8;
                        break;
                    case FOURCC_ATI2:
                        format = PixelFormat.THREEDC;
                        blocksize *= 16;
                        break;
                    case FOURCC_RXGB:
                        format = PixelFormat.RXGB;
                        blocksize *= 16;
                        break;
                    case FOURCC_DOLLARNULL:
                        format = PixelFormat.A16B16G16R16;
                        blocksize = header.width * header.height * header.depth * 8;
                        break;
                    case FOURCC_oNULL:
                        format = PixelFormat.R16F;
                        blocksize = header.width * header.height * header.depth * 2;
                        break;
                    case FOURCC_pNULL:
                        format = PixelFormat.G16R16F;
                        blocksize = header.width * header.height * header.depth * 4;
                        break;
                    case FOURCC_qNULL:
                        format = PixelFormat.A16B16G16R16F;
                        blocksize = header.width * header.height * header.depth * 8;
                        break;
                    case FOURCC_rNULL:
                        format = PixelFormat.R32F;
                        blocksize = header.width * header.height * header.depth * 4;
                        break;
                    case FOURCC_sNULL:
                        format = PixelFormat.G32R32F;
                        blocksize = header.width * header.height * header.depth * 8;
                        break;
                    case FOURCC_tNULL:
                        format = PixelFormat.A32B32G32R32F;
                        blocksize = header.width * header.height * header.depth * 16;
                        break;
                    default:
                        format = PixelFormat.UNKNOWN;
                        blocksize *= 16;
                        break;
                } // switch
            }
            else
            {
                // uncompressed image
                if ((header.pixelformat.flags & DDPF_LUMINANCE) == DDPF_LUMINANCE)
                {
                    format = (header.pixelformat.flags & DDPF_ALPHAPIXELS) == DDPF_ALPHAPIXELS ? PixelFormat.LUMINANCE_ALPHA : PixelFormat.LUMINANCE;
                }
                else
                {
                    format = (header.pixelformat.flags & DDPF_ALPHAPIXELS) == DDPF_ALPHAPIXELS ? PixelFormat.RGBA : PixelFormat.RGB;
                }

                blocksize = (header.width * header.height * header.depth * (header.pixelformat.rgbbitcount >> 3));
            }

            return format;
        }

        #region Helper Methods
        // iCompFormatToBpp
        static uint PixelFormatToBpp(PixelFormat pf, uint rgbbitcount)
        {
            switch (pf)
            {
                case PixelFormat.LUMINANCE:
                case PixelFormat.LUMINANCE_ALPHA:
                case PixelFormat.RGBA:
                case PixelFormat.RGB:
                    return rgbbitcount / 8;

                case PixelFormat.THREEDC:
                case PixelFormat.RXGB:
                    return 3;

                case PixelFormat.ATI1N:
                    return 1;

                case PixelFormat.R16F:
                    return 2;

                case PixelFormat.A16B16G16R16:
                case PixelFormat.A16B16G16R16F:
                case PixelFormat.G32R32F:
                    return 8;

                case PixelFormat.A32B32G32R32F:
                    return 16;

                default:
                    return 4;
            }
        }

        // iCompFormatToBpc
        static uint PixelFormatToBpc(PixelFormat pf)
        {
            switch (pf)
            {
                case PixelFormat.R16F:
                case PixelFormat.G16R16F:
                case PixelFormat.A16B16G16R16F:
                    return 4;
                case PixelFormat.R32F:
                case PixelFormat.G32R32F:
                case PixelFormat.A32B32G32R32F:
                    return 4;
                case PixelFormat.A16B16G16R16:
                    return 2;
                default:
                    return 1;
            }
        }

        static bool Check16BitComponents(DDSHeader header)
        {
            if (header.pixelformat.rgbbitcount != 32)
                return false;
            // a2b10g10r10 format
            if (header.pixelformat.rbitmask == 0x3FF00000 && header.pixelformat.gbitmask == 0x000FFC00 && header.pixelformat.bbitmask == 0x000003FF
                && header.pixelformat.alphabitmask == 0xC0000000)
                return true;
            // a2r10g10b10 format
            else if (header.pixelformat.rbitmask == 0x000003FF && header.pixelformat.gbitmask == 0x000FFC00 && header.pixelformat.bbitmask == 0x3FF00000
                && header.pixelformat.alphabitmask == 0xC0000000)
                return true;

            return false;
        }

        static void CorrectPremult(uint pixnum, ref byte[] buffer)
        {
            for (uint i = 0; i < pixnum; i++)
            {
                byte alpha = buffer[i + 3];
                if (alpha == 0) continue;
                int red = (buffer[i] << 8) / alpha;
                int green = (buffer[i + 1] << 8) / alpha;
                int blue = (buffer[i + 2] << 8) / alpha;

                buffer[i] = (byte)red;
                buffer[i + 1] = (byte)green;
                buffer[i + 2] = (byte)blue;
            }
        }

        static void ComputeMaskParams(uint mask, out int shift1, out int mul, out int shift2)
        {
            shift1 = 0; mul = 1; shift2 = 0;
            while ((mask & 1) == 0)
            {
                mask >>= 1;
                shift1++;
            }
            uint bc = 0;
            while ((mask & (1 << (int)bc)) != 0) bc++;
            while ((mask * mul) < 255)
                mul = (mul << (int)bc) + 1;
            mask *= (uint)mul;

            while ((mask & ~0xff) != 0)
            {
                mask >>= 1;
                shift2++;
            }
        }

        static unsafe void DxtcReadColors(byte* data, ref Colour8888[] op)
        {
            byte b0 = (byte)(data[0] & 0x1F);
            byte g0 = (byte)(((data[0] & 0xE0) >> 5) | ((data[1] & 0x7) << 3));
            byte r0 = (byte)((data[1] & 0xF8) >> 3);

            byte b1 = (byte)(data[2] & 0x1F);
            byte g1 = (byte)(((data[2] & 0xE0) >> 5) | ((data[3] & 0x7) << 3));
            byte r1 = (byte)((data[3] & 0xF8) >> 3);

            op[0].red = (byte)(r0 << 3 | r0 >> 2);
            op[0].green = (byte)(g0 << 2 | g0 >> 3);
            op[0].blue = (byte)(b0 << 3 | b0 >> 2);

            op[1].red = (byte)(r1 << 3 | r1 >> 2);
            op[1].green = (byte)(g1 << 2 | g1 >> 3);
            op[1].blue = (byte)(b1 << 3 | b1 >> 2);
        }

        static void DxtcReadColor(ushort data, ref Colour8888 op)
        {
            byte b = (byte)(data & 0x1f);
            byte g = (byte)((data & 0x7E0) >> 5);
            byte r = (byte)((data & 0xF800) >> 11);

            op.red = (byte)(r << 3 | r >> 2);
            op.green = (byte)(g << 2 | g >> 3);
            op.blue = (byte)(b << 3 | r >> 2);
        }

        static unsafe void DxtcReadColors(byte* data, ref Colour565 color_0, ref Colour565 color_1)
        {
            color_0.blue = (byte)(data[0] & 0x1F);
            color_0.green = (byte)(((data[0] & 0xE0) >> 5) | ((data[1] & 0x7) << 3));
            color_0.red = (byte)((data[1] & 0xF8) >> 3);

            color_1.blue = (byte)(data[2] & 0x1F);
            color_1.green = (byte)(((data[2] & 0xE0) >> 5) | ((data[3] & 0x7) << 3));
            color_1.red = (byte)((data[3] & 0xF8) >> 3);
        }

        static void GetBitsFromMask(uint mask, out uint shiftLeft, out uint shiftRight)
        {
            if (mask == 0)
            {
                shiftLeft = shiftRight = 0;
                return;
            }

            uint temp = mask;
            uint i;
            for (i = 0; i < 32; i++, temp >>= 1)
            {
                if ((temp & 1) != 0)
                    break;
            }
            shiftRight = i;

            // Temp is preserved, so use it again:
            for (i = 0; i < 8; i++, temp >>= 1)
            {
                if ((temp & 1) == 0)
                    break;
            }
            shiftLeft = 8 - i;
        }

        // This function simply counts how many contiguous bits are in the mask.
        static uint CountBitsFromMask(uint mask)
        {
            uint i, testBit = 0x01, count = 0;
            bool foundBit = false;

            for (i = 0; i < 32; i++, testBit <<= 1)
            {
                if ((mask & testBit) != 0)
                {
                    if (!foundBit)
                        foundBit = true;
                    count++;
                }
                else if (foundBit)
                    return count;
            }

            return count;
        }

        static uint HalfToFloat(ushort y)
        {
            int s = (y >> 15) & 0x00000001;
            int e = (y >> 10) & 0x0000001f;
            int m = y & 0x000003ff;

            if (e == 0)
            {
                if (m == 0)
                {
                    // Plus or minus zero
                    return (uint)(s << 31);
                }
                else
                {
                    // Denormalized number -- renormalize it
                    while ((m & 0x00000400) == 0)
                    {
                        m <<= 1;
                        e -= 1;
                    }

                    e += 1;
                    m &= ~0x00000400;
                }
            }
            else if (e == 31)
            {
                if (m == 0)
                {
                    // Positive or negative infinity
                    return (uint)((s << 31) | 0x7f800000);
                }
                else
                {
                    // Nan -- preserve sign and significand bits
                    return (uint)((s << 31) | 0x7f800000 | (m << 13));
                }
            }

            // Normalized number
            e = e + (127 - 15);
            m = m << 13;

            // Assemble s, e and m.
            return (uint)((s << 31) | (e << 23) | m);
        }

        static unsafe void ConvFloat16ToFloat32(uint* dest, ushort* src, uint size)
        {
            uint i;
            for (i = 0; i < size; ++i, ++dest, ++src)
            {
                //float: 1 sign bit, 8 exponent bits, 23 mantissa bits
                //half: 1 sign bit, 5 exponent bits, 10 mantissa bits
                *dest = HalfToFloat(*src);
            }
        }

        static unsafe void ConvG16R16ToFloat32(uint* dest, ushort* src, uint size)
        {
            uint i;
            for (i = 0; i < size; i += 3)
            {
                //float: 1 sign bit, 8 exponent bits, 23 mantissa bits
                //half: 1 sign bit, 5 exponent bits, 10 mantissa bits
                *dest++ = HalfToFloat(*src++);
                *dest++ = HalfToFloat(*src++);
                *((float*)dest++) = 1.0f;
            }
        }

        static unsafe void ConvR16ToFloat32(uint* dest, ushort* src, uint size)
        {
            uint i;
            for (i = 0; i < size; i += 3)
            {
                //float: 1 sign bit, 8 exponent bits, 23 mantissa bits
                //half: 1 sign bit, 5 exponent bits, 10 mantissa bits
                *dest++ = HalfToFloat(*src++);
                *((float*)dest++) = 1.0f;
                *((float*)dest++) = 1.0f;
            }
        }
        #endregion

        // @return RGBA byte[width*height*4]
        public static byte[] DecompressData(int width, int height, byte[] data, PixelFormat pixelFormat)
        {
            var header = new DDSHeader
            {
                width = (uint)width,
                height = (uint)height,
                depth = 1
            };
            return DecompressData(header, data, pixelFormat);
        }

        // @return RGBA byte[width*height*4]
        public static byte[] DecompressData(in DDSHeader header, byte[] data, PixelFormat pixelFormat)
        {
            //System.Diagnostics.Debug.WriteLine(pixelFormat);
            switch (pixelFormat)
            {
                case PixelFormat.RGBA:
                    return DecompressRGBA(header, data, pixelFormat);
                case PixelFormat.RGB:
                    return DecompressRGB(header, data, pixelFormat);
                case PixelFormat.LUMINANCE:
                case PixelFormat.LUMINANCE_ALPHA:
                    return DecompressLum(header, data, pixelFormat);
                case PixelFormat.DXT1:
                    return DecompressDXT1(header, data, pixelFormat);
                case PixelFormat.DXT2:
                    return DecompressDXT2(header, data, pixelFormat);
                case PixelFormat.DXT3:
                    return DecompressDXT3(header, data, pixelFormat);
                case PixelFormat.DXT4:
                    return DecompressDXT4(header, data, pixelFormat);
                case PixelFormat.DXT5:
                    return DecompressDXT5(header, data, pixelFormat);
                case PixelFormat.THREEDC:
                    return Decompress3Dc(header, data, pixelFormat);
                case PixelFormat.ATI1N:
                    return DecompressAti1n(header, data, pixelFormat);
                case PixelFormat.RXGB:
                    return DecompressRXGB(header, data, pixelFormat);
                case PixelFormat.R16F:
                case PixelFormat.G16R16F:
                case PixelFormat.A16B16G16R16F:
                case PixelFormat.R32F:
                case PixelFormat.G32R32F:
                case PixelFormat.A32B32G32R32F:
                    return DecompressFloat(header, data, pixelFormat);
                default:
                    throw new UnknownFileFormatException();
            }
        }

        #region Decompress Methods

        static unsafe byte[] DecompressDXT1(in DDSHeader header, byte[] data, PixelFormat pixelFormat)
        {
            // allocate bitmap
            int bpp = (int)(PixelFormatToBpp(pixelFormat, header.pixelformat.rgbbitcount));
            int bps = (int)(header.width * bpp * PixelFormatToBpc(pixelFormat));
            int sizeofplane = (int)(bps * header.height);
            int width = (int)header.width;
            int height = (int)header.height;
            int depth = (int)header.depth;

            // DXT1 decompressor
            byte[] rawData = new byte[depth * sizeofplane + height * bps + width * bpp];

            Colour8888[] colours = new Colour8888[4];
            colours[0].alpha = 0xFF;
            colours[1].alpha = 0xFF;
            colours[2].alpha = 0xFF;

            fixed (byte* bytePtr = data)
            {
                byte* temp = bytePtr;
                for (int z = 0; z < depth; z++)
                {
                    for (int y = 0; y < height; y += 4)
                    {
                        for (int x = 0; x < width; x += 4)
                        {
                            ushort colour0 = *((ushort*)temp);
                            ushort colour1 = *((ushort*)(temp + 2));
                            DxtcReadColor(colour0, ref colours[0]);
                            DxtcReadColor(colour1, ref colours[1]);

                            uint bitmask = ((uint*)temp)[1];
                            temp += 8;

                            if (colour0 > colour1)
                            {
                                // Four-color block: derive the other two colors.
                                // 00 = color_0, 01 = color_1, 10 = color_2, 11 = color_3
                                // These 2-bit codes correspond to the 2-bit fields
                                // stored in the 64-bit block.
                                colours[2].blue = (byte)((2 * colours[0].blue + colours[1].blue + 1) / 3);
                                colours[2].green = (byte)((2 * colours[0].green + colours[1].green + 1) / 3);
                                colours[2].red = (byte)((2 * colours[0].red + colours[1].red + 1) / 3);
                                //colours[2].alpha = 0xFF;

                                colours[3].blue = (byte)((colours[0].blue + 2 * colours[1].blue + 1) / 3);
                                colours[3].green = (byte)((colours[0].green + 2 * colours[1].green + 1) / 3);
                                colours[3].red = (byte)((colours[0].red + 2 * colours[1].red + 1) / 3);
                                colours[3].alpha = 0xFF;
                            }
                            else
                            {
                                // Three-color block: derive the other color.
                                // 00 = color_0,  01 = color_1,  10 = color_2,
                                // 11 = transparent.
                                // These 2-bit codes correspond to the 2-bit fields 
                                // stored in the 64-bit block. 
                                colours[2].blue = (byte)((colours[0].blue + colours[1].blue) / 2);
                                colours[2].green = (byte)((colours[0].green + colours[1].green) / 2);
                                colours[2].red = (byte)((colours[0].red + colours[1].red) / 2);
                                //colours[2].alpha = 0xFF;

                                colours[3].blue = (byte)((colours[0].blue + 2 * colours[1].blue + 1) / 3);
                                colours[3].green = (byte)((colours[0].green + 2 * colours[1].green + 1) / 3);
                                colours[3].red = (byte)((colours[0].red + 2 * colours[1].red + 1) / 3);
                                colours[3].alpha = 0x00;
                            }

                            for (int j = 0, k = 0; j < 4; j++)
                            {
                                for (int i = 0; i < 4; i++, k++)
                                {
                                    int select = (int)((bitmask & (0x03 << k * 2)) >> k * 2);
                                    Colour8888 col = colours[select];
                                    if (((x + i) < width) && ((y + j) < height))
                                    {
                                        uint offset = (uint)(z * sizeofplane + (y + j) * bps + (x + i) * bpp);
                                        rawData[offset + 0] = col.red;
                                        rawData[offset + 1] = col.green;
                                        rawData[offset + 2] = col.blue;
                                        rawData[offset + 3] = col.alpha;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return rawData;
        }

        static byte[] DecompressDXT2(in DDSHeader header, byte[] data, PixelFormat pixelFormat)
        {
            // allocate bitmap
            int width = (int)header.width;
            int height = (int)header.height;
            int depth = (int)header.depth;

            // Can do color & alpha same as dxt3, but color is pre-multiplied
            // so the result will be wrong unless corrected.
            byte[] rawData = DecompressDXT3(header, data, pixelFormat);
            CorrectPremult((uint)(width * height * depth), ref rawData);

            return rawData;
        }

        static unsafe byte[] DecompressDXT3(in DDSHeader header, byte[] data, PixelFormat pixelFormat)
        {
            // allocate bitmap
            int bpp = (int)(PixelFormatToBpp(pixelFormat, header.pixelformat.rgbbitcount));
            int bps = (int)(header.width * bpp * PixelFormatToBpc(pixelFormat));
            int sizeofplane = (int)(bps * header.height);
            int width = (int)header.width;
            int height = (int)header.height;
            int depth = (int)header.depth;

            // DXT3 decompressor
            byte[] rawData = new byte[depth * sizeofplane + height * bps + width * bpp];
            Colour8888[] colours = new Colour8888[4];

            fixed (byte* bytePtr = data)
            {
                byte* temp = bytePtr;
                for (int z = 0; z < depth; z++)
                {
                    for (int y = 0; y < height; y += 4)
                    {
                        for (int x = 0; x < width; x += 4)
                        {
                            byte* alpha = temp;
                            temp += 8;

                            DxtcReadColors(temp, ref colours);
                            temp += 4;

                            uint bitmask = ((uint*)temp)[1];
                            temp += 4;

                            // Four-color block: derive the other two colors.
                            // 00 = color_0, 01 = color_1, 10 = color_2, 11	= color_3
                            // These 2-bit codes correspond to the 2-bit fields
                            // stored in the 64-bit block.
                            colours[2].blue = (byte)((2 * colours[0].blue + colours[1].blue + 1) / 3);
                            colours[2].green = (byte)((2 * colours[0].green + colours[1].green + 1) / 3);
                            colours[2].red = (byte)((2 * colours[0].red + colours[1].red + 1) / 3);
                            //colours[2].alpha = 0xFF;

                            colours[3].blue = (byte)((colours[0].blue + 2 * colours[1].blue + 1) / 3);
                            colours[3].green = (byte)((colours[0].green + 2 * colours[1].green + 1) / 3);
                            colours[3].red = (byte)((colours[0].red + 2 * colours[1].red + 1) / 3);
                            //colours[3].alpha = 0xFF;

                            for (int j = 0, k = 0; j < 4; j++)
                            {
                                for (int i = 0; i < 4; k++, i++)
                                {
                                    int select = (int)((bitmask & (0x03 << k * 2)) >> k * 2);

                                    if (((x + i) < width) && ((y + j) < height))
                                    {
                                        uint offset = (uint)(z * sizeofplane + (y + j) * bps + (x + i) * bpp);
                                        rawData[offset + 0] = colours[select].red;
                                        rawData[offset + 1] = colours[select].green;
                                        rawData[offset + 2] = colours[select].blue;
                                    }
                                }
                            }

                            for (int j = 0; j < 4; j++)
                            {
                                //ushort word = (ushort)(alpha[2 * j] + 256 * alpha[2 * j + 1]);
                                ushort word = (ushort)(alpha[2 * j] | (alpha[2 * j + 1] << 8)); 
                                for (int i = 0; i < 4; i++)
                                {
                                    if (((x + i) < width) && ((y + j) < height))
                                    {
                                        uint offset = (uint)(z * sizeofplane + (y + j) * bps + (x + i) * bpp + 3);
                                        rawData[offset] = (byte)(word & 0x0F);
                                        rawData[offset] = (byte)(rawData[offset] | (rawData[offset] << 4));
                                    }
                                    word >>= 4;
                                }
                            }
                        }
                    }
                }
            }
            return rawData;
        }

        static byte[] DecompressDXT4(in DDSHeader header, byte[] data, PixelFormat pixelFormat)
        {
            // allocate bitmap
            int width = (int)header.width;
            int height = (int)header.height;
            int depth = (int)header.depth;

            // Can do color & alpha same as dxt5, but color is pre-multiplied
            // so the result will be wrong unless corrected.
            byte[] rawData = DecompressDXT5(header, data, pixelFormat);
            CorrectPremult((uint)(width * height * depth), ref rawData);

            return rawData;
        }

        static unsafe byte[] DecompressDXT5(in DDSHeader header, byte[] data, PixelFormat pixelFormat)
        {
            // allocate bitmap
            int bpp = (int)(PixelFormatToBpp(pixelFormat, header.pixelformat.rgbbitcount));
            int bps = (int)(header.width * bpp * PixelFormatToBpc(pixelFormat));
            int sizeofplane = (int)(bps * header.height);
            int width = (int)header.width;
            int height = (int)header.height;
            int depth = (int)header.depth;

            var rawData = new byte[depth * sizeofplane + height * bps + width * bpp];
            var colours = new Colour8888[4];
            var alphas  = new ushort[8];

            fixed (byte* bytePtr = data)
            {
                byte* temp = bytePtr;
                for (int z = 0; z < depth; z++)
                {
                    for (int y = 0; y < height; y += 4)
                    {
                        for (int x = 0; x < width; x += 4)
                        {
                            if (y >= height || x >= width)
                                break;

                            alphas[0] = temp[0];
                            alphas[1] = temp[1];
                            byte* alphamask = (temp + 2);
                            temp += 8;

                            DxtcReadColors(temp, ref colours);
                            uint bitmask = ((uint*)temp)[1];
                            temp += 8;

                            // Four-color block: derive the other two colors.
                            // 00 = color_0, 01 = color_1, 10 = color_2, 11	= color_3
                            // These 2-bit codes correspond to the 2-bit fields
                            // stored in the 64-bit block.
                            colours[2].blue  = (byte)((2 * colours[0].blue  + colours[1].blue  + 1) / 3);
                            colours[2].green = (byte)((2 * colours[0].green + colours[1].green + 1) / 3);
                            colours[2].red   = (byte)((2 * colours[0].red   + colours[1].red   + 1) / 3);
                            //colours[2].alpha = 0xFF;

                            colours[3].blue  = (byte)((colours[0].blue  + 2 * colours[1].blue  + 1) / 3);
                            colours[3].green = (byte)((colours[0].green + 2 * colours[1].green + 1) / 3);
                            colours[3].red   = (byte)((colours[0].red   + 2 * colours[1].red   + 1) / 3);
                            //colours[3].alpha = 0xFF;

                            int k = 0;
                            for (int j = 0; j < 4; j++)
                            {
                                for (int i = 0; i < 4; k++, i++)
                                {
                                    int select = (int)((bitmask & (0x03 << k * 2)) >> k * 2);
                                    Colour8888 col = colours[select];
                                    // only put pixels out < width or height
                                    if (((x + i) < width) && ((y + j) < height))
                                    {
                                        uint offset = (uint)(z * sizeofplane + (y + j) * bps + (x + i) * bpp);
                                        rawData[offset] = col.red;
                                        rawData[offset + 1] = col.green;
                                        rawData[offset + 2] = col.blue;
                                    }
                                }
                            }

                            // 8-alpha or 6-alpha block?
                            if (alphas[0] > alphas[1])
                            {
                                // 8-alpha block:  derive the other six alphas.
                                // Bit code 000 = alpha_0, 001 = alpha_1, others are interpolated.
                                alphas[2] = (ushort)((6 * alphas[0] + 1 * alphas[1] + 3) / 7); // bit code 010
                                alphas[3] = (ushort)((5 * alphas[0] + 2 * alphas[1] + 3) / 7); // bit code 011
                                alphas[4] = (ushort)((4 * alphas[0] + 3 * alphas[1] + 3) / 7); // bit code 100
                                alphas[5] = (ushort)((3 * alphas[0] + 4 * alphas[1] + 3) / 7); // bit code 101
                                alphas[6] = (ushort)((2 * alphas[0] + 5 * alphas[1] + 3) / 7); // bit code 110
                                alphas[7] = (ushort)((1 * alphas[0] + 6 * alphas[1] + 3) / 7); // bit code 111
                            }
                            else
                            {
                                // 6-alpha block.
                                // Bit code 000 = alpha_0, 001 = alpha_1, others are interpolated.
                                alphas[2] = (ushort)((4 * alphas[0] + 1 * alphas[1] + 2) / 5); // Bit code 010
                                alphas[3] = (ushort)((3 * alphas[0] + 2 * alphas[1] + 2) / 5); // Bit code 011
                                alphas[4] = (ushort)((2 * alphas[0] + 3 * alphas[1] + 2) / 5); // Bit code 100
                                alphas[5] = (ushort)((1 * alphas[0] + 4 * alphas[1] + 2) / 5); // Bit code 101
                                alphas[6] = 0x00; // Bit code 110
                                alphas[7] = 0xFF; // Bit code 111
                            }

                            // Note: Have to separate the next two loops,
                            // it operates on a 6-byte system.

                            // First three bytes
                            //uint bits = (uint)(alphamask[0]);
                            uint bits = (uint)((alphamask[0]) | (alphamask[1] << 8) | (alphamask[2] << 16));
                            for (int j = 0; j < 2; j++)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    // only put pixels out < width or height
                                    if (((x + i) < width) && ((y + j) < height))
                                    {
                                        uint offset = (uint)(z * sizeofplane + (y + j) * bps + (x + i) * bpp + 3);
                                        rawData[offset] = (byte)alphas[bits & 0x07];
                                    }
                                    bits >>= 3;
                                }
                            }

                            // Last three bytes
                            //bits = (uint)(alphamask[3]);
                            bits = (uint)((alphamask[3]) | (alphamask[4] << 8) | (alphamask[5] << 16));
                            for (int j = 2; j < 4; j++)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    // only put pixels out < width or height
                                    if (((x + i) < width) && ((y + j) < height))
                                    {
                                        uint offset = (uint)(z * sizeofplane + (y + j) * bps + (x + i) * bpp + 3);
                                        rawData[offset] = (byte)alphas[bits & 0x07];
                                    }
                                    bits >>= 3;
                                }
                            }
                        }
                    }
                }
            }

            return rawData;
        }

        static unsafe byte[] DecompressRGB(in DDSHeader header, byte[] data, PixelFormat pixelFormat)
        {
            // allocate bitmap
            int bpp = (int)(PixelFormatToBpp(pixelFormat, header.pixelformat.rgbbitcount));
            int bps = (int)(header.width * bpp * PixelFormatToBpc(pixelFormat));
            int sizeofplane = (int)(bps * header.height);
            int width = (int)header.width;
            int height = (int)header.height;
            int depth = (int)header.depth;

            byte[] rawData = new byte[depth * sizeofplane + height * bps + width * bpp];

            uint valMask = (uint)((header.pixelformat.rgbbitcount == 32) ? ~0 : (1 << (int)header.pixelformat.rgbbitcount) - 1);
            uint pixSize = (uint)(((int)header.pixelformat.rgbbitcount + 7) / 8);
            ComputeMaskParams(header.pixelformat.rbitmask, out int rShift1, out int rMul, out int rShift2);
            ComputeMaskParams(header.pixelformat.gbitmask, out int gShift1, out int gMul, out int gShift2);
            ComputeMaskParams(header.pixelformat.bbitmask, out int bShift1, out int bMul, out int bShift2);

            int offset = 0;
            int pixnum = width * height * depth;
            fixed (byte* bytePtr = data)
            {
                byte* temp = bytePtr;
                while (pixnum-- > 0)
                {
                    uint px = *((uint*)temp) & valMask;
                    temp += pixSize;
                    uint pxc = px & header.pixelformat.rbitmask;
                    rawData[offset + 0] = (byte)(((pxc >> rShift1) * rMul) >> rShift2);
                    pxc = px & header.pixelformat.gbitmask;
                    rawData[offset + 1] = (byte)(((pxc >> gShift1) * gMul) >> gShift2);
                    pxc = px & header.pixelformat.bbitmask;
                    rawData[offset + 2] = (byte)(((pxc >> bShift1) * bMul) >> bShift2);
                    rawData[offset + 3] = 0xff;
                    offset += 4;
                }
            }
            return rawData;
        }

        static unsafe byte[] DecompressRGBA(in DDSHeader header, byte[] data, PixelFormat pixelFormat)
        {
            // allocate bitmap
            int bpp = (int)(PixelFormatToBpp(pixelFormat, header.pixelformat.rgbbitcount));
            int bps = (int)(header.width * bpp * PixelFormatToBpc(pixelFormat));
            int sizeofplane = (int)(bps * header.height);
            int width = (int)header.width;
            int height = (int)header.height;
            int depth = (int)header.depth;

            var rawData = new byte[depth * sizeofplane + height * bps + width * bpp];

            uint valMask = (uint)((header.pixelformat.rgbbitcount == 32) ? ~0 : (1 << (int)header.pixelformat.rgbbitcount) - 1);
            // Funny x86s, make 1 << 32 == 1
            uint pixSize = (header.pixelformat.rgbbitcount + 7) / 8;
            ComputeMaskParams(header.pixelformat.rbitmask, out int rShift1, out int rMul, out int rShift2);
            ComputeMaskParams(header.pixelformat.gbitmask, out int gShift1, out int gMul, out int gShift2);
            ComputeMaskParams(header.pixelformat.bbitmask, out int bShift1, out int bMul, out int bShift2);
            ComputeMaskParams(header.pixelformat.alphabitmask, out int aShift1, out int aMul, out int aShift2);

            int offset = 0;
            int pixnum = width * height * depth;
            fixed (byte* bytePtr = data)
            {
                byte* temp = bytePtr;

                while (pixnum-- > 0)
                {
                    uint px = *((uint*)temp) & valMask;
                    temp += pixSize;
                    uint pxc = px & header.pixelformat.rbitmask;
                    rawData[offset + 0] = (byte)(((pxc >> rShift1) * rMul) >> rShift2);
                    pxc = px & header.pixelformat.gbitmask;
                    rawData[offset + 1] = (byte)(((pxc >> gShift1) * gMul) >> gShift2);
                    pxc = px & header.pixelformat.bbitmask;
                    rawData[offset + 2] = (byte)(((pxc >> bShift1) * bMul) >> bShift2);
                    pxc = px & header.pixelformat.alphabitmask;
                    rawData[offset + 3] = (byte)(((pxc >> aShift1) * aMul) >> aShift2);
                    offset += 4;
                }
            }
            return rawData;
        }

        static unsafe byte[] Decompress3Dc(in DDSHeader header, byte[] data, PixelFormat pixelFormat)
        {
            // allocate bitmap
            int bpp = (int)(PixelFormatToBpp(pixelFormat, header.pixelformat.rgbbitcount));
            int bps = (int)(header.width * bpp * PixelFormatToBpc(pixelFormat));
            int sizeofplane = (int)(bps * header.height);
            int width = (int)header.width;
            int height = (int)header.height;
            int depth = (int)header.depth;

            byte[] rawData = new byte[depth * sizeofplane + height * bps + width * bpp];
            byte[] yColours = new byte[8];
            byte[] xColours = new byte[8];

            int offset = 0;
            fixed (byte* bytePtr = data)
            {
                byte* temp = bytePtr;
                for (int z = 0; z < depth; z++)
                {
                    for (int y = 0; y < height; y += 4)
                    {
                        for (int x = 0; x < width; x += 4)
                        {
                            byte* temp2 = temp + 8;

                            //Read Y palette
                            int t1 = yColours[0] = temp[0];
                            int t2 = yColours[1] = temp[1];
                            temp += 2;
                            if (t1 > t2)
                                for (int i = 2; i < 8; ++i)
                                    yColours[i] = (byte)(t1 + ((t2 - t1) * (i - 1)) / 7);
                            else
                            {
                                for (int i = 2; i < 6; ++i)
                                    yColours[i] = (byte)(t1 + ((t2 - t1) * (i - 1)) / 5);
                                yColours[6] = 0;
                                yColours[7] = 255;
                            }

                            // Read X palette
                            t1 = xColours[0] = temp2[0];
                            t2 = xColours[1] = temp2[1];
                            temp2 += 2;
                            if (t1 > t2)
                                for (int i = 2; i < 8; ++i)
                                    xColours[i] = (byte)(t1 + ((t2 - t1) * (i - 1)) / 7);
                            else
                            {
                                for (int i = 2; i < 6; ++i)
                                    xColours[i] = (byte)(t1 + ((t2 - t1) * (i - 1)) / 5);
                                xColours[6] = 0;
                                xColours[7] = 255;
                            }

                            //decompress pixel data
                            int currentOffset = offset;
                            for (int k = 0; k < 4; k += 2)
                            {
                                // First three bytes
                                uint bitmask = ((uint)(temp[0]) << 0) | ((uint)(temp[1]) << 8) | ((uint)(temp[2]) << 16);
                                uint bitmask2 = ((uint)(temp2[0]) << 0) | ((uint)(temp2[1]) << 8) | ((uint)(temp2[2]) << 16);
                                for (int j = 0; j < 2; j++)
                                {
                                    // only put pixels out < height
                                    if ((y + k + j) < height)
                                    {
                                        for (int i = 0; i < 4; i++)
                                        {
                                            // only put pixels out < width
                                            if (((x + i) < width))
                                            {
                                                byte tx, ty;

                                                t1 = currentOffset + (x + i) * 3;
                                                rawData[t1 + 1] = ty = yColours[bitmask & 0x07];
                                                rawData[t1 + 0] = tx = xColours[bitmask2 & 0x07];

                                                //calculate b (z) component ((r/255)^2 + (g/255)^2 + (b/255)^2 = 1
                                                int t = 127 * 128 - (tx - 127) * (tx - 128) - (ty - 127) * (ty - 128);
                                                if (t > 0)
                                                    rawData[t1 + 2] = (byte)(Math.Sqrt(t) + 128);
                                                else
                                                    rawData[t1 + 2] = 0x7F;
                                            }
                                            bitmask >>= 3;
                                            bitmask2 >>= 3;
                                        }
                                        currentOffset += bps;
                                    }
                                }
                                temp += 3;
                                temp2 += 3;
                            }

                            //skip bytes that were read via Temp2
                            temp += 8;
                        }
                        offset += bps * 4;
                    }
                }
            }

            return rawData;
        }

        static unsafe byte[] DecompressAti1n(in DDSHeader header, byte[] data, PixelFormat pixelFormat)
        {
            // allocate bitmap
            int bpp = (int)(PixelFormatToBpp(pixelFormat, header.pixelformat.rgbbitcount));
            int bps = (int)(header.width * bpp * PixelFormatToBpc(pixelFormat));
            int sizeofplane = (int)(bps * header.height);
            int width = (int)header.width;
            int height = (int)header.height;
            int depth = (int)header.depth;

            byte[] rawData = new byte[depth * sizeofplane + height * bps + width * bpp];
            byte[] colours = new byte[8];

            uint offset = 0;
            fixed (byte* bytePtr = data)
            {
                byte* temp = bytePtr;
                for (int z = 0; z < depth; z++)
                {
                    for (int y = 0; y < height; y += 4)
                    {
                        for (int x = 0; x < width; x += 4)
                        {
                            //Read palette
                            int t1 = colours[0] = temp[0];
                            int t2 = colours[1] = temp[1];
                            temp += 2;
                            if (t1 > t2)
                                for (int i = 2; i < 8; ++i)
                                    colours[i] = (byte)(t1 + ((t2 - t1) * (i - 1)) / 7);
                            else
                            {
                                for (int i = 2; i < 6; ++i)
                                    colours[i] = (byte)(t1 + ((t2 - t1) * (i - 1)) / 5);
                                colours[6] = 0;
                                colours[7] = 255;
                            }

                            //decompress pixel data
                            uint currOffset = offset;
                            for (int k = 0; k < 4; k += 2)
                            {
                                // First three bytes
                                uint bitmask = ((uint)(temp[0]) << 0) | ((uint)(temp[1]) << 8) | ((uint)(temp[2]) << 16);
                                for (int j = 0; j < 2; j++)
                                {
                                    // only put pixels out < height
                                    if ((y + k + j) < height)
                                    {
                                        for (int i = 0; i < 4; i++)
                                        {
                                            // only put pixels out < width
                                            if (((x + i) < width))
                                            {
                                                t1 = (int)(currOffset + (x + i));
                                                rawData[t1] = colours[bitmask & 0x07];
                                            }
                                            bitmask >>= 3;
                                        }
                                        currOffset += (uint)bps;
                                    }
                                }
                                temp += 3;
                            }
                        }
                        offset += (uint)(bps * 4);
                    }
                }
            }
            return rawData;
        }

        static unsafe byte[] DecompressLum(in DDSHeader header, byte[] data, PixelFormat pixelFormat)
        {
            // allocate bitmap
            int bpp = (int)(PixelFormatToBpp(pixelFormat, header.pixelformat.rgbbitcount));
            int bps = (int)(header.width * bpp * PixelFormatToBpc(pixelFormat));
            int sizeofplane = (int)(bps * header.height);
            int width = (int)header.width;
            int height = (int)header.height;
            int depth = (int)header.depth;

            byte[] rawData = new byte[depth * sizeofplane + height * bps + width * bpp];

            ComputeMaskParams(header.pixelformat.rbitmask, out int lShift1, out int lMul, out int lShift2);

            int offset = 0;
            int pixnum = width * height * depth;
            fixed (byte* bytePtr = data)
            {
                byte* temp = bytePtr;
                while (pixnum-- > 0)
                {
                    byte px = *(temp++);
                    rawData[offset + 0] = (byte)(((px >> lShift1) * lMul) >> lShift2);
                    rawData[offset + 1] = (byte)(((px >> lShift1) * lMul) >> lShift2);
                    rawData[offset + 2] = (byte)(((px >> lShift1) * lMul) >> lShift2);
                    rawData[offset + 3] = (byte)(((px >> lShift1) * lMul) >> lShift2);
                    offset += 4;
                }
            }
            return rawData;
        }

        static unsafe byte[] DecompressRXGB(in DDSHeader header, byte[] data, PixelFormat pixelFormat)
        {
            // allocate bitmap
            int bpp = (int)(PixelFormatToBpp(pixelFormat, header.pixelformat.rgbbitcount));
            int bps = (int)(header.width * bpp * PixelFormatToBpc(pixelFormat));
            int sizeofplane = (int)(bps * header.height);
            int width = (int)header.width;
            int height = (int)header.height;
            int depth = (int)header.depth;

            byte[] rawData = new byte[depth * sizeofplane + height * bps + width * bpp];

            Colour565 color_0 = new Colour565();
            Colour565 color_1 = new Colour565();
	        Colour8888[]	colours = new Colour8888[4];
	        byte[] alphas = new byte[8];

            fixed (byte* bytePtr = data)
            {
                byte* temp = bytePtr;
                for (int z = 0; z < depth; z++)
                {
                    for (int y = 0; y < height; y += 4)
                    {
                        for (int x = 0; x < width; x += 4)
                        {
                            if (y >= height || x >= width)
                                break;
                            alphas[0] = temp[0];
                            alphas[1] = temp[1];
                            byte* alphamask = temp + 2;
                            temp += 8;

                            DxtcReadColors(temp, ref color_0, ref color_1);
                            temp += 4;

                            uint bitmask = ((uint*)temp)[1];
                            temp += 4;

                            colours[0].red = (byte)(color_0.red << 3);
                            colours[0].green = (byte)(color_0.green << 2);
                            colours[0].blue = (byte)(color_0.blue << 3);
                            colours[0].alpha = 0xFF;

                            colours[1].red = (byte)(color_1.red << 3);
                            colours[1].green = (byte)(color_1.green << 2);
                            colours[1].blue = (byte)(color_1.blue << 3);
                            colours[1].alpha = 0xFF;

                            // Four-color block: derive the other two colors.    
                            // 00 = color_0, 01 = color_1, 10 = color_2, 11 = color_3
                            // These 2-bit codes correspond to the 2-bit fields 
                            // stored in the 64-bit block.
                            colours[2].blue = (byte)((2 * colours[0].blue + colours[1].blue + 1) / 3);
                            colours[2].green = (byte)((2 * colours[0].green + colours[1].green + 1) / 3);
                            colours[2].red = (byte)((2 * colours[0].red + colours[1].red + 1) / 3);
                            colours[2].alpha = 0xFF;

                            colours[3].blue = (byte)((colours[0].blue + 2 * colours[1].blue + 1) / 3);
                            colours[3].green = (byte)((colours[0].green + 2 * colours[1].green + 1) / 3);
                            colours[3].red = (byte)((colours[0].red + 2 * colours[1].red + 1) / 3);
                            colours[3].alpha = 0xFF;

                            int k = 0;
                            for (int j = 0; j < 4; j++)
                            {
                                for (int i = 0; i < 4; i++, k++)
                                {
                                    int select = (int)((bitmask & (0x03 << k * 2)) >> k * 2);
                                    Colour8888 col = colours[select];

                                    // only put pixels out < width or height
                                    if (((x + i) < width) && ((y + j) < height))
                                    {
                                        uint offset = (uint)(z * sizeofplane + (y + j) * bps + (x + i) * bpp);
                                        rawData[offset + 0] = col.red;
                                        rawData[offset + 1] = col.green;
                                        rawData[offset + 2] = col.blue;
                                    }
                                }
                            }

                            // 8-alpha or 6-alpha block?    
                            if (alphas[0] > alphas[1])
                            {
                                // 8-alpha block:  derive the other six alphas.    
                                // Bit code 000 = alpha_0, 001 = alpha_1, others are interpolated.
                                alphas[2] = (byte)((6 * alphas[0] + 1 * alphas[1] + 3) / 7);	// bit code 010
                                alphas[3] = (byte)((5 * alphas[0] + 2 * alphas[1] + 3) / 7);	// bit code 011
                                alphas[4] = (byte)((4 * alphas[0] + 3 * alphas[1] + 3) / 7);	// bit code 100
                                alphas[5] = (byte)((3 * alphas[0] + 4 * alphas[1] + 3) / 7);	// bit code 101
                                alphas[6] = (byte)((2 * alphas[0] + 5 * alphas[1] + 3) / 7);	// bit code 110
                                alphas[7] = (byte)((1 * alphas[0] + 6 * alphas[1] + 3) / 7);	// bit code 111
                            }
                            else
                            {
                                // 6-alpha block.
                                // Bit code 000 = alpha_0, 001 = alpha_1, others are interpolated.
                                alphas[2] = (byte)((4 * alphas[0] + 1 * alphas[1] + 2) / 5);	// Bit code 010
                                alphas[3] = (byte)((3 * alphas[0] + 2 * alphas[1] + 2) / 5);	// Bit code 011
                                alphas[4] = (byte)((2 * alphas[0] + 3 * alphas[1] + 2) / 5);	// Bit code 100
                                alphas[5] = (byte)((1 * alphas[0] + 4 * alphas[1] + 2) / 5);	// Bit code 101
                                alphas[6] = 0x00;										// Bit code 110
                                alphas[7] = 0xFF;										// Bit code 111
                            }

                            // Note: Have to separate the next two loops,
                            //	it operates on a 6-byte system.
                            // First three bytes
                            uint bits = *((uint*)alphamask);
                            for (int j = 0; j < 2; j++)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    // only put pixels out < width or height
                                    if (((x + i) < width) && ((y + j) < height))
                                    {
                                        uint offset = (uint)(z * sizeofplane + (y + j) * bps + (x + i) * bpp + 3);
                                        rawData[offset] = alphas[bits & 0x07];
                                    }
                                    bits >>= 3;
                                }
                            }

                            // Last three bytes
                            bits = *((uint*)&alphamask[3]);
                            for (int j = 2; j < 4; j++)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    // only put pixels out < width or height
                                    if (((x + i) < width) && ((y + j) < height))
                                    {
                                        uint offset = (uint)(z * sizeofplane + (y + j) * bps + (x + i) * bpp + 3);
                                        rawData[offset] = alphas[bits & 0x07];
                                    }
                                    bits >>= 3;
                                }
                            }
                        }
                    }
                }
            }
            return rawData;
        }

        static unsafe byte[] DecompressFloat(in DDSHeader header, byte[] data, PixelFormat pixelFormat)
        {
            // allocate bitmap
            int bpp = (int)(PixelFormatToBpp(pixelFormat, header.pixelformat.rgbbitcount));
            int bps = (int)(header.width * bpp * PixelFormatToBpc(pixelFormat));
            int sizeofplane = (int)(bps * header.height);
            int width = (int)header.width;
            int height = (int)header.height;
            int depth = (int)header.depth;

            byte[] rawData = new byte[depth * sizeofplane + height * bps + width * bpp];
            fixed (byte* bytePtr = data)
            {
                byte* temp = bytePtr;
                fixed (byte* destPtr = rawData)
                {
                    byte* destData = destPtr;
                    int size;
                    switch (pixelFormat)
                    {
                        case PixelFormat.R32F:  // Red float, green = blue = max
                            size = width * height * depth * 3;
                            for (int i = 0, j = 0; i < size; i += 3, j++)
                            {
                                ((float*)destData)[i] = ((float*)temp)[j];
                                ((float*)destData)[i + 1] = 1.0f;
                                ((float*)destData)[i + 2] = 1.0f;
                            }
                            break;

                        case PixelFormat.A32B32G32R32F:  // Direct copy of float RGBA data
                            Array.Copy(data, rawData, data.Length);
                            break;

                        case PixelFormat.G32R32F:  // Red float, green float, blue = max
                            size = width * height * depth * 3;
                            for (int i = 0, j = 0; i < size; i += 3, j += 2)
                            {
                                ((float*)destData)[i] = ((float*)temp)[j];
                                ((float*)destData)[i + 1] = ((float*)temp)[j + 1];
                                ((float*)destData)[i + 2] = 1.0f;
                            }
                            break;

                        case PixelFormat.R16F:  // Red float, green = blue = max
                            size = width * height * depth * bpp;
                            ConvR16ToFloat32((uint*)destData, (ushort*)temp, (uint)size);
                            break;

                        case PixelFormat.A16B16G16R16F:  // Just convert from half to float.
                            size = width * height * depth * bpp;
                            ConvFloat16ToFloat32((uint*)destData, (ushort*)temp, (uint)size);
                            break;

                        case PixelFormat.G16R16F:  // Convert from half to float, set blue = max.
                            size = width * height * depth * bpp;
                            ConvG16R16ToFloat32((uint*)destData, (ushort*)temp, (uint)size);
                            break;
                    }
                }
            }

            return rawData;
        }

        #endregion // Decompress


        #region Nested Types

        [StructLayout(LayoutKind.Sequential)]
        private struct Colour8888
        {
            public byte red;
            public byte green;
            public byte blue;
            public byte alpha;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct Colour565
        {
            public ushort blue; //: 5;
            public ushort green; //: 6;
            public ushort red; //: 5;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DDSHeader
        {
            public uint size;		// equals size of struct (which is part of the data file!)
            public uint flags;
            public uint height;
            public uint width;
            public uint sizeorpitch;
            public uint depth;
            public uint mipmapcount;
            public uint alphabitdepth;
            //[MarshalAs(UnmanagedType.U4, SizeConst = 11)]
            public uint[] reserved;//[11];

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public struct pixelformatstruct
            {
                public uint size;	// equals size of struct (which is part of the data file!)
                public uint flags;
                public uint fourcc;
                public uint rgbbitcount;
                public uint rbitmask;
                public uint gbitmask;
                public uint bbitmask;
                public uint alphabitmask;
            }
            public pixelformatstruct pixelformat;

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public struct ddscapsstruct
            {
                public uint caps1;
                public uint caps2;
                public uint caps3;
                public uint caps4;
            }
            public ddscapsstruct ddscaps;
            public uint texturestage;

            //#ifndef __i386__
            //void to_little_endian()
            //{
            //	size_t size = sizeof(DDSStruct);
            //	assert(size % 4 == 0);
            //	size /= 4;
            //	for (size_t i=0; i<size; i++)
            //	{
            //		((int32_t*) this)[i] = little_endian(((int32_t*) this)[i]);
            //	}
            //}
            //#endif
        }

        private const int DDSD_CAPS = 0x00000001;
        private const int DDSD_HEIGHT = 0x00000002;
        private const int DDSD_WIDTH = 0x00000004;
        private const int DDSD_PITCH = 0x00000008;
        private const int DDSD_PIXELFORMAT = 0x00001000;
        private const int DDSD_MIPMAPCOUNT = 0x00020000;
        private const int DDSD_LINEARSIZE = 0x00080000;
        private const int DDSD_DEPTH = 0x00800000;

        private const int DDPF_ALPHAPIXELS = 0x00000001;
        private const int DDPF_FOURCC = 0x00000004;
        private const int DDPF_RGB = 0x00000040;
        private const int DDPF_LUMINANCE = 0x00020000;

        // caps1
        private const int DDSCAPS_COMPLEX = 0x00000008;
        private const int DDSCAPS_TEXTURE = 0x00001000;
        private const int DDSCAPS_MIPMAP = 0x00400000;
        // caps2
        private const int DDSCAPS2_CUBEMAP = 0x00000200;
        private const int DDSCAPS2_CUBEMAP_POSITIVEX = 0x00000400;
        private const int DDSCAPS2_CUBEMAP_NEGATIVEX = 0x00000800;
        private const int DDSCAPS2_CUBEMAP_POSITIVEY = 0x00001000;
        private const int DDSCAPS2_CUBEMAP_NEGATIVEY = 0x00002000;
        private const int DDSCAPS2_CUBEMAP_POSITIVEZ = 0x00004000;
        private const int DDSCAPS2_CUBEMAP_NEGATIVEZ = 0x00008000;
        private const int DDSCAPS2_VOLUME = 0x00200000;

        private const uint FOURCC_DXT1 = 0x31545844;
        private const uint FOURCC_DXT2 = 0x32545844;
        private const uint FOURCC_DXT3 = 0x33545844;
        private const uint FOURCC_DXT4 = 0x34545844;
        private const uint FOURCC_DXT5 = 0x35545844;
        private const uint FOURCC_ATI1 = 0x31495441;
        private const uint FOURCC_ATI2 = 0x32495441;
        private const uint FOURCC_RXGB = 0x42475852;
        private const uint FOURCC_DOLLARNULL = 0x24;
        private const uint FOURCC_oNULL = 0x6f;
        private const uint FOURCC_pNULL = 0x70;
        private const uint FOURCC_qNULL = 0x71;
        private const uint FOURCC_rNULL = 0x72;
        private const uint FOURCC_sNULL = 0x73;
        private const uint FOURCC_tNULL = 0x74;

        /// <summary>
        /// Various pixel formats/compressors used by the DDS image.
        /// </summary>
        public enum PixelFormat
        {
            /// <summary>
            /// 32-bit image, with 8-bit red, green, blue and alpha.
            /// </summary>
            RGBA,
            /// <summary>
            /// 24-bit image with 8-bit red, green, blue.
            /// </summary>
            RGB,
            /// <summary>
            /// 16-bit DXT-1 compression, 1-bit alpha.
            /// </summary>
            DXT1,
            /// <summary>
            /// DXT-2 Compression
            /// </summary>
            DXT2,
            /// <summary>
            /// DXT-3 Compression
            /// </summary>
            DXT3,
            /// <summary>
            /// DXT-4 Compression
            /// </summary>
            DXT4,
            /// <summary>
            /// DXT-5 Compression
            /// </summary>
            DXT5,
            /// <summary>
            /// 3DC Compression
            /// </summary>
            THREEDC,
            /// <summary>
            /// ATI1n Compression
            /// </summary>
            ATI1N,
            LUMINANCE,
            LUMINANCE_ALPHA,
            RXGB,
            A16B16G16R16,
            R16F,
            G16R16F,
            A16B16G16R16F,
            R32F,
            G32R32F,
            A32B32G32R32F,
            /// <summary>
            /// Unknown pixel format.
            /// </summary>
            UNKNOWN
        }

        #endregion
    }

    /// <summary>
    /// Thrown when an invalid file header has been encountered.
    /// </summary>
    public class InvalidFileHeaderException : Exception
    {
    }

    /// <summary>
    /// Thrown when there is an unknown compressor used in the DDS file.
    /// </summary>
    public class UnknownFileFormatException : Exception
    {
    }
}
