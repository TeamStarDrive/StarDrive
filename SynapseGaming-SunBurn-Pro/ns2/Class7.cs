// Decompiled with JetBrains decompiler
// Type: ns2.Class7
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using ns1;
using System;

namespace ns2
{
  internal class Class7
  {
    private Class6 class6_0;

    public Class7(Class6 crypto)
    {
      this.class6_0 = crypto;
    }

    public void method_0(string string_0, out string string_1, out uint uint_0, out uint uint_1, out DateTime dateTime_0)
    {
      this.method_1(Class8.smethod_1(string_0), out string_1, out uint_0, out uint_1, out dateTime_0);
    }

    public void method_1(byte[] byte_0, out string string_0, out uint uint_0, out uint uint_1, out DateTime dateTime_0)
    {
      byte[] numArray1 = this.class6_0.method_1(byte_0);
      if (numArray1.Length <= 8)
      {
        string_0 = "";
        uint_0 = 0U;
        uint_1 = 0U;
        dateTime_0 = DateTime.Now;
      }
      else
      {
        string_0 = "";
        byte[] numArray2 = new byte[2];
        for (int index = 0; index < numArray1.Length - 16; ++index)
        {
          numArray2[0] = numArray1[index];
          numArray2[1] = (byte) 0;
          string_0 += (string) (object) BitConverter.ToChar(numArray2, 0);
        }
        long int64 = BitConverter.ToInt64(numArray1, numArray1.Length - 16);
        dateTime_0 = DateTime.FromFileTimeUtc(int64).ToLocalTime();
        uint_0 = BitConverter.ToUInt32(numArray1, numArray1.Length - 8);
        uint_1 = BitConverter.ToUInt32(numArray1, numArray1.Length - 4);
      }
    }

    public string method_2(string string_0, uint uint_0, uint uint_1, DateTime dateTime_0)
    {
      return Class8.smethod_0(this.method_3(string_0, uint_0, uint_1, dateTime_0));
    }

    public byte[] method_3(string string_0, uint uint_0, uint uint_1, DateTime dateTime_0)
    {
      byte[] byte_0 = new byte[string_0.Length + 16];
      for (int index = 0; index < string_0.Length; ++index)
      {
        byte[] bytes = BitConverter.GetBytes(string_0[index]);
        byte_0[index] = bytes[0];
      }
      BitConverter.GetBytes(dateTime_0.ToFileTimeUtc()).CopyTo((Array) byte_0, byte_0.Length - 16);
      BitConverter.GetBytes(uint_0).CopyTo((Array) byte_0, byte_0.Length - 8);
      BitConverter.GetBytes(uint_1).CopyTo((Array) byte_0, byte_0.Length - 4);
      return this.class6_0.method_0(byte_0);
    }
  }
}
