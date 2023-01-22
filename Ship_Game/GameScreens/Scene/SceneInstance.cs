using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;
using Ship_Game.Graphics.Particles;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows;
using SDUtils;
using Vector3 = SDGraphics.Vector3;
using Matrix = SDGraphics.Matrix;
using Ship_Game.Utils;

namespace Ship_Game.GameScreens.Scene
{
    [StarDataType]
    public class SceneInstance
    {
        #pragma warning disable 649
        [StarData] public Vector3 SunPos = new Vector3(26000, -26000, 32000);
        [StarData] public Vector3 SunTarget = new Vector3(0, 0, 0);
        [StarData] public Color SunColor = new Color(255,254,224);
        [StarData] public float SunIntensity = 2f;
        [StarData] public Color AmbientColor = new Color(40, 60, 110); // dark violet
        [StarData] public float AmbientIntensity = 0.75f;
        [StarData] public Vector3 CameraPos = new Vector3(0, 0, -1000);
        [StarData] public Vector3 LookAt = new Vector3(0, 0, 10000f);
        [StarData] public SceneGroup[] Groups = new SceneGroup[0];
        #pragma warning restore 649

        public GameScreen Screen;
        public ParticleManager Particles;

        Map<string, EmpireData> Empires = new();

        public SeededRandom Random = new();

        public static SceneInstance FromFile(GameScreen screen, string relativePath)
        {
            FileInfo file = screen.ScreenManager.AddHotLoadTarget(screen, relativePath);
            var scene = YamlParser.Deserialize<SceneInstance>(file);
            scene.Initialize(screen);
            return scene;
        }

        public void Initialize(GameScreen screen)
        {
            Screen = screen;
            screen.ScreenManager.RemoveAllLights(LightRigIdentity.MainMenu);

            // TODO: some issue with directional lights
            //AddDirectionalLight("Scene Sun", SunColor, SunIntensity, SunPos, SunTarget);
            AddLight("Scene Sun", SunColor, SunIntensity, SunPos);

            screen.AddLight(new AmbientLight
            {
                Name = "Scene Ambient",
                DiffuseColor = AmbientColor.ToVector3(),
                Intensity = AmbientIntensity,
            }, dynamic:false);

            screen.SetViewMatrix(Matrix.CreateLookAt(CameraPos, LookAt, Vector3.Down));
            screen.SetPerspectiveProjection(maxDistance: 35000);

            // always reload because we might have switched mods (which unloaded the content)
            FTLManager.LoadContent(screen, reload:true);

            Particles = new ParticleManager(screen.TransientContent);

            foreach (SceneGroup fleet in Groups)
            {
                fleet.CreateShips(this, screen);
            }
        }

        void AddDirectionalLight(string name, Color color, float intensity, Vector3 source, Vector3 target)
        {
            var light = new DirectionalLight
            {
                Name                = name,
                DiffuseColor        = color.ToVector3(),
                Intensity           = intensity,
                ObjectType          = ObjectType.Static,
                Direction           = source.DirectionToTarget(target),
                Enabled             = true,
                ShadowPerSurfaceLOD = true,
                ShadowQuality = 1f
            };

            light.ShadowType = ShadowType.AllObjects;
            light.World = Matrix.CreateTranslation(source);
            Screen.AddLight(light, dynamic:false);
        }

        void AddLight(string name, Color color, float intensity, Vector3 position)
        {
            var light = new PointLight
            {
                Name                = name,
                DiffuseColor        = color.ToVector3(),
                Intensity           = intensity,
                ObjectType          = ObjectType.Static,
                FillLight           = false,
                Radius              = 100000,
                Position            = position,
                Enabled             = true,
                FalloffStrength     = 1f,
                ShadowPerSurfaceLOD = true,
                ShadowQuality = 1f
            };

            light.ShadowType = ShadowType.AllObjects;
            light.World = Matrix.CreateTranslation((Vector3)light.Position);
            Screen.AddLight(light, dynamic:false);
        }

        public EmpireData GetEmpire(string name)
        {
            if (name.NotEmpty() && name != "Random")
            {
                IEmpireData e = ResourceManager.AllRaces.Filter(
                    p => p.Name.Contains(name)).FirstOrDefault();
                if (e != null) return GetCachedEmpire(e.CreateInstance());
            }
            return GetCachedEmpire(ResourceManager.MajorRaces.RandItem());
        }

        EmpireData GetCachedEmpire(IEmpireData e)
        {
            if (!Empires.TryGetValue(e.Name, out EmpireData empire))
            {
                empire = e.CreateInstance();
                Empires[e.Name] = empire;
            }
            return empire;
        }

        public bool HandleInput(InputState input)
        {
            foreach (var fleet in Groups)
            {
                fleet.HandleInput(input, Screen);
            }

            //if (input.RightMouseHeldDown)
            //{
            //    Vector2 delta = (input.StartRightHold - input.EndRightHold);
            //    LookAt.X += delta.X.Clamped(-50, 50);
            //    LookAt.Y += delta.Y.Clamped(-50, 50);
            //    Screen.SetViewMatrix(Matrix.CreateLookAt(CameraPos, LookAt, Vector3.Down));
            //}
            return false;
        }

        public void Update(FixedSimTime timeStep)
        {
            foreach (SceneGroup fleet in Groups)
            {
                fleet.Update(Screen, timeStep);
            }
        }

        public void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            batch.SafeBegin();
            foreach (var fleet in Groups)
            {
                fleet.Draw(batch, Screen);
            }
            batch.SafeEnd();

            Particles.Draw(Screen.View, Screen.Projection, nearView: true);
            Particles.Update(elapsed.CurrentGameTime);
        }
    }
}
