using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Threading;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        public readonly ActionPool AsyncDataCollector = new ActionPool();

        void ProcessTurnsMonitored()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            Log.Write(ConsoleColor.Cyan, $"Start Universe.ProcessTurns Thread #{threadId}");
            Log.AddThreadMonitor();
            ProcessTurns();
            Log.RemoveThreadMonitor();
            Log.Write(ConsoleColor.Cyan, $"Stop Universe.ProcessTurns Thread #{threadId}");
        }

        void ProcessTurns()
        {
            int failedLoops = 0; // for detecting cyclic crash loops
            while (ProcessTurnsThread != null)
            {
                try
                {
                    // Wait for Draw() to finish. While SwapBuffers is blocking, we process the turns in between
                    DrawCompletedEvt.WaitOne();
                    if (ProcessTurnsThread == null)
                        break; // this thread is aborting

                    ProcessNextTurn();
                    failedLoops = 0; // no exceptions this turn
                }
                catch (ThreadAbortException)
                {
                    break; // Game over, Make sure to Quit the loop!
                }
                catch (Exception ex)
                {
                    if (++failedLoops > 1)
                    {
                        throw; // the loop is having a cyclic crash, no way to recover
                    }

                    Log.Error(ex, "ProcessTurns crashed");
                }
                finally
                {
                    // if the debug window hits a cyclic crash it can be turned off in game.
                    // i don't see a point in crashing the game because of a debug window error.
                    try
                    {
                        if (Debug)
                            DebugWin?.Update(SimulationDeltaTime);
                    }
                    catch
                    {
                        Debug = false;
                        Log.Warning("DebugWindowCrashed");
                    }

                    // Notify Draw() that taketurns has finished and another frame can be drawn now
                    ProcessTurnsCompletedEvt.Set();
                }
            }
        }

        void ProcessNextTurn()
        {
            float deltaTime = FrameDeltaTime;
            ScreenManager.ExecutePendingEmpireActions();

            if (Paused)
            {
                ++TurnId;
                UpdateAllSystems(0.0f);
                DeepSpaceThread(0.0f);
                RecomputeFleetButtons(true);
            }
            else
            {
                NotificationManager.Update(deltaTime);
                AutoSaveTimer -= deltaTime;

                if (AutoSaveTimer <= 0.0f)
                {
                    AutoSaveTimer = GlobalStats.AutoSaveFreq;
                    DoAutoSave();
                }
                if (IsActive)
                {
                    if (GameSpeed < 1f) //Speed <1.0
                    {
                        if (TurnFlipCounter >= 1)
                        {
                            TurnFlipCounter = 0;
                            ++TurnId;
                            ProcessTurnDelta(deltaTime);
                        }
                        TurnFlipCounter += GameSpeed;
                    }
                    else
                    {
                        // With higher GameSpeed, we take more than 1 turn
                        for (int numTurns = 0; numTurns < GameSpeed && IsActive; ++numTurns)
                        {
                            ++TurnId;
                            ProcessTurnDelta(deltaTime);
                            deltaTime = FrameDeltaTime;
                        }
                    }
                    if (GlobalStats.RestrictAIPlayerInteraction)
                    {
                        if (perfavg5.NumSamples > 0 && perfavg5.AvgTime * GameSpeed < 0.05f)
                            ++GameSpeed;
                        else if (--GameSpeed < 1.0f) GameSpeed = 1.0f;

                    }
                }
            }
        }

        // This is different than normal DeltaTime
        public float SimulationDeltaTime { get; private set; }

        void ProcessTurnDelta(float elapsedTime)
        {
            SimulationDeltaTime = elapsedTime;
            perfavg5.Start(); // total do work perf counter

            GlobalStats.BeamTests = 0;
            GlobalStats.Comparisons = 0;
            GlobalStats.ComparisonCounter += 1;
            GlobalStats.ModuleUpdates = 0;

            if (ProcessTurnEmpires(elapsedTime))
            {
                UpdateShipsAndFleets(elapsedTime);

                // this will update all ship Center coordinates
                ProcessTurnShipsAndSystems(elapsedTime);

                MasterShipList.ApplyPendingRemovals();
                CollisionTime.Start();
                
                // The lock assures that the asyncdatacollocter is finished before the quad manager updates.
                // anything after this lock and before QueueActionForThreading should be thread safe.
                // update spatial manager after ships have moved.
                // all the collisions will be triggered here:
                lock (SpaceManager.LockSpaceManager)
                {
                    SpaceManager.Update(elapsedTime);
                }

                MasterShipList.ApplyPendingRemovals();
                Exception threadException = null;
                Parallel.ForEach(EmpireManager.Empires, empire =>
                {
                    try
                    {
                        empire.Pool.UpdatePools();
                    }
                    catch (Exception ex)
                    {
                        threadException = ex;
                    }
                });

                if (threadException != null) Log.Error(threadException, "update pools failed");
                threadException = null;

                MasterShipList.ApplyPendingRemovals();
                RemoveDeadProjectiles();

                Parallel.ForEach(EmpireManager.Empires, empire =>
                {
                    try
                    {
                        empire.PopulateKnownShips();

                        foreach (var planet in empire.GetPlanets())
                            planet.UpdateSpaceCombatBuildings(
                                elapsedTime); // building weapon timers are in this method. 
                    }
                    catch (Exception ex)
                    {
                        threadException = ex;
                    }
                });

                if (threadException != null) Log.Error(threadException, "update pools failed");
                threadException = null;

                Parallel.ForEach(MasterShipList, ship =>
                {
                    try
                    {
                        ship.AI.UpdateCombatStateAI(elapsedTime);
                    }
                    catch(Exception ex)
                    {
                        threadException = ex;
                    }
                });

                if (threadException != null) Log.Error(threadException, "update pools failed");
                threadException = null;
                
                SubmitNextEmpireUpdate(0.01666667f);
                CollisionTime.Stop();

                ProcessTurnUpdateMisc(elapsedTime);
            }

            perfavg5.Stop();
        }

        /// <summary>
        /// Used to make ships alive at game load
        /// </summary>
        public void WarmUpShipsForLoad()
        {
            const float fixedUpdate = 0.01666667f;

            // makes sure all empire vision is updated.
            UpdateAllShipPositions(fixedUpdate);

            lock (SpaceManager.LockSpaceManager)
            {
                SpaceManager.Update(fixedUpdate);
            }

            foreach (Empire empire in EmpireManager.Empires)
            {
                UpdateShipSensorsAndInfluence(fixedUpdate, empire);
            }

            // TODO: some checks rely on previous frame information, this is a defect
            //       so we run this a second time
            foreach (Empire empire in EmpireManager.Empires)
            {
                UpdateShipSensorsAndInfluence(fixedUpdate, empire);
            }

            UpdateShipsAndFleets(fixedUpdate);

            foreach (var ship in MasterShipList)
            {
                ship.AI.ApplySensorScanResults();
            }

            EmpireManager.Player.PopulateKnownShips();
        }

        void RemoveDeadProjectiles()
        {
            for (int i = 0; i < DeepSpaceShips.Count; i++)
                DeepSpaceShips[i].RemoveDyingProjectiles();

            for (int i = 0; i < SolarSystemList.Count; i++)
            {
                SolarSystem system = SolarSystemList[i];
                for (int j = 0; j < system.ShipList.Count; ++j)
                    system.ShipList[j].RemoveDyingProjectiles();
            }
        }

        void ProcessTurnUpdateMisc(float elapsedTime)
        {
            UpdateClickableItems();

            bool flag1 = false;
            lock (GlobalStats.ClickableSystemsLock)
            {
                for (int i = 0; i < ClickPlanetList.Count; ++i)
                {
                    ClickablePlanets planet = ClickPlanetList[i];
                    if (Input.CursorPosition.InRadius(planet.ScreenPos, planet.Radius))
                    {
                        flag1 = true;
                        TooltipTimer -= 0.01666667f;
                        tippedPlanet = planet;
                    }
                }
            }
            if (TooltipTimer <= 0f && !LookingAtPlanet)
                TooltipTimer = 0.5f;
            if (!flag1)
            {
                ShowingPlanetToolTip = false;
                TooltipTimer = 0.5f;
            }

            bool clickedOnSystem = false;
            if (viewState > UnivScreenState.SectorView)
            {
                lock (GlobalStats.ClickableSystemsLock)
                {
                    for (int i = 0; i < ClickableSystems.Count; ++i)
                    {
                        ClickableSystem system = ClickableSystems[i];
                        if (Input.CursorPosition.InRadius(system.ScreenPos, system.Radius))
                        {
                            sTooltipTimer -= 0.01666667f;
                            tippedSystem = system;
                            clickedOnSystem = true;
                        }
                    }
                }
                if (sTooltipTimer <= 0f)
                    sTooltipTimer = 0.5f;
            }

            if (!clickedOnSystem)
                ShowingSysTooltip = false;

            JunkList.ApplyPendingRemovals();

            if (elapsedTime > 0)
            {
                ExplosionManager.Update(this, elapsedTime);
                MuzzleFlashManager.Update(elapsedTime);
            }

            foreach (Anomaly anomaly in anomalyManager.AnomaliesList)
                anomaly.Update(elapsedTime);
            if (elapsedTime > 0)
            {
                using (BombList.AcquireReadLock())
                {
                    for (int i = 0; i < BombList.Count; ++i)
                    {
                        BombList[i]?.Update(elapsedTime);
                    }
                }
                BombList.ApplyPendingRemovals();
            }
            anomalyManager.AnomaliesList.ApplyPendingRemovals();
            if (elapsedTime > 0)
            {
                ShieldManager.Update();
                FTLManager.Update(this, elapsedTime);

                for (int index = 0; index < JunkList.Count; ++index)
                    JunkList[index].Update(elapsedTime);
            }
            SelectedShipList.ApplyPendingRemovals();
        }

        public void UpdateShipsAndFleets(float elapsedTime)
        {
            PostEmpirePerf.Start();

            if (!Paused && IsActive)
            {
                for (int i = 0; i < EmpireManager.Empires.Count; i++)
                {
                    var empire = EmpireManager.Empires[i];
                    foreach (KeyValuePair<int, Fleet> kv in empire.GetFleetsDict())
                    {
                        kv.Value.SetSpeed();
                    }
                    empire.GetEmpireAI().ThreatMatrix.ProcessPendingActions();
                }

                if (elapsedTime > 0.0f && --shiptimer <= 0.0f)
                {
                    shiptimer = 1f;
                    for (int i = 0; i < MasterShipList.Count; i ++)
                    {
                        var ship = MasterShipList[i];
                        {
                            if (!ship.InRadiusOfCurrentSystem)
                            {
                                ship.SetSystem(null);

                                for (int x = 0; x < SolarSystemList.Count; x++)
                                {
                                    SolarSystem system = SolarSystemList[x];

                                    if (ship.InRadiusOfSystem(system))
                                    {
                                        system.SetExploredBy(ship.loyalty);
                                        ship.SetSystem(system);
                                        // No need to keep looping through all other systems
                                        // if one is found -Gretman
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            PostEmpirePerf.Stop();
        }

        int NextEmpireToUpdate = 0;
        int MaxTaskCores = Parallel.NumPhysicalCores - 1;

        void SubmitNextEmpireUpdate(float deltaTime)
        {
            Empire empireToUpdate = EmpireManager.Empires[NextEmpireToUpdate];
            if (++NextEmpireToUpdate >= EmpireManager.Empires.Count)
                NextEmpireToUpdate = 0;

            AsyncDataCollector.SubmitWork(() =>
            {
                UpdateAllShipPositions(deltaTime);
                UpdateShipSensorsAndInfluence(deltaTime, empireToUpdate);
            });
        }

        void UpdateAllShipPositions(float deltaTime)
        {

            // Update all ships and projectors in the universe
            Ship[] allShips = MasterShipList.GetInternalArrayItems();
            Parallel.For(MasterShipList.Count, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                {
                    Ship ship = allShips[i];
                    ship.UpdateModulePositions(deltaTime);

                    // make sure dead and dying ships can be seen.
                    if (!ship.Active && ship.KnownByEmpires.KnownByPlayer)
                        ship.KnownByEmpires.SetSeenByPlayer();
                }
            }, MaxTaskCores);
        }

        void UpdateShipSensorsAndInfluence(float deltaTime, Empire ourEmpire)
        {
            if (ourEmpire.IsEmpireDead())
                return;

            var ourShips = ourEmpire.GetShips();
            Parallel.For(ourShips.Count, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                {
                    Ship ourShip = ourShips[i];
                    ourShip.UpdateSensorsAndInfluence(deltaTime);
                }
            }, MaxTaskCores);

            ourEmpire.UpdateContactsAndBorders(deltaTime);
            ourEmpire.UpdateMilitaryStrengths();
        }

        void ProcessTurnShipsAndSystems(float elapsedTime)
        {
            Perfavg2.Start();
            float shipTime = !Paused ? 0.01666667f : 0;
            DeepSpaceThread(shipTime);
            var realTime = (float)StarDriveGame.Instance.GameTime.ElapsedRealTime.TotalSeconds;

            for (int i = 0; i < SolarSystemList.Count; i++)
            {
                SolarSystemList[i].Update(shipTime, this, realTime);
            }
            Perfavg2.Stop();
        }

        bool ProcessTurnEmpires(float elapsedTime)
        {
            PreEmpirePerf.Start();

            if (!IsActive)
            {
                ShowingSysTooltip = false;
                ShowingPlanetToolTip = false;
            }

            RecomputeFleetButtons(false);

            if (SelectedShip != null)
            {
                ProjectPieMenu(SelectedShip.Position, 0.0f);
            }
            else if (SelectedPlanet != null)
            {
                ProjectPieMenu(SelectedPlanet.Center, 2500f);
            }

            if (GlobalStats.RemnantArmageddon)
            {
                if (!Paused) ArmageddonTimer -= elapsedTime;
                if (ArmageddonTimer < 0.0)
                {
                    ArmageddonTimer = 300f;
                    ++ArmageddonCounter;
                    if (ArmageddonCounter > 5)
                        ArmageddonCounter = 5;
                    for (int i = 0; i < ArmageddonCounter; ++i)
                    {
                        Ship exterminator = Ship.CreateShipAtPoint("Remnant Exterminator", EmpireManager.Remnants,
                                player.WeightedCenter + new Vector2(RandomMath.RandomBetween(-500000f, 500000f),
                                    RandomMath.RandomBetween(-500000f, 500000f)));
                        exterminator.AI.DefaultAIState = AIState.Exterminate;
                    }
                }
            }

            //clear out general object removal.
            TotallyRemoveGameplayObjects();
            MasterShipList.ApplyPendingRemovals();

            if (Paused)
            {
                PreEmpirePerf.Stop();
                return true;
            }

            PreEmpirePerf.Stop();

            if (IsActive)
            {
                EmpireUpdatePerf.Start();
                for (var i = 0; i < EmpireManager.NumEmpires; i++)
                {
                    Empire empire = EmpireManager.Empires[i];
                    if (empire.data.Defeated) continue;
                    empire.Update(elapsedTime);
                }

                MasterShipList.ApplyPendingRemovals();

                EmpireUpdatePerf.Stop();
                return true;
            }

            return false;
        }

        public Vector2 PathMapPointToWorld(int x, int y, int universeOffSet)
        {
            return new Vector2((x - universeOffSet) * PathMapReducer,
                (y - universeOffSet) * PathMapReducer);
        }

        public Point WorldToPathMap(Vector2 worldPostion, int universeOffSet)
        {
            int x = universeOffSet;
            int y = universeOffSet;
            float xround = worldPostion.X > 0 ? .5f : -.5f;
            float yround = worldPostion.Y > 0 ? .5f : -.5f;
            x += (int)(worldPostion.X / PathMapReducer + xround);
            y += (int)(worldPostion.Y / PathMapReducer + yround);
            y = y.Clamped(0, universeOffSet * 2);
            x = x.Clamped(0, universeOffSet * 2);
            return new Point(x, y);
        }

        public void DeepSpaceThread(float elapsedTime)
        {
            SpaceManager.GetDeepSpaceShips(DeepSpaceShips);

            for (int i = 0; i < DeepSpaceShips.Count; i++)
            {
                DeepSpaceShips[i].Update(elapsedTime);
            }
        }

        public void UpdateAllSystems(float elapsedTime)
        {
            if (IsExiting)
                return;
            var realTime = (float)StarDriveGame.Instance.GameTime.ElapsedRealTime.TotalSeconds;
            foreach (SolarSystem system in SolarSystemList)
                system.Update(elapsedTime, this, realTime);
        }

        void HandleGameSpeedChange(InputState input)
        {
            if (input.SpeedReset)
                GameSpeed = 1f;
            else if (input.SpeedUp || input.SpeedDown)
            {
                bool unlimited = GlobalStats.UnlimitedSpeed || Debug;
                float speedMin = unlimited ? 0.0625f : 0.25f;
                float speedMax = unlimited ? 128f    : 6f;
                GameSpeed = GetGameSpeedAdjust(input.SpeedUp).Clamped(speedMin, speedMax);
            }
        }

        float GetGameSpeedAdjust(bool increase)
        {
            return increase
                ? GameSpeed <= 1 ? GameSpeed * 2 : GameSpeed + 1
                : GameSpeed <= 1 ? GameSpeed / 2 : GameSpeed - 1;
        }
    }
}