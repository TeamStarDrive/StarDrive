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

		public static UniverseScreen universeScreen;

		private SceneObject SO;

		public Matrix WorldMatrix;

        public int moonType;

        public Guid orbitTarget;

        public float OrbitRadius;

        public float Zrotate;

        private float ZrotateAmount = 0.05f;

        public float OrbitalAngle;

		public Moon()
		{
		}

		public override void Initialize()
		{
			base.Initialize();
			this.SO = new SceneObject(((ReadOnlyCollection<ModelMesh>)ResourceManager.GetModel("Model/SpaceObjects/planet_" + (object)this.moonType).Meshes)[0]);
            this.SO.ObjectType = ObjectType.Static;
            this.SO.Visibility = ObjectVisibility.Rendered;
			this.WorldMatrix = ((Matrix.Identity * Matrix.CreateScale(this.scale)) * Matrix.CreateTranslation(new Vector3(this.Position, 2500f)));
			this.SO.World = this.WorldMatrix;
			base.Radius = this.SO.ObjectBoundingSphere.Radius * this.scale * 0.65f;
		}

        public SceneObject GetSO()
        {
            return this.SO;
        }

		public void UpdatePosition(float elapsedTime)
		{
            this.Zrotate += this.ZrotateAmount * elapsedTime;
            if (!Planet.universeScreen.Paused)
            {
                this.OrbitalAngle += (float)Math.Asin(15.0 / (double)this.OrbitRadius);
                if ((double)this.OrbitalAngle >= 360.0)
                    this.OrbitalAngle -= 360f;
            }
            this.Position = universeScreen.PlanetsDict[orbitTarget].Position.PointOnCircle(OrbitalAngle, OrbitRadius);
            this.WorldMatrix = ((Matrix.Identity * Matrix.CreateScale(this.scale) * Matrix.CreateRotationZ(-this.Zrotate)) * Matrix.CreateTranslation(new Vector3(this.Position, 3200f)));
			if (this.SO != null)
			{
				this.SO.World = this.WorldMatrix;
			}
			base.Update(elapsedTime);
		}
	}
}