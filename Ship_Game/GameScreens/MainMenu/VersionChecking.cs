using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.GameScreens.MainMenu
{
    internal class VersionChecking : PopupWindow
    {
        readonly ReadRestAPIFromSite BlackBoxVersionCheck;
        readonly ReadRestAPIFromSite ModVersionCheck;
        string URL = "";
        string DownloadSite = "";
        string ModURL = "";
        string ModDownloadSite = "";

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
            var settings = GlobalStats.Settings;
            var modSettings = GlobalStats.ActiveMod?.Settings;

            var mod = GlobalStats.ActiveMod;
            var versionText = GlobalStats.Version.Split(' ')[0];
            var modVersionText = mod?.Version;
            
            URL = settings.URL;
            DownloadSite = settings.DownloadSite;

            if (modSettings != null)
            {
                if (modSettings.BitbucketAPIString != null)
                    ModURL = modSettings.BitbucketAPIString;
                if (modSettings.URL != null)
                    ModDownloadSite = modSettings.DownloadSite;

                MiddleText = $"{GlobalStats.ExtendedVersion}\nMod: {mod.ModName} - {mod.Version}";
            }
            else
            {
                MiddleText = $"{GlobalStats.ExtendedVersion}\nVanilla";
            }

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

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (BlackBoxVersionCheck.FilesAvailable == null)
            {
                ExitScreen();
                return;
            }
            base.Draw(batch, elapsed);
            batch.Begin();
            BlackBoxVersionCheck.Draw(batch, elapsed);
            ModVersionCheck.Draw(batch, elapsed);
            batch.End();
        }

        public override bool HandleInput(InputState input)
        {
            return BlackBoxVersionCheck.HandleInput(input, DownloadSite)
                || ModVersionCheck.HandleInput(input, ModDownloadSite)
                || base.HandleInput(input);
        }
    }
}
