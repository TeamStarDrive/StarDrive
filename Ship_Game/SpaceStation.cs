using System;
using SDGraphics;
using Ship_Game.Data.Mesh;
using SynapseGaming.LightingSystem.Rendering;
using Matrix = SDGraphics.Matrix;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game;

public sealed class SpaceStation
{
    SceneObject InnerSO;
    SceneObject OuterSO;
    bool IsLoadingSO;

    float ZRotation;
    const float RadiansPerSecond = RadMath.Deg1AsRads * 2;

    public SpaceStation()
    {
    }

    void UpdateTransforms(Vector2 position)
    {
        float scale = GlobalStats.Defaults.SpaceportScale;

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
        StaticMesh outerModel, innerModel = null;

        // use the root content manager, because there is not much point to clear this resource
        var content = ResourceManager.RootContent;

        if (owner == null || owner.data.SpacePortModel.IsEmpty())
        {
            innerModel = StaticMesh.LoadMesh(content, "Model/Stations/spacestation01_inner");
            outerModel = StaticMesh.LoadMesh(content, "Model/Stations/spacestation01_outer");
        }
        else
        {
            outerModel = StaticMesh.LoadMesh(content, owner.data.SpacePortModel);
        }

        if (innerModel != null)
        {
            InnerSO = innerModel.CreateSceneObject();
            if (InnerSO != null)
            {
                InnerSO.Name = "spacestation01_inner";
                InnerSO.Visibility = GlobalStats.ShipVisibility; // shadows or no?
                ScreenManager.Instance.AddObject(InnerSO);
            }
        }

        if (outerModel != null)
        {
            OuterSO = outerModel.CreateSceneObject();
            if (OuterSO != null)
            {
                OuterSO.Name = "spacestation01_outer";
                OuterSO.Visibility = GlobalStats.ShipVisibility; // shadows or no?
                ScreenManager.Instance.AddObject(OuterSO);
            }
        }

        UpdateTransforms(planet.Position);
    }

    public void RemoveSceneObject()
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
            UpdateTransforms(planet.Position);
        }
        else if (!IsLoadingSO)
        {
            // initialize the SceneObjects in the UI thread
            IsLoadingSO = true;
            planet.Universe.Screen.RunOnNextFrame(() =>
            {
                try
                {
                    CreateSceneObject(planet, planet.Owner);
                }
                catch (Exception e)
                {
                    Log.Error(e, "SpaceStation.CreateSceneObject failed");
                }
                finally
                {
                    IsLoadingSO = false;
                }
            });
        }
    }
}
