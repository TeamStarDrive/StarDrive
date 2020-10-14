using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Gameplay
{
    public sealed class Asteroid : GameplayObject
    {
        [Serialize(8)] public float Scale = 1.0f; // serialized
        [XmlIgnore][JsonIgnore] Vector3 RotationRadians;
        [XmlIgnore][JsonIgnore] readonly Vector3 Spin;
        [XmlIgnore][JsonIgnore] readonly int AsteroidId;
        [XmlIgnore][JsonIgnore] SceneObject So;

        // Serialized (SaveGame) asteroid
        public Asteroid() : base(GameObjectType.Asteroid)
        {
            Spin            = RandomMath.Vector3D(0.01f, 0.2f);
            RotationRadians = RandomMath.Vector3D(0.01f, 1.02f);
            AsteroidId      = RandomMath.InRange(ResourceManager.NumAsteroidModels);
            Radius = 50f; // some default radius for now
        }

        // New asteroid
        public Asteroid(float scaleMin, float scaleMax, Vector2 pos) : this()
        {
            Scale = RandomMath.RandomBetween(scaleMin, scaleMax);
            Position = pos;
        }

        void CreateSceneObject()
        {
            if (So != null)
                return;

            So = new SceneObject(ResourceManager.GetAsteroidModel(AsteroidId).Meshes[0])
            {
                ObjectType = ObjectType.Static,
                Visibility = GlobalStats.AsteroidVisibility
            };
            Radius = So.ObjectBoundingSphere.Radius * Scale * 0.65f;
            So.AffineTransform(new Vector3(Position, -500f), RotationRadians, Scale);
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
        public void UpdateVisibleAsteroid(FixedSimTime timeStep)
        {
            if (So != null)
            {
                Center = Position; // TODO: why do we have Center and Position both...
                RotationRadians += Spin * timeStep.FixedTime;
                So.AffineTransform(new Vector3(Position, -500f), RotationRadians, Scale);
            }
            else
            {
                CreateSceneObject();
            }
        }
    }
}
