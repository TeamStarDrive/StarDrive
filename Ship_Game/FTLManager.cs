using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	internal static class FTLManager
	{
        sealed class FTL
        {
            // the FTL flash moves from Ship front to ship end
            public Vector3 Front;
            public Vector3 Rear;
            public float Life  = 0.9f;
            public float Scale = 0.1f;
            public float Rotation;
            public Vector3 CurrentPos => Front.LerpTo(Rear, RelativeLife);
            public float RelativeLife => 1f - (Life / 0.9f);
        }
		static readonly Array<FTL> Effects = new Array<FTL>();
        static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();
        static SubTexture FTLTexture;

        public static void LoadContent(GameContentManager content)
        {
            FTLTexture = content.Load<SubTexture>("Textures/Ships/FTL");
        }

        public static void AddFTL(Vector3 position, Vector3 forward, float radius)
        {
            Vector3 front = position + forward*radius;
            Vector3 rear  = position - forward*(radius*3);
            var f = new FTL { Front = front, Rear = rear };

            using (Lock.AcquireWriteLock())
            {
                Effects.Add(f);
            }
        }

        public static void DrawFTLModels(GameScreen screen, SpriteBatch batch)
        {
            batch.Begin();
            using (Lock.AcquireReadLock())
            {
                foreach (FTL f in Effects)
                {
                    Vector3 worldPos = f.CurrentPos;
                    Vector2 pos  = screen.ProjectTo2D(worldPos);
                    Vector2 edge = screen.ProjectTo2D(worldPos+new Vector3(100,0,0));

                    float relSizeOnScreen = (edge.X - pos.X) / StarDriveGame.Instance.ScreenWidth;
                    float sizeScaleOnScreen = f.Scale * 1.25f * relSizeOnScreen;
                    
                    batch.Draw(FTLTexture, pos, Color.White, f.Rotation, 
                        FTLTexture.CenterF, sizeScaleOnScreen, SpriteEffects.FlipVertically, 0.9f);
                }
            }
            batch.End();
        }

		public static void Update(float deltaTime)
		{
            using (Lock.AcquireWriteLock())
            {
                for (int i = 0; i < Effects.Count; ++i)
			    {
                    FTL f = Effects[i];
			        f.Life -= deltaTime;
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

			        if (deltaTime > 0f)
			            f.Rotation += 0.09817477f;
			    }
            }
		}
	}
}