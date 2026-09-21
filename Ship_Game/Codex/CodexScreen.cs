using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.GameScreens;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Codex
{
    public sealed class CodexScreen : PopupWindow
    {
        readonly Array<CodexEntry> Roots;
        ScrollList<CodexCategoryListItem> CategoryList;
        // UID → ScrollList item, populated as the tree is built; powers OpenAt(uid).
        readonly Map<string, CodexCategoryListItem> ItemByUid = new();
        RectF TextRect;
        RectF TitleSeparator;
        Vector2 TitlePosition;
        StyledTextRenderer EntryBody;

        ScreenMediaPlayer Player;
        RectF SmallViewer;
        RectF BigViewer;
        CodexEntry ActiveEntry;
        string ActiveTitle = "";
        string ActiveText = "";
        // OpenAt may be invoked before LoadContent (ScreenManager.AddScreen
        // queues the screen for the next tick). Stash the UID and apply it
        // after the tree is built.
        string PendingUid;

        // Codex sizes to 60% of the screen on monitors big enough that 60%
        // width stays above 1024px; smaller screens get a fullscreen popup
        // so the layout (480px categories + body + margins) still fits.
        static int CodexSize(int dim) => (int)(GameBase.ScreenWidth * 0.6f) < 1024 ? dim : (int)(dim * 0.6f);

        public CodexScreen(GameScreen parent)
            : base(parent, CodexSize(GameBase.ScreenWidth), CodexSize(GameBase.ScreenHeight))
        {
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;

            Roots = CodexEntry.LoadAll();
            // opening the screen is the author's hot-reload point for both files
            CodexHooks.Reload();

            TitleText = Localizer.Token("CodexTitle");
        }

        public override void LoadContent()
        {
            base.LoadContent();

            TitleText += $" {GlobalStats.ExtendedVersion}";

            ActiveEntry = null;
            ActiveTitle = Localizer.Token("CodexTitle");
            ActiveText  = Localizer.Token(GameText.SelectATopicOnThe);

            // Lay out two columns of content between MidSepBot and the bottom chrome
            // (PopupWindow draws bottom border + close-button strip in the last ~30px).
            float top = MidSepBot.Y + 10;
            float usableH = Rect.Bottom - 30 - top;
            float catW = 480f;
            float gap = 10f;
            // Right margin = 25 from Rect.Right. Body width is the remaining
            // horizontal space between the category list's right edge and the margin.
            float bodyW = Rect.Width - 25 - catW - gap - 25;

            RectF categoriesRect = new(Rect.X + 25, top, catW, usableH);
            CategoryList = Add(new ScrollList<CodexCategoryListItem>(categoriesRect));

            TextRect = new(categoriesRect.X + catW + gap, top, bodyW, usableH);
            // Top margin leaves room for the title plus a comfortable gap before
            // the first body line — the title font scales to Arial20Bold at ≥1920,
            // so this needs to clear the larger glyph height.
            const float titleStripH = 55f;
            EntryBody = new StyledTextRenderer(new RectF(TextRect.X, TextRect.Y + titleStripH, TextRect.W, TextRect.H - titleStripH));
            TitleSeparator = new(TextRect.X, TextRect.Y + titleStripH - 13, TextRect.W, 2);

            ResetActiveTopic();

            SmallViewer = new(TextRect.X + 20, TextRect.Y + 40, 480, 270);
            BigViewer = new(ScreenWidth / 2 - 640, ScreenHeight / 2 - 360, 1280, 720);
            Player = new(ContentManager)
            {
                EnableInteraction = true,
                MuteGameAudioWhilePlaying = true,
                OnPlayStatusChange = OnPlayerStatusChanged
            };

            // LoadContent can re-run (resolution change, hot-reload). Rebuild the
            // UID map alongside the ScrollList so stale references can't leak in.
            ItemByUid.Clear();
            foreach (CodexEntry root in Roots)
                AddCategoryRecursive(parent: null, root, depth: 0);

            CategoryList.OnClick = OnCategoryClicked;

            if (PendingUid != null)
            {
                string uid = PendingUid;
                PendingUid = null;
                OpenAt(uid);
            }
        }

        // Build the ScrollList tree from CodexEntry.Children. Arbitrary depth: each
        // entry with children becomes an expandable header; leaves render Title +
        // ShortDesc directly.
        void AddCategoryRecursive(CodexCategoryListItem parent, CodexEntry entry, int depth)
        {
            if (entry.Hidden)
                return;

            var item = new CodexCategoryListItem(entry, depth);
            if (parent == null)
                CategoryList.AddItem(item);
            else
                parent.AddSubItem(item);

            if (!string.IsNullOrEmpty(entry.UID))
                ItemByUid[entry.UID] = item;

            if (entry.Children != null)
            {
                foreach (CodexEntry child in entry.Children)
                    AddCategoryRecursive(item, child, depth + 1);
            }
        }

        void ResetActiveTopic()
        {
            EntryBody.SetText(StyledTextParser.Parse(ActiveText));
            float titleW = CodexStyles.CaptionFont.TextWidth(ActiveTitle);
            TitlePosition = new(TextRect.CenterX - titleW / 2f - 15f, TextRect.Y + 10);
        }

        void OnCategoryClicked(CodexCategoryListItem item)
        {
            // A category with a body of its own reads like a topic when clicked, so
            // an overview lives on the category rather than in a filler child. A
            // bare header only expands: clear the video and keep the selection.
            if (item.IsHeader && !(item.Entry?.HasBody ?? false))
            {
                Player?.Stop();
                if (Player != null) Player.Visible = false;
                return;
            }

            SelectEntry(item.Entry);
        }


        void SelectEntry(CodexEntry entry)
        {
            ActiveEntry = entry;
            ActiveTitle = entry != null && !string.IsNullOrEmpty(entry.TitleId)
                ? Localizer.Token(entry.TitleId)
                : "";
            ActiveText  = entry != null && !string.IsNullOrEmpty(entry.TextId)
                ? Localizer.Token(entry.TextId)
                : "";

            // Always re-layout the body, even when the entry has no text — clears
            // the previous selection's content out of the renderer.
            ResetActiveTopic();

            if (entry != null && !string.IsNullOrEmpty(entry.Link))
                Log.OpenURL(entry.Link);

            if (entry == null || string.IsNullOrEmpty(entry.VideoPath))
            {
                Player?.Stop();
                if (Player != null) Player.Visible = false;
            }
            else
            {
                Player.PlayVideo(entry.VideoPath, looping: false, startPaused: true);
                Player.Visible = true;
            }
        }

        // Public deep-link API for tooltip hooks. Locate the entry by UID, expand
        // every ancestor in the source tree so the leaf is visible, then select it.
        // No-op + warn if the UID is not found — stale callsites mustn't hard-fail.
        public void OpenAt(string uid)
        {
            if (string.IsNullOrEmpty(uid))
                return;

            // Pre-LoadContent: CategoryList isn't built yet, defer.
            if (CategoryList == null)
            {
                PendingUid = uid;
                return;
            }

            if (!ItemByUid.TryGetValue(uid, out CodexCategoryListItem target))
            {
                Log.Warning($"CodexScreen.OpenAt: UID '{uid}' not found in Codex.yaml");
                return;
            }

            ExpandAncestors(Roots, uid);
            SelectEntry(target.Entry);
        }

        bool ExpandAncestors(Array<CodexEntry> branch, string targetUid)
        {
            foreach (CodexEntry e in branch)
            {
                if (e.UID == targetUid)
                    return true;
                if (e.Children != null && ExpandAncestors(e.Children, targetUid))
                {
                    if (ItemByUid.TryGetValue(e.UID, out CodexCategoryListItem item))
                        item.Expand(true);
                    return true;
                }
            }
            return false;
        }

        void OnPlayerStatusChanged()
        {
            Player.Rect = Player.IsPlaying ? BigViewer : SmallViewer;
        }

        public override bool HandleInput(InputState input)
        {
            if (Player != null && Player.HandleInput(input))
                return true;

            // Forward clicks and mouse-wheel scroll inside the body region.
            // Click → <url> spans. Wheel → scroll the styled-text content.
            if (EntryBody != null && TextRect.HitTest(input.CursorPosition))
            {
                if (input.LeftMouseClick && EntryBody.HandleClick(input.CursorPosition))
                    return true;
                if (input.ScrollIn)  { EntryBody.Scroll(-30f); return true; }
                if (input.ScrollOut) { EntryBody.Scroll(+30f); return true; }
            }

            if (!GlobalStats.TakingInput && (input.Codex || input.CodexHelp))
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }

            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            base.Draw(batch, elapsed);

            batch.SafeBegin();

            EntryBody?.Draw(batch);

            batch.Draw(ResourceManager.Texture("Popup/popup_separator"), TitleSeparator, Color.White);

            // Title is drawn above the body text for every entry; the original
            // wiki only showed it during paused video, which hid the topic name
            // for plain-text entries.
            batch.DrawString(CodexStyles.CaptionFont, ActiveTitle, TitlePosition, Color.White);

            Player?.Draw(batch);
            if (Player != null && Player.IsPaused)
                batch.DrawRectangleGlow(Player.Rect);

            batch.SafeEnd();
        }

        public override void ExitScreen()
        {
            Player?.Stop();
            base.ExitScreen();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;
            Player?.Dispose();
            base.Dispose(disposing);
        }
    }
}
