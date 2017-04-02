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
    internal class Buffer : IDisposable
    {
        private BoundingBox BoundingBox = new BoundingBox();
        public BoundingBox ObjectBoundingBox => BoundingBox;

        public int VertexCount { get; private set; }
        public IndexBuffer IndexBuffer { get; private set; }
        public VertexBuffer VertexBuffer { get; private set; }
        public VertexDeclaration VertexDeclaration { get; private set; }

        public unsafe void method_0(GraphicsDevice device, Struct1[] struct1_0, ushort[] ushort_0, int int_1)
        {
            if (VertexBuffer == null)
            {
                VertexDeclaration = new VertexDeclaration(device, Struct1.vertexElement_0);
                VertexBuffer = new VertexBuffer(device, typeof(Struct1), 4096, BufferUsage.None);
                IndexBuffer = new IndexBuffer(device, typeof(ushort), 6144, BufferUsage.None);
            }
            fixed (Struct1* struct1Ptr1 = struct1_0)
            fixed (Vector3* pMax = &BoundingBox.Max)
            fixed (Vector3* pMin = &BoundingBox.Min)
            {
                int length = struct1_0.Length;
                pMin->X = pMax->X = struct1Ptr1->vector3_0.X;
                pMin->Y = pMax->Y = struct1Ptr1->vector3_0.Y;
                pMax->Z = 1f;
                pMin->Z = 0.0f;
                Struct1* struct1Ptr3 = struct1Ptr1 + 1;
                for (int i = 1; i < length; ++i, ++struct1Ptr3)
                {
                    if      (struct1Ptr3->vector3_0.X > pMax->X) pMax->X = struct1Ptr3->vector3_0.X;
                    else if (struct1Ptr3->vector3_0.X < pMin->X) pMin->X = struct1Ptr3->vector3_0.X;
                    if      (struct1Ptr3->vector3_0.Y > pMax->Y) pMax->Y = struct1Ptr3->vector3_0.Y;
                    else if (struct1Ptr3->vector3_0.Y < pMin->Y) pMin->Y = struct1Ptr3->vector3_0.Y;
                }
            }
            VertexCount = int_1 * 4;
            VertexBuffer.SetData(struct1_0, 0, VertexCount);
            IndexBuffer.SetData(ushort_0, 0, int_1 * 6);
            int_1 = 0;
        }

        public void Dispose()
        {
            if (IndexBuffer != null)
            {
                IndexBuffer.Dispose();
                IndexBuffer = null;
            }
            if (VertexBuffer != null)
            {
                VertexBuffer.Dispose();
                VertexBuffer = null;
            }
            if (VertexDeclaration != null)
            {
                VertexDeclaration.Dispose();
                VertexDeclaration = null;
            }
        }
    }
}
