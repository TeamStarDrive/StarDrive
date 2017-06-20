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

    struct SDMesh
    {
        Mesh mesh;
        unordered_map<int, BasicVertexMesh> Groups;

        explicit SDMesh(strview path);

        BasicVertexMesh* GetMesh(int groupId);
        void GetStats(int groupId, int* outVertices, int* outIndices);
        void GetData(int groupId, SDVertex* vertices, ushort* indices);
    };

    extern "C" {
        __declspec(dllexport) SDMesh* __stdcall SDMeshOpen(const wchar_t* filename);
        __declspec(dllexport) void __stdcall SDMeshClose(SDMesh* mesh);

        // get stats for meshgroup, so C# can allocate vertex and index buffers
        __declspec(dllexport) void __stdcall SDMeshGroupStats(SDMesh* mesh, int groupId, 
                                                              int* outVertices, int* outIndices);

        // writes mesh data to vertices[] and indices[]
        __declspec(dllexport) void __stdcall SDMeshGetGroupData(SDMesh* mesh, int groupId,
                                                                SDVertex* vertices, ushort* indices);
    }

    ////////////////////////////////////////////////////////////////////////////////////
}

