// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.EffectBatchHelper
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using EmbeddedResources;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Helps maximize effect batching by collapsing identical effects in multiple models.
  /// </summary>
  public class EffectBatchHelper
  {
    private Dictionary<string, Effect> dictionary_0 = new Dictionary<string, Effect>(16);

    /// <summary>
    /// Maximize effect batching by collapsing identical effects with previously processed effects.
    /// </summary>
    /// <param name="effect"></param>
    /// <param name="disposeunused">Determines if the effects no longer used after collapsing are disposed.
    /// While this removes unused effects from the editor and frees up memory, it also leaves disposed
    /// effects in the XNA content manager (until Unload is called). Be careful when applying this option.</param>
    /// <returns></returns>
    public Effect CollapseEffect(Effect effect, bool disposeunused)
    {
      if (effect == null)
        return effect;
      string key = "";
      if (effect is Interface1)
        key = (effect as Interface1).MaterialFile;
      if (string.IsNullOrEmpty(key))
        return effect;
      if (this.dictionary_0.ContainsKey(key))
      {
        Effect effect1 = this.dictionary_0[key];
        if (disposeunused && effect != effect1 && !effect.IsDisposed)
          effect.Dispose();
        return effect1;
      }
      this.dictionary_0.Add(key, effect);
      return effect;
    }

    /// <summary>
    /// Maximize effect batching by collapsing identical effects in this and all previously processed models.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="disposeunused">Determines if the effects no longer used after collapsing are disposed.
    /// While this removes unused effects from the editor and frees up memory, it also leaves disposed
    /// effects in the XNA content manager (until Unload is called). Be careful when applying this option.</param>
    public void CollapseEffects(Model model, bool disposeunused)
    {
      for (int index1 = 0; index1 < model.Meshes.Count; ++index1)
      {
        ModelMesh mesh = model.Meshes[index1];
        for (int index2 = 0; index2 < mesh.MeshParts.Count; ++index2)
        {
          ModelMeshPart meshPart = mesh.MeshParts[index2];
          meshPart.Effect = this.CollapseEffect(meshPart.Effect, disposeunused);
        }
      }
    }

    /// <summary>Remove all processed effects.</summary>
    public void Clear()
    {
      this.dictionary_0.Clear();
    }
  }
}
