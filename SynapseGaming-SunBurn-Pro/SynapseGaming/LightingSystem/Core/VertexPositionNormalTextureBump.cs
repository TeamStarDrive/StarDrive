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
    public static readonly VertexElement[] VertexElements = new VertexElement[5]
    {
        new VertexElement(0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
        new VertexElement(0, 12, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
        new VertexElement(0, 24, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(0, 32, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Tangent, 0),
        new VertexElement(0, 44, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Binormal, 0)
    };
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
    public static int SizeInBytes => 56;

      /// <summary>
    /// Generates tangent space data (used for bump and specular mapping) from the provided vertex information.
    /// </summary>
    /// <param name="triangleIndices">Indices that describe a list of triangles to generate tangent space
    /// information for.  WARNING: this method requires triangle lists (not fans or strips).</param>
    /// <param name="vertices">Array of vertices.</param>
    public static void BuildTangentSpaceDataForTriangleList(
          short[] triangleIndices, VertexPositionNormalTextureBump[] vertices)
    {
      for (int i = 0; i < triangleIndices.Length; i += 3)
      {
        int in0 = triangleIndices[i];
        int in1 = triangleIndices[i + 1];
        int in2 = triangleIndices[i + 2];
        Vector2 uv0 = vertices[in0].TextureCoordinate;
        Vector2 uv1 = vertices[in1].TextureCoordinate;
        Vector2 uv2 = vertices[in2].TextureCoordinate;
        float xuv1 = uv1.X - uv0.X;
        float xuv2 = uv2.X - uv0.X;
        float yuv1 = uv1.Y - uv0.Y;
        float yuv2 = uv2.Y - uv0.Y;
        float num5 = (xuv1 * yuv2 - xuv2 *  yuv1);
        if (num5 != 0.0f)
        {
          float num6 = 1f / num5;
          Vector3 position1 = vertices[in0].Position;
          Vector3 position2 = vertices[in1].Position;
          Vector3 position3 = vertices[in2].Position;
          float num7 = position2.X - position1.X;
          float num8 = position3.X - position1.X;
          float num9 = position2.Y - position1.Y;
          float num10 = position3.Y - position1.Y;
          float num11 = position2.Z - position1.Z;
          float num12 = position3.Z - position1.Z;
          var vector3_1 = new Vector3((yuv2 * num7 - yuv1 * num8) * num6, (yuv2 * num9 - yuv1 * num10) * num6, (yuv2 * num11 - yuv1 * num12) * num6);
          var vector3_2 = new Vector3((xuv1 * num8 - xuv2 * num7) * num6, (xuv1 * num10 - xuv2 * num9) * num6, (xuv1 * num12 - xuv2 * num11) * num6);
          vertices[in0].Tangent += vector3_1;
          vertices[in1].Tangent += vector3_1;
          vertices[in2].Tangent += vector3_1;
          vertices[in0].Binormal += vector3_2;
          vertices[in1].Binormal += vector3_2;
          vertices[in2].Binormal += vector3_2;
        }
      }
      for (int index2 = 0; index2 < vertices.Length; ++index2)
      {
        vertices[index2].Tangent = Vector3.Normalize(vertices[index2].Tangent);
        vertices[index2].Binormal = Vector3.Normalize(vertices[index2].Binormal);
      }
    }
  }
}
