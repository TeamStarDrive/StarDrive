// Decompiled with JetBrains decompiler
// Type: ns9.Class70
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;

namespace Mesh
{
    internal class Class70
    {
        private Vector2[] vector2_0 = new Vector2[4] { new Vector2(0.0f, 0.0f), new Vector2(1f, 0.0f), new Vector2(0.0f, 1f), new Vector2(1f, 1f) };
        private Vertex[] Vertices = new Vertex[4096];
        private static ushort[] Indices = new ushort[6144];
        internal const int int_0 = 1024;
        internal const int int_1 = 4;
        internal const int int_2 = 6;
        private GraphicsDevice graphicsDevice_0;
        private int NumFaces;
        private MeshBuffer Buffer0;
        private DisposablePool<MeshBuffer> class22_0;

        public Effect Effect { get; }
        public List<MeshBuffer> Buffers { get; } = new List<MeshBuffer>(32);

        static Class70()
        {
            int num1 = 0;
            for (int index1 = 0; index1 < 1024; ++index1)
            {
                int num2 = index1 * 4;
                ushort[] ushort0_1 = Indices;
                int index2 = num1;
                int num3 = 1;
                int num4 = index2 + num3;
                int num5 = (ushort)num2;
                ushort0_1[index2] = (ushort)num5;
                ushort[] ushort0_2 = Indices;
                int index3 = num4;
                int num6 = 1;
                int num7 = index3 + num6;
                int num8 = (ushort)(num2 + 1);
                ushort0_2[index3] = (ushort)num8;
                ushort[] ushort0_3 = Indices;
                int index4 = num7;
                int num9 = 1;
                int num10 = index4 + num9;
                int num11 = (ushort)(num2 + 2);
                ushort0_3[index4] = (ushort)num11;
                ushort[] ushort0_4 = Indices;
                int index5 = num10;
                int num12 = 1;
                int num13 = index5 + num12;
                int num14 = (ushort)(num2 + 2);
                ushort0_4[index5] = (ushort)num14;
                ushort[] ushort0_5 = Indices;
                int index6 = num13;
                int num15 = 1;
                int num16 = index6 + num15;
                int num17 = (ushort)(num2 + 1);
                ushort0_5[index6] = (ushort)num17;
                ushort[] ushort0_6 = Indices;
                int index7 = num16;
                int num18 = 1;
                num1 = index7 + num18;
                int num19 = (ushort)(num2 + 3);
                ushort0_6[index7] = (ushort)num19;
            }
        }

        public Class70(GraphicsDevice device, DisposablePool<MeshBuffer> bufferfactory, Effect effect)
        {
            graphicsDevice_0 = device;
            class22_0 = bufferfactory;
            Effect = effect;
            for (int index = 0; index < Vertices.Length; ++index)
                Vertices[index].Normal = new Vector3(0.0f, 0.0f, -1f);
        }

        public unsafe void BuildSprite(ref Vector2 vector2_1, 
            ref Vector2 vector2_2, 
            float float_0, 
            ref Vector2 vector2_3, 
            ref Vector2 vector2_4, 
            ref Vector2 vector2_5, 
            float z)
        {
            if (Buffer0 == null || NumFaces >= 1024)
            {
                method_1();
                method_2();
            }
            int index1 = NumFaces * 4;
            if (index1 + 4 > Vertices.Length)
                throw new Exception("Unable to build sprite, vertex array to small for all vertices.");
            bool flag;
            float num1;
            float num2;
            if (flag = float_0 != 0.0)
            {
                num1 = (float)Math.Sin(float_0);
                num2 = (float)Math.Cos(float_0);
            }
            else
            {
                num1 = 0.0f;
                num2 = 1f;
            }
            fixed (Vertex* struct1Ptr1 = &Vertices[index1])
            fixed (Vector2* vector2Ptr1 = vector2_0)
            {
                Vertex* vertexPtr2 = struct1Ptr1;
                Vector2* vector2Ptr2 = vector2Ptr1;
                float num3 = 0.5f - vector2_3.X;
                float num4 = 0.5f - vector2_3.Y;
                for (int i = 0; i < 4; ++i)
                {
                    vertexPtr2->Coords.X = vector2Ptr2->X * vector2_4.X + vector2_5.X;
                    vertexPtr2->Coords.Y = vector2Ptr2->Y * vector2_4.Y + vector2_5.Y;
                    float num5 = vector2Ptr2->X - num3;
                    float num6 = vector2Ptr2->Y - num4;
                    if (flag)
                    {
                        float num7 = (float)(num5 * (double)num2 - num6 * (double)num1);
                        num6 = (float)(num5 * (double)num1 + num6 * (double)num2);
                        num5 = num7;
                    }
                    vertexPtr2->Position.X = num5 * vector2_1.X + vector2_2.X;
                    vertexPtr2->Position.Y = num6 * vector2_1.Y + vector2_2.Y;
                    vertexPtr2->Position.Z = z;
                    vertexPtr2->PackedBinormalTangent.X = -num1;
                    vertexPtr2->PackedBinormalTangent.Y = num2;
                    vertexPtr2->PackedBinormalTangent.Z = num1;
                    ++vertexPtr2;
                    ++vector2Ptr2;
                }
            }
            ++NumFaces;
        }

        public void method_1()
        {
            if (Buffer0 == null || NumFaces < 1)
                return;
            Buffer0.Create(graphicsDevice_0, Vertices, Indices, NumFaces);
        }

        private void method_2()
        {
            Buffer0 = class22_0.New();
            Buffers.Add(Buffer0);
            NumFaces = 0;
        }

        public void method_3()
        {
            foreach (MeshBuffer class69 in Buffers)
                class22_0.Free(class69);
            Buffer0 = null;
            Buffers.Clear();
            NumFaces = 0;
        }
    }
}
