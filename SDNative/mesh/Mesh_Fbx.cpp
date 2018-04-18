#include "Mesh.h"
#include <rpp/debugging.h>
#if _WIN32
#include <memory> // unique_ptr
#include <fbxsdk.h>
#include <rpp/file_io.h>

namespace mesh
{
    static FbxManager*    SdkManager;
    static FbxIOSettings* IOSettings;

    ///////////////////////////////////////////////////////////////////////////////////////////////

    // scoped pointer for safely managing FBX resources
    template<class T> using scoped_ptr = std::unique_ptr<T, void(*)(T*)>;

    template<class T> struct FbxPtr : scoped_ptr<T>
    {
        FbxPtr(T* obj) : scoped_ptr<T>(obj, [](T* o) { o->Destroy(); }) {}
    };

    // scoped read lock
    template<class T> struct FbxReadLock
    {
        FbxLayerElementArrayTemplate<T>& arr;
        const T* data;
        int count;
        FbxReadLock(FbxLayerElementArrayTemplate<T>& arr) : arr{arr}, 
            data{(T*)arr.GetLocked(FbxLayerElementArray::eReadLock)}, 
            count{arr.GetCount()} {}
        ~FbxReadLock() { arr.ReadUnlock(); }
    };

    // scoped write lock with a resize initializer
    template<class T> struct FbxWriteLock
    {
        FbxLayerElementArrayTemplate<T>& arr;
        T* data;
        FbxWriteLock(int resizeTo, FbxLayerElementArrayTemplate<T>& arr) : arr{ arr }
        {
            arr.AddMultiple(resizeTo);
            data = (T*)arr.GetLocked(FbxLayerElementArray::eReadWriteLock);
        }
        ~FbxWriteLock() { arr.ReadWriteUnlock(); }
    };

    static void InitFbxManager()
    {
        if (!SdkManager) 
        {
            SdkManager = FbxManager::Create();
            IOSettings = FbxIOSettings::Create(SdkManager, IOSROOT);
            SdkManager->SetIOSettings(IOSettings);
        }
    }

    static FbxGeometryElement::EMappingMode toFbxMapping(MapMode mode)
    {
        switch (mode)
        {
            default: Assert(false, "Unsupported mesh reference mode");
            case MapNone:          return FbxGeometryElement::eNone;
            case MapPerVertex:     return FbxGeometryElement::eByControlPoint;
            case MapPerFaceVertex: return FbxGeometryElement::eByPolygonVertex;
            case MapPerFace:       return FbxGeometryElement::eByPolygon;
        }
    }

    static FbxLayerElement::EReferenceMode toFbxReference(MapMode mode)
    {
        switch (mode)
        {
            default: Assert(false, "Unsupported mesh reference mode");
            case MapNone:
            case MapPerVertex:     return FbxLayerElement::eDirect;
            case MapPerFaceVertex: return FbxLayerElement::eIndexToDirect;
            case MapPerFace:       return FbxLayerElement::eIndexToDirect;
        }
    }

    // description string for mapping mode
    static const char* toString(FbxGeometryElement::EMappingMode mapping) noexcept
    {
        switch (mapping)
        {
            default:
            case FbxLayerElement::eNone:            return "no";
            case FbxLayerElement::eByControlPoint:  return "per-vertex";
            case FbxLayerElement::eByPolygonVertex: return "per-face-vertex";
            case FbxLayerElement::eByPolygon:       return "per-face";
            case FbxLayerElement::eByEdge:          return "per-edge";
            case FbxLayerElement::eAllSame:         return "uniform";
        }
    }

