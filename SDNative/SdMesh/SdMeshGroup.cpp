#include "SdMesh.h"

namespace SdMesh
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
        const Vector2* pCoords   = coords.data();

        tangents->resize(numVertices);
        biNormals->resize(numVertices);
        Vector3* pTangents  = tangents->data();
        Vector3* pBiNormals = biNormals->data();

        for (int i = 0; i < numIndices; i += 3)
        {
            int a = pIndices[i];
            int b = pIndices[i + 1];
            int c = pIndices[i + 2];
            Vector3 p0 = pVertices[a];
            Vector3 p1 = pVertices[b];
            Vector3 p2 = pVertices[c];
            Vector2 uv0 = pCoords[a];
            Vector2 uv1 = pCoords[b];
            Vector2 uv2 = pCoords[c];

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
        Nano::MeshGroup& group = GetGroup();
        Mat = mesh.GetOrCreateMat(group.Mat);
        if (!group.IsEmpty())
            Bounds = rpp::BoundingSphere::create(group.VertexData(), group.NumVerts());
    }

    template<class T> T* Resize(vector<T>& elements, Nano::MapMode& mapMode, int vertexCount)
    {
        mapMode = Nano::MapPerVertex;
        elements.resize(vertexCount);
        return elements.data();
    }

    template<class T> T* Next(T* ptr, int stride)
    {
        return (T*)((rpp::byte*)ptr + stride);
    }

    void SDMeshGroup::SetVertexDataFor(Nano::MeshGroup& group, const SDVertexData& vd) const
    {
        Matrix4 transform = Transform.inverse();
        int numVertices = vd.VertexCount;
        int stride = vd.VertexStride;

        if (const auto* src = vd.GetOffset<Vector3>(SDElementUsage::Position)) {
            group.Verts.resize(numVertices);
            auto* dst = group.Verts.data();
            for (int i = 0; i < numVertices; ++i) {
                Vector3 pos = transform * (*src);
                dst[i] = { pos.x, -pos.z, -pos.y };
                src = Next(src, stride);
            }
        }
        if (const auto* src = vd.GetOffset<Vector2>(SDElementUsage::Coordinate)) {
            auto* dst = Resize(group.Coords, group.CoordsMapping, numVertices);
            for (int i = 0; i < numVertices; ++i) {
                Vector2 uv = *src;
                dst[i] = { uv.x, 1.0f - uv.y };
                src = Next(src, stride);
            }
        }
        if (const auto* src = vd.GetOffset<Vector3>(SDElementUsage::Normal))
        {
            auto* dst = Resize(group.Normals, group.NormalsMapping, numVertices);
            for (int i = 0; i < numVertices; ++i) {
                Vector3 normal = transform * (*src);
                dst[i] = { normal.x, -normal.z, -normal.y };
                src = Next(src, stride);
            }
        }
        if (const auto* src = vd.GetOffset<Nano::BlendWeights>(SDElementUsage::BlendWeight)) {
            auto* dst = Resize(group.BlendWeights, group.BlendMapping, numVertices);
            for (int i = 0; i < numVertices; ++i) {
                dst[i] = *src;
                src = Next(src, stride);
            }
        }
        if (const auto* src = vd.GetOffset<Nano::BlendIndices>(SDElementUsage::BlendIndices)) {
            auto* dst = Resize(group.BlendIndices, group.BlendMapping, numVertices);
            for (int i = 0; i < numVertices; ++i) {
                dst[i] = *src;
                src = Next(src, stride);
            }
        }
    }

    void SDMeshGroup::SetData(const SDVertexData& vd)
    {
        Nano::MeshGroup& group = GetGroup();
        group.Clear();
        Layout.clear();
        IndexData.clear();
        VertexData.clear();

        SetVertexDataFor(group, vd);
        Bounds = rpp::BoundingSphere::create(group.Verts);

        const bool hasCoords  = !group.Coords.empty();
        const bool hasNormals = !group.Normals.empty();
        group.Tris.resize(vd.IndexCount / 3);
        auto* destFaces = group.Tris.data();

        for (int i = 0, faceId = 0; i < vd.IndexCount; i += 3, ++faceId)
        {
            int v0 = vd.IndexData[i];
            int v1 = vd.IndexData[i+1];
            int v2 = vd.IndexData[i+2];
            Nano::Triangle& tri = destFaces[faceId];
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

        Nano::MeshGroup& group = GetGroup();
        if (group.NumVerts() == 0)
            return {};

        // Sunburn expects ClockWise
        group.SetFaceWinding(Nano::FaceWinding::CW);
        group.OptimizedFlatten(); // force numVertices == numCoords == numNormals
        group.CreateIndexArray(IndexData);

        SDVertexData vd {};
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

    Nano::Mesh&      SDMeshGroup::GetMesh()  const { return TheMesh.Data; }
    Nano::MeshGroup& SDMeshGroup::GetGroup() const { return TheMesh.Data[GroupId]; }

    ////////////////////////////////////////////////////////////////////////////////////

    DLLAPI(void) SDMeshGroupSetData(SDMeshGroup* group, SDVertexData data)
    {
        group->SetData(data);
    }

    DLLAPI(SDVertexData) SDMeshGroupGetData(SDMeshGroup* group)
    {
        return group->CreateCachedVertexData();
    }

    ////////////////////////////////////////////////////////////////////////////////////
}
