using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Collections.Generic;
using SDGraphics;
using Ship_Game.UI;

namespace Ship_Game.Debug.Page;

public class TargetingDebug : DebugPage
{
    ShipModule SelectedModule;
    Submenu ModuleInfo;

    public TargetingDebug(DebugInfoScreen parent) : base(parent, DebugModes.Targeting)
    {
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (!Visible)
            return;

        IReadOnlyList<Ship> masterShipList = Screen.UState.Ships;
        for (int i = 0; i < masterShipList.Count; ++i)
        {
            Ship ship = masterShipList[i];
            if (ship == null || !ship.InFrustum || ship.AI.Target == null)
                continue;

            foreach (Weapon weapon in ship.Weapons)
            {
                var module = weapon.FireTarget as ShipModule;
                if (module == null || module.GetParent() != ship.AI.Target || weapon.Tag_Beam || weapon.Tag_Guided)
                    continue;

                Screen.DrawCircleProjected(module.Position, 8f, 6, Color.MediumVioletRed);
                if (weapon.DebugLastImpactPredict.NotZero())
                {
                    Vector2 impactNoError = weapon.ProjectedImpactPointNoError(module);
                    Screen.DrawLineProjected(weapon.Origin, weapon.DebugLastImpactPredict, Color.Yellow);

                    Screen.DrawCircleProjected(impactNoError, 22f, 10, Color.BlueViolet, 2f);
                    Screen.DrawStringProjected(impactNoError, 28f, Color.BlueViolet, "pip");
                    Screen.DrawLineProjected(impactNoError, weapon.DebugLastImpactPredict, Color.DarkKhaki, 2f);
                }

                // TODO: re-implement this
                //Projectile projectile = ship.CopyProjectiles.FirstOrDefault(p => p.Weapon == weapon);
                //if (projectile != null)
                //{
                //    Screen.DrawLineProjected(projectile.Center, projectile.Center + projectile.Velocity, Color.Red);
                //}
                break;
            }
        }
        
        DrawModuleInfo(batch);

        base.Draw(batch, elapsed);
    }

    void DrawModuleInfo(SpriteBatch batch)
    {
        if (SelectedModule == null)
        {
            ModuleInfo?.Hide();
            return;
        }

        ModuleInfo ??= CreateModuleInfo();
        ModuleInfo.Show();
        Screen.DrawCircleProjected(SelectedModule.Position, SelectedModule.Radius, Color.Green, thickness:5);
    }

    Submenu CreateModuleInfo()
    {
        var info = Add(new Submenu(new LocalPos(-300, 200), new Vector2(300, 300))
        {
            Visible = false,
            ParentAlign = Align.TopRight
        });

        var list = info.Add(new UIList(LocalPos.Zero, new Vector2(300), ListLayoutStyle.ResizeList));
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
        info.PerformLayout();
        return info;
    }

    public override bool HandleInput(InputState input)
    {
        // if we've selected a ship and double-click on something, try to get the module info
        Ship ship = Universe.Screen.SelectedShip;
        if (ship != null)
        {
            if (input.LeftMouseDoubleClick)
            {
                Vector2 pos = Screen.CursorWorldPosition2D;
                SelectedModule = ship.GetModuleAt(ship.WorldToGridLocalPointClipped(pos));
            }
        }
        else
        {
            SelectedModule = null;
        }

        return base.HandleInput(input);
    }

    public override void Update(float fixedDeltaTime)
    {
        base.Update(fixedDeltaTime);
    }
}