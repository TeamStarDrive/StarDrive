using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using Ship_Game;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;

namespace Ship_Game.Gameplay
{
	public sealed class Moon : GameplayObject
	{
		public float scale;
        public int moonType;
        public Guid orbitTarget;
        public float OrbitRadius;
        public float Zrotate;
	    public float OrbitalAngle;

	    private const float ZrotateAmount = 0.05f;

        [XmlIgnore]
		public SceneObject So;
        [XmlIgnore]
        private Planet OrbitPlanet;

		public Moon()
		{
		}

		public override void Initialize()
		{
			base.Initialize();
		    So = new SceneObject((ResourceManager.GetModel("Model/SpaceObjects/planet_" + moonType).Meshes)[0])
		    {
		        ObjectType = ObjectType.Static,
		        Visibility = ObjectVisibility.Rendered,
		        World = Matrix.CreateScale(scale)*Matrix.CreateTranslation(new Vector3(Position, 2500f))
		    };
		    Radius = So.ObjectBoundingSphere.Radius * scale * 0.65f;
		}

		public void UpdatePosition(float elapsedTime)
		{
            Zrotate += ZrotateAmount * elapsedTime;
            if (!Planet.universeScreen.Paused)
            {
                OrbitalAngle += (float)Math.Asin(15.0 / OrbitRadius);
                if (OrbitalAngle >= 360.0f) OrbitalAngle -= 360f;
            }

            if (OrbitPlanet == null)
                OrbitPlanet = Empire.Universe.PlanetsDict[orbitTarget];

            Position = OrbitPlanet.Position.PointOnCircle(OrbitalAngle, OrbitRadius);
			So.World = Matrix.CreateScale(scale) 
                        * Matrix.CreateRotationZ(-Zrotate) 
                        * Matrix.CreateTranslation(new Vector3(Position, 3200f));
			base.Update(elapsedTime);
		}
	}
}