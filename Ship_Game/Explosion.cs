using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Lights;
using System;

namespace Ship_Game
{
	public sealed class Explosion
	{
		public PointLight light;

		public bool sparkWave;

		public Vector3 pos;

		public float duration;

        //public float StartDuration = 0.8f;          //Not referenced in code, removing to save memory

        public Color color;

		public Rectangle ExplosionRect;

        //public bool Flickers;          //Not referenced in code, removing to save memory

        public float Radius;

		public string ExpColor = "";

		public bool sparks = true;

		public ShipModule module;

        //public float Alpha = 255f;          //Not referenced in code, removing to save memory

        public float Rotation;

		public float shockWaveTimer;

        //public bool hasShockwave;          //Not referenced in code, removing to save memory

        //public Texture2D shockwaveTexture;          //Not referenced in code, removing to save memory

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