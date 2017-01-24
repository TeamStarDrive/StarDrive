// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.ParameteredEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using SynapseGaming.LightingSystem.Core;
using System;
using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Base class for SAS, XSI, and other effects with shader driven properties.
  /// </summary>
  public abstract class ParameteredEffect : Effect, ITransparentEffect
  {
    private float float_0 = 1f;
    private Dictionary<string, object> dictionary_0 = new Dictionary<string, object>();
    internal ParameteredEffect.Class47 class47_0 = new ParameteredEffect.Class47();
    private bool bool_0;
    private bool bool_1;
    private bool bool_2;
    private TransparencyMode transparencyMode_0;
    private Texture texture_0;

    /// <summary>
    /// Determines if the effect's vertex transform differs from the built-in
    /// effects, this will cause z-fighting that must be accounted for. If
    /// the value is false (meaning it varies and is different from the built-in
    /// effects) a depth adjustment technique like depth-offset needs to be applied.
    /// </summary>
    public bool Invariant
    {
      get
      {
        return this.bool_0;
      }
    }

    /// <summary>
    /// Determines if the effect's shader alters render states during execution.
    /// </summary>
    public bool AffectsRenderStates
    {
      get
      {
        return this.bool_1;
      }
    }

    /// <summary>
    /// Surfaces rendered with the effect should be visible from both sides.
    /// </summary>
    public bool DoubleSided
    {
      get
      {
        return this.bool_2;
      }
      set
      {
        this.bool_2 = value;
      }
    }

    /// <summary>
    /// The transparency style used when rendering the effect.
    /// </summary>
    public TransparencyMode TransparencyMode
    {
      get
      {
        return this.transparencyMode_0;
      }
      set
      {
        this.transparencyMode_0 = value;
        this.SyncTransparency();
      }
    }

    /// <summary>
    /// Used with TransparencyMode to determine the effect transparency.
    ///   -For Clipped mode this value is a comparison value, where all TransparencyMap
    ///    alpha values below this value are *not* rendered.
    /// </summary>
    public float Transparency
    {
      get
      {
        return this.float_0;
      }
      set
      {
        this.float_0 = value;
        this.SyncTransparency();
      }
    }

    /// <summary>
    /// The texture map used for transparency (values are pulled from the alpha channel).
    /// </summary>
    public Texture TransparencyMap
    {
      get
      {
        return this.texture_0;
      }
      set
      {
      }
    }

    internal Dictionary<string, object> Properties
    {
      get
      {
        return this.dictionary_0;
      }
    }

    internal ParameteredEffect(GraphicsDevice graphicsDevice_0, Effect effect_0)
      : base(graphicsDevice_0, effect_0)
    {
      for (int index = 0; index < this.Parameters.Count; ++index)
      {
        EffectParameter parameter = this.Parameters[index];
        if (!this.dictionary_0.ContainsKey(parameter.Name) && parameter.RowCount <= 1 && (parameter.Elements.Count <= 0 && parameter.Annotations["SasBindAddress"] == null) && string.IsNullOrEmpty(parameter.Semantic))
        {
          if (parameter.ParameterType == EffectParameterType.Single)
          {
            if (parameter.ColumnCount == 0)
              this.dictionary_0.Add(parameter.Name, (object) parameter.GetValueSingle());
            else if (parameter.ColumnCount == 1 && parameter.ParameterClass == EffectParameterClass.Scalar)
              this.dictionary_0.Add(parameter.Name, (object) parameter.GetValueSingle());
            else if (parameter.ColumnCount == 3)
              this.dictionary_0.Add(parameter.Name, (object) parameter.GetValueVector3());
            else if (parameter.ColumnCount == 4)
              this.dictionary_0.Add(parameter.Name, (object) parameter.GetValueVector4());
          }
          else if (parameter.ParameterType == EffectParameterType.Int32 && parameter.ColumnCount == 0)
            this.dictionary_0.Add(parameter.Name, (object) parameter.GetValueInt32());
          else if (parameter.ParameterType == EffectParameterType.Texture2D && parameter.ColumnCount == 0)
            this.dictionary_0.Add(parameter.Name, (object) "");
          else if (parameter.ParameterType == EffectParameterType.Texture3D && parameter.ColumnCount == 0)
            this.dictionary_0.Add(parameter.Name, (object) "");
          else if (parameter.ParameterType == EffectParameterType.TextureCube && parameter.ColumnCount == 0)
            this.dictionary_0.Add(parameter.Name, (object) "");
          else if (parameter.ParameterType == EffectParameterType.Texture && parameter.ColumnCount == 0)
            this.dictionary_0.Add(parameter.Name, (object) "");
        }
      }
    }

    /// <summary>
    /// Sets all transparency information at once.  Used to improve performance
    /// by avoiding multiple effect technique changes.
    /// </summary>
    /// <param name="mode">The transparency style used when rendering the effect.</param>
    /// <param name="transparency">Used with TransparencyMode to determine the effect transparency.
    /// -For Clipped mode this value is a comparison value, where all TransparencyMap
    ///  alpha values below this value are *not* rendered.</param>
    /// <param name="map">The texture map used for transparency (values are pulled from the alpha channel).</param>
    public void SetTransparencyModeAndMap(TransparencyMode mode, float transparency, Texture map)
    {
      this.transparencyMode_0 = mode;
      this.float_0 = transparency;
      this.texture_0 = map;
      this.SyncTransparency();
    }

    /// <summary>
    /// Applies the object's transparency information to its effect parameters.
    /// </summary>
    protected virtual void SyncTransparency()
    {
    }

    /// <summary>Sets the effect technique by name.</summary>
    public void SetTechnique(string techniquename)
    {
      ++this.class47_0.lightingSystemStatistic_0.AccumulationValue;
      EffectTechnique technique = this.Techniques[techniquename];
      if (technique == null)
        return;
      this.CurrentTechnique = technique;
    }

    /// <summary>Sets the effect texture by name.</summary>
    public void SetTexture(string name, Texture texture)
    {
      EffectParameter parameter = this.Parameters[name];
      if (parameter == null)
        return;
      if (parameter.ParameterType == EffectParameterType.Texture2D && texture is Texture2D)
        parameter.SetValue(texture);
      else if (parameter.ParameterType == EffectParameterType.Texture3D && texture is Texture3D)
        parameter.SetValue(texture);
      else if (parameter.ParameterType == EffectParameterType.TextureCube && texture is TextureCube)
      {
        parameter.SetValue(texture);
      }
      else
      {
        if (parameter.ParameterType != EffectParameterType.Texture)
          return;
        parameter.SetValue(texture);
      }
    }

    internal void method_0()
    {
      foreach (KeyValuePair<string, object> keyValuePair in this.dictionary_0)
      {
        if (!(keyValuePair.Key == "EffectFile"))
        {
          if (keyValuePair.Key == "Technique")
            this.SetTechnique((string) keyValuePair.Value);
          else if (keyValuePair.Key == "Invariant")
            this.bool_0 = (bool) keyValuePair.Value;
          else if (keyValuePair.Key == "AffectsRenderStates")
            this.bool_1 = (bool) keyValuePair.Value;
          else if (keyValuePair.Key == "DoubleSided")
            this.bool_2 = (bool) keyValuePair.Value;
          else if (keyValuePair.Key == "TransparencyMode")
            this.transparencyMode_0 = (TransparencyMode) keyValuePair.Value;
          else if (keyValuePair.Key == "Transparency")
            this.float_0 = (float) keyValuePair.Value;
          else if (keyValuePair.Key == "TransparencyMapParameterName")
          {
            EffectParameter parameter = this.Parameters[(string) keyValuePair.Value];
            if (parameter != null)
            {
              if (parameter.ParameterType == EffectParameterType.Texture2D)
                this.texture_0 = (Texture) parameter.GetValueTexture2D();
              else if (parameter.ParameterType == EffectParameterType.Texture3D)
                this.texture_0 = (Texture) parameter.GetValueTexture3D();
            }
          }
          else
          {
            EffectParameter parameter = this.Parameters[keyValuePair.Key];
            if (parameter != null && (parameter.ParameterType == EffectParameterType.Single || parameter.ParameterType == EffectParameterType.Int32))
            {
              if (parameter.ParameterType == EffectParameterType.Single)
              {
                if (parameter.ColumnCount == 1)
                  parameter.SetValue(this.method_6(keyValuePair.Value));
                else if (parameter.ColumnCount == 3)
                  parameter.SetValue(this.method_5(keyValuePair.Value));
                else if (parameter.ColumnCount == 4)
                  parameter.SetValue(this.method_4(keyValuePair.Value));
              }
              else if (parameter.ParameterType == EffectParameterType.Int32)
                parameter.SetValue((int) this.method_6(keyValuePair.Value));
            }
          }
        }
      }
      this.SyncTransparency();
    }

    internal void method_1(Dictionary<string, object> dictionary_1)
    {
      foreach (KeyValuePair<string, object> keyValuePair in dictionary_1)
      {
        Type type1 = keyValuePair.Value.GetType();
        if ((keyValuePair.Key == "EffectFile" || keyValuePair.Key == "Technique" || (keyValuePair.Key == "DepthTechnique" || keyValuePair.Key == "GBufferTechnique") || (keyValuePair.Key == "FinalTechnique" || keyValuePair.Key == "ShadowGenerationTechnique" || (keyValuePair.Key == "Invariant" || keyValuePair.Key == "AffectsRenderStates")) || (keyValuePair.Key == "DoubleSided" || keyValuePair.Key == "TransparencyMode" || keyValuePair.Key == "TransparencyMapParameterName")) && type1 == typeof (string))
        {
          object object_0 = keyValuePair.Value;
          if (!(keyValuePair.Key == "Invariant") && !(keyValuePair.Key == "AffectsRenderStates") && !(keyValuePair.Key == "DoubleSided"))
          {
            if (keyValuePair.Key == "TransparencyMode")
              object_0 = (object) Class11.smethod_1<TransparencyMode>((string) object_0);
          }
          else
            object_0 = (object) this.method_3(object_0);
          if (this.dictionary_0.ContainsKey(keyValuePair.Key))
            this.dictionary_0[keyValuePair.Key] = object_0;
          else
            this.dictionary_0.Add(keyValuePair.Key, object_0);
        }
        else if (keyValuePair.Key == "Transparency" && type1 == typeof (float))
        {
          if (this.dictionary_0.ContainsKey(keyValuePair.Key))
            this.dictionary_0[keyValuePair.Key] = (object) (float) keyValuePair.Value;
          else
            this.dictionary_0.Add(keyValuePair.Key, (object) (float) keyValuePair.Value);
        }
        else if (this.dictionary_0.ContainsKey(keyValuePair.Key))
        {
          object obj = this.dictionary_0[keyValuePair.Key];
          Type type2 = obj.GetType();
          if (type2 == type1)
            obj = keyValuePair.Value;
          else if (type2 == typeof (float))
            obj = (object) this.method_6(keyValuePair.Value);
          else if (type2 == typeof (Vector3))
            obj = (object) this.method_5(keyValuePair.Value);
          else if (type2 == typeof (Vector4))
            obj = (object) this.method_4(keyValuePair.Value);
          this.dictionary_0[keyValuePair.Key] = obj;
        }
      }
      if (!this.dictionary_0.ContainsKey("Technique"))
        this.dictionary_0.Add("Technique", (object) this.CurrentTechnique.Name);
      if (!this.dictionary_0.ContainsKey("DepthTechnique"))
        this.dictionary_0.Add("DepthTechnique", (object) this.method_2("DepthTechnique"));
      if (!this.dictionary_0.ContainsKey("GBufferTechnique"))
        this.dictionary_0.Add("GBufferTechnique", (object) this.method_2("GBufferTechnique"));
      if (!this.dictionary_0.ContainsKey("FinalTechnique"))
        this.dictionary_0.Add("FinalTechnique", (object) this.method_2("FinalTechnique"));
      if (!this.dictionary_0.ContainsKey("ShadowGenerationTechnique"))
        this.dictionary_0.Add("ShadowGenerationTechnique", (object) this.method_2("ShadowGenerationTechnique"));
      if (!this.dictionary_0.ContainsKey("Invariant"))
        this.dictionary_0.Add("Invariant", (object) false);
      if (!this.dictionary_0.ContainsKey("AffectsRenderStates"))
        this.dictionary_0.Add("AffectsRenderStates", (object) false);
      if (!this.dictionary_0.ContainsKey("DoubleSided"))
        this.dictionary_0.Add("DoubleSided", (object) false);
      if (!this.dictionary_0.ContainsKey("TransparencyMode"))
        this.dictionary_0.Add("TransparencyMode", (object) TransparencyMode.None);
      if (!this.dictionary_0.ContainsKey("Transparency"))
        this.dictionary_0.Add("Transparency", (object) 0.5f);
      if (this.dictionary_0.ContainsKey("TransparencyMapParameterName"))
        return;
      string str = "";
      for (int index = 0; index < this.Parameters.Count; ++index)
      {
        EffectParameter parameter = this.Parameters[index];
        if ((parameter.ParameterType == EffectParameterType.Texture || parameter.ParameterType == EffectParameterType.Texture2D || parameter.ParameterType == EffectParameterType.Texture3D) && !string.IsNullOrEmpty(parameter.Name))
        {
          str = parameter.Name;
          break;
        }
      }
      this.dictionary_0.Add("TransparencyMapParameterName", (object) str);
    }

    private string method_2(string string_0)
    {
      if (this.Techniques[string_0] == null)
        return "";
      return string_0;
    }

    private bool method_3(object object_0)
    {
      if (object_0 is bool)
        return (bool) object_0;
      if (object_0 is string)
      {
        try
        {
          return bool.Parse((string) object_0);
        }
        catch
        {
        }
      }
      return false;
    }

    private Vector4 method_4(object object_0)
    {
      if (object_0 is Vector4)
        return (Vector4) object_0;
      if (object_0 is Vector3)
        return new Vector4((Vector3) object_0, 1f);
      if (object_0 is float)
        return new Vector4((float) object_0);
      return new Vector4();
    }

    private Vector3 method_5(object object_0)
    {
      if (object_0 is Vector4)
      {
        Vector4 vector4 = (Vector4) object_0;
        return new Vector3(vector4.X, vector4.Y, vector4.Z);
      }
      if (object_0 is Vector3)
        return (Vector3) object_0;
      if (object_0 is float)
        return new Vector3((float) object_0);
      return new Vector3();
    }

    private float method_6(object object_0)
    {
      if (object_0 is Vector4)
        return ((Vector4) object_0).X;
      if (object_0 is Vector3)
        return ((Vector3) object_0).X;
      if (object_0 is float)
        return (float) object_0;
      return 0.0f;
    }

    /// <summary>
    /// Creates a new effect of the same class type, with the same property values, and using the same effect file as this object.
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    public override Effect Clone(GraphicsDevice device)
    {
      Effect effect = this.Create(device);
      Class12.smethod_1((object) this, (object) effect);
      return effect;
    }

    /// <summary>
    /// Creates a new empty effect of the same class type and using the same effect file as this object.
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    protected abstract Effect Create(GraphicsDevice device);

    internal class Class47
    {
      public LightingSystemStatistic lightingSystemStatistic_0 = LightingSystemStatistics.GetStatistic("Effect_TechniqueChanges", LightingSystemStatisticCategory.Rendering);
      public LightingSystemStatistic lightingSystemStatistic_1 = LightingSystemStatistics.GetStatistic("Effect_LightSourceChanges", LightingSystemStatisticCategory.Rendering);
    }
  }
}
