using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Lights;
using System;

namespace Ship_Game
{
	public class Explosion
	{
		public PointLight light;

		public bool sparkWave;

		public Vector3 pos;

		public float duration;

		public float StartDuration = 0.8f;

		public Color color;

		public Rectangle ExplosionRect;

		public bool Flickers;

		public float Radius;

		public string ExpColor = "";

		public bool sparks = true;

		public ShipModule module;

		public float Alpha = 255f;

		public float Rotation;

		public float shockWaveTimer;

		public bool hasShockwave;

		public Texture2D shockwaveTexture;

		public int Animation = 1;

		public int AnimationFrames = 100;

		public string AnimationTexture;

		public string AnimationBasePath;

		public int AnimationFrame = 1;

		//private string fmt = "00000.##";

		public Explosion()
		{
		}
	}
}