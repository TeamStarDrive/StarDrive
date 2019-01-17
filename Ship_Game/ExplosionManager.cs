using System;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

namespace Ship_Game
{
    public sealed class ExplosionManager
    {
        sealed class Explosion
        {
            public PointLight Light;
            public Vector2 Pos;
            public float Time;
            public float Duration;
            public float Radius;
            public float Rotation = RandomMath2.RandomBetween(0f, 6.28318548f);
            public TextureAtlas Animation;
        }

        public static UniverseScreen Universe;
        static readonly Array<Explosion> Explosions = new Array<Explosion>();
        static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        static readonly Array<TextureAtlas> Generic   = new Array<TextureAtlas>();
        static readonly Array<TextureAtlas> Photon    = new Array<TextureAtlas>();
        static readonly Array<TextureAtlas> ShockWave = new Array<TextureAtlas>();

        static SubTexture LowResExplode;

        static void LoadAtlas(GameContentManager content, Array<TextureAtlas> target, string anim)
        {
            // @note This is very slow, so we have to check for cancel before starting
            if (ResourceManager.IsLoadCancelRequested)
                throw new OperationCanceledException();
            TextureAtlas atlas = content.LoadTextureAtlas(anim); // guaranteed to load an atlas with at least 1 tex
            if (atlas != null)
                target.Add(atlas);
        }

        static void LoadDefaults(GameContentManager content)
        {
            if (Generic.IsEmpty)
            {
                LoadAtlas(content, Generic, "Textures/sd_explosion_07a_cc");
                LoadAtlas(content, Generic, "Textures/sd_explosion_12a_cc");
                LoadAtlas(content, Generic, "Textures/sd_explosion_14a_cc");
            }
            if (Photon.IsEmpty)
                LoadAtlas(content, Photon, "Textures/sd_explosion_03_photon_256");
            if (ShockWave.IsEmpty)
                LoadAtlas(content, ShockWave, "Textures/sd_shockwave_01");

            if (!ResourceManager.IsLoadCancelRequested)
                LowResExplode = ResourceManager.Texture("UI/icon_injury");
        }

        static void LoadFromExplosionsList(GameContentManager content)
        {
            FileInfo explosions = ResourceManager.GetModOrVanillaFile("Explosions.txt");
            if (explosions == null) return;
            using (var reader = new StreamReader(explosions.OpenRead()))
            {
                string line;
                char[] split = { ' ' };
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.TrimStart();
                    if (line.Length == 0 || line[0] == '#')
                        continue;
                    string[] values = line.Split(split, 2, StringSplitOptions.RemoveEmptyEntries);
                    switch (values[0])
                    {
                        default:case "generic": LoadAtlas(content, Generic, values[1]);   break;
                        case "photon":          LoadAtlas(content, Photon, values[1]);    break;
                        case "shockwave":       LoadAtlas(content, ShockWave, values[1]); break;
                    }
                }
            }
        }

        // @note Since explosion atlas generation is slow, we need to check for cancellation event
        public static void Initialize(GameContentManager content)
        {
            Generic.Clear();
            Photon.Clear();
            ShockWave.Clear();
            try
            {
                LoadFromExplosionsList(content);
                LoadDefaults(content);
            }
            catch (OperationCanceledException) { /* expected */ }
        }

        static void AddLight(Explosion newExp, Vector3 position, float radius, float intensity)
        {
            if (radius <= 0f) radius = 1f;
            newExp.Radius = radius;

            if (Universe.viewState > UniverseScreen.UnivScreenState.SectorView)
                return;

            newExp.Light = new PointLight
            {
                World        = Matrix.CreateTranslation(position),
                Position     = position,
                Radius       = radius,
                ObjectType   = ObjectType.Dynamic,
                DiffuseColor = new Vector3(0.9f, 0.8f, 0.7f),
                Intensity    = intensity,
                Enabled      = true
            };
            Universe.AddLight(newExp.Light);
        }

        static TextureAtlas ChooseExplosion(string animationPath)
        {
            if (animationPath.NotEmpty())
            {
                foreach (TextureAtlas anim in Generic)
                    if (animationPath.Contains(anim.Name)) return anim;
                foreach (TextureAtlas anim in Photon)
                    if (animationPath.Contains(anim.Name)) return anim;
                foreach (TextureAtlas anim in ShockWave)
                    if (animationPath.Contains(anim.Name)) return anim;
            }
            return RandomMath2.RandItem(Generic); 
        }

        public static void AddExplosion(Vector3 position, float radius, float intensity, string explosionPath = null)
        {
            var newExp = new Explosion
            {
                Duration = 2.25f,
                Pos = position.ToVec2(),
                Animation = ChooseExplosion(explosionPath)
            };
            AddLight(newExp, position, radius, intensity);
            AddExplosion(newExp);
        }

        public static void AddExplosionNoFlames(Vector3 position, float radius, float intensity)
        {
            var newExp = new Explosion
            {
                Duration = 2.25f,
                Pos = position.ToVec2(),
            };
            AddLight(newExp, position, radius, intensity);
            AddExplosion(newExp);
        }

        public static void AddProjectileExplosion(Vector3 position, float radius, float intensity, string expColor)
        {
            var newExp = new Explosion
            {
                Duration = 2.25f,
                Pos = position.ToVec2(),
                Animation = RandomMath2.RandItem(expColor == "Blue_1" ? Photon : Generic)
            };
            AddLight(newExp, position, radius, intensity);
            AddExplosion(newExp);
        }

        public static void AddWarpExplosion(Vector3 position, float radius, float intensity)
        {
            var newExp = new Explosion
            {
                Duration = 2.25f,
                Pos = position.ToVec2(),
                Animation = RandomMath2.RandItem(ShockWave)
            };
            AddLight(newExp, position, radius, intensity);
            AddExplosion(newExp);
        }
        
        static void AddExplosion(Explosion exp)
        {
            using (Lock.AcquireWriteLock())
                Explosions.Add(exp);
        }

        static bool DebugExplosions = false;

        public static void Update(float elapsedTime)
        {
            if (DebugExplosions && Universe.Input.IsCtrlKeyDown && Universe.Input.LeftMouseClick)
            {
                GameAudio.PlaySfxAsync("sd_explosion_ship_det_large");
                AddExplosion(Universe.CursorWorldPosition, 500.0f, 5.0f);
            }

            using (Lock.AcquireReadLock())
            {
                for (int i = 0; i < Explosions.Count; ++i)
                {
                    Explosion e = Explosions[i];
                    if (e.Time > e.Duration)
                    {
                        Explosions.RemoveAtSwapLast(i--);
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
                foreach (Explosion e in Explosions)
                {
                    if (float.IsNaN(e.Radius) || e.Radius.AlmostZero() || e.Animation == null)
                        continue;
                    DrawExplosion(batch, vp, view, projection, e);
                }
            }
        }

        static void DrawExplosion(SpriteBatch batch, in Viewport vp, in Matrix view, in Matrix projection, Explosion e)
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

            int frame = (int)(e.Animation.Count * (e.Time / e.Duration));
            frame = frame.Clamped(0, e.Animation.Count-1);
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
