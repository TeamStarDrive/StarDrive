using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.ExtensionMethods;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;
#pragma warning disable CA1001

namespace Ship_Game.GameScreens.NewGame
{
    /// <summary>
    /// Helper class for creating a New universe with Empires
    /// </summary>
    public sealed class UniverseGenerator
    {
        readonly int NumSystems;
        readonly Array<Vector2> ClaimedSpots = new();
        readonly RaceDesignScreen.GameMode Mode;
        readonly GameDifficulty Difficulty;
        readonly int NumOpponents;
        readonly Array<IEmpireData> SelectedOpponents;
        readonly Empire Player;
        readonly UniverseScreen us;
        readonly UniverseState UState;

        readonly Array<SystemPlaceHolder> Systems = new();

        public readonly RandomBase Random;

        public UniverseGenerator(UniverseParams p)
        {
            // TODO: allow players to enter their own universe seed
            Random = new SeededRandom();

            foreach (Artifact art in ResourceManager.ArtifactsDict.Values)
                art.Discovered = false;

            Difficulty = p.Difficulty;
            Mode = p.Mode;
            NumOpponents = p.NumOpponents;
            NumSystems = p.NumSystems;
            SelectedOpponents = p.SelectedOpponents;
            ResourceManager.LoadEncounters();

            float uSize;
            switch (p.GalaxySize)
            {
                default:
                case GalSize.Tiny: uSize = 2_000_000; break;
                case GalSize.Small: uSize = 4_000_000; break;
                case GalSize.Medium: uSize = 6_000_000; break;
                case GalSize.Large: uSize = 9_000_000; break;
                case GalSize.Huge: uSize = 12_000_000; break;
                case GalSize.Epic: uSize = 15_000_000; break;
                case GalSize.TrulyEpic: uSize = 20_000_000; break;
            }


            us = new UniverseScreen(p, uSize);
            UState = us.UState;
            UState.BackgroundSeed = new Random().Next();

            UState.P.DisableInhibitionWarning = p.Difficulty > GameDifficulty.Hard;

            Player = new Empire(UState)
            {
                EmpireColor = p.PlayerData.Traits.Color,
                data = p.PlayerData,
                isPlayer = true,
            };

            UState.AddEmpire(Player); // this binds Player to the universe

            Player.Initialize();
            Player.data.CurrentAutoScout = Player.data.ScoutShip;
            Player.data.CurrentAutoColony = Player.data.ColonyShip;
            Player.data.CurrentAutoFreighter = Player.data.FreighterShip;
            Player.data.CurrentConstructor = Player.data.ConstructorShip;
            Player.data.CurrentResearchStation = Player.data.ResearchStation;
            Player.data.SelectedTraitSet = string.Join(", ", Player.data.Traits.PlayerTraitOptions);
        }

        public readonly ProgressCounter Progress = new ProgressCounter();

        public TaskResult<UniverseScreen> GenerateAsync()
        {
            return Parallel.Run(Generate);
        }

