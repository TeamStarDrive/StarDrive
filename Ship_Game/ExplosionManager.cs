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
        public Vector2 pos;
        public float duration;
        public Color color;
        public Rectangle ExplosionRect;
        public float Radius;
        public string ExpColor = "";
        public ShipModule module;
        public float Rotation = RandomMath2.RandomBetween(0f, 6.28318548f);
        public float shockWaveTimer;
        public bool Animated = true;
        public string AnimationTexture;
        public string AnimationBasePath;
        public int AnimationFrame = 1;
        public int AnimationFrames = 100;
    }

    public sealed class ExplosionManager
    {
        public static UniverseScreen Universe;
        public static BatchRemovalCollection<Explosion> ExplosionList = new BatchRemovalCollection<Explosion>();


        static void AddLight(Explosion newExp, Vector3 position, float radius, float intensity)
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

        static void PickRandomExplosion(Explosion newExp)
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

        public static void AddExplosion(Vector3 position, float radius, float intensity, float duration, string explosionPath = "", string explosionAnimation = "")
        {
            var newExp = new Explosion
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

            ExplosionList.Add(newExp);
        }

        public static void AddExplosionNoFlames(Vector3 position, float radius, float intensity, float duration)
        {
            var newExp = new Explosion
            {
                duration = 2.25f,
                pos = position.ToVec2()
            };
            AddLight(newExp, position, radius, intensity);
            newExp.Animated = false;

            ExplosionList.Add(newExp);
        }

        public static void AddProjectileExplosion(Vector3 position, float radius, float intensity, float duration, string which)
        {
            var newExp = new Explosion
            {
                ExpColor = which,
                duration = 2.25f,
                pos = position.ToVec2()
            };
            AddLight(newExp, position, radius, intensity);
            PickRandomExplosion(newExp);

            ExplosionList.Add(newExp);
        }

        public static void AddWarpExplosion(Vector3 position, float radius, float intensity, float duration)
        {
            var newExp = new Explosion
            {
                duration = 2.25f,
                pos = position.ToVec2()
            };
            AddLight(newExp, position, radius, intensity);
            newExp.AnimationFrames   = 59;
            newExp.AnimationTexture  = "sd_shockwave_01/sd_shockwave_01_00000";
            newExp.AnimationBasePath = "sd_shockwave_01/sd_shockwave_01_";

            ExplosionList.Add(newExp);
        }

        public static void Update(float elapsedTime)
        {
            using (ExplosionList.AcquireReadLock())
            foreach (Explosion e in ExplosionList)
            {
                e.duration       -= elapsedTime;
                e.shockWaveTimer += elapsedTime;
                if (e.light != null)
                {
                    e.light.Intensity -= 0.2f;
                }
                e.color = new Color(255f, 255f, 255f, 255f * e.duration / 0.2f);
                if (e.Animated)
                {
                    if (e.ExpColor != "Blue_1")
                    {
                        if (e.AnimationFrame < e.AnimationFrames)
                            e.AnimationFrame += 1;
                        string remainder = e.AnimationFrame.ToString("00000.##");
                        e.AnimationTexture = e.AnimationBasePath + remainder;
                    }
                    else
                    {
                        if (e.AnimationFrame < 88)
                            e.AnimationFrame += 1;
                        string remainder = e.AnimationFrame.ToString("00000.##");
                        e.AnimationTexture = "sd_explosion_03_photon_256/sd_explosion_03_photon_256_" + remainder;
                    }
                }
                if (e.duration <= 0f)
                {
                    ExplosionList.QueuePendingRemoval(e);
                    Universe.RemoveLight(e.light);
                }
            }
            ExplosionList.ApplyPendingRemovals();
        }

        public static void DrawExplosions(ScreenManager screen, Matrix view, Matrix projection)
        {
            var vp = Game1.Instance.Viewport;
            using (ExplosionList.AcquireReadLock())
            {
                foreach (Explosion e in ExplosionList)
                {
                    if (float.IsNaN(e.Radius) || !e.Animated)
                        continue;

                    Vector2 expCenter = e.module?.Position ?? e.pos;

                    // explosion center in screen coords
                    Vector3 expOnScreen = vp.Project(expCenter.ToVec3(), projection, view, Matrix.Identity);

                    // edge of the explosion in screen coords
                    Vector3 edgeOnScreen = vp.Project(expCenter.PointOnCircle(90f, e.Radius).ToVec3(), projection, view, Matrix.Identity);

                    int radiusOnScreen = (int)Math.Abs(edgeOnScreen.X - expOnScreen.X);
                    e.ExplosionRect = new Rectangle((int)expOnScreen.X, (int)expOnScreen.Y, radiusOnScreen, radiusOnScreen);

                    var tex = ResourceManager.Texture(e.AnimationTexture);
                    screen.SpriteBatch.Draw(tex, e.ExplosionRect, e.color, e.Rotation, tex.CenterF, SpriteEffects.None, 1f);
                }
            }
        }
    }
}
