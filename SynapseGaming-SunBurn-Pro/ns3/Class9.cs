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
  internal class Class9<T>
  {
    private static readonly PooledObjectFactory<Class9<T>> PooledObjectFactory0 = new PooledObjectFactory<Class9<T>>();
    private static float[] float_0 = new float[3];
    private List<T> list_0 = new List<T>();
    private int int_0;
    private BoundingBox boundingBox_0;
    private Enum2 enum2_0;
    private Plane plane_0;
    private Class9<T> class9_0;
    private Class9<T> class9_1;

      public void method_0(ref BoundingBox boundingBox_1, int int_1)
    {
      this.method_1();
      this.boundingBox_0 = boundingBox_1;
      this.int_0 = int_1;
      Vector3 vector3 = this.boundingBox_0.Max - this.boundingBox_0.Min;
      float x = vector3.X;
      int index1 = 0;
      float_0[0] = vector3.X;
      float_0[1] = vector3.Y;
      float_0[2] = vector3.Z;
      for (int index2 = 1; index2 < 3; ++index2)
      {
        if (x <= (double) float_0[index2])
        {
          x = float_0[index2];
          index1 = index2;
        }
      }
      this.enum2_0 = (Enum2) index1;
      float_0[0] = 0.0f;
      float_0[1] = 0.0f;
      float_0[2] = 0.0f;
      float_0[index1] = 1f;
      this.plane_0.Normal.X = float_0[0];
      this.plane_0.Normal.Y = float_0[1];
      this.plane_0.Normal.Z = float_0[2];
      float_0[0] = this.boundingBox_0.Min.X;
      float_0[1] = this.boundingBox_0.Min.Y;
      float_0[2] = this.boundingBox_0.Min.Z;
      this.plane_0.D = (float) -(float_0[index1] + x * 0.5);
    }

    public void method_1()
    {
      this.list_0.Clear();
      if (this.class9_0 != null)
      {
        this.class9_0.method_1();
        PooledObjectFactory0.Free(this.class9_0);
        this.class9_0 = null;
      }
      if (this.class9_1 == null)
        return;
      this.class9_1.method_1();
      PooledObjectFactory0.Free(this.class9_1);
      this.class9_1 = null;
    }

    public void method_2(BoundingBox boundingBox_1, T gparam_0)
    {
      this.method_6(ref boundingBox_1, gparam_0, 0, false).list_0.Add(gparam_0);
    }

    public void method_3(BoundingBox boundingBox_1, T gparam_0)
    {
      Class9<T> class9 = this.method_6(ref boundingBox_1, gparam_0, 0, true);
      if (class9.list_0.Contains(gparam_0))
        return;
      this.method_5(gparam_0);
      class9.list_0.Add(gparam_0);
    }

    public void method_4(BoundingBox boundingBox_1, T gparam_0)
    {
      if (this.method_6(ref boundingBox_1, gparam_0, 0, true).list_0.Remove(gparam_0))
        return;
      this.method_5(gparam_0);
    }

    private bool method_5(T gparam_0)
    {
      return list_0.Remove(gparam_0) 
                || class9_0 != null && class9_0.method_5(gparam_0) 
                || class9_1 != null && class9_1.method_5(gparam_0);
    }

    private Class9<T> method_6(ref BoundingBox boundingBox_1, T gparam_0, int int_1, bool bool_0)
    {
      bool flag1 = this.plane_0.DotCoordinate(boundingBox_1.Max) > 0.0;
      bool flag2 = this.plane_0.DotCoordinate(boundingBox_1.Min) > 0.0;
      if (flag1 != flag2 || int_1 >= this.int_0)
        return this;
      if (flag2)
      {
        if (this.class9_0 == null)
        {
          if (bool_0)
            return this;
          BoundingBox boundingBox0 = this.boundingBox_0;
          boundingBox0.Min = CoreUtils.smethod_3(boundingBox0.Min, (int) this.enum2_0, -this.plane_0.D);
          this.class9_0 = PooledObjectFactory0.New();
          this.class9_0.method_0(ref boundingBox0, this.int_0);
        }
        return this.class9_0.method_6(ref boundingBox_1, gparam_0, int_1 + 1, bool_0);
      }
      if (this.class9_1 == null)
      {
        if (bool_0)
          return this;
        BoundingBox boundingBox0 = this.boundingBox_0;
        boundingBox0.Max = CoreUtils.smethod_3(boundingBox0.Max, (int) this.enum2_0, -this.plane_0.D);
        this.class9_1 = PooledObjectFactory0.New();
        this.class9_1.method_0(ref boundingBox0, this.int_0);
      }
      return this.class9_1.method_6(ref boundingBox_1, gparam_0, int_1 + 1, bool_0);
    }

    public void method_7(BoundingBox boundingBox_1, List<T> list_1)
    {
      this.method_8(ref boundingBox_1, list_1);
    }

    private void method_8(ref BoundingBox boundingBox_1, List<T> list_1)
    {
      foreach (T obj in this.list_0)
        list_1.Add(obj);
      bool flag1 = this.plane_0.DotCoordinate(boundingBox_1.Max) > 0.0;
      bool flag2 = this.plane_0.DotCoordinate(boundingBox_1.Min) < 0.0;
      if (flag1 && this.class9_0 != null)
        this.class9_0.method_8(ref boundingBox_1, list_1);
      if (!flag2 || this.class9_1 == null)
        return;
      this.class9_1.method_8(ref boundingBox_1, list_1);
    }

    public void method_9(List<T> list_1)
    {
      foreach (T obj in this.list_0)
        list_1.Add(obj);
      if (this.class9_0 != null)
        this.class9_0.method_9(list_1);
      if (this.class9_1 == null)
        return;
      this.class9_1.method_9(list_1);
    }

    private enum Enum2
    {
      const_0,
      const_1,
      const_2,
      const_3
    }
  }
}
