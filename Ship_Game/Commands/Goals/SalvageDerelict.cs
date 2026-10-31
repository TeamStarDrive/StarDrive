using System;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.ExpansionAI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.Commands.Goals
{
    // an empire's order to salvage one derelict: it joins a salvage ship's mission in the derelict's system, whose ship, once at its site, launches a constructor from a free salvage unit; the constructor builds a salvage platform beside the derelict, which strips it for the ship or recovers it, then takes itself apart; pirates who board a platform recover its derelict with an order of their own, which joins no mission
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
        // the salvage ship's mission it joined, which holds one of the ship's units for it; none for the pirates' order of a platform they boarded
        [StarData] public SalvageMission Mission { get; private set; }
        // placed for a salvage ship's mission, or for a platform pirates boarded; an order saved with a salvage station's work, or a salvage ship of its own, ends on load
        [StarData] bool Pooled;
        // its platform stripped the derelict empty
        [StarData] public bool Emptied { get; private set; }
        // the Salvage button's second click called the order off while its platform stood
        [StarData] public bool CalledOff { get; private set; }
        // the Recover button's order: its platform works to bring the derelict's ship back instead of stripping it
        [StarData] public bool Recovering { get; private set; }
        // the work its platform has done on the recovery, kept while it strips
        [StarData] public float RecoveryWork { get; private set; }
        // the turns its platform has broken into the derelict since the order last turned to the recovery
        [StarData] public int BreakInDone { get; private set; }
        // its roll broke the derelict apart, so its platform's work is over
        [StarData] public bool RecoveryFailed { get; private set; }
        // its roll succeeded: the derelict rises for its ship, which takes its place
        [StarData] public bool RecoverySucceeded { get; private set; }
        [StarData] int DismantleTurnsLeft;
        // the ordnance its unit spent to launch its constructor, which the unit gets back once its platform has taken itself apart
        [StarData] float LaunchCost;
        // its derelict's techs and whole production, kept for the loads sent once the derelict has sunk away
        [StarData] public string[] Techs { get; private set; } = Empty<string>.Array;
        [StarData] public float DerelictProduction { get; private set; }
        // the research its loads have given each of those techs, by the techs' order
        [StarData] public float[] Gained { get; private set; } = Empty<float>.Array;
        // the production its loads have brought the salvage ship
        [StarData] public float Delivered { get; private set; }
        // its derelict's design name and hull icon, kept for the notifications once the derelict has sunk away
        [StarData] public string DerelictName { get; private set; }
        [StarData] public string DerelictIcon { get; private set; }
        Derelict Found;

        // its platform's work is over: it sends the salvage ship all it holds, then takes itself apart
        public bool Finished => Emptied || CalledOff || RecoveryFailed || Recovered;

        // from any thread: the recovered ship has taken the derelict's place, which has left the list
        public bool Recovered => RecoverySucceeded && UState.Derelicts.Find(DerelictId) == null;

        public bool Dismantling => DismantleTurnsLeft > 0;

        // from any thread: its mission's ship is to launch its constructor
        public bool Launching => StepName == nameof(LaunchConstructor);

        // its constructor is out, or its platform stands
        bool Launched => Step > 1;

        public Derelict Derelict => Found ??= UState.Derelicts.Find(DerelictId);

        // no build marker on the map: the derelict's Salvage button calls the order off
        public override bool IsDeploymentGoal => false;

        [StarDataConstructor]
        public SalvageDerelict(Empire owner) : base(GoalType.SalvageDerelict, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                Dispatch,
                LaunchConstructor,
                BuildPlatform,
                Salvage,
                Dismantle
            };
        }

        public SalvageDerelict(Empire owner, Derelict derelict, bool recover = false) : this(owner)
        {
            Pooled = true;
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

        // the pirates' order for a salvage platform they boarded: a recovery of the derelict its owner's order worked, from its start
        SalvageDerelict(Empire pirates, SalvageDerelict held, Ship platform) : this(pirates)
        {
            Pooled = true;
            DerelictId = held.DerelictId;
            Recovering = true;
            Techs = held.Techs;
            Gained = new float[Techs.Length];
            DerelictProduction = held.DerelictProduction;
            DerelictName = held.DerelictName;
            DerelictIcon = held.DerelictIcon;
            Initialize(ShipDesign.SalvagePlatformName, Vector2.Zero, held.TargetSystem);
            Platform = platform;
            ChangeToStep(Salvage);
        }

        public enum Block { None, Ordered, ClaimedByOther, Gone, NotAShip, NoSalvageShip, NoTech, NoUsableShip }

        // from any thread: why the empire cannot order the derelict salvaged, Ordered while its order stands, or None when it can; an empire that can build no salvage ship it could send and has none cannot, NoSalvageShip while its race names none, NoUsableShip while it builds salvage ships that all fall short, else NoTech
        public static Block WhyNoOrder(Empire empire, Derelict d)
        {
            if (!empire.Universe.P.SalvageOps || !d.CanBeSalvaged || !d.IsKnownBy(empire))
                return Block.Gone;
            if (d.Claimant != null && d.Claimant != empire)
                return Block.ClaimedByOther;
            if (d.Claimant == empire || OrderOf(empire, d) != null)
                return Block.Ordered;
            if (empire.CanBuildSalvageShips || HasSalvageShip(empire))
                return Block.None;
            if (empire.SalvageShipDesign == null)
                return Block.NoSalvageShip;
            return CanBuildAnySalvageShip(empire) ? Block.NoUsableShip : Block.NoTech;
        }

        // from any thread: a salvage ship of the empire's with a live salvage unit, at work or idle
        public static bool HasSalvageShip(Empire empire)
        {
            var ships = empire.OwnedShips;
            for (int i = 0; i < ships.Count; ++i)
                if (ships[i] is { IsSalvageShip: true, Active: true } ship && SalvageMission.LiveUnits(ship) > 0)
                    return true;
            return false;
        }

        // from any thread: a salvage ship design the empire can build, whether or not a pick could take it
        static bool CanBuildAnySalvageShip(Empire empire)
        {
            foreach (IShipDesign design in empire.ShipsWeCanBuildSnapshot)
                if (design.IsSalvageShip)
                    return true;
            return false;
        }

        // from any thread: as for salvage, but a station, which can only be salvaged, is NotAShip
        public static Block WhyNoRecovery(Empire empire, Derelict d) => RecoveryBlock(WhyNoOrder(empire, d), d);

        public static Block RecoveryBlock(Block salvageBlock, Derelict d)
            => salvageBlock is Block.Gone or Block.NoSalvageShip || DerelictRecovery.CanRecover(d) ? salvageBlock : Block.NotAShip;

        // an order whose platform's work is over is no longer the derelict's, though it holds its unit until its platform is gone
        public static SalvageDerelict OrderOf(Empire empire, Derelict d)
            => empire.AI.FindGoal(g => g is SalvageDerelict { Finished: false } s && s.DerelictId == d.Id) as SalvageDerelict;

        // the platform's cost to the empire, never below 100, both times the production pace
        public static float PlatformConstruction(Empire empire, IShipDesign platform)
            => Math.Max(platform.GetCost(empire), MinPlatformConstruction * empire.Universe.ProductionPace);

        // the constructor the empire can build that takes the least ordnance to launch, whatever it costs or the player picked; empty without one
        public static string ConstructorName(Empire empire)
        {
            if (!ResourceManager.Ships.GetDesign(ShipDesign.SalvagePlatformName, out IShipDesign _))
                return "";
            Ship lightest = null;
            foreach (IShipDesign design in empire.ShipsWeCanBuildSnapshot)
            {
                if (design.IsConstructor && ResourceManager.GetShipTemplate(design.Name, out Ship constructor)
                    && (lightest == null || constructor.ShipOrdLaunchCost < lightest.ShipOrdLaunchCost))
                    lightest = constructor;
            }
            return lightest?.Name ?? "";
        }

        // the ordnance a salvage unit spends to launch the empire's constructor, none without one
        public static float ConstructorLaunchCost(Empire empire)
            => ResourceManager.GetShipTemplate(ConstructorName(empire), out Ship constructor) ? constructor.ShipOrdLaunchCost : 0f;

        // the Salvage and Recover buttons, or a parked salvage ship's mission, which the order joins at once; false when the empire cannot order it
        public static bool Order(Empire empire, Derelict d, bool recover = false, SalvageMission by = null)
        {
            if ((recover ? WhyNoRecovery(empire, d) : WhyNoOrder(empire, d)) != Block.None)
                return false;
            empire.AI.AddGoalAndEvaluate(new SalvageDerelict(empire, d, recover) { Mission = by });
            return true;
        }

        // on the sim thread: a parked salvage ship's mission with a free unit takes the order while it still waits for one, and the order moves on to its launch at once
        public bool JoinWaiting(SalvageMission mission)
        {
            if (!Pooled || Mission != null || StepName != nameof(Dispatch) || Derelict is not { CanBeSalvaged: true } d
                || d.Claimant != null && d.Claimant != Owner || OrderOf(Owner, d) != this)
                return false;
            Mission = mission;
            ChangeToStep(LaunchConstructor);
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

        // the button's second click calls the order off on the sim thread, where its claim is given up and its empire's salvage ships stop ordering the derelict themselves; a platform standing sends what it holds and takes itself apart; a successful roll is past calling off
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
                d.SetCalledOffBy(empire);
                if (order.Platform is { Active: true })
                    order.CallOff();
                else
                    empire.AI.RemoveGoal(order);
            }
            else if (d.Claimant == empire) // a claim kept without its order, as a save can catch it
            {
                d.SetCalledOffBy(empire);
                d.ReleaseClaim();
            }
        }

        // the derelict's turns run on, and any empire can salvage it
        public void CallOff()
        {
            CalledOff = true;
            if (Derelict is { } d && d.Claimant == Owner)
                d.ReleaseClaim();
        }

        // an order stands while salvage ops are on and its derelict can be salvaged, unless another empire claimed it first, while the derelict rises after a successful roll, and once its platform's work is over
        bool Stands => Owner.Universe.P.SalvageOps
                       && (Finished || RecoverySucceeded || Derelict is { CanBeSalvaged: true } d && (d.Claimant == null || d.Claimant == Owner));

        // an order of an old save ends; one whose mission is over ends, the player told of a lost salvage ship unless the roll succeeded, as the ship still comes, but one whose ship was lost before its platform's work was over starts again, unless on automatic where the planner would stay out; one that no longer stands ends, the player told when a rival claimed its derelict first, or it sank before the claim
        protected override GoalStep? PreEvaluate()
        {
            if (!Pooled)
                return GoalStep.GoalFailed;
            if (Mission is { Over: true } mission)
            {
                if (mission.ShipLost && !RecoverySucceeded)
                    PlayerNotifications?.AddSalvageShipLostNotification(this, mission.ShipIcon);
                if (!mission.ShipLost || Finished || RecoverySucceeded || Owner.IsDefeated
                    || SalvageMission.OnAutomatic(Owner) && !SalvageOpsPlanner.WouldWork(Owner, TargetSystem))
                    return GoalStep.GoalFailed;
                StartOver();
            }
            if (Stands)
                return null;
            if (Owner.Universe.P.SalvageOps)
            {
                if (Derelict is { CanBeSalvaged: true } d)
                    PlayerNotifications?.AddSalvageClaimedFirstNotification(this, d.Claimant);
                else
                    PlayerNotifications?.AddSalvageSankNotification(this);
            }
            return GoalStep.GoalFailed;
        }

        NotificationManager PlayerNotifications => Owner.isPlayer ? UState.Notifications : null;

        public override void OnRemoved() => LetGoOfWork();

        // its constructor and platform gone with what the platform holds, its claim given up and its recovery's work lost, it joins a salvage ship's mission as a new order would, or one built for it
        void StartOver()
        {
            LetGoOfWork();
            ChangeToStep(Dispatch); // first, so the UI never reads a platform being built
            Mission = null;
            Platform = null;
            FinishedShip = null;
            StaticBuildPos = Vector2.Zero;
            RecoveryWork = 0f;
            BreakInDone = 0;
            LaunchCost = 0f;
        }

        // as the order ends or starts again: it calls its constructor home, or scuttles it with its ship lost or its empire gone, takes its platform away unless it is in other hands, where it stops coming apart, and gives up its claim unless another order of its empire's works the derelict
        void LetGoOfWork()
        {
            if (Constructor() is { } constructor)
            {
                if (constructor.Mothership is { Active: true } ship && ship.Loyalty == Owner && !Owner.IsDefeated)
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

        // as pirates board a salvage platform, its owner's order ends and what it holds is lost; their own order recovers the derelict's ship, or takes the platform apart at a derelict they cannot recover or already recover from another; one with no order, or boarded by pirates already defeated, is taken away; a save or the UI can apply the change of hands
        public static void Captured(Ship platform, Empire from, Empire to)
        {
            if (platform.Universe.Screen is { IsOnSimThread: false } screen)
            {
                screen.RunOnSimThread(() => Captured(platform, from, to));
                return;
            }
            // a queued call that the platform's fate, another change of hands or its owner's order has overtaken does nothing
            if (!to.WeArePirates || !platform.Active || platform.Loyalty != to || OfPlatform(platform) != null)
                return;

            var held = from.AI.FindGoal(g => g is SalvageDerelict s && s.Platform == platform) as SalvageDerelict;
            if (held != null)
                from.AI.RemoveGoal(held);
            if (held == null || to.IsDefeated) // defeated pirates run no orders
            {
                platform.QueueTotalRemoval();
                return;
            }
            platform.UnloadProduction(platform.GetProduction());
            Derelict d = held.Derelict;
            bool recovers = d is { CanBeSalvaged: true } && DerelictRecovery.CanRecover(d) && OrderOf(to, d) == null && d.TryClaim(to);
            var order = new SalvageDerelict(to, held, platform);
            if (!recovers)
                order.CalledOff = true; // it takes the platform apart, leaving any claim as it is
            to.AI.AddGoal(order);
        }

        // from the constructor's deploy: the platform takes over the derelict, and the ordnance its constructor took is spent
        public void OnPlatformBuilt(Ship platform, Ship constructor)
        {
            Platform = platform;
            constructor.Mothership?.OnLaunchedShipDie(constructor);
        }

        // it joins a mission once; orders placed together from the UI reach the sim thread together, and the first one holds the derelict; with no salvage ship to take it, it waits for one
        GoalStep Dispatch()
        {
            if (Mission != null)
                return GoalStep.GoToNextStep;
            if (OrderOf(Owner, Derelict) != this)
                return GoalStep.GoalFailed;
            Mission = SalvageMission.Join(Owner, TargetSystem);
            return Mission != null ? GoalStep.GoToNextStep : GoalStep.TryAgain;
        }

        // a free salvage unit of the parked ship launches the constructor when the ship has the ordnance for it, and for a salvage only while the ship's hold has room
        GoalStep LaunchConstructor()
        {
            if (!Mission.Parked(out Ship ship))
                return GoalStep.TryAgain;
            if (!HoldsAUnit(ship))
                return GoalStep.GoalFailed;
            if (!Recovering && HoldFull(ship))
                return GoalStep.TryAgain;

            ShipModule bay = FreeBay(ship);
            Ship constructor = bay != null ? Ship.CreateShipFromHangar(UState, bay, Owner, ship.Position, ship) : null;
            if (constructor == null)
                return GoalStep.TryAgain;

            ship.OnShipLaunched(constructor, bay);
            LaunchCost = constructor.ShipOrdLaunchCost;
            FinishedShip = constructor;
            if (StaticBuildPos == Vector2.Zero)
                StaticBuildPos = Derelict.RestPosition + MathExt.PointOnCircle(Owner.Random.Float(0f, 360f), Derelict.Radius + PlatformClearance);
            constructor.AI.OrderDeepSpaceBuild(this, PlatformConstruction(Owner, ToBuild), ToBuild.Grid.Radius);
            return GoalStep.GoToNextStep;
        }

        // the derelict becomes the empire's as its constructor starts to build, with the trust cost of salvaging in a system where other empires have colonies; one lost on its way is launched again
        GoalStep BuildPlatform()
        {
            if (Platform != null)
                return GoalStep.GoToNextStep;
            if (Constructor() is not { } constructor)
                return GoalStep.RestartGoal;

            if (constructor.Position.InRadius(BuildPosition, ConstructionShip.ConstructingDistance))
            {
                if (Derelict.Claimant != Owner && Derelict.TryClaim(Owner)) // Stands holds off another empire's claim
                    SalvageMission.CostTrustOfColonyOwners(Owner, TargetSystem);
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

        // a constructor in other hands is no longer its ship's, and its unit is free, with the ordnance it took written off
        static void LetGo(Ship mothership, Ship constructor)
        {
            mothership?.OnLaunchedShipDie(constructor);
            constructor.Mothership = null;
        }

        // each turn the platform strips what its hold has room for; with its hold full, its work over or a recovery begun it sends what it holds to the salvage ship while the ship is at its site, which takes what fits, and otherwise waits for it; a recovering one works on with a load it cannot send; once its work is over and its hold empty it takes itself apart, the player told of a salvage's end but not a recovery's; one lost or in other hands ends the order
        GoalStep Salvage()
        {
            if (Platform is not { Active: true } platform)
            {
                if (!RecoverySucceeded)
                    PlayerNotifications?.AddSalvagePlatformDestroyedNotification(this);
                return GoalStep.GoalFailed;
            }
            if (platform.Loyalty != Owner)
                return HandedOver(platform) ? GoalStep.TryAgain : GoalStep.GoalFailed;

            if (!Finished)
                Derelict.TryClaim(Owner); // takes back a claim that a call-off caught halfway by a save dropped
            if (platform.GetProduction() > 0f && (Finished || Recovering || HoldFull(platform)) && Mission?.Parked(out Ship ship) == true && !HoldFull(ship))
                SendLoad(platform, ship);
            else if (Recovering && !Finished && Recover(platform, Derelict))
                return Exploded(platform);
            else if (!Recovering && !Finished && !HoldFull(platform))
                Strip(platform, Derelict);

            if (!Finished || platform.GetProduction() > 0f)
                return GoalStep.TryAgain;
            DismantleTurnsLeft = DismantleTurns;
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

        // it is gone once its turns are over, freeing its unit, which gets back the ordnance its launch took
        GoalStep Dismantle()
        {
            if (Platform is not { Active: true } platform)
                return GoalStep.GoalFailed;
            if (platform.Loyalty != Owner)
                return HandedOver(platform) ? GoalStep.TryAgain : GoalStep.GoalFailed;
            if (--DismantleTurnsLeft > 0)
                return GoalStep.TryAgain;
            platform.FinishDismantling();
            Mission?.SalvageShip?.ChangeOrdnance(LaunchCost);
            return GoalStep.GoalComplete;
        }

        // a boarding this order missed, as one the UI or a save applied, hands the platform over to the pirates' order now, which ends this one
        bool HandedOver(Ship platform)
        {
            if (!platform.Loyalty.WeArePirates || OfPlatform(platform) != null)
                return false;
            Captured(platform, Owner, platform.Loyalty);
            return true;
        }

        // as much of its hold as the salvage ship has room for, its shuttles launching where the player sees either end
        void SendLoad(Ship platform, Ship ship)
        {
            float sent = ship.LoadProduction(Math.Min(platform.GetProduction(), ship.CargoRoom));
            platform.UnloadProduction(sent);
            if (sent <= 0f)
                return;
            Delivered += sent;
            AdvanceTechs(sent);
            if (UState.IsPlanetViewOrCloser && (SeenByPlayer(platform) || SeenByPlayer(ship)))
                UState.Screen.CargoShuttles.SendLoad(platform, ship, Owner.EmpireColor, CargoShuttles.LoadShuttlesFor(sent));
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

        public enum Stage { None, WaitingForShip, ShipOrdered, ShipOnItsWay, PlatformOrdered, BuildingPlatform, Salvaging, BreakingIn, Recovering }

        // from any thread: how far the empire's salvage or recovery of the derelict has come, with its order, or Salvaging or Recovering while another empire holds it, with its claimant as read once
        public static Stage StageOf(Empire empire, Derelict d, out Empire claimant, out SalvageDerelict order)
        {
            claimant = d.Claimant;
            order = null;
            if (claimant != null && claimant != empire)
                return OrderOf(claimant, d) is { Recovering: true } ? Stage.Recovering : Stage.Salvaging;
            if ((order = OrderOf(empire, d)) != null)
                return order.Platform != null ? (order.BreakingIn ? Stage.BreakingIn : order.Recovering ? Stage.Recovering : Stage.Salvaging)
                     : order.Launched ? Stage.BuildingPlatform : order.ShipStage;
            return claimant == empire ? Stage.Salvaging : Stage.None;
        }

        Stage ShipStage => Mission is not { } mission ? Stage.WaitingForShip
                         : mission.Building ? Stage.ShipOrdered
                         : mission.Parked(out _) ? Stage.PlatformOrdered : Stage.ShipOnItsWay;

        static bool SeenByPlayer(Ship ship) => ship.InFrustum && ship.InPlayerSensorRange;

        // from any thread: production the empire's platforms strip from a derelict in a turn, raised by its Salvage Speed research, and pirates' by their level
        public static float SalvageSpeed(Empire empire) => GlobalStats.Defaults.SalvageSpeed * empire.data.SalvageSpeedMultiplier;

        // it strips only what its hold took
        void Strip(Ship platform, Derelict d)
        {
            float wanted = Math.Min(Math.Min(SalvageSpeed(Owner), platform.CargoRoom), d.ProductionLeft);
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

        // less room than this left in a platform's or a ship's hold counts as full
        const float FullHoldRoom = 0.01f;

        public static bool HoldFull(Ship ship) => ship.CargoRoom < FullHoldRoom;

        // from any thread: with its hold full or its work over, its load waits for the salvage ship, which is away or has no room; a recovering one works on with its load
        public bool LoadWaitsForShip(Ship platform, out bool shipFull)
        {
            shipFull = false;
            if (Recovering && !Finished || !(Finished || HoldFull(platform)) || platform.GetProduction() <= 0f)
                return false;
            if (Mission?.Parked(out Ship ship) != true)
                return true;
            shipFull = HoldFull(ship);
            return shipFull;
        }

        // from any thread: the order a salvage platform works for, or null
        public static SalvageDerelict OfPlatform(Ship platform)
            => platform.Loyalty.AI.FindGoal(g => g is SalvageDerelict s && s.Platform == platform) as SalvageDerelict;

        // a bay whose constructor is in other hands, as one boarded on its way home after its order ended, lets it go here
        static ShipModule FreeBay(Ship ship)
        {
            ShipModule[] bays = ship.Carrier.AllHangars;
            for (int i = 0; i < bays.Length; ++i)
            {
                ShipModule bay = bays[i];
                if (!bay.IsSalvageBay || !bay.Active)
                    continue;
                if (bay.TryGetHangarShipActive(out Ship launched) && launched.Loyalty != ship.Loyalty)
                    LetGo(ship, launched);
                if (bay.HangarTimer <= 0f && !bay.IsHangarShipActive)
                    return bay;
            }
            return null;
        }

        // launched orders hold the ship's units first, then waiting ones as they were placed; the goals are evaluated last to first
        bool HoldsAUnit(Ship ship)
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
                if (goals[i] is SalvageDerelict s && s.Mission == Mission && (placedBefore || s.Launched))
                    ++ahead;
            }
            return ahead < SalvageMission.LiveUnits(ship);
        }
    }
}
