using System;
using SDGraphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe.SolarBodies;
using SynapseGaming.LightingSystem.Rendering;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.Gameplay
{
    [StarDataType]
    public sealed class Moon : GameObject
    {
        [StarData] public float MoonScale;
        [StarData] public int MoonId;
        [StarData] public Planet OrbitPlanet;
        [StarData] public float OrbitRadius;
        [StarData] public float OrbitalAngle;
        [StarData] public Vector3 RotationRadians;

        SceneObject So;

        // in this case, it's into the background, away from main plane
        public const float ZPos = 3200f;

        public Vector3 Position3D => new(Position, ZPos);

        [StarDataConstructor]
        public Moon() : base(0, GameObjectType.SolarBody)
        {
        }

        // Creating new game:
        public Moon(SolarSystem system, Planet orbitPlanet, int moon, float moonScale,
                    float orbitRadius, float orbitalAngle, Vector2 pos)
            : base(system.Universe.CreateId(), GameObjectType.SolarBody)
        {
            Active = true;
            System = system;
            OrbitPlanet = orbitPlanet;
            MoonId = moon;
            MoonScale = moonScale;
            OrbitRadius = orbitRadius;
            OrbitalAngle = orbitalAngle;
            Position = pos;
        }

        //[StarDataDeserialized]
        //public void Initialize()
        //{
        //}

        void CreateSceneObject()
        {
            if (So != null || GlobalStats.IsUnitTest)
                return;

            PlanetType moon = ResourceManager.Planets.Planet(MoonId);
            So = moon.CreatePlanetSO();
            So.Visibility = GlobalStats.AsteroidVisibility;
            Radius = So.ObjectBoundingSphere.Radius * MoonScale * 0.65f;

            RotationRadians.X = (-30f).ToRadians();
            RotationRadians.Y = (-30f).ToRadians();
            So.AffineTransform(new Vector3(Position, ZPos), RotationRadians, MoonScale);
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

        public void UpdateVisibleMoon(FixedSimTime timeStep)
        {
            RotationRadians.Z -= 0.05f * timeStep.FixedTime;
            if (!System.Universe.Paused)
            {
                OrbitalAngle += (float)Math.Asin(15f / OrbitRadius);
                if (OrbitalAngle >= 360.0f) OrbitalAngle -= 360f;
            }

            Position = OrbitPlanet.Position.PointFromAngle(OrbitalAngle, OrbitRadius);

            if (So != null)
            {
                So.AffineTransform(new Vector3(Position, ZPos), RotationRadians, MoonScale);
            }
            else
            {
                CreateSceneObject();
            }
        }
    }
}