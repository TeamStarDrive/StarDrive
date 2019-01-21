using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

namespace Ship_Game
{
	internal sealed class MuzzleFlashManager
	{
        sealed class MuzzleFlash
        {
            public float Life  = 0.02f;
            public float Scale = 0.25f;
            public Projectile Projectile;
            public PointLight Light;
            public Vector3 Position;
            public float Rotation;

            public void Update()
            {
                ShipModule mod = Projectile.Module;
                Vector2 muzzlePos = mod.Center + mod.Direction*mod.Radius;
                Position = new Vector3(muzzlePos.X, muzzlePos.Y, -45f);
                Rotation = Projectile.Rotation;
            }
        }

		static SubTexture FlashTexture;
		static Model flashModel;

		static readonly Array<MuzzleFlash> FlashList = new Array<MuzzleFlash>();
        static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public static void LoadContent(GameContentManager content)
        {
            flashModel   = content.Load<Model>("Model/Projectiles/muzzleEnergy");
            FlashTexture = new SubTexture("MuzzleFlash_01", content.Load<Texture2D>("Model/Projectiles/Textures/MuzzleFlash_01"));
        }

        public static void AddFlash(Projectile projectile)
        {
            var f = new MuzzleFlash { Projectile = projectile };
            f.Update();
            f.Light = new PointLight
            {
                Position     = f.Position,
                Radius       = 65f,
                ObjectType   = ObjectType.Dynamic,
                DiffuseColor = new Vector3(1f, 0.97f, 0.9f),
                Intensity    = 1f,
                FillLight    = false,
                Enabled      = true
            };
            Empire.Universe.AddLight(f.Light);

            using (Lock.AcquireWriteLock())
            {
                FlashList.Add(f);
            }
        }

	    public static void Update(float elapsedTime)
		{
            using (Lock.AcquireWriteLock())
            {
                for (int i = 0; i < FlashList.Count; i++)
                {
                    MuzzleFlash f = FlashList[i];
                    f.Life -= elapsedTime;
                    if (f.Life <= 0f)
                    {
                        Empire.Universe.RemoveLight(f.Light);
                        FlashList.RemoveAtSwapLast(i--);
                        continue;
                    }

                    f.Scale *= 2f;
                    if (f.Scale > 6f)
                        f.Scale = 6f;

                    f.Update();
                    f.Light.Position = f.Position;
                }
            }
		}

        public static void Draw(UniverseScreen us)
        {
            using (Lock.AcquireReadLock())
            {
                for (int i = 0; i < FlashList.Count; i++)
                {
                    MuzzleFlash f = FlashList[i];

                    Matrix world = Matrix.CreateRotationZ(f.Rotation)
                                 * Matrix.CreateTranslation(f.Position);
                    us.DrawTransparentModel(flashModel, world, FlashTexture, f.Scale);
                }
            }
        }
	}
}