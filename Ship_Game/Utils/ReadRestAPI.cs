﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ship_Game.Utils
{
    class ReadRestAPI
    {
        public JArray WebData;
        public Array<string> Names;
        public Array<string> Links;
        public Dictionary<string, string> FilesAndLinks;
        private readonly Array<UILabel> Versions = new Array<UILabel>();

        public ReadRestAPI()
        {
        }

        // ReSharper disable InconsistentNaming
        public class BitbucketRest
        {
            public string size = "";
            public string limit = "";
            public bool isLastPage = false;
            public JArray values = null;
            public string start = "";
            public string filter = "";
            public string nextPageStart = "";
        }
        // ReSharper restore InconsistentNaming
        

        public void LoadContent(string url)
        {
            if (url.IsEmpty()) return;
            Names = new Array<string>();
            Links = new Array<string>();
            FilesAndLinks = new Dictionary<string, string>();
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create
                    (url);

                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 58.0.3029.110 Safari / 537.36";
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                string content = string.Empty;
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        content = sr.ReadToEnd();
                    }
                }
                BitbucketRest jObject = JsonConvert.DeserializeObject<BitbucketRest>(content);

                WebData = jObject.values;

                foreach (JObject o in jObject.values)
                {
                    var name = o["name"].ToString();
                    int dotIndex = name.LastIndexOf('.');
                    name = name.Substring(0, dotIndex);
                    var link = o["links"]["self"]["href"].ToString();
                    FilesAndLinks.Add(name, link);

                }
            }
            catch(Exception e)
            {
                Log.Error(e, $"Failing to communicate with website {url}");
                FilesAndLinks = null;
            }



        }

        public Vector2 PopulateVersions(string versionText, GameScreen screen, Vector2 bodyTextStart)
        {
            string[] array = FilesAndLinks.Keys.ToArray();
            bool old = false;
            for (int i = 0; i < array.Length; i++)
            {
                var preText = "====";
                var item = array[i];
                var color = !old ? Color.White : Color.Gray;
                if (item.Contains(versionText))
                {
                    color = Color.Yellow;
                    preText = "*===";
                    if (i > 0)
                        color = Color.Red;
                    old = true;
                }
                var text = new UILabel(screen, bodyTextStart, $"{preText} {item} ====", color);
                Versions.Add(text);

                bodyTextStart.Y += 16;
            }
            return bodyTextStart;
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var item in Versions)
            {
                item.Draw(spriteBatch);
            }
        }
        public bool HandleInput(InputState input, string downLoadSite)
        {
            foreach (var version in Versions)
            {
                if (!input.LeftMouseClick) continue;
                if (!version.HitTest(input.CursorPosition)) continue;
                foreach (var kv in FilesAndLinks)
                {
                    if (!version.Text.Contains(kv.Key)) continue;
                    Log.OpenURL(downLoadSite);
                    Log.OpenURL(kv.Value);
                    return true;
                }
            }
            return false;
        }
    }
}
