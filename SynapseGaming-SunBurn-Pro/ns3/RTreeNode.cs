// Decompiled with JetBrains decompiler
// Type: ns3.Class9`1
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Core;

namespace ns3
{
    internal class RTreeNode<T>
    {
        private static readonly PooledObjectFactory<RTreeNode<T>> NodePool = new PooledObjectFactory<RTreeNode<T>>();
        private static readonly float[] Vec3 = new float[3];
        private readonly List<T> Objects = new List<T>();
        private int Depth;
        private BoundingBox Bounds;
        private Axis BiggestAxis;
        private Plane Plane;
        private RTreeNode<T> Left;
        private RTreeNode<T> Right;

        public void Create(ref BoundingBox bounds, int depth)
        {
            Clear();
            Bounds = bounds;
            Depth = depth;
            Vector3 size = Bounds.Max - Bounds.Min;
            float biggestAxis = size.X;
            int biggestIndex = 0;
            Vec3[0] = size.X;
            Vec3[1] = size.Y;
            Vec3[2] = size.Z;
            for (int i = 1; i < 3; ++i)
            {
                if (biggestAxis <= Vec3[i])
                {
                    biggestAxis = Vec3[i];
                    biggestIndex = i;
                }
            }
            BiggestAxis = (Axis)biggestIndex;
            Vec3[0] = 0.0f;
            Vec3[1] = 0.0f;
            Vec3[2] = 0.0f;
            Vec3[biggestIndex] = 1f;
            Plane.Normal.X = Vec3[0];
            Plane.Normal.Y = Vec3[1];
            Plane.Normal.Z = Vec3[2];
            Vec3[0] = Bounds.Min.X;
            Vec3[1] = Bounds.Min.Y;
            Vec3[2] = Bounds.Min.Z;
            Plane.D = (float) -(Vec3[biggestIndex] + biggestAxis * 0.5);
        }

        public void Clear()
        {
            Objects.Clear();
            if (Left != null)
            {
                Left.Clear();
                NodePool.Free(Left);
                Left = null;
            }
            if (Right != null)
            {
                Right.Clear();
                NodePool.Free(Right);
                Right = null;
            }
        }

        public void Insert(BoundingBox objectBounds, T obj)
        {
            FindNode(ref objectBounds, 0, false).Objects.Add(obj);
        }

        public void Update(BoundingBox objectBounds, T obj)
        {
            RTreeNode<T> node = FindNode(ref objectBounds, 0, true);
            if (node.Objects.Contains(obj))
                return;
            ExhaustiveRemove(obj);
            node.Objects.Add(obj);
        }

        public void Remove(BoundingBox objectBounds, T obj)
        {
            if (FindNode(ref objectBounds, 0, true).Objects.Remove(obj))
                return;
            ExhaustiveRemove(obj);
        }

        private bool ExhaustiveRemove(T obj)
        {
            return Objects.Remove(obj) 
                   || Left  != null && Left.ExhaustiveRemove(obj) 
                   || Right != null && Right.ExhaustiveRemove(obj);
        }

        private RTreeNode<T> FindNode(ref BoundingBox objectBounds, int depth, bool bool_0)
        {
            bool flag1 = Plane.DotCoordinate(objectBounds.Max) > 0.0;
            bool flag2 = Plane.DotCoordinate(objectBounds.Min) > 0.0;
            if (flag1 != flag2 || depth >= Depth)
                return this;
            if (flag2)
            {
                if (Left == null)
                {
                    if (bool_0)
                        return this;
                    BoundingBox bounds = Bounds;
                    bounds.Min = CoreUtils.smethod_3(bounds.Min, (int)BiggestAxis, -Plane.D);
                    Left = NodePool.New();
                    Left.Create(ref bounds, Depth);
                }
                return Left.FindNode(ref objectBounds, depth + 1, bool_0);
            }
            if (Right == null)
            {
                if (bool_0)
                    return this;
                BoundingBox bounds = Bounds;
                bounds.Max = CoreUtils.smethod_3(bounds.Max, (int)BiggestAxis, -Plane.D);
                Right = NodePool.New();
                Right.Create(ref bounds, Depth);
            }
            return Right.FindNode(ref objectBounds, depth + 1, bool_0);
        }

        public void FindInBounds(BoundingBox bounds, List<T> outObjects)
        {
            FindInBounds(ref bounds, outObjects);
        }

        private void FindInBounds(ref BoundingBox bounds, List<T> outObjects)
        {
            for (int i = 0; i < Objects.Count; ++i)
                outObjects.Add(Objects[i]);
            bool flag1 = Plane.DotCoordinate(bounds.Max) > 0.0;
            bool flag2 = Plane.DotCoordinate(bounds.Min) < 0.0;
            if (flag1) Left?.FindInBounds(ref bounds, outObjects);
            if (flag2) Right?.FindInBounds(ref bounds, outObjects);
        }

        public void RecursiveGetObjects(List<T> outObjects)
        {
            for (int i = 0; i < Objects.Count; ++i)
                outObjects.Add(Objects[i]);
            Left?.RecursiveGetObjects(outObjects);
            Right?.RecursiveGetObjects(outObjects);
        }

        private enum Axis
        {
            X,
            Y,
            Z,
            Unknown
        }
    }
}
