using System;
using Microsoft.Xna.Framework.Graphics;
using SgMotion;
using SgMotion.Controllers;
using SynapseGaming.LightingSystem.Core;
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

        static SceneObject SceneObjectFromModel(GameContentManager content, string modelName, int maxSubMeshes = 0)
        {
            var so = new SceneObject(modelName) { ObjectType = ObjectType.Dynamic };
            try
            {
                Model model = content.LoadModel(modelName);
                ModelMeshCollection meshes = model.Meshes;
                int count = SubMeshCount(maxSubMeshes, meshes.Count);
                for (int i = 0; i < count; ++i)
                    so.Add(meshes[i]);
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

        public static void PreLoadModel(GameContentManager content, string modelName, bool animated)
        {
            content = content ?? ResourceManager.RootContent;
            if (RawContentLoader.IsSupportedMesh(modelName))
                content.LoadStaticMesh(modelName);
            else if (animated)
                content.LoadSkinnedModel(modelName);
            else
                content.LoadModel(modelName);
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

        public static SceneObject GetPlanetarySceneMesh(GameContentManager content, string modelName)
        {
            if (RawContentLoader.IsSupportedMesh(modelName))
                return FromFbx(content, modelName, 1);
            return SceneObjectFromModel(content, modelName, 1);
        }
    }
}
