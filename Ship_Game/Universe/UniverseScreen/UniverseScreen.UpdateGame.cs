using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;

namespace Ship_Game
{
    public sealed partial class UniverseScreen
    {
        private void ProcessTurns()
        {
            int failedLoops = 0; // for detecting cyclic crash loops
            while (true)
            {
                try
                {
                    // Wait for Draw() to finish. While SwapBuffers is blocking, we process the turns inbetween
                    DrawCompletedEvt.WaitOne();
                    if (ProcessTurnsThread == null)
                        return; // this thread is aborting

                    float deltaTime = (float) zgameTime.ElapsedGameTime.TotalSeconds;
                    if (Paused)
                    {
                        ++FrameId;

                        UpdateAllSystems(0.0f);
                        foreach (Ship ship in MasterShipList)
                        {
                            if (viewState <= UnivScreenState.SystemView && Frustum.Contains(ship.Position, 2000f))
                            {
                                ship.InFrustum = true;
                                ship.GetSO().Visibility = ObjectVisibility.Rendered;
                                ship.GetSO().World = Matrix.Identity
                                                     * Matrix.CreateRotationY(ship.yRotation)
                                                     * Matrix.CreateRotationX(ship.xRotation)
                                                     * Matrix.CreateRotationZ(ship.Rotation)
                                                     * Matrix.CreateTranslation(new Vector3(ship.Center, 0.0f));
                            }
                            else
                            {
                                ship.InFrustum = false;
                                ship.GetSO().Visibility = ObjectVisibility.None;
                            }
                            ship.Update(0);
                        }
                        ClickTimer += deltaTime;
                        ClickTimer2 += deltaTime;
                        pieMenu.Update(zgameTime);
                        PieMenuTimer += deltaTime;
                    }
                    else
                    {
                        ClickTimer += deltaTime;
                        ClickTimer2 += deltaTime;
                        pieMenu.Update(zgameTime);
                        PieMenuTimer += deltaTime;
                        NotificationManager.Update(deltaTime);
                        AutoSaveTimer -= deltaTime;

                        if (AutoSaveTimer <= 0.0f)
                        {
                            AutoSaveTimer = GlobalStats.AutoSaveFreq;
                            DoAutoSave();
                        }
                        if (IsActive)
                        {
                            if (GameSpeed < 1.0f) // default to 0.5x,
                            {
                                if (TurnFlip)
                                {
                                    ++FrameId;
                                    ProcessTurnDelta(deltaTime);
                                }
                                TurnFlip = !TurnFlip;
                            }
                            else
                            {
                                // With higher GameSpeed, we take more than 1 turn
                                for (int numTurns = 0; numTurns < GameSpeed && IsActive; ++numTurns)
                                {
                                    ++FrameId;
                                    ProcessTurnDelta(deltaTime);
                                    deltaTime = (float) zgameTime.ElapsedGameTime.TotalSeconds;
                                }
                            }
#if AUTOTIME
                                if (perfavg5.NumSamples > 0 && perfavg5.AvgTime * GameSpeed < 0.05f)
                                    ++GameSpeed;
                                else if (--GameSpeed < 1.0f) GameSpeed = 1.0f;
                            #endif
                        }
                    }
                    failedLoops = 0; // no exceptions this turn
                }
                catch (ThreadAbortException)
                {
                    return; // Game over, Make sure to Quit the loop!
                }
                catch (Exception ex)
                {
                    if (++failedLoops > 1)
                        throw; // the loop is having a cyclic crash, no way to recover
                    Log.Error(ex, "ProcessTurns crashed");
                }
                finally
                {
                    // Notify Draw() that taketurns has finished and another frame can be drawn now
                    ProcessTurnsCompletedEvt.Set();
                }
            }
        }

