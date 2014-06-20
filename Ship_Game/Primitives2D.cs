using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public static class Primitives2D
	{
		private readonly static Dictionary<string, List<Vector2>> m_arcCache;

		private static Texture2D m_pixel;

		static Primitives2D()
		{
			Primitives2D.m_arcCache = new Dictionary<string, List<Vector2>>();
		}

		public static void BracketRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color, int BracketSize)
		{
			Primitives2D.DrawLine(spriteBatch, new Vector2((float)(rect.X - 1), (float)rect.Y), new Vector2((float)(rect.X + BracketSize - 1), (float)rect.Y), color);
			Primitives2D.DrawLine(spriteBatch, new Vector2((float)rect.X, (float)(rect.Y + 1)), new Vector2((float)rect.X, (float)(rect.Y + BracketSize)), color);
			Primitives2D.DrawLine(spriteBatch, new Vector2((float)(rect.X + rect.Width - BracketSize), (float)rect.Y), new Vector2((float)(rect.X + rect.Width), (float)rect.Y), color);
			Primitives2D.DrawLine(spriteBatch, new Vector2((float)(rect.X + rect.Width), (float)(rect.Y + 1)), new Vector2((float)(rect.X + rect.Width), (float)(rect.Y + BracketSize)), color);
			Primitives2D.DrawLine(spriteBatch, new Vector2((float)(rect.X + rect.Width - BracketSize + 1), (float)(rect.Y + rect.Height)), new Vector2((float)(rect.X + rect.Width + 1), (float)(rect.Y + rect.Height)), color);
			Primitives2D.DrawLine(spriteBatch, new Vector2((float)(rect.X + rect.Width), (float)(rect.Y + rect.Height)), new Vector2((float)(rect.X + rect.Width), (float)(rect.Y + rect.Height - BracketSize + 1)), color);
			Primitives2D.DrawLine(spriteBatch, new Vector2((float)rect.X, (float)(rect.Y + rect.Height)), new Vector2((float)(rect.X + BracketSize), (float)(rect.Y + rect.Height)), color);
			Primitives2D.DrawLine(spriteBatch, new Vector2((float)rect.X, (float)(rect.Y + rect.Height)), new Vector2((float)rect.X, (float)(rect.Y + rect.Height - BracketSize + 1)), color);
		}

		public static void BracketRectangle(SpriteBatch spriteBatch, Vector2 Position, float Radius, Color color)
		{
			Vector2 TL = Position + new Vector2(-(Radius + 3f), -(Radius + 3f));
			Vector2 BL = Position + new Vector2(-(Radius + 3f), Radius);
			Vector2 TR = Position + new Vector2(Radius, -(Radius + 3f));
			Vector2 BR = Position + new Vector2(Radius, Radius);
			spriteBatch.Draw(ResourceManager.TextureDict["UI/bracket_TR"], TR, color);
			spriteBatch.Draw(ResourceManager.TextureDict["UI/bracket_TL"], TL, color);
			spriteBatch.Draw(ResourceManager.TextureDict["UI/bracket_BR"], BR, color);
			spriteBatch.Draw(ResourceManager.TextureDict["UI/bracket_BL"], BL, color);
		}

		private static List<Vector2> CreateArc(float radius, int sides, float startingAngle, float degrees)
		{
			List<Vector2> points = new List<Vector2>();
			points.AddRange(Primitives2D.CreateCircle((double)radius, sides));
			points.RemoveAt(points.Count - 1);
			double curAngle = 0;
			double anglePerSide = 360 / (double)sides;
			while (curAngle + anglePerSide / 2 < (double)startingAngle)
			{
				curAngle = curAngle + anglePerSide;
				points.Add(points[0]);
				points.RemoveAt(0);
			}
			points.Add(points[0]);
			int sidesInArc = (int)((double)degrees / anglePerSide + 0.5);
			points.RemoveRange(sidesInArc + 1, points.Count - sidesInArc - 1);
			return points;
		}

		private static List<Vector2> CreateArc2(float radius, int sides, float startingAngle, float endingAngle)
		{
			double theta;
			object[] objArray = new object[] { radius, "x", sides, "x", startingAngle, "x", endingAngle };
			string arcKey = string.Concat(objArray);
			if (Primitives2D.m_arcCache.ContainsKey(arcKey))
			{
				return Primitives2D.m_arcCache[arcKey];
			}
			List<Vector2> points = new List<Vector2>();
			double startRadians = 3.14159265358979 * (double)startingAngle / 180;
			double endRadians = 3.14159265358979 * (double)endingAngle / 180;
			if (startRadians >= endRadians)
			{
				endRadians = endRadians + 6.28318530717959;
			}
			double step = (endRadians - startRadians) / (double)sides;
			for (theta = startRadians; theta < endRadians; theta = theta + step)
			{
				Vector2 r2 = new Vector2((float)((double)radius * Math.Cos(theta)), (float)((double)radius * Math.Sin(theta)));
				points.Add(r2);
			}
			Vector2 r3 = new Vector2((float)((double)radius * Math.Cos(theta)), (float)((double)radius * Math.Sin(theta)));
			points.Add(r3);
			Primitives2D.m_arcCache.Add(arcKey, points);
			return points;
		}

		public static List<Vector2> CreateArc3(float radius, int sides, float startingAngle, float endingAngle)
		{
			Vector2 startIntersect;
			Vector2 endIntersect;
			object[] objArray = new object[] { radius, "x", sides, "x", startingAngle, "x", endingAngle };
			string arcKey = string.Concat(objArray);
			if (Primitives2D.m_arcCache.ContainsKey(arcKey))
			{
				return Primitives2D.m_arcCache[arcKey];
			}
			List<Vector2> points = new List<Vector2>();
			double radiansPerSide = 6.28318530717959 / (double)sides;
			double startRadians = 3.14159265358979 * (double)startingAngle / 180;
			double endRadians = 3.14159265358979 * (double)endingAngle / 180;
			if (startRadians >= endRadians)
			{
				endRadians = endRadians + 6.28318530717959;
			}
			int start = (int)Math.Floor(startRadians / radiansPerSide);
			int end = (int)Math.Floor(endRadians / radiansPerSide);
			double theta = 0;
			for (theta = (double)start * radiansPerSide; theta <= (double)end * radiansPerSide; theta = theta + radiansPerSide)
			{
				Vector2 r = new Vector2((float)((double)radius * Math.Cos(theta)), (float)((double)radius * Math.Sin(theta)));
				points.Add(r);
			}
			Vector2 r1 = new Vector2((float)((double)radius * Math.Cos(theta)), (float)((double)radius * Math.Sin(theta)));
			points.Add(r1);
			if (points.Count > 1)
			{
				Vector2 startPoint = new Vector2((float)((double)radius * Math.Cos(startRadians)), (float)((double)radius * Math.Sin(startRadians)));
				Vector2 endPoint = new Vector2((float)((double)radius * Math.Cos(endRadians)), (float)((double)radius * Math.Sin(endRadians)));
				Vector2 center = new Vector2(0f, 0f);
				bool changeStart = false;
				bool changeEnd = false;
				if (Primitives2D.LineIntersect(center, startPoint, points[0], points[1], out startIntersect))
				{
					changeStart = true;
				}
				if (Primitives2D.LineIntersect(center, endPoint, points[points.Count - 2], points[points.Count - 1], out endIntersect))
				{
					changeEnd = true;
				}
				if (changeStart)
				{
					points[0] = startIntersect;
				}
				if (changeEnd)
				{
					points[points.Count - 1] = endIntersect;
				}
			}
			return points;
		}

		private static List<Vector2> CreateCircle(double radius, int sides)
		{
			List<Vector2> vectors = new List<Vector2>();
			double step = 6.28318530717959 / (double)sides;
			for (double theta = 0; theta < 6.28318530717959; theta = theta + step)
			{
				Vector2 r = new Vector2((float)(radius * Math.Cos(theta)), (float)(radius * Math.Sin(theta)));
				vectors.Add(r);
			}
			Vector2 r1 = new Vector2((float)(radius * Math.Cos(0)), (float)(radius * Math.Sin(0)));
			vectors.Add(r1);
			return vectors;
		}

		public static List<Vector2> CreateMyArc(SpriteBatch spriteBatch, Vector2 Center, float radius, int sides, float startingAngle, float degrees, Vector2 C0, Vector2 C1, Color c, float Thickness, List<Circle> Circles)
		{
			BatchRemovalCollection<Vector2> points = new BatchRemovalCollection<Vector2>();
			double curAngle = (double)MathHelper.ToRadians(startingAngle);
			double step = (double)(MathHelper.ToRadians(degrees) / (float)sides);
			for (double theta = curAngle + step; theta < curAngle + (double)MathHelper.ToRadians(degrees); theta = theta + step)
			{
				Vector2 r = new Vector2((float)((double)radius * Math.Cos(theta)), (float)((double)radius * Math.Sin(theta)));
				r = r + Center;
				points.Add(r);
			}
			foreach (Circle ToCheck in Circles)
			{
				foreach (Vector2 point in points)
				{
					if (!HelperFunctions.IsPointInCircle(ToCheck.Center, ToCheck.Radius - 2f, point))
					{
						continue;
					}
					points.QueuePendingRemoval(point);
				}
			}
			points.ApplyPendingRemovals();
			if (points.Count > 2)
			{
				if (Vector2.Distance(C0, points[1]) >= Vector2.Distance(C0, points[points.Count - 1]))
				{
					points[0] = C1;
					points[points.Count - 1] = C0;
				}
				else
				{
					points[0] = C0;
					points[points.Count - 1] = C1;
				}
			}
			return points;
		}

		private static List<Vector2> CreateMyCircle(Circle ToDraw, double radius, int sides, List<Circle> CircleList)
		{
			BatchRemovalCollection<Vector2> vectors = new BatchRemovalCollection<Vector2>();
			double step = 6.28318530717959 / (double)sides;
			for (double theta = 0; theta < 6.28318530717959; theta = theta + step)
			{
				Vector2 r = new Vector2((float)(radius * Math.Cos(theta)), (float)(radius * Math.Sin(theta)));
				vectors.Add(r);
			}
			Vector2 r1 = new Vector2((float)(radius * Math.Cos(0)), (float)(radius * Math.Sin(0)));
			r1 = r1 + ToDraw.Center;
			vectors.Add(r1);
			foreach (Circle ToCheck in CircleList)
			{
				foreach (Vector2 point in vectors)
				{
					if (Vector2.Distance(point, ToCheck.Center) >= ToCheck.Radius - 0.1f)
					{
						continue;
					}
					vectors.QueuePendingRemoval(point);
				}
			}
			return vectors;
		}

		private static void CreateThePixel(SpriteBatch spriteBatch)
		{
			Primitives2D.m_pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
			Primitives2D.m_pixel.SetData<Color>(new Color[] { Color.White });
		}

		public static void DrawArc(SpriteBatch spriteBatch, Vector2 center, float radius, int sides, float startingAngle, float degrees, Color color)
		{
			Primitives2D.DrawArc(spriteBatch, center, radius, sides, startingAngle, degrees, color, 1f);
		}

		public static Vector2[] DrawArc(SpriteBatch spriteBatch, Vector2 center, float radius, int sides, float startingAngle, float degrees, Color color, float thickness)
		{
			List<Vector2> arc = Primitives2D.CreateArc(radius, sides, startingAngle, degrees);
			Primitives2D.DrawPoints(spriteBatch, center, arc, color, thickness);
			Vector2[] ret = new Vector2[] { arc[0], arc[arc.Count - 1] };
			return ret;
		}

		public static void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, int sides, Color color)
		{
			Primitives2D.DrawPoints(spriteBatch, center, Primitives2D.CreateCircle((double)radius, sides), color, 1f);
		}

		public static void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, int sides, Color color, float thickness)
		{
			Primitives2D.DrawPoints(spriteBatch, center, Primitives2D.CreateCircle((double)radius, sides), color, thickness);
		}

		public static void DrawCircle(SpriteBatch spriteBatch, float x, float y, float radius, int sides, Color color)
		{
			Primitives2D.DrawPoints(spriteBatch, new Vector2(x, y), Primitives2D.CreateCircle((double)radius, sides), color, 1f);
		}

		public static void DrawCircle(SpriteBatch spriteBatch, float x, float y, float radius, int sides, Color color, float thickness)
		{
			Primitives2D.DrawPoints(spriteBatch, new Vector2(x, y), Primitives2D.CreateCircle((double)radius, sides), color, thickness);
		}

		public static void DrawLine(SpriteBatch spriteBatch, float x1, float y1, float x2, float y2, Color color)
		{
			Vector2 r = new Vector2(x1, y1);
			Vector2 r1 = new Vector2(x2, y2);
			Primitives2D.DrawLine(spriteBatch, r, r1, color, 1f);
		}

		public static void DrawLine(SpriteBatch spriteBatch, float x1, float y1, float x2, float y2, Color color, float thickness)
		{
			Vector2 v1 = new Vector2(x1, y1);
			Vector2 v2 = new Vector2(x2, y2);
			Primitives2D.DrawLine(spriteBatch, v1, v2, color, thickness);
		}

		public static void DrawLine(SpriteBatch spriteBatch, Vector2 point1, Vector2 point2, Color color)
		{
			Primitives2D.DrawLine(spriteBatch, point1, point2, color, 1f);
		}

		public static void DrawLine(SpriteBatch spriteBatch, Vector2 point1, Vector2 point2, Color color, float thickness)
		{
			float distance = Vector2.Distance(point1, point2);
			float angle = (float)Math.Atan2((double)(point2.Y - point1.Y), (double)(point2.X - point1.X));
			Primitives2D.DrawLine(spriteBatch, point1, distance, angle, color, thickness);
		}

		public static void DrawLine(SpriteBatch spriteBatch, Vector2 point, float length, float angle, Color color)
		{
			Primitives2D.DrawLine(spriteBatch, point, length, angle, color, 1f);
		}

		public static void DrawLine(SpriteBatch spriteBatch, Vector2 point, float length, float angle, Color color, float thickness)
		{
			if (Primitives2D.m_pixel == null)
			{
				Primitives2D.CreateThePixel(spriteBatch);
			}
			Vector2 v = new Vector2(length, thickness);
			Rectangle? nullable = null;
			spriteBatch.Draw(Primitives2D.m_pixel, point, nullable, color, angle, Vector2.Zero, v, SpriteEffects.None, 0f);
		}

		public static void DrawLowerLine(SpriteBatch spriteBatch, float x1, float y1, float x2, float y2, Color color)
		{
			Primitives2D.DrawLine(spriteBatch, new Vector2(x1, y1), new Vector2(x2, y2), color, 0.9f);
		}

		public static void DrawLowerLine(SpriteBatch spriteBatch, Vector2 point1, Vector2 point2, Color color)
		{
			Primitives2D.DrawLine(spriteBatch, point1, point2, color, 0.9f);
		}

		public static void DrawMyArc(SpriteBatch spriteBatch, Vector2 Center, float radius, int sides, float startingAngle, float degrees, Vector2 C0, Vector2 C1, Color c, float Thickness)
		{
			BatchRemovalCollection<Vector2> points = new BatchRemovalCollection<Vector2>();
			double curAngle = (double)MathHelper.ToRadians(startingAngle);
			double step = (double)(MathHelper.ToRadians(degrees) / (float)sides);
			for (double theta = curAngle; theta < curAngle + (double)MathHelper.ToRadians(degrees); theta = theta + step)
			{
				Vector2 r = new Vector2((float)((double)radius * Math.Cos(theta)), (float)((double)radius * Math.Sin(theta)));
				r = r + Center;
				points.Add(r);
			}
			if (points.Count > 0)
			{
				points[0] = C0;
				points.Add(C1);
			}
			Primitives2D.DrawMyPoints(spriteBatch, points, c, Thickness, radius);
		}

		public static void DrawMyArc(SpriteBatch spriteBatch, Vector2 Center, float radius, int sides, float startingAngle, float degrees, Vector2 C0, Vector2 C1, Color c, float Thickness, List<Circle> Circles)
		{
			BatchRemovalCollection<Vector2> points = new BatchRemovalCollection<Vector2>();
			double curAngle = (double)MathHelper.ToRadians(startingAngle);
			double step = (double)(MathHelper.ToRadians(degrees) / (float)sides);
			for (double theta = curAngle + step; theta < curAngle + (double)MathHelper.ToRadians(degrees); theta = theta + step)
			{
				Vector2 r = new Vector2((float)((double)radius * Math.Cos(theta)), (float)((double)radius * Math.Sin(theta)));
				r = r + Center;
				points.Add(r);
			}
			foreach (Circle ToCheck in Circles)
			{
				foreach (Vector2 point in points)
				{
					if (Vector2.Distance(point, ToCheck.Center) >= ToCheck.Radius - 5f)
					{
						continue;
					}
					points.QueuePendingRemoval(point);
				}
			}
			points.ApplyPendingRemovals();
			if (points.Count > 0)
			{
				points[0] = C0;
				points[points.Count - 1] = C1;
			}
			Primitives2D.DrawMyPoints(spriteBatch, points, c, Thickness, radius);
		}

		public static void DrawMyPoints(SpriteBatch spriteBatch, List<Vector2> points, Color color, float thickness, float Radius)
		{
			if (points.Count < 2)
			{
				return;
			}
			for (int i = 1; i < points.Count; i++)
			{
				if (Vector2.Distance(points[i - 1], points[i]) < 75f)
				{
					Primitives2D.DrawLine(spriteBatch, points[i - 1], points[i], color, thickness);
				}
			}
		}

		private static void DrawPoints(SpriteBatch spriteBatch, Vector2 position, List<Vector2> points, Color color)
		{
			Primitives2D.DrawPoints(spriteBatch, position, points, color, 1f);
		}

		private static void DrawPoints(SpriteBatch spriteBatch, Vector2 position, List<Vector2> points, Color color, float thickness)
		{
			if (points.Count < 2)
			{
				return;
			}
			for (int i = 1; i < points.Count; i++)
			{
				Primitives2D.DrawLine(spriteBatch, points[i - 1] + position, points[i] + position, color, thickness);
			}
		}

		public static void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color)
		{
			Primitives2D.DrawRectangle(spriteBatch, rect, color, 1f, 0f, new Vector2((float)rect.X, (float)rect.Y));
		}

		public static void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color, float thickness)
		{
			Primitives2D.DrawRectangle(spriteBatch, rect, color, thickness, 0f, new Vector2((float)rect.X, (float)rect.Y));
		}

		public static void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color, float thickness, float angle)
		{
			Primitives2D.DrawRectangle(spriteBatch, rect, color, thickness, angle, new Vector2((float)rect.X, (float)rect.Y));
		}

		public static void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color, float thickness, float angle, Vector2 rotateAround)
		{
			Primitives2D.DrawLine(spriteBatch, new Vector2((float)rect.X, (float)rect.Y), new Vector2((float)rect.Right, (float)rect.Y), color, thickness);
			Primitives2D.DrawLine(spriteBatch, new Vector2((float)rect.X, (float)rect.Y), new Vector2((float)rect.X, (float)rect.Bottom), color, thickness);
			Primitives2D.DrawLine(spriteBatch, new Vector2((float)rect.X - thickness, (float)rect.Bottom), new Vector2((float)rect.Right, (float)rect.Bottom), color, thickness);
			Primitives2D.DrawLine(spriteBatch, new Vector2((float)rect.Right, (float)rect.Y), new Vector2((float)rect.Right, (float)rect.Bottom), color, thickness);
		}

		public static void DrawRectangle(SpriteBatch spriteBatch, Vector2 location, Vector2 size, Color color)
		{
			Primitives2D.DrawRectangle(spriteBatch, new Rectangle((int)location.X, (int)location.Y, (int)size.X, (int)size.Y), color, 1f, 0f, location);
		}

		public static void DrawRectangle(SpriteBatch spriteBatch, Vector2 location, Vector2 size, Color color, float thickness)
		{
			Primitives2D.DrawRectangle(spriteBatch, new Rectangle((int)location.X, (int)location.Y, (int)size.X, (int)size.Y), color, thickness, 0f, location);
		}

		public static void DrawRectangle(SpriteBatch spriteBatch, Vector2 location, Vector2 size, Color color, float thickness, float angle)
		{
			Primitives2D.DrawRectangle(spriteBatch, new Rectangle((int)location.X, (int)location.Y, (int)size.X, (int)size.Y), color, thickness, angle, location);
		}

		public static void DrawRectangle(SpriteBatch spriteBatch, Vector2 location, Vector2 size, Color color, float thickness, float angle, Vector2 rotateAround)
		{
			Primitives2D.DrawRectangle(spriteBatch, new Rectangle((int)location.X, (int)location.Y, (int)size.X, (int)size.Y), color, thickness, angle, rotateAround);
		}

		public static void DrawRectangleGlow(SpriteBatch spriteBatch, Rectangle r)
		{
			r = new Rectangle(r.X - 13, r.Y - 12, r.Width + 25, r.Height + 25);
			Rectangle TL = new Rectangle(r.X, r.Y, 20, 20);
			spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/tech_underglow_container_corner_TL"], TL, Color.White);
			Rectangle TR = new Rectangle(r.X + r.Width - 20, r.Y, 20, 20);
			spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/tech_underglow_container_corner_TR"], TR, Color.White);
			Rectangle BL = new Rectangle(r.X, r.Y + r.Height - 20, 20, 20);
			spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/tech_underglow_container_corner_BL"], BL, Color.White);
			Rectangle BR = new Rectangle(r.X + r.Width - 20, r.Y + r.Height - 20, 20, 20);
			spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/tech_underglow_container_corner_BR"], BR, Color.White);
			Rectangle HT = new Rectangle(TL.X + 20, TL.Y, r.Width - 40, 20);
			spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/tech_underglow_horiz_T"], HT, Color.White);
			Rectangle HB = new Rectangle(TL.X + 20, BL.Y, r.Width - 40, 20);
			spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/tech_underglow_horiz_B"], HB, Color.White);
			Rectangle VL = new Rectangle(TL.X, TL.Y + 20, 20, r.Height - 40);
			spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/tech_underglow_verti_L"], VL, Color.White);
			Rectangle VR = new Rectangle(TL.X + r.Width - 20, TL.Y + 20, 20, r.Height - 40);
			spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/tech_underglow_verti_R"], VR, Color.White);
		}

		public static void DrawResearchLineHorizontal(SpriteBatch spriteBatch, Vector2 LeftPoint, Vector2 RightPoint, bool Complete)
		{
			Rectangle r = new Rectangle((int)LeftPoint.X + 5, (int)LeftPoint.Y - 2, (int)Vector2.Distance(LeftPoint, RightPoint) - 5, 5);
			Rectangle small = new Rectangle((int)LeftPoint.X, (int)LeftPoint.Y, 5, 1);
			Primitives2D.FillRectangle(spriteBatch, small, (Complete ? new Color(110, 171, 227) : new Color(194, 194, 194)));
			if (Complete)
			{
				spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/grid_horiz_complete"], r, Color.White);
				return;
			}
			spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/grid_horiz"], r, Color.White);
		}

		public static void DrawResearchLineHorizontalGradient(SpriteBatch spriteBatch, Vector2 LeftPoint, Vector2 RightPoint, bool Complete)
		{
			Rectangle r = new Rectangle((int)LeftPoint.X + 5, (int)LeftPoint.Y - 2, (int)Vector2.Distance(LeftPoint, RightPoint) - 5, 5);
			Rectangle small = new Rectangle((int)LeftPoint.X, (int)LeftPoint.Y, 5, 1);
			Primitives2D.FillRectangle(spriteBatch, small, (Complete ? new Color(110, 171, 227) : new Color(194, 194, 194)));
			if (Complete)
			{
				spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/grid_horiz_gradient_complete"], r, Color.White);
				return;
			}
			spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/grid_horiz_gradient"], r, Color.White);
		}

		public static void DrawResearchLineVertical(SpriteBatch spriteBatch, Vector2 top, Vector2 bottom, bool Complete)
		{
			Rectangle r = new Rectangle((int)top.X - 2, (int)top.Y, 5, (int)Vector2.Distance(top, bottom));
			if (Complete)
			{
				spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/grid_vert_complete"], r, Color.White);
				return;
			}
			spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/grid_vert"], r, Color.White);
		}

		public static void FillRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color)
		{
			if (Primitives2D.m_pixel == null)
			{
				Primitives2D.CreateThePixel(spriteBatch);
			}
			spriteBatch.Draw(Primitives2D.m_pixel, rect, color);
		}

		public static void FillRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color, float angle)
		{
			if (Primitives2D.m_pixel == null)
			{
				Primitives2D.CreateThePixel(spriteBatch);
			}
			Rectangle? nullable = null;
			spriteBatch.Draw(Primitives2D.m_pixel, rect, nullable, color, angle, Vector2.Zero, SpriteEffects.None, 0f);
		}

		public static void FillRectangle(SpriteBatch spriteBatch, Vector2 location, Vector2 size, Color color)
		{
			Primitives2D.FillRectangle(spriteBatch, location, size, color, 0f);
		}

		public static void FillRectangle(SpriteBatch spriteBatch, Vector2 location, Vector2 size, Color color, float angle)
		{
			if (Primitives2D.m_pixel == null)
			{
				Primitives2D.CreateThePixel(spriteBatch);
			}
			Rectangle? nullable = null;
			spriteBatch.Draw(Primitives2D.m_pixel, location, nullable, color, angle, Vector2.Zero, size, SpriteEffects.None, 0f);
		}

		public static void FillRectangle(SpriteBatch spriteBatch, float x1, float y1, float x2, float y2, Color color)
		{
			if (Primitives2D.m_pixel == null)
			{
				Primitives2D.CreateThePixel(spriteBatch);
			}
			Primitives2D.FillRectangle(spriteBatch, new Vector2(x1, y1), new Vector2(x2, y2), color, 1f);
		}

		public static void FillRectangle(SpriteBatch spriteBatch, float x1, float y1, float x2, float y2, Color color, float thickness)
		{
			if (Primitives2D.m_pixel == null)
			{
				Primitives2D.CreateThePixel(spriteBatch);
			}
			Primitives2D.FillRectangle(spriteBatch, new Vector2(x1, y1), new Vector2(x2, y2), color, thickness);
		}

		private static bool LineIntersect(Vector2 line1a, Vector2 line1b, Vector2 line2a, Vector2 line2b, out Vector2 intersectPoint)
		{
			bool result = false;
			intersectPoint = new Vector2(0f, 0f);
			Vector2 p01 = line1a;
			Vector2 p02 = line2a;
			Vector2 d1 = line1b - line1a;
			Vector2 d2 = line2b - line2a;
			d1.Normalize();
			d2.Normalize();
			float d = d2.X * d1.Y - d1.X * d2.Y;
			if ((double)Math.Abs(d) > 5E-05)
			{
				float s = d1.X * p02.Y - d1.X * p01.Y - d1.Y * p02.X + d1.Y * p01.X;
				s = s / d;
				intersectPoint = p02 + (s * d2);
				result = true;
			}
			return result;
		}

		private static bool MidVector(Vector2 line1a, Vector2 line1b, Vector2 line2a, Vector2 line2b, out Vector2 pCenter, out Vector2 dCenter)
		{
			Vector2 line1a_to_2;
			Vector2 line1b_to_2;
			bool result = false;
			dCenter = new Vector2(0f, 0f);
			pCenter = new Vector2(0f, 0f);
			if (!Primitives2D.LineIntersect(line1a, line1b, line2a, line2b, out pCenter))
			{
				Vector2 perp = Primitives2D.PerpendicularVector(line1b - line1a);
				Primitives2D.LineIntersect(line2a, line2b, line1a, line1a + perp, out line1a_to_2);
				Primitives2D.LineIntersect(line2a, line2b, line1b, line1b + perp, out line1b_to_2);
				Vector2 a1 = (line1a + line1a_to_2) / 2f;
				Vector2 a2 = (line1b + line1b_to_2) / 2f;
				pCenter = a1;
				dCenter = a2 - a1;
				dCenter.Normalize();
				result = true;
			}
			else
			{
				Vector2 dL1 = line1b - line1a;
				dL1.Normalize();
				Vector2 dL2 = line2b - line2a;
				dL2.Normalize();
				Vector2 a1 = pCenter + dL1;
				Vector2 a2 = pCenter + dL2;
				Vector2 A = (a1 + a2) / 2f;
				dCenter = A - pCenter;
				dCenter.Normalize();
				result = true;
			}
			return result;
		}

		private static Vector2 PerpendicularVector(Vector2 v)
		{
			Vector3 perp = Vector3.Cross(new Vector3(v.X, v.Y, 0f), new Vector3(0f, 0f, 1f));
			perp.Normalize();
			return new Vector2(perp.X, perp.Y);
		}

		public static void PutPixel(SpriteBatch spriteBatch, float x, float y, Color color)
		{
			Primitives2D.PutPixel(spriteBatch, new Vector2(x, y), color);
		}

		public static void PutPixel(SpriteBatch spriteBatch, Vector2 position, Color color)
		{
			if (Primitives2D.m_pixel == null)
			{
				Primitives2D.CreateThePixel(spriteBatch);
			}
			spriteBatch.Draw(Primitives2D.m_pixel, position, color);
		}
	}
}