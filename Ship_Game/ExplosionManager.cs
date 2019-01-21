using System;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.Ships;
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
            public float Time;
            public float Duration;
            public float Radius;
            public float Rotation = RandomMath2.RandomBetween(0f, 6.28318548f);
            public TextureAtlas Animation;
        }

        sealed class Explosion
        {
            #pragma warning disable 649 // They are serialized
            [StarDataKey] public readonly ExplosionType Type;
            [StarData]    public readonly string Path;
            [StarData]    public readonly float Scale = 1.0f;
            #pragma warning restore 649
            public TextureAtlas Atlas;
        }

        public static UniverseScreen Universe;
        static readonly Array<ExplosionState> ActiveExplosions = new Array<ExplosionState>();
        static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        static SubTexture LowResExplode;
        static readonly Map<ExplosionType, Array<Explosion>> Types = new Map<ExplosionType, Array<Explosion>>(new []
        {
            (ExplosionType.Ship,       new Array<Explosion>()),
            (ExplosionType.Projectile, new Array<Explosion>()),
            (ExplosionType.Photon,     new Array<Explosion>()),
            (ExplosionType.Warp,       new Array<Explosion>()),
        });

        public static void Initialize(GameContentManager content)
        {
            foreach (var kv in Types)
                kv.Value.Clear();

            LowResExplode = ResourceManager.Texture("UI/icon_injury");
            if (ResourceManager.IsLoadCancelRequested) return;

            FileInfo expDescriptors = ResourceManager.GetModOrVanillaFile("Explosions.yaml");
            using (var parser = new StarDataParser(expDescriptors))
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

        static void AddLight(ExplosionState newExp, Vector3 position, float intensity)
        {
            if (Universe.viewState > UniverseScreen.UnivScreenState.SectorView)
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
            Universe.AddLight(newExp.Light);
        }

        public static void AddExplosion(Vector3 position, float radius, float intensity, ExplosionType type)
        {
            Explosion expType = RandomMath2.RandItem(Types[type]);
            var exp = new ExplosionState
            {
                Duration = 2.25f,
                Pos = position.ToVec2(),
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

        static bool DebugExplosions = false;

        public static void Update(float elapsedTime)
        {
            if (DebugExplosions && Universe.Input.IsCtrlKeyDown && Universe.Input.LeftMouseClick)
            {
                GameAudio.PlaySfxAsync("sd_explosion_ship_det_large");
                AddExplosion(Universe.CursorWorldPosition, 500.0f, 5.0f, ExplosionType.Ship);
                AddExplosion(Universe.CursorWorldPosition+RandomMath.Vector3D(500f), 500.0f, 5.0f, ExplosionType.Projectile);
                AddExplosion(Universe.CursorWorldPosition+RandomMath.Vector3D(500f), 500.0f, 5.0f, ExplosionType.Photon);
                AddExplosion(Universe.CursorWorldPosition+RandomMath.Vector3D(500f), 500.0f, 5.0f, ExplosionType.Warp);
            }

            using (Lock.AcquireReadLock())
            {
                for (int i = 0; i < ActiveExplosions.Count; ++i)
                {
                    ExplosionState e = ActiveExplosions[i];
                    if (e.Time > e.Duration)
                    {
                        ActiveExplosions.RemoveAtSwapLast(i--);
                        Universe.RemoveLight(e.Light);
                        continue;
                    }

                    if (e.Light != null)
                    {
                        e.Light.Intensity -= 10f * elapsedTime;
                    }

                    // time is update last, because we don't want to skip frame 0 due to bad interpolation
                    e.Time += elapsedTime;
                }
            }

        }

        public static void DrawExplosions(SpriteBatch batch, in Matrix view, in Matrix projection)
        {
            Viewport vp = StarDriveGame.Instance.Viewport;
            using (Lock.AcquireReadLock())
            {
                foreach (ExplosionState e in ActiveExplosions)
                {
                    if (float.IsNaN(e.Radius) || e.Radius.AlmostZero() || e.Animation == null)
                        continue;
                    DrawExplosion(batch, vp, view, projection, e);
                }
            }
        }

        static void DrawExplosion(SpriteBatch batch, in Viewport vp, in Matrix view, in Matrix projection, ExplosionState e)
        {
            // explosion center in screen coords
            Vector3 expOnScreen = vp.Project(e.Pos.ToVec3(), projection, view, Matrix.Identity);

            // edge of the explosion in screen coords
            Vector3 edgeOnScreen = vp.Project(e.Pos.PointOnCircle(90f, e.Radius).ToVec3(), projection, view, Matrix.Identity);

            int screen = (int)Math.Abs(edgeOnScreen.X - expOnScreen.X);
            var r = new Rectangle((int)expOnScreen.X, (int)expOnScreen.Y, screen, screen);

            float relTime = e.Time / e.Duration;
            var color = new Color(255f, 255f, 255f, 255f * (1f - relTime));

            if (screen <= 16) // Low-res explosion marker
            {
                r.Width = r.Height = 12;
                batch.Draw(LowResExplode, r, color, 0f, LowResExplode.CenterF, SpriteEffects.None, 1f);
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

            batch.Draw(tex, r, color, e.Rotation, tex.CenterF, SpriteEffects.None, 1f);
        }
    }
}
