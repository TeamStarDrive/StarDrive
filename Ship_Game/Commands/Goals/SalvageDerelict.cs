using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.Commands.Goals
{
    // an empire's order to salvage one derelict: its salvage ship, built at its shipyard nearest the derelict, flies there and turns into a salvage platform, which strips the derelict into the order's hold or recovers it, then turns back into the ship, which brings home what it stripped
    [StarDataType]
    public class SalvageDerelict : DeepSpaceBuildGoal
    {
        // the platform's site is this far out from the derelict's radius
        public const float PlatformClearance = 100f;
        // the salvage ship starts to deploy once this close to the site
        public const float ArrivalRadius = 300f;

        // the salvage ship turns into the platform over two turns, and the platform back into the ship over two
        public const int DeployTurns = 2;
        public const int DismantleTurns = 2;

        [StarData] public int DerelictId;
        [StarData] public Ship Platform;
        // the salvage ship once built, while it flies out and deploys
        [StarData] public Ship SalvageShip { get; private set; }
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }
        // placed for a salvage ship; an order saved with a salvage station's constructor or platform ends on load
        [StarData] bool ByShip;
        // its platform stripped the derelict empty
        [StarData] public bool Emptied { get; private set; }
        // the Salvage button's second click called the order off while its platform stood
        [StarData] public bool CalledOff { get; private set; }
        // the Recover button's order: its platform works to bring the derelict's ship back instead of stripping it
        [StarData] public bool Recovering { get; private set; }
        // the work its platform has done on the recovery, kept while it strips and passed on with it
        [StarData] public float RecoveryWork { get; private set; }
        // the turns its platform has broken into the derelict since the order last turned to the recovery, passed on with it
        [StarData] public int BreakInDone { get; private set; }
        // its roll broke the derelict apart, so its platform's work is over
        [StarData] public bool RecoveryFailed { get; private set; }
        // its roll succeeded: the derelict rises for its ship, which takes its place
        [StarData] public bool RecoverySucceeded { get; private set; }
        // the turns left of the ship's deploy or the platform's dismantling
        [StarData] int TurnsLeft;
        // its derelict's techs and whole production, kept once the derelict has sunk away
        [StarData] public string[] Techs { get; private set; } = Empty<string>.Array;
        [StarData] public float DerelictProduction { get; private set; }
        // the research its platform's stripping has given each of those techs, by the techs' order; a passed order starts again from none
        [StarData] public float[] Gained { get; private set; } = Empty<float>.Array;
        // the production its platform has stripped, which its salvage ship brings home, lost with the platform or the ship; it passes on with the platform
        [StarData] public float Load { get; private set; }
        // its derelict's design name and hull icon, kept for the notifications once the derelict has sunk away
        [StarData] public string DerelictName { get; private set; }
        [StarData] public string DerelictIcon { get; private set; }
        Derelict Found;

        // its platform's work is over: it turns back into the salvage ship
        public bool Finished => Emptied || CalledOff || RecoveryFailed || Recovered;

        // from any thread: the recovered ship has taken the derelict's place, which has left the list
        public bool Recovered => RecoverySucceeded && UState.Derelicts.Find(DerelictId) == null;

        // from any thread: its salvage ship turns into the platform
        public bool Deploying => StepName == nameof(Deploy);

        // from any thread: its platform turns back into the salvage ship
        public bool Dismantling => StepName == nameof(Dismantle);

        public Derelict Derelict => Found ??= UState.Derelicts.Find(DerelictId);

        // no build marker on the map: the derelict's Salvage button calls the order off
        public override bool IsDeploymentGoal => false;

        [StarDataConstructor]
        public SalvageDerelict(Empire owner) : base(GoalType.SalvageDerelict, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                BuildShip,
                WaitForShip,
                FlyToDerelict,
                Deploy,
                Salvage,
                Dismantle
            };
        }

        public SalvageDerelict(Empire owner, Derelict derelict, bool recover = false) : this(owner)
        {
            ByShip = true;
            DerelictId = derelict.Id;
            Recovering = recover;
            Techs = derelict.Techs;
            Gained = new float[Techs.Length];
            DerelictProduction = derelict.Production;
            DerelictName = derelict.Design.Name;
            DerelictIcon = derelict.Design.BaseHull?.IconPath;
            Vector2 site = derelict.RestPosition + MathExt.PointOnCircle(owner.Random.Float(0f, 360f), derelict.Radius + PlatformClearance);
            Initialize(ShipDesign.SalvagePlatformName, site, derelict.System);
            Found = derelict;
        }

        // an order passed on with its platform, at work or turning back into the ship
        SalvageDerelict(Empire owner, SalvageDerelict passed) : this(owner)
        {
            ByShip = true;
            StarDateAdded = passed.StarDateAdded;
            DerelictId = passed.DerelictId;
            Techs = passed.Techs;
            Gained = new float[Techs.Length];
            DerelictProduction = passed.DerelictProduction;
            DerelictName = passed.DerelictName;
            DerelictIcon = passed.DerelictIcon;
            Initialize(ShipDesign.SalvagePlatformName, passed.StaticBuildPos, passed.TargetSystem);
            Platform = passed.Platform;
            Load = passed.Load;
            Emptied = passed.Emptied;
            CalledOff = passed.CalledOff;
            Recovering = passed.Recovering;
            RecoveryWork = passed.RecoveryWork;
            BreakInDone = passed.BreakInDone;
            RecoveryFailed = passed.RecoveryFailed;
            RecoverySucceeded = passed.RecoverySucceeded;
            TurnsLeft = passed.TurnsLeft;
            ChangeToStep(passed.Dismantling ? Dismantle : Salvage);
        }

        // on the sim thread, before the goals of an empire that leaves are cleared: its standing platforms' orders pass to the empire absorbing it or to its rebels, with the claims of those still at work, the platforms changing hands a step later
        public static void PassOn(Empire from, Empire to)
        {
            var goals = from.AI.Goals;
            for (int i = goals.Count - 1; i >= 0; --i)
            {
                if (goals[i] is not SalvageDerelict { Platform: { Active: true } platform } order || platform.Loyalty != from)
                    continue;

                to.AI.AddGoal(new SalvageDerelict(to, order));
                if (!order.Finished && order.Derelict is { } d && d.Claimant == from) // a finished order's derelict can be claimed by an order not yet at work, which ends
                {
                    d.ReleaseClaim();
                    d.TryClaim(to);
                    d.PassRecovery(from, to);
                }
                order.Platform = null; // its end no longer takes the platform away
                from.AI.RemoveGoal(order);
            }
        }

        public enum Block { None, Ordered, ClaimedByOther, Gone, NotAShip, NoSalvageShip, NoTech }

        // from any thread: why the empire cannot order the derelict salvaged, Ordered while its order stands, or None when it can; a race without a salvage ship cannot salvage, and an empire without the Salvage Ships technology cannot yet
        public static Block WhyNoOrder(Empire empire, Derelict d)
        {
            if (!empire.Universe.P.SalvageOps || !d.CanBeSalvaged || !d.IsKnownBy(empire))
                return Block.Gone;
            if (d.Claimant != null && d.Claimant != empire)
                return Block.ClaimedByOther;
            if (d.Claimant == empire || OrderOf(empire, d) != null)
                return Block.Ordered;
            if (empire.SalvageShipDesign == null)
                return Block.NoSalvageShip;
            return empire.data.AllowSalvageShip ? Block.None : Block.NoTech;
        }

        // from any thread: as for salvage, but a station, which can only be salvaged, is NotAShip
        public static Block WhyNoRecovery(Empire empire, Derelict d) => RecoveryBlock(WhyNoOrder(empire, d), d);

        public static Block RecoveryBlock(Block salvageBlock, Derelict d)
            => salvageBlock is Block.Gone or Block.NoSalvageShip || DerelictRecovery.CanRecover(d) ? salvageBlock : Block.NotAShip;

        // an order whose platform's work is over is no longer the derelict's
        public static SalvageDerelict OrderOf(Empire empire, Derelict d)
            => empire.AI.FindGoal(g => g is SalvageDerelict { Finished: false } s && s.DerelictId == d.Id) as SalvageDerelict;

        // the Salvage and Recover buttons; false when the empire cannot order it
        public static bool Order(Empire empire, Derelict d, bool recover = false)
        {
            if ((recover ? WhyNoRecovery(empire, d) : WhyNoOrder(empire, d)) != Block.None)
                return false;
            empire.AI.AddGoalAndEvaluate(new SalvageDerelict(empire, d, recover));
            return true;
        }

        // the other button's click while the order stands turns it to that button's work on the sim thread; a claim kept without its order is given up, as by a call-off
        public static void Switch(Empire empire, Derelict d, bool recover)
        {
            if (empire.Universe.Screen is { IsOnSimThread: false } screen)
            {
                screen.RunOnSimThread(() => Switch(empire, d, recover));
                return;
            }
            if (OrderOf(empire, d) is not { } order)
                Cancel(empire, d);
            else if (!order.RecoverySucceeded && (!recover || DerelictRecovery.CanRecover(d))) // a successful roll is past turning
                order.TurnTo(recover);
        }

        // turned to salvage, it keeps the recovery's work done, but a later recovery breaks in again
        void TurnTo(bool recover)
        {
            Recovering = recover; // first, so the UI never reads a salvage still breaking in
            if (!recover)
                BreakInDone = 0;
        }

        // from any thread: its platform has yet to break into the derelict before the recovery's work starts
        public bool BreakingIn => Recovering && BreakInDone < DerelictRecovery.BreakInTurns;

        public int BreakInTurnsLeft => (DerelictRecovery.BreakInTurns - BreakInDone).LowerBound(0);

        // the button's second click calls the order off on the sim thread, where its claim is given up; a platform standing turns back into the ship, which brings home what it stripped; a successful roll is past calling off
        public static void Cancel(Empire empire, Derelict d)
        {
            if (empire.Universe.Screen is { IsOnSimThread: false } screen)
            {
                screen.RunOnSimThread(() => Cancel(empire, d));
                return;
            }
            if (OrderOf(empire, d) is { } order)
            {
                if (order.RecoverySucceeded)
                    return;
                if (order.Platform is { Active: true })
                    order.CallOff();
                else
                    empire.AI.RemoveGoal(order);
            }
            else if (d.Claimant == empire) // a claim kept without its order, as a save can catch it
            {
                d.ReleaseClaim();
            }
        }

        // the derelict's turns run on, and any empire can salvage it
        void CallOff()
        {
            CalledOff = true;
            if (Derelict is { } d && d.Claimant == Owner)
                d.ReleaseClaim();
        }

        // an order stands while salvage ops are on and its derelict can be salvaged, unless another empire claimed it first or another order of its empire's has a platform at work on it, while the derelict rises after a successful roll, and once its platform's work is over
        bool Stands => Owner.Universe.P.SalvageOps
                       && (Finished || RecoverySucceeded || Derelict is { CanBeSalvaged: true } d && (d.Claimant == null || d.Claimant == Owner) && !GivesWay);

        // an order not yet at work gives way to one passed on to its empire with a platform at work on the derelict
        bool GivesWay => Platform == null
                         && Owner.AI.HasGoal(g => g != this && g is SalvageDerelict { Finished: false, Platform: { Active: true } } s
                                                  && s.DerelictId == DerelictId);

        // an order that no longer stands ends; the player is told when a rival claimed its derelict first, or it sank before the platform stood
        protected override GoalStep? PreEvaluate()
        {
            if (!ByShip)
                return GoalStep.GoalFailed;
            if (Stands)
                return null;
            if (Owner.Universe.P.SalvageOps && !GivesWay)
            {
                if (Derelict is { CanBeSalvaged: true } d)
                    PlayerNotifications?.AddSalvageClaimedFirstNotification(this, d.Claimant);
                else
                    PlayerNotifications?.AddSalvageSankNotification(this);
            }
            return GoalStep.GoalFailed;
        }

        NotificationManager PlayerNotifications => Owner.isPlayer ? UState.Notifications : null;

        // an order that ends takes its salvage ship out of its shipyard's queue, sends one flying out or deploying back to land at its empire's nearest safe colony, or loses one changing hands as its empire leaves, takes its platform away unless it is in other hands, where it stops coming apart, and gives up its claim unless another order of its empire's works the derelict; one saved with a station's work scuttles its constructor
        public override void OnRemoved()
        {
            if (PlanetBuildingAt is { } shipyard)
                Unqueue(shipyard);
            if (!ByShip)
            {
                FinishedShip?.AI.OrderScuttleShip();
            }
            else
            {
                SalvageShip ??= FinishedShip; // built since its last turn
                if (OwnShip() is { } ship)
                    GoHome(ship);
                else if (ShipChangesHands)
                    SalvageShip.QueueTotalRemoval();
            }

            if (Platform is { Active: true } platform)
            {
                if (platform.Loyalty == Owner)
                    platform.QueueTotalRemoval();
                else
                    platform.StopDismantling(); // its new owner keeps it whole
            }

            if (Derelict is { } d && d.Claimant == Owner
                && !Owner.AI.HasGoal(g => g != this && g is SalvageDerelict { Finished: false } s && s.DerelictId == DerelictId))
                d.ReleaseClaim();
        }

        // its cost is lost, and it lands at its empire's nearest safe colony
        void GoHome(Ship ship)
        {
            if (ship.Dismantling)
                ship.StopDismantling();
            DeliverRefund.Send(Owner, ship, 0f);
        }

        // its salvage ship while it is its empire's; one destroyed, boarded, changing hands or refitted in the queue to another design is not
        Ship OwnShip() => SalvageShip is { Active: true, Dying: false, IsSalvageShip: true } ship && ship.Loyalty == Owner && !ship.LoyaltyTracker.ChangesFrom(Owner) ? ship : null;

        // boarded or passed with its empire, it is another's at its next update
        bool ShipChangesHands => SalvageShip is { Active: true, IsSalvageShip: true } ship && ship.Loyalty == Owner && ship.LoyaltyTracker.ChangesFrom(Owner);

        // the race's salvage ship goes first in the queue of the empire's safe shipyard planet nearest the derelict; with none, the order waits for one; orders placed together from the UI reach the sim thread together, and the first one holds the derelict
        GoalStep BuildShip()
        {
            if (Owner.SalvageShipDesign is not { } design || OrderOf(Owner, Derelict) != this)
                return GoalStep.GoalFailed;
            if (NearestShipyard() is not { } shipyard)
                return GoalStep.TryAgain;

            PlanetBuildingAt = shipyard;
            shipyard.Construction.Enqueue(design, QueueItemType.OrbitalUrgent, this, notifyOnEmpty: false);
            shipyard.Construction.MoveToFront(this);
            return GoalStep.GoToNextStep;
        }

        Planet NearestShipyard() => Owner.SafeSpacePorts.FindMin(p => p.Position.SqDist(BuildPosition));

        // the queue reports the ship built; its shipyard lost, or unsafe while a safe one is left, it is queued again at the nearest; taken out of the queue, the order is called off
        GoalStep WaitForShip()
        {
            Planet shipyard = PlanetBuildingAt;
            if (shipyard == null || shipyard.Owner != Owner)
                return BuildAgain();
            if (QueuedItem(shipyard) == null)
                return GoalStep.GoalFailed;
            if (!shipyard.Safe && NearestShipyard() is { } safe && safe != shipyard)
            {
                Unqueue(shipyard);
                return BuildAgain();
            }
            return GoalStep.TryAgain;
        }

        QueueItem QueuedItem(Planet shipyard)
        {
            var queue = shipyard.ConstructionQueue;
            for (int i = 0; i < queue.Count; ++i)
                if (queue[i].Goal == this)
                    return queue[i];
            return null;
        }

        // out of the queue at once, as a cancel flag left for the queue's next turn is not saved
        void Unqueue(Planet shipyard)
        {
            if (QueuedItem(shipyard) is { } item)
                shipyard.Construction.Cancel(item);
        }

        GoalStep BuildAgain()
        {
            PlanetBuildingAt = null;
            return GoalStep.RestartGoal;
        }

        // it flies to the platform's site, taking no orders and no fights on the way; one destroyed or in other hands ends the order, the player told, one being boarded once it has changed hands; one refitted in the queue to another design ends it and is a ship like any other
        GoalStep FlyToDerelict()
        {
            SalvageShip ??= FinishedShip;
            if (SalvageShip is { IsSalvageShip: false })
                return GoalStep.GoalFailed;
            if (ShipChangesHands)
                return GoalStep.TryAgain;
            if (OwnShip() is not { } ship)
                return ShipLost();
            if (ship.Position.InRadius(BuildPosition, ArrivalRadius))
                return StartDeploy(ship);
            if (ship.AI.State != AIState.MoveTo)
            {
                ship.AI.OrderMoveTo(BuildPosition, ship.Position.DirectionToTarget(BuildPosition), AIState.MoveTo, MoveOrder.Regular, this);
                ship.AI.IgnoreCombat = true;
            }
            return GoalStep.TryAgain;
        }

        GoalStep ShipLost()
        {
            PlayerNotifications?.AddSalvageShipLostNotification(this);
            return GoalStep.GoalFailed;
        }

        // it stops at the site and turns into the platform over its turns, the derelict its empire's from the start, with the trust cost of salvaging in a system where other empires have colonies
        GoalStep StartDeploy(Ship ship)
        {
            Derelict.TryClaim(Owner);
            SalvageOps.CostTrustOfColonyOwners(Owner, TargetSystem);
            ship.AI.OrderHoldPosition(ship.Position, ship.Direction);
            ship.AI.IgnoreCombat = true;
            ship.StartDismantling(DeployTurns);
            TurnsLeft = DeployTurns;
            return GoalStep.GoToNextStep;
        }

        // the turns over, the platform stands where the ship was, and the empire sees the derelict's techs from then on
        GoalStep Deploy()
        {
            if (ShipChangesHands)
                return GoalStep.TryAgain;
            if (OwnShip() is not { } ship)
                return ShipLost();
            if (--TurnsLeft > 0)
                return GoalStep.TryAgain;

            Platform = Ship.CreateShipAtPoint(UState, ShipDesign.SalvagePlatformName, Owner, ship.Position);
            if (Platform == null)
                return GoalStep.GoalFailed;
            ship.FinishDismantling();
            UState.Derelicts.ShowTechs(Derelict, Owner);
            return GoalStep.GoToNextStep;
        }

        // each turn the platform strips into the order's hold, or a recovering one works on the recovery; once its work is over it turns back into the ship, the player told of a salvage's end but not a recovery's; one lost or in other hands ends the order with what it stripped, the player told unless the roll succeeded, as the ship still comes
        GoalStep Salvage()
        {
            if (Platform is not { Active: true } platform)
            {
                if (!RecoverySucceeded)
                    PlayerNotifications?.AddSalvagePlatformDestroyedNotification(this);
                return GoalStep.GoalFailed;
            }
            if (platform.Loyalty != Owner)
                return PassingOn(platform) ? GoalStep.TryAgain : GoalStep.GoalFailed;

            if (!Finished)
            {
                Derelict.TryClaim(Owner); // takes back a claim that a call-off caught halfway by a save dropped
                if (!Recovering)
                    Strip(platform, Derelict);
                else if (Recover(platform, Derelict))
                    return Exploded(platform);
            }

            if (!Finished)
                return GoalStep.TryAgain;
            TurnsLeft = DismantleTurns;
            platform.StartDismantling(DismantleTurns);
            if (!Recovering)
                PlayerNotifications?.AddSalvageFinishedNotification(this);
            return GoalStep.GoToNextStep;
        }

        // the blast always destroys the platform beside it, and the order ends with them
        GoalStep Exploded(Ship platform)
        {
            platform.InstantKill();
            Platform = null; // its end no longer takes the platform away
            return GoalStep.GoalFailed;
        }

        // the turns over, the platform turns back into its empire's salvage ship, which brings home what it stripped as an orbital's refund is brought, and lands at its last stop; with no salvage ship, what it stripped is paid at once as credits at the tax rate; one lost with production aboard is told
        GoalStep Dismantle()
        {
            if (Platform is not { Active: true } platform)
            {
                if (Load > 0f)
                    PlayerNotifications?.AddSalvagePlatformDestroyedNotification(this);
                return GoalStep.GoalFailed;
            }
            if (platform.Loyalty != Owner)
                return PassingOn(platform) ? GoalStep.TryAgain : GoalStep.GoalFailed;
            if (--TurnsLeft > 0)
                return GoalStep.TryAgain;

            Ship ship = Owner.SalvageShipDesign is { } design ? Ship.CreateShipAtPoint(UState, design.Name, Owner, platform.Position) : null;
            platform.FinishDismantling();
            Platform = null;
            if (ship != null)
                DeliverRefund.Send(Owner, ship, Load, salvaged: true);
            else
                Owner.AddExcessCargoMoney(Load * Owner.data.TaxRate);
            return GoalStep.GoalComplete;
        }

        // a platform passed on to the empire waits for its change of hands
        bool PassingOn(Ship platform) => platform.LoyaltyTracker.ChangesTo(Owner);

        // what was stripped advances each of its derelict's techs the empire can learn by the salvage tech share of what the tech has left to research, times its share of the derelict's production, so salvage never completes one
        void AdvanceTechs(float stripped)
        {
            if (DerelictProduction <= 0f)
                return;
            float share = GlobalStats.Defaults.SalvageTechShare * stripped / DerelictProduction;
            for (int i = 0; i < Techs.Length; ++i)
            {
                if (Owner.TryGetTechEntry(Techs[i], out TechEntry tech) && CanLearn(Owner, tech))
                {
                    float points = share * (tech.TechCost * Owner.data.Traits.ResearchMultiplierForTech(tech, Owner) - tech.Progress);
                    tech.Progress += points;
                    Gained[i] += points;
                }
            }
        }

        // a tech the empire has not researched and sees on its tree
        public static bool CanLearn(Empire empire, TechEntry tech) => tech.Locked && tech.Discovered && !tech.IsHidden(empire);

        // a tech the derelict's design uses that the empire can still learn and its designs use, as anyone can tell from the ship whatever few of them it holds
        public static bool DesignHasWantedTech(Empire empire, Derelict d)
        {
            Array<string> techs = Derelict.TechsOf(d.Design);
            for (int i = 0; i < techs.Count; ++i)
                if (empire.TryGetTechEntry(techs[i], out TechEntry tech) && CanLearn(empire, tech) && tech.shipDesignsCanuseThis)
                    return true;
            return false;
        }

        // the AI's pick, from what the derelict is and never from its own draws: it recovers a ship stronger than the strongest design of that hull role it can build, or of a role it can build none of, and strips the rest
        public static bool WouldRecover(Empire empire, Derelict d)
        {
            if (!DerelictRecovery.CanRecover(d))
                return false;

            RoleName role = d.Design.HullRole;
            float best = -1f;
            IShipDesign[] designs = empire.ShipsWeCanBuildSnapshot;
            for (int i = 0; i < designs.Length; ++i)
                if (designs[i].HullRole == role)
                    best = Math.Max(best, designs[i].GetStrength(empire));
            return best < 0f || d.Design.GetStrength(empire) > best;
        }

        // from any thread: an empire sees a derelict's techs once its salvage platform has stood at it
        public static bool SeesTechs(Empire empire, Derelict d) => d.ShowsTechsTo(empire);

        public enum Stage { None, ShipOrdered, ShipOnItsWay, Deploying, Salvaging, BreakingIn, Recovering }

        // from any thread: how far the empire's salvage or recovery of the derelict has come, with its order, or Salvaging or Recovering while another empire holds it, with its claimant as read once
        public static Stage StageOf(Empire empire, Derelict d, out Empire claimant, out SalvageDerelict order)
        {
            claimant = d.Claimant;
            order = null;
            if (claimant != null && claimant != empire)
                return OrderOf(claimant, d) is { Recovering: true } ? Stage.Recovering : Stage.Salvaging;
            if ((order = OrderOf(empire, d)) != null)
                return order.Platform == null ? order.ShipStage : order.BreakingIn ? Stage.BreakingIn : order.Recovering ? Stage.Recovering : Stage.Salvaging;
            return claimant == empire ? Stage.Salvaging : Stage.None;
        }

        Stage ShipStage => Deploying ? Stage.Deploying : StepName == nameof(FlyToDerelict) ? Stage.ShipOnItsWay : Stage.ShipOrdered;

        // from any thread: production the empire's platforms strip from a derelict in a turn, raised by its Salvage Speed research, and pirates' by their level
        public static float SalvageSpeed(Empire empire) => GlobalStats.Defaults.SalvageSpeed * empire.data.SalvageSpeedMultiplier;

        // a turn's salvage speed of what is left goes into the order's hold, which has no limit, and advances its techs
        void Strip(Ship platform, Derelict d)
        {
            float stripped = UState.Derelicts.Strip(d, platform, SalvageSpeed(Owner));
            if (stripped > 0f)
            {
                Load += stripped;
                AdvanceTechs(stripped);
                SendShuttles(platform, d, CargoShuttles.SalvageShuttlesFor(stripped));
            }
            Emptied = d.Stripped;
        }

        // a turn of breaking in, which steadies the derelict, then a turn's work at the recovery speed, the roll coming with the last of it; the shuttles fly a full turn's while it breaks in, and none with the last of the work; true when the derelict exploded
        bool Recover(Ship platform, Derelict d)
        {
            if (RecoverySucceeded)
                return false;
            float speed = DerelictRecovery.Speed(Owner);
            if (BreakingIn)
            {
                ++BreakInDone;
                d.SteadyTo(BreakInDone / (float)DerelictRecovery.BreakInTurns);
                d.ShowRecoveryWork(platform, DerelictRecovery.Progress(Owner, d, RecoveryWork));
                UState.Derelicts.SoundBreakIn(d);
                SendShuttles(platform, d, CargoShuttles.RecoveryShuttlesFor(speed));
                return false;
            }
            d.SteadyTo(1f); // at once after a break-in of no turns
            float work = DerelictRecovery.Work(Owner, d);
            if (RecoveryWork < work && speed > 0f)
            {
                float before = RecoveryWork;
                RecoveryWork = Math.Min(RecoveryWork + speed, work); // the work exactly, once all is done
                if (RecoveryWork < work) // the turn that finishes it shows the roll's outcome instead
                {
                    d.ShowRecoveryWork(platform, DerelictRecovery.Progress(Owner, d, RecoveryWork));
                    SendShuttles(platform, d, CargoShuttles.RecoveryShuttlesFor(RecoveryWork - before));
                }
            }
            return RecoveryWork >= work && Roll(platform, d);
        }

        // a success raises the derelict for its ship, which ends the platform's work once it takes the derelict's place, the player told then; a failure breaks the derelict apart and ends the platform's work, and a catastrophe blows it up, the player told at once; true for the catastrophe; the work's parts rise with the derelict as they shrink away, fade out as a finished build's, or go at once in the blast
        bool Roll(Ship platform, Derelict d)
        {
            switch (DerelictRecovery.Roll(Owner, d))
            {
                case DerelictRecovery.Outcome.Recovered:
                    RecoverySucceeded = true;
                    UState.Derelicts.Rise(d, Owner);
                    UState.Screen.RiseConstructionParts(platform.Id, Derelict.RiseSeconds, d.RestDepth / Derelict.RiseSeconds);
                    return false;
                case DerelictRecovery.Outcome.BrokenApart:
                    RecoveryFailed = true;
                    UState.Derelicts.BreakApart(d);
                    UState.Screen.ResetConstructionParts(platform.Id);
                    PlayerNotifications?.AddRecoveryFailedNotification(this, exploded: false);
                    return false;
                default:
                    UState.Derelicts.Explode(d);
                    UState.Screen.RemoveConstructionParts(platform.Id);
                    PlayerNotifications?.AddRecoveryFailedNotification(this, exploded: true);
                    return true;
            }
        }

        // they show only where the player sees the platform, as a supply ship's do
        void SendShuttles(Ship platform, Derelict d, int shuttles)
        {
            if (platform.InFrustum && UState.IsPlanetViewOrCloser && platform.InPlayerSensorRange)
                UState.Screen.CargoShuttles.SendToHullAndBack(platform, new Vector3(d.Position, d.Z), d.Radius, Owner.EmpireColor, shuttles);
        }

        // from any thread: the order a salvage platform works for, or null
        public static SalvageDerelict OfPlatform(Ship platform)
            => platform.Loyalty.AI.FindGoal(g => g is SalvageDerelict s && s.Platform == platform) as SalvageDerelict;

        // from any thread: the order a salvage ship flies out or deploys for, or null, as one bringing its load home is a delivery's
        public static SalvageDerelict OfShip(Ship ship)
            => ship.Loyalty.AI.FindGoal(g => g is SalvageDerelict s && s.SalvageShip == ship && s.Platform == null) as SalvageDerelict;
    }
}
