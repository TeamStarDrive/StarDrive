// Decompiled with JetBrains decompiler
// Type: ns3.Class18`2
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using SynapseGaming.LightingSystem.Core;
using System;
using System.Collections.Generic;

namespace ns3
{
  internal class Class18<Tkey, Tvalue>
  {
    public static uint uint_0 = (uint) int.MaxValue;
    private static PooledObjectFactory<Class18<Tkey, Tvalue>> pooledObjectFactory_0 = new PooledObjectFactory<Class18<Tkey, Tvalue>>();
    public List<Tvalue> list_0 = new List<Tvalue>();
    public Tkey gparam_0;
    public Class18<Tkey, Tvalue> class18_0;
    public Class18<Tkey, Tvalue> class18_1;
    internal uint uint_1;
    private Class18<Tkey, Tvalue> class18_2;
    private Class18<Tkey, Tvalue> class18_3;
    private Class18<Tkey, Tvalue> class18_4;

    public Class18(uint hashcode)
    {
      this.uint_1 = hashcode;
    }

    public Class18()
    {
      this.uint_1 = Class18<Tkey, Tvalue>.uint_0;
    }

    public Class18<Tkey, Tvalue> method_0()
    {
      return this.method_3(default (Tkey), 0U);
    }

    public Class18<Tkey, Tvalue> method_1(Tkey gparam_1)
    {
      return this.method_2(gparam_1);
    }

    public Class18<Tkey, Tvalue> method_2(Tkey gparam_1)
    {
      return this.method_3(gparam_1, (uint) gparam_1.GetHashCode());
    }

    public Class18<Tkey, Tvalue> method_3(Tkey gparam_1, uint uint_2)
    {
      if ((int) uint_2 == (int) this.uint_1)
      {
        if (EqualityComparer<Tkey>.Default.Equals(this.gparam_0, default (Tkey)))
          this.gparam_0 = gparam_1;
        return this;
      }
      if (uint_2 > this.uint_1)
      {
        if (this.class18_2 == null)
        {
          this.class18_2 = Class18<Tkey, Tvalue>.pooledObjectFactory_0.New();
          this.class18_2.uint_1 = uint_2;
          this.class18_2.class18_4 = this;
          this.method_5(this.class18_0, this, this.class18_2);
        }
        return this.class18_2.method_3(gparam_1, uint_2);
      }
      if (this.class18_3 == null)
      {
        this.class18_3 = Class18<Tkey, Tvalue>.pooledObjectFactory_0.New();
        this.class18_3.uint_1 = uint_2;
        this.class18_3.class18_4 = this;
        this.method_5(this, this.class18_1, this.class18_3);
      }
      return this.class18_3.method_3(gparam_1, uint_2);
    }

    public void method_4()
    {
      this.gparam_0 = default (Tkey);
      this.list_0.Clear();
      this.uint_1 = Class18<Tkey, Tvalue>.uint_0;
      this.class18_0 = (Class18<Tkey, Tvalue>) null;
      this.class18_1 = (Class18<Tkey, Tvalue>) null;
      this.class18_4 = (Class18<Tkey, Tvalue>) null;
      if (this.class18_2 != null)
      {
        this.class18_2.method_4();
        Class18<Tkey, Tvalue>.pooledObjectFactory_0.Free(this.class18_2);
        this.class18_2 = (Class18<Tkey, Tvalue>) null;
      }
      if (this.class18_3 == null)
        return;
      this.class18_3.method_4();
      Class18<Tkey, Tvalue>.pooledObjectFactory_0.Free(this.class18_3);
      this.class18_3 = (Class18<Tkey, Tvalue>) null;
    }

    private void method_5(Class18<Tkey, Tvalue> class18_5, Class18<Tkey, Tvalue> class18_6, Class18<Tkey, Tvalue> class18_7)
    {
      if (class18_5 != null)
        class18_5.class18_1 = class18_7;
      if (class18_6 != null)
        class18_6.class18_0 = class18_7;
      class18_7.class18_0 = class18_5;
      class18_7.class18_1 = class18_6;
    }

