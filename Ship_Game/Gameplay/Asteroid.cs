using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Data.Serialization;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Gameplay
{
    [StarDataType]
    public sealed class Asteroid : GameplayObject
    {
        [StarData] public float Scale = 1.0f; // serialized
        [XmlIgnore][JsonIgnore] Vector3 RotationRadians;
        [XmlIgnore][JsonIgnore] readonly Vector3 Spin;
        [XmlIgnore][JsonIgnore] readonly int AsteroidId;
        [XmlIgnore][JsonIgnore] SceneObject So;

        // Serialized (SaveGame) asteroid
        public Asteroid() : base(0, GameObjectType.Asteroid)
        {
            Spin            = RandomMath.Vector3D(0.01f, 0.2f);
            RotationRadians = RandomMath.Vector3D(0.01f, 1.02f);
            AsteroidId      = RandomMath.InRange(ResourceManager.NumAsteroidModels);
            Radius = 50f; // some default radius for now
        }

        // New asteroid
        public Asteroid(int id, float scaleMin, float scaleMax, Vector2 pos)
            : this()
        {
            Id = id;
            Scale = RandomMath.RandomBetween(scaleMin, scaleMax);
            Position = pos;
        }

        void CreateSceneObject(Vector2 systemPos)
        {
            if (So != null || GlobalStats.AsteroidVisibility == ObjectVisibility.None)
                return;

            var model = ResourceManager.GetAsteroidModel(AsteroidId);
            So = new SceneObject(model.Meshes[0])
            {
                Name = model.Meshes[0].Name,
                ObjectType = ObjectType.Static,
                Visibility = GlobalStats.AsteroidVisibility
            };
            Radius = So.ObjectBoundingSphere.Radius * Scale * 0.65f;
            So.AffineTransform(new Vector3(systemPos + Position, -500f), RotationRadians, Scale);
            ScreenManager.Instance.AddObject(So);
        }

        public void DestroySceneObject()
        {
            if (So != null)
            {
                ScreenManager.Instance.RemoveObject(So);
                So = null;
            }
        }

        // NOTE: Asteroids are updated ONLY if they are visible!
        //       so we do NOT need additional visibility checks
        public void UpdateVisibleAsteroid(Vector2 systemPos, FixedSimTime timeStep)
        {
            if (So != null)
            {
                RotationRadians += Spin * timeStep.FixedTime;
                So.AffineTransform(new Vector3(systemPos + Position, -500f), RotationRadians, Scale);
            }
            else
            {
                CreateSceneObject(systemPos);
            }
        }
    }
}
