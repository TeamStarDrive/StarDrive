// Decompiled with JetBrains decompiler
// Type: ns11.Class78
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
#pragma warning disable CA2002

namespace ns11
{
  internal class Class78
  {
    private Queue<Struct3> queue_0 = new Queue<Struct3>(16);
    private Class77 class77_0 = new Class77();

    public void method_0(Delegate delegate_0, object[] object_0)
    {
      lock (this)
        this.queue_0.Enqueue(new Struct3
        {
          delegate_0 = delegate_0,
          object_0 = object_0
        });
    }

    public void method_1()
    {
      this.class77_0.method_0();
      lock (this)
      {
        while (this.queue_0.Count > 0)
        {
          Struct3 local_0 = this.queue_0.Dequeue();
          local_0.delegate_0.DynamicInvoke(local_0.object_0);
        }
      }
    }

    private struct Struct3
    {
      public Delegate delegate_0;
      public object[] object_0;
    }
  }
}
