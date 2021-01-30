using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Ships;

namespace Ship_Game
{
    internal static class HelperFunctions
    {
        public static void ClampVectorToInt(ref Vector2 pos)
        {
            pos.X = (int)pos.X;
            pos.Y = (int)pos.Y;
        }

        public static bool ClickedRect(Rectangle toClick, InputState input)
        {
            return input.InGameSelect && toClick.HitTest(input.CursorPosition);
        }

        private static FleetDesign LoadFleetDesign(string fleetUid)
        {
            string designPath = fleetUid + ".xml";
            FileInfo info = ResourceManager.GetModOrVanillaFile(designPath) ??
                            new FileInfo(Dir.StarDriveAppData + "/Fleet Designs/" + designPath);
            if (info.Exists)
                return info.Deserialize<FleetDesign>();

            Log.Warning($"Failed to load fleet design '{designPath}'");
            return null;
        }

        static Fleet CreateFleetFromData(FleetDesign data, Empire owner, Vector2 position)
        {
            if (data == null)
                return null;

            var fleet = new Fleet
            {
                FinalPosition = position,
                Owner = owner
            };
            foreach (FleetDataNode node in data.Data)
            {
                FleetDataNode cloned = node.Clone();
                cloned.CombatState = node.CombatState;
                fleet.DataNodes.Add(cloned);
            }
            fleet.Name = data.Name;
            fleet.FleetIconIndex = data.FleetIconIndex;

            foreach (FleetDataNode node in fleet.DataNodes)
            {
                Ship s = Ship.CreateShipAtPoint(node.ShipName, owner, position + node.FleetOffset);
                if (s == null) continue;
                s.AI.CombatState = node.CombatState;
                s.RelativeFleetOffset = node.FleetOffset;
                node.Ship = s;
                node.OrdersRadius = node.OrdersRadius > 1 ? node.OrdersRadius : s.SensorRange * node.OrdersRadius;
                fleet.AddShip(s);
            }
            return fleet;
        }

        static Fleet CreateFleetFromData(FleetDesign data, Empire owner, Vector2 position, CombatState state)
        {
            var fleet = CreateFleetFromData(data, owner, position);
            if (fleet == null)
                return null;
            foreach (FleetDataNode node in fleet.DataNodes)
                node.CombatState = state;

            return fleet;
        }

        public static Fleet CreateFleetAt(string fleetUid, Empire owner, Vector2 position, CombatState state)
        {
            return CreateFleetFromData(LoadFleetDesign(fleetUid), owner, position, state);
        }
        public static void CreateFirstFleetAt(string fleetUid, Empire owner, Vector2 position)
        {
            Fleet fleet = CreateFleetFromData(LoadFleetDesign(fleetUid), owner, position);
            if (fleet != null)
                owner.FirstFleet = fleet;
        }

        public static bool IsInUniverseBounds(float universeSize, Vector2 pos)
        {
            float x = universeSize;
            float y = universeSize;

            return -x < pos.X && pos.X < x
                && -y < pos.Y && pos.Y < y;
        }

        public static void CompressDir(DirectoryInfo dir, string outFile)
        {
            FileInfo file = new FileInfo(outFile);
            if (file.Exists)
                file.Delete();

            ZipFile.CreateFromDirectory(dir.FullName, outFile, CompressionLevel.Fastest, true);
        }

        public static void Compress(FileInfo fi)
        {
            if (fi.Extension == ".gz" || (fi.Attributes & FileAttributes.Hidden) > 0)
                return;

            // unpacked savegames are roughly 100MB O_O, so only read 4MB at a time
            var buffer = new byte[4096*1024];

            using (FileStream inFile   = fi.OpenRead())
            using (FileStream outFile  = File.Create(fi.FullName + ".gz"))
            using (GZipStream compress = new GZipStream(outFile, CompressionMode.Compress))
            {
                int bytesRead;
                do {
                    compress.Write(buffer, 0, bytesRead = inFile.Read(buffer, 0, buffer.Length));
                } while (bytesRead == buffer.Length);

                Log.Info($"Compressed {fi.Name} from {fi.Length} to {outFile.Length} bytes.");
            }
        }

        public static string Decompress(FileInfo fi)
        {
            string curFile  = fi.FullName;
            string origName = curFile.Remove(curFile.Length - fi.Extension.Length); // remove ".gz"

            using (FileStream inFile = fi.OpenRead())
            using (GZipStream decompress = new GZipStream(inFile, CompressionMode.Decompress))
            using (FileStream outFile = File.Create(origName))
            {
                var buffer = new byte[4096*1024]; // average savegame is 4MB, so try and get this done in one go
                int numRead;
                while ((numRead = decompress.Read(buffer, 0, buffer.Length)) > 0)
                    outFile.Write(buffer, 0, numRead);
                Log.Info($"Decompressed: {fi.Name}");
                return origName;
            }
        }

