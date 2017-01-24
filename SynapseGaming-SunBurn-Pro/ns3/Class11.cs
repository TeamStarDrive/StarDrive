// Decompiled with JetBrains decompiler
// Type: ns3.Class11
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ns3
{
  internal class Class11
  {
    public const string string_0 = "Locale";
    public const string string_1 = "EffectFile";
    public const string string_2 = "Technique";
    public const string string_3 = "DepthTechnique";
    public const string string_4 = "GBufferTechnique";
    public const string string_5 = "FinalTechnique";
    public const string string_6 = "ShadowGenerationTechnique";
    public const string string_7 = "Invariant";
    public const string string_8 = "AffectsRenderStates";
    public const string string_9 = "DoubleSided";
    public const string string_10 = "TransparencyMode";
    public const string string_11 = "Transparency";
    public const string string_12 = "TransparencyMapParameterName";
    public const string string_13 = "SOFTWARE\\Microsoft\\.NETFramework\\v2.0.50727\\AssemblyFoldersEx\\Synapse Gaming - SunBurn {0} {1}";
    public const string string_14 = "\\Development\\Windows";
    public const string string_15 = "\\ShaderLibrary";

    public static object smethod_0(Type type_0, string string_16, CultureInfo cultureInfo_0)
    {
      if (type_0 == typeof (string))
        return (object) string_16;
      if (type_0 == typeof (bool))
        return (object) Class11.smethod_2(string_16);
      if (type_0 == typeof (float))
        return (object) Class11.smethod_3(string_16, cultureInfo_0);
      if (type_0 == typeof (Vector4))
        return (object) Class11.smethod_4(string_16, cultureInfo_0, Vector4.Zero);
      return (object) null;
    }

    public static T smethod_1<T>(string string_16)
    {
      try
      {
        return (T) Enum.Parse(typeof (T), string_16, true);
      }
      catch
      {
        throw new Exception("Invalid property value '" + string_16 + "'.");
      }
    }

    public static bool smethod_2(string string_16)
    {
      return bool.Parse(string_16);
    }

    public static float smethod_3(string string_16, CultureInfo cultureInfo_0)
    {
      return float.Parse(string_16, (IFormatProvider) cultureInfo_0.NumberFormat);
    }

    public static Vector4 smethod_4(string string_16, CultureInfo cultureInfo_0, Vector4 vector4_0)
    {
      string[] strArray = Regex.Split(string_16, " ");
      if (strArray.Length < 3 || strArray.Length > 4)
        throw new Exception("Invalid vector data.");
      vector4_0.X = Class11.smethod_3(strArray[0], cultureInfo_0);
      vector4_0.Y = Class11.smethod_3(strArray[1], cultureInfo_0);
      vector4_0.Z = Class11.smethod_3(strArray[2], cultureInfo_0);
      if (strArray.Length > 3)
        vector4_0.W = Class11.smethod_3(strArray[3], cultureInfo_0);
      return vector4_0;
    }
  }
}
