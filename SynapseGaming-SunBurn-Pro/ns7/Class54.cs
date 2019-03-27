// Decompiled with JetBrains decompiler
// Type: ns7.Class54
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;

namespace ns7
{
  internal class Class54 : IDisposable
  {
    private static List<VertexPositionNormalTextureBump> list_0 = new List<VertexPositionNormalTextureBump>(64);

    public int PrimitiveCount { get; private set; }

    public int VertexStride { get; private set; }

    public VertexBuffer VertexBuffer { get; private set; }

    public VertexDeclaration VertexDeclaration { get; private set; }

    public Class54(GraphicsDevice device, int slices)
    {
      list_0.Clear();
      float num1 = 1f;
      float num2 = num1 / slices;
      float float_0 = num2;
      float radians = MathHelper.ToRadians(90f);
      for (int index = 0; index < slices - 1; ++index)
      {
        float num3 = float_0 / num1;
        float num4 = (float) Math.Cos(radians * (double) num3);
        this.method_1(list_0, float_0, num4 * num1, num1, 1f - num3);
        float_0 += num2;
      }
      this.method_0(list_0, Matrix.CreateRotationZ(MathHelper.ToRadians(180f)));
      this.method_1(list_0, 0.0f, num1, num1, 1f);
      this.method_0(list_0, Matrix.CreateRotationZ(MathHelper.ToRadians(90f)));
      VertexPositionNormalTextureBump[] array = list_0.ToArray();
      this.PrimitiveCount = array.Length / 3;
      this.VertexStride = VertexPositionNormalTextureBump.SizeInBytes;
      this.VertexBuffer = new VertexBuffer(device, typeof (VertexPositionNormalTextureBump), array.Length, BufferUsage.WriteOnly);
      this.VertexBuffer.SetData(array);
      this.VertexDeclaration = new VertexDeclaration(device, VertexPositionNormalTextureBump.VertexElements);
    }

    public void Dispose()
    {
      this.PrimitiveCount = 0;
      this.VertexBuffer.Dispose();
      this.VertexDeclaration.Dispose();
    }

    private void method_0(List<VertexPositionNormalTextureBump> list_1, Matrix matrix_0)
    {
      int count = list_1.Count;
      for (int i = 0; i < count; ++i)
      {
        VertexPositionNormalTextureBump vertex = list_1[i];
        vertex.Position = Vector3.Transform(vertex.Position, matrix_0);
        vertex.Normal = Vector3.TransformNormal(vertex.Normal, matrix_0);
        list_1.Add(vertex);
      }
    }

    private void method_1(List<VertexPositionNormalTextureBump> list_1, float float_0, float float_1, float float_2, float float_3)
    {
      var vertex = new VertexPositionNormalTextureBump();
      var point2 = new Vector3(float_1, float_0, -float_2);
      var point3 = new Vector3(-float_1, float_0, -float_2);
      Vector3 normal = new Plane(Vector3.Zero, point2, point3).Normal;
      vertex.Position = Vector3.Zero;
      vertex.Normal = normal;
      vertex.TextureCoordinate = Vector2.Zero;
      vertex.Tangent = Vector3.One * float_3;
      list_1.Add(vertex);
      vertex.Position = point2;
      vertex.Normal = normal;
      vertex.TextureCoordinate = new Vector2(1f, 0.0f);
      vertex.Tangent = Vector3.Zero;
      list_1.Add(vertex);
      vertex.Position = point3;
      vertex.Normal = normal;
      vertex.TextureCoordinate = new Vector2(0.0f, 1f);
      vertex.Tangent = Vector3.Zero;
      list_1.Add(vertex);
    }
  }
}
