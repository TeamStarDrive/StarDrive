// Phase 3.3 probe helper. Extracts the inner D3DX fx_2_0 blob from an XNA 3.1
// EffectReader-baked .xnb. Used to feed standalone FX blobs into fxc /dumpbin
// or future disassembly steps without going through the LZX+manifest dance
// each time.
//
// Wire format confirmed by Phase 3.3 step 1 dump on 5 of 6 broken effects:
//   XNB header (LZX-compressed)
//     -> manifest ([0]=Microsoft.Xna.Framework.Content.EffectReader, sharedRes=0, primary=1)
//     -> Int32 fxBlobLength
//     -> byte[fxBlobLength] fxBlob   (starts with 01 09 FF FE FXLVM magic)
//
// Run: dotnet run --project x64Migration/Tools/EffectXnbDump --no-build -- extract <xnb> <out.fxb>
// (Sub-command 'extract' is the only difference from Program.Main; see Program.cs.)
using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace EffectXnbDump
{
    static class ExtractFxBlob
    {
        public static int Run(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("usage: extract <xnb> <out.fxb>");
                return 2;
            }
            string xnbPath = args[0];
            string outPath = args[1];

            using var fs = File.OpenRead(xnbPath);
            using var br = new BinaryReader(fs);

            if (br.ReadByte() != 'X' || br.ReadByte() != 'N' || br.ReadByte() != 'B')
                throw new InvalidDataException("Not an XNB");

            br.ReadByte(); br.ReadByte();           // target, version
            byte flags = br.ReadByte();
            int totalSize = br.ReadInt32();

            Stream body;
            bool needsClose = false;
            if ((flags & 0x80) != 0)
            {
                int decompressedSize = br.ReadInt32();
                int compressedSize = totalSize - 14;
                body = LzxDecompress(br, compressedSize, decompressedSize);
                needsClose = true;
            }
            else
            {
                body = fs;
            }

            try
            {
                using var brBody = new BinaryReader(body, Encoding.UTF8, leaveOpen: needsClose);
                int numReaders = Read7BitEncodedInt(brBody);
                for (int i = 0; i < numReaders; i++)
                {
                    brBody.ReadString();
                    brBody.ReadInt32();
                }
                Read7BitEncodedInt(brBody);          // sharedResourceCount
                Read7BitEncodedInt(brBody);          // primaryAssetReaderId

                int fxBlobLen = brBody.ReadInt32();
                byte[] blob = brBody.ReadBytes(fxBlobLen);
                File.WriteAllBytes(outPath, blob);
                Console.WriteLine($"wrote {fxBlobLen} bytes -> {outPath}");
                Console.WriteLine($"first 8 bytes: {blob[0]:X2} {blob[1]:X2} {blob[2]:X2} {blob[3]:X2} {blob[4]:X2} {blob[5]:X2} {blob[6]:X2} {blob[7]:X2}");
                return 0;
            }
            finally
            {
                if (needsClose) body.Dispose();
            }
        }

        static Stream LzxDecompress(BinaryReader br, int compressedSize, int decompressedSize)
        {
            Type lzxType = typeof(ContentManager).Assembly
                .GetType("Microsoft.Xna.Framework.Content.LzxDecoder", throwOnError: true);
            object decoder = Activator.CreateInstance(lzxType,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null, new object[] { 16 }, null);
            MethodInfo decompress = lzxType.GetMethod("Decompress",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            byte[] inBuf = br.ReadBytes(compressedSize);
            using var inStream = new MemoryStream(inBuf);
            var output = new MemoryStream(decompressedSize);

            int decodedBytes = 0;
            int defaultFrameSize = 0x8000;
            while (decodedBytes < decompressedSize)
            {
                int hi = inStream.ReadByte();
                int lo = inStream.ReadByte();
                if (hi < 0 || lo < 0) break;

                int blockSize, frameSize;
                if (hi == 0xFF)
                {
                    // See Program.LzxDecompress — frame_size byte order is (lo << 8) | b3.
                    int b3 = inStream.ReadByte();
                    frameSize = (lo << 8) | b3;
                    blockSize = (inStream.ReadByte() << 8) | inStream.ReadByte();
                    defaultFrameSize = frameSize;
                }
                else
                {
                    blockSize = (hi << 8) | lo;
                    frameSize = defaultFrameSize;
                }
                if (blockSize == 0 || frameSize == 0) break;

                int remaining = decompressedSize - decodedBytes;
                if (frameSize > remaining) frameSize = remaining;

                decompress.Invoke(decoder, new object[] { inStream, blockSize, output, frameSize });
                decodedBytes += frameSize;
            }

            output.Position = 0;
            return output;
        }

        static int Read7BitEncodedInt(BinaryReader r)
        {
            int result = 0, shift = 0;
            byte b;
            do
            {
                b = r.ReadByte();
                result |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return result;
        }
    }
}
