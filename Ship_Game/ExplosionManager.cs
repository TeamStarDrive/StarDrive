using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Data;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;
using Ship_Game.SpriteSystem;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Ship_Game
{
    public enum ExplosionType
    {
        Ship, Projectile, Photon, Warp
    }

    public sealed class ExplosionManager
    {
        sealed class ExplosionState
        {
            public PointLight Light;
            public Vector3 Pos;
            public Vector2 Vel;
            public float Time;
            public float Duration;
            public float Radius;
            public float Rotation = RandomMath2.Float(0f, 6.28318548f);
            public TextureAtlas Animation;
        }

        [StarDataType]
        sealed class Explosion
        {
            #pragma warning disable 649 // They are serialized
            [StarData] public readonly ExplosionType Type;
            [StarData] public readonly string Path;
            [StarData] public readonly float Scale = 1.0f;
            #pragma warning restore 649
            public TextureAtlas Atlas;
        }

        static readonly Array<ExplosionState> ActiveExplosions = new Array<ExplosionState>();
        static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        static SubTexture ExplosionPixel;
        static readonly Map<ExplosionType, Array<Explosion>> Types = new Map<ExplosionType, Array<Explosion>>(new []
        {
            (ExplosionType.Ship,       new Array<Explosion>()),
            (ExplosionType.Projectile, new Array<Explosion>()),
            (ExplosionType.Photon,     new Array<Explosion>()),
            (ExplosionType.Warp,       new Array<Explosion>()),
        });

        public static void LoadExplosions(GameContentManager content)
        {
            GameLoadingScreen.SetStatus("LoadExplosions");

            foreach (KeyValuePair<ExplosionType, Array<Explosion>> kv in Types)
                kv.Value.Clear();

            ExplosionPixel = ResourceManager.Texture("blank");
            if (ResourceManager.IsLoadCancelRequested) return;

            Array<Explosion> explosions = YamlParser.DeserializeArray<Explosion>("Explosions.yaml");
            foreach (Explosion e in explosions)
            {
                // @note LoadAtlas is very slow, so we have to check for cancel before starting
                if (ResourceManager.IsLoadCancelRequested) return;
                e.Atlas = content.LoadTextureAtlas(e.Path); // guaranteed to load an atlas with at least 1 tex
                if (e.Atlas == null)
                    continue;
                Types[e.Type].Add(e);
            }
        }

        public static void UnloadContent()
        {
            using (Lock.AcquireWriteLock())
            {
                ActiveExplosions.Clear();
                ExplosionPixel = null;
                Types.Clear();
            }
        }

        static void AddLight(ExplosionState newExp, Vector3 position, float intensity)
        {
            newExp.Light = new PointLight
            {
                World        = Matrix.CreateTranslation(position),
                Position     = position,
                Radius       = newExp.Radius,
                ObjectType   = ObjectType.Dynamic,
                DiffuseColor = new Vector3(0.9f, 0.8f, 0.7f),
                Intensity    = intensity,
                Enabled      = true
            };
            ScreenManager.Instance.AddLight(newExp.Light, dynamic:true);
        }

        public static void AddExplosion(UniverseScreen u, Vector3 position, Vector2 velocity,
                                        float radius, float intensity, ExplosionType type)
        {
            Array<Explosion> explosions = Types[type];
            if (explosions.IsEmpty)
                return; // explosions not loaded in Unit Tests

            Explosion expType = RandomMath2.RandItem(explosions);
            var exp = new ExplosionState
            {
                Duration = 2.25f,
                Pos = position,
                Vel = velocity,
                Animation = expType.Atlas,
                Radius = radius <= 0f ? 1f : radius*expType.Scale
            };

            if (u.CanAddDynamicLight && u.IsSectorViewOrCloser)
                AddLight(exp, position, intensity);

            using (Lock.AcquireWriteLock())
                ActiveExplosions.Add(exp);
        }

        // Light flash only, no explosion anim texture is played
        public static void AddExplosionNoFlames(UniverseScreen u, Vector3 position, float radius, float intensity)
        {
            var exp = new ExplosionState
            {
                Duration = 2.25f,
                Pos = position,
                Radius = radius <= 0f ? 1f : radius,
            };

            if (u.CanAddDynamicLight && u.IsSectorViewOrCloser)
                AddLight(exp, position, intensity);

            using (Lock.AcquireWriteLock())
                ActiveExplosions.Add(exp);
        }

        public static void Update(UniverseScreen us, float elapsedTime)
        {
            using (Lock.AcquireWriteLock())
            {
                for (int i = 0; i < ActiveExplosions.Count; ++i)
                {
                    ExplosionState e = ActiveExplosions[i];
                    if (e.Time > e.Duration)
                    {
                        ActiveExplosions.RemoveAtSwapLast(i--);
                        us.RemoveLight(e.Light, dynamic:true);
                        continue;
                    }

                    if (e.Light != null)
                    {
                        e.Light.Intensity -= 10f * elapsedTime;
                    }

                    // cheap and inaccurate integration
                    e.Pos.X += e.Vel.X * elapsedTime;
                    e.Pos.Y += e.Vel.Y * elapsedTime;

                    // time is update last, because we don't want to skip frame 0 due to bad interpolation
                    e.Time += elapsedTime;
                }
            }
        }

        public static void DrawExplosions(SpriteBatch batch, in Matrix view, in Matrix projection)
        {
            using (Lock.AcquireReadLock())
            {
                for (int i = 0; i < ActiveExplosions.Count; ++i)
                {
                    ExplosionState e = ActiveExplosions[i];
                    if (float.IsNaN(e.Radius) || e.Radius.AlmostZero() || e.Animation == null)
                        continue;
                    DrawExplosion(batch, GameBase.Viewport, view, projection, e);
                }
            }
        }

        static void DrawExplosion(SpriteBatch batch, in Viewport vp, in Matrix view, in Matrix projection, ExplosionState e)
        {
            // explosion center in screen coords
            Vector2 expOnScreen = vp.ProjectTo2D(e.Pos, projection, view);

            // edge of the explosion in screen coords
            Vector2 edgeOnScreen = vp.ProjectTo2D(new Vector3(e.Pos.X + e.Radius, e.Pos.Y, e.Pos.Z), projection, view);

            float size = edgeOnScreen.X - expOnScreen.X;
            if (size < 0.5f) return; // don't draw sub-pixel explosion

            int screen = (int)size;
            var r = new Rectangle((int)expOnScreen.X, (int)expOnScreen.Y, screen, screen);
            float relTime = e.Time / e.Duration;

            // y = cos(x * PI/2)  gives a nice curvy falloff
            // from 1.0 to 0.0
            float a = RadMath.Cos(relTime * RadMath.HalfPI);
            if (screen < 2) // Low-res explosion marker
            {
                r.Width = r.Height = 1;
                batch.Draw(ExplosionPixel, r, new Color(Color.LightYellow, a));
                return;
            }

            int last = e.Animation.Count-1;
            int frame = (int)(last * (e.Time / e.Duration));
            frame = frame.Clamped(0, last);
            SubTexture tex = e.Animation[frame];

            // support non-rectangular explosion anims:
            // smaller tex size component is equal to radius
            // bigger tex size component is multiplied
            if (tex.Height > tex.Width)
            {
                r.Height = (int) (screen * (tex.Height / (float) tex.Width));
            }
            else if (tex.Width > tex.Height)
            {
                r.Width = (int) (screen * (tex.Width / (float) tex.Height));
            }

            batch.Draw(tex, r, new Color(1f, 1f, 1f, a), e.Rotation, tex.CenterF, SpriteEffects.None, 1f);
        }
    }
}
