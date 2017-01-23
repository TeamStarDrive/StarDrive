// Decompiled with JetBrains decompiler
// Type: ns9.Class70
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using System;
using System.Collections.Generic;

namespace ns9
{
  internal class Class70
  {
    private Vector2[] vector2_0 = new Vector2[4]{ new Vector2(0.0f, 0.0f), new Vector2(1f, 0.0f), new Vector2(0.0f, 1f), new Vector2(1f, 1f) };
    private Struct1[] struct1_0 = new Struct1[4096];
    private List<Class69> list_0 = new List<Class69>(32);
    private static ushort[] ushort_0 = new ushort[6144];
    internal const int int_0 = 1024;
    internal const int int_1 = 4;
    internal const int int_2 = 6;
    private Effect effect_0;
    private GraphicsDevice graphicsDevice_0;
    private int int_3;
    private Class69 class69_0;
    private Class22<Class69> class22_0;

    public Effect Effect
    {
      get
      {
        return this.effect_0;
      }
    }

    public List<Class69> Buffers
    {
      get
      {
        return this.list_0;
      }
    }

    static Class70()
    {
      int num1 = 0;
      for (int index1 = 0; index1 < 1024; ++index1)
      {
        int num2 = index1 * 4;
        ushort[] ushort0_1 = Class70.ushort_0;
        int index2 = num1;
        int num3 = 1;
        int num4 = index2 + num3;
        int num5 = (int) (ushort) num2;
        ushort0_1[index2] = (ushort) num5;
        ushort[] ushort0_2 = Class70.ushort_0;
        int index3 = num4;
        int num6 = 1;
        int num7 = index3 + num6;
        int num8 = (int) (ushort) (num2 + 1);
        ushort0_2[index3] = (ushort) num8;
        ushort[] ushort0_3 = Class70.ushort_0;
        int index4 = num7;
        int num9 = 1;
        int num10 = index4 + num9;
        int num11 = (int) (ushort) (num2 + 2);
        ushort0_3[index4] = (ushort) num11;
        ushort[] ushort0_4 = Class70.ushort_0;
        int index5 = num10;
        int num12 = 1;
        int num13 = index5 + num12;
        int num14 = (int) (ushort) (num2 + 2);
        ushort0_4[index5] = (ushort) num14;
        ushort[] ushort0_5 = Class70.ushort_0;
        int index6 = num13;
        int num15 = 1;
        int num16 = index6 + num15;
        int num17 = (int) (ushort) (num2 + 1);
        ushort0_5[index6] = (ushort) num17;
        ushort[] ushort0_6 = Class70.ushort_0;
        int index7 = num16;
        int num18 = 1;
        num1 = index7 + num18;
        int num19 = (int) (ushort) (num2 + 3);
        ushort0_6[index7] = (ushort) num19;
      }
    }

    public Class70(GraphicsDevice device, Class22<Class69> bufferfactory, Effect effect)
    {
      this.graphicsDevice_0 = device;
      this.class22_0 = bufferfactory;
      this.effect_0 = effect;
      for (int index = 0; index < this.struct1_0.Length; ++index)
        this.struct1_0[index].vector3_1 = new Vector3(0.0f, 0.0f, -1f);
    }

    public unsafe void method_0(ref Vector2 vector2_1, ref Vector2 vector2_2, float float_0, ref Vector2 vector2_3, ref Vector2 vector2_4, ref Vector2 vector2_5, float float_1)
    {
      if (this.class69_0 == null || this.int_3 >= 1024)
      {
        this.method_1();
        this.method_2();
      }
      int index1 = this.int_3 * 4;
      if (index1 + 4 > this.struct1_0.Length)
        throw new Exception("Unable to build sprite, vertex array to small for all vertices.");
      bool flag;
      float num1;
      float num2;
      if (flag = (double) float_0 != 0.0)
      {
        num1 = (float) Math.Sin((double) float_0);
        num2 = (float) Math.Cos((double) float_0);
      }
      else
      {
        num1 = 0.0f;
        num2 = 1f;
      }
      fixed (Struct1* struct1Ptr1 = &this.struct1_0[index1])
        fixed (Vector2* vector2Ptr1 = this.vector2_0)
        {
          Struct1* struct1Ptr2 = struct1Ptr1;
          Vector2* vector2Ptr2 = vector2Ptr1;
          float num3 = 0.5f - vector2_3.X;
          float num4 = 0.5f - vector2_3.Y;
          for (int index2 = 0; index2 < 4; ++index2)
          {
            struct1Ptr2->vector2_0.X = vector2Ptr2->X * vector2_4.X + vector2_5.X;
            struct1Ptr2->vector2_0.Y = vector2Ptr2->Y * vector2_4.Y + vector2_5.Y;
            float num5 = vector2Ptr2->X - num3;
            float num6 = vector2Ptr2->Y - num4;
            if (flag)
            {
              float num7 = (float) ((double) num5 * (double) num2 - (double) num6 * (double) num1);
              num6 = (float) ((double) num5 * (double) num1 + (double) num6 * (double) num2);
              num5 = num7;
            }
            struct1Ptr2->vector3_0.X = num5 * vector2_1.X + vector2_2.X;
            struct1Ptr2->vector3_0.Y = num6 * vector2_1.Y + vector2_2.Y;
            struct1Ptr2->vector3_0.Z = float_1;
            struct1Ptr2->vector3_2.X = -num1;
            struct1Ptr2->vector3_2.Y = num2;
            struct1Ptr2->vector3_2.Z = num1;
            ++struct1Ptr2;
            ++vector2Ptr2;
          }
        }
      ++this.int_3;
    }

    public void method_1()
    {
      if (this.class69_0 == null || this.int_3 < 1)
        return;
      this.class69_0.method_0(this.graphicsDevice_0, this.struct1_0, Class70.ushort_0, this.int_3);
    }

    private void method_2()
    {
      this.class69_0 = this.class22_0.New();
      this.list_0.Add(this.class69_0);
      this.int_3 = 0;
    }

    public void method_3()
    {
      foreach (Class69 class69 in this.list_0)
        this.class22_0.Free(class69);
      this.class69_0 = (Class69) null;
      this.list_0.Clear();
      this.int_3 = 0;
    }
  }
}
