#include "Mesh.h"
#include <rpp/file_io.h>
#include <rpp/sprint.h>
#include <rpp/debugging.h>

namespace mesh
{
    using std::swap;
    using std::make_shared;
    using std::unordered_multimap;
    using rpp::string_buffer;
    using namespace rpp::literals;

    ///////////////////////////////////////////////////////////////////////////////////////////////

    bool Triangle::ContainsVertexId(int vertexId) const
    {
        return a.v == vertexId || b.v == vertexId || c.v == vertexId;
    }

    string to_string(const Triangle& triangle)
    {
        string_buffer sb;
        sb.writef("{%d,%d,%d}", triangle.a.v, triangle.b.v, triangle.c.v);
        return sb.str();
    }

    Vector3 PickedTriangle::center() const
    {
        Assert(good(), "Invalid PickedTriangle");
        Vector3 c = group->Vertex(face->a);
        c += group->Vertex(face->b);
        c += group->Vertex(face->c);
        c /= 3;
        return c;
    }

    Vector3 PickedTriangle::vertex(const VertexDescr& vd) const
    {
        Assert(good(), "Invalid PickedTriangle");
        Assert(vd.v != -1 && vd.v < group->NumVerts(), 
               "Invalid VertexDescr: %d / %d", vd.v, group->NumVerts());

        return group->VertexData()[vd.v];
    }

    int PickedTriangle::id() const
    {
        if (!group || !face)
            return -1;
        const Triangle* faces = group->Faces.data();
        size_t count = group->Faces.size();
        for (size_t i = 0; i < count; ++i)
            if (faces + i == face)
                return int(i);
        return -1;
    }

