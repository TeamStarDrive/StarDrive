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

        public ParticleDebug(UniverseScreen screen, DebugInfoScreen parent) : base(parent, DebugModes.Trade)
        {
            Screen = screen;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            ParticleManager manager = Screen.Particles;
            var particleSystems = manager.ParticleSystems.ToArray();

            SetTextCursor(50, 280, Color.White);

            foreach (ParticleSystem ps in particleSystems)
            {
                DrawParticleStats(batch, ps);
            }

            base.Draw(batch, elapsed);
        }

        void DrawParticleStats(SpriteBatch batch, ParticleSystem ps)
        {
            if (ps.IsOutOfParticles)
            {
                TextColor = Color.Orange;
            }
            else
            {
                TextColor = Color.White;
            }
            
            var cursor = TextCursor;
            DrawString($"PS {ps.Name,-32}");
            SetTextCursor(190, cursor.Y, TextColor);
            DrawString($" NAct:{ps.NumActive}  Act:{ps.FirstActive}  New:{ps.FirstNew}  Free:{ps.FirstFree}  Ret:{ps.FirstRetired}  Max:{ps.MaxParticles}  Out:{ps.IsOutOfParticles}");
            SetTextCursor(cursor.X, TextCursor.Y, TextColor); // restore X
        }
    }
}