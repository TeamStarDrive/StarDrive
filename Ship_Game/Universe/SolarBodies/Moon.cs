using System;
using System.Xml.Serialization;
using Newtonsoft.Json;
using SDGraphics;
using Ship_Game.Data;
using Ship_Game.Data.Mesh;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe.SolarBodies;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.Gameplay
{
    [StarDataType]
    public sealed class Moon : GameplayObject
    {
        [StarData] public float MoonScale;
        [StarData] public int MoonId;
        [StarData] public int OrbitPlanetId;
        [StarData] public float OrbitRadius;
        [StarData] public float OrbitalAngle;
        [StarData] public Vector3 RotationRadians;

        [XmlIgnore][JsonIgnore] SceneObject So;
        [XmlIgnore][JsonIgnore] Planet OrbitPlanet;

        // Serialize from save game (CANNOT HAVE ARGUMENTS!)
        public Moon() : base(0, GameObjectType.Moon)
        {
        }

        // Creating new game:
        public Moon(SolarSystem system, int orbitPlanetId, int moon, float moonScale,
                    float orbitRadius, float orbitalAngle, Vector2 pos) : this()
        {
            Id = system.Universe.CreateId();
            SetSystem(system);
            OrbitPlanetId = orbitPlanetId;
            MoonId = moon;
            MoonScale = moonScale;
            OrbitRadius = orbitRadius;
            OrbitalAngle = orbitalAngle;
            Position = pos;
        }

        void CreateSceneObject()
        {
            if (So != null)
                return;

            PlanetType moon = ResourceManager.Planets.Planet(MoonId);
            So = moon.CreatePlanetSO();
            So.Visibility = GlobalStats.AsteroidVisibility;
            Radius = So.ObjectBoundingSphere.Radius * MoonScale * 0.65f;

            RotationRadians.X = (-30f).ToRadians();
            RotationRadians.Y = (-30f).ToRadians();
            So.AffineTransform(new Vector3(Position, 3200f), RotationRadians, MoonScale);
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

        public void UpdateVisibleMoon(FixedSimTime timeStep)
        {
            RotationRadians.Z -= 0.05f * timeStep.FixedTime;
            if (!System.Universe.Paused)
            {
                OrbitalAngle += (float)Math.Asin(15f / OrbitRadius);
                if (OrbitalAngle >= 360.0f) OrbitalAngle -= 360f;
            }

            if (OrbitPlanet == null)
            {
                OrbitPlanet = System.Universe.GetPlanet(OrbitPlanetId);
            }

            Position = OrbitPlanet.Center.PointFromAngle(OrbitalAngle, OrbitRadius);

            if (So != null)
            {
                So.AffineTransform(new Vector3(Position, 3200f), RotationRadians, MoonScale);
            }
            else
            {
                CreateSceneObject();
            }
        }
    }
}