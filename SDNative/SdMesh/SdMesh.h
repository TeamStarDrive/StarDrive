#pragma once
#include <Nano/Mesh.h>
#include "SdMeshGroup.h"
#include "SdAnimation.h"

namespace SdMesh
{
    using rpp::strview;
    using std::unique_ptr;
    using std::string;
    ////////////////////////////////////////////////////////////////////////////////////

    static std::string toString(const wchar_t* wideStr)
    {
        return { wideStr, wideStr + wcslen(wideStr) };
    }

    struct SDMesh
    {
        // publicly visible in C#
        strview Name  = "";
        int NumGroups = 0;
        int NumFaces  = 0;
        int NumMaterials = 0;

        int NumModelBones = 0;
        int NumSkinnedBones = 0;
        int NumAnimClips = 0;

        // not mapped to C#
        Nano::Mesh TheMesh;
        vector<unique_ptr<SDAnimationClip>> Clips;
        vector<unique_ptr<SDMeshGroup>> Groups;
        vector<unique_ptr<SDMaterial>> Materials;

        SDMesh();
        explicit SDMesh(strview path);
        SDMaterial* GetOrCreateMat(const shared_ptr<Nano::Material>& mat);

        void SyncStats();
    };

    ////////////////////////////////////////////////////////////////////////////////////

    DLLAPI(SDMesh*) SDMeshOpen(const wchar_t* fileName);
    DLLAPI(void)    SDMeshClose(SDMesh* mesh);
    
    DLLAPI(SDMesh*) SDMeshCreateEmpty(const wchar_t* meshName);
    DLLAPI(bool)    SDMeshSave(SDMesh* mesh, const wchar_t* fileName);

    DLLAPI(SDMeshGroup*) SDMeshGetGroup(SDMesh* mesh, int groupId);
    DLLAPI(SDMeshGroup*) SDMeshNewGroup(SDMesh* mesh, const wchar_t* groupName, Matrix4* transform);

    ////////////////////////////////////////////////////////////////////////////////////
}