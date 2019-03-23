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
        private Matrix[] SkinBonesCache = new Matrix[1];
        private bool IsSkinned;
        private Matrix[] SkinBonesArray;
        private EffectParameter SkinBonesParam;

        /// <summary>
        /// Array of bone transforms for the skeleton's current pose. The matrix index is the
        /// same as the bone order used in the model or vertex buffer.
        /// </summary>
        public Matrix[] SkinBones
        {
            get => SkinBonesArray;
            set
            {
                if (value != null)
                {
                    _UpdatedByBatch = true;
                    EffectHelper.Update(value, ref SkinBonesArray, ref SkinBonesParam);
                }
                else
                {
                    if (!IsSkinned || SkinBonesParam == null)
                        return;

                    if (SkinBonesCache.Length < SkinBonesParam.Elements.Count)
                    {
                        SkinBonesCache = new Matrix[SkinBonesParam.Elements.Count];
                        for (int index = 0; index < SkinBonesCache.Length; ++index)
                            SkinBonesCache[index] = Matrix.Identity;
                    }

                    if (SkinBonesArray != SkinBonesCache)
                    {
                        _UpdatedByBatch = true;
                        SkinBonesArray = SkinBonesCache;
                        SkinBonesParam.SetArrayRange(0, SkinBonesArray.Length);
                        SkinBonesParam.SetValue(SkinBonesArray);
                    }
                }
            }
        }

        /// <summary>
        /// Determines if the effect is currently rendering skinned objects.
        /// </summary>
        public bool Skinned
        {
            get => IsSkinned;
            set
            {
                if (value == IsSkinned)
                    return;
                IsSkinned = value;
                SetTechnique();
                if (!IsSkinned || SkinBonesArray != null)
                    return;
                SkinBones = null;
            }
        }

        internal BaseSkinnedEffect(GraphicsDevice device, string effectName) : base(device, effectName)
        {
            SkinBonesParam = Parameters["_SkinBones"];
        }
    }
}
