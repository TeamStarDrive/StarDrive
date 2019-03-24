#pragma once
#include <Nano/Mesh.h>

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
        // publicly visible in C#
        strview Name; // name of the material instance
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

        // not mapped to C#
        shared_ptr<Nano::Material> Mat;

        explicit SDMaterial(const shared_ptr<Material>& mat) : Mat{mat}
        {
            if (!mat) return;
            Material& m = *mat;
            Name          = m.Name;
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

    struct SDMesh;

    struct SDMeshGroup
    {
        // publicly visible in C#
        int GroupId = -1;
        strview Name;
        SDMaterial* Mat = nullptr;
        int NumTriangles = 0;
        int NumVertices  = 0;
        int NumIndices   = 0;
        SDVertex* Vertices = nullptr;
        ushort*   Indices  = nullptr;
        BoundingSphere Bounds;
        Matrix4 Transform = Matrix4::Identity();

        // not mapped to C#
        SDMesh& TheMesh;
        vector<SDVertex> VertexData;
        vector<ushort>   IndexData;

        explicit SDMeshGroup(SDMesh& mesh, int groupId);
        void InitVertices();

        void SetData(Vector3* vertices, Vector3* normals, Vector2* coords, int numVertices,
                     const ushort* indices, int numIndices);

        Nano::Mesh& GetMesh() const;
        MeshGroup& GetGroup() const;
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
        vector<unique_ptr<SDMaterial>> Materials;

        SDMesh();
        explicit SDMesh(strview path);
        SDMeshGroup* GetGroup(int groupId);
        SDMeshGroup* AddGroup(string groupName);
        SDMaterial* GetOrCreateMat(const shared_ptr<Nano::Material>& mat);

        void SyncStats();
    };

    ////////////////////////////////////////////////////////////////////////////////////

    #define DLLAPI(returnType) extern "C" __declspec(dllexport) returnType __stdcall

    DLLAPI(SDMesh*) SDMeshOpen(const wchar_t* fileName);
    DLLAPI(void)    SDMeshClose(SDMesh* mesh);
    DLLAPI(SDMeshGroup*) SDMeshGetGroup(SDMesh* mesh, int groupId);

    DLLAPI(SDMesh*) SDMeshCreateEmpty(const wchar_t* meshName);
    DLLAPI(bool)    SDMeshSave(SDMesh* mesh, const wchar_t* fileName);
    DLLAPI(SDMeshGroup*) SDMeshNewGroup(SDMesh* mesh, const wchar_t* groupName, Matrix4* transform);

    DLLAPI(void) SDMeshGroupSetData(SDMeshGroup* group,
                    Vector3* vertices, Vector3* normals, Vector2* coords,
                    int numVertices, ushort* indices, int numIndices);

    /**
     * Create a new material instance
     * This instance is stored inside SDMesh and is automatically freed
     * during SDMeshClose
     */
    DLLAPI(SDMaterial*) SDMeshCreateMaterial(
            SDMesh* mesh,
            const wchar_t* name,
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

    DLLAPI(void) SDMeshGroupSetMaterial(SDMeshGroup* group, SDMaterial* material);


    ////////////////////////////////////////////////////////////////////////////////////
}

