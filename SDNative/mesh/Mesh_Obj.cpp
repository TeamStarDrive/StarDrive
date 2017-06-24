#include "Mesh.h"
#include <rpp/file_io.h>
#include <cassert>
#include <unordered_set>

namespace mesh
{
    ///////////////////////////////////////////////////////////////////////////////////////////////

    static bool SaveMaterials(const Mesh& mesh, const string& filename) noexcept
    {
        if (mesh.Groups.empty())
            return false;
        if ([&]{ for (const MeshGroup& group : mesh.Groups) if (group.Mat) return false; return true; }())
            return false;

        vector<Material*> written;
        shared_ptr<Material> defaultMat;
        auto getDefaultMat = [&]
        {
            if (defaultMat)
                return defaultMat;
            defaultMat = mesh.FindMaterial("default"_sv);
            if (!defaultMat) {
                defaultMat = make_shared<Material>();
                defaultMat->Name = "default"s;
            }
            return defaultMat;
        };

        if (file f = file{ filename, CREATENEW })
        {
            auto writeColor = [&](strview id, Color3 color) { f.writeln(id, color.r, color.g, color.b); };
            auto writeStr   = [&](strview id, strview str)  { if (str) f.writeln(id, str); };
            auto writeFloat = [&](strview id, float value)  { if (value != 1.0f) f.writeln(id, value); };

            f.writeln("#", filename, "MTL library");
            for (const MeshGroup& group : mesh.Groups)
            {
                Material& mat = *(group.Mat ? group.Mat : getDefaultMat()).get();
                if (contains(written, &mat))
                    continue; // skip
                written.push_back(&mat);

                f.writeln("newmtl", mat.Name);
                writeColor("Ka", mat.AmbientColor);
                writeColor("Kd", mat.DiffuseColor);
                writeColor("Ks", mat.SpecularColor);
                if (mat.EmissiveColor.notZero())
                    writeColor("Ke", mat.EmissiveColor);

                writeFloat("Ns", clamp(mat.Specular*1000.0f, 0.0f, 1000.0f)); // Ns is [0, 1000]
                writeFloat("d", mat.Alpha);

                writeStr("map_Kd",   mat.DiffusePath);
                writeStr("map_d",    mat.AlphaPath);
                writeStr("map_Ks",   mat.SpecularPath);
                writeStr("map_bump", mat.NormalPath);
                writeStr("map_Ke",   mat.EmissivePath);

                f.writeln("illum 2"); // default smooth shaded rendering
                f.writeln();
            }
            return true;
        }
        fprintf(stderr, "Failed to create file '%s'\n", filename.c_str());
        return false;
    }

