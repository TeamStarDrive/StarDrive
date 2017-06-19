#include "StaticMeshLoader.h"

namespace SDNative
{
    ////////////////////////////////////////////////////////////////////////////////////

    SDMesh::SDMesh(strview path): mesh(path)
    {
    }

    BasicVertexMesh* SDMesh::GetMesh(int groupId)
    {
        if (auto* groupMesh = find(Groups, groupId))
            return groupMesh;

        Groups.insert_or_assign(groupId, mesh.GetBasicVertexMesh(groupId));
        return find(Groups, groupId);
    }

    void SDMesh::GetStats(int groupId, int* outVertices, int* outIndices)
    {
        auto* groupMesh = GetMesh(groupId);
        *outVertices = groupMesh->Vertices.size();
        *outIndices  = groupMesh->Indices.size();
    }

    void SDMesh::GetData(int groupId, SunburnVertex* vertices, SunburnIndex* indices)
    {
        auto* groupMesh = GetMesh(groupId);
        int numVertices = groupMesh->Vertices.size();
        int numIndices  = groupMesh->Indices.size();
        auto* groupVertices = groupMesh->Vertices.data();
        auto* groupIndices  = groupMesh->Indices.data();

        for (int i = 0; i < numIndices; ++i)
        {
            indices[i] = (SunburnIndex)groupIndices[i];
        }

        for (int i = 0; i < numVertices; ++i)
        {
            SunburnVertex& sbv = vertices[i];
            BasicVertex& v = groupVertices[i];

            sbv.Position = v.pos;
            sbv.Coords   = v.uv;
            sbv.Normal   = v.norm;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////

    extern "C" SDMesh* __stdcall SDMeshOpen(const wchar_t* filename)
    {
        string path = { filename, filename + wcslen(filename) };
        auto sdm = new SDMesh(path);
        if (!sdm->mesh) {
            SDMeshClose(sdm);
            return nullptr;
        }
        return sdm;
    }

    extern "C" void __stdcall SDMeshClose(SDMesh* mesh)
    {
        delete mesh;
    }

    void __stdcall SDMeshGroupStats(SDMesh* mesh, int groupId, int* outVertices, int* outIndices)
    {
        mesh->GetStats(groupId, outVertices, outIndices);
    }

    void __stdcall SDMeshGetGroupData(SDMesh* mesh, int groupId, SunburnVertex* vertices, SunburnIndex* indices)
    {
        mesh->GetData(groupId, vertices, indices);
    }

    ////////////////////////////////////////////////////////////////////////////////////
}
