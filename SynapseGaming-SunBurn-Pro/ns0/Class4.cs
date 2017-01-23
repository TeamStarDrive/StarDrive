// Decompiled with JetBrains decompiler
// Type: ns0.Class4
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using ns1;
using ns2;
using SynapseGaming.DRM.ApertureClient.Production;
using System;
using System.IO;

namespace ns0
{
  internal class Class4
  {
    private static byte[] byte_0 = new byte[596]{ (byte) 7, (byte) 2, (byte) 0, (byte) 0, (byte) 0, (byte) 164, (byte) 0, (byte) 0, (byte) 82, (byte) 83, (byte) 65, (byte) 50, (byte) 0, (byte) 4, (byte) 0, (byte) 0, (byte) 1, (byte) 0, (byte) 1, (byte) 0, (byte) 125, (byte) 109, (byte) 98, (byte) 29, (byte) 61, (byte) 54, (byte) 25, (byte) 149, (byte) 63, (byte) 147, (byte) 210, (byte) 190, (byte) 131, (byte) 218, (byte) 2, (byte) 19, (byte) 208, (byte) 83, (byte) 145, (byte) 134, (byte) 203, (byte) 38, (byte) 82, (byte) 51, (byte) 0, (byte) 217, (byte) 18, (byte) 0, (byte) 78, (byte) 85, (byte) 17, (byte) 58, (byte) 210, (byte) 79, (byte) 196, (byte) 64, (byte) 252, (byte) 60, (byte) 104, (byte) 202, (byte) 100, (byte) 5, (byte) 133, (byte) 115, (byte) 59, (byte) 144, (byte) 26, (byte) 238, (byte) 183, (byte) 249, (byte) 248, (byte) 111, (byte) 67, (byte) 67, (byte) 74, (byte) 252, (byte) 21, (byte) 161, (byte) 175, (byte) 162, (byte) 75, (byte) 84, (byte) 8, (byte) 243, (byte) 74, (byte) 191, (byte) 103, (byte) 202, (byte) 242, (byte) 83, (byte) 187, (byte) 150, (byte) 43, (byte) 128, (byte) 132, (byte) 215, (byte) 68, (byte) 225, (byte) 203, (byte) 191, (byte) 243, (byte) 196, (byte) 101, (byte) 250, (byte) 241, (byte) 163, (byte) 3, (byte) 236, (byte) 6, (byte) 214, (byte) 122, (byte) 191, (byte) 181, (byte) 112, (byte) 64, (byte) 149, (byte) 64, (byte) 35, (byte) 9, (byte) 1, (byte) 32, (byte) 22, (byte) 140, (byte) 164, (byte) 231, (byte) 31, (byte) 135, (byte) 40, (byte) 239, (byte) 200, (byte) 26, (byte) 152, (byte) 125, (byte) 253, (byte) 82, (byte) 227, (byte) 43, (byte) 12, (byte) 121, (byte) 104, (byte) 192, (byte) 89, (byte) 122, (byte) 81, (byte) 78, (byte) 134, (byte) 95, (byte) 169, (byte) 29, (byte) 9, (byte) 167, (byte) 147, (byte) 64, (byte) 126, (byte) 67, (byte) 60, (byte) 7, (byte) 126, (byte) 60, (byte) 2, (byte) 90, (byte) 180, (byte) 79, (byte) 185, (byte) 217, (byte) 239, (byte) 19, (byte) 137, (byte) 231, (byte) 169, (byte) 28, (byte) 213, (byte) 36, (byte) 133, (byte) 167, (byte) 1, (byte) 39, (byte) 222, (byte) 144, (byte) 210, (byte) 2, (byte) 47, (byte) 115, (byte) 224, (byte) 133, (byte) 43, (byte) 9, (byte) 117, (byte) 253, (byte) 126, (byte) 244, (byte) 84, (byte) 198, (byte) 129, (byte) 82, (byte) 96, (byte) 240, (byte) 226, (byte) 125, (byte) 79, (byte) 218, (byte) 139, (byte) 161, (byte) 189, (byte) 66, (byte) 138, (byte) 65, (byte) 38, (byte) 89, (byte) 77, (byte) 86, (byte) 208, (byte) 225, (byte) 39, (byte) 240, (byte) 162, (byte) 32, (byte) 169, (byte) 156, (byte) 228, (byte) 195, (byte) 83, (byte) 174, (byte) 66, (byte) 30, (byte) 135, (byte) 125, (byte) 119, (byte) 78, (byte) 61, (byte) 169, (byte) 155, (byte) 127, (byte) 245, (byte) 14, (byte) 212, (byte) 146, (byte) 128, (byte) 251, (byte) 218, (byte) 38, (byte) 187, (byte) 152, (byte) 58, (byte) 139, (byte) 116, (byte) 160, (byte) 47, (byte) 55, (byte) 20, (byte) 227, (byte) 10, (byte) 56, (byte) 196, (byte) 110, (byte) 86, (byte) 163, (byte) 238, (byte) 3, (byte) 179, (byte) 213, (byte) 56, (byte) 212, (byte) 239, (byte) 132, (byte) 132, (byte) 49, (byte) 100, (byte) 38, (byte) 249, (byte) 229, (byte) 156, (byte) 78, (byte) 55, (byte) 31, (byte) 208, (byte) 237, (byte) 198, (byte) 198, (byte) 194, (byte) 117, (byte) 192, (byte) 160, (byte) 84, (byte) 185, (byte) 251, (byte) 212, (byte) 202, (byte) 0, (byte) 145, (byte) 165, (byte) 12, (byte) 231, (byte) 83, (byte) 229, (byte) 23, (byte) 131, (byte) 168, (byte) 132, (byte) 33, (byte) 221, (byte) 121, (byte) 99, (byte) 2, (byte) 25, (byte) 126, (byte) 89, (byte) 68, (byte) 248, (byte) 228, (byte) 80, (byte) 254, (byte) 121, (byte) 161, (byte) 223, (byte) 203, (byte) 73, (byte) 49, (byte) 213, (byte) 217, (byte) 6, (byte) 58, (byte) 194, (byte) 163, (byte) 153, (byte) 234, (byte) 91, (byte) 50, (byte) 194, (byte) 188, (byte) 123, (byte) 107, (byte) 68, (byte) 179, (byte) 187, (byte) 144, (byte) 119, (byte) 61, (byte) 244, (byte) 47, (byte) 225, (byte) 44, (byte) 33, (byte) 62, (byte) 212, (byte) 54, (byte) 212, (byte) 58, (byte) 0, (byte) 102, (byte) 156, (byte) 211, (byte) 68, (byte) 12, (byte) 228, (byte) 233, (byte) 214, (byte) 251, (byte) 224, (byte) 232, (byte) 53, (byte) 102, (byte) 183, (byte) 101, (byte) 77, (byte) 253, (byte) 206, (byte) 79, (byte) 204, (byte) 3, (byte) 214, (byte) 105, (byte) 113, (byte) 42, (byte) 156, (byte) 170, (byte) 84, (byte) 54, (byte) 20, (byte) 170, (byte) 118, (byte) 9, (byte) 75, (byte) 1, (byte) 249, (byte) 221, (byte) 138, (byte) 199, (byte) 195, (byte) 42, (byte) 22, (byte) 206, (byte) 168, (byte) 171, (byte) 38, (byte) 209, (byte) 92, (byte) 184, (byte) 132, (byte) 15, (byte) 244, (byte) 219, (byte) 86, (byte) 154, (byte) 93, (byte) 165, (byte) 108, (byte) 133, (byte) 93, (byte) 152, (byte) 6, (byte) 170, (byte) 16, (byte) 155, (byte) 9, (byte) 210, (byte) 224, (byte) 144, (byte) 139, (byte) 222, (byte) 55, (byte) 133, (byte) 159, (byte) 250, (byte) 234, (byte) 200, (byte) 28, (byte) 165, (byte) 198, (byte) 209, (byte) 97, (byte) 3, (byte) 242, (byte) 147, (byte) 31, (byte) 136, (byte) 149, (byte) 253, (byte) 117, (byte) 120, (byte) 237, (byte) 15, (byte) 167, (byte) 89, (byte) 121, (byte) 208, (byte) 204, (byte) 80, (byte) 85, (byte) 30, (byte) 47, (byte) 161, (byte) 250, (byte) 60, (byte) 187, (byte) 54, (byte) 62, (byte) 79, (byte) 245, (byte) 185, (byte) 110, (byte) 171, (byte) 137, (byte) 27, (byte) 252, (byte) 65, (byte) 189, (byte) 169, (byte) 1, (byte) 47, (byte) 109, (byte) 140, (byte) 82, (byte) 134, (byte) 63, (byte) 10, (byte) 16, (byte) 74, (byte) 9, (byte) 215, (byte) 50, (byte) 247, (byte) 22, (byte) 66, (byte) 172, (byte) 41, (byte) 142, (byte) 196, (byte) 75, (byte) 226, (byte) 6, (byte) 196, (byte) 87, (byte) 26, (byte) 163, (byte) 238, (byte) 49, (byte) 98, (byte) 139, (byte) 199, (byte) 93, (byte) 158, (byte) 49, (byte) 10, (byte) 163, (byte) 75, (byte) 117, (byte) 39, (byte) 237, (byte) 189, (byte) 6, (byte) 156, (byte) 8, (byte) 159, (byte) 157, (byte) 174, (byte) 214, (byte) 110, (byte) 14, (byte) 200, (byte) 137, (byte) 83, (byte) 231, (byte) 205, (byte) 243, (byte) 156, (byte) 225, (byte) 193, (byte) 254, (byte) 167, (byte) 61, (byte) 235, (byte) 20, (byte) 13, (byte) 122, (byte) 90, (byte) 180, (byte) 242, (byte) 177, (byte) 113, (byte) 121, (byte) 107, (byte) 176, (byte) 156, (byte) 190, (byte) 153, (byte) 192, (byte) 232, (byte) 150, (byte) 143, (byte) 197, (byte) 249, (byte) 144, (byte) 127, (byte) 181, (byte) 80, (byte) 241, (byte) 156, (byte) 146, (byte) 178, (byte) 204, (byte) 185, (byte) 202, (byte) 59, (byte) 232, (byte) 6, (byte) 31, (byte) 14, (byte) 227, (byte) 159, (byte) 72, (byte) 66, (byte) 156, (byte) 107, (byte) 249, (byte) 113, (byte) 114, (byte) 25, (byte) 142, (byte) 227, (byte) 240, byte.MaxValue, (byte) 56, (byte) 89, (byte) 9, (byte) 63, (byte) 93, (byte) 156, (byte) 44, (byte) 246, (byte) 93, (byte) 182, (byte) 180, (byte) 162, (byte) 54, (byte) 42 };
    private static byte[] byte_1 = new byte[148]{ (byte) 6, (byte) 2, (byte) 0, (byte) 0, (byte) 0, (byte) 164, (byte) 0, (byte) 0, (byte) 82, (byte) 83, (byte) 65, (byte) 49, (byte) 0, (byte) 4, (byte) 0, (byte) 0, (byte) 1, (byte) 0, (byte) 1, (byte) 0, (byte) 137, (byte) 16, (byte) 179, (byte) 235, (byte) 143, (byte) 151, (byte) 177, (byte) 248, (byte) 171, (byte) 249, (byte) 190, (byte) 6, (byte) 55, (byte) 35, (byte) 219, (byte) 161, (byte) 4, (byte) 151, (byte) 250, (byte) 70, (byte) 41, byte.MaxValue, (byte) 190, (byte) 150, (byte) 134, (byte) 80, (byte) 52, (byte) 234, (byte) 61, (byte) 198, (byte) 175, (byte) 162, (byte) 210, (byte) 76, (byte) 123, (byte) 78, (byte) 253, (byte) 195, (byte) 125, (byte) 5, (byte) 147, (byte) 80, (byte) 228, (byte) 71, (byte) 137, (byte) 91, (byte) 225, (byte) 148, (byte) 6, (byte) 201, (byte) 4, (byte) 182, (byte) 135, (byte) 88, (byte) 180, (byte) 128, (byte) 13, (byte) 33, (byte) 235, (byte) 80, (byte) 46, (byte) 245, (byte) 81, (byte) 184, (byte) 78, (byte) 162, (byte) 48, (byte) 79, (byte) 9, (byte) 172, (byte) 198, (byte) 45, (byte) 61, (byte) 101, (byte) 247, (byte) 14, (byte) 77, (byte) 11, (byte) 192, (byte) 32, (byte) 49, (byte) 183, (byte) 54, (byte) 198, (byte) 15, (byte) 175, (byte) 7, (byte) 53, (byte) 150, (byte) 189, (byte) 93, (byte) 161, (byte) 73, (byte) 191, (byte) 32, (byte) 27, (byte) 150, (byte) 139, (byte) 166, (byte) 230, (byte) 116, (byte) 6, (byte) 207, (byte) 164, (byte) 225, (byte) 6, (byte) 117, (byte) 204, (byte) 231, (byte) 254, (byte) 134, (byte) 222, (byte) 212, (byte) 102, (byte) 97, (byte) 227, (byte) 38, (byte) 133, (byte) 158, (byte) 66, (byte) 141, (byte) 234, (byte) 50, (byte) 102, (byte) 139, (byte) 184, (byte) 198, (byte) 212 };
    private char[] char_0 = new char[1]{ '-' };
    private Class6 class6_0;