    static vector<shared_ptr<Material>> LoadMaterials(strview matlibFile)
    {
        vector<shared_ptr<Material>> materials;

        if (auto parser = buffer_line_parser::from_file(matlibFile))
        {
            string matlibFolder = folder_path(matlibFile);
            Material* mat = nullptr;
            strview line;
            while (parser.read_line(line))
            {
                strview id = line.next(' ');
                if (id == "newmtl")
                {
                    materials.push_back(make_shared<Material>());
                    mat = materials.back().get();
                    mat->Name         = line.trim();
                    mat->MaterialFile = matlibFile;
                }
                else if (mat)
                {
                    if      (id == "Ka") mat->AmbientColor  = Color3::parseColor(line);
                    else if (id == "Kd") mat->DiffuseColor  = Color3::parseColor(line);
                    else if (id == "Ks") mat->SpecularColor = Color3::parseColor(line);
                    else if (id == "Ke") mat->EmissiveColor = Color3::parseColor(line);
                    else if (id == "Ns") mat->Specular = line.to_float() / 1000.0f; // Ns is [0, 1000], normalize to [0, 1]
                    else if (id == "d")  mat->Alpha    = line.to_float();
                    else if (id == "Tr") mat->Alpha    = 1.0f - line.to_float();
                    else if (id == "map_Kd")   mat->DiffusePath  = matlibFolder + line.next(' ');
                    else if (id == "map_d")    mat->AlphaPath    = matlibFolder + line.next(' ');
                    else if (id == "map_Ks")   mat->SpecularPath = matlibFolder + line.next(' ');
                    else if (id == "map_bump") mat->NormalPath   = matlibFolder + line.next(' ');
                    else if (id == "map_Ke")   mat->EmissivePath = matlibFolder + line.next(' ');
                }
            }
        }
        return materials;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    static constexpr size_t MaxStackAlloc = 1024 * 1024;

    struct ObjLoader
    {
        Mesh& mesh;
        strview meshPath;
        buffer_line_parser parser;
        size_t numVerts = 0, numCoords = 0, numNormals = 0, numColors = 0, numFaces = 0;
        vector<shared_ptr<Material>> materials;
        MeshGroup* group = nullptr;
        bool triedDefaultMat = false;

        Vector3* vertsData   = nullptr;
        Vector2* coordsData  = nullptr;
        Vector3* normalsData = nullptr;
        Color3*  colorsData  = nullptr;

        void* dataBuffer = nullptr;
        size_t bufferSize = 0;

        explicit ObjLoader(Mesh& mesh, strview meshPath)
            : mesh{ mesh }, meshPath{ meshPath }, parser{ buffer_line_parser::from_file(meshPath) }
        {
        }

        ~ObjLoader()
        {
            if (bufferSize > MaxStackAlloc) free(dataBuffer);
        }

        bool ProbeStats()
        {
            strview line;
            while (parser.read_line(line))
            {
                char c = line[0];
                if (c == 'v') switch (line[1])
                {
                    case ' ': ++numVerts;   break;
                    case 'n': ++numNormals; break;
                    case 't': ++numCoords;  break;
                    default:;
                }
                else if (c == 'f' && line[1] == ' ')
                {
                    ++numFaces;
                }
            }

            parser.reset();
            if (numVerts == 0) {
                fprintf(stderr, "Mesh::LoadOBJ() failed: No vertices in %s\n", meshPath.c_str());
                return false;
            }

            // megaBuffer strategy - one big allocation instead of a dozen small ones
            bufferSize = numVerts    * sizeof(Vector3)
                        + numCoords  * sizeof(Vector2)
                        + numNormals * sizeof(Vector3)
                        + numVerts   * sizeof(Color3);
            return true;
        }

        struct pool_helper {
            void* ptr;
            template<class T> T* next(size_t count) {
                T* data = (T*)ptr;
                ptr = data + count;
                return data;
            }
        };

        void InitPointers(void* allocated)
        {
            dataBuffer = allocated;
            pool_helper pool = { (byte*) allocated };
            vertsData   = pool.next<Vector3>(numVerts);
            coordsData  = pool.next<Vector2>(numCoords);
            normalsData = pool.next<Vector3>(numNormals);
            colorsData  = pool.next<Color3>(numVerts);
        }

        shared_ptr<Material> FindMat(strview matName)
        {
            if (materials.empty() && !triedDefaultMat) {
                triedDefaultMat = true;
                string defaultMat = file_replace_ext(meshPath, "mtl");
                materials = LoadMaterials(defaultMat);
            }
            for (auto& mat : materials)
                if (matName.equalsi(mat->Name))
                    return mat;
            return {};
        }

        MeshGroup* CurrentGroup()
        {
            return group ? group : (group = &mesh.CreateGroup({}));
        }

        void ParseMeshData()
        {
            int vertexId = 0, coordId = 0, normalId = 0, colorId = 0;

            strview line;
            while (parser.read_line(line)) // for each line
            {
                char c = line[0];
                if (c == 'v')
                {
                    c = line[1];
                    if (c == ' ') { // v 1.0 1.0 1.0
                        line.skip(2); // skip 'v '
                        Vector3& v = vertsData[vertexId];
                        line >> v.x >> v.y >> v.z;

                        if (!line.empty())
                        {
                            Vector3 col;
                            line >> col.x >> col.y >> col.z;
                            if (col.sqlength() > 0.001f)
                            {
                                // for OBJ we always use Per-Vertex color mapping...
                                // there is simply no other standardised way to do it
                                if (colorId == 0) {
                                    memset(colorsData, 0, numVerts*sizeof(Color3));
                                    numColors = numVerts;
                                }
                                ++colorId;
                                colorsData[vertexId] = col;
                            }
                        }
                        ++vertexId;
                        continue;
                    }
                    if (c == 'n') { // vn 1.0 1.0 1.0
                        line.skip(3); // skip 'vn '
                        Vector3& n = normalsData[normalId++];
                        line >> n.x >> n.y >> n.z;
                        // Use this if exporting for Direct3D
                        //n.z = -n.z; // invert Z to convert to lhs coordinates
                        continue;
                    }
                    if (c == 't') { // vt 1.0 1.0
                        line.skip(3); // skip 'vt '
                        Vector2& uv = coordsData[coordId++];
                        line >> uv.x >> uv.y;
                        //if (fmt == TXC_Direct3DTexCoords) // Use this if exporting for Direct3D
                        //    c.y = 1.0f - c.y; // invert the V coord to convert to lhs coordinates
                        continue;
                    }
                }
                else if (c == 'f')
                {
                    // f Vertex1/Texture1/Normal1 Vertex2/Texture2/Normal2 Vertex3/Texture3/Normal3
                    auto& faces = CurrentGroup()->Faces;
                    Face* f = &emplace_back(faces);

                    // load the face indices
                    line.skip(2); // skip 'f '

                    while (strview vertdescr = line.next(' '))
                    {
                        // when encountering quads or large polygons, we need to triangulate the mesh
                        // by tracking the first vertex descr and forming a fan; this requires convex polys
                        if (f->Count == 3)
                        {
                            // @note According to OBJ spec, face vertices are in CCW order:
                            // 0--3
                            // |\ |
                            // | \|
                            // 1--2

                            // v[0], v[2], v[3]
                            VertexDescr* vd0 = &f->VDS[0];
                            VertexDescr* vd2 = &f->VDS[2];
                            f = &emplace_back(faces);
                            f->VDS[f->Count++] = *vd0;
                            f->VDS[f->Count++] = *vd2;
                            // v[3] is parsed below:
                        }
                        VertexDescr& vd = f->VDS[f->Count++];
                        if (strview v = vertdescr.next('/')) {
                            vd.v = v.to_int() - 1;
                            if (vd.v < 0) vd.v = numVerts + vd.v; // negative indexing mode (3ds Max exporter)
                        }
                        if (strview t = vertdescr.next('/')) {
                            vd.t = t.to_int() - 1;
                            if (vd.t < 0) vd.t = numCoords + vd.t;
                        }
                        if (strview n = vertdescr) {
                            vd.n = n.to_int() - 1;
                            if (vd.n < 0) vd.n = numNormals + vd.n;
                        }
                    }
                }
                //else if (c == 's')
                //{
                //    line.skip(2); // skip "s "
                //    line >> group->SmoothingGroup;
                //}
                else if (c == 'u' && memcmp(line.str, "usemtl", 6) == 0)
                {
                    line.skip(7); // skip "usemtl "
                    strview matName = line.next(' ');
                    CurrentGroup()->Mat = FindMat(matName);
                }
                else if (c == 'm' && memcmp(line.str, "mtllib", 6) == 0)
                {
                    line.skip(7); // skip "mtllib "
                    strview matlib = line.next(' ');
                    string matlibPath = path_combine(folder_path(meshPath), matlib);
                    materials = LoadMaterials(matlibPath);
                }
                else if (c == 'g')
                {
                    line.skip(2); // skip "g "
                    group = &mesh.FindOrCreateGroup(line.next(' '));
                }
                else if (c == 'o')
                {
                    line.skip(2); // skip "o "
                    mesh.Name = (string)line.next(' ');
                }
            }
        }

        void RemoveEmptyGroups() const
        {
            for (auto it = mesh.Groups.begin(); it != mesh.Groups.end();)
                if (it->IsEmpty()) it = mesh.Groups.erase(it); else ++it;
        }

        void BuildMeshGroups()
        {
            for (MeshGroup& g : mesh.Groups)
            {
                mesh.NumFaces += g.NumFaces();

                int vertsMin   = numVerts,   vertsMax   = 0;
                int coordsMin  = numCoords,  coordsMax  = 0;
                int normalsMin = numNormals, normalsMax = 0;
                for (Face& face : g.Faces)
                {
                    for (VertexDescr& vd : face) 
                    {
                        if (vd.v < vertsMin) vertsMin = vd.v;
                        if (vd.v > vertsMax) vertsMax = vd.v;
                        if (vd.t != -1) {
                            if (vd.t < coordsMin) coordsMin = vd.t;
                            if (vd.t > coordsMax) coordsMax = vd.t;
                        }
                        if (vd.n != -1) {
                            if (vd.n < normalsMin) normalsMin = vd.n;
                            if (vd.n > normalsMax) normalsMax = vd.n;
                        }
                    }
                }

                auto copyElements = [](auto& dst, auto* src, int min, int max) {
                    if (max >= min) dst.assign(src + min, src + max + 1);
                };
                copyElements(g.Verts,   vertsData,   vertsMin,   vertsMax);
                copyElements(g.Coords,  coordsData,  coordsMin,  coordsMax);
                copyElements(g.Normals, normalsData, normalsMin, normalsMax);

                const bool vertexColors = numColors > 0;
                if (vertexColors) {
                    copyElements(g.Colors, colorsData, vertsMin, vertsMax);
                    g.ColorMapping = MapPerVertex;
                }

                if (g.Coords.size() == g.Verts.size())
                    g.CoordsMapping = MapPerVertex;

                if (g.Normals.size() == g.Verts.size())
                    g.NormalsMapping = MapPerVertex;

                for (Face& face : g.Faces) // now assign new ids
                {
                    for (VertexDescr& vd : face) 
                    {
                        vd.v -= vertsMin;
                        if (vd.t != -1) vd.t -= coordsMin;
                        if (vd.n != -1) vd.n -= normalsMin;
                        if (vertexColors) vd.c = vd.v;
                    }
                }
            }
        }
    };

    bool Mesh::LoadOBJ(strview meshPath) noexcept
    {
        Clear();

        ObjLoader loader { *this, meshPath };

        if (!loader.parser) {
            println(stderr, "Failed to open file:", meshPath);
            return false;
        }

        if (!loader.ProbeStats()) {
            println(stderr, "Mesh::LoadOBJ() failed! No vertices in:", meshPath);
            return false;
        }

        // OBJ maps vertex data globally, not per-mesh-group like most game engines expect
        // so this really complicates things when we build the mesh groups...
        // to speed up mesh loading, we use very heavy stack allocation

        // ObjLoader will free these in destructor if malloc was used
        // ReSharper disable once CppNonReclaimedResourceAcquisition
        void* mem = loader.bufferSize <= MaxStackAlloc
                    ? alloca(loader.bufferSize)
                    : malloc(loader.bufferSize);
        loader.InitPointers(mem);
        loader.ParseMeshData();
        loader.RemoveEmptyGroups();
        loader.BuildMeshGroups();

        return true;
    }


    ///////////////////////////////////////////////////////////////////////////////////////////////

    static vector<Vector3> FlattenColors(const MeshGroup& group)
    {
        vector<Vector3> colors = { group.Verts.size(), Vector3::ZERO };

        for (const Face& face : group)
            for (const VertexDescr& vd : face)
            {
                if (vd.c == -1) continue;
                Vector3& dst = colors[vd.v];
                if (dst == Vector3::ZERO || dst == Vector3::ONE)
                    dst = group.Colors[vd.c];
            }
        return colors;
    }

    bool Mesh::SaveAsOBJ(strview meshPath) const noexcept
    {
        if (file f = file{ meshPath, CREATENEW })
        {
            // straight to file, #dontcare about perf atm

            string matlib = file_replace_ext(meshPath, "mtl");
            if (SaveMaterials(*this, file_replace_ext(meshPath, "mtl")))
                f.writeln("mtllib", matlib);

            if (!Name.empty())
                f.writeln("o", Name);

            string buf;
            for (int group = 0; group < (int)Groups.size(); ++group)
            {
                const MeshGroup& g = Groups[group];
                if (!g.Name.empty()) f.writeln("g", g.Name);
                if (g.Mat)           f.writeln("usemtl", g.Mat->Name);
                f.writeln("s", group);

                auto* vertsData = g.Verts.data();
                if (g.Colors.empty())
                {
                    for (const Vector3& v : g.Verts)
                        f.writef("v %.6f %.6f %.6f\n", v.x, v.y, v.z);
                }
                else // non-standard extension for OBJ vertex colors
                {
                    // @todo Just leave a warning and export incorrect vertex colors?
                    assert((g.ColorMapping == MapPerVertex || g.ColorMapping == MapPerFaceVertex) 
                        && "OBJ export only supports per-vertex and per-face-vertex color mapping!");
                    assert(g.NumColors() >= g.NumVerts());

                    auto& colors = g.ColorMapping == MapPerFaceVertex ? FlattenColors(g) : g.Colors;
                    auto* colorsData = colors.data();

                    const int numVerts = g.NumVerts();
                    for (int i = 0; i < numVerts; ++i)
                    {
                        const Vector3& v = vertsData[i];
                        const Vector3& c = colorsData[i];
                        if (c == Vector3::ZERO) f.writef("v %.6f %.6f %.6f\n", v.x, v.y, v.z);
                        else f.writef("v %.6f %.6f %.6f %.6f %.6f %.6f\n", v.x, v.y, v.z, c.x, c.y, c.z);
                    }
                }

                for (const Vector2& v : g.Coords)  f.writef("vt %.4f %.4f\n", v.x, v.y);
                for (const Vector3& v : g.Normals) f.writef("vn %.4f %.4f %.4f\n", v.x, v.y, v.z);

                for (const Face& face : g.Faces)
                {
                    buf.clear();
                    buf += 'f';
                    for (const VertexDescr& vd : face)
                    {
                        buf += ' ', buf += to_string(vd.v + 1);
                        if (vd.t != -1) buf += '/', buf += to_string(vd.t + 1);
                        if (vd.n != -1) buf += '/', buf += to_string(vd.n + 1);
                    }
                    buf += '\n';
                    f.write(buf);
                }
            }
            for (const MeshGroup& g : Groups)
            {

            }
            return true;
        }
        println(stderr, "Failed to create file: ", meshPath);
        return false;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
}
