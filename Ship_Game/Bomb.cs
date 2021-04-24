using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Particle3DSample;
using Ship_Game.Audio;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed class Bomb
    {
        public Vector3 Position;
        public Vector3 Velocity;
        private Planet TargetPlanet;
        public Matrix World { get; private set; }

        public Weapon Weapon;
        private const string TextureName = "projBall_02_orange";
        private const string ModelName   = "projBall";

        private ParticleEmitter TrailEmitter;
        private ParticleEmitter FireTrailEmitter;
        public readonly int TroopDamageMin;
        public readonly int TroopDamageMax;
        public readonly int HardDamageMin;
        public readonly int HardDamageMax;
        public readonly float PopKilled;
        public readonly float FertilityDamage;
        public readonly string SpecialAction;
        public Empire Owner;
        private float PlanetRadius;

        public SubTexture Texture { get; }
        public Model      Model   { get; }

        public Bomb(Vector3 position, Empire empire, string weaponName)
        {
            Owner       = empire;
            Texture     = ResourceManager.ProjTexture(TextureName);
            Model       = ResourceManager.ProjectileModelDict[ModelName];
            Position    = position;
            Weapon      = ResourceManager.GetWeaponTemplate(weaponName) ?? ResourceManager.GetWeaponTemplate("NuclearBomb");

            TroopDamageMin  = Weapon.BombTroopDamage_Min;
            TroopDamageMax  = Weapon.BombTroopDamage_Max;
            HardDamageMin   = Weapon.BombHardDamageMin;
            HardDamageMax   = Weapon.BombHardDamageMax;
            PopKilled       = Weapon.BombPopulationKillPerHit;
            FertilityDamage = Weapon.FertilityDamage;
            SpecialAction   = Weapon.HardCodedAction;
        }

        public void DoImpact()
        {
            TargetPlanet.DropBomb(this);
            Empire.Universe.BombList.QueuePendingRemoval(this);
        }

        private void SurfaceImpactEffects()
        {
            if (Empire.Universe.IsSystemViewOrCloser && TargetPlanet.ParentSystem.isVisible)
            {
                TargetPlanet.PlayPlanetSfx("sd_bomb_impact_01", Position);
                ExplosionManager.AddExplosionNoFlames(Position, 200f, 7.5f);
                Empire.Universe.flash.AddParticleThreadB(Position, Vector3.Zero);
                for (int i = 0; i < 50; i++)
                    Empire.Universe.explosionParticles.AddParticleThreadB(Position, Vector3.Zero);
            }
        }

        public void PlayCombatScreenEffects(Planet planet, OrbitalDrop od)
        {
            if (Empire.Universe.IsViewingCombatScreen(planet))
            {
                GameAudio.PlaySfxAsync("Explo1");
                ((CombatScreen)Empire.Universe.workersPanel).AddExplosion(od.TargetTile.ClickRect, 4);
            }
            else
                SurfaceImpactEffects(); // If viewing the planet from space
        }

        public void ResolveSpecialBombActions(Planet planet)
        {
            if (SpecialAction.IsEmpty() || SpecialAction != "Free Owlwoks")
                return;

            if (planet.Owner == null || planet.Owner != EmpireManager.Cordrazine)
                return;

            for (int i = 0; i < planet.TroopsHere.Count; i++)
            {
                Troop troop = planet.TroopsHere[i];
                if (troop.Loyalty == EmpireManager.Cordrazine && troop.TargetType == TargetType.Soft)
                {
                    StarDriveGame.Instance?.SetSteamAchievement("Owlwoks_Freed");
                    troop.SetOwner(Owner);
                    troop.Name = EmpireManager.Cordrazine.data.TroopName.Text;
                    troop.Description = EmpireManager.Cordrazine.data.TroopDescription.Text;
                }
            }
        }

        public void SetTarget(Planet p)
        {
            TargetPlanet = p;
            PlanetRadius = TargetPlanet.SO.WorldBoundingSphere.Radius;
            Vector3 vtt = new Vector3(TargetPlanet.Center, 2500f) + 
                new Vector3(RandomMath2.RandomBetween(-500f, 500f) * p.Scale, 
                            RandomMath2.RandomBetween(-500f, 500f) * p.Scale, 0f) - Position;
            vtt = Vector3.Normalize(vtt);
            Velocity = vtt * 1350f;
        }

        public void Update(FixedSimTime timeStep)
        {
            Position += Velocity * timeStep.FixedTime;
            World    = Matrix.CreateTranslation(Position);
                        //* Matrix.CreateRotationZ(Facing);

            Vector3 planetPos = TargetPlanet.Center.ToVec3(z:2500f);

            float impactRadius = TargetPlanet.ShieldStrengthCurrent > 0f ? 100f : 30f;
            if (Position.InRadius(planetPos, PlanetRadius + impactRadius))
                DoImpact();


            // fiery trail radius:
            if (!Position.InRadius(planetPos, PlanetRadius + 1000f))
                return;

            if (TrailEmitter == null)
            {
                Velocity *= 0.65f;
                TrailEmitter     = Empire.Universe.projectileTrailParticles.NewEmitter(500f, Position);
                FireTrailEmitter = Empire.Universe.fireTrailParticles.NewEmitter(500f, Position);
            }
            TrailEmitter.Update(timeStep.FixedTime, Position);
            FireTrailEmitter.Update(timeStep.FixedTime, Position);
        }
    }
}