using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Data.Mesh;

using BoundingBox = Microsoft.Xna.Framework.BoundingBox;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

// TODO Phase 2: rebuild against MonoGame's ModelMesh layout (IndexBuffer/VertexBuffer
// moved to ModelMeshPart) and replace SunBurn SceneObject with native rendering.
// XNAnimation/SgMotion removed in Phase 1.9, so SkinnedModel paths are stubbed.
public sealed class StaticMesh : IDisposable
{
    public string Name { get; set; }

    // this is the RawMesh data from MeshImporter
    public Array<MeshData> RawMeshes { get; set; } = new();

    // data from Model and SkinnedModel
    public ModelMeshCollection ModelMeshes;
    public readonly BoundingBox Bounds;
    public readonly float Radius;

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

    // TODO Phase 2: ModelMesh.IndexBuffer/VertexBuffer don't exist in MonoGame
    // (moved to ModelMeshPart). Stubbed; rebuild against MonoGame's API.
    public static void DisposeModelMeshes(ModelMeshCollection meshes) { }

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
            return so;
        }
        catch (Exception e)
        {
            Log.Error(e, $"CreateSceneObject failed: {Name}");
            return null;
        }
    }

    // TODO Phase 2: Draw paths used XNA 3.1 patterns (effect.Begin/End, pass.Begin/End,
    // gd.Vertices[0].SetSource, the 6-arg DrawIndexedPrimitives) that no longer exist in MonoGame.
    // Stubbed to no-ops so the rest of the game can compile and tick.
    public void Draw(Effect effect = null) { }
    public void Draw(BasicEffect effect, Texture2D texture) { }
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
