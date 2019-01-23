#include "StaticMeshLoader.h"
#include <rpp/file_io.h>

namespace SDNative
{
    ////////////////////////////////////////////////////////////////////////////////////

    SDMeshGroup::SDMeshGroup(SDMesh& mesh, int groupId)
		: GroupId{ groupId }, Mat{ mesh.Data[groupId].Mat }, TheMesh{ mesh }
    {
        Name = mesh.Data[groupId].Name;
        InitVertices();
    }

    static void ComputeTangentBasis(
        const Vector3& p0, const Vector3& p1, const Vector3& p2,
        const Vector2& uv0, const Vector2& uv1, const Vector2& uv2,
        Vector3& tangent, Vector3& biNormal)
    {
        // using Eric Lengyel's approach with a few modifications
        // from Mathematics for 3D Game Programming and Computer Graphics
        float s1 = uv1.x - uv0.x;
        float t1 = uv1.y - uv0.y;
        float s2 = uv2.x - uv0.x;
        float t2 = uv2.y - uv0.y;
        float st = s1*t2 - s2*t1;
        //float st = t1*s2 - s1*t2; // ver2
        float tmp = fabsf(st) <= 0.0001f ? 1.0f : 1.0f / st;

        Vector3 P = p1 - p0;
        Vector3 Q = p2 - p0;
        tangent = (t2*P - t1*Q) * tmp;
        biNormal = (s1*Q - s2*P) * tmp;
        //tangent  = (Q*t1 - P*t2) * tmp; // ver2
        //biNormal = (Q*s1 - P*s2) * tmp;
        tangent.normalize();
        biNormal.normalize();
    }

    void SDMeshGroup::InitVertices()
    {
		Nano::MeshGroup& group = GetGroup();
        if (group.IsEmpty())
            return;

        // Sunburn expects ClockWise
		group.SetFaceWinding(FaceWinding::CW);

        vector<int> indices;
        group.OptimizedFlatten(); // force numVertices == numCoords == numNormals
        group.CreateIndexArray(indices);

        NumTriangles = group.NumTris();
        int numVertices = NumVertices = (int)group.Verts.size();
        int numIndices  = NumIndices  = (int)indices.size();
        auto* pVertices = group.Verts.data();
        auto* pCoords   = group.Coords.data();
        auto* pNormals  = group.Normals.data();
        auto* pIndices = indices.data();

        if (numVertices == 0 || numIndices == 0) {
            fprintf(stderr, "WARNING: No mesh data for group %d\n", GroupId);
            return;
        }

        IndexData.resize(numIndices);
        VertexData.resize(numVertices);
        auto* outIndices  = Indices  = IndexData.data();
        auto* outVertices = Vertices = VertexData.data();

        for (int i = 0; i < numIndices; ++i)
            outIndices[i] = (rpp::ushort)pIndices[i];

        Bounds = rpp::BoundingSphere::create(pVertices, numVertices);

        for (int i = 0; i < numVertices; ++i)
        {
            SDVertex& sdv = outVertices[i];
            sdv.Position = pVertices[i];
            sdv.Coords = { pCoords[i].x, 1.0f - pCoords[i].y };
            sdv.Normal = pNormals[i];
        }

		Vector3 tangent{}, biNormal{};
        for (int i = 0; i < numIndices; i += 3)
        {
            SDVertex& v0 = outVertices[outIndices[i]];
            SDVertex& v1 = outVertices[outIndices[i + 1]];
            SDVertex& v2 = outVertices[outIndices[i + 2]];
            ComputeTangentBasis(v0.Position, v1.Position, v2.Position,
                v0.Coords, v1.Coords, v2.Coords, tangent, biNormal);
            v0.Tangent  = tangent;
            v0.Binormal = biNormal;
            v1.Tangent  = tangent;
            v1.Binormal = biNormal;
            v2.Tangent  = tangent;
            v2.Binormal = biNormal;
        }
    }

