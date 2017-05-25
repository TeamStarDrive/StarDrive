// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.SpriteContainer
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns9;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Specialized 2D scene object used to store and render sprites using
  /// SunBurn's forward and deferred rendering systems and effects.
  /// 
  /// Create an instance using SpriteManager.CreateSpriteContainer().
  /// </summary>
  public class SpriteContainer : SceneObject
  {
    private Vector2 vector2_0 = Vector2.One;
    private Vector2 vector2_1 = Vector2.Zero;
    private int int_1 = -1;
    private Dictionary<int, Class70> dictionary_0 = new Dictionary<int, Class70>(16);
    private bool bool_3;
    private GraphicsDevice graphicsDevice_0;
    private TrackingPool<RenderableMesh> class21_0;
    private DisposablePool<MeshBuffer> class22_0;
    private Class70 class70_0;

    internal SpriteContainer(GraphicsDevice graphicsDevice_1, TrackingPool<RenderableMesh> class21_1, DisposablePool<MeshBuffer> class22_1)
    {
      this.graphicsDevice_0 = graphicsDevice_1;
      this.class21_0 = class21_1;
      this.class22_0 = class22_1;
    }

    /// <summary>
    /// Prepares the container for new sprites, also clears all existing sprites from the container.
    /// </summary>
    public void Begin()
    {
      if (this.bool_3)
        throw new Exception("Begin already called on this object, make sure all Begin calls have an accompanying End call.");
      this.bool_3 = true;
      this.graphicsDevice_0.Indices = null;
      foreach (KeyValuePair<int, Class70> keyValuePair in this.dictionary_0)
        keyValuePair.Value.method_3();
      while (this.RenderableMeshes.Count > 0)
      {
        RenderableMesh renderableMesh = this.RenderableMeshes[0];
        this.class21_0.Free(renderableMesh);
        this.Remove(renderableMesh);
      }
    }

    /// <summary>
    /// Adds a sprite to this container. Can only be used between calls to Begin() and End().
    /// </summary>
    /// <param name="effect">Effect applied to the sprite during rendering.</param>
    /// <param name="size">Size of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="position">Position of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="layerdepth">Controls both the z-sorting and the height between
    /// sprites, which is critical for proper shadowing. If shadows are too
    /// disconnected form the caster try reducing the depth between the shadow
    /// caster and receiver.</param>
    public void Add(Effect effect, Vector2 size, Vector2 position, float layerdepth)
    {
      this.Add(effect, effect.GetHashCode(), ref size, ref position, 0.0f, ref this.vector2_1, ref this.vector2_0, ref this.vector2_1, layerdepth);
    }

    /// <summary>
    /// Adds a sprite to this container. Can only be used between calls to Begin() and End().
    /// </summary>
    /// <param name="effect">Effect applied to the sprite during rendering.</param>
    /// <param name="size">Size of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="position">Position of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="rotation">Rotation of the sprite in radians.</param>
    /// <param name="layerdepth">Controls both the z-sorting and the height between
    /// sprites, which is critical for proper shadowing. If shadows are too
    /// disconnected form the caster try reducing the depth between the shadow
    /// caster and receiver.</param>
    public void Add(Effect effect, Vector2 size, Vector2 position, float rotation, float layerdepth)
    {
      this.Add(effect, effect.GetHashCode(), ref size, ref position, rotation, ref this.vector2_1, ref this.vector2_0, ref this.vector2_1, layerdepth);
    }

    /// <summary>
    /// Adds a sprite to this container. Can only be used between calls to Begin() and End().
    /// </summary>
    /// <param name="effect">Effect applied to the sprite during rendering.</param>
    /// <param name="size">Size of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="position">Position of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="rotation">Rotation of the sprite in radians.</param>
    /// <param name="origin">Indicates the sprite origin or pivot point (offset from
    /// the sprite center).</param>
    /// <param name="layerdepth">Controls both the z-sorting and the height between
    /// sprites, which is critical for proper shadowing. If shadows are too
    /// disconnected form the caster try reducing the depth between the shadow
    /// caster and receiver.</param>
    public void Add(Effect effect, Vector2 size, Vector2 position, float rotation, Vector2 origin, float layerdepth)
    {
      this.Add(effect, effect.GetHashCode(), ref size, ref position, rotation, ref origin, ref this.vector2_0, ref this.vector2_1, layerdepth);
    }

    /// <summary>
    /// Adds a sprite to this container. Can only be used between calls to Begin() and End().
    /// </summary>
    /// <param name="effect">Effect applied to the sprite during rendering.</param>
    /// <param name="size">Size of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="position">Position of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="uvsize">Indicates the number of times a material will tile
    /// across the sprite.</param>
    /// <param name="uvposition">Indicates the uv offset applied to a material
    /// on the sprite (in uv coordinates, where a single material tile ranges from 0.0f - 1.0f).</param>
    /// <param name="layerdepth">Controls both the z-sorting and the height between
    /// sprites, which is critical for proper shadowing. If shadows are too
    /// disconnected form the caster try reducing the depth between the shadow
    /// caster and receiver.</param>
    public void Add(Effect effect, Vector2 size, Vector2 position, Vector2 uvsize, Vector2 uvposition, float layerdepth)
    {
      this.Add(effect, effect.GetHashCode(), ref size, ref position, 0.0f, ref this.vector2_1, ref uvsize, ref uvposition, layerdepth);
    }

    /// <summary>
    /// Adds a sprite to this container. Can only be used between calls to Begin() and End().
    /// </summary>
    /// <param name="effect">Effect applied to the sprite during rendering.</param>
    /// <param name="size">Size of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="position">Position of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="rotation">Rotation of the sprite in radians.</param>
    /// <param name="uvsize">Indicates the number of times a material will tile
    /// across the sprite.</param>
    /// <param name="uvposition">Indicates the uv offset applied to a material
    /// on the sprite (in uv coordinates, where a single material tile ranges from 0.0f - 1.0f).</param>
    /// <param name="layerdepth">Controls both the z-sorting and the height between
    /// sprites, which is critical for proper shadowing. If shadows are too
    /// disconnected form the caster try reducing the depth between the shadow
    /// caster and receiver.</param>
    public void Add(Effect effect, Vector2 size, Vector2 position, float rotation, Vector2 uvsize, Vector2 uvposition, float layerdepth)
    {
      this.Add(effect, effect.GetHashCode(), ref size, ref position, rotation, ref this.vector2_1, ref uvsize, ref uvposition, layerdepth);
    }

    /// <summary>
    /// Adds a sprite to this container. Can only be used between calls to Begin() and End().
    /// </summary>
    /// <param name="effect">Effect applied to the sprite during rendering.</param>
    /// <param name="size">Size of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="position">Position of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="rotation">Rotation of the sprite in radians.</param>
    /// <param name="origin">Indicates the sprite origin or pivot point (offset from
    /// the sprite center).</param>
    /// <param name="uvsize">Indicates the number of times a material will tile
    /// across the sprite.</param>
    /// <param name="uvposition">Indicates the uv offset applied to a material
    /// on the sprite (in uv coordinates, where a single material tile ranges from 0.0f - 1.0f).</param>
    /// <param name="layerdepth">Controls both the z-sorting and the height between
    /// sprites, which is critical for proper shadowing. If shadows are too
    /// disconnected form the caster try reducing the depth between the shadow
    /// caster and receiver.</param>
    public void Add(Effect effect, Vector2 size, Vector2 position, float rotation, Vector2 origin, Vector2 uvsize, Vector2 uvposition, float layerdepth)
    {
      this.Add(effect, effect.GetHashCode(), ref size, ref position, rotation, ref origin, ref uvsize, ref uvposition, layerdepth);
    }

    /// <summary>
    /// Adds a sprite to this container. Can only be used between calls to Begin() and End().
    /// </summary>
    /// <param name="effect">Effect applied to the sprite during rendering.</param>
    /// <param name="effecthashcode">Unique hashcode of the effect.</param>
    /// <param name="size">Size of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="position">Position of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="rotation">Rotation of the sprite in radians.</param>
    /// <param name="origin">Indicates the sprite origin or pivot point (offset from
    /// the sprite center).</param>
    /// <param name="uvsize">Indicates the number of times a material will tile
    /// across the sprite.</param>
    /// <param name="uvposition">Indicates the uv offset applied to a material
    /// on the sprite (in uv coordinates, where a single material tile ranges from 0.0f - 1.0f).</param>
    /// <param name="layerdepth">Controls both the z-sorting and the height between
    /// sprites, which is critical for proper shadowing. If shadows are too
    /// disconnected form the caster try reducing the depth between the shadow
    /// caster and receiver.</param>
    public void Add(Effect effect, int effecthashcode, Vector2 size, Vector2 position, float rotation, Vector2 origin, Vector2 uvsize, Vector2 uvposition, float layerdepth)
    {
      this.Add(effect, effecthashcode, ref size, ref position, rotation, ref origin, ref uvsize, ref uvposition, layerdepth);
    }

    /// <summary>
    /// Adds a sprite to this container. Can only be used between calls to Begin() and End().
    /// </summary>
    /// <param name="effect">Effect applied to the sprite during rendering.</param>
    /// <param name="size">Size of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="position">Position of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="rotation">Rotation of the sprite in radians.</param>
    /// <param name="origin">Indicates the sprite origin or pivot point (offset from
    /// the sprite center).</param>
    /// <param name="uvsize">Indicates the number of times a material will tile
    /// across the sprite.</param>
    /// <param name="uvposition">Indicates the uv offset applied to a material
    /// on the sprite (in uv coordinates, where a single material tile ranges from 0.0f - 1.0f).</param>
    /// <param name="layerdepth">Controls both the z-sorting and the height between
    /// sprites, which is critical for proper shadowing. If shadows are too
    /// disconnected form the caster try reducing the depth between the shadow
    /// caster and receiver.</param>
    public void Add(Effect effect, ref Vector2 size, ref Vector2 position, float rotation, ref Vector2 origin, ref Vector2 uvsize, ref Vector2 uvposition, float layerdepth)
    {
      this.Add(effect, effect.GetHashCode(), ref size, ref position, rotation, ref origin, ref uvsize, ref uvposition, layerdepth);
    }

    /// <summary>
    /// Adds a sprite to this container. Can only be used between calls to Begin() and End().
    /// </summary>
    /// <param name="effect">Effect applied to the sprite during rendering.</param>
    /// <param name="effecthashcode">Unique hashcode of the effect.</param>
    /// <param name="size">Size of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="position">Position of the sprite (in world-space if the container
    /// uses an identity world transform, otherwise in object-space)</param>
    /// <param name="rotation">Rotation of the sprite in radians.</param>
    /// <param name="origin">Indicates the sprite origin or pivot point (offset from
    /// the sprite center).</param>
    /// <param name="uvsize">Indicates the number of times a material will tile
    /// across the sprite.</param>
    /// <param name="uvposition">Indicates the uv offset applied to a material
    /// on the sprite (in uv coordinates, where a single material tile ranges from 0.0f - 1.0f).</param>
    /// <param name="layerdepth">Controls both the z-sorting and the height between
    /// sprites, which is critical for proper shadowing. If shadows are too
    /// disconnected form the caster try reducing the depth between the shadow
    /// caster and receiver.</param>
    public void Add(Effect effect, int effecthashcode, ref Vector2 size, ref Vector2 position, float rotation, ref Vector2 origin, ref Vector2 uvsize, ref Vector2 uvposition, float layerdepth)
    {
      if (!this.bool_3)
        throw new Exception("Begin must be called before adding sprites to the container.");
      Class70 class70;
      if (this.int_1 == effecthashcode && this.class70_0 != null)
      {
        class70 = this.class70_0;
      }
      else
      {
        if (!this.dictionary_0.TryGetValue(effecthashcode, out class70))
        {
          class70 = new Class70(this.graphicsDevice_0, this.class22_0, effect);
          this.dictionary_0.Add(effecthashcode, class70);
        }
        this.int_1 = effecthashcode;
        this.class70_0 = class70;
      }
      class70.BuildSprite(ref size, ref position, rotation, ref origin, ref uvsize, ref uvposition, layerdepth);
    }

    /// <summary>
    /// Finishes all sprite operations until the next call to Begin().
    /// </summary>
    public void End()
    {
      if (!this.bool_3)
        throw new Exception("Begin must be called before calling End.");
      this.bool_3 = false;
      foreach (KeyValuePair<int, Class70> keyValuePair in this.dictionary_0)
      {
        Class70 class70 = keyValuePair.Value;
        Effect effect = class70.Effect;
        class70.method_1();
        foreach (MeshBuffer buffer in class70.Buffers)
        {
          RenderableMesh mesh = this.class21_0.New();
          mesh.Build(this, effect, Matrix.Identity, BoundingSphere.CreateFromBoundingBox(buffer.ObjectBoundingBox), buffer.IndexBuffer, buffer.VertexBuffer, buffer.VertexDeclaration, 0, PrimitiveType.TriangleList, buffer.VertexCount / 4 * 2, 0, buffer.VertexCount, 0, Vertex.SizeInBytes);
          this.Add(mesh);
        }
      }
    }
  }
}
