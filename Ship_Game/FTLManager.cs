using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	internal static class FTLManager
	{
		private static readonly BatchRemovalCollection<FTL> FTLList = new BatchRemovalCollection<FTL>();
        private static Texture2D FTLTexture;

        public static void LoadContent(GameContentManager content)
        {
            if (FTLTexture == null)
                FTLTexture = content.Load<Texture2D>("Textures/Ships/FTL");
        }

        public static void AddFTL(Vector2 center)
        {
            FTL ftl = new FTL();
            ftl.Center = new Vector2(center.X, center.Y);
            using (FTLList.AcquireWriteLock())
                FTLList.Add(ftl);
        }

        public static void DrawFTLModels(UniverseScreen us)
        {
            using (FTLList.AcquireReadLock())
            {
                foreach (FTL item in FTLList)
                {
                    us.DrawSunModel(item.WorldMatrix, FTLTexture, item.scale * 1.0f / 50.0f);
                }
            }
        }

		public static void Update(float elapsedTime)
		{
            using (FTLList.AcquireReadLock())
            {
                foreach (FTL item in FTLList)
			    {
			        item.timer -= elapsedTime;
			        if (item.timer < 0.6f)
			        {
			            item.scale /= 2.5f;
			        }
			        else
			        {
			            item.scale *= 1.5625f;
			            if (item.scale > 60f)
			                item.scale = 60f;
			        }
			        if (elapsedTime > 0f)
			        {
			            item.rotation += 0.09817477f;
			        }
			        item.WorldMatrix = Matrix.CreateRotationZ(item.rotation) * Matrix.CreateTranslation(new Vector3(item.Center, 0f));
			        if (item.timer <= 0f)
			        {
			            FTLList.QueuePendingRemoval(item);
			        }
			    }
            }
            FTLList.ApplyPendingRemovals();
		}
	}
}