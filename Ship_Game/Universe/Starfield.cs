using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class StarField : IDisposable
	{
		static readonly Color[] LayerColors =
        {
            new Color(255, 255, 255, 160), 
            new Color(255, 255, 255, 160), 
            new Color(255, 255, 255, 255), 
            new Color(255, 255, 255, 255), 
            new Color(255, 255, 255, 255), 
            new Color(255, 255, 255, 110), 
            new Color(255, 255, 255, 220), 
            new Color(255, 255, 255, 90)
        };

		static readonly float[] MoveFactors = { 0.1f, 0.07f, 0.00007f, 0.0006f, 0.001f, 0.014f, 0.002f, 0.0001f };

        readonly int DesiredSmallStars = RandomMath.IntBetween(10,30);
        readonly int DesiredMedStars   = RandomMath.IntBetween(2, 10);
        readonly int DesiredLargeStars = RandomMath.IntBetween(1, 4);

        struct Star
        {
            public Vector2 Position;
            public int Size;
            public int whichStar;
            public int whichLayer;
        }


        Vector2 LastCamPos;
        Vector2 CameraPos;

        readonly Rectangle StarFieldR;
        readonly Star[] Stars;
        readonly SubTexture[] StarTex = new SubTexture[4];
        Texture2D StarTexture;

        SubTexture CloudTex;
        Effect CloudEffect;
        EffectParameter CloudEffectPos;
        Vector2 CloudPos;

		public StarField(GameScreen screen)
		{
			Stars = new Star[100];
			Reset(Vector2.Zero);

            StarFieldR = new Rectangle(0, 0, screen.ScreenWidth, screen.ScreenHeight);
            CloudTex = ResourceManager.Texture("clouds");
            CloudEffect = screen.TransientContent.Load<Effect>("Effects/Clouds");
            CloudEffectPos = CloudEffect.Parameters["Position"];
            StarTex[0] = ResourceManager.Texture("Suns/star_binary");
            StarTex[1] = ResourceManager.Texture("Suns/star_yellow");
            StarTex[2] = ResourceManager.Texture("Suns/star_red");
            StarTex[3] = ResourceManager.Texture("Suns/star_neutron");
            StarTexture = new Texture2D(screen.Device, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            StarTexture.SetData(new[] { Color.White });
		}
        
        ~StarField() { Destroy(); }
		public void Dispose() { Destroy(); GC.SuppressFinalize(this); }
        void Destroy()
		{
            StarTexture?.Dispose(ref StarTexture);
        }

		public void Draw(Vector2 cameraPos, SpriteBatch batch)
		{
			LastCamPos = CameraPos;
			CameraPos = cameraPos;
			Vector2 movement = -1f * (cameraPos - LastCamPos);
			if (movement.Length() > 20000f)
			{
				Reset(cameraPos);
				return;
			}
			batch.End();
			if (CloudEffect != null)
			{
				batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
				CloudEffect.Begin();
				CloudPos -= ((movement * 0.3f) * 1f);
				CloudEffectPos.SetValue(CloudPos);
				CloudEffect.CurrentTechnique.Passes[0].Begin();
				batch.Draw(CloudTex.Texture, StarFieldR, null, new Color(255, 0, 0, 255), 0f, Vector2.Zero, SpriteEffects.None, 1f);
				CloudEffect.CurrentTechnique.Passes[0].End();
				CloudEffect.End();
				batch.End();
			}
			batch.Begin();
			for (int i = 0; i < Stars.Length; i++)
			{
                ref Star star = ref Stars[i];
				star.Position += (movement * MoveFactors[star.whichLayer]);
				if (star.Position.X < StarFieldR.X)
				{
					star.Position.X = StarFieldR.Right;
					star.Position.Y = StarFieldR.Y + RandomMath.Random.Next(StarFieldR.Height);
				}
				if (star.Position.X > StarFieldR.Right)
				{
					star.Position.X = StarFieldR.X;
					star.Position.Y = StarFieldR.Y + RandomMath.Random.Next(StarFieldR.Height);
				}
				if (star.Position.Y < StarFieldR.Y)
				{
					star.Position.X = StarFieldR.X + RandomMath.Random.Next(StarFieldR.Width);
					star.Position.Y = StarFieldR.Bottom;
				}
				if (star.Position.Y > StarFieldR.Y + Empire.Universe.Viewport.Height)
				{
					star.Position.X = StarFieldR.X + RandomMath.Random.Next(StarFieldR.Width);
					star.Position.Y = StarFieldR.Y;
				}
				float alpha = 255f;
				switch (star.whichLayer)
				{
					case 2:
					{
						batch.Draw(ResourceManager.SmallStars[star.whichStar], star.Position, LayerColors[star.whichLayer]);
						break;
					}
					case 3:
					{
						batch.Draw(ResourceManager.MediumStars[star.whichStar], star.Position, new Color(LayerColors[star.whichLayer].R, LayerColors[star.whichLayer].G, LayerColors[star.whichLayer].B, (byte)alpha));
						break;
					}
					case 4:
					{
						batch.Draw(ResourceManager.LargeStars[star.whichStar], star.Position, new Color(LayerColors[star.whichLayer].R, LayerColors[star.whichLayer].G, LayerColors[star.whichLayer].B, (byte)alpha));
						break;
					}
					default:
					{
						batch.Draw(StarTex[star.whichStar], 
                            new Rectangle((int)star.Position.X, (int)star.Position.Y, star.Size, star.Size),
                            LayerColors[star.whichLayer]);
						break;
					}
				}
			}
		}

		public void Reset(Vector2 position)
		{
			int numSmallStars = 0;
			int numMedStars = 0;
			int numLargeStars = 0;
			int viewportWidth = Empire.Universe.Viewport.Width;
			int viewportHeight = Empire.Universe.Viewport.Height;
			for (int i = 0; i < Stars.Length; i++)
			{
                ref Star star = ref Stars[i];
				int depth = i % MoveFactors.Length;
				if (depth != 2 && depth != 3 && depth != 4)
				{
					star.Position = new Vector2(RandomMath.Random.Next(0, viewportWidth), RandomMath.Random.Next(0, viewportHeight));
					star.Size = 1;
					star.whichStar = (int)RandomMath.RandomBetween(0f, 3f);
					star.whichLayer = depth;
				}
				else if (depth == 2 && numSmallStars < DesiredSmallStars)
				{
					numSmallStars++;
					star.Position = new Vector2(RandomMath.Random.Next(0, viewportWidth), RandomMath.Random.Next(0, viewportHeight));
					star.Size = 1;
					star.whichStar = (int)RandomMath.RandomBetween(0f, ResourceManager.SmallStars.Count);
					star.whichLayer = 2;
				}
				else if (depth == 3 && numMedStars < DesiredMedStars)
				{
					numMedStars++;
					star.Position = new Vector2(RandomMath.Random.Next(0, viewportWidth), RandomMath.Random.Next(0, viewportHeight));
					star.Size = 1;
					star.whichStar = (int)RandomMath.RandomBetween(0f, ResourceManager.MediumStars.Count);
					star.whichLayer = 3;
				}
				else if (depth != 4 || numLargeStars >= DesiredLargeStars)
				{
					star.Position = new Vector2(RandomMath.Random.Next(0, viewportWidth), RandomMath.Random.Next(0, viewportHeight));
					star.Size = 1;
					star.whichStar = (int)RandomMath.RandomBetween(0f, 3f);
					star.whichLayer = 7;
				}
				else
				{
					numLargeStars++;
					star.Position = new Vector2(RandomMath.Random.Next(0, viewportWidth), RandomMath.Random.Next(0, viewportHeight));
					star.Size = 1;
					star.whichStar = (int)RandomMath.RandomBetween(0f, ResourceManager.LargeStars.Count);
					star.whichLayer = 4;
				}
			}
		}

		public void UnloadContent()
		{
			CloudTex = null;
			CloudEffect = null;
			CloudEffectPos = null;
            StarTexture?.Dispose(ref StarTexture);
		}
	}
}