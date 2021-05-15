using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.Fleets;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using static Ship_Game.AI.Tasks.MilitaryTask;

namespace Ship_Game.Debug.Page
{
    public class DebugAO : DebugPage
    {
        readonly UniverseScreen Screen;
        int EmpireID = 1;
        Empire EmpireAtWar;

        public DebugAO(UniverseScreen screen, DebugInfoScreen parent) : base(parent, DebugModes.AO)
        {
            Screen = screen;
            if (TextColumns.Count <= 1)
                TextColumns.Add(Label(Rect.X, Rect.Y + 300, ""));
            EmpireAtWar = EmpireManager.GetEmpireById(EmpireID);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            if (!Visible)
                return;

            DrawAOs();
            base.Draw(batch, elapsed);
        }

        public override bool HandleInput(InputState input)
        {
            if (input.ArrowUp) ChangeEmpireId(true);
            else if (input.ArrowDown) ChangeEmpireId(false);
            return base.HandleInput(input);
        }

        void ChangeEmpireId(bool increase)
        {
            do
            {
                EmpireID = EmpireID + (increase ? 1 : -1);
                if (EmpireID > EmpireManager.NumEmpires) EmpireID = 1;
                if (EmpireID < 1) EmpireID = EmpireManager.NumEmpires;
                EmpireAtWar = EmpireManager.GetEmpireById(EmpireID);
            }
            while (EmpireAtWar.data.Defeated);

            TextColumns[0].Text = $"Empire: {EmpireAtWar.Name}";
            TextColumns[0].Color = EmpireAtWar.EmpireColor;
        }

        public override void Update(float fixedDeltaTime)
        {
            if (EmpireAtWar.data.Defeated) return;

            var allShips = Empire.Universe.GetMasterShipList().ToArray().Filter(s=> s.loyalty == EmpireAtWar);
            var ourShips = new Array<Ship>(EmpireAtWar.OwnedShips);
            var aoShips = EmpireAtWar.EmpireShipLists;
            var fleets = EmpireAtWar.GetFleetsDict().Values;

            var text = new Array<DebugTextBlock>();

            /// empire data
            var column = new DebugTextBlock();
            column.AddLine($"{EmpireID} {EmpireAtWar.Name}", EmpireAtWar.EmpireColor);
            text.Add(column);
            column = new DebugTextBlock();

            column.AddLine($"MasterShip List: {allShips.Length}");
            column.AddLine($"Empire Ship List: {ourShips.Count}");
            column.AddLine($"EmpirePool Ship List: {aoShips.ForcePool.GetInternalArrayItems().Length}");
            column.AddLine($"EmpirePool fleets: {aoShips.CurrentUseableFleets}");
            column.AddLine($"Fleets in use: {fleets.Count}");
            text.Add(column);

            column = new DebugTextBlock();
            column.AddLine($"fleets");
            foreach (var fleet in fleets)
            {
                if (fleet.Ships.IsEmpty) continue;
                column.AddLine($"{fleet.Name}  -  Ships: {fleet.Ships.Count}");
            }

            text.Add(column);

            text.Add(ShipStates(allShips));
            text.AddRange(RoleCounts(allShips));
            text.AddRange(GetAllShipsUnderConstruction());
            text.AddRange(TaskStats());
            text.Add(Tasks());
            SetTextColumns(text);

            base.Update(fixedDeltaTime);
        }

        DebugTextBlock ShipStates(Ship[] allShips)
        {
            ///// ship states
            var column = new DebugTextBlock();
            column.AddLine($"Ship States");
            var shipStates = new Map<AIState, Array<Ship>>();
            foreach (var ship in allShips)
            {
                if (ship == null) continue;
                if (shipStates.TryGetValue(ship.AI.State, out var ships))
                {
                    ships.Add(ship);
                }
                else
                {
                    shipStates[ship.AI.State] = new Array<Ship>() { ship };
                }
            }
            foreach (var state in shipStates)
            {
                column.AddLine($"{state.Key.ToString()} = {state.Value.Count}");
            }
            return column;
        }

        private Array<DebugTextBlock> RoleCounts(Ship[] allShips)
        {
            ///// ship hulls
            var columns = new Array<DebugTextBlock>();
            var column = new DebugTextBlock();
            var shipHulls = new Map<ShipData.RoleName, Array<Ship>>();
            column.Header= $"Ship Roles";

            foreach (var ship in allShips)
            {
                if (ship == null) continue;
                var keys = ship.DesignRole;
                if (shipHulls.TryGetValue(keys, out var ships))
                {
                    ships.Add(ship);
                }
                else
                {
                    shipHulls[keys] = new Array<Ship>() { ship };
                }
            }

            foreach (var keys in shipHulls)
            {
                column.AddLine($"{keys.Key.ToString()} = ");
            }
            columns.Add(column);
            column = new DebugTextBlock();
            column.AddLine($"Counts");
            foreach (var keys in shipHulls)
            {
                column.AddLine($"{keys.Value.Count}");
            }
            columns.Add(column);

            return columns;
        }

        private DebugTextBlock Tasks()
        {
            ///// Tasks
            DebugTextBlock column = new DebugTextBlock();
            column.Header= $"--Tasks--";
            column.AddLine($"Empire Tasks");

            var tasks = EmpireAtWar.GetEmpireAI().GetAtomicTasksCopy();

            foreach (var task in tasks)
            {
                column.AddLine($"{task.type} - {task.MinimumTaskForceStrength}  -  Ending: {task.QueuedForRemoval}");
            }

            column.AddLine($"---------");
            column.AddLine($"War Tasks");

            /// WarTasks
            if (EmpireAtWar.GetEmpireAI().PauseWarTimer < 0)
            {
                var warTasks = EmpireAtWar.GetEmpireAI().WarTasks;

                foreach (var task in warTasks.NewTasks)
                {
                    column.AddLine($"{task.type} - {task.MinimumTaskForceStrength}  -  Ending: {task.QueuedForRemoval}");
                }
            }

            return column;
        }

