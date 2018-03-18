// Decompiled with JetBrains decompiler
// Type: ns3.Class28
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Runtime.Serialization;

namespace ns3
{
    internal static class Serialize
    {
        public static void GetValue<T>(this SerializationInfo info, string key, out T outValue)
        {
            try
            {
                outValue = (T) info.GetValue(key, typeof (T));
            }
            catch
            {
                outValue = default (T);
            }
        }

        public static void GetEnum<T>(this SerializationInfo info, string key, out T outEnum)
        {
            try
            {
                string str = (string) info.GetValue(key, typeof (string));
                outEnum = (T) Enum.Parse(typeof (T), str, false);
            }
            catch
            {
                outEnum = default (T);
            }
        }
    }
}
