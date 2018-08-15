using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

namespace Ship_Game
{
    public sealed class Explosion
    {
        public PointLight light;
        public bool sparkWave;
        public Vector2 pos;
        public float duration;
        public Color color;
        public Rectangle ExplosionRect;
        public float Radius;
        public string ExpColor = "";
        public bool sparks = true;
        public ShipModule module;
        public float Rotation;
        public float shockWaveTimer;
        public bool Animated = true;
        public int AnimationFrames = 100;
        public string AnimationTexture;
        public string AnimationBasePath;
        public int AnimationFrame = 1;
    }

    public sealed class ExplosionManager
    {
        public static UniverseScreen Universe;
        public static BatchRemovalCollection<Explosion> ExplosionList = new BatchRemovalCollection<Explosion>();
        private static readonly Random Random = new Random();

        private static void AddLight(Explosion newExp, Vector3 position, float radius, float intensity)
        {
            if (Universe.viewState > UniverseScreen.UnivScreenState.ShipView)
                return;

            if (radius <= 0f) radius = 1f;
            newExp.Radius = radius;
            newExp.light = new PointLight
            {
                World        = Matrix.CreateTranslation(position),
                Position     = position,
                Radius       = radius,
                ObjectType   = ObjectType.Dynamic,
                DiffuseColor = new Vector3(0.9f, 0.8f, 0.7f),
                Intensity    = intensity,
                Enabled      = true
            };
            Universe.AddLight(newExp.light);
        }

        private static void PickRandomExplosion(Explosion newExp)
        {
            switch (RandomMath2.IntBetween(0, 2))
            {
                default://0:
                    newExp.AnimationTexture  = "sd_explosion_12a_cc/sd_explosion_12a_cc_00000";
                    newExp.AnimationBasePath = "sd_explosion_12a_cc/sd_explosion_12a_cc_";
                    break;
                case 1:
                    newExp.AnimationTexture  = "sd_explosion_14a_cc/sd_explosion_14a_cc_00000";
                    newExp.AnimationBasePath = "sd_explosion_14a_cc/sd_explosion_14a_cc_";
                    break;
                case 2:
                    newExp.AnimationTexture  = "sd_explosion_07a_cc/sd_explosion_07a_cc_00000";
                    newExp.AnimationBasePath = "sd_explosion_07a_cc/sd_explosion_07a_cc_";
                    break;                               
            }
        }

        public static void AddExplosion(Vector3 position, float radius, float intensity, float duration
            , string explosionPath = "", string explosionAnimation = "")
        {
            Explosion newExp = new Explosion
            {
                duration = 2.25f,
                pos = position.ToVec2()
            };
            AddLight(newExp, position, radius, intensity);
            if (explosionAnimation == "" || explosionPath == "")
                PickRandomExplosion(newExp);
            else
            {
                newExp.AnimationBasePath = explosionPath;
                newExp.AnimationTexture = explosionAnimation;
            }
            newExp.Rotation = RandomMath2.RandomBetween(0f, 6.28318548f);

            ExplosionList.Add(newExp);
        }

        public static void AddExplosion(Vector3 position, float radius, float intensity, float duration, int nosparks)
        {
            Explosion newExp = new Explosion
            {
                sparks = false,
                duration = 2.25f,
                pos = position.ToVec2()
            };
            AddLight(newExp, position, radius, intensity);
            PickRandomExplosion(newExp);
            newExp.Rotation = RandomMath2.RandomBetween(0f, 6.28318548f);

            ExplosionList.Add(newExp);
        }

        public static void AddExplosion(Vector3 position, float radius, float intensity, float duration, ShipModule mod)
        {
            Explosion newExp = new Explosion
            {
                duration = 2.25f,
                pos = position.ToVec2()
            };
            AddLight(newExp, position, radius, intensity);
            PickRandomExplosion(newExp);

            ExplosionList.Add(newExp);
        }

        public static void AddExplosion(Vector3 position, float radius, float intensity, float duration, bool Shockwave)
        {
            Explosion newExp = new Explosion
            {
                duration = duration,
                pos = position.ToVec2()
            };
            AddLight(newExp, position, radius, intensity);

            ExplosionList.Add(newExp);
        }

        public static void AddExplosionNoFlames(Vector3 position, float radius, float intensity, float duration)
        {
            Explosion newExp = new Explosion
            {
                duration = 2.25f,
                pos = position.ToVec2()
            };
            AddLight(newExp, position, radius, intensity);
            newExp.Animated = false;
            newExp.Rotation = RandomMath2.RandomBetween(0f, 6.28318548f);

            ExplosionList.Add(newExp);
        }

