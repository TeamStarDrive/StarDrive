using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Ships;

namespace Ship_Game.Gameplay
{
    // FOR UNIT TESTS
    public class WeaponTestWrapper : Weapon, IWeaponTemplate
    {
        public WeaponTestWrapper(Weapon w, ShipHull hull) : base(w.T, w.Owner, w.Module, hull)
        {
            TestHitPoints = base.HitPoints;
            TestBaseRange = base.BaseRange;
            TestDamageAmount = base.DamageAmount;
            TestProjectileSpeed = base.ProjectileSpeed;
            TestExplosionRadius = base.ExplosionRadius;
            TestProjectileRadius = base.ProjectileRadius;
            TestPowerRequiredToFire = base.PowerRequiredToFire;
            TestOrdinanceRequiredToFire = base.OrdinanceRequiredToFire;
            TestProjectileCount = base.ProjectileCount;
            TestSalvoCount = base.SalvoCount;
            TestTruePD = base.TruePD;
            TestTag_PD = base.Tag_PD;
        }

        public float TestHitPoints;
        public float TestBaseRange;
        public float TestDamageAmount;
        public float TestProjectileSpeed;
        public float TestExplosionRadius;
        public float TestProjectileRadius;
        public float TestPowerRequiredToFire;
        public float TestOrdinanceRequiredToFire;
        public int TestProjectileCount;
        public int TestSalvoCount;
        public bool TestTruePD;
        public bool TestTag_PD;

        public override float HitPoints => TestHitPoints;
        public override float BaseRange => TestBaseRange;
        public override float DamageAmount => TestDamageAmount;
        public override float ProjectileSpeed => TestProjectileSpeed;
        public override float ExplosionRadius => TestExplosionRadius;
        public override float ProjectileRadius => TestProjectileRadius;
        public override float PowerRequiredToFire => TestPowerRequiredToFire;
        public override float OrdinanceRequiredToFire => TestOrdinanceRequiredToFire;
        public override int ProjectileCount => TestProjectileCount;
        public override int SalvoCount => TestSalvoCount;
        public override bool TruePD => TestTruePD;
        public override bool Tag_PD => TestTag_PD;
    }
}
