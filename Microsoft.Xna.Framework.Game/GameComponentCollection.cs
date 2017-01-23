// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.GameComponentCollection
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using System;
using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework
{
  public sealed class GameComponentCollection : Collection<IGameComponent>
  {
    public event EventHandler<GameComponentCollectionEventArgs> ComponentAdded;

    public event EventHandler<GameComponentCollectionEventArgs> ComponentRemoved;

    protected override void InsertItem(int index, IGameComponent item)
    {
      if (this.IndexOf(item) != -1)
        throw new ArgumentException(Resources.CannotAddSameComponentMultipleTimes);
      base.InsertItem(index, item);
      if (item == null)
        return;
      this.OnComponentAdded(new GameComponentCollectionEventArgs(item));
    }

    protected override void RemoveItem(int index)
    {
      IGameComponent gameComponent = this[index];
      base.RemoveItem(index);
      if (gameComponent == null)
        return;
      this.OnComponentRemoved(new GameComponentCollectionEventArgs(gameComponent));
    }

    protected override void SetItem(int index, IGameComponent item)
    {
      throw new NotSupportedException(Resources.CannotSetItemsIntoGameComponentCollection);
    }

    protected override void ClearItems()
    {
      for (int index = 0; index < this.Count; ++index)
        this.OnComponentRemoved(new GameComponentCollectionEventArgs(this[index]));
      base.ClearItems();
    }

    private void OnComponentAdded(GameComponentCollectionEventArgs eventArgs)
    {
      if (this.ComponentAdded == null)
        return;
      this.ComponentAdded((object) this, eventArgs);
    }

    private void OnComponentRemoved(GameComponentCollectionEventArgs eventArgs)
    {
      if (this.ComponentRemoved == null)
        return;
      this.ComponentRemoved((object) this, eventArgs);
    }
  }
}
