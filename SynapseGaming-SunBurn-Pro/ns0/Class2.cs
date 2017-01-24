// Decompiled with JetBrains decompiler
// Type: ns0.Class2
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;

namespace ns0
{
  internal class Class2
  {
    public static Class2.Class3[] class3_0 = new Class2.Class3[4]{ new Class2.Class3("SunBurn Indie", "SunBurn-Indie.auth"), new Class2.Class3("SunBurn Pro", "SunBurn-Pro.auth"), new Class2.Class3("SunBurn Community", "SunBurn-Community.auth"), new Class2.Class3("SunBurn Studio", "SunBurn-Studio.auth") };

    public static string GetActivationPath()
    {
      return Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Synapse Gaming\\Activation\\";
    }

    public class Class3
    {
      private string string_0;
      private string string_1;

      public string Name
      {
        get
        {
          return this.string_0;
        }
      }

      public string FileName
      {
        get
        {
          return this.string_1;
        }
      }

      public Class3(string name, string filename)
      {
        this.string_0 = name;
        this.string_1 = filename;
      }
    }

    public enum Enum0
    {
      SunBurn_Indie,
      SunBurn_Pro,
      SunBurn_Community,
      SunBurn_Studio,
    }
  }
}