    public Class4()
    {
      this.class6_0 = new Class6(Class4.byte_0, Class4.byte_1);
    }

    public Class4.Enum1 method_0(string string_0, string string_1, string string_2, uint uint_0)
    {
      try
      {
        string path1 = Class2.smethod_0();
        Directory.CreateDirectory(path1);
        string path2 = path1 + string_0;
        byte[] authorizationFile = new ApertureAuthorityService().GenerateAuthorizationFile(this.method_3(string_1, string_2, uint_0));
        if (authorizationFile != null && authorizationFile.Length >= 1)
        {
          FileStream fileStream = File.Create(path2);
          fileStream.Write(authorizationFile, 0, authorizationFile.Length);
          fileStream.Flush();
          fileStream.Close();
          return Class4.Enum1.Success;
        }
        File.Delete(path2);
        return Class4.Enum1.InvalidCode;
      }
      catch
      {
        return Class4.Enum1.HostUnreachable;
      }
    }

    public bool method_1(string string_0, string string_1, uint uint_0)
    {
      TimeSpan timeSpan_0;
      if (this.method_4(string_0, string_1, uint_0, out timeSpan_0))
        return true;
      this.method_2(string_0, string_1);
      return this.method_4(string_0, string_1, uint_0, out timeSpan_0) || timeSpan_0.TotalDays > 0.0 && timeSpan_0.TotalDays < 45.0;
    }

