using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Utils;

namespace Ship_Game.GameScreens.MainMenu
{
    internal class VersionChecking : PopupWindow
    {
        readonly ReadRestAPIFromSite BlackBoxVersionCheck;
        readonly ReadRestAPIFromSite ModVersionCheck;
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
            Add(new UILabel(drawLoc, "Click to download\n========== BlackBox =========="));
            drawLoc.Y += 32;
            drawLoc = BlackBoxVersionCheck.PopulateVersions(versionText, drawLoc);
            drawLoc.Y += 16;
            Add(new UILabel(drawLoc, $"========== {mod?.ModName ?? "Vanilla"} =========="));
            drawLoc.Y += 16;
            
            if (ModURL.NotEmpty())
                ModVersionCheck.PopulateVersions(modVersionText, drawLoc);
        }

        public override void Draw(SpriteBatch batch)
        {
            if (BlackBoxVersionCheck.FilesAvailable == null)
            {
                ExitScreen();
                return;
            }
            base.Draw(batch);
            BlackBoxVersionCheck.Draw(batch);
            ModVersionCheck.Draw(batch);

            
        }

        public override bool HandleInput(InputState input)
        {
            BlackBoxVersionCheck.HandleInput(input, DownLoadSite);
            ModVersionCheck.HandleInput(input, ModDownLoadSite);
            
            return base.HandleInput(input);
        }

    }
}