        public static void DrawDropShadowImage(this SpriteBatch batch, Rectangle rect, SubTexture texture, Color topColor)
        {
            var offsetRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width, rect.Height);
            batch.Draw(texture, offsetRect, Color.Black);
            batch.Draw(texture, rect, topColor);
        }
        public static void DrawDropShadowText(this SpriteBatch batch, string text, Vector2 pos, SpriteFont font)
        {
            DrawDropShadowText(batch, text, pos, font, Color.White);
        }
        public static void DrawDropShadowText1(this SpriteBatch batch, string text, Vector2 pos, SpriteFont font, Color c)
        {
            DrawDropShadowText(batch, text, pos, font, c, 1f);
        }
        public static void DrawDropShadowText(this SpriteBatch batch, string text, Vector2 pos, SpriteFont font, Color c, float shadowOffset = 2f)
        {
            pos.X = (int)pos.X;
            pos.Y = (int)pos.Y;
            batch.DrawString(font, text, pos + new Vector2(shadowOffset), Color.Black);
            batch.DrawString(font, text, pos, c);
        }

        public static void DrawGrid(SpriteBatch spriteBatch, int xpos, int ypos, int xGridSize, int yGridSize, int numberXs, int numberYs)
        {
            int xsize = xGridSize / numberXs;
            int ysize = yGridSize / numberYs;
            var color  = new Color(211, 211, 211, 70);
            var origin = new Vector2(xpos + 1, ypos);
            var end    = new Vector2(xpos, ypos + yGridSize - 1);
            for (int x = 0; x < numberXs; ++x)
            {
                spriteBatch.DrawLine(origin, end, color, 2f);
                origin.X += xsize;
                end.X    += xsize;
            }
            origin = new Vector2(xpos, ypos);
            end    = new Vector2(xpos + xGridSize - 3, ypos);
            for (int y = 0; y < numberYs; ++y)
            {
                spriteBatch.DrawLine(origin, end, color, 2f);
                origin.Y += ysize;
                end.Y    += ysize;
            }
        }

        public static Vector2 MeasureLines(this SpriteFont font, Array<string> lines)
        {
            var size = new Vector2();
            foreach (string line in lines)
            {
                size.X = Math.Max(size.X, font.MeasureString(line).X);
                size.Y += font.LineSpacing + 2;
            }
            return size;
        }

        public static int TextWidth(this SpriteFont font, string text)
        {
            return (int)font.MeasureString(text).X;
        }

        public static int TextWidth(this SpriteFont font, int localizationId)
        {
            return (int)font.MeasureString(Localizer.Token(localizationId)).X;
        }

        public static Vector2 MeasureString(this SpriteFont font, in LocalizedText text)
        {
            return font.MeasureString(text.Text);
        }

        public static string ParseText(this SpriteFont font, in LocalizedText text, float maxLineWidth)
        {
            return ParseText(font, text.Text, maxLineWidth);
        }

        public static string ParseText(this SpriteFont font, string text, float maxLineWidth)
        {
            string[] words = text.Split(' ');
            return ParseText(font, words, maxLineWidth);
        }

        public static string ParseText(this SpriteFont font, string[] words, float maxLineWidth)
        {
            var result = new StringBuilder();
            float spaceLength = font.MeasureString(" ").X;
            float lineLength = 0.0f;
            foreach (string word in words)
            {
                if (word == "\\n" || word == "\n")
                {
                    result.Append('\n');
                    lineLength = 0f;
                    continue;
                }
                float wordLength = font.MeasureString(word).X;
                if ((lineLength + wordLength) > maxLineWidth)
                {
                    result.Append('\n');
                    lineLength = wordLength;
                    result.Append(word);
                }
                else
                {
                    if (result.Length != 0)
                    {
                        lineLength += spaceLength;
                        result.Append(' ');
                    }
                    lineLength += wordLength;
                    result.Append(word);
                }
            }
            return result.ToString();
        }

        public static string[] ParseTextToLines(this SpriteFont font, string text, float maxLineWidth)
        {
            string parsed = ParseText(font, text, maxLineWidth);
            string[] lines = parsed.Split('\n');
            return lines;
        }

        public static void ResetWithParseText(
            this ScrollList2<TextListItem> list, SpriteFont font, string text, float maxLineWidth)
        {
            string parsed = ParseText(font, text, maxLineWidth);
            list.Reset();
            string[] lines = parsed.Split('\n');
            TextListItem[] textItems = lines.Select(line => new TextListItem(line, font));
            list.SetItems(textItems);
        }

