// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.ObjectGraph`1
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ns3;

namespace SynapseGaming.LightingSystem.Core
{
    /// <summary>
    /// Acts as a storage tree / scenegraph for objects of a particular
    /// class or interface.  Supports object adding, moving, and removing
    /// as well as auto-detecting movement of dynamic objects using the
    /// MoveDynamicObjects method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectGraph<T> : IQuery<T>, ISubmit<T> where T : IMovableObject
    {
        private int WorldTreeMaxDepth = 20;
        private Dictionary<T, int> DynamicObjects = new(32);
        private RTreeNode<T> Root = new();
        private Vector3[] WorldBoundsCorners = new Vector3[8];
        private List<T> TempList = new(32);
        private Statistics Stats = new();

        /// <summary>The current containment volume for this object.</summary>
        public BoundingBox WorldBoundingBox { get; private set; }

        /// <summary>
        /// Creates a new ObjectGraph using the default world size and tree depth.
        /// </summary>
        public ObjectGraph()
        {
            WorldBoundingBox = new BoundingBox(new Vector3(-1000f, -1000f, -1000f), new Vector3(1000f, 1000f, 1000f));
            Resize(WorldBoundingBox, WorldTreeMaxDepth);
        }

        /// <summary>
        /// Creates a new ObjectGraph using the provided world size and tree depth.
        /// </summary>
        /// <param name="worldboundingbox">The smallest bounding area that completely
        /// contains the scene. Helps build an optimal scene tree.</param>
        /// <param name="worldtreemaxdepth">Maximum depth for entries in the scene tree. Small
        /// scenes with few objects see better performance with shallow trees. Large complex
        /// scenes often need deeper trees.</param>
        public ObjectGraph(BoundingBox worldboundingbox, int worldtreemaxdepth)
        {
            Resize(worldboundingbox, worldtreemaxdepth);
        }

        /// <summary>Resizes the tree used to store contained objects.</summary>
        /// <param name="worldboundingbox">The smallest bounding area that completely
        /// contains the scene. Helps the ObjectGraph build an optimal scene tree.</param>
        /// <param name="worldtreemaxdepth">Maximum depth for entries in the scene tree. Small
        /// scenes with few objects see better performance with shallow trees. Large complex
        /// scenes often need deeper trees.</param>
        public void Resize(BoundingBox worldboundingbox, int worldtreemaxdepth)
        {
            WorldBoundingBox = worldboundingbox;
            WorldTreeMaxDepth = worldtreemaxdepth;
            Root.Create(ref worldboundingbox, worldtreemaxdepth);
        }

        /// <summary>Optimizes the tree used to store contained objects.</summary>
        public virtual void Optimize()
        {
            var boundingBox = new BoundingBox();
            this.TempList.Clear();
            this.Root.RecursiveGetObjects(this.TempList);
            foreach (T obj in this.TempList)
            {
                if (!obj.InfiniteBounds)
                    boundingBox = BoundingBox.CreateMerged(boundingBox, obj.WorldBoundingBox);
            }
            int worldtreemaxdepth = Math.Max(1, this.TempList.Count / 40);
            this.Resize(boundingBox, worldtreemaxdepth);
            foreach (T gparam_0 in this.TempList)
                this.Root.Insert(gparam_0.WorldBoundingBox, gparam_0);
        }

        /// <summary>
        /// Adds an object to the container. This does not transfer ownership, disposable
        /// objects should be maintained and disposed separately.
        /// </summary>
        /// <param name="obj"></param>
        public virtual void Submit(T obj)
        {
            if (obj.ObjectType == ObjectType.Dynamic)
            {
                if (DynamicObjects.ContainsKey(obj))
                    return;
                DynamicObjects.Add(obj, obj.MoveId);
            }
            Root.Insert(obj.WorldBoundingBox, obj);
            ++Stats.ObjectsSubmitted.AccumulationValue;
        }

        /// <summary>
        /// Repositions an object within the container. This method is used when a static object
        /// moves to reposition it in the storage tree / scenegraph.
        /// </summary>
        /// <param name="obj"></param>
        public virtual void Move(T obj)
        {
            Root.Update(obj.WorldBoundingBox, obj);
            ++Stats.ObjectsMoved.AccumulationValue;
        }

        /// <summary>
        /// Auto-detects moved dynamic objects and repositions them in the storage tree / scenegraph.
        /// </summary>
        public virtual void MoveDynamicObjects()
        {
            TempList.Clear();
            foreach (KeyValuePair<T, int> kv in DynamicObjects)
            {
                if (kv.Key.MoveId != kv.Value)
                {
                    T key = kv.Key;
                    Root.Update(key.WorldBoundingBox, key);
                    TempList.Add(key);
                    ++Stats.ObjectsMovedDynamic.AccumulationValue;
                }
            }
            foreach (T obj in TempList)
                DynamicObjects[obj] = obj.MoveId;
        }

        /// <summary>
        /// Finds all contained objects that match a set of filter attributes
        /// and overlap with or are contained in a bounding area.
        /// </summary>
        /// <param name="foundobjects">List used to store found objects during the query.</param>
        /// <param name="worldbounds">Bounding area used to limit query results.</param>
        /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
        public virtual void Find(List<T> foundobjects, BoundingFrustum worldbounds, ObjectFilter objectfilter)
        {
            int count1 = foundobjects.Count;
            worldbounds.GetCorners(this.WorldBoundsCorners);
            this.TempList.Clear();
            this.Root.FindInBounds(CoreUtils.smethod_11(this.WorldBoundsCorners), this.TempList);
            bool flag1 = (objectfilter & ObjectFilter.Dynamic) != 0;
            bool flag2 = (objectfilter & ObjectFilter.Static) != 0;
            int count2 = this.TempList.Count;
            for (int index = 0; index < count2; ++index)
            {
                T obj = this.TempList[index];
                if ((flag1 && obj.ObjectType == ObjectType.Dynamic || flag2 && obj.ObjectType == ObjectType.Static) && worldbounds.Contains(obj.WorldBoundingBox) != ContainmentType.Disjoint)
                    foundobjects.Add(obj);
            }
            this.Stats.ObjectsRetrieved.AccumulationValue += foundobjects.Count - count1;
        }

        /// <summary>
        /// Finds all contained objects that match a set of filter attributes
        /// and overlap with or are contained in a bounding area.
        /// </summary>
        /// <param name="foundobjects">List used to store found objects during the query.</param>
        /// <param name="worldbounds">Bounding area used to limit query results.</param>
        /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
        public virtual void Find(List<T> foundobjects, BoundingBox worldbounds, ObjectFilter objectfilter)
        {
            int count1 = foundobjects.Count;
            this.TempList.Clear();
            this.Root.FindInBounds(worldbounds, this.TempList);
            bool flag1 = (objectfilter & ObjectFilter.Dynamic) != 0;
            bool flag2 = (objectfilter & ObjectFilter.Static) != 0;
            int count2 = this.TempList.Count;
            for (int i = 0; i < count2; ++i)
            {
                T obj = this.TempList[i];
                if ((flag1 && obj.ObjectType == ObjectType.Dynamic || flag2 && obj.ObjectType == ObjectType.Static) && worldbounds.Contains(obj.WorldBoundingBox) != ContainmentType.Disjoint)
                    foundobjects.Add(obj);
            }
            this.Stats.ObjectsRetrieved.AccumulationValue += foundobjects.Count - count1;
        }

        /// <summary>
        /// Finds all contained objects that match a set of filter attributes.
        /// </summary>
        /// <param name="foundobjects">List used to store found objects during the query.</param>
        /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
        public virtual void Find(List<T> foundobjects, ObjectFilter objectfilter)
        {
            int count1 = foundobjects.Count;
            TempList.Clear();
            Root.RecursiveGetObjects(TempList);
            bool isDynamic = (objectfilter & ObjectFilter.Dynamic) != 0;
            bool isStatic  = (objectfilter & ObjectFilter.Static) != 0;
            int count2 = TempList.Count;
            for (int i = 0; i < count2; ++i)
            {
                T obj = TempList[i];
                if (isDynamic && obj.ObjectType == ObjectType.Dynamic || isStatic && obj.ObjectType == ObjectType.Static)
                    foundobjects.Add(obj);
            }
            Stats.ObjectsRetrieved.AccumulationValue += foundobjects.Count - count1;
        }

        /// <summary>
        /// Quickly finds all objects near a bounding area without the overhead of
        /// filtering by object type, checking if objects are enabled, or verifying
        /// containment within the bounds.
        /// </summary>
        /// <param name="foundobjects">List used to store found objects during the query.</param>
        /// <param name="worldbounds">Bounding area used to limit query results.</param>
        public void FindFast(List<T> foundobjects, BoundingBox worldbounds)
        {
            int count = foundobjects.Count;
            this.Root.FindInBounds(worldbounds, foundobjects);
            this.Stats.ObjectsRetrieved.AccumulationValue += foundobjects.Count - count;
        }

        /// <summary>
        /// Quickly finds all objects without the overhead of filtering by object
        /// type or checking if objects are enabled.
        /// </summary>
        /// <param name="foundobjects">List used to store found objects during the query.</param>
        public void FindFast(List<T> foundobjects)
        {
            int count = foundobjects.Count;
            Root.RecursiveGetObjects(foundobjects);
            Stats.ObjectsRetrieved.AccumulationValue += foundobjects.Count - count;
        }

        /// <summary>Removes an object from the container.</summary>
        /// <param name="obj"></param>
        public virtual bool Remove(T obj)
        {
            bool exists = DynamicObjects.Remove(obj);
            Root.Remove(obj.WorldBoundingBox, obj);
            ++Stats.ObjectsRemoved.AccumulationValue;
            return exists;
        }

        /// <summary>
        /// Removes resources managed by this object. Commonly used while clearing the scene.
        /// </summary>
        public virtual void Clear()
        {
            DynamicObjects.Clear();
            Root.Clear();
        }

        private class Statistics
        {
            public readonly LightingSystemStatistic ObjectsSubmitted    = LightingSystemStatistics.GetStatistic("SceneGraph_ObjectsSubmitted", LightingSystemStatisticCategory.SceneGraph);
            public readonly LightingSystemStatistic ObjectsMoved        = LightingSystemStatistics.GetStatistic("SceneGraph_ObjectsMoved", LightingSystemStatisticCategory.SceneGraph);
            public readonly LightingSystemStatistic ObjectsMovedDynamic = LightingSystemStatistics.GetStatistic("SceneGraph_ObjectsMovedDynamic", LightingSystemStatisticCategory.SceneGraph);
            public readonly LightingSystemStatistic ObjectsRemoved      = LightingSystemStatistics.GetStatistic("SceneGraph_ObjectsRemoved", LightingSystemStatisticCategory.SceneGraph);
            public readonly LightingSystemStatistic ObjectsRetrieved    = LightingSystemStatistics.GetStatistic("SceneGraph_ObjectsRetrieved", LightingSystemStatisticCategory.SceneGraph);
        }
    }
}
