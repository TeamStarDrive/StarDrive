using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ship_Game.Utils
{
    class ReadRestAPIFromSite
    {
        public Dictionary<string, string> FilesAvailable { get; private set; }
        readonly Array<UILabel> Versions = new Array<UILabel>();

        // ReSharper disable InconsistentNaming
        public class BitbucketRest
        {
            public string size   = "";
            public string limit  = "";
            public JArray values = null;
            public string start  = "";
            public string filter = "";
        }
        // ReSharper restore InconsistentNaming
        

        public void LoadContent(string url)
        {
            if (url.IsEmpty()) return;
            FilesAvailable = new Dictionary<string, string>();
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                request.AuthenticationLevel = System.Net.Security.AuthenticationLevel.None;

                var response = (HttpWebResponse)request.GetResponse();

                string content;
                using (Stream stream = response.GetResponseStream())
                {
                    using (var sr = new StreamReader(stream))
                        content = sr.ReadToEnd();
                }

                var jObject = JsonConvert.DeserializeObject<BitbucketRest>(content);

                foreach (JObject o in jObject.values)
                {
                    string name = o["name"].ToString();
                    int dotIndex = name.LastIndexOf('.');
                    name = name.Substring(0, dotIndex);
                    string link = o["links"]["self"]["href"].ToString();
                    FilesAvailable.Add(name, link);
                }
            }
            catch(Exception e)
            {
                Log.Error(e, $"Failing to communicate with website {url}");
                FilesAvailable = null;
            }
        }

        public Vector2 PopulateVersions(string versionText, Vector2 bodyTextStart)
        {
            var entries = CreateEntryList(versionText);
            foreach (var entry in entries)
            {
                Versions.Add(new UILabel(bodyTextStart, entry.EntryString, entry.Color));
                bodyTextStart.Y += 16;
            }
            return bodyTextStart;
        }

        public Entry[] CreateEntryList(string versionText)
        {
            string[] fileNames = FilesAvailable.Keys.ToArray();
            var entries        = new Entry[FilesAvailable.Keys.Count];
            int versionIndex   = CurrentVersionIndex(fileNames, versionText);

            for (int i = 0; i < fileNames.Length; i++) 
                entries[i] = new Entry(fileNames[i], versionIndex, i);
            return entries;
        }

        static int CurrentVersionIndex(string[] fileNames, string versionText)
        {
            for (int i = 0; i < fileNames.Length; i++)
            {
                string item = fileNames[i];
                if (item.Contains(versionText))
                    return i;
            }
            return int.MaxValue;
        }

        public void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            foreach (UILabel version in Versions)
            {
                version.Draw(batch, elapsed);
            }
        }
        public bool HandleInput(InputState input, string downLoadSite)
        {
            foreach (UILabel version in Versions)
            {
                if (!input.LeftMouseClick) continue;
                if (!version.HitTest(input.CursorPosition)) continue;
                foreach (var kv in FilesAvailable)
                {
                    if (version.Text.Text.Contains(kv.Key))
                    {
                        Log.OpenURL(downLoadSite);
                        Log.OpenURL(kv.Value);
                        return true;
                    }
                }
            }
            return false;
        }
    }
    public struct Entry
    {
        public Color Color;
        public string EntryString;
        public Age EntryAge;

        public Entry(string item, int versionIndex, int currentIndex)
        {
            EntryAge = SetAge(versionIndex, currentIndex);
            EntryString = FormatEntryString(item, EntryAge, out Color color);
            Color = color;
        }

        static Age SetAge(int versionIndex, int currentIndex)
        {
            if (versionIndex == currentIndex)
                return Age.Current;
            if (versionIndex > currentIndex)
                return Age.New;
            return Age.Old;
        }
        static string FormatEntryString(string item, Age age, out Color color)
        {
           switch (age)
            {
                case Age.Old:
                    color = Color.Gray;
                    return $"=Old= {item} ====";
                case Age.Current:
                    color = Color.Yellow;
                    return $"*==== {item} ====";
                case Age.New:
                    color = Color.White;
                    return $"=New= {item} ====";
                default:
                    throw new ArgumentOutOfRangeException(nameof(age), age, null);
            }
        }
        public enum Age
        {
            Old,
            Current,
            New
        }
    }
}
