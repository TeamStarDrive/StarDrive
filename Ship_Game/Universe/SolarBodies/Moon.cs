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
        [Serialize(9)]  public float scale;
        [Serialize(10)] public int   moonType;
        [Serialize(11)] public Guid  orbitTarget;
        [Serialize(12)] public float OrbitRadius;
        [Serialize(13)] public float Zrotate;
        [Serialize(14)] public float OrbitalAngle;

        [XmlIgnore][JsonIgnore] public SceneObject So;
        [XmlIgnore][JsonIgnore] Planet OrbitPlanet;

        public Moon() : base(GameObjectType.Moon)
        {
        }

        public override void Initialize()
        {
            So = StaticMesh.GetPlanetarySceneMesh(ResourceManager.RootContent, "Model/SpaceObjects/planet_" + moonType);
            So.ObjectType = ObjectType.Static;
            So.Visibility = ObjectVisibility.Rendered;
            So.World = Matrix.CreateScale(scale)*Matrix.CreateTranslation(new Vector3(Position, 2500f));

            Radius = So.ObjectBoundingSphere.Radius * scale * 0.65f;
            base.Initialize();
        }

        public void UpdatePosition(float elapsedTime)
        {
            Zrotate += 0.05f * elapsedTime;
            if (!Empire.Universe.Paused)
            {
                OrbitalAngle += (float)Math.Asin(15.0 / OrbitRadius);
                if (OrbitalAngle >= 360.0f) OrbitalAngle -= 360f;
            }

            if (OrbitPlanet == null)
                OrbitPlanet = Empire.Universe.PlanetsDict[orbitTarget];

            Position = OrbitPlanet.Center.PointOnCircle(OrbitalAngle, OrbitRadius);
            So.World = Matrix.CreateScale(scale) 
                        * Matrix.CreateRotationZ(-Zrotate) 
                        * Matrix.CreateTranslation(new Vector3(Position, 3200f));
            Update(elapsedTime);
        }
    }
}