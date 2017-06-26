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

    Material& MeshGroup::CreateMaterial(string name)
    {
        Mat = make_shared<Material>();
        Mat->Name = move(name);
        return *Mat;
    }

    bool MeshGroup::CheckIsTriangulated() const
    {
        for (const Face& face : Faces)
            if (face.Count != 3)
                return false;
        return true;
    }

    void MeshGroup::Triangulate()
    {
        throw runtime_error("not implemented");
    }

    void MeshGroup::InvertFaceWindingOrder()
    {
        for (Face& face : Faces)
        {
            if (face.Count == 3)
            {
                // 0 1 2 --> 0 2 1
                swap(face.VDS[1], face.VDS[2]);
            }
            else if (face.Count == 4)
            {
                // 0 1 2 3 --> 0 3 2 1
                swap(face.VDS[1], face.VDS[3]);
            }
        }

        // flip the winding tag
        Winding = (Winding == FaceWindClockWise) ? FaceWindCounterClockWise : FaceWindClockWise;
    }

    void MeshGroup::UpdateNormal(const VertexDescr& vd0, 
                                 const VertexDescr& vd1, 
                                 const VertexDescr& vd2, 
                                 const bool checkDuplicateVerts) noexcept
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

        if (!checkDuplicateVerts)
        {
            // apply normal directly to indexed normals, no exhaustive matching
            Assert(vd0.n != -1 && vd1.n != -1 && vd2.n != -1, "Invalid vertex normals: %d, %d, %d", vd0.n, vd1.n, vd2.n);
            normals[vd0.n] += normal;
            normals[vd1.n] += normal;
            normals[vd2.n] += normal;
        }
        else
        {
            // add normals to any vertex that shares v0/v1/v2 coordinates
            // an unoptimized Mesh may have multiple vertices occupying the same XYZ position
            for (const Face& f : Faces)
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

    }

    void MeshGroup::RecalculateNormals(const bool checkDuplicateVerts) noexcept
    {
        for (Vector3& normal : Normals)
            normal = Vector3::ZERO;

        FaceWindOrder winding = Winding;

        // normals are calculated for each face:
        for (const Face& face : Faces)
        {
            int numVerts = face.Count;
            if (numVerts == 3)
            {
                if (winding == FaceWindCounterClockWise)
                {
                    UpdateNormal(face[0], face[1], face[2], checkDuplicateVerts);
                }
                else
                {
                    UpdateNormal(face[2], face[1], face[0], checkDuplicateVerts);
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
                if (winding == FaceWindCounterClockWise)
                {
                    UpdateNormal(face[0], face[1], face[3], checkDuplicateVerts);
                    UpdateNormal(face[1], face[2], face[3], checkDuplicateVerts);
                }
                else
                {
                    UpdateNormal(face[3], face[1], face[0], checkDuplicateVerts);
                    UpdateNormal(face[3], face[2], face[1], checkDuplicateVerts);
                }
            }
            else
            {
                LogError("Unsupported number of verts per face: %d", numVerts);
            }
        }

        for (Vector3& normal : Normals)
            normal.normalize();
    }

    void MeshGroup::InvertNormals() noexcept
    {
        for (Vector3& normal : Normals)
            normal = -normal;
    }


    void MeshGroup::FlattenFaceData() noexcept
    {
        // Flatten the mesh, so each Face Vertex is unique
        auto* meshVerts   = Verts.data();
        auto* meshCoords  = Coords.data();
        auto* meshNormals = Normals.data();
        auto* meshColors  = Colors.data();
        size_t count = Faces.size() * 3u;
        vector<Vector3> verts; verts.reserve(count);
        vector<Vector2> coords;  if (!Coords.empty())   coords.reserve(count);
        vector<Vector3> normals; if (!Normals.empty()) normals.reserve(count);
        vector<Color3>  colors;  if (!Colors.empty())   colors.reserve(count);

        int vertexId = 0, coordId = 0, normalId = 0, colorId = 0;
        for (Face& f : Faces)
        {
            for (VertexDescr& vd : f)
            {
                if (vd.v != -1) {
                    verts.push_back(meshVerts[vd.v]);
                    vd.v = vertexId++; // set new vertex Id's on the fly
                }
                if (vd.t != -1) {
                    coords.push_back(meshCoords[vd.t]);
                    vd.t = coordId++;
                }
                if (vd.n != -1) {
                    normals.push_back(meshNormals[vd.n]);
                    vd.n = normalId++;
                }
                if (vd.c != -1) {
                    colors.push_back(meshColors[vd.c]);
                    vd.c = colorId++;
                }
            }
        }
        Verts   = move(verts);
        Coords  = move(coords);
        Normals = move(normals);
        Colors  = move(colors);
        CoordsMapping  = Coords.empty()  ? MapNone : MapPerFaceVertex;
        NormalsMapping = Normals.empty() ? MapNone : MapPerFaceVertex;
        ColorMapping   = Colors.empty()  ? MapNone : MapPerFaceVertex;
    }

    void MeshGroup::SetVertexColor(int vertexId, const Color3& vertexColor) noexcept
    {
        Assert(vertexId < NumVerts(), "Invalid vertexId %d >= numVerts(%ld)", vertexId, NumVerts());

        if (Colors.empty()) {
            Colors.resize(Verts.size());
            ColorMapping = MapPerVertex;
        }
        Colors[vertexId] = vertexColor;
    }

    void MeshGroup::AddMeshData(const MeshGroup& group, Vector3 offset) noexcept
    {
        int numVertsOld   = (int)Verts.size();
        int numCoordsOld  = (int)Coords.size();
        int numNormalsOld = (int)Normals.size();
        int numFacesOld   = (int)Faces.size();

        append(Verts, group.Verts);
        if (offset != Vector3::ZERO)
        {
            for (int i = numVertsOld; i < (int)Verts.size(); ++i)
                Verts[i] += offset;
        }
        append(Coords, group.Coords);
        append(Normals, group.Normals);

        // Colors are optional, but since it's a flatmap, we need to resize as appropriate
        if (!Colors.empty() || !group.Colors.empty())
        {
            if (group.Colors.empty()) {
                Colors.resize(Verts.size());
            }
            else {
                Colors.resize(size_t(numVertsOld));
                append(Colors, group.Colors);
            }
            ColorMapping = MapPerVertex;
        }

        append(Faces, group.Faces);
        for (int i = numFacesOld; i < (int)Faces.size(); ++i)
        {
            Face& face = Faces[i];
            for (VertexDescr& vd : face)
            {
                vd.v += numVertsOld;
                if (vd.t != -1) vd.t += numCoordsOld;
                if (vd.n != -1) vd.n += numNormalsOld;
                if (vd.c != -1) vd.c += numVertsOld;
            }
        }
    }

    void MeshGroup::CreateGameVertexData(vector<BasicVertex>& vertices, vector<int>& indices) const noexcept
    {
        auto* meshVerts   = Verts.data();
        auto* meshCoords  = Coords.data();
        auto* meshNormals = Normals.data();

        vertices.reserve(Faces.size() * 3u);
        indices.reserve(Faces.size() * 3u);

        int vertexId = 0;
        auto addVertex = [&](const VertexDescr& vd)
        {
            indices.push_back(vertexId++);
            vertices.emplace_back<BasicVertex>({
                vd.v != -1 ? meshVerts[vd.v]   : Vector3::ZERO,
                vd.t != -1 ? meshCoords[vd.t]  : Vector2::ZERO,
                vd.n != -1 ? meshNormals[vd.n] : Vector3::ZERO
            });
        };

        const bool optimizedVertexSharing = !IsFlattened();
        if (optimizedVertexSharing)
        {
            struct VertexInfo { int index, coordId, normalId; };
            auto canShareVertex = [&](const VertexInfo& in, const VertexDescr& vd)
            {
                if (vd.t == in.coordId && vd.n == in.normalId)
                    return true;
                if (vd.t != -1 && in.coordId != -1 && meshCoords[vd.t] != meshCoords[in.coordId])
                    return false;
                if (vd.n != -1 && in.normalId != -1 && meshNormals[vd.n] != meshNormals[in.normalId])
                    return false;
                return true;
            };

            vector<VertexInfo> flatmap; flatmap.resize(NumVerts());
            auto* flatmapData = flatmap.data();
            memset(flatmapData, -1, sizeof(VertexInfo)*flatmap.size());

            for (const Face& face : Faces)
            {
                for (const VertexDescr& vd : face)
                {
                    VertexInfo& info = flatmapData[vd.v];
                    if (info.index == -1)
                    {
                        info = { vertexId, vd.t, vd.n };
                        addVertex(vd);
                    }
                    else if (canShareVertex(info, vd))
                    {
                        indices.push_back(info.index);
                    }
                    else // vertex can't be shared, so just write a new copy
                    {
                        addVertex(vd);
                    }
                }
            }
        }
        else
        {
            for (const Face& face : Faces)
                for (const VertexDescr& vd : face)
                    addVertex(vd);
        }
    }

    PickFaceResult MeshGroup::PickFace(const Ray& ray) const noexcept
    {
        const Vector3* verts = Verts.data();
        const Face* pickedFace = nullptr;
        float closestDist = 9999999999999.0f;

        for (const Face& face : Faces)
        {
            int numVerts = face.Count;
            if (numVerts < 3) {
                LogError("Unsupported number of verts per face: %d", numVerts);
                continue;
            }

            const Vector3& v0 = verts[face[0].v];
            const Vector3& v1 = verts[face[1].v];
            const Vector3& v2 = verts[face[2].v];
            float dist = ray.intersectTriangle(v0, v1, v2);
            if (dist > 0.0f && dist < closestDist) {
                closestDist = dist;
                pickedFace  = &face;
            }
            if (numVerts == 4)
            {
                const Vector3& v3 = verts[face[3].v];
                dist = ray.intersectTriangle(v1, v2, v3);
                if (dist > 0.0f && dist < closestDist) {
                    closestDist = dist;
                    pickedFace  = &face;
                }
            }
        }
        return pickedFace ? PickFaceResult{ this, pickedFace, closestDist } : PickFaceResult{};
    }



    ///////////////////////////////////////////////////////////////////////////////////////////////
    


    Mesh::Mesh() noexcept
    {
    }

    Mesh::Mesh(strview meshPath) noexcept
    {
        Load(meshPath);
    }

    Mesh::~Mesh() noexcept
    {
    }

    int Mesh::TotalFaces() const
    {
        return sum_all(Groups, &MeshGroup::NumFaces);
    }
    int Mesh::TotalVerts() const
    {
        return sum_all(Groups, &MeshGroup::NumVerts);
    }
    int Mesh::TotalCoords() const
    {
        return sum_all(Groups, &MeshGroup::NumCoords);
    }
    int Mesh::TotalNormals() const
    {
        return sum_all(Groups, &MeshGroup::NumNormals);
    }
    int Mesh::TotalColors() const
    {
        return sum_all(Groups, &MeshGroup::NumColors);
    }

    MeshGroup* Mesh::FindGroup(strview name)
    {
        for (auto& group : Groups)
            if (group.Name == name) return &group;
        return nullptr;
    }

    const MeshGroup* Mesh::FindGroup(strview name) const
    {
        for (auto& group : Groups)
            if (group.Name == name) return &group;
        return nullptr;
    }

    MeshGroup& Mesh::CreateGroup(string name)
    {
        return emplace_back(Groups, (int)Groups.size(), name);
    }

    MeshGroup& Mesh::FindOrCreateGroup(strview name)
    {
        if (MeshGroup* group = FindGroup(name))
            return *group;
        return emplace_back(Groups, (int)Groups.size(), name);
    }

    shared_ptr<Material> Mesh::FindMaterial(strview name) const
    {
        for (const MeshGroup& group : Groups)
            if (name.equalsi(group.Name))
                return group.Mat;
        return {};
    }

    void Mesh::Clear() noexcept
    {
        Name.clear();
        Groups.clear();
        NumFaces = 0;
    }

    Mesh Mesh::Clone(const bool cloneMaterials) const noexcept
    {
        Mesh obj;
        obj.Name     = Name;
        obj.Groups   = Groups;
        obj.NumFaces = NumFaces;
        if (cloneMaterials) {
            for (auto& group : obj.Groups)
                group.Mat = make_shared<Material>(*group.Mat);
        }
        return obj;
    }

    bool Mesh::Load(strview meshPath) noexcept
    {
        strview ext = file_ext(meshPath);
        if (ext.equalsi("fbx"_sv)) return LoadFBX(meshPath);
        if (ext.equalsi("obj"_sv)) return LoadOBJ(meshPath);

        LogError("Error: unrecognized mesh format for file '%s'", meshPath.to_cstr());
        return false;
    }

    bool Mesh::SaveAs(strview meshPath) const noexcept
    {
        strview ext = file_ext(meshPath);
        if (ext.equalsi("fbx"_sv)) return SaveAsFBX(meshPath);
        if (ext.equalsi("obj"_sv)) return SaveAsOBJ(meshPath);

        LogError("Error: unrecognized mesh format for file '%s'", meshPath.to_cstr());
        return false;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    void Mesh::RecalculateNormals(const bool checkDuplicateVerts) noexcept
    {
        for (MeshGroup& group : Groups)
            group.RecalculateNormals(checkDuplicateVerts);
    }

    void Mesh::InvertNormals() noexcept
    {
        for (MeshGroup& group : Groups)
            group.InvertNormals();
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    void Mesh::AddMeshData(const Mesh& mesh, Vector3 offset) noexcept
    {
        int numGroupsOld  = (int)Groups.size();
        append(Groups, mesh.Groups);

        auto oldGroupHasIdenticalName = [=](strview name) {
            for (int i = 0; i < numGroupsOld; ++i)
                if (Groups[i].Name == name) return true;
            return false;
        };

        for (int i = numGroupsOld; i < (int)Groups.size(); ++i)
        {
            MeshGroup& group = Groups[i];
            while (oldGroupHasIdenticalName(group.Name))
                group.Name += "_" + to_string(numGroupsOld);

            if (offset != Vector3::ZERO) {
                for (Vector3& vertex : group.Verts)
                    vertex += offset;
            }
        }
    }

    BoundingBox Mesh::CalculateBBox() const noexcept
    {
        BoundingBox bounds = {};
        for (const MeshGroup& group : Groups)
            bounds.join(BoundingBox::create(group.Verts));
        return bounds;
    }

    void Mesh::FlattenMeshData() noexcept
    {
        for (MeshGroup& group : Groups)
            if (!group.IsFlattened()) group.FlattenFaceData();
    }

    bool Mesh::IsFlattened() const noexcept
    {
        for (const MeshGroup& group : Groups)
            if (!group.IsFlattened()) return false;
        return true;
    }


    PickFaceResult Mesh::PickFace(const Ray& ray) const noexcept
    {
        PickFaceResult closest = {};
        for (const MeshGroup& group : Groups)
        {
            if (PickFaceResult result = group.PickFace(ray)) {
                if (!closest || result.distance < closest.distance)
                    closest = result;
            }
        }
        return closest;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
}

