using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Lights;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Xna.Framework.Content;

namespace Ship_Game
{
	public sealed class ShieldManager
	{
		private static readonly BatchRemovalCollection<Shield> ShieldList = new BatchRemovalCollection<Shield>();
		private static readonly BatchRemovalCollection<Shield> PlanetaryShieldList = new BatchRemovalCollection<Shield>();

		private static Model     ShieldModel;
		private static Texture2D ShieldTexture;
		private static Texture2D GradientTexture;
		private static Effect    ShieldEffect;

		private const float Y = 0.0f;
		private const float Z = 2.8f;

        public static void LoadContent(GameContentManager content)
        {
            ShieldModel     = content.Load<Model>("Model/Projectiles/shield");
			ShieldTexture   = content.Load<Texture2D>("Model/Projectiles/shield_d");
			GradientTexture = content.Load<Texture2D>("Model/Projectiles/shieldgradient");
			ShieldEffect    = content.Load<Effect>("Effects/scale");
        }

		public static void Draw(Matrix view, Matrix projection)
		{
			lock (GlobalStats.ShieldLocker)
			{
				foreach (Shield shield in ShieldList)
				{
					Vector3 shieldcenter = new Vector3(shield.Owner.Center, 0f);
					if (Ship.universeScreen.Frustum.Contains(shieldcenter) == ContainmentType.Disjoint)
						continue;
					if (shield.pointLight.Intensity <= 0f)
						shield.pointLight.Enabled = false;
					if (shield.texscale <= 0f)
						continue;

					Matrix w = ((Matrix.Identity * Matrix.CreateScale(shield.Radius / 100f)) * Matrix.CreateRotationZ(shield.Rotation)) * Matrix.CreateTranslation(shield.Owner.Center.X, shield.Owner.Center.Y, 0f);
					shield.World = w;
					DrawShield(shield, view, projection);
				}
				foreach (Shield shield in PlanetaryShieldList)
				{
					if (shield.pointLight.Intensity <= 0f)
					{
						shield.pointLight.Enabled = false;
					}
					if (shield.texscale <= 0f)
					{
						continue;
					}
					DrawShield(shield, view, projection);
				}
				ShieldList.ApplyPendingRemovals();
				PlanetaryShieldList.ApplyPendingRemovals();
			}
		}

		private static void DrawShield(Shield shield, Matrix view, Matrix projection)
		{
            ShieldEffect.Parameters["World"].SetValue(Matrix.CreateScale(50f) * shield.World);
			ShieldEffect.Parameters["View"].SetValue(view);
			ShieldEffect.Parameters["Projection"].SetValue(projection);
			ShieldEffect.Parameters["tex"].SetValue(ShieldTexture);
			ShieldEffect.Parameters["AlphaMap"].SetValue(GradientTexture);
			ShieldEffect.Parameters["scale"].SetValue(shield.texscale);
			ShieldEffect.Parameters["displacement"].SetValue(shield.displacement);
			ShieldEffect.CurrentTechnique = ShieldEffect.Techniques["Technique1"];

			foreach (ModelMesh mesh in ShieldModel.Meshes)
			{
				foreach (ModelMeshPart part in mesh.MeshParts)
					part.Effect = ShieldEffect;
				mesh.Draw();
			}
		}

        public static void Clear()
        {
            lock (GlobalStats.ShieldLocker)
            {
                ShieldList.Clear();
                PlanetaryShieldList.Clear();
            }
        }

        public static Shield AddPlanetaryShield(Vector2 position)
        {
            var shield = new Shield
            {
                Center = new Vector3(position.X, position.Y, 2500f),
                displacement = 0.0f,
                texscale = 2.8f,
                Rotation = 0.0f,
                World = Matrix.Identity
                        * Matrix.CreateScale(2f)
                        * Matrix.CreateRotationZ(0.0f)
                        * Matrix.CreateTranslation(position.X, position.Y, 2500f)
            };
            lock (GlobalStats.ShieldLocker)
                PlanetaryShieldList.Add(shield);
            return shield;
        }

		public static void FireShieldAnimation(GameplayObject owner, float rotation)
		{
		    Shield shield = new Shield
		    {
		        Owner    = owner,
		        texscale = Z,
		        Rotation = rotation,
		        displacement = Y,
		        World = Matrix.Identity * Matrix.CreateScale(1f)
                        * Matrix.CreateRotationZ(rotation)
                        * Matrix.CreateTranslation(owner.Center.X, owner.Center.Y, 0f)
		    };
		    lock (GlobalStats.ShieldLocker)
			    ShieldList.Add(shield);
		}

        public static Shield AddShield(GameplayObject owner, float rotation, Vector2 center)
        {
            var shield = new Shield
            {
                Owner         = owner,
                displacement  = 0.0f,
                texscale      = 2.8f,
                Rotation      = rotation,
                World = Matrix.Identity * Matrix.CreateScale(2f)
                        * Matrix.CreateRotationZ(rotation)
                        * Matrix.CreateTranslation(center.X, center.Y, 0.0f)
            };
            lock (GlobalStats.ShieldLocker)
                ShieldList.Add(shield);
            return shield;
        }

		public static void Update()
		{
			foreach (Shield shield in PlanetaryShieldList)
			{
				shield.pointLight.Intensity -= 2.45f;
				if (shield.pointLight.Intensity <= 0f)
					shield.pointLight.Enabled = false;
				if (shield.texscale <= 0f)
					continue;

				shield.World = ((Matrix.Identity 
                    * Matrix.CreateScale(shield.Radius / 100f)) 
                    * Matrix.CreateRotationZ(shield.Rotation)) 
                    * Matrix.CreateTranslation(shield.Center.X, shield.Center.Y, 2500f);

				shield.displacement += 0.085f;
				shield.texscale     -= 0.185f;
			}
			var source = Enumerable.Range(0, ShieldList.Count).ToArray();
            var rangePartitioner = Partitioner.Create(0, source.Length);
            //handle each weapon group in parallel
            Parallel.ForEach(rangePartitioner, (range, loopState) =>
            {
                //standard for loop through each weapon group.
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    Shield shield = ShieldList[i];
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
            });

            lock (GlobalStats.ShieldLocker)
            {
                for (int i = 0; i < ShieldList.Count; ++i)
                {
                    Shield shield = ShieldList[i];
                    if (shield.Owner == null || shield.Owner.Active)
                        continue;

                    ShieldList.QueuePendingRemoval(shield);
                    lock (GlobalStats.ObjectManagerLocker)
                    {
                        Empire.Universe.ScreenManager.inter.LightManager.Remove(shield.pointLight);
                    }
                }
                ShieldList.ApplyPendingRemovals();
            }
        }
	}
}