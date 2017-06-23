#pragma once
#include "mesh/Mesh.h"

namespace SDNative
{
    using namespace mesh;
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
        strview Name; // name of the material instance
        strview MaterialFile; // 'default.mtl'
        strview DiffusePath;
        strview AlphaPath;
        strview SpecularPath;
        strview NormalPath;
        strview EmissivePath;
        Color3 AmbientColor  = Color3::WHITE;
        Color3 DiffuseColor  = Color3::WHITE;
        Color3 SpecularColor = Color3::WHITE;
        Color3 EmissiveColor = Color3::BLACK;
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
        int GroupId = -1;
        strview Name;
        SDMaterial Mat;
        int NumVertices = 0;
        int NumIndices  = 0;
        SDVertex* Vertices = nullptr;
        ushort*   Indices  = nullptr;

        // not mapped to C#
        Mesh& Owner;
        MeshGroup& Data;
        vector<SDVertex> VertexData;
        vector<ushort>   IndexData;

        explicit SDMeshGroup(Mesh& mesh, int groupId);
        void InitVerts();
    };

    struct SDMesh
    {
        strview Name;
        int NumGroups;
        int NumFaces;

        Mesh Data;
        vector<unique_ptr<SDMeshGroup>> Groups;

        explicit SDMesh(strview path);
        SDMeshGroup* GetGroup(int groupId);
    };

    ////////////////////////////////////////////////////////////////////////////////////

    #define DLLAPI(returnType) extern "C" __declspec(dllexport) returnType __stdcall

    DLLAPI(SDMesh*) SDMeshOpen(const wchar_t* filename);
    DLLAPI(void)    SDMeshClose(SDMesh* mesh);
    DLLAPI(SDMeshGroup*) SDMeshGetGroup(SDMesh* mesh, int groupId);

    ////////////////////////////////////////////////////////////////////////////////////
}

