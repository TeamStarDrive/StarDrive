using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class Combat
    {
        public float Timer = 4f;
        public int Phase   = 1;
        public PlanetGridSquare DefenseTile;
        public Empire AttackerLoyalty;
        public Troop AttackingTroop;
        public Troop DefendingTroop;
        public Building AttackingBuilding;
        public Building DefendingBuilding;
        public Planet Planet;


        public Combat(Troop attacker, Troop defender, PlanetGridSquare defenseTile, Planet planet)
        {
            AttackingTroop  = attacker;
            DefendingTroop  = defender;
            AttackerLoyalty = attacker.Loyalty;
            DefenseTile     = defenseTile;
            Planet          = planet;
        }

        public Combat(Building attacker, Troop defender, PlanetGridSquare defenseTile, Planet planet)
        {
            AttackingBuilding = attacker;
            DefendingTroop    = defender;
            AttackerLoyalty   = planet.Owner;
            DefenseTile       = defenseTile;
            Planet            = planet;
        }

        public Combat(Troop attacker, Building defender, PlanetGridSquare defenseTile, Planet planet)
        {
            AttackingTroop    = attacker;
            DefendingBuilding = defender;
            AttackerLoyalty   = attacker.Loyalty;
            DefenseTile       = defenseTile;
            Planet            = planet;
        }


        public void ResolveDamage(bool isViewing = false)
        {
            int damage = RollDamage();
            DealDamage(damage, isViewing);
            Phase = 2;
        }

        private int RollDamage()
        {
            TargetType attackType       = DefendingBuilding != null ? TargetType.Hard : DefendingTroop.TargetType;
            AttackerStats attackerStats = AttackingBuilding != null ? new AttackerStats(AttackingBuilding) 
                                                                    : new AttackerStats(AttackingTroop);

            float attackValue = attackType == TargetType.Soft ? attackerStats.SoftAttack : attackerStats.HardAttack;
            int damage = 0;
            for (int index = 0; index < attackerStats.Strength; ++index)
            {
                if (RandomMath.RandomBetween(0.0f, 100f) < attackValue)
                    ++damage;
            }

            return damage;
        }

        private void DealDamage(int damage, bool isViewing)
        {
            if (damage == 0)
            {
                if (isViewing)
                    GameAudio.PlaySfxAsync("sd_troop_attack_miss");

                return;
            }

            if (isViewing)
            {
                GameAudio.PlaySfxAsync("sd_troop_attack_hit");
                ((CombatScreen)Empire.Universe.workersPanel).AddExplosion(DefendingTroop?.ClickRect ?? DefenseTile.ClickRect, 1);
            }

            if (DefendingTroop != null)
            {
                if (isViewing && DefenseTile.TroopsHere.Contains(DefendingTroop))
                {
                    GameAudio.PlaySfxAsync("Explo1");
                    ((CombatScreen)Empire.Universe.workersPanel).AddExplosion(DefenseTile.ClickRect, 4);
                }

                DefendingTroop.DamageTroop(damage, Planet, DefenseTile, out bool dead);
                if (!dead) // Troops are still alive
                    return;

                Planet.ActiveCombats.QueuePendingRemoval(this);
                AttackingTroop?.LevelUp();
            }
            else if (DefendingBuilding != null)
            {
                DefendingBuilding.Strength -= damage;
                DefendingBuilding.CombatStrength -= damage;
                if (DefendingBuilding.Strength > 0)
                    return; // Building still stands

                Planet.BuildingList.Remove(DefendingBuilding);
                DefenseTile.Building = null; // make pgs building private in the future
            }
        }

        public bool Done => DefendingTroop?.Strength <= 0 || DefendingBuilding?.Strength <= 0 
                                                          || AttackingBuilding?.Strength <= 0 
                                                          || AttackingTroop?.Strength <= 0;

        private struct AttackerStats
        {
            public readonly float Strength;
            public readonly int HardAttack;
            public readonly int SoftAttack;

            public AttackerStats(Troop t)
            {
                Strength   = t.Strength;
                HardAttack = t.ActualHardAttack;
                SoftAttack = t.ActualSoftAttack;
            }

            public AttackerStats(Building b)
            {
                Strength   = b.Strength;
                HardAttack = b.HardAttack;
                SoftAttack = b.SoftAttack;
            }
        }
    }
}