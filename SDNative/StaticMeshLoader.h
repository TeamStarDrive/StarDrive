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

    struct SDMeshGroup
    {
        // publicly visible in C#
        int GroupId;
        strview Name;
        Material* Mat;

        // not mapped to C#
        Mesh& Owner;
        MeshGroup& Data;
        vector<BasicVertex> Vertices;
        vector<int> Indices;

        explicit SDMeshGroup(Mesh& mesh, int groupId);

        void GetData(SDVertex* vertices, ushort* indices);
    };

    struct SDMesh
    {
        Mesh Data;
        vector<unique_ptr<SDMeshGroup>> Groups;

        explicit SDMesh(strview path);

        SDMeshGroup* GetGroup(int groupId);
    };

    ////////////////////////////////////////////////////////////////////////////////////

    #define DLLAPI(returnType) extern "C" __declspec(dllexport) returnType __stdcall

    DLLAPI(SDMesh*) SDMeshOpen(const wchar_t* filename);
    DLLAPI(void)    SDMeshClose(SDMesh* mesh);
    DLLAPI(int)          SDMeshNumGroups(SDMesh* mesh);
    DLLAPI(SDMeshGroup*) SDMeshGetGroup(SDMesh* mesh, int groupId);

    // get stats for meshgroup, so C# can allocate vertex and index buffers
    DLLAPI(void) SDMeshGroupStats(SDMeshGroup* group, int* outVertices, int* outIndices);

    // writes mesh data to preallocated vertices[] and indices[]
    DLLAPI(void) SDMeshGetGroupData(SDMeshGroup* group, SDVertex* vertices, ushort* indices);

    ////////////////////////////////////////////////////////////////////////////////////
}