    string to_string(const PickedTriangle& triangle)
    {
        string_buffer sb;
        sb.write('{');
        sb.write(triangle.group?triangle.group->GroupId:-1);
        sb.write(',');
        if (triangle.face) sb.write(*triangle.face);
        else               sb.write(-1);
        sb.write('}');
        return sb.str();
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    Material& MeshGroup::CreateMaterial(string name)
    {
        Mat = make_shared<Material>();
        Mat->Name = move(name);
        return *Mat;
    }

    void MeshGroup::InvertFaceWindingOrder()
    {
        for (Triangle& tri : Faces)
        {
            // 0 1 2 --> 0 2 1
            swap(tri.b, tri.c);
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
            for (const Triangle& f : Faces)
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

        // normals are calculated for each tri:
        for (const Triangle& tri : Faces)
        {
            if (winding == FaceWindCounterClockWise)
            {
                UpdateNormal(tri.a, tri.b, tri.c, checkDuplicateVerts);
            }
            else
            {
                UpdateNormal(tri.c, tri.b, tri.a, checkDuplicateVerts);
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
        // Flatten the mesh, so each Triangle Vertex is unique
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
        for (Triangle& f : Faces)
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
        Assert(vertexId < NumVerts(), "Invalid vertexId %d >= numVerts(%d)", vertexId, NumVerts());

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
            for (int i = numVertsOld, count = (int)Verts.size(); i < count; ++i)
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

        rpp::append(Faces, group.Faces);
        for (int i = numFacesOld, numFaces = (int)Faces.size(); i < numFaces; ++i)
        {
            Triangle& face = Faces[i];
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

        for (const Triangle& face : Faces)
            for (const VertexDescr& vd : face)
                addVertex(vd);
    }

    void MeshGroup::SplitSeamVertices() noexcept
    {
        auto canShareVertex = [](const VertexDescr& a, const VertexDescr& b) {
            return a.t == b.t && a.n == b.n && a.c == b.c;
        };

        unordered_multimap<int, VertexDescr> addedVerts; addedVerts.reserve(NumVerts());

        auto getExistingVertex = [&](const VertexDescr& old, VertexDescr& out) -> bool
        {
            for (auto r = addedVerts.equal_range(old.v); r.first != r.second; ++r.first)
            {
                VertexDescr existing = r.first->second;
                if (canShareVertex(old, existing)) {
                    out = existing;
                    return true;
                }
            }
            return false;
        };

        size_t numFaces = Faces.size();
        auto*  oldFaces = Faces.data();
        auto*  oldVerts = Verts.data();
        vector<Triangle> faces; faces.resize(numFaces);
        vector<Vector3>  verts; verts.reserve(Verts.size());

        for (size_t faceId = 0; faceId < numFaces; ++faceId)
        {
            const Triangle& oldFace = oldFaces[faceId];
            Triangle& newFace = faces[faceId];
            for (int i = 0; i < 3; ++i)
            {
                const VertexDescr& old = oldFace[i];
                VertexDescr&    result = newFace[i];

                if (getExistingVertex(old, result))
                    continue;

                // insert new
                verts.push_back(oldVerts[old.v]);
                result = { (int)verts.size() - 1, old.t, old.n, old.c };
                addedVerts.emplace(old.v, result);
            }
        }
        Verts = move(verts);
        Faces = move(faces);
    }

    void MeshGroup::PerVertexFlatten() noexcept
    {
        auto* oldCoords  = Coords.empty()  ? nullptr : Coords.data();
        auto* oldNormals = Normals.empty() ? nullptr : Normals.data();
        auto* oldColors  = Colors.empty()  ? nullptr : Colors.data();
        vector<Vector2> coords;   coords.reserve(Verts.size());
        vector<Vector3> normals; normals.reserve(Verts.size());
        vector<Color3>  colors;   colors.reserve(Verts.size());

        vector<bool> added; added.resize(Verts.size());

        for (Triangle& face : Faces)
        {
            for (VertexDescr& vd : face)
            {
                int vertexId = vd.v;
                if (!added[vertexId])
                {
                    added[vertexId] = true;
                    if (oldCoords)   coords.push_back(vd.t != -1 ?  oldCoords[vd.t] : Vector2::ZERO);
                    if (oldNormals) normals.push_back(vd.n != -1 ? oldNormals[vd.n] : Vector3::ZERO);
                    if (oldColors)   colors.push_back(vd.c != -1 ?  oldColors[vd.c] : Color3::ZERO);
                }

                if (oldCoords)  vd.t = vertexId;
                if (oldNormals) vd.n = vertexId;
                if (oldColors)  vd.c = vertexId;
            }
        }

        if (CoordsMapping) {
            CoordsMapping = MapPerVertex;
            Coords = move(coords);
            Assert(Coords.size()  == Verts.size(), "Coords must match vertices");
        }
        if (NormalsMapping) {
            NormalsMapping = MapPerVertex;
            Normals = move(normals);
            Assert(Normals.size() == Verts.size(), "Normals must match vertices");
        }
        if (ColorMapping) {
            ColorMapping = MapPerVertex;
            Colors = move(colors);
            Assert(Colors.size()  == Verts.size(), "Colors must match vertices");
        }
    }

    void MeshGroup::OptimizedFlatten() noexcept
    {
        SplitSeamVertices();
        PerVertexFlatten();
    }

    void MeshGroup::CreateIndexArray(vector<int> &indices) const noexcept
    {
        indices.clear();
        indices.reserve(Faces.size() * 3u);

        for (const Triangle& face : Faces)
            for (const VertexDescr& vd : face)
                indices.push_back(vd.v);
    }

    PickedTriangle MeshGroup::PickTriangle(const Ray& ray) const noexcept
    {
        const Vector3* verts = Verts.data();
        const Triangle* picked = nullptr;
        float closestDist = 9999999999999.0f;

        for (const Triangle& tri : Faces)
        {
            const Vector3& v0 = verts[tri.a.v];
            const Vector3& v1 = verts[tri.b.v];
            const Vector3& v2 = verts[tri.c.v];
            float dist = ray.intersectTriangle(v0, v1, v2);
            if (dist > 0.0f && dist < closestDist) {
                closestDist = dist;
                picked      = &tri;
            }
        }
        return picked ? PickedTriangle{ this, picked, closestDist } : PickedTriangle{};
    }

    void MeshGroup::Print() const
    {
        printf("   group  %-28s  %5d verts  %5d tris", Name.c_str(), NumVerts(), NumFaces());
        if (NumCoords()) printf("  %5d uvs", NumCoords());
        if (NumColors()) printf("  %5d colors", NumColors());
        printf("\n");
    }



    ///////////////////////////////////////////////////////////////////////////////////////////////
    


    Mesh::Mesh() noexcept = default;
    Mesh::~Mesh() noexcept = default;

    Mesh::Mesh(strview meshPath, MeshLoaderOptions options) noexcept
    {
        Load(meshPath, options);
    }

    int Mesh::TotalFaces() const
    {
        return rpp::sum_all(Groups, &MeshGroup::NumFaces);
    }
    int Mesh::TotalVerts() const
    {
        return rpp::sum_all(Groups, &MeshGroup::NumVerts);
    }
    int Mesh::TotalCoords() const
    {
        return rpp::sum_all(Groups, &MeshGroup::NumCoords);
    }
    int Mesh::TotalNormals() const
    {
        return rpp::sum_all(Groups, &MeshGroup::NumNormals);
    }
    int Mesh::TotalColors() const
    {
        return rpp::sum_all(Groups, &MeshGroup::NumColors);
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
        return rpp::emplace_back(Groups, (int)Groups.size(), name);
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

    bool Mesh::Load(strview meshPath, MeshLoaderOptions options) noexcept
    {
        strview ext = file_ext(meshPath);
        if (ext.equalsi("fbx"_sv)) return LoadFBX(meshPath, options);
        if (ext.equalsi("obj"_sv)) return LoadOBJ(meshPath, options);

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
        size_t numGroupsOld  = Groups.size();
        rpp::append(Groups, mesh.Groups);

        auto oldGroupHasIdenticalName = [=](strview name) {
            for (size_t i = 0; i < numGroupsOld; ++i)
                if (Groups[i].Name == name) return true;
            return false;
        };

        for (size_t i = numGroupsOld; i < Groups.size(); ++i)
        {
            MeshGroup& group = Groups[i];
            while (oldGroupHasIdenticalName(group.Name))
                group.Name += "_" + std::to_string(numGroupsOld);

            if (offset != Vector3::ZERO) {
                for (Vector3& vertex : group.Verts)
                    vertex += offset;
            }
        }
    }

    BoundingBox Mesh::CalculateBBox() const noexcept
    {
        if (Groups.empty())
            return {};
        BoundingBox bounds = BoundingBox::create(Groups.front().Verts);
        for (size_t i = 1; i < Groups.size(); ++i)
            bounds.join(BoundingBox::create(Groups[i].Verts));
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

    void Mesh::OptimizedFlatten() noexcept
    {
        for (MeshGroup& group : Groups)
            group.OptimizedFlatten();
    }

    void Mesh::MergeGroups() noexcept
    {
        if (Groups.size() <= 1u)
            return;

        auto& merged = Groups.front();
        while (Groups.size() > 1)
        {
            merged.AddMeshData(Groups.back());
            Groups.pop_back();
        }
    }

    PickedTriangle Mesh::PickTriangle(const Ray& ray) const noexcept
    {
        PickedTriangle closest = {};
        for (const MeshGroup& group : Groups)
        {
            if (PickedTriangle result = group.PickTriangle(ray)) {
                if (!closest || result.distance < closest.distance)
                    closest = result;
            }
        }
        return closest;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
}

