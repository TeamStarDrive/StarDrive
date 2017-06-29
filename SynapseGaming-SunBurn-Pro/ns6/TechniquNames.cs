// Decompiled with JetBrains decompiler
// Type: ns6.Class48
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;

namespace EmbeddedResources
{
  internal class TechniquNames
  {
    private static Dictionary<int, string> dictionary_0 = new Dictionary<int, string>(32);

    private static int smethod_0(Enum3 enum3_0, Enum4 enum4_0, int int_0, bool bool_0, bool bool_1, bool bool_2, bool bool_3)
    {
      int num = (int) (enum3_0 + ((int) enum4_0 << 8) + (int_0 << 16));
      if (bool_2)
        num += 16777216;
      if (bool_0)
        num += 33554432;
      if (bool_1)
        num += 67108864;
      if (bool_3)
        num += 134217728;
      return num;
    }

    public static void ValidateModes()
    {
      Dictionary<int, char> dictionary = new Dictionary<int, char>(16);
      for (int index1 = 0; index1 < 8; ++index1)
      {
        for (int index2 = 0; index2 < 30; ++index2)
        {
          for (int int_0 = 0; int_0 < 3; ++int_0)
          {
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, false, false, false, false), '0');
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, true, false, false, false), '0');
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, false, true, false, false), '0');
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, true, true, false, false), '0');
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, false, false, true, false), '0');
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, true, false, true, false), '0');
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, false, true, true, false), '0');
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, true, true, true, false), '0');
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, false, false, false, true), '0');
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, true, false, false, true), '0');
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, false, true, false, true), '0');
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, true, true, false, true), '0');
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, false, false, true, true), '0');
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, true, false, true, true), '0');
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, false, true, true, true), '0');
            dictionary.Add(smethod_0((Enum3) index1, (Enum4) index2, int_0, true, true, true, true), '0');
          }
        }
      }
    }

    public static string Get(Enum3 enum3_0, Enum4 enum4_0, int int_0, bool bool_0, bool bool_1, bool bool_2, bool bool_3)
    {
      int key = smethod_0(enum3_0, enum4_0, int_0, bool_0, bool_1, bool_2, bool_3);
      if (dictionary_0.ContainsKey(key))
        return dictionary_0[key];
      string str1 = "";
      if (enum3_0 == Enum3.DeferredDepth)
        str1 = "DeferredDepth_";
      else if (enum3_0 == Enum3.DeferredGBuffer)
        str1 = "DeferredGBuffer_";
      else if (enum3_0 == Enum3.DeferredFinal)
        str1 = "DeferredFinal_";
      else if (enum3_0 == Enum3.DeferredFinalFog)
        str1 = "DeferredFinalFog_";
      else if (enum3_0 == Enum3.Lighting)
        str1 = "Lighting_";
      else if (enum3_0 == Enum3.Shadow)
        str1 = "Shadow_";
      else if (enum3_0 == Enum3.ShadowGen)
        str1 = "ShadowGen_";
      else if (enum3_0 == Enum3.Fog)
        str1 = "Fog_";
      else if (enum3_0 == Enum3.Billboard)
        str1 = "Billboard_";
      if (enum4_0 == Enum4.Diffuse)
        str1 += "D_";
      else if (enum4_0 == Enum4.DiffuseBump)
        str1 += "DB_";
      else if (enum4_0 == Enum4.DiffuseBumpSpecular)
        str1 += "DBS_";
      else if (enum4_0 == Enum4.DiffuseBumpSpecularColor)
        str1 += "DBSC_";
      else if (enum4_0 == Enum4.DiffuseBumpFresnel)
        str1 += "DBF_";
      else if (enum4_0 == Enum4.DiffuseBumpFresnelColor)
        str1 += "DBFC_";
      else if (enum4_0 == Enum4.DiffuseParallax)
        str1 += "DP_";
      else if (enum4_0 == Enum4.DiffuseParallaxSpecular)
        str1 += "DPS_";
      else if (enum4_0 == Enum4.DiffuseParallaxSpecularColor)
        str1 += "DPSC_";
      else if (enum4_0 == Enum4.DiffuseParallaxFresnel)
        str1 += "DPF_";
      else if (enum4_0 == Enum4.DiffuseParallaxFresnelColor)
        str1 += "DPFC_";
      else if (enum4_0 == Enum4.DiffuseAmbient)
        str1 += "DA_";
      else if (enum4_0 == Enum4.DiffuseAmbientCustom)
        str1 += "DA2_";
      else if (enum4_0 == Enum4.DiffuseBumpAmbient)
        str1 += "DBA_";
      else if (enum4_0 == Enum4.DiffuseParallaxAmbient)
        str1 += "DPA_";
      else if (enum4_0 == Enum4.DiffuseAmbientEmissive)
        str1 += "DAG_";
      else if (enum4_0 == Enum4.DiffuseBumpAmbientEmissive)
        str1 += "DBAG_";
      else if (enum4_0 == Enum4.DiffuseParallaxAmbientEmissive)
        str1 += "DPAG_";
      else if (enum4_0 == Enum4.DiffuseParallaxSpecularColorEmissive)
        str1 += "DPSCE_";
      else if (enum4_0 == Enum4.DiffuseParallaxEmissive)
        str1 += "DPE_";
      else if (enum4_0 == Enum4.DiffuseBumpSpecularColorEmissive)
        str1 += "DBSCE_";
      else if (enum4_0 == Enum4.DiffuseBumpEmissive)
        str1 += "DBE_";
      else if (enum4_0 == Enum4.Linear)
        str1 += "Linear_";
      else if (enum4_0 == Enum4.Point)
        str1 += "Point_";
      else if (enum4_0 == Enum4.Point3)
        str1 += "Point3_";
      else if (enum4_0 == Enum4.Point4)
        str1 += "Point4_";
      else if (enum4_0 == Enum4.Directional)
        str1 += "Directional_";
      else if (enum4_0 == Enum4.Directional3)
        str1 += "Directional3_";
      else if (enum4_0 == Enum4.Directional4)
        str1 += "Directional4_";
      if (enum3_0 == Enum3.Lighting)
        str1 = str1 + "L" + int_0 + "_";
      if (bool_0)
        str1 += "Double_";
      if (bool_1)
        str1 += "Transparent_";
      if (bool_2)
        str1 += "Skinned_";
      if (bool_3)
        str1 += "Terrain_";
      string str2 = str1 + "Technique";
      dictionary_0.Add(key, str2);
      return str2;
    }

    public enum Enum3
    {
      DeferredDepth,
      DeferredGBuffer,
      DeferredFinal,
      DeferredFinalFog,
      Lighting,
      Shadow,
      ShadowGen,
      Fog,
      Billboard
    }

    public enum Enum4
    {
      None,
      Diffuse,
      DiffuseBump,
      DiffuseBumpSpecular,
      DiffuseBumpSpecularColor,
      DiffuseBumpFresnel,
      DiffuseBumpFresnelColor,
      DiffuseParallax,
      DiffuseParallaxSpecular,
      DiffuseParallaxSpecularColor,
      DiffuseParallaxFresnel,
      DiffuseParallaxFresnelColor,
      DiffuseAmbient,
      DiffuseAmbientCustom,
      DiffuseBumpAmbient,
      DiffuseParallaxAmbient,
      DiffuseAmbientEmissive,
      DiffuseBumpAmbientEmissive,
      DiffuseParallaxAmbientEmissive,
      DiffuseParallaxSpecularColorEmissive,
      DiffuseParallaxEmissive,
      DiffuseBumpSpecularColorEmissive,
      DiffuseBumpEmissive,
      Linear,
      Point,
      Directional,
      Point3,
      Directional3,
      Point4,
      Directional4,
      Count
    }
  }
}
