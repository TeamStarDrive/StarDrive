// Decompiled with JetBrains decompiler
// Type: ns3.Class22`1
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;

namespace ns3
{
  internal class Class22<T> : Class21<T> where T : IDisposable, new()
  {
    public override void Clear()
    {
      this.method_0();
      foreach (T obj in this._UnusedObjectPool)
        obj.Dispose();
      this._UnusedObjectPool.Clear();
      if (this._LostObjectCount > 0)
        throw new Exception("Some tracked pool objects were not disposed.");
    }

    public void method_1()
    {
      this.Clear();
    }
  }
}
