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

        public void LoadContent(ScreenManager manager, Empire owner)
        {
            Model innerModel = null;
            Model outerModel;
            if (owner == null || owner.data.SpacePortModel.IsEmpty())
            {
                innerModel = ResourceManager.RootContent.Load<Model>("Model/Stations/spacestation01_inner");
                outerModel = ResourceManager.RootContent.Load<Model>("Model/Stations/spacestation01_outer");
            }
            else
            {
                outerModel = ResourceManager.RootContent.Load<Model>(owner.data.SpacePortModel);
            }

            if (innerModel != null)
            {
                InnerSO      = new SceneObject(innerModel.Meshes[0]) { ObjectType = ObjectType.Dynamic };
                InnerSO.Name = "spacestation01_inner";
                manager.AddObject(InnerSO);
            }

            OuterSO      = new SceneObject(outerModel.Meshes[0]) { ObjectType = ObjectType.Dynamic };
            OuterSO.Name = "spacestation01_outer";
            manager.AddObject(OuterSO);
            UpdateTransforms();
        }

        void UpdateTransforms()
        {
            float scale = 0.8f;
            if (GlobalStats.HasMod) // The Doctor: Mod defined spaceport 'station' art scaling
                scale = GlobalStats.ActiveModInfo.SpaceportScale;

            Vector2 position = Planet.Center;
            if (InnerSO != null)
            {
                InnerSO.World = Matrix.CreateScale(scale)
                                * Matrix.CreateRotationZ(90f.ToRadians() + ZRotation)
                                * Matrix.CreateRotationX(20f.ToRadians())
                                * Matrix.CreateRotationY(65f.ToRadians())
                                * Matrix.CreateRotationZ(90f.ToRadians())
                                * Matrix.CreateTranslation(position.X, position.Y, 600f);
            }
                
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
            {
                Log.Error("SpaceStation.SetVisibility Planet cannot be null!");
                return;
            }

            if (p.Owner?.data.SpacePortModel.IsEmpty() == true && InnerSO == null || OuterSO == null)
            {
                LoadContent(manager, Planet.Owner);
            }
            if (vis)
            {
                if (InnerSO != null)
                    InnerSO.Visibility = ObjectVisibility.RenderedAndCastShadows;

                OuterSO.Visibility = ObjectVisibility.RenderedAndCastShadows;
                return;
            }

            if (InnerSO != null)
                InnerSO.Visibility = ObjectVisibility.None;

            OuterSO.Visibility = ObjectVisibility.None;
        }

        public void Update(FixedSimTime timeStep)
        {
            ZRotation += RadiansPerSecond * timeStep.FixedTime;

            if ((Planet.Owner?.data.SpacePortModel.NotEmpty() == true || InnerSO != null) 
                && OuterSO != null && Planet.SO.Visibility == ObjectVisibility.Rendered)
            {
                UpdateTransforms();
            }
        }
    }
}