// Decompiled with JetBrains decompiler
// Type: ns3.Class17
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using System.IO;

namespace ns3
{
  internal class Class17
  {
    private static List<Struct0> list_0 = new List<Struct0>(32);

    public static void smethod_0(string string_0)
    {
      list_0.Add(new Struct0(string_0, -1f));
    }

    public static void smethod_1(string string_0, float float_0)
    {
      list_0.Add(new Struct0(string_0, float_0));
    }

    public static void smethod_2(string string_0)
    {
      string contents = "";
      foreach (Struct0 struct0 in list_0)
      {
        if (struct0.float_0 >= 0.0)
          contents = contents + struct0.string_0 + ": " + struct0.float_0 + "\r\n";
        else
          contents = contents + struct0.string_0 + "\r\n";
      }
      list_0.Clear();
      File.WriteAllText(string_0, contents);
    }

    private struct Struct0
    {
      public string string_0;
      public float float_0;

      public Struct0(string name, float value)
      {
        this.string_0 = name;
        this.float_0 = value;
      }
    }
  }
}
