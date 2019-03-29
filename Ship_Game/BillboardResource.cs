using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class BillboardResource : IDisposable
	{
	    public BoundingSphere BoundingSphere { get; }
	    public IndexBuffer IndexBuffer { get; private set; }

	    public int IndexStart { get; } = 0;
	    public int PrimitiveCount { get; } = 2;
	    public PrimitiveType PrimitiveType { get; } = PrimitiveType.TriangleList;
	    public int SizeInBytes { get; } = BillboardVertex.SizeInBytes;
	    public int VertexBase { get; } = 0;
	    public VertexBuffer VertexBuffer { get; private set; }
	    public VertexDeclaration VertexDeclaration { get; private set; }
	    public int VertexRange { get; } = 6;
	    public int VertexStreamOffset { get; } = 0;

	    public BillboardResource(GraphicsDevice device)
		{
			VertexDeclaration = new VertexDeclaration(device, BillboardVertex.VertexElements);
			VertexBuffer = new VertexBuffer(device, typeof(BillboardVertex), 4, BufferUsage.WriteOnly);
			IndexBuffer = new IndexBuffer(device, typeof(short), 6, BufferUsage.WriteOnly);
			short[] indices = { 0, 1, 2, 2, 1, 3 };
			Vector3[] positions = { new Vector3(0.5f, 1f, 0f), new Vector3(-0.5f, 1f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(-0.5f, 0f, 0f) };
			Vector2[] uvs = { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
			Vector3[] tangents = new Vector3[4];
			Vector3[] binormals = new Vector3[4];
			BuildTangentSpaceDataForTriangleList(indices, positions, uvs, tangents, binormals);
			BillboardVertex[] verts = new BillboardVertex[4];
			verts[0].Position = positions[0];
			verts[0].TextureCoordinate = uvs[0];
			verts[0].Normal = Vector3.Forward;
			verts[0].Tangent = tangents[0];
			verts[0].Binormal = binormals[0];
			verts[1].Position = positions[1];
			verts[1].TextureCoordinate = uvs[1];
			verts[1].Normal = Vector3.Forward;
			verts[1].Tangent = tangents[1];
			verts[1].Binormal = binormals[1];
			verts[2].Position = positions[2];
			verts[2].TextureCoordinate = uvs[2];
			verts[2].Normal = Vector3.Forward;
			verts[2].Tangent = tangents[2];
			verts[2].Binormal = binormals[2];
			verts[3].Position = positions[3];
			verts[3].TextureCoordinate = uvs[3];
			verts[3].Normal = Vector3.Forward;
			verts[3].Tangent = tangents[3];
			verts[3].Binormal = binormals[3];
			VertexBuffer.SetData(verts);
			IndexBuffer.SetData(indices);
			BoundingSphere = BoundingSphere.CreateFromPoints(positions);
		}

		private void BuildTangentSpaceDataForTriangleList(short[] indices, Vector3[] positions, Vector2[] uvs, Vector3[] tangents, Vector3[] binormals)
		{
			for (int i = 0; i < indices.Length; i = i + 3)
			{
				int index_vert0 = indices[i];
				int index_vert1 = indices[i + 1];
				int index_vert2 = indices[i + 2];
				Vector2 uv0 = uvs[index_vert0];
				Vector2 uv1 = uvs[index_vert1];
				Vector2 uv2 = uvs[index_vert2];
				float s1 = uv1.X - uv0.X;
				float s2 = uv2.X - uv0.X;
				float t1 = uv1.Y - uv0.Y;
				float t2 = uv2.Y - uv0.Y;
				float r = s1 * t2 - s2 * t1;
				if (r != 0f)
				{
					r = 1f / r;
					Vector3 position0 = positions[index_vert0];
					Vector3 position1 = positions[index_vert1];
					Vector3 position2 = positions[index_vert2];
					float x1 = position1.X - position0.X;
					float x2 = position2.X - position0.X;
					float y1 = position1.Y - position0.Y;
					float y2 = position2.Y - position0.Y;
					float z1 = position1.Z - position0.Z;
					float z2 = position2.Z - position0.Z;
					Vector3 tangent = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
					Vector3 binormal = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);
					tangents[index_vert0] = tangents[index_vert0] + tangent;
					tangents[index_vert1] = tangents[index_vert1] + tangent;
					tangents[index_vert2] = tangents[index_vert2] + tangent;
					binormals[index_vert0] = binormals[index_vert0] + binormal;
					binormals[index_vert1] = binormals[index_vert1] + binormal;
					binormals[index_vert2] = binormals[index_vert2] + binormal;
				}
			}
			for (int i = 0; i < tangents.Length; i++)
			{
				tangents[i].Normalize();
				binormals[i].Normalize();
			}
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BillboardResource() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            IndexBuffer?.Dispose();
            VertexBuffer?.Dispose();
            VertexDeclaration?.Dispose();
            IndexBuffer = null;
            VertexBuffer = null;
            VertexDeclaration = null;
        }

		private struct BillboardVertex
		{
			public Vector3 Position;

			public Vector3 Normal;

			public Vector2 TextureCoordinate;

			public Vector3 Tangent;

			public Vector3 Binormal;

			public static readonly VertexElement[] VertexElements =
            {
                new VertexElement(0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
                new VertexElement(0, 12, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
                new VertexElement(0, 24, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(0, 32, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Tangent, 0),
                new VertexElement(0, 44, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Binormal, 0)
            };

			public static int SizeInBytes => 56;

            //static BillboardVertex()
            //{
            //    VertexElement[] vertexElement = new VertexElement[] { new VertexElement(0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0), new VertexElement(0, 12, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0), new VertexElement(0, 24, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0), new VertexElement(0, 32, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Tangent, 0), new VertexElement(0, 44, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Binormal, 0) };
            //    BillboardResource.BillboardVertex.VertexElements = vertexElement;
            //}
		}
	}
}