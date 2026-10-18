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
        [StarData] int DismantleTurnsLeft;
        // its derelict's techs and whole production, kept for the loads sent once the derelict has sunk away
        [StarData] public string[] Techs { get; private set; } = Empty<string>.Array;
        [StarData] public float DerelictProduction { get; private set; }
        Derelict Found;

        // its platform's work is over: it sends its station all it holds, then takes itself apart
        public bool Finished => Emptied || CalledOff;

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

        public SalvageDerelict(Empire owner, Derelict derelict) : this(owner)
        {
            DerelictId = derelict.Id;
            Techs = derelict.Techs;
            DerelictProduction = derelict.Production;
            Initialize(ShipDesign.SalvagePlatformName, Vector2.Zero, derelict.System);
            Found = derelict;
        }

        // an order passed on with its platform, at work or coming apart
        SalvageDerelict(Empire owner, SalvageDerelict passed) : this(owner)
        {
            StarDateAdded = passed.StarDateAdded;
            DerelictId = passed.DerelictId;
            Techs = passed.Techs;
            DerelictProduction = passed.DerelictProduction;
            Initialize(ShipDesign.SalvagePlatformName, passed.StaticBuildPos, passed.TargetSystem);
            Platform = passed.Platform;
            Emptied = passed.Emptied;
            CalledOff = passed.CalledOff;
            DismantleTurnsLeft = passed.DismantleTurnsLeft;
            ChangeToStep(passed.Dismantling ? Dismantle : Salvage);
        }

        // on the sim thread, before the goals of an empire that leaves are cleared: its built platforms' orders and claims pass to the empire absorbing it or to its rebels, the platforms changing hands a step later
        public static void PassOn(Empire from, Empire to)
        {
            var goals = from.AI.Goals;
            for (int i = goals.Count - 1; i >= 0; --i)
            {
                if (goals[i] is not SalvageDerelict { Platform: { Active: true } platform } order || platform.Loyalty != from)
                    continue;

                to.AI.AddGoal(new SalvageDerelict(to, order));
                if (order.Derelict is { } d && d.Claimant == from)
                {
                    d.ReleaseClaim();
                    d.TryClaim(to);
                }
                order.Platform = null; // its end no longer takes the platform away
                from.AI.RemoveGoal(order);
            }
        }

        public enum Block { None, Ordered, ClaimedByOther, NoStation, NoFreeUnit, Gone }

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

        // the derelict panel's and the derelicts window's Salvage button; false when the empire cannot order it
        public static bool Order(Empire empire, Derelict d)
        {
            if (WhyNoOrder(empire, d) != Block.None)
                return false;
            empire.AI.AddGoalAndEvaluate(new SalvageDerelict(empire, d));
            return true;
        }

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

        // an order stands while salvage ops are on and its derelict can be salvaged, unless another empire claimed it first or another order of its empire's has a platform at work on it, and once its platform's work is over
        bool Stands => Owner.Universe.P.SalvageOps
                       && (Finished || Derelict is { CanBeSalvaged: true } d && (d.Claimant == null || d.Claimant == Owner) && !GivesWay);

        // an order not yet built gives way to one passed on to its empire with a platform at work on the derelict
        bool GivesWay => Platform == null
                         && Owner.AI.HasGoal(g => g != this && g is SalvageDerelict { Finished: false, Platform: { Active: true } } s
                                                  && s.DerelictId == DerelictId);

        bool Launched => Step > 0;

        protected override GoalStep? PreEvaluate() => Stands ? null : GoalStep.GoalFailed;

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
            Ship station = SalvageOps.StationIn(Owner, TargetSystem);
            if (station == null || !HoldsAUnit(station))
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
            if (SalvageOps.StationIn(Owner, TargetSystem) == null)
                return GoalStep.GoalFailed;

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

            constructor.Mothership?.OnLaunchedShipDie(constructor);
            constructor.Mothership = null;
            FinishedShip = null;
            return null;
        }

        // each turn the platform strips what its hold has room for, or with its hold full sends it to the station, which takes what fits; once its work is over it sends all it holds, then takes itself apart; one lost or in other hands ends the order, and a lost station takes its platform with it
        GoalStep Salvage()
        {
            if (Platform is not { Active: true } platform)
                return GoalStep.GoalFailed;
            if (platform.Loyalty != Owner)
                return PassingOn(platform) ? GoalStep.TryAgain : GoalStep.GoalFailed;
            if (SalvageOps.StationIn(Owner, TargetSystem) is not { } station)
                return GoalStep.GoalFailed;

            if (Finished || HoldFull(platform))
                SendLoad(platform, station);
            else
                Strip(platform, Derelict);

            if (!Finished || platform.GetProduction() > 0f)
                return GoalStep.TryAgain;
            DismantleTurnsLeft = DismantleTurns;
            platform.StartDismantling();
            return GoalStep.GoToNextStep;
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

        // as much of its hold as the station has room for, its shuttles launching where the player sees either end
        void SendLoad(Ship platform, Ship station)
        {
            float sent = station.LoadProduction(Math.Min(platform.GetProduction(), station.CargoRoom));
            platform.UnloadProduction(sent);
            if (sent <= 0f)
                return;
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
                    tech.Progress += share * (tech.TechCost * Owner.data.Traits.ResearchMultiplierForTech(tech, Owner) - tech.Progress);
            }
        }

        // a tech the empire has not researched and sees on its tree
        public static bool CanLearn(Empire empire, TechEntry tech) => tech.Locked && tech.Discovered && !tech.IsHidden(empire);

        static bool SeenByPlayer(Ship ship) => ship.InFrustum && ship.InPlayerSensorRange;

        // it strips only what its hold took
        void Strip(Ship platform, Derelict d)
        {
            float wanted = Math.Min(Math.Min(GlobalStats.Defaults.SalvageSpeed, platform.CargoRoom), d.ProductionLeft);
            float stripped = UState.Derelicts.Strip(d, platform, platform.LoadProduction(wanted));
            if (stripped > 0f)
                SendShuttles(platform, d, stripped);
            Emptied = d.Stripped;
        }

        // they show only where the player sees the platform, as a supply ship's do
        void SendShuttles(Ship platform, Derelict d, float stripped)
        {
            if (platform.InFrustum && UState.IsPlanetViewOrCloser && platform.InPlayerSensorRange)
                UState.Screen.CargoShuttles.SendToHullAndBack(platform, new Vector3(d.Position, d.Z), d.Radius, Owner.EmpireColor,
                                                              CargoShuttles.SalvageShuttlesFor(stripped));
        }

        // less room than this left in a platform's or a station's hold counts as full
        const float FullHoldRoom = 0.01f;

        public static bool HoldFull(Ship ship) => ship.CargoRoom < FullHoldRoom;

        // from any thread: with its hold full or its work over, its load waits for room in its station
        public bool LoadWaitsForStation(Ship platform)
            => (Finished || HoldFull(platform)) && platform.GetProduction() > 0f
               && SalvageOps.StationIn(Owner, TargetSystem) is { } station && HoldFull(station);

        // from any thread: the order a salvage platform works for, or null
        public static SalvageDerelict OfPlatform(Ship platform)
            => platform.Loyalty.AI.FindGoal(g => g is SalvageDerelict s && s.Platform == platform) as SalvageDerelict;

        static ShipModule FreeBay(Ship station)
        {
            ShipModule[] bays = station.Carrier.AllHangars;
            for (int i = 0; i < bays.Length; ++i)
            {
                ShipModule bay = bays[i];
                if (bay.IsSalvageBay && bay.Active && bay.HangarTimer <= 0f && !bay.IsHangarShipActive)
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