        private void PathGridtranslateBordernode(Empire empire, byte weight, byte[,] grid)
        {
            //this.reducer = (int)(Empire.ProjectorRadius *.5f  );
            int granularity = (int) (this.Size.X / this.reducer);
            foreach (var node in empire.BorderNodes)
            {
                SolarSystem ss = node.SourceObject as SolarSystem;
                Planet p = node.SourceObject as Planet;
                if (this.FTLModifier < 1 && ss != null)
                    weight += 20;
                if ((this.EnemyFTLModifier < 1 || !this.FTLInNuetralSystems) && ss != null && weight > 1)
                    weight += 20;
                if (p != null && weight > 1)
                    weight += 20;
                float xround = node.Position.X > 0 ? .5f : -.5f;
                float yround = node.Position.Y > 0 ? .5f : -.5f;
                int ocx = (int) ((node.Position.X / this.reducer) + xround);
                int ocy = (int) ((node.Position.Y / this.reducer) + yround);
                int cx = ocx + granularity;
                int cy = ocy + granularity;
                cy = cy < 0 ? 0 : cy;
                cy = cy > granularity * 2 ? granularity * 2 : cy;
                cx = cx < 0 ? 0 : cx;
                cx = cx > granularity * 2 ? granularity * 2 : cx;
                Vector2 upscale = new Vector2((float) (ocx * this.reducer),
                    (float) (ocy * this.reducer));
                if (Vector2.Distance(upscale, node.Position) < node.Radius)
                    grid[cx, cy] = weight;
                if (weight > 1 || weight == 0 || node.Radius > Empire.ProjectorRadius)
                {
                    float test = node.Radius > Empire.ProjectorRadius ? 1 : 2;
                    int rad = (int) (Math.Ceiling((double) (node.Radius / ((float) reducer) * test)));
                    //rad--;

                    int negx = cx - rad;
                    if (negx < 0)
                        negx = 0;
                    int posx = cx + rad;
                    if (posx > granularity * 2)
                        posx = granularity * 2;
                    int negy = cy - rad;
                    if (negy < 0)
                        negy = 0;
                    int posy = cy + rad;
                    if (posy > granularity * 2)
                        posy = granularity * 2;
                    for (int x = negx; x < posx; x++)
                    for (int y = negy; y < posy; y++)
                    {
                        //if (grid[x, y] >= 80 || grid[x, y] <= weight)
                        {
                            upscale = new Vector2((float) ((x - granularity) * reducer),
                                (float) ((y - granularity) * reducer));
                            if (Vector2.Distance(upscale, node.Position) <= node.Radius * test)
                                grid[x, y] = weight;
                        }
                    }
                }
            }
        }

        private void ProcessTurnDelta(float elapsedTime)
        {
            perfavg5.Start(); // total dowork lag

            PreEmpirePerf.Start();
            zTime = elapsedTime;

            #region PreEmpire

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
                ProjectPieMenu(SelectedPlanet.Position, 2500f);
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
                        Ship.CreateShipAtPoint("Remnant Exterminator", EmpireManager.Remnants,
                                player.GetWeightedCenter() + new Vector2(RandomMath.RandomBetween(-500000f, 500000f),
                                    RandomMath.RandomBetween(-500000f, 500000f)))
                            .AI.DefaultAIState = AIState.Exterminate;
                }
            }


            while (!ShipsToRemove.IsEmpty)
            {
                ShipsToRemove.TryTake(out Ship remove);
                remove.TotallyRemove();
            }
            MasterShipList.ApplyPendingRemovals();

