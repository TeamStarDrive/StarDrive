// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.ObjectGraph`1
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using ns3;
using System;
using System.Collections.Generic;

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
        private int int_0 = 20;
        private Dictionary<T, int> dictionary_0 = new Dictionary<T, int>(32);
        private Class9<T> class9_0 = new Class9<T>();
        private Vector3[] WorldBoundsCorners = new Vector3[8];
        private List<T> list_0 = new List<T>(32);
        private Class19 class19_0 = new Class19();
        private BoundingBox boundingBox_0;

        /// <summary>The current containment volume for this object.</summary>
        public BoundingBox WorldBoundingBox
        {
            get
            {
                return this.boundingBox_0;
            }
        }

        /// <summary>
        /// Creates a new ObjectGraph using the default world size and tree depth.
        /// </summary>
        public ObjectGraph()
        {
            this.boundingBox_0 = new BoundingBox(new Vector3(-1000f, -1000f, -1000f), new Vector3(1000f, 1000f, 1000f));
            this.Resize(this.boundingBox_0, this.int_0);
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
            this.Resize(worldboundingbox, worldtreemaxdepth);
        }

        /// <summary>Resizes the tree used to store contained objects.</summary>
        /// <param name="worldboundingbox">The smallest bounding area that completely
        /// contains the scene. Helps the ObjectGraph build an optimal scene tree.</param>
        /// <param name="worldtreemaxdepth">Maximum depth for entries in the scene tree. Small
        /// scenes with few objects see better performance with shallow trees. Large complex
        /// scenes often need deeper trees.</param>
        public virtual void Resize(BoundingBox worldboundingbox, int worldtreemaxdepth)
        {
            this.boundingBox_0 = worldboundingbox;
            this.int_0 = worldtreemaxdepth;
            this.class9_0.method_0(ref worldboundingbox, worldtreemaxdepth);
        }

        /// <summary>Optimizes the tree used to store contained objects.</summary>
        public virtual void Optimize()
        {
            BoundingBox boundingBox = new BoundingBox();
            this.list_0.Clear();
            this.class9_0.method_9(this.list_0);
            foreach (T obj in this.list_0)
            {
                if (!obj.InfiniteBounds)
                    boundingBox = BoundingBox.CreateMerged(boundingBox, obj.WorldBoundingBox);
            }
            int worldtreemaxdepth = Math.Max(1, this.list_0.Count / 40);
            this.Resize(boundingBox, worldtreemaxdepth);
            foreach (T gparam_0 in this.list_0)
                this.class9_0.method_2(gparam_0.WorldBoundingBox, gparam_0);
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
                if (this.dictionary_0.ContainsKey(obj))
                    return;
                this.dictionary_0.Add(obj, obj.MoveId);
            }
            this.class9_0.method_2(obj.WorldBoundingBox, obj);
            ++this.class19_0.lightingSystemStatistic_0.AccumulationValue;
        }

        /// <summary>
        /// Repositions an object within the container. This method is used when a static object
        /// moves to reposition it in the storage tree / scenegraph.
        /// </summary>
        /// <param name="obj"></param>
        public virtual void Move(T obj)
        {
            this.class9_0.method_3(obj.WorldBoundingBox, obj);
            ++this.class19_0.lightingSystemStatistic_1.AccumulationValue;
        }

        /// <summary>
        /// Auto-detects moved dynamic objects and repositions them in the storage tree / scenegraph.
        /// </summary>
        public virtual void MoveDynamicObjects()
        {
            this.list_0.Clear();
            foreach (KeyValuePair<T, int> keyValuePair in this.dictionary_0)
            {
                if (keyValuePair.Key.MoveId != keyValuePair.Value)
                {
                    T key = keyValuePair.Key;
                    this.class9_0.method_3(key.WorldBoundingBox, key);
                    this.list_0.Add(key);
                    ++this.class19_0.lightingSystemStatistic_2.AccumulationValue;
                }
            }
            foreach (T index in this.list_0)
                this.dictionary_0[index] = index.MoveId;
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
            this.list_0.Clear();
            this.class9_0.method_7(Class13.smethod_11(this.WorldBoundsCorners), this.list_0);
            bool flag1 = (objectfilter & ObjectFilter.Dynamic) != (ObjectFilter)0;
            bool flag2 = (objectfilter & ObjectFilter.Static) != (ObjectFilter)0;
            int count2 = this.list_0.Count;
            for (int index = 0; index < count2; ++index)
            {
                T obj = this.list_0[index];
                if ((flag1 && obj.ObjectType == ObjectType.Dynamic || flag2 && obj.ObjectType == ObjectType.Static) && worldbounds.Contains(obj.WorldBoundingBox) != ContainmentType.Disjoint)
                    foundobjects.Add(obj);
            }
            this.class19_0.lightingSystemStatistic_4.AccumulationValue += foundobjects.Count - count1;
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
            this.list_0.Clear();
            this.class9_0.method_7(worldbounds, this.list_0);
            bool flag1 = (objectfilter & ObjectFilter.Dynamic) != (ObjectFilter)0;
            bool flag2 = (objectfilter & ObjectFilter.Static) != (ObjectFilter)0;
            int count2 = this.list_0.Count;
            for (int index = 0; index < count2; ++index)
            {
                T obj = this.list_0[index];
                if ((flag1 && obj.ObjectType == ObjectType.Dynamic || flag2 && obj.ObjectType == ObjectType.Static) && worldbounds.Contains(obj.WorldBoundingBox) != ContainmentType.Disjoint)
                    foundobjects.Add(obj);
            }
            this.class19_0.lightingSystemStatistic_4.AccumulationValue += foundobjects.Count - count1;
        }

        /// <summary>
        /// Finds all contained objects that match a set of filter attributes.
        /// </summary>
        /// <param name="foundobjects">List used to store found objects during the query.</param>
        /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
        public virtual void Find(List<T> foundobjects, ObjectFilter objectfilter)
        {
            int count1 = foundobjects.Count;
            this.list_0.Clear();
            this.class9_0.method_9(this.list_0);
            bool flag1 = (objectfilter & ObjectFilter.Dynamic) != (ObjectFilter)0;
            bool flag2 = (objectfilter & ObjectFilter.Static) != (ObjectFilter)0;
            int count2 = this.list_0.Count;
            for (int index = 0; index < count2; ++index)
            {
                T obj = this.list_0[index];
                if (flag1 && obj.ObjectType == ObjectType.Dynamic || flag2 && obj.ObjectType == ObjectType.Static)
                    foundobjects.Add(obj);
            }
            this.class19_0.lightingSystemStatistic_4.AccumulationValue += foundobjects.Count - count1;
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
            this.class9_0.method_7(worldbounds, foundobjects);
            this.class19_0.lightingSystemStatistic_4.AccumulationValue += foundobjects.Count - count;
        }

        /// <summary>
        /// Quickly finds all objects without the overhead of filtering by object
        /// type or checking if objects are enabled.
        /// </summary>
        /// <param name="foundobjects">List used to store found objects during the query.</param>
        public void FindFast(List<T> foundobjects)
        {
            int count = foundobjects.Count;
            this.class9_0.method_9(foundobjects);
            this.class19_0.lightingSystemStatistic_4.AccumulationValue += foundobjects.Count - count;
        }

        /// <summary>Removes an object from the container.</summary>
        /// <param name="obj"></param>
        public virtual void Remove(T obj)
        {
            this.dictionary_0.Remove(obj);
            this.class9_0.method_4(obj.WorldBoundingBox, obj);
            ++this.class19_0.lightingSystemStatistic_3.AccumulationValue;
        }

        /// <summary>
        /// Removes resources managed by this object. Commonly used while clearing the scene.
        /// </summary>
        public virtual void Clear()
        {
            this.dictionary_0.Clear();
            this.class9_0.method_1();
        }

        private class Class19
        {
            public LightingSystemStatistic lightingSystemStatistic_0 = LightingSystemStatistics.GetStatistic("SceneGraph_ObjectsSubmitted", LightingSystemStatisticCategory.SceneGraph);
            public LightingSystemStatistic lightingSystemStatistic_1 = LightingSystemStatistics.GetStatistic("SceneGraph_ObjectsMoved", LightingSystemStatisticCategory.SceneGraph);
            public LightingSystemStatistic lightingSystemStatistic_2 = LightingSystemStatistics.GetStatistic("SceneGraph_ObjectsMovedDynamic", LightingSystemStatisticCategory.SceneGraph);
            public LightingSystemStatistic lightingSystemStatistic_3 = LightingSystemStatistics.GetStatistic("SceneGraph_ObjectsRemoved", LightingSystemStatisticCategory.SceneGraph);
            public LightingSystemStatistic lightingSystemStatistic_4 = LightingSystemStatistics.GetStatistic("SceneGraph_ObjectsRetrieved", LightingSystemStatisticCategory.SceneGraph);
        }
    }
}
