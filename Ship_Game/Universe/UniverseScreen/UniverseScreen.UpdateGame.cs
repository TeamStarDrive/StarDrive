using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Empires;
using Ship_Game.Fleets;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Threading;
using Ship_Game.Empires.Components;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        readonly object SimTimeLock = new object();

        // Can be used in unit testing to prevent LoadContent() from launching sim thread
        public bool CreateSimThread = true;
        Thread SimThread;

        // This is our current time in simulation time axis [0 .. current .. target]
        float CurrentSimTime;

        // This is the known end time in simulation time axis [0 ... target]
        float TargetSimTime;

        // Modifier to increase or reduce simulation fidelity
        int SimFPSModifier;

        readonly PerfTimer TimeSinceLastAutoFPS = new PerfTimer();

        int CurrentSimFPS => GlobalStats.SimulationFramesPerSecond + SimFPSModifier;
        int ActualSimFPS => (int)(TurnTimePerf.MeasuredSamples / GameSpeed);

        /// <summary>
        /// NOTE: This must be called from UI Update thread to advance the simulation time forward
        /// GameSpeed modifies the target simulation time advancement
        /// Simulation time can be advanced by any arbitrary amount
        /// </summary>
        public void AdvanceSimulationTargetTime(float deltaTimeFromUI)
        {
            lock (SimTimeLock)
            {
                // Only advance simulation time if lag is less than 1 second
                float lag = TargetSimTime - CurrentSimTime;
                if (lag <= 1.0f)
                {
                    TargetSimTime += deltaTimeFromUI * GameSpeed;
                }
            }
        }

        void UniverseSimMonitored()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            Log.Write(ConsoleColor.Cyan, $"Start Universe.ProcessTurns Thread #{threadId}");
            Log.AddThreadMonitor();
            UniverseSimulationLoop();
            Log.RemoveThreadMonitor();
            Log.Write(ConsoleColor.Cyan, $"Stop Universe.ProcessTurns Thread #{threadId}");
        }

        // This is the main loop which runs the entire game simulation
        void UniverseSimulationLoop()
        {
            int failedLoops = 0; // for detecting cyclic crash loops

            while (SimThread != null)
            {
                try
                {
                    // Wait for Draw() to finish.
                    // While SwapBuffers is blocking, we process the turns in between
                    DrawCompletedEvt.WaitOne();
                    if (SimThread == null)
                        break; // this thread is aborting

                    ProcessSimTurnsPerf.Start();
                    ProcessSimulationTurns();
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
                    ProcessSimTurnsPerf.Stop();

                    // Notify Draw() that taketurns has finished and another frame can be drawn now
                    //ProcessTurnsCompletedEvt.Set();
                }
            }
        }

        void ProcessSimulationTurns()
        {
            if (Paused)
            {
                // Execute all the actions submitted from UI thread
                // into this Simulation / Empire thread
                ScreenManager.InvokePendingEmpireThreadActions();
                ++SimTurnId;

                // recalculates empire stats and updates lists using current shiplists
                EndOfTurnUpdate(FixedSimTime.Zero/*paused*/);

                Objects.Update(FixedSimTime.Zero/*paused*/);
                RecomputeFleetButtons(true);
            }
            else
            {
                CheckAutoSaveTimer();

                if (IsActive)
                {
                    // Edge case: user manually edited global sim FPS
                    while (SimFPSModifier < 0 && CurrentSimFPS < 10)
                        SimFPSModifier += 5;

                    // If we increase GameSpeed, also do less simulation steps to speed things up
                    // And at 0.5x speed, do twice as many steps.
                    // Note: beyond 2x step we suffer major precision issues, so we use clamp
                    float gameSpeed = GameSpeed.UpperBound(1);
                    float fixedTimeStep = (1f / CurrentSimFPS) * gameSpeed;
                    var fixedSimStep = new FixedSimTime(fixedTimeStep);

                    // put a limit to simulation iterations
                    // because sometimes we cannot catch up
                    int MAX_ITERATIONS = (int)(30 * GameSpeed);

                    // run the allotted number of game turns
                    // if Simulation FPS is `10` and game speed is `0.5`, this will run 5x per second
                    // if Simulation FPS is `60` and game speed is `4.0`, this will run 240x per second
                    // if the game freezes due to rendering or some other issue,
                    // the simulation time sink will record the missed time and process missed turns
                    int simIterations = 0;
                    for (; simIterations < MAX_ITERATIONS; ++simIterations)
                    {
                        lock (SimTimeLock)
                        {
                            float newSimTime = CurrentSimTime + fixedSimStep.FixedTime;
                            if (newSimTime > TargetSimTime)
                                break;
                            CurrentSimTime = newSimTime;
                        }

                        ++SimTurnId;
                        
                        TurnTimePerf.Start();
                        SingleSimulationStep(fixedSimStep);
                        TurnTimePerf.Stop();
                    }

                    AutoAdjustSimulationFrameRate();

                    if (GlobalStats.RestrictAIPlayerInteraction)
                    {
                        if (TurnTimePerf.MeasuredSamples > 0 && TurnTimePerf.AvgTime * GameSpeed < 0.05f)
                        {
                            ++GameSpeed;
                        }
                        else if (--GameSpeed < 1.0f)
                        {
                            GameSpeed = 1.0f;
                        }
                    }
                }
            }
        }

        void AutoAdjustSimulationFrameRate()
        {
            if (TimeSinceLastAutoFPS.Elapsed > 1.5f)
            {
                TimeSinceLastAutoFPS.Start();

                // Are we running slowly?
                if (CurrentSimFPS > 10 && TurnTimePerf.MeasuredTotal > 0.7f)
                {
                    SimFPSModifier -= 5;
                }
                else if (SimFPSModifier < 0 && TurnTimePerf.MeasuredTotal < 0.4f)
                {
                    SimFPSModifier += 5;
                }
            }
        }

        // This does a single simulation step with fixed time step
        public void SingleSimulationStep(FixedSimTime timeStep)
        {
            ScreenManager.InvokePendingEmpireThreadActions();
            if (ProcessTurnEmpires(timeStep))
            {
                UpdateInfluenceForAllEmpires(timeStep);

                Objects.Update(timeStep);

                ProcessTurnUpdateMisc(timeStep);
                EndOfTurnUpdate(timeStep);
            }
        }

        void CheckAutoSaveTimer()
        {
            GameBase game = GameBase.Base;
            if (LastAutosaveTime == 0f)
                LastAutosaveTime = game.TotalElapsed;

            float timeSinceLastAutoSave = (game.TotalElapsed - LastAutosaveTime);
            if (timeSinceLastAutoSave >= GlobalStats.AutoSaveFreq)
            {
                LastAutosaveTime = game.TotalElapsed;
                AutoSaveCurrentGame();
            }
        }

        /// <summary>
        /// Used to make ships alive at game load
        /// </summary>
        public void WarmUpShipsForLoad()
        {
            foreach (Empire empire in EmpireManager.Empires)
                RemoveDuplicateProjectorWorkAround(empire); 

            // We need to update objects at least once to have visibility
            Objects.InitializeFromSave();

            // makes sure all empire vision is updated.
            UpdateInfluenceForAllEmpires(FixedSimTime.Zero);
            EndOfTurnUpdate(FixedSimTime.Zero);
        }

        public void UpdateStarDateAndTriggerEvents(float newStarDate)
        {
            StarDate = (float)Math.Round(newStarDate, 1);

            ExplorationEvent evt = ResourceManager.EventByDate(StarDate);
            if (evt != null)
            {
                Log.Info($"Trigger Timed Exploration Event  StarDate:{StarDate}");
                evt.TriggerExplorationEvent(this);
            }
        }

        void ProcessTurnUpdateMisc(FixedSimTime timeStep)
        {
            EmpireMiscPerf.Start();
            UpdateClickableItems();

            JunkList.ApplyPendingRemovals();

            for (int i = 0; i < anomalyManager.AnomaliesList.Count; i++)
            {
                Anomaly anomaly = anomalyManager.AnomaliesList[i];
                anomaly.Update(timeStep);
            }

            anomalyManager.AnomaliesList.ApplyPendingRemovals();

            if (timeStep.FixedTime > 0)
            {
                ExplosionManager.Update(this, timeStep.FixedTime);
                MuzzleFlashManager.Update(timeStep.FixedTime);

                using (BombList.AcquireReadLock())
                {
                    for (int i = 0; i < BombList.Count; ++i)
                    {
                        BombList[i]?.Update(timeStep);
                    }
                }
                BombList.ApplyPendingRemovals();

                ShieldManager.Update();
                FTLManager.Update(this, timeStep);

                for (int index = 0; index < JunkList.Count; ++index)
                    JunkList[index].Update(timeStep);
            }
            SelectedShipList.ApplyPendingRemovals();
            EmpireMiscPerf.Stop();
        }

        public readonly int MaxTaskCores = Parallel.NumPhysicalCores - 1;

        // FB todo: this a work around from duplicate SSP create somewhere in the game but are not seen before loading the game
        void RemoveDuplicateProjectorWorkAround(Empire empire)
        {
            var ourSSPs = empire.OwnedProjectors;
            for (int i = ourSSPs.Count - 1; i >= 0; i--)
            {
                Ship projector = ourSSPs[i];
                Vector2 center = projector.Center;
                var sspInSameSpot = ourSSPs.Filter(s => s.Center.AlmostEqual(center, 1));
                if (sspInSameSpot.Length > 1)
                {
                    ((IEmpireShipLists)empire).RemoveShipAtEndOfTurn(projector);
                    projector.QueueTotalRemoval();
                    Log.Error($"Removed Duplicate SSP for {empire.Name} - Center {center}");
                }
            }
        }

        // sensor scan is heavy
        void UpdateInfluenceForAllEmpires(FixedSimTime timeStep)
        {
            EmpireInfluPerf.Start();

            for (int i = 0; i < EmpireManager.Empires.Count; ++i)
            {
                Empire empireToUpdate = EmpireManager.Empires[i];
                empireToUpdate.UpdateContactsAndBorders(timeStep);
            }

            EmpireInfluPerf.Stop();
        }

        bool ProcessTurnEmpires(FixedSimTime timeStep)
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

            // todo figure what to do with this
            /*
            if (GlobalStats.RemnantArmageddon)
            {
                ArmageddonCountdown(timeStep);
            }
                ArmageddonTimer -= timeStep.FixedTime;
                if (ArmageddonTimer < 0f)
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
            ArmageddonCountdown(timeStep);
            */

            PreEmpirePerf.Stop();
            
            if (!Paused && IsActive)
            {
                EmpireUpdatePerf.Start();
                UpdateEmpires(timeStep);
                EmpireUpdatePerf.Stop();
            }
            
            return !Paused;
        }

        /// <summary>
        /// Should be run once at the end of a game turn, once before game start, and once after load.
        /// Anything that the game needs at the start should be placed here.
        /// </summary>
        public void EndOfTurnUpdate(FixedSimTime timeStep)
        {
            PostEmpirePerf.Start();
            if (IsActive)
            {
                void PostEmpireUpdate(int start, int end)
                {
                    for (int i = start; i < end; i++)
                    {
                        var empire = EmpireManager.Empires[i];
                        empire.AIManagedShips.Update();
                        empire.UpdateMilitaryStrengths();
                        empire.AssessSystemsInDanger(timeStep);
                        empire.GetEmpireAI().ThreatMatrix.ProcessPendingActions();
                        foreach (KeyValuePair<int, Fleet> kv in empire.GetFleetsDict())
                        {
                            if (kv.Value.Ships.NotEmpty)
                            {
                                kv.Value.AveragePosition();
                                kv.Value.SetSpeed();
                            }
                        }
                    }
                }
                Parallel.For(EmpireManager.Empires.Count, PostEmpireUpdate, MaxTaskCores);
            }

            PostEmpirePerf.Stop();
        }

        void UpdateEmpires(FixedSimTime timeStep)
        {
            for (int i = 0; i < EmpireManager.NumEmpires; i++)
            {
                Empire empire = EmpireManager.Empires[i];
                if (!empire.data.Defeated)
                {
                    empire.Update(timeStep);
                }
            }
        }

        /*
        void ArmageddonCountdown(FixedSimTime timeStep)
        {
            ArmageddonTimer -= timeStep.FixedTime;
            if (ArmageddonTimer < 0f)
            {
                ArmageddonTimer = 300f;
                ++ArmageddonCounter;
                if (ArmageddonCounter > 5)
                    ArmageddonCounter = 5;
                for (int i = 0; i < ArmageddonCounter; ++i)
                {
                    var exterminator = Ship.CreateShipAtPoint("Remnant Exterminator", EmpireManager.Remnants,
                                                              player.WeightedCenter + RandomMath.Vector2D(500_000f));
                    exterminator.AI.DefaultAIState = AIState.Exterminate;
                }
            }
        }*/

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