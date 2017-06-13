// Decompiled with JetBrains decompiler
// Type: ns9.Class64
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects;
using SynapseGaming.LightingSystem.Rendering;

namespace Mesh
{
  internal class Class64
  {
    private static TrackingPool<Class63> class21_0 = new TrackingPool<Class63>();

    public void method_0(Matrix matrix_0, Matrix matrix_1, Matrix matrix_2, Matrix matrix_3, List<Class63> list_0, List<RenderableMesh> list_1, Enum7 enum7_0)
    {
      list_0.Clear();
      for (int index = 0; index < list_1.Count; ++index)
      {
        RenderableMesh renderableMesh = list_1[index];
        if (renderableMesh != null)
          renderableMesh.bool_1 = false;
      }
      for (int index1 = 0; index1 < list_1.Count; ++index1)
      {
        RenderableMesh renderableMesh_0_1 = list_1[index1];
        if (renderableMesh_0_1 != null && !renderableMesh_0_1.bool_1)
        {
          Effect effect0 = renderableMesh_0_1.effect_0;
          if (effect0 is ILightingEffect)
          {
            if ((enum7_0 & Enum7.flag_0) == 0)
              continue;
          }
          else if (!(effect0 is BasicEffect) && (enum7_0 & Enum7.flag_3) == 0)
            continue;
          bool flag = false;
          if (effect0 is IRenderableEffect)
          {
            (effect0 as IRenderableEffect).SetViewAndProjection(matrix_0, matrix_1, matrix_2, matrix_3);
            flag = true;
          }
          else if (effect0 is BasicEffect)
          {
            BasicEffect basicEffect = effect0 as BasicEffect;
            if ((!basicEffect.LightingEnabled || (enum7_0 & Enum7.flag_1) != 0) && (basicEffect.LightingEnabled || (enum7_0 & Enum7.flag_2) != 0))
            {
              basicEffect.View = matrix_0;
              basicEffect.Projection = matrix_2;
              flag = true;
            }
            else
              continue;
          }
          Class63 class63 = class21_0.New();
          class63.method_0();
          class63.Effect = effect0;
          list_0.Add(class63);
          class63.Objects.method_0(renderableMesh_0_1);
          renderableMesh_0_1.bool_1 = true;
          if (flag)
          {
            for (int index2 = index1 + 1; index2 < list_1.Count; ++index2)
            {
              RenderableMesh renderableMesh_0_2 = list_1[index2];
              if (renderableMesh_0_2 != null && !renderableMesh_0_2.bool_1 && renderableMesh_0_1.int_6 == renderableMesh_0_2.int_6)
              {
                class63.Objects.method_0(renderableMesh_0_2);
                renderableMesh_0_2.bool_1 = true;
              }
            }
          }
        }
      }
    }

    public void method_1(List<Class63> list_0, List<RenderableMesh> list_1, bool bool_0, bool bool_1)
    {
      list_0.Clear();
      for (int index = 0; index < list_1.Count; ++index)
      {
        RenderableMesh renderableMesh = list_1[index];
        if (renderableMesh != null)
          renderableMesh.bool_1 = false;
      }
      for (int index1 = 0; index1 < list_1.Count; ++index1)
      {
        RenderableMesh renderableMesh_0_1 = list_1[index1];
        if (renderableMesh_0_1 != null && !renderableMesh_0_1.bool_1 && (!bool_0 || renderableMesh_0_1.bool_0))
        {
          Class63 class63 = class21_0.New();
          class63.method_0();
          class63.Effect = renderableMesh_0_1.effect_0;
          class63.Transparent = renderableMesh_0_1.bool_2;
          class63.DoubleSided = renderableMesh_0_1.bool_3;
          class63.CustomShadowGeneration = renderableMesh_0_1.bool_4;
          class63.Objects.method_0(renderableMesh_0_1);
          list_0.Add(class63);
          renderableMesh_0_1.bool_1 = true;
          for (int index2 = index1 + 1; index2 < list_1.Count; ++index2)
          {
            RenderableMesh renderableMesh_0_2 = list_1[index2];
            if (renderableMesh_0_2 != null && !renderableMesh_0_2.bool_1 && (!bool_0 || renderableMesh_0_2.bool_0) && (renderableMesh_0_1.int_6 == renderableMesh_0_2.int_6 || !renderableMesh_0_1.bool_2 && !renderableMesh_0_2.bool_2 && (!renderableMesh_0_1.bool_4 && !renderableMesh_0_2.bool_4) && (renderableMesh_0_1.bool_3 == renderableMesh_0_2.bool_3 && !renderableMesh_0_1.bool_5 && !renderableMesh_0_2.bool_5)))
            {
              class63.Objects.method_0(renderableMesh_0_2);
              renderableMesh_0_2.bool_1 = true;
            }
          }
        }
      }
    }

    public void method_2()
    {
      class21_0.RecycleAllTracked();
    }
  }
}
