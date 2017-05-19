// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.LightingSystemPreferences
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;
using ns3;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Provides user and hardware specific preferences to the Lighting System.
  /// </summary>
  [Serializable]
  public class LightingSystemPreferences : IPreferences, ISerializable, ILightingSystemPreferences
  {
    private SamplingPreference samplingPreference_0 = SamplingPreference.Trilinear;
    private int int_0 = 4;
    private DetailPreference detailPreference_1 = DetailPreference.Medium;
    private float float_0 = 1f;
    private DetailPreference detailPreference_0;
    private DetailPreference detailPreference_2;
    private DetailPreference detailPreference_3;
    private static SerializeTypeDictionary serializeTypeDictionary_0;

    /// <summary>
    /// Sets the user preferred balance of texture sampling quality and performance.
    /// </summary>
    public SamplingPreference TextureSampling
    {
      get => this.samplingPreference_0;
        set => this.samplingPreference_0 = value;
    }

    /// <summary>
    /// Sets the user preferred balance of texture resolution and performance.
    /// </summary>
    public DetailPreference TextureQuality
    {
      get => this.detailPreference_0;
        set => this.detailPreference_0 = value;
    }

    /// <summary>
    /// Sets the maximum anisotropy level when TextureSampling is set to Anisotropic.
    /// </summary>
    public int MaxAnisotropy
    {
      get => this.int_0;
        set => this.int_0 = value;
    }

    /// <summary>
    /// Sets the user preferred balance of shadow filtering quality and performance.
    /// </summary>
    public DetailPreference ShadowDetail
    {
      get => this.detailPreference_1;
        set => this.detailPreference_1 = value;
    }

    /// <summary>
    /// Sets the user preferred balance of shadow resolution and performance.
    /// </summary>
    public float ShadowQuality
    {
      get => this.float_0;
        set => this.float_0 = value;
    }

    /// <summary>
    /// Sets the user preferred balance of LightingEffect detail and performance.
    /// </summary>
    public DetailPreference EffectDetail
    {
      get => this.detailPreference_2;
        set => this.detailPreference_2 = value;
    }

    /// <summary>
    /// Sets the user preferred balance of post-processing effect detail and performance.
    /// </summary>
    public DetailPreference PostProcessingDetail
    {
      get => this.detailPreference_3;
        set => this.detailPreference_3 = value;
    }

    /// <summary>
    /// Used to support serializing user defined preferences. Register any additional
    /// classes and their xml element names to support persisting custom preference objects.
    /// </summary>
    public static SerializeTypeDictionary SerializeTypeDictionary
    {
      get
      {
        if (serializeTypeDictionary_0 == null)
        {
          serializeTypeDictionary_0 = new SerializeTypeDictionary();
          serializeTypeDictionary_0.RegisterType("Preferences", typeof (LightingSystemPreferences));
          serializeTypeDictionary_0.RegisterType("Sampling", typeof (SamplingPreference));
          serializeTypeDictionary_0.RegisterType("Detail", typeof (DetailPreference));
        }
        return serializeTypeDictionary_0;
      }
    }

    /// <summary>Creates a new LightingSystemPreferences object.</summary>
    public LightingSystemPreferences()
    {
    }

    /// <summary />
    protected LightingSystemPreferences(SerializationInfo serializationInfo_0, StreamingContext streamingContext_0)
    {
      foreach (SerializationEntry serializationEntry in serializationInfo_0)
      {
        switch (serializationEntry.Name)
        {
          case "TextureSampling":
            Class28.smethod_1(ref this.samplingPreference_0, serializationInfo_0, "TextureSampling");
            continue;
          case "TextureQuality":
            Class28.smethod_1(ref this.detailPreference_0, serializationInfo_0, "TextureQuality");
            continue;
          case "MaxAnisotropy":
            Class28.smethod_0(ref this.int_0, serializationInfo_0, "MaxAnisotropy");
            continue;
          case "ShadowDetail":
            Class28.smethod_1(ref this.detailPreference_1, serializationInfo_0, "ShadowDetail");
            continue;
          case "ShadowQuality":
            Class28.smethod_0(ref this.float_0, serializationInfo_0, "ShadowQuality");
            continue;
          case "EffectDetail":
            Class28.smethod_1(ref this.detailPreference_2, serializationInfo_0, "EffectDetail");
            continue;
          case "PostProcessingDetail":
            Class28.smethod_1(ref this.detailPreference_3, serializationInfo_0, "PostProcessingDetail");
            continue;
          default:
            continue;
        }
      }
    }

    /// <summary>
    /// Loads preferences from file (available on Windows only – Xbox 360 implementations
    /// using LightingSystemPreferences should set preferences via code as all target
    /// hardware is the same).
    /// </summary>
    /// <param name="filename">Path and name of file.</param>
    public void LoadFromFile(string filename)
    {
      FileStream fileStream = File.OpenRead(filename);
      object object_0 = new Class29(SerializeTypeDictionary).Deserialize(fileStream);
      if (object_0 != null)
        Class12.smethod_1(object_0, this);
      fileStream.Flush();
      fileStream.Close();
      fileStream.Dispose();
    }

    /// <summary>
    /// Saves preferences to file (available on Windows only – Xbox 360 implementations
    /// using LightingSystemPreferences should set preferences via code as all target
    /// hardware is the same).
    /// </summary>
    /// <param name="filename">Path and name of file.</param>
    public void SaveToFile(string filename)
    {
      FileStream fileStream = File.Create(filename);
      new Class29(SerializeTypeDictionary).Serialize(fileStream, this);
      fileStream.Flush();
      fileStream.Close();
      fileStream.Dispose();
    }

    /// <summary />
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("TextureSampling", this.samplingPreference_0);
      info.AddValue("TextureQuality", this.detailPreference_0);
      info.AddValue("MaxAnisotropy", this.int_0);
      info.AddValue("ShadowDetail", this.detailPreference_1);
      info.AddValue("ShadowQuality", this.float_0);
      info.AddValue("EffectDetail", this.detailPreference_2);
      info.AddValue("PostProcessingDetail", this.detailPreference_3);
    }
  }
}
