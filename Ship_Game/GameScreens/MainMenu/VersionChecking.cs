using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Utils;

namespace Ship_Game.GameScreens.MainMenu
{
    internal class VersionChecking : PopupWindow
    {        
        ReadRestAPI BBVersionCheck;
        ReadRestAPI ModVersionCheck;
        UILabel BBListHeader;
        UILabel ModListHeader;
        //Array<UILabel> Versions;
        string URL = "http://api.bitbucket.org/2.0/repositories/CrunchyGremlin/sd-blackbox/downloads";
        string ModURL = "";
        string DownLoadSite = "http://bitbucket.org/CrunchyGremlin/sd-blackbox/downloads/";
        string ModDownLoadSite = "";
        public VersionChecking(GameScreen parent, int width, int height) : base(parent, width, height)
        {
            IsPopup = true;
            BBVersionCheck = new ReadRestAPI();
            ModVersionCheck = new ReadRestAPI();
        }
        public VersionChecking(GameScreen parent) : this(parent, 500, 600)
        {
            
        }
        public override void LoadContent()
        {            
            TitleText = "Version Check";
            var verMod = "Vanilla";
            var mod = GlobalStats.ActiveMod;
            var versionText = GlobalStats.Version;
            var modVersionText = mod?.Version;
            
            if (mod?.mi != null)
            {
                if (mod?.mi.BitbucketAPIString != null)
                {
                    verMod = $"{mod.ModName} - {mod.Version}";
                    ModURL = mod.mi.BitbucketAPIString;
                    ModDownLoadSite = mod.mi.DownLoadSite;
                }
                else
                {
                    verMod = "Unsupported";
                }
            }

            MiddleText = $"{GlobalStats.ExtendedVersion}\nMod: {verMod}";
            base.LoadContent();            
            BBVersionCheck.LoadContent(URL);
            ModVersionCheck.LoadContent(ModURL);
            if (BBVersionCheck.FilesAndLinks == null)
            {
                ExitScreen();
                return;
            }
            Vector2 drawLoc = BodyTextStart;            
            BBListHeader = new UILabel(this, drawLoc, "========== BlackBox ==========");
            drawLoc.Y += 16;
            drawLoc = BBVersionCheck.PopulateVersions(versionText, this, drawLoc);
            drawLoc.Y += 16;
            ModListHeader = new UILabel(this, drawLoc, $"========== {mod?.ModName ?? "Vanilla"} ==========");
            drawLoc.Y += 16;
            
            if (ModURL.NotEmpty()) ModVersionCheck.PopulateVersions(modVersionText, this, drawLoc);

        }

        public override void Draw(SpriteBatch batch)
        {
            if (BBVersionCheck.FilesAndLinks == null)
            {
                ExitScreen();
                return;
            }
            base.Draw(batch);
            batch.Begin();
            BBListHeader.Draw(batch);
            BBVersionCheck.Draw(batch);
            ModListHeader.Draw(batch);
            ModVersionCheck.Draw(batch);


            batch.End();
            
        }

        public override bool HandleInput(InputState input)
        {
            if (input.Escaped)
            {
                ExitScreen();
                return true;
            }
            BBVersionCheck.HandleInput(input,DownLoadSite);
            ModVersionCheck.HandleInput(input, ModDownLoadSite);
            
            return base.HandleInput(input);
        }

    }
}