// Decompiled with JetBrains decompiler
// Type: ns2.Class8
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Text;

namespace ns2
{
  internal class Class8
  {
    public static string smethod_0(byte[] byte_0)
    {
      int num1 = byte_0.Length % 4;
      byte[] numArray1 = byte_0;
      if (num1 > 0)
      {
        numArray1 = new byte[byte_0.Length + (4 - num1)];
        byte_0.CopyTo(numArray1, 0);
      }
      byte[] bytes = new byte[byte_0.Length / 4 * 7];
      int num2 = 0;
      int startIndex = 0;
      while (startIndex < numArray1.Length)
      {
        int int32 = BitConverter.ToInt32(numArray1, startIndex);
        byte[] numArray2 = bytes;
        int index1 = num2;
        int num3 = 1;
        int num4 = index1 + num3;
        int num5 = (byte) (int32 & 31);
        numArray2[index1] = (byte) num5;
        byte[] numArray3 = bytes;
        int index2 = num4;
        int num6 = 1;
        int num7 = index2 + num6;
        int num8 = (byte) (int32 >> 5 & 31);
        numArray3[index2] = (byte) num8;
        byte[] numArray4 = bytes;
        int index3 = num7;
        int num9 = 1;
        int num10 = index3 + num9;
        int num11 = (byte) (int32 >> 10 & 31);
        numArray4[index3] = (byte) num11;
        byte[] numArray5 = bytes;
        int index4 = num10;
        int num12 = 1;
        int num13 = index4 + num12;
        int num14 = (byte) (int32 >> 15 & 31);
        numArray5[index4] = (byte) num14;
        byte[] numArray6 = bytes;
        int index5 = num13;
        int num15 = 1;
        int num16 = index5 + num15;
        int num17 = (byte) (int32 >> 20 & 31);
        numArray6[index5] = (byte) num17;
        byte[] numArray7 = bytes;
        int index6 = num16;
        int num18 = 1;
        int num19 = index6 + num18;
        int num20 = (byte) (int32 >> 25 & 31);
        numArray7[index6] = (byte) num20;
        byte[] numArray8 = bytes;
        int index7 = num19;
        int num21 = 1;
        num2 = index7 + num21;
        int num22 = (byte) (int32 >> 30 & 31);
        numArray8[index7] = (byte) num22;
        startIndex += 4;
      }
      for (int index = 0; index < bytes.Length; ++index)
        bytes[index] = (int) bytes[index] >= 10 ? (byte) (bytes[index] + 55U) : (byte) (bytes[index] + 48U);
      return Encoding.ASCII.GetString(bytes, 0, bytes.Length);
    }

    public static byte[] smethod_1(string string_0)
    {
      byte[] bytes1 = new ASCIIEncoding().GetBytes(string_0);
      if (bytes1.Length % 7 > 0)
        return null;
      byte[] numArray1 = new byte[bytes1.Length / 7 * 4];
      int num1 = 0;
      for (int index = 0; index < bytes1.Length; ++index)
        bytes1[index] = (int) bytes1[index] >= 65 ? (byte) (bytes1[index] - 55U) : (byte) (bytes1[index] - 48U);
      int index1 = 0;
      while (index1 < bytes1.Length)
      {
        byte[] bytes2 = BitConverter.GetBytes(bytes1[index1] & 31 | (bytes1[index1 + 1] & 31) << 5 | (bytes1[index1 + 2] & 31) << 10 | (bytes1[index1 + 3] & 31) << 15 | (bytes1[index1 + 4] & 31) << 20 | (bytes1[index1 + 5] & 31) << 25 | (bytes1[index1 + 6] & 3) << 30);
        byte[] numArray2 = numArray1;
        int index2 = num1;
        int num2 = 1;
        int num3 = index2 + num2;
        int num4 = bytes2[0];
        numArray2[index2] = (byte) num4;
        byte[] numArray3 = numArray1;
        int index3 = num3;
        int num5 = 1;
        int num6 = index3 + num5;
        int num7 = bytes2[1];
        numArray3[index3] = (byte) num7;
        byte[] numArray4 = numArray1;
        int index4 = num6;
        int num8 = 1;
        int num9 = index4 + num8;
        int num10 = bytes2[2];
        numArray4[index4] = (byte) num10;
        byte[] numArray5 = numArray1;
        int index5 = num9;
        int num11 = 1;
        num1 = index5 + num11;
        int num12 = bytes2[3];
        numArray5[index5] = (byte) num12;
        index1 += 7;
      }
      return numArray1;
    }
  }
}
