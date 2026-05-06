using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace Phase3AssetInventory
{
    // Phase 3.1 step 6: walk every *.xnb under a content root, decode the type-reader
    // manifest, and classify each file as static-sunburn / skinned-sgmotion / static-raw /
    // non-model. Output CSV + summary markdown for §3.4 / §3.5 scoping.
    //
    // The classifier is manifest-level only — no full XNB binary decode. The discovery
    // from the Phase 3 baseline runtime log was that the reader chain string self-identifies
    // the failure mode (see phase3-logs/phase3-baseline-summary.md).
    //
    // Standalone — no project references. LZX + 7-bit-int + ReadString logic copied
    // locally from Ship_Game/Data/Xna31Texture2DReader.cs to keep the tool independent
    // of the main solution.
    static class Program
    {
        static int Main(string[] args)
        {
            string contentRoot = args.Length > 0 ? args[0] : @"C:\Development\stardrive\BlackBoxPlus\game\Content\Model";
            string outCsv      = args.Length > 1 ? args[1] : @"C:\Development\stardrive\BlackBoxPlus\x64Migration\phase3-logs\asset-survey.csv";
            string outSummary  = Path.Combine(Path.GetDirectoryName(outCsv) ?? ".", "asset-survey-summary.md");

            if (!Directory.Exists(contentRoot))
            {
                Console.Error.WriteLine($"ERROR: content root '{contentRoot}' does not exist");
                return 2;
            }

            // Diagnostic: probe MonoGame's internal types to verify which LZX path is available.
            Console.WriteLine($"MonoGame.Framework asm: {typeof(ContentManager).Assembly.Location}");
            foreach (string typeName in new[] {
                "Microsoft.Xna.Framework.Content.LzxDecoder",
                "Microsoft.Xna.Framework.Content.LzxDecoderStream",
                "Microsoft.Xna.Framework.Content.Lz4DecoderStream",
            })
            {
                Type t = typeof(ContentManager).Assembly.GetType(typeName, false);
                Console.WriteLine($"  {typeName}: {(t != null ? "FOUND" : "not found")}");
            }

            var xnbFiles = Directory.GetFiles(contentRoot, "*.xnb", SearchOption.AllDirectories)
                                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                                    .ToArray();
            Console.WriteLine($"Scanning {xnbFiles.Length} XNB files under {contentRoot}");

            var rows = new List<Row>(xnbFiles.Length);
            int ok = 0, failed = 0;
            foreach (string path in xnbFiles)
            {
                try
                {
                    var readers = ReadTypeReaders(path);
                    rows.Add(Classify(path, contentRoot, readers));
                    ok++;
                }
                catch (Exception ex)
                {
                    Exception inner = ex;
                    while (inner is System.Reflection.TargetInvocationException tie && tie.InnerException != null)
                        inner = tie.InnerException;
                    rows.Add(new Row
                    {
                        RelPath     = MakeRel(path, contentRoot),
                        Kind        = "unreadable",
                        ReaderCount = 0,
                        ReaderChain = $"{inner.GetType().Name}: {inner.Message}",
                    });
                    failed++;
                    Console.WriteLine($"  FAIL: {Path.GetFileName(path)} — {inner.GetType().Name}: {inner.Message}");
                }
            }
            Console.WriteLine($"Parsed {ok} / Failed {failed}");

            Directory.CreateDirectory(Path.GetDirectoryName(outCsv) ?? ".");
            WriteCsv(outCsv, rows);
            WriteSummary(outSummary, rows, contentRoot);
            Console.WriteLine($"Wrote {outCsv}");
            Console.WriteLine($"Wrote {outSummary}");
            return 0;
        }

        sealed class Row
        {
            public string RelPath;
            public string Kind;
            public int ReaderCount;
            public string ReaderChain;
            public bool HasAnimation;
            public bool HasSunBurn;
            public bool IsModel;
        }

        static Row Classify(string fullPath, string contentRoot, List<(int version, string name)> readers)
        {
            string chain = string.Join("; ", readers.Select(r => r.name));
            bool isModel       = chain.Contains("ModelReader", StringComparison.Ordinal);
            bool isSkinned     = chain.Contains("SkinnedModelReader", StringComparison.Ordinal)
                              || chain.Contains("XNAnimation", StringComparison.Ordinal)
                              || chain.Contains("SgMotion", StringComparison.Ordinal);
            bool hasSunBurn    = chain.Contains("SynapseGaming", StringComparison.Ordinal)
                              || chain.Contains("SunBurn", StringComparison.Ordinal);
            bool hasAnimation  = isSkinned
                              || chain.Contains("AnimationClip", StringComparison.Ordinal)
                              || chain.Contains("BoneReader", StringComparison.Ordinal);

            string kind;
            if (!isModel)
                kind = "non-model"; // Effect, Texture, SpriteFont, etc.
            else if (isSkinned)
                kind = "skinned-sgmotion";
            else if (hasSunBurn)
                kind = "static-sunburn";
            else
                kind = "static-raw";

            return new Row
            {
                RelPath      = MakeRel(fullPath, contentRoot),
                Kind         = kind,
                ReaderCount  = readers.Count,
                ReaderChain  = chain,
                HasAnimation = hasAnimation,
                HasSunBurn   = hasSunBurn,
                IsModel      = isModel,
            };
        }

        static string MakeRel(string fullPath, string root)
        {
            string r = root.TrimEnd('\\', '/');
            string f = fullPath.Replace('\\', '/');
            string rNorm = r.Replace('\\', '/');
            if (f.StartsWith(rNorm + "/", StringComparison.OrdinalIgnoreCase))
                return f.Substring(rNorm.Length + 1);
            return f;
        }

        static void WriteCsv(string outPath, List<Row> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("rel_path,kind,reader_count,is_model,has_sunburn,has_animation,reader_chain");
            foreach (var r in rows)
            {
                sb.Append(CsvField(r.RelPath)).Append(',');
                sb.Append(CsvField(r.Kind)).Append(',');
                sb.Append(r.ReaderCount).Append(',');
                sb.Append(r.IsModel ? "true" : "false").Append(',');
                sb.Append(r.HasSunBurn ? "true" : "false").Append(',');
                sb.Append(r.HasAnimation ? "true" : "false").Append(',');
                sb.AppendLine(CsvField(r.ReaderChain));
            }
            File.WriteAllText(outPath, sb.ToString());
        }

        static string CsvField(string s)
        {
            if (s == null) return "";
            bool needsQuote = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
            return needsQuote ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
        }

        static void WriteSummary(string outPath, List<Row> rows, string contentRoot)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Phase 3 Asset Inventory Summary");
            sb.AppendLine();
            sb.AppendLine($"Generated by `x64Migration/Tools/Phase3AssetInventory/` from `{contentRoot}`");
            sb.AppendLine($"Total XNBs scanned: **{rows.Count}**");
            sb.AppendLine();

            sb.AppendLine("## Counts by kind");
            sb.AppendLine();
            sb.AppendLine("| Kind | Count |");
            sb.AppendLine("|---|---|");
            foreach (var g in rows.GroupBy(r => r.Kind).OrderByDescending(g => g.Count()))
                sb.AppendLine($"| `{g.Key}` | {g.Count()} |");
            sb.AppendLine();

            sb.AppendLine("## Counts by directory × kind (Models only)");
            sb.AppendLine();
            sb.AppendLine("| Directory | static-sunburn | static-raw | skinned-sgmotion | non-model | unreadable | total |");
            sb.AppendLine("|---|---:|---:|---:|---:|---:|---:|");
            var modelRows = rows.Where(r => r.Kind != "unreadable" && r.Kind != "non-model").ToList();
            var byDir = rows.GroupBy(r => TopLevelDir(r.RelPath))
                            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);
            foreach (var g in byDir)
            {
                int sb_ = g.Count(r => r.Kind == "static-sunburn");
                int sr  = g.Count(r => r.Kind == "static-raw");
                int sk  = g.Count(r => r.Kind == "skinned-sgmotion");
                int nm  = g.Count(r => r.Kind == "non-model");
                int un  = g.Count(r => r.Kind == "unreadable");
                int tot = g.Count();
                sb.AppendLine($"| {g.Key} | {sb_} | {sr} | {sk} | {nm} | {un} | {tot} |");
            }
            sb.AppendLine();

            sb.AppendLine("## Skinned/animated assets (full list — §3.5 scope)");
            sb.AppendLine();
            var skinned = rows.Where(r => r.Kind == "skinned-sgmotion").OrderBy(r => r.RelPath, StringComparer.OrdinalIgnoreCase).ToList();
            if (skinned.Count == 0)
            {
                sb.AppendLine("_(none — no SgMotion-bound XNBs found)_");
            }
            else
            {
                sb.AppendLine("| Path | Reader chain |");
                sb.AppendLine("|---|---|");
                foreach (var r in skinned)
                    sb.AppendLine($"| `{r.RelPath}` | `{r.ReaderChain}` |");
            }
            sb.AppendLine();

            sb.AppendLine("## Static-raw (no SunBurn material — §3.4 VertexDeclaration-only path)");
            sb.AppendLine();
            var raw = rows.Where(r => r.Kind == "static-raw").OrderBy(r => r.RelPath, StringComparer.OrdinalIgnoreCase).ToList();
            sb.AppendLine($"_{raw.Count} files. These don't reference SunBurn so reader-chain resolution succeeds; they fail at the 3.1 VertexDeclaration binary decode (matches the runtime IndexOutOfRangeException cluster from `phase3-baseline-summary.md`)._");
            sb.AppendLine();
            if (raw.Count > 0)
            {
                sb.AppendLine("| Path |");
                sb.AppendLine("|---|");
                foreach (var r in raw)
                    sb.AppendLine($"| `{r.RelPath}` |");
            }
            sb.AppendLine();

            sb.AppendLine("## Unreadable (parse failure — needs investigation)");
            sb.AppendLine();
            var bad = rows.Where(r => r.Kind == "unreadable").ToList();
            if (bad.Count == 0)
            {
                sb.AppendLine("_(none — every XNB parsed cleanly)_");
            }
            else
            {
                sb.AppendLine("| Path | Error |");
                sb.AppendLine("|---|---|");
                foreach (var r in bad)
                    sb.AppendLine($"| `{r.RelPath}` | `{r.ReaderChain}` |");
            }
            sb.AppendLine();

            File.WriteAllText(outPath, sb.ToString());
        }

        static string TopLevelDir(string relPath)
        {
            int slash = relPath.IndexOfAny(new[] { '/', '\\' });
            if (slash < 0) return "(root)";
            string first = relPath.Substring(0, slash);
            int slash2 = relPath.IndexOfAny(new[] { '/', '\\' }, slash + 1);
            if (slash2 < 0) return first;
            return first + "/" + relPath.Substring(slash + 1, slash2 - slash - 1);
        }

        // ====================================================================
        // XNB header + LZX + manifest parsing — copied from Ship_Game/Data/Xna31Texture2DReader.cs
        // (DumpXnbTypeReaders) so the tool stays standalone. Reads the XNB header,
        // LZX-decompresses if needed, returns the type-reader name list.
        // ====================================================================
        static List<(int version, string name)> ReadTypeReaders(string xnbPath)
        {
            using var fs = File.OpenRead(xnbPath);
            using var br = new BinaryReader(fs);

            if (br.ReadByte() != 'X' || br.ReadByte() != 'N' || br.ReadByte() != 'B')
                throw new InvalidDataException($"Not an XNB file");

            br.ReadByte();              // target ('w', 'x', etc.)
            br.ReadByte();              // version (4 for XNA 3.1, 5 for 4.0)
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
                var list = new List<(int, string)>(numReaders);
                for (int i = 0; i < numReaders; i++)
                {
                    string name = brBody.ReadString();
                    int rdrVersion = brBody.ReadInt32();
                    list.Add((rdrVersion, name));
                }
                return list;
            }
            finally
            {
                if (needsClose) body.Dispose();
            }
        }

        static Stream LzxDecompress(BinaryReader br, int compressedSize, int decompressedSize)
        {
            // Mirrors MonoGame's `LzxDecoderStream` ctor with the critical clamp on
            // `frameSize` to `decompressedSize - decodedBytes` for the last (or only)
            // frame. Without this, the decoder tries to produce 0x8000 bytes from a
            // file whose entire decompressed payload is smaller, reading past EOF on
            // the input. The 5-byte FF-prefixed frame header has frameSize little-
            // endian and blockSize big-endian.
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
                    int b3 = inStream.ReadByte();
                    frameSize = (b3 << 8) | lo;
                    blockSize = (inStream.ReadByte() << 8) | inStream.ReadByte();
                    defaultFrameSize = frameSize; // sticky for subsequent 2-byte frames
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
