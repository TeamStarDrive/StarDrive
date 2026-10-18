using System;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.Commands.Goals
{
    // an empire's order to salvage one derelict: a salvage unit of its station in the derelict's system launches a constructor, which builds a salvage platform beside it
    [StarDataType]
    public class SalvageDerelict : DeepSpaceBuildGoal
    {
        // the platform's construction is never below this, times the production pace
        public const float MinPlatformConstruction = 100f;
        // the platform's site is this far out from the derelict's radius
        public const float PlatformClearance = 100f;

        [StarData] public int DerelictId;
        [StarData] public Ship Platform;
        // its platform stripped the derelict empty, and holds its load on
        [StarData] public bool Emptied { get; private set; }
        Derelict Found;

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
                Salvage
            };
        }

        public SalvageDerelict(Empire owner, Derelict derelict) : this(owner)
        {
            DerelictId = derelict.Id;
            Initialize(ShipDesign.SalvagePlatformName, Vector2.Zero, derelict.System);
            Found = derelict;
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

        public static SalvageDerelict OrderOf(Empire empire, Derelict d)
            => empire.AI.FindGoal(g => g is SalvageDerelict s && s.DerelictId == d.Id) as SalvageDerelict;

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

        // the constructor a small Deep Space Build job gets, the cheapest one with auto pick, or empty without one
        public static string ConstructorName(Empire empire)
        {
            if (!ResourceManager.Ships.GetDesign(ShipDesign.SalvagePlatformName, out IShipDesign platform))
                return "";
            return ShipBuilder.PickConstructor(empire, PlatformConstruction(empire, platform), GetCheapest: true)?.Name ?? "";
        }

        // the derelict panel's and the derelicts window's Salvage button; false when the empire cannot order it
        public static bool Order(Empire empire, Derelict d)
        {
            if (WhyNoOrder(empire, d) != Block.None)
                return false;
            empire.AI.AddGoalAndEvaluate(new SalvageDerelict(empire, d));
            return true;
        }

        // the button's second click calls the order off on the sim thread, where its claim is given up
        public static void Cancel(Empire empire, Derelict d)
        {
            if (empire.Universe.Screen is { IsOnSimThread: false } screen)
            {
                screen.RunOnSimThread(() => Cancel(empire, d));
                return;
            }
            if (OrderOf(empire, d) is { } order)
                empire.AI.RemoveGoal(order);
            else if (d.Claimant == empire) // a claim kept without its order, as a save can catch it
                d.ReleaseClaim();
        }

        // an order stands while salvage ops are on and its derelict can be salvaged, unless another empire claimed it first, and once its platform has stripped it empty
        bool Stands => Owner.Universe.P.SalvageOps
                       && (Emptied || Derelict is { CanBeSalvaged: true } d && (d.Claimant == null || d.Claimant == Owner));

        bool Launched => Step > 0;

        protected override GoalStep? PreEvaluate() => Stands ? null : GoalStep.GoalFailed;

        // an order that ends calls its constructor home, or scuttles it with its station lost, takes its platform away unless it is in other hands, and gives up its claim unless another order of its empire's holds the derelict
        public override void OnRemoved()
        {
            if (Constructor() is { } constructor)
            {
                if (constructor.Mothership is { Active: true } station && station.Loyalty == Owner)
                {
                    constructor.AI.OrderReturnToHangarDeferred();
                }
                else
                {
                    constructor.Mothership?.OnLaunchedShipDie(constructor);
                    constructor.AI.OrderScuttleShip();
                }
            }
            if (Platform is { Active: true } platform && platform.Loyalty == Owner)
                platform.QueueTotalRemoval();

            if (Derelict is { } d && d.Claimant == Owner
                && !Owner.AI.HasGoal(g => g != this && g is SalvageDerelict s && s.DerelictId == DerelictId))
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

        // each turn the platform strips what its hold has room for, and waits with its hold full; one lost or in other hands ends the order
        GoalStep Salvage()
        {
            if (Platform is not { Active: true } platform || platform.Loyalty != Owner)
                return GoalStep.GoalFailed;
            if (!Emptied && !HoldFull(platform))
                Strip(platform, Derelict);
            return GoalStep.TryAgain;
        }

        // it strips only what its hold took
        void Strip(Ship platform, Derelict d)
        {
            float wanted = Math.Min(Math.Min(GlobalStats.Defaults.SalvageSpeed, platform.CargoRoom), d.ProductionLeft);
            float stripped = UState.Derelicts.Strip(d, Owner, platform, platform.LoadProduction(wanted));
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

        // less room than this left in a platform's hold counts as full
        const float FullHoldRoom = 0.01f;

        public static bool HoldFull(Ship platform) => platform.CargoRoom < FullHoldRoom;

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

        // launched orders hold the station's units first, then waiting ones as they were placed, and a second order for the same derelict ends; the goals are evaluated last to first
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
                    if (s.DerelictId == DerelictId)
                        return false;
                    ++ahead;
                }
            }
            return ahead < LiveUnits(station);
        }
    }
}
