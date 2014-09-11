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
		public float scale;

		public static UniverseScreen universeScreen;

		private SceneObject SO;

		public Matrix WorldMatrix;

        public int moonType;

        public Planet planet;

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

        private Vector2 GeneratePointOnCircle(float angle, Vector2 center, float radius)
        {
            return this.findPointFromAngleAndDistance(center, angle, radius);
        }

        private Vector2 findPointFromAngleAndDistance(Vector2 position, float angle, float distance)
        {
            Vector2 vector2 = new Vector2(0.0f, 0.0f);
            float num1 = angle;
            float num2 = distance;
            int num3 = 0;
            float num4 = 0.0f;
            float num5 = 0.0f;
            if ((double)num1 > 360.0)
                num1 -= 360f;
            if ((double)num1 < 90.0)
            {
                float num6 = (float)((double)(90f - num1) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 1;
            }
            else if ((double)num1 > 90.0 && (double)num1 < 180.0)
            {
                float num6 = (float)((double)(num1 - 90f) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 2;
            }
            else if ((double)num1 > 180.0 && (double)num1 < 270.0)
            {
                float num6 = (float)((double)(270f - num1) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 3;
            }
            else if ((double)num1 > 270.0 && (double)num1 < 360.0)
            {
                float num6 = (float)((double)(num1 - 270f) * 3.14159274101257 / 180.0);
                num4 = num2 * (float)Math.Sin((double)num6);
                num5 = num2 * (float)Math.Cos((double)num6);
                num3 = 4;
            }
            if ((double)num1 == 0.0)
            {
                vector2.X = position.X;
                vector2.Y = position.Y - num2;
            }
            if ((double)num1 == 90.0)
            {
                vector2.X = position.X + num2;
                vector2.Y = position.Y;
            }
            if ((double)num1 == 180.0)
            {
                vector2.X = position.X;
                vector2.Y = position.Y + num2;
            }
            if ((double)num1 == 270.0)
            {
                vector2.X = position.X - num2;
                vector2.Y = position.Y;
            }
            if (num3 == 1)
            {
                vector2.X = position.X + num5;
                vector2.Y = position.Y - num4;
            }
            else if (num3 == 2)
            {
                vector2.X = position.X + num5;
                vector2.Y = position.Y + num4;
            }
            else if (num3 == 3)
            {
                vector2.X = position.X - num5;
                vector2.Y = position.Y + num4;
            }
            else if (num3 == 4)
            {
                vector2.X = position.X - num5;
                vector2.Y = position.Y - num4;
            }
            return vector2;
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
            this.Position = this.GeneratePointOnCircle(this.OrbitalAngle, this.planet.Position, this.OrbitRadius);
            this.WorldMatrix = ((Matrix.Identity * Matrix.CreateScale(this.scale) * Matrix.CreateRotationZ(-this.Zrotate)) * Matrix.CreateTranslation(new Vector3(this.Position, 3200f)));
			if (this.SO != null)
			{
				this.SO.World = this.WorldMatrix;
			}
			base.Update(elapsedTime);
		}
	}
}