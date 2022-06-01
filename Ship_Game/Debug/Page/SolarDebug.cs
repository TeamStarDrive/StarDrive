using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.Debug.Page
{
    internal class SolarDebug : DebugPage
    {
        SolarSystem System;
        readonly UniverseScreen Screen;
        public SolarDebug(UniverseScreen screen, DebugInfoScreen parent) : base(parent, DebugModes.Trade)
        {
            Screen = screen;
        }
        
        public override void Update(float fixedDeltaTime)
        {
            System = Screen.UState.Systems.FirstOrDefault(s => s.InFrustum);
            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch spriteBatch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            Screen.UState.Influence.DebugVisualize(Screen);

            if (System != null)
            {
                foreach (Planet p in System.PlanetList)
                {
                    Screen.DrawCircleProjected(p.Position3D, p.Radius, Color.Green);
                    Screen.DrawStringProjected(p.Position, p.Radius*0.2f, Color.Green, $"Scale={p.Scale}\nRadius={p.Radius}");
                }
                foreach (Moon m in System.MoonList)
                {
                    Screen.DrawCircleProjected(m.Position3D, m.Radius, Color.LightGray);
                    Screen.DrawStringProjected(m.Position, m.Radius*0.2f, Color.Green, $"Scale={m.MoonScale}\nRadius={m.Radius}");
                }
            }

            base.Draw(spriteBatch, elapsed);
        }
    }
}
