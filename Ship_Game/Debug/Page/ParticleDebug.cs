using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Graphics.Particles;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.Debug.Page
{
    internal class ParticleDebug : DebugPage
    {
        readonly UniverseScreen Screen;

        Map<string, bool> Selected = new Map<string, bool>();
        FloatSlider Scale;
        FloatSlider Velocity;
        FloatSlider LoyaltySlider;
        Empire Loyalty;

        ParticleManager Manager => Screen.Particles;
        IParticle[] ParticleSystems => Manager.ParticleSystems.ToArr();

        bool IsSelected(IParticle ps) => Selected.TryGetValue(ps.Name, out bool isSelected) && isSelected;

        public ParticleDebug(UniverseScreen screen, DebugInfoScreen parent)
            : base(parent, DebugModes.Particles)
        {
            Screen = screen;

            var right = AddList(Screen.Width - 300, 300);
            right.AddLabel("Ctrl+LeftMouse to trigger Explosion");
            Scale = right.Add(new FloatSlider(SliderStyle.Decimal1, 200, 30,
                                              "Particle Draw Scale", 0.1f, 20.0f, 5.0f));
            Velocity = right.Add(new FloatSlider(SliderStyle.Decimal, 200, 30,
                                                 "Particle Velocity", 0, 10000, 500));

            Loyalty = screen.UState.GetEmpireById(1);
            LoyaltySlider = right.Add(new FloatSlider(SliderStyle.Decimal, 200, 30,
                                                     $"Particle Loyalty: {Loyalty.Name}",
                                                     1, screen.UState.NumEmpires, 0));
            LoyaltySlider.OnChange = (FloatSlider f) =>
            {
                Loyalty = screen.UState.GetEmpireById((int)f.AbsoluteValue);
                f.Text = $"Particle Loyalty: {Loyalty.Name}";
            };

            var left = AddList(20, 320);
            foreach (IParticle ps in ParticleSystems)
            {
                Selected[ps.Name] = false;

                var horizontal = left.AddList(Vector2.Zero, new Vector2(400, 20));
                horizontal.Direction = new Vector2(1f, 0);

                horizontal.AddCheckbox(() => ps.IsEnabled, "on", "Toggle to enable/disable particle system");
                horizontal.AddCheckbox(() => ps.EnableDebug, "dbg", "Toggle to enable/disable particle DEBUG");
                horizontal.AddCheckbox(() => Selected[ps.Name], b => Selected[ps.Name] = b, "draw", "Select this particle for drawing");

                var name = horizontal.AddLabel(new Vector2(100, 20), $"PS {ps.Name}");
                name.DynamicText = l =>
                {
                    if (ps.IsOutOfParticles) l.Color = Color.Red;
                    else if (IsSelected(ps)) l.Color = Color.Green;
                    else                     l.Color = Color.White;
                    return $"PS {ps.Name}";
                };

                var stats = horizontal.AddLabel(new Vector2(200, 20), "");
                stats.DynamicText = l => $"  {ps.ActiveParticles}/{ps.MaxParticles}  {(100f*ps.AllocatedParticles/ps.MaxParticles).String(1)}%";
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (input.IsCtrlKeyDown && input.LeftMouseClick)
            {
                GameAudio.PlaySfxAsync("sd_explosion_ship_det_large");
                MakeItRain(offset: 0f,   velocity: 50f, radius: 500f, ExplosionType.Ship);
                MakeItRain(offset: 500f, velocity: 50f, radius: 500f, ExplosionType.Projectile);
                MakeItRain(offset: 500f, velocity: 50f, radius: 500f, ExplosionType.Photon);
                MakeItRain(offset: 500f, velocity: 50f, radius: 500f, ExplosionType.Warp);
                for (int i = 0; i < 15; ++i) // some fireworks!
                    MakeItRain(offset: 500f, velocity: 50f, radius: 200f, ExplosionType.Projectile);
                return true;
            }
            return base.HandleInput(input);
        }

        void MakeItRain(float offset, float velocity, float radius, ExplosionType type)
        {
            ExplosionManager.AddExplosion(Screen, Screen.CursorWorldPosition + RandomMath.Vector3D(offset),
                                          velocity:RandomMath.Vector2D(velocity),
                                          radius: radius, intensity:5.0f, type:type);
        }

        public override void Update(float fixedDeltaTime)
        {
            if (!Screen.UState.Paused)
            {
                foreach (IParticle ps in ParticleSystems)
                {
                    if (IsSelected(ps))
                    {
                        Color color = Color.White;
                        if (ps == Manager.ThrustEffect) color = Loyalty.ThrustColor1;
                        else if (ps == Manager.EngineTrail) color = Loyalty.EmpireColor;

                        ps.AddParticle(Screen.CursorWorldPosition,
                                       new Vector3(Velocity.AbsoluteValue, 0, 0),
                                       Scale.AbsoluteValue, color);
                    }
                }
            }
            base.Update(fixedDeltaTime);
        }
    }
}