using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Data;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;
using Ship_Game.Ships;
using Ship_Game.SpriteSystem;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

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
            public Vector2 Pos;
            public Vector2 Vel;
            public float Time;
            public float Duration;
            public float Radius;
            public float Rotation = RandomMath2.RandomBetween(0f, 6.28318548f);
            public TextureAtlas Animation;
        }

        [StarDataType]
        sealed class Explosion
        {
            #pragma warning disable 649 // They are serialized
            [StarDataKey] public readonly ExplosionType Type;
            [StarData]    public readonly string Path;
            [StarData]    public readonly float Scale = 1.0f;
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

            using (var parser = new YamlParser("Explosions.yaml"))
            {
                Array<Explosion> explosions = parser.DeserializeArray<Explosion>();
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
            if (!Empire.Universe.IsSectorViewOrCloser)
                return;

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
            Empire.Universe.AddLight(newExp.Light);
        }

        public static void AddExplosion(Vector3 position, Vector2 velocity, float radius, float intensity, ExplosionType type)
        {
            Array<Explosion> explosions = Types[type];
            if (explosions.IsEmpty)
                return; // explosions not loaded in Unit Tests

            Explosion expType = RandomMath2.RandItem(explosions);
            var exp = new ExplosionState
            {
                Duration = 2.25f,
                Pos = position.ToVec2(),
                Vel = velocity,
                Animation = expType.Atlas,
                Radius = radius <= 0f ? 1f : radius*expType.Scale
            };

            AddLight(exp, position, intensity);
            using (Lock.AcquireWriteLock())
                ActiveExplosions.Add(exp);
        }

        // Light flash only, no explosion anim texture is played
        public static void AddExplosionNoFlames(Vector3 position, float radius, float intensity)
        {
            var exp = new ExplosionState
            {
                Duration = 2.25f,
                Pos = position.ToVec2(),
                Radius = radius <= 0f ? 1f : radius,
            };
            AddLight(exp, position, intensity);
            using (Lock.AcquireWriteLock())
                ActiveExplosions.Add(exp);
        }

        public static void Update(UniverseScreen us, float elapsedTime)
        {
            // This is purely for DEBUGGING all explosion effects
            bool debugExplosions = Log.HasDebugger && false;
            if (debugExplosions && us.Input.IsCtrlKeyDown && us.Input.LeftMouseClick)
            {
                GameAudio.PlaySfxAsync("sd_explosion_ship_det_large");
                AddExplosion(us.CursorWorldPosition, RandomMath.Vector2D(50f), 500.0f, 5.0f, ExplosionType.Ship);
                AddExplosion(us.CursorWorldPosition+RandomMath.Vector3D(500f), RandomMath.Vector2D(50f), 500.0f, 5.0f, ExplosionType.Projectile);
                AddExplosion(us.CursorWorldPosition+RandomMath.Vector3D(500f), RandomMath.Vector2D(50f), 500.0f, 5.0f, ExplosionType.Photon);
                AddExplosion(us.CursorWorldPosition+RandomMath.Vector3D(500f), RandomMath.Vector2D(50f), 500.0f, 5.0f, ExplosionType.Warp);

                for (int i = 0; i < 15; ++i) // some fireworks!
                    AddExplosion(us.CursorWorldPosition+RandomMath.Vector3D(500f), RandomMath.Vector2D(50f), 200.0f, 5.0f, ExplosionType.Projectile);
            }

            using (Lock.AcquireWriteLock())
            {
                for (int i = 0; i < ActiveExplosions.Count; ++i)
                {
                    ExplosionState e = ActiveExplosions[i];
                    if (e.Time > e.Duration)
                    {
                        ActiveExplosions.RemoveAtSwapLast(i--);
                        us.RemoveLight(e.Light);
                        continue;
                    }

                    if (e.Light != null)
                    {
                        e.Light.Intensity -= 10f * elapsedTime;
                    }

                    e.Pos += e.Vel * elapsedTime;

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
            Vector3 expOnScreen = vp.Project(e.Pos.ToVec3(), projection, view, Matrix.Identity);

            // edge of the explosion in screen coords
            Vector3 edgeOnScreen = vp.Project(e.Pos.PointFromAngle(90f, e.Radius).ToVec3(), projection, view, Matrix.Identity);

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
