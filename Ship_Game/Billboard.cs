using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
	public sealed class Billboard
	{
		private Vector3 Position;
		private readonly Vector3 Scale;

		private readonly ISceneObject SceneObject;

		public Billboard(ScreenManager screenManager, BillboardResource resource, Effect effect, Vector3 position, Vector3 scale)
		{
			Position = position;
			Scale    = scale;
			SceneObject = new SceneObject(effect, resource.BoundingSphere, Matrix.Identity, resource.IndexBuffer, resource.VertexBuffer, resource.VertexDeclaration, resource.IndexStart, resource.PrimitiveType, resource.PrimitiveCount, resource.VertexBase, resource.VertexRange, resource.VertexStreamOffset, resource.SizeInBytes)
			{
				ObjectType = ObjectType.Dynamic,
				Visibility = ObjectVisibility.RenderedAndCastShadows,
				World = Matrix.CreateScale(Scale) * Matrix.CreateTranslation(Position)
			};
            screenManager.AddObject(SceneObject);
		}

		public void Update(ISceneState scenestate)
		{
			Matrix billboardtransform = Matrix.CreateConstrainedBillboard(Position, scenestate.ViewToWorld.Translation, Vector3.Up, null, null);
			Matrix scaletransform = Matrix.CreateScale(Scale);
			SceneObject.World = scaletransform * billboardtransform;
		}

		public void UpdatePosition(Vector3 pos)
		{
			Position = pos;
		}
	}
}