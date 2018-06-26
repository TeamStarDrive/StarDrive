using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime;
using System.Text;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game
{
    internal static class HelperFunctions
    {
        // return number of solutions, false if no solutions or infinite solutions
        // @note this is optimized quite well
        public static bool CircleIntersection(Circle ca, Circle cb, out Vector2 intersect1, out Vector2 intersect2)
        {
            float dx = ca.Center.X - cb.Center.Y;
            float dy = ca.Center.Y - cb.Center.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            float r0 = ca.Radius;
            float r1 = cb.Radius;
            if (dist < 0.0001f || dist > r0 + r1 || dist < Math.Abs(r0 - r1))
            {
                intersect1 = Vector2.Zero;
                intersect2 = Vector2.Zero;
                return false;
            }

            // Determine the distance from point 0 to point 2
            float a = (r0*r0 - r1*r1 + dist*dist) / (2.0f * dist);

            // Determine the coordinates of point 2
            float aDivDist = a / dist;
            float x2 = ca.Center.X + dx * aDivDist;
            float y2 = ca.Center.Y + dy * aDivDist;

            // Determine the distance from point 2 to either of the intersection points.
            float hDivDist = (float)Math.Sqrt(r0*r0 - a*a) / dist;

            // Now determine the offsets of the intersection points from point 2
            float rx = -dy * hDivDist;
            float ry =  dx * hDivDist;

            // Determine the absolute intersection points
            intersect1 = new Vector2(x2 + rx, y2 + ry);
            intersect2 = new Vector2(x2 - rx, y2 - ry);
            return true;
        }

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
                            new FileInfo(Dir.ApplicationData + "/StarDrive/Fleet Designs/" + designPath);
            if (info.Exists)
                return info.Deserialize<FleetDesign>();

            Log.Warning($"Failed to load fleet design '{designPath}'");
            return null;
        }

        private static Fleet CreateFleetFromData(FleetDesign data, Empire owner, Vector2 position)
        {
            if (data == null)
                return null;

            var fleet = new Fleet
            {
                Position  = position,
                Owner     = owner,
                DataNodes = new BatchRemovalCollection<FleetDataNode>()
            };
            foreach (FleetDataNode node in data.Data)
                fleet.DataNodes.Add(node);
            fleet.Name           = data.Name;
            fleet.FleetIconIndex = data.FleetIconIndex;

            using (fleet.DataNodes.AcquireWriteLock())
                foreach (FleetDataNode node in fleet.DataNodes)
                {
                    Ship s = Ship.CreateShipAtPoint(node.ShipName, owner, position + node.FleetOffset);
                    if (s == null) continue;
                    s.RelativeFleetOffset = node.FleetOffset;
                    node.Ship = s;
                    node.OrdersRadius = node.OrdersRadius >1 ? node.OrdersRadius : s.SensorRange * node.OrdersRadius;
                    fleet.AddShip(s);
                }
            return fleet;
        }

        public static void CreateFleetFromDataAt(FleetDesign data, Empire owner, Vector2 position)
        {
            owner.FirstFleet = CreateFleetFromData(data, owner, position);
        }
        public static Fleet CreateDefensiveFleetAt(string fleetUid, Empire owner, Vector2 position)
        {
            return CreateFleetFromData(LoadFleetDesign(fleetUid), owner, position);
        }
        public static void CreateFleetAt(string fleetUid, Empire owner, Vector2 position)
        {
            Fleet fleet = CreateDefensiveFleetAt(fleetUid, owner, position);
            if (fleet != null)
                owner.FirstFleet = fleet;
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

        public static void DrawDropShadowImage(ScreenManager screenManager, Rectangle rect, string texture, Color topColor)
        {
            DrawDropShadowImage(screenManager, rect, ResourceManager.TextureDict[texture], topColor);
        }
        public static void DrawDropShadowImage(ScreenManager screenManager, Rectangle rect, Texture2D texture, Color topColor)
        {
            Rectangle offsetRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width, rect.Height);
            screenManager.SpriteBatch.Draw(texture, offsetRect, Color.Black);
            screenManager.SpriteBatch.Draw(texture, rect, topColor);
        }
        public static void DrawDropShadowText(ScreenManager screenManager, string text, Vector2 pos, SpriteFont font)
        {
            DrawDropShadowText(screenManager, text, pos, font, Color.White);
        }
        public static void DrawDropShadowText1(ScreenManager screenManager, string text, Vector2 pos, SpriteFont font, Color c)
        {
            DrawDropShadowText(screenManager, text, pos, font, c, 1f);
        }
        public static void DrawDropShadowText(ScreenManager screenManager, string text, Vector2 pos, SpriteFont font, Color c, float shadowOffset = 2f)
        {
            pos.X = (int)pos.X;
            pos.Y = (int)pos.Y;
            screenManager.SpriteBatch.DrawString(font, text, pos + new Vector2(shadowOffset), Color.Black);
            screenManager.SpriteBatch.DrawString(font, text, pos, c);
        }

        public static void DrawDropShadowText(SpriteBatch spriteBatch, string text, Vector2 pos, SpriteFont font, Color c, float shadowOffset = 2f)
        {
            pos.X = (int)pos.X;
            pos.Y = (int)pos.Y;
            spriteBatch.DrawString(font, text, pos + new Vector2(shadowOffset), Color.Black);
            spriteBatch.DrawString(font, text, pos, c);
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

        public static void DrawStroke(SpriteFont font, SpriteBatch sb, string text, Color backColor, Color frontColor, float scale, float rotation, Vector2 position)
        {
            var textSize = font.MeasureString(text);
            Vector2 origin = new Vector2(textSize.X / 2f, textSize.Y / 2f);
            sb.DrawString(font, text, position + new Vector2(1f * scale, 1f * scale), backColor, rotation, origin, scale, SpriteEffects.None, 1f);
            sb.DrawString(font, text, position + new Vector2(-1f * scale, -1f * scale), backColor, rotation, origin, scale, SpriteEffects.None, 1f);
            sb.DrawString(font, text, position + new Vector2(-1f * scale, 1f * scale), backColor, rotation, origin, scale, SpriteEffects.None, 1f);
            sb.DrawString(font, text, position + new Vector2(1f * scale, -1f * scale), backColor, rotation, origin, scale, SpriteEffects.None, 1f);
            sb.DrawString(font, text, position, frontColor, rotation, origin, scale, SpriteEffects.None, 1f);
        }

        public static bool IntersectCircleSegment(Vector2 c, float r, Vector2 p1, Vector2 p2)
        {
            float x0 = c.X;
            float y0 = c.Y;
            float x1 = p1.X;
            float y1 = p1.Y;
            float x2 = p2.X;
            float y2 = p2.Y;
            float n = Math.Abs((x2 - x1) * (y1 - y0) - (x1 - x0) * (y2 - y1));
            float d = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            if (n / d > r)
            {
                return false;
            }
            if ((float)Math.Sqrt(((x0 - x1) * (x0 - x1) + (y0 - y1) * (y0 - y1))) - r > d)
            {
                return false;
            }
            if ((float)Math.Sqrt(((x0 - x2) * (x0 - x2) + (y0 - y2) * (y0 - y2))) - r > d)
            {
                return false;
            }
            return true;
        }

        public static string ParseText(SpriteFont font, string text, float maxLineWidth)
        {
            var result = new StringBuilder();
            var wordArray = text.Split(' ');
            float length = 0.0f;
            foreach (string word in wordArray)
            {
                if (word != "\\n" && word != "\n")
                {
                    length += font.MeasureString(word).Length();
                    result.Append(word);
                    if (length < maxLineWidth)
                    {
                        result.Append(' ');
                        continue;
                    }
                }
                result.Append('\n');
                length = 0f;
            }
            return result.ToString();
        }

        public static void parseTextToSL(string text, float Width, SpriteFont font, ref ScrollList List)
        {
            string line = string.Empty;
            string returnString = string.Empty;
            char[] chrArray = new char[] { ' ', '\n' };
            string[] wordArray = text.Split(chrArray);
            for (int i = 0; i < (int)wordArray.Length; i++)
            {
                string word = wordArray[i];
                if (string.IsNullOrEmpty(word))
                {
                    word = "\n";
                }
                if (word == "\\n" || word == "\n")
                {
                    word = "";
                    returnString = string.Concat(returnString, line, '\n');
                    line = string.Empty;
                }
                else if (font.MeasureString(string.Concat(line, word)).Length() > Width)
                {
                    returnString = string.Concat(returnString, line, '\n');
                    line = string.Empty;
                }
                if (!string.IsNullOrEmpty(word))
                {
                    line = string.Concat(line, word, ' ');
                }
            }
            string[] lineArray = returnString.Split(new char[] { '\n' });
            for (int i = 0; i < (int)lineArray.Length; i++)
            {
                string sent = lineArray[i];
                if (sent.Length > 0)
                {
                    List.AddItem(sent);
                }
                else if (string.IsNullOrEmpty(sent) && (int)lineArray.Length > i + 1 && !string.IsNullOrEmpty(lineArray[i + 1]))
                {
                    List.AddItem("\n");
                }
            }
            List.AddItem(line);
        }

        public static int RoundTo(float amount1, int roundTo)
        {
            int rounded = (int)(((double)amount1 + 0.5 * (double)roundTo) / (double)roundTo) * roundTo;
            return rounded;
        }

        // Added by McShooterz: module repair priority list, main moduletype (disregard secondary module fucntions)
        public static int ModulePriority(ShipModule shipModule)
        {
            switch (shipModule.ModuleType)
            {
                case ShipModuleType.Command:      return 0;
                case ShipModuleType.PowerPlant:   return 1;
                case ShipModuleType.PowerConduit: return 2;
                case ShipModuleType.Engine:       return 3;
                case ShipModuleType.Shield:       return 4;
                case ShipModuleType.Armor:        return 6;
                default:                          return 5;
            }
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
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }
    }
}