// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.ShadowSource
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using Microsoft.Xna.Framework;
using ns3;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Shadows
{
  /// <summary>
  /// Provides a shareable point shadow source for use with PointLight objects.
  /// Any number of PointLight objects can share the same shadow source.  Shadow
  /// source position and properties are independent of the lights that reference it.
  /// </summary>
  [Serializable]
  public class ShadowSource : INamedObject, IEditorObject, ISerializable, IPointSource, IShadowSource
  {
    private string string_0 = "";
    private ShadowType shadowType_0 = ShadowType.AllObjects;
    private float float_0 = 0.5f;
    private float float_1 = 1f;
    private float float_2 = 0.2f;
    private bool bool_0 = true;
    private Matrix matrix_0 = Matrix.Identity;
    private bool bool_1;

    /// <summary>The object's current name.</summary>
    public string Name
    {
      get => this.string_0;
        set => this.string_0 = value;
    }

    /// <summary>
    /// Notifies the editor that this object is partially controlled via code. The editor
    /// will display information to the user indicating some property values are
    /// overridden in code and changes may not take effect.
    /// </summary>
    public bool AffectedInCode { get; set; }

    /// <summary>
    /// Defines the type of objects that cast shadows from the source.
    /// Does not affect an object's ability to receive shadows.
    /// </summary>
    public ShadowType ShadowType
    {
      get => this.shadowType_0;
        set => this.shadowType_0 = value;
    }

    /// <summary>Position in world space of the shadow source.</summary>
    public Vector3 ShadowPosition => this.matrix_0.Translation;

      /// <summary>Adjusts the visual quality of casts shadows.</summary>
    public float ShadowQuality
    {
      get => this.float_0;
          set => this.float_0 = MathHelper.Clamp(value, 0.0f, 1f);
      }

    /// <summary>Main property used to eliminate shadow artifacts.</summary>
    public float ShadowPrimaryBias
    {
      get => this.float_1;
        set => this.float_1 = value;
    }

    /// <summary>
    /// Additional fine-tuned property used to eliminate shadow artifacts.
    /// </summary>
    public float ShadowSecondaryBias
    {
      get => this.float_2;
        set => this.float_2 = value;
    }

    /// <summary>
    /// Enables independent level-of-detail per cubemap face on point-based lights.
    /// </summary>
    public bool ShadowPerSurfaceLOD
    {
      get => this.bool_0;
        set => this.bool_0 = value;
    }

    /// <summary>
    /// Requests that all lights contained within the shadow source are rendered in one
    /// pass (this is only a performance hint - support depends on the rendering implementation).
    /// </summary>
    public bool ShadowRenderLightsTogether
    {
      get => this.bool_1;
        set => this.bool_1 = value;
    }

    /// <summary>Position in world space of the source.</summary>
    public Vector3 Position
    {
      get => this.matrix_0.Translation;
        set => this.matrix_0.Translation = value;
    }

    /// <summary>
    /// Maximum distance in world space of the source's influence.
    /// </summary>
    public float Radius
    {
      get
      {
        return 0.0f;
      }
      set
      {
      }
    }

    /// <summary>World space transform of the shadow source.</summary>
    public Matrix World
    {
      get => this.matrix_0;
        set => this.matrix_0 = value;
    }

    /// <summary>Creates a new ShadowSource instance.</summary>
    public ShadowSource()
    {
    }

    /// <summary />
    protected ShadowSource(SerializationInfo serializationInfo_0, StreamingContext streamingContext_0)
    {
      Vector3 gparam_0 = new Vector3();
      foreach (SerializationEntry serializationEntry in serializationInfo_0)
      {
        switch (serializationEntry.Name)
        {
          case "ShadowType":
            Class28.smethod_1(ref this.shadowType_0, serializationInfo_0, "ShadowType");
            continue;
          case "Position":
            Class28.smethod_0(ref gparam_0, serializationInfo_0, "Position");
            this.Position = gparam_0;
            continue;
          case "Name":
            Class28.smethod_0(ref this.string_0, serializationInfo_0, "Name");
            continue;
          case "ShadowQuality":
            Class28.smethod_0(ref this.float_0, serializationInfo_0, "ShadowQuality");
            continue;
          case "ShadowPrimaryBias":
            Class28.smethod_0(ref this.float_1, serializationInfo_0, "ShadowPrimaryBias");
            continue;
          case "ShadowSecondaryBias":
            Class28.smethod_0(ref this.float_2, serializationInfo_0, "ShadowSecondaryBias");
            continue;
          case "ShadowPerSurfaceLOD":
            Class28.smethod_0(ref this.bool_0, serializationInfo_0, "ShadowPerSurfaceLOD");
            continue;
          case "ShadowRenderLightsTogether":
            Class28.smethod_0(ref this.bool_1, serializationInfo_0, "ShadowRenderLightsTogether");
            continue;
          default:
            continue;
        }
      }
    }

    internal void method_0(IShadowSource ishadowSource_0)
    {
      this.ShadowPerSurfaceLOD = ishadowSource_0.ShadowPerSurfaceLOD;
      this.ShadowQuality = ishadowSource_0.ShadowQuality;
      this.ShadowPrimaryBias = ishadowSource_0.ShadowPrimaryBias;
      this.ShadowSecondaryBias = ishadowSource_0.ShadowSecondaryBias;
      this.ShadowType = ishadowSource_0.ShadowType;
      this.World = ishadowSource_0.World;
    }

    /// <summary>
    /// Returns a hash code that uniquely identifies the shadow source
    /// and its current state.  Changes to ShadowPosition affects the
    /// hash code, which is used to trigger updates on related shadows.
    /// </summary>
    /// <returns>Shadow hash code.</returns>
    public int GetShadowSourceHashCode()
    {
      return this.ShadowPosition.GetHashCode();
    }

    /// <summary />
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("Name", this.Name);
      info.AddValue("ShadowType", this.ShadowType);
      info.AddValue("Position", this.Position);
      info.AddValue("Radius", this.Radius);
      info.AddValue("ShadowQuality", this.ShadowQuality);
      info.AddValue("ShadowPrimaryBias", this.ShadowPrimaryBias);
      info.AddValue("ShadowSecondaryBias", this.ShadowSecondaryBias);
      info.AddValue("ShadowPerSurfaceLOD", this.ShadowPerSurfaceLOD);
      info.AddValue("ShadowRenderLightsTogether", this.ShadowRenderLightsTogether);
    }
  }
}
