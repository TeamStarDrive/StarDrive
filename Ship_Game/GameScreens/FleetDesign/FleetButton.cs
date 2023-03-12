using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Fleets;
using System;
using SDUtils;
using Ship_Game.Graphics;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.FleetDesign;

// Fleet hotkey button for FleetDesigner
public class FleetButton : UIPanel
{
    readonly UniverseScreen Screen;
    Empire Player => Screen.Player;
    public readonly int FleetKey;
    public bool FleetDesigner = true;

    public Action<FleetButton> OnClick;
    public Func<FleetButton, bool> IsActive;

    public FleetButton(UniverseScreen us, int key, Vector2 size)
        : base(UI.LocalPos.Zero, size, Color.TransparentBlack)
    {
        Screen = us;
        FleetKey = key;
    }

    public static int InputFleetSelection(InputState input)
    {
        if (input.Fleet1) return 1;
        if (input.Fleet2) return 2;
        if (input.Fleet3) return 3;
        if (input.Fleet4) return 4;
        if (input.Fleet5) return 5;
        if (input.Fleet6) return 6;
        if (input.Fleet7) return 7;
        if (input.Fleet8) return 8;
        if (input.Fleet9) return 9;
        return -1;
    }

    public override bool HandleInput(InputState input)
    {
        if (input.LeftMouseClick && HitTest(input.CursorPosition))
        {
            OnClick?.Invoke(this);
            return true;
        }

        // handle hotkey
        if (InputFleetSelection(input) == FleetKey)
        {
            OnClick?.Invoke(this);
        }
        return base.HandleInput(input);
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        base.Draw(batch, elapsed);

        Fleet f = Screen.Player?.GetFleetOrNull(FleetKey);

        if (FleetDesigner)
        {
            DrawOnFleetDesignScreen(batch, elapsed, f, RectF);
        }
        else if (f != null) // UniverseScreen
        {
            DrawOnUniverseScreen(batch, elapsed, f, RectF);
        }
    }

    void DrawOnFleetDesignScreen(SpriteBatch batch, DrawTimes elapsed, Fleet f, RectF r)
    {
        bool isActive = IsActive(this);
        var sel = new Selector(r, Color.TransparentBlack);
        Color background = isActive ? new(0, 0, 255, 80) : Color.Black;
        batch.Draw(ResourceManager.Texture("NewUI/rounded_square"), r, background);
        sel.Draw(batch, elapsed);

        if (f?.DataNodes.Count > 0)
        {
            RectF firect = new(r.X + 6, r.Y + 6, r.W - 12, r.W - 12);
            batch.Draw(f.Icon, firect, Screen.Player.EmpireColor);
            if (f.AutoRequisition)
            {
                RectF autoReq = new(firect.X + 54, firect.Y + 12, 20, 27);
                var colorReq = Screen.ApplyCurrentAlphaToColor(Screen.Player.EmpireColor);
                batch.Draw(ResourceManager.Texture("NewUI/AutoRequisition"), autoReq, colorReq);
            }
        }

        Vector2 cursor = new(r.X + 4, r.Y + 4);
        batch.DrawString(Fonts.Pirulen12, FleetKey.ToString(), cursor, Color.Orange);
        cursor.X += (r.W + 5);
        if (f != null)
        {
            batch.DrawString(Fonts.Pirulen12, f.Name, cursor, isActive ? Color.White : Color.Gray);
            DrawFleetShipIcons(batch, f, r);
        }
    }

    void DrawOnUniverseScreen(SpriteBatch batch, DrawTimes elapsed, Fleet fleet, RectF r)
    {
        Selector buttonSelector = new(r, Color.TransparentBlack);
        RectF housing = new(r.X + 6, r.Y + 6, r.W - 12, r.W - 12);

        bool inCombat = fleet.IsAnyShipInCombat();
        Font fleetFont = Fonts.Pirulen12;
        Color fleetKey  = Color.Orange;
        bool needShadow = false;
        var keyPos = new Vector2(r.X + 4, r.Y + 4);
        if (Screen.SelectedFleet == fleet)
        {
            fleetKey   = Color.White;
            fleetFont  = Fonts.Pirulen16;
            needShadow = true;
            keyPos = new Vector2(keyPos.X, keyPos.Y - 2);
        }

        Color background = inCombat ? Screen.ApplyCurrentAlphaToColor(new(255, 0, 0)) : new( 0,  0,  0,  80);
        batch.Draw(ResourceManager.Texture("NewUI/rounded_square"), r, background);

        if (fleet.AutoRequisition)
        {
            RectF autoReq = new(r.X - 18, r.Y + 5, 15, 20);
            batch.Draw(ResourceManager.Texture("NewUI/AutoRequisition"), autoReq, Player.EmpireColor);
        }

        buttonSelector.Draw(batch, elapsed);
        batch.Draw(fleet.Icon, housing, Player.EmpireColor);
        if (needShadow)
            batch.DrawString(fleetFont, FleetKey.ToString(), new(keyPos.X + 2, keyPos.Y + 2), Color.Black);

        batch.DrawString(fleetFont, FleetKey.ToString(), keyPos, fleetKey);
        DrawFleetShipIcons(batch, fleet, r);
    }

