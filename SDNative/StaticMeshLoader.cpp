#include "StaticMeshLoader.h"

namespace SDNative
{
    ////////////////////////////////////////////////////////////////////////////////////

    SDMeshGroup::SDMeshGroup(Mesh& mesh, int groupId)
        : GroupId(groupId), Mat{mesh[groupId].Mat}, Owner{ mesh }, Data{mesh[groupId]}
    {
        Name = Data.Name;
        InitVerts();
    }

    static void ComputeTangentBasis(
        const Vector3& p0,  const Vector3& p1,  const Vector3& p2, 
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

    void SDMeshGroup::InitVerts()
    {
        vector<int> indices;
        vector<BasicVertex> vertices;

        Data.InvertFaceWindingOrder();
        Data.CreateGameVertexData(vertices, indices);
        NumTriangles    = Data.NumFaces();
        int numVertices = NumVertices = (int)vertices.size();
        int numIndices  = NumIndices  = (int)indices.size();
        auto* groupVertices = vertices.data();
        auto* groupIndices  = indices.data();

        if (numVertices == 0 || numIndices == 0) {
            fprintf(stderr, "WARNING: No mesh data for group %d\n", GroupId);
            return;
        }

        bool isTriangulated = Owner.Groups[GroupId].CheckIsTriangulated();
        if (!isTriangulated) {
            fprintf(stderr, "WARNING: MeshGroup %d is not triangulated!!!\n", GroupId);
        }

        IndexData.resize(NumIndices);
        VertexData.resize(NumVertices);
        auto* outIndices  = Indices  = IndexData.data();
        auto* outVertices = Vertices = VertexData.data();

        for (int i = 0; i < numIndices; ++i)
            outIndices[i] = (ushort)groupIndices[i];

        Bounds = BoundingSphere::create(groupVertices, numVertices);

        for (int i = 0; i < numVertices; ++i)
        {
            SDVertex& sdv  = outVertices[i];
            BasicVertex& v = groupVertices[i];
            sdv.Position = v.pos;
            sdv.Coords   = { v.uv.x, 1.0f - v.uv.y };
            sdv.Normal   = v.norm;
        }

        Vector3 tangent, binormal;
        for (int i = 0; i < numIndices; i += 3)
        {
            SDVertex& v0 = outVertices[outIndices[i]];
            SDVertex& v1 = outVertices[outIndices[i+1]];
            SDVertex& v2 = outVertices[outIndices[i+2]];
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
        Name = Data.Name;
        NumGroups = Data.NumGroups();
        NumFaces = Data.NumFaces;
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

    DLLAPI(SDMeshGroup*) SDMeshGetGroup(SDMesh* mesh, int groupId)
    {
        return mesh ? mesh->GetGroup(groupId) : nullptr;
    }

    ////////////////////////////////////////////////////////////////////////////////////
}
