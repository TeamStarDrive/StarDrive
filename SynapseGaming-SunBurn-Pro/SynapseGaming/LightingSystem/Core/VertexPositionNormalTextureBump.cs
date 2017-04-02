// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.VertexPositionNormalTextureBump
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Describes a SunBurn compatible vertex format structure that contains position, normal data, one set of texture coordinates,
  /// and tangent space information used in bump / specular mapping.
  /// </summary>
  public struct VertexPositionNormalTextureBump
  {
    /// <summary>An array of vertex elements describing this vertex.</summary>
    public static readonly VertexElement[] VertexElements = new VertexElement[5]{ new VertexElement((short) 0, (short) 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, (byte) 0), new VertexElement((short) 0, (short) 12, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, (byte) 0), new VertexElement((short) 0, (short) 24, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, (byte) 0), new VertexElement((short) 0, (short) 32, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Tangent, (byte) 0), new VertexElement((short) 0, (short) 44, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Binormal, (byte) 0) };
    /// <summary>The vertex position.</summary>
    public Vector3 Position;
    /// <summary>The vertex normal.</summary>
    public Vector3 Normal;
    /// <summary>The texture coordinates.</summary>
    public Vector2 TextureCoordinate;
    /// <summary>
    /// Tangent space tangent element used in bump / specular mapping.
    /// </summary>
    public Vector3 Tangent;
    /// <summary>
    /// Tangent space binormal element used in bump / specular mapping.
    /// </summary>
    public Vector3 Binormal;

    /// <summary>Gets the size of this structure.</summary>
    public static int SizeInBytes
    {
      get
      {
        return 56;
      }
    }

    /// <summary>
    /// Generates tangent space data (used for bump and specular mapping) from the provided vertex information.
    /// </summary>
    /// <param name="indices">Indices that describe a list of triangles to generate tangent space
    /// information for.  WARNING: this method requires triangle lists (not fans or strips).</param>
    /// <param name="vertices">Array of vertices.</param>
    public static void BuildTangentSpaceDataForTriangleList(short[] indices, VertexPositionNormalTextureBump[] vertices)
    {
      int index1 = 0;
      while (index1 < indices.Length)
      {
        int index2 = (int) indices[index1];
        int index3 = (int) indices[index1 + 1];
        int index4 = (int) indices[index1 + 2];
        Vector2 textureCoordinate1 = vertices[index2].TextureCoordinate;
        Vector2 textureCoordinate2 = vertices[index3].TextureCoordinate;
        Vector2 textureCoordinate3 = vertices[index4].TextureCoordinate;
        float num1 = textureCoordinate2.X - textureCoordinate1.X;
        float num2 = textureCoordinate3.X - textureCoordinate1.X;
        float num3 = textureCoordinate2.Y - textureCoordinate1.Y;
        float num4 = textureCoordinate3.Y - textureCoordinate1.Y;
        float num5 = (float) ((double) num1 * (double) num4 - (double) num2 * (double) num3);
        if ((double) num5 != 0.0)
        {
          float num6 = 1f / num5;
          Vector3 position1 = vertices[index2].Position;
          Vector3 position2 = vertices[index3].Position;
          Vector3 position3 = vertices[index4].Position;
          float num7 = position2.X - position1.X;
          float num8 = position3.X - position1.X;
          float num9 = position2.Y - position1.Y;
          float num10 = position3.Y - position1.Y;
          float num11 = position2.Z - position1.Z;
          float num12 = position3.Z - position1.Z;
          Vector3 vector3_1 = new Vector3((float) ((double) num4 * (double) num7 - (double) num3 * (double) num8) * num6, (float) ((double) num4 * (double) num9 - (double) num3 * (double) num10) * num6, (float) ((double) num4 * (double) num11 - (double) num3 * (double) num12) * num6);
          Vector3 vector3_2 = new Vector3((float) ((double) num1 * (double) num8 - (double) num2 * (double) num7) * num6, (float) ((double) num1 * (double) num10 - (double) num2 * (double) num9) * num6, (float) ((double) num1 * (double) num12 - (double) num2 * (double) num11) * num6);
          vertices[index2].Tangent += vector3_1;
          vertices[index3].Tangent += vector3_1;
          vertices[index4].Tangent += vector3_1;
          vertices[index2].Binormal += vector3_2;
          vertices[index3].Binormal += vector3_2;
          vertices[index4].Binormal += vector3_2;
        }
        index1 += 3;
      }
      for (int index2 = 0; index2 < vertices.Length; ++index2)
      {
        vertices[index2].Tangent = Vector3.Normalize(vertices[index2].Tangent);
        vertices[index2].Binormal = Vector3.Normalize(vertices[index2].Binormal);
      }
    }
  }
}
