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
    template<class T> struct FbxPtr : public unique_ptr<T, void(*)(T*)>
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

    static FbxGeometryElement::EMappingMode toFbxMapping(Mesh::MapMode mode)
    {
        switch (mode)
        {
            default: assert(false && "Unsupported mesh reference mode");
            case Mesh::MapNone:          return FbxGeometryElement::eNone;
            case Mesh::MapPerVertex:     return FbxGeometryElement::eByControlPoint;
            case Mesh::MapPerFaceVertex: return FbxGeometryElement::eByPolygonVertex;
            case Mesh::MapPerFace:       return FbxGeometryElement::eByPolygon;
        }
    }

    static FbxLayerElement::EReferenceMode toFbxReference(Mesh::MapMode mode)
    {
        switch (mode)
        {
            default: assert(false && "Unsupported mesh reference mode");
            case Mesh::MapNone:
            case Mesh::MapPerVertex:     return FbxLayerElement::eDirect;
            case Mesh::MapPerFaceVertex: return FbxLayerElement::eIndexToDirect;
            case Mesh::MapPerFace:       return FbxLayerElement::eIndexToDirect;
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

    static void LoadVertsAndFaces(Mesh& mesh, const FbxMesh* fbxMesh, const char* name)
    {
        MeshGroup& meshGroup = emplace_back(mesh.Groups);
        meshGroup.Name = name;

        int numVerts = fbxMesh->GetControlPointsCount();
        mesh.Verts.resize(numVerts);
        Vector3*    verts    = mesh.Verts.data();
        FbxVector4* fbxVerts = fbxMesh->GetControlPoints();

        for (int i = 0; i < numVerts; ++i) // indexing: enable AVX optimization
        {
            //verts[i].x = (float)fbxVerts[i].mData[0]; 
            //verts[i].y = (float)fbxVerts[i].mData[1];
            //verts[i].z = (float)fbxVerts[i].mData[2];
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

    static void LoadNormals(Mesh& mesh, FbxMesh* fbxMesh, FbxGeometryElementNormal* elementNormal)
    {
        FbxLayerElement::EMappingMode mapMode = elementNormal->GetMappingMode();
        const bool perPointMapping = mapMode == FbxLayerElement::eByControlPoint; // vertex or polygon normals?

        FbxReadLock<FbxVector4> normalsLock = elementNormal->GetDirectArray();
        FbxReadLock<int>        indexLock   = elementNormal->GetIndexArray();
        const FbxVector4* fbxNormals = normalsLock.data;
        const int*        indices    = indexLock.data; // if != null, normals are indexed

        //printf("  %5d  %s normals\n", normalsLock.count, toString(mapMode));

        const int numNormals = normalsLock.count;
        mesh.Normals.resize(numNormals);
        Vector3* normals = mesh.Normals.data();

        // copy all normals; at this point it's not important if they are indexed or unindexed
        for (int i = 0; i < numNormals; ++i)
        {
            //normals[i].x = (float)fbxNormals[i].mData[0];
            //normals[i].y = (float)fbxNormals[i].mData[1];
            //normals[i].z = (float)fbxNormals[i].mData[2];
            normals[i].x =  (float)fbxNormals[i].mData[0];
            normals[i].z = -(float)fbxNormals[i].mData[1];
            normals[i].y =  (float)fbxNormals[i].mData[2];
        }

        MeshGroup& meshGroup = mesh.Groups.back();
        const int numFaces = meshGroup.NumFaces();
        Face* faces = meshGroup.Faces.data();

        // each polygon vertex can have multiple normals, but if indices are used, most will be shared normals
        if (mapMode == FbxLayerElement::eByPolygonVertex)
        {
            mesh.NormalsMapping = Mesh::MapPerFaceVertex;

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
            mesh.NormalsMapping = Mesh::MapPerVertex;

            for (int faceId = 0; faceId < numFaces; ++faceId)
                for (VertexDescr& vd : faces[faceId])
                    vd.n = indices ? indices[vd.v] : vd.v; // indexed by VertexId OR same as VertexId
        }
        else if (mapMode == FbxLayerElement::eByPolygon) // each polygon has a single normal, OK case, but not ideal
        {
            mesh.NormalsMapping = Mesh::MapPerFace;

            // @todo indices[faceId] might be wrong
            for (int faceId = 0; faceId < numFaces; ++faceId)
                for (VertexDescr& vd : faces[faceId])
                    vd.n = indices ? indices[faceId] : faceId;
        }
    }

    static void LoadCoords(Mesh& mesh, const FbxMesh* fbxMesh, FbxGeometryElementUV* elementUVs)
    {
        FbxLayerElement::EMappingMode mapMode = elementUVs->GetMappingMode();
        assert(mapMode == FbxLayerElement::eByPolygonVertex);

        FbxReadLock<FbxVector2> uvsLock   = elementUVs->GetDirectArray();
        FbxReadLock<int>        indexLock = elementUVs->GetIndexArray();
        const FbxVector2* fbxUVs  = uvsLock.data;
        const int*        indices = indexLock.data; // if != null, UVs are indexed

        const int numCoords = uvsLock.count;
        mesh.Coords.resize(numCoords);
        Vector2* coords = mesh.Coords.data();

        //printf("  %5d  %s coords\n", numCoords, toString(mapMode));

        for (int i = 0; i < numCoords; ++i)
        {
            coords[i].x = (float)fbxUVs[i].mData[0];
            coords[i].y = (float)fbxUVs[i].mData[1];
        }

        MeshGroup& meshGroup = mesh.Groups.back();
        const int numFaces = meshGroup.NumFaces();
        Face* faces = meshGroup.Faces.data();

        // each polygon vertex can have multiple UV coords,
        // this allows multiple UV shells, so UV-s aren't forced to be contiguous
        // if indices != null, then most of these UV-s coords will be shared
        if (mapMode == FbxLayerElement::eByPolygonVertex)
        {
            mesh.CoordsMapping = Mesh::MapPerFaceVertex;

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
            mesh.CoordsMapping = Mesh::MapPerVertex;

            for (int faceId = 0; faceId < numFaces; ++faceId)
                for (VertexDescr& vd : faces[faceId])
                    vd.t = indices ? indices[vd.v] : vd.v; // indexed separately OR same as VertexId
        }
        else assert(false && "Unsupported UV map mode");
    }

    static void LoadVertexColors(Mesh& mesh, const FbxMesh* fbxMesh, FbxGeometryElementVertexColor* elementColors)
    {
        FbxLayerElement::EMappingMode mapMode = elementColors->GetMappingMode();
        const bool perPointMapping = mapMode == FbxLayerElement::eByControlPoint; // vertex or polygon colors?

        FbxReadLock<FbxColor> colorsLock = elementColors->GetDirectArray();
        FbxReadLock<int>      indexLock  = elementColors->GetIndexArray();
        const FbxColor* fbxColors = colorsLock.data;
        const int*      indices   = indexLock.data; // if != null, colors are indexed

        const int numColors = colorsLock.count;
        mesh.Colors.resize(numColors);
        Vector3* colors = mesh.Colors.data();

        //printf("  %5d  %s colors\n", numColors, toString(mapMode));

        for (int i = 0; i < numColors; ++i)
        {
            colors[i].x = (float)fbxColors[i].mRed;
            colors[i].y = (float)fbxColors[i].mGreen;
            colors[i].z = (float)fbxColors[i].mBlue;
        }

        MeshGroup& meshGroup = mesh.Groups.back();
        const int numFaces = meshGroup.NumFaces();
        Face* faces = meshGroup.Faces.data();

        // with eByPolygonVertex, each polygon vertex can have multiple colors,
        // this allows full face coloring with no falloff blending with neighbouring faces
        // if indices != null, then most of these colors will be shared
        if (mapMode == FbxLayerElement::eByPolygonVertex)
        {
            mesh.ColorMapping = Mesh::MapPerFaceVertex;

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
            mesh.ColorMapping = Mesh::MapPerVertex;

            for (int faceId = 0; faceId < numFaces; ++faceId)
                for (VertexDescr& vd : faces[faceId])
                    vd.c = indices ? indices[vd.v] : vd.v; // indexed separately OR same as VertexId
        }
        else if (mapMode == FbxLayerElement::eByPolygon)
        {
            mesh.ColorMapping = Mesh::MapPerFace;

            // @todo indices[faceId] might be wrong
            for (int faceId = 0; faceId < numFaces; ++faceId)
                for (VertexDescr& vd : faces[faceId])
                    vd.c = indices ? indices[faceId] : faceId; // indexed separately OR same as FaceId
        }
    }

    bool Mesh::IsFBXSupported() noexcept { return true; }

    bool Mesh::LoadFBX(const string& meshPath) noexcept
    {
        Clear();
        InitFbxManager();
        FbxPtr<FbxImporter> importer = FbxImporter::Create(SdkManager, "");
        //int format = SdkManager->GetIOPluginRegistry()->FindReaderIDByExtension("fbx");
        int format = -1;

        if (!importer->Initialize(meshPath.c_str(), format, SdkManager->GetIOSettings())) {
            fprintf(stderr, "Failed to open file '%s': %s\n", meshPath.c_str(), importer->GetStatus().GetErrorString());
            return false;
        }

        FbxPtr<FbxScene> scene = FbxScene::Create(SdkManager, "scene");
        if (!importer->Import(scene.get())) {
            fprintf(stderr, "Failed to load FBX '%s': %s\n", meshPath.c_str(), importer->GetStatus().GetErrorString());
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

                    LoadVertsAndFaces(*this, mesh, child->GetName());
                    if (auto* normals = mesh->GetElementNormal())     LoadNormals(*this, mesh, normals);
                    if (auto* uvs = mesh->GetElementUV())             LoadCoords(*this, mesh, uvs);
                    if (auto* colors = mesh->GetElementVertexColor()) LoadVertexColors(*this, mesh, colors);

                    MeshGroup& group = Groups.back();
                    NumFaces += group.NumFaces();

                    printf("  %5d verts  %5d polys", NumVerts(), NumFaces);
                    if (NumCoords()) printf("  %5d uvs", NumCoords());
                    if (NumColors()) printf("  %5d colors", NumColors());
                    printf("\n");
                }
            }
            return true;
        }
        return false;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    bool Mesh::SaveAsFBX(const string& meshPath) const noexcept
    {
        const int numVerts = NumVerts();
        if (!numVerts) {
            fprintf(stderr, "Warning: no vertices to export to '%s'\n", meshPath.c_str());
            return false;
        }
        if (!NumGroups()) {
            fprintf(stderr, "Warning: no mesh groups to export to '%s'\n", meshPath.c_str());
            return false;
        }

        InitFbxManager();
        FbxPtr<FbxExporter> exporter = FbxExporter::Create(SdkManager, "");
        //int format = SdkManager->GetIOPluginRegistry()->FindWriterIDByDescription("FBX 6.0 binary (*.fbx)");
        int format = -1;

        if (!exporter->Initialize(meshPath.c_str(), format, SdkManager->GetIOSettings())) {
            fprintf(stderr, "Failed to open file '%s' for writing: %s\n", meshPath.c_str(), exporter->GetStatus().GetErrorString());
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
            FbxMesh* mesh = FbxMesh::Create(scene.get(), "");
            mesh->InitControlPoints(numVerts);
            FbxVector4* points = mesh->GetControlPoints();
            const Vector3* verts = Verts.data();

            printf("SaveFBX %20s:  %5d verts  %5d polys", Name.c_str(), numVerts, NumFaces);

            for (int i = 0; i < numVerts; ++i) // indexing: enable AVX optimization
            {
                //points[i].mData[0] = verts[i].x;
                //points[i].mData[1] = verts[i].y;
                //points[i].mData[2] = verts[i].z;
                points[i].mData[0] = verts[i].x;
                points[i].mData[1] = -verts[i].y;
                points[i].mData[2] = -verts[i].z;
            }

            if (const int numNormals = NumNormals())
            {
                //printf("  %5d  normals\n", numNormals);

                assert((NormalsMapping == MapPerVertex || NormalsMapping == MapPerFaceVertex)
                    && "Only per-vertex or per-face-vertex normals are supported");

                FbxGeometryElementNormal* elementNormal = mesh->CreateElementNormal();
                elementNormal->SetMappingMode(toFbxMapping(NormalsMapping));
                elementNormal->SetReferenceMode(toFbxReference(NormalsMapping));

                auto& elements = elementNormal->GetDirectArray();
                const Vector3* normals = Normals.data();

                for (int i = 0; i < numNormals; ++i)
                {
                    elements.Add(FbxVector4{
                        normals[i].x,
                        -normals[i].y,
                        -normals[i].z
                    });
                }

                if (NormalsMapping == MapPerFaceVertex)
                {
                    auto& indices = elementNormal->GetIndexArray();
                    for (const MeshGroup& group : Groups)
                        for (const Face& face : group)
                            for (const VertexDescr& vd : face)
                                indices.Add(vd.n);
                }
            }

            if (const int numCoords = NumCoords())
            {
                printf("  %5d uvs", numCoords);

                assert((CoordsMapping == MapPerVertex || CoordsMapping == MapPerFaceVertex)
                    && "Only per-vertex or per-face-vertex UV coords are supported");

                FbxGeometryElementUV* elementUVs = mesh->CreateElementUV("DiffuseUV");
                elementUVs->SetMappingMode(toFbxMapping(CoordsMapping));
                elementUVs->SetReferenceMode(toFbxReference(CoordsMapping));

                auto& elements = elementUVs->GetDirectArray();
                const Vector2* uvs = Coords.data();

                for (int i = 0; i < numCoords; ++i)
                {
                    elements.Add(FbxVector2{ uvs[i].x, uvs[i].y });
                }

                if (CoordsMapping == MapPerFaceVertex)
                {
                    auto& indices = elementUVs->GetIndexArray();
                    for (const MeshGroup& group : Groups)
                        for (const Face& face : group)
                            for (const VertexDescr& vd : face)
                                indices.Add(vd.t);
                }
            }

            if (const int numColors = NumColors())
            {
                printf("  %5d colors", numColors);

                assert((ColorMapping == MapPerVertex || ColorMapping == MapPerFaceVertex) 
                    && "Only per-vertex or per-face-vertex colors are supported");

                FbxGeometryElementVertexColor* elementColors = mesh->CreateElementVertexColor();
                elementColors->SetMappingMode(toFbxMapping(ColorMapping));
                elementColors->SetReferenceMode(toFbxReference(ColorMapping));

                auto& elements = elementColors->GetDirectArray();
                const Vector3* colors = Colors.data();
                for (int i = 0; i < numColors; ++i)
                {
                    elements.Add(FbxColor{ colors[i].x, colors[i].y, colors[i].z });
                }

                if (ColorMapping == MapPerFaceVertex)
                {
                    auto& indices = elementColors->GetIndexArray();
                    for (const MeshGroup& group : Groups)
                        for (const Face& face : group)
                            for (const VertexDescr& vd : face)
                                indices.Add(vd.c);
                }
            }

            printf("\n");

            int groupId = 0;
            for (const MeshGroup& group : Groups)
            {
                //printf("  group %d  %5d  faces\n", groupId, group.NumFaces());

                for (const Face& face : group)
                {
                    mesh->BeginPolygon(-1, -1, -1, false);
                    for (const VertexDescr& vd : face)
                    {
                        mesh->AddPolygon(vd.v, -1);
                    }
                    mesh->EndPolygon();
                }
                ++groupId;
            }
            mesh->BuildMeshEdgeArray();

            FbxNode* node = FbxNode::Create(scene.get(), Groups.front().Name.c_str());
            node->SetNodeAttribute(mesh);
            root->AddChild(node);
        }

        if (!exporter->Export(scene.get())) {
            fprintf(stderr, "Failed to export FBX '%s': %s\n", meshPath.c_str(), exporter->GetStatus().GetErrorString());
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
