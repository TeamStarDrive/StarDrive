using System;
using System.IO;
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
        public ShipModule module;
        public float Rotation = RandomMath2.RandomBetween(0f, 6.28318548f);
        public float shockWaveTimer;
        public int AnimationFrame = -1;
        public TextureAtlas Animation;
    }

    public sealed class ExplosionManager
    {
        public static UniverseScreen Universe;
        public static BatchRemovalCollection<Explosion> ActiveExplosions = new BatchRemovalCollection<Explosion>();

        static readonly Array<TextureAtlas> Generic   = new Array<TextureAtlas>();
        static readonly Array<TextureAtlas> Photon    = new Array<TextureAtlas>();
        static readonly Array<TextureAtlas> ShockWave = new Array<TextureAtlas>();

        static void LoadDefaults(GameContentManager content)
        {
            if (Generic.IsEmpty) Generic.AddRange(new []
            {
                content.LoadTextureAtlas("Textures/sd_explosion_07a_cc"),
                content.LoadTextureAtlas("Textures/sd_explosion_12a_cc"),
                content.LoadTextureAtlas("Textures/sd_explosion_14a_cc")
            });
            if (Photon.IsEmpty) Photon.AddRange(new []
            {
                content.LoadTextureAtlas("Textures/sd_explosion_03_photon_256")
            });
            if (ShockWave.IsEmpty) ShockWave.AddRange(new []
            {
                content.LoadTextureAtlas("Textures/sd_shockwave_01"),
            });
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
                    TextureAtlas anim = content.LoadTextureAtlas(values[1]);
                    switch (values[0])
                    {
                        default:case "generic": Generic.Add(anim);   break;
                        case "photon":          Photon.Add(anim);    break;
                        case "shockwave":       ShockWave.Add(anim); break;
                    }
                }
            }
        }

        public static void Initialize(GameContentManager content)
        {
            Generic.Clear();
            Photon.Clear();
            ShockWave.Clear();
            LoadFromExplosionsList(content);
            LoadDefaults(content);
        }

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

        public static void AddExplosion(Vector3 position, float radius, float intensity, float duration, string explosionPath = null)
        {
            var newExp = new Explosion
            {
                duration = 2.25f,
                pos = position.ToVec2(),
                Animation = ChooseExplosion(explosionPath)
            };
            AddLight(newExp, position, radius, intensity);
            ActiveExplosions.Add(newExp);
        }

        public static void AddExplosionNoFlames(Vector3 position, float radius, float intensity, float duration)
        {
            var newExp = new Explosion
            {
                duration = 2.25f,
                pos = position.ToVec2(),
            };
            AddLight(newExp, position, radius, intensity);
            ActiveExplosions.Add(newExp);
        }

        public static void AddProjectileExplosion(Vector3 position, float radius, float intensity, float duration, string expColor)
        {
            var newExp = new Explosion
            {
                duration = 2.25f,
                pos = position.ToVec2(),
                Animation = RandomMath2.RandItem(expColor == "Blue_1" ? Photon : Generic)
            };
            AddLight(newExp, position, radius, intensity);
            ActiveExplosions.Add(newExp);
        }

        public static void AddWarpExplosion(Vector3 position, float radius, float intensity, float duration)
        {
            var newExp = new Explosion
            {
                duration = 2.25f,
                pos = position.ToVec2(),
                Animation = RandomMath2.RandItem(ShockWave)
            };
            AddLight(newExp, position, radius, intensity);
            ActiveExplosions.Add(newExp);
        }

        public static void Update(float elapsedTime)
        {
            using (ActiveExplosions.AcquireReadLock())
            foreach (Explosion e in ActiveExplosions)
            {
                e.duration       -= elapsedTime;
                e.shockWaveTimer += elapsedTime;
                if (e.light != null)
                {
                    e.light.Intensity -= 0.2f;
                }
                e.color = new Color(255f, 255f, 255f, 255f * e.duration / 0.2f);
                if (e.Animation != null)
                {
                    if (e.AnimationFrame < e.Animation.Count-1)
                        e.AnimationFrame += 1;
                    //else
                    //    e.AnimationFrame = 0; // restart
                }
                if (e.duration <= 0f)
                {
                    ActiveExplosions.QueuePendingRemoval(e);
                    Universe.RemoveLight(e.light);
                }
            }
            ActiveExplosions.ApplyPendingRemovals();
        }

        public static void DrawExplosions(ScreenManager screen, Matrix view, Matrix projection)
        {
            var vp = Game1.Instance.Viewport;
            using (ActiveExplosions.AcquireReadLock())
            {
                foreach (Explosion e in ActiveExplosions)
                {
                    if (float.IsNaN(e.Radius) || e.Animation == null)
                        continue;

                    Vector2 expCenter = e.module?.Position ?? e.pos;

                    // explosion center in screen coords
                    Vector3 expOnScreen = vp.Project(expCenter.ToVec3(), projection, view, Matrix.Identity);

                    // edge of the explosion in screen coords
                    Vector3 edgeOnScreen = vp.Project(expCenter.PointOnCircle(90f, e.Radius).ToVec3(), projection, view, Matrix.Identity);

                    int radiusOnScreen = (int)Math.Abs(edgeOnScreen.X - expOnScreen.X);
                    e.ExplosionRect = new Rectangle((int)expOnScreen.X, (int)expOnScreen.Y, radiusOnScreen, radiusOnScreen);

                    // animations could be added between Update and Draw
                    int frame = e.AnimationFrame;
                    if (frame == -1) frame = 0;
                    SubTexture tex = e.Animation[e.AnimationFrame];
                    screen.SpriteBatch.Draw(tex, e.ExplosionRect, e.color, e.Rotation, tex.CenterF, SpriteEffects.None, 1f);
                }
            }
        }
    }
}