            if (!Paused)
            {
                bool rebuild = false;
                //System.Threading.Tasks.Parallel.ForEach(EmpireManager.Empires, empire =>
                //Parallel.For(EmpireManager.Empires.Count, (start, end) =>
                {
                    //for (int i = start; i < end; i++)
                    for (int i = 0; i < EmpireManager.Empires.Count; i++)
                    {
                        var empire = EmpireManager.Empires[i];

                        if (!empire.isPlayer)
                        {
                            Ship[] forcePool = empire.GetForcePool().ToArray();
                            empire.GetForcePool().Clear();
                            for (int j = forcePool.Length - 1; j >= 0; j--)
                            {
                                Ship ship = forcePool[j];
                                empire.ForcePoolAdd(ship);
                            }
                        }

                        foreach (Ship s in empire.ShipsToAdd)
                        {
                            empire.AddShip(s);
                            if (!empire.isPlayer) empire.ForcePoolAdd(s);
                        }

                        empire.ShipsToAdd.Clear();
                        empire.updateContactsTimer -= 0.01666667f; //elapsedTime;
                        if (empire.updateContactsTimer <= 0f && !empire.data.Defeated)
                        {
                            int check = empire.BorderNodes.Count;
                            empire.ResetBorders();

                            if (empire.BorderNodes.Count != check)
                            {
                                rebuild = true;
                                empire.PathCache.Clear();
                            }
                            foreach (Ship ship in MasterShipList)
                            {
                                //added by gremlin reset border stats.
                                ship.BorderCheck.Remove(empire);
                            }

                            empire.UpdateKnownShips();
                            empire.updateContactsTimer = elapsedTime + RandomMath.RandomBetween(2f, 3.5f);
                        }
                    }
                } //);
                if (rebuild)
                {
                    reducer = (int) (Empire.ProjectorRadius * .75f);
                    int granularity = (int) (Size.X / reducer);
                    int elegran = granularity * 2;
                    int elements = elegran < 128 ? 128 : elegran < 256 ? 256 : elegran < 512 ? 512 : 1024;
                    byte[,] grid = new byte[elements, elements];
                    for (int x = 0; x < elements; x++)
                    for (int y = 0; y < elements; y++)
                    {
                        if (x > elegran || y > elegran)
                            grid[x, y] = 0;
                        else
                            grid[x, y] = 80;
                    }
                    foreach (Planet p in PlanetsDict.Values)
                    {
                        int x = granularity;
                        int y = granularity;
                        float xround = p.Position.X > 0 ? .5f : -.5f;
                        float yround = p.Position.Y > 0 ? .5f : -.5f;
                        x += (int) (p.Position.X / reducer + xround);
                        y += (int) (p.Position.Y / reducer + yround);
                        if (y < 0) y = 0;
                        if (x < 0) x = 0;
                        grid[x, y] = 200;
                    }
                    //System.Threading.Tasks.Parallel.ForEach(EmpireManager.Empires, empire =>
                    //Parallel.For(EmpireManager.Empires.Count, (start, end) =>
                    {
                        //for (int i = start; i < end; i++)
                        for (int i = 0; i < EmpireManager.Empires.Count; i++)
                        {
                            var empire = EmpireManager.Empires[i];

                            byte[,] grid1 = (byte[,]) grid.Clone();
                            PathGridtranslateBordernode(empire, 1, grid1);

                            foreach (KeyValuePair<Empire, Relationship> rels in empire.AllRelations)
                            {
                                if (!rels.Value.Known)
                                    continue;
                                if (rels.Value.Treaty_Alliance)
                                {
                                    PathGridtranslateBordernode(rels.Key, 1, grid1);
                                }
                                if (rels.Value.AtWar)
                                    PathGridtranslateBordernode(rels.Key, 80, grid1);
                                else if (!rels.Value.Treaty_OpenBorders)
                                    PathGridtranslateBordernode(rels.Key, 0, grid1);
                            }

                            empire.grid = grid1;
                            empire.granularity = granularity;
                        }
                    } //);
                }

                #endregion

                PreEmpirePerf.Stop();
                if (!IsActive)
                    return;

                #region Empire

                EmpireUpdatePerf.Start();
                for (var index = 0; index < EmpireManager.Empires.Count; index++)
                {
                    Empire empire = EmpireManager.Empires[index];
                    empire.Update(elapsedTime);
                }
                MasterShipList.ApplyPendingRemovals();

                lock (GlobalStats.AddShipLocker) //needed to fix Issue #629
                {
                    foreach (Ship ship in ShipsToAdd)
                    {
                        MasterShipList.Add(ship);
                    }
                    ShipsToAdd.Clear();
                }
                shiptimer -= elapsedTime; // 0.01666667f;//
                EmpireUpdatePerf.Stop();
            }

