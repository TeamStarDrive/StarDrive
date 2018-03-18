// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Processors.Forward.SasMaterialReader_Pro
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Effects.Forward;

namespace SynapseGaming.LightingSystem.Processors.Forward
{
  /// <summary />
  public class SasMaterialReader_Pro : ContentTypeReader<SasEffect>
  {
    /// <summary />
    protected override SasEffect Read(ContentReader input, SasEffect instance)
    {
      SasEffect bestSasEffectType = SasEffect.CreateBestSASEffectType(((IGraphicsDeviceService) input.ContentManager.ServiceProvider.GetService(typeof (IGraphicsDeviceService))).GraphicsDevice, input.ReadObject<Effect>());
      bestSasEffectType.MaterialName = input.ReadString();
      bestSasEffectType.MaterialFile = input.ReadString();
      bestSasEffectType.ProjectFile = input.ReadString();
      bestSasEffectType.Skinned = input.ReadBoolean();
      bestSasEffectType.EffectFile = input.ReadString();
      Dictionary<string, object> dictionary_1 = input.ReadObject<Dictionary<string, object>>();
      foreach (KeyValuePair<string, Texture> keyValuePair in input.ReadObject<Dictionary<string, Texture>>())
        bestSasEffectType.SetTexture(keyValuePair.Key, keyValuePair.Value);
      bestSasEffectType.method_1(dictionary_1);
      bestSasEffectType.method_0();
      BlockUtil.SkipBlock(input);
      if (input.ReadInt32() != 1234)
        throw new Exception("Error loading asset.");
      return bestSasEffectType;
    }
  }
}
