using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Graphics.Particles;
using Ship_Game.Ships;

namespace Ship_Game.Debug.Page
{
    internal class ParticleDebug : DebugPage
    {
        readonly UniverseScreen Screen;

        Vector2 Origin;

        public ParticleDebug(UniverseScreen screen, DebugInfoScreen parent) : base(parent, DebugModes.Trade)
        {
            Screen = screen;
            Origin = new Vector2(50, 280);

            Vector2 pos = Origin;
            foreach (ParticleSystem ps in screen.Particles.ParticleSystems.ToArray())
            {
                Add(new UICheckBox(pos.X - 32f, pos.Y, () => ps.IsEnabled, Fonts.Arial12,
                    "", "Toggle to enable/disable particle system"));
                Add(new UICheckBox(pos.X - 16f, pos.Y, () => ps.EnableDebug, Fonts.Arial12,
                    "", "Toggle to enable/disable particle DEBUG"));
                pos.Y += TextFont.LineSpacing;
            }
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            ParticleManager manager = Screen.Particles;
            var particleSystems = manager.ParticleSystems.ToArray();

            SetTextCursor(Origin.X, Origin.Y, Color.White);

            foreach (ParticleSystem ps in particleSystems)
            {
                DrawParticleStats(batch, ps);
            }

            base.Draw(batch, elapsed);
        }

        void DrawParticleStats(SpriteBatch batch, ParticleSystem ps)
        {
            if (ps.IsOutOfParticles)
                TextColor = Color.Orange;
            else
                TextColor = Color.White;
            
            var cursor = TextCursor;
            DrawString($"PS {ps.Name,-32}");
            SetTextCursor(Origin.X + 140, cursor.Y, TextColor);
            DrawString($" NAct:{ps.NumActive}  Act:{ps.FirstActive}  New:{ps.FirstNew}  Free:{ps.FirstFree}  Ret:{ps.FirstRetired}  Max:{ps.MaxParticles}  Out:{ps.IsOutOfParticles}");
            SetTextCursor(Origin.X, TextCursor.Y, TextColor); // restore X
        }
    }
}