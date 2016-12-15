using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Gameplay
{
	public sealed class Asteroid : GameplayObject
	{
        // Note: These fields are all serialized
		public Vector3 Position3D;
		public float Scale = 1.0f;
        private readonly Vector3 Spin;
        private Vector3 RotationRadians;
	    private readonly int AsteroidId;

        [XmlIgnore]
        public SceneObject So { get; private set; }

		public Asteroid()
		{
			Radius          = RandomMath.RandomBetween(30f, 90f);
            Spin            = RandomMath.Vector3D(0.01f, 0.2f);
            RotationRadians = RandomMath.Vector3D(0.01f, 1.02f);
            AsteroidId      = RandomMath.InRange(ResourceManager.NumAsteroidModels);
		}

	    public override void Initialize()
		{
			base.Initialize();
			So = new SceneObject(ResourceManager.GetAsteroidModel(AsteroidId).Meshes[0])
			{
				ObjectType = ObjectType.Static,
				Visibility = ObjectVisibility.Rendered
			};

			Radius   = So.ObjectBoundingSphere.Radius * Scale * 0.65f;
            Position = Center = new Vector2(Position3D.X, Position3D.Y);
			So.World = MathExt.AffineTransform(Position3D, RotationRadians, Scale);
		}

        private static int LogicFlip = 0;
		public override void Update(float elapsedTime)
		{
            if (!Active 
                ||  Empire.Universe.viewState > UniverseScreen.UnivScreenState.SystemView 
                || !Empire.Universe.Frustum.Contains(Position, 10f))
                return;

            RotationRadians += Spin * elapsedTime;

            // WIP new faster affine transform
            //++LogicFlip;
            //if (LogicFlip > 5000)
            //{
            //    LogicFlip = 0;
            //    global::System.Diagnostics.Debug.WriteLine("Asteroids DEFAULT");
            //}
            //else if (LogicFlip >= 2500)
            //{
            //    if (LogicFlip == 2500)
            //        global::System.Diagnostics.Debug.WriteLine("Asteroids NEW");
            //    Vector3 dir = RotationRadians.DirectionFromRadians();
            //    Vector3 up = Vector3.Up;
            //    Matrix affine;
            //    Matrix.CreateWorld(ref Position3D, ref dir, ref up, out affine);
            //    So.World = Matrix.CreateScale(Scale) * affine;

            //    return;
            //}

            So.SetAffineTransform(Position3D, RotationRadians, Scale);
		}
	}
}