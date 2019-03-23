// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.BaseTerrainEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using ns4;
using EmbeddedResources;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Base class that provides data for rendering SunBurn's terrain.
  /// </summary>
  [Attribute0(true)]
  public abstract class BaseTerrainEffect : BaseRenderableEffect, IEditorObject, IProjectFile, Interface1, ITerrainEffect
  {
      private int int_0;
    private int int_1;
    private int int_2;
    private float float_1;
    private float float_2;
    private float float_3;
    private float float_4;
    private float float_5;
    private float float_6;
    private Vector3 vector3_0;
    private Texture2D texture2D_0;
    private Texture2D texture2D_1;
    private Texture2D texture2D_2;
    private Texture2D texture2D_3;
    private Texture2D texture2D_4;
    private Texture2D texture2D_5;
    private Texture2D texture2D_6;
    private Texture2D texture2D_7;
    private Texture2D texture2D_8;
    private Texture2D texture2D_9;
    private Texture2D texture2D_10;
    private Texture2D texture2D_11;
    private Texture2D texture2D_12;
    private EffectParameter effectParameter_11;
    private EffectParameter effectParameter_12;
    private EffectParameter effectParameter_13;
    private EffectParameter effectParameter_14;
    private EffectParameter effectParameter_15;
    private EffectParameter effectParameter_16;
    private EffectParameter effectParameter_17;
    private EffectParameter effectParameter_18;
    private EffectParameter effectParameter_19;
    private EffectParameter effectParameter_20;
    private EffectParameter effectParameter_21;
    private EffectParameter effectParameter_22;
    private EffectParameter effectParameter_23;
    private EffectParameter effectParameter_24;
    private EffectParameter effectParameter_25;
    private EffectParameter effectParameter_26;
    private EffectParameter effectParameter_27;
    private EffectParameter effectParameter_28;
    private EffectParameter effectParameter_29;
    private EffectParameter effectParameter_30;

    /// <summary>
    /// Notifies the editor that this object is partially controlled via code. The editor
    /// will display information to the user indicating some property values are
    /// overridden in code and changes may not take effect.
    /// </summary>
    public bool AffectedInCode { get; set; }

    internal string MaterialFile { get; set; } = "";

      string Interface1.MaterialFile => this.MaterialFile;

      internal string MaterialName { get; set; }

    internal string ProjectFile { get; set; }

    string IProjectFile.ProjectFile => this.ProjectFile;

      internal string DiffuseMapLayer1File { get; set; }

    internal string DiffuseMapLayer2File { get; set; }

    internal string DiffuseMapLayer3File { get; set; }

    internal string DiffuseMapLayer4File { get; set; }

    internal string NormalMapLayer1File { get; set; }

    internal string NormalMapLayer2File { get; set; }

    internal string NormalMapLayer3File { get; set; }

    internal string NormalMapLayer4File { get; set; }

    internal string HeightMapFile { get; set; }

    internal string BlendMapFile { get; set; }

    /// <summary>
    /// Diffuse texture used in blend mapping (associated with the Red
    /// blend map texture channel).
    /// 
    /// For optimal performance always use the lowest layers first (for instance:
    /// if using two layers use layer 1 and layer 2).
    /// </summary>
    [Attribute2("DiffuseMapLayer1File")]
    [Attribute1(true, Description = "Diffuse 1", MajorGrouping = 2, MinorGrouping = 1, ToolTipText = "")]
    public Texture2D DiffuseMapLayer1Texture
    {
      get => this.texture2D_3;
        set => EffectHelper.Update(value, this.texture2D_1, ref this.texture2D_3, ref this.effectParameter_12);
    }

    /// <summary>
    /// Diffuse texture used in blend mapping (associated with the Green
    /// blend map texture channel).
    /// 
    /// For optimal performance always use the lowest layers first (for instance:
    /// if using two layers use layer 1 and layer 2).
    /// </summary>
    [Attribute2("DiffuseMapLayer2File")]
    [Attribute1(true, Description = "Diffuse 2", MajorGrouping = 3, MinorGrouping = 1, ToolTipText = "")]
    public Texture2D DiffuseMapLayer2Texture
    {
      get => this.texture2D_4;
        set
      {
        EffectHelper.Update(value, this.texture2D_1, ref this.texture2D_4, ref this.effectParameter_13);
        this.method_3();
      }
    }

    /// <summary>
    /// Diffuse texture used in blend mapping (associated with the Blue
    /// blend map texture channel).
    /// 
    /// For optimal performance always use the lowest layers first (for instance:
    /// if using two layers use layer 1 and layer 2).
    /// </summary>
    [Attribute2("DiffuseMapLayer3File")]
    [Attribute1(true, Description = "Diffuse 3", MajorGrouping = 4, MinorGrouping = 1, ToolTipText = "")]
    public Texture2D DiffuseMapLayer3Texture
    {
      get => this.texture2D_5;
        set
      {
        EffectHelper.Update(value, this.texture2D_1, ref this.texture2D_5, ref this.effectParameter_14);
        this.method_3();
      }
    }

    /// <summary>
    /// Diffuse texture used in blend mapping (associated with the Alpha
    /// blend map texture channel).
    /// 
    /// For optimal performance always use the lowest layers first (for instance:
    /// if using two layers use layer 1 and layer 2).
    /// </summary>
    [Attribute2("DiffuseMapLayer4File")]
    [Attribute1(true, Description = "Diffuse 4", MajorGrouping = 5, MinorGrouping = 1, ToolTipText = "")]
    public Texture2D DiffuseMapLayer4Texture
    {
      get => this.texture2D_6;
        set
      {
        EffectHelper.Update(value, this.texture2D_1, ref this.texture2D_6, ref this.effectParameter_15);
        this.method_3();
      }
    }

    /// <summary>
    /// Normal map texture used in blend mapping (associated with the Red
    /// blend map texture channel).
    /// 
    /// For optimal performance always use the lowest layers first (for instance:
    /// if using two layers use layer 1 and layer 2).
    /// </summary>
    [Attribute1(true, Description = "Normal 1", MajorGrouping = 2, MinorGrouping = 2, ToolTipText = "")]
    [Attribute2("NormalMapLayer1File")]
    public Texture2D NormalMapLayer1Texture
    {
      get => this.texture2D_7;
        set => EffectHelper.Update(value, this.texture2D_2, ref this.texture2D_7, ref this.effectParameter_16);
    }

    /// <summary>
    /// Normal map texture used in blend mapping (associated with the Green
    /// blend map texture channel).
    /// 
    /// For optimal performance always use the lowest layers first (for instance:
    /// if using two layers use layer 1 and layer 2).
    /// </summary>
    [Attribute2("NormalMapLayer2File")]
    [Attribute1(true, Description = "Normal 2", MajorGrouping = 3, MinorGrouping = 2, ToolTipText = "")]
    public Texture2D NormalMapLayer2Texture
    {
      get => this.texture2D_8;
        set
      {
        EffectHelper.Update(value, this.texture2D_2, ref this.texture2D_8, ref this.effectParameter_17);
        this.method_3();
      }
    }

    /// <summary>
    /// Normal map texture used in blend mapping (associated with the Blue
    /// blend map texture channel).
    /// 
    /// For optimal performance always use the lowest layers first (for instance:
    /// if using two layers use layer 1 and layer 2).
    /// </summary>
    [Attribute2("NormalMapLayer3File")]
    [Attribute1(true, Description = "Normal 3", MajorGrouping = 4, MinorGrouping = 2, ToolTipText = "")]
    public Texture2D NormalMapLayer3Texture
    {
      get => this.texture2D_9;
        set
      {
        EffectHelper.Update(value, this.texture2D_2, ref this.texture2D_9, ref this.effectParameter_18);
        this.method_3();
      }
    }

    /// <summary>
    /// Normal map texture used in blend mapping (associated with the Alpha
    /// blend map texture channel).
    /// 
    /// For optimal performance always use the lowest layers first (for instance:
    /// if using two layers use layer 1 and layer 2).
    /// </summary>
    [Attribute2("NormalMapLayer4File")]
    [Attribute1(true, Description = "Normal 4", MajorGrouping = 5, MinorGrouping = 2, ToolTipText = "")]
    public Texture2D NormalMapLayer4Texture
    {
      get => this.texture2D_10;
        set
      {
        EffectHelper.Update(value, this.texture2D_2, ref this.texture2D_10, ref this.effectParameter_19);
        this.method_3();
      }
    }

    /// <summary>
    /// Texture containing height values used to displace a terrain mesh. Also used
    /// for low frequency lighting.
    /// </summary>
    [Attribute2("HeightMapFile")]
    [Attribute1(true, Description = "Height Map", MajorGrouping = 1, MinorGrouping = 1, ToolTipText = "")]
    public Texture2D HeightMapTexture
    {
      get => this.texture2D_11;
        set
      {
        EffectHelper.Update(value, this.texture2D_0, ref this.texture2D_11, ref this.effectParameter_20);
        if (this.texture2D_11 == null)
          return;
        EffectHelper.Update(this.texture2D_11.Width / 3, ref this.int_2, ref this.effectParameter_23);
      }
    }

    /// <summary>
    /// Texture containing intensity values used to blend diffuse and normal map textures
    /// into the final material. Each texture channel (Red, Green, Blue, Alpha) controls
    /// a terrain texture layer (layer 1, 2, 3, 4).
    /// </summary>
    [Attribute1(true, Description = "Blend Map", MajorGrouping = 1, MinorGrouping = 2, ToolTipText = "")]
    [Attribute2("BlendMapFile")]
    public Texture2D BlendMapTexture
    {
      get => this.texture2D_12;
        set => EffectHelper.Update(value, this.texture2D_1, ref this.texture2D_12, ref this.effectParameter_21);
    }

    /// <summary>
    /// Controls the depth or detail level of low frequency lighting on a terrain.
    /// </summary>
    [Attribute1(true, Description = "Normal Strength", MajorGrouping = 6, MinorGrouping = 4, ToolTipText = "")]
    [Attribute5(2, 0.0, 32.0, 0.1)]
    public float NormalMapStrength
    {
      get => this.float_1;
        set => EffectHelper.Update(value, ref this.float_1, ref this.effectParameter_24);
    }

    /// <summary>
    /// Adjusts the number of times the blend mapped materials tile across a terrain's
    /// mesh. Similar to uv scale when texture mapping.
    /// </summary>
    [Attribute1(true, Description = "Material Scale", MajorGrouping = 6, MinorGrouping = 3, ToolTipText = "")]
    [Attribute5(2, 0.0, 512.0, 0.2)]
    public float DiffuseScale
    {
      get => this.float_2;
        set => EffectHelper.Update(value, ref this.float_2, ref this.effectParameter_25);
    }

    /// <summary>Adjusts the terrain displacement magnitude.</summary>
    [Attribute5(3, 0.0, 100.0, 0.01)]
    [Attribute1(true, Description = "Height Scale", MajorGrouping = 6, MinorGrouping = 1, ToolTipText = "")]
    public float HeightScale
    {
      get => this.float_3;
        set => EffectHelper.Update(value, ref this.float_3, ref this.effectParameter_26);
    }

    /// <summary>
    /// Adjusts the number of times the height map tiles across a terrain's
    /// mesh. Similar to uv scale when texture mapping.
    /// </summary>
    [Attribute1(true, Description = "Tiling Amount", MajorGrouping = 6, MinorGrouping = 2, ToolTipText = "")]
    [Attribute5(3, 0.0, 100.0, 0.01)]
    public float Tiling
    {
      get => this.float_4;
        set => EffectHelper.Update(value, ref this.float_4, ref this.effectParameter_27);
    }

    /// <summary>
    /// Power applied to material specular reflections. Affects how shiny a material appears.
    /// </summary>
    [Attribute5(2, 0.0, 256.0, 0.5)]
    [Attribute1(true, Description = "Specular Power", MajorGrouping = 7, MinorGrouping = 1, ToolTipText = "")]
    public float SpecularPower
    {
      get => this.float_5;
        set => EffectHelper.Update(value, ref this.float_5, ref this.effectParameter_28);
    }

    /// <summary>
    /// Intensity applied to material specular reflections. Affects how intense the specular appears.
    /// </summary>
    [Attribute5(2, 0.0, 32.0, 0.5)]
    [Attribute1(true, Description = "Specular Amount", MajorGrouping = 7, MinorGrouping = 2, ToolTipText = "")]
    public float SpecularAmount
    {
      get => this.float_6;
        set => EffectHelper.Update(value, ref this.float_6, ref this.effectParameter_29);
    }

    /// <summary>Color applied to material specular reflections.</summary>
    [Attribute1(true, ControlType = ControlType.ColorSelection, Description = "Specular Color", MajorGrouping = 7, MinorGrouping = 11, ToolTipText = "")]
    public Vector3 SpecularColor
    {
      get => this.vector3_0;
        set => EffectHelper.Update(value, ref this.vector3_0, ref this.effectParameter_30);
    }

    /// <summary>Density or tessellation of the terrain mesh.</summary>
    public int MeshSegments
    {
      get => this.int_1;
        set => EffectHelper.Update(value, ref this.int_1, ref this.effectParameter_22);
    }

    /// <summary>Creates a new BaseTerrainEffect instance.</summary>
    /// <param name="graphicsdevice"></param>
    /// <param name="effectname"></param>
    public BaseTerrainEffect(GraphicsDevice graphicsdevice, string effectname)
      : base(graphicsdevice, effectname)
    {
      this.method_4(graphicsdevice, true);
    }

    /// <summary>Creates a new BaseTerrainEffect instance.</summary>
    /// <param name="graphicsdevice"></param>
    /// <param name="effectname"></param>
    /// <param name="trackeffect"></param>
    internal BaseTerrainEffect(GraphicsDevice device, string string_13, bool bool_3)
      : base(device, string_13)
    {
      this.method_4(device, bool_3);
    }

    private bool method_2(Texture2D texture2D_13, Texture2D texture2D_14)
    {
      if (texture2D_13 != texture2D_14)
        return texture2D_13 == null;
      return true;
    }

    private void method_3()
    {
      if (this.effectParameter_11 == null)
        return;
      int int_0 = 1;
      if (this.method_2(this.texture2D_6, this.texture2D_1) && this.method_2(this.texture2D_10, this.texture2D_2))
      {
        if (this.method_2(this.texture2D_5, this.texture2D_1) && this.method_2(this.texture2D_9, this.texture2D_2))
        {
          if (!this.method_2(this.texture2D_4, this.texture2D_1) || !this.method_2(this.texture2D_8, this.texture2D_2))
            int_0 = 2;
        }
        else
          int_0 = 3;
      }
      else
        int_0 = 4;
      EffectHelper.Update(int_0, ref this.int_0, ref this.effectParameter_11);
    }

    private void method_4(GraphicsDevice graphicsDevice_0, bool bool_3)
    {
      this.effectParameter_11 = this.Parameters["LayerCount"];
      this.effectParameter_12 = this.Parameters["DiffuseLayer1Texture"];
      this.effectParameter_13 = this.Parameters["DiffuseLayer2Texture"];
      this.effectParameter_14 = this.Parameters["DiffuseLayer3Texture"];
      this.effectParameter_15 = this.Parameters["DiffuseLayer4Texture"];
      this.effectParameter_16 = this.Parameters["NormalLayer1Texture"];
      this.effectParameter_17 = this.Parameters["NormalLayer2Texture"];
      this.effectParameter_18 = this.Parameters["NormalLayer3Texture"];
      this.effectParameter_19 = this.Parameters["NormalLayer4Texture"];
      this.effectParameter_20 = this.Parameters["HeightMapTexture"];
      this.effectParameter_21 = this.Parameters["BlendMapTexture"];
      this.effectParameter_22 = this.Parameters["MeshSegments"];
      this.effectParameter_23 = this.Parameters["NormalMapSize"];
      this.effectParameter_24 = this.Parameters["NormalMapStrength"];
      this.effectParameter_25 = this.Parameters["DiffuseScale"];
      this.effectParameter_26 = this.Parameters["HeightScale"];
      this.effectParameter_27 = this.Parameters["Tiling"];
      this.effectParameter_28 = this.Parameters["SpecularPower"];
      this.effectParameter_29 = this.Parameters["SpecularAmount"];
      this.effectParameter_30 = this.Parameters["SpecularColor"];
      this.texture2D_1 = LightingSystemManager.Instance.EmbeddedTexture("White");
      this.texture2D_0 = LightingSystemManager.Instance.EmbeddedTexture("White");
      this.texture2D_2 = LightingSystemManager.Instance.EmbeddedTexture("Normal");
      this.DiffuseMapLayer1Texture = this.texture2D_1;
      this.DiffuseMapLayer2Texture = this.texture2D_1;
      this.DiffuseMapLayer3Texture = this.texture2D_1;
      this.DiffuseMapLayer4Texture = this.texture2D_1;
      this.NormalMapLayer1Texture = this.texture2D_2;
      this.NormalMapLayer2Texture = this.texture2D_2;
      this.NormalMapLayer3Texture = this.texture2D_2;
      this.NormalMapLayer4Texture = this.texture2D_2;
      this.BlendMapTexture = this.texture2D_1;
      this.HeightMapTexture = this.texture2D_0;
      this.SetTechnique();
      this.method_3();
      if (!bool_3)
        return;
      LightingSystemEditor.OnCreateResource(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the Effect and optionally releases the managed resources.
    /// </summary>
    /// <param name="releasemanaged"></param>
    protected override void Dispose(bool releasemanaged)
    {
      base.Dispose(releasemanaged);
      LightingSystemEditor.OnDisposeResource(this);
    }
  }
}
