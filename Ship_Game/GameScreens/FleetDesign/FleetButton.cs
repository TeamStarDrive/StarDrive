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
    public Action<FleetButton> OnHotKey;
    public Func<FleetButton, bool> IsSelected;

    public FleetButton(UniverseScreen us, int key, Vector2 size)
        : base(UI.LocalPos.Zero, size, Color.TransparentBlack)
    {
        Screen = us;
        FleetKey = key;
    }


    public override bool HandleInput(InputState input)
    {
        // handle mouse clicks only
        if (input.LeftMouseClick && HitTest(input.CursorPosition))
        {
            OnClick?.Invoke(this);
            return true;
        }
        return base.HandleInput(input);
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        base.Draw(batch, elapsed);

        Fleet f = Player?.GetFleetOrNull(FleetKey);

        // FleetDesigner: fleet can be null
        // UniverseScreen: fleet cannot be null
        if (FleetDesigner || f != null) 
        {
            RectF r = RectF;
            bool isSelected = IsSelected(this);
            DrawBackground(batch, f, isSelected, r);
            DrawIcon(batch, f, r);
            DrawRequisitionIcon(batch, f, r);
            DrawFleetKey(batch, isSelected, r);
            DrawFleetShipIcons(batch, f, r);
        }
    }

    void DrawBackground(SpriteBatch batch, Fleet f, bool isSelected, in RectF r)
    {
        bool inCombat = f?.IsAnyShipInCombat() == true;
        Color background = isSelected ? new(50,50,100,160) : new(Color.Black, 80);
        if (inCombat) background = Screen.ApplyCurrentAlphaToColor(Color.Red);
        batch.Draw(ResourceManager.Texture("NewUI/rounded_square"), r, background);

        // draw the borders
        new Selector(r, Color.TransparentBlack).Draw(batch, null);
    }

    void DrawIcon(SpriteBatch batch, Fleet f, in RectF r)
    {
        if (f != null)
            batch.Draw(f.Icon, r.Bevel(-6), Player.EmpireColor);
    }

    void DrawRequisitionIcon(SpriteBatch batch, Fleet f, in RectF r)
    {
        if (f.AutoRequisition)
        {
            RectF autoReq = new(r.X - 18, r.Y + 5, 15, 20);
            Color colorReq = Screen.ApplyCurrentAlphaToColor(Player.EmpireColor);
            batch.Draw(ResourceManager.Texture("NewUI/AutoRequisition"), autoReq, colorReq);
        }
    }

    void DrawFleetKey(SpriteBatch batch, bool isSelected, in RectF r)
    {
        Font font = isSelected ? Fonts.Pirulen16 : Fonts.Pirulen12;
        Color color = isSelected ? Color.White : Color.Orange;
        Vector2 keyPos = isSelected ? new(r.X + 4, r.Y + 2) : new(r.X + 4, r.Y + 4);

        string key = FleetKey.ToString();
        if (isSelected) batch.DrawDropShadowText(key, keyPos, font, color);
        else            batch.DrawString(font, key, keyPos, color);
    }

    void DrawFleetShipIcons(SpriteBatch batch, Fleet f, RectF r)
    {
        if (f != null)
        {
            if (f.Ships.Count <= 30) DrawFleetShipIcons30(batch, f, r.X + 55, r.Y);
            else                     DrawFleetShipIconsSums(batch, f, r.X + 55, r.Y);
        }
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
