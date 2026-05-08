using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Data.Mesh;

using BoundingBox = Microsoft.Xna.Framework.BoundingBox;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

// Phase 2.8.C: native MonoGame rendering wired (SceneObject + RenderableMesh fed
// from RawMeshes via MeshImporter; ModelMesh path operates on the new MeshPart
// layout). SkinnedModel paths remain stubbed pending Phase 3.6 (SgMotion / XNAnimation
// extraction + runtime BoneAnimationPlayer).
public sealed class StaticMesh : IDisposable
{
    public string Name { get; set; }

    // this is the RawMesh data from MeshImporter
    public Array<MeshData> RawMeshes { get; set; } = new();

    // data from Model and SkinnedModel
    public ModelMeshCollection ModelMeshes;
    public readonly BoundingBox Bounds;
    public readonly float Radius;

    // Phase 3.10.B.3: optional skin + animation payload populated by
    // MeshImporter when the loaded FBX has FbxSkin/FbxCluster + FbxAnimStack
    // data. Both arrays are null on static meshes (the common case).
    public SkinnedBoneData[] SkinnedBones;
    public AnimationClipData[] AnimationClips;
    public bool IsSkinned => SkinnedBones != null && SkinnedBones.Length > 0;

    public StaticMesh(string name, in BoundingBox bounds)
    {
        Name = name;
        Bounds = bounds;
        Radius = bounds.Radius();
    }

    ~StaticMesh() { Dispose(false); }

