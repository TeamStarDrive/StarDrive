#pragma once
#include <Nano/Mesh.h>

#ifndef DLLAPI
#  define DLLAPI(returnType) extern "C" __declspec(dllexport) returnType __stdcall
#endif

namespace SdMesh
{
    struct SDMesh;
    struct SDMeshGroup;

    using rpp::strview;
    using rpp::Color3;
    using std::shared_ptr;
    ////////////////////////////////////////////////////////////////////////////////////

    struct SDMaterial
    {
        // publicly visible in C#
        strview Name; // name of the material instance
        strview DiffusePath;
        strview AlphaPath;
        strview SpecularPath;
        strview NormalPath;
        strview EmissivePath;
        Color3 AmbientColor  = Color3::White();
        Color3 DiffuseColor  = Color3::White();
        Color3 SpecularColor = Color3::White();
        Color3 EmissiveColor = Color3::Black();
        float Specular = 1.0f;
        float Alpha    = 1.0f;

        // not mapped to C#
        shared_ptr<Nano::Material> Mat;

        explicit SDMaterial(const shared_ptr<Nano::Material>& mat) : Mat{mat}
        {
            if (!mat) return;
            Nano::Material& m = *mat;
            Name          = m.Name;
            DiffusePath   = m.DiffusePath;
            AlphaPath     = m.AlphaPath;
            SpecularPath  = m.SpecularPath;
            NormalPath    = m.NormalPath;
            EmissivePath  = m.EmissivePath;
            AmbientColor  = m.AmbientColor;
            DiffuseColor  = m.DiffuseColor;
            SpecularColor = m.SpecularColor;
            EmissiveColor = m.EmissiveColor;
            Specular      = m.Specular;
            Alpha         = m.Alpha;
        }
    };

    ////////////////////////////////////////////////////////////////////////////////////
    
    /**
     * Create a new material instance
     * This instance is stored inside SDMesh and is automatically freed during SDMeshClose
     */
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
            float alpha);

    DLLAPI(void) SDMeshGroupSetMaterial(SDMeshGroup* group, SDMaterial* material);

    ////////////////////////////////////////////////////////////////////////////////////
}
