using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Utils;

namespace Ship_Game.GameScreens.MainMenu
{
    internal class VersionChecking : PopupWindow
    {
        readonly ReadRestAPIFromSite BlackBoxVersionCheck;
        readonly ReadRestAPIFromSite ModVersionCheck;
        UILabel BlackBoxListHeader;
        UILabel ModListHeader;
        const string URL = "http://api.bitbucket.org/2.0/repositories/CrunchyGremlin/stardrive-blackbox/downloads";
        string ModURL = "";
        const string DownLoadSite = "http://bitbucket.org/CrunchyGremlin/stardrive-blackbox/downloads/";
        string ModDownLoadSite = "";
        public VersionChecking(GameScreen parent, int width, int height) : base(parent, width, height)
        {
            IsPopup = true;
            BlackBoxVersionCheck = new ReadRestAPIFromSite();
            ModVersionCheck = new ReadRestAPIFromSite();
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
            BlackBoxVersionCheck.LoadContent(URL);
            ModVersionCheck.LoadContent(ModURL);
            if (BlackBoxVersionCheck.FilesAvailable == null)
            {
                ExitScreen();
                return;
            }
            Vector2 drawLoc = BodyTextStart;            
            BlackBoxListHeader = new UILabel(this, drawLoc, "Click to download\n========== BlackBox ==========");
            drawLoc.Y += 32;
            drawLoc = BlackBoxVersionCheck.PopulateVersions(versionText, this, drawLoc);
            drawLoc.Y += 16;
            ModListHeader = new UILabel(this, drawLoc, $"========== {mod?.ModName ?? "Vanilla"} ==========");
            drawLoc.Y += 16;
            
            if (ModURL.NotEmpty()) ModVersionCheck.PopulateVersions(modVersionText, this, drawLoc);

        }

        public override void Draw(SpriteBatch batch)
        {
            if (BlackBoxVersionCheck.FilesAvailable == null)
            {
                ExitScreen();
                return;
            }
            base.Draw(batch);
            batch.Begin();
            BlackBoxListHeader.Draw(batch);
            BlackBoxVersionCheck.Draw(batch);
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
            BlackBoxVersionCheck.HandleInput(input,DownLoadSite);
            ModVersionCheck.HandleInput(input, ModDownLoadSite);
            
            return base.HandleInput(input);
        }

    }
}