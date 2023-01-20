// Decompiled with JetBrains decompiler
// Type: ns10.Class72
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns11;
using ns3;
using SynapseGaming.LightingSystem.Core;
#pragma warning disable CA2213

namespace ns10
{
  internal sealed class Class72 : IDisposable
  {
    private static List<Struct2> list_2 = new List<Struct2>();
    private Class18<int, Rectangle> class18_0 = new Class18<int, Rectangle>(128U);
    private Rectangle[] rectangle_0 = new Rectangle[2];
    private List<Rectangle> list_0 = new List<Rectangle>(8);
    private List<Rectangle> list_1 = new List<Rectangle>(8);
    private Class73 class73_0 = new Class73();
    private RenderTarget2D renderTarget2D_0;
    private int int_0;
    private int int_1;

    public RenderTarget2D RenderTarget => this.renderTarget2D_0;

      public Class72(GraphicsDevice device, int size, SurfaceFormat format)
    {
      this.renderTarget2D_0 = new RenderTarget2D(device, size, size, 1, format, LightingSystemManager.Instance.GetBestRenderTargetUsage());
      int num = size / 2;
      this.rectangle_0[0] = new Rectangle(0, 0, num, size);
      this.rectangle_0[1] = new Rectangle(num, 0, num, size);
      this.int_0 = size * size;
      this.method_6(this.rectangle_0[0]);
      this.method_6(this.rectangle_0[1]);
      this.method_4();
    }

    public void method_0()
    {
      this.class18_0.method_4();
      this.method_6(this.rectangle_0[0]);
      this.method_6(this.rectangle_0[1]);
      this.method_4();
    }

    public void Dispose()
    {
      this.method_0();
      renderTarget2D_0?.Dispose();
      renderTarget2D_0 = null;
    }

    public bool method_1()
    {
      return this.method_3() >= this.int_0;
    }

    private int method_2(Rectangle rectangle_1)
    {
      return Math.Min(rectangle_1.Width, rectangle_1.Height);
    }

    internal int method_3()
    {
      int num = 0;
      for (Class18<int, Rectangle> class18 = this.class18_0.method_0(); class18 != null; class18 = class18.class18_0)
      {
        foreach (Rectangle rectangle in class18.list_0)
          num += rectangle.Width * rectangle.Height;
      }
      return num;
    }

    private void method_4()
    {
      this.list_0.Clear();
      this.list_1.Clear();
      this.int_1 = this.method_3();
    }

    private void method_5()
    {
      foreach (Rectangle rectangle_1 in this.list_1)
        this.method_7(rectangle_1);
      foreach (Rectangle rectangle_1 in this.list_0)
      {
        int gparam_1 = this.method_2(rectangle_1);
        this.class18_0.method_3(gparam_1, (uint) gparam_1).list_0.Remove(rectangle_1);
      }
      this.list_0.Clear();
      this.list_1.Clear();
      if (this.method_3() != this.int_1)
        throw new Exception("Unable to rollback shadow cache data.");
    }

    private void method_6(Rectangle rectangle_1)
    {
      if (rectangle_1.Width < 1 || rectangle_1.Height < 1)
        return;
      this.method_7(rectangle_1);
      this.list_0.Add(rectangle_1);
    }

    private void method_7(Rectangle rectangle_1)
    {
      if (rectangle_1.Width < 1 || rectangle_1.Height < 1)
        return;
      int gparam_1 = this.method_2(rectangle_1);
      this.class18_0.method_3(gparam_1, (uint) gparam_1).list_0.Add(rectangle_1);
    }

    private bool method_8(int int_2, ref Rectangle rectangle_1)
    {
      if (int_2 < 1)
        return false;
      for (Class18<int, Rectangle> class18 = this.class18_0.method_3(int_2, (uint) int_2); class18 != null; class18 = class18.class18_0)
      {
        for (int index = 0; index < class18.list_0.Count; ++index)
        {
          Rectangle rectangle = class18.list_0[index];
          if (rectangle.Width >= int_2)
          {
            class18.list_0.RemoveAt(index);
            this.list_1.Add(rectangle);
            Rectangle rectangle_1_1 = new Rectangle();
            Rectangle rectangle_1_2 = new Rectangle();
            rectangle_1_1.X = rectangle.X + int_2;
            rectangle_1_1.Y = rectangle.Y;
            rectangle_1_1.Width = rectangle.Width - int_2;
            rectangle_1_1.Height = int_2;
            this.method_6(rectangle_1_1);
            rectangle_1_2.X = rectangle.X;
            rectangle_1_2.Y = rectangle.Y + int_2;
            rectangle_1_2.Width = rectangle.Width;
            rectangle_1_2.Height = rectangle.Height - int_2;
            this.method_6(rectangle_1_2);
            rectangle_1.X = rectangle.X;
            rectangle_1.Y = rectangle.Y;
            rectangle_1.Width = int_2;
            rectangle_1.Height = int_2;
            return true;
          }
        }
      }
      return false;
    }

    public bool method_9(List<Rectangle> list_3)
    {
      list_2.Clear();
      for (int index = 0; index < list_3.Count; ++index)
        list_2.Add(new Struct2
        {
          int_0 = index,
          rectangle_0 = list_3[index]
        });
      list_2.Sort(this.class73_0);
      for (int index = 0; index < list_2.Count; ++index)
      {
        Struct2 struct2 = list_2[index];
        if (this.method_8(struct2.rectangle_0.Height, ref struct2.rectangle_0))
        {
          list_3[struct2.int_0] = struct2.rectangle_0;
        }
        else
        {
          this.method_5();
          return false;
        }
      }
      this.method_4();
      return true;
    }

    private class Class73 : IComparer<Struct2>
    {
      public int Compare(Struct2 struct2_0, Struct2 struct2_1)
      {
        return struct2_1.rectangle_0.Height - struct2_0.rectangle_0.Height;
      }
    }

    private struct Struct2
    {
      public int int_0;
      public Rectangle rectangle_0;
    }
  }
}
