// Decompiled with JetBrains decompiler
// Type: ns11.Class76
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
namespace ns11
{
    internal class Disposable
    {
        public static void Free<T>(ref T item) where T : IDisposable
        {
            if (item != null)
            {
                item.Dispose();
                item = default(T);
            }
        }
    }
}
