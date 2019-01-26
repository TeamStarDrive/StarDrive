using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	internal static class FTLManager
	{
        sealed class FTL
        {
            public Vector2 WorldPos;
            public float Life  = 0.9f;
            public float Scale = 0.1f;
            public float Rotation;
        }
		static readonly Array<FTL> Effects = new Array<FTL>();
        static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        static SubTexture FTLTexture;

        public static void LoadContent(GameContentManager content)
        {
            FTLTexture = content.Load<SubTexture>("Textures/Ships/FTL");
        }

        public static void AddFTL(Vector2 worldPos)
        {
            var f = new FTL { WorldPos = worldPos };
            using (Lock.AcquireWriteLock())
                Effects.Add(f);
        }

        static Vector2 ScreenPosition(Vector2 worldPos, in Matrix view, in Matrix projection)
        {
            return StarDriveGame.Instance.Viewport.Project(worldPos.ToVec3(), 
                                 projection, view, Matrix.Identity).ToVec2();
        }

        public static void DrawFTLModels(UniverseScreen us, SpriteBatch batch)
        {
            batch.Begin();
            using (Lock.AcquireReadLock())
            {
                foreach (FTL f in Effects)
                {
                    Vector2 pos  = ScreenPosition(f.WorldPos, us.view, us.projection);
                    Vector2 edge = ScreenPosition(f.WorldPos+new Vector2(100f,0f), us.view, us.projection);

                    float relSizeOnScreen = (edge.X - pos.X) / StarDriveGame.Instance.ScreenWidth;
                    float sizeScaleOnScreen = f.Scale * 1.25f * relSizeOnScreen;
                    
                    batch.Draw(FTLTexture, pos, Color.White, f.Rotation, 
                        FTLTexture.CenterF, sizeScaleOnScreen, SpriteEffects.FlipVertically, 0.9f);
                }
            }
            batch.End();
        }

		public static void Update(float elapsedTime)
		{
            using (Lock.AcquireWriteLock())
            {
                for (int i = 0; i < Effects.Count; ++i)
			    {
                    FTL f = Effects[i];
			        f.Life -= elapsedTime;
                    if (f.Life <= 0f)
                    {
                        Effects.RemoveAtSwapLast(i--);
                        continue;
                    }

			        if (f.Life < 0.6f)
			        {
			            f.Scale /= 2.5f;
			        }
			        else
			        {
			            f.Scale *= 1.5625f;
			            if (f.Scale > 60f)
			                f.Scale = 60f;
			        }

			        if (elapsedTime > 0f)
			            f.Rotation += 0.09817477f;
			    }
            }
		}
	}
}