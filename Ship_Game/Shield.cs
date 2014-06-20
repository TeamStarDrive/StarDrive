using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Lights;
using System;

namespace Ship_Game
{
	public class Shield
	{
		public static ContentManager content;

		public float texscale;

		public float displacement;

		public Matrix World;

		public float Radius;

		public Matrix View;

		public Matrix Projection;

		public float Rotation;

		public GameplayObject Owner;

		public Vector3 Center;

		public Model shieldModel;

		public Texture2D shieldTexture;

		public Texture2D gradientTexture;

		public Effect ShieldEffect;

		public PointLight pointLight = new PointLight();

		public Shield()
		{
		}

		public void LoadContent()
		{
			this.shieldModel = Shield.content.Load<Model>("Model/Projectiles/shield");
			this.shieldTexture = Shield.content.Load<Texture2D>("Model/Projectiles/shield_d");
			this.gradientTexture = Shield.content.Load<Texture2D>("Model/Projectiles/shieldgradient");
			this.ShieldEffect = Shield.content.Load<Effect>("Effects/scale");
		}
	}
}