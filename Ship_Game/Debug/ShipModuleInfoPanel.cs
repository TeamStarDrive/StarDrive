using Ship_Game.UI;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Ships;

namespace Ship_Game.Debug;

public class ShipModuleInfoPanel : Submenu
{
    readonly UniverseScreen Screen;
    UIList Text;

    public ShipModule SelectedModule { get; private set; }

    public ShipModuleInfoPanel(UniverseScreen screen, LocalPos pos, Vector2 size)
        : base(pos, size)
    {
        Screen = screen;
        SetBackground(new Color(0, 0, 0, 50));
    }

    // shows a module, or hides it if module is null
    public void ShowModule(ShipModule module)
    {
        SelectedModule = module;
        Visible = module != null;
        if (Visible) { Text ??= CreateModuleTextLabels(); }
    }

    public bool TrySelectModule(InputState input, out ShipModule module)
    {
        module = null;

        // if we've selected a ship and double-click on something, try to get the module info
        Ship ship = Screen.SelectedShip;
        if (ship != null)
        {
            if (input.LeftMouseClick)
            {
                Vector2 pos = Screen.CursorWorldPosition2D;
                module = ship.GetModuleAt(ship.WorldToGridLocalPointClipped(pos));
                return true;
            }
        }
        else
        {
            return true; // force change in selected module
        }
        return false;
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (SelectedModule != null)
        {
            Screen.DrawCapsuleProjected(SelectedModule.GetModuleCollisionCapsule(), Color.Green, 4f);
        }
        base.Draw(batch, elapsed);
    }
    
    UIList CreateModuleTextLabels()
    {
        var list = Add(new UIList(new LocalPos(5, 5), Size, ListLayoutStyle.ResizeList));
        list.Padding = new(0,0);
        list.Add(new UILabel(_ => $"Module UID: {SelectedModule.UID}"));
        list.Add(new UILabel(_ => $"ID: {SelectedModule.Id} Active={SelectedModule.Active}"));
        list.Add(new UILabel(_ => $"Size: {SelectedModule.XSize} x {SelectedModule.YSize}"));
        list.Add(new UILabel(_ => $"Restrictions: {SelectedModule.Restrictions}"));
        list.Add(new UILabel(_ => $"GridPos: {SelectedModule.Pos}"));
        list.Add(new UILabel(_ => $"LocalCenter: {SelectedModule.LocalCenter}"));
        list.Add(new UILabel(_ => $"WorldPos: {SelectedModule.Position}"));
        list.Add(new UILabel(_ => $"HP: {SelectedModule.Health}/{SelectedModule.ActualMaxHealth} {SelectedModule.HealthPercent*100:0.0}%"));
        list.Add(new UILabel(_ => $"Ship: {SelectedModule.GetParent()?.Name}"));

        // since we are creating it during drawing, need to manually layout the elements
        PerformLayout();
        return list;
    }
}