        Array<DebugTextBlock> TaskStats()
        {
            var itemName = Enum.GetValues(typeof(RequisitionStatus));
            var taskMap = new Map<RequisitionStatus, int>();
            foreach (RequisitionStatus item in itemName) taskMap.Add(item, 0);

            var tasks = EmpireAtWar.GetEmpireAI().GetAtomicTasksCopy();

            foreach (var task in tasks) 
                taskMap[task.GetRequisitionStatus()]++;
            tasks = EmpireAtWar.GetEmpireAI().WarTasks.NewTasks.ToArray();
            foreach (var task in tasks)
                taskMap[task.GetRequisitionStatus()]++;
            var names = new DebugTextBlock();
            var count = new DebugTextBlock();
            names.AddLine("Task Results");
            count.AddLine("Counts");
            foreach (var kv in taskMap)
            {
                names.AddLine($"{kv.Key}");
                count.AddLine($"{kv.Value}");
            }
            return new Array<DebugTextBlock> { names, count };
        }

        Array<DebugTextBlock> GetAllShipsUnderConstruction()
        {
            ///// ship hulls under construction
            var columns = new Array<DebugTextBlock>();
            var column = new DebugTextBlock();
            column.AddLine($"Under Construction");
            var queue = new Map<ShipData.RoleName, Array<ShipData>>();
            var queues = EmpireAtWar.GetPlanets().Select(p => p.ConstructionQueue);
            var shipData = new Array<ShipData>();
            if (queues.Length == 0) return new Array<DebugTextBlock>{column};
            for (int i = 0; i < queues.Length; i++)
            {
                var q = queues[i];
                foreach (var qi in q)
                {
                    if (!qi.isShip) continue;
                    shipData.Add(qi.sData);
                }
            }

            for (int i = 0; i < shipData.Count; i++)
            {
                var ship = shipData[i];
                var keys = ship.Role;
                if (queue.TryGetValue(keys, out var ships))
                {
                    ships.Add(ship);
                }
                else
                {
                    queue[keys] = new Array<ShipData>() {ship};
                }
            }

            foreach (var keys in queue)
            {
                column.AddLine($"{keys.Key.ToString()} = ");
            }

            column.AddLine("-----");
            foreach (var items in queues)
            {
                foreach (var item in items)
                {
                    if (item?.isShip != true ||// item.DisplayName.IsEmpty() ||
                            !(ShipData.ShipRoleToRoleType(item.sData.Role) == ShipData.RoleType.Warship ||
                            ShipData.ShipRoleToRoleType(item.sData.Role) == ShipData.RoleType.WarSupport)) continue;
                    column.AddLine(item.sData.Name);
                }
            }


            columns.Add(column);


            column = new DebugTextBlock();
            column.AddLine("Counts");
            if (queue.Count > 0)
            {
                foreach (var keys in queue)
                {
                    if (keys.Value == null) continue;
                    column.AddLine($"{keys.Value.Count}");
                }

                column.AddLine("-----");
                foreach (var items in queues)
                {
                    foreach (var item in items)
                    {
                        if (!item.isShip ||// item.DisplayName.IsEmpty() ||
                                !(ShipData.ShipRoleToRoleType(item.sData.Role) == ShipData.RoleType.Warship ||
                                ShipData.ShipRoleToRoleType(item.sData.Role) == ShipData.RoleType.WarSupport)) continue;
                        column.AddLine(item.Planet.Name);
                    }
                }

                columns.Add(column);
            }

            return columns;
        }

        void DrawAOs()
        {
            var aos = EmpireAtWar.GetEmpireAI().AreasOfOperations;
            for (int i = 0; i < aos.Count; i++)
            {
                var ao = aos[i];
                Screen.DrawCircleProjected(ao.Center, ao.Radius, EmpireAtWar.EmpireColor, 2);
            }


            foreach ((Empire them, Relationship rel) in EmpireAtWar.AllRelations)
            {
                if (them.data.Defeated) continue;
                var war = rel.ActiveWar;
                if (war == null || war.Them.isFaction) continue;
                float minPri = EmpireAtWar.GetEmpireAI().MinWarPriority;
                float warPri = war.GetPriority();
                if (warPri > minPri)
                    continue;

                for (int i = 0; i < EmpireAtWar.AllActiveWarTheaters.Length; i++)
                {
                    var theater = EmpireAtWar.AllActiveWarTheaters[i];
                    float thickness = 10;
                    var ao = theater.TheaterAO;
                    var rallyAo = theater.RallyAO;
                    Screen.DrawCircleProjected(ao.Center, ao.Radius, war.Them.EmpireColor, thickness);
                    if (rallyAo == null) continue;
                    Screen.DrawLineWideProjected(ao.Center, rallyAo.Center, Colors.Attack(), 2);
                    Screen.DrawCircleProjected(rallyAo.Center, rallyAo.Radius, war.Them.EmpireColor, 2);
                    Screen.DrawCircleProjected(rallyAo.Center, rallyAo.Radius, war.Them.EmpireColor, 2);
                }
            }
        }
    }
}
