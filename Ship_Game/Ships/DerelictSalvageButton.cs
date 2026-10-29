using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Commands.Goals;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = SDGraphics.Rectangle;
using Block = Ship_Game.Commands.Goals.SalvageDerelict.Block;

namespace Ship_Game.Ships;

// the Salvage and Recover buttons on the derelict's panel and on each row of the derelicts window, shown unless the derelict is gone or the player has no salvage ship, can build none and its race names none: gray, lit under the cursor when a click can order, and lit while the order stands in its button's work; gray while the player can build no salvage ship and has none
public static class DerelictSalvageButton
{
    public enum Kind { Salvage, Recover }

    public static bool Clickable(Block block) => block is Block.None or Block.Ordered;

    // from any thread: the Recover button's from the Salvage button's
    public static Block BlockOf(Kind kind, Block salvageBlock, Derelict d)
        => kind == Kind.Recover ? SalvageDerelict.RecoveryBlock(salvageBlock, d) : salvageBlock;

    // from any thread: the Salvage button's block decides for both
    public static bool Shown(Block salvageBlock) => salvageBlock is not (Block.Gone or Block.NoSalvageShip);

    // from any thread: the order standing on the derelict, read once for both buttons, or the recovery whose roll raises it, with its bar full and no buttons; none for a claim a save kept without its order
    public static SalvageDerelict StandingOrder(Empire player, Derelict d, Block salvageBlock)
        => salvageBlock == Block.Ordered || d.Rising ? SalvageDerelict.OrderOf(player, d) : null;

    // the order stands in the button's work; a claim kept without its order lights the Salvage button
    public static bool Lit(Kind kind, Block block, SalvageDerelict order)
        => block == Block.Ordered && (order?.Recovering ?? false) == (kind == Kind.Recover);

    public static void Draw(SpriteBatch batch, in Rectangle rect, Kind kind, Block block, bool lit, bool hovered)
    {
        bool lights = hovered && Clickable(block);
        bool bright = lit || lights;
        string back = lights ? "SelectionBox/button_action_hover"
                    : lit ? "SelectionBox/button_action"
                    : "SelectionBox/button_action_disabled";
        batch.Draw(ResourceManager.Texture(back), rect, Color.White);

        int size = rect.Width * 5 / 8;
        var iconRect = new Rectangle(rect.X + (rect.Width - size) / 2, rect.Y + (rect.Height - size) / 2, size, size);
        string icon = kind == Kind.Recover ? "TacticalIcons/symbol_construction" : "Modules/SalvageUnit";
        batch.Draw(ResourceManager.Texture(icon), iconRect, bright ? Color.White : Color.Gray);
    }

    // the tips carry their ids, so they link to the Codex; those with figures link through the id they are made from
    public static void Tooltip(Empire player, Derelict d, Kind kind, Block block, bool lit, SalvageDerelict order)
    {
        if (block == Block.Ordered && order == null) // a claim kept without its order, which either button's click gives up
        {
            ToolTip.CreateTooltip(GameText.SalvageDerelictOrderedTip);
            return;
        }
        if (kind == Kind.Recover && !lit && block is Block.None or Block.Ordered)
        {
            GameText id = block == Block.None ? GameText.RecoverDerelictTip : GameText.RecoverDerelictSwitchTip;
            string chances = string.Format(Localizer.Token(id), Percent(DerelictRecovery.Chance(player, d)),
                                           Percent(DerelictRecovery.CatastropheChanceOf(player)));
            ToolTip.CreateTooltip(chances, codexUid: Codex.CodexHooks.Find(id));
            return;
        }

        LocalizedText tip = block switch
        {
            Block.None => GameText.SalvageDerelictTip,
            Block.Ordered when lit => kind == Kind.Recover ? GameText.RecoverDerelictOrderedTip : GameText.SalvageDerelictOrderedTip,
            Block.Ordered => GameText.SalvageDerelictSwitchTip,
            Block.NoTech => GameText.SalvageDerelictNoTechTip,
            Block.NotAShip => GameText.RecoverDerelictStationTip,
            _ => LocalizedText.None,
        };
        if (block == Block.ClaimedByOther)
            ClaimTooltip(d, d.Claimant);
        else if (tip.IsValid)
            ToolTip.CreateTooltip(tip);
    }

    // from any thread: the empire holding the derelict, salvaging or recovering it
    public static string ClaimTip(Derelict d, Empire claimant)
        => string.Format(Localizer.Token(ClaimTipId(d, claimant)), claimant?.Name ?? "");

    static GameText ClaimTipId(Derelict d, Empire claimant)
        => claimant != null && SalvageDerelict.OrderOf(claimant, d) is { Recovering: true } ? GameText.DerelictRecoveredBy : GameText.DerelictSalvagedBy;

