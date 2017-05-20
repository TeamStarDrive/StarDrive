using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SgMotion;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game {
    public sealed partial class ShipDesignScreen
    {
        public void ChangeHull(ShipData hull) //Mer
        {
#if SHIPYARD
            TotalI = TotalO = TotalE = TotalIO = TotalIE = TotalOE = TotalIOE = 0;
        #endif
            Reset = true;
            DesignStack.Clear();
            LastDesignActionPos = Vector2.Zero;
            LastActiveUID = "";

            lock (GlobalStats.ObjectManagerLocker)
            {
                if (shipSO != null)
                {
                    ScreenManager.inter.ObjectManager.Remove(shipSO);
                }
            }
            ActiveHull = new ShipData()
            {
                Animated = hull.Animated,
                CombatState = hull.CombatState,
                Hull = hull.Hull,
                IconPath = hull.IconPath,
                ModelPath = hull.ModelPath,
                Name = hull.Name,
                Role = hull.Role,
                ShipStyle = hull.ShipStyle,
                ThrusterList = hull.ThrusterList,
                ShipCategory = hull.ShipCategory,
                CarrierShip = hull.CarrierShip
            };
            Techs.Clear();
            AddToTechList(ActiveHull.HullData.techsNeeded);
            CarrierOnly = hull.CarrierShip;
            LoadCategory = hull.ShipCategory;
            Fml = true;
            Fmlevenmore = true;

            ActiveHull.ModuleSlots = new ModuleSlotData[hull.ModuleSlots.Length];
            for (int i = 0; i < hull.ModuleSlots.Length; ++i)
            {
                ModuleSlotData hullSlot = hull.ModuleSlots[i];
                ModuleSlotData data = new ModuleSlotData()
                {
                    Position = hullSlot.Position,
                    Restrictions = hullSlot.Restrictions,
                    Facing = hullSlot.Facing,
                    InstalledModuleUID = hullSlot.InstalledModuleUID
                };
                ActiveHull.ModuleSlots[i] = data;
#if SHIPYARD
                if (data.Restrictions == Restrictions.I) TotalI++;
                if (data.Restrictions == Restrictions.O) TotalO++;
                if (data.Restrictions == Restrictions.E) TotalE++;
                if (data.Restrictions == Restrictions.IO) TotalIO++;
                if (data.Restrictions == Restrictions.IE) TotalIE++;
                if (data.Restrictions == Restrictions.OE) TotalOE++;
                if (data.Restrictions == Restrictions.IOE) TotalIOE++;
            #endif
            }
            this.CombatState = hull.CombatState;
            if (!hull.Animated)
            {
                this.ActiveModel = Ship_Game.ResourceManager.GetModel(this.ActiveHull.ModelPath);
                ModelMesh mesh = this.ActiveModel.Meshes[0];
                this.shipSO = new SceneObject(mesh)
                {
                    ObjectType = ObjectType.Dynamic,
                    World = this.WorldMatrix
                };
                lock (GlobalStats.ObjectManagerLocker)
                {
                    base.ScreenManager.inter.ObjectManager.Submit(this.shipSO);
                }
            }
            else
            {
                SkinnedModel sm = Ship_Game.ResourceManager.GetSkinnedModel(this.ActiveHull.ModelPath);
                this.shipSO = new SceneObject(sm.Model)
                {
                    ObjectType = ObjectType.Dynamic,
                    World = this.WorldMatrix
                };
                lock (GlobalStats.ObjectManagerLocker)
                {
                    base.ScreenManager.inter.ObjectManager.Submit(this.shipSO);
                }
            }
            foreach (ToggleButton button in CombatStatusButtons)
            {
                switch (button.Action)
                {
                    default: continue;
                    case "attack":
                        button.Active = CombatState == CombatState.AttackRuns;
                        break;
                    case "arty":
                        button.Active = CombatState == CombatState.Artillery;
                        break;
                    case "hold":
                        button.Active = CombatState == CombatState.HoldPosition;
                        break;
                    case "orbit_left":
                        button.Active = CombatState == CombatState.OrbitLeft;
                        break;
                    case "broadside_left":
                        button.Active = CombatState == CombatState.BroadsideLeft;
                        break;
                    case "broadside_right":
                        button.Active = CombatState == CombatState.BroadsideRight;
                        break;
                    case "short":
                        button.Active = CombatState == CombatState.ShortRange;
                        break;
                    case "evade":
                        button.Active = CombatState == CombatState.Evade;
                        break;
                    case "orbit_right":
                        button.Active = CombatState == CombatState.OrbitRight;
                        break;
                }
            }
            SetupSlots();
        }

        private bool CheckDesign()
        {
            bool emptySlots = true;
            bool hasBridge = false;
            foreach (SlotStruct slot in Slots)
            {
                if (slot.ModuleUID == null)
                    emptySlots = false;
                if (slot.ModuleUID == null || !slot.Module.IsCommandModule)
                    continue;
                hasBridge = true;
            }
            if (!hasBridge && ActiveHull.Role != ShipData.RoleName.platform &&
                ActiveHull.Role != ShipData.RoleName.station || !emptySlots)
                return false;
            return true;
        }

        public void CreateShipModuleSelectionWindow()
        {
            this.UpArrow = new Rectangle(this.ModuleSelectionArea.X + this.ModuleSelectionArea.Width - 22,
                this.ModuleSelectionArea.Y, 22, 30);
            this.DownArrow = new Rectangle(this.ModuleSelectionArea.X + this.ModuleSelectionArea.Width - 22,
                this.ModuleSelectionArea.Y + this.ModuleSelectionArea.Height - 32, 20, 30);
            Array<string> Categories = new Array<string>();
            Dictionary<string, Array<ShipModule>> moduleDict = new Map<string, Array<ShipModule>>();
            foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
            {
                if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                {
                    continue;
                }
                string cat = module.Value.ModuleType.ToString();
                if (!Categories.Contains(cat))
                {
                    Categories.Add(cat);
                }
                if (moduleDict.ContainsKey(cat))
                {
                    moduleDict[cat].Add(module.Value);
                }
                else
                {
                    moduleDict.Add(cat, new Array<ShipModule>());
                    moduleDict[cat].Add(module.Value);
                }
                ModuleButton mb = new ModuleButton()
                {
                    moduleRect = new Rectangle(0, 0, 128, 128),
                    ModuleUID = module.Key
                };
                this.ModuleButtons.Add(mb);
            }
            Categories.Sort();
            int i = 0;
            foreach (string cat in Categories)
            {
                ShipDesignScreen.ModuleCatButton ModuleCatButton = new ShipDesignScreen.ModuleCatButton()
                {
                    mRect = new Rectangle(this.ModuleSelectionArea.X + 10, this.ModuleSelectionArea.Y + 10 + i * 25, 45,
                        25),
                    Category = cat
                };
                this.ModuleCatButtons.Add(ModuleCatButton);
                i++;
            }
            int x = 0;
            int y = 0;
            foreach (ModuleButton mb in this.ModuleButtons)
            {
                mb.moduleRect.X = this.ModuleSelectionArea.X + 20 + x * 128;
                mb.moduleRect.Y = this.ModuleSelectionArea.Y + 10 + y * 128;
                x++;
                if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280)
                {
                    if (x <= 1) continue;
                    y++;
                    x = 0;
                }
                else
                {
                    if (x <= 2) continue;
                    y++;
                    x = 0;
                }
            }
        }

        private void CreateSOFromHull()
        {
            lock (GlobalStats.ObjectManagerLocker)
            {
                if (this.shipSO != null)
                {
                    base.ScreenManager.inter.ObjectManager.Remove(this.shipSO);
                }
                ModelMesh mesh = this.ActiveModel.Meshes[0];
                this.shipSO = new SceneObject(mesh)
                {
                    ObjectType = ObjectType.Dynamic,
                    World = this.WorldMatrix
                };
                base.ScreenManager.inter.ObjectManager.Submit(this.shipSO);
                this.SetupSlots();
            }
        }

        private void DoExit(object sender, EventArgs e)
        {
            ReallyExit();
        }

        private void DoExitToFleetsList(object sender, EventArgs e) //Unused
        {
            ScreenManager.AddScreen(new FleetDesignScreen(this, EmpireUI));
            ReallyExit();
        }

        private void DoExitToShipList(object sender, EventArgs e) //Unused
        {
            ReallyExit();
        }

        private void DoExitToShipsList(object sender, EventArgs e) //Unused
        {
            ScreenManager.AddScreen(new ShipListScreen(this, EmpireUI));
            ReallyExit();
        }

        public override void ExitScreen()
        {
            if (!this.ShipSaved && !this.CheckDesign())
            {
                MessageBoxScreen message = new MessageBoxScreen(this, Localizer.Token(2121), "Save", "Exit");
                message.Cancelled += new EventHandler<EventArgs>(this.DoExit);
                message.Accepted += new EventHandler<EventArgs>(this.SaveWIP);
                base.ScreenManager.AddScreen(message);
                return;
            }
            if (this.ShipSaved || !this.CheckDesign())
            {
                this.ReallyExit();
                return;
            }
            MessageBoxScreen message0 = new MessageBoxScreen(this, Localizer.Token(2137), "Save", "Exit");
            message0.Cancelled += new EventHandler<EventArgs>(this.DoExit);
            message0.Accepted += new EventHandler<EventArgs>(this.SaveChanges);
            base.ScreenManager.AddScreen(message0);
        }

        public void ExitToMenu(string launches)
        {
            this.ScreenToLaunch = launches;
            MessageBoxScreen message;
            if (this.ShipSaved && this.CheckDesign())
            {
                this.LaunchScreen(null, null);
                this.ReallyExit();
                return;
            }
            else if (!this.ShipSaved && this.CheckDesign())
            {
                message = new MessageBoxScreen(this, Localizer.Token(2137), "Save", "Exit");
                message.Cancelled += new EventHandler<EventArgs>(this.LaunchScreen);
                message.Accepted += new EventHandler<EventArgs>(this.SaveChanges);
                base.ScreenManager.AddScreen(message);
                return;
            }
            message = new MessageBoxScreen(this, Localizer.Token(2121), "Save", "Exit");
            message.Cancelled += new EventHandler<EventArgs>(this.LaunchScreen);
            message.Accepted += new EventHandler<EventArgs>(this.SaveWIPThenLaunchScreen);
            base.ScreenManager.AddScreen(message);
        }

        private static FileInfo[] GetFilesFromDirectory(string DirPath) //Unused
        {
            return (new DirectoryInfo(DirPath)).GetFiles("*.*", SearchOption.AllDirectories);
        }

        private void GoHullLeft() //Unused
        {
            ShipDesignScreen hullIndex = this;
            hullIndex.HullIndex = hullIndex.HullIndex - 1;
            if (this.HullIndex < 0)
            {
                this.HullIndex = this.AvailableHulls.Count - 1;
            }
            this.ChangeHull(this.AvailableHulls[this.HullIndex]);
        }

        private void GoHullRight() //Unused
        {
            ShipDesignScreen hullIndex = this;
            hullIndex.HullIndex = hullIndex.HullIndex + 1;
            if (this.HullIndex > this.AvailableHulls.Count - 1)
            {
                this.HullIndex = 0;
            }
            this.ChangeHull(this.AvailableHulls[this.HullIndex]);
        }

        public override void HandleInput(InputState input)
        {
            this.CategoryList.HandleInput(input);
            if (DropdownRect.HitTest(input.CursorPosition)) //fbedard: add tooltip for CategoryList
            {
                switch (this.CategoryList.Options[this.CategoryList.ActiveIndex].@value)
                {
                    case 1:
                    {
                        ToolTip.CreateTooltip("Repair when damaged at 75%", this.ScreenManager);
                        break;
                    }
                    case 2:
                    {
                        ToolTip.CreateTooltip(
                            "Can be used as Freighter.\nEvade when enemy.\nRepair when damaged at 15%",
                            this.ScreenManager);
                        break;
                    }
                    case 3:
                    {
                        ToolTip.CreateTooltip("Repair when damaged at 35%", this.ScreenManager);
                        break;
                    }
                    case 4:
                    case 5:
                    case 6:
                    {
                        ToolTip.CreateTooltip("Repair when damaged at 55%", this.ScreenManager);
                        break;
                    }
                    case 7:
                    {
                        ToolTip.CreateTooltip("Never Repair!", this.ScreenManager);
                        break;
                    }
                    default:
                    {
                        ToolTip.CreateTooltip("Repair when damaged at 75%", this.ScreenManager);
                        break;
                    }
                }
            }
            this.CarrierOnlyBox.HandleInput(input);

            if (this.ActiveModule != null && (this.ActiveModule.InstalledWeapon != null
                                              && this.ActiveModule.ModuleType != ShipModuleType.Turret ||
                                              this.ActiveModule.XSIZE != this.ActiveModule.YSIZE))
            {
                if (input.Left)
                    this.ChangeModuleState(ShipDesignScreen.ActiveModuleState.Left);
                if (input.Right)
                    this.ChangeModuleState(ShipDesignScreen.ActiveModuleState.Right);
                if (input.Down)
                    this.ChangeModuleState(ShipDesignScreen.ActiveModuleState.Rear);
                if (input.Up)
                    this.ChangeModuleState(ShipDesignScreen.ActiveModuleState.Normal);
            }
            if (input.ShipDesignExit && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                this.ExitScreen();
            }
            if (this.Close.HandleInput(input))
                this.ExitScreen();
            else if (input.CurrentKeyboardState.IsKeyDown(Keys.Z) && input.LastKeyboardState.IsKeyUp(Keys.Z)
                     && input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
            {
                if (this.DesignStack.Count <= 0)
                    return;
                LastActiveUID = "";
                ShipModule shipModule = this.ActiveModule;
                DesignAction designAction = this.DesignStack.Pop();
                SlotStruct slot1 = new SlotStruct();
                foreach (SlotStruct slot2 in this.Slots)
                {
                    if (slot2.PQ == designAction.clickedSS.PQ)
                    {
                        this.ClearSlotNoStack(slot2);
                        slot1 = slot2;
                        slot1.Facing = designAction.clickedSS.Facing;
                    }
                    foreach (SlotStruct slotStruct in designAction.AlteredSlots)
                    {
                        if (slot2.PQ == slotStruct.PQ)
                        {
                            this.ClearSlotNoStack(slot2);
                            break;
                        }
                    }
                }
                if (designAction.clickedSS.ModuleUID != null)
                {
                    this.ActiveModule = ShipModule.CreateNoParent(designAction.clickedSS.ModuleUID);
                    this.ResetModuleState();
                    this.InstallModuleNoStack(slot1);
                }
                foreach (SlotStruct slotStruct in designAction.AlteredSlots)
                {
                    foreach (SlotStruct slot2 in this.Slots)
                    {
                        if (slot2.PQ == slotStruct.PQ && slotStruct.ModuleUID != null)
                        {
                            this.ActiveModule = ShipModule.CreateNoParent(slotStruct.ModuleUID);
                            this.ResetModuleState();
                            this.InstallModuleNoStack(slot2);
                            slot2.Facing = slotStruct.Facing;
                            slot2.ModuleUID = slotStruct.ModuleUID;
                        }
                    }
                }
                this.ActiveModule = shipModule;
                this.ResetModuleState();
            }
            else
            {
                if (!this.ModuleSelectionMenu.Menu.HitTest(input.CursorPosition)
                    && !this.HullSelectionRect.HitTest(input.CursorPosition)
                    && !this.ChooseFighterSub.Menu.HitTest(input.CursorPosition))
                {
                    if (input.ScrollOut)
                    {
                        this.TransitionZoom -= 0.1f;
                        if ((double) this.TransitionZoom < 0.300000011920929)
                            this.TransitionZoom = 0.3f;
                        if ((double) this.TransitionZoom > 2.65000009536743)
                            this.TransitionZoom = 2.65f;
                    }
                    if (input.ScrollIn)
                    {
                        this.TransitionZoom += 0.1f;
                        if ((double) this.TransitionZoom < 0.300000011920929)
                            this.TransitionZoom = 0.3f;
                        if ((double) this.TransitionZoom > 2.65000009536743)
                            this.TransitionZoom = 2.65f;
                    }
                }

                if (Debug)
                {
                    if (input.CurrentKeyboardState.IsKeyDown(Keys.Enter) && input.LastKeyboardState.IsKeyUp(Keys.Enter))
                    {
                        foreach (ModuleSlotData moduleSlotData in ActiveHull.ModuleSlots)
                            moduleSlotData.InstalledModuleUID = null;
                        new XmlSerializer(typeof(ShipData)).Serialize(
                            new StreamWriter("Content/Hulls/" + ActiveHull.ShipStyle + "/" + ActiveHull.Name + ".xml"),
                            ActiveHull);
                    }
                    if (input.Right)
                        ++Operation;
                    if (Operation > SlotModOperation.Normal)
                        Operation = SlotModOperation.Delete;
                }

                this.HoveredModule = null;
                this.MouseStateCurrent = Mouse.GetState();
                Vector2 vector2 = new Vector2(MouseStateCurrent.X, MouseStateCurrent.Y);
                this.selector = null;
                this.EmpireUI.HandleInput(input, this);
                this.ActiveModSubMenu.HandleInputNoReset(this);
                this.HullSL.HandleInput(input);
                for (int index = HullSL.indexAtTop;
                    index < HullSL.Copied.Count && index < HullSL.indexAtTop + HullSL.entriesToDisplay;
                    ++index)
                {
                    ScrollList.Entry e = HullSL.Copied[index];
                    if (e.item is ModuleHeader)
                    {
                        if ((e.item as ModuleHeader).HandleInput(input, e))
                            return;
                    }
                    else if (e.clickRect.HitTest(vector2))
                    {
                        this.selector = new Selector(this.ScreenManager, e.clickRect);
                        e.clickRectHover = 1;
                        this.selector = new Selector(this.ScreenManager, e.clickRect);
                        if (input.InGameSelect)
                        {
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                            if (!this.ShipSaved && !this.CheckDesign())
                            {
                                MessageBoxScreen messageBoxScreen =
                                    new MessageBoxScreen(this, Localizer.Token(2121), "Save", "No");
                                messageBoxScreen.Accepted += new EventHandler<EventArgs>(this.SaveWIPThenChangeHull);
                                messageBoxScreen.Cancelled += new EventHandler<EventArgs>(this.JustChangeHull);
                                this.Changeto = e.item as ShipData;
                                this.ScreenManager.AddScreen((GameScreen) messageBoxScreen);
                                return;
                            }
                            else
                            {
                                this.ChangeHull(e.item as ShipData);
                                return;
                            }
                        }
                    }
                    else
                        e.clickRectHover = 0;
                }
                this.ModSel.HandleInput((object) this);
                if (this.ActiveModule != null)
                {
                    if (this.ActiveModule.ModuleType == ShipModuleType.Hangar && !this.ActiveModule.IsTroopBay
                        && !this.ActiveModule.IsSupplyBay)
                    {
                        this.UpdateHangarOptions(this.ActiveModule);
                        this.ChooseFighterSL.HandleInput(input);
                        for (int index = this.ChooseFighterSL.indexAtTop;
                            index < this.ChooseFighterSL.Copied.Count
                            && index < this.ChooseFighterSL.indexAtTop + this.ChooseFighterSL.entriesToDisplay;
                            ++index)
                        {
                            ScrollList.Entry entry = this.ChooseFighterSL.Copied[index];
                            if (entry.clickRect.HitTest(vector2))
                            {
                                this.selector = new Selector(this.ScreenManager, entry.clickRect);
                                entry.clickRectHover = 1;
                                this.selector = new Selector(this.ScreenManager, entry.clickRect);
                                if (input.InGameSelect)
                                {
                                    this.ActiveModule.hangarShipUID = (entry.item as Ship).Name;
                                    this.HangarShipUIDLast = (entry.item as Ship).Name;
                                    GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                                    return;
                                }
                            }
                        }
                    }
                }
                else if (this.HighlightedModule != null && this.HighlightedModule.ModuleType == ShipModuleType.Hangar
                         && (!this.HighlightedModule.IsTroopBay && !this.HighlightedModule.IsSupplyBay))
                {
                    this.ChooseFighterSL.HandleInput(input);
                    for (int index = this.ChooseFighterSL.indexAtTop;
                        index < this.ChooseFighterSL.Copied.Count
                        && index < this.ChooseFighterSL.indexAtTop + this.ChooseFighterSL.entriesToDisplay;
                        ++index)
                    {
                        ScrollList.Entry entry = this.ChooseFighterSL.Copied[index];
                        if (entry.clickRect.HitTest(vector2))
                        {
                            this.selector = new Selector(this.ScreenManager, entry.clickRect);
                            entry.clickRectHover = 1;
                            this.selector = new Selector(this.ScreenManager, entry.clickRect);
                            if (input.InGameSelect)
                            {
                                this.HighlightedModule.hangarShipUID = (entry.item as Ship).Name;
                                this.HangarShipUIDLast = (entry.item as Ship).Name;
                                GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                                return;
                            }
                        }
                    }
                }
                for (int index = this.WeaponSl.indexAtTop;
                    index < this.WeaponSl.Copied.Count
                    && index < this.WeaponSl.indexAtTop + this.WeaponSl.entriesToDisplay;
                    ++index)
                {
                    ScrollList.Entry e = this.WeaponSl.Copied[index];
                    if (e.item is ModuleHeader)
                    {
                        if ((e.item as ModuleHeader).HandleInput(input, e))
                            return;
                    }
                    else if (e.clickRect.HitTest(vector2))
                    {
                        this.selector = new Selector(this.ScreenManager, e.clickRect);
                        e.clickRectHover = 1;
                        this.selector = new Selector(this.ScreenManager, e.clickRect);
                        if (input.InGameSelect)
                        {
                            this.SetActiveModule(ShipModule.CreateNoParent((e.item as ShipModule).UID));
                            this.ResetModuleState();
                            return;
                        }
                    }
                    else
                        e.clickRectHover = 0;
                }
                this.WeaponSl.HandleInput(input);
                if (this.HullSelectionRect.HitTest(input.CursorPosition)
                    && input.CurrentMouseState.LeftButton == ButtonState.Pressed
                    || this.ModSel.Menu.HitTest(input.CursorPosition)
                    && input.CurrentMouseState.LeftButton == ButtonState.Pressed
                    || this.ActiveModSubMenu.Menu.HitTest(input.CursorPosition)
                    && input.CurrentMouseState.LeftButton == ButtonState.Pressed)
                    return;
                if (this.ModSel.Menu.HitTest(vector2))
                {
                    if (this.MouseStateCurrent.ScrollWheelValue > this.MouseStatePrevious.ScrollWheelValue
                        && this.WeaponSl.indexAtTop > 0)
                        --this.WeaponSl.indexAtTop;
                    if (this.MouseStateCurrent.ScrollWheelValue < this.MouseStatePrevious.ScrollWheelValue
                        && this.WeaponSl.indexAtTop + this.WeaponSl.entriesToDisplay < this.WeaponSl.Entries.Count)
                        ++this.WeaponSl.indexAtTop;
                }
                if (this.ArcsButton.R.HitTest(input.CursorPosition))
                    ToolTip.CreateTooltip(134, this.ScreenManager);
                if (this.ArcsButton.HandleInput(input))
                {
                    this.ArcsButton.ToggleOn = !this.ArcsButton.ToggleOn;
                    this.ShowAllArcs = this.ArcsButton.ToggleOn;
                }
                if (input.Tab)
                {
                    this.ShowAllArcs = !this.ShowAllArcs;
                    this.ArcsButton.ToggleOn = this.ShowAllArcs;
                }
                if (input.CurrentMouseState.RightButton == ButtonState.Pressed &&
                    input.LastMouseState.RightButton == ButtonState.Released)
                {
                    this.StartDragPos = input.CursorPosition;
                    this.CameraVelocity.X = 0.0f;
                    this.CameraVelocity.Y = 0.0f;
                }
                if (input.CurrentMouseState.RightButton == ButtonState.Pressed &&
                    input.LastMouseState.RightButton == ButtonState.Pressed)
                {
                    float num1 = input.CursorPosition.X - this.StartDragPos.X;
                    float num2 = input.CursorPosition.Y - this.StartDragPos.Y;
                    this.Camera._pos += new Vector2(-num1, -num2);
                    this.StartDragPos = input.CursorPosition;
                    this.cameraPosition.X += -num1;
                    this.cameraPosition.Y += -num2;
                }
                else
                {
                    this.CameraVelocity.X = 0.0f;
                    this.CameraVelocity.Y = 0.0f;
                }
                this.CameraVelocity.X = MathHelper.Clamp(this.CameraVelocity.X, -10f, 10f);
                this.CameraVelocity.Y = MathHelper.Clamp(this.CameraVelocity.Y, -10f, 10f);
                if (input.Escaped)
                    this.ExitScreen();
                if (this.ToggleOverlay)
                {
                    foreach (SlotStruct slotStruct in this.Slots)
                    {
                        Vector2 spaceFromWorldSpace =
                            this.Camera.GetScreenSpaceFromWorldSpace(new Vector2((float) slotStruct.PQ.enclosingRect.X,
                                (float) slotStruct.PQ.enclosingRect.Y));
                        if (new Rectangle((int) spaceFromWorldSpace.X, (int) spaceFromWorldSpace.Y,
                                (int) (16.0 * (double) this.Camera.Zoom), (int) (16.0 * (double) this.Camera.Zoom))
                            .HitTest(vector2))
                        {
                            if (slotStruct.Module != null)
                                this.HoveredModule = slotStruct.Module;
                            if (input.CurrentMouseState.LeftButton == ButtonState.Pressed &&
                                input.LastMouseState.LeftButton == ButtonState.Released)
                            {
                                GameAudio.PlaySfxAsync("simple_beep");
                                if (this.Debug)
                                {
                                    this.DebugAlterSlot(slotStruct.SlotReference.Position, this.Operation);
                                    return;
                                }
                                else if (slotStruct.Module != null)
                                    this.HighlightedModule = slotStruct.Module;
                            }
                        }
                    }
                }
                if (this.UpArrow.HitTest(vector2) && this.MouseStateCurrent.LeftButton == ButtonState.Released &&
                    (this.MouseStatePrevious.LeftButton == ButtonState.Pressed && this.ScrollPosition > 0))
                {
                    --this.ScrollPosition;
                    GameAudio.PlaySfxAsync("blip_click");
                    foreach (ModuleButton moduleButton in this.ModuleButtons)
                        moduleButton.moduleRect.Y += 128;
                }
                if (this.DownArrow.HitTest(vector2) && input.LeftMouseClick)
                {
                    ++this.ScrollPosition;
                    GameAudio.PlaySfxAsync("blip_click");
                    foreach (ModuleButton moduleButton in this.ModuleButtons)
                        moduleButton.moduleRect.Y -= 128;
                }
                if (this.ModuleSelectionArea.HitTest(vector2))
                {
                    if (input.ScrollIn && this.ScrollPosition > 0)
                    {
                        --this.ScrollPosition;
                        GameAudio.PlaySfxAsync("blip_click");
                        foreach (ModuleButton moduleButton in this.ModuleButtons)
                            moduleButton.moduleRect.Y += 128;
                    }
                    if (input.ScrollOut)
                    {
                        ++this.ScrollPosition;
                        GameAudio.PlaySfxAsync("blip_click");
                        foreach (ModuleButton moduleButton in this.ModuleButtons)
                            moduleButton.moduleRect.Y -= 128;
                    }
                }
                if (input.RightMouseClick)
                {
                    //this should actually clear slots
                    this.ActiveModule = (ShipModule) null;
                    foreach (SlotStruct slot in this.Slots)
                    {
                        slot.SetValidity(null);
                        Vector2 spaceFromWorldSpace = this.Camera.GetScreenSpaceFromWorldSpace(
                            new Vector2((float) slot.PQ.enclosingRect.X, (float) slot.PQ.enclosingRect.Y));
                        Rectangle rect = new Rectangle((int) spaceFromWorldSpace.X, (int) spaceFromWorldSpace.Y
                            , (int) (16.0 * (double) this.Camera.Zoom), (int) (16.0 * (double) this.Camera.Zoom));
                        if (slot.Module != null && rect.HitTest(vector2)) //if clicked at this slot
                        {
                            slot.SetValidity(slot.Module);
                            DesignAction designAction = new DesignAction();
                            designAction.clickedSS = new SlotStruct();
                            designAction.clickedSS.PQ = slot.PQ;
                            designAction.clickedSS.Restrictions = slot.Restrictions;
                            designAction.clickedSS.Facing = slot.Module != null ? slot.Module.Facing : 0.0f;
                            designAction.clickedSS.ModuleUID = slot.ModuleUID;
                            designAction.clickedSS.Module = slot.Module;
                            designAction.clickedSS.SlotReference = slot.SlotReference;
                            DesignStack.Push(designAction);
                            GameAudio.PlaySfxAsync("sub_bass_whoosh");
                            ClearParentSlot(slot);
                            RecalculatePower();
                        }
                    }
                }
                foreach (ModuleButton moduleButton in this.ModuleButtons)
                {
                    if (this.ModuleSelectionArea.HitTest(new Vector2((float) (moduleButton.moduleRect.X + 30),
                        (float) (moduleButton.moduleRect.Y + 30))))
                    {
                        if (moduleButton.moduleRect.HitTest(vector2))
                        {
                            if (input.InGameSelect)
                                this.SetActiveModule(ShipModule.CreateNoParent(moduleButton.ModuleUID));
                            moduleButton.isHighlighted = true;
                        }
                        else
                            moduleButton.isHighlighted = false;
                    }
                }
                if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && this.ActiveModule != null)
                {
                    foreach (SlotStruct slot in this.Slots)
                    {
                        Vector2 spaceFromWorldSpace = this.Camera.GetScreenSpaceFromWorldSpace(new Vector2(
                            (float) slot.PQ.enclosingRect.X
                            , (float) slot.PQ.enclosingRect.Y));
                        if (new Rectangle((int) spaceFromWorldSpace.X, (int) spaceFromWorldSpace.Y
                                , (int) (16.0 * (double) this.Camera.Zoom), (int) (16.0 * (double) this.Camera.Zoom))
                            .HitTest(vector2))
                        {
                            GameAudio.PlaySfxAsync("sub_bass_mouseover");

                            if (slot.PQ.X != this.LastDesignActionPos.X || slot.PQ.Y != this.LastDesignActionPos.Y
                                || ActiveModule.UID != this.LastActiveUID)
                            {
                                this.InstallModule(
                                    slot); //This will make the Ctrl+Z functionality in the shipyard a lot more responsive -Gretman
                                this.LastDesignActionPos.X = slot.PQ.X;
                                this.LastDesignActionPos.Y = slot.PQ.Y;
                                this.LastActiveUID = ActiveModule.UID;
                            }
                        }
                    }
                }
                else if (input.LeftMouseClick)
                    this.HoldTimer -= .01666f;
                else
                    this.HoldTimer = 0.50f;

                foreach (SlotStruct slotStruct in this.Slots)
                {
                    if (slotStruct.ModuleUID != null && this.HighlightedModule != null &&
                        (slotStruct.Module == this.HighlightedModule &&
                         (double) slotStruct.Module.FieldOfFire != 0.0) &&
                        slotStruct.Module.ModuleType == ShipModuleType.Turret)
                    {
                        float num1 = slotStruct.Module.FieldOfFire / 2f;
                        Vector2 spaceFromWorldSpace =
                            this.Camera.GetScreenSpaceFromWorldSpace(new Vector2(
                                (float) (slotStruct.PQ.enclosingRect.X + 16 * (int) slotStruct.Module.XSIZE / 2),
                                (float) (slotStruct.PQ.enclosingRect.Y + 16 * (int) slotStruct.Module.YSIZE / 2)));
                        float num2 = spaceFromWorldSpace.AngleToTarget(vector2);
                        float num3 = this.HighlightedModule.Facing;
                        float num4 = Math.Abs(num2 - num3);
                        if ((double) num4 > (double) num1)
                        {
                            if ((double) num2 > 180.0)
                                num2 = (float) (-1.0 * (360.0 - (double) num2));
                            if ((double) num3 > 180.0)
                                num3 = (float) (-1.0 * (360.0 - (double) num3));
                            num4 = Math.Abs(num2 - num3);
                        }

                        if (GlobalStats.AltArcControl)
                        {
                            //The Doctor: ALT (either) + LEFT CLICK to pick and move arcs. This way, it's impossible to accidentally pick the wrong arc, while it's just as responsive and smooth as the original method when you are trying to.                    
                            if ((double) num4 < (double) num1 &&
                                (this.MouseStateCurrent.LeftButton == ButtonState.Pressed &&
                                 this.MouseStatePrevious.LeftButton == ButtonState.Pressed &&
                                 ((input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt) ||
                                   input.LastKeyboardState.IsKeyDown(Keys.LeftAlt)) ||
                                  (input.CurrentKeyboardState.IsKeyDown(Keys.RightAlt) ||
                                   input.LastKeyboardState.IsKeyDown(Keys.RightAlt)))))
                            {
                                this.HighlightedModule.Facing = spaceFromWorldSpace.AngleToTarget(vector2);
                            }
                        }
                        else
                        {
                            //Delay method
                            if ((this.MouseStateCurrent.LeftButton == ButtonState.Pressed &&
                                 this.MouseStatePrevious.LeftButton == ButtonState.Pressed && this.HoldTimer < 0))
                            {
                                this.HighlightedModule.Facing = spaceFromWorldSpace.AngleToTarget(vector2);
                            }
                        }
                    }
                }
                foreach (UIButton uiButton in this.Buttons)
                {
                    if (uiButton.Rect.HitTest(vector2))
                    {
                        uiButton.State = UIButton.PressState.Hover;
                        if (this.MouseStateCurrent.LeftButton == ButtonState.Pressed &&
                            this.MouseStatePrevious.LeftButton == ButtonState.Pressed)
                            uiButton.State = UIButton.PressState.Pressed;
                        if (this.MouseStateCurrent.LeftButton == ButtonState.Released &&
                            this.MouseStatePrevious.LeftButton == ButtonState.Pressed)
                        {
                            switch (uiButton.Launches)
                            {
                                case "Toggle Overlay":
                                    GameAudio.PlaySfxAsync("blip_click");
                                    this.ToggleOverlay = !this.ToggleOverlay;
                                    continue;
                                case "Save As...":
                                    if (this.CheckDesign())
                                    {
                                        this.ScreenManager.AddScreen(new DesignManager(this, this.ActiveHull.Name));
                                        continue;
                                    }
                                    else
                                    {
                                        GameAudio.PlaySfxAsync("UI_Misc20");
                                        this.ScreenManager.AddScreen(new MessageBoxScreen(this, Localizer.Token(2049)));
                                        continue;
                                    }
                                case "Load":
                                    this.ScreenManager.AddScreen((GameScreen) new LoadDesigns(this));
                                    continue;
                                default:
                                    continue;
                            }
                        }
                    }
                    else
                        uiButton.State = UIButton.PressState.Default;
                }
                if (this.ActiveHull != null)
                {
                    foreach (ToggleButton toggleButton in this.CombatStatusButtons)
                    {
                        if (toggleButton.r.HitTest(input.CursorPosition))
                        {
                            if (toggleButton.HasToolTip)
                                ToolTip.CreateTooltip(toggleButton.WhichToolTip, this.ScreenManager);
                            if (input.InGameSelect)
                            {
                                GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                                switch (toggleButton.Action)
                                {
                                    case "attack":
                                        this.CombatState = CombatState.AttackRuns;
                                        break;
                                    case "arty":
                                        this.CombatState = CombatState.Artillery;
                                        break;
                                    case "hold":
                                        this.CombatState = CombatState.HoldPosition;
                                        break;
                                    case "orbit_left":
                                        this.CombatState = CombatState.OrbitLeft;
                                        break;
                                    case "broadside_left":
                                        this.CombatState = CombatState.BroadsideLeft;
                                        break;
                                    case "orbit_right":
                                        this.CombatState = CombatState.OrbitRight;
                                        break;
                                    case "broadside_right":
                                        this.CombatState = CombatState.BroadsideRight;
                                        break;
                                    case "evade":
                                        this.CombatState = CombatState.Evade;
                                        break;
                                    case "short":
                                        this.CombatState = CombatState.ShortRange;
                                        break;
                                }
                            }
                        }
                        else
                            toggleButton.Hover = false;
                        switch (toggleButton.Action)
                        {
                            case "attack":
                                toggleButton.Active = this.CombatState == CombatState.AttackRuns;
                                continue;
                            case "arty":
                                toggleButton.Active = this.CombatState == CombatState.Artillery;
                                continue;
                            case "hold":
                                toggleButton.Active = this.CombatState == CombatState.HoldPosition;
                                continue;
                            case "orbit_left":
                                toggleButton.Active = this.CombatState == CombatState.OrbitLeft;
                                continue;
                            case "broadside_left":
                                toggleButton.Active = this.CombatState == CombatState.BroadsideLeft;
                                continue;
                            case "orbit_right":
                                toggleButton.Active = this.CombatState == CombatState.OrbitRight;
                                continue;
                            case "broadside_right":
                                toggleButton.Active = this.CombatState == CombatState.BroadsideRight;
                                continue;
                            case "evade":
                                toggleButton.Active = this.CombatState == CombatState.Evade;
                                continue;
                            case "short":
                                toggleButton.Active = this.CombatState == CombatState.ShortRange;
                                continue;
                            default:
                                continue;
                        }
                    }
                }
                this.MouseStatePrevious = this.MouseStateCurrent;
                base.HandleInput(input);
            }
        }

        private void JustChangeHull(object sender, EventArgs e)
        {
            this.ShipSaved = true;
            this.ChangeHull(this.Changeto);
        }

        private void LaunchScreen(object sender, EventArgs e)
        {
            string str = this.ScreenToLaunch;
            string str1 = str;
            if (str != null)
            {
                if (str1 == "Research")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    ScreenManager.AddScreen(new ResearchScreenNew(this, EmpireUI));
                }
                else if (str1 == "Budget")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    ScreenManager.AddScreen(new BudgetScreen(Empire.Universe));
                }
            }
            string str2 = this.ScreenToLaunch;
            string str3 = str2;
            if (str2 != null)
            {
                if (str3 == "Main Menu")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    ScreenManager.AddScreen(new GameplayMMScreen(Empire.Universe));
                }
                else if (str3 == "Shipyard")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else if (str3 == "Empire")
                {
                    ScreenManager.AddScreen(new EmpireScreen(Empire.Universe, this.EmpireUI));
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else if (str3 == "Diplomacy")
                {
                    ScreenManager.AddScreen(new MainDiplomacyScreen(Empire.Universe));
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else if (str3 == "?")
                {
                    GameAudio.PlaySfxAsync("sd_ui_tactical_pause");
                    InGameWiki wiki = new InGameWiki(this, new Rectangle(0, 0, 750, 600))
                    {
                        TitleText = "StarDrive Help",
                        MiddleText =
                            "This help menu contains information on all of the gameplay systems contained in StarDrive. You can also watch one of several tutorial videos for a developer-guided introduction to StarDrive."
                    };
                }
            }
            this.ReallyExit();
        }

        public override void LoadContent()
        {
            LightRig rig = TransientContent.Load<LightRig>("example/ShipyardLightrig");
            rig.AssignTo(this);
            if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280 || base.ScreenManager
                    .GraphicsDevice.PresentationParameters.BackBufferHeight <= 768)
            {
                this.LowRes = true;
            }
            Rectangle leftRect = new Rectangle(5, 45, 405,
                base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 45 -
                (int) (0.4f * (float) base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight) + 10);
            this.ModuleSelectionMenu = new Menu1(base.ScreenManager, leftRect);
            Rectangle modSelR = new Rectangle(0, (this.LowRes ? 45 : 100), 305, (this.LowRes ? 350 : 400));
            this.ModSel = new Submenu(base.ScreenManager, modSelR, true);
            this.ModSel.AddTab("Wpn");
            this.ModSel.AddTab("Pwr");
            this.ModSel.AddTab("Def");
            this.ModSel.AddTab("Spc");
            this.WeaponSl = new ScrollList(this.ModSel);
            Vector2 Cursor =
                new Vector2(
                    (float) (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 175), 80f);
            Rectangle active = new Rectangle(modSelR.X, modSelR.Y + modSelR.Height + 15, modSelR.Width, 300);
            this.activeModWindow = new Menu1(base.ScreenManager, active);
            Rectangle acsub = new Rectangle(active.X, modSelR.Y + modSelR.Height + 15, 305, 320);
            if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight > 760)
            {
                acsub.Height = acsub.Height + 120;
            }
            this.ActiveModSubMenu = new Submenu(base.ScreenManager, acsub);
            this.ActiveModSubMenu.AddTab("Active Module");
            this.Choosefighterrect = new Rectangle(acsub.X + acsub.Width + 5, acsub.Y - 90, 240, 270);
            if (this.Choosefighterrect.Y + this.Choosefighterrect.Height >
                base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
            {
                int diff = this.Choosefighterrect.Y + this.Choosefighterrect.Height - base.ScreenManager.GraphicsDevice
                               .PresentationParameters.BackBufferHeight;
                this.Choosefighterrect.Height = this.Choosefighterrect.Height - (diff + 10);
            }
            this.Choosefighterrect.Height = acsub.Height;
            this.ChooseFighterSub = new Submenu(base.ScreenManager, this.Choosefighterrect);
            this.ChooseFighterSub.AddTab("Choose Fighter");
            this.ChooseFighterSL = new ScrollList(this.ChooseFighterSub, 40);
            foreach (KeyValuePair<string, bool> hull in EmpireManager.Player.GetHDict())
            {
                if (!hull.Value)
                {
                    continue;
                }
                this.AvailableHulls.Add(Ship_Game.ResourceManager.HullsDict[hull.Key]);
            }
            PrimitiveQuad.graphicsDevice = base.ScreenManager.GraphicsDevice;
            float width = (float) base.Viewport.Width;
            Viewport viewport = base.Viewport;
            float aspectRatio = width / (float) viewport.Height;
            this.offset = new Vector2();
            Viewport viewport1 = base.Viewport;
            this.offset.X = (float) (viewport1.Width / 2 - 256);
            Viewport viewport2 = base.Viewport;
            this.offset.Y = (float) (viewport2.Height / 2 - 256);
            this.Camera = new Camera2d();
            Camera2d vector2 = this.Camera;
            Viewport viewport3 = base.Viewport;
            float single = (float) viewport3.Width / 2f;
            Viewport viewport4 = base.Viewport;
            vector2.Pos = new Vector2(single, (float) viewport4.Height / 2f);
            Vector3 camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
            this.View = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) *
                         Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos,
                            new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
            this.Projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 20000f);
            this.ChangeHull(this.AvailableHulls[0]);
            lock (GlobalStats.ObjectManagerLocker)
            {
                if (!ActiveHull.Animated)
                {
                    ActiveModel = TransientContent.Load<Model>(ActiveHull.ModelPath);
                    CreateSOFromHull();
                }
                else
                {
                    base.ScreenManager.inter.ObjectManager.Remove(this.shipSO);
                    SkinnedModel sm = ResourceManager.GetSkinnedModel(this.ActiveHull.ModelPath);
                    this.shipSO = new SceneObject(sm.Model)
                    {
                        ObjectType = ObjectType.Dynamic,
                        World = this.WorldMatrix
                    };
                    base.ScreenManager.inter.ObjectManager.Submit(this.shipSO);
                    this.SetupSlots();
                }
            }
            foreach (ModuleSlotData slot in this.ActiveHull.ModuleSlots)
            {
                if (slot.Position.X < this.LowestX)
                {
                    this.LowestX = slot.Position.X;
                }
                if (slot.Position.X <= this.HighestX)
                {
                    continue;
                }
                this.HighestX = slot.Position.X;
            }
            float xDistance = this.HighestX - this.LowestX;
            BoundingSphere bs = this.shipSO.WorldBoundingSphere;
            Viewport viewport5 = base.Viewport;
            Vector3 pScreenSpace = viewport5.Project(Vector3.Zero, this.Projection, this.View, Matrix.Identity);
            Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
            Vector2 radialPos = MathExt.PointOnCircle(90f, xDistance);
            Viewport viewport6 = base.Viewport;
            Vector3 insetRadialPos = viewport6.Project(new Vector3(radialPos, 0f), this.Projection, this.View,
                Matrix.Identity);
            Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
            float Radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
            if (Radius >= xDistance)
            {
                while (Radius > xDistance)
                {
                    camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
                    this.View = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) *
                                 Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos,
                                    new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
                    bs = this.shipSO.WorldBoundingSphere;
                    Viewport viewport7 = base.Viewport;
                    pScreenSpace = viewport7.Project(Vector3.Zero, this.Projection, this.View, Matrix.Identity);
                    pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    radialPos = MathExt.PointOnCircle(90f, xDistance);
                    Viewport viewport8 = base.Viewport;
                    insetRadialPos = viewport8.Project(new Vector3(radialPos, 0f), this.Projection, this.View,
                        Matrix.Identity);
                    insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    Radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
                    this.cameraPosition.Z = this.cameraPosition.Z + 1f;
                }
            }
            else
            {
                while (Radius < xDistance)
                {
                    camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
                    this.View = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) *
                                 Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos,
                                    new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
                    bs = this.shipSO.WorldBoundingSphere;
                    Viewport viewport9 = base.Viewport;
                    pScreenSpace = viewport9.Project(Vector3.Zero, this.Projection, this.View, Matrix.Identity);
                    pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    radialPos = MathExt.PointOnCircle(90f, xDistance);
                    Viewport viewport10 = base.Viewport;
                    insetRadialPos = viewport10.Project(new Vector3(radialPos, 0f), this.Projection, this.View,
                        Matrix.Identity);
                    insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
                    Radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
                    this.cameraPosition.Z = this.cameraPosition.Z - 1f;
                }
            }
            this.BlackBar = new Rectangle(0,
                base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 70, 3000, 70);
            this.SideBar = new Rectangle(0, 0, 280,
                base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);
            Rectangle w = new Rectangle(20, this.ModSel.Menu.Y - 10, 32, 32);
            Rectangle p = new Rectangle(80, w.Y, 32, 32);
            Rectangle df = new Rectangle(150, w.Y, 32, 32);
            Rectangle sp = new Rectangle(220, w.Y, 32, 32);
            this.wpn = new SkinnableButton(w, "Modules/FlakTurret3x3")
            {
                IsToggle = true,
                Toggled = true
            };
            this.pwr = new SkinnableButton(p, "Modules/NuclearReactorMedium")
            {
                IsToggle = true
            };
            this.def = new SkinnableButton(df, "Modules/SteelArmorMedium")
            {
                IsToggle = true
            };
            this.spc = new SkinnableButton(sp, "Modules/sensors_2x2")
            {
                IsToggle = true
            };
            this.SelectedCatTextPos = new Vector2(20f, (float) (w.Y - 25 - Fonts.Arial20Bold.LineSpacing / 2));
            this.SearchBar =
                new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 585,
                    base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 47, 210, 25);
            this.ClassifCursor =
                new Vector2(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * .5f,
                    Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Height + 10);
            Cursor = new Vector2((float) (this.ClassifCursor.X), (float) (this.ClassifCursor.Y));
            Vector2 OrdersBarPos = new Vector2(Cursor.X, (float) ((int) Cursor.Y + 20));
            OrdersBarPos.X = OrdersBarPos.X - 15;
            ToggleButton AttackRuns =
                new ToggleButton(new Rectangle((int) OrdersBarPos.X, (int) OrdersBarPos.Y, 24, 24),
                    "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                    "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                    "SelectionBox/icon_formation_headon");
            this.CombatStatusButtons.Add(AttackRuns);
            AttackRuns.Action = "attack";
            AttackRuns.HasToolTip = true;
            AttackRuns.WhichToolTip = 1;

            OrdersBarPos.X = OrdersBarPos.X + 29f;
            ToggleButton ShortRange =
                new ToggleButton(new Rectangle((int) OrdersBarPos.X, (int) OrdersBarPos.Y, 24, 24),
                    "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                    "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                    "SelectionBox/icon_grid");
            this.CombatStatusButtons.Add(ShortRange);
            ShortRange.Action = "short";
            ShortRange.HasToolTip = true;
            ShortRange.WhichToolTip = 228;

            OrdersBarPos.X = OrdersBarPos.X + 29f;
            ToggleButton Artillery = new ToggleButton(new Rectangle((int) OrdersBarPos.X, (int) OrdersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                "SelectionBox/icon_formation_aft");
            this.CombatStatusButtons.Add(Artillery);
            Artillery.Action = "arty";
            Artillery.HasToolTip = true;
            Artillery.WhichToolTip = 2;

            OrdersBarPos.X = OrdersBarPos.X + 29f;
            ToggleButton HoldPos = new ToggleButton(new Rectangle((int) OrdersBarPos.X, (int) OrdersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                "SelectionBox/icon_formation_x");
            this.CombatStatusButtons.Add(HoldPos);
            HoldPos.Action = "hold";
            HoldPos.HasToolTip = true;
            HoldPos.WhichToolTip = 65;
            OrdersBarPos.X = OrdersBarPos.X + 29f;
            ToggleButton OrbitLeft = new ToggleButton(new Rectangle((int) OrdersBarPos.X, (int) OrdersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                "SelectionBox/icon_formation_left");
            this.CombatStatusButtons.Add(OrbitLeft);
            OrbitLeft.Action = "orbit_left";
            OrbitLeft.HasToolTip = true;
            OrbitLeft.WhichToolTip = 3;
            OrdersBarPos.Y = OrdersBarPos.Y + 29f;

            ToggleButton BroadsideLeft =
                new ToggleButton(new Rectangle((int) OrdersBarPos.X, (int) OrdersBarPos.Y, 24, 24),
                    "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                    "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                    "SelectionBox/icon_formation_bleft");
            this.CombatStatusButtons.Add(BroadsideLeft);
            BroadsideLeft.Action = "broadside_left";
            BroadsideLeft.HasToolTip = true;
            BroadsideLeft.WhichToolTip = 159;
            OrdersBarPos.Y = OrdersBarPos.Y - 29f;
            OrdersBarPos.X = OrdersBarPos.X + 29f;
            ToggleButton OrbitRight =
                new ToggleButton(new Rectangle((int) OrdersBarPos.X, (int) OrdersBarPos.Y, 24, 24),
                    "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                    "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                    "SelectionBox/icon_formation_right");
            this.CombatStatusButtons.Add(OrbitRight);
            OrbitRight.Action = "orbit_right";
            OrbitRight.HasToolTip = true;
            OrbitRight.WhichToolTip = 4;
            OrdersBarPos.Y = OrdersBarPos.Y + 29f;

            ToggleButton BroadsideRight =
                new ToggleButton(new Rectangle((int) OrdersBarPos.X, (int) OrdersBarPos.Y, 24, 24),
                    "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                    "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                    "SelectionBox/icon_formation_bright");
            this.CombatStatusButtons.Add(BroadsideRight);
            BroadsideRight.Action = "broadside_right";
            BroadsideRight.HasToolTip = true;
            BroadsideRight.WhichToolTip = 160;
            OrdersBarPos.Y = OrdersBarPos.Y - 29f;
            OrdersBarPos.X = OrdersBarPos.X + 29f;
            ToggleButton Evade = new ToggleButton(new Rectangle((int) OrdersBarPos.X, (int) OrdersBarPos.Y, 24, 24),
                "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive",
                "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press",
                "SelectionBox/icon_formation_stop");
            this.CombatStatusButtons.Add(Evade);
            Evade.Action = "evade";
            Evade.HasToolTip = true;
            Evade.WhichToolTip = 6;

            Cursor = new Vector2(
                (float) (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 150),
                (float) base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 47);

            this.SaveButton = new UIButton()
            {
                Rect = new Rectangle((int) Cursor.X, (int) Cursor.Y,
                    Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Width,
                    Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Height),
                NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"],
                HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_hover"],
                PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_pressed"],
                Text = Localizer.Token(105),
                Launches = "Save As..."
            };
            this.Buttons.Add(this.SaveButton);
            this.LoadButton = new UIButton()
            {
                Rect = new Rectangle((int) Cursor.X - 78, (int) Cursor.Y,
                    Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width,
                    Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
                NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
                HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
                PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
                Text = Localizer.Token(8),
                Launches = "Load"
            };
            this.Buttons.Add(this.LoadButton);
            this.ToggleOverlayButton = new UIButton()
            {
                Rect = new Rectangle(this.LoadButton.Rect.X - 140, (int) Cursor.Y,
                    Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Width,
                    Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
                NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"],
                HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_hover"],
                PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_pressed"],
                Launches = "Toggle Overlay",
                Text = Localizer.Token(106)
            };
            this.Buttons.Add(this.ToggleOverlayButton);
            this.BottomSep = new Rectangle(this.BlackBar.X, this.BlackBar.Y, this.BlackBar.Width, 1);
            this.HullSelectionRect =
                new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 285,
                    (this.LowRes ? 45 : 100), 280, (this.LowRes ? 350 : 400));
            this.HullSelectionSub = new Submenu(base.ScreenManager, this.HullSelectionRect, true);
            this.WeaponSl = new ScrollList(this.ModSel);
            this.HullSelectionSub.AddTab(Localizer.Token(107));
            this.HullSL = new ScrollList(this.HullSelectionSub);
            Array<string> Categories = new Array<string>();
            foreach (KeyValuePair<string, ShipData> hull in Ship_Game.ResourceManager.HullsDict)
            {
                if (!EmpireManager.Player.GetHDict()[hull.Key])
                {
                    continue;
                }
                string cat = Localizer.GetRole(hull.Value.Role, EmpireManager.Player);
                if (Categories.Contains(cat))
                {
                    continue;
                }
                Categories.Add(cat);
            }
            Categories.Sort();
            foreach (string cat in Categories)
            {
                ModuleHeader type = new ModuleHeader(cat, 240f);
                this.HullSL.AddItem(type);
            }
            foreach (ScrollList.Entry e in this.HullSL.Entries)
            {
                foreach (KeyValuePair<string, ShipData> hull in Ship_Game.ResourceManager.HullsDict)
                {
                    if (!EmpireManager.Player.GetHDict()[hull.Key] ||
                        !((e.item as ModuleHeader).Text == Localizer.GetRole(hull.Value.Role, EmpireManager.Player)))
                    {
                        continue;
                    }
                    e.AddItem(hull.Value);
                }
            }
            Rectangle ShipStatsPanel = new Rectangle(this.HullSelectionRect.X + 50,
                this.HullSelectionRect.Y + this.HullSelectionRect.Height - 20, 280, 320);


            //base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth
            DropdownRect =
                new Rectangle((int) (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * .25f),
                    (int) OrdersBarPos.Y, 100, 18);
            //dropdownRect = new Rectangle((int)ShipStatsPanel.X, (int)ShipStatsPanel.Y + ShipStatsPanel.Height + 118, 100, 18);

            this.CategoryList = new DropOptions(DropdownRect);
            //this.CategoryList.AddOption("Unclassified", 1);
            //this.CategoryList.AddOption("Civilian", 2);
            //this.CategoryList.AddOption("Recon", 3);
            //this.CategoryList.AddOption("Combat", 4);
            //this.CategoryList.AddOption("Kamikaze", 5);
            foreach (Ship_Game.ShipData.Category item in Enum.GetValues(typeof(Ship_Game.ShipData.Category))
                .Cast<Ship_Game.ShipData.Category>())
            {
                this.CategoryList.AddOption(item.ToString(), (int) item + 1);
            }

            CarrierOnly = ActiveHull.CarrierShip;
            CoBoxCursor = new Vector2(DropdownRect.X + 106, DropdownRect.Y);
            CarrierOnlyBox = new Checkbox(CoBoxCursor.X, CoBoxCursor.Y, () => CarrierOnly, Fonts.Arial12Bold,
                "Carrier Only", 0);

            this.ShipStats = new Menu1(base.ScreenManager, ShipStatsPanel);
            this.StatsSub = new Submenu(base.ScreenManager, ShipStatsPanel);
            this.StatsSub.AddTab(Localizer.Token(108));
            this.ArcsButton = new GenericButton(new Vector2((float) (this.HullSelectionRect.X - 32), 97f), "Arcs",
                Fonts.Pirulen20,
                Fonts.Pirulen16); //new GenericButton(new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 32), 97f), "Arcs", Fonts.Pirulen20, Fonts.Pirulen16);
            this.Close = new CloseButton(new Rectangle(
                base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 27, 99, 20, 20));
            this.OriginalZ = this.cameraPosition.Z;
        }

        private string parseText(string text, float Width, SpriteFont font)
        {
            string line = string.Empty;
            string returnString = string.Empty;
            string[] strArrays = text.Split(new char[] {' '});
            for (int i = 0; i < (int) strArrays.Length; i++)
            {
                string word = strArrays[i];
                if (font.MeasureString(string.Concat(line, word)).Length() > Width)
                {
                    returnString = string.Concat(returnString, line, '\n');
                    line = string.Empty;
                }
                line = string.Concat(line, word, ' ');
            }
            return string.Concat(returnString, line);
        }

        private void ReallyExit()
        {
            LightRig rig = TransientContent.Load<LightRig>("example/NewGamelight_rig");
            rig.AssignTo(this);

            lock (GlobalStats.ObjectManagerLocker)
            {
                base.ScreenManager.inter.ObjectManager.Remove(this.shipSO);
            }
            if (Empire.Universe.LookingAtPlanet && Empire.Universe.workersPanel is ColonyScreen)
            {
                (Empire.Universe.workersPanel as ColonyScreen).Reset = true;
            }
            //this should go some where else, need to find it a home
            this.ScreenManager.RemoveScreen(this);
            base.ExitScreen();
        }

        public void ResetLists()
        {
            this.Reset = true;
            this.WeaponSl.indexAtTop = 0;
        }

        private void ResetModuleState()
        {
            this.ActiveModState = ShipDesignScreen.ActiveModuleState.Normal;
        }

        private void SaveChanges(object sender, EventArgs e)
        {
            base.ScreenManager.AddScreen(new DesignManager(this, this.ActiveHull.Name));
            this.ShipSaved = true;
        }

        public void SaveShipDesign(string name)
        {
            ActiveHull.ModuleSlots = Empty<ModuleSlotData>.Array;
            ActiveHull.Name = name;
            ShipData toSave = ActiveHull.GetClone();

            toSave.ModuleSlots = new ModuleSlotData[Slots.Count];
            for (int i = 0; i < Slots.Count; ++i)
            {
                SlotStruct slot = Slots[i];
                ModuleSlotData savedSlot = new ModuleSlotData()
                {
                    InstalledModuleUID = slot.ModuleUID,
                    Position = slot.SlotReference.Position,
                    Restrictions = slot.Restrictions,
                };
                if (slot.Module != null)
                {
                    savedSlot.Facing = slot.Module.Facing;

                    if (slot.Module.ModuleType == ShipModuleType.Hangar)
                        savedSlot.SlotOptions = slot.Module.hangarShipUID;
                }
                toSave.ModuleSlots[i] = savedSlot;
            }
            string path = Dir.ApplicationData;
            CombatState combatState = toSave.CombatState;
            toSave.CombatState = CombatState;
            toSave.Name = name;

            //Cases correspond to the 5 options in the drop-down menu; default exists for... Propriety, mainly. The option selected when saving will always be the Category saved, pretty straightforward.
            foreach (var item in Enum.GetValues(typeof(ShipData.Category)).Cast<ShipData.Category>())
            {
                if (CategoryList.Options[CategoryList.ActiveIndex].Name == item.ToString())
                {
                    ActiveHull.ShipCategory = item;
                    break;
                }
            }

            //Adds the category determined by the case from the dropdown to the 'toSave' ShipData.
            toSave.ShipCategory = ActiveHull.ShipCategory;

            //Adds the boolean derived from the checkbox boolean (CarrierOnly) to the ShipData. Defaults to 'false'.
            toSave.CarrierShip = CarrierOnly;

            XmlSerializer Serializer = new XmlSerializer(typeof(ShipData));
            TextWriter WriteFileStream =
                new StreamWriter(string.Concat(path, "/StarDrive/Saved Designs/", name, ".xml"));
            Serializer.Serialize(WriteFileStream, toSave);
            WriteFileStream.Close();
            ShipSaved = true;

            Ship newShip = Ship.CreateShipFromShipData(toSave, fromSave: false);
            if (newShip == null) // happens if module creation failed
                return;
            newShip.SetShipData(toSave);
            newShip.InitializeStatus(fromSave: false);
            newShip.IsPlayerDesign = true;
            ResourceManager.ShipsDict[name] = newShip;

            newShip.BaseStrength = -1;
            newShip.BaseStrength = newShip.GetStrength();
            EmpireManager.Player.UpdateShipsWeCanBuild();
            ActiveHull.CombatState = CombatState;
            ChangeHull(ActiveHull);
        }

        private void SaveWIP(object sender, EventArgs e)
        {
            ShipData savedShip = new ShipData()
            {
                Animated = this.ActiveHull.Animated,
                CombatState = this.ActiveHull.CombatState,
                Hull = this.ActiveHull.Hull,
                IconPath = this.ActiveHull.IconPath,
                ModelPath = this.ActiveHull.ModelPath,
                Name = this.ActiveHull.Name,
                Role = this.ActiveHull.Role,
                ShipStyle = this.ActiveHull.ShipStyle,
                ThrusterList = this.ActiveHull.ThrusterList
            };

            savedShip.ModuleSlots = new ModuleSlotData[Slots.Count];
            for (int i = 0; i < Slots.Count; ++i)
            {
                SlotStruct slot = Slots[i];
                ModuleSlotData data = new ModuleSlotData
                {
                    InstalledModuleUID = slot.ModuleUID,
                    Position = slot.SlotReference.Position,
                    Restrictions = slot.Restrictions
                };
                if (slot.Module != null)
                {
                    if (slot.Module.ModuleType == ShipModuleType.Hangar)
                        data.SlotOptions = slot.Module.hangarShipUID;
                }
                savedShip.ModuleSlots[i] = data;
            }
            string path = Dir.ApplicationData;
            CombatState defaultstate = this.ActiveHull.CombatState;
            savedShip.CombatState = this.CombatState;
            savedShip.Name = string.Format("{0:yyyy-MM-dd}__{1}", DateTime.Now, ActiveHull.Name);
            XmlSerializer Serializer = new XmlSerializer(typeof(ShipData));
            TextWriter WriteFileStream =
                new StreamWriter(string.Concat(path, "/StarDrive/WIP/", savedShip.Name, ".xml"));
            Serializer.Serialize(WriteFileStream, savedShip);
            WriteFileStream.Close();
            savedShip.CombatState = defaultstate;
            this.ShipSaved = true;
        }

        private void SaveWIPThenChangeHull(object sender, EventArgs e)
        {
            this.SaveWIP(sender, e);
            this.ChangeHull(this.Changeto);
        }

        private void SaveWIPThenExitToFleets(object sender, EventArgs e) //Unused
        {
            this.SaveWIP(sender, e);
            base.ScreenManager.AddScreen(new FleetDesignScreen(this, EmpireUI));
            this.ReallyExit();
        }

        private void SaveWIPThenExitToShipsList(object sender, EventArgs e) //Unused
        {
            this.SaveWIP(sender, e);
            base.ScreenManager.AddScreen(new ShipListScreen(this, EmpireUI));
            this.ReallyExit();
        }

        private void SaveWIPThenLaunchScreen(object sender, EventArgs e)
        {
            this.SaveWIP(sender, e);
            string str = this.ScreenToLaunch;
            string str1 = str;
            if (str != null)
            {
                if (str1 == "Research")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    base.ScreenManager.AddScreen(new ResearchScreenNew(this, EmpireUI));
                }
                else if (str1 == "Budget")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    base.ScreenManager.AddScreen(new BudgetScreen(Empire.Universe));
                }
            }
            string str2 = this.ScreenToLaunch;
            string str3 = str2;
            if (str2 != null)
            {
                if (str3 == "Main Menu")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    ScreenManager.AddScreen(new GameplayMMScreen(Empire.Universe));
                }
                else if (str3 == "Shipyard")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else if (str3 == "Empire")
                {
                    ScreenManager.AddScreen(new EmpireScreen(Empire.Universe, EmpireUI));
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else if (str3 == "Diplomacy")
                {
                    ScreenManager.AddScreen(new MainDiplomacyScreen(Empire.Universe));
                    GameAudio.PlaySfxAsync("echo_affirm");
                }
                else if (str3 == "?")
                {
                    GameAudio.PlaySfxAsync("sd_ui_tactical_pause");
                    InGameWiki wiki = new InGameWiki(this, new Rectangle(0, 0, 750, 600))
                    {
                        TitleText = "StarDrive Help",
                        MiddleText =
                            "This help menu contains information on all of the gameplay systems contained in StarDrive. You can also watch one of several tutorial videos for a developer-guided introduction to StarDrive."
                    };
                }
            }
            this.ReallyExit();
        }

        private void SetupSlots()
        {
            this.Slots.Clear();
            foreach (ModuleSlotData slot in this.ActiveHull.ModuleSlots)
            {
                PrimitiveQuad pq = new PrimitiveQuad(slot.Position.X + this.offset.X - 8f,
                    slot.Position.Y + this.offset.Y - 8f, 16f, 16f);
                SlotStruct ss = new SlotStruct()
                {
                    PQ = pq,
                    Restrictions = slot.Restrictions,
                    Facing = slot.Facing,
                    ModuleUID = slot.InstalledModuleUID,
                    SlotReference = slot,
                    SlotOptions = slot.SlotOptions
                };
                this.Slots.Add(ss);
            }
            foreach (SlotStruct slot in this.Slots)
            {
                slot.SetValidity();
                if (slot.ModuleUID == null)
                {
                    continue;
                }
                this.ActiveModule = ShipModule.CreateNoParent(slot.ModuleUID);
                this.ChangeModuleState(slot.State);
                this.InstallModuleFromLoad(slot);
                if (slot.Module == null || slot.Module.ModuleType != ShipModuleType.Hangar)
                {
                    continue;
                }
                slot.Module.hangarShipUID = slot.SlotOptions;
            }
            this.ActiveModule = null;
            this.ActiveModState = ShipDesignScreen.ActiveModuleState.Normal;
        }

        private float GetHullDamageBonus()
        {
            if (GlobalStats.ActiveModInfo == null || !GlobalStats.ActiveModInfo.useHullBonuses)
                return 1f;
            HullBonus bonus;
            if (ResourceManager.HullBonuses.TryGetValue(this.ActiveHull.Hull, out bonus))
            {
                return 1f + bonus.DamageBonus;
            }
            else
                return 1f;
        }

        private float GetHullFireRateBonus()
        {
            if (GlobalStats.ActiveModInfo == null || !GlobalStats.ActiveModInfo.useHullBonuses)
                return 1f;
            HullBonus bonus;
            if (ResourceManager.HullBonuses.TryGetValue(this.ActiveHull.Hull, out bonus))
            {
                return 1f - bonus.FireRateBonus;
            }
            else
                return 1f;
        }
    }
}