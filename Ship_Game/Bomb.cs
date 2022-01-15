using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        public int ShipLevel { get; }
        public float ShipHealthPercent { get; }

        public SubTexture Texture { get; }
        public Model      Model   { get; }

        public Bomb(Vector3 position, Empire empire, string weaponName, int shipLevel, float shipHealthPercent)
        {
            Owner       = empire;
            Texture     = ResourceManager.ProjTexture(TextureName);
            Model       = ResourceManager.ProjectileModelDict[ModelName];
            Position    = position;
            Weapon      = ResourceManager.GetWeaponTemplate(weaponName) ?? ResourceManager.GetWeaponTemplate("NuclearBomb");
            ShipLevel   = shipLevel;

            TroopDamageMin  = Weapon.BombTroopDamageMin;
            TroopDamageMax  = Weapon.BombTroopDamageMax;
            HardDamageMin   = Weapon.BombHardDamageMin;
            HardDamageMax   = Weapon.BombHardDamageMax;
            PopKilled       = Weapon.BombPopulationKillPerHit;
            FertilityDamage = Weapon.FertilityDamage;
            SpecialAction   = Weapon.HardCodedAction;
            ShipHealthPercent = shipHealthPercent;
        }

        public void DoImpact()
        {
            TargetPlanet.DropBomb(this);
            Owner.Universum.BombList.QueuePendingRemoval(this);
        }

        private void SurfaceImpactEffects()
        {
            if (Owner.Universum.IsSystemViewOrCloser && TargetPlanet.ParentSystem.IsVisible)
            {
                TargetPlanet.PlayPlanetSfx("sd_bomb_impact_01", Position);
                ExplosionManager.AddExplosionNoFlames(Owner.Universum, Position, 200f, 7.5f);
                Owner.Universum.Particles.Flash.AddParticle(Position, Vector3.Zero);
                for (int i = 0; i < 50; i++)
                    Owner.Universum.Particles.Explosion.AddParticle(Position, Vector3.Zero);
            }
        }

        public void PlayCombatScreenEffects(Planet planet, OrbitalDrop od)
        {
            if (Owner.Universum.IsViewingCombatScreen(planet))
            {
                GameAudio.PlaySfxAsync("Explo1");
                ((CombatScreen)Owner.Universum.workersPanel).AddExplosion(od.TargetTile.ClickRect, 4);
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
            PlanetRadius = TargetPlanet.ObjectRadius;
            Vector3 vtt = TargetPlanet.Center3D + 
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

            Vector3 planetPos = TargetPlanet.Center3D;

            float impactRadius = TargetPlanet.ShieldStrengthCurrent > 0f ? 100f : 30f;
            if (Position.InRadius(planetPos, PlanetRadius + impactRadius))
                DoImpact();


            // fiery trail radius:
            if (!Position.InRadius(planetPos, PlanetRadius + 1000f))
                return;

            if (TrailEmitter == null)
            {
                Velocity *= 0.65f;
                TrailEmitter     = Owner.Universum.Particles.ProjectileTrail.NewEmitter(500f, Position);
                FireTrailEmitter = Owner.Universum.Particles.FireTrail.NewEmitter(500f, Position);
            }
            TrailEmitter.Update(timeStep.FixedTime, Position);
            FireTrailEmitter.Update(timeStep.FixedTime, Position);
        }
    }
}