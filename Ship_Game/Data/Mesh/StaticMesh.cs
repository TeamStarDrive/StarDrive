using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using SgMotion;
using SgMotion.Controllers;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Data.Mesh;

using BoundingBox = Microsoft.Xna.Framework.BoundingBox;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

// TODO: Rename into something else. This now supports animations, trying to unify the clusterfk of XNA models
public class StaticMesh : IDisposable
{
    public string Name { get; set; }

    // this is the RawMesh data from MeshImporter
    public Array<MeshData> RawMeshes { get; set; } = new();

    // data from Model and SkinnedModel
    public ModelMeshCollection ModelMeshes;
    public readonly BoundingBox Bounds;
    public readonly float Radius;

    // used if animation is enabled
    public SkinnedModelBoneCollection Skeleton;
    public AnimationClipDictionary AnimationClips;

    public StaticMesh(string name, in BoundingBox bounds)
    {
        Name = name;
        Bounds = bounds;
        Radius = bounds.Radius();
    }

    ~StaticMesh() { Destroy(); }

    public bool IsDisposed => ModelMeshes == null && RawMeshes.IsEmpty;

    public void Dispose()
    {
        Destroy();
        GC.SuppressFinalize(this);
    }

    void Destroy()
    {
        RawMeshes.ClearAndDispose();
        if (ModelMeshes != null)
        {
            DisposeModelMeshes(ModelMeshes);
            ModelMeshes = null;
        }

        Skeleton = null;
        AnimationClips = null;
    }

    public static void DisposeModelMeshes(ModelMeshCollection meshes)
    {
        foreach (ModelMesh mesh in meshes)
        {
            if (!mesh.IndexBuffer.IsDisposed) mesh.IndexBuffer.Dispose();
            if (!mesh.VertexBuffer.IsDisposed) mesh.VertexBuffer.Dispose();
            foreach (var part in mesh.MeshParts)
                if (!part.VertexDeclaration.IsDisposed)
                    part.VertexDeclaration.Dispose();
        }
    }
    
    public static bool IsModelDisposed(Model m) => m.Meshes.Count == 0 || m.Meshes[0].IndexBuffer.IsDisposed;
    public static void DisposeModel(Model m) => DisposeModelMeshes(m.Meshes);

    public static bool IsModelDisposed(SkinnedModel sm) => IsModelDisposed(sm.Model);
    public static void DisposeModel(SkinnedModel sm) => DisposeModelMeshes(sm.Model.Meshes);
    
    /// <summary>
    /// Loads a cached StaticMesh from GameContentManager.
    /// If StaticMesh is already loaded, no extra loading is done.
    /// This method is mainly for those cases where modelName could potentially be null
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

    static StaticMesh FromNewMesh(GameContentManager content, string modelName)
    {
        StaticMesh mesh = content.LoadStaticMesh(modelName);
        return mesh;
    }
    
    /// <summary>
    /// NOTE: StaticMesh will take ownership of `SkinnedModel`
    /// </summary>
    public static StaticMesh FromSkinnedModel(string modelName, SkinnedModel skinned)
    {
        var bounds = GetBoundingBox(skinned.Model);
        StaticMesh mesh = new(modelName, bounds)
        {
            Skeleton = skinned.SkeletonBones,
            AnimationClips = skinned.AnimationClips,
            ModelMeshes = skinned.Model.Meshes
        };
        return mesh;
    }

    /// <summary>
    /// NOTE: StaticMesh will take ownership of `Model`
    /// </summary>
    public static StaticMesh FromStaticModel(string modelName, Model model)
    {
        var bounds = GetBoundingBox(model);
        StaticMesh mesh = new(modelName, bounds)
        {
            ModelMeshes = model.Meshes
        };
        return mesh;
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

    void CreateAnimation(SceneObject so)
    {
        if (AnimationClips != null)
        {
            so.Animation = new(Skeleton)
            {
                TranslationInterpolation = InterpolationMode.Linear,
                OrientationInterpolation = InterpolationMode.Linear,
                ScaleInterpolation = InterpolationMode.Linear,
                Speed = 0.5f
            };
            so.Animation.StartClip(AnimationClips.Values[0]);
        }
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
            CreateAnimation(so);
            return so;
        }
        catch (Exception e)
        {
            Log.Error(e, $"CreateSceneObject failed: {Name}");
            return null;
        }
    }

    /// <summary>
    /// Draws this StaticMesh
    /// </summary>
    /// <param name="effect">Optionally override the default Effect if there is any</param>
    public void Draw(Effect effect = null)
    {
        if (ModelMeshes != null)
            Draw(ModelMeshes, effect);
        else
            Draw(RawMeshes.AsSpan(), effect);
    }

    /// <summary>
    /// Draws this StaticMesh with a BasicEffect and Texture2D override.
    /// The original material is ignored.
    /// </summary>
    public void Draw(BasicEffect effect, Texture2D texture)
    {
        effect.Texture = texture;
        if (ModelMeshes != null)
            Draw(ModelMeshes, effect);
        else
            Draw(RawMeshes.AsSpan(), effect);
    }

