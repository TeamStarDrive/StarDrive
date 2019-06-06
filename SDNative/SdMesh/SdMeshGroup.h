#pragma once
#include <Nano/Mesh.h>
#include "SdMaterial.h"

namespace SdMesh
{
    using std::vector;
    using rpp::ushort;
    using rpp::Vector2;
    using rpp::Vector3;
    using rpp::Vector4;
    using rpp::Matrix4;
    ////////////////////////////////////////////////////////////////////////////////////

    enum class SDElementFormat : rpp::byte
    {
        Single, Vector2, Vector3, Vector4, Color, Byte4, Short2, Short4, Rgba32,
    };
    
    enum class SDElementUsage : rpp::byte
    {
        Position, BlendWeight, BlendIndices, Normal, PointSize, Coordinate, Tangent, 
        BiNormal, TessellateFactor, Color = 10, Fog, Depth, Sample,
    };

    struct SDVertexElement
    {
        rpp::byte Offset = 0; // element offset in vertex buffer data
        rpp::byte Size   = 0; // element size in bytes
        SDElementFormat Format {};
        SDElementUsage  Usage  {};
    };

    ////////////////////////////////////////////////////////////////////////////////////

    struct SDVertexData
    {
        int VertexStride;
        int LayoutCount;
        int IndexCount;
        int VertexCount;
        const SDVertexElement* Layout;
        const ushort*     IndexData;
        const rpp::byte*  VertexData;

        template<class T> [[nodiscard]] const T* GetOffset(SDElementUsage usage) const
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
        rpp::BoundingSphere Bounds;
        Matrix4 Transform = Matrix4::Identity();

        // not mapped to C#
        SDMesh& TheMesh;
        vector<SDVertexElement> Layout;
        vector<ushort>    IndexData;
        vector<rpp::byte> VertexData;

        explicit SDMeshGroup(SDMesh& mesh, int groupId);

        void SetVertexDataFor(Nano::MeshGroup& group, const SDVertexData& vd) const;
        void SetData(const SDVertexData& vd);
        SDVertexData CreateCachedVertexData();
        void AddVertexElement(int& stride, SDElementFormat format, SDElementUsage usage);

        Nano::Mesh& GetMesh() const;
        Nano::MeshGroup& GetGroup() const;
    };

    ////////////////////////////////////////////////////////////////////////////////////

    DLLAPI(void) SDMeshGroupSetData(SDMeshGroup* group, SDVertexData data);
    DLLAPI(SDVertexData) SDMeshGroupGetData(SDMeshGroup* group);

    ////////////////////////////////////////////////////////////////////////////////////
}