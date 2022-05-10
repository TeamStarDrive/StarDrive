using System;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using SgMotion;
using SgMotion.Controllers;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Data.Mesh
{
    public class StaticMesh
    {
        public string Name { get; set; }
        public Array<MeshData> Meshes { get; set; } = new Array<MeshData>();
        public int Count => Meshes.Count;

        public SkinnedModelBoneCollection Skeleton;
        public AnimationClipDictionary AnimationClips;

        static int SubMeshCount(int maxSubMeshes, int meshSubMeshCount)
        {
            return maxSubMeshes == 0 ? meshSubMeshCount : Math.Min(maxSubMeshes, meshSubMeshCount);
        }

        static SceneObject FromFbx(GameContentManager content, string modelName, int maxSubMeshes = 0)
        {
            var so = new SceneObject(modelName) { ObjectType = ObjectType.Dynamic };
            try
            {
                StaticMesh staticMesh = content.LoadStaticMesh(modelName);
                int count = SubMeshCount(maxSubMeshes, staticMesh.Count);

                for (int i = 0; i < count; ++i)
                {
                    MeshData mesh = staticMesh.Meshes[i];

                    var renderable = new RenderableMesh(so,
                        mesh.Effect,
                        mesh.MeshToObject,
                        mesh.ObjectSpaceBoundingSphere,
                        mesh.IndexBuffer,
                        mesh.VertexBuffer,
                        mesh.VertexDeclaration, 0,
                        PrimitiveType.TriangleList,
                        mesh.PrimitiveCount,
                        0, mesh.VertexCount,
                        0, mesh.VertexStride);
                    so.Add(renderable);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"FromFbx failed: {modelName}");
            }
            return so;
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
            ModelMeshCollection meshes = model.Meshes;
            for (int i = 0; i < meshes.Count; ++i)
                so.Add(meshes[i], effect);
            return so;
        }

        public static SceneObject SceneObjectFromModel(
            GameContentManager content,
            string modelName,
            int maxSubMeshes = 0,
            Effect effect = null)
        {
            var so = new SceneObject(modelName) { ObjectType = ObjectType.Dynamic };
            try
            {
                Model model = content.LoadModel(modelName);
                ModelMeshCollection meshes = model.Meshes;
                int count = SubMeshCount(maxSubMeshes, meshes.Count);
                for (int i = 0; i < count; ++i)
                    so.Add(meshes[i], effect);
            }
            catch (Exception e)
            {
                Log.Error(e, $"SceneObjectFromModel failed: {modelName}");
            }
            return so;
        }

        static SceneObject SceneObjectFromSkinnedModel(GameContentManager content, string modelName)
        {
            var so = new SceneObject(modelName) { ObjectType = ObjectType.Dynamic };
            try
            {
                SkinnedModel skinned = content.LoadSkinnedModel(modelName);
                ModelMeshCollection meshes = skinned.Model.Meshes;
                for (int i = 0; i < meshes.Count; ++i)
                    so.Add(meshes[i]);

                so.Animation = new AnimationController(skinned.SkeletonBones)
                {
                    TranslationInterpolation = InterpolationMode.Linear,
                    OrientationInterpolation = InterpolationMode.Linear,
                    ScaleInterpolation = InterpolationMode.Linear,
                    Speed = 0.5f
                };
                so.Animation.StartClip(skinned.AnimationClips.Values[0]);
            }
            catch (Exception e)
            {
                Log.Error(e, $"SceneObjectFromSkinnedModel failed: {modelName}");
            }
            return so;
        }

        public static SceneObject GetSceneMesh(GameContentManager content, string modelName, bool animated = false)
        {
            content = content ?? ResourceManager.RootContent;
            if (RawContentLoader.IsSupportedMesh(modelName))
                return FromFbx(content, modelName);
            if (animated)
                return SceneObjectFromSkinnedModel(content, modelName);
            return SceneObjectFromModel(content, modelName);
        }
    }
}