    // Draw a model with a custom material effect override
    public static void Draw(Model model, Effect effect)
    {
        Draw(model.Meshes, effect);
    }
    
    // Draw a model with a custom material+texture effect override
    public static void Draw(Model model, BasicEffect effect, Texture2D texture)
    {
        effect.Texture = texture;
        Draw(model.Meshes, effect);
    }

    static void Draw(Span<MeshData> rawMeshes, Effect @override)
    {
        if (rawMeshes.IsEmpty)
            return;

        if (IsUsingASingleEffect(rawMeshes, @override, out Effect singleEffect))
        {
            var passes = singleEffect.CurrentTechnique.Passes;
            int numPasses = passes.Count;
            singleEffect.Begin(SaveStateMode.None);
            try
            {
                for (int passIdx = 0; passIdx < numPasses; ++passIdx)
                {
                    EffectPass pass = passes[passIdx];
                    pass.Begin();
                    foreach (MeshData mesh in rawMeshes)
                        DrawPrimitive(mesh);
                    pass.End();
                }
            }
            finally
            {
                singleEffect.End();
            }
        }
        else
        {
            foreach (MeshData mesh in rawMeshes)
            {
                Effect meshEffect = mesh.Effect;
                var passes = singleEffect.CurrentTechnique.Passes;
                int numPasses = passes.Count;
                meshEffect.Begin(SaveStateMode.None);
                try
                {
                    for (int passIdx = 0; passIdx < numPasses; ++passIdx)
                    {
                        EffectPass pass = passes[passIdx];
                        pass.Begin();
                        DrawPrimitive(mesh);
                        pass.End();
                    }
                }
                finally
                {
                    meshEffect.End();
                }
            }
        }
    }

    static void DrawPrimitive(MeshData mesh)
    {
        var vd = mesh.VertexDeclaration;
        GraphicsDevice gd = vd.GraphicsDevice;

        gd.VertexDeclaration = vd;
        gd.Vertices[0].SetSource(mesh.VertexBuffer, 0, mesh.VertexStride);
        gd.Indices = mesh.IndexBuffer;
        gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, mesh.VertexCount, 0, mesh.PrimitiveCount);
    }

    // Legacy XNA ModelMesh Draw
    static void Draw(ModelMeshCollection meshes, Effect effect)
    {
        int numMeshes = meshes.Count;
        if (numMeshes == 0)
            return;

        if (effect == null)
            throw new NullReferenceException("Cannot Draw ModelMeshCollection without a basic Effect");

        var passes = effect.CurrentTechnique.Passes;
        int numPasses = passes.Count;
        effect.Begin(SaveStateMode.None);
        try
        {
            for (int passIdx = 0; passIdx < numPasses; ++passIdx)
            {
                EffectPass pass = passes[passIdx];
                pass.Begin();
                for (int i = 0; i < numMeshes; ++i)
                {
                    ModelMesh mesh = meshes[i];
                    DrawPrimitive(mesh);
                }
                pass.End();
            }
        }
        finally
        {
            effect.End();
        }
    }

    static void DrawPrimitive(ModelMesh mesh)
    {
        int numParts = mesh.MeshParts.Count;
        for (int meshPartIdx = 0; meshPartIdx < numParts; ++meshPartIdx)
        {
            ModelMeshPart part = mesh.MeshParts[meshPartIdx];

            var vd = part.VertexDeclaration;
            GraphicsDevice gd = vd.GraphicsDevice;

            gd.VertexDeclaration = vd;
            gd.Vertices[0].SetSource(mesh.VertexBuffer, part.StreamOffset, part.VertexStride);
            gd.Indices = mesh.IndexBuffer;
            gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.BaseVertex, 0, 
                                         part.NumVertices, part.StartIndex, part.PrimitiveCount);
        }
    }
    
    /// <summary>
    /// Gets the first effect in this mesh, or null if there are no associated effects
    /// </summary>
    public Effect GetFirstEffect()
    {
        return GetEffects().FirstOrDefault();
    }

    /// <summary>
    /// Gets the first Effect which matches the Type parameter `T`, or null if no such effects
    /// </summary>
    public T GetFirstEffect<T>() where T : Effect
    {
        return GetEffects<T>().FirstOrDefault();
    }

    /// <summary>
    /// Enumerates all effects on this mesh, regardless of effect type
    /// </summary>
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

    /// <summary>
    /// Gets all effects which match the Type parameter `T`
    /// </summary>
    public IEnumerable<T> GetEffects<T>() where T : Effect
    {
        foreach (Effect effect in GetEffects())
            if (effect is T fx)
                yield return fx;
    }

    static bool IsUsingASingleEffect(Span<MeshData> rawMeshes, Effect @override, out Effect singleEffect)
    {
        if (@override != null) // this overrides everything, always a single effect
        {
            singleEffect = @override;
            return true;
        }

        Effect firstEffect = rawMeshes[0].Effect;
        for (int i = 1; i < rawMeshes.Length; ++i)
        {
            if (firstEffect != rawMeshes[i].Effect)
            {
                singleEffect = null;
                return false; // No, we have multiple effects
            }
        }
        singleEffect = firstEffect;
        return true;
    }
}
