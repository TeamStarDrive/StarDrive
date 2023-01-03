using System;
using SDGraphics;
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
        [StarData] public float Scale;
        [StarData] readonly float OrbitalRadius; // distance from sun
        [StarData] float OrbitalAngle; // current RADIANS angle of the orbit

        Vector3 CurrentSpin; // asteroid's current rotation around its own axis
        Vector3 SpinSpeed; // the speed of rotation around its own axis
        int AsteroidId; // which asteroid?
        SceneObject So; // 3D mesh

        // Serialized (SaveGame) asteroid
        [StarDataConstructor]
        public Asteroid() : base(0, GameObjectType.SolarBody)
        {
            Radius = 50f; // some default radius for now
        }

        // New asteroid
        public Asteroid(int id, RandomBase random, float scaleMin, float scaleMax, float orbitalRadius, float orbitalAngle)
             : base(id, GameObjectType.SolarBody)
        {
            Active = true;
            Radius = 50f; // some default radius for now
            Scale = random.Float(scaleMin, scaleMax);
            OrbitalRadius = orbitalRadius;
            OrbitalAngle = orbitalAngle;
            Initialize(random);
        }

        void Initialize(RandomBase random)
        {
            SpinSpeed = random.Vector3D(0.01f, 0.05f);
            CurrentSpin = random.Vector3D(0.01f, 1.02f);
            AsteroidId = random.InRange(ResourceManager.NumAsteroidModels);
        }

        [StarDataDeserialized]
        public void OnDeserialize(UniverseState us)
        {
            Initialize(us.Random);
        }

        void UpdatePosition(Vector2 systemPos)
        {
            Position = systemPos.PointFromRadians(OrbitalAngle, OrbitalRadius);
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
            UpdatePosition(systemPos);
            So.AffineTransform(new(Position, -500f), CurrentSpin, Scale);
            ScreenManager.Instance.AddObject(So);
        }

        public void RemoveSceneObject()
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
                float orbitSpeed = (10f * Scale) / OrbitalRadius;
                OrbitalAngle += orbitSpeed * timeStep.FixedTime;
                if (OrbitalAngle >= RadMath.TwoPI)
                    OrbitalAngle -= RadMath.TwoPI;

                CurrentSpin += SpinSpeed * timeStep.FixedTime;
                UpdatePosition(systemPos);
                So.AffineTransform(new(Position, -500f), CurrentSpin, Scale);
            }
            else
            {
                CreateSceneObject(systemPos);
            }
        }
    }
}
