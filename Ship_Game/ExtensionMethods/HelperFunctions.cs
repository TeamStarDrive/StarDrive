using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Serialization;
using Ship_Game.AI;

namespace Ship_Game
{
	internal sealed class HelperFunctions
	{
		public HelperFunctions()
		{
		}

		public static Vector3[] BeamPoints(Vector2 src, Vector2 dst, float width, Vector2[] outPoints, int offset, float BeamZ)
		{
			Vector2 dir = dst - src;
			Vector2 right = Vector2.Normalize(new Vector2(dir.Y, -dir.X));
			outPoints[0] = src - (right * width);
			outPoints[1] = src + (right * width);
			outPoints[2] = dst - (right * width);
			outPoints[3] = dst + (right * width);
			return new [] { new Vector3(outPoints[0], BeamZ), new Vector3(outPoints[1], BeamZ), new Vector3(outPoints[2], BeamZ), new Vector3(outPoints[3], BeamZ) };
		}

		public static bool CheckIntersection(Rectangle rect, Vector2 pos)
		{
			return pos.X > rect.X && pos.Y > rect.Y && pos.X < rect.X+rect.Width && pos.Y < rect.Y+rect.Height;
		}

		public static Vector2[] CircleIntersection(Circle ca, Circle cb)
		{
			float distance = Vector2.Distance(ca.Center, cb.Center);
			if (distance > ca.Radius + cb.Radius)
			{
				return null;
			}
			float a = (float)(Math.Pow(ca.Radius, 2) - Math.Pow(cb.Radius, 2) + Math.Pow(distance, 2)) / (distance * 2f);
			float b = distance - a;
			float h = (float)Math.Sqrt(Math.Pow(ca.Radius, 2) - Math.Pow(a, 2));
			Vector2 p2 = ca.Center + (a * (cb.Center - ca.Center)) / distance;
			Vector2 intersection1 = new Vector2()
			{
				X = p2.X + h * (cb.Center.Y - ca.Center.Y) / distance,
				Y = p2.Y - h * (cb.Center.X - ca.Center.X) / distance
			};
			Vector2 intersection2 = new Vector2()
			{
				X = p2.X - h * (cb.Center.Y - ca.Center.Y) / distance,
				Y = p2.Y + h * (cb.Center.X - ca.Center.X) / distance
			};
			return float.IsNaN(intersection1.X) ? null : new []{ intersection1, intersection2, new Vector2(a, b), p2 };
		}

		public static void ClampVectorToInt(ref Vector2 pos)
		{
			pos.X = (int)pos.X;
			pos.Y = (int)pos.Y;
		}

		public static bool ClickedRect(Rectangle toClick, InputState input)
		{
		    return input.InGameSelect && CheckIntersection(toClick, input.CursorPosition);
		}

        private static FleetDesign LoadFleetDesign(string fleetUid)
        {
            string designPath = fleetUid + ".xml";
            FileInfo info = ResourceManager.GetModOrVanillaFile(designPath);

            if (info == null)
            {
                info = new FileInfo(Dir.ApplicationData + "/StarDrive/Fleet Designs/" + designPath);
            }
			return info.Deserialize<FleetDesign>();
        }

        private static Fleet CreateFleetFromData(FleetDesign data, Empire owner, Vector2 position)
        {
            Fleet fleet = new Fleet
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
                    Ship s = ResourceManager.CreateShipAtPoint(node.ShipName, owner, position + node.FleetOffset);
                    s.RelativeFleetOffset = node.FleetOffset;
                    node.Ship = s;
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
			owner.FirstFleet = CreateDefensiveFleetAt(fleetUid, owner, position);
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

                Log.Info("Compressed {0} from {1} to {2} bytes.", fi.Name, fi.Length, outFile.Length);
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
				Log.Info("Decompressed: {0}", fi.Name);
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
		public static void DrawDropShadowText(ScreenManager screenManager, string text, Vector2 pos, SpriteFont font, Color c, float offs = 2f)
		{
			pos.X = (int)pos.X;
			pos.Y = (int)pos.Y;
			screenManager.SpriteBatch.DrawString(font, text, pos + new Vector2(offs), Color.Black);
			screenManager.SpriteBatch.DrawString(font, text, pos, c);
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
				Primitives2D.DrawLine(spriteBatch, origin, end, color, 2f);
                origin.X += xsize;
                end.X    += xsize;
			}
            origin = new Vector2(xpos, ypos);
            end    = new Vector2(xpos + xGridSize - 3, ypos);
			for (int y = 0; y < numberYs; ++y)
			{
				Primitives2D.DrawLine(spriteBatch, origin, end, color, 2f);
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
			float d = (float)Math.Sqrt((double)((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1)));
			if (n / d > r)
			{
				return false;
			}
			if ((float)Math.Sqrt((double)((x0 - x1) * (x0 - x1) + (y0 - y1) * (y0 - y1))) - r > d)
			{
				return false;
			}
			if ((float)Math.Sqrt((double)((x0 - x2) * (x0 - x2) + (y0 - y2) * (y0 - y2))) - r > d)
			{
				return false;
			}
			return true;
		}

		public static bool IsPointInCircle(Vector2 circleCenter, float radius, Vector2 point)
		{
            float dx = circleCenter.X - point.X;
            float dy = circleCenter.Y - point.Y;
			return dx*dx + dy*dy <= radius*radius;
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

        // Added by McShooterz: module repair priority list
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

            Log.Info(ConsoleColor.DarkYellow, " ========= CollectMemory ========= ");
            float before = GC.GetTotalMemory(false) / (1024f * 1024f);
            float after  = GC.GetTotalMemory(forceFullCollection: true) / (1024f * 1024f);
            Log.Info(ConsoleColor.DarkYellow, "   Before: {0:0.0}MB  After: {1:0.0}MB", before, after);
            Log.Info(ConsoleColor.DarkYellow, " ================================= ");
        }

        public static void CollectMemorySilent()
        {
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public static float ProcessMemoryMb => Process.GetCurrentProcess().WorkingSet64 / (1024f * 1024f);
    }
}