        public static void AddProjectileExplosion(Vector3 position, float radius, float intensity, float duration, string which)
        {
            Explosion newExp = new Explosion
            {
                ExpColor = which,
                duration = 2.25f,
                pos = position.ToVec2()
            };
            AddLight(newExp, position, radius, intensity);
            PickRandomExplosion(newExp);
            newExp.Rotation = RandomMath2.RandomBetween(0f, 6.28318548f);

            ExplosionList.Add(newExp);
        }

        public static void AddWarpExplosion(Vector3 position, float radius, float intensity, float duration)
        {
            Explosion newExp = new Explosion
            {
                duration = 2.25f,
                pos = position.ToVec2()
            };

            AddLight(newExp, position, radius, intensity);
            newExp.AnimationFrames   = 59;
            newExp.AnimationTexture  = "sd_shockwave_01/sd_shockwave_01_00000";
            newExp.AnimationBasePath = "sd_shockwave_01/sd_shockwave_01_";
            newExp.Rotation = RandomMath2.RandomBetween(0f, 6.28318548f);

            ExplosionList.Add(newExp);
        }

        private static Vector3 RandomPointOnCircle(float radius, Vector3 center)
        {
            double angle = Random.NextDouble() * 3.14159265358979 * 2;
            float x = (float)Math.Cos(angle);
            float y = (float)Math.Sin(angle);
            return new Vector3(center.X + x * radius, center.Y, y * radius);
        }

        private static Vector3 RandomSpherePoint(float radius, Vector3 Center)
        {
            Vector3 v = Vector3.Zero;
            do
            {
                v.X = 2f * (float)Random.NextDouble() - 1f;
                v.Y = 2f * (float)Random.NextDouble() - 1f;
                v.Z = 2f * (float)Random.NextDouble() - 1f;
            }
            while (v.LengthSquared() == 0f || v.LengthSquared() > 1f);
            v.Normalize();
            v = v * radius;
            v = v + Center;
            return v;
        }

        public static void Update(float elapsedTime)
        {
            using (ExplosionList.AcquireReadLock())
            foreach (Explosion explosion in ExplosionList)
            {
                explosion.duration       -= elapsedTime;
                explosion.shockWaveTimer += elapsedTime;
                if (explosion.light != null)
                {
                    explosion.light.Intensity -= 0.2f;
                }
                explosion.color = new Color(255f, 255f, 255f, 255f * explosion.duration / 0.2f);
                if (explosion.Animated)
                {
                    if (explosion.ExpColor != "Blue_1")
                    {
                        if (explosion.AnimationFrame < explosion.AnimationFrames)
                            explosion.AnimationFrame += 1;
                        string remainder = explosion.AnimationFrame.ToString("00000.##");
                        explosion.AnimationTexture = explosion.AnimationBasePath + remainder;
                    }
                    else
                    {
                        if (explosion.AnimationFrame < 88)
                            explosion.AnimationFrame += 1;
                        string remainder = explosion.AnimationFrame.ToString("00000.##");
                        explosion.AnimationTexture = "sd_explosion_03_photon_256/sd_explosion_03_photon_256_" + remainder;
                    }
                }
                if (explosion.duration <= 0f)
                {
                    ExplosionList.QueuePendingRemoval(explosion);
                    Universe.RemoveLight(explosion.light);
                }
            }
            ExplosionList.ApplyPendingRemovals();
        }

        public static void DrawExplosions(ScreenManager screen, Matrix view, Matrix projection)
        {
            var vp = Game1.Instance.Viewport;
            using (ExplosionList.AcquireReadLock())
            {
                foreach (Explosion exp in ExplosionList)
                {
                    if (float.IsNaN(exp.Radius) || !exp.Animated)
                        continue;

                    Vector2 expCenter = exp.module?.Position ?? exp.pos;

                    // explosion center in screen coords
                    Vector3 expOnScreen = vp.Project(expCenter.ToVec3(), projection, view, Matrix.Identity);

                    // edge of the explosion in screen coords
                    Vector3 edgeOnScreen = vp.Project(expCenter.PointOnCircle(90f, exp.Radius).ToVec3(), projection, view, Matrix.Identity);

                    int radiusOnScreen = (int)Math.Abs(edgeOnScreen.X - expOnScreen.X);
                    exp.ExplosionRect = new Rectangle((int)expOnScreen.X, (int)expOnScreen.Y, radiusOnScreen, radiusOnScreen);

                    var tex = ResourceManager.Texture(exp.AnimationTexture);
                    screen.SpriteBatch.Draw(tex, exp.ExplosionRect, null, exp.color, exp.Rotation, tex.Center(), SpriteEffects.None, 1f);
                }
            }
        }
    }
}
