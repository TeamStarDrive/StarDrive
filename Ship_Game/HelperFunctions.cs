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
	internal class HelperFunctions
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
			Vector3[] points = new Vector3[] { new Vector3(outPoints[0], BeamZ), new Vector3(outPoints[1], BeamZ), new Vector3(outPoints[2], BeamZ), new Vector3(outPoints[3], BeamZ) };
			return points;
		}

		public static bool CheckIntersection(Rectangle rect, Vector2 pos)
		{
			if (pos.X > (float)rect.X && pos.X < (float)(rect.X + rect.Width) && pos.Y > (float)rect.Y && pos.Y < (float)(rect.Y + rect.Height))
			{
				return true;
			}
			return false;
		}

		public static Vector2[] CircleIntersection(Circle A, Circle B)
		{
			float Distance = Vector2.Distance(A.Center, B.Center);
			if (Distance > A.Radius + B.Radius)
			{
				return null;
			}
			float a = (float)(Math.Pow((double)A.Radius, 2) - Math.Pow((double)B.Radius, 2) + Math.Pow((double)Distance, 2)) / (Distance * 2f);
			float b = Distance - a;
			float h = (float)Math.Sqrt(Math.Pow((double)A.Radius, 2) - Math.Pow((double)a, 2));
			Vector2 P2 = A.Center + ((a * (B.Center - A.Center)) / Distance);
			Vector2 Intersection1 = new Vector2()
			{
				X = P2.X + h * (B.Center.Y - A.Center.Y) / Distance,
				Y = P2.Y - h * (B.Center.X - A.Center.X) / Distance
			};
			Vector2 Intersection2 = new Vector2()
			{
				X = P2.X - h * (B.Center.Y - A.Center.Y) / Distance,
				Y = P2.Y + h * (B.Center.X - A.Center.X) / Distance
			};
			if (float.IsNaN(Intersection1.X))
			{
				return null;
			}
			Vector2[] VecArray = new Vector2[] { Intersection1, Intersection2, new Vector2(a, b), P2 };
			return VecArray;
		}

		public static void ClampVectorToInt(ref Vector2 pos)
		{
			pos.X = (float)((int)pos.X);
			pos.Y = (float)((int)pos.Y);
		}

		public static bool ClickedRect(Rectangle toClick, InputState input)
		{
			if (HelperFunctions.CheckIntersection(toClick, input.CursorPosition) && input.InGameSelect)
			{
				return true;
			}
			return false;
		}

		public static void Compress(FileInfo fi)
		{
			using (FileStream inFile = fi.OpenRead())
			{
				if ((File.GetAttributes(fi.FullName) & FileAttributes.Hidden) != FileAttributes.Hidden & (fi.Extension != ".gz"))
				{
					using (FileStream outFile = File.Create(string.Concat(fi.FullName, ".gz")))
					{
						using (GZipStream Compress = new GZipStream(outFile, CompressionMode.Compress))
						{
							byte[] buffer = new byte[4096];
							while (true)
							{
								int num = inFile.Read(buffer, 0, (int)buffer.Length);
								int numRead = num;
								if (num == 0)
								{
									break;
								}
								Compress.Write(buffer, 0, numRead);
							}
							string name = fi.Name;
							string str = fi.Length.ToString();
							long length = outFile.Length;
							Console.WriteLine("Compressed {0} from {1} to {2} bytes.", name, str, length.ToString());
						}
					}
				}
			}
		}

		public static Fleet CreateDefensiveFleetAt(string FleetUID, Empire Owner, Vector2 Position)
		{
			FileInfo theFleetFI;
			if (File.Exists(string.Concat("Content/FleetDesigns/", FleetUID, ".xml")))
			{
				theFleetFI = new FileInfo(string.Concat("Content/FleetDesigns/", FleetUID, ".xml"));
			}
			else
			{
				string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				theFleetFI = new FileInfo(string.Concat(path, "/StarDrive/Fleet Designs/", FleetUID, ".xml"));
			}
			XmlSerializer serializer1 = new XmlSerializer(typeof(FleetDesign));
			FleetDesign data = (FleetDesign)serializer1.Deserialize(theFleetFI.OpenRead());
			Fleet fleet = new Fleet()
			{
				Position = Position,
				Owner = Owner,
				DataNodes = new BatchRemovalCollection<FleetDataNode>()
			};
			foreach (FleetDataNode node in data.Data)
			{
				fleet.DataNodes.Add(node);
			}
			fleet.Name = data.Name;
			fleet.FleetIconIndex = data.FleetIconIndex;
			foreach (FleetDataNode node in fleet.DataNodes)
			{
				Ship s = ResourceManager.CreateShipAtPoint(node.ShipName, Owner, Position + node.FleetOffset);
				s.RelativeFleetOffset = node.FleetOffset;
				node.SetShip(s);
				fleet.AddShip(s);
			}
			return fleet;
		}

		public static void CreateFleetAt(string FleetUID, Empire Owner, Vector2 Position)
		{
			FileInfo theFleetFI;
			if (File.Exists(string.Concat("Content/FleetDesigns/", FleetUID, ".xml")))
			{
				theFleetFI = new FileInfo(string.Concat("Content/FleetDesigns/", FleetUID, ".xml"));
			}
			else
			{
				string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				theFleetFI = new FileInfo(string.Concat(path, "/StarDrive/Fleet Designs/", FleetUID, ".xml"));
			}
			XmlSerializer serializer1 = new XmlSerializer(typeof(FleetDesign));
			FleetDesign data = (FleetDesign)serializer1.Deserialize(theFleetFI.OpenRead());
			Fleet fleet = new Fleet()
			{
				Position = Position,
				Owner = Owner,
				DataNodes = new BatchRemovalCollection<FleetDataNode>()
			};
			foreach (FleetDataNode node in data.Data)
			{
				fleet.DataNodes.Add(node);
			}
			fleet.Name = data.Name;
			fleet.FleetIconIndex = data.FleetIconIndex;
			foreach (FleetDataNode node in fleet.DataNodes)
			{
				Ship s = ResourceManager.CreateShipAtPoint(node.ShipName, Owner, Position + node.FleetOffset);
				s.RelativeFleetOffset = node.FleetOffset;
				node.SetShip(s);
				fleet.AddShip(s);
			}
			foreach (Ship s in Owner.GetFleetsDict()[1].Ships)
			{
				s.fleet = null;
			}
			Owner.GetFleetsDict()[1] = fleet;
		}

		public static void CreateFleetFromDataAt(FleetDesign data, Empire Owner, Vector2 Position)
		{
			Fleet fleet = new Fleet()
			{
				Position = Position,
				Owner = Owner,
				DataNodes = new BatchRemovalCollection<FleetDataNode>()
			};
			foreach (FleetDataNode node in data.Data)
			{
				fleet.DataNodes.Add(node);
			}
			fleet.Name = data.Name;
			fleet.FleetIconIndex = data.FleetIconIndex;
			foreach (FleetDataNode node in fleet.DataNodes)
			{
				Ship s = ResourceManager.CreateShipAtPoint(node.ShipName, Owner, Position + node.FleetOffset);
				s.RelativeFleetOffset = node.FleetOffset;
				node.SetShip(s);
				fleet.AddShip(s);
			}
			foreach (Ship s in Owner.GetFleetsDict()[1].Ships)
			{
				s.fleet = null;
			}
			Owner.GetFleetsDict()[1] = fleet;
		}

		public static void CreateFleetFromDataAt(FleetDesign data, Empire Owner, Vector2 Position, float facing)
		{
			Fleet fleet = new Fleet()
			{
				Position = Position,
				facing = facing,
				Owner = Owner,
				DataNodes = new BatchRemovalCollection<FleetDataNode>()
			};
			foreach (FleetDataNode node in data.Data)
			{
				fleet.DataNodes.Add(node);
			}
			fleet.AssignDataPositions(facing);
			fleet.Name = data.Name;
			fleet.FleetIconIndex = data.FleetIconIndex;
			foreach (FleetDataNode node in fleet.DataNodes)
			{
				Ship s = ResourceManager.CreateShipAtPoint(node.ShipName, Owner, Position + node.OrdersOffset, facing);
				s.RelativeFleetOffset = node.FleetOffset;
				node.SetShip(s);
				fleet.AddShip(s);
			}
			foreach (Ship s in Owner.GetFleetsDict()[1].Ships)
			{
				s.fleet = null;
			}
			Owner.GetFleetsDict()[1] = fleet;
		}

		public static string Decompress(FileInfo fi)
		{
			string str;
			using (FileStream inFile = fi.OpenRead())
			{
				string curFile = fi.FullName;
				string origName = curFile.Remove(curFile.Length - fi.Extension.Length);
				using (FileStream outFile = File.Create(origName))
				{
					using (GZipStream Decompress = new GZipStream(inFile, CompressionMode.Decompress))
					{
						byte[] buffer = new byte[4096];
						while (true)
						{
							int num = Decompress.Read(buffer, 0, (int)buffer.Length);
							int numRead = num;
							if (num == 0)
							{
								break;
							}
							outFile.Write(buffer, 0, numRead);
						}
						Console.WriteLine("Decompressed: {0}", fi.Name);
						str = origName;
					}
				}
			}
			return str;
		}

		public static void DrawDropShadowImage(Ship_Game.ScreenManager ScreenManager, Rectangle rect, string Texture, Color TopColor)
		{
			Rectangle offsetRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width, rect.Height);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[Texture], offsetRect, Color.Black);
			ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict[Texture], rect, TopColor);
		}

		public static void DrawDropShadowImage(Ship_Game.ScreenManager ScreenManager, Rectangle rect, Texture2D Texture, Color TopColor)
		{
			Rectangle offsetRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width, rect.Height);
			ScreenManager.SpriteBatch.Draw(Texture, offsetRect, Color.Black);
			ScreenManager.SpriteBatch.Draw(Texture, rect, TopColor);
		}

		public static void DrawDropShadowText(Ship_Game.ScreenManager ScreenManager, string Text, Vector2 Pos, SpriteFont Font)
		{
			Pos.X = (float)((int)Pos.X);
			Pos.Y = (float)((int)Pos.Y);
			Vector2 offset = new Vector2(2f, 2f);
			ScreenManager.SpriteBatch.DrawString(Font, Text, Pos + offset, Color.Black);
			ScreenManager.SpriteBatch.DrawString(Font, Text, Pos, Color.White);
		}

		public static void DrawDropShadowText(Ship_Game.ScreenManager ScreenManager, string Text, Vector2 Pos, SpriteFont Font, Color c)
		{
			Pos.X = (float)((int)Pos.X);
			Pos.Y = (float)((int)Pos.Y);
			Vector2 offset = new Vector2(2f, 2f);
			ScreenManager.SpriteBatch.DrawString(Font, Text, Pos + offset, Color.Black);
			ScreenManager.SpriteBatch.DrawString(Font, Text, Pos, c);
		}

		public static void DrawDropShadowText1(Ship_Game.ScreenManager ScreenManager, string Text, Vector2 Pos, SpriteFont Font, Color c)
		{
			Pos.X = (float)((int)Pos.X);
			Pos.Y = (float)((int)Pos.Y);
			Vector2 offset = new Vector2(1f, 1f);
			ScreenManager.SpriteBatch.DrawString(Font, Text, Pos + offset, Color.Black);
			ScreenManager.SpriteBatch.DrawString(Font, Text, Pos, c);
		}

		public static void DrawGrid(SpriteBatch spriteBatch, int xpos, int ypos, int xGridSize, int yGridSize, int numberXs, int numberYs)
		{
			int xsize = xGridSize / numberXs;
			int ysize = yGridSize / numberYs;
			for (int x = 0; x < numberXs; x++)
			{
				Vector2 origin = new Vector2((float)(xpos + 1 + x * xsize), (float)ypos);
				Vector2 end = new Vector2((float)(xpos + x * xsize), (float)(ypos + yGridSize - 1));
				Primitives2D.DrawLine(spriteBatch, origin, end, new Color(211, 211, 211, 70), 2f);
			}
			for (int y = 0; y < numberYs; y++)
			{
				Vector2 origin = new Vector2((float)xpos, (float)(ypos + y * ysize));
				Vector2 end = new Vector2((float)(xpos + xGridSize - 3), (float)(ypos + y * ysize));
				Primitives2D.DrawLine(spriteBatch, origin, end, new Color(211, 211, 211, 70), 2f);
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

		public static Vector2 findPointFromAngleAndDistance(Vector2 position, float angle, float distance)
		{
			float theta;
			Vector2 TargetPosition = new Vector2(0f, 0f);
			float gamma = angle;
			float D = distance;
			int gammaQuadrant = 0;
			float oppY = 0f;
			float adjX = 0f;
			if (gamma > 360f)
			{
				gamma = gamma - 360f;
			}
			if (gamma < 90f)
			{
				theta = 90f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
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
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
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
			return HelperFunctions.findPointFromAngleAndDistance(center, angle, radius);
		}

		public static FileInfo[] GetFilesFromDirectory(string DirPath)
		{
			return (new DirectoryInfo(DirPath)).GetFiles("*.*", SearchOption.TopDirectoryOnly);
		}

		public static FileInfo[] GetFilesFromDirectoryAndSubs(string DirPath)
		{
			return (new DirectoryInfo(DirPath)).GetFiles("*.*", SearchOption.AllDirectories);
		}

		public static int GetRandomIndex(int Count)
		{
			int Random = (int)RandomMath.RandomBetween(0f, (float)Count + 0.95f);
			if (Random > Count - 1)
			{
				Random = Count - 1;
			}
			return Random;
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
				if (word != "")
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
				if (word == "")
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
				if (word != "")
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
				else if (sent == "" && (int)lineArray.Length > i + 1 && lineArray[i + 1] != "")
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
            if (ShipModule.ModuleType == ShipModuleType.Armor || ShipModule.ModuleType == ShipModuleType.Dummy)
                return 6;
            return 5;
        }
	}
}