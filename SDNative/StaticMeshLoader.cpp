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

    static void ComputeTangentBasis(const Vector3& p0, const Vector3& p1, const Vector3& p2, 
                                    const Vector2& uv0, const Vector2& uv1, const Vector2& uv2,
                                    Vector3& tangent, Vector3& binormal)
    {
        // using Eric Lengyel's approach with a few modifications
        // from Mathematics for 3D Game Programmming and Computer Graphics
        float s1 = uv1.x - uv0.x;
        float t1 = uv1.y - uv0.y;
        float s2 = uv2.x - uv0.x;
        float t2 = uv2.y - uv0.y;
        float st = s1*t2 - s2*t1;
        float tmp = fabsf(st) <= 0.0001f ? 1.0f : 1.0f/st;

        Vector3 P = p1 - p0;
        Vector3 Q = p2 - p0;
        tangent.x = (t2*P.x - t1*Q.x);
        tangent.y = (t2*P.y - t1*Q.y);
        tangent.z = (t2*P.z - t1*Q.z);
        tangent = tangent*tmp;
        tangent.normalize();

        binormal.x = (s1*Q.x - s2*P.x);
        binormal.y = (s1*Q.y - s2*P.y);
        binormal.z = (s1*Q.z - s2*P.z);
        binormal = binormal*tmp;
        binormal.normalize();
    }

    void SDMesh::GetData(int groupId, SunburnVertex* vertices, SunburnIndex* indices)
    {
        auto* groupMesh = GetMesh(groupId);
        int numVertices = groupMesh->Vertices.size();
        int numIndices = groupMesh->Indices.size();
        auto* groupVertices = groupMesh->Vertices.data();
        auto* groupIndices = groupMesh->Indices.data();

        if (numVertices == 0 || numIndices == 0) {
            fprintf(stderr, "WARNING: No mesh data for group %d\n", groupId);
            return;
        }

        bool isTriangulated = mesh.Groups[groupId].IsTriangulated();
        if (!isTriangulated) {
            fprintf(stderr, "WARNING: MeshGroup %d is not triangulated!!!\n", groupId);
        }

        for (int i = 0; i < numIndices; ++i)
            indices[i] = (SunburnIndex)groupIndices[i];

        for (int i = 0; i < numVertices; ++i)
        {
            SunburnVertex& sbv = vertices[i];
            BasicVertex& v = groupVertices[i];
            sbv.Position = v.pos;
            sbv.Coords   = v.uv;
            sbv.Normal   = v.norm;
        }

        Vector3 tangent, binormal;
        for (int i = 0; i < numIndices; i += 3)
        {
            SunburnVertex& v0 = vertices[i];
            SunburnVertex& v1 = vertices[i+1];
            SunburnVertex& v2 = vertices[i+2];
            ComputeTangentBasis(v0.Position, v1.Position, v2.Position, v0.Coords, v1.Coords, v2.Coords, tangent, binormal);

            v0.PackedBinormalTangent.xy = binormal.xy;
            v0.PackedBinormalTangent.z  = tangent.y;
            v1.PackedBinormalTangent.xy = binormal.xy;
            v1.PackedBinormalTangent.z  = tangent.y;
            v2.PackedBinormalTangent.xy = binormal.xy;
            v2.PackedBinormalTangent.z  = tangent.y;
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
