// Decompiled with JetBrains decompiler
// Type: ns11.Class77
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Threading;

namespace ns11
{
  internal class Class77
  {
    private int int_0;

    public Class77()
    {
      this.int_0 = Thread.CurrentThread.ManagedThreadId;
    }

    public void method_0()
    {
      if (this.int_0 != Thread.CurrentThread.ManagedThreadId)
        throw new Exception("Calls to object cannot be made from threads other than the one that created it.");
    }
  }
}
