// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.PooledObjectFactory`1
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Object pool that maintains a list of unused objects
  /// which are recycled to avoid allocating new objects.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class PooledObjectFactory<T> where T : new()
  {
    /// <summary />
    protected List<T> _UnusedObjectPool = new List<T>();
    /// <summary />
    protected int _LostObjectCount;

    /// <summary>
    /// Returns an existing unused object if one exists,
    /// otherwise a new object is created.
    /// </summary>
    /// <returns></returns>
    public virtual T New()
    {
      ++this._LostObjectCount;
      if (this._UnusedObjectPool.Count < 1)
        return new T();
      int index = this._UnusedObjectPool.Count - 1;
      T obj = this._UnusedObjectPool[index];
      this._UnusedObjectPool.RemoveAt(index);
      return obj;
    }

    /// <summary>
    /// Places an unused object back in the object pool
    /// for reuse during a later call to the New method.
    /// </summary>
    /// <param name="obj"></param>
    public virtual void Free(T obj)
    {
      --this._LostObjectCount;
      this._UnusedObjectPool.Add(obj);
    }

    /// <summary>Removes all objects from the object pool.</summary>
    public virtual void Clear()
    {
      this._UnusedObjectPool.Clear();
    }

    /// <summary>
    /// Returns all unused objects and removes them from the
    /// object pool.  This is useful when pooling disposable
    /// objects, as the method returns all objects for manual
    /// disposal.
    /// </summary>
    public virtual void Clear(List<T> returnedobjects)
    {
      foreach (T obj in this._UnusedObjectPool)
        returnedobjects.Add(obj);
      this._UnusedObjectPool.Clear();
    }
  }
}