        public static int RoundTo(float amount1, int roundTo)
        {
            int rounded = (int)((amount1 + 0.5 * roundTo) / roundTo) * roundTo;
            return rounded;
        }

        // Added by RedFox: blocking full blown GC to reduce memory fragmentation
        public static void CollectMemory()
        {
            // the GetTotalMemory full collection loop is pretty good, so we use it instead of GC.Collect()
            Log.Warning(" ========= CollectMemory ========= ");
            float before = GC.GetTotalMemory(forceFullCollection: false) / (1024f * 1024f);
            CollectMemorySilent();
            float after  = GC.GetTotalMemory(forceFullCollection: true) / (1024f * 1024f);
            Log.Warning($"   Before: {before:0.0}MB  After: {after:0.0}MB");
            float processMemory = Process.GetCurrentProcess().WorkingSet64 / (1024f * 1024f);
            Log.Warning($"   Process Memory: {processMemory:0.0}MB");
            Log.Warning(" ================================= ");
        }

        public static void CollectMemorySilent()
        {
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public static string GetNumberString(this float stat)
        {
            CultureInfo invariant = CultureInfo.InvariantCulture;
            if (Math.Abs(stat) < 100f)   return stat.ToString("0.##", invariant); // 95.75  or 0.25
            if (Math.Abs(stat) < 1000f)  return stat.ToString("0.#", invariant);  // 950.7  or 0.5
            if (Math.Abs(stat) < 10000f) return stat.ToString("#", invariant);    // 9500
            float single = stat / 1000f;
            if (Math.Abs(single) < 100f)  return single.ToString("0.##", invariant) + "k"; // 57.75k or 0.5k
            if (Math.Abs(single) < 1000f) return single.ToString("0.#", invariant) + "k";  // 950.7k
            return single.ToString("#", invariant) + "k"; // 1000k
        }

        public static bool DataVisibleToPlayer(Empire empire)
        {
            if (empire.isPlayer || empire.IsAlliedWith(EmpireManager.Player) || Empire.Universe.Debug)
                return true;

            return empire.DifficultyModifiers.DataVisibleToPlayer;
        }

        public static SortedList<int, Array<T>> BucketItems<T>(Array<T> items, Func<T, int> bucketSort)
        {
            //SortRoles
            /*
             * take each ship and create buckets using the bucketSort ascending.
             */
            var sort = new SortedList<int, Array<T>>();

            foreach (T item in items)
            {
                int key = bucketSort(item);
                if (sort.TryGetValue(key, out Array<T> test))
                    test.Add(item);
                else
                {
                    test = new Array<T> { item };
                    sort.Add(key, test);
                }
            }
            return sort;
        }

        public static bool GetLoneSystem(out SolarSystem system)
        {
            system = null;
            var systems = UniverseScreen.SolarSystemList.Filter(s => s.RingList.Count == 0
                                                                     && !s.PiratePresence);

            if (systems.Length > 0)
                system = systems.RandItem();

            return system != null;
        }

        public static bool GetUnownedSystems(out SolarSystem[] systems)
        {
            systems = UniverseScreen.SolarSystemList.Filter(s => s.OwnerList.Count == 0
                                                                 && s.RingList.Count > 0
                                                                 && !s.PiratePresence
                                                                 && !s.ShipList.Any(g => g.IsGuardian));

            return systems.Length > 0;
        }

        public static bool GetRadiatingStars(out SolarSystem[] systems)
        {
            systems = UniverseScreen.SolarSystemList.Filter(s => s.OwnerList.Count == 0
                                                                 && !s.PiratePresence
                                                                 && s.Sun.RadiationRadius.Greater(0));

            return systems.Length > 0;
        }

        // For specific cases were non squared icons requires a different texture when oriented, light thrusters
        public static bool GetOrientedModuleTexture(ShipModule template, ref SubTexture tex, ModuleOrientation orientation)
        {
            if (template.DisableRotation)
                return false;

            string defaultTex = template.IconTexturePath;
            switch (orientation)
            {
                case ModuleOrientation.Left:  tex = ResourceManager.TextureOrDefault($"{defaultTex}_270", defaultTex); break;
                case ModuleOrientation.Right: tex = ResourceManager.TextureOrDefault($"{defaultTex}_90", defaultTex); break;
                case ModuleOrientation.Rear:  tex = ResourceManager.TextureOrDefault($"{defaultTex}_180", defaultTex); break;
                default: return false;
            }

            return tex.Name.EndsWith("_90")
                   || tex.Name.EndsWith("_180")
                   || tex.Name.EndsWith("_270");
        }
    }
}