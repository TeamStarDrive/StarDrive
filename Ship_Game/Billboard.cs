using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;

namespace Ship_Game
{
	public class Billboard
	{
		private Vector3 _Position = new Vector3();

		private Vector3 _Scale = Vector3.One;

		private ISceneObject _SceneObject;

		public Billboard(SceneInterface sceneinterface, BillboardResource resource, Effect effect, Vector3 position, Vector3 scale)
		{
			this._Position = position;
			this._Scale = scale;
			this._SceneObject = new SceneObject(effect, resource.BoundingSphere, Matrix.Identity, resource.IndexBuffer, resource.VertexBuffer, resource.VertexDeclaration, resource.IndexStart, resource.PrimitiveType, resource.PrimitiveCount, resource.VertexBase, resource.VertexRange, resource.VertexStreamOffset, resource.SizeInBytes)
			{
				ObjectType = ObjectType.Dynamic,
				Visibility = ObjectVisibility.RenderedAndCastShadows,
				World = Matrix.CreateScale(this._Scale) * Matrix.CreateTranslation(this._Position)
			};
			sceneinterface.ObjectManager.Submit(this._SceneObject);
		}

		public void Update(ISceneState scenestate)
		{
			Vector3 vector3 = this._Position;
			Matrix viewToWorld = scenestate.ViewToWorld;
			Vector3? nullable = null;
			Vector3? nullable1 = null;
			Matrix billboardtransform = Matrix.CreateConstrainedBillboard(vector3, viewToWorld.Translation, Vector3.Up, nullable, nullable1);
			Matrix scaletransform = Matrix.CreateScale(this._Scale);
			this._SceneObject.World = scaletransform * billboardtransform;
		}

		public void UpdatePosition(Vector3 pos)
		{
			this._Position = pos;
		}
	}
}