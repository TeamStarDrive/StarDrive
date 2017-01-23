// Decompiled with JetBrains decompiler
// Type: ns3.Class21`1
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using SynapseGaming.LightingSystem.Core;
using System.Collections.Generic;

namespace ns3
{
  internal class Class21<T> : PooledObjectFactory<T> where T : new()
  {
    private List<T> list_0 = new List<T>();

    public override T New()
    {
      T obj = base.New();
      this.list_0.Add(obj);
      return obj;
    }

    public override void Free(T obj)
    {
      this.list_0.Remove(obj);
      base.Free(obj);
    }

    public void method_0()
    {
      foreach (T obj in this.list_0)
        base.Free(obj);
      this.list_0.Clear();
    }

    public override void Clear()
    {
      this.method_0();
      base.Clear();
    }
  }
}
