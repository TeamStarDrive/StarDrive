using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class ShieldManager
	{
		private static readonly Array<Shield> ShieldList = new Array<Shield>();
		private static readonly Array<Shield> PlanetaryShieldList = new Array<Shield>();

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
			    for (int index = 0; index < ShieldList.Count; index++)
			    {
			        Shield shield = ShieldList[index];
			        Vector3 shieldcenter = new Vector3(shield.Owner.Center, 0f);
			        if (Empire.Universe.Frustum.Contains(shieldcenter) == ContainmentType.Disjoint)
			            continue;
			        if (shield.pointLight.Intensity <= 0f)
			            shield.pointLight.Enabled = false;
			        if (shield.texscale <= 0f)
			            continue;

			        Matrix w = ((Matrix.Identity * Matrix.CreateScale(shield.Radius / 100f)) *
			                    Matrix.CreateRotationZ(shield.Rotation)) *
			                   Matrix.CreateTranslation(shield.Owner.Center.X, shield.Owner.Center.Y, 0f);
			        shield.World = w;
			        DrawShield(shield, view, projection);
			    }
			    for (int index = 0; index < PlanetaryShieldList.Count; index++)
			    {
			        Shield shield = PlanetaryShieldList[index];
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
            if (Empire.Universe.viewState < UniverseScreen.UnivScreenState.SectorView)
            {
                for (int index = 0; index < PlanetaryShieldList.Count; index++)
                {
                    Shield shield = PlanetaryShieldList[index];
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
                    shield.texscale -= 0.185f;
                }
                for (int index = 0; index < ShieldList.Count; index++)
                {
                    Shield shield = ShieldList[index];
                    shield.pointLight.Intensity -= 2.45f;
                    if (shield.pointLight.Intensity <= 0f)
                    {
                        shield.pointLight.Enabled = false;
                    }
                    if (shield.texscale > 0f)
                    {
                        shield.displacement += 0.085f;
                        shield.texscale -= 0.185f;
                    }
                }
            }
		    lock (GlobalStats.ShieldLocker)
            {
                for (int i = ShieldList.Count - 1; i >= 0; --i)
                {
                    Shield shield = ShieldList[i];
                    if (shield.Owner == null || shield.Owner.Active)
                        continue;
                    ShieldList.RemoveAtSwapLast(i);                    
                    lock (GlobalStats.ObjectManagerLocker)
                    {
                        Empire.Universe.ScreenManager.inter.LightManager.Remove(shield.pointLight);
                    }
                }
            }
        }
	}
}