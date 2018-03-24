using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ship_Game.Utils
{
    class ReadRestAPI
    {
        public JArray WebData;
        public Array<string> Names;
        public Array<string> Links;
        public Dictionary<string, string> filesAndLinks;
        public ReadRestAPI()
        {
        }

        public class BitbucketRest
        {
            public string size;
            public string limit;
            public bool isLastPage;
            public JArray values;
            public string start;
            public string filter;
            public string nextPageStart;                        

        }

        public class BitbucketRestData
        {            
            public string name;
            public JObject links;
            public int downloads;
            public string created_on;
            public JObject user;
            public string type;
            public int size;

        }
        public class LinkData
        {
            public string Name;
            public string Link;
        }
        

        public void LoadContent(string url = "https://api.bitbucket.org/2.0/repositories/CrunchyGremlin/sd-blackbox/downloads")
        {
            Names = new Array<string>();
            Links = new Array<string>();
            filesAndLinks = new Dictionary<string, string>();
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
                    filesAndLinks.Add(name, link);

                }
            }
            catch(Exception e)
            {
                Log.Error(e, $"Failing to communicate with website {url}");
                filesAndLinks = null;
            }



        }
    }
}
