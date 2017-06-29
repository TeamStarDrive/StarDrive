#include "Mesh.h"
#if _WIN32
#include <memory> // unique_ptr
#include <cassert>
#include <fbxsdk.h>
#include <rpp/file_io.h>

namespace mesh
{
    static FbxManager*    SdkManager;
    static FbxIOSettings* IOSettings;
    static FbxAxisSystem  SaveAxisSystem = { FbxAxisSystem::eDirectX };

    ///////////////////////////////////////////////////////////////////////////////////////////////

    // scoped pointer for safely managing FBX resources
    template<class T> struct FbxPtr : unique_ptr<T, void(*)(T*)>
    {
        FbxPtr(T* obj) : unique_ptr<T, void(*)(T*)>(obj, [](T* o) { o->Destroy(); }) {}
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
            default: assert(false && "Unsupported mesh reference mode");
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
            default: assert(false && "Unsupported mesh reference mode");
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

    ///////////////////////////////////////////////////////////////////////////////////////////////

    static void LoadVertsAndFaces(MeshGroup& meshGroup, const FbxMesh* fbxMesh)
    {
        int numVerts = fbxMesh->GetControlPointsCount();
        meshGroup.Verts.resize(numVerts);
        Vector3*    verts    = meshGroup.Verts.data();
        FbxVector4* fbxVerts = fbxMesh->GetControlPoints();

        for (int i = 0; i < numVerts; ++i) // indexing: enable AVX optimization
        {
            verts[i].x =  (float)fbxVerts[i].mData[0];
            verts[i].z = -(float)fbxVerts[i].mData[1]; // OGL Z is fwd, so ourFwdZ = fbxFwdY
            verts[i].y =  (float)fbxVerts[i].mData[2]; // in OGL/D3D Y axis is up, so ourUpY = fbxUpZ
        }

        //int polyVertCount = fbxMesh->GetPolygonVertexCount(); // num indices???

        int numPolys = fbxMesh->GetPolygonCount();
        int* indices = fbxMesh->GetPolygonVertices();
        meshGroup.Faces.resize(numPolys);
        Face* faces = meshGroup.Faces.data();

        //printf("  %5d  vertices\n", numVerts);
        //printf("  %5d  faces\n", numPolys);

        for (int ipoly = 0; ipoly < numPolys; ++ipoly)
        {
            Face& face = faces[ipoly];
            int numPolyVerts = fbxMesh->GetPolygonSize(ipoly);
            int* vertexIds = &indices[fbxMesh->GetPolygonVertexIndex(ipoly)];
            
            for (int i = 0; i < numPolyVerts; ++i)
                face.emplace_elem().v = vertexIds[i];
        }
    }

    static void LoadNormals(MeshGroup& meshGroup, FbxGeometryElementNormal* elementNormal)
    {
        FbxLayerElement::EMappingMode mapMode = elementNormal->GetMappingMode();
        const bool perPointMapping = mapMode == FbxLayerElement::eByControlPoint; // vertex or polygon normals?

        FbxReadLock<FbxVector4> normalsLock = elementNormal->GetDirectArray();
        FbxReadLock<int>        indexLock   = elementNormal->GetIndexArray();
        const FbxVector4* fbxNormals = normalsLock.data;
        const int*        indices    = indexLock.data; // if != null, normals are indexed

        //printf("  %5d  %s normals\n", normalsLock.count, toString(mapMode));

        const int numNormals = normalsLock.count;
        meshGroup.Normals.resize(numNormals);
        Vector3* normals = meshGroup.Normals.data();

        // copy all normals; at this point it's not important if they are indexed or unindexed
        for (int i = 0; i < numNormals; ++i)
        {
            normals[i].x =  (float)fbxNormals[i].mData[0];
            normals[i].z = -(float)fbxNormals[i].mData[1];
            normals[i].y =  (float)fbxNormals[i].mData[2];
        }

        const int numFaces = meshGroup.NumFaces();
        Face* faces = meshGroup.Faces.data();

        // each polygon vertex can have multiple normals, but if indices are used, most will be shared normals
        if (mapMode == FbxLayerElement::eByPolygonVertex)
        {
            meshGroup.NormalsMapping = MapPerFaceVertex;

            // @todo optimize normals by joining duplicate normals
            int nextNormalId = 0; // per-face-vertex ID
            for (int faceId = 0; faceId < numFaces; ++faceId)
            {
                for (VertexDescr& vd : faces[faceId])
                {
                    assert((indices ? nextNormalId < indexLock.count : nextNormalId < numNormals));
                    vd.n = indices ? indices[nextNormalId] : nextNormalId;
                    ++nextNormalId;
                }
            }
        }
        else if (mapMode == FbxLayerElement::eByControlPoint) // each mesh vertex has a single normal (best case)
        {
            meshGroup.NormalsMapping = MapPerVertex;

            for (int faceId = 0; faceId < numFaces; ++faceId)
                for (VertexDescr& vd : faces[faceId])
                    vd.n = indices ? indices[vd.v] : vd.v; // indexed by VertexId OR same as VertexId
        }
        else if (mapMode == FbxLayerElement::eByPolygon) // each polygon has a single normal, OK case, but not ideal
        {
            meshGroup.NormalsMapping = MapPerFace;

            // @todo indices[faceId] might be wrong
            for (int faceId = 0; faceId < numFaces; ++faceId)
                for (VertexDescr& vd : faces[faceId])
                    vd.n = indices ? indices[faceId] : faceId;
        }
    }

    static void LoadCoords(MeshGroup& meshGroup, FbxGeometryElementUV* elementUVs)
    {
        FbxLayerElement::EMappingMode mapMode = elementUVs->GetMappingMode();
        assert(mapMode == FbxLayerElement::eByPolygonVertex);

        FbxReadLock<FbxVector2> uvsLock   = elementUVs->GetDirectArray();
        FbxReadLock<int>        indexLock = elementUVs->GetIndexArray();
        const FbxVector2* fbxUVs  = uvsLock.data;
        const int*        indices = indexLock.data; // if != null, UVs are indexed

        const int numCoords = uvsLock.count;
        meshGroup.Coords.resize(numCoords);
        Vector2* coords = meshGroup.Coords.data();

        //printf("  %5d  %s coords\n", numCoords, toString(mapMode));

        for (int i = 0; i < numCoords; ++i)
        {
            coords[i].x = (float)fbxUVs[i].mData[0];
            coords[i].y = (float)fbxUVs[i].mData[1];
        }

        const int numFaces = meshGroup.NumFaces();
        Face* faces = meshGroup.Faces.data();

        // each polygon vertex can have multiple UV coords,
        // this allows multiple UV shells, so UV-s aren't forced to be contiguous
        // if indices != null, then most of these UV-s coords will be shared
        if (mapMode == FbxLayerElement::eByPolygonVertex)
        {
            meshGroup.CoordsMapping = MapPerFaceVertex;

            int nextCoordId = 0; // per-face-vertex ID
            for (int faceId = 0; faceId < numFaces; ++faceId)
            {
                for (VertexDescr& vd : faces[faceId])
                {
                    assert((indices ? nextCoordId < indexLock.count : nextCoordId < numCoords));
                    vd.t = indices ? indices[nextCoordId] : nextCoordId;
                    ++nextCoordId;
                }
            }
        }
        else if (mapMode == FbxLayerElement::eByControlPoint)
        {
            meshGroup.CoordsMapping = MapPerVertex;

            for (int faceId = 0; faceId < numFaces; ++faceId)
                for (VertexDescr& vd : faces[faceId])
                    vd.t = indices ? indices[vd.v] : vd.v; // indexed separately OR same as VertexId
        }
        else assert(false && "Unsupported UV map mode");
    }

    static void LoadVertexColors(MeshGroup& meshGroup, FbxGeometryElementVertexColor* elementColors)
    {
        FbxLayerElement::EMappingMode mapMode = elementColors->GetMappingMode();
        const bool perPointMapping = mapMode == FbxLayerElement::eByControlPoint; // vertex or polygon colors?

        FbxReadLock<FbxColor> colorsLock = elementColors->GetDirectArray();
        FbxReadLock<int>      indexLock  = elementColors->GetIndexArray();
        const FbxColor* fbxColors = colorsLock.data;
        const int*      indices   = indexLock.data; // if != null, colors are indexed

        const int numColors = colorsLock.count;
        meshGroup.Colors.resize(numColors);
        Vector3* colors = meshGroup.Colors.data();

        //printf("  %5d  %s colors\n", numColors, toString(mapMode));

        for (int i = 0; i < numColors; ++i)
        {
            colors[i].x = (float)fbxColors[i].mRed;
            colors[i].y = (float)fbxColors[i].mGreen;
            colors[i].z = (float)fbxColors[i].mBlue;
        }

        const int numFaces = meshGroup.NumFaces();
        Face* faces = meshGroup.Faces.data();

        // with eByPolygonVertex, each polygon vertex can have multiple colors,
        // this allows full face coloring with no falloff blending with neighbouring faces
        // if indices != null, then most of these colors will be shared
        if (mapMode == FbxLayerElement::eByPolygonVertex)
        {
            meshGroup.ColorMapping = MapPerFaceVertex;

            // @todo Optimize, Detect identical colors for non-optimized index colors

            int nextColorId = 0; // per-face-vertex ID
            for (int faceId = 0; faceId < numFaces; ++faceId)
            {
                for (VertexDescr& vd : faces[faceId])
                {
                    assert((indices ? nextColorId < indexLock.count : nextColorId < numColors));
                    vd.c = indices ? indices[nextColorId] : nextColorId;
                    ++nextColorId;
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

    bool Mesh::LoadFBX(strview meshPath) noexcept
    {
        Clear();
        InitFbxManager();
        FbxPtr<FbxImporter> importer = FbxImporter::Create(SdkManager, "");
        //int format = SdkManager->GetIOPluginRegistry()->FindReaderIDByExtension("fbx");
        int format = -1;

        if (!importer->Initialize(meshPath.to_cstr(), format, SdkManager->GetIOSettings())) {
            fprintf(stderr, "Failed to open file '%s': %s\n", meshPath.to_cstr(), importer->GetStatus().GetErrorString());
            return false;
        }

        FbxPtr<FbxScene> scene = FbxScene::Create(SdkManager, "scene");
        if (!importer->Import(scene.get())) {
            fprintf(stderr, "Failed to load FBX '%s': %s\n", meshPath.to_cstr(), importer->GetStatus().GetErrorString());
            return false;
        }
        importer.reset();

        if (FbxNode* root = scene->GetRootNode())
        {
            Name = file_name(meshPath);
            printf("LoadFBX %20s:", Name.c_str());

            // @note ConvertScene only affects the global/local matrices, it doesn't modify the vertices themselves
            //FbxAxisSystem sceneAxisSys = scene->GetGlobalSettings().GetAxisSystem();
            //if (sceneAxisSys != AxisSystem)
            //    AxisSystem.ConvertScene(scene.get());

            int numChildren = root->GetChildCount();
            for (int ichild = 0; ichild < numChildren; ++ichild)
            {
                FbxNode* child = root->GetChild(ichild);
                if (FbxMesh* mesh = child->GetMesh())
                {
                    assert(NumGroups() == 0 && "Multiple SubMeshes not supported yet");
                    //printf("FbxMesh '%s':", child->GetName());
                    //FbxDouble3 rot = child->LclRotation.Get();
                    //printf("  rotation %.1f, %.1f, %.1f\n", rot[0], rot[1], rot[2]);

                    auto& meshGroup = CreateGroup(child->GetName());

                    LoadVertsAndFaces(meshGroup, mesh);
                    if (auto* normals = mesh->GetElementNormal())      LoadNormals(meshGroup, normals);
                    if (auto* uvs     = mesh->GetElementUV())          LoadCoords(meshGroup, uvs);
                    if (auto* colors  = mesh->GetElementVertexColor()) LoadVertexColors(meshGroup, colors);

                    NumFaces += meshGroup.NumFaces();

                    printf("  %5d verts  %5d polys", meshGroup.NumVerts(), meshGroup.NumFaces());
                    if (meshGroup.NumCoords()) printf("  %5d uvs", meshGroup.NumCoords());
                    if (meshGroup.NumColors()) printf("  %5d colors", meshGroup.NumColors());
                    printf("\n");
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

        printf("  %20s:  %5d verts  %5d polys", group.Name.c_str(), numVerts, group.NumFaces());

        for (int i = 0; i < numVerts; ++i) // indexing: enable AVX optimization
        {
            points[i].mData[0] = verts[i].x;
            points[i].mData[1] = -verts[i].y;
            points[i].mData[2] = -verts[i].z;
        }
    }

    static void SaveNormals(const MeshGroup& group, FbxMesh* mesh)
    {
        if (const int numNormals = group.NumNormals())
        {
            //printf("  %5d  normals\n", numNormals);

            assert((group.NormalsMapping == MapPerVertex || group.NormalsMapping == MapPerFaceVertex)
                && "Only per-vertex or per-face-vertex normals are supported");

            FbxGeometryElementNormal* elementNormal = mesh->CreateElementNormal();
            elementNormal->SetMappingMode(toFbxMapping(group.NormalsMapping));
            elementNormal->SetReferenceMode(toFbxReference(group.NormalsMapping));

            auto& elements = elementNormal->GetDirectArray();
            const Vector3* normals = group.Normals.data();

            for (int i = 0; i < numNormals; ++i)
            {
                elements.Add(FbxVector4{
                     normals[i].x,
                    -normals[i].y,
                    -normals[i].z
                });
            }

            if (group.NormalsMapping == MapPerFaceVertex)
            {
                auto& indices = elementNormal->GetIndexArray();
                for (const Face& face : group)
                    for (const VertexDescr& vd : face)
                        indices.Add(vd.n);
            }
        }
    }

    static void SaveCoords(const MeshGroup& group, FbxMesh* mesh)
    {
        if (const int numCoords = group.NumCoords())
        {
            printf("  %5d uvs", numCoords);

            assert((group.CoordsMapping == MapPerVertex || group.CoordsMapping == MapPerFaceVertex)
                && "Only per-vertex or per-face-vertex UV coords are supported");

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
                for (const Face& face : group)
                    for (const VertexDescr& vd : face)
                        indices.Add(vd.t);
            }
        }
    }

    static void SaveColors(const MeshGroup& group, FbxMesh* mesh)
    {
        if (const int numColors = group.NumColors())
        {
            printf("  %5d colors", numColors);

            assert((group.ColorMapping == MapPerVertex || group.ColorMapping == MapPerFaceVertex) 
                && "Only per-vertex or per-face-vertex colors are supported");

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
                for (const Face& face : group)
                    for (const VertexDescr& vd : face)
                        indices.Add(vd.c);
            }
        }
    }

    static void CreatePolygons(const MeshGroup& group, FbxMesh* mesh)
    {
        for (const Face& face : group)
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
            fprintf(stderr, "Warning: no faces to export to '%s'\n", meshPath.to_cstr());
            return false;
        }
        if (!NumGroups()) {
            fprintf(stderr, "Warning: no mesh groups to export to '%s'\n", meshPath.to_cstr());
            return false;
        }

        InitFbxManager();
        FbxPtr<FbxExporter> exporter = FbxExporter::Create(SdkManager, "");
        //int format = SdkManager->GetIOPluginRegistry()->FindWriterIDByDescription("FBX 6.0 binary (*.fbx)");
        int format = -1;

        if (!exporter->Initialize(meshPath.to_cstr(), format, SdkManager->GetIOSettings())) {
            fprintf(stderr, "Failed to open file '%s' for writing: %s\n", meshPath.to_cstr(), exporter->GetStatus().GetErrorString());
            return false;
        }
        if (!exporter->SetFileExportVersion(FBX_2014_00_COMPATIBLE, FbxSceneRenamer::eNone)) {
            fprintf(stderr, "Failed to set FBX export version: %s\n", exporter->GetStatus().GetErrorString());
            return false;
        }

        FbxPtr<FbxScene> scene = FbxScene::Create(SdkManager, "scene");
        scene->GetGlobalSettings().SetAxisSystem(SaveAxisSystem);
        scene->GetGlobalSettings().SetSystemUnit(FbxSystemUnit(100.0/*meters*/));

        if (FbxNode* root = scene->GetRootNode())
        {
            printf("SaveFBX  %20s:  %5d verts  %5d polys", Name.c_str(), TotalVerts(), TotalFaces());

            for (const MeshGroup& group : Groups)
            {
                FbxMesh* mesh = FbxMesh::Create(scene.get(), "");
                SaveVertices(group, mesh);
                SaveNormals(group, mesh);
                SaveCoords(group, mesh);
                SaveColors(group, mesh);
                CreatePolygons(group, mesh);

                FbxNode* node = FbxNode::Create(scene.get(), group.Name.c_str());
                node->SetNodeAttribute(mesh);
                root->AddChild(node);
            }
            printf("\n");
        }

        if (!exporter->Export(scene.get())) {
            fprintf(stderr, "Failed to export FBX '%s': %s\n", meshPath.to_cstr(), exporter->GetStatus().GetErrorString());
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
    bool Mesh::LoadFBX(const string& meshPath) noexcept
    {
        fprintf(stderr, "Error: FBX not supported on this platform!\n%s\n", meshPath.c_str());
        return false;
    }
    bool Mesh::SaveAsFBX(const string& meshPath) const noexcept
    {
        fprintf(stderr, "Error: FBX not supported on this platform!\n%s\n", meshPath.c_str());
        return false;
    }
}
#endif