            #endregion


            perfavg4.Start();

            #region Mid

            if (elapsedTime > 0.0f && shiptimer <= 0.0f)
            {
                shiptimer = 1f;
                //foreach (Ship ship in (Array<Ship>)this.MasterShipList)
                //var source = Enumerable.Range(0, this.MasterShipList.Count).ToArray();
                //var rangePartitioner = Partitioner.Create(0, source.Length);

                //System.Threading.Tasks.Parallel.ForEach(rangePartitioner, (range, loopState) =>
                //Parallel.For(this.MasterShipList.Count, (start, end) =>
                {
                    //for (int i = start; i < end; i++)
                    for (int i = 0; i < MasterShipList.Count; i++)
                    {
                        Ship ship = MasterShipList[i];
                        foreach (SolarSystem system in SolarSystemList)
                        {
                            if (ship.Position.InRadius(system.Position, 100000.0f))
                            {
                                system.ExploredDict[ship.loyalty] = true;
                                ship.SetSystem(system);
                                break; // No need to keep looping through all other systems if one is found -Gretman
                            }
                        }
                        if (ship.System == null)
                            ship
                                .SetSystem(
                                    null); //Add ships to deepspacemanageer if system is null. Ships are not getting added to the deepspace manager from here. 
                    }
                } //);
            }

            //System.Threading.Tasks.Parallel.ForEach(EmpireManager.Empires, empire =>
            //Parallel.For(EmpireManager.Empires.Count, (start, end) =>
            {
                //for (int i = start; i < end; i++)
                for (int i = 0; i < EmpireManager.Empires.Count; i++)
                {
                    foreach (var kv in EmpireManager.Empires[i].GetFleetsDict())
                    {
                        var fleet = kv.Value;
                        if (fleet.Ships.Count <= 0)
                            continue;
                        using (fleet.Ships.AcquireReadLock())
                        {
                            fleet.Setavgtodestination();
                            fleet.SetSpeed();
                            fleet.StoredFleetPosition = fleet.FindAveragePositionset();
                        }
                    }
                }
            } //);

            GlobalStats.BeamTests = 0;
            GlobalStats.Comparisons = 0;
            ++GlobalStats.ComparisonCounter;
            GlobalStats.ModuleUpdates = 0;
            GlobalStats.ModulesMoved = 0;

            #endregion

            perfavg4.Stop();


            Perfavg2.Start();

            #region Ships

#if !PLAYERONLY

            DeepSpaceThread();

            //Parallel.For(SolarSystemList.Count, (first, last) =>
            {
                //for (int i = first; i < last; i++)
                for (int i = 0; i < SolarSystemList.Count; i++)
                {
                    SystemUpdaterTaskBased(SolarSystemList[i]);
                }
            } //);

            /*
            Array<SolarSystem> peacefulSystems = new Array<SolarSystem>(SolarSystemList.Count / 2);
            Array<SolarSystem> combatSystems   = new Array<SolarSystem>(SolarSystemList.Count / 2);

            foreach (SolarSystem system in SolarSystemList)
            {
                int shipsInCombat = 0;
                foreach (Ship ship in system.ShipList)
                    if (ship.InCombatTimer == 15 && ++shipsInCombat >= 5)
                        break;
                (shipsInCombat < 5 ? peacefulSystems : combatSystems).Add(system);
            }

            //FleetTask DeepSpaceTask = FleetTask.Factory.StartNew(() =>
            {
                DeepSpaceThread();
                foreach (SolarSystem system in combatSystems)
                {
                    SystemUpdaterTaskBased(system);
                }
            }//);

            #if true // use multithreaded update loop
                //var source1 = Enumerable.Range(0, peacefulSystems.Count).ToArray();
                //var normalsystems = Partitioner.Create(0, source1.Length);
                //System.Threading.Tasks.Parallel.ForEach(normalsystems, (range, loopState) =>
                {
                    //standard for loop through each weapon group.
                    //for (int T = range.Item1; T < range.Item2; T++)
                    for (int T = 0; T < peacefulSystems.Count; T++)
                    {
                        SystemUpdaterTaskBased(peacefulSystems[T]);
                    }
                }//);
#else
                foreach(SolarSystem s in peacefulSystems)
                {
                    SystemUpdaterTaskBased(s);
                }
                */
#endif

