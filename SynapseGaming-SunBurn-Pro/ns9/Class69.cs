// Decompiled with JetBrains decompiler
// Type: ns9.Class69
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ns9
{
  internal class Class69 : IDisposable
  {
    private BoundingBox boundingBox_0 = new BoundingBox();
    private int int_0;
    private IndexBuffer indexBuffer_0;
    private VertexBuffer vertexBuffer_0;
    private VertexDeclaration vertexDeclaration_0;

    public BoundingBox ObjectBoundingBox
    {
      get
      {
        return this.boundingBox_0;
      }
    }

    public int VertexCount
    {
      get
      {
        return this.int_0;
      }
    }

    public IndexBuffer IndexBuffer
    {
      get
      {
        return this.indexBuffer_0;
      }
    }

    public VertexBuffer VertexBuffer
    {
      get
      {
        return this.vertexBuffer_0;
      }
    }

    public VertexDeclaration VertexDeclaration
    {
      get
      {
        return this.vertexDeclaration_0;
      }
    }

    public unsafe void method_0(GraphicsDevice graphicsDevice_0, Struct1[] struct1_0, ushort[] ushort_0, int int_1)
    {
      if (this.vertexBuffer_0 == null)
      {
        this.vertexDeclaration_0 = new VertexDeclaration(graphicsDevice_0, Struct1.vertexElement_0);
        this.vertexBuffer_0 = new VertexBuffer(graphicsDevice_0, typeof (Struct1), 4096, BufferUsage.None);
        this.indexBuffer_0 = new IndexBuffer(graphicsDevice_0, typeof (ushort), 6144, BufferUsage.None);
      }
      fixed (Struct1* struct1Ptr1 = struct1_0)
        fixed (Vector3* vector3Ptr1 = &this.boundingBox_0.Max)
          fixed (Vector3* vector3Ptr2 = &this.boundingBox_0.Min)
          {
            int length = struct1_0.Length;
            Struct1* struct1Ptr2 = struct1Ptr1;
            vector3Ptr2->X = vector3Ptr1->X = struct1Ptr2->vector3_0.X;
            vector3Ptr2->Y = vector3Ptr1->Y = struct1Ptr2->vector3_0.Y;
            vector3Ptr1->Z = 1f;
            vector3Ptr2->Z = 0.0f;
            Struct1* struct1Ptr3 = struct1Ptr2 + 1;
            for (int index = 1; index < length; ++index)
            {
              if ((double) struct1Ptr3->vector3_0.X > (double) vector3Ptr1->X)
                vector3Ptr1->X = struct1Ptr3->vector3_0.X;
              else if ((double) struct1Ptr3->vector3_0.X < (double) vector3Ptr2->X)
                vector3Ptr2->X = struct1Ptr3->vector3_0.X;
              if ((double) struct1Ptr3->vector3_0.Y > (double) vector3Ptr1->Y)
                vector3Ptr1->Y = struct1Ptr3->vector3_0.Y;
              else if ((double) struct1Ptr3->vector3_0.Y < (double) vector3Ptr2->Y)
                vector3Ptr2->Y = struct1Ptr3->vector3_0.Y;
              ++struct1Ptr3;
            }
          }
      this.int_0 = int_1 * 4;
      this.vertexBuffer_0.SetData<Struct1>(struct1_0, 0, this.int_0);
      this.indexBuffer_0.SetData<ushort>(ushort_0, 0, int_1 * 6);
      int_1 = 0;
    }

    public void Dispose()
    {
      if (this.indexBuffer_0 != null)
      {
        this.indexBuffer_0.Dispose();
        this.indexBuffer_0 = (IndexBuffer) null;
      }
      if (this.vertexBuffer_0 != null)
      {
        this.vertexBuffer_0.Dispose();
        this.vertexBuffer_0 = (VertexBuffer) null;
      }
      if (this.vertexDeclaration_0 == null)
        return;
      this.vertexDeclaration_0.Dispose();
      this.vertexDeclaration_0 = (VertexDeclaration) null;
    }
  }
}
