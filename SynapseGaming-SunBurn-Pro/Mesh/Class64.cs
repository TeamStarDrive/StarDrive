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
    private static TrackingPool<EffectMeshGroup> Pool = new TrackingPool<EffectMeshGroup>();

    public void CreateGroups(Matrix matrix_0, Matrix matrix_1, Matrix matrix_2, Matrix matrix_3, List<EffectMeshGroup> groups, List<RenderableMesh> meshes, Enum7 enum7_0)
    {
      groups.Clear();
      for (int i = 0; i < meshes.Count; ++i)
      {
        RenderableMesh mesh = meshes[i];
        if (mesh != null)
          mesh.AddedToEffectGroup = false;
      }
      for (int index1 = 0; index1 < meshes.Count; ++index1)
      {
        RenderableMesh mesh = meshes[index1];
        if (mesh != null && !mesh.AddedToEffectGroup)
        {
          Effect effect0 = mesh.effect;
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
          EffectMeshGroup group = Pool.New();
          group.Initialize();
          group.Effect = effect0;
          groups.Add(group);
          group.Objects.Add(mesh);
          mesh.AddedToEffectGroup = true;
          if (flag)
          {
            for (int index2 = index1 + 1; index2 < meshes.Count; ++index2)
            {
              RenderableMesh mesh2 = meshes[index2];
              if (mesh2 != null && !mesh2.AddedToEffectGroup && mesh.EffectHash == mesh2.EffectHash)
              {
                group.Objects.Add(mesh2);
                mesh2.AddedToEffectGroup = true;
              }
            }
          }
        }
      }
    }

    public void method_1(List<EffectMeshGroup> groups, List<RenderableMesh> meshes, bool bool_0, bool bool_1)
    {
      groups.Clear();
      for (int i = 0; i < meshes.Count; ++i)
      {
        RenderableMesh mesh = meshes[i];
        if (mesh != null)
          mesh.AddedToEffectGroup = false;
      }
      for (int i = 0; i < meshes.Count; ++i)
      {
        RenderableMesh mesh = meshes[i];
        if (mesh != null && !mesh.AddedToEffectGroup && (!bool_0 || mesh.ShadowInFrustum))
        {
          EffectMeshGroup group = Pool.New();
          group.Initialize();
          group.Effect = mesh.effect;
          group.Transparent = mesh.HasTransparency;
          group.DoubleSided = mesh.IsDoubleSided;
          group.CustomShadowGeneration = mesh.SupportsShadows;
          group.Objects.Add(mesh);
          groups.Add(group);
          mesh.AddedToEffectGroup = true;
          for (int j = i + 1; j < meshes.Count; ++j)
          {
            RenderableMesh mesh2 = meshes[j];
            if (mesh2 != null && !mesh2.AddedToEffectGroup && (!bool_0 || mesh2.ShadowInFrustum) && (mesh.EffectHash == mesh2.EffectHash || !mesh.HasTransparency && !mesh2.HasTransparency && (!mesh.SupportsShadows && !mesh2.SupportsShadows) && (mesh.IsDoubleSided == mesh2.IsDoubleSided && !mesh.IsTerrain && !mesh2.IsTerrain)))
            {
              group.Objects.Add(mesh2);
              mesh2.AddedToEffectGroup = true;
            }
          }
        }
      }
    }

    public void ResetPool()
    {
      Pool.RecycleAllTracked();
    }
  }
}
