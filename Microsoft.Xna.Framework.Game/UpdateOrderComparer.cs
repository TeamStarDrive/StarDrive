// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.UpdateOrderComparer
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using System.Collections.Generic;

namespace Microsoft.Xna.Framework
{
  internal class UpdateOrderComparer : IComparer<IUpdateable>
  {
    public static readonly UpdateOrderComparer Default = new UpdateOrderComparer();

    public int Compare(IUpdateable x, IUpdateable y)
    {
      if (x == null && y == null)
        return 0;
      if (x == null)
        return 1;
      if (y == null)
        return -1;
      if (x.Equals((object) y))
        return 0;
      return x.UpdateOrder < y.UpdateOrder ? -1 : 1;
    }
  }
}
