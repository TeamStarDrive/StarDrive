using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	internal static class FTLManager
	{
        sealed class FTL
        {
            public Matrix WorldMatrix;
            public Vector2 Center;
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

        public static void AddFTL(Vector2 center)
        {
            var f = new FTL { Center = center };
            using (Lock.AcquireWriteLock())
                Effects.Add(f);
        }

        public static void DrawFTLModels(UniverseScreen us)
        {
            using (Lock.AcquireReadLock())
            {
                foreach (FTL item in Effects)
                {
                    us.DrawSunModel(item.WorldMatrix, FTLTexture, item.Scale * (1.0f / 50.0f));
                }
            }
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

			        f.WorldMatrix = Matrix.CreateRotationZ(f.Rotation)
			                      * Matrix.CreateTranslation(new Vector3(f.Center, 0f));
			    }
            }
		}
	}
}