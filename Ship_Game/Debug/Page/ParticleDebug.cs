﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Graphics.Particles;

namespace Ship_Game.Debug.Page
{
    internal class ParticleDebug : DebugPage
    {
        readonly UniverseScreen Screen;

        Map<IParticleSystem, bool> Selected = new Map<IParticleSystem, bool>();
        FloatSlider Scale;
        FloatSlider Velocity;
        FloatSlider LoyaltySlider;
        Empire Loyalty;

        ParticleManager Manager => Screen.Particles;
        IParticleSystem[] ParticleSystems => Manager.ParticleSystems.ToArray();

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

            Loyalty = EmpireManager.GetEmpireById(1);
            LoyaltySlider = right.Add(new FloatSlider(SliderStyle.Decimal, 200, 30,
                                                     $"Particle Loyalty: {Loyalty.Name}",
                                                     1, EmpireManager.NumEmpires, 0));
            LoyaltySlider.OnChange = (FloatSlider f) =>
            {
                Loyalty = EmpireManager.GetEmpireById((int)f.AbsoluteValue);
                f.Text = $"Particle Loyalty: {Loyalty.Name}";
            };

            var left = AddList(20, 320);
            foreach (IParticleSystem ps in ParticleSystems)
            {
                Selected[ps] = false;

                var horizontal = left.AddList(Vector2.Zero, new Vector2(400, 20));
                horizontal.Direction = new Vector2(1f, 0);

                horizontal.AddCheckbox(() => ps.IsEnabled, "on", "Toggle to enable/disable particle system");
                horizontal.AddCheckbox(() => ps.EnableDebug, "dbg", "Toggle to enable/disable particle DEBUG");
                horizontal.AddCheckbox(() => Selected[ps], b => Selected[ps] = b, "draw", "Select this particle for drawing");

                var name = horizontal.AddLabel(new Vector2(100, 20), $"PS {ps.Name}");
                name.DynamicText = l =>
                {
                    if (ps.IsOutOfParticles) l.Color = Color.Orange;
                    else if (Selected[ps])   l.Color = Color.Green;
                    else                     l.Color = Color.White;
                    return $"PS {ps.Name}";
                };

                var stats = horizontal.AddLabel(new Vector2(200, 20), "");
                stats.DynamicText = l => $" Active:{ps.ActiveParticles}  Max:{ps.MaxParticles}  Out:{ps.IsOutOfParticles}";
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
            ExplosionManager.AddExplosion(Screen.CursorWorldPosition + RandomMath.Vector3D(offset),
                                          velocity:RandomMath.Vector2D(velocity),
                                          radius: radius, intensity:5.0f, type:type);
        }

        public override void Update(float fixedDeltaTime)
        {
            foreach (IParticleSystem ps in ParticleSystems)
            {
                if (Selected[ps])
                {
                    Color color = Color.White;
                    if      (ps == Manager.ThrustEffect) color = Loyalty.ThrustColor1;
                    else if (ps == Manager.EngineTrail)  color = Loyalty.EmpireColor;

                    ps.AddParticle(Screen.CursorWorldPosition,
                                   new Vector3(Velocity.AbsoluteValue, 0, 0),
                                   Scale.AbsoluteValue, color);
                }
            }
            base.Update(fixedDeltaTime);
        }
    }
}