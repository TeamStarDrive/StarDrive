using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;

namespace Mesh
{
    internal class SpriteFactory
    {
        private readonly Vector2[] Coords =
        {
            new Vector2(0.0f, 0.0f),
            new Vector2(1f, 0.0f),
            new Vector2(0.0f, 1f),
            new Vector2(1f, 1f)
        };
        private readonly SpriteVertex[] Vertices = new SpriteVertex[4096];
        private static readonly ushort[] Indices = new ushort[6144];
        private static bool Initialized = false;

        internal const int int_0 = 1024;
        internal const int int_1 = 4;
        internal const int int_2 = 6;
        private readonly GraphicsDevice Device;
        private int NumFaces;
        private MeshBuffer Buffer;
        private readonly DisposablePool<MeshBuffer> BuffFactory;

        public Effect Effect { get; }
        public List<MeshBuffer> Buffers { get; } = new List<MeshBuffer>(32);

        public SpriteFactory(GraphicsDevice device, DisposablePool<MeshBuffer> bufferfactory, Effect effect)
        {
            Device = device;
            BuffFactory = bufferfactory;
            Effect = effect;

            Vector3 normal = new Vector3(0.0f, 0.0f, -1.0f);
            for (int i = 0; i < Vertices.Length; ++i)
                Vertices[i].Normal = normal;

            if (!Initialized)
            {
                Initialized = true;

                int index = 0;
                for (int i = 0; i < 1024; ++i)
                {
                    int num2 = i * 4;
                    Indices[index + 0] = (ushort)(num2 + 0);
                    Indices[index + 1] = (ushort)(num2 + 1);
                    Indices[index + 2] = (ushort)(num2 + 2);
                    Indices[index + 3] = (ushort)(num2 + 2);
                    Indices[index + 4] = (ushort)(num2 + 1);
                    Indices[index + 5] = (ushort)(num2 + 3);
                    index += 6;
                }
            }
        }

        public unsafe void BuildSprite(ref Vector2 size, 
            ref Vector2 position, 
            float rotation, 
            ref Vector2 origin, 
            ref Vector2 uvsize, 
            ref Vector2 uvposition, 
            float z)
        {
            if (Buffer == null || NumFaces >= 1024)
            {
                method_1();
                method_2();
            }
            int index1 = NumFaces * 4;
            if (index1 + 4 > Vertices.Length)
                throw new Exception("Unable to build sprite, vertex array to small for all vertices.");
            bool rotated = rotation != 0f;
            float rotSin, rotCos;
            if (rotated)
            {
                rotSin = (float)Math.Sin(rotation);
                rotCos = (float)Math.Cos(rotation);
            }
            else
            {
                rotSin = 0.0f;
                rotCos = 1f;
            }
            fixed (SpriteVertex* pVerts = &Vertices[index1])
            fixed (Vector2* pCoords = Coords)
            {
                SpriteVertex* vertex = pVerts;
                Vector2* coord = pCoords;
                float centerX = 0.5f - origin.X;
                float centerY = 0.5f - origin.Y;
                for (int i = 0; i < 4; ++i)
                {
                    vertex->Coords.X = coord->X * uvsize.X + uvposition.X;
                    vertex->Coords.Y = coord->Y * uvsize.Y + uvposition.Y;
                    float num5 = coord->X - centerX;
                    float num6 = coord->Y - centerY;
                    if (rotated)
                    {
                        float num7 = (float)(num5 * (double)rotCos - num6 * (double)rotSin);
                        num6 = (float)(num5 * (double)rotSin + num6 * (double)rotCos);
                        num5 = num7;
                    }
                    vertex->Position.X = num5 * size.X + position.X;
                    vertex->Position.Y = num6 * size.Y + position.Y;
                    vertex->Position.Z = z;
                    vertex->PackedBinormalTangent.X = -rotSin;
                    vertex->PackedBinormalTangent.Y = rotCos;
                    vertex->PackedBinormalTangent.Z = rotSin;
                    ++vertex;
                    ++coord;
                }
            }
            ++NumFaces;
        }

        public void method_1()
        {
            if (Buffer == null || NumFaces < 1)
                return;
            Buffer.Create(Device, Vertices, Indices, NumFaces);
        }

        private void method_2()
        {
            Buffer = BuffFactory.New();
            Buffers.Add(Buffer);
            NumFaces = 0;
        }

        public void method_3()
        {
            foreach (MeshBuffer class69 in Buffers)
                BuffFactory.Free(class69);
            Buffer = null;
            Buffers.Clear();
            NumFaces = 0;
        }
    }
}