    private void method_2(string string_0, string string_1)
    {
      try
      {
        string string_1_1;
        uint uint_0;
        uint uint_1;
        DateTime dateTime_0;
        this.method_6(string_0, out string_1_1, out uint_0, out uint_1, out dateTime_0);
        int num = (int) this.method_0(string_0, string_1_1, string_1, uint_1);
      }
      catch
      {
      }
    }

    private string method_3(string string_0, string string_1, uint uint_0)
    {
      return new Class7(this.class6_0).method_2(string_0, (uint) Class4.smethod_0(string_1), uint_0, DateTime.Now);
    }

    private bool method_4(string string_0, string string_1, uint uint_0, out TimeSpan timeSpan_0)
    {
      timeSpan_0 = TimeSpan.MaxValue;
      try
      {
        string string_1_1;
        uint uint_0_1;
        uint uint_1;
        DateTime dateTime_0;
        this.method_6(string_0, out string_1_1, out uint_0_1, out uint_1, out dateTime_0);
        if (string_1_1 == "" || !this.method_5(uint_0_1, uint_1, dateTime_0, string_1, uint_0, ref timeSpan_0))
          return false;
        string[] strArray = string_1_1.Split(this.char_0);
        Class0.string_0 = strArray == null || strArray.Length < 5 ? "-unlicensed-" : "****-" + strArray[4];
        return true;
      }
      catch
      {
        return false;
      }
    }

