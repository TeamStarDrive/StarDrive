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
    bool DisableLoading;

    float ZRotation;
    internal float Scale { get; private set; }
    const float RadiansPerSecond = RadMath.Deg1AsRads * 2;
    public const float PosZ = 600f;

    public SpaceStation()
    {
    }

    static bool HasRaceModel(Empire owner) => owner != null && owner.data.SpacePortModel.NotEmpty();

    internal static float StationScale(Empire owner)
    {
        float scale = GlobalStats.Defaults.SpaceportScale;
        return HasRaceModel(owner) ? scale * owner.data.SpacePortScale : scale;
    }

    void UpdateTransforms(Vector2 position)
    {
        if (InnerSO == null && OuterSO == null)
            return;

        Matrix transform = Matrix.CreateScale(Scale)
                           * Matrix.CreateRotationZ(90f.ToRadians() + ZRotation)
                           * Matrix.CreateRotationX(20f.ToRadians())
                           * Matrix.CreateRotationY(65f.ToRadians())
                           * Matrix.CreateRotationZ(90f.ToRadians())
                           * Matrix.CreateTranslation(position.X, position.Y, PosZ);
        if (InnerSO != null)
            InnerSO.World = transform;
        if (OuterSO != null)
            OuterSO.World = transform;
    }

    internal void CreateSceneObject(Planet planet, Empire owner)
    {
        StaticMesh outerModel, innerModel = null;

        // use the root content manager, because there is not much point to clear this resource
        var content = ResourceManager.RootContent;

        if (!HasRaceModel(owner))
        {
            innerModel = StaticMesh.LoadMesh(content, "Model/Stations/spacestation01_inner");
            outerModel = StaticMesh.LoadMesh(content, "Model/Stations/spacestation01_outer");
        }
        else
        {
            outerModel = StaticMesh.LoadMesh(content, owner.data.SpacePortModel);
        }
        Scale = StationScale(owner);

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
        else if (!IsLoadingSO && !DisableLoading)
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
                    DisableLoading = true;
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
