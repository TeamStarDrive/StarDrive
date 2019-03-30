#include "SdMesh.h"

namespace SdMesh
{
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

    DLLAPI(SDMesh*) SDMeshOpen(const wchar_t* fileName)
    {
        auto sdm = new SDMesh{ toString(fileName) };
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
    
    DLLAPI(SDMesh*) SDMeshCreateEmpty(const wchar_t* meshName)
    {
        auto* mesh = new SDMesh{};
        mesh->Data.Name = toString(meshName);
        mesh->Name = mesh->Data.Name;
        return mesh;
    }

    DLLAPI(bool) SDMeshSave(SDMesh* mesh, const wchar_t* fileName)
    {
        return mesh->Data.SaveAs(toString(fileName));
    }

    DLLAPI(SDMeshGroup*) SDMeshGetGroup(SDMesh* mesh, int groupId)
    {
        if (mesh && mesh->Data.IsValidGroup(groupId))
        {
            return mesh->Groups[groupId].get();
        }
        return nullptr;
    }

    DLLAPI(SDMeshGroup*) SDMeshNewGroup(SDMesh* mesh, const wchar_t* groupName, Matrix4* transform)
    {
        Nano::MeshGroup& group = mesh->Data.CreateGroup(toString(groupName));
        auto* g = mesh->Groups.emplace_back(std::make_unique<SDMeshGroup>(*mesh, group.GroupId)).get();
        mesh->NumGroups++;
        if (transform) g->Transform = *transform;
        return g;
    }

    ////////////////////////////////////////////////////////////////////////////////////
}
