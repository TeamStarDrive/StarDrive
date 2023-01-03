using System;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using SgMotion;
using SgMotion.Controllers;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Data.Mesh;

using BoundingBox = Microsoft.Xna.Framework.BoundingBox;

// TODO: Rename into something else. This now supports animations, trying to unify the clusterfk of XNA models
public class StaticMesh : IDisposable
{
    public string Name { get; set; }

    // this is the RawMesh data from MeshImporter
    public Array<MeshData> RawMeshes { get; set; } = new();

    // data from Model and SkinnedModel
    public ModelMeshCollection ModelMeshes;
    public BoundingBox Bounds;

    // used if animation is enabled
    public SkinnedModelBoneCollection Skeleton;
    public AnimationClipDictionary AnimationClips;

    public StaticMesh() { }
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

    static StaticMesh FromNewMesh(GameContentManager content, string modelName)
    {
        StaticMesh mesh = content.LoadStaticMesh(modelName);
        return mesh;
    }

    public static StaticMesh FromSkinnedModel(string meshName, SkinnedModel skinned)
    {
        StaticMesh mesh = new()
        {
            Name = meshName,
            Skeleton = skinned.SkeletonBones,
            AnimationClips = skinned.AnimationClips,
            ModelMeshes = skinned.Model.Meshes,
            Bounds = GetBoundingBox(skinned.Model),
        };
        return mesh;
    }

    public static StaticMesh FromStaticModel(string modelName, Model model)
    {
        StaticMesh mesh = new()
        {
            ModelMeshes = model.Meshes,
            Bounds = GetBoundingBox(model),
        };
        return mesh;
    }

    static BoundingBox GetBoundingBox(Model model)
    {
        BoundingBox bounds = default;
        foreach (ModelMesh m in model.Meshes)
        {
            bounds = bounds.Join(BoundingBox.CreateFromSphere(m.BoundingSphere));
        }
        return bounds;
    }

    void CreateAnimation(SceneObject so)
    {
        if (AnimationClips != null)
        {
            so.Animation = new AnimationController(Skeleton)
            {
                TranslationInterpolation = InterpolationMode.Linear,
                OrientationInterpolation = InterpolationMode.Linear,
                ScaleInterpolation = InterpolationMode.Linear,
                Speed = 0.5f
            };
            so.Animation.StartClip(AnimationClips.Values[0]);
        }
    }

    public SceneObject CreateSceneObject()
    {
        try
        {
            var so = new SceneObject(Name) { ObjectType = ObjectType.Dynamic };
            if (ModelMeshes != null)
            {
                foreach (ModelMesh mesh in ModelMeshes)
                    so.Add(mesh);
            }
            else
            {
                foreach (MeshData mesh in RawMeshes)
                {
                    so.Add(new RenderableMesh(so,
                        mesh.Effect,
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

    delegate void DrawDelegate(ModelMeshPart mesh);
    static DrawDelegate ModelMeshDraw;

    // Draw a model with a custom material effect override
    // TODO: Instead of using ModelMesh, implement Draw() for StaticMesh by looking at ModelMesh impl.
    public static void Draw(Model model, Effect effect)
    {
        if (ModelMeshDraw == null)
        {
            var draw = typeof(ModelMeshPart).GetMethod("Draw", BindingFlags.NonPublic|BindingFlags.Instance);
            ModelMeshDraw = (DrawDelegate)draw.CreateDelegate(typeof(DrawDelegate));
        }

        var passes = effect.CurrentTechnique.Passes;
        int numPasses = passes.Count;
        effect.Begin(SaveStateMode.None);
        try
        {
            for (int passIdx = 0; passIdx < numPasses; ++passIdx)
            {
                EffectPass pass = passes[passIdx];
                pass.Begin();
                    
                int numMeshes = model.Meshes.Count;
                for (int i = 0; i < numMeshes; ++i)
                {
                    ModelMesh mesh = model.Meshes[i];
                    int numParts = mesh.MeshParts.Count;
                    for (int meshPartIdx = 0; meshPartIdx < numParts; ++meshPartIdx)
                    {
                        ModelMeshPart meshPart = mesh.MeshParts[meshPartIdx];
                        ModelMeshDraw(meshPart);
                    }
                }

                pass.End();
            }
        }
        finally
        {
            effect.End();
        }
    }

    public static void Draw(Model model, BasicEffect effect, Texture2D texture)
    {
        effect.Texture = texture;
        Draw(model, effect);
    }

    public static SceneObject SceneObjectFromModel(Model model, Effect effect)
    {
        var so = new SceneObject(model.Root.Name) { ObjectType = ObjectType.Dynamic };
        foreach (ModelMesh mesh in model.Meshes)
            so.Add(mesh, effect);
        return so;
    }

    /// <summary>
    /// Loads a cached StaticMesh from GameContentManager. If StaticMesh is already loaded, no extra loading is done.
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
}
