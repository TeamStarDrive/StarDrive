#include "Mesh.h"
#include <rpp/file_io.h>
#include <rpp/debugging.h>

namespace mesh
{
    ///////////////////////////////////////////////////////////////////////////////////////////////

    bool Face::ContainsVertexId(int vertexId) const
    {
        for (int i = 0; i < Count; ++i)
            if (VDS[i].v == vertexId)
                return true;
        return false;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    Mesh::Mesh() noexcept
    {
    }

    Mesh::Mesh(const string& meshPath) noexcept
    {
        Load(meshPath);
    }

    Mesh::~Mesh() noexcept
    {
    }

    void Mesh::Clear() noexcept
    {
        Name.clear();
        Verts.clear();
        Coords.clear();
        Normals.clear();
        Colors.clear();
        Groups.clear();
        NumFaces       = 0;
        ColorMapping   = MapNone;
        CoordsMapping  = MapNone;
        NormalsMapping = MapNone;
    }

    Mesh Mesh::Clone() const noexcept
    {
        Mesh obj;
        //obj.MatLib = MatLib;
        obj.Name    = Name;
        obj.Verts   = Verts;
        obj.Coords  = Coords;
        obj.Normals = Normals;
        obj.Colors  = Colors;
        obj.Groups  = Groups;
        obj.NumFaces       = NumFaces;
        obj.ColorMapping   = ColorMapping;
        obj.CoordsMapping  = CoordsMapping;
        obj.NormalsMapping = NormalsMapping;
        return obj;
    }

    bool Mesh::Load(const string& meshPath) noexcept
    {
        strview ext = file_ext(meshPath);
        if (ext.equalsi("fbx"_sv)) return LoadFBX(meshPath);
        if (ext.equalsi("obj"_sv)) return LoadOBJ(meshPath);

        LogError("Error: unrecognized mesh format for file '%s'", meshPath.c_str());
        return false;
    }

    bool Mesh::SaveAs(const string& meshPath) const noexcept
    {
        strview ext = file_ext(meshPath);
        if (ext.equalsi("fbx"_sv)) return SaveAsFBX(meshPath);
        if (ext.equalsi("obj"_sv)) return SaveAsOBJ(meshPath);

        LogError("Error: unrecognized mesh format for file '%s'", meshPath.c_str());
        return false;
    }



    void Mesh::RecalculateNormals(const bool checkDuplicateVerts) noexcept
    {
        // reset all normals (if you are recalculating them)
        for (Vector3& normal : Normals)
            normal = Vector3::ZERO;

        for (const MeshGroup& g : Groups)
        {
            // normals are calculated for each face:
            for (const Face& f : g.Faces)
            {
                int numVerts = f.Count;
                if (numVerts == 3)
                {
                    if (!checkDuplicateVerts)
                    {
                        UpdateTriangleNormal(f[0], f[1], f[2]);
                    }
                    else
                    {
                        UpdateTriangleNormalExhaustive(g.Faces, f[0], f[1], f[2]);
                    } 
                }
                else if (numVerts == 4)
                {
                    // @todo According to OBJ spec, face vertices are in CCW order:
                    // 0--3       3--2
                    // | /|  or   |\ |
                    // |/ |       | \|
                    // 1--2       0--1
                    // This will affect the result of normal calculation, so it should
                    // be reviewed depending on the final target application which may
                    // expect normals in the opposite order
                    if (!checkDuplicateVerts)
                    {
                        UpdateTriangleNormal(f[0], f[1], f[3]);
                        UpdateTriangleNormal(f[1], f[2], f[3]);
                    }
                    else
                    {
                        UpdateTriangleNormalExhaustive(g.Faces, f[0], f[1], f[3]);
                        UpdateTriangleNormalExhaustive(g.Faces, f[1], f[2], f[3]);
                    }
                }
                else
                {
                    LogError("Unsupported number of verts per face: %d", numVerts);
                }
            }
        }

        // normalize all vertex normals
        for (Vector3& normal : Normals)
            normal.normalize();
    }

    void Mesh::UpdateTriangleNormal(const VertexDescr& vd0, const VertexDescr& vd1, const VertexDescr& vd2) noexcept
    {
        auto* verts   = Verts.data();
        auto* normals = Normals.data();

        const Vector3& v0 = verts[vd0.v];
        const Vector3& v1 = verts[vd1.v];
        const Vector3& v2 = verts[vd2.v];

        // calculate triangle normal:
        Vector3 ba = v1 - v0;
        Vector3 ca = v2 - v0;
        Vector3 normal = ba.cross(ca);

        // apply normal directly to indexed normals, no exhaustive matching
        Assert(vd0.n != -1 && vd1.n != -1 && vd2.n != -1, "Invalid vertex normals: %d, %d, %d", vd0.n, vd1.n, vd2.n);
        normals[vd0.n] += normal;
        normals[vd1.n] += normal;
        normals[vd2.n] += normal;
    }

    void Mesh::UpdateTriangleNormalExhaustive(const vector<Face>& faces, const VertexDescr& vd0, 
                                              const VertexDescr& vd1, const VertexDescr& vd2) noexcept
    {
        auto* verts   = Verts.data();
        auto* normals = Normals.data();
        const Vector3& v0 = verts[vd0.v];
        const Vector3& v1 = verts[vd1.v];
        const Vector3& v2 = verts[vd2.v];

        // calculate triangle normal:
        Vector3 ba = v1 - v0;
        Vector3 ca = v2 - v0;
        Vector3 normal = ba.cross(ca);

        // add normals to any vertex that shares v0/v1/v2 coordinates
        // an unoptimized Mesh may have multiple vertices occupying the same XYZ position
        for (const Face& f : faces)
        {
            for (const VertexDescr& vd : f)
            {
                const Vector3& v = verts[vd.v];
                if (v == v0 || v == v1 || v == v2)
                {
                    Assert(vd.n != -1, "Invalid vertex normalId -1");
                    normals[vd.n] += normal;
                }
            }
        }
    }

    void Mesh::SetVertexColor(int vertexId, const Color3& vertexColor) noexcept
    {
        Assert(vertexId < Verts.size(), "Invalid vertexId %d >= numVerts(%ld)", vertexId, Verts.size());

        if (Colors.empty()) {
            Colors.resize(Verts.size());
            if (ColorMapping == MapNone)
                ColorMapping = MapPerVertex;
        }
        Colors[vertexId] = vertexColor;
    }

    void Mesh::AddMeshData(const Mesh & mesh, Vector3 offset) noexcept
    {
        int numVertsOld   = (int)Verts.size();
        int numCoordsOld  = (int)Coords.size();
        int numNormalsOld = (int)Normals.size();
        int numGroupsOld  = (int)Groups.size();

        append(Verts, mesh.Verts);
        if (offset != Vector3::ZERO)
        {
            for (int i = numVertsOld; i < (int)Verts.size(); ++i)
                Verts[i] += offset;
        }
        append(Coords, mesh.Coords);
        append(Normals, mesh.Normals);

        // Colors are optional, but since it's a flatmap, we need to resize as appropriate
        if (!Colors.empty() || !mesh.Colors.empty())
        {
            if (mesh.Colors.empty()) {
                Colors.resize(Verts.size());
            }
            else {
                Colors.resize(size_t(numVertsOld));
                append(Colors, mesh.Colors);
            }
        }
        append(Groups, mesh.Groups);

        for (int i = numGroupsOld; i < (int)Groups.size(); ++i)
        {
            MeshGroup& group = Groups[i];
            group.Name += "_" + to_string(numGroupsOld);

            for (Face& face : group.Faces)
            {
                for (VertexDescr& vd : face)
                {
                    vd.v += numVertsOld;
                    if (vd.t != -1) vd.t += numCoordsOld;
                    if (vd.n != -1) vd.n += numNormalsOld;
                }
            }
        }
    }

    FaceId Mesh::PickFaceId(const Ray& ray) const noexcept
    {
        const Vector3* verts = Verts.data();
        int groupId = 0;
        int faceId  = 0;
        int closestGroup = -1;
        int closestFace  = -1;
        float closestDist = 9999999999999.0f;

        for (const MeshGroup& g : Groups)
        {
            for (const Face& f : g.Faces)
            {
                int numVerts = f.Count;
                if (numVerts < 3) {
                    LogError("Unsupported number of verts per face: %d", numVerts);
                    continue;
                }

                const Vector3& v0 = verts[f[0].v];
                const Vector3& v1 = verts[f[1].v];
                const Vector3& v2 = verts[f[2].v];
                float dist = ray.intersectTriangle(v0, v1, v2);
                if (dist > 0.0f && dist < closestDist) {
                    closestDist = dist;
                    closestGroup = groupId;
                    closestFace = faceId;
                }
                if (numVerts == 4)
                {
                    const Vector3& v3 = verts[f[3].v];
                    dist = ray.intersectTriangle(v1, v2, v3);
                    if (dist > 0.0f && dist < closestDist) {
                        closestDist = dist;
                        closestGroup = groupId;
                        closestFace = faceId;
                    }
                }

                ++faceId;
            }
            ++groupId;
        }
        return FaceId{ closestGroup, closestFace };
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
}