            //The two above were the originals

            //if (DeepSpaceTask != null)
            //    DeepSpaceTask.Wait();
//#endif
#if PLAYERONLY
            FleetTask DeepSpaceTask = FleetTask.Factory.StartNew(this.DeepSpaceThread);
            foreach (SolarSystem solarsystem in this.SolarSystemDict.Values)
            {
                SystemUpdaterTaskBased(solarsystem);
            }
            if (DeepSpaceTask != null)
                DeepSpaceTask.Wait();
#endif

            #endregion

            Perfavg2.Stop();

            #region end

            //Log.Info(this.zgameTime.TotalGameTime.Seconds - elapsedTime);
            //System.Threading.Tasks.Parallel.Invoke(() =>
            {
                if (elapsedTime > 0f)
                {
                    DeepSpaceManager.Update(elapsedTime, null);
                }
            } //,
            //() =>
            {
                //lock (GlobalStats.ClickableItemLocker)
                this.UpdateClickableItems();
                if (this.LookingAtPlanet)
                    this.workersPanel.Update(elapsedTime);
                bool flag1 = false;
                lock (GlobalStats.ClickableSystemsLock)
                {
                    for (int i = 0; i < this.ClickPlanetList.Count; ++i)
                    {
                        try
                        {
                            UniverseScreen.ClickablePlanets local_12 = this.ClickPlanetList[i];
                            if (Vector2.Distance(new Vector2((float) Mouse.GetState().X, (float) Mouse.GetState().Y),
                                    local_12.ScreenPos) <= local_12.Radius)
                            {
                                flag1 = true;
                                this.TooltipTimer -= 0.01666667f;
                                this.tippedPlanet = local_12;
                            }
                        }
                        catch { }
                    }
                }
                if (this.TooltipTimer <= 0f && !this.LookingAtPlanet)
                    this.TooltipTimer = 0.5f;
                if (!flag1)
                {
                    this.ShowingPlanetToolTip = false;
                    this.TooltipTimer = 0.5f;
                }

                bool flag2 = false;
                if (this.viewState > UniverseScreen.UnivScreenState.SectorView)
                {
                    lock (GlobalStats.ClickableSystemsLock)
                    {
                        for (int local_15 = 0; local_15 < this.ClickableSystems.Count; ++local_15)
                        {
                            UniverseScreen.ClickableSystem local_16 = this.ClickableSystems[local_15];
                            if (Vector2.Distance(new Vector2((float) Mouse.GetState().X, (float) Mouse.GetState().Y),
                                    local_16.ScreenPos) <= local_16.Radius)
                            {
                                this.sTooltipTimer -= 0.01666667f;
                                this.tippedSystem = local_16;
                                flag2 = true;
                            }
                        }
                    }
                    if (this.sTooltipTimer <= 0f)
                        this.sTooltipTimer = 0.5f;
                }
                if (!flag2)
                    this.ShowingSysTooltip = false;
                this.Zrotate += 0.03f * elapsedTime;

                JunkList.ApplyPendingRemovals();

                if (elapsedTime > 0)
                {
                    lock (GlobalStats.ExplosionLocker)
                    {
                        ExplosionManager.Update(elapsedTime);
                        ExplosionManager.ExplosionList.ApplyPendingRemovals();
                    }
                    MuzzleFlashManager.Update(elapsedTime);
                }
                lock (GlobalStats.ExplosionLocker)
                    MuzzleFlashManager.FlashList.ApplyPendingRemovals();
                foreach (Anomaly anomaly in (Array<Anomaly>) this.anomalyManager.AnomaliesList)
                    anomaly.Update(elapsedTime);
                if (elapsedTime > 0)
                {
                    using (BombList.AcquireReadLock())
                    {
                        for (int local_19 = 0; local_19 < this.BombList.Count; ++local_19)
                        {
                            Bomb local_20 = this.BombList[local_19];
                            if (local_20 != null)
                                local_20.Update(elapsedTime);
                        }
                    }
                    BombList.ApplyPendingRemovals();
                }
                this.anomalyManager.AnomaliesList.ApplyPendingRemovals();
            }
            //);
            if (elapsedTime > 0)
            {
                ShieldManager.Update();
                FTLManager.Update(elapsedTime);

                for (int index = 0; index < JunkList.Count; ++index)
                    JunkList[index].Update(elapsedTime);
            }
            this.SelectedShipList.ApplyPendingRemovals();
            this.MasterShipList.ApplyPendingRemovals();
            if (this.perStarDateTimer <= this.StarDate)
            {
                this.perStarDateTimer = this.StarDate + .1f;
                this.perStarDateTimer = (float) Math.Round((double) this.perStarDateTimer, 1);
                this.empireShipCountReserve = EmpireManager.Empires
                    .Where(empire => empire != this.player && !empire.data.Defeated && !empire.isFaction)
                    .Sum(empire => empire.EmpireShipCountReserve);
                this.globalshipCount = this.MasterShipList
                    .Where(ship => (ship.loyalty != null && ship.loyalty != this.player) &&
                                   ship.shipData.Role != ShipData.RoleName.troop && ship.Mothership == null)
                    .Count();
            }

