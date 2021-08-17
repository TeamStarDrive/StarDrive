using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;

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

            var allShips = Empire.Universe.GetMasterShipList().ToArray().Filter(s=> s.loyalty == EmpireAtWar && s.Active);
            var ourShips = new Array<Ship>(EmpireAtWar.OwnedShips);
            var hangarShips = ourShips.Filter(s => s.IsHangarShip);
            var civilianShips = ourShips.Filter(s => s.DesignRoleType == RoleType.Civilian);
            var AOs = EmpireAtWar.GetEmpireAI().AreasOfOperations.ToArray();
            var aoShips = EmpireAtWar.AIManagedShips;
            var fleets = EmpireAtWar.GetFleetsDict().Values;

            var text = new Array<DebugTextBlock>();

            /// empire data
            var column = new DebugTextBlock();
            column.AddLine($"{EmpireID} {EmpireAtWar.Name}", EmpireAtWar.EmpireColor);
            text.Add(column);
            column = new DebugTextBlock();

            column.AddLine($"MasterShip List: {allShips.Length}");
            column.AddLine($"Empire Ship List: {ourShips.Count}");
            column.AddLine($"Hangar Ships: {hangarShips.Length}");
            column.AddLine($"Civilian Ships: {civilianShips.Length}");
            column.AddLine($"EmpirePool Ready: {aoShips.InitialReadyShips}");
            column.AddLine($"EmpirePool fleets: {aoShips.CurrentUseableFleets}");
            column.AddLine($"Fleets in use: {fleets.Count}");
            column.AddLine($"AO's {AOs.Length}");
            foreach(var ao in AOs)
            {
                Planet coreWorld = ao.CoreWorld;
                int ships = ao.GetNumOffensiveForcePoolShips();
                column.AddLine($"AO: {coreWorld.ParentSystem.Name}");
                column.AddLine($"   #Ships {ships}");
            }
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
            if (allShips.Length > 0)
                text.AddRange(RoleCounts(allShips));
            text.AddRange(GetAllShipsUnderConstruction());
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
            var shipHulls = new Map<RoleName, Array<Ship>>();
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
                column.AddLine($"{task.Type} - {task.MinimumTaskForceStrength}  -  Ending: {task.QueuedForRemoval}");
            }

            column.AddLine($"---------");
            column.AddLine($"War Tasks");
            return column;
        }

        Array<DebugTextBlock> GetAllShipsUnderConstruction()
        {
            ///// ship hulls under construction
            var columns = new Array<DebugTextBlock>();
            var column = new DebugTextBlock();
            column.AddLine($"Under Construction");
            var queue = new Map<RoleName, Array<ShipDesign>>();
            var queues = EmpireAtWar.GetPlanets().Select(p => p.ConstructionQueue);
            var shipData = new Array<ShipDesign>();
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
                    queue[keys] = new Array<ShipDesign>() {ship};
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
                            !(ShipDesign.ShipRoleToRoleType(item.sData.Role) == RoleType.Warship ||
                            ShipDesign.ShipRoleToRoleType(item.sData.Role) == RoleType.WarSupport)) continue;
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
                                !(ShipDesign.ShipRoleToRoleType(item.sData.Role) == RoleType.Warship ||
                                ShipDesign.ShipRoleToRoleType(item.sData.Role) == RoleType.WarSupport)) continue;
                        column.AddLine(item.Planet.Name);
                    }
                }

                columns.Add(column);
            }

            return columns;
        }
    }
}