    public bool IsDisposed => ModelMeshes == null && RawMeshes.IsEmpty;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        RawMeshes.ClearAndDispose();
        ModelMeshes = null;
    }

    // Note: ModelMesh.IndexBuffer/VertexBuffer don't exist in MonoGame (moved to
    // ModelMeshPart); buffer disposal happens at ModelMeshPart granularity, which
    // GameContentManager.UnloadAsset handles directly. This helper is a no-op
    // preserved for legacy call sites; safe to remove if confirmed unused.

    public static bool IsModelDisposed(Model m) => m == null || m.Meshes.Count == 0;
    public static void DisposeModel(Model m) { }

    /// <summary>
    /// Loads a cached StaticMesh from GameContentManager.
    /// </summary>
    /// <returns>`null` on failure, otherwise a valid StaticMesh</returns>
    public static StaticMesh LoadMesh(GameContentManager content, string modelName, bool animated = false)
    {
        try
        {
            var c = content ?? ResourceManager.RootContent;
            return c.LoadStaticMesh(modelName, animated);
        }
        catch (Exception e)
        {
            Log.Error(e, $"LoadMesh failed: {modelName}");
            return null;
        }
    }

    /// <summary>
    /// NOTE: StaticMesh will take ownership of `Model`
    /// </summary>
    public static StaticMesh FromStaticModel(string modelName, Model model)
    {
        var bounds = GetBoundingBox(model);
        return new(modelName, bounds)
        {
            ModelMeshes = model.Meshes
        };
    }

    static BoundingBox GetBoundingBox(Model model)
    {
        BoundingBox bounds = default;
        foreach (ModelMesh m in model.Meshes)
        {
            var bb = BoundingBox.CreateFromSphere(m.BoundingSphere);
            if (m.ParentBone != null) // scale the bounds according to the parent bone
            {
                var mat = m.ParentBone.Transform;
                bb.Min = XnaVector3.Transform(bb.Min, mat);
                bb.Max = XnaVector3.Transform(bb.Max, mat);
            }
            bounds = bounds == default ? bb : bounds.Join(bb);
        }
        return bounds;
    }

    public SceneObject CreateSceneObject(ObjectType type = ObjectType.Dynamic, Effect effect = null)
    {
        try
        {
            var so = new SceneObject(Name) { ObjectType = type };
            // §4.6 #1.b: propagate mesh-space bounds onto the SceneObject so
            // downstream consumers (SceneObj.HalfLength → FTL radius, hull
            // size estimates, debug overlays) see real values instead of the
            // default (0,0,0)–(0,0,0) box. Pre-fix this was always zero,
            // collapsing FTL warp effects to a 0-scale sprite (invisible) on
            // the MainMenu scene where ships warp on a cycle.
            so.WorldBoundingBox = Bounds;
            so.ObjectBoundingSphere = new Microsoft.Xna.Framework.BoundingSphere(
                XnaVector3.Lerp(Bounds.Min, Bounds.Max, 0.5f),
                Radius);
            if (ModelMeshes != null)
            {
                foreach (ModelMesh mesh in ModelMeshes)
                    so.Add(mesh, effect);
            }
            else
            {
                foreach (MeshData mesh in RawMeshes)
                {
                    so.Add(new RenderableMesh(so,
                        effect ?? mesh.Effect,
                        mesh.MeshToObject,
                        mesh.ObjectSpaceBoundingSphere,
                        mesh.IndexBuffer,
                        mesh.VertexBuffer,
                        mesh.VertexDeclaration, 0,
                        PrimitiveType.TriangleList,
                        mesh.PrimitiveCount,
                        0, mesh.VertexCount,
                        0, mesh.VertexStride));
                }
            }

            // Phase 3.10.B.7: auto-attach a per-instance animation player and
            // auto-play the first clip looping. Each SO gets its own player so
            // separately-spawned instances animate at independent phases. No
            // gameplay code wires this — UpdateAnimation is already called
            // per-frame from Ship_Update etc.
            if (IsSkinned)
            {
                var player = new BoneAnimationPlayer(SkinnedBones, AnimationClips);
                if (player.HasClips)
                    player.StartClip(0);
                so.AnimationPlayer = player;
            }
            return so;
        }
        catch (Exception e)
        {
            Log.Error(e, $"CreateSceneObject failed: {Name}");
            return null;
        }
    }

    // Phase 2.8 sub-phase A2/B4: forward-renderer Draw. Iterates ModelMesh.MeshParts
    // and RawMeshes (MeshData), binds buffers, loops technique passes and issues
    // DrawIndexedPrimitives. The Draw(Effect) overload is the single source of truth;
    // the device-aware overload below sets W/V/P on the LightingEffect, and the
    // BasicEffect+Texture overload binds the texture, then both delegate here.
    // Empty-mesh guard early-returns if both backing collections are empty (the
    // common case while Phase 2 model XNB loads return stub meshes).
    public void Draw(Effect effect = null)
    {
        if (effect == null) return;
        GraphicsDevice device = effect.GraphicsDevice;
        EffectTechnique technique = effect.CurrentTechnique;
        if (device == null || technique == null) return;

        bool hasModelMeshes = ModelMeshes != null && ModelMeshes.Count > 0;
        bool hasRawMeshes = !RawMeshes.IsEmpty;
        if (!hasModelMeshes && !hasRawMeshes) return;

        if (hasModelMeshes)
        {
            foreach (ModelMesh mesh in ModelMeshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    if (part.PrimitiveCount == 0) continue;
                    device.SetVertexBuffer(part.VertexBuffer);
                    device.Indices = part.IndexBuffer;
                    foreach (EffectPass pass in technique.Passes)
                    {
                        pass.Apply();
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                            part.VertexOffset, part.StartIndex, part.PrimitiveCount);
                    }
                }
            }
        }

        if (hasRawMeshes)
        {
            foreach (MeshData md in RawMeshes)
            {
                if (md.PrimitiveCount == 0 || md.VertexBuffer == null || md.IndexBuffer == null) continue;
                device.SetVertexBuffer(md.VertexBuffer);
                device.Indices = md.IndexBuffer;
                foreach (EffectPass pass in technique.Passes)
                {
                    pass.Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                        baseVertex: 0, startIndex: 0, primitiveCount: md.PrimitiveCount);
                }
            }
        }
    }

    public void Draw(GraphicsDevice device, XnaMatrix world, XnaMatrix view, XnaMatrix projection, LightingEffect effect)
    {
        if (effect == null || device == null) return;
        effect.View = view;
        effect.Projection = projection;
        effect.World = world;
        Draw((Effect)effect);
    }

    public void Draw(BasicEffect effect, Texture2D texture)
    {
        if (effect == null) return;
        effect.Texture = texture;
        effect.TextureEnabled = texture != null;
        Draw((Effect)effect);
    }

    // TODO Phase 4: static Draw(Model,...) overloads have no current callers; preserved as
    // no-ops in case mod / test code relies on the legacy signatures. Remove if confirmed unused.
    public static void Draw(Model model, Effect effect) { }
    public static void Draw(Model model, BasicEffect effect, Texture2D texture) { }

    public Effect GetFirstEffect() => GetEffects().FirstOrDefault();

    public T GetFirstEffect<T>() where T : Effect => GetEffects<T>().FirstOrDefault();

    public IEnumerable<Effect> GetEffects()
    {
        if (ModelMeshes != null)
            foreach (var mesh in ModelMeshes)
                foreach (var effect in mesh.Effects)
                    yield return effect;
        else
            foreach (var mesh in RawMeshes)
                yield return mesh.Effect;
    }

    public IEnumerable<T> GetEffects<T>() where T : Effect
    {
        foreach (Effect effect in GetEffects())
            if (effect is T fx)
                yield return fx;
    }
}
