#pragma once
#include "NanoMesh/Nano/Mesh.h"

namespace SDNative
{
    using namespace Nano;
    using rpp::ushort;
    using rpp::BoundingSphere;
    using rpp::Matrix4;
    using std::unique_ptr;
    ////////////////////////////////////////////////////////////////////////////////////

    struct SDVertex
    {
        Vector3 Position;
        Vector3 Normal;
        Vector2 Coords;
        Vector3 Tangent;
        Vector3 Binormal;
    };

    static_assert(sizeof(SDVertex) == 56, "SDVertex size mismatch. Sunburn requires a specific vertex layout");

    struct SDMaterial
    {
        // all publicly visible in C#
        strview Name; // name of the material instance
        strview MaterialFile; // 'default.mtl'
        strview DiffusePath;
        strview AlphaPath;
        strview SpecularPath;
        strview NormalPath;
        strview EmissivePath;
        Color3 AmbientColor  = Color3::White();
        Color3 DiffuseColor  = Color3::White();
        Color3 SpecularColor = Color3::White();
        Color3 EmissiveColor = Color3::Black();
        float Specular = 1.0f;
        float Alpha    = 1.0f;

        explicit SDMaterial(const shared_ptr<Material>& mat)
        {
            if (!mat) return;
            Material& m = *mat;
            Name          = m.Name;
            MaterialFile  = m.MaterialFile;
            DiffusePath   = m.DiffusePath;
            AlphaPath     = m.AlphaPath;
            SpecularPath  = m.SpecularPath;
            NormalPath    = m.NormalPath;
            EmissivePath  = m.EmissivePath;
            AmbientColor  = m.AmbientColor;
            DiffuseColor  = m.DiffuseColor;
            SpecularColor = m.SpecularColor;
            EmissiveColor = m.EmissiveColor;
            Specular      = m.Specular;
            Alpha         = m.Alpha;
        }
    };

    struct SDMeshGroup
    {
        // publicly visible in C#
        int     GroupId    = -1;
        strview Name;
        SDMaterial Mat;
        int NumTriangles   = 0;
        int NumVertices    = 0;
        int NumIndices     = 0;
        SDVertex* Vertices = nullptr;
        ushort*   Indices  = nullptr;
        BoundingSphere Bounds;
        Matrix4 Transform = Matrix4::Identity();

        // not mapped to C#
        Mesh& Owner;
        MeshGroup& Data;
        vector<SDVertex> VertexData;
        vector<ushort>   IndexData;

        explicit SDMeshGroup(Mesh& mesh, MeshGroup& group);
        explicit SDMeshGroup(Mesh& mesh, int groupId);
        void InitVerts();

        void SetData(Vector3* verts, Vector3* normals, Vector2* coords, int numVertices,
                     ushort* indices, int numIndices);
    };

    struct SDMesh
    {
        // publicly visible in C#
        strview Name  = "";
        int NumGroups = 0;
        int NumFaces  = 0;

        // not mapped to C#
        Mesh Data;
        vector<unique_ptr<SDMeshGroup>> Groups;

        SDMesh();
        explicit SDMesh(strview path);
        SDMeshGroup* GetGroup(int groupId);
        SDMeshGroup* AddGroup(string groupname);
    };

    ////////////////////////////////////////////////////////////////////////////////////

    #define DLLAPI(returnType) extern "C" __declspec(dllexport) returnType __stdcall

    DLLAPI(SDMesh*) SDMeshOpen(const wchar_t* filename);
    DLLAPI(void)    SDMeshClose(SDMesh* mesh);
    DLLAPI(SDMeshGroup*) SDMeshGetGroup(SDMesh* mesh, int groupId);

    DLLAPI(SDMesh*) SDMeshCreateEmpty(const wchar_t* meshname);
    DLLAPI(bool)    SDMeshSave(SDMesh* mesh, const wchar_t* filename);
    DLLAPI(SDMeshGroup*) SDMeshNewGroup(SDMesh* mesh, const wchar_t* groupname, Matrix4* transform);

    DLLAPI(void) SDMeshGroupSetData(SDMeshGroup* group,
                    Vector3* verts, Vector3* normals, Vector2* coords, int numVertices,
                    ushort* indices, int numIndices);

    DLLAPI(void) SDMeshGroupSetMaterial(
                    SDMeshGroup* group,
                    const wchar_t* name,
                    const wchar_t* materialFile,
                    const wchar_t* diffusePath,
                    const wchar_t* alphaPath,
                    const wchar_t* specularPath,
                    const wchar_t* normalPath,
                    const wchar_t* emissivePath,
                    Color3 ambientColor,
                    Color3 diffuseColor,
                    Color3 specularColor,
                    Color3 emissiveColor,
                    float specular,
                    float alpha);

    ////////////////////////////////////////////////////////////////////////////////////
}

