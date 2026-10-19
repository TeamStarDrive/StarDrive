using System.Globalization;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = SDGraphics.Rectangle;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Ships;

// a derelict's techs as its panel and the derelicts window show them to the player: the count of all it holds until the player sees its techs, then the icons of those the player can learn, each over a bar of the player's research of it
public static class DerelictTechIcons
{
    static readonly Color IconBorder = new(32, 30, 18); // as on a research notification
    static readonly Color BarBack = new(30, 34, 40);
    static readonly Color BarBorder = new(60, 66, 76);
    static readonly Color Researched = new(90, 170, 230);
    public const int BarHeight = 4;

    // the derelict's techs the empire can learn, in the derelict's order, into a list the caller keeps
    public static void Learnable(Empire empire, Derelict d, Array<TechEntry> learnable)
    {
        learnable.Clear();
        string[] techs = d.Techs;
        for (int i = 0; i < techs.Length; ++i)
            if (empire.TryGetTechEntry(techs[i], out TechEntry tech) && SalvageDerelict.CanLearn(empire, tech))
                learnable.Add(tech);
    }

    // all it holds until the empire sees its techs, then only those the empire can learn
    public static int Count(bool seen, Derelict d, Array<TechEntry> learnable) => seen ? learnable.Count : d.Techs.Length;

    // the research icon, with the count beside it as the ship info panel writes a ship's stats
    public static void DrawCount(SpriteBatch batch, in Rectangle iconRect, int count)
    {
        batch.Draw(ResourceManager.Texture("NewUI/icon_science"), iconRect, Color.White);
        var font = Fonts.Arial12Bold;
        var pos = new Vector2(iconRect.X + iconRect.Width + 2, iconRect.CenterY() - font.LineSpacing / 2);
        batch.DrawString(font, count.ToString(), pos, count > 0 ? Color.White : Color.Gray);
    }

    // its icon on the research notification's background, over a bar of how far the empire has researched it
    public static void DrawIcon(SpriteBatch batch, in Rectangle rect, TechEntry tech)
    {
        batch.Draw(ResourceManager.Texture("TechIcons/techbg"), rect, Color.White);
        batch.Draw(tech.Tech.Icon, rect, Color.White);
        batch.DrawRectangle(rect, IconBorder);

        var bar = new Rectangle(rect.X, rect.Bottom + 1, rect.Width, BarHeight);
        batch.FillRectangle(bar, BarBack);
        batch.DrawRectangle(bar, BarBorder);
        int filled = (int)((bar.Width - 2) * tech.PercentResearched.Clamped(0f, 1f));
        if (filled > 0)
            batch.FillRectangle(new Rectangle(bar.X + 1, bar.Y + 1, filled, bar.Height - 2), Researched);
    }

    // what the order's loads have given the tech
    public static float GainedOf(SalvageDerelict order, TechEntry tech)
    {
        string[] techs = order.Techs;
        for (int i = 0; i < techs.Length; ++i)
            if (techs[i] == tech.UID)
                return order.Gained[i];
        return 0f;
    }

    // whole points below 10,000, then thousands with one decimal, and whole thousands from 100,000, so six fit side by side on the panel
    public static string Points(float points)
    {
        if (points < 10_000f)
            return ((int)points).ToString(CultureInfo.InvariantCulture);
        return (points / 1000f).ToString(points < 99_950f ? "0.0" : "0", CultureInfo.InvariantCulture) + "k";
    }

    // its name, how far the empire has researched it and, while the empire's salvage of the derelict stands, what that salvage has given it
    public static string Tooltip(TechEntry tech, SalvageDerelict order)
    {
        int percent = (int)(tech.PercentResearched.Clamped(0f, 1f) * 100f);
        string tip = tech.Tech.Name.Text + "\n" + string.Format(Localizer.Token(GameText.DerelictTechResearched), percent);
        if (order != null)
            tip += "\n" + string.Format(Localizer.Token(GameText.DerelictTechGained), Points(GainedOf(order, tech)));
        return tip;
    }
}
