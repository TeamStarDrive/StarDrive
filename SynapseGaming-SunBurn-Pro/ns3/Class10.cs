// Decompiled with JetBrains decompiler
// Type: ns3.Class10
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns11;

namespace ns3
{
  internal class Class10 : IDisposable
  {
    private static Vector3[] vector3_0 = new Vector3[8]{ new Vector3(1f, 1f, 1f), new Vector3(0.0f, 1f, 1f), new Vector3(1f, 0.0f, 1f), new Vector3(0.0f, 0.0f, 1f), new Vector3(1f, 1f, 0.0f), new Vector3(0.0f, 1f, 0.0f), new Vector3(1f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f) };
    private static int[] int_0 = new int[24]{ 1, 0, 3, 2, 4, 5, 6, 7, 0, 4, 2, 6, 5, 1, 7, 3, 0, 1, 4, 5, 3, 2, 7, 6 };
    private GraphicsDevice graphicsDevice_0;
    private BasicEffect basicEffect_0;
    private VertexBuffer vertexBuffer_0;
    private IndexBuffer indexBuffer_0;
    private VertexDeclaration vertexDeclaration_0;

    public BasicEffect DefaultEffect => this.basicEffect_0;

    public Class10(GraphicsDevice device)
    {
      this.graphicsDevice_0 = device;
      this.basicEffect_0 = new BasicEffect(this.graphicsDevice_0, null);
      this.basicEffect_0.TextureEnabled = false;
      this.basicEffect_0.VertexColorEnabled = false;
      this.basicEffect_0.PreferPerPixelLighting = false;
      this.basicEffect_0.LightingEnabled = false;
      this.basicEffect_0.FogEnabled = false;
      this.basicEffect_0.SpecularPower = 0.0f;
      VertexPositionColor[] data1 = new VertexPositionColor[24];
      short[] data2 = new short[36];
      int index1 = 0;
      int num1 = 0;
      for (int index2 = 0; index2 < 6; ++index2)
      {
        Vector3 vector3_1 = vector3_0[int_0[index1]];
        Vector3 vector3_2 = vector3_0[int_0[index1 + 1]];
        Vector3 vector3_3 = vector3_0[int_0[index1 + 2]];
        Vector3 vector3_4 = vector3_0[int_0[index1 + 3]];
        data1[index1].Position = vector3_1;
        data1[index1 + 1].Position = vector3_2;
        data1[index1 + 2].Position = vector3_3;
        data1[index1 + 3].Position = vector3_4;
        short[] numArray1 = data2;
        int index3 = num1;
        int num2 = 1;
        int num3 = index3 + num2;
        int num4 = (byte) index1;
        numArray1[index3] = (short) num4;
        short[] numArray2 = data2;
        int index4 = num3;
        int num5 = 1;
        int num6 = index4 + num5;
        int num7 = (byte) (index1 + 1);
        numArray2[index4] = (short) num7;
        short[] numArray3 = data2;
        int index5 = num6;
        int num8 = 1;
        int num9 = index5 + num8;
        int num10 = (byte) (index1 + 2);
        numArray3[index5] = (short) num10;
        short[] numArray4 = data2;
        int index6 = num9;
        int num11 = 1;
        int num12 = index6 + num11;
        int num13 = (byte) (index1 + 3);
        numArray4[index6] = (short) num13;
        short[] numArray5 = data2;
        int index7 = num12;
        int num14 = 1;
        int num15 = index7 + num14;
        int num16 = (byte) (index1 + 2);
        numArray5[index7] = (short) num16;
        short[] numArray6 = data2;
        int index8 = num15;
        int num17 = 1;
        num1 = index8 + num17;
        int num18 = (byte) (index1 + 1);
        numArray6[index8] = (short) num18;
        index1 += 4;
      }
      this.vertexBuffer_0 = new VertexBuffer(this.graphicsDevice_0, typeof (VertexPositionColor), data1.Length, BufferUsage.WriteOnly);
      this.vertexBuffer_0.SetData(data1);
      this.indexBuffer_0 = new IndexBuffer(this.graphicsDevice_0, typeof (short), data2.Length, BufferUsage.WriteOnly);
      this.indexBuffer_0.SetData(data2);
      this.vertexDeclaration_0 = new VertexDeclaration(this.graphicsDevice_0, VertexPositionColor.VertexElements);
    }

    public Matrix method_0(BoundingBox boundingBox_0)
    {
      Matrix scale = Matrix.CreateScale(boundingBox_0.Max - boundingBox_0.Min);
      scale.Translation = boundingBox_0.Min;
      return scale;
    }

    public void method_1()
    {
      this.graphicsDevice_0.Vertices[0].SetSource(this.vertexBuffer_0, 0, VertexPositionColor.SizeInBytes);
      this.graphicsDevice_0.VertexDeclaration = this.vertexDeclaration_0;
      this.graphicsDevice_0.Indices = this.indexBuffer_0;
      this.graphicsDevice_0.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 24, 0, 12);
    }

    public void Dispose()
    {
      Disposable.Dispose(ref this.basicEffect_0);
      Disposable.Dispose(ref this.vertexBuffer_0);
      Disposable.Dispose(ref this.indexBuffer_0);
      Disposable.Dispose(ref this.vertexDeclaration_0);
    }
  }
}