            #endregion

            perfavg5.Stop();
            Lag = perfavg5.AvgTime;

            //if (this.perfavg5.Average() > .1f)
            //    GlobalStats.ForceFullSim = false;
            //else GlobalStats.ForceFullSim = true;
        }

        public void SystemUpdaterTaskBased(SolarSystem system)
        {
            //Array<SolarSystem> list = (Array<SolarSystem>)data;
            // while (true)
            {
                //this.SystemGateKeeper[list[0].IndexOfResetEvent].WaitOne();
                //float elapsedTime = this.ztimeSnapShot;
                float elapsedTime = !this.Paused ? 0.01666667f : 0.0f;
                float realTime = this.zTime; // this.zgameTime.ElapsedGameTime.Seconds;
                {
                    system.DangerTimer -= realTime;
                    system.DangerUpdater -= realTime;
                    if (system.DangerUpdater < 0.0)
                    {
                        system.DangerUpdater = 10f;
                        system.DangerTimer = (double) this.player.GetGSAI()
                                                 .ThreatMatrix
                                                 .PingRadarStr(system.Position,
                                                     100000f * UniverseScreen.GameScaleStatic, this.player) <= 0.0
                            ? 0.0f
                            : 120f;
                    }
                    system.combatTimer -= realTime;


                    if (system.combatTimer <= 0.0)
                        system.CombatInSystem = false;
                    bool viewing = false;
                    this.Viewport.Project(new Vector3(system.Position, 0.0f),
                        this.projection, this.view, Matrix.Identity);
                    if (this.Frustum.Contains(new BoundingSphere(new Vector3(system.Position, 0.0f), 100000f)) !=
                        ContainmentType.Disjoint)
                        viewing = true;
                    else if (this.viewState <= UniverseScreen.UnivScreenState.ShipView)
                    {
                        Rectangle rect = new Rectangle((int) system.Position.X - 100000,
                            (int) system.Position.Y - 100000, 200000, 200000);
                        Vector3 position =
                            this.Viewport.Unproject(new Vector3(500f, 500f, 0.0f),
                                this.projection, this.view, Matrix.Identity);
                        Vector3 direction =
                            this.Viewport.Unproject(new Vector3(500f, 500f, 1f),
                                this.projection, this.view, Matrix.Identity) - position;
                        direction.Normalize();
                        Ray ray = new Ray(position, direction);
                        float num = -ray.Position.Z / ray.Direction.Z;
                        Vector3 vector3 = new Vector3(ray.Position.X + num * ray.Direction.X,
                            ray.Position.Y + num * ray.Direction.Y, 0.0f);
                        Vector2 pos = new Vector2(vector3.X, vector3.Y);
                        if (HelperFunctions.CheckIntersection(rect, pos))
                            viewing = true;
                    }
                    if (system.Explored(player) && viewing)
                    {
                        system.isVisible = viewState <= UnivScreenState.SectorView;
                    }
                    if (system.isVisible && viewState <= UnivScreenState.SystemView)
                    {
                        foreach (Asteroid asteroid in system.AsteroidsList)
                        {
                            asteroid.So.Visibility = ObjectVisibility.Rendered;
                            asteroid.Update(elapsedTime);
                        }
                        foreach (Moon moon in system.MoonList)
                        {
                            moon.So.Visibility = ObjectVisibility.Rendered;
                            moon.UpdatePosition(elapsedTime);
                        }
                    }
                    else
                    {
                        foreach (Asteroid asteroid in system.AsteroidsList)
                        {
                            asteroid.So.Visibility = ObjectVisibility.None;
                        }
                        foreach (Moon moon in system.MoonList)
                        {
                            moon.So.Visibility = ObjectVisibility.None;
                        }
                    }
                    foreach (Planet planet in system.PlanetList)
                    {
                        planet.Update(elapsedTime);
                        if (planet.HasShipyard && system.isVisible)
                            planet.Station.Update(elapsedTime);
                    }

                    for (int i = 0; i < system.ShipList.Count; ++i)
                    {
                        Ship ship = system.ShipList[i];
                        if (ship.System == null)
                            continue;
                        if (!ship.Active || ship.ModuleSlotList.Length == 0) // added by gremlin ghost ship killer
                        {
                            ship.Die(null, true);
                        }
                        else
                        {
                            if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
                            {
                                ship.Inhibited = true;
                                ship.InhibitedTimer = 10f;
                            }
                            //ship.PauseUpdate = true;
                            ship.Update(elapsedTime);
                            if (ship.PlayerShip)
                                ship.ProcessInput(elapsedTime);
                        }
                    }
                    if (!Paused && IsActive)
                        system.spatialManager.Update(elapsedTime, system);
                    system.AsteroidsList.ApplyPendingRemovals();
                } //);
                // this.SystemResetEvents[list[0].IndexOfResetEvent].Set();
            }
        }

        private void DeepSpaceThread()
        {
            float elapsedTime = !Paused ? 0.01666667f : 0.0f;

            DeepSpaceManager.GetDeepSpaceShips(DeepSpaceShips);

            //Parallel.For(0, DeepSpaceShips.Count, (start, end) =>
            {
                //for (int i = start; i < end; i++)
                for (int i = 0; i < DeepSpaceShips.Count; i++)
                {
                    if (!DeepSpaceShips[i].shipInitialized)
                        continue;

                    if (DeepSpaceShips[i].Active && DeepSpaceShips[i].ModuleSlotList.Length != 0)
                    {
                        if (RandomEventManager.ActiveEvent != null && RandomEventManager.ActiveEvent.InhibitWarp)
                        {
                            DeepSpaceShips[i].Inhibited = true;
                            DeepSpaceShips[i].InhibitedTimer = 10f;
                        }

                        if (DeepSpaceShips[i].PlayerShip)
                            DeepSpaceShips[i].ProcessInput(elapsedTime);
                    }
                    else
                    {
                        DeepSpaceShips[i].Die(null, true);
                    }
                    DeepSpaceShips[i].Update(elapsedTime);
                }
            } //);
        }

        public void UpdateAllSystems(float elapsedTime)
        {
            if (IsExiting)
                return;

            foreach (SolarSystem system in SolarSystemList)
            {
                system.DangerTimer -= elapsedTime;
                system.DangerUpdater -= elapsedTime;
                foreach (KeyValuePair<Empire, SolarSystem.PredictionTimeout> predict in system.predictionTimeout)
                    predict.Value.update(elapsedTime);

                if (system.DangerUpdater < 0.0f)
                {
                    system.DangerUpdater = 10f;
                    system.DangerTimer = player.GetGSAI()
                                             .ThreatMatrix
                                             .PingRadarStr(system.Position, 100000f * GameScaleStatic, player) <= 0.0
                        ? 0.0f
                        : 120f;
                }
                system.combatTimer -= elapsedTime;
                if (system.combatTimer <= 0.0f)
                    system.CombatInSystem = false;

                if (elapsedTime > 0.0f)
                    system.spatialManager.Update(elapsedTime, system);

                bool inFrustrum = false;
                if (Frustum.Contains(system.Position, 100000f))
                    inFrustrum = true;
                else if (viewState <= UnivScreenState.ShipView)
                {
                    Rectangle rect = new Rectangle((int) system.Position.X - 100000, (int) system.Position.Y - 100000,
                        200000, 200000);
                    Vector3 position =
                        this.Viewport.Unproject(new Vector3(500f, 500f, 0.0f),
                            this.projection, this.view, Matrix.Identity);
                    Vector3 direction =
                        this.Viewport.Unproject(new Vector3(500f, 500f, 1f),
                            this.projection, this.view, Matrix.Identity) - position;
                    direction.Normalize();
                    Ray ray = new Ray(position, direction);
                    float num = -ray.Position.Z / ray.Direction.Z;
                    Vector3 vector3 = new Vector3(ray.Position.X + num * ray.Direction.X,
                        ray.Position.Y + num * ray.Direction.Y, 0.0f);
                    Vector2 pos = new Vector2(vector3.X, vector3.Y);
                    if (HelperFunctions.CheckIntersection(rect, pos))
                        inFrustrum = true;
                }
                if (system.Explored(this.player) && inFrustrum)
                {
                    system.isVisible = camHeight < GetZfromScreenState(UnivScreenState.GalaxyView);
                }
                if (system.isVisible && camHeight < GetZfromScreenState(UnivScreenState.SystemView))
                {
                    foreach (Asteroid asteroid in system.AsteroidsList)
                    {
                        asteroid.So.Visibility = ObjectVisibility.Rendered;
                        asteroid.Update(elapsedTime);
                    }
                    foreach (Moon moon in system.MoonList)
                    {
                        moon.So.Visibility = ObjectVisibility.Rendered;
                        moon.UpdatePosition(elapsedTime);
                    }
                }
                else
                {
                    foreach (Asteroid asteroid in system.AsteroidsList)
                    {
                        asteroid.So.Visibility = ObjectVisibility.None;
                    }
                    foreach (Moon moon in system.MoonList)
                    {
                        moon.So.Visibility = ObjectVisibility.None;
                    }
                }
                foreach (Planet planet in system.PlanetList)
                {
                    planet.Update(elapsedTime);
                    if (planet.HasShipyard && system.isVisible)
                        planet.Station.Update(elapsedTime);
                }
                if (system.isVisible && camHeight < GetZfromScreenState(UnivScreenState.SystemView))
                {
                    foreach (Asteroid asteroid in system.AsteroidsList)
                        asteroid.Update(elapsedTime);
                }
                system.AsteroidsList.ApplyPendingRemovals();
            }
        }

        public void GameSpeedIncrease(bool increase)
        {
            if (!increase) return;

            if (GameSpeed < 1.0)
                GameSpeed = 1f;
            else
                ++GameSpeed;
            if (GameSpeed > 4.0 && GlobalStats.LimitSpeed)
                GameSpeed = 4f;
        }

        private void GameSpeedDecrease(bool decrease)
        {
            if (!decrease) return;

            if (GameSpeed <= 1.0)
                GameSpeed = 0.5f;
            else
                --GameSpeed;
        }
    }
}