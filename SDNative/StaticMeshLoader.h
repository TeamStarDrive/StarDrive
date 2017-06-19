#pragma once
#include "mesh/Mesh.h"

namespace SDNative
{
    using namespace mesh;
    ////////////////////////////////////////////////////////////////////////////////////

    struct SunburnVertex
    {
        Vector3 Position;
        Vector3 Normal;
        Vector2 Coords;
        Vector3 PackedBinormalTangent;
    };

    using SunburnIndex = ushort;
    static_assert(sizeof(SunburnVertex) == 44, "SunburnVertex size mismatch");

    struct SDMesh
    {
        Mesh mesh;
        unordered_map<int, BasicVertexMesh> Groups;

        explicit SDMesh(strview path);

        BasicVertexMesh* GetMesh(int groupId);
        void GetStats(int groupId, int* outVertices, int* outIndices);
        void GetData(int groupId, SunburnVertex* vertices, SunburnIndex* indices);
    };

    extern "C" {
        __declspec(dllexport) SDMesh* __stdcall SDMeshOpen(const wchar_t* filename);
        __declspec(dllexport) void __stdcall SDMeshClose(SDMesh* mesh);

        // get stats for meshgroup, so C# can allocate vertex and index buffers
        __declspec(dllexport) void __stdcall SDMeshGroupStats(SDMesh* mesh, int groupId, 
                                                              int* outVertices, int* outIndices);

        // writes mesh data to vertices[] and indices[]
        __declspec(dllexport) void __stdcall SDMeshGetGroupData(SDMesh* mesh, int groupId,
                                                                SunburnVertex* vertices, SunburnIndex* indices);
    }

    ////////////////////////////////////////////////////////////////////////////////////
}

