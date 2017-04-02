// Decompiled with JetBrains decompiler
// Type: ns3.Class28
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Runtime.Serialization;

namespace ns3
{
  internal class Class28
  {
    public static void smethod_0<T>(ref T gparam_0, SerializationInfo serializationInfo_0, string string_0)
    {
      try
      {
        gparam_0 = (T) serializationInfo_0.GetValue(string_0, typeof (T));
      }
      catch
      {
        gparam_0 = default (T);
      }
    }

    public static void smethod_1<T>(ref T gparam_0, SerializationInfo serializationInfo_0, string string_0)
    {
      try
      {
        string str = (string) serializationInfo_0.GetValue(string_0, typeof (string));
        gparam_0 = (T) Enum.Parse(typeof (T), str, false);
      }
      catch
      {
        gparam_0 = default (T);
      }
    }
  }
}