    void SDMeshGroup::SetData(Vector3* vertices, Vector3* normals, Vector2* coords, int numVertices,
                              const rpp::ushort* indices, int numIndices)
    {
		Nano::MeshGroup& group = GetGroup();
        Matrix4 transform = Transform.inverse();
        if (vertices)
        {
            group.Verts.resize(numVertices);
            auto* dst = group.Verts.data();

            for (int i = 0; i < numVertices; ++i) {
                Vector3 pos = transform * vertices[i];
                //dst[i] = pos;
                dst[i] = {pos.x, -pos.z, -pos.y};
            }
            Bounds = BoundingSphere::create(dst, numVertices);
        }

        if (coords)
        {
            group.Coords.resize(numVertices);
            auto* dst = group.Coords.data();

            for (int i = 0; i < numVertices; ++i) {
                Vector2 uv = coords[i];
                dst[i] = { uv.x, 1.0f - uv.y };
            }
        }
		group.CoordsMapping = coords ? MapPerVertex : MapNone;

        if (normals)
        {
            group.Normals.resize(numVertices);
            auto* dst = group.Normals.data();

            for (int i = 0; i < numVertices; ++i) {
                Vector3 normal = transform * normals[i];
                //dst[i] = normal;
                dst[i] = {normal.x, -normal.z, -normal.y};
            }
        }
		group.NormalsMapping = coords ? MapPerVertex : MapNone;

        const bool hasCoords  = !group.Coords.empty();
        const bool hasNormals = !group.Normals.empty();

        int numTriangles = numIndices / 3;
		group.Tris.resize(numTriangles);
        auto* destFaces = group.Tris.data();

        for (int i = 0, faceId = 0; i < numIndices; i += 3, ++faceId)
        {
            int v0 = indices[i];
            int v1 = indices[i+1];
            int v2 = indices[i+2];
            Triangle& tri = destFaces[faceId];
            tri.a = { v0, hasCoords?v0:-1, hasNormals?v0:-1 };
            tri.b = { v1, hasCoords?v1:-1, hasNormals?v1:-1 };
            tri.c = { v2, hasCoords?v2:-1, hasNormals?v2:-1 };
        }

		TheMesh.SyncStats();
    }

    Mesh& SDMeshGroup::GetMesh() const
    {
		return TheMesh.Data;
    }

    MeshGroup& SDMeshGroup::GetGroup() const
    {
		return GetMesh()[GroupId];
    }

    ////////////////////////////////////////////////////////////////////////////////////

    SDMesh::SDMesh() = default;

    SDMesh::SDMesh(strview path) : Data{ path }
    {
        Groups.resize(Data.NumGroups());
        Name = Data.Name;
		SyncStats();
    }

    SDMeshGroup* SDMesh::GetGroup(int groupId)
    {
        if (!Data.IsValidGroup(groupId))
            return nullptr;

        if (auto* groupMesh = Groups[groupId].get())
            return groupMesh;

        Groups[groupId] = std::make_unique<SDMeshGroup>(*this, groupId);
        return Groups[groupId].get();
    }

    SDMeshGroup* SDMesh::AddGroup(string groupName)
    {
        MeshGroup& group = Data.CreateGroup(move(groupName));
        auto* g = Groups.emplace_back(std::make_unique<SDMeshGroup>(*this, group.GroupId)).get();
		SyncStats();
		return g;
    }

    void SDMesh::SyncStats()
    {
		NumGroups = Data.NumGroups();
        NumFaces  = Data.TotalTris();
    }

    ////////////////////////////////////////////////////////////////////////////////////

    static string to_string(const wchar_t* wideStr)
    {
        return { wideStr, wideStr + wcslen(wideStr) };
    }

    DLLAPI(SDMesh*) SDMeshOpen(const wchar_t* fileName)
    {
        auto sdm = new SDMesh{ to_string(fileName) };
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

    DLLAPI(SDMesh*) SDMeshCreateEmpty(const wchar_t* meshName)
    {
        auto* mesh = new SDMesh{};
        mesh->Data.Name = to_string(meshName);
        mesh->Name = mesh->Data.Name;
        return mesh;
    }

    DLLAPI(bool) SDMeshSave(SDMesh* mesh, const wchar_t* filename)
    {
        return mesh->Data.SaveAs(to_string(filename));
    }

    DLLAPI(SDMeshGroup*) SDMeshNewGroup(SDMesh* mesh, const wchar_t* groupName, Matrix4* transform)
    {
        SDMeshGroup* group = mesh->AddGroup(to_string(groupName));
        if (transform) group->Transform = *transform;
        return group;
    }

    DLLAPI(void) SDMeshGroupSetData(SDMeshGroup* group,
                                    Vector3* vertices, Vector3* normals, Vector2* coords, int numVertices,
                                    ushort* indices, int numIndices)
    {
        group->SetData(vertices, normals, coords, numVertices, indices, numIndices);
    }

    DLLAPI(void) SDMeshGroupSetMaterial(
                    SDMeshGroup* group, 
                    const wchar_t* name, 
                    const wchar_t* materialFile, 
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
                    float alpha)
    {
        Material& mat = group->GetGroup().CreateMaterial(to_string(name));
        mat.MaterialFile  = to_string(materialFile);
        mat.DiffusePath   = to_string(diffusePath);
        mat.AlphaPath     = to_string(alphaPath);
        mat.SpecularPath  = to_string(specularPath);
        mat.NormalPath    = to_string(normalPath);
        mat.EmissivePath  = to_string(emissivePath);
        mat.AmbientColor  = ambientColor;
        mat.DiffuseColor  = diffuseColor;
        mat.SpecularColor = specularColor;
        mat.EmissiveColor = emissiveColor;
        mat.Specular      = specular;
        mat.Alpha         = alpha;
    }

    ////////////////////////////////////////////////////////////////////////////////////
}
