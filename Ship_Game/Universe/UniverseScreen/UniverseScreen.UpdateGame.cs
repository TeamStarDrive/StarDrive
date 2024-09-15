using System;
using System.Collections.Generic;
using System.Threading;
using SDGraphics;
using SDUtils;
using System.Diagnostics;
using Ship_Game.Utils;
using Ship_Game.AI;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        readonly object SimTimeLock = new object();

        // Can be used in unit testing to prevent LoadContent() from launching sim thread
        public bool CreateSimThread = true;
        Thread SimThread;

        // This is our current time in simulation time axis [0 .. current .. target]
        public float CurrentSimTime;

        // This is the known end time in simulation time axis [0 ... target]
        public float TargetSimTime;

        // Modifier to increase or reduce simulation fidelity
        int SimFPSModifier;

        readonly PerfTimer TimeSinceLastAutoFPS = new PerfTimer();

        public int CurrentSimFPS => GlobalStats.SimulationFramesPerSecond + SimFPSModifier;
        public int ActualSimFPS => (int)(TurnTimePerf.MeasuredSamples / UState.GameSpeed);

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
                    TargetSimTime += deltaTimeFromUI * UState.GameSpeed;
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
            // process pending saves before entering the main loop 
            CheckForPendingSaves();

            if (UState.Paused || IsSaving)
            {
                // Execute all the actions submitted from UI thread
                // into this Simulation / Empire thread
                InvokePendingSimThreadActions();
                ++SimTurnId;

                UState.Objects.Update(FixedSimTime.Zero/*paused*/);
            }
            else
            {
                if (IsActive)
                {
                    // Edge case: user manually edited global sim FPS
                    while (SimFPSModifier < 0 && CurrentSimFPS < 10)
                        SimFPSModifier += 5;

                    // If we increase GameSpeed, also do less simulation steps to speed things up
                    // And at 0.5x speed, do twice as many steps.
                    // Note: beyond 2x step we suffer major precision issues, so we use clamp
                    float gameSpeed = UState.GameSpeed.UpperBound(1);
                    float fixedTimeStep = (1f / CurrentSimFPS) * gameSpeed;
                    var fixedSimStep = new FixedSimTime(fixedTimeStep);

                    // put a limit to simulation iterations
                    // because sometimes we cannot catch up
                    int MAX_ITERATIONS = (int)(30 * UState.GameSpeed);

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
                        if (TurnTimePerf.MeasuredSamples > 0 && TurnTimePerf.AvgTime * UState.GameSpeed < 0.05f)
                        {
                            ++UState.GameSpeed;
                        }
                        else if (--UState.GameSpeed < 1.0f)
                        {
                            UState.GameSpeed = 1.0f;
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
            InvokePendingSimThreadActions();

            Array<Empire> updated = ProcessTurnEmpires(timeStep);

            if (!UState.Paused)
            {
                UState.Objects.Update(timeStep);
                UpdateMiscComponents(timeStep);
            }

            EndOfTurnUpdate(updated, timeStep);
        }

        /// <summary>
        /// Used to make ships alive at game load
        /// </summary>
        public void WarmUpShipsForLoad()
        {
            // We need to update objects at least once to have visibility
            UState.Objects.InitializeFromSave();

            UpdateDysonSwarms();
            EndOfTurnUpdate(UState.Empires, FixedSimTime.Zero);
        }

        public void UpdateStarDateAndTriggerEvents(float newStarDate)
        {
            UState.StarDate = (float)Math.Round(newStarDate, 1);

            ExplorationEvent evt = ResourceManager.EventByDate(UState.StarDate);
            if (evt != null)
            {
                Log.Info($"Trigger Timed Exploration Event  StarDate:{StarDateString}");
                evt.TriggerExplorationEvent(this);
            }
        }

        // TODO: all of these updates are kind of annoying to set up,
        //       we should consider a Component oriented design now that code is sufficiently clean
        void UpdateMiscComponents(FixedSimTime timeStep)
        {
            EmpireMiscPerf.Start();
            UpdateClickableItems();

            if (anomalyManager != null)
            {
                for (int i = anomalyManager.AnomaliesList.Count - 1; i >= 0; --i)
                {
                    Anomaly anomaly = anomalyManager.AnomaliesList[i];
                    anomaly?.Update(timeStep);
                }
            }

            if (timeStep.FixedTime > 0)
            {
                ExplosionManager.Update(this, timeStep.FixedTime);

                Span<Bomb> bombs = BombList.AsSpan();
                for (int i = bombs.Length - 1; i >= 0; --i)
                {
                    Bomb bomb = bombs[i];
                    bomb?.Update(timeStep);
                }

                Shields?.Update(timeStep);
                FTLManager.Update(this, timeStep);

                // update in reverse, to allow Update() to remove the junk
                for (int i = UState.JunkList.Count - 1; i >= 0; --i)
                    UState.JunkList[i]?.Update(timeStep);
            }
            EmpireMiscPerf.Stop();
        }

        // We are just setting the duration to 0 so update will remove them
        public void ResetConstructionParts(int id)
        {
            for (int i = UState.JunkList.Count - 1; i >= 0; --i)
                UState.JunkList[i]?.TryReset(id);
        }

        /// <summary>Returns list of updated Empires</summary>
        Array<Empire> ProcessTurnEmpires(FixedSimTime timeStep)
        {
            Array<Empire> updated = null;
            if (!UState.Paused && IsActive)
            {
                UpdateDysonSwarms();
                EmpireUpdatePerf.Start();
                updated = UpdateEmpires(timeStep);
                EmpireUpdatePerf.Stop();
            }
            return updated ?? new();
        }

        void UpdateDysonSwarms()
        {
            for (int i = 0; i < UState.DysonSwarmPotentials.Length; i++)
            {
                SolarSystem system = UState.DysonSwarmPotentials[i];
                system.DysonSwarm?.Update();
            }
        }

        /// <summary>
        /// Should be run once at the end of a game turn, once before game start, and once after load.
        /// Anything that the game needs at the start should be placed here.
        /// </summary>
        public void EndOfTurnUpdate(IReadOnlyList<Empire> wereUpdated, FixedSimTime timeStep)
        {
            if (wereUpdated.Count == 0)
                return; // nothing to do

            PostEmpirePerf.Start();
            if (IsActive)
            {
                void PostEmpireUpdate(int start, int end)
                {
                    for (int i = start; i < end; i++)
                    {
                        Empire empire = wereUpdated[i];
                        empire.UpdateMilitaryStrengths();
                        empire.UpdateMoneyLeechedLastTurn();
                    }
                }

                UState.UpdateNumPirateFactions();
                if (UState.Objects.EnableParallelUpdate)
                    Parallel.For(wereUpdated.Count, PostEmpireUpdate, UState.Objects.MaxTaskCores);
                else
                    PostEmpireUpdate(0, wereUpdated.Count);
            }

            PostEmpirePerf.Stop();
        }

        /// <summary>Returns list of updated empires</summary>
        Array<Empire> UpdateEmpires(FixedSimTime timeStep)
        {
            Array<Empire> updated = new();
            for (int i = 0; i < UState.NumEmpires; i++)
            {
                Empire empire = UState.Empires[i];
                if (!empire.IsDefeated && empire.Update(UState, timeStep))
                    updated.Add(empire);
            }
            return updated;
        }

        void HandleGameSpeedChange(InputState input)
        {
            if (input.SpeedReset)
                UState.GameSpeed = 1f;
            else if (input.SpeedUp || input.SpeedDown)
            {
                bool unlimited = Debug || Debugger.IsAttached;
                float speedMin = unlimited ? 0.0625f : 0.25f;
                float speedMax = unlimited ? 128f    : 10f;
                UState.GameSpeed = GetGameSpeedAdjust(input.SpeedUp).Clamped(speedMin, speedMax);
            }
        }

        float GetGameSpeedAdjust(bool increase)
        {
            float speed = UState.GameSpeed;
            return increase
                ? speed <= 1 ? speed * 2 : speed + 1
                : speed <= 1 ? speed / 2 : speed - 1;
        }

        // Thread safe input queue for running UI input on empire thread
        readonly SafeQueue<Action> PendingSimThreadActions = new();
        
        /// <summary>
        /// Invokes all Pending actions. This should only be called from ProcessTurns !!!
        /// </summary>
        public void InvokePendingSimThreadActions()
        {
            while (PendingSimThreadActions.TryDequeue(out Action action))
                action();
        }

        /// <summary>
        /// Queues action to run on the Simulation thread, aka ProcessTurns thread.
        /// </summary>
        public void RunOnSimThread(Action action)
        {
            if (action != null)
            {
                PendingSimThreadActions.Enqueue(action);
            }
            else
            {
                Log.WarningWithCallStack("Null Action passed to RunOnEmpireThread method");
            }
        }
    }
}