    static FINLINE Vector3 FbxToOpenGL(FbxVector4 v)
    {
        return {
             (float)v.mData[0],
             (float)v.mData[2],  // in OGL/D3D Y axis is up, so ourUpY = fbxUpZ
            -(float)v.mData[1],  // OGL Z is fwd, so ourFwdZ = fbxFwdY
        };
    }
    static FINLINE Vector3 FbxToOpenGL(FbxDouble3 v)
    {
        return {
             (float)v.mData[0],
             (float)v.mData[2],  // in OGL/D3D Y axis is up, so ourUpY = fbxUpZ
            -(float)v.mData[1],  // OGL Z is fwd, so ourFwdZ = fbxFwdY
        };
    }
    static FINLINE FbxVector4 GLToFbxVec4(Vector3 v)
    {
        return {
            (double)v.x,
           -(double)v.z,
            (double)v.y 
        };
    }
    static FINLINE FbxDouble3 GLToFbxDouble3(Vector3 v)
    {
        return {
            (double)v.x,
           -(double)v.z,
            (double)v.y 
        };
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
    
    static void LoadVertsAndFaces(
        MeshGroup& meshGroup, const FbxMesh* fbxMesh, 
        vector<int>& oldIndices)
    {
        int numVerts = fbxMesh->GetControlPointsCount();
        meshGroup.Verts.resize(numVerts);
        Vector3*    verts    = meshGroup.Verts.data();
        FbxVector4* fbxVerts = fbxMesh->GetControlPoints();

        for (int i = 0; i < numVerts; ++i)
            verts[i] = FbxToOpenGL(fbxVerts[i]);

        int numPolys = fbxMesh->GetPolygonCount();
        int* indices = fbxMesh->GetPolygonVertices(); // control point indices

        vector<Triangle>& faces = meshGroup.Faces;
        faces.reserve(numPolys);
        oldIndices.reserve(numPolys * 3);

        int oldPolyVertId = 0;
        for (int ipoly = 0; ipoly < numPolys; ++ipoly)
        {
            int numPolyVerts = fbxMesh->GetPolygonSize(ipoly);
            int* vertexIds   = &indices[ fbxMesh->GetPolygonVertexIndex(ipoly) ];
            
            Assert(numPolyVerts >= 3, "Not enough polygon vertices: %d. Expected at least 3.", numPolyVerts);

            Triangle* f = &rpp::emplace_back(faces);
            f->a.v = vertexIds[0];
            f->b.v = vertexIds[1];
            f->c.v = vertexIds[2];
            oldIndices.push_back(oldPolyVertId + 0);
            oldIndices.push_back(oldPolyVertId + 1);
            oldIndices.push_back(oldPolyVertId + 2);

            // if we have Quads or Polys, then force triangulation:
            for (int i = 3; i < numPolyVerts; ++i)
            {
                // CCW order:
                // v[0], v[2], v[3]
                VertexDescr vd0 = f->a; // by value, because emplace_back may realloc
                VertexDescr vd2 = f->c;
                f = &rpp::emplace_back(faces);
                f->a = vd0;
                f->b = vd2;
                f->c.v = vertexIds[i];

                int id0 = oldIndices[oldIndices.size() - 3];
                int id2 = oldIndices[oldIndices.size() - 1];
                oldIndices.push_back(id0);
                oldIndices.push_back(id2);
                oldIndices.push_back(oldPolyVertId + i);
            }
            oldPolyVertId += numPolyVerts;
        }
    }

    static void LoadNormals(MeshGroup& meshGroup, 
        FbxGeometryElementNormal* elementNormal, 
        const vector<int>& oldIndices)
    {
        FbxLayerElement::EMappingMode mapMode = elementNormal->GetMappingMode();
        FbxReadLock<FbxVector4> normalsLock = elementNormal->GetDirectArray();
        FbxReadLock<int>        indexLock   = elementNormal->GetIndexArray();
        const FbxVector4* fbxNormals = normalsLock.data;
        const int*        indices    = indexLock.data; // if != null, normals are indexed

        //printf("  %5d  %s normals\n", normalsLock.count, toString(mapMode));

        const int numNormals = normalsLock.count;
        const int maxNormals = indices ? indexLock.count : numNormals;
        meshGroup.Normals.resize(numNormals);
        Vector3* normals = meshGroup.Normals.data();

        // copy all normals; at this point it's not important if they are indexed or unindexed
        for (int i = 0; i < numNormals; ++i)
            normals[i] = FbxToOpenGL(fbxNormals[i]);

        const int numFaces = meshGroup.NumFaces();
        Triangle* faces = meshGroup.Faces.data();

        // each polygon vertex can have multiple normals, 
        // but if indices are used, most will be shared normals
        // eByPolygonVertex There will be one mapping coordinate for each vertex, 
        // for every polygon of which it is a part. This means that a vertex will 
        // have as many mapping coordinates as polygons of which it is a part.
        if (mapMode == FbxLayerElement::eByPolygonVertex)
        {
            meshGroup.NormalsMapping = MapPerFaceVertex;

            const int* oldPolyVertIds = oldIndices.data();
            for (int nextId = 0, faceId = 0; faceId < numFaces; ++faceId)
            {
                for (VertexDescr& vd : faces[faceId])
                {
                    const int polyVertId = oldPolyVertIds[nextId++];
                    Assert(polyVertId < maxNormals, "Normal index out of bounds: %d / %d", polyVertId, maxNormals);
                    vd.n = indices ? indices[polyVertId] : polyVertId;
                }
            }
        }
        else if (mapMode == FbxLayerElement::eByControlPoint) // each mesh vertex has a single normal (best case)
        {
            meshGroup.NormalsMapping = MapPerVertex;

            for (int faceId = 0; faceId < numFaces; ++faceId)
                for (VertexDescr& vd : faces[faceId])
                {
                    const int vertexId = vd.v;
                    Assert(vertexId < maxNormals, "Normal index out of bounds: %d / %d", vertexId, maxNormals);
                    vd.n = indices ? indices[vertexId] : vertexId; // indexed by VertexId OR same as VertexId
                }
        }
        else if (mapMode == FbxLayerElement::eByPolygon) // each polygon has a single normal, OK case, but not ideal
        {
            meshGroup.NormalsMapping = MapPerFace;

            // @todo indices[faceId] might be wrong
            for (int faceId = 0; faceId < numFaces; ++faceId)
                for (VertexDescr& vd : faces[faceId])
                {
                    Assert(faceId < maxNormals, "Normal index out of bounds: %d / %d", faceId, maxNormals);
                    vd.n = indices ? indices[faceId] : faceId;
                }
        }
    }

    static void LoadCoords(MeshGroup& meshGroup, 
        FbxGeometryElementUV* elementUVs, 
        const vector<int>& oldIndices)
    {
        FbxLayerElement::EMappingMode mapMode = elementUVs->GetMappingMode();
        Assert(mapMode == FbxLayerElement::eByPolygonVertex, "Only ByPolygonVertex mapping is supported");

        FbxReadLock<FbxVector2> uvsLock   = elementUVs->GetDirectArray();
        FbxReadLock<int>        indexLock = elementUVs->GetIndexArray();
        const FbxVector2* fbxUVs  = uvsLock.data;
        const int*        indices = indexLock.data; // if != null, UVs are indexed

        const int numCoords = uvsLock.count;
        const int maxCoords = indices ? indexLock.count : numCoords;
        meshGroup.Coords.resize(numCoords);
        Vector2* coords = meshGroup.Coords.data();

        for (int i = 0; i < numCoords; ++i)
        {
            coords[i].x = (float)fbxUVs[i].mData[0];
            coords[i].y = (float)fbxUVs[i].mData[1];
        }

        const int numFaces = meshGroup.NumFaces();
        Triangle* faces = meshGroup.Faces.data();

        // each polygon vertex can have multiple UV coords,
        // this allows multiple UV shells, so UV-s aren't forced to be contiguous
        // if indices != null, then most of these UV-s coords will be shared
        if (mapMode == FbxLayerElement::eByPolygonVertex)
        {
            meshGroup.CoordsMapping = MapPerFaceVertex;

            const int* oldPolyVertIds = oldIndices.data();
            for (int nextId = 0, faceId = 0; faceId < numFaces; ++faceId)
            {
                for (VertexDescr& vd : faces[faceId])
                {
                    const int polyVertId = oldPolyVertIds[nextId++];
                    Assert(polyVertId < maxCoords, "UV index out of bounds: %d / %d", polyVertId, maxCoords);
                    vd.t = indices ? indices[polyVertId] : polyVertId;
                }
            }
        }
        else if (mapMode == FbxLayerElement::eByControlPoint)
        {
            meshGroup.CoordsMapping = MapPerVertex;

            for (int faceId = 0; faceId < numFaces; ++faceId)
                for (VertexDescr& vd : faces[faceId])
                {
                    const int vertexId = vd.v;
                    Assert(vertexId < maxCoords, "UV index out of bounds: %d / %d", vertexId, maxCoords);
                    vd.t = indices ? indices[vertexId] : vertexId; // indexed separately OR same as VertexId
                }
        }
        else Assert(false, "Unsupported UV map mode");
    }

    static void LoadColors(MeshGroup& meshGroup, 
        FbxGeometryElementVertexColor* elementColors, 
        const vector<int>& oldIndices)
    {
        FbxLayerElement::EMappingMode mapMode = elementColors->GetMappingMode();
        FbxReadLock<FbxColor> colorsLock = elementColors->GetDirectArray();
        FbxReadLock<int>      indexLock  = elementColors->GetIndexArray();
        const FbxColor* fbxColors = colorsLock.data;
        const int*      indices   = indexLock.data; // if != null, colors are indexed

        const int numColors = colorsLock.count;
        const int maxColors = indices ? indexLock.count : numColors;
        meshGroup.Colors.resize(numColors);
        Vector3* colors = meshGroup.Colors.data();

        for (int i = 0; i < numColors; ++i)
        {
            colors[i].x = (float)fbxColors[i].mRed;
            colors[i].y = (float)fbxColors[i].mGreen;
            colors[i].z = (float)fbxColors[i].mBlue;
        }

        const int numFaces = meshGroup.NumFaces();
        Triangle* faces = meshGroup.Faces.data();

        // with eByPolygonVertex, each polygon vertex can have multiple colors,
        // this allows full face coloring with no falloff blending with neighbouring faces
        // if indices != null, then most of these colors will be shared
        if (mapMode == FbxLayerElement::eByPolygonVertex)
        {
            meshGroup.ColorMapping = MapPerFaceVertex;

            const int* oldPolyVertIds = oldIndices.data();
            for (int nextId = 0, faceId = 0; faceId < numFaces; ++faceId)
            {
                for (VertexDescr& vd : faces[faceId])
                {
                    const int polyVertId = oldPolyVertIds[nextId++];
                    Assert(polyVertId < maxColors, "Color index out of bounds: %d / %d", polyVertId, maxColors);
                    vd.c = indices ? indices[polyVertId] : polyVertId;
                }
            }
        }
        else if (mapMode == FbxLayerElement::eByControlPoint)
        {
            meshGroup.ColorMapping = MapPerVertex;

            for (int faceId = 0; faceId < numFaces; ++faceId)
                for (VertexDescr& vd : faces[faceId])
                    vd.c = indices ? indices[vd.v] : vd.v; // indexed separately OR same as VertexId
        }
        else if (mapMode == FbxLayerElement::eByPolygon)
        {
            meshGroup.ColorMapping = MapPerFace;

            // @todo indices[faceId] might be wrong
            for (int faceId = 0; faceId < numFaces; ++faceId)
                for (VertexDescr& vd : faces[faceId])
                    vd.c = indices ? indices[faceId] : faceId; // indexed separately OR same as FaceId
        }
    }

    bool Mesh::IsFBXSupported() noexcept { return true; }

    bool Mesh::LoadFBX(strview meshPath, MeshLoaderOptions options) noexcept
    {
        Clear();
        InitFbxManager();
        FbxPtr<FbxImporter> importer = FbxImporter::Create(SdkManager, "");
        //int format = SdkManager->GetIOPluginRegistry()->FindReaderIDByExtension("fbx");
        int format = -1;

        if (!importer->Initialize(meshPath.to_cstr(), format, SdkManager->GetIOSettings())) {
            LogWarning("Failed to open file '%s': %s\n", meshPath, importer->GetStatus().GetErrorString());
            return false;
        }

        FbxPtr<FbxScene> scene = FbxScene::Create(SdkManager, "scene");
        if (!importer->Import(scene.get())) {
            LogWarning("Failed to load FBX '%s': %s\n", meshPath, importer->GetStatus().GetErrorString());
            return false;
        }
        importer.reset();

        if (FbxNode* root = scene->GetRootNode())
        {
            Name = file_name(meshPath);
            LogInfo("LoadFBX %-20s", Name);

            // @note ConvertScene only affects the global/local matrices, it doesn't modify the vertices themselves
            FbxAxisSystem sceneAxisSys = scene->GetGlobalSettings().GetAxisSystem();
            if (sceneAxisSys != FbxAxisSystem{ FbxAxisSystem::eOpenGL })
                LogWarning("Invalid AxisSystem! Please Re-Export the FBX in OpenGL Axis System");

            int numChildren = root->GetChildCount();

            for (int ichild = 0; ichild < numChildren; ++ichild)
            {
                FbxNode* child = root->GetChild(ichild);
                if (FbxMesh* mesh = child->GetMesh())
                {
                    MeshGroup& group = CreateGroup(child->GetName());

                    FbxDouble3 offset = child->LclTranslation.Get();
                    FbxDouble3 rot    = child->LclRotation.Get();   // @note Euler XYZ Degrees
                    FbxDouble3 scale  = child->LclScaling.Get();

                    group.Offset   = FbxToOpenGL(offset);
                    group.Rotation = FbxToOpenGL(rot);
                    group.Scale    = FbxToOpenGL(scale);

                    vector<int> oldIndices;
                    LoadVertsAndFaces(group, mesh, oldIndices);
                    if (auto* normals = mesh->GetElementNormal())      LoadNormals(group, normals, oldIndices);
                    if (auto* uvs     = mesh->GetElementUV())          LoadCoords(group,  uvs,     oldIndices);
                    if (auto* colors  = mesh->GetElementVertexColor()) LoadColors(group,  colors,  oldIndices);

                    NumFaces += group.NumFaces();
                    group.Print();
                }
            }
            return true;
        }
        return false;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    static void SaveVertices(const MeshGroup& group, FbxMesh* mesh)
    {
        int numVerts = group.NumVerts();
        mesh->InitControlPoints(numVerts);
        FbxVector4* points = mesh->GetControlPoints();
        const Vector3* verts = group.Verts.data();

        for (int i = 0; i < numVerts; ++i)
        {
            points[i] = GLToFbxVec4(verts[i]);
        }
    }

    static void SaveNormals(const MeshGroup& group, FbxMesh* mesh)
    {
        if (const int numNormals = group.NumNormals())
        {
            //printf("  %5d  normals\n", numNormals);

            Assert((group.NormalsMapping == MapPerVertex || group.NormalsMapping == MapPerFaceVertex), 
                    "Only per-vertex or per-face-vertex normals are supported");

            FbxGeometryElementNormal* elementNormal = mesh->CreateElementNormal();
            elementNormal->SetMappingMode(toFbxMapping(group.NormalsMapping));
            elementNormal->SetReferenceMode(toFbxReference(group.NormalsMapping));

            auto& elements = elementNormal->GetDirectArray();
            const Vector3* normals = group.Normals.data();

            for (int i = 0; i < numNormals; ++i)
            {
                elements.Add(GLToFbxVec4(normals[i]));
            }

            if (group.NormalsMapping == MapPerFaceVertex)
            {
                auto& indices = elementNormal->GetIndexArray();
                for (const Triangle& face : group)
                    for (const VertexDescr& vd : face)
                        indices.Add(vd.n);
            }
        }
    }

    static void SaveCoords(const MeshGroup& group, FbxMesh* mesh)
    {
        if (const int numCoords = group.NumCoords())
        {
            Assert((group.CoordsMapping == MapPerVertex || group.CoordsMapping == MapPerFaceVertex), 
                    "Only per-vertex or per-face-vertex UV coords are supported");

            FbxGeometryElementUV* elementUVs = mesh->CreateElementUV("DiffuseUV");
            elementUVs->SetMappingMode(toFbxMapping(group.CoordsMapping));
            elementUVs->SetReferenceMode(toFbxReference(group.CoordsMapping));

            auto& elements = elementUVs->GetDirectArray();
            const Vector2* uvs = group.Coords.data();

            for (int i = 0; i < numCoords; ++i)
            {
                elements.Add(FbxVector2{ uvs[i].x, uvs[i].y });
            }

            if (group.CoordsMapping == MapPerFaceVertex)
            {
                auto& indices = elementUVs->GetIndexArray();
                for (const Triangle& face : group)
                    for (const VertexDescr& vd : face)
                        indices.Add(vd.t);
            }
        }
    }

    static void SaveColors(const MeshGroup& group, FbxMesh* mesh)
    {
        if (const int numColors = group.NumColors())
        {
            Assert((group.ColorMapping == MapPerVertex || group.ColorMapping == MapPerFaceVertex), 
                "Only per-vertex or per-face-vertex colors are supported");

            FbxGeometryElementVertexColor* elementColors = mesh->CreateElementVertexColor();
            elementColors->SetMappingMode(toFbxMapping(group.ColorMapping));
            elementColors->SetReferenceMode(toFbxReference(group.ColorMapping));

            auto& elements = elementColors->GetDirectArray();
            const Vector3* colors = group.Colors.data();
            for (int i = 0; i < numColors; ++i)
            {
                elements.Add(FbxColor{ colors[i].x, colors[i].y, colors[i].z });
            }

            if (group.ColorMapping == MapPerFaceVertex)
            {
                auto& indices = elementColors->GetIndexArray();
                for (const Triangle& face : group)
                    for (const VertexDescr& vd : face)
                        indices.Add(vd.c);
            }
        }
    }

    static void CreatePolygons(const MeshGroup& group, FbxMesh* mesh)
    {
        for (const Triangle& face : group)
        {
            mesh->BeginPolygon(-1, -1, -1, false);
            for (const VertexDescr& vd : face)
            {
                mesh->AddPolygon(vd.v, -1);
            }
            mesh->EndPolygon();
        }
        mesh->BuildMeshEdgeArray();
    }

    bool Mesh::SaveAsFBX(strview meshPath) const noexcept
    {
        if (!NumFaces) {
            LogWarning("No faces to export to '%s'\n", meshPath);
            return false;
        }
        if (!NumGroups()) {
            LogWarning("No mesh groups to export to '%s'\n", meshPath);
            return false;
        }

        InitFbxManager();
        FbxPtr<FbxExporter> exporter = FbxExporter::Create(SdkManager, "");
        //int format = SdkManager->GetIOPluginRegistry()->FindWriterIDByDescription("FBX 6.0 binary (*.fbx)");
        int format = -1;

        if (!exporter->Initialize(meshPath.to_cstr(), format, SdkManager->GetIOSettings())) {
            LogWarning("Failed to open file '%s' for writing: %s\n", meshPath, exporter->GetStatus().GetErrorString());
            return false;
        }
        if (!exporter->SetFileExportVersion(FBX_2014_00_COMPATIBLE, FbxSceneRenamer::eNone)) {
            LogWarning("Failed to set FBX export version: %s\n", exporter->GetStatus().GetErrorString());
            return false;
        }

        FbxPtr<FbxScene> scene = FbxScene::Create(SdkManager, "scene");

        FbxAxisSystem axisSys = { FbxAxisSystem::eOpenGL };
        scene->GetGlobalSettings().SetAxisSystem(axisSys);
        scene->GetGlobalSettings().SetSystemUnit(FbxSystemUnit(100.0/*meters*/));

        if (FbxNode* root = scene->GetRootNode())
        {
            LogInfo("SaveFBX %-28s  %5d verts  %5d tris", Name, TotalVerts(), TotalFaces());
            for (const MeshGroup& group : Groups)
            {
                group.Print();
                FbxMesh* mesh = FbxMesh::Create(scene.get(), "");
                SaveVertices(group, mesh);
                SaveNormals(group, mesh);
                SaveCoords(group, mesh);
                SaveColors(group, mesh);
                CreatePolygons(group, mesh);

                FbxNode* node = FbxNode::Create(scene.get(), group.Name.c_str());

                FbxDouble3 pos   = GLToFbxDouble3(group.Offset);
                FbxDouble3 rot   = GLToFbxDouble3(group.Rotation);
                FbxDouble3 scale = GLToFbxDouble3(group.Scale);
                node->LclTranslation.Set(pos);
                node->LclRotation.Set(rot);
                node->LclScaling.Set(scale);

                node->SetNodeAttribute(mesh);
                root->AddChild(node);
            }
        }

        if (!exporter->Export(scene.get())) {
            LogWarning("Failed to export FBX '%s': %s\n", meshPath, exporter->GetStatus().GetErrorString());
            return false;
        }
        return true;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
}
#else // not WIN32:
namespace Wolf3D
{
    bool Mesh::IsFBXSupported() noexcept { return false; }
    bool Mesh::LoadFBX(strview meshPath, MeshLoaderOptions
     options) noexcept
    {
        LogError("FBX not supported on this platform!\n%s", meshPath);
        return false;
    }
    bool Mesh::SaveAsFBX(strview meshPath) const noexcept
    {
        LogError("FBX not supported on this platform!\n%s", meshPath);
        return false;
    }
}
#endif
