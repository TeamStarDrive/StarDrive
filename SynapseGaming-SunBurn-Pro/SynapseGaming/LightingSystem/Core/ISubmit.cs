// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.ISubmit`1
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Generic interface used by container objects that implement submitting
  /// and removing other objects.
  /// </summary>
  /// <typeparam name="T">Type of objects that will be submitted and removed.</typeparam>
  public interface ISubmit<T>
  {
    /// <summary>
    /// Adds an object to the container. This does not transfer ownership, disposable
    /// objects should be maintained and disposed separately.
    /// </summary>
    /// <param name="obj"></param>
    void Submit(T obj);

    /// <summary>
    /// Repositions an object within the container. This method is used when the container
    /// implements a tree or graph, and relocates an object within that structure
    /// often due to a change in object world position.
    /// </summary>
    /// <param name="obj"></param>
    void Move(T obj);

    /// <summary>
    /// Auto-detects moved dynamic objects and repositions them in the storage tree / scenegraph.
    /// This method is used when the container implements a tree or graph, and relocates all
    /// dynamic objects within that structure often due to a change in object world position.
    /// </summary>
    void MoveDynamicObjects();

    /// <summary>Removes an object from the container.</summary>
    /// <param name="obj"></param>
    void Remove(T obj);

    /// <summary>
    /// Removes all objects from the container. Commonly used while clearing the scene.
    /// </summary>
    void Clear();
  }
}
