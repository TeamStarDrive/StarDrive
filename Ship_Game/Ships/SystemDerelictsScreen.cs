using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.UI;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = SDGraphics.Rectangle;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Ships;

// the salvageable derelicts the player knows of in one system, opened from its Exotic Systems row
public sealed class SystemDerelictsScreen : GameScreen
{
    public override bool HelpKeyOpensCodex => true;

    readonly SolarSystem System;

    public SystemDerelictsScreen(UniverseScreen universe, SolarSystem system) : base(universe, toPause: null)
    {
        System = system;
        IsPopup = true;
        TransitionOnTime = 0.25f;
        TransitionOffTime = 0.25f;
    }

    sealed class DerelictListItem : ScrollListItem<DerelictListItem>
    {
        readonly Derelict Derelict;

        public DerelictListItem(Derelict derelict)
        {
            Derelict = derelict;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            batch.Draw(Derelict.Design.Icon, new Rectangle((int)X, (int)Y, 29, 30), Color.White);
            batch.DrawString(Fonts.Arial12Bold, Derelict.Design.Name, new Vector2(X + 40f, Y + 8f), Color.White);
        }
    }

    public override void LoadContent()
    {
        RectF rect = new(ScreenWidth / 2 - 200, 200, 400, 500);
        string title = string.Format(Localizer.Token(GameText.DerelictsInSystem), System.Name);
        var derelictsList = Add(new SubmenuScrollList<DerelictListItem>(rect, title));
        derelictsList.SetBackground(Colors.TransparentBlackFill);

        Derelict[] derelicts = System.Universe.Derelicts.Current;
        for (int i = 0; i < derelicts.Length; ++i)
            if (derelicts[i].System == System && derelicts[i].SelectableByPlayer)
                derelictsList.List.AddItem(new DerelictListItem(derelicts[i]));

        base.LoadContent();
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
        batch.SafeBegin();
        base.Draw(batch, elapsed);
        batch.SafeEnd();
    }
}
