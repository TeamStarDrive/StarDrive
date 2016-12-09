using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Gameplay
{
	public sealed class Asteroid : GameplayObject
	{
		public Vector3 Position3D;
		public float scale;
		public float spinx;
		public float spiny;
		public float spinz;
		public float Xrotate;
		public float Yrotate;
		public float Zrotate;
		public int whichRoid;
		public static UniverseScreen universeScreen;
		private SceneObject AsteroidSO;
		public Matrix WorldMatrix;
		private BoundingSphere bs;

		public Asteroid()
		{
			spinx = RandomMath.RandomBetween(0.01f, 0.2f);
			spiny = RandomMath.RandomBetween(0.01f, 0.2f);
			spinz = RandomMath.RandomBetween(0.01f, 0.2f);
			Xrotate = RandomMath.RandomBetween(0.01f, 1.02f);
			Yrotate = RandomMath.RandomBetween(0.01f, 1.02f);
			Zrotate = RandomMath.RandomBetween(0.01f, 1.02f);
            whichRoid = RandomMath.InRange(ResourceManager.NumAsteroidModels);
			Radius = RandomMath.RandomBetween(30f, 90f);
		}

		public SceneObject GetSO()
		{
			return AsteroidSO;
		}

		public override void Initialize()
		{
			base.Initialize();
			AsteroidSO = new SceneObject(ResourceManager.GetAsteroidModel(whichRoid).Meshes[0])
			{
				ObjectType = ObjectType.Static,
				Visibility = ObjectVisibility.Rendered
			};
			WorldMatrix = ((((Matrix.Identity * Matrix.CreateScale(scale)) 
                * Matrix.CreateRotationX(Xrotate)) 
                * Matrix.CreateRotationY(Yrotate)) 
                * Matrix.CreateRotationZ(Zrotate)) 
                * Matrix.CreateTranslation(Position3D);

			AsteroidSO.World = WorldMatrix;
			Radius       = AsteroidSO.ObjectBoundingSphere.Radius * scale * 0.65f;
            Position     = new Vector2(Position3D.X, Position3D.Y);
            Position3D.X = Position.X;
            Position3D.Y = Position.Y;
            Center = Position;
		}

		public override void Update(float elapsedTime)
		{
			ContainmentType currentContainmentType = ContainmentType.Disjoint;
			bs = new BoundingSphere(new Vector3(Position, 0f), 200f);
			if (universeScreen.Frustum != null)
				currentContainmentType = universeScreen.Frustum.Contains(bs);
		    if (!Active)
                return;

		    if (currentContainmentType != ContainmentType.Disjoint && 
		        universeScreen.viewState <= UniverseScreen.UnivScreenState.SystemView)
		    {
		        Xrotate += spinx * elapsedTime;
		        Zrotate += spiny * elapsedTime;
		        Yrotate += spinz * elapsedTime;
		        WorldMatrix = ((((Matrix.Identity * Matrix.CreateScale(scale)) 
		                         * Matrix.CreateRotationX(Xrotate)) 
		                        * Matrix.CreateRotationY(Yrotate)) 
		                       * Matrix.CreateRotationZ(Zrotate)) 
		                      * Matrix.CreateTranslation(Position3D);
		        if (AsteroidSO != null)
		        {
		            AsteroidSO.World = WorldMatrix;
		        }
		    }
		    base.Update(elapsedTime);
		}
	}
}