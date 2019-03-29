// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.VertexPositionNormalTextureBumpSkin
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Describes a SunBurn compatible vertex format structure that contains position, normal data, one set of texture coordinates,
  /// tangent space information used in bump / specular mapping, and skinning information.
  /// </summary>
  public struct VertexPositionNormalTextureBumpSkin
  {
    /// <summary>An array of vertex elements describing this vertex.</summary>
    public static readonly VertexElement[] VertexElements = new VertexElement[7]
    { 
        new VertexElement(0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
        new VertexElement(0, 12, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0), 
        new VertexElement(0, 24, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(0, 32, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Tangent, 0),
        new VertexElement(0, 44, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Binormal, 0),
        new VertexElement(0, 56, VertexElementFormat.Byte4, VertexElementMethod.Default, VertexElementUsage.BlendIndices, 0),
        new VertexElement(0, 60, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.BlendWeight, 0)
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
    /// <summary>
    /// Index used during skinning to lookup the meshToObject transform from a bone
    /// transform array given to an effect or render manager for rendering.
    /// </summary>
    public byte BoneIndex0;
    /// <summary>
    /// Index used during skinning to lookup the meshToObject transform from a bone
    /// transform array given to an effect or render manager for rendering.
    /// </summary>
    public byte BoneIndex1;
    /// <summary>
    /// Index used during skinning to lookup the meshToObject transform from a bone
    /// transform array given to an effect or render manager for rendering.
    /// </summary>
    public byte BoneIndex2;
    /// <summary>
    /// Index used during skinning to lookup the meshToObject transform from a bone
    /// transform array given to an effect or render manager for rendering.
    /// </summary>
    public byte BoneIndex3;
    /// <summary>
    /// Weights used to blend between the transforms assigned via bone indices 0 - 3.
    /// </summary>
    public Vector4 BoneWeights;

    /// <summary>Gets the size of this structure.</summary>
    public static int SizeInBytes => 76;

      /// <summary>
    /// Generates tangent space data (used for bump and specular mapping) from the provided vertex information.
    /// </summary>
    /// <param name="indices">Indices that describe a list of triangles to generate tangent space
    /// information for.  WARNING: this method requires triangle lists (not fans or strips).</param>
    /// <param name="vertices">Array of vertices.</param>
    public static void BuildTangentSpaceDataForTriangleList(short[] indices, VertexPositionNormalTextureBumpSkin[] vertices)
    {
      int index1 = 0;
      while (index1 < indices.Length)
      {
        int index2 = indices[index1];
        int index3 = indices[index1 + 1];
        int index4 = indices[index1 + 2];
        Vector2 textureCoordinate1 = vertices[index2].TextureCoordinate;
        Vector2 textureCoordinate2 = vertices[index3].TextureCoordinate;
        Vector2 textureCoordinate3 = vertices[index4].TextureCoordinate;
        float num1 = textureCoordinate2.X - textureCoordinate1.X;
        float num2 = textureCoordinate3.X - textureCoordinate1.X;
        float num3 = textureCoordinate2.Y - textureCoordinate1.Y;
        float num4 = textureCoordinate3.Y - textureCoordinate1.Y;
        float num5 = (float) (num1 * (double) num4 - num2 * (double) num3);
        if (num5 != 0.0)
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
          Vector3 vector3_1 = new Vector3((float) (num4 * (double) num7 - num3 * (double) num8) * num6, (float) (num4 * (double) num9 - num3 * (double) num10) * num6, (float) (num4 * (double) num11 - num3 * (double) num12) * num6);
          Vector3 vector3_2 = new Vector3((float) (num1 * (double) num8 - num2 * (double) num7) * num6, (float) (num1 * (double) num10 - num2 * (double) num9) * num6, (float) (num1 * (double) num12 - num2 * (double) num11) * num6);
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