    // the claim's tip, linked to the Codex through the id it is made from
    public static void ClaimTooltip(Derelict d, Empire claimant)
        => ToolTip.CreateTooltip(ClaimTip(d, claimant), codexUid: Codex.CodexHooks.Find(ClaimTipId(d, claimant)));

    // from any thread: a salvage's stage as the derelicts window and the derelict's panel read it, with the turns left while its platform breaks in
    public static string StageText(SalvageDerelict.Stage stage, SalvageDerelict order) => stage switch
    {
        SalvageDerelict.Stage.BreakingIn       => BreakingIn(order.BreakInTurnsLeft),
        SalvageDerelict.Stage.WaitingForShip   => Localizer.Token(GameText.WaitingForASalvageShip),
        SalvageDerelict.Stage.ShipOrdered      => Localizer.Token(GameText.SalvageShipOrdered),
        SalvageDerelict.Stage.ShipOnItsWay     => Localizer.Token(GameText.SalvageShipOnItsWay),
        SalvageDerelict.Stage.PlatformOrdered  => Localizer.Token(GameText.SalvagePlatformOrdered),
        SalvageDerelict.Stage.BuildingPlatform => Localizer.Token(GameText.BuildingSalvagePlatform),
        SalvageDerelict.Stage.Recovering       => Localizer.Token(GameText.DerelictRecovering),
        _                                      => Localizer.Token(GameText.DerelictSalvaging),
    };

    // on the UI thread: made again only when the turns left change, so a status keeps its fit
    static string BreakingInText;
    static int BreakingInTurns = -1;

    static string BreakingIn(int turnsLeft)
    {
        if (turnsLeft != BreakingInTurns)
        {
            BreakingInTurns = turnsLeft;
            BreakingInText = string.Format(Localizer.Token(GameText.DerelictBreakingIn), turnsLeft);
        }
        return BreakingInText;
    }

    // a whole percent, as the panel shows it
    public static string Percent(float percent) => $"{Whole(percent)}%";

    static int Whole(float value) => (int)(value + 0.5f);
    static int WholeUp(float value) => (int)Math.Ceiling(value - 0.001f);

    // the yellow bar of a recovery's progress, its percent rounded down, so it reads 100% only once all is done
    public static ProgressBar RecoveryBar(in Rectangle rect) => new(rect) { color = "yellow", Max = 100f, DrawPercentage = true };

    // the empire's recovery's progress in the techs' place: the crossed hammers and a bar of the work done
    public static void DrawRecovery(SpriteBatch batch, in Rectangle iconRect, ProgressBar bar, Empire empire, Derelict d, SalvageDerelict order)
    {
        batch.Draw(ResourceManager.Texture("TacticalIcons/symbol_construction"), iconRect, Color.White);
        bar.Progress = DerelictRecovery.Progress(empire, d, order.RecoveryWork) * 100f;
        bar.Draw(batch);
    }

    // the player's recovery: the work done and all of it, the recovery speed, the break-in's turns, and the turns left in all; once the roll succeeds, that the ship comes
    public static void RecoveryTooltip(Empire empire, Derelict d, SalvageDerelict order)
    {
        if (order.RecoverySucceeded)
        {
            ToolTip.CreateTooltip(GameText.DerelictRisingTip);
            return;
        }
        int turnsLeft = DerelictRecovery.TurnsLeft(empire, d, order.BreakInDone, order.RecoveryWork);
        string tip = string.Format(Localizer.Token(GameText.DerelictRecoveryProgressTip), Whole(order.RecoveryWork),
                                   Whole(DerelictRecovery.Work(empire, d)), WholeUp(DerelictRecovery.Speed(empire)), DerelictRecovery.BreakInTurns,
                                   turnsLeft);
        ToolTip.CreateTooltip(tip, codexUid: Codex.CodexHooks.Find(GameText.DerelictRecoveryProgressTip));
    }

    // orders the derelict salvaged or recovered, calls the order off from its lit button, or turns it to the other button's work
    public static void Click(Empire player, Derelict d, Kind kind, Block block, bool lit)
    {
        bool recover = kind == Kind.Recover;
        if (block == Block.Ordered)
        {
            GameAudio.EchoAffirmative();
            if (lit)
                SalvageDerelict.Cancel(player, d);
            else
                SalvageDerelict.Switch(player, d, recover);
        }
        else if (block == Block.None && SalvageDerelict.Order(player, d, recover))
        {
            GameAudio.EchoAffirmative();
        }
        else
        {
            GameAudio.NegativeClick();
        }
    }
}
