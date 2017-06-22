#include "StaticMeshLoader.h"

namespace SDNative
{
    ////////////////////////////////////////////////////////////////////////////////////

    SDMeshGroup::SDMeshGroup(Mesh& mesh, int groupId)
        : GroupId(groupId), Owner{ mesh }, Data{mesh[groupId]}
    {
        Name = Data.Name;
        Mat  = Data.Mat.get();
        Data.CreateGameVertexData(Vertices, Indices);
    }

    static void ComputeTangentBasis(
        const Vector3& p0, const Vector3& p1, const Vector3& p2, 
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
        tangent  = (t2*P - t1*Q) * tmp;
        binormal = (s1*Q - s2*P) * tmp;
        tangent.normalize();
        binormal.normalize();
    }

    void SDMeshGroup::GetData(SDVertex* vertices, ushort* indices)
    {
        int numVertices = Vertices.size();
        int numIndices  = Indices.size();
        auto* groupVertices = Vertices.data();
        auto* groupIndices  = Indices.data();

        if (numVertices == 0 || numIndices == 0) {
            fprintf(stderr, "WARNING: No mesh data for group %d\n", GroupId);
            return;
        }

        bool isTriangulated = Owner.Groups[GroupId].IsTriangulated();
        if (!isTriangulated) {
            fprintf(stderr, "WARNING: MeshGroup %d is not triangulated!!!\n", GroupId);
        }

        for (int i = 0; i < numIndices; ++i)
            indices[i] = (ushort)groupIndices[i];

        for (int i = 0; i < numVertices; ++i)
        {
            SDVertex& sdv = vertices[i];
            BasicVertex& v = groupVertices[i];
            sdv.Position = v.pos;
            sdv.Coords   = v.uv;
            sdv.Normal   = v.norm;
        }

        Vector3 tangent, binormal;
        for (int i = 0; i < numIndices; i += 3)
        {
            SDVertex& v0 = vertices[i];
            SDVertex& v1 = vertices[i+1];
            SDVertex& v2 = vertices[i+2];
            ComputeTangentBasis(v0.Position, v1.Position, v2.Position, v0.Coords, v1.Coords, v2.Coords, tangent, binormal);
            v0.Tangent  = tangent;
            v0.Binormal = binormal;
            v1.Tangent  = tangent;
            v1.Binormal = binormal;
            v2.Tangent  = tangent;
            v2.Binormal = binormal;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////

    SDMesh::SDMesh(strview path) : Data{ path }
    {
        Groups.resize(Data.NumGroups());
    }

    SDMeshGroup* SDMesh::GetGroup(int groupId)
    {
        if (!Data.IsValidGroup(groupId))
            return nullptr;

        if (auto* groupMesh = Groups[groupId].get())
            return groupMesh;

        Groups[groupId] = make_unique<SDMeshGroup>(Data, groupId);
        return Groups[groupId].get();
    }

    ////////////////////////////////////////////////////////////////////////////////////

    DLLAPI(SDMesh*) SDMeshOpen(const wchar_t* filename)
    {
        string path = { filename, filename + wcslen(filename) };
        auto sdm = new SDMesh{ path };
        if (!sdm->Data) {
            SDMeshClose(sdm);
            return nullptr;
        }
        return sdm;
    }

    DLLAPI(void) SDMeshClose(SDMesh* mesh)
    {
        delete mesh;
    }

    DLLAPI(int) SDMeshNumGroups(SDMesh* mesh)
    {
        return mesh ? mesh->Data.NumGroups() : 0;
    }

    DLLAPI(SDMeshGroup*) SDMeshGetGroup(SDMesh* mesh, int groupId)
    {
        return mesh ? mesh->GetGroup(groupId) : nullptr;
    }

    DLLAPI(void) SDMeshGroupStats(SDMeshGroup* group, int* outVertices, int* outIndices)
    {
        *outVertices = group ? group->Vertices.size() : 0;
        *outIndices  = group ? group->Indices.size()  : 0;
    }

    DLLAPI(void) SDMeshGetGroupData(SDMeshGroup* group, SDVertex* vertices, ushort* indices)
    {
        if (group) group->GetData(vertices, indices);
    }

    ////////////////////////////////////////////////////////////////////////////////////
}
