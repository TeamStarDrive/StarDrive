using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml.Serialization;

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
            FileInfo info;
            if (GlobalStats.ActiveMod != null && Directory.Exists(ResourceManager.WhichModPath + "/FleetDesigns") 
                && File.Exists(ResourceManager.WhichModPath + "/FleetDesigns/" + designPath))
            {
                info = new FileInfo(ResourceManager.WhichModPath + "/FleetDesigns/" + designPath);
            }
			else if (File.Exists("Content/FleetDesigns/" + designPath))
			{
				info = new FileInfo("Content/FleetDesigns/" + designPath);
			}
			else
			{
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				info = new FileInfo(appData + "/StarDrive/Fleet Designs/" + designPath);
			}
			var serializer1 = new XmlSerializer(typeof(FleetDesign));
			return (FleetDesign)serializer1.Deserialize(info.OpenRead());
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

            fleet.DataNodes.thisLock.EnterWriteLock();
			foreach (FleetDataNode node in fleet.DataNodes)
			{
				Ship s = ResourceManager.CreateShipAtPoint(node.ShipName, owner, position + node.FleetOffset);
				s.RelativeFleetOffset = node.FleetOffset;
				node.SetShip(s);
				fleet.AddShip(s);
			}
            fleet.DataNodes.thisLock.ExitWriteLock();
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

            var inData = File.ReadAllBytes(fi.FullName);
			using (FileStream outFile = File.Create(fi.FullName + ".gz"))
			using (GZipStream compress = new GZipStream(outFile, CompressionMode.Compress))
			{
				compress.Write(inData, 0, inData.Length);
				Console.WriteLine("Compressed {0} from {1} to {2} bytes.", fi.Name, fi.Length, outFile.Length);
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
				Console.WriteLine("Decompressed: {0}", fi.Name);
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
            var color = new Color(211, 211, 211, 70);
			for (int x = 0; x < numberXs; x++)
			{
				Vector2 origin = new Vector2(xpos + 1 + x * xsize, ypos);
				Vector2 end    = new Vector2(xpos + x * xsize, ypos + yGridSize - 1);
				Primitives2D.DrawLine(spriteBatch, origin, end, color, 2f);
			}
			for (int y = 0; y < numberYs; y++)
			{
				Vector2 origin = new Vector2(xpos, ypos + y * ysize);
				Vector2 end    = new Vector2(xpos + xGridSize - 3, ypos + y * ysize);
				Primitives2D.DrawLine(spriteBatch, origin, end, color, 2f);
			}
		}

		public static void DrawStroke(SpriteFont font, SpriteBatch sb, string text, Color backColor, Color frontColor, float scale, float rotation, Vector2 position)
		{
			Vector2 origin = new Vector2(font.MeasureString(text).X / 2f, font.MeasureString(text).Y / 2f);
			sb.DrawString(font, text, position + new Vector2(1f * scale, 1f * scale), backColor, rotation, origin, scale, SpriteEffects.None, 1f);
			sb.DrawString(font, text, position + new Vector2(-1f * scale, -1f * scale), backColor, rotation, origin, scale, SpriteEffects.None, 1f);
			sb.DrawString(font, text, position + new Vector2(-1f * scale, 1f * scale), backColor, rotation, origin, scale, SpriteEffects.None, 1f);
			sb.DrawString(font, text, position + new Vector2(1f * scale, -1f * scale), backColor, rotation, origin, scale, SpriteEffects.None, 1f);
			sb.DrawString(font, text, position, frontColor, rotation, origin, scale, SpriteEffects.None, 1f);
		}

		public static float findAngleToTarget(Vector2 origin, Vector2 target)
		{
			float theta;
			float tX = target.X;
			float tY = target.Y;
			float centerX = origin.X;
			float centerY = origin.Y;
			float angle_to_target = 0f;
			if (tX > centerX && tY < centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				theta = theta * 180f / 3.14159274f;
				angle_to_target = 90f - Math.Abs(theta);
			}
			else if (tX > centerX && tY > centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				angle_to_target = 90f + theta * 180f / 3.14159274f;
			}
			else if (tX < centerX && tY > centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				theta = theta * 180f / 3.14159274f;
				angle_to_target = 270f - Math.Abs(theta);
				angle_to_target = -angle_to_target;
			}
			else if (tX < centerX && tY < centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				angle_to_target = 270f + theta * 180f / 3.14159274f;
				angle_to_target = -angle_to_target;
			}
			if (tX == centerX && tY < centerY)
			{
				angle_to_target = 0f;
			}
			else if (tX > centerX && tY == centerY)
			{
				angle_to_target = 90f;
			}
			else if (tX == centerX && tY > centerY)
			{
				angle_to_target = 180f;
			}
			else if (tX < centerX && tY == centerY)
			{
				angle_to_target = 270f;
			}
			if (float.IsNaN(angle_to_target))
			{
				return 0f;
			}
			return Math.Abs(angle_to_target);
		}

		public static Vector2 FindPointFromAngleAndDistance(Vector2 position, float angle, float distance)
		{
			Vector2 targetPosition = new Vector2(0f, 0f);
			float gamma = angle;
			if (gamma > 360f)
				gamma -= 360f;

            const float pidiv180 = 3.14159274f / 180f;
			if (gamma < 90f)
			{
				float theta = (90f - gamma) * pidiv180;
				float oppY = distance * (float)Math.Sin(theta);
				float adjX = distance * (float)Math.Cos(theta);
				targetPosition.X = position.X + adjX;
				targetPosition.Y = position.Y - oppY;
			}
			else if (gamma > 90f && gamma < 180f)
			{
				float theta = (gamma - 90f) * pidiv180;
				float oppY = distance * (float)Math.Sin(theta);
				float adjX = distance * (float)Math.Cos(theta);
				targetPosition.X = position.X + adjX;
				targetPosition.Y = position.Y + oppY;
			}
			else if (gamma > 180f && gamma < 270f)
			{
				float theta = (270f - gamma) * pidiv180;
				float oppY = distance * (float)Math.Sin(theta);
				float adjX = distance * (float)Math.Cos(theta);
				targetPosition.X = position.X - adjX;
				targetPosition.Y = position.Y + oppY;
			}
			else if (gamma > 270f && gamma < 360f)
			{
				float theta = (gamma - 270f) * pidiv180;
				float oppY = distance * (float)Math.Sin(theta);
				float adjX = distance * (float)Math.Cos(theta);
				targetPosition.X = position.X - adjX;
				targetPosition.Y = position.Y - oppY;
			}
			return targetPosition;
		}

		public static Vector2 findPointFromAngleAndDistanceUsingRadians(Vector2 position, float angle, float distance)
		{
			float theta;
			Vector2 TargetPosition = new Vector2(0f, 0f);
			float gamma = MathHelper.ToDegrees(angle);
			float D = distance;
			int gammaQuadrant = 0;
			float oppY = 0f;
			float adjX = 0f;
			if (gamma >= 360f)
			{
				gamma = gamma - 360f;
			}
			if (gamma < 90f)
			{
				theta = 90f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin(theta);
				adjX = D * (float)Math.Cos(theta);
				gammaQuadrant = 1;
			}
			else if (gamma > 90f && gamma < 180f)
			{
				theta = gamma - 90f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 2;
			}
			else if (gamma > 180f && gamma < 270f)
			{
				theta = 270f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 3;
			}
			else if (gamma > 270f && gamma < 360f)
			{
				theta = gamma - 270f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 4;
			}
			if (gamma == 0f)
			{
				TargetPosition.X = position.X;
				TargetPosition.Y = position.Y - D;
			}
			if (gamma == 90f)
			{
				TargetPosition.X = position.X + D;
				TargetPosition.Y = position.Y;
			}
			if (gamma == 180f)
			{
				TargetPosition.X = position.X;
				TargetPosition.Y = position.Y + D;
			}
			if (gamma == 270f)
			{
				TargetPosition.X = position.X - D;
				TargetPosition.Y = position.Y;
			}
			if (gammaQuadrant == 1)
			{
				TargetPosition.X = position.X + adjX;
				TargetPosition.Y = position.Y - oppY;
			}
			else if (gammaQuadrant == 2)
			{
				TargetPosition.X = position.X + adjX;
				TargetPosition.Y = position.Y + oppY;
			}
			else if (gammaQuadrant == 3)
			{
				TargetPosition.X = position.X - adjX;
				TargetPosition.Y = position.Y + oppY;
			}
			else if (gammaQuadrant == 4)
			{
				TargetPosition.X = position.X - adjX;
				TargetPosition.Y = position.Y - oppY;
			}
			return TargetPosition;
		}

		public static Vector2 FindVectorToTarget(Vector2 Origin, Vector2 Target)
		{
			return Vector2.Normalize(Target - Origin);
		}

		public static Vector2 GeneratePointOnCircle(float angle, Vector2 center, float radius)
		{
			if (angle >= 360f)
			{
				angle = angle - 360f;
			}
			return FindPointFromAngleAndDistance(center, angle, radius);
		}

		public static FileInfo[] GetFilesFromDirectory(string DirPath)
		{
			return (new DirectoryInfo(DirPath)).GetFiles("*.*", SearchOption.TopDirectoryOnly);
		}

		public static FileInfo[] GetFilesFromDirectoryAndSubs(string DirPath)
		{
			return (new DirectoryInfo(DirPath)).GetFiles("*.*", SearchOption.AllDirectories);
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

		public static bool IsPointInCircle(Vector2 CicleCenter, float Radius, Vector2 Point)
		{
			float square_dist = (float)Math.Pow((double)(CicleCenter.X - Point.X), 2) + (float)Math.Pow((double)(CicleCenter.Y - Point.Y), 2);
			return (double)square_dist <= Math.Pow((double)Radius, 2);
		}

		public static string parseText(SpriteFont Font, string text, float Width)
		{
			string line = string.Empty;
			string returnString = string.Empty;
			string[] wordArray = text.Split(new char[] { ' ' });
			for (int i = 0; i < (int)wordArray.Length; i++)
			{
                if (wordArray[i] == null)
                    break;
				string word = wordArray[i];
				if (Font.MeasureString(string.Concat(line, word)).Length() > Width)
				{
					returnString = string.Concat(returnString, line, '\n');
					line = string.Empty;
				}
				else if (word == "\\n" || word == "\n")
				{
					word = "";
					returnString = string.Concat(returnString, line, '\n');
					line = string.Empty;
				}
				if (!string.IsNullOrEmpty(word))
				{
					line = string.Concat(line, word, ' ');
				}
			}
			return string.Concat(returnString, line);
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

        //Added by McShooterz: module repair priority list
        public static int ModulePriority(ShipModule ShipModule)
        {
            if (ShipModule.ModuleType == ShipModuleType.Command)
                return 0;
            if (ShipModule.ModuleType == ShipModuleType.PowerPlant)
                return 1;
            if (ShipModule.ModuleType == ShipModuleType.PowerConduit)
                return 2;
            if (ShipModule.ModuleType == ShipModuleType.Engine)
                return 3;
            if (ShipModule.ModuleType == ShipModuleType.Shield)
                return 4;
            if (ShipModule.ModuleType == ShipModuleType.Armor)
                return 6;
            return 5;
        }
	}
}