        /// <summary>
        /// Generates a new UniverseScreen with UniverseState.
        ///
        /// After completion you have to LoadContent via
        /// ScreenManager.AddScreenAndLoadContent(us)
        /// or manually via us.LoadContent()
        /// </summary>
        public UniverseScreen Generate()
        {
            Progress.Start(0.65f, 0.35f);
            GenerateInitialSystemData(Progress.NextStep());
            FinalizeEmpires(Progress.NextStep());
            Progress.Finish();

            Planet homePlanet = Player.GetPlanets()[0];
            us.CamPos = new Vector3d(homePlanet.Position.X, homePlanet.Position.Y, 5000);

            Log.Info(ConsoleColor.Blue, $"  GenerateInitialSystemData elapsed: {Progress[0].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"  FinalizeEmpires           elapsed: {Progress[1].ElapsedMillis}ms");
            Log.Info(ConsoleColor.DarkRed, $"TOTAL GenerateSystems       elapsed: {Progress.ElapsedMillis}ms");
            return us;
        }

        void FinalizeEmpires(ProgressCounter step)
        {
            step.Start(UState.Empires.Count);
            foreach (Empire empire in UState.Empires)
            {
                step.Advance();
                if (empire.IsFaction)
                    continue;

                IReadOnlyList<Planet> planets = empire.GetPlanets();
                for (int planetId = 0; planetId < planets.Count; planetId++)
                {
                    Planet planet = planets[planetId];
                    planet.MineralRichness += UState.P.StartingPlanetRichnessBonus;
                    planet.System.SetExploredBy(empire);
                    planet.SetExploredBy(empire);

                    foreach (Planet p in planet.System.PlanetList)
                    {
                        p.SetExploredBy(empire);
                    }

                    if (planet.System.OwnerList.Count == 0)
                    {
                        planet.System.OwnerList.Add(empire);
                        foreach (Planet planet2 in planet.System.PlanetList)
                            planet2.SetExploredBy(empire);
                    }
                }
            }

            foreach (Empire e in UState.Empires)
            {
                e.InitFleetEmpireStrMultiplier();
                if (e.IsFaction)
                    continue;

                Planet homeWorld = e.GetPlanets()[0];
                SolarSystem[] closestSystems = UState.Systems.Sorted(system => homeWorld.Position.SqDist(system.Position));

                // Home system is always set to be explored
                int numExplored = (e.data.Traits.BonusExplored + 1).UpperBound(UState.Systems.Count);

                for (int i = 0; i < numExplored; ++i)
                {
                    SolarSystem ss = closestSystems[i];
                    ss.SetExploredBy(e);
                    foreach (Planet planet in ss.PlanetList)
                        planet.SetExploredBy(e);

                    ss.UpdateFullyExploredBy(e);
                }
            }

            EmpireHullBonuses.RefreshBonuses(UState);
        }

        class SystemPlaceHolder
        {
            public string SystemName;
            public SolarSystemData Data;
            public Empire Owner;
            public Vector2 Position;
            public bool DontStartNearPlayer;
            public bool IsStartingSystem => Owner != null;
        }

        void GenerateInitialSystemData(ProgressCounter step)
        {
            // expected times of each step
            step.StartAbsolute(0.228f, 0.007f, 0.043f, 0.008f, 0.376f);

            CreateOpponents(step.NextStep()); // 228ms
            ShipDesignUtils.MarkDesignsUnlockable(step.NextStep()); // 7ms
            CreateSystemPlaceHolders(step.NextStep()); // 43ms
            CreateSystemPositions(step.NextStep()); // 8ms
            GenerateSystems(step.NextStep()); // 376ms

            step.Finish();
            Log.Info(ConsoleColor.Blue, $"    ## CreateOpponents           elapsed: {step[0].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## MarkShipDesignsUnlockable elapsed: {step[1].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## CreateSystemPlaceHolders  elapsed: {step[2].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## CreateSystemPositions     elapsed: {step[3].ElapsedMillis}ms");
            Log.Info(ConsoleColor.Blue, $"    ## GenerateSystems           elapsed: {step[4].ElapsedMillis}ms");
        }

        void CreateOpponents(ProgressCounter step)
        {
            IEmpireData[] randomMajorRaces = ResourceManager.MajorRaces.Filter(
                                data => data.ArchetypeName != Player.data.ArchetypeName
                                && !SelectedOpponents.Contains(data));

            // create a randomly shuffled list of opponents which were not yet selected for the game.
            int randomMajorRacesNeeded = NumOpponents - SelectedOpponents.Count;
            var randomOpponents = new Array<IEmpireData>(randomMajorRaces);
            randomOpponents.Shuffle();
            randomOpponents.Resize(Math.Min(randomOpponents.Count, randomMajorRacesNeeded)); // truncate
            // combined the random opponents with the already selected ones
            SelectedOpponents.AddRange(randomOpponents);
            step.Start(SelectedOpponents.Count + ResourceManager.MinorRaces.Count);
            foreach (IEmpireData readOnlyData in SelectedOpponents)
            {
                Empire e = UState.CreateEmpire(readOnlyData, isPlayer: false, difficulty: Difficulty);
                RacialTrait t = e.data.Traits;
                e.data.FlatMoneyBonus += e.DifficultyModifiers.FlatMoneyBonus;
                t.ShipCostMod += e.DifficultyModifiers.ShipCostMod;

                if (e.DifficultyModifiers.ProductionMod.NotZero())
                    t.ProductionMod = (1 + t.ProductionMod) * e.DifficultyModifiers.ProductionMod;
                if (e.DifficultyModifiers.ResearchMod.NotZero())
                    t.ResearchMod = (1 + t.ResearchMod) * e.DifficultyModifiers.ResearchMod;
                if (e.DifficultyModifiers.TaxMod.NotZero())
                    t.TaxMod = (1 + t.TaxMod) * e.DifficultyModifiers.TaxMod;

                t.ModHpModifier += e.DifficultyModifiers.ModHpModifier;

                t.ResearchTaxMultiplier = e.DifficultyModifiers.ResearchTaxMultiplier; // the "=" here is intended

                step.Advance();
            }

            foreach (IEmpireData readOnlyData in ResourceManager.MinorRaces)
            {
                UState.CreateEmpire(readOnlyData, isPlayer: false, difficulty: Difficulty);
                step.Advance();
            }

            UState.CalcInitialSettings();
        }

        void CreateSystemPlaceHolders(ProgressCounter step)
        {
            Empire[] majorEmpires = UState.Empires.Filter(e => !e.IsFaction);
            
            step.Start(NumSystems + majorEmpires.Length);

            foreach (Empire e in majorEmpires)
            {
                Systems.Add(new SystemPlaceHolder
                {
                    Owner = e,
                    Data = ResourceManager.LoadSolarSystemData(e.data.Traits.HomeSystemName), // SystemData can be null
                    SystemName = e.data.Traits.HomeSystemName,
                });
                step.Advance();
            }

            int systemCount = 0;
            foreach (SolarSystemData systemData in ResourceManager.LoadRandomSolarSystems())
            {
                if (systemCount > NumSystems)
                    break;
                ++systemCount;
                Systems.Add(new SystemPlaceHolder { DontStartNearPlayer = true, Data = systemData });
                step.Advance();
            }

            if (systemCount < NumSystems)
            {
                var nameGenerator = ResourceManager.GetNameGenerator("NameGenerators/names.txt");
                for (; systemCount < NumSystems; ++systemCount)
                {
                    Systems.Add(new SystemPlaceHolder { SystemName = nameGenerator.NextName });
                    step.Advance();
                }
            }
        }

        void CreateSystemPositions(ProgressCounter step)
        {
            step.Start(Systems.Count);
            switch (Mode)
            {
                case RaceDesignScreen.GameMode.Corners:       GenerateCornersGameMode(step);  break;
                case RaceDesignScreen.GameMode.BigClusters:   GenerateBigClusters(step);      break;
                case RaceDesignScreen.GameMode.SmallClusters: GenerateSmallClusters(step);    break;
                case RaceDesignScreen.GameMode.Ring:          GenerateRingMap(step);          break;
                case RaceDesignScreen.GameMode.SpiralTwoArm:     GenerateSpiralMap(step, SpiralVariant.TwoArm);     break;
                case RaceDesignScreen.GameMode.SpiralFourArm:    GenerateSpiralMap(step, SpiralVariant.FourArm);    break;
                case RaceDesignScreen.GameMode.SpiralBarred:     GenerateSpiralMap(step, SpiralVariant.Barred);     break;
                case RaceDesignScreen.GameMode.SpiralMagellanic: GenerateSpiralMap(step, SpiralVariant.Magellanic); break;
                case RaceDesignScreen.GameMode.Sandbox:       GenerateRandomMap(step, false); break;
                default:                                      GenerateRandomMap(step, true);  break;
            }
        }

        void GenerateSystems(ProgressCounter step)
        {
            step.Start(Systems.Count);
            float exoticPlanetMultiplier = (100f / Systems.Count).UpperBound(1);
            foreach (SystemPlaceHolder placeHolder in Systems)
            {
                Empire e = placeHolder.Owner;
                var sys = new SolarSystem(UState, placeHolder.Position);

                if (placeHolder.Data != null)
                    sys.GenerateFromData(UState, Random, placeHolder.Data, e, exoticPlanetMultiplier);
                else
                    sys.GenerateRandomSystem(UState, Random, placeHolder.SystemName, e, exoticPlanetMultiplier);

                if (e != null && e.GetOwnedSystems().Count == 0)
                {
                    Log.Error($"Failed to create starting system for {e}");
                }

                UState.AddSolarSystem(sys);
                step.Advance();
            }

            // once all systems are generated, init FiveClosestSystems for all
            foreach (SolarSystem system in UState.Systems)
            {
                system.FiveClosestSystems = UState.GetFiveClosestSystems(system);
            }

            UState.MineablePlanets.Sort(p => -p.Mining.Richness);
            UState.GeneratePotentialDysonSwarms();
            step.Finish();
        }

        void SolarSystemSpacingRing(ProgressCounter step)
        {
            foreach (SystemPlaceHolder sys in Systems)
            {
                float spacing = 350000f;
                if (sys.DontStartNearPlayer)
                    spacing = UState.Size / (2f - 1f / (UState.Empires.Count - 1));

                sys.Position = GenerateRandomSysPosInRing(spacing);
                step.Advance();
            }
        }

        void SolarSystemSpacing(ProgressCounter step, bool randomStartingPos)
        {
            foreach (SystemPlaceHolder sys in Systems)
            {
                float spacing = 350000f;
                if (sys.IsStartingSystem && !randomStartingPos)
                    continue; // We created starting systems before

                if (sys.DontStartNearPlayer)
                    spacing = UState.Size / (2f - 1f / (UState.Empires.Count - 1));

                sys.Position = GenerateRandomSysPos(spacing);
                step.Advance();
            }
        }

        void GenerateCornersGameMode(ProgressCounter step)
        {
            int whichCorner = StartingPositionCorners(step);

            foreach (SystemPlaceHolder sys in Systems)
            {
                // This will distribute all the rest of the planets evenly
                if (!sys.IsStartingSystem && !sys.DontStartNearPlayer)
                {
                    sys.Position = GenerateRandomCorners(whichCorner);
                    step.Advance();
                    NextCorner(ref whichCorner);
                }
            }
        }

        static void NextCorner(ref int whichCorner)
        {
            if (++whichCorner > 3)
                whichCorner = 0;
        }

        int StartingPositionCorners(ProgressCounter step)
        {
            float universeSize = UState.Size;
            int whichCorner = Random.Int(0, 3); //So the player doesnt always end up in the same corner;
            foreach (SystemPlaceHolder sys in Systems)
            {
                if (sys.IsStartingSystem || sys.DontStartNearPlayer)
                {
                    if (sys.IsStartingSystem)
                    {
                        //Corner Values
                        //0 = Top Left
                        //1 = Top Right
                        //2 = Bottom Left
                        //3 = Bottom Right

                        //Put the 4 Home Planets into their corners, nessled nicely back a bit
                        float RandomoffsetX = Random.Float(0, 19) / 100; //Do want some variance in location, but still in the back
                        float RandomoffsetY = Random.Float(0, 19) / 100;
                        float MinOffset = 0.04f; //Minimum Offset
                        //Theorectical Min = 0.04 (4%)                  Theoretical Max = 0.18 (18%)

                        float CornerOffset = 0.75f; //Additional Offset for being in corner
                        //Theoretical Min with Corneroffset = 0.84 (84%)    Theoretical Max with Corneroffset = 0.98 (98%)  <--- thats wwaayy in the corner, but still good  =)
                        switch (whichCorner)
                        {
                            case 0:
                                sys.Position = new Vector2(
                                    (-universeSize + (universeSize * (MinOffset + RandomoffsetX))),
                                    (-universeSize + (universeSize * (MinOffset + RandomoffsetX))));
                                ClaimedSpots.Add(sys.Position);
                                break;
                            case 1:
                                sys.Position = new Vector2(
                                    (universeSize * (MinOffset + RandomoffsetX + CornerOffset)),
                                    (-universeSize + (universeSize * (MinOffset + RandomoffsetX))));
                                ClaimedSpots.Add(sys.Position);
                                break;
                            case 2:
                                sys.Position = new Vector2(
                                    (-universeSize + (universeSize * (MinOffset + RandomoffsetX))),
                                    (universeSize * (MinOffset + RandomoffsetX + CornerOffset)));
                                ClaimedSpots.Add(sys.Position);
                                break;
                            case 3:
                                sys.Position = new Vector2(
                                    (universeSize * (MinOffset + RandomoffsetX + CornerOffset)),
                                    (universeSize * (MinOffset + RandomoffsetX + CornerOffset)));
                                ClaimedSpots.Add(sys.Position);
                                break;
                            default: throw new IndexOutOfRangeException(nameof(whichCorner));
                        }
                    }
                    else
                    {
                        //This will distribute the extra planets from "/SolarSystems/Random" evenly
                        sys.Position = GenerateRandomCorners(whichCorner);
                    }
                    step.Advance();
                    NextCorner(ref whichCorner);
                }
            }

            return whichCorner;
        }

        Vector2 GenerateRandomSysPosInRing(float spacing)
        {
            float safetyBreak = 1f;
            Vector2 sysPos;
            do
            {
                spacing *= safetyBreak;
                sysPos = Random.RandomPointInRing(UState.Size * 0.75f, UState.Size - 100000f);
                safetyBreak *= 0.97f;
            } while (!SystemPosOK(sysPos, spacing));

            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        Vector2 GenerateRandomSysPos(float spacing)
        {
            float safetyBreak = 1f;
            Vector2 sysPos;
            do
            {
                spacing *= safetyBreak;
                sysPos = Random.Vector2D(UState.Size - 100000f);
                safetyBreak *= 0.97f;
            } while (!SystemPosOK(sysPos, spacing));

            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        void GenerateRandomMap(ProgressCounter step, bool randomStartingPos)
        {
            if (!randomStartingPos)
            {
                // FB - we are using the sector creation only for starting systems here. the rest will be created randomly
                (int numHorizontalSectors, int numVerticalSectors) = GetNumSectors((NumOpponents + 1).LowerBound(9));
                Array<Sector> sectors = GenerateSectors(numHorizontalSectors, numVerticalSectors, 0.1f);
                GenerateClustersStartingSystems(step, sectors);
            }

            SolarSystemSpacing(step, randomStartingPos);
        }

        void GenerateRingMap(ProgressCounter step)
        {
            SolarSystemSpacingRing(step);
        }

        // Four flavors of spiral galaxy, each picked explicitly via GameMode.
        enum SpiralVariant { TwoArm, FourArm, Barred, Magellanic }

        void GenerateSpiralMap(ProgressCounter step, SpiralVariant variant)
        {
            // Logarithmic spiral: r(t) = armStartR * exp(pitch * t), theta = t + armOffset + phase.
            // Stars are sampled by drawing a parameter t along the arm length, mapping to (r, theta),
            // then adding a perpendicular jitter to give the arms visible thickness.
            int numArms;
            float bulgeFraction;
            float bulgeRadius;   // bulge radial extent (fraction of uSize)
            float armThicknessFrac;
            float armStartR;  // arm origin radius (fraction of uSize)
            float armEndR;    // arm end radius (fraction of uSize)
            float pitch;      // log-spiral pitch (radians per e-fold of r) -- smaller = tighter winding = more curves
            float armFlareFactor;  // 0 = uniform-thickness arms; >0 flares the bar-tip end and tapers toward armEndR
            bool hasBar;
            switch (variant)
            {
                case SpiralVariant.FourArm:
                    numArms = 4; bulgeFraction = 0.15f; bulgeRadius = 0.22f; armThicknessFrac = 0.045f;
                    armStartR = 0.20f; armEndR = 0.92f; pitch = 0.45f; armFlareFactor = 0f; hasBar = false; break;
                case SpiralVariant.Barred:
                    // Target look: long bar (~72% of uSize each side) with two arms wrapping
                    // ~180 each around the bar tips. Arms attach at bar tip (armStartR ==
                    // barHalfLen) and span a wider radial band (0.72 -> 0.95) so they read
                    // as a fat band rather than a perfect arc; pitch tuned for ~180 sweep
                    // (maxT = ln(0.95/0.72)/0.088 ~ 3.1 rad). Flare factor 4.0 puffs the
                    // bar-tip end (jitter is 5x at t=0 tapering to 1x at outer end) so the
                    // arms read as a flared trumpet rather than uniform ribbons.
                    numArms = 2; bulgeFraction = 0.30f; bulgeRadius = 0.18f; armThicknessFrac = 0.06f;
                    armStartR = 0.72f; armEndR = 0.95f; pitch = 0.088f; armFlareFactor = 4.0f; hasBar = true;  break;
                case SpiralVariant.Magellanic:
                    // Single fat arm wrapping ~250 around an off-center bulge. No bar.
                    // originOffset (computed below) shifts the whole galaxy sideways so the
                    // map looks lopsided -- the Magellanic Clouds signature. Sized to fill
                    // the universe: bulge to 0.45 uSize, arm reaches to 0.95 uSize
                    // (+0.10 offset = ~1.05 from world center on the offset axis; OOB
                    // stars are rejected by SystemPosOK and respawned).
                    numArms = 1; bulgeFraction = 0.30f; bulgeRadius = 0.55f; armThicknessFrac = 0.11f;
                    armStartR = 0.50f; armEndR = 0.95f; pitch = 0.147f; armFlareFactor = 1.0f; hasBar = false; break;
                default: // TwoArm -- tighter winding (more curves) than the other variants
                    numArms = 2; bulgeFraction = 0.18f; bulgeRadius = 0.22f; armThicknessFrac = 0.075f;
                    armStartR = 0.20f; armEndR = 0.92f; pitch = 0.25f; armFlareFactor = 0f; hasBar = false; break;
            }

            const float barHalfLen   = 0.72f;  // bar half-length along its long axis (~72% of uSize each side)
            const float barHalfWid   = 0.06f;  // bar half-width perpendicular

            float uSize  = UState.Size;
            float phase  = Random.Float(0f, RadMath.TwoPI); // randomize galactic orientation

            // Magellanic-only: shift the whole galaxy off-center perpendicular to the arm
            // origin direction so the map looks visibly lopsided. Magnitude (0.10 * uSize)
            // tuned so the bulk of the arm stays in-bounds even with armEndR=0.92;
            // outliers are rejected by SystemPosOK and respawned.
            Vector2 originOffset = Vector2.Zero;
            if (variant == SpiralVariant.Magellanic)
                originOffset = new Vector2(-RadMath.Sin(phase), RadMath.Cos(phase)) * (uSize * 0.10f);

            Log.Info($"Spiral galaxy variant: {variant} (numArms={numArms}, bulge={bulgeFraction:P0})");

            // Magellanic puts most starting empires inside the bulge (it's huge and
            // anchors the galaxy); the rest spread along the single arm. Other variants
            // place all empires on arms via round-robin -- no bulge starts.
            float bulgeStartFraction = variant == SpiralVariant.Magellanic ? 0.75f : 0f;

            PlaceSpiralStartingSystems(numArms, phase, uSize, armStartR, armEndR, pitch, armThicknessFrac, armFlareFactor,
                                        bulgeRadius, bulgeStartFraction, originOffset, step);
            PlaceSpiralBackgroundSystems(numArms, phase, uSize, armStartR, armEndR, pitch,
                                          armThicknessFrac, armFlareFactor, bulgeFraction, bulgeRadius,
                                          hasBar, barHalfLen, barHalfWid, originOffset, step);
        }

        void PlaceSpiralStartingSystems(int numArms, float phase, float uSize,
                                         float armStartR, float armEndR, float pitch,
                                         float armThicknessFrac, float armFlareFactor,
                                         float bulgeRadius, float bulgeStartFraction,
                                         Vector2 originOffset, ProgressCounter step)
        {
            // Two-phase placement so that bulge-anchored variants (Magellanic) can put
            // some empires in the central blob and the rest on arms.
            //
            // Phase 1: bulgeStartFraction of starts get sampled inside the bulge with
            //          decaying min-spacing -- empires spread inside the central blob
            //          but not too close to each other.
            // Phase 2: remaining starts are slotted onto arms via deterministic
            //          round-robin (arm = i % numArms, slot = i / numArms). Pure
            //          random placement into SampleArmPos uses a min-spacing retry
            //          that decays 0.97x per attempt -- on narrow arm bands the
            //          constraint degrades into nothing and empires cluster. Slotting
            //          guarantees angular + radial separation regardless of arm shape.
            SystemPlaceHolder[] starting = Systems.Filter(s => s.IsStartingSystem);
            Random.Shuffle(starting); // randomize which empire gets bulge vs arm

            int inBulgeCount = (int)Math.Round(starting.Length * bulgeStartFraction);
            int onArmCount   = starting.Length - inBulgeCount;

            // Phase 1: bulge placements
            float bulgeMaxR    = uSize * bulgeRadius;
            float bulgeSpacing = bulgeMaxR * 0.5f;
            for (int i = 0; i < inBulgeCount; i++)
            {
                Vector2 sysPos = default;
                int attempts = 0;
                do
                {
                    sysPos = Random.RandomPointInRing(0f, bulgeMaxR) + originOffset;
                    bulgeSpacing *= 0.97f;
                } while (!SystemPosOK(sysPos, bulgeSpacing) && ++attempts < 200);

                starting[i].Position = sysPos;
                ClaimedSpots.Add(sysPos);
                step.Advance();
            }

            // Phase 2: arm placements via round-robin slotting
            int slotsPerArm   = onArmCount > 0 ? (onArmCount + numArms - 1) / numArms : 1;
            float maxT        = (float)Math.Log(armEndR / armStartR) / pitch;
            float jitterScale = uSize * armThicknessFrac;
            float armSep      = RadMath.TwoPI / numArms;

            for (int armIndex = 0; armIndex < onArmCount; armIndex++)
            {
                int arm  = armIndex % numArms;
                int slot = armIndex / numArms;

                // Center the empire in its arm slot, then add a small wiggle so the
                // same slot doesn't land at the exact same t across replays.
                float tFrac = (slot + 0.5f) / slotsPerArm;
                tFrac += Random.Float(-0.15f, 0.15f) / slotsPerArm;
                float t = tFrac.Clamped(0.05f, 0.95f) * maxT;

                float r     = armStartR * uSize * (float)Math.Exp(pitch * t);
                float theta = t + arm * armSep + phase;

                // Same flare-aware perpendicular jitter SampleArmPos uses, so starting
                // systems visually blend with the background arm stars.
                float taperMult = 1f + armFlareFactor * (1f - t / maxT);
                float jitterMag = (Random.Float(-1f, 1f) + Random.Float(-1f, 1f)) * 0.5f * jitterScale * taperMult;
                float perpAngle = theta + RadMath.HalfPI;

                Vector2 sysPos = new Vector2(r * RadMath.Cos(theta) + jitterMag * RadMath.Cos(perpAngle),
                                              r * RadMath.Sin(theta) + jitterMag * RadMath.Sin(perpAngle));
                sysPos += originOffset;

                starting[inBulgeCount + armIndex].Position = sysPos;
                ClaimedSpots.Add(sysPos);
                step.Advance();
            }
        }

        void PlaceSpiralBackgroundSystems(int numArms, float phase, float uSize,
                                           float armStartR, float armEndR, float pitch,
                                           float armThicknessFrac, float armFlareFactor,
                                           float bulgeFraction, float bulgeRadius,
                                           bool hasBar, float barHalfLen, float barHalfWid,
                                           Vector2 originOffset, ProgressCounter step)
        {
            // Shuffle so file-order doesn't determine arm position (same reasoning as
            // GenerateClusterSystems: predefined systems should not always land in the
            // same spot across unrelated seeds).
            SystemPlaceHolder[] background = Systems.Filter(s => !s.IsStartingSystem);
            Random.Shuffle(background);

            foreach (SystemPlaceHolder sys in background)
            {
                bool inBulge = Random.Float() < bulgeFraction;
                if (inBulge && hasBar)
                    sys.Position = SampleBarPos(phase, uSize, barHalfLen, barHalfWid);
                else if (inBulge)
                    sys.Position = SampleBulgePos(uSize, bulgeRadius, originOffset);
                else
                    sys.Position = SampleArmPos(numArms, phase, uSize, armStartR, armEndR, pitch,
                                                 armThicknessFrac, armFlareFactor, originOffset, 250000f);
                step.Advance();
            }
        }

        Vector2 SampleArmPos(int numArms, float phase, float uSize,
                              float armStartR, float armEndR, float pitch,
                              float armThicknessFrac, float armFlareFactor,
                              Vector2 originOffset, float spacing)
        {
            float maxT = (float)Math.Log(armEndR / armStartR) / pitch; // angular extent of one arm
            float jitterScale = uSize * armThicknessFrac;
            float armSep = RadMath.TwoPI / numArms;

            Vector2 sysPos = default;
            int attempts = 0;
            do
            {
                int arm = Random.InRange(numArms);
                float t = Random.Float() * maxT;
                float r = armStartR * uSize * (float)Math.Exp(pitch * t);
                float theta = t + arm * armSep + phase;

                // Optional taper: arms flare at the base (near armStartR) and taper toward
                // the outer end. flareFactor=0 keeps uniform thickness; >0 multiplies jitter
                // by (1 + flareFactor) at t=0 and 1.0x at t=maxT. Used for Barred so the
                // arms don't read as a thin line where they leave the bar tips.
                float taperMult = 1f + armFlareFactor * (1f - t / maxT);

                // Radial scatter (Barred only -- gated on flare): pushes stars off the perfect
                // log-spiral path so the arms read as a 2D band rather than a 1D curve. Without
                // this, even with perpendicular jitter the arms' centerline is a clean arc.
                if (armFlareFactor > 0f)
                {
                    float rJitter = (Random.Float(-1f, 1f) + Random.Float(-1f, 1f)) * 0.5f * jitterScale * 0.4f * taperMult;
                    r += rJitter;
                }

                // Triangular distribution (sum of two uniforms) approximates a Gaussian
                // cheaply — most stars near the arm centerline, few at the edges.
                float jitterMag = (Random.Float(-1f, 1f) + Random.Float(-1f, 1f)) * 0.5f * jitterScale * taperMult;
                float perpAngle = theta + RadMath.HalfPI;
                sysPos = new Vector2(r * RadMath.Cos(theta) + jitterMag * RadMath.Cos(perpAngle),
                                     r * RadMath.Sin(theta) + jitterMag * RadMath.Sin(perpAngle));
                sysPos += originOffset;

                spacing *= 0.97f;
            } while (!SystemPosOK(sysPos, spacing) && ++attempts < 200);

            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        Vector2 SampleBulgePos(float uSize, float bulgeRadiusFrac, Vector2 originOffset)
        {
            float maxR = uSize * bulgeRadiusFrac;
            float spacing = 200000f;
            Vector2 sysPos = default;
            int attempts = 0;
            do
            {
                sysPos = Random.RandomPointInRing(0f, maxR) + originOffset;
                spacing *= 0.95f;
            } while (!SystemPosOK(sysPos, spacing) && ++attempts < 200);

            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        Vector2 SampleBarPos(float phase, float uSize, float halfLengthFrac, float halfWidthFrac)
        {
            // Sample uniformly in the bar's local frame (long axis = X), then rotate by phase
            // so the bar inherits the same galactic orientation as the arms attached to it.
            float halfLen = uSize * halfLengthFrac;
            float halfWid = uSize * halfWidthFrac;
            float c = RadMath.Cos(phase);
            float s = RadMath.Sin(phase);
            float spacing = 220000f;
            Vector2 sysPos = default;
            int attempts = 0;
            do
            {
                float bx = Random.Float(-halfLen, halfLen);
                float by = Random.Float(-halfWid, halfWid);
                sysPos = new Vector2(bx * c - by * s, bx * s + by * c);
                spacing *= 0.95f;
            } while (!SystemPosOK(sysPos, spacing) && ++attempts < 200);

            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        void GenerateBigClusters(ProgressCounter step)
        {
            // Divides the galaxy to several sectors and populates each sector with stars.
            // 0.35 deviation lets each cluster center wander noticeably off its grid point
            // so a 3x2 / 3x3 layout doesn't look mechanical. Sector ctor enforces a
            // border pad so the wider deviation can't push systems against the universe rim.
            (int numHorizontalSectors, int numVerticalSectors) = GetNumSectors(NumOpponents + 1);
            Array<Sector> sectors = GenerateSectors(numHorizontalSectors, numVerticalSectors, 0.35f);
            GenerateClustersStartingSystems(step, sectors);
            GenerateClusterSystems(step, sectors);
        }

        void GenerateSmallClusters(ProgressCounter step)
        {
            // Divides the galaxy to many sectors and populates each sector with stars
            int numSectorsPerAxis = GetNumSectorsPerAxis(NumSystems, NumOpponents + 1);
            float offsetMultiplier = 0.28f / numSectorsPerAxis.UpperBound(4);
            float deviation = 0.07f * numSectorsPerAxis.UpperBound(4);
            Array<Sector> sectors = GenerateSectors(numSectorsPerAxis, numSectorsPerAxis, deviation, offsetMultiplier);
            GenerateClustersStartingSystems(step, sectors, numSectorsPerAxis - 1);
            GenerateClusterSystems(step, sectors);
        }

        (int NumHorizontalSectors, int NumVerticalSectors) GetNumSectors(int numEmpires)
        {
            int numHorizontalSectors = 2;
            int numVerticalSectors = 2;

            if (numEmpires > 9) // 4x4 sectors - probably not applicable (limited empires to 8 by default)
            {
                numHorizontalSectors = 4;
                numVerticalSectors = 4;
            }
            else if (numEmpires > 6) // 3x3 sectors
            {
                numHorizontalSectors = 3;
                numVerticalSectors = 3;
            }
            else if (numEmpires > 4) // 3x2 sectors
            {
                numHorizontalSectors = 3;
            }

            return (NumHorizontalSectors: numHorizontalSectors, NumVerticalSectors: numVerticalSectors);
        }

        // This will divide number of stars by number of empires to get the number of wanted sectors.
        // Then it will use square root to get the number of sector per axis
        int GetNumSectorsPerAxis(int numSystems, int numEmpires)
        {
            int numSectors = numSystems / numEmpires.LowerBound(4); // each sector will have stars as ~player num, minimum of 4
            int numSectorsPerAxis = (int)Math.Sqrt(numSectors) + 1;

            return numSectorsPerAxis.LowerBound(numEmpires / 2);
        }

        Array<Sector> GenerateSectors(int numHorizontalSectors, int numVerticalSectors, float deviation, float offsetMultiplier = 0.1f)
        {
            var sectors = new Array<Sector>();
            for (int h = 1; h <= numHorizontalSectors; ++h)
            {
                for (int v = 1; v <= numVerticalSectors; ++v)
                {
                    sectors.Add(new Sector(Random, UState.Size, numHorizontalSectors, numVerticalSectors,
                                           h, v, deviation, offsetMultiplier));
                }
            }

            return sectors;
        }

        void GenerateClustersStartingSystems(ProgressCounter step, Array<Sector> sectors, int trySpacingNum = 1)
        {
            var claimedSectors = new Array<Sector>();
            var startingSystems = Systems.Filter(s => s.IsStartingSystem);
            if (sectors.Count < startingSystems.Length)
                Log.Error($"Sectors ({sectors.Count}) < starting Systems ({startingSystems.Length})");

            SystemPlaceHolder firstSystem = startingSystems[0];
            Sector initialSector = Random.Item(sectors);
            firstSystem.Position = GenerateSystemInCluster(initialSector, 350000f);
            step.Advance();
            claimedSectors.Add(initialSector);

            for (int i = 1; i < startingSystems.Length; i++) // starting with 2nd (i = 1) item since the first one was added above
            {
                SystemPlaceHolder system = startingSystems[i];
                var remainingSectors = sectors.Filter(s => !claimedSectors.Contains(s));
                int spacing = trySpacingNum;
                var potentialSectors = remainingSectors.Filter(s => IsSuitableSector(s, claimedSectors, spacing));

                while (potentialSectors.Length == 0)
                {
                    spacing--;
                    if (spacing < 0)
                        Log.Error("GenerateClustersStartingSystems: Could not find suitable sectors to add starting system");

                    potentialSectors = remainingSectors.Filter(s => IsSuitableSector(s, claimedSectors, spacing));
                }

                Sector nextSector = Random.Item(potentialSectors);
                system.Position = GenerateSystemInCluster(nextSector, 350000f);
                step.Advance();
                claimedSectors.Add(nextSector);
            }

            bool SpaceBetweenMoreThan(int space, Sector a, Sector b)
            {
                return Math.Abs(a.X - b.X) > space || Math.Abs(a.Y - b.Y) > space;
            }

            bool IsSuitableSector(Sector sector, Array<Sector> list, int space)
            {
                foreach (Sector s in list)
                {
                    if (!SpaceBetweenMoreThan(space, sector, s))
                        return false;
                }

                return true;
            }
        }

        void GenerateClusterSystems(ProgressCounter step, Array<Sector> sectors)
        {
            // Shuffle so the same alphabetically-first SolarSystems/Random/*.xml file
            // doesn't always land in sectors[0] (h=1,v=1) every game. Without this,
            // sector-to-system mapping is purely insertion-ordered and reproducible
            // across unrelated seeds, which gave players a positional bias on the
            // same corner sector.
            SystemPlaceHolder[] nonStarting = Systems.Filter(s => !s.IsStartingSystem);
            Random.Shuffle(nonStarting);

            int i = 0;
            foreach (SystemPlaceHolder sys in nonStarting)
            {
                Sector currentSector = sectors[i];
                sys.Position = GenerateSystemInCluster(currentSector, 300000f);
                step.Advance();
                i = i < sectors.Count - 1 ? i + 1 : 0; // always cycle within the array
            }
        }

        Vector2 GenerateSystemInCluster(Sector sector, float spacing)
        {
            float safetyBreak = 1;
            Vector2 sysPos;
            do
            {
                spacing *= safetyBreak;
                sysPos = sector.GetRandomPosInSector(Random);
                safetyBreak *= 0.99f;
            } while (!SystemPosOK(sysPos, spacing));

            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        struct Sector
        {
            private readonly float SampleRadius;
            private readonly Vector2 Center;
            public readonly int X;
            public readonly int Y;

            public Sector(RandomBase random, float universeSize,
                          int horizontalSectors, int verticalSectors, int horizontalNum, int verticalNum,
                          float deviation, float offsetMultiplier) : this()
            {
                X = horizontalNum;
                Y = verticalNum;
                float xSection = universeSize / horizontalSectors;
                float ySection = universeSize / verticalSectors;
                float offset = universeSize * offsetMultiplier;

                // Border pad keeps cluster bounds (and therefore the systems sampled
                // inside them) away from the universe rim, even when a wide deviation
                // would otherwise push an edge sector hard against the wall.
                float borderPad = universeSize * 0.08f;
                float innerSize = universeSize - borderPad;
                float minBound  = -innerSize;
                float maxBound  = +innerSize;

                // raw center is the center of the sector before generating offset (for gaps)
                Vector2 rawCenter = new Vector2(-universeSize + xSection * (-1 + horizontalNum * 2),
                                             -universeSize + ySection * (-1 + verticalNum * 2));

                // Some deviation in the center of the cluster
                rawCenter = rawCenter.GenerateRandomPointInsideCircle(universeSize * deviation, random);

                float leftX  = (rawCenter.X - xSection).LowerBound(minBound);
                float rightX = (rawCenter.X + xSection).UpperBound(maxBound);
                float topY   = (rawCenter.Y - ySection).LowerBound(minBound) + offset;
                float botY   = (rawCenter.Y + ySection).UpperBound(maxBound) - offset;

                // creating some gaps between clusters; pass innerSize so the edge-detection
                // inside GenerateOffset still fires for sectors clamped to the padded bound.
                GenerateOffset(innerSize, offset, ref leftX, ref rightX);
                GenerateOffset(innerSize, offset, ref topY, ref botY);

                Center = new Vector2((leftX + rightX) * 0.5f, (topY + botY) * 0.5f);

                // Sample radius derives from the GRID section size, not the post-offset
                // bounds. Edge sectors get clamped + offset-shrunk by GenerateOffset above,
                // which would give them a noticeably smaller radius than inner sectors and
                // make their stars visibly crowded for the same star count. Anchoring the
                // radius to the grid section size gives every cluster the same scale, so
                // edge clusters look as airy as inner ones. Subtract `offset` to leave a
                // small natural gap between neighbors. Captured BEFORE the post-creation
                // Center jitter so cluster size stays stable as Center moves.
                //
                // LowerBound: on very dense grids (e.g. SmallClusters numSectorsPerAxis ~15,
                // xSection ~0.067 universeSize) `min - offset` can drift negative because
                // offsetMultiplier floors at 0.28/4 = 0.07. A negative radius silently
                // inverts GenerateRandomPointInsideCircle and makes the safeInner clamp
                // larger than universeSize. Pin to 5% of half-extent as a sane floor.
                SampleRadius = (Math.Min(xSection, ySection) - offset).LowerBound(universeSize * 0.05f);

                // Post-creation jitter: move Center in a random direction by a random
                // magnitude to break the visible grid pattern. Magnitude varies per
                // cluster so neighbors don't shift uniformly together. Clamped to a
                // safe inner box so the cluster never wanders toward the rim.
                float maxJitter = universeSize * 0.18f;
                float safeInner = universeSize - SampleRadius - borderPad;
                Vector2 jitter = random.Direction2D() * random.Float(0f, maxJitter);
                Center = new Vector2(
                    (Center.X + jitter.X).Clamped(-safeInner, safeInner),
                    (Center.Y + jitter.Y).Clamped(-safeInner, safeInner));
            }

            // Offset from borders. Less offset if near one or 2 edges
            void GenerateOffset(float size, float offset, ref float leftOrTop, ref float rightOrBot)
            {
                if (leftOrTop.AlmostEqual(-size))
                {
                    leftOrTop += offset * 0.1f;
                    rightOrBot -= offset * 1.9f;
                }
                else if (rightOrBot.AlmostEqual(size))
                {
                    leftOrTop += offset * 1.9f;
                    rightOrBot -= offset * 0.1f;
                }
                else
                {
                    leftOrTop += offset;
                    rightOrBot -= offset;
                }
            }

            public Vector2 GetRandomPosInSector(RandomBase random) => Center.GenerateRandomPointInsideCircle(SampleRadius, random);
        }

        Vector2 GenerateRandomCorners(int corner) //Added by Gretman for Corners Game type
        {
            //Corner Values
            //0 = Top Left
            //1 = Top Right
            //2 = Bottom Left
            //3 = Bottom Right
            float uSize = UState.Size;
            float SizeX = uSize * 2;     //Allow for new negative coordinates
            float SizeY = uSize * 2;

            double CornerSizeX = SizeX * 0.4;    //20% of map per corner
            double CornerSizeY = SizeY * 0.4;

            double offsetX = 100000;
            double offsetY = 100000;
            if (corner == 1 || corner == 3)
                offsetX = SizeX * 0.6 - 100000;    //This creates a Huge blank "Neutral Zone" between corner areas
            if (corner == 2 || corner == 3)
                offsetY = SizeY * 0.6 - 100000;

            Vector2 sysPos;
            long noinfiniteloop = 0;
            do
            {
                sysPos = new Vector2(Random.Float(-uSize + (float)offsetX, -uSize + (float)(CornerSizeX + offsetX)),
                                     Random.Float(-uSize + (float)offsetY, -uSize + (float)(CornerSizeY + offsetY)));
                noinfiniteloop += 1000;
            }
            //Decrease the acceptable proximity slightly each attempt, so there wont be an infinite loop here on 'tiny' + 'SuperPacked' maps
            while (!SystemPosOK(sysPos, 400000 - noinfiniteloop));
            ClaimedSpots.Add(sysPos);
            return sysPos;
        }

        public void GenerateArm(int numOfStars, float rotation)
        {
            float uSize = UState.Size;
            float num1 = (float)(2f / numOfStars * 2.0 * 3.14159274101257);
            for (int index = 0; index < numOfStars; ++index)
            {
                float num2 = (float)Math.Pow(uSize - 0.0850000008940697 * uSize, index / (float)numOfStars);
                float num3 = index * num1 + rotation;
                float x = RadMath.Cos(num3) * num2;
                float y = RadMath.Sin(num3) * num2;
                Vector2 sysPos = new Vector2(Random.Float(-10000f, 10000f) * index,
                                     (float)(Random.Float(-10000f, 10000f) * (double)index / 4.0));
                sysPos = new Vector2(x, y) + sysPos;
                if (SystemPosOK(sysPos))
                {
                    ClaimedSpots.Add(sysPos);
                }
                else
                {
                    double halfSize = uSize / 2.0;
                    // extra padding to avoid suns existing at the edge of the universe
                    double padding = 0.085 * uSize;
                    float min = (float)(-halfSize + padding);
                    float max = (float)(+halfSize - padding);
                    while (!SystemPosOK(sysPos))
                    {
                        sysPos.X = Random.Float(min, max);
                        sysPos.Y = Random.Float(min, max);
                    }

                    ClaimedSpots.Add(sysPos);
                }
            }
        }

        bool IsInUniverseBounds(Vector2 sysPos)
        {
            float uSize = UState.Size;
            return -uSize < sysPos.X && sysPos.X < uSize
                && -uSize < sysPos.Y && sysPos.Y < uSize;
        }

        bool SystemPosOK(Vector2 sysPos)
        {
            return SystemPosOK(sysPos, 300000f);
        }

        bool SystemPosOK(Vector2 sysPos, float spacing)
        {
            if (!IsInUniverseBounds(sysPos))
                return false;

            for (int i = 0; i < ClaimedSpots.Count; ++i)
            {
                Vector2 claimed = ClaimedSpots[i];
                if (sysPos.InRadius(claimed, spacing))
                    return false;
            }
            return true;
        }

    }
}
