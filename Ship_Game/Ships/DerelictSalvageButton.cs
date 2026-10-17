using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = SDGraphics.Rectangle;
using Block = Ship_Game.Commands.Goals.SalvageDerelict.Block;

namespace Ship_Game.Ships;

// the Salvage button on the derelict's panel and on each row of the derelicts window: lit while it can order, sunken while the order stands, grey otherwise
public static class DerelictSalvageButton
{
    public static bool Clickable(Block block) => block is Block.None or Block.Ordered;

    public static void Draw(SpriteBatch batch, in Rectangle rect, Block block, bool hovered)
    {
        bool clickable = Clickable(block);
        string back = !clickable ? "SelectionBox/button_action_disabled"
                    : hovered ? "SelectionBox/button_action_hover"
                    : block == Block.Ordered ? "SelectionBox/button_action_pressed"
                    : "SelectionBox/button_action";
        batch.Draw(ResourceManager.Texture(back), rect, Color.White);

        int size = rect.Width * 5 / 8;
        var iconRect = new Rectangle(rect.X + (rect.Width - size) / 2, rect.Y + (rect.Height - size) / 2, size, size);
        batch.Draw(ResourceManager.Texture("Modules/SalvageUnit"), iconRect, clickable ? Color.White : Color.Gray);
    }

    // the tips carry their ids, so they link to the Codex
    public static LocalizedText Tooltip(Block block, Derelict d) => block switch
    {
        Block.None => GameText.SalvageDerelictTip,
        Block.Ordered => GameText.SalvageDerelictOrderedTip,
        Block.ClaimedByOther => string.Format(Localizer.Token(GameText.DerelictSalvagedBy), d.Claimant?.Name ?? ""),
        Block.NoStation => GameText.SalvageDerelictNoStationTip,
        Block.NoFreeUnit => GameText.SalvageDerelictNoFreeUnitTip,
        _ => LocalizedText.None,
    };

    // orders the derelict salvaged, or calls the order off
    public static void Click(Empire player, Derelict d, Block block)
    {
        if (block == Block.Ordered)
        {
            GameAudio.EchoAffirmative();
            SalvageDerelict.Cancel(player, d);
        }
        else if (block == Block.None && SalvageDerelict.Order(player, d))
        {
            GameAudio.EchoAffirmative();
        }
        else
        {
            GameAudio.NegativeClick();
        }
    }
}
