using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Ship_Game.Data;
using Ship_Game.Data.Mesh;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Gameplay
{
    public sealed class Moon : GameplayObject
    {
        [Serialize(8)]  public float scale;
        [Serialize(9)]  public int   moonType;
        [Serialize(10)] public Guid  orbitTarget;
        [Serialize(11)] public float OrbitRadius;
        [Serialize(12)] public float OrbitalAngle;
        [Serialize(13)] public Vector3 RotationRadians;

        [XmlIgnore][JsonIgnore] SceneObject So;
        [XmlIgnore][JsonIgnore] Planet OrbitPlanet;

        // Serialize from save game
        public Moon() : base(GameObjectType.Moon)
        {
        }

        // Creating new game:
        public Moon(Guid orbitTgt, int moon, float moonScale,
                    float orbitRadius, float orbitalAngle, Vector2 pos) : this()
        {
            orbitTarget = orbitTgt;
            moonType = moon;
            scale = moonScale;
            OrbitRadius = orbitRadius;
            OrbitalAngle = orbitalAngle;
            Position = pos;
        }

        void CreateSceneObject()
        {
            if (So != null)
                return;

            var content = Empire.Universe?.ContentManager ?? ResourceManager.RootContent;
            So = StaticMesh.GetPlanetarySceneMesh(content, "Model/SpaceObjects/planet_"+moonType);
            So.ObjectType = ObjectType.Static;
            So.Visibility = GlobalStats.AsteroidVisibility;
            Radius = So.ObjectBoundingSphere.Radius * scale * 0.65f;

            RotationRadians.X = (-30f).ToRadians();
            RotationRadians.Y = (-30f).ToRadians();
            So.AffineTransform(new Vector3(Position, 3200f), RotationRadians, scale);
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
            if (!Empire.Universe.Paused)
            {
                OrbitalAngle += (float)Math.Asin(15f / OrbitRadius);
                if (OrbitalAngle >= 360.0f) OrbitalAngle -= 360f;
            }

            if (OrbitPlanet == null)
            {
                OrbitPlanet = Empire.Universe.GetPlanet(orbitTarget);
            }

            Position = OrbitPlanet.Center.PointFromAngle(OrbitalAngle, OrbitRadius);

            if (So != null)
            {
                So.AffineTransform(new Vector3(Position, 3200f), RotationRadians, scale);
            }
            else
            {
                CreateSceneObject();
            }
        }
    }
}