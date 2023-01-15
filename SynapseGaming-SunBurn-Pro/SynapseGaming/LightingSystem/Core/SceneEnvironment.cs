// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.SceneEnvironment
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using Microsoft.Xna.Framework;
using ns3;
using SynapseGaming.LightingSystem.Editor;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Provides scene environmental information to the lighting system.
  /// </summary>
  [Serializable]
  public sealed class SceneEnvironment : IDisposable, INamedObject, IEditorObject, IProjectFile, ISerializable, ISceneEnvironment
  {
    private float float_0 = 300f;
    private bool bool_0 = true;
    private float float_1 = 200f;
    private float float_2 = 300f;
    private Vector3 vector3_0 = Vector3.One * 0.75f;
    private float float_3 = 300f;
    private float float_4 = 300f;
    private float float_5 = 300f;
    private float float_6 = 3f;
    private float float_7 = 0.9f;
    private float float_8 = 1f;
    private float float_9 = 0.5f;
    private float float_10 = 100f;
    private float float_11 = 0.01f;
    private string string_0 = "";
    private string string_1 = "";
    private string string_2 = "";
    private bool bool_1;
    private static SerializeTypeDictionary serializeTypeDictionary_0;

    /// <summary>Maximum world space distance objects are visible.</summary>
    public float VisibleDistance
    {
      get => this.float_0;
        set => this.float_0 = value;
    }

    /// <summary>Enables scene fog.</summary>
    public bool FogEnabled
    {
      get => this.bool_0;
        set => this.bool_0 = value;
    }

    /// <summary>World space distance that fog begins.</summary>
    public float FogStartDistance
    {
      get => this.float_1;
        set => this.float_1 = value;
    }

    /// <summary>World space distance that fog fully obscures objects.</summary>
    public float FogEndDistance
    {
      get => this.float_2;
        set => this.float_2 = value;
    }

    /// <summary>Color applied to scene fog.</summary>
    public Vector3 FogColor
    {
      get => this.vector3_0;
        set => this.vector3_0 = value;
    }

    /// <summary>
    /// World space distance that directional shadows begin fading.
    /// </summary>
    public float ShadowFadeStartDistance
    {
      get => this.float_3;
        set => this.float_3 = value;
    }

    /// <summary>
    /// World space distance that directional shadows completely disappear.
    /// </summary>
    public float ShadowFadeEndDistance
    {
      get => this.float_4;
        set => this.float_4 = value;
    }

    /// <summary>
    /// World space distance used to include shadow casters. This allows including shadows
    /// from objects further away than the shadow fade area, for instance shadows from
    /// distant mountains.
    /// </summary>
    public float ShadowCasterDistance
    {
      get => this.float_5;
        set => this.float_5 = value;
    }

    /// <summary>Strength of bloom applied to the scene.</summary>
    public float BloomAmount
    {
      get => this.float_6;
        set => this.float_6 = value;
    }

    /// <summary>Minimum pixel intensity required for bloom to occur.</summary>
    public float BloomThreshold
    {
      get => this.float_7;
        set => this.float_7 = value;
    }

    /// <summary>Intensity of the scene exposure.</summary>
    public float ExposureAmount
    {
      get => this.float_8;
        set => this.float_8 = value;
    }

    /// <summary>
    /// Time required to fully adjust High Dynamic Range to lighting changes.
    /// </summary>
    public float DynamicRangeTransitionTime
    {
      get => this.float_9;
        set => this.float_9 = value;
    }

    /// <summary>
    /// Maximum intensity increase allowed for High Dynamic Range. Limits intensity
    /// increases, which sets the darkness-level where the scene will remain dark.
    /// </summary>
    public float DynamicRangeTransitionMaxScale
    {
      get => this.float_10;
        set => this.float_10 = value;
    }

    /// <summary>
    /// Maximum intensity decrease allowed for High Dynamic Range. Limits intensity
    /// decreases, which sets the brightness-level where the scene will remain overly bright.
    /// </summary>
    public float DynamicRangeTransitionMinScale
    {
      get => this.float_11;
        set => this.float_11 = value;
    }

    /// <summary>The object's current name.</summary>
    public string Name
    {
      get
      {
        return this.string_0;
      }
      set
      {
      }
    }

    /// <summary>
    /// Notifies the editor that this object is partially controlled via code. The editor
    /// will display information to the user indicating some property values are
    /// overridden in code and changes may not take effect.
    /// </summary>
    public bool AffectedInCode { get; set; }

    internal string SceneEnvironmentFile
    {
      get => this.string_1;
        set => this.string_1 = value;
    }

    internal string ProjectFile
    {
      get => this.string_2;
        set => this.string_2 = value;
    }

    string IProjectFile.ProjectFile => this.string_2;

      /// <summary>
    /// Used to support serializing user defined scene environment objects. Register any additional
    /// classes and their xml element names to support persisting custom scene environment objects.
    /// </summary>
    public static SerializeTypeDictionary SerializeTypeDictionary
    {
      get
      {
        if (serializeTypeDictionary_0 == null)
        {
          serializeTypeDictionary_0 = new SerializeTypeDictionary();
          serializeTypeDictionary_0.RegisterType("SceneEnvironment", typeof (SceneEnvironment));
          serializeTypeDictionary_0.RegisterType("Vector3", typeof (Vector3));
        }
        return serializeTypeDictionary_0;
      }
    }

    /// <summary>Creates a new SceneEnvironment instance.</summary>
    public SceneEnvironment()
    {
      LightingSystemEditor.OnCreateResource(this);
    }

    SceneEnvironment(SerializationInfo serializationInfo_0, StreamingContext streamingContext_0)
    {
      foreach (SerializationEntry serializationEntry in serializationInfo_0)
      {
        switch (serializationEntry.Name)
        {
          case "VisibleDistance":
            this.VisibleDistance = (float) serializationInfo_0.GetValue("VisibleDistance", typeof (float));
            continue;
          case "FogEnabled":
            this.FogEnabled = (bool) serializationInfo_0.GetValue("FogEnabled", typeof (bool));
            continue;
          case "FogColor":
            this.FogColor = (Vector3) serializationInfo_0.GetValue("FogColor", typeof (Vector3));
            continue;
          case "FogStartDistance":
            this.FogStartDistance = (float) serializationInfo_0.GetValue("FogStartDistance", typeof (float));
            continue;
          case "FogEndDistance":
            this.FogEndDistance = (float) serializationInfo_0.GetValue("FogEndDistance", typeof (float));
            continue;
          case "ShadowFadeStartDistance":
            this.ShadowFadeStartDistance = (float) serializationInfo_0.GetValue("ShadowFadeStartDistance", typeof (float));
            continue;
          case "ShadowFadeEndDistance":
            this.ShadowFadeEndDistance = (float) serializationInfo_0.GetValue("ShadowFadeEndDistance", typeof (float));
            continue;
          case "ShadowCasterDistance":
            this.ShadowCasterDistance = (float) serializationInfo_0.GetValue("ShadowCasterDistance", typeof (float));
            continue;
          case "BloomAmount":
            this.BloomAmount = (float) serializationInfo_0.GetValue("BloomAmount", typeof (float));
            continue;
          case "BloomThreshold":
            this.BloomThreshold = (float) serializationInfo_0.GetValue("BloomThreshold", typeof (float));
            continue;
          case "ExposureAmount":
            this.ExposureAmount = (float) serializationInfo_0.GetValue("ExposureAmount", typeof (float));
            continue;
          case "DynamicRangeTransitionMaxScale":
            this.DynamicRangeTransitionMaxScale = (float) serializationInfo_0.GetValue("DynamicRangeTransitionMaxScale", typeof (float));
            continue;
          case "DynamicRangeTransitionMinScale":
            this.DynamicRangeTransitionMinScale = (float) serializationInfo_0.GetValue("DynamicRangeTransitionMinScale", typeof (float));
            continue;
          case "DynamicRangeTransitionTime":
            this.DynamicRangeTransitionTime = (float) serializationInfo_0.GetValue("DynamicRangeTransitionTime", typeof (float));
            continue;
          default:
            continue;
        }
      }
    }

    internal void method_0(string string_3)
    {
      this.string_0 = string_3;
    }

    /// <summary>Releases resources allocated by this object.</summary>
    public void Dispose()
    {
      if (this.bool_1)
        return;
      this.bool_1 = true;
      LightingSystemEditor.OnDisposeResource(this);
    }

    /// <summary>Saves the object back to its originating file.</summary>
    public void Save()
    {
      this.method_1(this.string_1);
    }

    internal static SceneEnvironment smethod_0(string string_3)
    {
      MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(string_3));
      SceneEnvironment sceneEnvironment = (SceneEnvironment) new Class29(SerializeTypeDictionary).Deserialize(memoryStream) ?? new SceneEnvironment();
      memoryStream.Close();
      memoryStream.Dispose();
      return sceneEnvironment;
    }

    internal void method_1(string string_3)
    {
      if (!File.Exists(string_3))
        return;
      FileStream fileStream = File.Create(string_3);
      new Class29(SerializeTypeDictionary).Serialize(fileStream, this);
      fileStream.Flush();
      fileStream.Close();
      fileStream.Dispose();
    }

    /// <summary />
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("VisibleDistance", this.VisibleDistance);
      info.AddValue("FogEnabled", this.FogEnabled);
      info.AddValue("FogColor", this.FogColor);
      info.AddValue("FogStartDistance", this.FogStartDistance);
      info.AddValue("FogEndDistance", this.FogEndDistance);
      info.AddValue("ShadowFadeStartDistance", this.ShadowFadeStartDistance);
      info.AddValue("ShadowFadeEndDistance", this.ShadowFadeEndDistance);
      info.AddValue("ShadowCasterDistance", this.ShadowCasterDistance);
      info.AddValue("BloomAmount", this.BloomAmount);
      info.AddValue("BloomThreshold", this.BloomThreshold);
      info.AddValue("ExposureAmount", this.ExposureAmount);
      info.AddValue("DynamicRangeTransitionMaxScale", this.DynamicRangeTransitionMaxScale);
      info.AddValue("DynamicRangeTransitionMinScale", this.DynamicRangeTransitionMinScale);
      info.AddValue("DynamicRangeTransitionTime", this.DynamicRangeTransitionTime);
    }
  }
}
