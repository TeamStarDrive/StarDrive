// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Processors.Forward.XSIMaterialReader_Pro
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Effects.Forward;

namespace SynapseGaming.LightingSystem.Processors.Forward
{
  /// <summary />
  public class XSIMaterialReader_Pro : ContentTypeReader<XSIEffect>
  {
    /// <summary />
    protected override XSIEffect Read(ContentReader input, XSIEffect instance)
    {
      IGraphicsDeviceService service = (IGraphicsDeviceService) input.ContentManager.ServiceProvider.GetService(typeof (IGraphicsDeviceService));
      bool flag = input.ReadBoolean();
      Effect effect = input.ReadObject<Effect>();
      XSIEffect xsiEffect = new XSIEffect(service.GraphicsDevice, effect);
      xsiEffect.Skinned = flag;
      BlockUtil.SkipBlock(input);
      return xsiEffect;
    }
  }
}
