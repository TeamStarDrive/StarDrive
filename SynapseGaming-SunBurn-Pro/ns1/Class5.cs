// Decompiled with JetBrains decompiler
// Type: ns1.Class5
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.IO;
using System.Security.Cryptography;

namespace ns1
{
  internal class Class5
  {
    private RijndaelManaged rijndaelManaged_0 = new RijndaelManaged();

    public Class5(byte[] keyfiledata)
    {
      int length = (int) keyfiledata[0];
      byte[] byte_0 = new byte[length];
      byte[] byte_1 = new byte[keyfiledata.Length - (length + 1)];
      Array.Copy((Array) keyfiledata, 1, (Array) byte_0, 0, byte_0.Length);
      Array.Copy((Array) keyfiledata, length + 1, (Array) byte_1, 0, byte_1.Length);
      this.method_0(byte_0, byte_1);
    }

    public Class5(byte[] key, byte[] byte_0)
    {
      this.method_0(key, byte_0);
    }

    private void method_0(byte[] byte_0, byte[] byte_1)
    {
      this.rijndaelManaged_0.Mode = CipherMode.CBC;
      this.rijndaelManaged_0.Key = byte_0;
      this.rijndaelManaged_0.IV = byte_1;
    }

    public byte[] method_1(byte[] byte_0)
    {
      ICryptoTransform encryptor = this.rijndaelManaged_0.CreateEncryptor();
      MemoryStream memoryStream = new MemoryStream();
      CryptoStream cryptoStream = new CryptoStream((Stream) memoryStream, encryptor, CryptoStreamMode.Write);
      cryptoStream.Write(byte_0, 0, byte_0.Length);
      cryptoStream.FlushFinalBlock();
      byte[] array = memoryStream.ToArray();
      cryptoStream.Close();
      memoryStream.Close();
      return array;
    }

    public byte[] method_2(byte[] byte_0)
    {
      ICryptoTransform decryptor = this.rijndaelManaged_0.CreateDecryptor();
      MemoryStream memoryStream = new MemoryStream(byte_0);
      CryptoStream cryptoStream = new CryptoStream((Stream) memoryStream, decryptor, CryptoStreamMode.Read);
      byte[] buffer = new byte[byte_0.Length];
      cryptoStream.Read(buffer, 0, buffer.Length);
      cryptoStream.Close();
      memoryStream.Close();
      return buffer;
    }

    public static void smethod_0(int int_0, string string_0)
    {
      RijndaelManaged rijndaelManaged = new RijndaelManaged();
      rijndaelManaged.KeySize = int_0;
      rijndaelManaged.GenerateKey();
      rijndaelManaged.GenerateIV();
      byte[] buffer = new byte[rijndaelManaged.Key.Length + rijndaelManaged.IV.Length + 1];
      buffer[0] = (byte) rijndaelManaged.Key.Length;
      rijndaelManaged.Key.CopyTo((Array) buffer, 1);
      rijndaelManaged.IV.CopyTo((Array) buffer, rijndaelManaged.Key.Length + 1);
      FileStream fileStream = File.Create(string_0);
      fileStream.Position = 0L;
      fileStream.Write(buffer, 0, buffer.Length);
      fileStream.Close();
    }
  }
}
