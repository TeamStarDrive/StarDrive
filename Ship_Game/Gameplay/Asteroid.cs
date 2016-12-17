using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using MsgPack.Serialization;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Gameplay
{
	public sealed class Asteroid : GameplayObject
	{
		[MessagePackMember(9)]  public Vector3 Position3D;
        [MessagePackMember(10)] public float Scale = 1.0f;
        [XmlIgnore][MessagePackIgnore] private Vector3 RotationRadians;
        [XmlIgnore][MessagePackIgnore] private readonly Vector3 Spin;
        [XmlIgnore][MessagePackIgnore] private readonly int AsteroidId;

        [XmlIgnore][MessagePackIgnore] public SceneObject So;

		public Asteroid()
		{
            Spin            = RandomMath.Vector3D(0.01f, 0.2f);
            RotationRadians = RandomMath.Vector3D(0.01f, 1.02f);
            AsteroidId      = RandomMath.InRange(ResourceManager.NumAsteroidModels);
		}

	    public override void Initialize()
		{
			So = new SceneObject(ResourceManager.GetAsteroidModel(AsteroidId).Meshes[0])
			{
				ObjectType = ObjectType.Static,
				Visibility = ObjectVisibility.Rendered
			};

			Radius   = So.ObjectBoundingSphere.Radius * Scale * 0.65f;
            Position = Center = new Vector2(Position3D.X, Position3D.Y);
			So.AffineTransform(Position3D, RotationRadians, Scale);
		}

        //private static int LogicFlip = 0;
		public override void Update(float elapsedTime)
		{
            if (!Active 
                ||  Empire.Universe.viewState > UniverseScreen.UnivScreenState.SystemView 
                || !Empire.Universe.Frustum.Contains(Position, 10f))
                return;

            RotationRadians += Spin * elapsedTime;
            So.AffineTransform(Position3D, RotationRadians, Scale);
		}
	}
}