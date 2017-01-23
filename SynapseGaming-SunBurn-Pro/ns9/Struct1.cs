// Decompiled with JetBrains decompiler
// Type: ns9.Struct1
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ns9
{
  internal struct Struct1
  {
    public static readonly VertexElement[] vertexElement_0 = new VertexElement[5]{ new VertexElement((short) 0, (short) 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, (byte) 0), new VertexElement((short) 0, (short) 12, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, (byte) 0), new VertexElement((short) 0, (short) 24, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, (byte) 0), new VertexElement((short) 0, (short) 32, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.Binormal, (byte) 0), new VertexElement((short) 0, (short) 36, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.Tangent, (byte) 0) };
    public Vector3 vector3_0;
    public Vector3 vector3_1;
    public Vector2 vector2_0;
    public Vector3 vector3_2;

    public static int SizeInBytes
    {
      get
      {
        return 44;
      }
    }
  }
}
