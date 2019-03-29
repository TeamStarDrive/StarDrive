#pragma once
#include <Nano/Mesh.h>

namespace SDNative
{
    using namespace Nano;
    using rpp::ushort;
    using rpp::BoundingSphere;
    using rpp::Matrix4;
    using std::unique_ptr;

    struct SDMesh;
    struct SDMeshGroup;
    ////////////////////////////////////////////////////////////////////////////////////

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

    enum class SDElementFormat : rpp::byte
    {
        Single,
        Vector2,
        Vector3,
        Vector4,
        Color,
        Byte4,
        Short2,
        Short4,
        Rgba32,
    };
    
    enum class SDElementUsage : rpp::byte
    {
        Position = 0,
        BlendWeight = 1,
        BlendIndices = 2,
        Normal = 3,
        PointSize = 4,
        Coordinate = 5,
        Tangent = 6,
        BiNormal = 7,
        TessellateFactor = 8,
        Color = 10, // 0x0A
        Fog = 11, // 0x0B
        Depth = 12, // 0x0C
        Sample = 13, // 0x0D
    };

    struct SDVertexElement
    {
        rpp::byte Offset = 0; // element offset in vertex buffer data
        rpp::byte Size   = 0; // element size in bytes
        SDElementFormat Format {};
        SDElementUsage  Usage  {};
    };

    struct SDBonePose
    {
        Vector3 Translation;
        Vector4 Orientation; // Quaternion
        Vector3 Scale;
    };

    struct SDModelBone
    {
        // publicly visible in C#
        strview Name;
        int BoneIndex;
        int ParentBone;
        SDBonePose Pose;

        // not mapped to C#
        SDMeshGroup& Group;
        string TheName;
    };

    struct SDVertexData
    {
        int VertexStride = 0;
        int LayoutCount  = 0;
        int IndexCount   = 0;
        int VertexCount  = 0;
        const SDVertexElement* Layout = nullptr;
        const ushort*    IndexData  = nullptr;
        const rpp::byte* VertexData = nullptr;

        template<class T> const T* GetOffset(SDElementUsage usage) const
        {
            for (int i = 0; i < LayoutCount; ++i)
                if (Layout[i].Usage == usage)
                    return (const T*)(VertexData + Layout[i].Offset);
            return nullptr;
        }
    };


    struct SDMeshGroup
    {
        // publicly visible in C#
        int GroupId = -1;
        strview Name;
        SDMaterial* Mat = nullptr;
        BoundingSphere Bounds;
        Matrix4 Transform = Matrix4::Identity();

        // not mapped to C#
        SDMesh& TheMesh;
        vector<SDVertexElement> Layout;
        vector<ushort> IndexData;
        vector<rpp::byte> VertexData;

        explicit SDMeshGroup(SDMesh& mesh, int groupId);
        void InitVertices();

        void SetData(SDVertexData vd);
        SDVertexData CreateCachedVertexData();
        void AddVertexElement(int& stride, SDElementFormat format, SDElementUsage usage);

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

    DLLAPI(void) SDMeshGroupSetData(SDMeshGroup* group, SDVertexData data);
    DLLAPI(SDVertexData) SDMeshGroupGetData(SDMeshGroup* group);

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

    DLLAPI(void) SDMeshGroupSetSkeleton(SDMeshGroup* group);

    ////////////////////////////////////////////////////////////////////////////////////
}

