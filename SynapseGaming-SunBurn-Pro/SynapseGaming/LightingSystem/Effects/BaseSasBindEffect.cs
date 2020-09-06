using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Base class for effects with automatic support for, and binding of, FX Standard Annotations and Semantics (SAS).
  /// </summary>
  public abstract class BaseSasBindEffect : ParameteredEffect
  {
    /// <summary />
    public static readonly string[] SASAddress_AmbientLight_Color = new string[4]{ "Sas.AmbientLight[0].Color", "Sas.AmbientLight[1].Color", "Sas.AmbientLight[2].Color", "Sas.AmbientLight[3].Color" };
    /// <summary />
    public static readonly string[] SASAddress_DirectionalLight_Color = new string[4]{ "Sas.DirectionalLight[0].Color", "Sas.DirectionalLight[1].Color", "Sas.DirectionalLight[2].Color", "Sas.DirectionalLight[3].Color" };
    /// <summary />
    public static readonly string[] SASAddress_DirectionalLight_Direction = new string[4]{ "Sas.DirectionalLight[0].Direction", "Sas.DirectionalLight[1].Direction", "Sas.DirectionalLight[2].Direction", "Sas.DirectionalLight[3].Direction" };
    /// <summary />
    public static readonly string[] SASAddress_PointLight_Color = new string[4]{ "Sas.PointLight[0].Color", "Sas.PointLight[1].Color", "Sas.PointLight[2].Color", "Sas.PointLight[3].Color" };
    /// <summary />
    public static readonly string[] SASAddress_PointLight_Position = new string[4]{ "Sas.PointLight[0].Position", "Sas.PointLight[1].Position", "Sas.PointLight[2].Position", "Sas.PointLight[3].Position" };
    /// <summary />
    public static readonly string[] SASAddress_PointLight_Range = new string[4]{ "Sas.PointLight[0].Range", "Sas.PointLight[1].Range", "Sas.PointLight[2].Range", "Sas.PointLight[3].Range" };
    private float Now;
    private float Last;

      /// <summary />
    public const string SASAddress_World_Matrix = "Sas.Camera.World";
    /// <summary />
    public const string SASAddress_WorldInverse_Matrix = "Sas.Camera.WorldInverse";
    /// <summary />
    public const string SASAddress_WorldTranspose_Matrix = "Sas.Camera.WorldTranspose";
    /// <summary />
    public const string SASAddress_WorldInverseTranspose_Matrix = "Sas.Camera.WorldInverseTranspose";
    /// <summary />
    public const string SASAddress_WorldView_Matrix = "Sas.Camera.ObjectToView";
    /// <summary />
    public const string SASAddress_WorldViewInverse_Matrix = "Sas.Camera.ObjectToViewInverse";
    /// <summary />
    public const string SASAddress_WorldViewTranspose_Matrix = "Sas.Camera.ObjectToViewTranspose";
    /// <summary />
    public const string SASAddress_WorldViewInverseTranspose_Matrix = "Sas.Camera.ObjectToViewInverseTranspose";
    /// <summary />
    public const string SASAddress_WorldViewProjection_Matrix = "Sas.Camera.ObjectToProjection";
    /// <summary />
    public const string SASAddress_WorldViewProjectionInverse_Matrix = "Sas.Camera.ObjectToProjectionInverse";
    /// <summary />
    public const string SASAddress_WorldViewProjectionTranspose_Matrix = "Sas.Camera.ObjectToProjectionTranspose";
    /// <summary />
    public const string SASAddress_WorldViewProjectionInverseTranspose_Matrix = "Sas.Camera.ObjectToProjectionInverseTranspose";
    /// <summary />
    public const string SASAddress_View_Matrix = "Sas.Camera.WorldToView";
    /// <summary />
    public const string SASAddress_ViewInverse_Matrix = "Sas.Camera.WorldToViewInverse";
    /// <summary />
    public const string SASAddress_ViewTranspose_Matrix = "Sas.Camera.WorldToViewTranspose";
    /// <summary />
    public const string SASAddress_ViewInverseTranspose_Matrix = "Sas.Camera.WorldToViewInverseTranspose";
    /// <summary />
    public const string SASAddress_Projection_Matrix = "Sas.Camera.Projection";
    /// <summary />
    public const string SASAddress_ProjectionInverse_Matrix = "Sas.Camera.ProjectionInverse";
    /// <summary />
    public const string SASAddress_ProjectionTranspose_Matrix = "Sas.Camera.ProjectionTranspose";
    /// <summary />
    public const string SASAddress_ProjectionInverseTranspose_Matrix = "Sas.Camera.ProjectionInverseTranspose";
    /// <summary />
    public const string SASAddress_NumAmbientLights = "Sas.NumAmbientLights";
    /// <summary />
    public const string SASAddress_NumDirectionalLights = "Sas.NumDirectionalLights";
    /// <summary />
    public const string SASAddress_NumPointLights = "Sas.NumPointLights";
    /// <summary />
    public const string SASAddress_Camera_Position = "Sas.Camera.Position";
    /// <summary />
    public const string SASAddress_SkeletonBones_Matrix = "Sas.Skeleton.MeshToJointToWorld[*]";
    /// <summary />
    public const string SASAddress_Time_Now = "Sas.Time.Now";
    /// <summary />
    public const string SASAddress_Time_Last = "Sas.Time.Last";
    /// <summary />
    public const string SASAddress_Time_FrameNumber = "Sas.Time.FrameNumber";
    private int FrameNumber;

    public void UpdateTime(float deltaTime)
    {
        Last = Now;
        Now += deltaTime;
        ++FrameNumber;
        SyncTimeEffectData();
    }

    /// <summary>
    /// Maintains a table of string addresses and their bound effect parameter lists.
    /// Used to tie any number of similar parameters using different names, semantics,
    /// and bind addresses to the same single address.
    /// </summary>
    protected GClass0 SasAutoBindTable { get; } = new GClass0();

      internal BaseSasBindEffect(GraphicsDevice device, Effect effect_0)
      : base(device, effect_0)
    {
      this.BindAllByPartialSasAddress("Sas.");
      this.BindBySasAddress(this.FindBySemantic("MODEL"), "Sas.Camera.World");
      this.BindBySasAddress(this.FindBySemantic("MODELI"), "Sas.Camera.WorldInverse");
      this.BindBySasAddress(this.FindBySemantic("MODELINVERSE"), "Sas.Camera.WorldInverse");
      this.BindBySasAddress(this.FindBySemantic("MODELT"), "Sas.Camera.WorldTranspose");
      this.BindBySasAddress(this.FindBySemantic("MODELTRANSPOSE"), "Sas.Camera.WorldTranspose");
      this.BindBySasAddress(this.FindBySemantic("MODELIT"), "Sas.Camera.WorldInverseTranspose");
      this.BindBySasAddress(this.FindBySemantic("MODELINVERSETRANSPOSE"), "Sas.Camera.WorldInverseTranspose");
      this.BindBySasAddress(this.FindBySemantic("WORLD"), "Sas.Camera.World");
      this.BindBySasAddress(this.FindBySemantic("WORLDI"), "Sas.Camera.WorldInverse");
      this.BindBySasAddress(this.FindBySemantic("WORLDINVERSE"), "Sas.Camera.WorldInverse");
      this.BindBySasAddress(this.FindBySemantic("WORLDT"), "Sas.Camera.WorldTranspose");
      this.BindBySasAddress(this.FindBySemantic("WORLDTRANSPOSE"), "Sas.Camera.WorldTranspose");
      this.BindBySasAddress(this.FindBySemantic("WORLDIT"), "Sas.Camera.WorldInverseTranspose");
      this.BindBySasAddress(this.FindBySemantic("WORLDINVERSETRANSPOSE"), "Sas.Camera.WorldInverseTranspose");
      this.BindBySasAddress(this.FindBySemantic("MODELVIEW"), "Sas.Camera.ObjectToView");
      this.BindBySasAddress(this.FindBySemantic("MODELVIEWI"), "Sas.Camera.ObjectToViewInverse");
      this.BindBySasAddress(this.FindBySemantic("MODELVIEWINVERSE"), "Sas.Camera.ObjectToViewInverse");
      this.BindBySasAddress(this.FindBySemantic("MODELVIEWT"), "Sas.Camera.ObjectToViewTranspose");
      this.BindBySasAddress(this.FindBySemantic("MODELVIEWTRANSPOSE"), "Sas.Camera.ObjectToViewTranspose");
      this.BindBySasAddress(this.FindBySemantic("MODELVIEWIT"), "Sas.Camera.ObjectToViewInverseTranspose");
      this.BindBySasAddress(this.FindBySemantic("MODELVIEWINVERSETRANSPOSE"), "Sas.Camera.ObjectToViewInverseTranspose");
      this.BindBySasAddress(this.FindBySemantic("WORLDVIEW"), "Sas.Camera.ObjectToView");
      this.BindBySasAddress(this.FindBySemantic("WORLDVIEWI"), "Sas.Camera.ObjectToViewInverse");
      this.BindBySasAddress(this.FindBySemantic("WORLDVIEWINVERSE"), "Sas.Camera.ObjectToViewInverse");
      this.BindBySasAddress(this.FindBySemantic("WORLDVIEWT"), "Sas.Camera.ObjectToViewTranspose");
      this.BindBySasAddress(this.FindBySemantic("WORLDVIEWTRANSPOSE"), "Sas.Camera.ObjectToViewTranspose");
      this.BindBySasAddress(this.FindBySemantic("WORLDVIEWIT"), "Sas.Camera.ObjectToViewInverseTranspose");
      this.BindBySasAddress(this.FindBySemantic("WORLDVIEWINVERSETRANSPOSE"), "Sas.Camera.ObjectToViewInverseTranspose");
      this.BindBySasAddress(this.FindBySemantic("MODELVIEWPROJECTION"), "Sas.Camera.ObjectToProjection");
      this.BindBySasAddress(this.FindBySemantic("MODELVIEWPROJECTIONI"), "Sas.Camera.ObjectToProjectionInverse");
      this.BindBySasAddress(this.FindBySemantic("MODELVIEWPROJECTIONINVERSE"), "Sas.Camera.ObjectToProjectionInverse");
      this.BindBySasAddress(this.FindBySemantic("MODELVIEWPROJECTIONT"), "Sas.Camera.ObjectToProjectionTranspose");
      this.BindBySasAddress(this.FindBySemantic("MODELVIEWPROJECTIONTRANSPOSE"), "Sas.Camera.ObjectToProjectionTranspose");
      this.BindBySasAddress(this.FindBySemantic("MODELVIEWPROJECTIONIT"), "Sas.Camera.ObjectToProjectionInverseTranspose");
      this.BindBySasAddress(this.FindBySemantic("MODELVIEWPROJECTIONINVERSETRANSPOSE"), "Sas.Camera.ObjectToProjectionInverseTranspose");
      this.BindBySasAddress(this.FindBySemantic("WORLDVIEWPROJECTION"), "Sas.Camera.ObjectToProjection");
      this.BindBySasAddress(this.FindBySemantic("WORLDVIEWPROJECTIONI"), "Sas.Camera.ObjectToProjectionInverse");
      this.BindBySasAddress(this.FindBySemantic("WORLDVIEWPROJECTIONINVERSE"), "Sas.Camera.ObjectToProjectionInverse");
      this.BindBySasAddress(this.FindBySemantic("WORLDVIEWPROJECTIONT"), "Sas.Camera.ObjectToProjectionTranspose");
      this.BindBySasAddress(this.FindBySemantic("WORLDVIEWPROJECTIONTRANSPOSE"), "Sas.Camera.ObjectToProjectionTranspose");
      this.BindBySasAddress(this.FindBySemantic("WORLDVIEWPROJECTIONIT"), "Sas.Camera.ObjectToProjectionInverseTranspose");
      this.BindBySasAddress(this.FindBySemantic("WORLDVIEWPROJECTIONINVERSETRANSPOSE"), "Sas.Camera.ObjectToProjectionInverseTranspose");
      this.BindBySasAddress(this.FindBySemantic("VIEW"), "Sas.Camera.WorldToView");
      this.BindBySasAddress(this.FindBySemantic("VIEWI"), "Sas.Camera.WorldToViewInverse");
      this.BindBySasAddress(this.FindBySemantic("VIEWINVERSE"), "Sas.Camera.WorldToViewInverse");
      this.BindBySasAddress(this.FindBySemantic("VIEWT"), "Sas.Camera.WorldToViewTranspose");
      this.BindBySasAddress(this.FindBySemantic("VIEWTRANSPOSE"), "Sas.Camera.WorldToViewTranspose");
      this.BindBySasAddress(this.FindBySemantic("VIEWIT"), "Sas.Camera.WorldToViewInverseTranspose");
      this.BindBySasAddress(this.FindBySemantic("VIEWINVERSETRANSPOSE"), "Sas.Camera.WorldToViewInverseTranspose");
      this.BindBySasAddress(this.FindBySemantic("PROJECTION"), "Sas.Camera.Projection");
      this.BindBySasAddress(this.FindBySemantic("PROJECTIONI"), "Sas.Camera.ProjectionInverse");
      this.BindBySasAddress(this.FindBySemantic("PROJECTIONINVERSE"), "Sas.Camera.ProjectionInverse");
      this.BindBySasAddress(this.FindBySemantic("PROJECTIONT"), "Sas.Camera.ProjectionTranspose");
      this.BindBySasAddress(this.FindBySemantic("PROJECTIONTRANSPOSE"), "Sas.Camera.ProjectionTranspose");
      this.BindBySasAddress(this.FindBySemantic("PROJECTIONIT"), "Sas.Camera.ProjectionInverseTranspose");
      this.BindBySasAddress(this.FindBySemantic("PROJECTIONINVERSETRANSPOSE"), "Sas.Camera.ProjectionInverseTranspose");
    }

    /// <summary>Finds parameter by shader variable name.</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected EffectParameter FindByName(string name)
    {
      return this.Parameters[name];
    }

    /// <summary>Finds parameter by shader variable semantic.</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected EffectParameter FindBySemantic(string name)
    {
      for (int index = 0; index < this.Parameters.Count; ++index)
      {
        EffectParameter parameter = this.Parameters[index];
        string semantic = parameter.Semantic;
        if (!string.IsNullOrEmpty(semantic) && string.Compare(semantic, name, true) == 0)
          return parameter;
      }
      return null;
    }

    /// <summary>Finds parameter by shader variable bind address.</summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected EffectParameter FindBySasAddress(string name)
    {
      for (int index = 0; index < this.Parameters.Count; ++index)
      {
        EffectParameter parameter = this.Parameters[index];
        EffectAnnotation annotation = parameter.Annotations["SasBindAddress"];
        if (annotation != null)
        {
          string valueString = annotation.GetValueString();
          if (!string.IsNullOrEmpty(valueString) && valueString == name)
            return parameter;
        }
      }
      return null;
    }

    /// <summary>
    /// Binds parameter to a specific string address. Generally used to remap
    /// non standard semantics to standard bind addresses.
    /// </summary>
    /// <param name="parameter"></param>
    /// <param name="address"></param>
    protected void BindBySasAddress(EffectParameter parameter, string address)
    {
      if (parameter == null)
        return;
      this.SasAutoBindTable.method_0(address, parameter);
    }

    /// <summary>
    /// Binds all parameters containing a bind address that starts
    /// with the partial address, to the partial address.
    /// </summary>
    /// <param name="partialaddress"></param>
    protected void BindAllByPartialSasAddress(string partialaddress)
    {
      for (int index = 0; index < this.Parameters.Count; ++index)
      {
        EffectParameter parameter = this.Parameters[index];
        EffectAnnotation annotation = parameter.Annotations["SasBindAddress"];
        if (annotation != null)
        {
          string valueString = annotation.GetValueString();
          if (!string.IsNullOrEmpty(valueString) && valueString.Length >= partialaddress.Length && !(partialaddress != valueString.Substring(0, partialaddress.Length)))
          {
            if (parameter.Elements.Count > 0)
              this.method_7(parameter, valueString);
            else if (parameter.StructureMembers.Count > 0)
              this.method_8(parameter, valueString);
            else
              this.BindBySasAddress(parameter, valueString);
          }
        }
      }
    }

    private void method_7(EffectParameter effectParameter_0, string string_0)
    {
      for (int index = 0; index < effectParameter_0.Elements.Count; ++index)
      {
        EffectParameter element = effectParameter_0.Elements[index];
        string str = string_0.Replace("*", index.ToString());
        if (element.StructureMembers.Count > 0)
          this.method_8(element, str);
        else
          this.BindBySasAddress(element, str);
      }
    }

    private void method_8(EffectParameter effectParameter_0, string string_0)
    {
      for (int index = 0; index < effectParameter_0.StructureMembers.Count; ++index)
        this.BindBySasAddress(effectParameter_0.StructureMembers[index], string_0 + "." + effectParameter_0.StructureMembers[index].Name);
    }

    /// <summary>
    /// Applies the current game time information to the bound effect time parameters.
    /// </summary>
    protected void SyncTimeEffectData()
    {
      EffectHelper.Update(this.SasAutoBindTable.method_1("Sas.Time.Now"), new Vector4(Now*1000));
      EffectHelper.Update(this.SasAutoBindTable.method_1("Sas.Time.Last"), new Vector4(Last*1000));
      EffectHelper.Update(this.SasAutoBindTable.method_1("Sas.Time.FrameNumber"), new Vector4(FrameNumber));
    }

    protected class GClass0
    {
      private Dictionary<string, List<EffectParameter>> dictionary_0 = new Dictionary<string, List<EffectParameter>>(128);

      public void method_0(string string_0, EffectParameter effectParameter_0)
      {
        if (!this.dictionary_0.ContainsKey(string_0))
          this.dictionary_0.Add(string_0, new List<EffectParameter>(8)
          {
            effectParameter_0
          });
        else
          this.dictionary_0[string_0].Add(effectParameter_0);
      }

      public List<EffectParameter> method_1(string string_0)
      {
        if (!this.dictionary_0.ContainsKey(string_0))
          return null;
        return this.dictionary_0[string_0];
      }

      public void method_2()
      {
        this.dictionary_0.Clear();
      }
    }
  }
}
