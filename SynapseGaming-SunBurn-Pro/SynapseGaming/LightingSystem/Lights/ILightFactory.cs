// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.ILightFactory
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

namespace SynapseGaming.LightingSystem.Lights
{
  /// <summary>
  /// Interface used by objects that create user defined lights. Primarily used on custom
  /// light rigs to allow creating user defined lights through the editor.
  /// </summary>
  public interface ILightFactory
  {
    /// <summary>
    /// Creates an instance of a user defined directional light.
    /// </summary>
    /// <returns></returns>
    ILight CreateDirectionalLight();

    /// <summary>Creates an instance of a user defined point light.</summary>
    /// <returns></returns>
    ILight CreatePointLight();

    /// <summary>Creates an instance of a user defined spot light.</summary>
    /// <returns></returns>
    ILight CreateSpotLight();

    /// <summary>Creates an instance of a user defined ambient light.</summary>
    /// <returns></returns>
    ILight CreateAmbientLight();
  }
}
