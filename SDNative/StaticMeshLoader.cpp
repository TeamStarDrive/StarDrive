#include "StaticMeshLoader.h"
#include <rpp/file_io.h>

namespace SDNative
{
    ////////////////////////////////////////////////////////////////////////////////////

    SDMeshGroup::SDMeshGroup(Mesh& mesh, MeshGroup& group)
        : GroupId(group.GroupId), Mat{ group.Mat }, Owner{ mesh }, Data{ group }
    {
        Name = Data.Name;
    }

    SDMeshGroup::SDMeshGroup(Mesh& mesh, int groupId)
        : GroupId(groupId), Mat{ mesh[groupId].Mat }, Owner{ mesh }, Data{ mesh[groupId] }
    {
        Name = Data.Name;
        InitVerts();
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
        //float st = t1*s2 - s1*t2; // ver2
        float tmp = fabsf(st) <= 0.0001f ? 1.0f : 1.0f / st;

        Vector3 P = p1 - p0;
        Vector3 Q = p2 - p0;
        tangent = (t2*P - t1*Q) * tmp;
        binormal = (s1*Q - s2*P) * tmp;
        //tangent  = (Q*t1 - P*t2) * tmp; // ver2
        //binormal = (Q*s1 - P*s2) * tmp;
        tangent.normalize();
        binormal.normalize();
    }

    void SDMeshGroup::InitVerts()
    {
        if (Data.IsEmpty())
            return;

        vector<int> indices;
        vector<BasicVertex> vertices;

        // Sunburn expects ClockWise
        if (Data.Winding == FaceWindCounterClockWise)
        {
            Data.InvertFaceWindingOrder();
            //Data.RecalculateNormals(false);
        }

        Data.CreateGameVertexData(vertices, indices);

        NumTriangles = Data.NumFaces();
        int numVertices = NumVertices = (int)vertices.size();
        int numIndices = NumIndices = (int)indices.size();
        auto* groupVertices = vertices.data();
        auto* groupIndices = indices.data();

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
        auto* outIndices = Indices = IndexData.data();
        auto* outVertices = Vertices = VertexData.data();

        for (int i = 0; i < numIndices; ++i)
            outIndices[i] = (ushort)groupIndices[i];

        Bounds = BoundingSphere::create(groupVertices, numVertices);

        for (int i = 0; i < numVertices; ++i)
        {
            SDVertex& sdv = outVertices[i];
            BasicVertex& v = groupVertices[i];
            sdv.Position = v.pos;
            sdv.Coords = { v.uv.x, 1.0f - v.uv.y };
            sdv.Normal = v.norm;
        }

        Vector3 tangent, binormal;
        for (int i = 0; i < numIndices; i += 3)
        {
            SDVertex& v0 = outVertices[outIndices[i]];
            SDVertex& v1 = outVertices[outIndices[i + 1]];
            SDVertex& v2 = outVertices[outIndices[i + 2]];
            ComputeTangentBasis(v0.Position, v1.Position, v2.Position,
                v0.Coords, v1.Coords, v2.Coords, tangent, binormal);
            v0.Tangent = tangent;
            v0.Binormal = binormal;
            v1.Tangent = tangent;
            v1.Binormal = binormal;
            v2.Tangent = tangent;
            v2.Binormal = binormal;
        }
    }

    void SDMeshGroup::SetData(Vector3* verts, Vector3* normals, Vector2* coords, int numVertices,
                              ushort* indices, int numIndices)
    {
        Matrix4 transform = Transform.inverse();
        if (verts)
        {
            Data.Verts.resize(numVertices);
            auto* dst = Data.Verts.data();

            for (int i = 0; i < numVertices; ++i) {
                Vector3 pos = transform * verts[i];
                //dst[i] = pos;
                dst[i] = {pos.x, -pos.z, -pos.y};
            }
            Bounds = BoundingSphere::create(dst, numVertices);
        }

        if (coords)
        {
            Data.Coords.resize(numVertices);
            auto* dst = Data.Coords.data();

            for (int i = 0; i < numVertices; ++i) {
                Vector2 uv = coords[i];
                dst[i] = { uv.x, 1.0f - uv.y };
            }
        }

        if (normals)
        {
            Data.Normals.resize(numVertices);
            auto* dst = Data.Normals.data();

            for (int i = 0; i < numVertices; ++i) {
                Vector3 normal = transform * normals[i];
                //dst[i] = normal;
                dst[i] = {normal.x, -normal.z, -normal.y};
            }
        }

        const bool hasCoords  = !Data.Coords.empty();
        const bool hasNormals = !Data.Normals.empty();

        int numTriangles = numIndices / 3;
        Data.Faces.resize(numTriangles);
        auto* destFaces = Data.Faces.data();

        for (int i = 0, faceId = 0; i < numIndices; i += 3, ++faceId)
        {
            int v0 = indices[i];
            int v1 = indices[i+1];
            int v2 = indices[i+2];
            Face& face = destFaces[faceId];
            face.add(v0, hasCoords?v0:-1, hasNormals?v0:-1);
            face.add(v1, hasCoords?v1:-1, hasNormals?v1:-1);
            face.add(v2, hasCoords?v2:-1, hasNormals?v2:-1);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////

    SDMesh::SDMesh()
    {
    }

    SDMesh::SDMesh(strview path) : Data{ path }
    {
        Groups.resize(Data.NumGroups());
        Name      = Data.Name;
        NumGroups = Data.NumGroups();
        NumFaces  = Data.NumFaces;

        string copy = path_combine(folder_path(path), file_name(path) + "_copy.obj");
        Data.SaveAsOBJ(copy);
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

    SDMeshGroup* SDMesh::AddGroup(string groupname)
    {
        MeshGroup& group = Data.CreateGroup(groupname);
        Groups.emplace_back(make_unique<SDMeshGroup>(Data, group));
        return Groups.back().get();
    }

    ////////////////////////////////////////////////////////////////////////////////////

    static string to_string(const wchar_t* wstr)
    {
        return { wstr, wstr + wcslen(wstr) };
    }

    DLLAPI(SDMesh*) SDMeshOpen(const wchar_t* filename)
    {
        auto sdm = new SDMesh{ to_string(filename) };
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

    DLLAPI(SDMesh*) SDMeshCreateEmpty(const wchar_t* meshname)
    {
        SDMesh* mesh = new SDMesh{};
        mesh->Data.Name = to_string(meshname);
        mesh->Name = mesh->Data.Name;
        return mesh;
    }

    DLLAPI(bool) SDMeshSave(SDMesh* mesh, const wchar_t* filename)
    {
        return mesh->Data.SaveAs(to_string(filename));
    }

    DLLAPI(SDMeshGroup*) SDMeshNewGroup(SDMesh* mesh, const wchar_t* groupname, Matrix4* transform)
    {
        SDMeshGroup* group = mesh->AddGroup(to_string(groupname));
        if (transform) group->Transform = *transform;
        return group;
    }

    DLLAPI(void) SDMeshGroupSetData(SDMeshGroup* group,
                                    Vector3* verts, Vector3* normals, Vector2* coords, int numVertices,
                                    ushort* indices, int numIndices)
    {
        group->SetData(verts, normals, coords, numVertices, indices, numIndices);
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
        Material& mat = group->Data.CreateMaterial(to_string(name));
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
