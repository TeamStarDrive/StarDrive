using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    public sealed class SpaceStation
    {
        public SceneObject InnerSO;
        public SceneObject OuterSO;
        public Planet Planet;

        float ZRotation;
        float RadiansPerSecond = 0.1f;

        public SpaceStation(Planet p)
        {
            Planet = p;
        }

        public void LoadContent(ScreenManager manager)
        {
            var innerModel = ResourceManager.RootContent.Load<Model>("Model/Stations/spacestation01_inner");
            var outerModel = ResourceManager.RootContent.Load<Model>("Model/Stations/spacestation01_outer");

            InnerSO = new SceneObject(innerModel.Meshes[0]) { ObjectType = ObjectType.Dynamic };
            OuterSO = new SceneObject(outerModel.Meshes[0]) { ObjectType = ObjectType.Dynamic };
            InnerSO.Name = "spacestation01_inner";
            OuterSO.Name = "spacestation01_outer";
            UpdateTransforms();

            manager.AddObject(InnerSO);
            manager.AddObject(OuterSO);
        }

        void UpdateTransforms()
        {
            float scale = 0.8f;
            if (GlobalStats.HasMod) // The Doctor: Mod defined spaceport 'station' art scaling
                scale = GlobalStats.ActiveModInfo.SpaceportScale;

            Vector2 position = Planet.Center;

            InnerSO.World = Matrix.CreateScale(scale)
                            * Matrix.CreateRotationZ(90f.ToRadians() + ZRotation)
                            * Matrix.CreateRotationX(20f.ToRadians())
                            * Matrix.CreateRotationY(65f.ToRadians())
                            * Matrix.CreateRotationZ(90f.ToRadians())
                            * Matrix.CreateTranslation(position.X, position.Y, 600f);
                
            OuterSO.World = Matrix.CreateScale(scale)
                            * Matrix.CreateRotationZ(90f.ToRadians() - ZRotation)
                            * Matrix.CreateRotationX(20f.ToRadians())
                            * Matrix.CreateRotationY(65f.ToRadians())
                            * Matrix.CreateRotationZ(90f.ToRadians())
                            * Matrix.CreateTranslation(position.X, position.Y, 600f);
        }

        public void SetVisibility(bool vis, ScreenManager manager, Planet p)
        {
            Planet = p;
            if (p == null)
                Log.Error("SpaceStation.SetVisibility Planet cannot be null!");

            if (InnerSO == null || OuterSO == null)
            {
                LoadContent(manager);
            }
            if (vis)
            {
                InnerSO.Visibility = ObjectVisibility.RenderedAndCastShadows;
                OuterSO.Visibility = ObjectVisibility.RenderedAndCastShadows;
                return;
            }
            InnerSO.Visibility = ObjectVisibility.None;
            OuterSO.Visibility = ObjectVisibility.None;
        }

        public void Update(FixedSimTime timeStep)
        {
            ZRotation += RadiansPerSecond * timeStep.FixedTime;

            if (InnerSO != null && OuterSO != null && Planet.SO.Visibility == ObjectVisibility.Rendered)
            {
                UpdateTransforms();
            }
        }
    }
}