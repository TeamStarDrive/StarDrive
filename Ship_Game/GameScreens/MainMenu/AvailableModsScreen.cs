using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.GameScreens.MainMenu;

/// <summary>
/// Lists the mods that can be downloaded from GitHub with their latest and installed versions,
/// and installs or updates one through the AutoPatcher
/// </summary>
public sealed class AvailableModsScreen : GameScreen
{
    public enum ModState { Checking, Unavailable, NotInstalled, UpdateAvailable, UpToDate, DevCopy }

    public sealed class AvailableMod
    {
        public readonly string Name;
        public readonly string ModPath; // "Mods/Combined Arms/"
        public readonly string DownloadSite;
        public readonly string PageUrl; // the GitHub repo page
        public string InstalledVersion; // null when not installed, "" when the version cannot be read
        public bool IsDevCopy;
        public ReleaseInfo? Latest;
        public bool CheckFailed;

        public AvailableMod(string name, string folder, string downloadSite)
        {
            Name = name;
            ModPath = $"Mods/{folder}/";
            DownloadSite = downloadSite;
            const string releases = "/releases";
            PageUrl = downloadSite.EndsWith(releases) ? downloadSite.Substring(0, downloadSite.Length - releases.Length) : downloadSite;
        }

        public ModState State => GetState(IsDevCopy, InstalledVersion, Latest?.Version, CheckFailed);
    }

    static AvailableMod[] CreateKnownMods() => new AvailableMod[]
    {
        new("Combined Arms", "Combined Arms", "https://github.com/TeamStarDrive/CombinedArms/releases"),
        new("Star Trek", "Star Trek", "https://github.com/TeamStarDrive/StarTrekShatteredAlliance/releases"),
    };

    public static ModState GetState(bool isDevCopy, string installedVersion, string latestVersion, bool checkFailed)
    {
        if (isDevCopy)
            return ModState.DevCopy;
        if (latestVersion == null)
            return checkFailed ? ModState.Unavailable : ModState.Checking;
        if (installedVersion == null)
            return ModState.NotInstalled;
        return AutoUpdateChecker.IsModLatestNewer(latestVersion, installedVersion) ? ModState.UpdateAvailable : ModState.UpToDate;
    }

