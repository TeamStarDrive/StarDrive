using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Yaml;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows;

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
        [StarData] public SceneFleet[] Fleets = new SceneFleet[0];
        #pragma warning restore 649

        GameScreen Screen;

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
            screen.ScreenManager.RemoveAllLights();
            screen.ScreenManager.LightRigIdentity = LightRigIdentity.MainMenu;
            screen.ScreenManager.Environment = screen.TransientContent.Load<SceneEnvironment>("example/scene_environment");

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

            FTLManager.LoadContent(screen);

            foreach (SceneFleet fleet in Fleets)
            {
                fleet.CreateShips(screen);
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
            light.World = Matrix.CreateTranslation(light.Position);
            Screen.AddLight(light, dynamic:false);
        }


        public bool HandleInput(InputState input)
        {
            foreach (var fleet in Fleets)
            {
                fleet.HandleInput(input, Screen);
            }
            return false;
        }

        public void Update(FixedSimTime timeStep)
        {
            foreach (SceneFleet fleet in Fleets)
            {
                fleet.Update(Screen, timeStep);
            }
        }

        public void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            foreach (var fleet in Fleets)
            {
                fleet.Draw(batch, Screen);
            }
        }
    }
}
