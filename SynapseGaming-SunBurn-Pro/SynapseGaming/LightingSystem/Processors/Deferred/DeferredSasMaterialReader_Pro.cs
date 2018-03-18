// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Processors.Deferred.DeferredSasMaterialReader_Pro
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Effects.Deferred;

namespace SynapseGaming.LightingSystem.Processors.Deferred
{
  /// <summary />
  public class DeferredSasMaterialReader_Pro : ContentTypeReader<DeferredSasEffect>
  {
    /// <summary />
    protected override DeferredSasEffect Read(ContentReader input, DeferredSasEffect instance)
    {
      DeferredSasEffect deferredSasEffect = new DeferredSasEffect(((IGraphicsDeviceService) input.ContentManager.ServiceProvider.GetService(typeof (IGraphicsDeviceService))).GraphicsDevice, input.ReadObject<Effect>());
      deferredSasEffect.MaterialName = input.ReadString();
      deferredSasEffect.MaterialFile = input.ReadString();
      deferredSasEffect.ProjectFile = input.ReadString();
      deferredSasEffect.Skinned = input.ReadBoolean();
      deferredSasEffect.EffectFile = input.ReadString();
      Dictionary<string, object> dictionary_1 = input.ReadObject<Dictionary<string, object>>();
      foreach (KeyValuePair<string, Texture> keyValuePair in input.ReadObject<Dictionary<string, Texture>>())
        deferredSasEffect.SetTexture(keyValuePair.Key, keyValuePair.Value);
      deferredSasEffect.method_1(dictionary_1);
      deferredSasEffect.method_0();
      BlockUtil.SkipBlock(input);
      if (input.ReadInt32() != 1234)
        throw new Exception("Error loading asset.");
      return deferredSasEffect;
    }
  }
}
