using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.SpriteSystem
{
    public partial class TextureAtlas
    {
        static TextureInfo[] CreateTextureInfos(AtlasPath path, FileInfo[] textureFiles)
        {
            var textures = new TextureInfo[textureFiles.Length];

            bool noPackAll = ResourceManager.AtlasExcludeFolder.Contains(path.OriginalName);
            HashSet<string> ignore = ResourceManager.AtlasExcludeTextures; // HACK

            for (int i = 0; i < textureFiles.Length; ++i)
            {
                FileInfo info = textureFiles[i];
                string texName = info.NameNoExt();
                string ext = info.Extension.Substring(1);
                Texture2D tex = ResourceManager.RootContent.LoadUncachedTexture(info, ext);
                bool noPack = noPackAll || ignore.Contains(texName);
                textures[i] = new TextureInfo
                {
                    Name    = texName,
                    Type    = ext,
                    Width   = tex.Width,
                    Height  = tex.Height,
                    Texture = tex,
                    NoPack  = noPack,
                };
            }
            return textures;
        }

        static FileInfo[] GatherUniqueTextures(string folder)
        {
            FileInfo[] textureFiles = ResourceManager.GatherTextureFiles(folder, recursive: false);
            var uniqueTextures = new Map<string, FileInfo>();
            foreach (FileInfo info in textureFiles)
            {
                string texName = info.NameNoExt();
                if (uniqueTextures.TryGetValue(texName, out FileInfo existing))
                {
                    if (existing.Extension == "xnb") // only replace if old was xnb
                        uniqueTextures[texName] = info;
                }
                else uniqueTextures.Add(texName, info);
            }
            return uniqueTextures.Values.ToArray();
        }

        static ulong CreateHash(FileInfo[] textures)
        {
            // @note Had to roll back to a custom Fnv1AHash over text,
            //       since typical int hash-combine gave bad results.
            var ms = new MemoryStream(4096);
            var bw = new BinaryWriter(ms);
            bw.Write(textures.Length);
            bw.Write(Version);
            foreach (FileInfo info in textures)
            {
                bw.Write(info.Name);
                bw.Write(info.Length);
                bw.Write(info.LastWriteTimeUtc.Ticks);
            }
            return Fnv1AHash(ms.ToArray());
        }

        static ulong Fnv1AHash(byte[] bytes)
        {
            ulong hash = 0xcbf29ce484222325;
            foreach (byte b in bytes)
            {
                hash = hash ^ b;
                hash = hash * 0x100000001b3;
            }
            return hash;
        }
    }
}