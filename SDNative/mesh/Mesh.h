#pragma once
#ifndef MESH_MESH_H
#define MESH_MESH_H
#include <rpp/vec.h>
#include "CollectionExt.h"

namespace mesh
{
    using namespace rpp;
    //////////////////////////////////////////////////////////////////////

    struct VertexDescr
    {
        int v = -1; // vertex position index (vertexId)
        int t = -1; // vertex texture index (can be -1, aka no UV info)
        int n = -1; // vertex normal index (can be -1, aka no normal info)
        int c = -1; // vertex color index (can be -1, aka no color info)
    };

    struct Face
    {
        int Count = 0; // number of vertex descriptors, max 4
        VertexDescr VDS[4];

        VertexDescr& operator[](int index) { return VDS[index]; }
        const VertexDescr& operator[](int index) const { return VDS[index]; }
        const VertexDescr* begin() const { return VDS; }
        const VertexDescr* end()   const { return VDS + Count; }
        VertexDescr* begin() { return VDS; }
        VertexDescr* end()   { return VDS + Count; }
        VertexDescr& emplace_elem() { return VDS[Count++]; }
        void resize(int size) { Count = size; }

        bool ContainsVertexId(int vertexId) const;
    };

    struct Material
    {
        string Name; // name of the material instance

        bool empty() const { return Name.empty(); }
    };

    struct MeshGroup
    {
        string Name;       // name of the suboject
        Material Mat;

        vector<Face> Faces; // face descriptors

        int NumFaces() const { return (int)Faces.size(); }
        const Face* begin() const { return &Faces.front(); }
        const Face* end()   const { return &Faces.back() + 1; }
        Face* begin() { return &Faces.front(); }
        Face* end()   { return &Faces.back() + 1; }
    };

    struct FaceId
    {
        int group = -1;
        int face  = -1;

        bool good() const { return group != -1 && face != -1; }
    };

    //////////////////////////////////////////////////////////////////////

    /**
     * Mesh coordinate system is the OPENGL coordinate system
     * +X is Right on the screen, +Y is Up, +Z is INTO the screen
     */
    class Mesh
    {
    public:

        enum MapMode
        {
            // this mesh element is not mapped
            MapNone,

            // extra data is mapped per vertex, this means:
            // colors are mapped for every vertex
            // normals are mapped for every vertex
            // coords are  mapped for every vertex, so UV shells must be contiguous
            MapPerVertex, 

            // extra data is mapped per each face vertex; data can still be shared, but this allows
            // discontiguous submesh data, which is very common
            // colors are mapped for every face vertex, this is quite rare
            // normals are mapped for every face vertex, this is common if you have submeshes with split smoothing groups
            // coords mapped this way can have discontiguous UV shells, which is VERY common
            MapPerFaceVertex,

            // extra data is mapped per face, this is very rare
            // colors are mapped for every face
            // normals are mapped for every face
            // coords are NEVER mapped this way
            MapPerFace,
        };

        // These are intentionally public to allow custom mesh manipulation

        string Name;
        vector<Vector3> Verts;
        vector<Color3>  Colors;
        vector<Vector2> Coords;
        vector<Vector3> Normals;
        vector<MeshGroup> Groups;
        int NumFaces = 0;

        // If you edit these, you must also modify the actual mesh data
        MapMode ColorMapping   = MapNone;
        MapMode CoordsMapping  = MapNone;
        MapMode NormalsMapping = MapNone;

        // Default empty mesh
        Mesh() noexcept;
        
        // Automatically constructs a new mesh, check good() or cast to bool to check if successful
        Mesh(const string& meshPath) noexcept;
        ~Mesh() noexcept;

        bool good() const { return !Verts.empty(); }
        explicit operator bool() const { return !Verts.empty(); }
        bool operator!() const { return Verts.empty(); }
        int NumVerts()   const { return (int)Verts.size();   }
        int NumColors()  const { return (int)Colors.size();  }
        int NumCoords()  const { return (int)Coords.size();  }
        int NumNormals() const { return (int)Normals.size(); }
        int NumGroups()  const { return (int)Groups.size();  }

        Mesh(Mesh&& o) noexcept = default; // Allow MOVE
        Mesh(const Mesh&) noexcept = delete; // NOCOPY, call Clone() manually plz
        Mesh& operator=(Mesh&& o) = default;
        Mesh& operator=(const Mesh&) noexcept = delete;

        void Clear() noexcept;
        // Create a clone of this 3D Mesh on demand. No automatic copy operators allowed.
        Mesh Clone() const noexcept;

        bool Load(const string& meshPath) noexcept;
        bool SaveAs(const string& meshPath) const noexcept;

        // Is FBX supported on this platform?
        static bool IsFBXSupported() noexcept;
        bool LoadFBX(const string& meshPath) noexcept;
        bool LoadOBJ(const string& meshPath) noexcept;

        bool SaveAsFBX(const string& meshPath) const noexcept;
        bool SaveAsOBJ(const string& meshPath) const noexcept;

        // Recalculates all normals by find shared and non-shared vertices on the same pos
        // Currently does not respect smoothing groups
        // @param checkDuplicateVerts Will perform an O(n^2) search for duplicate vertices to
        //                            correctly calculate normals for mesh surfaces with unwelded verts
        void RecalculateNormals(const bool checkDuplicateVerts = false) noexcept;

        void UpdateTriangleNormal(const VertexDescr& vd0, const VertexDescr& vd1, const VertexDescr& vd2) noexcept;
        void UpdateTriangleNormalExhaustive(const vector<Face>& faces, const VertexDescr& vd0,
                                            const VertexDescr& vd1, const VertexDescr& vd2) noexcept;

        void SetVertexColor(int vertexId, const Color3& vertexColor) noexcept;

        BoundingBox CalculateBBox() const noexcept { return BoundingBox::create(Verts); }
        BoundingBox CalculateBBox(const vector<IdVector3>& deltas) const noexcept { return BoundingBox::create(Verts, deltas); }

        // Adds additional meshgroups from another Mesh
        // Optionally appends an extra offset to position vertices
        void AddMeshData(const Mesh& mesh, Vector3 offset = Vector3::ZERO) noexcept;

        // Pick the closest face that intersects with the ray
        FaceId PickFaceId(const Ray& ray) const noexcept;
    };

    //////////////////////////////////////////////////////////////////////
}

#endif // MESH_MESH_H
