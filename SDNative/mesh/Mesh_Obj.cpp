#include "Mesh.h"
#include <rpp/file_io.h>
#include <cassert>
#include <unordered_set>

namespace mesh
{
    ///////////////////////////////////////////////////////////////////////////////////////////////

    shared_ptr<Material> DefaultMaterial()
    {
        shared_ptr<Material> defaultMat = make_shared<Material>();
        return defaultMat;
    }

    static bool SaveMaterials(const Mesh& mesh, const string& filename) noexcept
    {
        vector<Material*> written;
        shared_ptr<Material> defaultMat;
        auto getDefaultMat = [&] {
            return defaultMat ? defaultMat : (defaultMat = DefaultMaterial());
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

    static vector<shared_ptr<Material>> LoadMaterials(const string& matlibFile)
    {
        vector<shared_ptr<Material>> materials;

        if (auto parser = buffer_line_parser::from_file(matlibFile))
        {
            Material* mat = nullptr;
            strview line;
            while (parser.read_line(line))
            {
                strview id = line.next(' ');
                if (id == "newmtl")
                {
                    materials.push_back(make_shared<Material>());
                    mat = materials.back().get();
                    mat->Name = (string)line.trim();
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
                    else if (id == "map_Kd")   mat->DiffusePath  = line.next(' ');
                    else if (id == "map_d")    mat->AlphaPath    = line.next(' ');
                    else if (id == "map_Ks")   mat->SpecularPath = line.next(' ');
                    else if (id == "map_bump") mat->NormalPath   = line.next(' ');
                    else if (id == "map_Ke")   mat->EmissivePath = line.next(' ');
                }
            }
        }
        return materials;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    bool Mesh::LoadOBJ(const string& meshPath) noexcept
    {
        Clear();

        load_buffer filebuf = file::read_all(meshPath);
        if (filebuf.len == 0) {
            fprintf(stderr, "Failed to open file '%s'\n", meshPath.c_str());
            return false;
        }

        int numVerts = 0;
        int numCoords = 0;
        int numNormals = 0;
        int numFaces = 0;

        strview line;
        line_parser parser = strview(filebuf);
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

        if (numVerts == 0) {
            fprintf(stderr, "Mesh::LoadOBJ() failed: No vertices in %s\n", meshPath.c_str());
            return true;
        }

        vector<shared_ptr<Material>> materials;
        auto findMat = [&](strview matName) {
            for (auto& mat : materials)
                if (matName.equalsi(mat->Name))
                    return mat;
            return shared_ptr<Material>{};
        };

        Verts.reserve(numVerts);
        Coords.reserve(numCoords);
        Normals.reserve(numNormals);

        MeshGroup* group = &emplace_back(Groups);
        group->Faces.reserve(numFaces);

        // indices for the arrays
        parser = strview(filebuf);
        while (parser.read_line(line)) // for each line
        {
            char c = line[0];
            if (c == 'v')
            {
                c = line[1];
                if (c == ' ') { // v 1.0 1.0 1.0
                    line.skip(2); // skip 'v '
                    Vector3& v = emplace_back(Verts);
                    line >> v.x >> v.y >> v.z;

                    if (!line.empty())
                    {
                        Vector3 col;
                        line >> col.x >> col.y >> col.z;
                        if (col.sqlength() > 0.001f)
                        {
                            // for OBJ we always use Per-Vertex color mapping
                            if (Colors.empty())
                                Colors.resize(numVerts);

                            Colors[Verts.size() - 1] = col;
                        }
                    }
                    // Use this if exporting for Direct3D
                    //v.z = -v.z; // invert Z to convert to lhs coordinates
                    continue;
                }
                if (c == 'n') { // vn 1.0 1.0 1.0
                    line.skip(3); // skip 'vn '
                    Vector3& n = emplace_back(Normals);
                    line >> n.x >> n.y >> n.z;
                    // Use this if exporting for Direct3D
                    //n.z = -n.z; // invert Z to convert to lhs coordinates
                    continue;
                }
                if (c == 't') { // vt 1.0 1.0
                    line.skip(3); // skip 'vt '
                    Vector2& uv = emplace_back(Coords);
                    line >> uv.x >> uv.y;
                    //if (fmt == TXC_Direct3DTexCoords) // Use this if exporting for Direct3D
                    //    c.y = 1.0f - c.y; // invert the V coord to convert to lhs coordinates
                    continue;
                }
            }
            else if (c == 'f')
            {
                // f Vertex1/Texture1/Normal1 Vertex2/Texture2/Normal2 Vertex3/Texture3/Normal3
                Face& f = emplace_back(group->Faces);

                // load the face indices
                line.skip(2); // skip 'f '

                while (strview vertdescr = line.next(' '))
                {
                    VertexDescr& vd = f.VDS[f.Count++];
                    if (strview v = vertdescr.next('/')) vd.v = v.to_int() - 1;
                    if (strview t = vertdescr.next('/')) vd.t = t.to_int() - 1;
                    if (strview n = vertdescr)           vd.n = n.to_int() - 1;
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
                group->Mat = findMat(matName);
            }
            else if (c == 'm' && memcmp(line.str, "mtllib", 6) == 0)
            {
                line.skip(7); // skip "mtllib "
                strview matLib = line.next(' ');
                materials = LoadMaterials(matLib);
            }
            else if (c == 'g')
            {
                line.skip(2); // skip "g "
                group->Name = (string)line.next(' ');
            }
            else if (c == 'o')
            {
                line.skip(2); // skip "o "
                Name = (string)line.next(' ');
            }
        }

        for (auto& g : Groups)
            NumFaces += g.NumFaces();

        if (Colors.size() == Verts.size())
        {
            // assign per-vertex color ID-s
            for (auto& g : Groups)
                for (auto& face : g)
                    for (auto& vd : face)
                        vd.c = vd.v;
        }
        return true;
    }


    ///////////////////////////////////////////////////////////////////////////////////////////////

    static vector<Vector3> FlattenColors(const Mesh& mesh)
    {
        vector<Vector3> colors = { mesh.Verts.size(), Vector3::ZERO };

        for (const MeshGroup& group : mesh.Groups)
            for (const Face& face : group)
                for (const VertexDescr& vd : face)
                {
                    if (vd.c == -1) continue;
                    Vector3& dst = colors[vd.v];
                    if (dst == Vector3::ZERO || dst == Vector3::ONE)
                        dst = mesh.Colors[vd.c];
                }
        return colors;
    }

    bool Mesh::SaveAsOBJ(const string& meshPath) const noexcept
    {
        if (file f = file{ meshPath, CREATENEW })
        {
            // straight to file, #dontcare about perf atm
            //if (!MatLib.empty()) f.writef("mtllib %s\n", MatLib.c_str());
            if (!Name.empty())   f.writef("o %s\n", Name.c_str());

            if (Colors.empty())
            {
                for (const Vector3& v : Verts)
                    f.writef("v %.6f %.6f %.6f\n", v.x, v.y, v.z);
            }
            else // non-standard extension for OBJ vertex colors
            {
                // @todo Just leave a warning and export incorrect vertex colors?
                assert((ColorMapping == MapPerVertex || ColorMapping == MapPerFaceVertex) 
                    && "OBJ export only supports per-vertex and per-face-vertex color mapping!");
                assert(NumColors() >= NumVerts());

                auto& colors = ColorMapping == MapPerFaceVertex ? FlattenColors(*this) : Colors;

                const int numVerts = NumVerts();
                for (int i = 0; i < numVerts; ++i)
                {
                    const Vector3& v = Verts[i];
                    const Vector3& c = colors[i];
                    if (c == Vector3::ZERO) f.writef("v %.6f %.6f %.6f\n", v.x, v.y, v.z);
                    else f.writef("v %.6f %.6f %.6f %.6f %.6f %.6f\n", v.x, v.y, v.z, c.x, c.y, c.z);
                }
            }

            for (const Vector2& v : Coords)  f.writef("vt %.4f %.4f\n", v.x, v.y);
            for (const Vector3& v : Normals) f.writef("vn %.4f %.4f %.4f\n", v.x, v.y, v.z);

            string buf;
            for (int group = 0; group < (int)Groups.size(); ++group)
            {
                const MeshGroup& g = Groups[group];
                if (!g.Name.empty()) f.writeln("g", g.Name);
                if (g.Mat)           f.writeln("usemtl", g.Mat->Name);
                f.writeln("s", group);

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
        fprintf(stderr, "Failed to create file '%s'\n", meshPath.c_str());
        return false;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////
}
