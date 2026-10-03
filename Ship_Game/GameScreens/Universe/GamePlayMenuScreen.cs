using System;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDGraphics;
using SDGraphics.Input;
using Ship_Game.Audio;
using Ship_Game.GameScreens.MainMenu;
using Ship_Game.Graphics;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game;

/// <summary>
/// In-Game Menu for Load/Save/Options and Exit to Windows
/// </summary>
public sealed class GamePlayMenuScreen : GameScreen
{
    public override bool HelpKeyOpensCodex => true;

    readonly UniverseScreen Universe;
    UILabel SavingText;
    UIButton SaveButton;
    UIButton LoadButton;
    UIButton ExitToMainMenu;
    UIButton ExitToWindows;

    public GamePlayMenuScreen(UniverseScreen screen) : base(screen, toPause: screen)
    {
        Universe = screen;
        IsPopup = true;
        TransitionOnTime  = 0.25f;
        TransitionOffTime = 0.25f;
    }

    public override void LoadContent()
    {
        RemoveAll();

        Vector2 c = ScreenCenter;
        Add(new Menu2(new RectF(c.X - 100, c.Y - 150, 200, 330)));

        SavingText = Add(new UILabel(GameText.Saving, Fonts.Pirulen16, Color.White));
        SavingText.Visible = false;
        SavingText.TextAlign = TextAlign.Center;
        SavingText.Pos = new Vector2(c.X - SavingText.Size.X*0.5f, 
            50 + Fonts.Pirulen16.LineSpacing * 2);

        UIList buttons = AddList(new Vector2(c.X - 84, c.Y - 100));
        buttons.Padding = new Vector2(2f, 12f);
        buttons.LayoutStyle = ListLayoutStyle.ResizeList;

        SaveButton = buttons.Add(ButtonStyle.Default, GameText.Save, Save_OnClick);
        LoadButton = buttons.Add(ButtonStyle.Default, GameText.LoadGame,   Load_OnClick);
        buttons.Add(ButtonStyle.Default, GameText.Options,   Options_OnClick);
        buttons.Add(ButtonStyle.Default, GameText.ReturnToGame, Return_OnClick);
        ExitToMainMenu = buttons.Add(ButtonStyle.Default, GameText.ExitToMainMenu, ExitToMain_OnClick);
        ExitToWindows = buttons.Add(ButtonStyle.Default, GameText.ExitToWindows, Exit_OnClick);
    }

    public override bool HandleInput(InputState input)
    {
        if (input.KeyPressed(Keys.O) && !GlobalStats.TakingInput)
        {
            GameAudio.EchoAffirmative();
            ExitScreen();
            return true;
        }

        return base.HandleInput(input);
    }

    public override void Update(float fixedDeltaTime)
    {
        // enable/disable buttons based on current status
        bool buttonsEnabled = !Universe.IsSaving && !IsExiting;
        SaveButton.Enabled = buttonsEnabled;
        LoadButton.Enabled = buttonsEnabled;
        ExitToMainMenu.Enabled = buttonsEnabled;
        ExitToWindows.Enabled = buttonsEnabled;

        SavingText.Enabled = Universe.IsSaving;
        if (SavingText.Enabled)
        {
            SavingText.Color = CurrentFlashColor;
        }
        base.Update(fixedDeltaTime);
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
        batch.SafeBegin();
        base.Draw(batch, elapsed);
        batch.SafeEnd();
    }

    // double layer of security, the Save/Load/Exit actions must be double-checked
    bool DisallowActions()
    {
        if (Universe.IsSaving || IsExiting)
        {
            GameAudio.NegativeClick();
            return true;
        }
        return false;
    }

    void Save_OnClick(UIButton button)
    {
        if (DisallowActions()) return;

        ScreenManager.AddScreen(new SaveGameScreen(Universe));
    }

    void Load_OnClick(UIButton button)
    {
        if (DisallowActions()) return;

        ExitScreen(); // exit before opening new screen
        ScreenManager.AddScreen(new LoadSaveScreen(Universe));
    }

    void Options_OnClick(UIButton button)
    {
        ScreenManager.AddScreen(new OptionsScreen(Universe)
        {
            TitleText  = Localizer.Token(GameText.Options),
            MiddleText = Localizer.Token(GameText.ChangeAudioVideoAndGameplay)
        });
    }

    void Return_OnClick(UIButton button)
    {
        ExitScreen(); 
    }

    void ExitToMain_OnClick(UIButton button)
    {
        if (DisallowActions()) return;

        // Leaving a game to MainMenu: the in-game content cache (RootContent +
        // ResourceManager.Textures, including decompressed DXT5 atlases in both RAM and
        // VRAM) stays pinned, and MainMenu's own atlas decompress allocates on top of it.
        // On memory-tight machines running Combined Arms that OOM'd inside
        // DxtReader.DecompressDXT5 even with a healthy managed heap (OS-level pressure).
        //
        // If both system RAM and VRAM have comfortable headroom right now, keep the cache:
        // fast exit, and a same-content new game reuses it. Otherwise free it first by
        // routing through GameLoadingScreen(resetResources:true) - the same safely-sequenced
        // UnloadAllData → UnloadGraphicsResources → boot LoadContent the mod-switch flow uses
        // (so MainMenu finds ResourceManager.Blank, Beam.BeamEffect, the Textures index, etc.
        // repopulated). The two are coupled: the fast path is only valid because we keep that
        // boot content loaded.
        if (MemoryPressure.HasHeadroomToKeepContent(Device))
            ScreenManager.GoToScreen(new MainMenuScreen(), clear3DObjects:true);
        else
            ScreenManager.GoToScreen(new GameLoadingScreen(showSplash:false, resetResources:true), clear3DObjects:true);
    }

    void Exit_OnClick(UIButton button)
    {
        if (DisallowActions()) return;

        StarDriveGame.Instance.Exit();
    }
}