// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.BaseSkinnedEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>Provides basic skinned animation rendering support.</summary>
  public abstract class BaseSkinnedEffect : BaseRenderableEffect, ISkinnedEffect
  {
    private Matrix[] matrix_10 = new Matrix[1];
    private bool bool_2;
    private Matrix[] matrix_9;
    private EffectParameter effectParameter_11;

    /// <summary>
    /// Array of bone transforms for the skeleton's current pose. The matrix index is the
    /// same as the bone order used in the model or vertex buffer.
    /// </summary>
    public Matrix[] SkinBones
    {
      get
      {
        return this.matrix_9;
      }
      set
      {
        if (value != null)
        {
          this._UpdatedByBatch = true;
          EffectHelper.smethod_1(value, ref this.matrix_9, ref this.effectParameter_11);
        }
        else
        {
          if (!this.bool_2 || this.effectParameter_11 == null)
            return;
          if (this.matrix_10.Length < this.effectParameter_11.Elements.Count)
          {
            this.matrix_10 = new Matrix[this.effectParameter_11.Elements.Count];
            for (int index = 0; index < this.matrix_10.Length; ++index)
              this.matrix_10[index] = Matrix.Identity;
          }
          if (this.matrix_9 == this.matrix_10)
            return;
          this._UpdatedByBatch = true;
          this.matrix_9 = this.matrix_10;
          this.effectParameter_11.SetArrayRange(0, this.matrix_9.Length);
          this.effectParameter_11.SetValue(this.matrix_9);
        }
      }
    }

    /// <summary>
    /// Determines if the effect is currently rendering skinned objects.
    /// </summary>
    public bool Skinned
    {
      get => this.bool_2;
        set
      {
        if (value == this.bool_2)
          return;
        this.bool_2 = value;
        this.SetTechnique();
        if (!this.bool_2 || this.matrix_9 != null)
          return;
        this.SkinBones = null;
      }
    }

    internal BaseSkinnedEffect(GraphicsDevice graphicsDevice_0, string string_0)
      : base(graphicsDevice_0, string_0)
    {
      this.effectParameter_11 = this.Parameters["_SkinBones"];
    }
  }
}
