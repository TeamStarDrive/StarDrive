// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.AmbientLight
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using ns3;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Shadows;
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace SynapseGaming.LightingSystem.Lights
{
  /// <summary>
  /// Provides ambient light information for rendering lighting.
  /// </summary>
  [Serializable]
  public class AmbientLight : IMovableObject, INamedObject, IEditorObject, ISerializable, ILight, IAmbientSource
  {
    private static BoundingBox boundingBox_0 = new BoundingBox(new Vector3(float.MinValue, float.MinValue, float.MinValue), new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
    private static BoundingSphere boundingSphere_0 = new BoundingSphere(new Vector3(), float.MaxValue);
    private bool bool_0 = true;
    private Vector3 vector3_0 = new Vector3(0.0f, 0.0f, 0.0f);
    private float float_0 = 1f;
    private float float_1 = 0.15f;
    private string string_0 = "";

    /// <summary>
    /// Turns illumination on and off without removing the light from the scene.
    /// </summary>
    public bool Enabled
    {
      get
      {
        return this.bool_0;
      }
      set
      {
        this.bool_0 = value;
      }
    }

    /// <summary>Direct lighting color given off by the light.</summary>
    public Vector3 DiffuseColor
    {
      get
      {
        return this.vector3_0;
      }
      set
      {
        this.vector3_0 = value;
      }
    }

    /// <summary>Intensity of the light.</summary>
    public float Intensity
    {
      get
      {
        return this.float_0;
      }
      set
      {
        this.float_0 = value;
      }
    }

    /// <summary>Unused.</summary>
    public bool FillLight
    {
      get
      {
        return true;
      }
      set
      {
      }
    }

    /// <summary>
    /// Controls how quickly lighting falls off over distance (unused in this light type).
    /// </summary>
    public float FalloffStrength
    {
      get
      {
        return 0.0f;
      }
      set
      {
      }
    }

    /// <summary>
    /// The combined light color and intensity (provided for convenience).
    /// </summary>
    public Vector3 CompositeColorAndIntensity
    {
      get
      {
        return this.vector3_0 * this.float_0;
      }
    }

    /// <summary>Bounding area of the light's influence.</summary>
    public BoundingBox WorldBoundingBox
    {
      get
      {
        return AmbientLight.boundingBox_0;
      }
    }

    /// <summary>Bounding area of the light's influence.</summary>
    public BoundingSphere WorldBoundingSphere
    {
      get
      {
        return AmbientLight.boundingSphere_0;
      }
    }

    /// <summary>Shadow source the light's shadows are generated from.</summary>
    public IShadowSource ShadowSource
    {
      get
      {
        return (IShadowSource) null;
      }
      set
      {
      }
    }

    /// <summary>World space transform of the light.</summary>
    public Matrix World
    {
      get
      {
        return Matrix.Identity;
      }
      set
      {
      }
    }

    /// <summary>
    /// Indicates the object bounding area spans the entire world and
    /// the object is always visible.
    /// </summary>
    public bool InfiniteBounds
    {
      get
      {
        return true;
      }
    }

    /// <summary>
    /// Indicates the current move. This value increments each time the object
    /// is moved (when the World transform changes).
    /// </summary>
    public int MoveId
    {
      get
      {
        return 0;
      }
    }

    /// <summary>
    /// Defines how movement is applied. Updates to Dynamic objects
    /// are automatically applied, where Static objects must be moved
    /// manually using [manager].Move().
    /// 
    /// Important note: ObjectType can be changed at any time, HOWEVER managers
    /// will only see the change after removing and resubmitting the object.
    /// </summary>
    public ObjectType ObjectType
    {
      get
      {
        return ObjectType.Static;
      }
      set
      {
      }
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
        this.string_0 = value;
      }
    }

    /// <summary>
    /// Notifies the editor that this object is partially controlled via code. The editor
    /// will display information to the user indicating some property values are
    /// overridden in code and changes may not take effect.
    /// </summary>
    public bool AffectedInCode { get; set; }

    /// <summary>
    /// Increases the detail of normal mapped surfaces during the ambient lighting pass (deferred rendering only).
    /// </summary>
    public float Depth
    {
      get
      {
        return this.float_1;
      }
      set
      {
        this.float_1 = MathHelper.Clamp(value, 0.0f, 0.5f);
      }
    }

    /// <summary>Creates a new AmbientLight instance.</summary>
    public AmbientLight()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected AmbientLight(SerializationInfo serializationInfo_0, StreamingContext streamingContext_0)
    {
      foreach (SerializationEntry serializationEntry in serializationInfo_0)
      {
        switch (serializationEntry.Name)
        {
          case "Enabled":
            Class28.smethod_0<bool>(ref this.bool_0, serializationInfo_0, "Enabled");
            continue;
          case "DiffuseColor":
            Class28.smethod_0<Vector3>(ref this.vector3_0, serializationInfo_0, "DiffuseColor");
            continue;
          case "Intensity":
            Class28.smethod_0<float>(ref this.float_0, serializationInfo_0, "Intensity");
            continue;
          case "Name":
            Class28.smethod_0<string>(ref this.string_0, serializationInfo_0, "Name");
            continue;
          case "Depth":
            float gparam_0 = 0.0f;
            Class28.smethod_0<float>(ref gparam_0, serializationInfo_0, "Depth");
            this.Depth = gparam_0;
            continue;
          default:
            continue;
        }
      }
    }

    /// <summary>Returns a String that represents the current Object.</summary>
    /// <returns></returns>
    public override string ToString()
    {
      return CoreUtils.NamedObject((INamedObject) this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("Name", (object) this.Name);
      info.AddValue("Enabled", this.Enabled);
      info.AddValue("DiffuseColor", (object) this.DiffuseColor);
      info.AddValue("Intensity", this.Intensity);
      info.AddValue("Depth", this.Depth);
    }
  }
}
