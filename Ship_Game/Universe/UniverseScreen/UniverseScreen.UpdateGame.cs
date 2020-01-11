using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ship_Game
{
    public partial class UniverseScreen
    {
        void ProcessTurnsMonitored()
        {
            Log.AddThreadMonitor();
            ProcessTurns();
            Log.RemoveThreadMonitor();
        }

        void ProcessTurns()
        {
            int failedLoops = 0; // for detecting cyclic crash loops
            while (true)
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

            if (Paused)
            {
                ++TurnId;

                UpdateAllSystems(0.0f);
                DeepSpaceThread(0.0f);
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

        void PathGridtranslateBordernode(Empire empire, byte weight, byte[,] grid)
        {
            //this.reducer = (int)(Empire.ProjectorRadius *.5f  );
            int granularity = (int) (UniverseSize / PathMapReducer);
            foreach (var node in empire.BorderNodes)
            {
                byte modifiedWeight = weight;

                Point point = WorldToPathMap(node.Position, granularity);


                Vector2 upscale = new Vector2(point.X * PathMapReducer,
                    point.Y * PathMapReducer);

                if (modifiedWeight != 0 && modifiedWeight < 81 && upscale.InRadius(node.Position, node.Radius))
                {
                    grid[point.X, point.Y] = modifiedWeight;

                }

                float increaser = modifiedWeight == 0 ? 1.25f : 1;

                ApplyWeightToMapArea(node.Position,node.Radius * increaser, modifiedWeight, granularity, grid);
            }
        }

        public bool MapPointInWorldRadius(Point mapPoint, Vector2 worldPosition, float worldRadius, int universeOffset)
        {
            worldRadius -= PathMapReducer * .25f;
            Vector2 mapInWorld = PathMapPointToWorld(mapPoint.X, mapPoint.Y, universeOffset);
            return mapInWorld.InRadius(worldPosition, worldRadius);
        }

        public void ApplyWeightToMapArea(Vector2 worldPosition, float worldRadius, byte weight, int universeOffset, byte[,] grid)
        {
            Point topLeft = WorldToPathMap(new Vector2 (worldPosition.X - worldRadius , worldPosition.Y - worldRadius), universeOffset);
            Point bottomRight = WorldToPathMap(new Vector2(worldPosition.X + worldRadius, worldPosition.Y + worldRadius), universeOffset);

            for (int x = topLeft.X; x <= bottomRight.X  ; x++)
                for (int y = topLeft.Y  ; y <= bottomRight.Y ; y++)
                {
                    if (grid[x, y] == 0) continue;
                    if (MapPointInWorldRadius(new Point(x,y), worldPosition, worldRadius, universeOffset))
                        grid[x, y] = weight;
                }
        }

        // This is different than normal DeltaTime
        public float SimulationDeltaTime { get; private set; }

        void ProcessTurnDelta(float elapsedTime)
        {
            SimulationDeltaTime = elapsedTime;
            perfavg5.Start(); // total do work perf counter

            GlobalStats.BeamTests     = 0;
            GlobalStats.Comparisons   = 0;
            GlobalStats.ComparisonCounter += 1;
            GlobalStats.ModuleUpdates = 0;

            if (ProcessTurnEmpires(elapsedTime))
                return;

            UpdateShipsAndFleets(elapsedTime);

            // this will update all ship Center coordinates
            ProcessTurnShipsAndSystems(elapsedTime);

            // update spatial manager after ships have moved.
            // all the collisions will be triggered here:
            SpaceManager.Update(elapsedTime);

            ProcessTurnUpdateMisc(elapsedTime);

            // bulk remove all dead projectiles to prevent their update next frame
            RemoveDeadProjectiles();

            perfavg5.Stop();
            Lag = perfavg5.AvgTime;
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
            MasterShipList.ApplyPendingRemovals();
            if (perStarDateTimer <= StarDate)
            {
                perStarDateTimer = StarDate + 0.1f;
                perStarDateTimer = (float) Math.Round(perStarDateTimer, 1);
                empireShipCountReserve = EmpireManager.Empires.Sum(empire =>
                    {
                        if (empire == player || empire.data.Defeated || empire.isFaction)
                            return 0;
                        return empire.EmpireShipCountReserve;
                    }
                );
                globalshipCount = MasterShipList.Filter(ship => (ship.loyalty != null && !ship.loyalty.isPlayer && !ship.loyalty.isFaction) &&
                                                                  ship.shipData.Role != ShipData.RoleName.troop &&
                                                                  ship.Mothership == null).Length;
            }
        }

        void UpdateShipsAndFleets(float elapsedTime)
        {
            perfavg4.Start();

            if (elapsedTime > 0.0f && shiptimer <= 0.0f)
            {
                shiptimer = 1f;

                // @todo REMOVE THIS LOOP BASED RADIUS CHECKING AND USE QUADTREE INSTEAD
                for (int i = 0; i < MasterShipList.Count; i++)
                {
                    Ship ship = MasterShipList[i];
                    if (!ship.InRadiusOfCurrentSystem)
                    {
                        ship.SetSystem(null);

                        for (int x = 0; x < SolarSystemList.Count; x++)
                        {
                            SolarSystem system = SolarSystemList[x];

                            if (!ship.InRadiusOfSystem(system))
                                continue;
                            system.SetExploredBy(ship.loyalty);
                            ship.SetSystem(system);
                            break; // No need to keep looping through all other systems if one is found -Gretman
                        }
                    }
                }
            }

            for (int i = 0; i < EmpireManager.NumEmpires; i++)
            {
                foreach (KeyValuePair<int, Fleet> kv in EmpireManager.Empires[i].GetFleetsDict())
                {
                    kv.Value.SetSpeed();
                }
            }

            perfavg4.Stop();
        }

        void ProcessTurnShipsAndSystems(float elapsedTime)
        {
            Perfavg2.Start();
#if !PLAYERONLY
            DeepSpaceThread(elapsedTime);
            var realTime = (float)StarDriveGame.Instance.GameTime.ElapsedRealTime.TotalSeconds;
            for (int i = 0; i < SolarSystemList.Count; i++)
            {
                SolarSystemList[i].Update(!Paused ? 0.01666667f : 0.0f, this, realTime);
            }
#else
            FleetTask DeepSpaceTask = FleetTask.Factory.StartNew(this.DeepSpaceThread);
            foreach (SolarSystem solarsystem in this.SolarSystemDict.Values)
            {
                SystemUpdaterTaskBased(solarsystem);
            }
            if (DeepSpaceTask != null)
                DeepSpaceTask.Wait();
 #endif
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
                                player.GetWeightedCenter() + new Vector2(RandomMath.RandomBetween(-500000f, 500000f),
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
                return false;
            }

            bool rebuildPathingMap = false; // REBUILD WHAT??? Pathing map.
            if (IsActive)
            {
                for (int i = 0; i < EmpireManager.NumEmpires; i++)
                {
                    Empire empire = EmpireManager.Empires[i];

                    empire.ResetForcePool();
                    empire.AddShipsToForcePoolFromShipsToAdd();
                    rebuildPathingMap = empire.UpdateContactsAndBorders(0.01666667f);
                }
            }

            if (rebuildPathingMap)
                DoPathingMapRebuild();

            PreEmpirePerf.Stop();

            if (!IsActive)
                return true;

            EmpireUpdatePerf.Start();
            for (var i = 0; i < EmpireManager.NumEmpires; i++)
            {
                Empire empire = EmpireManager.Empires[i];
                empire.Update(elapsedTime);
            }
            MasterShipList.ApplyPendingRemovals();

            shiptimer -= elapsedTime; // 0.01666667f;//
            EmpireUpdatePerf.Stop();
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

        public void DoPathingMapRebuild()
        {
            PathMapReducer = (int) (SubSpaceProjectors.Radius);
            int universeOffSet = (int) (UniverseSize  / PathMapReducer);
            int elegran = universeOffSet * 2;
            int elements =0;
            var power2 = new int[] {0,32, 64, 128, 256, 512, 1024 };
            for (int x = 0; x < power2.Length; x++)
            {
                int power = power2[x];
                if (power < elegran) continue;
                elements = power;
                break;
            }
            byte[,] grid = new byte[elements, elements];
            for (int x = 0; x < elements; x++)
            for (int y = 0; y < elements; y++)
            {
                    if (x > elegran || y > elegran)
                        grid[x, y] = 0;
                    else
                        grid[x, y] = 80;
            }
            bool blockSystems = !FTLInNuetralSystems || EnemyFTLModifier < 1 || FTLModifier < 1;
            foreach (var ss in SolarSystemDict)
            {
                var point = WorldToPathMap(ss.Value.Position, universeOffSet);
                grid[point.X, point.Y] = 0;
                byte weight = blockSystems ? (byte)0 : (byte)90;
                ApplyWeightToMapArea(ss.Value.Position, ss.Value.Radius, weight, universeOffSet, grid);

                foreach(var p in PlanetsDict)
                {
                    point = WorldToPathMap(p.Value.Center, universeOffSet);
                    grid[point.X, point.Y] = 0;
                }
            }

            for (int i = 0; i < EmpireManager.NumEmpires; i++)
            {
                var empire = EmpireManager.Empires[i];

                byte[,] grid1 = new byte[elements, elements];
                Array.Copy(grid, grid1, grid.Length);

                foreach (KeyValuePair<Empire, Relationship> rels in empire.AllRelations)
                {
                    if (!rels.Value.Known)
                        continue;
                    if (rels.Value.Treaty_Alliance)
                        PathGridtranslateBordernode(rels.Key, 1, grid1);
                    else if (rels.Value.AtWar || rels.Value.Treaty_OpenBorders)
                        PathGridtranslateBordernode(rels.Key, 80, grid1);
                    else
                        PathGridtranslateBordernode(rels.Key, 0, grid1);
                }
                PathGridtranslateBordernode(empire, 1, grid1);
                empire.grid = grid1;
                empire.granularity = universeOffSet;
            }
        }

        void DeepSpaceThread(float elapsedTime)
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