    public static void smethod_0(Class18<Tkey, Tvalue> class18_5)
    {
      if (class18_5.class18_4 == null)
        return;
      if (class18_5.class18_0 != null)
        class18_5.class18_0.class18_1 = class18_5.class18_1;
      if (class18_5.class18_1 != null)
        class18_5.class18_1.class18_0 = class18_5.class18_0;
      if (class18_5.class18_2 == null)
      {
        if (class18_5.Equals((object) class18_5.class18_4.class18_2))
          class18_5.class18_4.class18_2 = class18_5.class18_3;
        else
          class18_5.class18_4.class18_3 = class18_5.class18_3;
        if (class18_5.class18_3 != null)
          class18_5.class18_3.class18_4 = class18_5.class18_4;
      }
      else if (class18_5.class18_3 == null)
      {
        if (class18_5.Equals((object) class18_5.class18_4.class18_2))
          class18_5.class18_4.class18_2 = class18_5.class18_2;
        else
          class18_5.class18_4.class18_3 = class18_5.class18_2;
        if (class18_5.class18_2 != null)
          class18_5.class18_2.class18_4 = class18_5.class18_4;
      }
      else
      {
        if (class18_5.Equals((object) class18_5.class18_4.class18_2))
          class18_5.class18_4.class18_2 = class18_5.class18_3;
        else
          class18_5.class18_4.class18_3 = class18_5.class18_3;
        class18_5.class18_3.class18_4 = class18_5.class18_4;
        Class18<Tkey, Tvalue> class18 = class18_5.class18_1;
        while (class18.class18_2 != null)
          class18 = class18.class18_2;
        class18.class18_2 = class18_5.class18_2;
        class18_5.class18_2.class18_4 = class18;
      }
      class18_5.class18_2 = (Class18<Tkey, Tvalue>) null;
      class18_5.class18_3 = (Class18<Tkey, Tvalue>) null;
      class18_5.method_4();
      Class18<Tkey, Tvalue>.pooledObjectFactory_0.Free(class18_5);
    }

    public static void smethod_1(Class18<Tkey, Tvalue> class18_5)
    {
      Class18<Tkey, Tvalue> class18_5_1 = class18_5;
      while (class18_5_1.class18_4 != null)
        class18_5_1 = class18_5_1.class18_4;
      Class18<Tkey, Tvalue> class18 = class18_5_1.method_0();
      uint uint1 = class18.uint_1;
      int num = 0;
      for (; class18 != null; class18 = class18.class18_0)
      {
        if (uint1 > class18.uint_1 || (int) uint1 == (int) class18.uint_1 && (int) uint1 != 0)
          throw new Exception("Tree failed link verification.");
        ++num;
        uint1 = class18.uint_1;
      }
      int int_0 = 0;
      Class18<Tkey, Tvalue>.smethod_2(class18_5_1, ref int_0);
      if (num != int_0)
        throw new Exception("Tree failed comparison verification.");
    }

    public static void smethod_2(Class18<Tkey, Tvalue> class18_5, ref int int_0)
    {
      bool flag = false;
      if (class18_5.class18_3 != null && class18_5.class18_3.uint_1 >= class18_5.uint_1)
        flag = true;
      if (class18_5.class18_2 != null && class18_5.class18_2.uint_1 <= class18_5.uint_1)
        flag = true;
      if (flag)
        throw new Exception("Tree failed map verification.");
      if (class18_5.class18_3 != null)
        Class18<Tkey, Tvalue>.smethod_2(class18_5.class18_3, ref int_0);
      if (class18_5.class18_2 != null)
        Class18<Tkey, Tvalue>.smethod_2(class18_5.class18_2, ref int_0);
      ++int_0;
    }
  }
}
