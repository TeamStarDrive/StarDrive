using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;
using Ship_Game.Utils;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.Gameplay
{
    [StarDataType]
    public sealed class Asteroid : GameObject
    {
        [StarData] public float Scale = 1.0f; // serialized
        [XmlIgnore][JsonIgnore] Vector3 RotationRadians;
        [XmlIgnore][JsonIgnore] Vector3 Spin;
        [XmlIgnore][JsonIgnore] int AsteroidId;
        [XmlIgnore][JsonIgnore] SceneObject So;

        // Serialized (SaveGame) asteroid
        public Asteroid() : base(0, GameObjectType.Asteroid)
        {
            Radius = 50f; // some default radius for now
        }

        // New asteroid
        public Asteroid(int id, RandomBase random, float scaleMin, float scaleMax, Vector2 pos)
             : base(id, GameObjectType.Asteroid)
        {
            Radius = 50f; // some default radius for now
            Scale = random.Float(scaleMin, scaleMax);
            Position = pos;
            Initialize(random);
        }

        void Initialize(RandomBase random)
        {
            Spin            = random.Vector3D(0.01f, 0.2f);
            RotationRadians = random.Vector3D(0.01f, 1.02f);
            AsteroidId      = random.InRange(ResourceManager.NumAsteroidModels);
        }

        [StarDataDeserialized]
        public void OnDeserialize(UniverseState us)
        {
            Initialize(us.Random);
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
