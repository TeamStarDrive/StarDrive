using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Collections.Generic;
using SDGraphics;
using Ship_Game.UI;

namespace Ship_Game.Debug.Page;

public class TargetingDebug : DebugPage
{
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
        
        base.Draw(batch, elapsed);
    }

    public override bool HandleInput(InputState input)
    {
        return base.HandleInput(input);
    }

    public override void Update(float fixedDeltaTime)
    {
        base.Update(fixedDeltaTime);
    }
}