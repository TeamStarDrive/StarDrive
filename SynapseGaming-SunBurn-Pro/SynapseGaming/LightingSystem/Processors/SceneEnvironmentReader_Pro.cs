// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Processors.SceneEnvironmentReader_Pro
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Content;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Processors
{
  /// <summary />
  public class SceneEnvironmentReader_Pro : ContentTypeReader<SceneEnvironment>
  {
    /// <summary />
    protected override SceneEnvironment Read(ContentReader input, SceneEnvironment instance)
    {
      string string_3 = input.ReadString();
      string str1 = input.ReadString();
      string str2 = input.ReadString();
      SceneEnvironment sceneEnvironment = SceneEnvironment.smethod_0(input.ReadString());
      sceneEnvironment.method_0(string_3);
      sceneEnvironment.SceneEnvironmentFile = str1;
      sceneEnvironment.ProjectFile = str2;
      BlockUtil.SkipBlock(input);
      return sceneEnvironment;
    }
  }
}
