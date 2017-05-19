// Decompiled with JetBrains decompiler
// Type: ns1.Class6
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace ns1
{
  internal class Class6
  {
    private RSACryptoServiceProvider rsacryptoServiceProvider_0 = new RSACryptoServiceProvider();
    private RSACryptoServiceProvider rsacryptoServiceProvider_1 = new RSACryptoServiceProvider();
    private SHA1Managed sha1Managed_0 = new SHA1Managed();
    private List<byte[]> list_0 = new List<byte[]>();

    public Class6(byte[] localprivatekey, byte[] remotepublickey)
    {
      this.rsacryptoServiceProvider_1.PersistKeyInCsp = false;
      this.rsacryptoServiceProvider_1.ImportCspBlob(localprivatekey);
      this.rsacryptoServiceProvider_0.PersistKeyInCsp = false;
      this.rsacryptoServiceProvider_0.ImportCspBlob(remotepublickey);
    }

    public byte[] method_0(byte[] byte_0)
    {
      byte[] buffer = this.method_2(byte_0);
      byte[] numArray1 = this.rsacryptoServiceProvider_1.SignHash(this.sha1Managed_0.ComputeHash(buffer), CryptoConfig.MapNameToOID("SHA1"));
      byte[] bytes = BitConverter.GetBytes(numArray1.Length);
      byte[] numArray2 = new byte[buffer.Length + numArray1.Length + bytes.Length];
      bytes.CopyTo(numArray2, 0);
      numArray1.CopyTo(numArray2, bytes.Length);
      buffer.CopyTo(numArray2, bytes.Length + numArray1.Length);
      return numArray2;
    }

    public byte[] method_1(byte[] byte_0)
    {
      int int32 = BitConverter.ToInt32(byte_0, 0);
      byte[] rgbSignature = new byte[int32];
      byte[] numArray = new byte[byte_0.Length - (int32 + 4)];
      Array.Copy(byte_0, 4, rgbSignature, 0, rgbSignature.Length);
      Array.Copy(byte_0, int32 + 4, numArray, 0, numArray.Length);
      if (!this.rsacryptoServiceProvider_0.VerifyHash(this.sha1Managed_0.ComputeHash(numArray), CryptoConfig.MapNameToOID("SHA1"), rgbSignature))
        return null;
      return this.method_3(numArray);
    }

    private byte[] method_2(byte[] byte_0)
    {
      int val1 = this.rsacryptoServiceProvider_0.KeySize / 8 - 42;
      byte[] numArray1 = new byte[val1];
      int sourceIndex = 0;
      int length1 = 0;
      this.list_0.Clear();
      while (sourceIndex < byte_0.Length)
      {
        int length2 = Math.Min(val1, byte_0.Length - sourceIndex);
        byte[] rgb = numArray1;
        if (length2 < rgb.Length)
          rgb = new byte[length2];
        Array.Copy(byte_0, sourceIndex, rgb, 0, length2);
        byte[] numArray2 = this.rsacryptoServiceProvider_0.Encrypt(rgb, true);
        sourceIndex += length2;
        length1 += numArray2.Length;
        this.list_0.Add(numArray2);
      }
      byte[] numArray3 = new byte[length1];
      int index = 0;
      foreach (byte[] numArray2 in this.list_0)
      {
        numArray2.CopyTo(numArray3, index);
        index += numArray2.Length;
      }
      return numArray3;
    }

    private byte[] method_3(byte[] byte_0)
    {
      int val1 = this.rsacryptoServiceProvider_1.KeySize / 8;
      byte[] numArray1 = new byte[val1];
      int sourceIndex = 0;
      int length1 = 0;
      this.list_0.Clear();
      while (sourceIndex < byte_0.Length)
      {
        int length2 = Math.Min(val1, byte_0.Length - sourceIndex);
        byte[] rgb = numArray1;
        if (length2 < rgb.Length)
          rgb = new byte[length2];
        Array.Copy(byte_0, sourceIndex, rgb, 0, length2);
        byte[] numArray2 = this.rsacryptoServiceProvider_1.Decrypt(rgb, true);
        sourceIndex += length2;
        length1 += numArray2.Length;
        this.list_0.Add(numArray2);
      }
      byte[] numArray3 = new byte[length1];
      int index = 0;
      foreach (byte[] numArray2 in this.list_0)
      {
        numArray2.CopyTo(numArray3, index);
        index += numArray2.Length;
      }
      return numArray3;
    }

    public static void smethod_0(int int_0, string string_0, string string_1)
    {
      RSACryptoServiceProvider cryptoServiceProvider = new RSACryptoServiceProvider(int_0);
      cryptoServiceProvider.Encrypt(new byte[1], true);
      FileStream fileStream1 = File.Create(string_1);
      fileStream1.Position = 0L;
      byte[] buffer1 = cryptoServiceProvider.ExportCspBlob(true);
      fileStream1.Write(buffer1, 0, buffer1.Length);
      fileStream1.Close();
      FileStream fileStream2 = File.Create(string_0);
      fileStream2.Position = 0L;
      byte[] buffer2 = cryptoServiceProvider.ExportCspBlob(false);
      fileStream2.Write(buffer2, 0, buffer2.Length);
      fileStream2.Close();
    }
  }
}
