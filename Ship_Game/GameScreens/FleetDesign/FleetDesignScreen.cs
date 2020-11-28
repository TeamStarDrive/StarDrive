using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using System.Collections.Generic;
using Ship_Game.Audio;
using Ship_Game.GameScreens;
using Ship_Game.Fleets;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class FleetDesignScreen : GameScreen
    {
        public Camera2D Camera;
        //private Background bg = new Background();
        StarField StarField;
        public EmpireUIOverlay EmpireUI;
        Menu2 TitleBar;
        Menu2 ShipDesigns;
        Vector2 TitlePos;
        Vector2 ShipDesignsTitlePos;
        Menu1 LeftMenu;
        Menu1 RightMenu;
        public Fleet SelectedFleet;
        ScrollList2<FleetDesignShipListItem> ShipSL;
        BlueButton RequisitionForces;
        BlueButton SaveDesign;
        BlueButton LoadDesign;
        Rectangle SelectedStuffRect;
        Rectangle OperationsRect;
        Rectangle PrioritiesRect;
        WeightSlider SliderAssist;
        WeightSlider SliderVulture;
        WeightSlider SliderDefend;
        WeightSlider SliderDps;
        WeightSlider SliderArmor;
        WeightSlider SliderShield;
        readonly Array<ToggleButton> OrdersButtons = new Array<ToggleButton>();
        FloatSlider OperationalRadius;
        SizeSlider SliderSize;
        public Submenu SubShips;
        Array<Ship> AvailableShips = new Array<Ship>();
        Vector3 CamPos = new Vector3(0f, 0f, 14000f);
        readonly Map<int, Rectangle> FleetsRects = new Map<int, Rectangle>();
        readonly Array<ClickableSquad> ClickableSquads = new Array<ClickableSquad>();
        Vector2 CamVelocity = Vector2.Zero;
        float DesiredCamHeight = 14000f;
        Ship ActiveShipDesign;
        public int FleetToEdit = -1;
        readonly UITextEntry FleetNameEntry = new UITextEntry();
        Selector StuffSelector;
        Selector OperationsSelector;
        Selector Priorityselector;
        readonly Array<ClickableNode> ClickableNodes = new Array<ClickableNode>();
        Fleet.Squad SelectedSquad;
        Fleet.Squad HoveredSquad;
        Rectangle SelectionBox;
        readonly Array<FleetDataNode> SelectedNodeList = new Array<FleetDataNode>();
        readonly Array<FleetDataNode> HoveredNodeList = new Array<FleetDataNode>();
        readonly ShipInfoOverlayComponent ShipInfoOverlay;

        public FleetDesignScreen(GameScreen parent, EmpireUIOverlay empireUI, string audioCue ="") : base(parent)
        {
            GameAudio.PlaySfxAsync(audioCue);
            SelectedFleet = new Fleet();
            EmpireUI = empireUI;
            TransitionOnTime = 0.75f;
            EmpireUI.empire.UpdateShipsWeCanBuild();
            ShipInfoOverlay = Add(new ShipInfoOverlayComponent(this));
        }

        public void ChangeFleet(int which)
        {
            SelectedNodeList.Clear();
            // dear scroll list branch. How are you? the object visiblility is being changed here.
            // so make sure that the so's are removed and added at each fleet button press.
            if (FleetToEdit != -1)
            {
                foreach (var kv in EmpireManager.Player.GetFleetsDict())
                {
                    foreach (Ship ship in kv.Value.Ships)
                    {
                        ship.RemoveSceneObject();
                    }
                }
            }

            FleetToEdit = which;
            Fleet fleet = EmpireManager.Player.GetFleetsDict()[FleetToEdit];
            var toRemove = new Array<FleetDataNode>();
            foreach (FleetDataNode node in fleet.DataNodes)
            {
                if ((!ResourceManager.ShipsDict.ContainsKey(node.ShipName) && node.Ship == null) ||
                    (node.Ship == null && !EmpireManager.Player.WeCanBuildThis(node.ShipName)))
                    toRemove.Add(node);
            }

            var squadsToRemove = new Array<Fleet.Squad>();
            foreach (FleetDataNode node in toRemove)
            {
                fleet.DataNodes.Remove(node);
                foreach (Array<Fleet.Squad> flanks in fleet.AllFlanks)
                {
                    foreach (Fleet.Squad squad in flanks)
                    {
                        squad.DataNodes.Remove(node);
                        if (squad.DataNodes.Count == 0)
                            squadsToRemove.Add(squad);
                    }
                }
            }

            foreach (Array<Fleet.Squad> flanks in fleet.AllFlanks)
            {
                foreach (Fleet.Squad squad in squadsToRemove)
                    if (flanks.Contains(squad))
                        flanks.Remove(squad);
            }

            SelectedFleet = EmpireManager.Player.GetFleet(which);
            foreach (Ship ship in SelectedFleet.Ships)
            {
                ship.ShowSceneObjectAt(new Vector3(ship.RelativeFleetOffset, 0f));
            }
        }

        public void OnSubShipsTabChanged(int tabIndex)
        {
            ResetLists();
        }

        protected override void Destroy()
        {
            StarField?.Dispose(ref StarField);
            SelectedFleet = null;
            base.Destroy();
        }

        public override void ExitScreen()
        {
            if (!StarDriveGame.Instance.IsExiting) // RedFox: if game is exiting, we don't need to restore universe screen
            {
                Empire.Universe.RecomputeFleetButtons(true);
            }
            base.ExitScreen();
        }

        public override void LoadContent()
        {
            Add(new CloseButton(ScreenWidth - 38, 97));
            AssignLightRig(LightRigIdentity.FleetDesign, "example/ShipyardLightrig");
            StarField = new StarField(this);

            var titleRect = new Rectangle(2, 44, 250, 80);
            TitleBar = new Menu2(titleRect);
            TitlePos = new Vector2(titleRect.X + titleRect.Width / 2f - Fonts.Laserian14.MeasureString("Fleet Hotkeys").X / 2f
                , titleRect.Y + titleRect.Height / 2f - Fonts.Laserian14.LineSpacing / 2f);
            var leftRect = new Rectangle(2, titleRect.Y + titleRect.Height + 5, titleRect.Width, 500);
            LeftMenu = new Menu1(leftRect, true);

            int i = 0;
            foreach (KeyValuePair<int, Fleet> fleet in EmpireManager.Player.GetFleetsDict())
            {
                FleetsRects.Add(fleet.Key, new Rectangle(leftRect.X + 2, leftRect.Y + i * 53, 52, 48));
                i++;
            }

            var shipRect = new Rectangle(ScreenWidth - 282, 140, 280, 80);
            ShipDesigns = new Menu2(shipRect);
            ShipDesignsTitlePos = new Vector2(shipRect.X + shipRect.Width / 2 - Fonts.Laserian14.MeasureString("Ship Designs").X / 2f, shipRect.Y + shipRect.Height / 2 - Fonts.Laserian14.LineSpacing / 2);
            var shipDesignsRect = new Rectangle(ScreenWidth - shipRect.Width - 2, shipRect.Y + shipRect.Height + 5, shipRect.Width, 500);
            RightMenu = new Menu1(shipDesignsRect);

            SubShips = new Submenu(shipDesignsRect);
            SubShips.Color = new Color(0, 0, 0, 130);
            SubShips.AddTab("Designs");
            SubShips.AddTab("Owned");
            SubShips.SelectedIndex = 0;
            SubShips.OnTabChange = OnSubShipsTabChanged;
            ShipSL = Add(new ScrollList2<FleetDesignShipListItem>(SubShips, 40));
            ShipSL.OnClick = OnDesignShipItemClicked;
            ShipSL.EnableItemHighlight = true;
            ShipSL.OnHovered = (item) =>
            {
                ShipInfoOverlay.ShowToLeftOf(item?.Pos ?? Vector2.Zero, item?.Ship);
            };

            ResetLists();
            SelectedStuffRect = new Rectangle(ScreenWidth / 2 - 220, -13 + ScreenHeight - 200, 440, 210);

            var ordersBarPos = new Vector2(SelectedStuffRect.X + 20, SelectedStuffRect.Y + 65);
            void AddOrdersBtn(CombatState state, string icon, int toolTip)
            {
                var button = new ToggleButton(ordersBarPos, ToggleButtonStyle.Formation, icon)
                {
                    CombatState = state,
                    Tooltip = toolTip,
                    OnClick = (b) => OnOrderButtonClicked(b, state),
                    Visible = false,
                };
                Add(button);
                OrdersButtons.Add(button);
                ordersBarPos.X += 29f;
            }

            AddOrdersBtn(CombatState.AttackRuns,   "SelectionBox/icon_formation_headon", toolTip: 1);
            AddOrdersBtn(CombatState.Artillery,    "SelectionBox/icon_formation_aft",    toolTip: 2);
            AddOrdersBtn(CombatState.HoldPosition, "SelectionBox/icon_formation_x",      toolTip: 65);
            AddOrdersBtn(CombatState.OrbitLeft,    "SelectionBox/icon_formation_left",   toolTip: 3);
            AddOrdersBtn(CombatState.OrbitRight,   "SelectionBox/icon_formation_right",  toolTip: 4);
            AddOrdersBtn(CombatState.Evade,        "SelectionBox/icon_formation_stop",   toolTip: 6);

            ordersBarPos = new Vector2(SelectedStuffRect.X + 20 + 3*29f, ordersBarPos.Y + 29f);
            AddOrdersBtn(CombatState.BroadsideLeft,  "SelectionBox/icon_formation_bleft",  toolTip: 159);
            AddOrdersBtn(CombatState.BroadsideRight, "SelectionBox/icon_formation_bright", toolTip: 160);


            RequisitionForces = new BlueButton(new Vector2(SelectedStuffRect.X + 240, SelectedStuffRect.Y + Fonts.Arial20Bold.LineSpacing + 20), "Requisition...");
            SaveDesign = new BlueButton(new Vector2(SelectedStuffRect.X + 240, SelectedStuffRect.Y + Fonts.Arial20Bold.LineSpacing + 20 + 50), "Save Design...");
            LoadDesign = new BlueButton(new Vector2(SelectedStuffRect.X + 240, SelectedStuffRect.Y + Fonts.Arial20Bold.LineSpacing + 20 + 100), "Load Design...");
            RequisitionForces.ToggleOn = true;
            SaveDesign.ToggleOn = true;
            LoadDesign.ToggleOn = true;
            OperationsRect = new Rectangle(SelectedStuffRect.X + SelectedStuffRect.Width + 2, SelectedStuffRect.Y + 30, 360, SelectedStuffRect.Height - 30);
            var assistRect = new Rectangle(OperationsRect.X + 15, OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 20, 150, 40);
            SliderAssist = new WeightSlider(assistRect, "Assist Nearby Weight")
            {
                Tip_ID = 7
            };
            var defenderRect = new Rectangle(OperationsRect.X + 15, OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 70, 150, 40);
            SliderDefend = new WeightSlider(defenderRect, "Defend Nearby Weight")
            {
                Tip_ID = 8
            };
            var vultureRect = new Rectangle(OperationsRect.X + 15, OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 120, 150, 40);
            SliderVulture = new WeightSlider(vultureRect, "Target Damaged Weight")
            {
                Tip_ID = 9
            };
            var armoredRect = new Rectangle(OperationsRect.X + 15 + 180, OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 20, 150, 40);
            SliderArmor = new WeightSlider(armoredRect, "Target Armored Weight")
            {
                Tip_ID = 10
            };
            var shieldedRect = new Rectangle(OperationsRect.X + 15 + 180, OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 70, 150, 40);
            SliderShield = new WeightSlider(shieldedRect, "Target Shielded Weight")
            {
                Tip_ID = 11
            };
            var dpsRect = new Rectangle(OperationsRect.X + 15 + 180, OperationsRect.Y + Fonts.Arial12Bold.LineSpacing + 120, 150, 40);
            SliderDps = new WeightSlider(dpsRect, "Target DPS Weight")
            {
                Tip_ID = 12
            };
            PrioritiesRect = new Rectangle(SelectedStuffRect.X - OperationsRect.Width - 2, OperationsRect.Y, OperationsRect.Width, OperationsRect.Height);
            var oprect = new Rectangle(PrioritiesRect.X + 15, PrioritiesRect.Y + Fonts.Arial12Bold.LineSpacing + 20, 300, 40);
            OperationalRadius = new FloatSlider(oprect, "Operational Radius", max: 500000, value: 10000)
            {
                RelativeValue = 0.2f,
                Tip = 13
            };
            var sizerect = new Rectangle(PrioritiesRect.X + 15, PrioritiesRect.Y + Fonts.Arial12Bold.LineSpacing + 70, 300, 40);
            SliderSize = new SizeSlider(sizerect, "Target UniverseRadius Preference");
            SliderSize.SetAmount(0.5f);
            SliderSize.Tip_ID = 14;
            StarField = new StarField(this);
            Projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, Viewport.AspectRatio, 100f, 15000f);
            foreach (Ship ship in SelectedFleet.Ships)
            {
                ship.ShowSceneObjectAt(new Vector3(ship.RelativeFleetOffset, 0f));
            }
            base.LoadContent();
        }


        public void LoadData(FleetDesign data)
        {
            var fleet = EmpireManager.Player.GetFleetsDict()[FleetToEdit];

            for (int i = fleet.Ships.Count - 1; i >= 0; i--)
            {
                Ship ship = fleet.Ships[i];
                ship.ShowSceneObjectAt(new Vector3(ship.RelativeFleetOffset, -1000000f));
                ship.ClearFleet();
            }
            SelectedFleet.DataNodes.Clear();
            SelectedFleet.Ships.Clear();
            ClickableNodes.Clear();
            foreach (Array<Fleet.Squad> flank in SelectedFleet.AllFlanks)
            {
                flank.Clear();
            }
            SelectedFleet.Name = data.Name;
            foreach (FleetDataNode node in data.Data)
            {
                SelectedFleet.DataNodes.Add(node);
            }
            SelectedFleet.FleetIconIndex = data.FleetIconIndex;
        }

        public void ResetLists()
        {
            ShipSL.Reset();
            if (SubShips.SelectedIndex == 0)
            {
                var shipList = new Array<Ship>();
                foreach (string shipName in EmpireManager.Player.ShipsWeCanBuild)
                {
                    Ship ship = ResourceManager.GetShipTemplate(shipName);
                    shipList.Add(ship);
                }

                SortShipSL(shipList);
            }
            else if (SubShips.SelectedIndex == 1)
            {
                AvailableShips.Clear();
                AvailableShips.AddRange(EmpireManager.Player.GetShips()
                                        .Filter(s => s.fleet == null && s.Active));

                SortShipSL(AvailableShips);
            }
        }

        void SortShipSL(Array<Ship> shipList)
        {
            var roles = new Array<string>();
            foreach (Ship ship in shipList)
            {
                if (IsCandidateShip(ship))
                    roles.AddUnique(ship.DesignRoleName);
            }

            roles.Sort();
            foreach (string role in roles)
            {
                FleetDesignShipListItem header = ShipSL.AddItem(
                    new FleetDesignShipListItem(this, role));

                foreach (string shipName in EmpireManager.Player.ShipsWeCanBuild)
                {
                    Ship ship = ResourceManager.ShipsDict[shipName];
                    if (IsCandidateShip(ship) && ship.DesignRoleName == header.HeaderText)
                        header.AddSubItem(new FleetDesignShipListItem(this, ship));
                }
            }
        }

        bool IsCandidateShip(Ship ship)
        {
            if (ship.shipData.Role == ShipData.RoleName.troop
                || ship.DesignRole == ShipData.RoleName.ssp
                || ship.DesignRole == ShipData.RoleName.construction)
            {
                return false;
            }

            return true;
        }
        
        void UpdateSelectedFleet()
        {
            if (SelectedFleet == null)
                return;

            foreach (Array<Fleet.Squad> flank in SelectedFleet.AllFlanks)
            {
                foreach (Fleet.Squad squad in flank)
                {
                    Viewport viewport = Viewport;
                    Vector3 pScreenSpace = viewport.Project(new Vector3(squad.Offset, 0f), Projection, View, Matrix.Identity);
                    Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    ClickableSquad cs = new ClickableSquad
                    {
                        ScreenPos = pPos,
                        Squad = squad
                    };
                    ClickableSquads.Add(cs);
                }
            }
            SelectedFleet.AssembleFleet2(SelectedFleet.FinalPosition, SelectedFleet.FinalDirection);
        }

        public override void Update(UpdateTimes elapsed, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            CamPos.X += CamVelocity.X;
            CamPos.Y += CamVelocity.Y;
            CamPos.Z = MathHelper.SmoothStep(CamPos.Z, DesiredCamHeight, 0.2f);
            View = Matrix.CreateRotationY(180f.ToRadians())
                * Matrix.CreateLookAt(new Vector3(-CamPos.X, CamPos.Y, CamPos.Z), new Vector3(-CamPos.X, CamPos.Y, 0f), Vector3.Down);
            
            ClickableSquads.Clear();
            UpdateSelectedFleet();

            base.Update(elapsed, otherScreenHasFocus, coveredByOtherScreen);
        }

        public struct ClickableNode
        {
            public Vector2 ScreenPos;

            public float Radius;

            public FleetDataNode NodeToClick;
        }

        struct ClickableSquad
        {
            public Fleet.Squad Squad;

            public Vector2 ScreenPos;
        }
    }
}