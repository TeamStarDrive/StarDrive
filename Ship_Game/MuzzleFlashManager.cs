using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class MuzzleFlash
    {
        public Matrix WorldMatrix;
        public float Life  = 0.02f;
        public float Scale = 0.25f;
        public GameplayObject Owner;
    }

	internal sealed class MuzzleFlashManager
	{
		static SubTexture FlashTexture;
		static Model flashModel;

		static readonly Array<MuzzleFlash> FlashList = new Array<MuzzleFlash>();
        static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public static void LoadContent(GameContentManager content)
        {
            flashModel   = content.Load<Model>("Model/Projectiles/muzzleEnergy");
            FlashTexture = new SubTexture("MuzzleFlash_01", content.Load<Texture2D>("Model/Projectiles/Textures/MuzzleFlash_01"));
        }

        public static void AddFlash(MuzzleFlash flash)
        {
            using (Lock.AcquireWriteLock())
            {
                FlashList.Add(flash);
            }
        }

	    public static void Update(float elapsedTime)
		{
            using (Lock.AcquireWriteLock())
            {
                for (int i = 0; i < FlashList.Count; i++)
                {
                    MuzzleFlash flash = FlashList[i];
                    flash.Life -= elapsedTime;
                    if (flash.Life <= 0f)
                    {
                        FlashList.RemoveAtSwapLast(i--);
                        continue;
                    }

                    flash.Scale *= 2f;
                    if (flash.Scale > 6f)
                        flash.Scale = 6f;
                }
            }
		}

        public static void Draw(UniverseScreen screen)
        {
            using (Lock.AcquireReadLock())
            for (int i = 0; i < FlashList.Count; i++)
            {
                MuzzleFlash f = FlashList[i];
                screen.DrawTransparentModel(flashModel, f.WorldMatrix, FlashTexture, f.Scale);
            }
        }
	}
}