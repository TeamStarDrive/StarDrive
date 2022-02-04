using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
    public sealed class SpaceStation
    {
        SceneObject InnerSO;
        SceneObject OuterSO;

        float ZRotation;
        const float RadiansPerSecond = RadMath.Deg1AsRads * 2;

        public SpaceStation()
        {
        }

        void UpdateTransforms(Vector2 position)
        {
            float scale = 0.8f;
            if (GlobalStats.HasMod) // The Doctor: Mod defined spaceport 'station' art scaling
                scale = GlobalStats.ActiveModInfo.SpaceportScale;

            Matrix transform = Matrix.CreateScale(scale)
                             * Matrix.CreateRotationZ(90f.ToRadians() + ZRotation)
                             * Matrix.CreateRotationX(20f.ToRadians())
                             * Matrix.CreateRotationY(65f.ToRadians())
                             * Matrix.CreateRotationZ(90f.ToRadians())
                             * Matrix.CreateTranslation(position.X, position.Y, 600f);
            if (InnerSO != null)
                InnerSO.World = transform;
            OuterSO.World = transform;
        }

        void CreateSceneObject(Planet planet, Empire owner)
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
                InnerSO = new SceneObject(innerModel.Meshes[0])
                {
                    ObjectType = ObjectType.Dynamic,
                    Visibility = GlobalStats.ShipVisibility,
                };
                InnerSO.Name = "spacestation01_inner";
                ScreenManager.Instance.AddObject(InnerSO);
            }

            OuterSO = new SceneObject(outerModel.Meshes[0])
            {
                ObjectType = ObjectType.Dynamic,
                Visibility = GlobalStats.ShipVisibility,
            };
            OuterSO.Name = "spacestation01_outer";
            ScreenManager.Instance.AddObject(OuterSO);
            UpdateTransforms(planet.Center);
        }

        public void DestroySceneObject()
        {
            if (InnerSO != null)
            {
                ScreenManager.Instance.RemoveObject(InnerSO);
                InnerSO = null;
            }
            if (OuterSO != null)
            {
                ScreenManager.Instance.RemoveObject(OuterSO);
                OuterSO = null;
            }
        }

        public void UpdateVisibleStation(Planet planet, FixedSimTime timeStep)
        {
            if (OuterSO != null)
            {
                ZRotation += RadiansPerSecond * timeStep.FixedTime;
                UpdateTransforms(planet.Center);
            }
            else
            {
                CreateSceneObject(planet, planet.Owner);
            }
        }
    }
}