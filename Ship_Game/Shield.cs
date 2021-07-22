using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Lights;

namespace Ship_Game
{
    public sealed class Shield
    {
        public float TexScale;
        public float Displacement;
        public Matrix World;
        private float Radius;
        private float Rotation;
        public GameplayObject Owner; // is null for PlanetaryShields
        private Vector2 PlanetCenter; // only valid for PlanetaryShields
        private PointLight Light;

        public Shield()
        {
        }

        // shield attached to a ShipModule
        public Shield(GameplayObject owner, float rotation, Vector2 center)
        {
            Owner         = owner;
            TexScale      = 2.8f;
            Rotation      = rotation;
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

        public bool InFrustum()
        {
            Vector2 center = Owner?.Position ?? PlanetCenter;
            return Empire.Universe.Frustum.Contains(center, Radius);
        }

        public void AddLight()
        {
            if (Light != null)                           
                return;
            
            Light = new PointLight();
            Empire.Universe.AddLight(Light);
        }

        public void RemoveLight()
        {
            if (Light == null)
                return;

            Empire.Universe.RemoveLight(Light);
            Light = null;
        }

        public void UpdateLightIntensity(float intensityReduction = 0.0f)
        {
            if (Light == null)
                return;

            Light.Intensity -= intensityReduction;
            if (Light.Intensity <= 0f)
                Light.Enabled = false;
        }

        public void HitShield(Planet planet, Bomb bomb, Vector2 planetCenter, float shieldRadius)
            => HitShield(planet, bomb.World, bomb.Position, planetCenter, shieldRadius);

        public void HitShield(Planet planet, Ship ship, Vector2 planetCenter, float shieldRadius)
            => HitShield(planet, ship.GetSO().World, ship.Position.ToVec3(ship.GetSO().World.Translation.Z), planetCenter, shieldRadius);

        public void HitShield(Planet planet, Matrix world, Vector3 pos, Vector2 planetCenter, float shieldRadius)
        {
            PlanetCenter = planetCenter;
            Vector3 center3D = PlanetCenter.ToVec3(2500f);
            planet.PlayPlanetSfx("sd_impact_shield_01", center3D);

            Rotation     = planetCenter.RadiansToTarget(pos.ToVec2());
            Radius       = shieldRadius;
            Displacement = 0.085f * RandomMath.RandomBetween(1f, 10f);
            TexScale     = 2.8f - 0.185f * RandomMath.RandomBetween(1f, 10f);

            AddLight();
            Light.World        = world;
            Light.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
            Light.Radius       = Radius* RandomMath.RandomBetween(1, 2);
            Light.Intensity    = RandomMath.RandomBetween(5, 15);
            Light.Enabled      = true;

            Vector3 vel = (pos - center3D).Normalized();

            Empire.Universe.Particles.Flash.AddParticle(pos);
            for (int i = 0; i < 200; ++i)
            {
                Empire.Universe.Particles.Sparks.AddParticle(pos, vel * RandomMath.Vector3D(25f));
            }
        }

        private static void CreateShieldHitParticles(Vector3 victim, Vector2 impact, bool beamFlash)
        {
            Vector2 vel = (impact - victim.ToVec2()).Normalized();
            Vector3 pos = impact.ToVec3(victim.Z);

            if (!beamFlash || RandomMath.RandomBetween(0f, 100f) > 90f)
                Empire.Universe.Particles.Flash.AddParticle(pos);

            for (int i = 0; i < 20; ++i)
            {
                var randVel = new Vector3(vel * RandomMath.RandomBetween(40f, 80f), RandomMath.RandomBetween(-25f, 25f));
                Empire.Universe.Particles.Sparks.AddParticle(pos, randVel);
            }
        }

        public void HitShield(ShipModule module, Projectile proj)
        {
            GameAudio.PlaySfxAsync("sd_impact_shield_01", module.GetParent().SoundEmitter);

            float intensity = 10f.Clamped(1, proj.DamageAmount / module.ShieldPower);

            Rotation     = module.Position.RadiansToTarget(proj.Position);
            Radius       = module.ShieldHitRadius;
            TexScale     = 2.8f - 0.185f * RandomMath.RandomBetween(intensity, 10f);
            Displacement = 0.085f * RandomMath.RandomBetween(intensity, 10f);

            AddLight();
            Light.World        = proj.WorldMatrix;
            Light.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
            Light.Radius       = module.ShieldHitRadius;
            Light.Intensity    = RandomMath.RandomBetween(intensity * 0.5f, 10f);
            Light.Enabled      = true;

            CreateShieldHitParticles(module.Center3D, proj.Position, beamFlash: false);
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
    }
}