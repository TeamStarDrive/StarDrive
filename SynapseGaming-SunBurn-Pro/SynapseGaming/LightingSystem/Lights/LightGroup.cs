// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.LightGroup
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Shadows;

namespace SynapseGaming.LightingSystem.Lights
{
  /// <summary>
  /// Light group object used to help organizing scene lights within a rig.
  /// </summary>
  [Serializable]
  public class LightGroup : ShadowSource, INamedObject, IEditorObject, ISerializable, IShadowSource, ILightGroup
  {
    private List<ILight> list_0 = new List<ILight>(16);
    private bool bool_3;

      /// <summary>Readonly list of the contained lights.</summary>
    public IList<ILight> Lights { get; private set; }

      /// <summary>
    /// Determines if the group acts as a shared shadow source for all contained
    /// lights. This allows a considerable performance increase over per-light shadows.
    /// </summary>
    public bool ShadowGroup
    {
      get => this.bool_3;
          set
      {
        this.bool_3 = value;
        IShadowSource shadowSource = null;
        if (value)
          shadowSource = this;
        foreach (ILight light in this.list_0)
          light.ShadowSource = shadowSource;
      }
    }

    /// <summary>Creates a LightGroup instance.</summary>
    public LightGroup()
    {
      this.Name = "Group";
      this.method_1();
    }

    /// <summary />
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected LightGroup(SerializationInfo serializationInfo_0, StreamingContext streamingContext_0)
      : base(serializationInfo_0, streamingContext_0)
    {
      this.method_1();
      foreach (SerializationEntry serializationEntry in serializationInfo_0)
      {
        switch (serializationEntry.Name)
        {
          case "Lights":
            this.list_0.AddRange((IEnumerable<ILight>) serializationInfo_0.GetValue("Lights", typeof (List<ILight>)));
            continue;
          case "ShadowGroup":
            this.bool_3 = (bool) serializationInfo_0.GetValue("ShadowGroup", typeof (bool));
            continue;
          default:
            continue;
        }
      }
      this.ShadowGroup = this.ShadowGroup;
    }

    /// <summary>Adds a light to the group.</summary>
    /// <param name="light"></param>
    public void Add(ILight light)
    {
      this.list_0.Add(light);
      if (this.bool_3)
        light.ShadowSource = this;
      else
        light.ShadowSource = null;
    }

    /// <summary>Removes a light to the group.</summary>
    /// <param name="light"></param>
    public void Remove(ILight light)
    {
      this.list_0.Remove(light);
      light.ShadowSource = null;
    }

    /// <summary>Removes the light at a specific index.</summary>
    /// <param name="index"></param>
    public void RemoveAt(int index)
    {
      this.Remove(this.list_0[index]);
    }

    /// <summary>Removes all lights from the group.</summary>
    public void Clear()
    {
      foreach (ILight light in this.list_0)
        light.ShadowSource = null;
      this.list_0.Clear();
    }

    private void method_1()
    {
      this.Lights = this.list_0.AsReadOnly();
    }

    /// <summary />
    /// <param name="info"></param>
    /// <param name="context"></param>
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      base.GetObjectData(info, context);
      info.AddValue("ShadowGroup", this.bool_3);
      info.AddValue("Lights", this.list_0);
    }
  }
}