    void DrawFleetShipIcons(SpriteBatch batch, Fleet fleet, RectF r)
    {
        float x = r.X + 55; // Offset from the button
        float y = r.Y;

        if (fleet.Ships.Count <= 30)
            DrawFleetShipIcons30(batch, fleet, x, y);
        else
            DrawFleetShipIconsSums(batch, fleet, x, y);
    }

    void DrawFleetShipIcons30(SpriteBatch batch, Fleet fleet, float x, float y)
    {
        // Draw ship icons to right of button
        Vector2 shipSpacingH = new(x, y);
        for (int i = 0; i < fleet.Ships.Count; ++i)
        {
            Ship ship = fleet.Ships[i];
            RectF iconHousing = new(shipSpacingH.X, shipSpacingH.Y, 15, 15);
            shipSpacingH.X += 18f;
            if (shipSpacingH.X > 237) // 10 Ships per row
            {
                shipSpacingH.X  = x;
                shipSpacingH.Y += 18f;
            }

            Color statColor = ship.GetStatusColor();
            if (statColor != Color.Black)
            {
                statColor = Screen.ApplyCurrentAlphaToColor(statColor);
                batch.Draw(ResourceManager.Texture("TacticalIcons/symbol_status"), iconHousing, statColor);
            }

            Color iconColor = ship.Resupplying ? Color.Gray : fleet.Owner.EmpireColor;

            TacticalIcon icon = ship.TacticalIcon();
            icon.Draw(batch, iconHousing, iconColor);
        }
    }

    void DrawFleetShipIconsSums(SpriteBatch batch, Fleet fleet, float x, float y)
    {
        Color color  = fleet.Owner.EmpireColor;
        Map<TacticalIcon, int> sums = new();
        for (int i = 0; i < fleet.Ships.Count; ++i)
        {
            Ship ship = fleet.Ships[i];

            TacticalIcon icon = ship.TacticalIcon();
            if (sums.TryGetValue(icon, out int value))
                sums[icon] = value + 1;
            else
                sums.Add(icon, 1);
        }

        Vector2 shipSpacingH = new(x, y);
        int roleCounter = 1;
        Color sumColor = Color.Goldenrod;
        if (sums.Count > 12) // Switch to default sum views if too many icon sums
        {
            sums = ConvertToPrimaryIconSums(sums);
            sumColor = Color.Gold;
        }

        foreach (TacticalIcon iconPair in sums.Keys.ToArr())
        {
            Rectangle iconHousing = new((int)shipSpacingH.X, (int)shipSpacingH.Y, 15, 15);
            string space = sums[iconPair] < 9 ? "  " : "";
            string sum = $"{space}{sums[iconPair]}x";
            batch.DrawString(Fonts.Arial10, sum, iconHousing.X, iconHousing.Y, sumColor);
            float ident = Fonts.Arial10.MeasureString(sum).X;
            shipSpacingH.X += ident;
            iconHousing.X += (int)ident;

            batch.Draw(iconPair.Primary, iconHousing, color);
            if (iconPair.Secondary != null)
                batch.Draw(iconPair.Secondary, iconHousing, color);

            shipSpacingH.X += 25f;
            if (roleCounter % 4 == 0) // 4 roles per line
            {
                shipSpacingH.X = x;
                shipSpacingH.Y += 15f;
            }

            ++roleCounter;
        }

        // Ignore secondary icons and returns only the hull role icons
        Map<TacticalIcon, int> ConvertToPrimaryIconSums(Map<TacticalIcon, int> excessSums)
        {
            Map<TacticalIcon, int> recalculated = new();
            foreach (TacticalIcon iconPair in excessSums.Keys.ToArr())
            {
                int numShips = excessSums[iconPair];

                TacticalIcon primaryIcon = new(iconPair.Primary, null);
                if (recalculated.TryGetValue(primaryIcon, out int count))
                    recalculated[primaryIcon] = count + numShips;
                else
                    recalculated.Add(primaryIcon, numShips);
            }
            return recalculated;
        }
    }
}
