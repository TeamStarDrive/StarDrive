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
    // an empire's order to salvage one derelict: a salvage unit of its station in the derelict's system launches a constructor, which builds a salvage platform beside it; the platform strips it for the station, then takes itself apart
    [StarDataType]
    public class SalvageDerelict : DeepSpaceBuildGoal
    {
        // the platform's construction is never below this, times the production pace
        public const float MinPlatformConstruction = 100f;
        // the platform's site is this far out from the derelict's radius
        public const float PlatformClearance = 100f;

        // a platform takes itself apart over two turns
        public const int DismantleTurns = 2;

        [StarData] public int DerelictId;
        [StarData] public Ship Platform;
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
        [StarData] int DismantleTurnsLeft;
        // its derelict's techs and whole production, kept for the loads sent once the derelict has sunk away
        [StarData] public string[] Techs { get; private set; } = Empty<string>.Array;
        [StarData] public float DerelictProduction { get; private set; }
        // the research its loads have given each of those techs, by the techs' order; a passed order starts again from none
        [StarData] public float[] Gained { get; private set; } = Empty<float>.Array;
        // the production its loads have brought its station; a passed order starts again from none
        [StarData] public float Delivered { get; private set; }
        // its derelict's design name and hull icon, kept for the notifications once the derelict has sunk away
        [StarData] public string DerelictName { get; private set; }
        [StarData] public string DerelictIcon { get; private set; }
        Derelict Found;
        // its station as last found, for the notification of its loss
        Ship LastStation;

        // its platform's work is over: it sends its station all it holds, then takes itself apart
        public bool Finished => Emptied || CalledOff || RecoveryFailed || Recovered;

        // from any thread: the recovered ship has taken the derelict's place, which has left the list
        public bool Recovered => RecoverySucceeded && UState.Derelicts.Find(DerelictId) == null;

        public bool Dismantling => DismantleTurnsLeft > 0;

        public Derelict Derelict => Found ??= UState.Derelicts.Find(DerelictId);

        // no build marker on the map: the derelict's Salvage button calls the order off
        public override bool IsDeploymentGoal => false;

        [StarDataConstructor]
        public SalvageDerelict(Empire owner) : base(GoalType.SalvageDerelict, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                LaunchConstructor,
                BuildPlatform,
                Salvage,
                Dismantle
            };
        }

        public SalvageDerelict(Empire owner, Derelict derelict, bool recover = false) : this(owner)
        {
            DerelictId = derelict.Id;
            Recovering = recover;
            Techs = derelict.Techs;
            Gained = new float[Techs.Length];
            DerelictProduction = derelict.Production;
            DerelictName = derelict.Design.Name;
            DerelictIcon = derelict.Design.BaseHull?.IconPath;
            Initialize(ShipDesign.SalvagePlatformName, Vector2.Zero, derelict.System);
            Found = derelict;
        }

        // an order passed on with its platform, at work or coming apart
        SalvageDerelict(Empire owner, SalvageDerelict passed) : this(owner)
        {
            StarDateAdded = passed.StarDateAdded;
            DerelictId = passed.DerelictId;
            Techs = passed.Techs;
            Gained = new float[Techs.Length];
            DerelictProduction = passed.DerelictProduction;
            DerelictName = passed.DerelictName;
            DerelictIcon = passed.DerelictIcon;
            Initialize(ShipDesign.SalvagePlatformName, passed.StaticBuildPos, passed.TargetSystem);
            Platform = passed.Platform;
            Emptied = passed.Emptied;
            CalledOff = passed.CalledOff;
            Recovering = passed.Recovering;
            RecoveryWork = passed.RecoveryWork;
            BreakInDone = passed.BreakInDone;
            RecoveryFailed = passed.RecoveryFailed;
            RecoverySucceeded = passed.RecoverySucceeded;
            DismantleTurnsLeft = passed.DismantleTurnsLeft;
            ChangeToStep(passed.Dismantling ? Dismantle : Salvage);
        }

        // on the sim thread, before the goals of an empire that leaves are cleared: its built platforms' orders pass to the empire absorbing it or to its rebels, with the claims of those still at work, the platforms changing hands a step later
        public static void PassOn(Empire from, Empire to)
        {
            var goals = from.AI.Goals;
            for (int i = goals.Count - 1; i >= 0; --i)
            {
                if (goals[i] is not SalvageDerelict { Platform: { Active: true } platform } order || platform.Loyalty != from)
                    continue;

                to.AI.AddGoal(new SalvageDerelict(to, order));
                if (!order.Finished && order.Derelict is { } d && d.Claimant == from) // a finished order's derelict can be claimed by an order not yet built, which ends
                {
                    d.ReleaseClaim();
                    d.TryClaim(to);
                    d.PassRecovery(from, to);
                }
                order.Platform = null; // its end no longer takes the platform away
                from.AI.RemoveGoal(order);
            }
        }

        public enum Block { None, Ordered, ClaimedByOther, NoStation, NoFreeUnit, Gone, NotAShip }

        // from any thread: why the empire cannot order the derelict salvaged, Ordered while its order stands, or None when it can
        public static Block WhyNoOrder(Empire empire, Derelict d)
        {
            if (!empire.Universe.P.SalvageOps || !d.CanBeSalvaged || !d.IsKnownBy(empire))
                return Block.Gone;
            if (d.Claimant != null && d.Claimant != empire)
                return Block.ClaimedByOther;
            if (d.Claimant == empire || OrderOf(empire, d) != null)
                return Block.Ordered;

            Ship station = SalvageOps.StationIn(empire, d.System);
            if (station == null)
                return Block.NoStation;
            return LiveUnits(station) > NumOrdersIn(empire, d.System) ? Block.None : Block.NoFreeUnit;
        }

        // from any thread: as for salvage, but a station, which can only be salvaged, is NotAShip
        public static Block WhyNoRecovery(Empire empire, Derelict d) => RecoveryBlock(WhyNoOrder(empire, d), d);

        public static Block RecoveryBlock(Block salvageBlock, Derelict d)
            => salvageBlock is Block.Gone or Block.NoStation || DerelictRecovery.CanRecover(d) ? salvageBlock : Block.NotAShip;

        // an order whose platform's work is over is no longer the derelict's, though it holds its unit until its platform is gone
        public static SalvageDerelict OrderOf(Empire empire, Derelict d)
            => empire.AI.FindGoal(g => g is SalvageDerelict { Finished: false } s && s.DerelictId == d.Id) as SalvageDerelict;

        // every order of the empire's in the system takes a unit of its one station there
        static int NumOrdersIn(Empire empire, SolarSystem system)
            => empire.AI.CountGoals(g => g is SalvageDerelict s && s.TargetSystem == system);

        public static int LiveUnits(Ship station)
        {
            return station.Modules.Count(m => m.Salvage && m.Active);
        }

        // the platform's cost to the empire, never below 100, both times the production pace
        public static float PlatformConstruction(Empire empire, IShipDesign platform)
            => Math.Max(platform.GetCost(empire), MinPlatformConstruction * empire.Universe.ProductionPace);

        // the empire's constructor, or with auto pick the cheapest whatever the platform costs, a job of no cost being small for every constructor; empty without one
        public static string ConstructorName(Empire empire)
        {
            if (!ResourceManager.Ships.GetDesign(ShipDesign.SalvagePlatformName, out IShipDesign _))
                return "";
            return ShipBuilder.PickConstructor(empire, 0f, GetCheapest: true)?.Name ?? "";
        }

        // the derelict panel's and the derelicts window's Salvage and Recover buttons; false when the empire cannot order it
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

        // the button's second click calls the order off on the sim thread, where its claim is given up; a platform standing sends what it holds and takes itself apart
        public static void Cancel(Empire empire, Derelict d)
        {
            if (empire.Universe.Screen is { IsOnSimThread: false } screen)
            {
                screen.RunOnSimThread(() => Cancel(empire, d));
                return;
            }
            if (OrderOf(empire, d) is { } order)
            {
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

        // an order not yet built gives way to one passed on to its empire with a platform at work on the derelict
        bool GivesWay => Platform == null
                         && Owner.AI.HasGoal(g => g != this && g is SalvageDerelict { Finished: false, Platform: { Active: true } } s
                                                  && s.DerelictId == DerelictId);

        bool Launched => Step > 0;

        // an order that no longer stands ends; the player is told when a rival claimed its derelict first, or it sank before the claim
        protected override GoalStep? PreEvaluate()
        {
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

        // an order that ends calls its constructor home, or scuttles it with its station lost or its empire gone, takes its platform away unless it is in other hands, where it stops coming apart, and gives up its claim unless another order of its empire's works the derelict
        public override void OnRemoved()
        {
            if (Constructor() is { } constructor)
            {
                if (constructor.Mothership is { Active: true } station && station.Loyalty == Owner && !Owner.IsDefeated)
                {
                    constructor.AI.OrderReturnToHangarDeferred();
                }
                else
                {
                    constructor.Mothership?.OnLaunchedShipDie(constructor);
                    constructor.AI.OrderScuttleShip();
                }
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

        // from the constructor's deploy: the platform takes over the derelict, and the ordnance its constructor took is spent
        public void OnPlatformBuilt(Ship platform, Ship constructor)
        {
            Platform = platform;
            constructor.Mothership?.OnLaunchedShipDie(constructor);
        }

        // a free salvage unit of the station launches the constructor when the station has the ordnance for it
        GoalStep LaunchConstructor()
        {
            Ship station = Station();
            if (station == null)
                return StationLost();
            if (!HoldsAUnit(station))
                return GoalStep.GoalFailed;

            ShipModule bay = FreeBay(station);
            Ship constructor = bay != null ? Ship.CreateShipFromHangar(UState, bay, Owner, station.Position, station) : null;
            if (constructor == null)
                return GoalStep.TryAgain;

            station.OnShipLaunched(constructor, bay);
            FinishedShip = constructor;
            if (StaticBuildPos == Vector2.Zero)
                StaticBuildPos = Derelict.RestPosition + MathExt.PointOnCircle(Owner.Random.Float(0f, 360f), Derelict.Radius + PlatformClearance);
            constructor.AI.OrderDeepSpaceBuild(this, PlatformConstruction(Owner, ToBuild), ToBuild.Grid.Radius);
            return GoalStep.GoToNextStep;
        }

        // the derelict becomes the empire's as its constructor starts to build; one lost on its way is launched again
        GoalStep BuildPlatform()
        {
            if (Platform != null)
                return GoalStep.GoToNextStep;
            if (Constructor() is not { } constructor)
                return GoalStep.RestartGoal;
            if (Station() == null)
                return StationLost();

            if (constructor.Position.InRadius(BuildPosition, ConstructionShip.ConstructingDistance))
            {
                Derelict.TryClaim(Owner); // Stands holds off another empire's claim
                constructor.Construction.TryConstruct(BuildPosition);
            }
            return GoalStep.TryAgain;
        }

        // the order's constructor while it flies or builds; one boarded on its way is its boarder's ship now, and frees its unit
        Ship Constructor()
        {
            Ship constructor = FinishedShip;
            if (constructor == null || constructor.Loyalty == Owner)
                return constructor;

            LetGo(constructor.Mothership, constructor);
            FinishedShip = null;
            return null;
        }

        // a constructor in other hands is no longer its station's, and its unit is free, with the ordnance it took written off
        static void LetGo(Ship station, Ship constructor)
        {
            station?.OnLaunchedShipDie(constructor);
            constructor.Mothership = null;
        }

        // each turn the platform strips what its hold has room for, or with its hold full sends it to the station, which takes what fits; a recovering one strips nothing, first sends all it holds, then works on the recovery; once its work is over it sends all it holds, then takes itself apart, the player told of a salvage's end; one lost or in other hands ends the order, and a lost station takes its platform with it
        GoalStep Salvage()
        {
            if (Platform is not { Active: true } platform)
            {
                PlayerNotifications?.AddSalvagePlatformDestroyedNotification(this);
                return GoalStep.GoalFailed;
            }
            if (platform.Loyalty != Owner)
                return PassingOn(platform) ? GoalStep.TryAgain : GoalStep.GoalFailed;
            if (Station() is not { } station)
                return StationLost();

            if (!Finished)
                Derelict.TryClaim(Owner); // takes back a claim that a call-off caught halfway by a save dropped
            if (Finished || HoldFull(platform) || Recovering && platform.GetProduction() > 0f)
                SendLoad(platform, station);
            else if (Recovering && Recover(platform, Derelict))
                return Exploded(platform);
            else if (!Recovering)
                Strip(platform, Derelict);

            if (!Finished || platform.GetProduction() > 0f)
                return GoalStep.TryAgain;
            DismantleTurnsLeft = DismantleTurns;
            platform.StartDismantling();
            if (!RecoveryFailed && !RecoverySucceeded)
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

        // it is gone once its turns are over, freeing its unit
        GoalStep Dismantle()
        {
            if (Platform is not { Active: true } platform)
                return GoalStep.GoalFailed;
            if (platform.Loyalty != Owner)
                return PassingOn(platform) ? GoalStep.TryAgain : GoalStep.GoalFailed;
            if (SalvageOps.StationIn(Owner, TargetSystem) == null)
                return GoalStep.GoalFailed;
            if (--DismantleTurnsLeft > 0)
                return GoalStep.TryAgain;
            platform.FinishDismantling();
            return GoalStep.GoalComplete;
        }

        // a platform passed on to the empire waits for its change of hands
        bool PassingOn(Ship platform) => platform.LoyaltyTracker.ChangesTo(Owner);

        Ship Station() => SalvageOps.StationIn(Owner, TargetSystem) is { } station ? LastStation = station : null;

        // a station lost ends the order, and the player is told unless the station was scrapped or scuttled, which leaves its scuttle timer set
        GoalStep StationLost()
        {
            bool scrapped = LastStation != null && LastStation.ScuttleTimer != -1f;
            if (!scrapped)
                PlayerNotifications?.AddSalvageStationLostNotification(this, LastStation);
            return GoalStep.GoalFailed;
        }

        // as much of its hold as the station has room for, its shuttles launching where the player sees either end
        void SendLoad(Ship platform, Ship station)
        {
            float sent = station.LoadProduction(Math.Min(platform.GetProduction(), station.CargoRoom));
            platform.UnloadProduction(sent);
            if (sent <= 0f)
                return;
            Delivered += sent;
            AdvanceTechs(sent);
            if (UState.IsPlanetViewOrCloser && (SeenByPlayer(platform) || SeenByPlayer(station)))
                UState.Screen.CargoShuttles.SendLoad(platform, station, Owner.EmpireColor, CargoShuttles.LoadShuttlesFor(sent));
        }

        // a load advances each of its derelict's techs the empire can learn by the salvage tech share of what the tech has left to research, times the load's share of the derelict's production, so salvage never completes one
        void AdvanceTechs(float sent)
        {
            if (DerelictProduction <= 0f)
                return;
            float share = GlobalStats.Defaults.SalvageTechShare * sent / DerelictProduction;
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

        // from any thread: an empire sees a derelict's techs while it has a salvage station built in the derelict's system, and from then on
        public static bool SeesTechs(Empire empire, Derelict d) => d.ShowsTechsTo(empire) || SalvageOps.StationIn(empire, d.System) != null;

        public enum Stage { None, PlatformOrdered, BuildingPlatform, Salvaging, BreakingIn, Recovering }

        // from any thread: how far the empire's salvage or recovery of the derelict has come, with its order, or Salvaging or Recovering while another empire holds it, with its claimant as read once
        public static Stage StageOf(Empire empire, Derelict d, out Empire claimant, out SalvageDerelict order)
        {
            claimant = d.Claimant;
            order = null;
            if (claimant != null && claimant != empire)
                return OrderOf(claimant, d) is { Recovering: true } ? Stage.Recovering : Stage.Salvaging;
            if ((order = OrderOf(empire, d)) != null)
                return !order.Launched ? Stage.PlatformOrdered : order.Platform == null ? Stage.BuildingPlatform
                     : order.BreakingIn ? Stage.BreakingIn : order.Recovering ? Stage.Recovering : Stage.Salvaging;
            return claimant == empire ? Stage.Salvaging : Stage.None;
        }

        static bool SeenByPlayer(Ship ship) => ship.InFrustum && ship.InPlayerSensorRange;

        // it strips only what its hold took
        void Strip(Ship platform, Derelict d)
        {
            float wanted = Math.Min(Math.Min(GlobalStats.Defaults.SalvageSpeed, platform.CargoRoom), d.ProductionLeft);
            float stripped = UState.Derelicts.Strip(d, platform, platform.LoadProduction(wanted));
            if (stripped > 0f)
                SendShuttles(platform, d, CargoShuttles.SalvageShuttlesFor(stripped));
            Emptied = d.Stripped;
        }

        // a turn of breaking in, which steadies the derelict, then a turn's work at the recovery speed, the roll coming with the last of it; the shuttles fly a full turn's while it breaks in, and none with the last of the work; true when the derelict exploded
        bool Recover(Ship platform, Derelict d)
        {
            if (RecoverySucceeded)
                return false;
            float speed = DerelictRecovery.Speed;
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

        // a success raises the derelict for its ship, which ends the platform's work once it takes the derelict's place; a failure breaks the derelict apart and ends the platform's work, and a catastrophe blows it up; true for the catastrophe; the work's parts rise with the derelict as they shrink away, fade out as a finished build's, or go at once in the blast
        bool Roll(Ship platform, Derelict d)
        {
            switch (DerelictRecovery.Roll(Owner, d, Owner.Random))
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
                    return false;
                default:
                    UState.Derelicts.Explode(d);
                    UState.Screen.RemoveConstructionParts(platform.Id);
                    return true;
            }
        }

        // they show only where the player sees the platform, as a supply ship's do
        void SendShuttles(Ship platform, Derelict d, int shuttles)
        {
            if (platform.InFrustum && UState.IsPlanetViewOrCloser && platform.InPlayerSensorRange)
                UState.Screen.CargoShuttles.SendToHullAndBack(platform, new Vector3(d.Position, d.Z), d.Radius, Owner.EmpireColor, shuttles);
        }

        // less room than this left in a platform's or a station's hold counts as full
        const float FullHoldRoom = 0.01f;

        public static bool HoldFull(Ship ship) => ship.CargoRoom < FullHoldRoom;

        // from any thread: with its hold full, its work over or a recovery begun, its load waits for room in its station
        public bool LoadWaitsForStation(Ship platform)
            => (Finished || Recovering || HoldFull(platform)) && platform.GetProduction() > 0f
               && SalvageOps.StationIn(Owner, TargetSystem) is { } station && HoldFull(station);

        // from any thread: the order a salvage platform works for, or null
        public static SalvageDerelict OfPlatform(Ship platform)
            => platform.Loyalty.AI.FindGoal(g => g is SalvageDerelict s && s.Platform == platform) as SalvageDerelict;

        // a bay whose constructor is in other hands, as one boarded on its way home after its order ended, lets it go here
        static ShipModule FreeBay(Ship station)
        {
            ShipModule[] bays = station.Carrier.AllHangars;
            for (int i = 0; i < bays.Length; ++i)
            {
                ShipModule bay = bays[i];
                if (!bay.IsSalvageBay || !bay.Active)
                    continue;
                if (bay.TryGetHangarShipActive(out Ship launched) && launched.Loyalty != station.Loyalty)
                    LetGo(station, launched);
                if (bay.HangarTimer <= 0f && !bay.IsHangarShipActive)
                    return bay;
            }
            return null;
        }

        // launched orders hold the station's units first, then waiting ones as they were placed, and a second order for the same derelict ends, unless the first one's work is over; the goals are evaluated last to first
        bool HoldsAUnit(Ship station)
        {
            int ahead = 0;
            bool placedBefore = true;
            var goals = Owner.AI.Goals;
            for (int i = 0; i < goals.Count; ++i)
            {
                if (goals[i] == this)
                {
                    placedBefore = false;
                    continue;
                }
                if (goals[i] is SalvageDerelict s && s.TargetSystem == TargetSystem && s.Stands && (placedBefore || s.Launched))
                {
                    if (s.DerelictId == DerelictId && !s.Finished)
                        return false;
                    ++ahead;
                }
            }
            return ahead < LiveUnits(station);
        }
    }
}
