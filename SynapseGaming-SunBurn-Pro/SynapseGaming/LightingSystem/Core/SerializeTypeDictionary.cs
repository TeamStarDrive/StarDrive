// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.SerializeTypeDictionary
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary />
  public class SerializeTypeDictionary
  {
    private Dictionary<string, List<Type>> dictionary_0 = new Dictionary<string, List<Type>>(16);

    /// <summary />
    public void RegisterType(string name, Type objectype)
    {
      if (!this.dictionary_0.ContainsKey(name))
        this.dictionary_0.Add(name, new List<Type>(4));
      this.dictionary_0[name].Insert(0, objectype);
    }

    /// <summary />
    public void UnregisterType(string name, Type objectype)
    {
      if (!this.dictionary_0.ContainsKey(name))
        return;
      List<Type> typeList = this.dictionary_0[name];
      if (typeList == null || typeList.Count < 1)
        return;
      typeList.Remove(objectype);
    }

    /// <summary />
    public string GetName(Type objecttype)
    {
      foreach (KeyValuePair<string, List<Type>> keyValuePair in this.dictionary_0)
      {
        if (keyValuePair.Value != null && keyValuePair.Value.Count >= 1 && keyValuePair.Value[0] == objecttype)
          return keyValuePair.Key;
      }
      return null;
    }

    /// <summary />
    public Type GetType(string name)
    {
      if (!this.dictionary_0.ContainsKey(name))
        return null;
      List<Type> typeList = this.dictionary_0[name];
      if (typeList != null && typeList.Count >= 1)
        return typeList[0];
      return null;
    }
  }
}