    private bool method_5(uint uint_0, uint uint_1, DateTime dateTime_0, string string_0, uint uint_2, ref TimeSpan timeSpan_0)
    {
      if ((int) uint_1 != (int) uint_2 || (int) uint_1 == 0 || (int) uint_2 == 0)
        return false;
      uint num = (uint) Class4.smethod_0(string_0);
      if ((int) uint_0 != (int) num || (int) uint_0 == 0 || string_0 == "")
        return false;
      TimeSpan timeSpan = DateTime.Now - dateTime_0;
      if (timeSpan.TotalDays > 30.0 || timeSpan.TotalDays < -2.0)
        return false;
      timeSpan_0 = timeSpan;
      return true;
    }

    internal static int smethod_0(string string_0)
    {
      int num1 = 352654597;
      int num2 = 352654597;
      int length = string_0.Length;
      int index = 0;
      while (index < string_0.Length)
      {
        char ch = string_0[index];
        char minValue = char.MinValue;
        if (index < string_0.Length - 1)
          minValue = string_0[index + 1];
        int num3 = (int) ch | (int) minValue << 16;
        if (index % 4 == 0)
          num1 = (num1 << 5) + num1 + (num1 >> 27) ^ num3;
        else
          num2 = (num2 << 5) + num2 + (num2 >> 27) ^ num3;
        index += 2;
      }
      return num1 + num2 * 1566083941;
    }

    private void method_6(string string_0, out string string_1, out uint uint_0, out uint uint_1, out DateTime dateTime_0)
    {
      try
      {
        FileStream fileStream = File.OpenRead(Class2.smethod_0() + string_0);
        byte[] numArray = new byte[fileStream.Length];
        fileStream.Read(numArray, 0, numArray.Length);
        fileStream.Close();
        new Class7(this.class6_0).method_1(numArray, out string_1, out uint_0, out uint_1, out dateTime_0);
      }
      catch
      {
        string_1 = "";
        uint_0 = 0U;
        uint_1 = 0U;
        dateTime_0 = DateTime.MinValue;
      }
    }

    public enum Enum1
    {
      Success,
      InvalidCode,
      HostUnreachable,
    }
  }
}
