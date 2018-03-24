using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using Ship_Game.Utils;

namespace Ship_Game.GameScreens.MainMenu
{
    internal class VersionChecking : PopupWindow
    {        
        ReadRestAPI ReadRest;
        Array<UILabel> Versions;
        string URL = "https://api.bitbucket.org/2.0/repositories/CrunchyGremlin/sd-blackbox/downloads";
        string DownLoadSite = "https://bitbucket.org/CrunchyGremlin/sd-blackbox/downloads/";
        public VersionChecking(GameScreen parent, int width, int height) : base(parent, width, height)
        {
            IsPopup = true;
            ReadRest = new ReadRestAPI();
            Versions = new Array<UILabel>();
        }
        public VersionChecking(GameScreen parent) : this(parent, 500, 600)
        {
            
        }
        public override void LoadContent()
        {            
            TitleText = "Version Check";
            var verMod = $"Vanilla";
            var versionText = GlobalStats.Version;
            var mod = GlobalStats.ActiveMod;
            if (mod?.mi.BitbucketAPIString != null)
            {
                verMod = $"{mod.ModName} - {mod.Version}";
                URL = mod.mi.BitbucketAPIString ?? URL;
                versionText = mod.Version ?? versionText;
                DownLoadSite = mod.mi.DownLoadSite ?? DownLoadSite;
            }

            MiddleText = $"{GlobalStats.ExtendedVersion}\nMod: {verMod}";
            base.LoadContent();            
            ReadRest.LoadContent(URL);
            if (ReadRest.filesAndLinks == null)
            {
                ExitScreen();
                return;
            }
            
            string[] array = ReadRest.filesAndLinks.Keys.ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                var preText = "====";
                var item = array[i];
                var color = Color.White;
                if (item.Contains(versionText))
                {
                    color = Color.Yellow;
                    preText = "*===";
                    if (i > 0)
                        color = Color.Red;
                }
                var text = new UILabel(this, BodyTextStart, $"{preText} {item} ====",color);
                Versions.Add(text);

                BodyTextStart.Y += 16;
            }


        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (ReadRest.filesAndLinks == null)
            {
                ExitScreen();
                return;
            }
            base.Draw(spriteBatch);
            spriteBatch.Begin();            
            foreach(var item in Versions)
            {
                item.Draw(spriteBatch);
            }

            spriteBatch.End();
            
        }

        public override bool HandleInput(InputState input)
        {
            if (input.Escaped)
            {
                ExitScreen();
                return true;
            }

            foreach(var version in Versions)
            {
                if (!input.LeftMouseClick) continue;
                if (!version.HitTest(input.CursorPosition)) continue;
                foreach(var kv in ReadRest.filesAndLinks)
                {
                    if (!version.Text.Contains(kv.Key)) continue;                    
                    Log.OpenURL(DownLoadSite);
                    Log.OpenURL(kv.Value);
                }
            }

            return base.HandleInput(input);
        }

    }
}