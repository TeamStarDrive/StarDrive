using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Lights;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class ShieldManager
	{
		public static BatchRemovalCollection<Shield> shieldList;

		public static BatchRemovalCollection<Shield> PlanetaryShieldList;

		public static Model shieldModel;

		public static Texture2D shieldTexture;

		public static Texture2D gradientTexture;

		public static Effect ShieldEffect;

		public bool doneRunning;

		private static float y;

		private static float z;

		static ShieldManager()
		{
			ShieldManager.shieldList = new BatchRemovalCollection<Shield>();
			ShieldManager.PlanetaryShieldList = new BatchRemovalCollection<Shield>();
			ShieldManager.y = 0f;
			ShieldManager.z = 2.8f;
		}

		public ShieldManager()
		{
		}

		public static void Draw(Matrix view, Matrix projection)
		{
			lock (GlobalStats.ShieldLocker)
			{
				foreach (Shield shield in ShieldManager.shieldList)
				{
					Vector3 shieldcenter = new Vector3(shield.Owner.Center, 0f);
					if (Ship.universeScreen.Frustum.Contains(shieldcenter) == ContainmentType.Disjoint)
					{
						continue;
					}
					if (shield.pointLight.Intensity <= 0f)
					{
						shield.pointLight.Enabled = false;
					}
					if (shield.texscale <= 0f)
					{
						continue;
					}
					Matrix w = ((Matrix.Identity * Matrix.CreateScale(shield.Radius / 100f)) * Matrix.CreateRotationZ(shield.Rotation)) * Matrix.CreateTranslation(shield.Owner.Center.X, shield.Owner.Center.Y, 0f);
					shield.World = w;
					ShieldManager.DrawShield(shield, view, projection);
				}
				foreach (Shield shield in ShieldManager.PlanetaryShieldList)
				{
					if (shield.pointLight.Intensity <= 0f)
					{
						shield.pointLight.Enabled = false;
					}
					if (shield.texscale <= 0f)
					{
						continue;
					}
					ShieldManager.DrawShield(shield, view, projection);
				}
				ShieldManager.shieldList.ApplyPendingRemovals();
				ShieldManager.PlanetaryShieldList.ApplyPendingRemovals();
			}
		}

		private static void DrawShield(Shield shield, Matrix view, Matrix projection)
		{
			foreach (ModelMesh mesh in ShieldManager.shieldModel.Meshes)
			{
				ShieldManager.ShieldEffect.Parameters["World"].SetValue(Matrix.CreateScale(50f) * shield.World);
				ShieldManager.ShieldEffect.Parameters["View"].SetValue(view);
				ShieldManager.ShieldEffect.Parameters["Projection"].SetValue(projection);
				ShieldManager.ShieldEffect.Parameters["tex"].SetValue(ShieldManager.shieldTexture);
				ShieldManager.ShieldEffect.Parameters["AlphaMap"].SetValue(ShieldManager.gradientTexture);
				ShieldManager.ShieldEffect.Parameters["scale"].SetValue((float)shield.texscale);
				ShieldManager.ShieldEffect.Parameters["displacement"].SetValue((float)shield.displacement);
				ShieldManager.ShieldEffect.CurrentTechnique = ShieldManager.ShieldEffect.Techniques["Technique1"];
				foreach (ModelMeshPart part in mesh.MeshParts)
				{
					part.Effect = ShieldManager.ShieldEffect;
				}
				mesh.Draw();
			}
		}

		public static void FireShieldAnimation(GameplayObject Obj, float Rotation, float Scale)
		{
			Shield shield = new Shield();
			Matrix w = ((Matrix.Identity * Matrix.CreateScale(1f)) * Matrix.CreateRotationZ(Rotation)) * Matrix.CreateTranslation(Obj.Center.X, Obj.Center.Y, 0f);
			shield.World = w;
			shield.Owner = Obj;
			shield.displacement = ShieldManager.y;
			shield.texscale = ShieldManager.z;
			shield.Rotation = Rotation;
			ShieldManager.shieldList.Add(shield);
		}

		public static void Update()
		{
			foreach (Shield shield in ShieldManager.PlanetaryShieldList)
			{
				PointLight intensity = shield.pointLight;
				intensity.Intensity = intensity.Intensity - 2.45f;
				if (shield.pointLight.Intensity <= 0f)
				{
					shield.pointLight.Enabled = false;
				}
				if (shield.texscale <= 0f)
				{
					continue;
				}
				Matrix w = ((Matrix.Identity * Matrix.CreateScale(shield.Radius / 100f)) * Matrix.CreateRotationZ(shield.Rotation)) * Matrix.CreateTranslation(shield.Center.X, shield.Center.Y, 2500f);
				shield.World = w;
				Shield shield1 = shield;
				shield1.displacement = shield1.displacement + 0.085f;
				Shield shield2 = shield;
				shield2.texscale = shield2.texscale - 0.185f;
			}
			for (int i = 0; i < ShieldManager.shieldList.Count; i++)
			{
				Shield shield = ShieldManager.shieldList[i];
				PointLight pointLight = shield.pointLight;
				pointLight.Intensity = pointLight.Intensity - 2.45f;
				if (shield.pointLight.Intensity <= 0f)
				{
					shield.pointLight.Enabled = false;
				}
				if (shield.texscale > 0f)
				{
					Shield shield3 = shield;
					shield3.displacement = shield3.displacement + 0.085f;
					Shield shield4 = shield;
					shield4.texscale = shield4.texscale - 0.185f;
				}
			}
		}
	}
}