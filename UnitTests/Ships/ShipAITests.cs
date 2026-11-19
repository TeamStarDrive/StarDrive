using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace UnitTests.Ships
{
    [TestClass()]
    public class ShipAITests : StarDriveTest
    {
        public ShipAITests()
        {
            LoadStarterShips("Heavy Carrier mk5-b",
                             "Owlwok Freighter S",
                             "Subspace Projector");
            CreateUniverseAndPlayerEmpire();
        }

        [TestMethod]
        public void IsTargetValidTest()
        {
            Empire us = Player;
            Ship ourShip = SpawnShip("Heavy Carrier mk5-b", us, new Vector2(0,0.1f));
            
            Ship theirShip = SpawnShip("Owlwok Freighter S", Enemy, new Vector2(0, 0.1f));
            ourShip.AI.Target = theirShip;
            Empire.SetRelationsAsKnown(us, Enemy);
            var ourRelation = us.GetRelations(Enemy);
            UState.Objects.Update(new(time: 1.0f));
            us.data.DiplomaticPersonality.Territorialism  = 60;
            us.data.DiplomaticPersonality.Opportunism     = 0.2f;
            us.data.DiplomaticPersonality.Trustworthiness = 80;

            // basic qualifiers
            theirShip.Active = false;
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));

            theirShip.Active = true;
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));

            theirShip.engineState = Ship.MoveState.Warp;
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));

            theirShip.engineState = Ship.MoveState.Sublight;
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), GetFailString(us, ourShip, theirShip, ourRelation));

            // advanced qualifiers

            // cant attack trade
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourRelation.Treaty_Trade = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Trade: " +GetFailString(us, ourShip, theirShip, ourRelation));

            // cant attack during peace.
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourRelation.Treaty_Peace = true;
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Peace: " + GetFailString(us, ourShip, theirShip, ourRelation));

            // faction tests
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Enemy.IsFaction = true;
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), "Faction: " + GetFailString(us, ourShip, theirShip, ourRelation));

            // faction trade works like empire
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Enemy.IsFaction = true;
                ourRelation.Treaty_Trade = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Faction Trade: " + GetFailString(us, ourShip, theirShip, ourRelation));

            // faction na pact
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Enemy.IsFaction = true;
                ourRelation.Treaty_NAPact = true;
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Faction NA Pact: " + GetFailString(us, ourShip, theirShip, ourRelation));

            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Player.IsFaction = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
                ourRelation.Treaty_NAPact = true;
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Faction NA Pact: " + GetFailString(us, ourShip, theirShip, ourRelation));

            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Player.IsFaction = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
                ourRelation.Treaty_NAPact = true;
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(null), "Faction NA Pact: " + GetFailString(us, ourShip, theirShip, ourRelation));

            Ship ourProjector = SpawnShip("Subspace Projector", Player, Vector2.Zero);

            // faction reset treaties and anger
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Enemy.IsFaction = true;
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), "Faction" + GetFailString(us, ourShip, theirShip, ourRelation));

            // violation tests.
            // anger inborders
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourRelation.AddAngerShipsInOurBorders(100);
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), "Anger Borders" + GetFailString(us, ourShip, theirShip, ourRelation));

            // anger military conflict
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourRelation.AddAngerMilitaryConflict(100);
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), "Anger Military:" + GetFailString(us, ourShip, theirShip, ourRelation));

            // anger territorial
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourRelation.AddAngerTerritorialConflict(100);
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), $"Territory: {GetFailString(us, ourShip, theirShip, ourRelation)}");

            // sanity test no environment
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
            });
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), GetFailString(us,ourShip, theirShip,ourRelation));

            ourProjector.InstantKill();

            // war tests
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Enemy.IsFaction = false;
                us.AI.DeclareWarOn(Enemy, WarType.BorderConflict);
            });
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), $"War: {GetFailString(us, ourShip, theirShip, ourRelation)}");

            // Empire attack can attacked tests.
            // faction attack napact
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Player.IsFaction = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
                ourRelation.Treaty_NAPact = true;
            });
            Assert.IsFalse(Player.IsEmpireAttackable(Enemy), "Faction attack NA Pact: " + GetFailString(us, ourShip, theirShip, ourRelation));

            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                Player.IsFaction = true;
                theirShip.AI.ChangeAIState(Ship_Game.AI.AIState.SystemTrader);
            });
            Assert.IsTrue(Player.IsEmpireHostile(Enemy), "Faction NA Pact hostile: " + GetFailString(us, ourShip, theirShip, ourRelation));

            // player tests
            /* FB - This one pass locally but fails in CI - need to check this
            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourShip.AI.HasPriorityTarget = true;
            });
            Empire.UpdateBilateralRelations(us, Enemy);
            Assert.IsTrue(ourShip.AI.IsTargetValid(theirShip), "Player chooses target: " + GetFailString(us, ourShip, theirShip, ourRelation));*/

            SetEnvironment(us, theirShip, ourRelation, () =>
            {
                ourShip.AI.HasPriorityTarget = true;
                ourRelation.Treaty_Alliance = true;
            });
            Empire.UpdateBilateralRelations(us, Enemy);
            Assert.IsFalse(ourShip.AI.IsTargetValid(theirShip), "Player Chooses Alliance Target: " + GetFailString(us, ourShip, theirShip, ourRelation));
        }

        [TestMethod]
        public void IsTargetValidTargetedBy()
        {
            CreateThirdMajorEmpire();

            Empire aggressive = Enemy;
            Empire peaceful   = ThirdMajor;

            aggressive.data.DiplomaticPersonality.Trustworthiness = 50;
            peaceful.data.DiplomaticPersonality.Trustworthiness   = 50;

            Ship aggressiveShip = SpawnShip("Heavy Carrier mk5-b", aggressive, Vector2.Zero);
            Ship peacefulShip   = SpawnShip("Heavy Carrier mk5-b", peaceful, Vector2.Zero);
            var aggressiveRel   = aggressive.GetRelations(peaceful);
            var peacefulRel     = peaceful.GetRelations(aggressive);

            aggressiveRel.UpdateRelationship(aggressive, peaceful);
            peacefulRel.UpdateRelationship(peaceful, aggressive);

            Assert.IsFalse(aggressiveShip.IsAttackable(peaceful, peacefulRel), "Aggressive Ship should not be attackable");
            Assert.IsFalse(peacefulShip.IsAttackable(aggressive, aggressiveRel), "Peaceful Ship should not be attackable");
            
            aggressiveRel.TotalAnger = 100;
            Assert.IsFalse(peaceful.IsEmpireAttackable(aggressive), "Peaceful Empire should not be attackable");
            Assert.IsFalse(aggressive.IsEmpireAttackable(peaceful), "Aggressive Empire should not be attackable");

            // Ship can be attacked even when empire is not attackable due to total anger (AttackForTransgressions)
            Assert.IsTrue(peacefulShip.IsAttackable(aggressive, aggressiveRel), "Peaceful Ship should be attackable since other party is angry");
            Assert.IsFalse(aggressiveShip.IsAttackable(peaceful, peacefulRel), "Aggressive ship should not be attackable, since Peaceful empire is not angry");

            aggressiveShip.AI.Target = peacefulShip;
            Assert.IsTrue(aggressiveShip.IsAttackable(peaceful, peacefulRel), "Aggressive ship should now be attackable since it is targeting the peaceful ship");
        }

        [TestMethod]
        [DataRow(TreatyType.NonAggression)]
        [DataRow(TreatyType.Peace)]
        public void UnderATreatyAShipAimingAtOursIsAttackableOnlyOnceItsEmpireHitIt(TreatyType treaty)
        {
            CreateThirdMajorEmpire();
            Ship theirs = SpawnShip("Heavy Carrier mk5-b", Enemy, Vector2.Zero);
            Ship ours   = SpawnShip("Heavy Carrier mk5-b", ThirdMajor, Vector2.Zero);
            Enemy.SignTreatyWith(ThirdMajor, treaty);
            Relationship oursToTheirs = ThirdMajor.GetRelations(Enemy);
            Assert.IsFalse(oursToTheirs.CanAttack, "setup: the treaty keeps us from attacking them");

            theirs.AI.Target = ours;
            Assert.IsFalse(theirs.IsAttackable(ThirdMajor, oursToTheirs), "a partner's ship that only aims at ours is left alone");

            ours.SetLastDamagedBy(theirs);
            Assert.IsTrue(theirs.IsAttackable(ThirdMajor, oursToTheirs), "once its empire has hit the ship it aims at, it is fair game");
        }

        [TestMethod]
        [DataRow(TreatyType.NonAggression)]
        [DataRow(TreatyType.Peace)]
        public void SigningATreatyEndsTheTwoEmpiresTargetingEachOther(TreatyType treaty)
        {
            CreateThirdMajorEmpire();
            Ship theirs = SpawnShip("Heavy Carrier mk5-b", Enemy, Vector2.Zero);
            Ship ours   = SpawnShip("Heavy Carrier mk5-b", ThirdMajor, Vector2.Zero);
            UState.Objects.Update(TestSimStep);
            Assert.IsTrue(System.Linq.Enumerable.Contains(Enemy.OwnedShips, theirs) && System.Linq.Enumerable.Contains(ThirdMajor.OwnedShips, ours),
                          "setup: each empire lists its ship");
            theirs.AI.Target = ours;
            ours.AI.Target = theirs;

            Enemy.SignTreatyWith(ThirdMajor, treaty);

            Assert.IsNull(theirs.AI.Target, "their ship drops its target on ours");
            Assert.IsNull(ours.AI.Target, "our ship drops its target on theirs");
        }

        [TestMethod]
        public void SigningATreatyEndsThePlayersOrdersAndOurWeaponsAimOnThePartnersShips()
        {
            CreateThirdMajorEmpire();
            Ship partners = SpawnShip("Heavy Carrier mk5-b", Enemy, Vector2.Zero);
            Ship others   = SpawnShip("Heavy Carrier mk5-b", ThirdMajor, Vector2.Zero);
            Ship ours     = SpawnShip("Heavy Carrier mk5-b", Player, Vector2.Zero);
            UState.Objects.Update(TestSimStep);
            Assert.IsTrue(ours.Weapons.Count >= 2, "setup: our ship has two weapons");
            Weapon onPartner = ours.Weapons[0], onOther = ours.Weapons[1];
            AimAt(onPartner, partners.Modules[0]);
            AimAt(onOther, others.Modules[0]);
            FiringASalvoAt(onPartner, others.Modules[0]);
            FiringASalvoAt(onOther, partners.Modules[0]);
            ours.AI.TargetQueue.Add(partners);
            ours.AI.TargetQueue.Add(others);
            ours.AI.Target = partners;
            ours.AI.HasPriorityTarget = true;
            Projectile partnersShot = Projectile.Create(partners.Weapons[0], partners, partners.Position, new Vector2(0, -1), ours.Modules[0], playSound: false);
            Projectile othersShot = Projectile.Create(others.Weapons[0], others, others.Position, new Vector2(0, -1), ours.Modules[0], playSound: false);
            ours.AI.PotentialTargets = new[] { partners, others };
            ours.AI.TrackProjectiles = new[] { partnersShot, othersShot };

            Player.SignTreatyWith(Enemy, TreatyType.Peace);

            Assert.IsNull(ours.AI.Target, "the ship drops the target the player gave it");
            Assert.IsTrue(ours.AI.TargetQueue.Count == 1 && ours.AI.TargetQueue[0] == others, "the partner's ships leave the player's queue, others stay");
            Assert.IsNull(onPartner.FireTarget, "a weapon aimed at the partner's ship stops");
            Assert.IsTrue(onPartner.SalvosToFire == 3 && SalvoTargetOf(onPartner) == others.Modules[0], "its salvo at another empire's ship goes on");
            Assert.AreEqual(others.Modules[0], onOther.FireTarget, "one aimed at another empire's ship keeps its aim");
            Assert.IsTrue(onOther.SalvosToFire == 0 && SalvoTargetOf(onOther) == null, "but the rest of its salvo at the partner is not fired");
            Assert.IsTrue(ours.AI.PotentialTargets.Length == 1 && ours.AI.PotentialTargets[0] == others,
                          "the weapons cannot pick the partner's ship again before the next scan");
            Assert.IsTrue(ours.AI.TrackProjectiles.Length == 1 && ours.AI.TrackProjectiles[0] == othersShot,
                          "nor the partner's shots");
        }

        static void AimAt(Weapon weapon, GameObject target)
            => typeof(Weapon).GetProperty(nameof(Weapon.FireTarget))!.SetValue(weapon, target);

        static readonly FieldInfo SalvoTarget = typeof(Weapon).GetField("SalvoTarget", BindingFlags.NonPublic | BindingFlags.Instance)!;
        static GameObject SalvoTargetOf(Weapon weapon) => (GameObject)SalvoTarget.GetValue(weapon);

        static void FiringASalvoAt(Weapon weapon, GameObject target)
        {
            SalvoTarget.SetValue(weapon, target);
            typeof(Weapon).GetProperty(nameof(Weapon.SalvosToFire))!.SetValue(weapon, 3);
        }

        [TestMethod]
        public void SigningATreatyPutsOutOurBeamsOnThePartnersShips()
        {
            CreateThirdMajorEmpire();
            Ship partners = SpawnShip("Heavy Carrier mk5-b", Enemy, new Vector2(0, -1000));
            Ship others   = SpawnShip("Heavy Carrier mk5-b", ThirdMajor, new Vector2(1000, 0));
            Ship ours     = SpawnShip("Heavy Carrier mk5-b", Player, Vector2.Zero);
            ShipModule emitter = System.Linq.Enumerable.First(ours.Modules, m => m.InstalledWeapon != null);
            Weapon laser = ResourceManager.CreateWeapon(UState, "LaserBeam", ours, emitter);
            var onPartner = new Beam(UState.CreateId(), laser, emitter.Position, partners.Position, partners.Modules[0]);
            var onOther   = new Beam(UState.CreateId(), laser, emitter.Position, others.Position, others.Modules[0]);
            ShipModule theirEmitter = System.Linq.Enumerable.First(partners.Modules, m => m.InstalledWeapon != null);
            Weapon theirLaser = ResourceManager.CreateWeapon(UState, "LaserBeam", partners, theirEmitter);
            var onUs = new Beam(UState.CreateId(), theirLaser, theirEmitter.Position, ours.Position, ours.Modules[0]);
            Assert.IsTrue(onPartner.Active && onOther.Active && onUs.Active && onPartner.TargetShip == partners,
                          "setup: our beams fire at the partner's ship and another's, and theirs at ours");

            Player.SignTreatyWith(Enemy, TreatyType.Peace);

            Assert.IsFalse(onPartner.Active, "a beam on the partner's ship goes out, though it keeps its own target");
            Assert.IsFalse(onUs.Active, "and the partner's beam on ours");
            Assert.IsTrue(onOther.Active, "a beam on another empire's ship burns on");
        }

        // a ship of ours carrying one troop, and the partner's ship it would board
        (Ship ours, Ship partners) ABoarderAndItsTarget()
        {
            Ship partners = SpawnShip("Heavy Carrier mk5-b", Enemy, new Vector2(0, -5000));
            Ship ours     = SpawnShip("Heavy Carrier mk5-b", Player, Vector2.Zero);
            ours.KillAllTroops();
            ResourceManager.CreateTroop("Wyvern", Player).LandOnShip(ours);
            Assert.IsFalse(Player.MayNotBoard(Enemy), "setup: before a treaty the partner's ship may be boarded");
            return (ours, partners);
        }

        [TestMethod]
        public void AfterSigningATreatyAShuttleOnItsWayToBoardThePartnersShipTurnsBack()
        {
            (Ship ours, Ship partners) = ABoarderAndItsTarget();
            ours.AI.OrderTroopToBoardShip(partners);
            ours.AI.Update(TestSimStep);
            Assert.AreEqual(ShipAI.Plan.BoardShip, ours.AI.OrderQueue.PeekFirst?.Plan, "setup: it is on its way to board");

            Player.SignTreatyWith(Enemy, TreatyType.Peace);
            ours.AI.Update(TestSimStep);

            Assert.AreNotEqual(ShipAI.Plan.BoardShip, ours.AI.OrderQueue.PeekFirst?.Plan, "it is called off");
        }

        [TestMethod]
        public void AfterSigningATreatyAShuttleTouchingDownOnThePartnersShipKeepsItsTroop()
        {
            (Ship ours, Ship partners) = ABoarderAndItsTarget();
            Player.SignTreatyWith(Enemy, TreatyType.Peace);

            ours.AI.LandTroopsAfterTouchdown(partners);

            Assert.IsFalse(partners.TroopsAreBoardingShip, "no troop goes aboard");
            AssertEqual(1, ours.TroopCount, "the shuttle keeps its troop");
        }

        // our ship listed by our empire, boarded by a troop of the partner's and one of a third empire
        (Ship ours, Troop partnersBoarder, Troop othersBoarder) BoardedByThePartnerAndAnother()
        {
            CreateThirdMajorEmpire();
            LoadStarterShips(Enemy.data.DefaultAssaultShuttle);
            Ship ours = SpawnShip("Heavy Carrier mk5-b", Player, Vector2.Zero);
            UState.Objects.Update(TestSimStep);
            Assert.IsTrue(System.Linq.Enumerable.Contains(Player.OwnedShips, ours), "setup: the empire lists our ship");
            Troop partnersBoarder = ResourceManager.CreateTroop("Wyvern", Enemy);
            Troop othersBoarder = ResourceManager.CreateTroop("Wyvern", ThirdMajor);
            partnersBoarder.LandOnShip(ours);
            othersBoarder.LandOnShip(ours);
            Assert.IsTrue(ours.TroopsAreBoardingShip, "setup: both troops are boarding our ship");
            return (ours, partnersBoarder, othersBoarder);
        }

        [TestMethod]
        public void SigningATreatySendsThePartnersBoardersOffOurShip()
        {
            (Ship ours, Troop partnersBoarder, Troop othersBoarder) = BoardedByThePartnerAndAnother();

            Player.SignTreatyWith(Enemy, TreatyType.Peace);

            Assert.IsTrue(partnersBoarder.HostShip != ours && partnersBoarder.HostShip?.Loyalty == Enemy,
                          "the partner's boarder leaves our ship in a shuttle of its own empire");
            Assert.AreSame(ours, othersBoarder.HostShip, "another empire's boarder fights on");
        }

        [TestMethod]
        public void ACaptureWonBeforeTheSigningStands()
        {
            (Ship ours, Troop partnersBoarder, _) = BoardedByThePartnerAndAnother();
            ours.LoyaltyChangeFromBoarding(Enemy, addNotification: false);

            Player.SignTreatyWith(Enemy, TreatyType.Peace);

            Assert.AreSame(ours, partnersBoarder.HostShip, "the boarders who won stay aboard the ship passing to their empire");
        }

        void ResetAnger(Relationship rel)
        {
            rel.AddAngerMilitaryConflict(-rel.Anger_MilitaryConflict + 5);
            rel.AddAngerShipsInOurBorders(-rel.Anger_FromShipsInOurBorders + 5);
            rel.AddAngerTerritorialConflict(-rel.Anger_TerritorialConflict + 5);
        }

        string GetFailString(Empire us, Ship ourShip, Ship theirShip, Relationship ourRelation)
        {
            return $"relation attack: {ourRelation.CanAttack}  ship attack: {theirShip.IsAttackable(us, ourRelation)} Loyalty attack {us.IsEmpireAttackable(Enemy, theirShip)}";
        }

        public void SetEnvironment(Empire us, Ship theirShip, Relationship ourRelation, Action setEnvironment)
        {
            theirShip.AI.ClearOrders();

            Enemy.IsFaction                 = false;
            Player.IsFaction                = false;
            ourRelation.Treaty_NAPact       = false;
            ourRelation.Treaty_Trade        = false;
            ourRelation.Treaty_Peace        = false;
            ourRelation.PeaceTurnsRemaining = 0;
            ourRelation.AtWar               = false;
            ourRelation.PreparingForWar     = false;
            ourRelation.ActiveWar           = null;

            ResetAnger(ourRelation);
            theirShip.AI.ClearOrders();

            setEnvironment.Invoke();
            ourRelation.UpdateRelationship(us, Enemy);
        }
    }
}