    // "v1.60.0009", "1.60.0009" and "1.60.9.0" are the same version
    public static bool IsSameVersion(string a, string b)
    {
        if (Version.TryParse(a.TrimStart('v', 'V'), out Version va) && Version.TryParse(b.TrimStart('v', 'V'), out Version vb))
            return va.Major == vb.Major && va.Minor == vb.Minor && Math.Max(va.Build, 0) == Math.Max(vb.Build, 0)
                && Math.Max(va.Revision, 0) == Math.Max(vb.Revision, 0);
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    public static (Color Latest, Color Installed) VersionColors(string installedVersion, string latestVersion)
    {
        if (installedVersion == null)
            return (Color.White, Color.Gray);
        if (latestVersion == null)
            return (Color.White, Color.White);
        return IsSameVersion(installedVersion, latestVersion) ? (Color.LightGreen, Color.LightGreen) : (Color.White, Color.Pink);
    }

    const int RowHeight = 48;

    readonly AvailableMod[] Mods = CreateKnownMods();
    Rectangle Window;
    ScrollList<AvailableModItem> ModsList;
    TaskResult CheckTask;

    public AvailableModsScreen(GameScreen parent) : base(parent, toPause: null)
    {
        IsPopup = true;
        TransitionOnTime = 0.25f;
        TransitionOffTime = 0.25f;
    }

    public override void LoadContent()
    {
        Window = new(ScreenWidth / 2 - 430, ScreenHeight / 2 - 150, 860, 300);
        Add(new Menu1(Window));

        Submenu sub = Add(new Submenu(new RectF(Window.X + 20, Window.Y + 20, Window.Width - 40, Window.Height - 40), GameText.AvailableMods));
        RectF client = sub.ClientArea;
        float listTop = client.Y + Fonts.Arial14Bold.LineSpacing + 16;
        ModsList = Add(new ScrollList<AvailableModItem>(new RectF(client.X, listTop, client.W, client.Bottom - listTop), RowHeight));

        foreach (AvailableMod mod in Mods)
        {
            ReadInstalled(mod);
            ModsList.AddItem(new AvailableModItem(this, mod));
        }

        base.LoadContent();
        CheckLatestVersions();
    }

    static void ReadInstalled(AvailableMod mod)
    {
        mod.IsDevCopy = AutoPatcher.IsDevCopy(mod.ModPath);
        var globals = new FileInfo(mod.ModPath + "Globals.yaml");
        if (!globals.Exists)
        {
            mod.InstalledVersion = null;
            return;
        }
        try
        {
            mod.InstalledVersion = GamePlayGlobals.Deserialize(globals).Mod?.Version ?? "";
        }
        catch (Exception e)
        {
            Log.Warning($"AvailableMods: cannot read {globals.FullName}: {e.Message}");
            mod.InstalledVersion = "";
        }
    }

    void CheckLatestVersions()
    {
        CheckTask = Parallel.Run(() =>
        {
            foreach (AvailableMod mod in Mods)
            {
                if (CheckTask is { IsCancelRequested: true })
                    return;

                ReleaseInfo? latest = null;
                try
                {
                    latest = AutoUpdateChecker.GetLatestModRelease(mod.DownloadSite, CheckTask);
                }
                catch (Exception e)
                {
                    Log.Warning($"AvailableMods: {mod.Name} release check failed: {e.Message}");
                }

                if (CheckTask is { IsCancelRequested: true })
                    return;
                RunOnNextFrame(() =>
                {
                    mod.Latest = latest;
                    mod.CheckFailed = latest == null;
                    ModsList.RequiresLayout = true;
                });
            }
        });
    }

    public override void ExitScreen()
    {
        CheckTask?.Cancel();
        base.ExitScreen();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            CheckTask?.Dispose();
        base.Dispose(disposing);
    }

    void ConfirmInstall(AvailableMod mod)
    {
        ModState state = mod.State;
        if (mod.Latest == null || (state != ModState.NotInstalled && state != ModState.UpdateAvailable))
            return;

        ReleaseInfo release = mod.Latest.Value with { Name = $"{mod.Name} {mod.Latest.Value.Version}" };
        int megabytes = (int)Math.Max(1, Math.Round(release.DownloadBytes / (1024.0 * 1024.0)));
        string folder = mod.ModPath.TrimEnd('/');

        string message = state == ModState.NotInstalled
            ? string.Format(Localizer.Token(GameText.ModInstallConfirm), mod.Name, release.Version, megabytes, folder)
            : string.Format(Localizer.Token(GameText.ModUpdateConfirm), mod.Name, InstalledText(mod), release.Version, megabytes, folder);
        message += "\n\n" + Localizer.Token(GameText.ModInstallRestart);

        var confirm = new MessageBoxScreen(this, message, MessageBoxButtons.Default, width: 420);
        confirm.Accepted = () => ScreenManager.AddScreen(new AutoPatcher(this, release, mod.ModPath));
        ScreenManager.AddScreen(confirm);
    }

    static string InstalledText(AvailableMod mod)
    {
        if (mod.InstalledVersion == null) return Localizer.Token(GameText.ModNotInstalled);
        if (mod.InstalledVersion.IsEmpty()) return Localizer.Token(GameText.ModUnknownVersion);
        return mod.InstalledVersion;
    }

    // Mod | Latest Version | Installed Version | Install/Update
    static RectF[] Columns(in RectF row)
    {
        float nameW = row.W * 0.34f;
        float versionW = row.W * 0.22f;
        RectF name = new(row.X, row.Y, nameW, row.H);
        RectF latest = new(name.Right, row.Y, versionW, row.H);
        RectF installed = new(latest.Right, row.Y, versionW, row.H);
        RectF action = new(installed.Right, row.Y, row.Right - installed.Right, row.H);
        return new[] { name, latest, installed, action };
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (IsExiting)
            return;
        ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
        batch.SafeBegin();
        base.Draw(batch, elapsed);

        if (ModsList.NumEntries > 0)
        {
            RectF first = ModsList.ItemAtTop.RectF;
            float headerY = ModsList.Y - Fonts.Arial14Bold.LineSpacing - 8;
            RectF[] columns = Columns(new RectF(first.X, headerY, first.W, Fonts.Arial14Bold.LineSpacing));
            DrawHeader(batch, columns[0], GameText.ModColumnName);
            DrawHeader(batch, columns[1], GameText.ModColumnLatest);
            DrawHeader(batch, columns[2], GameText.ModColumnInstalled);

            Color lineColor = new(118, 102, 67, 255);
            for (int i = 1; i < columns.Length; ++i)
                batch.DrawLine(new Vector2(columns[i].X, headerY), new Vector2(columns[i].X, ModsList.ItemsHousing.Bottom), lineColor);
            batch.DrawRectangle(ModsList.ItemsHousing, lineColor);
        }
        batch.SafeEnd();
    }

    static void DrawHeader(SpriteBatch batch, in RectF column, GameText text)
    {
        string title = Localizer.Token(text);
        var pos = new Vector2(column.CenterX - Fonts.Arial14Bold.TextWidth(title) / 2f, column.Y);
        batch.DrawString(Fonts.Arial14Bold, title, pos, Colors.Cream);
    }

    sealed class AvailableModItem : ScrollListItem<AvailableModItem>
    {
        readonly AvailableModsScreen Screen;
        readonly AvailableMod Mod;
        UILabel PageLink;

        public AvailableModItem(AvailableModsScreen screen, AvailableMod mod)
        {
            Screen = screen;
            Mod = mod;
        }

        public override void PerformLayout()
        {
            RemoveAll();
            RectF[] columns = Columns(RectF);
            ModState state = Mod.State;

            PageLink = AddCentered(columns[0], Mod.Name, Fonts.Arial20Bold, Color.Gold);
            PageLink.Highlight = Color.Transparent;
            PageLink.Tooltip = new LocalizedText(Mod.PageUrl, LocalizationMethod.RawText);

            var colors = VersionColors(Mod.InstalledVersion, Mod.Latest?.Version);
            if (Mod.Latest != null)
            {
                AddCentered(columns[1], Mod.Latest.Value.Version, Fonts.Arial12Bold, colors.Latest);
            }
            else if (!Mod.CheckFailed)
            {
                AddCentered(columns[1], Localizer.Token(GameText.ModChecking), Fonts.Arial12Bold, Color.Gray);
            }
            else
            {
                UILabel unavailable = AddCentered(columns[1], Localizer.Token(GameText.ModUnavailable), Fonts.Arial12Bold, Color.Pink);
                unavailable.Tooltip = GameText.ModUnavailableTip;
            }

            AddCentered(columns[2], InstalledText(Mod), Fonts.Arial12Bold, colors.Installed);

            AddActionButton(columns[3], state);
            base.PerformLayout();
        }

        public override bool HandleInput(InputState input)
        {
            if (PageLink != null && input.InGameSelect && PageLink.HitTest(input.CursorPosition))
            {
                GameAudio.BlipClick();
                Log.OpenURL(Mod.PageUrl);
                return true;
            }
            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (PageLink != null)
                PageLink.Color = PageLink.HitTest(Screen.Input.CursorPosition) ? Color.LightGoldenrodYellow : Color.Gold;

            base.Draw(batch, elapsed);

            if (PageLink != null)
            {
                float y = PageLink.Y + PageLink.Height - 1f;
                batch.DrawLine(new Vector2(PageLink.X, y), new Vector2(PageLink.X + PageLink.Width, y), PageLink.Color);
            }
        }

        UILabel AddCentered(in RectF column, string text, Graphics.Font font, Color color)
        {
            var pos = new Vector2(column.CenterX - font.TextWidth(text) / 2f, column.CenterY - font.LineSpacing / 2f);
            return Label(pos, text, font, color);
        }

        void AddActionButton(in RectF column, ModState state)
        {
            LocalizedText text = state switch
            {
                ModState.NotInstalled    => GameText.ModInstall,
                ModState.UpdateAvailable => GameText.ModUpdate,
                ModState.UpToDate        => GameText.ModUpToDate,
                ModState.DevCopy         => GameText.ModDevCopy,
                _ => LocalizedText.None,
            };
            if (!text.IsValid)
                return;

            UIButton button = Button(ButtonStyle.Default, text, OnActionClicked);
            button.Pos = new Vector2(column.CenterX - button.Width / 2f, column.CenterY - button.Height / 2f);

            if (state == ModState.UpToDate)
            {
                button.Enabled = false;
            }
            else if (state == ModState.DevCopy)
            {
                button.DefaultTextColor = button.HoverTextColor = button.PressTextColor = Color.Gray;
                button.Tooltip = GameText.ModDevCopyTip;
                button.ClickSfx = null;
            }
        }

        void OnActionClicked(UIButton button)
        {
            if (Mod.State == ModState.DevCopy)
            {
                GameAudio.NegativeClick();
                return;
            }
            Screen.ConfirmInstall(Mod);
        }
    }
}
