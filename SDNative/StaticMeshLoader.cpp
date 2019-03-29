#include "StaticMeshLoader.h"
#include <rpp/file_io.h>

namespace SDNative
{
    ////////////////////////////////////////////////////////////////////////////////////

    static void ComputeTangentSpace(
        const vector<Vector3>& vertices,
        const vector<Vector2>& coords,
        const vector<ushort>& indices,
        vector<Vector3>* tangents,
        vector<Vector3>* biNormals)
    {
        int numVertices = (int)vertices.size();
        int numIndices  = (int)indices.size();
        const Vector3* pVertices = vertices.data();
        const ushort*  pIndices  = indices.data();

        tangents->resize(numVertices);
        biNormals->resize(numVertices);
        Vector3* pTangents  = tangents->data();
        Vector3* pBiNormals = biNormals->data();

        for (int i = 0; i < numIndices; i += 3)
        {
            int a = pIndices[i];
            int b = pIndices[i + 1];
            int c = pIndices[i + 2];
            Vector3 p0 = vertices[a];
            Vector3 p1 = vertices[b];
            Vector3 p2 = vertices[c];
            Vector2 uv0 = coords[a];
            Vector2 uv1 = coords[b];
            Vector2 uv2 = coords[c];

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
            Vector3 tangent  = (t2*P - t1*Q) * tmp;
            Vector3 biNormal = (s1*Q - s2*P) * tmp;

            pTangents[a] += tangent;
            pTangents[b] += tangent;
            pTangents[c] += tangent;
            pBiNormals[a] += biNormal;
            pBiNormals[b] += biNormal;
            pBiNormals[c] += biNormal;
        }
        for (int i = 0; i < numVertices; ++i)
        {
            pTangents[i].normalize();
            pBiNormals[i].normalize();
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////

    SDMeshGroup::SDMeshGroup(SDMesh& mesh, int groupId)
        : GroupId{ groupId }, TheMesh{ mesh }
    {
        Name = mesh.Data[groupId].Name;
        Mat = mesh.GetOrCreateMat(GetGroup().Mat);
        InitVertices();
    }

    void SDMeshGroup::InitVertices()
    {
        Nano::MeshGroup& group = GetGroup();
        if (!group.IsEmpty())
        {
            Bounds = rpp::BoundingSphere::create(group.VertexData(), group.NumVerts());
        }
        else
        {
            fprintf(stderr, "WARNING: No mesh data for group %d\n", GroupId);
        }
    }

    template<class T> T* Resize(vector<T>& elements, MapMode& mapMode, int vertexCount)
    {
        mapMode = MapPerVertex;
        elements.resize(vertexCount);
        return elements.data();
    }

    template<class T> T* Next(T* ptr, int stride)
    {
        return (T*)((rpp::byte*)ptr + stride);
    }

    void SDMeshGroup::SetData(SDVertexData vd)
    {
        MeshGroup& group = GetGroup();
        Matrix4 transform = Transform.inverse();

        group.Clear();
        Layout.clear();
        IndexData.clear();
        VertexData.clear();

        int numVertices = vd.VertexCount;
        if (numVertices > 0)
        {
            int stride = vd.VertexStride;

            if (const auto* src = vd.GetOffset<Vector3>(SDElementUsage::Position))
            {
                group.Verts.resize(numVertices);
                auto* dst = group.Verts.data();
                for (int i = 0; i < numVertices; ++i)
                {
                    Vector3 pos = transform * (*src);
                    dst[i] = { pos.x, -pos.z, -pos.y };
                    src = Next(src, stride);
                }
                Bounds = BoundingSphere::create(dst, numVertices);
            }
            if (const auto* src = vd.GetOffset<Vector2>(SDElementUsage::Coordinate))
            {
                auto* dst = Resize(group.Coords, group.CoordsMapping, numVertices);
                for (int i = 0; i < numVertices; ++i)
                {
                    Vector2 uv = *src;
                    dst[i] = { uv.x, 1.0f - uv.y };
                    src = Next(src, stride);
                }
            }
            if (const auto* src = vd.GetOffset<Vector3>(SDElementUsage::Normal))
            {
                auto* dst = Resize(group.Normals, group.NormalsMapping, numVertices);
                for (int i = 0; i < numVertices; ++i)
                {
                    Vector3 normal = transform * (*src);
                    dst[i] = { normal.x, -normal.z, -normal.y };
                    src = Next(src, stride);
                }
            }
            if (const auto* src = vd.GetOffset<BlendWeights>(SDElementUsage::BlendWeight))
            {
                auto* dst = Resize(group.BlendWeights, group.BlendMapping, numVertices);
                for (int i = 0; i < numVertices; ++i)
                {
                    dst[i] = *src;
                    src = Next(src, stride);
                }
            }
            if (const auto* src = vd.GetOffset<BlendIndices>(SDElementUsage::BlendIndices))
            {
                auto* dst = Resize(group.BlendIndices, group.BlendMapping, numVertices);
                for (int i = 0; i < numVertices; ++i)
                {
                    dst[i] = *src;
                    src = Next(src, stride);
                }
            }
        }

        const bool hasCoords  = !group.Coords.empty();
        const bool hasNormals = !group.Normals.empty();
        group.Tris.resize(vd.IndexCount / 3);
        auto* destFaces = group.Tris.data();

        for (int i = 0, faceId = 0; i < vd.IndexCount; i += 3, ++faceId)
        {
            int v0 = vd.IndexData[i];
            int v1 = vd.IndexData[i+1];
            int v2 = vd.IndexData[i+2];
            Triangle& tri = destFaces[faceId];
            tri.a = { v0, hasCoords?v0:-1, hasNormals?v0:-1 };
            tri.b = { v1, hasCoords?v1:-1, hasNormals?v1:-1 };
            tri.c = { v2, hasCoords?v2:-1, hasNormals?v2:-1 };
        }

        TheMesh.SyncStats();
    }

    template<class T> void CopyElements(const SDVertexData& vd, rpp::byte* ptr, const T* src)
    {
        int stride = vd.VertexStride;
        int vertexCount = vd.VertexCount;
        for (int i = 0; i < vertexCount; ++i)
        {
            *(T*)ptr = src[i];
            ptr += stride;
        }
    }

    SDVertexData SDMeshGroup::CreateCachedVertexData()
    {
        Layout.clear();
        IndexData.clear();
        VertexData.clear();

        MeshGroup& group = GetGroup();
        if (group.NumVerts() == 0)
            return {};

        // Sunburn expects ClockWise
        group.SetFaceWinding(FaceWinding::CW);
        group.OptimizedFlatten(); // force numVertices == numCoords == numNormals
        group.CreateIndexArray(IndexData);

        SDVertexData vd;
        vd.VertexStride = 0;
        vd.IndexCount  = (int)IndexData.size();
        vd.VertexCount = group.NumVerts();
        vd.IndexData   = IndexData.data();

        AddVertexElement(vd.VertexStride, SDElementFormat::Vector3, SDElementUsage::Position);
        
        if (group.NumNormals() > 0)
            AddVertexElement(vd.VertexStride, SDElementFormat::Vector3, SDElementUsage::Normal);
        
        if (group.NumCoords() > 0)
            AddVertexElement(vd.VertexStride, SDElementFormat::Vector2, SDElementUsage::Coordinate);

        vector<Vector3> tangents, biNormals;
        if (group.NumCoords() > 0)
        {
            ComputeTangentSpace(group.Verts, group.Coords, IndexData, &tangents, &biNormals);
            AddVertexElement(vd.VertexStride, SDElementFormat::Vector3, SDElementUsage::Tangent);
            AddVertexElement(vd.VertexStride, SDElementFormat::Vector3, SDElementUsage::BiNormal);
        }

        if (group.NumBlendIndices() > 0)
            AddVertexElement(vd.VertexStride, SDElementFormat::Byte4, SDElementUsage::BlendIndices);

        if (group.NumBlendWeights() > 0)
            AddVertexElement(vd.VertexStride, SDElementFormat::Vector4, SDElementUsage::BlendWeight);

        VertexData.resize(vd.VertexCount * vd.VertexStride);
        for (const SDVertexElement& element : Layout)
        {
            rpp::byte* ptr = VertexData.data() + element.Offset;
            switch (element.Usage)
            {
                case SDElementUsage::Position:     CopyElements(vd, ptr, group.Verts.data());   break;
                case SDElementUsage::Normal:       CopyElements(vd, ptr, group.Normals.data()); break;
                case SDElementUsage::Coordinate:   CopyElements(vd, ptr, group.Coords.data());  break;
                case SDElementUsage::Tangent:      CopyElements(vd, ptr, tangents.data());      break;
                case SDElementUsage::BiNormal:     CopyElements(vd, ptr, biNormals.data());          break;
                case SDElementUsage::BlendIndices: CopyElements(vd, ptr, group.BlendIndices.data()); break;
                case SDElementUsage::BlendWeight:  CopyElements(vd, ptr, group.BlendWeights.data()); break;
                default: break;
            }
        }

        vd.LayoutCount = (int)Layout.size();
        vd.Layout     = Layout.data();
        vd.VertexData = VertexData.data();
        return vd;
    }

    static int ElementSizeInBytes(SDElementFormat format)
    {
        switch (format)
        {
            case SDElementFormat::Single:  return sizeof(float);
            case SDElementFormat::Vector2: return sizeof(float)*2;
            case SDElementFormat::Vector3: return sizeof(float)*3;
            case SDElementFormat::Vector4: return sizeof(float)*4;
            case SDElementFormat::Color:   return sizeof(int); // packed color RGBA
            case SDElementFormat::Byte4:   return 4;
            case SDElementFormat::Short2:  return 4;
            case SDElementFormat::Short4:  return 8;
            case SDElementFormat::Rgba32:  return 4;
        }
        return 4;
    }

    void SDMeshGroup::AddVertexElement(int& stride, SDElementFormat format, SDElementUsage usage)
    {
        auto element = SDVertexElement{(rpp::byte)stride, (rpp::byte)ElementSizeInBytes(format), format, usage};
        Layout.push_back(element);
        stride += element.Size;
    }

    Mesh&      SDMeshGroup::GetMesh()  const { return TheMesh.Data; }
    MeshGroup& SDMeshGroup::GetGroup() const { return TheMesh.Data[GroupId]; }

    ////////////////////////////////////////////////////////////////////////////////////

    SDMesh::SDMesh() = default;

    SDMesh::SDMesh(strview path) : Data{ path }
    {
        Name = Data.Name;
        Groups.resize(Data.NumGroups());
        for (int i = 0; i < Data.NumGroups(); ++i)
        {
            Groups[i] = std::make_unique<SDMeshGroup>(*this, i);
        }
        SyncStats();
    }

    SDMeshGroup* SDMesh::GetGroup(int groupId)
    {
        if (Data.IsValidGroup(groupId))
            return Groups[groupId].get();
        return nullptr;
    }

    SDMeshGroup* SDMesh::AddGroup(string groupName)
    {
        MeshGroup& group = Data.CreateGroup(move(groupName));
        auto* g = Groups.emplace_back(std::make_unique<SDMeshGroup>(*this, group.GroupId)).get();
        SyncStats();
        return g;
    }

    SDMaterial* SDMesh::GetOrCreateMat(const shared_ptr<Nano::Material>& mat)
    {
        if (!mat)
            return nullptr;

        for (unique_ptr<SDMaterial>& mapping : Materials)
            if (mapping->Mat == mat)
                return mapping.get();

        Materials.push_back(std::make_unique<SDMaterial>(mat)); // add new
        return Materials.back().get();
    }

    void SDMesh::SyncStats()
    {
        Name      = Data.Name;
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

    DLLAPI(bool) SDMeshSave(SDMesh* mesh, const wchar_t* fileName)
    {
        return mesh->Data.SaveAs(to_string(fileName));
    }

    DLLAPI(SDMeshGroup*) SDMeshNewGroup(SDMesh* mesh, const wchar_t* groupName, Matrix4* transform)
    {
        SDMeshGroup* group = mesh->AddGroup(to_string(groupName));
        if (transform) group->Transform = *transform;
        return group;
    }

    DLLAPI(void) SDMeshGroupSetData(SDMeshGroup* group, SDVertexData data)
    {
        group->SetData(data);
    }

    DLLAPI(SDVertexData) SDMeshGroupGetData(SDMeshGroup* group)
    {
        return group->CreateCachedVertexData();
    }

    DLLAPI(SDMaterial*) SDMeshCreateMaterial(
        SDMesh* mesh,
        const wchar_t* name, 
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
        shared_ptr<Nano::Material> matPtr = std::make_shared<Nano::Material>();
        Material& mat = *matPtr;
        mat.Name          = to_string(name);
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

        SDMaterial* sdMat = mesh->GetOrCreateMat(matPtr);
        mesh->SyncStats();
        return sdMat;
    }

    DLLAPI(void) SDMeshGroupSetMaterial(SDMeshGroup* group, SDMaterial* material)
    {
        group->Mat = material;
        group->GetGroup().Mat = material ? material->Mat : nullptr;
    }

    DLLAPI(void) SDMeshGroupSetSkeleton(SDMeshGroup* group)
    {
    }

    ////////////////////////////////////////////////////////////////////////////////////
}
