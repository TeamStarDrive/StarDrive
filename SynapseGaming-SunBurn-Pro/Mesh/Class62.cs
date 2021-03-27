// Decompiled with JetBrains decompiler
// Type: ns9.Class62
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using SynapseGaming.LightingSystem.Effects;
using SynapseGaming.LightingSystem.Rendering;

namespace Mesh
{
    internal class MeshContainer
    {
        public List<RenderableMesh> All { get; } = new List<RenderableMesh>(128);

        public List<RenderableMesh> NonSkinned { get; } = new List<RenderableMesh>(128);

        public List<RenderableMesh> Skinned { get; } = new List<RenderableMesh>(128);

        public void Add(RenderableMesh mesh)
        {
            if (mesh == null)
                return;
            All.Add(mesh);
            if (mesh.effect is ISkinnedEffect && (mesh.effect as ISkinnedEffect).Skinned)
                Skinned.Add(mesh);
            else
                NonSkinned.Add(mesh);
        }

        public void Clear()
        {
            All.Clear();
            Skinned.Clear();
            NonSkinned.Clear();
        }
    }
}
