using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Graphics.Particles;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Lights;
using Matrix = SDGraphics.Matrix;

namespace Ship_Game
{
    public sealed class Shield
    {
        public float TexScale { get; private set; }
        public float Displacement { get; private set; }
        public Matrix World { get; private set; }
        float Radius;
        float Rotation;
        public readonly GameObject Owner; // is null for PlanetaryShields
        Vector2 PlanetCenter; // only valid for PlanetaryShields
        PointLight Light;

        public Shield()
        {
        }

        // shield attached to a ShipModule
        public Shield(GameObject owner, float rotation, Vector2 center)
        {
            Owner = owner;
            TexScale = 2.8f;
            Rotation = rotation;
            UpdateWorldTransform();
        }

        // stationary planet shields
        public Shield(Vector2 position)
        {
            PlanetCenter = position;
            TexScale = 2.8f;
            UpdateWorldTransform();
        }

        public void UpdateWorldTransform()
        {
            if (Owner != null)
            {
                World = Matrix.CreateScale(Radius /2) 
                      * Matrix.CreateRotationZ(Rotation)
                      * Matrix.CreateTranslation(Owner.Position.X, Owner.Position.Y, 0f);
            }
            else
            {
                World = Matrix.CreateScale(2f + 50f)
                      * Matrix.CreateRotationZ(0.0f)
                      * Matrix.CreateTranslation(PlanetCenter.X, PlanetCenter.Y, 2500f);
            }
        }

        public bool InFrustum(UniverseScreen u)
        {
            return u.IsInFrustum(Owner?.Position ?? PlanetCenter, Radius);
        }

        public void AddLight(UniverseScreen u)
        {
            if (Light != null)
                return;
            
            Light = new PointLight();
            u.AddLight(Light, dynamic:true);
        }

        public void RemoveLight(UniverseScreen u)
        {
            if (Light == null)
                return;

            u.RemoveLight(Light, dynamic:true);
            Light = null;
        }

        public void UpdateLightIntensity(float intensity)
        {
            if (Light == null)
                return;

            Light.Intensity += intensity;
            if (Light.Intensity <= 0f)
                Light.Enabled = false;
        }

        public void HitShield(Planet planet, Bomb bomb, Vector2 planetCenter, float shieldRadius)
            => HitShield(planet, bomb.World, bomb.Position, planetCenter, shieldRadius);

        public void HitShield(Planet planet, Ship ship, Vector2 planetCenter, float shieldRadius)
        {
            var shipSo = ship.GetSO();
            if (shipSo != null)
            {
                Matrix soWorld = (Matrix)shipSo.World;
                HitShield(planet, soWorld, ship.Position.ToVec3(soWorld.Translation.Z), planetCenter, shieldRadius);
            }
        }

        public void HitShield(Planet planet, in Matrix world, Vector3 pos, Vector2 planetCenter, float shieldRadius)
        {
            PlanetCenter = planetCenter;
            Vector3 center3D = PlanetCenter.ToVec3(2500f);
            planet.PlayPlanetSfx("sd_impact_shield_01", center3D);

            Rotation     = planetCenter.RadiansToTarget(pos.ToVec2());
            Radius       = shieldRadius;
            Displacement = 0.085f * planet.Random.Float(1f, 10f);
            TexScale     = 2.8f - 0.185f * planet.Random.Float(1f, 10f);

            if (planet.Universe.Screen.CanAddDynamicLight)
            {
                AddLight(planet.Universe.Screen);
                Light.World        = world;
                Light.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
                Light.Radius       = Radius * planet.Random.Float(1, 2);
                Light.Intensity    = planet.Random.Float(5, 15);
                Light.Enabled      = true;
            }


            var particles = planet.Universe.Screen.Particles;
            Vector3 impactNormal = center3D.DirectionToTarget(pos);

            particles.Flash.AddParticle(pos);
            for (int i = 0; i < 80; ++i)
            {
                particles.Sparks.AddParticle(pos, impactNormal * planet.Random.Vector3D(25f));
            }
        }

        static void CreateShieldHitParticles(ParticleManager particles, Vector2 projectilePos, Vector3 moduleCenter, bool beamFlash)
        {
            Vector3 pos = projectilePos.ToVec3(moduleCenter.Z);
            Vector2 impactNormal = moduleCenter.ToVec2().DirectionToTarget(projectilePos);

            if (!beamFlash || particles.Random.Float(0f, 100f) > 30f)
                particles.Flash.AddParticle(pos);


            for (int i = 0; i < 20; ++i)
            {
                var randVel = new Vector3(impactNormal * particles.Random.Float(40f, 80f), particles.Random.Float(-25f, 25f));
                particles.Sparks.AddParticle(pos, randVel);
            }
        }

        public void HitShield(UniverseScreen universe, ShipModule module, Projectile proj)
        {
            GameAudio.PlaySfxAsync("sd_impact_shield_01", module.GetParent().SoundEmitter);

            float intensity = 10f.Clamped(1, proj.DamageAmount / module.ShieldPower);

            Rotation = module.Position.RadiansToTarget(proj.Position);
            Radius = module.ShieldHitRadius;
            var random = module.GetParent().Loyalty.Random;
            TexScale = 2.6f - 0.1f * random.Float(intensity, 6f);
            Displacement = 0.085f * random.Float(intensity, 10f);

            if (universe.CanAddDynamicLight)
            {
                AddLight(universe);
                Light.World = proj.WorldMatrix;
                Light.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
                Light.Radius = module.ShieldHitRadius;
                Light.Intensity = random.Float(intensity * 0.5f, 10f);
                Light.Enabled = true;
            }
            CreateShieldHitParticles(universe.Particles, proj.Position, module.Center3D, beamFlash: false);
        }

        public static Color GetBubbleColor(float shieldRate, string colorName = "Green")
        {
            float alpha = shieldRate * 0.8f;
            switch (colorName)
            {
                default:
                case "Green": return new Color(0f, 1f, 0f, alpha);
                case "Red": return new Color(1f, 0f, 0f, alpha);
                case "Blue": return new Color(0f, 0f, 1f, alpha);
                case "Yellow": return new Color(1f, 1f, 0f, alpha);
            }
        }

        public bool LightEnabled => Light?.Enabled == true;

        public void UpdateTexScale(float value)
        {
            TexScale += value;
        }

        public void UpdateDisplacement(float value)
        {
            Displacement += value;
        }
    }
}