#pragma once
#ifndef MESH_MESH_H
#define MESH_MESH_H
#include <rpp/vec.h>
#include "CollectionExt.h"
#include <memory>

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

        VertexDescr&       operator[](int index)       { return VDS[index]; }
        const VertexDescr& operator[](int index) const { return VDS[index]; }
        const VertexDescr* begin() const { return VDS; }
        const VertexDescr* end()   const { return VDS + Count; }
        VertexDescr* begin() { return VDS; }
        VertexDescr* end()   { return VDS + Count; }
        VertexDescr& emplace_elem() { return VDS[Count++]; }
        void resize(int size) { Count = size; }
        void add(int vertexId, int textureId, int normalId) { VDS[Count++] = { vertexId, textureId, normalId }; }

        bool ContainsVertexId(int vertexId) const;
    };


    struct Material
    {
        string Name; // name of the material instance
        string MaterialFile; // eg 'default.mtl'

        string DiffusePath;
        string AlphaPath;
        string SpecularPath;
        string NormalPath;
        string EmissivePath;

        Color3 AmbientColor  = Color3::WHITE;
        Color3 DiffuseColor  = Color3::WHITE;
        Color3 SpecularColor = Color3::WHITE;
        Color3 EmissiveColor = Color3::BLACK;

        float Specular = 1.0f;
        float Alpha    = 1.0f;
    };


    enum MapMode
    {
        // this meshgroup element is not mapped
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


    struct MeshGroup;

    struct PickFaceResult
    {
        // @warning These pointers will invalidate if you modify the mesh!!
        const MeshGroup* group = nullptr;
        const Face*      face  = nullptr;
        float distance = 0.0f;
        bool good() const { return group && face && distance != 0.0f; }
        explicit operator bool() const { return good(); }
    };


    // Common 3D mesh vertex for games, as generic as it can get
    struct BasicVertex
    {
        Vector3 pos;
        Vector2 uv;
        Vector3 norm;
    };

    enum FaceWindOrder
    {
        FaceWindClockWise,
        FaceWindCounterClockWise,
    };


    struct MeshGroup
    {
        int GroupId = -1;
        string Name; // name of the suboject
        shared_ptr<Material> Mat;

        // we treat mesh data as 'layers', so everything except Verts is optional
        vector<Vector3> Verts;
        vector<Vector2> Coords;
        vector<Vector3> Normals;
        vector<Color3>  Colors;

        vector<Face> Faces; // face descriptors (tris and/or quads)

        MapMode CoordsMapping  = MapNone;
        MapMode NormalsMapping = MapNone;
        MapMode ColorMapping   = MapNone;

        FaceWindOrder Winding = FaceWindClockWise;

        MeshGroup(int groupId, string name) : GroupId(groupId), Name(name) {}

        bool IsEmpty()   const { return Faces.empty(); }
        int NumFaces()   const { return (int)Faces.size(); }
        int NumVerts()   const { return (int)Verts.size(); }
        int NumCoords()  const { return (int)Coords.size(); }
        int NumNormals() const { return (int)Normals.size(); }
        int NumColors()  const { return (int)Colors.size(); }

        const Face* begin() const { return &Faces.front(); }
        const Face* end()   const { return &Faces.back() + 1; }
        Face* begin() { return &Faces.front(); }
        Face* end()   { return &Faces.back() + 1; }

        // creates and assigns a new material to this mesh group
        Material& CreateMaterial(string name);

        // will scan all Face declarations to ensure all faces are triangular, not quads or polys
        bool CheckIsTriangulated() const;
        void Triangulate();

        // will flip the face winding from CW to CCW or from CCW to CW
        void InvertFaceWindingOrder();

        bool IsFlattened() const noexcept
        {
            return CoordsMapping  == MapPerFaceVertex
                && NormalsMapping == MapPerFaceVertex
                && ColorMapping   == MapPerFaceVertex;
        }

        void UpdateNormal(const VertexDescr& vd0,
                          const VertexDescr& vd1,
                          const VertexDescr& vd2,
                          const bool checkDuplicateVerts = false) noexcept;

        // Recalculates all normals by find shared and non-shared vertices on the same pos
        // Currently does not respect smoothing groups
        // @param checkDuplicateVerts Will perform an O(n^2) search for duplicate vertices to
        //                            correctly calculate normals for mesh surfaces with unwelded verts
        void RecalculateNormals(const bool checkDuplicateVerts = false) noexcept;

        // normal = -normal;
        void InvertNormals() noexcept;

        void SetVertexColor(int vertexId, const Color3& vertexColor) noexcept;

        // Flattens all mesh data, so MapMode is MapPerFaceVertex
        // This will make the mesh data compatible with any 3D graphics engine out there
        // However, mesh data will be thus stored less efficiently (no vertex data sharing)
        // 
        // Verts, Coords, Normals and Colors will all be stored in a linear sequence
        // with equal length, so creating a corresponding vertex/index array is trivial
        //
        // @note DOES NOT TRIANGULATE THE MESH, call MeshGroup::Triangulate() for that
        void FlattenFaceData() noexcept;

        // Adds additional meshgroups from another Mesh
        // Optionally appends an extra offset to position vertices
        void AddMeshData(const MeshGroup& group, Vector3 offset = Vector3::ZERO) noexcept;

        // Gets a basic vertex mesh representation which can be used safely in most games,
        // because the vertices are safely flattened with optimal vertex sharing
        // @note If you called FlattenMeshData() before this, then optimal vertex sharing is not possible
        void CreateGameVertexData(vector<BasicVertex>& vertices, vector<int>& indices) const noexcept;

        // Pick the closest face that intersects with the ray
        PickFaceResult PickFace(const Ray& ray) const noexcept;
    };


    //////////////////////////////////////////////////////////////////////


    /**
     * Mesh coordinate system is the OPENGL coordinate system
     * +X is Right on the screen, +Y is Up, +Z is INTO the screen
     */
    class Mesh
    {
    public:
        // These are intentionally public to allow custom mesh manipulation
        string Name;
        vector<MeshGroup> Groups;
        int NumFaces = 0;
        
        // Default empty mesh
        Mesh() noexcept;
        
        // Automatically constructs a new mesh, check good() or cast to bool to check if successful
        explicit Mesh(strview meshPath) noexcept;

        ~Mesh() noexcept;

        int TotalFaces() const;
        int TotalVerts() const;
        int TotalCoords() const;
        int TotalNormals() const;
        int TotalColors() const;


        bool good() const { return !Groups.empty() && NumFaces > 0; }
        explicit operator bool() const { return  good(); }
        bool operator!()         const { return !good(); }


        MeshGroup* FindGroup(strview name);
        const MeshGroup* FindGroup(strview name) const;
        MeshGroup& CreateGroup(string name);
        MeshGroup& FindOrCreateGroup(strview name);

        shared_ptr<Material> FindMaterial(strview name) const;

        int NumGroups() const { return (int)Groups.size();  }
        bool IsValidGroup(int groupId) const { return (size_t)groupId < Groups.size(); }


        MeshGroup* begin()               { return Groups.data(); }
        MeshGroup* end()                 { return Groups.data() + Groups.size(); }
        MeshGroup& Default()             { return Groups.front(); }
        MeshGroup& operator[](int index) { return Groups[index]; }

        const MeshGroup* begin()               const { return Groups.data(); }
        const MeshGroup* end()                 const { return Groups.data() + Groups.size(); }
        const MeshGroup& Default()             const { return Groups.front(); }
        const MeshGroup& operator[](int index) const { return Groups[index]; }


        Mesh(Mesh&& o) noexcept = default; // Allow MOVE
        Mesh(const Mesh&) noexcept = delete; // NOCOPY, call Clone() manually plz
        Mesh& operator=(Mesh&& o) = default;
        Mesh& operator=(const Mesh&) noexcept = delete;


        void Clear() noexcept;


        // Create a clone of this 3D Mesh on demand. No automatic copy operators allowed.
        // @param cloneMaterials Will also clone the material references
        Mesh Clone(const bool cloneMaterials = false) const noexcept;


        bool Load(strview meshPath) noexcept;
        bool SaveAs(strview meshPath) const noexcept;

        // Is FBX supported on this platform?
        static bool IsFBXSupported() noexcept;
        bool LoadFBX(strview meshPath) noexcept;
        bool LoadOBJ(strview meshPath) noexcept;

        bool SaveAsFBX(strview meshPath) const noexcept;
        bool SaveAsOBJ(strview meshPath) const noexcept;

        // Recalculates all normals by find shared and non-shared vertices on the same pos
        // Currently does not respect smoothing groups
        // @param checkDuplicateVerts Will perform an O(n^2) search for duplicate vertices to
        //                            correctly calculate normals for mesh surfaces with unwelded verts
        void RecalculateNormals(const bool checkDuplicateVerts = false) noexcept;

        // normal = -normal;
        void InvertNormals() noexcept;

        BoundingBox CalculateBBox() const noexcept;
        //BoundingBox CalculateBBox(const vector<IdVector3>& deltas) const noexcept { return BoundingBox::create(Verts, deltas); }

        // Adds additional meshgroups from another Mesh
        // Optionally appends an extra offset to position vertices
        void AddMeshData(const Mesh& mesh, Vector3 offset = Vector3::ZERO) noexcept;

        // Flattens all mesh data, so MapMode is MapPerFaceVertex
        // This will make the mesh data compatible with any 3D graphics engine out there
        // However, mesh data will be thus stored less efficiently (no vertex data sharing)
        // 
        // Verts, Coords, Normals and Colors will all be stored in a linear sequence
        // with equal length, so creating a corresponding vertex/index array is trivial
        //
        // @note DOES NOT TRIANGULATE THE MESH, call MeshGroup::Triangulate() for that
        void FlattenMeshData() noexcept;
        bool IsFlattened() const noexcept;

        // Pick the closest face that intersects with the ray
        PickFaceResult PickFace(const Ray& ray) const noexcept;
    };

    //////////////////////////////////////////////////////////////////////
}

#endif // MESH_MESH_H
