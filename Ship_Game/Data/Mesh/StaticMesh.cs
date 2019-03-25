using System;
using Microsoft.Xna.Framework.Graphics;
using SgMotion;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Data.Mesh
{
    public class StaticMesh
    {
        public string Name { get; set; }
        public Array<MeshData> Meshes { get; set; } = new Array<MeshData>();
        public int Count => Meshes.Count;

        static int SubMeshCount(int maxSubMeshes, int meshSubMeshCount)
        {
            return maxSubMeshes == 0 ? meshSubMeshCount : Math.Min(maxSubMeshes, meshSubMeshCount);
        }

        static SceneObject SceneObjectFromStaticMesh(GameContentManager content, string modelName, int maxSubMeshes = 0)
        {
            StaticMesh staticMesh = content.LoadStaticMesh(modelName);
            if (staticMesh == null)
                return null;

            var so = new SceneObject(modelName) { ObjectType = ObjectType.Dynamic };
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
            return so;
        }

        static SceneObject SceneObjectFromModel(GameContentManager content, string modelName, int maxSubMeshes = 0)
        {
            Model model = content.LoadModel(modelName);
            if (model == null)
                return null;
            
            var so = new SceneObject(modelName) { ObjectType = ObjectType.Dynamic };
            so.Visibility = ObjectVisibility.RenderedAndCastShadows;

            int count = SubMeshCount(maxSubMeshes, model.Meshes.Count);
            for (int i = 0; i < count; ++i)
                so.Add(model.Meshes[i]);
            return so;
        }

        static SceneObject SceneObjectFromSkinnedModel(GameContentManager content, string modelName)
        {
            SkinnedModel skinned = content.LoadSkinnedModel(modelName);
            if (skinned == null)
                return null;

            var so = new SceneObject(skinned.Model, modelName)
            {
                ObjectType = ObjectType.Dynamic
            };
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
                return SceneObjectFromStaticMesh(content, modelName);
            if (animated)
                return SceneObjectFromSkinnedModel(content, modelName);
            return SceneObjectFromModel(content, modelName);
        }

        public static SceneObject GetPlanetarySceneMesh(GameContentManager content, string modelName)
        {
            if (RawContentLoader.IsSupportedMesh(modelName))
                return SceneObjectFromStaticMesh(content, modelName, 1);
            return SceneObjectFromModel(content, modelName, 1);
        }
    }
}
