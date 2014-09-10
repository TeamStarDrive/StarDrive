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
	public class Moon : GameplayObject
	{
		public Vector3 Position3D;

		public float scale;

		public static UniverseScreen universeScreen;

		private SceneObject SO;

		public Matrix WorldMatrix;

		private BoundingSphere bs;

        public int moonType;

        public Planet planet;

		public Moon()
		{
		}

		public override void Initialize()
		{
			base.Initialize();
			this.SO = new SceneObject(((ReadOnlyCollection<ModelMesh>)ResourceManager.GetModel("Model/SpaceObjects/planet_" + (object)this.moonType).Meshes)[0]);
            this.SO.ObjectType = ObjectType.Static;
            this.SO.Visibility = ObjectVisibility.Rendered;
			this.WorldMatrix = ((Matrix.Identity * Matrix.CreateScale(this.scale)) * Matrix.CreateTranslation(this.Position3D));
			this.SO.World = this.WorldMatrix;
			base.Radius = this.SO.ObjectBoundingSphere.Radius * this.scale * 0.65f;
		}

        public SceneObject GetSO()
        {
            return this.SO;
        }

		public override void Update(float elapsedTime)
		{
			ContainmentType currentContainmentType = ContainmentType.Disjoint;
			this.bs = new BoundingSphere(new Vector3(base.Position, 0f), 200f);
			if (Moon.universeScreen.Frustum != null)
			{
				currentContainmentType = Moon.universeScreen.Frustum.Contains(this.bs);
			}
			if (this.Active)
			{
                this.Position = new Vector2(this.planet.Position.X + 2000f, this.planet.Position.Y);

				if (currentContainmentType != ContainmentType.Disjoint && Moon.universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
				{
					this.WorldMatrix = ((Matrix.Identity * Matrix.CreateScale(this.scale)) * Matrix.CreateTranslation(this.Position3D));
					if (this.SO != null)
					{
						this.SO.World = this.WorldMatrix;
					}
				}
				base.Update(elapsedTime);
			}
		}
	}
}