using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SgMotion;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;

namespace Ship_Game
{
	public sealed class ShipDesignScreen : GameScreen, IDisposable
	{
		private Matrix worldMatrix = Matrix.Identity;

		private Matrix view;

		private Matrix projection;

		public Camera2d camera;

		public List<ToggleButton> CombatStatusButtons = new List<ToggleButton>();

		public bool Debug;

		public ShipData ActiveHull;

		public EmpireUIOverlay EmpireUI;

		public static UniverseScreen screen;

		private Menu1 ModuleSelectionMenu;

		private Model ActiveModel;

		private SceneObject shipSO;

		private Vector3 cameraPosition = new Vector3(0f, 0f, 1300f);

		public List<SlotStruct> Slots = new List<SlotStruct>();

		private Vector2 offset;

		private Ship_Game.Gameplay.CombatState CombatState = Ship_Game.Gameplay.CombatState.AttackRuns;

		private bool ShipSaved = true;

		private List<ShipData> AvailableHulls = new List<ShipData>();

		private List<UIButton> Buttons = new List<UIButton>();

		//private UIButton HullLeft;

		//private UIButton HullRight;

		private UIButton ToggleOverlayButton;

		private UIButton SaveButton;

		private UIButton LoadButton;

		private Submenu modSel;

		private Submenu statsSub;

		private Menu1 ShipStats;

		private Menu1 activeModWindow;

		private Submenu activeModSubMenu;

		private ScrollList weaponSL;

		private bool Reset = true;

		private Submenu ChooseFighterSub;

		private ScrollList ChooseFighterSL;

		private bool LowRes;

		private float LowestX;

		private float HighestX;

		private GenericButton ArcsButton;

		private CloseButton close;

		private float OriginalZ;

		private Rectangle choosefighterrect;

		private Rectangle SearchBar;

		private Rectangle bottom_sep;

		private ScrollList hullSL;

		private Rectangle HullSelectionRect;

		private Submenu hullSelectionSub;

		private Rectangle BlackBar;

		private Rectangle SideBar;

		private Vector2 SelectedCatTextPos;

		private SkinnableButton wpn;

		private SkinnableButton pwr;

		private SkinnableButton def;

		private SkinnableButton spc;

		private Rectangle ModuleSelectionArea = new Rectangle();

		private List<ShipDesignScreen.ModuleCatButton> ModuleCatButtons = new List<ShipDesignScreen.ModuleCatButton>();

		private List<ModuleButton> ModuleButtons = new List<ModuleButton>();

		private Rectangle upArrow;

		private Rectangle downArrow;

		private MouseState mouseStateCurrent;

		private MouseState mouseStatePrevious;

		private ShipModule HighlightedModule;

		private Vector2 cameraVelocity = Vector2.Zero;

		private Vector2 StartDragPos = new Vector2();

		private ShipData changeto;

		private string screenToLaunch;

		private bool ShowAllArcs;

		private ShipModule HoveredModule;

		private float TransitionZoom = 1f;

		private ShipDesignScreen.SlotModOperation operation;

		//private ShipDesignScreen.Colors sColor;

		private int HullIndex;

		private ShipModule ActiveModule;

		private ShipDesignScreen.ActiveModuleState ActiveModState;

		private Selector selector;

		public bool ToggleOverlay = true;

		private Vector2 starfieldPos = Vector2.Zero;

		private int scrollPosition;

        private DropOptions CategoryList;

        private Rectangle dropdownRect;

        private Vector2 classifCursor;

		public Stack<DesignAction> DesignStack = new Stack<DesignAction>();

        private Vector2 COBoxCursor;

        private Checkbox CarrierOnlyBox;

        private bool fml = false;

        private bool fmlevenmore = false;

        public bool CarrierOnly;

        private ShipData.Category LoadCategory;

        public string HangarShipUIDLast = "Undefined";

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

		public ShipDesignScreen(EmpireUIOverlay EmpireUI)
		{
			this.EmpireUI = EmpireUI;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            
		}

		public void ChangeHull(ShipData hull)
		{
			this.Reset = true;
            this.DesignStack.Clear();
			lock (GlobalStats.ObjectManagerLocker)
			{
				if (this.shipSO != null)
				{
					base.ScreenManager.inter.ObjectManager.Remove(this.shipSO);
				}
			}
			this.ActiveHull = new ShipData()
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
                CarrierShip = hull.CarrierShip,
				ModuleSlotList = new List<ModuleSlotData>(),
			};
            this.CarrierOnly = hull.CarrierShip;
            this.LoadCategory = hull.ShipCategory;
            this.fml = true;
            this.fmlevenmore = true;
			foreach (ModuleSlotData slot in hull.ModuleSlotList)
			{
				ModuleSlotData data = new ModuleSlotData()
				{
					Position = slot.Position,
					Restrictions = slot.Restrictions,
					facing = slot.facing,
					InstalledModuleUID = slot.InstalledModuleUID
				};
				this.ActiveHull.ModuleSlotList.Add(slot);
			}
			this.CombatState = hull.CombatState;
			if (!hull.Animated)
			{
				this.ActiveModel = Ship_Game.ResourceManager.GetModel(this.ActiveHull.ModelPath);
				ModelMesh mesh = this.ActiveModel.Meshes[0];
				this.shipSO = new SceneObject(mesh)
				{
					ObjectType = ObjectType.Dynamic,
					World = this.worldMatrix
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
					World = this.worldMatrix
				};
				lock (GlobalStats.ObjectManagerLocker)
				{
					base.ScreenManager.inter.ObjectManager.Submit(this.shipSO);
				}
			}
			foreach (ToggleButton button in this.CombatStatusButtons)
			{
				string action = button.Action;
				string str = action;
				if (action == null)
				{
					continue;
				}
				if (str == "attack")
				{
					if (this.CombatState != Ship_Game.Gameplay.CombatState.AttackRuns)
					{
						button.Active = false;
					}
					else
					{
						button.Active = true;
					}
				}
				else if (str == "arty")
				{
					if (this.CombatState != Ship_Game.Gameplay.CombatState.Artillery)
					{
						button.Active = false;
					}
					else
					{
						button.Active = true;
					}
				}
				else if (str == "hold")
				{
					if (this.CombatState != Ship_Game.Gameplay.CombatState.HoldPosition)
					{
						button.Active = false;
					}
					else
					{
						button.Active = true;
					}
				}
				else if (str == "orbit_left")
				{
					if (this.CombatState != Ship_Game.Gameplay.CombatState.OrbitLeft)
					{
						button.Active = false;
					}
					else
					{
						button.Active = true;
					}
				}
                else if (str == "broadside_left")
                {
                    if (this.CombatState != Ship_Game.Gameplay.CombatState.BroadsideLeft)
                    {
                        button.Active = false;
                    }
                    else
                    {
                        button.Active = true;
                    }
                }
				else if (str != "orbit_right")
				{
					if (str == "evade")
					{
						if (this.CombatState != Ship_Game.Gameplay.CombatState.Evade)
						{
							button.Active = false;
						}
						else
						{
							button.Active = true;
						}
					}
				}
                else if (str == "broadside_right")
                {
                    if (this.CombatState != Ship_Game.Gameplay.CombatState.BroadsideRight)
                    {
                        button.Active = false;
                    }
                    else
                    {
                        button.Active = true;
                    }
                }
				else if (this.CombatState != Ship_Game.Gameplay.CombatState.OrbitRight)
				{
					button.Active = false;
				}
				else
				{
					button.Active = true;
				}
			}
			this.SetupSlots();
		}

		private void ChangeModuleState(ShipDesignScreen.ActiveModuleState state)
		{
			if (this.ActiveModule == null)
			{
				return;
			}
			byte x = Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].XSIZE;
			byte y = Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].YSIZE;
			switch (state)
			{
				case ShipDesignScreen.ActiveModuleState.Normal:
				{
					this.ActiveModule.XSIZE = Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].XSIZE;
					this.ActiveModule.YSIZE = Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].YSIZE;
					this.ActiveModState = ShipDesignScreen.ActiveModuleState.Normal;
					return;
				}
				case ShipDesignScreen.ActiveModuleState.Left:
				{
					this.ActiveModule.XSIZE = y;
					this.ActiveModule.YSIZE = x;
					this.ActiveModState = ShipDesignScreen.ActiveModuleState.Left;
					this.ActiveModule.facing = 270f;
					return;
				}
				case ShipDesignScreen.ActiveModuleState.Right:
				{
					this.ActiveModule.XSIZE = y;
					this.ActiveModule.YSIZE = x;
					this.ActiveModState = ShipDesignScreen.ActiveModuleState.Right;
					this.ActiveModule.facing = 90f;
					return;
				}
				case ShipDesignScreen.ActiveModuleState.Rear:
				{
					this.ActiveModule.XSIZE = Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].XSIZE;
					this.ActiveModule.YSIZE = Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].YSIZE;
					this.ActiveModState = ShipDesignScreen.ActiveModuleState.Rear;
					this.ActiveModule.facing = 180f;
					return;
				}
				default:
				{
					return;
				}
			}
		}

		private void CheckAndPowerConduit(SlotStruct slot)
		{
			slot.module.Powered = true;
			slot.CheckedConduits = true;
			foreach (SlotStruct ss in this.Slots)
			{
				if (ss == slot || Math.Abs(slot.pq.X - ss.pq.X) / 16 + Math.Abs(slot.pq.Y - ss.pq.Y) / 16 != 1 || ss.module == null || ss.module.ModuleType != ShipModuleType.PowerConduit || ss.CheckedConduits)
				{
					continue;
				}
				this.CheckAndPowerConduit(ss);
			}
		}

		private bool CheckDesign()
		{
			bool EmptySlots = true;
			bool hasBridge = false;
			foreach (SlotStruct slot in this.Slots)
			{
				if (!slot.isDummy && slot.ModuleUID == null)
				{
					EmptySlots = false;
				}
				if (slot.ModuleUID == null || !slot.module.IsCommandModule)
				{
					continue;
				}
				hasBridge = true;
			}
			if (!hasBridge && this.ActiveHull.Role != "platform" && this.ActiveHull.Role != "station" || !EmptySlots)
			{
				return false;
			}
			return true;
		}

		private void ClearDestinationSlots(SlotStruct slot)
		{
			for (int y = 0; y < this.ActiveModule.YSIZE; y++)
			{
				for (int x = 0; x < this.ActiveModule.XSIZE; x++)
				{
                    //added by gremlin changed to not like the other modules clear methods are.
					if (!(x == 0 & y == 0))
					{
						foreach (SlotStruct dummyslot in this.Slots)
						{
							if (dummyslot.pq.Y != slot.pq.Y + 16 * y || dummyslot.pq.X != slot.pq.X + 16 * x)
							{
								continue;
							}
							if (dummyslot.module != null)
							{
								SlotStruct copy = new SlotStruct()
								{
									pq = dummyslot.pq,
									Restrictions = dummyslot.Restrictions,
									facing = dummyslot.facing,
									ModuleUID = dummyslot.ModuleUID,
									module = dummyslot.module,
									state = dummyslot.state,
									slotReference = dummyslot.slotReference
								};
								if (this.DesignStack.Count > 0)
								{
									this.DesignStack.Peek().AlteredSlots.Add(copy);
								}
								this.ClearParentSlot(dummyslot);
							}
							if (dummyslot.isDummy && dummyslot.parent != null && dummyslot.parent.module != null)
							{
								this.ClearParentSlot(dummyslot.parent);
							}
							dummyslot.ModuleUID = null;
							dummyslot.isDummy = false;
							dummyslot.tex = null;
							dummyslot.module = null;
							dummyslot.state = ShipDesignScreen.ActiveModuleState.Normal;
							dummyslot.parent = slot;
						}
					}
				}
			}
		}

		private void ClearDestinationSlotsNoStack(SlotStruct slot)
		{
			for (int y = 0; y < this.ActiveModule.YSIZE; y++)
			{
				for (int x = 0; x < this.ActiveModule.XSIZE; x++)
				{
					//added by gremlin Changed to not like the other methods are.
                    if (!(x == 0 & y == 0))
					{
						foreach (SlotStruct dummyslot in this.Slots)
						{
							if (dummyslot.pq.Y != slot.pq.Y + 16 * y || dummyslot.pq.X != slot.pq.X + 16 * x)
							{
								continue;
							}
							if (dummyslot.module != null)
							{
								this.ClearParentSlot(dummyslot);
							}
							if (dummyslot.isDummy && dummyslot.parent != null && dummyslot.parent.module != null)
							{
								this.ClearParentSlotNoStack(dummyslot.parent);
							}
							dummyslot.ModuleUID = null;
							dummyslot.isDummy = false;
							dummyslot.tex = null;
							dummyslot.module = null;
							dummyslot.parent = slot;
							dummyslot.state = ShipDesignScreen.ActiveModuleState.Normal;
						}
					}
				}
			}
		}

        private void ClearParentSlot(SlotStruct parentSlotStruct)
        {   //actually supposed to clear ALL slots of a module, not just the parent
            SlotStruct slotStruct1 = new SlotStruct();
            slotStruct1.pq = parentSlotStruct.pq;
            slotStruct1.Restrictions = parentSlotStruct.Restrictions;
            slotStruct1.facing = parentSlotStruct.facing;
            slotStruct1.ModuleUID = parentSlotStruct.ModuleUID;
            slotStruct1.module = parentSlotStruct.module;
            slotStruct1.state = parentSlotStruct.state;
            slotStruct1.slotReference = parentSlotStruct.slotReference;
            if (this.DesignStack.Count > 0)
                this.DesignStack.Peek().AlteredSlots.Add(slotStruct1);
            //clear up child slots
            for (int index1 = 0; index1 < (int)parentSlotStruct.module.YSIZE; ++index1)
            {
                for (int index2 = 0; index2 < (int)parentSlotStruct.module.XSIZE; ++index2)
                {
                    if (!(index2 == 0 & index1 == 0))
                    {
                        foreach (SlotStruct slotStruct2 in this.Slots)
                        {
                            if (slotStruct2.pq.Y == parentSlotStruct.pq.Y + 16 * index1 && slotStruct2.pq.X == parentSlotStruct.pq.X + 16 * index2)
                            {
                                slotStruct2.ModuleUID = (string)null;
                                slotStruct2.isDummy = false;
                                slotStruct2.tex = (Texture2D)null;
                                slotStruct2.module = (ShipModule)null;
                                slotStruct2.parent = (SlotStruct)null;
                                slotStruct2.state = ShipDesignScreen.ActiveModuleState.Normal;
                            }
                        }
                    }
                }
            }
            //clear parent slot
            parentSlotStruct.ModuleUID = (string)null;
            parentSlotStruct.isDummy = false;
            parentSlotStruct.tex = (Texture2D)null;
            parentSlotStruct.module = (ShipModule)null;
            parentSlotStruct.parent = null;
            parentSlotStruct.state = ShipDesignScreen.ActiveModuleState.Normal;
        }

        private void ClearParentSlotNoStack(SlotStruct parent)
        {
            for (int index1 = 0; index1 < (int)parent.module.YSIZE; ++index1)
            {
                for (int index2 = 0; index2 < (int)parent.module.XSIZE; ++index2)
                {
                    if (!(index2 == 0 & index1 == 0))
                    {
                        foreach (SlotStruct slotStruct in this.Slots)
                        {
                            if (slotStruct.pq.Y == parent.pq.Y + 16 * index1 && slotStruct.pq.X == parent.pq.X + 16 * index2)
                            {
                                slotStruct.ModuleUID = (string)null;
                                slotStruct.isDummy = false;
                                slotStruct.tex = (Texture2D)null;
                                slotStruct.module = (ShipModule)null;
                                slotStruct.parent = (SlotStruct)null;
                                slotStruct.state = ShipDesignScreen.ActiveModuleState.Normal;
                            }
                        }
                    }
                }
            }
            parent.ModuleUID = (string)null;
            parent.isDummy = false;
            parent.tex = (Texture2D)null;
            parent.module = (ShipModule)null;
            parent.state = ShipDesignScreen.ActiveModuleState.Normal;
        }

        private void ClearSlot(SlotStruct slot)
        {   //this is the clearslot function actually used atm
            //only called from installmodule atm, not from manual module removal
            if (slot.isDummy)
            {
                System.Diagnostics.Debug.Assert(slot.module == null);
                if (slot.parent.module != null)
                {
                    this.ClearParentSlot(slot.parent);
                }
            }
            else if (slot.module != null)
            {
                this.ClearParentSlot(slot);
            }
            else
            {   //this requires not being a child slot and not containing a module
                //only empty parent slots can trigger this
                //why would we want to clear an empty slot?
                //might be used on initial load instead of a proper slot constructor
                slot.ModuleUID = (string)null;
                slot.isDummy = false;
                slot.tex = (Texture2D)null;
                slot.parent = (SlotStruct)null;
                slot.module = (ShipModule)null;
                slot.state = ShipDesignScreen.ActiveModuleState.Normal;
            }
        }

        private void ClearSlotNoStack(SlotStruct slot)
        {   //this function might never be called, see if anyone triggers this
            //System.Diagnostics.Debug.Assert(false);  Appears to part of teh ctrl-Z functionality
            if (slot.isDummy)
            {
                if (slot.parent.module == null)
                    return;
                this.ClearParentSlotNoStack(slot.parent);
            }
            else if (slot.module != null)
            {
                this.ClearParentSlotNoStack(slot);
            }
            else
            {
                slot.ModuleUID = (string)null;
                slot.isDummy = false;
                slot.tex = (Texture2D)null;
                slot.parent = (SlotStruct)null;
                slot.module = (ShipModule)null;
                slot.state = ShipDesignScreen.ActiveModuleState.Normal;
            }
        }

		public void CreateShipModuleSelectionWindow()
		{
			this.upArrow = new Rectangle(this.ModuleSelectionArea.X + this.ModuleSelectionArea.Width - 22, this.ModuleSelectionArea.Y, 22, 30);
			this.downArrow = new Rectangle(this.ModuleSelectionArea.X + this.ModuleSelectionArea.Width - 22, this.ModuleSelectionArea.Y + this.ModuleSelectionArea.Height - 32, 20, 30);
			List<string> Categories = new List<string>();
			Dictionary<string, List<ShipModule>> ModuleDict = new Dictionary<string, List<ShipModule>>();
			foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
			{
				if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
				{
					continue;
				}
				string cat = module.Value.ModuleType.ToString();
				if (!Categories.Contains(cat))
				{
					Categories.Add(cat);
				}
				if (ModuleDict.ContainsKey(cat))
				{
					ModuleDict[cat].Add(module.Value);
				}
				else
				{
					ModuleDict.Add(cat, new List<ShipModule>());
					ModuleDict[cat].Add(module.Value);
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
					mRect = new Rectangle(this.ModuleSelectionArea.X + 10, this.ModuleSelectionArea.Y + 10 + i * 25, 45, 25),
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
					if (x <= 1)
					{
						continue;
					}
					y++;
					x = 0;
				}
				else
				{
					if (x <= 2)
					{
						continue;
					}
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
					World = this.worldMatrix
				};
				base.ScreenManager.inter.ObjectManager.Submit(this.shipSO);
				this.SetupSlots();
			}
		}

		private void DebugAlterSlot(Vector2 SlotPos, ShipDesignScreen.SlotModOperation op)
		{
			ModuleSlotData toRemove;
			switch (op)
			{
				case ShipDesignScreen.SlotModOperation.Delete:
				{
					toRemove = null;
					foreach (ModuleSlotData slotdata in this.ActiveHull.ModuleSlotList)
					{
						if (slotdata.Position != SlotPos)
						{
							continue;
						}
						toRemove = slotdata;
						break;
					}
					if (toRemove == null)
					{
						return;
					}
					this.ActiveHull.ModuleSlotList.Remove(toRemove);
					this.ChangeHull(this.ActiveHull);
					return;
				}
				case ShipDesignScreen.SlotModOperation.I:
				{
					toRemove = null;
					foreach (ModuleSlotData slotdata in this.ActiveHull.ModuleSlotList)
					{
						if (slotdata.Position != SlotPos)
						{
							continue;
						}
						toRemove = slotdata;
						break;
					}
					if (toRemove == null)
					{
						return;
					}
					toRemove.Restrictions = Restrictions.I;
					this.ChangeHull(this.ActiveHull);
					return;
				}
				case ShipDesignScreen.SlotModOperation.IO:
				{
					toRemove = null;
					foreach (ModuleSlotData slotdata in this.ActiveHull.ModuleSlotList)
					{
						if (slotdata.Position != SlotPos)
						{
							continue;
						}
						toRemove = slotdata;
						break;
					}
					if (toRemove == null)
					{
						return;
					}
					toRemove.Restrictions = Restrictions.IO;
					this.ChangeHull(this.ActiveHull);
					return;
				}
				case ShipDesignScreen.SlotModOperation.O:
				{
					toRemove = null;
					foreach (ModuleSlotData slotdata in this.ActiveHull.ModuleSlotList)
					{
						if (slotdata.Position != SlotPos)
						{
							continue;
						}
						toRemove = slotdata;
						break;
					}
					if (toRemove == null)
					{
						return;
					}
					toRemove.Restrictions = Restrictions.O;
					this.ChangeHull(this.ActiveHull);
					return;
				}
				case ShipDesignScreen.SlotModOperation.Add:
				{
					return;
				}
				case ShipDesignScreen.SlotModOperation.E:
				{
					toRemove = null;
					foreach (ModuleSlotData slotdata in this.ActiveHull.ModuleSlotList)
					{
						if (slotdata.Position != SlotPos)
						{
							continue;
						}
						toRemove = slotdata;
						break;
					}
					if (toRemove == null)
					{
						return;
					}
					toRemove.Restrictions = Restrictions.E;
					this.ChangeHull(this.ActiveHull);
					return;
				}
				default:
				{
					return;
				}
			}
		}

		public void Dispose()
		{

			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

        ~ShipDesignScreen() { Dispose(false); }

        protected void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.hullSL != null)
                        this.hullSL.Dispose();
                    if (this.weaponSL != null)
                        this.weaponSL.Dispose();
                    if (this.ChooseFighterSL != null)
                        this.ChooseFighterSL.Dispose();

                }
                this.hullSL = null;
                this.weaponSL = null;
                this.ChooseFighterSL = null;
                this.disposed = true;
            }
        }

		private void DoExit(object sender, EventArgs e)
		{
			this.ReallyExit();
		}

		private void DoExitToFleetsList(object sender, EventArgs e)
		{
			base.ScreenManager.AddScreen(new FleetDesignScreen(this.EmpireUI));
			this.ReallyExit();
		}

		private void DoExitToShipList(object sender, EventArgs e)
		{
			this.ReallyExit();
		}

		private void DoExitToShipsList(object sender, EventArgs e)
		{
			base.ScreenManager.AddScreen(new ShipListScreen(base.ScreenManager, this.EmpireUI));
			this.ReallyExit();
		}

		public override void Draw(GameTime gameTime)
		{
			int x;
			int y;
			//int x;    //wtf?
			//int y;
			Color lightGreen;
			Color color;
			lock (GlobalStats.ObjectManagerLocker)
			{
				base.ScreenManager.sceneState.BeginFrameRendering(this.view, this.projection, gameTime, base.ScreenManager.environment, true);
				base.ScreenManager.editor.BeginFrameRendering(base.ScreenManager.sceneState);
				base.ScreenManager.inter.BeginFrameRendering(base.ScreenManager.sceneState);
				ShipDesignScreen.screen.bg.Draw(ShipDesignScreen.screen, ShipDesignScreen.screen.starfield);
				base.ScreenManager.inter.RenderManager.Render();
			}
			base.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, this.camera.get_transformation(base.ScreenManager.GraphicsDevice));
			if (this.ToggleOverlay)
			{
				foreach (SlotStruct slot in this.Slots)
				{
					if (slot.module != null)
					{
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Modules/tile_concreteglass_1x1"], new Rectangle(slot.pq.enclosingRect.X, slot.pq.enclosingRect.Y, 16 * slot.module.XSIZE, 16 * slot.module.YSIZE), Color.Gray);
					}
					else if (!slot.isDummy)
					{
						if (this.ActiveModule != null)
						{
							SpriteBatch spriteBatch = base.ScreenManager.SpriteBatch;
							Texture2D item = Ship_Game.ResourceManager.TextureDict["Modules/tile_concreteglass_1x1"];
							Rectangle rectangle = slot.pq.enclosingRect;
							if (slot.ShowValid)
							{
								color = Color.LightGreen;
							}
							else
							{
								color = (slot.ShowInvalid ? Color.Red : Color.White);
							}
							spriteBatch.Draw(item, rectangle, color);
							if (slot.Powered)
							{
								base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Modules/tile_concreteglass_1x1"], slot.pq.enclosingRect, new Color(255, 255, 0, 150));
							}
						}
						else if (slot.Powered)
						{
							base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Modules/tile_concreteglass_1x1"], slot.pq.enclosingRect, Color.Yellow);
						}
						else
						{
							SpriteBatch spriteBatch1 = base.ScreenManager.SpriteBatch;
							Texture2D texture2D = Ship_Game.ResourceManager.TextureDict["Modules/tile_concreteglass_1x1"];
							Rectangle rectangle1 = slot.pq.enclosingRect;
							if (slot.ShowValid)
							{
								lightGreen = Color.LightGreen;
							}
							else
							{
								lightGreen = (slot.ShowInvalid ? Color.Red : Color.White);
							}
							spriteBatch1.Draw(texture2D, rectangle1, lightGreen);
						}
					}
					if (slot.module != null || slot.isDummy)
					{
						continue;
					}
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(" ", slot.Restrictions), new Vector2((float)slot.pq.enclosingRect.X, (float)slot.pq.enclosingRect.Y), Color.Navy, 0f, Vector2.Zero, 0.4f, SpriteEffects.None, 1f);
				}
				foreach (SlotStruct slot in this.Slots)
				{
					if (slot.ModuleUID == null || slot.tex == null)
					{
						continue;
					}
					if (slot.state != ShipDesignScreen.ActiveModuleState.Normal)
					{
						Rectangle r = new Rectangle(slot.pq.enclosingRect.X, slot.pq.enclosingRect.Y, 16 * slot.module.XSIZE, 16 * slot.module.YSIZE);
						switch (slot.state)
						{
							case ShipDesignScreen.ActiveModuleState.Left:
							{
								x = slot.module.YSIZE * 16;
								y = slot.module.XSIZE * 16;
								r.Width = x;
								r.Height = y;
								r.Y = r.Y + x;
								Rectangle? nullable = null;
								base.ScreenManager.SpriteBatch.Draw(slot.tex, r, nullable, Color.White, -1.57079637f, Vector2.Zero, SpriteEffects.None, 1f);
								break;
							}
							case ShipDesignScreen.ActiveModuleState.Right:
							{
								x = slot.module.YSIZE * 16;
								y = slot.module.XSIZE * 16;
								r.Width = x;
								r.Height = y;
								r.X = r.X + y;
								Rectangle? nullable1 = null;
								base.ScreenManager.SpriteBatch.Draw(slot.tex, r, nullable1, Color.White, 1.57079637f, Vector2.Zero, SpriteEffects.None, 1f);
								break;
							}
							case ShipDesignScreen.ActiveModuleState.Rear:
							{
								Rectangle? nullable2 = null;
								base.ScreenManager.SpriteBatch.Draw(slot.tex, r, nullable2, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 1f);
								break;
							}
						}
					}
					else if (slot.module.XSIZE <= 1 && slot.module.YSIZE <= 1)
					{
						if (slot.module.ModuleType != ShipModuleType.PowerConduit)
						{
							base.ScreenManager.SpriteBatch.Draw(slot.tex, slot.pq.enclosingRect, Color.White);
						}
						else
						{
							string graphic = this.GetConduitGraphic(slot);
							base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[string.Concat("Conduits/", graphic)], slot.pq.enclosingRect, Color.White);
							if (slot.module.Powered)
							{
								graphic = string.Concat(graphic, "_power");
								base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[string.Concat("Conduits/", graphic)], slot.pq.enclosingRect, Color.White);
							}
						}
					}
					else if (slot.slotReference.Position.X <= 256f)
					{
						base.ScreenManager.SpriteBatch.Draw(slot.tex, new Rectangle(slot.pq.enclosingRect.X, slot.pq.enclosingRect.Y, 16 * slot.module.XSIZE, 16 * slot.module.YSIZE), Color.White);
					}
					else
					{
						Rectangle? nullable3 = null;
						base.ScreenManager.SpriteBatch.Draw(slot.tex, new Rectangle(slot.pq.enclosingRect.X, slot.pq.enclosingRect.Y, 16 * slot.module.XSIZE, 16 * slot.module.YSIZE), nullable3, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 1f);
					}
					if (slot.module != this.HoveredModule)
					{
						continue;
					}
					Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, new Rectangle(slot.pq.enclosingRect.X, slot.pq.enclosingRect.Y, 16 * slot.module.XSIZE, 16 * slot.module.YSIZE), Color.White, 2f);
				}
				foreach (SlotStruct slot in this.Slots)
				{
					if (slot.ModuleUID == null || slot.tex == null || slot.module != this.HighlightedModule && !this.ShowAllArcs)
					{
						continue;
					}
					if (slot.module.shield_power_max > 0f)
					{
						Vector2 Center = new Vector2((float)(slot.pq.enclosingRect.X + 16 * slot.module.XSIZE / 2), (float)(slot.pq.enclosingRect.Y + 16 * slot.module.YSIZE / 2));
						Primitives2D.DrawCircle(base.ScreenManager.SpriteBatch, Center, slot.module.shield_radius, 50, Color.LightGreen);
					}
                    //Original by The Doctor, modified by McShooterz
                    if (slot.module.FieldOfFire == 90f && Ship_Game.ResourceManager.TextureDict.ContainsKey("Arcs/Arc90"))
                    {
                        Vector2 Center = new Vector2((float)(slot.pq.enclosingRect.X + 16 * slot.module.XSIZE / 2), (float)(slot.pq.enclosingRect.Y + 16 * slot.module.YSIZE / 2));
                        Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.module.InstalledWeapon.Tag_Cannon && !slot.module.InstalledWeapon.Tag_Energy)
                        {
                            Color drawcolor = new Color(255, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable4 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc90"], toDraw, nullable4, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.module.InstalledWeapon.Tag_Railgun || slot.module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc90"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.module.InstalledWeapon.Tag_Cannon)
                        {
                            Color drawcolor = new Color(0, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc90"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }                       
                        else if (!slot.module.InstalledWeapon.isBeam)
                        {
                            Color drawcolor = new Color(255, 0, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable6 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc90"], toDraw, nullable6, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Color drawcolor = new Color(0, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable7 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc90"], toDraw, nullable7, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                    }
                    //Original by The Doctor, modified by McShooterz
                    else if (slot.module.FieldOfFire == 15f && Ship_Game.ResourceManager.TextureDict.ContainsKey("Arcs/Arc15"))
                    {
                        Vector2 Center = new Vector2((float)(slot.pq.enclosingRect.X + 16 * slot.module.XSIZE / 2), (float)(slot.pq.enclosingRect.Y + 16 * slot.module.YSIZE / 2));
                        Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.module.InstalledWeapon.Tag_Cannon && !slot.module.InstalledWeapon.Tag_Energy)
                        {
                            Color drawcolor = new Color(255, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable4 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc15"], toDraw, nullable4, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.module.InstalledWeapon.Tag_Railgun || slot.module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc15"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.module.InstalledWeapon.Tag_Cannon)
                        {
                            Color drawcolor = new Color(0, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc15"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (!slot.module.InstalledWeapon.isBeam)
                        {
                            Color drawcolor = new Color(255, 0, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable6 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc15"], toDraw, nullable6, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Color drawcolor = new Color(0, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable7 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc15"], toDraw, nullable7, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                    }
                    //Original by The Doctor, modified by McShooterz
                    else if (slot.module.FieldOfFire == 20f && Ship_Game.ResourceManager.TextureDict.ContainsKey("Arcs/Arc20"))
                    {
                        Vector2 Center = new Vector2((float)(slot.pq.enclosingRect.X + 16 * slot.module.XSIZE / 2), (float)(slot.pq.enclosingRect.Y + 16 * slot.module.YSIZE / 2));
                        Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.module.InstalledWeapon.Tag_Cannon && !slot.module.InstalledWeapon.Tag_Energy)
                        {
                            Color drawcolor = new Color(255, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable4 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc20"], toDraw, nullable4, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.module.InstalledWeapon.Tag_Railgun || slot.module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc20"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.module.InstalledWeapon.Tag_Cannon)
                        {
                            Color drawcolor = new Color(0, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc20"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (!slot.module.InstalledWeapon.isBeam)
                        {
                            Color drawcolor = new Color(255, 0, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable6 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc20"], toDraw, nullable6, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Color drawcolor = new Color(0, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable7 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc20"], toDraw, nullable7, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                    }
                    //Original by The Doctor, modified by McShooterz
                    else if (slot.module.FieldOfFire == 45f && Ship_Game.ResourceManager.TextureDict.ContainsKey("Arcs/Arc45"))
                    {
                        Vector2 Center = new Vector2((float)(slot.pq.enclosingRect.X + 16 * slot.module.XSIZE / 2), (float)(slot.pq.enclosingRect.Y + 16 * slot.module.YSIZE / 2));
                        Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.module.InstalledWeapon.Tag_Cannon && !slot.module.InstalledWeapon.Tag_Energy)
                        {
                            Color drawcolor = new Color(255, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable4 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc45"], toDraw, nullable4, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.module.InstalledWeapon.Tag_Railgun || slot.module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc45"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.module.InstalledWeapon.Tag_Cannon)
                        {
                            Color drawcolor = new Color(0, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc45"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (!slot.module.InstalledWeapon.isBeam)
                        {
                            Color drawcolor = new Color(255, 0, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable6 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc45"], toDraw, nullable6, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Color drawcolor = new Color(0, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable7 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc45"], toDraw, nullable7, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                    }
                    //Original by The Doctor, modified by McShooterz
                    else if (slot.module.FieldOfFire == 120f && Ship_Game.ResourceManager.TextureDict.ContainsKey("Arcs/Arc120"))
                    {
                        Vector2 Center = new Vector2((float)(slot.pq.enclosingRect.X + 16 * slot.module.XSIZE / 2), (float)(slot.pq.enclosingRect.Y + 16 * slot.module.YSIZE / 2));
                        Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.module.InstalledWeapon.Tag_Cannon && !slot.module.InstalledWeapon.Tag_Energy)
                        {
                            Color drawcolor = new Color(255, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable4 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc120"], toDraw, nullable4, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.module.InstalledWeapon.Tag_Railgun || slot.module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc120"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.module.InstalledWeapon.Tag_Cannon)
                        {
                            Color drawcolor = new Color(0, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc120"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (!slot.module.InstalledWeapon.isBeam)
                        {
                            Color drawcolor = new Color(255, 0, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable6 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc120"], toDraw, nullable6, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Color drawcolor = new Color(0, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable7 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc120"], toDraw, nullable7, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                    }
                    //Original by The Doctor, modified by McShooterz
                    else if (slot.module.FieldOfFire == 60f && Ship_Game.ResourceManager.TextureDict.ContainsKey("Arcs/Arc60"))
                    {
                        Vector2 Center = new Vector2((float)(slot.pq.enclosingRect.X + 16 * slot.module.XSIZE / 2), (float)(slot.pq.enclosingRect.Y + 16 * slot.module.YSIZE / 2));
                        Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.module.InstalledWeapon.Tag_Cannon && !slot.module.InstalledWeapon.Tag_Energy)
                        {
                            Color drawcolor = new Color(255, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable4 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc60"], toDraw, nullable4, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.module.InstalledWeapon.Tag_Railgun || slot.module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc60"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.module.InstalledWeapon.Tag_Cannon)
                        {
                            Color drawcolor = new Color(0, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc60"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (!slot.module.InstalledWeapon.isBeam)
                        {
                            Color drawcolor = new Color(255, 0, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable6 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc60"], toDraw, nullable6, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Color drawcolor = new Color(0, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable7 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc60"], toDraw, nullable7, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                    }
                    //Original by The Doctor, modified by McShooterz
                    else if (slot.module.FieldOfFire == 360f && Ship_Game.ResourceManager.TextureDict.ContainsKey("Arcs/Arc360"))
                    {
                        Vector2 Center = new Vector2((float)(slot.pq.enclosingRect.X + 16 * slot.module.XSIZE / 2), (float)(slot.pq.enclosingRect.Y + 16 * slot.module.YSIZE / 2));
                        Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.module.InstalledWeapon.Tag_Cannon && !slot.module.InstalledWeapon.Tag_Energy)
                        {
                            Color drawcolor = new Color(255, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable4 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc360"], toDraw, nullable4, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.module.InstalledWeapon.Tag_Railgun || slot.module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc360"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.module.InstalledWeapon.Tag_Cannon)
                        {
                            Color drawcolor = new Color(0, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc360"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (!slot.module.InstalledWeapon.isBeam)
                        {
                            Color drawcolor = new Color(255, 0, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable6 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc360"], toDraw, nullable6, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Color drawcolor = new Color(0, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable7 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc360"], toDraw, nullable7, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                    }
                    //Original by The Doctor, modified by McShooterz
                    else if (slot.module.FieldOfFire == 180f && Ship_Game.ResourceManager.TextureDict.ContainsKey("Arcs/Arc180"))
					{
						Vector2 Center = new Vector2((float)(slot.pq.enclosingRect.X + 16 * slot.module.XSIZE / 2), (float)(slot.pq.enclosingRect.Y + 16 * slot.module.YSIZE / 2));
						Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.module.InstalledWeapon.Tag_Cannon && !slot.module.InstalledWeapon.Tag_Energy)
						{
							Color drawcolor = new Color(255, 255, 0, 255);
							Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
							Rectangle? nullable4 = null;
							base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc180"], toDraw, nullable4, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
						}
                        else if (slot.module.InstalledWeapon.Tag_Railgun || slot.module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc180"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.module.InstalledWeapon.Tag_Cannon)
						{
                            Color drawcolor = new Color(0, 255, 0, 255);
							Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
							Rectangle? nullable5 = null;
							base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc180"], toDraw, nullable5, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
						}
						else if (!slot.module.InstalledWeapon.isBeam)
						{
							Color drawcolor = new Color(255, 0, 0, 255);
							Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
							Rectangle? nullable6 = null;
							base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc180"], toDraw, nullable6, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
						}
						else
						{
							Color drawcolor = new Color(0, 0, 255, 255);
							Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
							Rectangle? nullable7 = null;
							base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc180"], toDraw, nullable7, drawcolor, (float)MathHelper.ToRadians(slot.module.facing), Origin, SpriteEffects.None, 1f);
						}
					}
                    //Original by The Doctor, modified by McShooterz
                    else
                    {
                        if (slot.module.FieldOfFire == 0f)
						{
							continue;
						}
						float halfArc = slot.module.FieldOfFire / 2f;
						Vector2 Center = new Vector2((float)(slot.pq.enclosingRect.X + 16 * slot.module.XSIZE / 2), (float)(slot.pq.enclosingRect.Y + 16 * slot.module.YSIZE / 2));
						Vector2 leftArc = this.findPointFromAngleAndDistance(Center, slot.module.facing + -halfArc, 300f);
						Vector2 rightArc = this.findPointFromAngleAndDistance(Center, slot.module.facing + halfArc, 300f);
						leftArc = this.findPointFromAngleAndDistance(Center, slot.module.facing + -halfArc, 300f);
						rightArc = this.findPointFromAngleAndDistance(Center, slot.module.facing + halfArc, 300f);
						Color arc = new Color(255, 165, 0, 100);
						Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, Center, leftArc, arc, 3f);
						Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, Center, rightArc, arc, 3f);
                    }
				}
				foreach (SlotStruct ss in this.Slots)
				{
					if (ss.module == null)
					{
						continue;
					}
					Vector2 Center = new Vector2((float)(ss.pq.X + 16 * ss.module.XSIZE / 2), (float)(ss.pq.Y + 16 * ss.module.YSIZE / 2));
					Vector2 lightOrigin = new Vector2(8f, 8f);
					if (ss.module.PowerDraw <= 0f || ss.module.Powered || ss.module.ModuleType == ShipModuleType.PowerConduit)
					{
						continue;
					}
					Rectangle? nullable8 = null;
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["UI/lightningBolt"], Center, nullable8, Color.White, 0f, lightOrigin, 1f, SpriteEffects.None, 1f);
				}
			}
			base.ScreenManager.SpriteBatch.End();
			base.ScreenManager.SpriteBatch.Begin();
			foreach (ModuleButton mb in this.ModuleButtons)
			{
				if (!HelperFunctions.CheckIntersection(this.ModuleSelectionArea, new Vector2((float)(mb.moduleRect.X + 30), (float)(mb.moduleRect.Y + 30))))
				{
					continue;
				}
				if (mb.isHighlighted)
				{
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["UI/blueHighlight"], mb.moduleRect, Color.White);
				}
				Rectangle modRect = new Rectangle(0, 0, Ship_Game.ResourceManager.ShipModulesDict[mb.ModuleUID].XSIZE * 16, Ship_Game.ResourceManager.ShipModulesDict[mb.ModuleUID].YSIZE * 16);
				//{
					modRect.X = mb.moduleRect.X + 64 - modRect.Width / 2;
                    modRect.Y = mb.moduleRect.Y + 64 - modRect.Height / 2;
				//};
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[mb.ModuleUID].IconTexturePath], modRect, Color.White);
				float nWidth = Fonts.Arial12.MeasureString(Localizer.Token(Ship_Game.ResourceManager.ShipModulesDict[mb.ModuleUID].NameIndex)).X;
				Vector2 nameCursor = new Vector2((float)(mb.moduleRect.X + 64) - nWidth / 2f, (float)(mb.moduleRect.Y + 128 - Fonts.Arial12.LineSpacing - 2));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, Localizer.Token(Ship_Game.ResourceManager.ShipModulesDict[mb.ModuleUID].NameIndex), nameCursor, Color.White);
			}
			float single = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(single, (float)state.Y);
			if (this.ActiveModule != null && !HelperFunctions.CheckIntersection(this.activeModSubMenu.Menu, MousePos) && !HelperFunctions.CheckIntersection(this.modSel.Menu, MousePos) && (!HelperFunctions.CheckIntersection(this.choosefighterrect, MousePos) || this.ActiveModule.ModuleType != ShipModuleType.Hangar || this.ActiveModule.IsSupplyBay || this.ActiveModule.IsTroopBay))
			{
				Rectangle r = new Rectangle(this.mouseStateCurrent.X, this.mouseStateCurrent.Y, (int)((float)(16 * this.ActiveModule.XSIZE) * this.camera.Zoom), (int)((float)(16 * this.ActiveModule.YSIZE) * this.camera.Zoom));
				switch (this.ActiveModState)
				{
					case ShipDesignScreen.ActiveModuleState.Normal:
					{
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].IconTexturePath], r, Color.White);
						break;
					}
					case ShipDesignScreen.ActiveModuleState.Left:
					{
						r.Y = r.Y + (int)((float)(16 * Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].XSIZE) * this.camera.Zoom);
						x = r.Height;
						y = r.Width;
						r.Width = x;
						r.Height = y;
						Rectangle? nullable9 = null;
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].IconTexturePath], r, nullable9, Color.White, -1.57079637f, Vector2.Zero, SpriteEffects.None, 1f);
						break;
					}
					case ShipDesignScreen.ActiveModuleState.Right:
					{
						r.X = r.X + (int)((float)(16 * Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].YSIZE) * this.camera.Zoom);
						x = r.Height;
						y = r.Width;
						r.Width = x;
						r.Height = y;
						Rectangle? nullable10 = null;
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].IconTexturePath], r, nullable10, Color.White, 1.57079637f, Vector2.Zero, SpriteEffects.None, 1f);
						break;
					}
					case ShipDesignScreen.ActiveModuleState.Rear:
					{
						Rectangle? nullable11 = null;
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].IconTexturePath], r, nullable11, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 1f);
						break;
					}
				}
				if (this.ActiveModule.shield_power_max > 0f)
				{
					Vector2 center = new Vector2((float)this.mouseStateCurrent.X, (float)this.mouseStateCurrent.Y) + new Vector2((float)(Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].XSIZE * 16 / 2), (float)(Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].YSIZE * 16 / 2));
					Primitives2D.DrawCircle(base.ScreenManager.SpriteBatch, center, this.ActiveModule.shield_radius * this.camera.Zoom, 50, Color.LightGreen);
				}
			}
			this.DrawUI(gameTime);
			if (this.selector != null)
			{
				this.selector.Draw();
			}
			this.ArcsButton.DrawWithShadowCaps(base.ScreenManager);
			if (this.Debug)
			{
				Vector2 Pos = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Arial20Bold.MeasureString("Debug").X, 120f);
				HelperFunctions.DrawDropShadowText(base.ScreenManager, "Debug", Pos, Fonts.Arial20Bold);
				Pos = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Arial20Bold.MeasureString(this.operation.ToString()).X, 140f);
				HelperFunctions.DrawDropShadowText(base.ScreenManager, this.operation.ToString(), Pos, Fonts.Arial20Bold);
			}
			this.close.Draw(base.ScreenManager);
			base.ScreenManager.SpriteBatch.End();
			lock (GlobalStats.ObjectManagerLocker)
			{
				base.ScreenManager.inter.EndFrameRendering();
				base.ScreenManager.editor.EndFrameRendering();
				base.ScreenManager.sceneState.EndFrameRendering();
			}
		}

		private void DrawActiveModuleData()
		{
			float powerDraw;
			this.activeModSubMenu.Draw();
			Rectangle r = this.activeModSubMenu.Menu;
			r.Y = r.Y + 25;
			r.Height = r.Height - 25;
			Selector sel = new Selector(base.ScreenManager, r, new Color(0, 0, 0, 210));
			sel.Draw();
			ShipModule mod = this.ActiveModule;
			if (this.ActiveModule == null && this.HighlightedModule != null)
			{
				mod = this.HighlightedModule;
			}
			else if (this.ActiveModule != null)
			{
				mod = this.ActiveModule;
			}
			if (mod != null)
			{
				mod.HealthMax = Ship_Game.ResourceManager.ShipModulesDict[mod.UID].HealthMax;
			}
			if (this.activeModSubMenu.Tabs[0].Selected && mod != null)
			{
                //Added by McShooterz: Changed how modules names are displayed for allowing longer names
				Vector2 modTitlePos = new Vector2((float)(this.activeModSubMenu.Menu.X + 10), (float)(this.activeModSubMenu.Menu.Y + 35));
                if (Fonts.Arial20Bold.MeasureString(Localizer.Token(Ship_Game.ResourceManager.ShipModulesDict[mod.UID].NameIndex)).X + 16 < this.activeModSubMenu.Menu.Width)
                {
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(Ship_Game.ResourceManager.ShipModulesDict[mod.UID].NameIndex), modTitlePos, Color.White);
                    modTitlePos.Y = modTitlePos.Y + (float)(Fonts.Arial20Bold.LineSpacing + 6);
                }
                else
                {
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, Localizer.Token(Ship_Game.ResourceManager.ShipModulesDict[mod.UID].NameIndex), modTitlePos, Color.White);
                    modTitlePos.Y = modTitlePos.Y + (float)(Fonts.Arial14Bold.LineSpacing + 4);
                }
				string rest = "";
				if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].Restrictions == Restrictions.IO)
				{
					rest = "I or O or IO";
				}
				else if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].Restrictions == Restrictions.I)
				{
					rest = "I or IO only";
				}
				else if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].Restrictions == Restrictions.O)
				{
					rest = "O or IO only";
				}
				else if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].Restrictions == Restrictions.E)
				{
					rest = "E only";
				}
				else if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].Restrictions == Restrictions.IOE)
				{
					rest = "I, O, or E";
				}
                else if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].Restrictions == Restrictions.IE)
                {
                    rest = "I or E only";
                }
                else if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].Restrictions == Restrictions.OE)
                {
                    rest = "O or E only";
                }

                // Concat ship class restrictions
                string shipRest = "";
                bool specialString = false;

				if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDrones && GlobalStats.ActiveModInfo.useDestroyers)
                {
                    if (!mod.FightersOnly && mod.DroneModule && mod.FighterModule && mod.CorvetteModule && mod.FrigateModule && mod.DestroyerModule && mod.CruiserModule && mod.CruiserModule && mod.CarrierModule && mod.CapitalModule && mod.PlatformModule && mod.StationModule && mod.FreighterModule)
                    {
                        shipRest = "All Hulls";
                        specialString = true;
                    }
                    else if (!mod.FightersOnly && !mod.DroneModule && mod.FighterModule && mod.CorvetteModule && mod.FrigateModule && mod.DestroyerModule && mod.CruiserModule && mod.CruiserModule && mod.CarrierModule && mod.CapitalModule && mod.PlatformModule && mod.StationModule && mod.FreighterModule)
                    {
                        shipRest = "All Crewed";
                        specialString = true;
                    }
                    else if (mod.FighterModule && !mod.DroneModule && !mod.CorvetteModule && !mod.FrigateModule && !mod.DestroyerModule && !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule && !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                    {
                        shipRest = "Fighters Only";
                        specialString = true;
                    }
                    else if (mod.DroneModule && !mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule && !mod.DestroyerModule && !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule && !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                    {
                        shipRest = "Drones Only";
                        specialString = true;
                    }
                    else if (mod.FightersOnly && !specialString)
                    {
                        shipRest = "Fighters/Corvettes Only";
                        specialString = true;
                    }
                    else if (!mod.FightersOnly && !mod.DroneModule && !mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule && !mod.DestroyerModule && !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule && !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                    {
                        shipRest = "All Hulls";
                        specialString = true;
                    }

                }
				if (GlobalStats.ActiveModInfo != null && !GlobalStats.ActiveModInfo.useDrones && GlobalStats.ActiveModInfo.useDestroyers)
                {
                    if (!mod.FightersOnly && mod.FighterModule && mod.CorvetteModule && mod.FrigateModule && mod.DestroyerModule && mod.CruiserModule && mod.CruiserModule && mod.CarrierModule && mod.CapitalModule && mod.PlatformModule && mod.StationModule && mod.FreighterModule)
                    {
                        shipRest = "All Hulls";
                        specialString = true;
                    }
                    else if (mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule && !mod.DestroyerModule && !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule && !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                    {
                        shipRest = "Fighters Only";
                        specialString = true;
                    }
                    else if (mod.FightersOnly && !specialString)
                    {
                        shipRest = "Fighters/Corvettes Only";
                        specialString = true;
                    }
                    else if (!mod.FightersOnly && !mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule && !mod.DestroyerModule && !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule && !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                    {
                        shipRest = "All Hulls";
                        specialString = true;
                    }

                }
				if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDrones && !GlobalStats.ActiveModInfo.useDestroyers)
                {
                    if (!mod.FightersOnly && mod.DroneModule && mod.FighterModule && mod.CorvetteModule && mod.FrigateModule && mod.CruiserModule && mod.CruiserModule && mod.CarrierModule && mod.CapitalModule && mod.PlatformModule && mod.StationModule && mod.FreighterModule)
                    {
                        shipRest = "All Hulls";
                        specialString = true;
                    }
                    else if (!mod.FightersOnly && !mod.DroneModule && mod.FighterModule && mod.CorvetteModule && mod.FrigateModule && mod.CruiserModule && mod.CruiserModule && mod.CarrierModule && mod.CapitalModule && mod.PlatformModule && mod.StationModule && mod.FreighterModule)
                    {
                        shipRest = "All Crewed";
                        specialString = true;
                    }
                    else if (mod.FighterModule && !mod.DroneModule && !mod.CorvetteModule && !mod.FrigateModule && !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule && !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                    {
                        shipRest = "Fighters Only";
                        specialString = true;
                    }
                    else if (mod.DroneModule && !mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule && !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule && !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                    {
                        shipRest = "Drones Only";
                        specialString = true;
                    }
                    else if (mod.FightersOnly && !specialString)
                    {
                        shipRest = "Fighters/Corvettes Only";
                        specialString = true;
                    }
                    else if (!mod.FightersOnly && !mod.DroneModule && !mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule && !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule && !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                    {
                        shipRest = "All Hulls";
                        specialString = true;
                    }
                }
				if (GlobalStats.ActiveModInfo == null || (!GlobalStats.ActiveModInfo.useDrones && !GlobalStats.ActiveModInfo.useDestroyers))
                {
                    if (!mod.FightersOnly && mod.FighterModule && mod.CorvetteModule && mod.FrigateModule && mod.CruiserModule && mod.CruiserModule && mod.CarrierModule && mod.CapitalModule && mod.PlatformModule && mod.StationModule && mod.FreighterModule)
                    {
                        shipRest = "All Hulls";
                        specialString = true;
                    }
                    else if (mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule && !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule && !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                    {
                        shipRest = "Fighters Only";
                        specialString = true;
                    }
                    else if (mod.FightersOnly && !specialString)
                    {
                        shipRest = "Fighters/Corvettes Only";
                        specialString = true;
                    }
                    else if (!mod.FightersOnly && !mod.FighterModule && !mod.CorvetteModule && !mod.FrigateModule && !mod.CruiserModule && !mod.CruiserModule && !mod.CarrierModule && !mod.CapitalModule && !mod.PlatformModule && !mod.StationModule && !mod.FreighterModule)
                    {
                        shipRest = "All Hulls";
                        specialString = true;
                    }
                }

				else if (!specialString && (!mod.DroneModule && GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDrones) || !mod.FighterModule || !mod.CorvetteModule || !mod.FrigateModule || (!mod.DestroyerModule && GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDestroyers) || !mod.CruiserModule || !mod.CruiserModule || !mod.CarrierModule || !mod.CapitalModule || !mod.PlatformModule || !mod.StationModule || !mod.FreighterModule)
                {
					if (mod.DroneModule && GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDrones)
                        shipRest += "Dr ";
                    if (mod.FighterModule)
                        shipRest += "F ";
                    if (mod.CorvetteModule)
                        shipRest += "CO ";
                    if (mod.FrigateModule)
                        shipRest += "FF ";
					if (mod.DestroyerModule && GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDestroyers)
                        shipRest += "DD ";
                    if (mod.CruiserModule)
                        shipRest += "CC ";
                    if (mod.CarrierModule)
                        shipRest += "CV ";
                    if (mod.CapitalModule)
                        shipRest += "CA ";
                    if (mod.FreighterModule)
                        shipRest += "Frt ";
                    if (mod.PlatformModule || mod.StationModule)
                        shipRest += "Stat ";
                }

				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat(Localizer.Token(122), ": ", rest), modTitlePos, Color.Orange);
				modTitlePos.Y = modTitlePos.Y + (float)(Fonts.Arial8Bold.LineSpacing);
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, string.Concat("Hulls: ", shipRest), modTitlePos, Color.LightSteelBlue);
                modTitlePos.Y = modTitlePos.Y + (float)(Fonts.Arial8Bold.LineSpacing + 11);
				int startx = (int)modTitlePos.X;
				string tag = "";
				if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].IsWeapon && Ship_Game.ResourceManager.ShipModulesDict[mod.UID].BombType == null)
				{
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Guided)
					{
						tag = string.Concat(tag, "GUIDED ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Intercept)
					{
						tag = string.Concat(tag, "INTERCEPTABLE ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Energy)
					{
						tag = string.Concat(tag, "ENERGY ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Hybrid)
					{
						tag = string.Concat(tag, "HYBRID ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Kinetic)
					{
						tag = string.Concat(tag, "KINETIC ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
                    if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Explosive && !Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Flak)
					{
						tag = string.Concat(tag, "EXPLOSIVE ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Subspace)
					{
						tag = string.Concat(tag, "SUBSPACE ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Warp)
					{
						tag = string.Concat(tag, "WARP ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_PD)
					{
						tag = string.Concat(tag, "POINT DEFENSE ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
                    if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Flak)
                    {
                        tag = string.Concat(tag, "FLAK ");
                        base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
                        modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
                        tag = "";
                    }

					if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.expandedWeaponCats && (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Missile & !Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Guided))
                    {
                        tag = string.Concat(tag, "ROCKET ");
                        base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
                        modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
                        tag = "";
                    }
					else if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Missile)
					{
						tag = string.Concat(tag, "MISSILE ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}

                    if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Tractor)
                    {
                        tag = string.Concat(tag, "TRACTOR ");
                        base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
                        modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
                        tag = "";
                    }
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Beam)
					{
						tag = string.Concat(tag, "BEAM ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
                    if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Array)
                    {
                        tag = string.Concat(tag, "ARRAY ");
                        base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
                        modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
                        tag = "";
                    }
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Railgun)
					{
						tag = string.Concat(tag, "RAILGUN ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Torpedo)
					{
						tag = string.Concat(tag, "TORPEDO ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Bomb)
					{
						tag = string.Concat(tag, "BOMB ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_BioWeapon)
					{
						tag = string.Concat(tag, "BIOWEAPON ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_SpaceBomb)
					{
						tag = string.Concat(tag, "SPACEBOMB ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Drone)
					{
						tag = string.Concat(tag, "DRONE ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[mod.UID].WeaponType].Tag_Cannon)
					{
						tag = string.Concat(tag, "CANNON ");
						base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, tag, modTitlePos, Color.SpringGreen);
						modTitlePos.X = modTitlePos.X + Fonts.Arial8Bold.MeasureString(tag).X;
						tag = "";
					}
					modTitlePos.Y = modTitlePos.Y + (float)(Fonts.Arial8Bold.LineSpacing + 5);
					modTitlePos.X = (float)startx;
				}
				else if (Ship_Game.ResourceManager.ShipModulesDict[mod.UID].IsWeapon)
				{
					string bombType = Ship_Game.ResourceManager.ShipModulesDict[mod.UID].BombType;
				}
				string txt = this.parseText(Localizer.Token(Ship_Game.ResourceManager.ShipModulesDict[mod.UID].DescriptionIndex), (float)(this.activeModSubMenu.Menu.Width - 20), Fonts.Arial12);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, txt, modTitlePos, Color.White);
				modTitlePos.Y = modTitlePos.Y + (Fonts.Arial12Bold.MeasureString(txt).Y + 8f);
				float starty = modTitlePos.Y;
				if (!mod.isWeapon || mod.InstalledWeapon == null)
				{
                    if (mod.Cost != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(128), (float)mod.Cost * UniverseScreen.GamePaceStatic, 84);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.Mass != 0)
                    {
                        float MassMod = (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.MassModifier;
                        float ArmourMassMod = (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.ArmourMassModifier;

                        if (mod.ModuleType == ShipModuleType.Armor)
                        {
                            this.DrawStat(ref modTitlePos, Localizer.Token(123), (ArmourMassMod * mod.Mass) * MassMod, 79);
                        }
                        else
                        {
                            this.DrawStat(ref modTitlePos, Localizer.Token(123), MassMod * mod.Mass, 79);
                        }
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.HealthMax != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(124), (float)mod.HealthMax + mod.HealthMax * (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.ModHpModifier, 80);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
					if (mod.ModuleType != ShipModuleType.PowerPlant)
					{
						powerDraw = -(float)mod.PowerDraw;
					}
					else
					{
                        powerDraw = (mod.PowerDraw > 0f ? (float)(-mod.PowerDraw) : mod.PowerFlowMax + mod.PowerFlowMax * EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.PowerFlowMod);
					}
                    if (powerDraw != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(125), powerDraw, 81);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.MechanicalBoardingDefense != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(2231), (float)mod.MechanicalBoardingDefense, 143);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
					if (mod.BonusRepairRate != 0f)
					{
						this.DrawStat(ref modTitlePos, string.Concat(Localizer.Token(135), "+"), (float)((mod.BonusRepairRate + mod.BonusRepairRate * EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.RepairMod) * (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull) ? 1f + Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].RepairBonus : 1)), 97);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
                    //Shift to next Column
                    float MaxDepth = modTitlePos.Y;
					modTitlePos.X = modTitlePos.X + 152f;
					modTitlePos.Y = starty;
                    if (mod.thrust != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(131), (float)mod.thrust, 91);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.WarpThrust != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(2064), (float)mod.WarpThrust, 92);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.TurnThrust != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(2260), (float)mod.TurnThrust, 148);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.shield_power_max != 0)
                    {
						this.DrawStat(ref modTitlePos, Localizer.Token(132), mod.shield_power_max * (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull) ? 1f + Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].ShieldBonus : 1f) + EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.ShieldPowerMod * mod.shield_power_max, 93);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.shield_radius != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(133), (float)mod.shield_radius, 94);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.shield_recharge_rate != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(134), (float)mod.shield_recharge_rate, 95);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }

                    // Doc: new shield resistances, UI info.

                    if (mod.shield_kinetic_resist != 0)
                    {
                        this.DrawStatPCShield(ref modTitlePos, Localizer.Token(6162), (float)mod.shield_kinetic_resist, 209);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.shield_energy_resist != 0)
                    {
                        this.DrawStatPCShield(ref modTitlePos, Localizer.Token(6163), (float)mod.shield_energy_resist, 210);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.shield_explosive_resist != 0)
                    {
                        this.DrawStatPCShield(ref modTitlePos, Localizer.Token(6164), (float)mod.shield_explosive_resist, 211);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.shield_missile_resist != 0)
                    {
                        this.DrawStatPCShield(ref modTitlePos, Localizer.Token(6165), (float)mod.shield_missile_resist, 212);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.shield_flak_resist != 0)
                    {
                        this.DrawStatPCShield(ref modTitlePos, Localizer.Token(6166), (float)mod.shield_flak_resist, 213);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.shield_hybrid_resist != 0)
                    {
                        this.DrawStatPCShield(ref modTitlePos, Localizer.Token(6167), (float)mod.shield_hybrid_resist, 214);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.shield_railgun_resist != 0)
                    {
                        this.DrawStatPCShield(ref modTitlePos, Localizer.Token(6168), (float)mod.shield_railgun_resist, 215);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.shield_subspace_resist != 0)
                    {
                        this.DrawStatPCShield(ref modTitlePos, Localizer.Token(6169), (float)mod.shield_subspace_resist, 216);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.shield_warp_resist != 0)
                    {
                        this.DrawStatPCShield(ref modTitlePos, Localizer.Token(6170), (float)mod.shield_warp_resist, 217);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.shield_beam_resist != 0)
                    {
                        this.DrawStatPCShield(ref modTitlePos, Localizer.Token(6171), (float)mod.shield_beam_resist, 218);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.shield_threshold != 0)
                    {
                        this.DrawStatPCShield(ref modTitlePos, Localizer.Token(6176), (float)mod.shield_threshold, 222);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }


                    if (mod.SensorRange != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(126), (float)mod.SensorRange, 96);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.SensorBonus != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(6121), (float)mod.SensorBonus, 167);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.HealPerTurn != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(6131), mod.HealPerTurn, 174);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.TransporterRange != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(126), (float)mod.TransporterRange, 168);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.TransporterPower != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(6123), (float)mod.TransporterPower, 169);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.TransporterTimerConstant != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(6122), (float)mod.TransporterTimerConstant, 170);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.TransporterOrdnance != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(6124), (float)mod.TransporterOrdnance, 171);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.TransporterTroopAssault != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(6135), (float)mod.TransporterTroopAssault, 187);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.TransporterTroopLanding != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(6128), (float)mod.TransporterTroopLanding, 172);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.OrdinanceCapacity != 0)
					{
						this.DrawStat(ref modTitlePos, Localizer.Token(2129), (float)mod.OrdinanceCapacity, 124);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
                    if (mod.Cargo_Capacity != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(119), (float)mod.Cargo_Capacity, 109);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.OrdnanceAddedPerSecond != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(6120), (float)mod.OrdnanceAddedPerSecond, 162);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.InhibitionRadius != 0)
					{
						this.DrawStat(ref modTitlePos, Localizer.Token(2233), (float)mod.InhibitionRadius, 144);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
                    if (mod.TroopCapacity != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(336), (float)mod.TroopCapacity, 173);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.PowerStoreMax != 0)
					{
                        this.DrawStat(ref modTitlePos, Localizer.Token(2235), (float)(mod.PowerStoreMax + mod.PowerStoreMax * EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.FuelCellModifier), 145);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
                    //added by McShooterz: Allow Power Draw at Warp variable to show up in design screen for any module
                    if (mod.PowerDrawAtWarp != 0f)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(6011), (float)(-mod.PowerDrawAtWarp), 178);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
					if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.enableECM && mod.ECM != 0)
                    {
                        this.DrawStatPercent(ref modTitlePos, Localizer.Token(6004), (float)mod.ECM, 154);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.ModuleType == ShipModuleType.Hangar &&  mod.hangarTimerConstant != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(136), (float)mod.hangarTimerConstant, 98);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.explodes)
                    {
                        base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Explodes", modTitlePos, Color.OrangeRed);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.KineticResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6142), (float)mod.KineticResist, 189);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.EnergyResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6143), (float)mod.EnergyResist, 190);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.GuidedResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6144), (float)mod.GuidedResist, 191);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.MissileResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6145), (float)mod.MissileResist, 192);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.HybridResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6146), (float)mod.HybridResist, 193);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.BeamResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6147), (float)mod.BeamResist, 194);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.ExplosiveResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6148), (float)mod.ExplosiveResist, 195);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.InterceptResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6149), (float)mod.InterceptResist, 196);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.RailgunResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6150), (float)mod.RailgunResist, 197);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.SpaceBombResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6151), (float)mod.SpaceBombResist, 198);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.BombResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6152), (float)mod.BombResist, 199);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.BioWeaponResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6153), (float)mod.BioWeaponResist, 200);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.DroneResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6154), (float)mod.DroneResist, 201);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.WarpResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6155), (float)mod.WarpResist, 202);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.TorpedoResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6156), (float)mod.TorpedoResist, 203);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.CannonResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6157), (float)mod.CannonResist, 204);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.SubspaceResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6158), (float)mod.SubspaceResist, 205);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.PDResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6159), (float)mod.PDResist, 206);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.FlakResist != 0)
                    {
                        this.DrawStatPC(ref modTitlePos, Localizer.Token(6160), (float)mod.FlakResist, 207);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.APResist != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(6161), (float)mod.APResist, 208);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.DamageThreshold != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(6175), (float)mod.DamageThreshold, 221);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.EMP_Protection != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(6174), (float)mod.EMP_Protection, 219);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }


                    if (mod.PermittedHangarRoles.Count != 0)
                    {
                        modTitlePos.Y = Math.Max(modTitlePos.Y, MaxDepth) + (float)Fonts.Arial12Bold.LineSpacing;
                        Vector2 shipSelectionPos = new Vector2(modTitlePos.X - 152f, modTitlePos.Y);
                        base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(Localizer.Token(137), " : ", mod.hangarShipUID), shipSelectionPos, Color.Orange);
                        r = this.ChooseFighterSub.Menu;
                        r.Y = r.Y + 25;
                        r.Height = r.Height - 25;
                        sel = new Selector(base.ScreenManager, r, new Color(0, 0, 0, 210));
                        sel.Draw();
                        this.UpdateHangarOptions(mod);
                        this.ChooseFighterSub.Draw();
                        this.ChooseFighterSL.Draw(base.ScreenManager.SpriteBatch);
                        Vector2 bCursor = new Vector2((float)(this.ChooseFighterSub.Menu.X + 15), (float)(this.ChooseFighterSub.Menu.Y + 25));
                        for (int i = this.ChooseFighterSL.indexAtTop; i < this.ChooseFighterSL.Entries.Count && i < this.ChooseFighterSL.indexAtTop + this.ChooseFighterSL.entriesToDisplay; i++)
                        {
                            ScrollList.Entry e = this.ChooseFighterSL.Entries[i];
                            bCursor.Y = (float)e.clickRect.Y;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.HullsDict[(e.item as Ship).GetShipData().Hull].IconPath], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
                            Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
                            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (!string.IsNullOrEmpty((e.item as Ship).VanityName) ? (e.item as Ship).VanityName : (e.item as Ship).Name), tCursor, Color.White);
                            tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                        }
                        if (this.selector != null)
                        {
                            this.selector.Draw();
                            return;
                        }
                    }
                    return;
                }
				else
				{
                    this.DrawStat(ref modTitlePos, Localizer.Token(128), (float)mod.Cost * UniverseScreen.GamePaceStatic, 84);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.DrawStat(ref modTitlePos, Localizer.Token(123), (float)EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.MassModifier * mod.Mass, 79);
					modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.DrawStat(ref modTitlePos, Localizer.Token(124), (float)mod.HealthMax + EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.ModHpModifier * mod.HealthMax, 80);
					modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    this.DrawStat(ref modTitlePos, Localizer.Token(125), (mod.ModuleType != ShipModuleType.PowerPlant ? -(float)mod.PowerDraw : mod.PowerFlowMax), 81);
					modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					this.DrawStat(ref modTitlePos, Localizer.Token(126), (float)ModifiedWeaponStat(mod.InstalledWeapon, "range"), 82);
					modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					if (!mod.InstalledWeapon.explodes || mod.InstalledWeapon.OrdinanceRequiredToFire <= 0f)
					{
                        if (mod.InstalledWeapon.isRepairBeam)
                        {
                            this.DrawStat(ref modTitlePos, Localizer.Token(135), (float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * -90f * mod.InstalledWeapon.BeamDuration * GetHullDamageBonus(), 166);
                            modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                            this.DrawStat(ref modTitlePos, "Duration", (float)mod.InstalledWeapon.BeamDuration, 188);
                            modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                        }
                        else if (mod.InstalledWeapon.isBeam)
                        {
                            this.DrawStat(ref modTitlePos, Localizer.Token(127), (float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * 90f * mod.InstalledWeapon.BeamDuration * GetHullDamageBonus(), 83);
                            modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                            this.DrawStat(ref modTitlePos, "Duration", (float)mod.InstalledWeapon.BeamDuration, 188);
                            modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                        }
                        else
                        {
                            this.DrawStat(ref modTitlePos, Localizer.Token(127), (float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus(), 83);
                            modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                        }
					}
					else
					{
                        this.DrawStat(ref modTitlePos, Localizer.Token(127), (float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() + EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.OrdnanceEffectivenessBonus * mod.InstalledWeapon.DamageAmount, 83);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
					modTitlePos.X = modTitlePos.X + 152f;
					modTitlePos.Y = starty;
                    if (!mod.InstalledWeapon.isBeam && !mod.InstalledWeapon.isRepairBeam)
					{
                        this.DrawStat(ref modTitlePos, Localizer.Token(129), (float)ModifiedWeaponStat(mod.InstalledWeapon, "speed"), 85);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
                    if (mod.InstalledWeapon.DamageAmount > 0f)
                    {
                        if (mod.InstalledWeapon.isBeam)
                        {
                            float dps = (float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() * 90f * mod.InstalledWeapon.BeamDuration / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus());
                            this.DrawStat(ref modTitlePos, "DPS", dps, 86);
                            modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                        }
                        else if (mod.InstalledWeapon.explodes && mod.InstalledWeapon.OrdinanceRequiredToFire > 0f)
                        {
                            if (mod.InstalledWeapon.SalvoCount <= 1)
                            {
                                float dps = 1f / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) * ((float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() + EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.OrdnanceEffectivenessBonus * mod.InstalledWeapon.DamageAmount);
                                dps = dps * (float)mod.InstalledWeapon.ProjectileCount;
                                this.DrawStat(ref modTitlePos, "DPS", dps, 86);
                                modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                            }
                            else
                            {
                                float dps = (float)mod.InstalledWeapon.SalvoCount / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) * ((float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() + EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.OrdnanceEffectivenessBonus * mod.InstalledWeapon.DamageAmount);
                                dps = dps * (float)mod.InstalledWeapon.ProjectileCount;
                                this.DrawStat(ref modTitlePos, "DPS", dps, 86);
                                modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                                this.DrawStat(ref modTitlePos, "Salvo", (float)mod.InstalledWeapon.SalvoCount, 182);
                                modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                            }
                        }
                        else if (mod.InstalledWeapon.SalvoCount <= 1)
                        {
                            float dps = 1f / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) * ((float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() + (float)mod.InstalledWeapon.DamageAmount * EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.EnergyDamageMod);
                            dps = dps * (float)mod.InstalledWeapon.ProjectileCount;
                            this.DrawStat(ref modTitlePos, "DPS", dps, 86);
                            modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                        }
                        else
                        {
                            float dps = (float)mod.InstalledWeapon.SalvoCount / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) * ((float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() + (float)mod.InstalledWeapon.DamageAmount * EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.EnergyDamageMod);
                            dps = dps * (float)mod.InstalledWeapon.ProjectileCount;
                            this.DrawStat(ref modTitlePos, "DPS", dps, 86);
                            modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                            this.DrawStat(ref modTitlePos, "Salvo", (float)mod.InstalledWeapon.SalvoCount, 182);
                            modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                        }
                    }
                    if (mod.InstalledWeapon.BeamPowerCostPerSecond > 0f)
                    {
                        this.DrawStat(ref modTitlePos, "Pwr/s", (float)mod.InstalledWeapon.BeamPowerCostPerSecond, 87);
                        modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    this.DrawStat(ref modTitlePos, "Delay", mod.InstalledWeapon.fireDelay, 183);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					if (mod.InstalledWeapon.EMPDamage > 0f)
					{
                        this.DrawStat(ref modTitlePos, "EMP", 1f / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) * (float)mod.InstalledWeapon.EMPDamage, 110);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
                    if (mod.InstalledWeapon.SiphonDamage > 0f)
                    {
                        float damage;
                        if (mod.InstalledWeapon.isBeam)
                            damage = mod.InstalledWeapon.SiphonDamage * 90f * mod.InstalledWeapon.BeamDuration;
                        else
                            damage = mod.InstalledWeapon.SiphonDamage;
                        this.DrawStat(ref modTitlePos, "Siphon", damage, 184);
                        modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.InstalledWeapon.MassDamage > 0f)
                    {
                        float damage;
                        if (mod.InstalledWeapon.isBeam)
                            damage = mod.InstalledWeapon.MassDamage * 90f * mod.InstalledWeapon.BeamDuration;
                        else
                            damage = mod.InstalledWeapon.MassDamage;
                        this.DrawStat(ref modTitlePos, "Tractor", damage, 185);
                        modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.InstalledWeapon.PowerDamage > 0f)
                    {
                        float damage;
                        if (mod.InstalledWeapon.isBeam)
                            damage = mod.InstalledWeapon.PowerDamage * 90f * mod.InstalledWeapon.BeamDuration;
                        else
                            damage = mod.InstalledWeapon.PowerDamage;
                        this.DrawStat(ref modTitlePos, "Pwr Dmg", damage, 186);
                        modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                    }
					this.DrawStat(ref modTitlePos, Localizer.Token(130), (float)mod.FieldOfFire, 88);
					modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					if (mod.InstalledWeapon.OrdinanceRequiredToFire > 0f)
					{
						this.DrawStat(ref modTitlePos, "Ord / Shot", (float)mod.InstalledWeapon.OrdinanceRequiredToFire, 89);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
					if (mod.InstalledWeapon.PowerRequiredToFire > 0f)
					{
						this.DrawStat(ref modTitlePos, "Pwr / Shot", (float)mod.InstalledWeapon.PowerRequiredToFire, 90);
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
					if (mod.InstalledWeapon.Tag_Guided && GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.enableECM)
                    {
                        this.DrawStatPercent(ref modTitlePos, Localizer.Token(6005), (float)mod.InstalledWeapon.ECMResist, 155);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
					if (mod.InstalledWeapon.EffectVsArmor != 1f)
					{
						if (mod.InstalledWeapon.EffectVsArmor <= 1f)
						{
                            float effectVsArmor = ModifiedWeaponStat(mod.InstalledWeapon, "armor") * 100f;
							this.DrawStat105Bad(ref modTitlePos, "VS Armor", string.Concat(effectVsArmor.ToString("#"), "%"), 147);
						}
						else
						{
                            float single = ModifiedWeaponStat(mod.InstalledWeapon, "armor") * 100f;
							this.DrawStat105(ref modTitlePos, "VS Armor", string.Concat(single.ToString("#"), "%"), 147);
						}
						modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
					}
                    if (mod.InstalledWeapon.EffectVSShields != 1f)
                    {
                        if (mod.InstalledWeapon.EffectVSShields <= 1f)
                        {
                            float effectVSShields = ModifiedWeaponStat(mod.InstalledWeapon, "shield") * 100f;
                            this.DrawStat105Bad(ref modTitlePos, "VS Shield", string.Concat(effectVSShields.ToString("#"), "%"), 147);
                        }
                        else
                        {
                            float effectVSShields1 = ModifiedWeaponStat(mod.InstalledWeapon, "shield") * 100f;
                            this.DrawStat105(ref modTitlePos, "VS Shield", string.Concat(effectVSShields1.ToString("#"), "%"), 147);
                        }
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.InstalledWeapon.ShieldPenChance > 0)
                    {
                        this.DrawStat(ref modTitlePos, "Shield Pen", mod.InstalledWeapon.ShieldPenChance, 181);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    if (mod.OrdinanceCapacity != 0)
                    {
                        this.DrawStat(ref modTitlePos, Localizer.Token(2129), (float)mod.OrdinanceCapacity, 124);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }

                    
                    if (mod.InstalledWeapon.TruePD)
                    {
                        string fireRest = "Cannot Target Ships";
                        modTitlePos.Y = modTitlePos.Y + 2* ((float)Fonts.Arial12Bold.LineSpacing);
                        modTitlePos.X = modTitlePos.X - 152f;
                        base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(fireRest), modTitlePos, Color.LightCoral);
                        return;
					}
                    if (!mod.InstalledWeapon.TruePD && mod.InstalledWeapon.Excludes_Fighters || mod.InstalledWeapon.Excludes_Corvettes || mod.InstalledWeapon.Excludes_Capitals || mod.InstalledWeapon.Excludes_Stations)
                    {
                        string fireRest = "Cannot Target:";
                        modTitlePos.Y = modTitlePos.Y + 2 * ((float)Fonts.Arial12Bold.LineSpacing);
                        modTitlePos.X = modTitlePos.X - 152f;
                        base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, string.Concat(fireRest), modTitlePos, Color.LightCoral);
                        modTitlePos.X = modTitlePos.X + 120f;

                        if (mod.InstalledWeapon.Excludes_Fighters)
                        {
							if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDrones)
                            {
                                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Drones", modTitlePos, Color.LightCoral);
                                modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                            }
                            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Fighters", modTitlePos, Color.LightCoral);
                            modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                        }
                        if (mod.InstalledWeapon.Excludes_Corvettes)
                        {
                            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Corvettes", modTitlePos, Color.LightCoral);
                            modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                        }
                        if (mod.InstalledWeapon.Excludes_Capitals)
                        {
                            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Capitals", modTitlePos, Color.LightCoral);
                            modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                        }
                        if (mod.InstalledWeapon.Excludes_Stations)
                        {
                            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Stations", modTitlePos, Color.LightCoral);
                            modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                        }

                        return;

                    }
                    else
                        return;
                }
			}
		}

		private void DrawHullSelection()
		{
			Rectangle r = this.hullSelectionSub.Menu;
			r.Y = r.Y + 25;
			r.Height = r.Height - 25;
			Selector sel = new Selector(base.ScreenManager, r, new Color(0, 0, 0, 210));
			sel.Draw();
			this.hullSL.Draw(base.ScreenManager.SpriteBatch);
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			this.hullSelectionSub.Draw();
			Vector2 bCursor = new Vector2((float)(this.hullSelectionSub.Menu.X + 10), (float)(this.hullSelectionSub.Menu.Y + 45));
			for (int i = this.hullSL.indexAtTop; i < this.hullSL.Copied.Count && i < this.hullSL.indexAtTop + this.hullSL.entriesToDisplay; i++)
			{
				bCursor = new Vector2((float)(this.hullSelectionSub.Menu.X + 10), (float)(this.hullSelectionSub.Menu.Y + 45));
				ScrollList.Entry e = this.hullSL.Copied[i];
				bCursor.Y = (float)e.clickRect.Y;
				if (e.item is ModuleHeader)
				{
					(e.item as ModuleHeader).Draw(base.ScreenManager, bCursor);
				}
				else if (e.item is ShipData)
				{
					bCursor.X = bCursor.X + 10f;
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[(e.item as ShipData).IconPath], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y + 3f);
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (e.item as ShipData).Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, Localizer.GetRole((e.item as ShipData).Role, EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty)), tCursor, Color.Orange);
					if (HelperFunctions.CheckIntersection(e.clickRect, MousePos))
					{
						if (e.clickRectHover == 0)
						{
							AudioManager.PlayCue("sd_ui_mouseover");
						}
						e.clickRectHover = 1;
					}
				}
			}
		}

		private void DrawList()
		{
			float h;
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			Vector2 bCursor = new Vector2((float)(this.modSel.Menu.X + 10), (float)(this.modSel.Menu.Y + 45));
			for (int i = this.weaponSL.indexAtTop; i < this.weaponSL.Copied.Count && i < this.weaponSL.indexAtTop + this.weaponSL.entriesToDisplay; i++)
			{
				bCursor = new Vector2((float)(this.modSel.Menu.X + 10), (float)(this.modSel.Menu.Y + 45));
				ScrollList.Entry e = this.weaponSL.Copied[i];
                bCursor.Y = (float)e.clickRect.Y;
				if (e.item is ModuleHeader)
				{
					(e.item as ModuleHeader).Draw(base.ScreenManager, bCursor);
				}
				else if (e.item is ShipModule)
				{
                    
                    bCursor.X += 5f;
					Rectangle modRect = new Rectangle((int)bCursor.X, (int)bCursor.Y, Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].IconTexturePath].Width, Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].IconTexturePath].Height);
					Vector2 vector2 = new Vector2(bCursor.X + 15f, bCursor.Y + 15f);
					Vector2 vector21 = new Vector2((float)(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].IconTexturePath].Width / 2), (float)(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].IconTexturePath].Height / 2));
					float aspectRatio = (float)Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].IconTexturePath].Width / (float)Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].IconTexturePath].Height;
					float w = (float)modRect.Width;
					for (h = (float)modRect.Height; w > 30f || h > 30f; h = h - 1.6f)
					{
						w = w - aspectRatio * 1.6f;
					}
					modRect.Width = (int)w;
					modRect.Height = (int)h;
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].IconTexturePath], modRect, Color.White);
                    //Added by McShooterz: allow longer modules names
					Vector2 tCursor = new Vector2(bCursor.X + 35f, bCursor.Y + 3f);
                    if (Fonts.Arial12Bold.MeasureString(Localizer.Token((e.item as ShipModule).NameIndex)).X + 90 < this.modSel.Menu.Width)
                    {
                        base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, Localizer.Token((e.item as ShipModule).NameIndex), tCursor, Color.White);
                        tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    else
                    {
                        base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial11Bold, Localizer.Token((e.item as ShipModule).NameIndex), tCursor, Color.White);
                        tCursor.Y = tCursor.Y + (float)Fonts.Arial11Bold.LineSpacing;
                    }
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].Restrictions.ToString(), tCursor, Color.Orange);
					tCursor.X = tCursor.X + Fonts.Arial8Bold.MeasureString(Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].Restrictions.ToString()).X;
                    if (Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].InstalledWeapon != null && Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].ModuleType != ShipModuleType.Turret || Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].XSIZE != Ship_Game.ResourceManager.ShipModulesDict[(e.item as ShipModule).UID].YSIZE)
					{
						Rectangle rotateRect = new Rectangle((int)bCursor.X + 240, (int)bCursor.Y + 3, 20, 22);
						base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["UI/icon_can_rotate"], rotateRect, Color.White);
						if (HelperFunctions.CheckIntersection(rotateRect, MousePos))
						{
							ToolTip.CreateTooltip("Indicates that this module can be rotated using the arrow keys", base.ScreenManager);
						}
					}
					if (HelperFunctions.CheckIntersection(e.clickRect, MousePos))
					{
						if (e.clickRectHover == 0)
						{
							AudioManager.PlayCue("sd_ui_mouseover");
						}
						e.clickRectHover = 1;
					}
				}
			}
		}

		private void DrawModuleSelection()
		{
			Rectangle r = this.modSel.Menu;
			r.Y = r.Y + 25;
			r.Height = r.Height - 25;
			Selector sel = new Selector(base.ScreenManager, r, new Color(0, 0, 0, 210));
			sel.Draw();
			this.modSel.Draw();
			this.weaponSL.Draw(base.ScreenManager.SpriteBatch);
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 vector2 = new Vector2(x, (float)state.Y);
			if (this.modSel.Tabs[0].Selected)
			{
				if (this.Reset)
				{
					this.weaponSL.Entries.Clear();
					List<string> WeaponCategories = new List<string>();
					foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
					{
						if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
						{
							continue;
						}
						module.Value.ModuleType.ToString();
						ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
						tmp.SetAttributesNoParent();
                        bool restricted = tmp.FighterModule || tmp.CorvetteModule || tmp.FrigateModule || tmp.StationModule || tmp.DestroyerModule || tmp.CruiserModule
                            || tmp.CarrierModule || tmp.CapitalModule || tmp.FreighterModule || tmp.PlatformModule || tmp.DroneModule;
                        if (restricted)
                        {
                            if (this.ActiveHull.Role == "drone" && tmp.DroneModule == false)
                            {
                                continue;
                            }
                            if ((this.ActiveHull.Role == "fighter" || this.ActiveHull.Role == "scout" )&& tmp.FighterModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "corvette" && tmp.CorvetteModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "frigate" && tmp.FrigateModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "destroyer" && tmp.DestroyerModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "cruiser" && tmp.CruiserModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "carrier" && tmp.CarrierModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "capital" && tmp.CapitalModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "freighter" && tmp.FreighterModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "platform" && tmp.PlatformModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "station" && tmp.StationModule == false)
                            {
                                continue;
                            }
                        }
                            // if not using new tags, ensure original <FightersOnly> still functions as in vanilla.
                        else if (!restricted && tmp.FightersOnly && this.ActiveHull.Role != "fighter" && this.ActiveHull.Role != "scout" && this.ActiveHull.Role != "corvette")
                            continue;
						if (tmp.isWeapon)
						{
							if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.expandedWeaponCats)
                            {
                                if (tmp.InstalledWeapon.Tag_Flak && !WeaponCategories.Contains("Flak Cannon"))
                                {
                                    WeaponCategories.Add("Flak Cannon");
                                    ModuleHeader type = new ModuleHeader("Flak Cannon", 240f);
                                    this.weaponSL.AddItem(type);
                                }
                                if (tmp.InstalledWeapon.Tag_Railgun && !WeaponCategories.Contains("Magnetic Cannon"))
                                {
                                    WeaponCategories.Add("Magnetic Cannon");
                                    ModuleHeader type = new ModuleHeader("Magnetic Cannon", 240f);
                                    this.weaponSL.AddItem(type);
                                }
                                if (tmp.InstalledWeapon.Tag_Array && !WeaponCategories.Contains("Beam Array"))
                                {
                                    WeaponCategories.Add("Beam Array");
                                    ModuleHeader type = new ModuleHeader("Beam Array", 240f);
                                    this.weaponSL.AddItem(type);
                                }
                                if (tmp.InstalledWeapon.Tag_Tractor && !WeaponCategories.Contains("Tractor Beam"))
                                {
                                    WeaponCategories.Add("Tractor Beam");
                                    ModuleHeader type = new ModuleHeader("Tractor Beam", 240f);
                                    this.weaponSL.AddItem(type);
                                }
                                if (tmp.InstalledWeapon.Tag_Missile && !tmp.InstalledWeapon.Tag_Guided && !WeaponCategories.Contains("Unguided Rocket"))
                                {
                                    WeaponCategories.Add("Unguided Rocket");
                                    ModuleHeader type = new ModuleHeader("Unguided Rocket", 240f);
                                    this.weaponSL.AddItem(type);
                                }
                                else if (!WeaponCategories.Contains(tmp.InstalledWeapon.WeaponType))
                                {
                                    WeaponCategories.Add(tmp.InstalledWeapon.WeaponType);
                                    ModuleHeader type = new ModuleHeader(tmp.InstalledWeapon.WeaponType, 240f);
                                    this.weaponSL.AddItem(type);
                                }
                            }
                            else
                            {
                                if (!WeaponCategories.Contains(tmp.InstalledWeapon.WeaponType))
                                {
                                    WeaponCategories.Add(tmp.InstalledWeapon.WeaponType);
                                    ModuleHeader type = new ModuleHeader(tmp.InstalledWeapon.WeaponType, 240f);
                                    this.weaponSL.AddItem(type);
                                }
                            }
						}
						else if (tmp.ModuleType == ShipModuleType.Bomb && !WeaponCategories.Contains("Bomb"))
						{
							WeaponCategories.Add("Bomb");
							ModuleHeader type = new ModuleHeader("Bomb", 240f);
							this.weaponSL.AddItem(type);
						}
						tmp = null;
					}
					foreach (ScrollList.Entry e in this.weaponSL.Entries)
					{
						foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
						{
							if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
							{
								continue;
							}
							ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
							tmp.SetAttributesNoParent();                            
                            bool restricted = tmp.FighterModule || tmp.CorvetteModule || tmp.FrigateModule || tmp.StationModule || tmp.DestroyerModule || tmp.CruiserModule
                            || tmp.CarrierModule || tmp.CapitalModule || tmp.FreighterModule || tmp.PlatformModule || tmp.DroneModule;
                            if (restricted)
                            {
                                if (this.ActiveHull.Role == "drone" && tmp.DroneModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "scout" && tmp.FighterModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "fighter" && tmp.FighterModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "corvette" && tmp.CorvetteModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "frigate" && tmp.FrigateModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "destroyer" && tmp.DestroyerModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "cruiser" && tmp.CruiserModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "carrier" && tmp.CarrierModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "capital" && tmp.CapitalModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "freighter" && tmp.FreighterModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "platform" && tmp.PlatformModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "station" && tmp.StationModule == false)
                                {
                                    continue;
                                }
                            }
                            // if not using new tags, ensure original <FightersOnly> still functions as in vanilla.
                            else if (!restricted && tmp.FightersOnly && this.ActiveHull.Role != "fighter" && this.ActiveHull.Role != "scout" && this.ActiveHull.Role != "corvette")
                                continue;
							if (tmp.isWeapon)
							{
								if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.expandedWeaponCats)
                                {
                                    if (tmp.InstalledWeapon.Tag_Flak || tmp.InstalledWeapon.Tag_Array || tmp.InstalledWeapon.Tag_Railgun || tmp.InstalledWeapon.Tag_Tractor || (tmp.InstalledWeapon.Tag_Missile && !tmp.InstalledWeapon.Tag_Guided))
                                    {
                                        if ((e.item as ModuleHeader).Text == "Flak Cannon" && tmp.InstalledWeapon.Tag_Flak)
                                            e.AddItem(module.Value);
                                        if ((e.item as ModuleHeader).Text == "Magnetic Cannon" && tmp.InstalledWeapon.Tag_Railgun)
                                            e.AddItem(module.Value);
                                        if ((e.item as ModuleHeader).Text == "Beam Array" && tmp.InstalledWeapon.Tag_Array)
                                            e.AddItem(module.Value);
                                        if ((e.item as ModuleHeader).Text == "Tractor Beam" && tmp.InstalledWeapon.Tag_Tractor)
                                            e.AddItem(module.Value);
                                        if ((e.item as ModuleHeader).Text == "Unguided Rocket" && tmp.InstalledWeapon.Tag_Missile && !tmp.InstalledWeapon.Tag_Guided)
                                            e.AddItem(module.Value);
                                    }
                                    else if ((e.item as ModuleHeader).Text == tmp.InstalledWeapon.WeaponType)
                                    {
                                        e.AddItem(module.Value);
                                    }
                                }
                                else
                                {
                                    if ((e.item as ModuleHeader).Text == tmp.InstalledWeapon.WeaponType)
                                    {
                                        e.AddItem(module.Value);
                                    }
                                }
							}
							else if (tmp.ModuleType == ShipModuleType.Bomb && (e.item as ModuleHeader).Text == "Bomb")
							{
								e.AddItem(module.Value);
							}
							tmp = null;
						}
					}
					this.Reset = false;
				}
				this.DrawList();
			}
			if (this.modSel.Tabs[2].Selected)
			{
				if (this.Reset)
				{
					this.weaponSL.Entries.Clear();
					List<string> ModuleCategories = new List<string>();
					foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
					{
						if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
						{
							continue;
						}
						module.Value.ModuleType.ToString();
						ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
						tmp.SetAttributesNoParent();                        
                        bool restricted = tmp.FighterModule || tmp.CorvetteModule || tmp.FrigateModule || tmp.StationModule || tmp.DestroyerModule || tmp.CruiserModule
                            || tmp.CarrierModule || tmp.CapitalModule || tmp.FreighterModule || tmp.PlatformModule || tmp.DroneModule;
                        if (restricted)
                        {
                            if (this.ActiveHull.Role == "drone" && tmp.DroneModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "scout" && tmp.FighterModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "fighter" && tmp.FighterModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "corvette" && tmp.CorvetteModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "frigate" && tmp.FrigateModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "destroyer" && tmp.DestroyerModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "cruiser" && tmp.CruiserModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "carrier" && tmp.CarrierModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "capital" && tmp.CapitalModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "freighter" && tmp.FreighterModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "platform" && tmp.PlatformModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "station" && tmp.StationModule == false)
                            {
                                continue;
                            }
                        }
                        // if not using new tags, ensure original <FightersOnly> still functions as in vanilla.
                        else if (!restricted && tmp.FightersOnly && this.ActiveHull.Role != "fighter" && this.ActiveHull.Role != "scout" && this.ActiveHull.Role != "corvette")
                            continue;
						if ((tmp.ModuleType == ShipModuleType.Armor || tmp.ModuleType == ShipModuleType.Shield || tmp.ModuleType == ShipModuleType.Countermeasure) && !tmp.isBulkhead && !tmp.isPowerArmour && !ModuleCategories.Contains(tmp.ModuleType.ToString()))
						{
							ModuleCategories.Add(tmp.ModuleType.ToString());
							ModuleHeader type = new ModuleHeader(tmp.ModuleType.ToString(), 240f);
							this.weaponSL.AddItem(type);
						}

                        // These need special booleans as they are ModuleType ARMOR - and the armor ModuleType is needed for vsArmor damage calculations - don't want to use new moduletype therefore.
                        if (tmp.isPowerArmour && tmp.ModuleType == ShipModuleType.Armor && !ModuleCategories.Contains(Localizer.Token(6172)))
                        {
                            ModuleCategories.Add(Localizer.Token(6172));
                            ModuleHeader type = new ModuleHeader(Localizer.Token(6172), 240f);
                            this.weaponSL.AddItem(type);
                        }
                        if (tmp.isBulkhead && tmp.ModuleType == ShipModuleType.Armor && !ModuleCategories.Contains(Localizer.Token(6173)))
                        {
                            ModuleCategories.Add(Localizer.Token(6173));
                            ModuleHeader type = new ModuleHeader(Localizer.Token(6173), 240f);
                            this.weaponSL.AddItem(type);
                        }

						tmp = null;
					}
					foreach (ScrollList.Entry e in this.weaponSL.Entries)
					{
						foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
						{
							if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
							{
								continue;
							}
							ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
							tmp.SetAttributesNoParent();

                            bool restricted = tmp.FighterModule || tmp.CorvetteModule || tmp.FrigateModule || tmp.StationModule || tmp.DestroyerModule || tmp.CruiserModule
                            || tmp.CarrierModule || tmp.CapitalModule || tmp.FreighterModule || tmp.PlatformModule || tmp.DroneModule;
                            if (restricted)
                            {
                                if (this.ActiveHull.Role == "drone" && tmp.DroneModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "scout" && tmp.FighterModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "fighter" && tmp.FighterModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "corvette" && tmp.CorvetteModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "frigate" && tmp.FrigateModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "destroyer" && tmp.DestroyerModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "cruiser" && tmp.CruiserModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "carrier" && tmp.CarrierModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "capital" && tmp.CapitalModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "freighter" && tmp.FreighterModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "platform" && tmp.PlatformModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "station" && tmp.StationModule == false)
                                {
                                    continue;
                                }
                            }
							if ((tmp.ModuleType == ShipModuleType.Armor || tmp.ModuleType == ShipModuleType.Shield || tmp.ModuleType == ShipModuleType.Countermeasure) && !tmp.isBulkhead && !tmp.isPowerArmour && (e.item as ModuleHeader).Text == tmp.ModuleType.ToString())
							{
								e.AddItem(module.Value);
							}
                            if (tmp.isPowerArmour && (e.item as ModuleHeader).Text == Localizer.Token(6172))
                            {
                                e.AddItem(module.Value);
                            }
                            if (tmp.isBulkhead && (e.item as ModuleHeader).Text == Localizer.Token(6173))
                            {
                                e.AddItem(module.Value);
                            }
							tmp = null;
						}
					}
					this.Reset = false;
				}
				this.DrawList();
			}
			if (this.modSel.Tabs[1].Selected)
			{
				if (this.Reset)
				{
					this.weaponSL.Entries.Clear();
					List<string> ModuleCategories = new List<string>();
					foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
					{
						if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
						{
							continue;
						}
						module.Value.ModuleType.ToString();
						ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
						tmp.SetAttributesNoParent();                        
                        bool restricted = tmp.FighterModule || tmp.CorvetteModule || tmp.FrigateModule || tmp.StationModule || tmp.DestroyerModule || tmp.CruiserModule
                            || tmp.CarrierModule || tmp.CapitalModule || tmp.FreighterModule || tmp.PlatformModule || tmp.DroneModule;
                        if (restricted)
                        {
                            if (this.ActiveHull.Role == "drone" && tmp.DroneModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "scout" && tmp.FighterModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "fighter" && tmp.FighterModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "corvette" && tmp.CorvetteModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "frigate" && tmp.FrigateModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "destroyer" && tmp.DestroyerModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "cruiser" && tmp.CruiserModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "carrier" && tmp.CarrierModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "capital" && tmp.CapitalModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "freighter" && tmp.FreighterModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "platform" && tmp.PlatformModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "station" && tmp.StationModule == false)
                            {
                                continue;
                            }
                        }
						if ((tmp.ModuleType == ShipModuleType.Engine || tmp.ModuleType == ShipModuleType.FuelCell || tmp.ModuleType == ShipModuleType.PowerPlant || tmp.ModuleType == ShipModuleType.PowerConduit) && !ModuleCategories.Contains(tmp.ModuleType.ToString()))
						{
							ModuleCategories.Add(tmp.ModuleType.ToString());
							ModuleHeader type = new ModuleHeader(tmp.ModuleType.ToString(), 240f);
							this.weaponSL.AddItem(type);
						}
						tmp = null;
					}
					foreach (ScrollList.Entry e in this.weaponSL.Entries)
					{
						foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
						{
							if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
							{
								continue;
							}
							ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
							tmp.SetAttributesNoParent();
                            bool restricted = tmp.FighterModule || tmp.CorvetteModule || tmp.FrigateModule || tmp.StationModule || tmp.DestroyerModule || tmp.CruiserModule
                            || tmp.CarrierModule || tmp.CapitalModule || tmp.FreighterModule || tmp.PlatformModule || tmp.DroneModule;
                            if (restricted)
                            {
                                if (this.ActiveHull.Role == "drone" && tmp.DroneModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "scout" && tmp.FighterModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "fighter" && tmp.FighterModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "corvette" && tmp.CorvetteModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "frigate" && tmp.FrigateModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "destroyer" && tmp.DestroyerModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "cruiser" && tmp.CruiserModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "carrier" && tmp.CarrierModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "capital" && tmp.CapitalModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "freighter" && tmp.FreighterModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "platform" && tmp.PlatformModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "station" && tmp.StationModule == false)
                                {
                                    continue;
                                }
                            }
							if ((tmp.ModuleType == ShipModuleType.Engine || tmp.ModuleType == ShipModuleType.FuelCell || tmp.ModuleType == ShipModuleType.PowerPlant || tmp.ModuleType == ShipModuleType.PowerConduit) && (e.item as ModuleHeader).Text == tmp.ModuleType.ToString())
							{
								e.AddItem(module.Value);
							}
							tmp = null;
						}
					}
					this.Reset = false;
				}
				this.DrawList();
			}
			if (this.modSel.Tabs[3].Selected)
			{
				if (this.Reset)
				{
					this.weaponSL.Entries.Clear();
					List<string> ModuleCategories = new List<string>();
					foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
					{
						if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
						{
							continue;
						}
						module.Value.ModuleType.ToString();
						ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
						tmp.SetAttributesNoParent();
                        bool restricted = tmp.FighterModule || tmp.CorvetteModule || tmp.FrigateModule || tmp.StationModule || tmp.DestroyerModule || tmp.CruiserModule
                            || tmp.CarrierModule || tmp.CapitalModule || tmp.FreighterModule || tmp.PlatformModule || tmp.DroneModule;
                        if (restricted)
                        {
                            if (this.ActiveHull.Role == "drone" && tmp.DroneModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "scout" && tmp.FighterModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "fighter" && tmp.FighterModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "corvette" && tmp.CorvetteModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "frigate" && tmp.FrigateModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "destroyer" && tmp.DestroyerModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "cruiser" && tmp.CruiserModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "carrier" && tmp.CarrierModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "capital" && tmp.CapitalModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "freighter" && tmp.FreighterModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "platform" && tmp.PlatformModule == false)
                            {
                                continue;
                            }
                            if (this.ActiveHull.Role == "station" && tmp.StationModule == false)
                            {
                                continue;
                            }
                        }
                        if ((tmp.ModuleType == ShipModuleType.Troop || tmp.ModuleType == ShipModuleType.Colony || tmp.ModuleType == ShipModuleType.Command || tmp.ModuleType == ShipModuleType.Storage || tmp.ModuleType == ShipModuleType.Hangar || tmp.ModuleType == ShipModuleType.Sensors || tmp.ModuleType == ShipModuleType.Special || tmp.ModuleType == ShipModuleType.Transporter || tmp.ModuleType == ShipModuleType.Ordnance) && !ModuleCategories.Contains(tmp.ModuleType.ToString()))
						{
							ModuleCategories.Add(tmp.ModuleType.ToString());
							ModuleHeader type = new ModuleHeader(tmp.ModuleType.ToString(), 240f);
							this.weaponSL.AddItem(type);
						}
						tmp = null;
					}
					foreach (ScrollList.Entry e in this.weaponSL.Entries)
					{
						foreach (KeyValuePair<string, ShipModule> module in Ship_Game.ResourceManager.ShipModulesDict)
						{
							if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetMDict()[module.Key] || module.Value.UID == "Dummy")
							{
								continue;
							}
							ShipModule tmp = Ship_Game.ResourceManager.GetModule(module.Key);
							tmp.SetAttributesNoParent();
                            bool restricted = tmp.FighterModule || tmp.CorvetteModule || tmp.FrigateModule || tmp.StationModule || tmp.DestroyerModule || tmp.CruiserModule
                            || tmp.CarrierModule || tmp.CapitalModule || tmp.FreighterModule || tmp.PlatformModule || tmp.DroneModule;
                            if (restricted)
                            {
                                if (this.ActiveHull.Role == "drone" && tmp.DroneModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "scout" && tmp.FighterModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "fighter" && tmp.FighterModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "corvette" && tmp.CorvetteModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "frigate" && tmp.FrigateModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "destroyer" && tmp.DestroyerModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "cruiser" && tmp.CruiserModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "carrier" && tmp.CarrierModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "capital" && tmp.CapitalModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "freighter" && tmp.FreighterModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "platform" && tmp.PlatformModule == false)
                                {
                                    continue;
                                }
                                if (this.ActiveHull.Role == "station" && tmp.StationModule == false)
                                {
                                    continue;
                                }
                            }
                            if ((tmp.ModuleType == ShipModuleType.Troop || tmp.ModuleType == ShipModuleType.Colony || tmp.ModuleType == ShipModuleType.Command || tmp.ModuleType == ShipModuleType.Storage || tmp.ModuleType == ShipModuleType.Hangar || tmp.ModuleType == ShipModuleType.Sensors || tmp.ModuleType == ShipModuleType.Special || tmp.ModuleType == ShipModuleType.Transporter || tmp.ModuleType == ShipModuleType.Ordnance) && (e.item as ModuleHeader).Text == tmp.ModuleType.ToString())
							{
								e.AddItem(module.Value);
							}
							tmp = null;
						}
					}
					this.Reset = false;
				}
				this.DrawList();
			}
		}

		private void DrawRequirement(ref Vector2 Cursor, string words, bool met)
		{
			float amount = 165f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 35f;
			}
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, (met ? Color.LightGreen : Color.LightPink));
			string stats = (met ? "OK" : "X");
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stats).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stats, Cursor, (met ? Color.LightGreen : Color.LightPink));
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stats).X);
		}

		private void DrawShipInfoPanel()
		{
			float HitPoints = 0f;
			float Mass = 0f;
			float PowerDraw = 0f;
			float PowerCapacity = 0f;
			float OrdnanceCap = 0f;
			float PowerFlow = 0f;
			float ShieldPower = 0f;
			float Thrust = 0f;
			float AfterThrust = 0f;
			float CargoSpace = 0f;
            byte TroopCount = 0;
			float Size = 0f;
			float Cost = 0f;
			float WarpThrust = 0f;
			float TurnThrust = 0f;
			float WarpableMass = 0f;
			float WarpDraw = 0f;
			float FTLCount = 0f;
			float FTLSpeed = 0f;
            float RepairRate = 0f;
            float sensorRange = 0f;
            float sensorBonus = 0f;
            float BeamLongestDuration = 0f;
            float OrdnanceUsed=0f;
            float OrdnanceRecoverd = 0f;
            float WeaponPowerNeeded = 0f;
            float Upkeep = 0f;
            float FTLSpoolTimer = 0f;
            float EMPResist = 0f;
            bool bEnergyWeapons = false;
			foreach (SlotStruct slot in this.Slots)
			{
				Size = Size + 1f;
				if (slot.isDummy || slot.module == null)
				{
					continue;
				}
				HitPoints = HitPoints + (slot.module.Health + EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.Traits.ModHpModifier * slot.module.Health);
				if (slot.module.Mass < 0f && slot.Powered)
				{
                    if (slot.module.ModuleType == ShipModuleType.Armor)
                    {
                        Mass += slot.module.Mass * EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.ArmourMassModifier;
                    }
                    else
					    Mass += slot.module.Mass;
				}
				else if (slot.module.Mass > 0f)
				{
                    if (slot.module.ModuleType == ShipModuleType.Armor)
                    {
                        Mass += slot.module.Mass * EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.ArmourMassModifier;
                    }
                    else
                        Mass += slot.module.Mass;
				}
                TroopCount += slot.module.TroopCapacity;
                PowerCapacity += slot.module.PowerStoreMax + slot.module.PowerStoreMax * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.FuelCellModifier; 
				OrdnanceCap = OrdnanceCap + (float)slot.module.OrdinanceCapacity;
				PowerFlow += slot.module.PowerFlowMax + slot.module.PowerFlowMax * EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.PowerFlowMod;
				if (slot.module.Powered)
				{
                    EMPResist += slot.module.EMP_Protection;
					WarpableMass = WarpableMass + slot.module.WarpMassCapacity;
                    PowerDraw = PowerDraw + slot.module.PowerDraw;
					WarpDraw = WarpDraw + slot.module.PowerDrawAtWarp;
                    if (slot.module.InstalledWeapon != null && slot.module.InstalledWeapon.PowerRequiredToFire > 0)
                        bEnergyWeapons = true;
                    if (slot.module.InstalledWeapon != null && slot.module.InstalledWeapon.BeamPowerCostPerSecond > 0)
                        bEnergyWeapons = true;
                    if (slot.module.FTLSpeed > 0f)
					{
						FTLCount = FTLCount + 1f;
						FTLSpeed = FTLSpeed + slot.module.FTLSpeed;
					}
                    if (slot.module.FTLSpoolTime * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.SpoolTimeModifier > FTLSpoolTimer)
                    {
                        FTLSpoolTimer = slot.module.FTLSpoolTime * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.SpoolTimeModifier;
                    }
                    ShieldPower += slot.module.shield_power_max + EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.ShieldPowerMod * slot.module.shield_power_max;
					Thrust = Thrust + slot.module.thrust;
					WarpThrust = WarpThrust + (float)slot.module.WarpThrust;
					TurnThrust = TurnThrust + (float)slot.module.TurnThrust;
                    RepairRate += ((slot.module.BonusRepairRate + slot.module.BonusRepairRate * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.Traits.RepairMod) * (GlobalStats.ActiveMod != null && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull) ? 1f + Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].RepairBonus : 1));
                    OrdnanceRecoverd += slot.module.OrdnanceAddedPerSecond;
                    if (slot.module.SensorRange > sensorRange)
                    {
                        sensorRange = slot.module.SensorRange;
                    }
                    if (slot.module.SensorBonus > sensorBonus)
                        sensorBonus = slot.module.SensorBonus;
                    
                    //added by gremlin collect weapon stats                  
                    if (slot.module.isWeapon || slot.module.BombType != null)
                    {
                        Weapon weapon;
                        if (slot.module.BombType == null)
                            weapon = slot.module.InstalledWeapon;
                        else
                            weapon = ResourceManager.WeaponsDict[slot.module.BombType];
                        OrdnanceUsed += weapon.OrdinanceRequiredToFire / weapon.fireDelay * weapon.SalvoCount;
                        WeaponPowerNeeded += weapon.PowerRequiredToFire / weapon.fireDelay * weapon.SalvoCount;
                        if(weapon.isBeam)
                            WeaponPowerNeeded += weapon.BeamPowerCostPerSecond * weapon.BeamDuration / weapon.fireDelay;
                        if(BeamLongestDuration < weapon.BeamDuration)
                            BeamLongestDuration = weapon.BeamDuration; 
                        
                    }
                    //end
				}
				Cost = Cost + slot.module.Cost * UniverseScreen.GamePaceStatic;
				CargoSpace = CargoSpace + slot.module.Cargo_Capacity;

            }
			Mass = Mass + (float)(this.ActiveHull.ModuleSlotList.Count / 2);
			Mass = Mass * EmpireManager.GetEmpireByName(ShipDesignScreen.screen.PlayerLoyalty).data.MassModifier;
			if (Mass < (float)(this.ActiveHull.ModuleSlotList.Count / 2))
			{
				Mass = (float)(this.ActiveHull.ModuleSlotList.Count / 2);
			}
			float Speed = 0f;
			float WarpSpeed = WarpThrust / (Mass + 0.1f);
            //Added by McShooterz: hull bonus speed
            WarpSpeed = WarpSpeed * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.FTLModifier * (GlobalStats.ActiveMod != null && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull) ? 1f + Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].SpeedBonus : 1);
			float single = WarpSpeed / 1000f;
			string WarpString = string.Concat(single.ToString("#.0"), "k");
			float Turn = 0f;
			if (Mass > 0f)
			{
				Speed = Thrust / Mass;
				Turn = TurnThrust / Mass / 700f;
			}
			float AfterSpeed = AfterThrust / (Mass + 0.1f);
			AfterSpeed = AfterSpeed * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.SubLightModifier;
			Turn = (float)MathHelper.ToDegrees(Turn);
            Vector2 Cursor = new Vector2((float)(this.statsSub.Menu.X + 10), (float)(this.ShipStats.Menu.Y + 33));
            //Added by McShooterz: Draw Hull Bonuses
			if (GlobalStats.ActiveMod != null && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull))
            {
               Vector2 LCursor = new Vector2(this.HullSelectionRect.X - 145, HullSelectionRect.Y + 31);
               if (Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].ArmoredBonus != 0 ||
                   Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].ShieldBonus != 0 ||
                   Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].SensorBonus != 0 ||
                   Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].SpeedBonus != 0 ||
                   Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].CargoBonus != 0 ||
                   Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].DamageBonus != 0 ||
                   Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].FireRateBonus != 0 ||
                   Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].RepairBonus != 0 ||
                   Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].CostBonus != 0)
                {
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Verdana14Bold ,Localizer.Token(6015), LCursor, Color.Orange);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Verdana14Bold.LineSpacing + 2);
                }
                if (Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].ArmoredBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6016), Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].ArmoredBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].ShieldBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, "Shield Strength", Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].ShieldBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].SensorBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6017), Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].SensorBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].SpeedBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6018), Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].SpeedBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].CargoBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6019), Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].CargoBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].DamageBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, "Weapon Damage", Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].DamageBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].FireRateBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6020), Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].FireRateBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].RepairBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6013), Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].RepairBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].CostBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6021), Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].CostBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
            }
            //Added by McShooterz: hull bonus starting cost
			this.DrawStat60(ref Cursor, string.Concat(Localizer.Token(109), ":"), (float)(((int)Cost + (GlobalStats.ActiveMod != null && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull) ? Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].StartingCost : 0)) * (GlobalStats.ActiveMod != null && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull) ? 1f - Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].CostBonus : 1)), 99);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);

            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
            {
                Upkeep = GetMaintCostShipyardProportional(this.ActiveHull, Cost, EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty));
            }
            else
            {
                Upkeep = GetMaintCostShipyard(this.ActiveHull, Size, EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty));
            }

            this.DrawStatUpkeep(ref Cursor, "Upkeep Cost:", Upkeep, 175);
            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);

			this.DrawStatEnergy(ref Cursor, string.Concat(Localizer.Token(110), ":"), (int)PowerCapacity, 100);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			this.DrawStatEnergy(ref Cursor, string.Concat(Localizer.Token(111), ":"), (int)(PowerFlow - PowerDraw), 101);
			
	        //added by McShooterz: Allow Warp draw and after burner values be displayed in ship info
            float fDrawAtWarp = 0;
            if (WarpDraw != 0)
            {
                fDrawAtWarp = (PowerFlow - (WarpDraw / 2 * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.FTLPowerDrainModifier + (PowerDraw * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.FTLPowerDrainModifier)));
                if (WarpSpeed > 0)
                {
                    Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergy(ref Cursor, string.Concat(Localizer.Token(112), ":"), (int)fDrawAtWarp, 102);
                }

            }
            else
            {
                fDrawAtWarp = (PowerFlow - PowerDraw * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.FTLPowerDrainModifier);
                if (WarpSpeed > 0)
                {
                    Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergy(ref Cursor, string.Concat(Localizer.Token(112), ":"), (int)fDrawAtWarp, 102);
                }
            }
            

            float fWarpTime = ((-PowerCapacity / fDrawAtWarp) * 0.9f);
            string sWarpTime = fWarpTime.ToString("0.#");
            if (WarpSpeed > 0)
            {
                if (fDrawAtWarp < 0)
                {
                    Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergy(ref Cursor, "FTL Time:", sWarpTime, 176);
                }
                else if (fWarpTime > 900)
                {
                    Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergy(ref Cursor, "FTL Time:", "INF", 176);
                }
                else
                {
                    Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergy(ref Cursor, "FTL Time:", "INF", 176);
                }                
            }
            

            float powerconsumed = WeaponPowerNeeded - PowerFlow;
            float EnergyDuration = 0f;
            if (powerconsumed > 0)
            {
                EnergyDuration = WeaponPowerNeeded > 0 ? ((PowerCapacity) / powerconsumed) : 0;
                if ((EnergyDuration >= BeamLongestDuration) && bEnergyWeapons == true)
                {
                    Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergy60(ref Cursor, "Power Time:", EnergyDuration, 163);
                }
                else if (bEnergyWeapons == true)
                {
                    Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergyBad(ref Cursor, "Power Time:", EnergyDuration.ToString("N1"), 163);
                }

            }
            else
            {
                if (bEnergyWeapons == true)
                {
                    Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatEnergy(ref Cursor, "Power Time:", "INF", 163);
                }
            }
            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
			this.DrawStatDefence(ref Cursor, string.Concat(Localizer.Token(113), ":"), (int)HitPoints, 103);
            //Added by McShooterz: draw total repair
            if (RepairRate > 0)
            {
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatDefence(ref Cursor, string.Concat(Localizer.Token(6013), ":"), (int)RepairRate, 103);                
            }
			if (ShieldPower > 0)
            {
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatDefence(ref Cursor, string.Concat(Localizer.Token(114), ":"), (int)ShieldPower, 104);                
            }
            if (EMPResist > 0)
            {
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatDefence(ref Cursor, string.Concat(Localizer.Token(6177), ":"), (int)EMPResist, 220);
            }

			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
            

            // The Doctor: removed the mass display. It's a meaningless value to the player, and it takes up a valuable line in the limited space.
			//this.DrawStat(ref Cursor, string.Concat(Localizer.Token(115), ":"), (int)Mass, 79);
			//Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);

            #region HardcoreRule info
            if (GlobalStats.HardcoreRuleset)
			{
				string massstring = "";
				if (Mass >= 1000f && Mass < 10000f || Mass <= -1000f && Mass > -10000f)
				{
					float single1 = (float)Mass / 1000f;
					massstring = string.Concat(single1.ToString("#.#"), "k");
				}
				else if (Mass < 10000f)
				{
					massstring = Mass.ToString("#.#");
				}
				else
				{
					float single2 = (float)Mass / 1000f;
					massstring = string.Concat(single2.ToString("#"), "k");
				}
				string wmassstring = "";
				if (WarpableMass >= 1000f && WarpableMass < 10000f || WarpableMass <= -1000f && WarpableMass > -10000f)
				{
					float single3 = (float)WarpableMass / 1000f;
					wmassstring = string.Concat(single3.ToString("#.#"), "k");
				}
				else if (WarpableMass < 10000f)
				{
					wmassstring = WarpableMass.ToString("0.#");
				}
				else
				{
					float single4 = (float)WarpableMass / 1000f;
					wmassstring = string.Concat(single4.ToString("#"), "k");
				}
				string warpmassstring = string.Concat(massstring, "/", wmassstring);
				if (Mass > WarpableMass)
				{
					this.DrawStatBad(ref Cursor, "Warpable Mass:", warpmassstring, 153);
				}
				else
				{
					this.DrawStat(ref Cursor, "Warpable Mass:", warpmassstring, 153);
				}
				Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				this.DrawRequirement(ref Cursor, "Warp Capable", Mass <= WarpableMass);
				Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				if (FTLCount > 0f)
				{
					float speed = FTLSpeed / FTLCount;
					this.DrawStat(ref Cursor, string.Concat(Localizer.Token(2170), ":"), (int)speed, 135);
					Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
				}
            }
            #endregion
            else if (WarpSpeed <= 0f)
			{
				this.DrawStatPropulsion(ref Cursor, string.Concat(Localizer.Token(2170), ":"), 0, 135);
				Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
			else
			{
				this.DrawStatPropulsion(ref Cursor, string.Concat(Localizer.Token(2170), ":"), WarpString, 135);
				Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
            if (WarpSpeed > 0 && FTLSpoolTimer > 0)
            {
                this.DrawStatPropulsion(ref Cursor, "FTL Spool:", FTLSpoolTimer, 177);
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            }
            this.DrawStatPropulsion(ref Cursor, string.Concat(Localizer.Token(116), ":"), (int)(Speed * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.SubLightModifier * (GlobalStats.ActiveMod != null && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull) ? 1f + Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].SpeedBonus : 1)), 105);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            //added by McShooterz: afterburn speed
            if (AfterSpeed != 0)
            {
                this.DrawStatPropulsion(ref Cursor, "Afterburner Speed:", (int)AfterSpeed, 105);
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            }
			this.DrawStatPropulsion60(ref Cursor, string.Concat(Localizer.Token(117), ":"), Turn, 107);
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
            if (OrdnanceCap > 0)
            {
                this.DrawStatOrdnance60(ref Cursor, string.Concat(Localizer.Token(118), ":"), OrdnanceCap, 108);
                
            }
            if (OrdnanceRecoverd > 0)
            {
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatOrdnance60(ref Cursor, "Ordnance Created / s:", OrdnanceRecoverd, 162);
                
            }
            if (OrdnanceCap > 0)
            {
                float AmmoTime = 0f;
                if (OrdnanceUsed - OrdnanceRecoverd > 0)
                {
                    AmmoTime = OrdnanceCap / (OrdnanceUsed - OrdnanceRecoverd);
                    Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatOrdnance60(ref Cursor, "Ammo Time:", AmmoTime, 164);
                }
                else
                {
                    Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatOrdnance(ref Cursor, "Ammo Time:", "INF", 164);
                }

                
            }
            if (TroopCount > 0)
            {
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatOrdnance60(ref Cursor, string.Concat(Localizer.Token(6132), ":"), (float)TroopCount, 180);                
            }

            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);

            if (CargoSpace > 0)
            {
				this.DrawStat60(ref Cursor, string.Concat(Localizer.Token(119), ":"), (CargoSpace + (GlobalStats.ActiveMod != null && GlobalStats.ActiveModInfo.useHullBonuses && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull) ? CargoSpace * Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].CargoBonus : 0)), 109);
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            }
            if (sensorRange != 0)
            {
                this.DrawStat(ref Cursor, string.Concat(Localizer.Token(6130), ":"), (int)((sensorRange + sensorBonus) + (GlobalStats.ActiveMod != null && GlobalStats.ActiveModInfo.useHullBonuses && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull) ? (sensorRange + sensorBonus) * Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].SensorBonus : 0)), 159);
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            }
			Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing);
			bool hasBridge = false;
			bool EmptySlots = true;
			foreach (SlotStruct slot in this.Slots)
			{
				if (!slot.isDummy && slot.ModuleUID == null)
				{
					EmptySlots = false;
				}
				if (slot.ModuleUID == null || !Ship_Game.ResourceManager.ShipModulesDict[slot.ModuleUID].IsCommandModule)
				{
					continue;
				}
				hasBridge = true;
			}

            Vector2 CursorReq = new Vector2((float)(this.statsSub.Menu.X - 180), (float)(this.ShipStats.Menu.Y + (Fonts.Arial12Bold.LineSpacing * 2) + 45));
			if (this.ActiveHull.Role != "platform")
			{
				this.DrawRequirement(ref CursorReq, Localizer.Token(120), hasBridge);
				CursorReq.Y = CursorReq.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
			}
			this.DrawRequirement(ref CursorReq, Localizer.Token(121), EmptySlots);
		}

        private float GetMaintCostShipyard(ShipData ship, float Size, Empire empire)
        {
            float maint = 0f;
            string role = ship.Role;
            string str = role;
            //bool nonCombat = false;
            //added by gremlin: Maintenance changes
            float maintModReduction = 1;

            //Get Maintanence of ship role
            bool foundMaint = false;
            if (ResourceManager.ShipRoles.ContainsKey(ship.Role))
            {
                for (int i = 0; i < ResourceManager.ShipRoles[ship.Role].RaceList.Count; i++)
                {
                    if (ResourceManager.ShipRoles[ship.Role].RaceList[i].ShipType == empire.data.Traits.ShipType)
                    {
                        maint = ResourceManager.ShipRoles[ship.Role].RaceList[i].Upkeep;
                        foundMaint = true;
                        break;
                    }
                }
                if (!foundMaint)
                    maint = ResourceManager.ShipRoles[ship.Role].Upkeep;
            }
            else
                return 0f;

            //Modify Maintanence by freighter size
            if (ship.Role == "freighter")
            {
                switch ((int)Size / 50)
                {
                    case 0:
                        {
                            break;
                        }

                    case 1:
                        {
                            maint *= 1.5f;
                            break;
                        }

                    case 2:
                    case 3:
                    case 4:
                        {
                            maint *= 2f;
                            break;
                        }
                    default:
                        {
                            maint *= (int)Size / 50;
                            break;
                        }
                }
            }

            if ((ship.Role == "freighter" || ship.Role == "platform") && empire.data.CivMaintMod != 1.0)
            {
                maint *= empire.data.CivMaintMod;
            }

            //Apply Privatization
            if ((ship.Role == "freighter" || ship.Role == "platform") && empire.data.Privatization)
            {
                maint *= 0.5f;
            }

            //Subspace Projectors do not get any more modifiers
            if (ship.Name == "Subspace Projector")
            {
                return maint;
            }

            //Maintenance fluctuator
            //string configvalue1 = ConfigurationManager.AppSettings["countoffiles"];
            float OptionIncreaseShipMaintenance = GlobalStats.OptionIncreaseShipMaintenance;
            if (OptionIncreaseShipMaintenance > 1)
            {
                maintModReduction = OptionIncreaseShipMaintenance;
                maint *= maintModReduction;
            }
            return maint;
        }

        private float GetMaintCostShipyardProportional(ShipData ship, float fCost, Empire empire)
        {
            float maint = 0f;
            float maintModReduction = 1;
            string role = ship.Role;

            // Calculate maintenance by proportion of ship cost, Duh.
            if (ship.Role == "fighter" || ship.Role == "scout")
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepFighter;
            else if (ship.Role == "corvette")
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepCorvette;
            else if (ship.Role == "frigate" || ship.Role == "destroyer")
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepFrigate;
            else if (ship.Role == "cruiser")
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepCruiser;
            else if (ship.Role == "carrier")
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepCarrier;
            else if (ship.Role == "capital")
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepCapital;
            else if (ship.Role == "freighter")
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepFreighter;
            else if (ship.Role == "platform")
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepPlatform;
            else if (ship.Role == "station")
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepStation;
            else if (ship.Role == "drone" && GlobalStats.ActiveModInfo.useDrones)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepDrone;
            else
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepBaseline;
            if (maint == 0f && GlobalStats.ActiveModInfo.UpkeepBaseline > 0)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepBaseline;
            else if (maint == 0f && GlobalStats.ActiveModInfo.UpkeepBaseline == 0)
                maint = fCost * 0.004f;


            // Modifiers below here  

            if ((ship.Role == "freighter" || ship.Role == "platform") && empire != null && !empire.isFaction && empire.data.CivMaintMod != 1.0)
            {
                maint *= empire.data.CivMaintMod;
            }

            if ((ship.Role == "freighter" || ship.Role == "platform") && empire != null && !empire.isFaction && empire.data.Privatization)
            {
                maint *= 0.5f;
            }

            if (GlobalStats.OptionIncreaseShipMaintenance > 1)
            {
                maintModReduction = GlobalStats.OptionIncreaseShipMaintenance;
                maint *= (float)maintModReduction;
            }
            return maint;

        }


        private void DrawHullBonus(ref Vector2 Cursor, string words, float stat)
        {
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Verdana12, string.Concat((stat * 100f).ToString(), "% ", words), Cursor, Color.Orange);
        }

        private string GetNumberString(float stat)
        {
            if (stat < 1000f)
                return stat.ToString("#.#");
            else if (stat < 10000f)
                return stat.ToString("#");
            float single = stat / 1000f;
            if (single < 100)
                return string.Concat(single.ToString("#.##"), "k");
            if(single < 1000)
                return string.Concat(single.ToString("#.#"), "k");
            return string.Concat(single.ToString("#"), "k");
        }

		private void DrawStat(ref Vector2 Cursor, string words, float stat, string tip)
		{
			float amount = 105f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
            string numbers = GetNumberString(stat);
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0f ? Color.LightGreen : Color.LightPink));
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(tip, base.ScreenManager);
			}
		}

		private void DrawStat(ref Vector2 Cursor, string words, float stat, int Tooltip_ID)
		{
			float amount = 105f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
            string numbers = stat.ToString("0.0");
			if (stat == 0f)
			{
				numbers = "0";
			}
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0f ? Color.LightGreen : Color.LightPink));
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
			}
		}

        private void DrawStatPC(ref Vector2 Cursor, string words, float stat, int Tooltip_ID)
        {
            float amount = 120f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
            string numbers = stat.ToString("p1");
            if (stat == 0f)
            {
                numbers = "0";
            }
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0f ? Color.LightGreen : Color.LightPink));
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

        private void DrawStatPCShield(ref Vector2 Cursor, string words, float stat, int Tooltip_ID)
        {
            float amount = 120f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.LightSkyBlue);
            string numbers = stat.ToString("p1");
            if (stat == 0f)
            {
                numbers = "0";
            }
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0f ? Color.LightGreen : Color.LightPink));
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

        private void DrawStatEnergy(ref Vector2 Cursor, string words, float stat, int Tooltip_ID)
        {
            float amount = 105f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.LightSkyBlue);
            string numbers = GetNumberString(stat);
            if (stat == 0f)
            {
                numbers = "0";
            }
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0f ? Color.LightGreen : Color.LightPink));
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

        private void DrawStatPropulsion(ref Vector2 Cursor, string words, float stat, int Tooltip_ID)
        {
            float amount = 105f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.DarkSeaGreen);
            string numbers = GetNumberString(stat);
            if (stat == 0f)
            {
                numbers = "0";
            }
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0f ? Color.LightGreen : Color.LightPink));
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

        private void DrawStatUpkeep(ref Vector2 Cursor, string words, float stat, int Tooltip_ID)
        {
            float amount = 165f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
            string numbers = stat.ToString("F2");
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, Color.Salmon);
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

        private void DrawStatPercent(ref Vector2 Cursor, string words, float stat, int Tooltip_ID)
        {
            float amount = 105f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
            string numbers = "";
            float statPC = stat * 100;
            numbers = string.Concat(statPC.ToString("#"), "%");
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0f ? Color.LightGreen : Color.LightPink));
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

		private void DrawStat(ref Vector2 Cursor, string words, int stat, string tip)
		{
			float amount = 165f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
            string numbers = GetNumberString(stat);
			if (stat == 0)
			{
				numbers = "0";
			}
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0 ? Color.LightGreen : Color.LightPink));
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(tip, base.ScreenManager);
			}
		}

		private void DrawStat(ref Vector2 Cursor, string words, int stat, int Tooltip_ID)
		{
			float amount = 165f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
            string numbers = GetNumberString(stat);
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0 ? Color.LightGreen : Color.LightPink));
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
			}
		}

        private void DrawStatEnergy(ref Vector2 Cursor, string words, int stat, int Tooltip_ID)
        {
            float amount = 165f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.LightSkyBlue);
            string numbers = GetNumberString(stat);
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0 ? Color.LightGreen : Color.LightPink));
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

        private void DrawStatDefence(ref Vector2 Cursor, string words, int stat, int Tooltip_ID)
        {
            float amount = 165f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.Goldenrod);
            string numbers = GetNumberString(stat);
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0 ? Color.LightGreen : Color.LightPink));
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

        private void DrawStatPropulsion(ref Vector2 Cursor, string words, int stat, int Tooltip_ID)
        {
            float amount = 165f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.DarkSeaGreen);
            string numbers = GetNumberString(stat);
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0 ? Color.LightGreen : Color.LightPink));
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }


        private void DrawStatOrdnance(ref Vector2 Cursor, string words, int stat, int Tooltip_ID)
        {
            float amount = 165f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.IndianRed);
            string numbers = GetNumberString(stat);
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0 ? Color.LightGreen : Color.LightPink));
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

		private void DrawStat(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
		{
			float amount = 165f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stat).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stat, Cursor, Color.LightGreen);
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stat.ToString()).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
			}
		}

        private void DrawStatEnergy(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            float amount = 165f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.LightSkyBlue);
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stat).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stat, Cursor, Color.LightGreen);
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stat.ToString()).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

        private void DrawStatPropulsion(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            float amount = 165f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.DarkSeaGreen);
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stat).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stat, Cursor, Color.LightGreen);
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stat.ToString()).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

        private void DrawStatOrdnance(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            float amount = 165f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stat).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stat, Cursor, Color.LightGreen);
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stat.ToString()).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

		private void DrawStat105(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
		{
			float amount = 105f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stat).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stat, Cursor, Color.LightGreen);
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stat.ToString()).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
			}
		}

		private void DrawStat105Bad(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
		{
			float amount = 105f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stat).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stat, Cursor, Color.LightPink);
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stat.ToString()).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
			}
		}

		private void DrawStat60(ref Vector2 Cursor, string words, float stat, int Tooltip_ID)
		{
			float amount = 165f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
            string numbers = GetNumberString(stat);
			if (stat == 0f)
			{
				numbers = "0";
			}
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0f ? Color.LightGreen : Color.LightPink));
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
			}
		}

        private void DrawStatEnergy60(ref Vector2 Cursor, string words, float stat, int Tooltip_ID)
        {
            float amount = 165f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.LightSkyBlue);
            string numbers = GetNumberString(stat);
            if (stat == 0f)
            {
                numbers = "0";
            }
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0f ? Color.LightGreen : Color.LightPink));
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

        private void DrawStatPropulsion60(ref Vector2 Cursor, string words, float stat, int Tooltip_ID)
        {
            float amount = 165f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.DarkSeaGreen);
            string numbers = GetNumberString(stat);
            if (stat == 0f)
            {
                numbers = "0";
            }
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0f ? Color.LightGreen : Color.LightPink));
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

        private void DrawStatOrdnance60(ref Vector2 Cursor, string words, float stat, int Tooltip_ID)
        {
            float amount = 165f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.IndianRed);
            string numbers = GetNumberString(stat);
            if (stat == 0f)
            {
                numbers = "0";
            }
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0f ? Color.LightGreen : Color.LightPink));
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }


		private void DrawStatBad(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
		{
			float amount = 165f;
			if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
			{
				amount = amount + 20f;
			}
			float x = (float)Mouse.GetState().X;
			MouseState state = Mouse.GetState();
			Vector2 MousePos = new Vector2(x, (float)state.Y);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.White);
			Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stat).X);
			base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stat, Cursor, Color.LightPink);
			Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stat.ToString()).X);
			if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
			{
				ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
			}
		}

        private void DrawStatEnergyBad(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            float amount = 165f;
            if (GlobalStats.Config.Language == "German" || GlobalStats.Config.Language == "French" || GlobalStats.Config.Language == "Polish")
            {
                amount = amount + 20f;
            }
            float x = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(x, (float)state.Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, Color.LightSkyBlue);
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(stat).X);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, stat, Cursor, Color.LightPink);
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(stat.ToString()).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(stat.ToString()).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

		private void DrawUI(GameTime gameTime)
        {
            this.EmpireUI.Draw(base.ScreenManager.SpriteBatch);
            this.DrawShipInfoPanel();

            //Defaults based on hull types
            //Freighter hull type defaults to Civilian behaviour when the hull is selected, player has to actively opt to change classification to disable flee/freighter behaviour
            if (this.ActiveHull.Role == "freighter" && this.fml)
            {
                this.CategoryList.ActiveIndex = 1;
                this.fml = false;
            }
            //Scout hull type defaults to Recon behaviour. Not really important, as the 'Recon' tag is going to supplant the notion of having 'Fighter' class hulls automatically be scouts, but it makes things easier when working with scout hulls without existing categorisation.
            else if (this.ActiveHull.Role == "scout" && this.fml)
            {
                this.CategoryList.ActiveIndex = 2;
                this.fml = false;
            }
            //All other hulls default to unclassified.
            else if (this.fml)
            {
                this.CategoryList.ActiveIndex = 0;
                this.fml = false;
            }

            //Loads the Category from the ShipDesign XML of the ship being loaded, and loads this OVER the hull type default, very importantly.
            foreach (Entry e in this.CategoryList.Options)
            {
                if (e.Name == LoadCategory.ToString() && this.fmlevenmore)
                {
                    this.CategoryList.ActiveIndex = e.@value - 1;
                    this.fmlevenmore = false;
                }
            }
            this.CategoryList.Draw(base.ScreenManager.SpriteBatch);
            this.CarrierOnlyBox.Draw(base.ScreenManager);
            string classifTitle = "Behaviour Presets";
            this.ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, classifTitle, classifCursor, Color.Orange);
            float transitionOffset = (float)Math.Pow((double)base.TransitionPosition, 2);
            Rectangle r = this.BlackBar;
            if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int)(transitionOffset * 50f);
            }
            Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, r, Color.Black);
            r = this.bottom_sep;
            if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int)(transitionOffset * 50f);
            }
            Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, r, new Color(77, 55, 25));
            r = this.SearchBar;
            if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int)(transitionOffset * 50f);
            }
            Primitives2D.FillRectangle(base.ScreenManager.SpriteBatch, r, new Color(54, 54, 54));
            if (Fonts.Arial20Bold.MeasureString(this.ActiveHull.Name).X <= (float)(this.SearchBar.Width - 5))
            {
                Vector2 Cursor = new Vector2((float)(this.SearchBar.X + 3), (float)(r.Y + 14 - Fonts.Arial20Bold.LineSpacing / 2));
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.ActiveHull.Name, Cursor, Color.White);
            }
            else
            {
                Vector2 Cursor = new Vector2((float)(this.SearchBar.X + 3), (float)(r.Y + 14 - Fonts.Arial12Bold.LineSpacing / 2));
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, this.ActiveHull.Name, Cursor, Color.White);
            }
            r = this.SaveButton.Rect;
            if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int)(transitionOffset * 50f);
            }
            this.SaveButton.Draw(base.ScreenManager.SpriteBatch, r);
            r = this.LoadButton.Rect;
            if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int)(transitionOffset * 50f);
            }
            this.LoadButton.Draw(base.ScreenManager.SpriteBatch, r);
            r = this.ToggleOverlayButton.Rect;
            if (base.ScreenState == Ship_Game.ScreenState.TransitionOn || base.ScreenState == Ship_Game.ScreenState.TransitionOff)
            {
                r.Y = r.Y + (int)(transitionOffset * 50f);
            }
            this.ToggleOverlayButton.Draw(base.ScreenManager.SpriteBatch, r);
            this.DrawModuleSelection();
            this.DrawHullSelection();
            if (this.ActiveModule != null || this.HighlightedModule != null)
            {
                this.DrawActiveModuleData();
            }
            foreach (ToggleButton button in this.CombatStatusButtons)
            {
                button.Draw(base.ScreenManager);
            }
            if (base.IsActive)
            {
                ToolTip.Draw(base.ScreenManager);
            }
        }

		public override void ExitScreen()
		{
			if (!this.ShipSaved && !this.CheckDesign())
			{
				MessageBoxScreen message = new MessageBoxScreen(Localizer.Token(2121), "Save", "Exit");
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
			MessageBoxScreen message0 = new MessageBoxScreen(Localizer.Token(2137), "Save", "Exit");
			message0.Cancelled += new EventHandler<EventArgs>(this.DoExit);
			message0.Accepted += new EventHandler<EventArgs>(this.SaveChanges);
			base.ScreenManager.AddScreen(message0);
		}

		public void ExitToMenu(string launches)
		{
			this.screenToLaunch = launches;
			if (this.ShipSaved || this.CheckDesign())
			{
				this.LaunchScreen(null, null);
				this.ReallyExit();
				return;
			}
			MessageBoxScreen message = new MessageBoxScreen(Localizer.Token(2121), "Save", "Exit");
			message.Cancelled += new EventHandler<EventArgs>(this.LaunchScreen);
			message.Accepted += new EventHandler<EventArgs>(this.SaveWIPThenLaunchScreen);
			base.ScreenManager.AddScreen(message);
		}

		public float findAngleToTarget(Vector2 origin, Vector2 target)
		{
			float theta;
			float tX = target.X;
			float tY = target.Y;
			float centerX = origin.X;
			float centerY = origin.Y;
			float angle_to_target = 0f;
			if (tX > centerX && tY < centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				theta = theta * 180f / 3.14159274f;
				angle_to_target = 90f - Math.Abs(theta);
			}
			else if (tX > centerX && tY > centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				angle_to_target = 90f + theta * 180f / 3.14159274f;
			}
			else if (tX < centerX && tY > centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				theta = theta * 180f / 3.14159274f;
				angle_to_target = 270f - Math.Abs(theta);
				angle_to_target = -angle_to_target;
			}
			else if (tX < centerX && tY < centerY)
			{
				theta = (float)Math.Atan((double)((tY - centerY) / (tX - centerX)));
				angle_to_target = 270f + theta * 180f / 3.14159274f;
				angle_to_target = -angle_to_target;
			}
			if (tX == centerX && tY < centerY)
			{
				angle_to_target = 0f;
			}
			else if (tX > centerX && tY == centerY)
			{
				angle_to_target = 90f;
			}
			else if (tX == centerX && tY > centerY)
			{
				angle_to_target = 180f;
			}
			else if (tX < centerX && tY == centerY)
			{
				angle_to_target = 270f;
			}
			return angle_to_target;
		}

		private Vector2 findPointFromAngleAndDistance(Vector2 position, float angle, float distance)
		{
			float theta;
			Vector2 TargetPosition = new Vector2(0f, 0f);
			float gamma = angle;
			float D = distance;
			int gammaQuadrant = 0;
			float oppY = 0f;
			float adjX = 0f;
			if (gamma > 360f)
			{
				gamma = gamma - 360f;
			}
			if (gamma < 90f)
			{
				theta = 90f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 1;
			}
			else if (gamma > 90f && gamma < 180f)
			{
				theta = gamma - 90f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 2;
			}
			else if (gamma > 180f && gamma < 270f)
			{
				theta = 270f - gamma;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 3;
			}
			else if (gamma > 270f && gamma < 360f)
			{
				theta = gamma - 270f;
				theta = theta * 3.14159274f / 180f;
				oppY = D * (float)Math.Sin((double)theta);
				adjX = D * (float)Math.Cos((double)theta);
				gammaQuadrant = 4;
			}
			if (gamma == 0f)
			{
				TargetPosition.X = position.X;
				TargetPosition.Y = position.Y - D;
			}
			if (gamma == 90f)
			{
				TargetPosition.X = position.X + D;
				TargetPosition.Y = position.Y;
			}
			if (gamma == 180f)
			{
				TargetPosition.X = position.X;
				TargetPosition.Y = position.Y + D;
			}
			if (gamma == 270f)
			{
				TargetPosition.X = position.X - D;
				TargetPosition.Y = position.Y;
			}
			if (gammaQuadrant == 1)
			{
				TargetPosition.X = position.X + adjX;
				TargetPosition.Y = position.Y - oppY;
			}
			else if (gammaQuadrant == 2)
			{
				TargetPosition.X = position.X + adjX;
				TargetPosition.Y = position.Y + oppY;
			}
			else if (gammaQuadrant == 3)
			{
				TargetPosition.X = position.X - adjX;
				TargetPosition.Y = position.Y + oppY;
			}
			else if (gammaQuadrant == 4)
			{
				TargetPosition.X = position.X - adjX;
				TargetPosition.Y = position.Y - oppY;
			}
			return TargetPosition;
		}

		private Vector2 GeneratePointOnCircle(float angle, Vector2 center, float radius)
		{
			return this.findPointFromAngleAndDistance(center, angle, radius);
		}

		private string GetConduitGraphic(SlotStruct ss)
		{
			bool right = false;
			bool left = false;
			bool up = false;
			bool down = false;
			int numNear = 0;
			foreach (SlotStruct slot in this.Slots)
			{
				if (slot.module == null || slot.module.ModuleType != ShipModuleType.PowerConduit || slot == ss)
				{
					continue;
				}
				int totalDistanceX = Math.Abs(slot.pq.X - ss.pq.X) / 16;
				int totalDistanceY = Math.Abs(slot.pq.Y - ss.pq.Y) / 16;
				if (totalDistanceX == 1 && totalDistanceY == 0)
				{
					if (slot.pq.X <= ss.pq.X)
					{
						right = true;
					}
					else
					{
						left = true;
					}
				}
				if (totalDistanceY != 1 || totalDistanceX != 0)
				{
					continue;
				}
				if (slot.pq.Y <= ss.pq.Y)
				{
					down = true;
				}
				else
				{
					up = true;
				}
			}
			if (left)
			{
				numNear++;
			}
			if (right)
			{
				numNear++;
			}
			if (up)
			{
				numNear++;
			}
			if (down)
			{
				numNear++;
			}
			if (numNear <= 1)
			{
				if (up)
				{
					return "conduit_powerpoint_up";
				}
				if (down)
				{
					return "conduit_powerpoint_down";
				}
				if (left)
				{
					return "conduit_powerpoint_left";
				}
				if (right)
				{
					return "conduit_powerpoint_right";
				}
				return "conduit_intersection";
			}
			if (numNear != 3)
			{
				if (numNear == 4)
				{
					return "conduit_intersection";
				}
				if (numNear == 2)
				{
					if (left && up)
					{
						return "conduit_corner_TL";
					}
					if (left && down)
					{
						return "conduit_corner_BL";
					}
					if (right && up)
					{
						return "conduit_corner_TR";
					}
					if (right && down)
					{
						return "conduit_corner_BR";
					}
					if (up && down)
					{
						return "conduit_straight_vertical";
					}
					if (left && right)
					{
						return "conduit_straight_horizontal";
					}
				}
			}
			else
			{
				if (up && down && left)
				{
					return "conduit_tsection_right";
				}
				if (up && down && right)
				{
					return "conduit_tsection_left";
				}
				if (left && right && down)
				{
					return "conduit_tsection_up";
				}
				if (left && right && up)
				{
					return "conduit_tsection_down";
				}
			}
			return "";
		}

		private static FileInfo[] GetFilesFromDirectory(string DirPath)
		{
			return (new DirectoryInfo(DirPath)).GetFiles("*.*", SearchOption.AllDirectories);
		}

		private void GoHullLeft()
		{
			ShipDesignScreen hullIndex = this;
			hullIndex.HullIndex = hullIndex.HullIndex - 1;
			if (this.HullIndex < 0)
			{
				this.HullIndex = this.AvailableHulls.Count - 1;
			}
			this.ChangeHull(this.AvailableHulls[this.HullIndex]);
		}

		private void GoHullRight()
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
            this.CarrierOnlyBox.HandleInput(input);

            if (this.ActiveModule != null && (this.ActiveModule.InstalledWeapon != null && this.ActiveModule.ModuleType != ShipModuleType.Turret || this.ActiveModule.XSIZE != this.ActiveModule.YSIZE))
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
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Y) && !input.LastKeyboardState.IsKeyDown(Keys.Y) && !GlobalStats.TakingInput)
            {
                AudioManager.PlayCue("echo_affirm");
                this.ExitScreen();
            }
            if (this.close.HandleInput(input))
                this.ExitScreen();
            else if (input.CurrentKeyboardState.IsKeyDown(Keys.Z) && input.LastKeyboardState.IsKeyUp(Keys.Z) && input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
            {
                if (this.DesignStack.Count <= 0)
                    return;
                ShipModule shipModule = this.ActiveModule;
                DesignAction designAction = this.DesignStack.Pop();
                SlotStruct slot1 = new SlotStruct();
                foreach (SlotStruct slot2 in this.Slots)
                {
                    if (slot2.pq == designAction.clickedSS.pq)
                    {
                        this.ClearSlotNoStack(slot2);
                        slot1 = slot2;
                        slot1.facing = designAction.clickedSS.facing;
                    }
                    foreach (SlotStruct slotStruct in designAction.AlteredSlots)
                    {
                        if (slot2.pq == slotStruct.pq)
                        {
                            this.ClearSlotNoStack(slot2);
                            break;
                        }
                    }
                }
                if (designAction.clickedSS.ModuleUID != null)
                {
                    this.ActiveModule = ResourceManager.GetModule(designAction.clickedSS.ModuleUID);
                    this.ResetModuleState();
                    this.InstallModuleNoStack(slot1);
                }
                foreach (SlotStruct slotStruct in designAction.AlteredSlots)
                {
                    foreach (SlotStruct slot2 in this.Slots)
                    {
                        if (slot2.pq == slotStruct.pq && slotStruct.ModuleUID != null)
                        {
                            this.ActiveModule = ResourceManager.GetModule(slotStruct.ModuleUID);
                            this.ResetModuleState();
                            this.InstallModuleNoStack(slot2);
                            slot2.facing = slotStruct.facing;
                            slot2.ModuleUID = slotStruct.ModuleUID;
                        }
                    }
                }
                this.ActiveModule = shipModule;
                this.ResetModuleState();
            }
            else
            {
                if (!HelperFunctions.CheckIntersection(this.ModuleSelectionMenu.Menu, input.CursorPosition) && !HelperFunctions.CheckIntersection(this.HullSelectionRect, input.CursorPosition) && !HelperFunctions.CheckIntersection(this.ChooseFighterSub.Menu, input.CursorPosition))
                {
                    if (input.ScrollOut)
                    {
                        this.TransitionZoom -= 0.1f;
                        if ((double)this.TransitionZoom < 0.300000011920929)
                            this.TransitionZoom = 0.3f;
                        if ((double)this.TransitionZoom > 2.65000009536743)
                            this.TransitionZoom = 2.65f;
                    }
                    if (input.ScrollIn)
                    {
                        this.TransitionZoom += 0.1f;
                        if ((double)this.TransitionZoom < 0.300000011920929)
                            this.TransitionZoom = 0.3f;
                        if ((double)this.TransitionZoom > 2.65000009536743)
                            this.TransitionZoom = 2.65f;
                    }
                }
                if (input.CurrentKeyboardState.IsKeyDown(Keys.OemTilde))
                    input.LastKeyboardState.IsKeyUp(Keys.OemTilde);
                if (this.Debug)
                {
                    if (input.CurrentKeyboardState.IsKeyDown(Keys.Enter) && input.LastKeyboardState.IsKeyUp(Keys.Enter))
                    {
                        foreach (ModuleSlotData moduleSlotData in this.ActiveHull.ModuleSlotList)
                            moduleSlotData.InstalledModuleUID = (string)null;
                        new XmlSerializer(typeof(ShipData)).Serialize((TextWriter)new StreamWriter("Content/Hulls/" + this.ActiveHull.ShipStyle + "/" + this.ActiveHull.Name + ".xml"), (object)this.ActiveHull);
                    }
                    if (input.Right)
                        ++this.operation;
                    if (this.operation > (ShipDesignScreen.SlotModOperation)6)
                        this.operation = ShipDesignScreen.SlotModOperation.Delete;
                }
                this.HoveredModule = (ShipModule)null;
                this.mouseStateCurrent = Mouse.GetState();
                Vector2 vector2 = new Vector2((float)this.mouseStateCurrent.X, (float)this.mouseStateCurrent.Y);
                this.selector = (Selector)null;
                this.EmpireUI.HandleInput(input, (GameScreen)this);
                this.activeModSubMenu.HandleInputNoReset((object)this);
                this.hullSL.HandleInput(input);
                for (int index = this.hullSL.indexAtTop; index < this.hullSL.Copied.Count && index < this.hullSL.indexAtTop + this.hullSL.entriesToDisplay; ++index)
                {
                    ScrollList.Entry e = this.hullSL.Copied[index];
                    if (e.item is ModuleHeader)
                    {
                        if ((e.item as ModuleHeader).HandleInput(input, e))
                            return;
                    }
                    else if (HelperFunctions.CheckIntersection(e.clickRect, vector2))
                    {
                        this.selector = new Selector(this.ScreenManager, e.clickRect);
                        e.clickRectHover = 1;
                        this.selector = new Selector(this.ScreenManager, e.clickRect);
                        if (input.InGameSelect)
                        {
                            AudioManager.PlayCue("sd_ui_accept_alt3");
                            if (!this.ShipSaved && !this.CheckDesign())
                            {
                                MessageBoxScreen messageBoxScreen = new MessageBoxScreen(Localizer.Token(2121), "Save", "No");
                                messageBoxScreen.Accepted += new EventHandler<EventArgs>(this.SaveWIPThenChangeHull);
                                messageBoxScreen.Cancelled += new EventHandler<EventArgs>(this.JustChangeHull);
                                this.changeto = e.item as ShipData;
                                this.ScreenManager.AddScreen((GameScreen)messageBoxScreen);
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
                this.modSel.HandleInput((object)this);
                if (this.ActiveModule != null)
                {
                    if (this.ActiveModule.ModuleType == ShipModuleType.Hangar && !this.ActiveModule.IsTroopBay && !this.ActiveModule.IsSupplyBay)
                    {
                        this.UpdateHangarOptions(this.ActiveModule);
                        this.ChooseFighterSL.HandleInput(input);
                        for (int index = this.ChooseFighterSL.indexAtTop; index < this.ChooseFighterSL.Copied.Count && index < this.ChooseFighterSL.indexAtTop + this.ChooseFighterSL.entriesToDisplay; ++index)
                        {
                            ScrollList.Entry entry = this.ChooseFighterSL.Copied[index];
                            if (HelperFunctions.CheckIntersection(entry.clickRect, vector2))
                            {
                                this.selector = new Selector(this.ScreenManager, entry.clickRect);
                                entry.clickRectHover = 1;
                                this.selector = new Selector(this.ScreenManager, entry.clickRect);
                                if (input.InGameSelect)
                                {
                                    this.ActiveModule.hangarShipUID = (entry.item as Ship).Name;
                                    this.HangarShipUIDLast = (entry.item as Ship).Name;
                                    AudioManager.PlayCue("sd_ui_accept_alt3");
                                    return;
                                }
                            }
                        }
                    }
                }
                else if (this.HighlightedModule != null && this.HighlightedModule.ModuleType == ShipModuleType.Hangar && (!this.HighlightedModule.IsTroopBay && !this.HighlightedModule.IsSupplyBay))
                {
                    this.ChooseFighterSL.HandleInput(input);
                    for (int index = this.ChooseFighterSL.indexAtTop; index < this.ChooseFighterSL.Copied.Count && index < this.ChooseFighterSL.indexAtTop + this.ChooseFighterSL.entriesToDisplay; ++index)
                    {
                        ScrollList.Entry entry = this.ChooseFighterSL.Copied[index];
                        if (HelperFunctions.CheckIntersection(entry.clickRect, vector2))
                        {
                            this.selector = new Selector(this.ScreenManager, entry.clickRect);
                            entry.clickRectHover = 1;
                            this.selector = new Selector(this.ScreenManager, entry.clickRect);
                            if (input.InGameSelect)
                            {
                                this.HighlightedModule.hangarShipUID = (entry.item as Ship).Name;
                                this.HangarShipUIDLast = (entry.item as Ship).Name;
                                AudioManager.PlayCue("sd_ui_accept_alt3");
                                return;
                            }
                        }
                    }
                }
                for (int index = this.weaponSL.indexAtTop; index < this.weaponSL.Copied.Count && index < this.weaponSL.indexAtTop + this.weaponSL.entriesToDisplay; ++index)
                {
                    ScrollList.Entry e = this.weaponSL.Copied[index];
                    if (e.item is ModuleHeader)
                    {
                        if ((e.item as ModuleHeader).HandleInput(input, e))
                            return;
                    }
                    else if (HelperFunctions.CheckIntersection(e.clickRect, vector2))
                    {
                        this.selector = new Selector(this.ScreenManager, e.clickRect);
                        e.clickRectHover = 1;
                        this.selector = new Selector(this.ScreenManager, e.clickRect);
                        if (input.InGameSelect)
                        {
                            this.SetActiveModule(ResourceManager.GetModule((e.item as ShipModule).UID));
                            this.ResetModuleState();
                            return;
                        }
                    }
                    else
                        e.clickRectHover = 0;
                }
                this.weaponSL.HandleInput(input);
                if (HelperFunctions.CheckIntersection(this.HullSelectionRect, input.CursorPosition) && input.CurrentMouseState.LeftButton == ButtonState.Pressed || HelperFunctions.CheckIntersection(this.modSel.Menu, input.CursorPosition) && input.CurrentMouseState.LeftButton == ButtonState.Pressed || HelperFunctions.CheckIntersection(this.activeModSubMenu.Menu, input.CursorPosition) && input.CurrentMouseState.LeftButton == ButtonState.Pressed)
                    return;
                if (HelperFunctions.CheckIntersection(this.modSel.Menu, vector2))
                {
                    if (this.mouseStateCurrent.ScrollWheelValue > this.mouseStatePrevious.ScrollWheelValue && this.weaponSL.indexAtTop > 0)
                        --this.weaponSL.indexAtTop;
                    if (this.mouseStateCurrent.ScrollWheelValue < this.mouseStatePrevious.ScrollWheelValue && this.weaponSL.indexAtTop + this.weaponSL.entriesToDisplay < this.weaponSL.Entries.Count)
                        ++this.weaponSL.indexAtTop;
                }
                if (HelperFunctions.CheckIntersection(this.ArcsButton.R, input.CursorPosition))
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
                if (input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Released)
                {
                    this.StartDragPos = input.CursorPosition;
                    this.cameraVelocity.X = 0.0f;
                    this.cameraVelocity.Y = 0.0f;
                }
                if (input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Pressed)
                {
                    float num1 = input.CursorPosition.X - this.StartDragPos.X;
                    float num2 = input.CursorPosition.Y - this.StartDragPos.Y;
                    this.camera._pos += new Vector2(-num1, -num2);
                    this.StartDragPos = input.CursorPosition;
                    this.cameraPosition.X += -num1;
                    this.cameraPosition.Y += -num2;
                }
                else
                {
                    this.cameraVelocity.X = 0.0f;
                    this.cameraVelocity.Y = 0.0f;
                }
                this.cameraVelocity.X = MathHelper.Clamp(this.cameraVelocity.X, -10f, 10f);
                this.cameraVelocity.Y = MathHelper.Clamp(this.cameraVelocity.Y, -10f, 10f);
                if (input.Escaped)
                    this.ExitScreen();
                if (this.ToggleOverlay)
                {
                    foreach (SlotStruct slotStruct in this.Slots)
                    {
                        Vector2 spaceFromWorldSpace = this.camera.GetScreenSpaceFromWorldSpace(new Vector2((float)slotStruct.pq.enclosingRect.X, (float)slotStruct.pq.enclosingRect.Y));
                        if (HelperFunctions.CheckIntersection(new Rectangle((int)spaceFromWorldSpace.X, (int)spaceFromWorldSpace.Y, (int)(16.0 * (double)this.camera.Zoom), (int)(16.0 * (double)this.camera.Zoom)), vector2))
                        {
                            if (slotStruct.isDummy && slotStruct.parent.module != null)
                                this.HoveredModule = slotStruct.parent.module;
                            else if (slotStruct.module != null)
                                this.HoveredModule = slotStruct.module;
                            if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
                            {
                                AudioManager.GetCue("simple_beep").Play();
                                if (this.Debug)
                                {
                                    this.DebugAlterSlot(slotStruct.slotReference.Position, this.operation);
                                    return;
                                }
                                else if (slotStruct.isDummy && slotStruct.parent.module != null)
                                    this.HighlightedModule = slotStruct.parent.module;
                                else if (slotStruct.module != null)
                                    this.HighlightedModule = slotStruct.module;
                            }
                        }
                    }
                }
                if (HelperFunctions.CheckIntersection(this.upArrow, vector2) && this.mouseStateCurrent.LeftButton == ButtonState.Released && (this.mouseStatePrevious.LeftButton == ButtonState.Pressed && this.scrollPosition > 0))
                {
                    --this.scrollPosition;
                    AudioManager.GetCue("blip_click").Play();
                    foreach (ModuleButton moduleButton in this.ModuleButtons)
                        moduleButton.moduleRect.Y += 128;
                }
                if (HelperFunctions.CheckIntersection(this.downArrow, vector2) && this.mouseStateCurrent.LeftButton == ButtonState.Released && this.mouseStatePrevious.LeftButton == ButtonState.Pressed)
                {
                    ++this.scrollPosition;
                    AudioManager.GetCue("blip_click").Play();
                    foreach (ModuleButton moduleButton in this.ModuleButtons)
                        moduleButton.moduleRect.Y -= 128;
                }
                if (HelperFunctions.CheckIntersection(this.ModuleSelectionArea, vector2))
                {
                    if (input.ScrollIn && this.scrollPosition > 0)
                    {
                        --this.scrollPosition;
                        AudioManager.GetCue("blip_click").Play();
                        foreach (ModuleButton moduleButton in this.ModuleButtons)
                            moduleButton.moduleRect.Y += 128;
                    }
                    if (input.ScrollOut)
                    {
                        ++this.scrollPosition;
                        AudioManager.GetCue("blip_click").Play();
                        foreach (ModuleButton moduleButton in this.ModuleButtons)
                            moduleButton.moduleRect.Y -= 128;
                    }
                }
                if (this.mouseStateCurrent.RightButton == ButtonState.Released && this.mouseStatePrevious.RightButton == ButtonState.Pressed)
                {
                    //this should actually clear slots
                    this.ActiveModule = (ShipModule)null;
                    foreach (SlotStruct parent in this.Slots)
                    {
                        parent.ShowInvalid = false;
                        parent.ShowValid = false;
                        Vector2 spaceFromWorldSpace = this.camera.GetScreenSpaceFromWorldSpace(new Vector2((float)parent.pq.enclosingRect.X, (float)parent.pq.enclosingRect.Y));
                        Rectangle rect = new Rectangle((int)spaceFromWorldSpace.X, (int)spaceFromWorldSpace.Y, (int)(16.0 * (double)this.camera.Zoom), (int)(16.0 * (double)this.camera.Zoom));
                        if ((parent.module != null || parent.isDummy) && HelperFunctions.CheckIntersection(rect, vector2)) //if clicked at this slot
                        {
                            DesignAction designAction = new DesignAction();
                            designAction.clickedSS = new SlotStruct();
                            designAction.clickedSS.pq = parent.isDummy ? parent.parent.pq : parent.pq;
                            designAction.clickedSS.Restrictions = parent.Restrictions;
                            designAction.clickedSS.facing = parent.module != null ? parent.module.facing : 0.0f;
                            designAction.clickedSS.ModuleUID = parent.isDummy ? parent.parent.ModuleUID : parent.ModuleUID;
                            designAction.clickedSS.module = parent.module;
                            designAction.clickedSS.slotReference = parent.isDummy ? parent.parent.slotReference : parent.slotReference;
                            this.DesignStack.Push(designAction);
                            AudioManager.GetCue("sub_bass_whoosh").Play();
                            if (parent.isDummy)
                                this.ClearParentSlot(parent.parent);
                            else
                                this.ClearParentSlot(parent);
                            this.RecalculatePower();
                        }
                    }
                }
                foreach (ModuleButton moduleButton in this.ModuleButtons)
                {
                    if (HelperFunctions.CheckIntersection(this.ModuleSelectionArea, new Vector2((float)(moduleButton.moduleRect.X + 30), (float)(moduleButton.moduleRect.Y + 30))))
                    {
                        if (HelperFunctions.CheckIntersection(moduleButton.moduleRect, vector2))
                        {
                            if (input.InGameSelect)
                                this.SetActiveModule(ResourceManager.GetModule(moduleButton.ModuleUID));
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
                        Vector2 spaceFromWorldSpace = this.camera.GetScreenSpaceFromWorldSpace(new Vector2((float)slot.pq.enclosingRect.X, (float)slot.pq.enclosingRect.Y));
                        if (HelperFunctions.CheckIntersection(new Rectangle((int)spaceFromWorldSpace.X, (int)spaceFromWorldSpace.Y, (int)(16.0 * (double)this.camera.Zoom), (int)(16.0 * (double)this.camera.Zoom)), vector2))
                        {
                            AudioManager.GetCue("sub_bass_mouseover").Play();
                            this.InstallModule(slot);
                        }
                    }
                }
                foreach (SlotStruct slotStruct in this.Slots)
                {
                    if (slotStruct.ModuleUID != null && this.HighlightedModule != null && (slotStruct.module == this.HighlightedModule && (double)slotStruct.module.FieldOfFire != 0.0) && slotStruct.module.ModuleType == ShipModuleType.Turret)
                    {
                        float num1 = slotStruct.module.FieldOfFire / 2f;
                        Vector2 spaceFromWorldSpace = this.camera.GetScreenSpaceFromWorldSpace(new Vector2((float)(slotStruct.pq.enclosingRect.X + 16 * (int)slotStruct.module.XSIZE / 2), (float)(slotStruct.pq.enclosingRect.Y + 16 * (int)slotStruct.module.YSIZE / 2)));
                        float num2 = Math.Abs(this.findAngleToTarget(spaceFromWorldSpace, vector2));
                        float num3 = this.HighlightedModule.facing;
                        float num4 = Math.Abs(num2 - num3);
                        if ((double)num4 > (double)num1)
                        {
                            if ((double)num2 > 180.0)
                                num2 = (float)(-1.0 * (360.0 - (double)num2));
                            if ((double)num3 > 180.0)
                                num3 = (float)(-1.0 * (360.0 - (double)num3));
                            num4 = Math.Abs(num2 - num3);
                        }
                        if ((double)num4 < (double)num1 && (double)Vector2.Distance(spaceFromWorldSpace, vector2) < 300.0 && (this.mouseStateCurrent.LeftButton == ButtonState.Pressed && this.mouseStatePrevious.LeftButton == ButtonState.Pressed))
                            this.HighlightedModule.facing = Math.Abs(this.findAngleToTarget(spaceFromWorldSpace, vector2));
                    }
                }
                foreach (UIButton uiButton in this.Buttons)
                {
                    if (HelperFunctions.CheckIntersection(uiButton.Rect, vector2))
                    {
                        uiButton.State = UIButton.PressState.Hover;
                        if (this.mouseStateCurrent.LeftButton == ButtonState.Pressed && this.mouseStatePrevious.LeftButton == ButtonState.Pressed)
                            uiButton.State = UIButton.PressState.Pressed;
                        if (this.mouseStateCurrent.LeftButton == ButtonState.Released && this.mouseStatePrevious.LeftButton == ButtonState.Pressed)
                        {
                            switch (uiButton.Launches)
                            {
                                case "Toggle Overlay":
                                    AudioManager.PlayCue("blip_click");
                                    this.ToggleOverlay = !this.ToggleOverlay;
                                    continue;
                                case "Save As...":
                                    if (this.CheckDesign())
                                    {
                                        this.ScreenManager.AddScreen((GameScreen)new DesignManager(this, this.ActiveHull.Name));
                                        continue;
                                    }
                                    else
                                    {
                                        AudioManager.PlayCue("UI_Misc20");
                                        this.ScreenManager.AddScreen((GameScreen)new MessageBoxScreen(Localizer.Token(2049)));
                                        continue;
                                    }
                                case "Load":
                                    this.ScreenManager.AddScreen((GameScreen)new LoadDesigns(this));
                                    continue;
                                default:
                                    continue;
                            }
                        }
                    }
                    else
                        uiButton.State = UIButton.PressState.Normal;
                }
                if (this.ActiveHull != null)
                {
                    foreach (ToggleButton toggleButton in this.CombatStatusButtons)
                    {
                        if (HelperFunctions.CheckIntersection(toggleButton.r, input.CursorPosition))
                        {
                            if (toggleButton.HasToolTip)
                                ToolTip.CreateTooltip(toggleButton.WhichToolTip, this.ScreenManager);
                            if (input.InGameSelect)
                            {
                                AudioManager.PlayCue("sd_ui_accept_alt3");
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
                            default:
                                continue;
                        }
                    }
                }
                this.mouseStatePrevious = this.mouseStateCurrent;
                base.HandleInput(input);
            }
        }

        
        private void InstallModuleOrig(SlotStruct slot)
        {
            int num = 0;
            for (int index1 = 0; index1 < (int)this.ActiveModule.YSIZE; ++index1)
            {
                for (int index2 = 0; index2 < (int)this.ActiveModule.XSIZE; ++index2)
                {
                    foreach (SlotStruct slotStruct in this.Slots)
                    {
                        if (slotStruct.pq.Y == slot.pq.Y + 16 * index1 && slotStruct.pq.X == slot.pq.X + 16 * index2 && slotStruct.ShowValid)
                        {
                            if (slotStruct.module == null && slotStruct.parent == null)
                            {   //make sure they are actually empty!
                                ++num;
                            }
                        }
                            
                    }
                }
            }
            if (num == (int)this.ActiveModule.XSIZE * (int)this.ActiveModule.YSIZE)
            {
                DesignAction designAction = new DesignAction();
                designAction.clickedSS = new SlotStruct();
                designAction.clickedSS.pq = slot.pq;
                designAction.clickedSS.Restrictions = slot.Restrictions;
                designAction.clickedSS.facing = slot.module != null ? slot.module.facing : 0.0f;
                designAction.clickedSS.ModuleUID = slot.ModuleUID;
                designAction.clickedSS.module = slot.module;
                designAction.clickedSS.tex = slot.tex;
                designAction.clickedSS.slotReference = slot.slotReference;
                designAction.clickedSS.state = slot.state;
                this.DesignStack.Push(designAction);
                this.ClearSlot(slot);
                this.ClearDestinationSlots(slot);
                slot.ModuleUID = this.ActiveModule.UID;
                slot.module = this.ActiveModule;
                slot.module.SetAttributesNoParent();
                slot.state = this.ActiveModState;
                slot.module.facing = this.ActiveModule.facing;
                slot.tex = ResourceManager.TextureDict[ResourceManager.ShipModulesDict[this.ActiveModule.UID].IconTexturePath];
                for (int index1 = 0; index1 < (int)this.ActiveModule.YSIZE; ++index1)
                {
                    for (int index2 = 0; index2 < (int)this.ActiveModule.XSIZE; ++index2)
                    {
                        if (!(index2 == 0 & index1 == 0))
                        {
                            foreach (SlotStruct slotStruct in this.Slots)
                            {
                                if (slotStruct.pq.Y == slot.pq.Y + 16 * index1 && slotStruct.pq.X == slot.pq.X + 16 * index2)
                                {
                                    slot.facing = 0.0f;
                                    slotStruct.facing = 0.0f;
                                    slotStruct.ModuleUID = (string)null;
                                    slotStruct.isDummy = true;
                                    slotStruct.tex = (Texture2D)null;
                                    slotStruct.module = (ShipModule)null;
                                    slotStruct.parent = slot;
                                }
                            }
                        }
                    }
                }
                this.RecalculatePower();
                this.ShipSaved = false;
                if (this.ActiveModule.ModuleType != ShipModuleType.Hangar)
                {
                    this.ActiveModule = Ship_Game.ResourceManager.GetModule(this.ActiveModule.UID);
                }
                this.ChangeModuleState(this.ActiveModState);
            }
            else
                this.PlayNegativeSound();
        }

        private void InstallModule(SlotStruct slot)
        {
            int num = 0;
            //Checks if active module can fit
            for (int index1 = 0; index1 < (int)this.ActiveModule.YSIZE; ++index1)
            {
                for (int index2 = 0; index2 < (int)this.ActiveModule.XSIZE; ++index2)
                {
                    foreach (SlotStruct slotStruct in this.Slots)
                    {
                        if (slotStruct.pq.Y == slot.pq.Y + 16 * index1 && slotStruct.pq.X == slot.pq.X + 16 * index2 && slotStruct.ShowValid)
                            ++num;
                    }
                }
            }
            //if module fits
            if (num == (int)this.ActiveModule.XSIZE * (int)this.ActiveModule.YSIZE)
            {
                DesignAction designAction = new DesignAction();
                designAction.clickedSS = new SlotStruct();
                designAction.clickedSS.pq = slot.pq;
                designAction.clickedSS.Restrictions = slot.Restrictions;
                designAction.clickedSS.facing = slot.module != null ? slot.module.facing : 0.0f;
                designAction.clickedSS.ModuleUID = slot.ModuleUID;
                designAction.clickedSS.module = slot.module;
                designAction.clickedSS.tex = slot.tex;
                designAction.clickedSS.slotReference = slot.slotReference;
                designAction.clickedSS.state = slot.state;
                this.DesignStack.Push(designAction);
                this.ClearSlot(slot);
                this.ClearDestinationSlots(slot);
                slot.ModuleUID = this.ActiveModule.UID;
                slot.module = this.ActiveModule;
                slot.module.SetAttributesNoParent();
                slot.state = this.ActiveModState;
                slot.module.hangarShipUID = this.ActiveModule.hangarShipUID;
                slot.module.facing = this.ActiveModule.facing;
                slot.tex = Ship_Game.ResourceManager.TextureDict[Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].IconTexturePath];
                for (int index1 = 0; index1 < (int)this.ActiveModule.YSIZE; ++index1)
                {
                    for (int index2 = 0; index2 < (int)this.ActiveModule.XSIZE; ++index2)
                    {
                        if (!(index2 == 0 & index1 == 0))
                        {
                            foreach (SlotStruct slotStruct in this.Slots)
                            {
                                if (slotStruct.pq.Y == slot.pq.Y + 16 * index1 && slotStruct.pq.X == slot.pq.X + 16 * index2)
                                {
                                    slot.facing = 0.0f;
                                    slotStruct.facing = 0.0f;
                                    slotStruct.ModuleUID = (string)null;
                                    slotStruct.isDummy = true;
                                    slotStruct.tex = (Texture2D)null;
                                    slotStruct.module = (ShipModule)null;
                                    slotStruct.parent = slot;
                                }
                            }
                        }
                    }
                }
                this.RecalculatePower();
                this.ShipSaved = false;
                if (this.ActiveModule.ModuleType != ShipModuleType.Hangar)
                {
                    this.ActiveModule = Ship_Game.ResourceManager.GetModule(this.ActiveModule.UID);
                }
                this.ChangeModuleState(this.ActiveModState);
            }
            else
                this.PlayNegativeSound();
        }

        private void InstallModuleFromLoad(SlotStruct slot)
        {
            int num = 0;
            for (int index1 = 0; index1 < (int)this.ActiveModule.YSIZE; ++index1)
            {
                for (int index2 = 0; index2 < (int)this.ActiveModule.XSIZE; ++index2)
                {
                    foreach (SlotStruct slotStruct in this.Slots)
                    {
                        if (slotStruct.pq.Y == slot.pq.Y + 16 * index1 && slotStruct.pq.X == slot.pq.X + 16 * index2)
                            ++num;
                    }
                }
            }
            if (num == (int)this.ActiveModule.XSIZE * (int)this.ActiveModule.YSIZE)
            {
                ShipDesignScreen.ActiveModuleState activeModuleState = slot.state;
                this.ClearSlot(slot);
                this.ClearDestinationSlotsNoStack(slot);
                slot.ModuleUID = this.ActiveModule.UID;
                slot.module = this.ActiveModule;
                slot.module.SetAttributesNoParent();
                slot.state = activeModuleState;
                //slot.module.hangarShipUID = this.ActiveModule.hangarShipUID;
                slot.module.facing = slot.facing;
                slot.tex = ResourceManager.TextureDict[ResourceManager.ShipModulesDict[this.ActiveModule.UID].IconTexturePath];
                for (int index1 = 0; index1 < (int)this.ActiveModule.YSIZE; ++index1)
                {
                    for (int index2 = 0; index2 < (int)this.ActiveModule.XSIZE; ++index2)
                    {
                        if (!(index2 == 0 & index1 == 0))
                        {
                            foreach (SlotStruct slotStruct in this.Slots)
                            {
                                if (slotStruct.pq.Y == slot.pq.Y + 16 * index1 && slotStruct.pq.X == slot.pq.X + 16 * index2)
                                {
                                    slotStruct.ModuleUID = (string)null;
                                    slotStruct.isDummy = true;
                                    slotStruct.tex = (Texture2D)null;
                                    slotStruct.module = (ShipModule)null;
                                    slotStruct.parent = slot;
                                }
                            }
                        }
                    }
                }
                this.RecalculatePower();
            }
            else
                this.PlayNegativeSound();
        }

        private void InstallModuleNoStack(SlotStruct slot)
        {
            //System.Diagnostics.Debug.Assert(false);
            //looks like this function is not actually used, see if anyone manages to trigger this
            int num = 0;    //check for sufficient slots
            for (int index1 = 0; index1 < (int)this.ActiveModule.YSIZE; ++index1)
            {
                for (int index2 = 0; index2 < (int)this.ActiveModule.XSIZE; ++index2)
                {
                    foreach (SlotStruct slotStruct in this.Slots)
                    {   //checks if this slot is within xsize and ysize
                        if (slotStruct.pq.Y == slot.pq.Y + 16 * index1 && slotStruct.pq.X == slot.pq.X + 16 * index2)
                        {
                            if(slotStruct.module == null && slotStruct.parent == null){   //make sure they are actually empty!
                                ++num;
                            }
                        }                            
                    }
                }
            }
            if (num == (int)this.ActiveModule.XSIZE * (int)this.ActiveModule.YSIZE)
            {   //set module to this slot
                this.ClearSlotNoStack(slot);
                this.ClearDestinationSlotsNoStack(slot);
                slot.ModuleUID = this.ActiveModule.UID;
                slot.module = this.ActiveModule;
                slot.module.SetAttributesNoParent();
                slot.state = this.ActiveModState;
                slot.module.hangarShipUID = this.ActiveModule.hangarShipUID;
                slot.module.facing = this.ActiveModule.facing;
                slot.tex = ResourceManager.TextureDict[ResourceManager.ShipModulesDict[this.ActiveModule.UID].IconTexturePath];
                //set other slots occupied by the module to use this slot as parent
                for (int index1 = 0; index1 < (int)this.ActiveModule.YSIZE; ++index1)
                {
                    for (int index2 = 0; index2 < (int)this.ActiveModule.XSIZE; ++index2)
                    {
                        if (!(index2 == 0 && index1 == 0))  //if not the parent slot
                        {
                            foreach (SlotStruct slotStruct in this.Slots)
                            {
                                if (slotStruct.pq.Y == slot.pq.Y + 16 * index1 && slotStruct.pq.X == slot.pq.X + 16 * index2)
                                {
                                    slot.facing = 0.0f;
                                    slotStruct.facing = 0.0f;
                                    slotStruct.ModuleUID = (string)null;
                                    slotStruct.isDummy = true;
                                    slotStruct.tex = (Texture2D)null;
                                    slotStruct.module = (ShipModule)null;
                                    slotStruct.parent = slot;
                                }
                            }
                        }
                    }
                }
                this.RecalculatePower();
                this.ShipSaved = false;
                if (this.ActiveModule.ModuleType != ShipModuleType.Hangar)
                {
                    this.ActiveModule = Ship_Game.ResourceManager.GetModule(this.ActiveModule.UID);
                }
                //grabs a fresh copy of the same module type to cursor 
                this.ChangeModuleState(this.ActiveModState);
                //set rotation for new module at cursor
            }
            else
                this.PlayNegativeSound();
        }

		private void JustChangeHull(object sender, EventArgs e)
		{
			this.ShipSaved = true;
			this.ChangeHull(this.changeto);
		}

		private void LaunchScreen(object sender, EventArgs e)
		{
			string str = this.screenToLaunch;
			string str1 = str;
			if (str != null)
			{
				if (str1 == "Research")
				{
					AudioManager.PlayCue("echo_affirm");
					base.ScreenManager.AddScreen(new ResearchScreenNew(this.EmpireUI));
				}
				else if (str1 == "Budget")
				{
					AudioManager.PlayCue("echo_affirm");
					base.ScreenManager.AddScreen(new BudgetScreen(ShipDesignScreen.screen));
				}
			}
			string str2 = this.screenToLaunch;
			string str3 = str2;
			if (str2 != null)
			{
				if (str3 == "Main Menu")
				{
					AudioManager.PlayCue("echo_affirm");
					ShipDesignScreen.screen.ScreenManager.AddScreen(new GameplayMMScreen(ShipDesignScreen.screen));
				}
				else if (str3 == "Shipyard")
				{
					AudioManager.PlayCue("echo_affirm");
				}
				else if (str3 == "Empire")
				{
					ShipDesignScreen.screen.ScreenManager.AddScreen(new EmpireScreen(ShipDesignScreen.screen.ScreenManager, this.EmpireUI));
					AudioManager.PlayCue("echo_affirm");
				}
				else if (str3 == "Diplomacy")
				{
					ShipDesignScreen.screen.ScreenManager.AddScreen(new MainDiplomacyScreen(ShipDesignScreen.screen));
					AudioManager.PlayCue("echo_affirm");
				}
				else if (str3 == "?")
				{
					AudioManager.PlayCue("sd_ui_tactical_pause");
					InGameWiki wiki = new InGameWiki(new Rectangle(0, 0, 750, 600))
					{
						TitleText = "StarDrive Help",
						MiddleText = "This help menu contains information on all of the gameplay systems contained in StarDrive. You can also watch one of several tutorial videos for a developer-guided introduction to StarDrive."
					};
				}
			}
			this.ReallyExit();
		}

		public override void LoadContent()
		{
			LightRig rig = base.ScreenManager.Content.Load<LightRig>("example/ShipyardLightrig");
			lock (GlobalStats.ObjectManagerLocker)
			{
				base.ScreenManager.inter.LightManager.Clear();
				base.ScreenManager.inter.LightManager.Submit(rig);
			}
			if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth <= 1280 || base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight <= 768)
			{
				this.LowRes = true;
			}
			Rectangle leftRect = new Rectangle(5, 45, 405, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 45 - (int)(0.4f * (float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight) + 10);
			this.ModuleSelectionMenu = new Menu1(base.ScreenManager, leftRect);
			Rectangle modSelR = new Rectangle(0, (this.LowRes ? 45 : 100), 305, (this.LowRes ? 350 : 400));
			this.modSel = new Submenu(base.ScreenManager, modSelR, true);
			this.modSel.AddTab("Wpn");
			this.modSel.AddTab("Pwr");
			this.modSel.AddTab("Def");
			this.modSel.AddTab("Spc");
			this.weaponSL = new ScrollList(this.modSel);
			Vector2 Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 175), 80f);
			Rectangle active = new Rectangle(modSelR.X, modSelR.Y + modSelR.Height + 15, modSelR.Width, 300);
			this.activeModWindow = new Menu1(base.ScreenManager, active);
			Rectangle acsub = new Rectangle(active.X, modSelR.Y + modSelR.Height + 15, 305, 320);
			if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight > 760)
			{
				acsub.Height = acsub.Height + 120;
			}
			this.activeModSubMenu = new Submenu(base.ScreenManager, acsub);
			this.activeModSubMenu.AddTab("Active Module");
			this.choosefighterrect = new Rectangle(acsub.X + acsub.Width + 5, acsub.Y, 240, 270);
			if (this.choosefighterrect.Y + this.choosefighterrect.Height > base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
			{
				int diff = this.choosefighterrect.Y + this.choosefighterrect.Height - base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
				this.choosefighterrect.Height = this.choosefighterrect.Height - (diff + 10);
			}
			this.choosefighterrect.Height = acsub.Height;
			this.ChooseFighterSub = new Submenu(base.ScreenManager, this.choosefighterrect);
			this.ChooseFighterSub.AddTab("Choose Fighter");
			this.ChooseFighterSL = new ScrollList(this.ChooseFighterSub, 40);
			foreach (KeyValuePair<string, bool> hull in EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetHDict())
			{
				if (!hull.Value)
				{
					continue;
				}
				this.AvailableHulls.Add(Ship_Game.ResourceManager.HullsDict[hull.Key]);
			}
			PrimitiveQuad.graphicsDevice = base.ScreenManager.GraphicsDevice;
			float width = (float)base.ScreenManager.GraphicsDevice.Viewport.Width;
			Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
			float aspectRatio = width / (float)viewport.Height;
			this.offset = new Vector2();
			Viewport viewport1 = base.ScreenManager.GraphicsDevice.Viewport;
			this.offset.X = (float)(viewport1.Width / 2 - 256);
			Viewport viewport2 = base.ScreenManager.GraphicsDevice.Viewport;
			this.offset.Y = (float)(viewport2.Height / 2 - 256);
			this.camera = new Camera2d();
			Camera2d vector2 = this.camera;
			Viewport viewport3 = base.ScreenManager.GraphicsDevice.Viewport;
			float single = (float)viewport3.Width / 2f;
			Viewport viewport4 = base.ScreenManager.GraphicsDevice.Viewport;
			vector2.Pos = new Vector2(single, (float)viewport4.Height / 2f);
			Vector3 camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
			this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f))) * Matrix.CreateRotationX(MathHelper.ToRadians(0f))) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
			this.projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 20000f);
			this.ChangeHull(this.AvailableHulls[0]);
			lock (GlobalStats.ObjectManagerLocker)
			{
				if (!this.ActiveHull.Animated)
				{
					this.ActiveModel = base.ScreenManager.Content.Load<Model>(this.ActiveHull.ModelPath);
					this.CreateSOFromHull();
				}
				else
				{
					base.ScreenManager.inter.ObjectManager.Remove(this.shipSO);
					SkinnedModel sm = Ship_Game.ResourceManager.GetSkinnedModel(this.ActiveHull.ModelPath);
					this.shipSO = new SceneObject(sm.Model)
					{
						ObjectType = ObjectType.Dynamic,
						World = this.worldMatrix
					};
					base.ScreenManager.inter.ObjectManager.Submit(this.shipSO);
					this.SetupSlots();
				}
			}
			foreach (ModuleSlotData slot in this.ActiveHull.ModuleSlotList)
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
			Viewport viewport5 = base.ScreenManager.GraphicsDevice.Viewport;
			Vector3 pScreenSpace = viewport5.Project(Vector3.Zero, this.projection, this.view, Matrix.Identity);
			Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
			Vector2 radialPos = this.GeneratePointOnCircle(90f, Vector2.Zero, xDistance);
			Viewport viewport6 = base.ScreenManager.GraphicsDevice.Viewport;
			Vector3 insetRadialPos = viewport6.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
			Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
			float Radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
			if (Radius >= xDistance)
			{
				while (Radius > xDistance)
				{
					camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
					this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f))) * Matrix.CreateRotationX(MathHelper.ToRadians(0f))) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
					bs = this.shipSO.WorldBoundingSphere;
					Viewport viewport7 = base.ScreenManager.GraphicsDevice.Viewport;
					pScreenSpace = viewport7.Project(Vector3.Zero, this.projection, this.view, Matrix.Identity);
					pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
					radialPos = this.GeneratePointOnCircle(90f, Vector2.Zero, xDistance);
					Viewport viewport8 = base.ScreenManager.GraphicsDevice.Viewport;
					insetRadialPos = viewport8.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
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
					this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f))) * Matrix.CreateRotationX(MathHelper.ToRadians(0f))) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
					bs = this.shipSO.WorldBoundingSphere;
					Viewport viewport9 = base.ScreenManager.GraphicsDevice.Viewport;
					pScreenSpace = viewport9.Project(Vector3.Zero, this.projection, this.view, Matrix.Identity);
					pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
					radialPos = this.GeneratePointOnCircle(90f, Vector2.Zero, xDistance);
					Viewport viewport10 = base.ScreenManager.GraphicsDevice.Viewport;
					insetRadialPos = viewport10.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
					insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
					Radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
					this.cameraPosition.Z = this.cameraPosition.Z - 1f;
				}
			}
			this.BlackBar = new Rectangle(0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 70, 3000, 70);
			this.SideBar = new Rectangle(0, 0, 280, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);
			Rectangle w = new Rectangle(20, this.modSel.Menu.Y - 10, 32, 32);
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
			this.SelectedCatTextPos = new Vector2(20f, (float)(w.Y - 25 - Fonts.Arial20Bold.LineSpacing / 2));
			this.SearchBar = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 585, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 47, 210, 25);
			Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 370), (float)(modSelR.Y + modSelR.Height + 408));
			Vector2 OrdersBarPos = new Vector2(Cursor.X - 60f, (float)((int)Cursor.Y + 10));
			ToggleButton AttackRuns = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_headon");
			this.CombatStatusButtons.Add(AttackRuns);
			AttackRuns.Action = "attack";
			AttackRuns.HasToolTip = true;
			AttackRuns.WhichToolTip = 1;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton Artillery = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_aft");
			this.CombatStatusButtons.Add(Artillery);
			Artillery.Action = "arty";
			Artillery.HasToolTip = true;
			Artillery.WhichToolTip = 2;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton HoldPos = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_x");
			this.CombatStatusButtons.Add(HoldPos);
			HoldPos.Action = "hold";
			HoldPos.HasToolTip = true;
			HoldPos.WhichToolTip = 65;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton OrbitLeft = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_left");
			this.CombatStatusButtons.Add(OrbitLeft);
			OrbitLeft.Action = "orbit_left";
			OrbitLeft.HasToolTip = true;
			OrbitLeft.WhichToolTip = 3;
            OrdersBarPos.Y = OrdersBarPos.Y + 29f;

            ToggleButton BroadsideLeft = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_bleft");
            this.CombatStatusButtons.Add(BroadsideLeft);
            BroadsideLeft.Action = "broadside_left";
            BroadsideLeft.HasToolTip = true;
            BroadsideLeft.WhichToolTip = 159;
            OrdersBarPos.Y = OrdersBarPos.Y - 29f;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton OrbitRight = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_right");
			this.CombatStatusButtons.Add(OrbitRight);
			OrbitRight.Action = "orbit_right";
			OrbitRight.HasToolTip = true;
			OrbitRight.WhichToolTip = 4;
            OrdersBarPos.Y = OrdersBarPos.Y + 29f;

            ToggleButton BroadsideRight = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_bright");
            this.CombatStatusButtons.Add(BroadsideRight);
            BroadsideRight.Action = "broadside_right";
            BroadsideRight.HasToolTip = true;
            BroadsideRight.WhichToolTip = 160;
            OrdersBarPos.Y = OrdersBarPos.Y - 29f;
			OrdersBarPos.X = OrdersBarPos.X + 29f;
			ToggleButton Evade = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_stop");
			this.CombatStatusButtons.Add(Evade);
			Evade.Action = "evade";
			Evade.HasToolTip = true;
			Evade.WhichToolTip = 6;

            Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 150), (float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 47);      
   
			this.SaveButton = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Width, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Height),
				NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"],
				HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_hover"],
				PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_pressed"],
				Text = Localizer.Token(105),
				Launches = "Save As..."
			};
			this.Buttons.Add(this.SaveButton);
			this.LoadButton = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X - 78, (int)Cursor.Y, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Width, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
				NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"],
				HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_hover"],
				PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px_pressed"],
				Text = Localizer.Token(8),
				Launches = "Load"
			};
			this.Buttons.Add(this.LoadButton);
			this.ToggleOverlayButton = new UIButton()
			{
				Rect = new Rectangle(this.LoadButton.Rect.X - 140, (int)Cursor.Y, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Width, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_68px"].Height),
				NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"],
				HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_hover"],
				PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px_pressed"],
				Launches = "Toggle Overlay",
				Text = Localizer.Token(106)
			};
			this.Buttons.Add(this.ToggleOverlayButton);
			this.bottom_sep = new Rectangle(this.BlackBar.X, this.BlackBar.Y, this.BlackBar.Width, 1);
			this.HullSelectionRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 285, (this.LowRes ? 45 : 100), 280, (this.LowRes ? 350 : 400));
			this.hullSelectionSub = new Submenu(base.ScreenManager, this.HullSelectionRect, true);
			this.weaponSL = new ScrollList(this.modSel);
			this.hullSelectionSub.AddTab(Localizer.Token(107));
			this.hullSL = new ScrollList(this.hullSelectionSub);
			List<string> Categories = new List<string>();
			foreach (KeyValuePair<string, ShipData> hull in Ship_Game.ResourceManager.HullsDict)
			{
				if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetHDict()[hull.Key])
				{
					continue;
				}
                string cat = Localizer.GetRole(hull.Value.Role, EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty));
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
				this.hullSL.AddItem(type);
			}
			foreach (ScrollList.Entry e in this.hullSL.Entries)
			{
				foreach (KeyValuePair<string, ShipData> hull in Ship_Game.ResourceManager.HullsDict)
				{
                    if (!EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).GetHDict()[hull.Key] || !((e.item as ModuleHeader).Text == Localizer.GetRole(hull.Value.Role, EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty))))
					{
						continue;
					}
					e.AddItem(hull.Value);
				}
			}
			Rectangle ShipStatsPanel = new Rectangle(this.HullSelectionRect.X + 50, this.HullSelectionRect.Y + this.HullSelectionRect.Height - 20, 280, 320);

            this.classifCursor = new Vector2(ShipStatsPanel.X - 100, ShipStatsPanel.Y + ShipStatsPanel.Height + 92);

            dropdownRect = new Rectangle((int)ShipStatsPanel.X, (int)ShipStatsPanel.Y + ShipStatsPanel.Height + 118, 100, 18);

            this.CategoryList = new DropOptions(dropdownRect);
            this.CategoryList.AddOption("Unclassified", 1);
            this.CategoryList.AddOption("Civilian", 2);
            this.CategoryList.AddOption("Recon", 3);
            this.CategoryList.AddOption("Fighter", 4);
            this.CategoryList.AddOption("Bomber", 5);

            this.CarrierOnly = this.ActiveHull.CarrierShip;
            Ref<bool> CORef = new Ref<bool>(() => this.CarrierOnly, (bool x) => {
				this.CarrierOnly = x;              
			});

            this.COBoxCursor = new Vector2(dropdownRect.X + 106, dropdownRect.Y);
            this.CarrierOnlyBox = new Checkbox(this.COBoxCursor, "Carrier Only", CORef, Fonts.Arial12Bold); 

			this.ShipStats = new Menu1(base.ScreenManager, ShipStatsPanel);
			this.statsSub = new Submenu(base.ScreenManager, ShipStatsPanel);
			this.statsSub.AddTab(Localizer.Token(108));
			this.ArcsButton = new GenericButton(new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 32), 97f), "Arcs", Fonts.Pirulen20, Fonts.Pirulen16);
			this.close = new CloseButton(new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 27, 99, 20, 20));
			this.OriginalZ = this.cameraPosition.Z;
		}

		private string parseText(string text, float Width, SpriteFont font)
		{
			string line = string.Empty;
			string returnString = string.Empty;
			string[] strArrays = text.Split(new char[] { ' ' });
			for (int i = 0; i < (int)strArrays.Length; i++)
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

		public void PlayNegativeSound()
		{
			AudioManager.GetCue("UI_Misc20").Play();
		}

		private void ReallyExit()
		{
			LightRig rig = base.ScreenManager.Content.Load<LightRig>("example/NewGamelight_rig");
			lock (GlobalStats.ObjectManagerLocker)
			{
				base.ScreenManager.inter.LightManager.Clear();
				base.ScreenManager.inter.LightManager.Submit(rig);
				base.ScreenManager.inter.ObjectManager.Remove(this.shipSO);
			}
			if (Ship.universeScreen.LookingAtPlanet && Ship.universeScreen.workersPanel is ColonyScreen)
			{
				(Ship.universeScreen.workersPanel as ColonyScreen).Reset = true;
			}
            //this should go some where else, need to find it a home
            this.ScreenManager.RemoveScreen(this);
			base.ExitScreen();
		}

        private void RecalculatePower()
        {
            foreach (SlotStruct slotStruct in this.Slots)
            {
                slotStruct.Powered = false;
                slotStruct.CheckedConduits = false;
                if (slotStruct.module != null)
                    slotStruct.module.Powered = false;
            }
            foreach (SlotStruct slotStruct in this.Slots)
            {
                //System.Diagnostics.Debug.Assert(slotStruct.parent != null, "parent is null");                   
                if (slotStruct.module != null && slotStruct.module.ModuleType == ShipModuleType.PowerPlant)
                {
                    foreach (SlotStruct slot in this.Slots)
                    {
                        if (slot.module != null && slot.module.ModuleType == ShipModuleType.PowerConduit && (Math.Abs(slot.pq.X - slotStruct.pq.X) / 16 + Math.Abs(slot.pq.Y - slotStruct.pq.Y) / 16 == 1 && slot.module != null))
                            this.CheckAndPowerConduit(slot);
                    }
                }                
                else if (slotStruct.parent != null)               
                {
                    //System.Diagnostics.Debug.Assert(slotStruct.parent.module != null, "parent is fine, module is null");
                    if (slotStruct.parent.module != null)
                    {
                        //System.Diagnostics.Debug.Assert(slotStruct.parent.module.ModuleType != null, "parent is fine, module is fine, moduletype is null");
                        if (slotStruct.parent.module.ModuleType == ShipModuleType.PowerPlant)
                        {
                            foreach (SlotStruct slot in this.Slots)
                            {
                                if (slot.module != null && slot.module.ModuleType == ShipModuleType.PowerConduit && (Math.Abs(slot.pq.X - slotStruct.pq.X) / 16 + Math.Abs(slot.pq.Y - slotStruct.pq.Y) / 16 == 1 && slot.module != null))
                                    this.CheckAndPowerConduit(slot);
                            }
                        }
                    }
                }
            }
            foreach (SlotStruct slotStruct1 in this.Slots)
            {
                if (!slotStruct1.isDummy && slotStruct1.module != null && (int)slotStruct1.module.PowerRadius > 0 && (slotStruct1.module.ModuleType != ShipModuleType.PowerConduit || slotStruct1.module.Powered))
                {
                    foreach (SlotStruct slotStruct2 in this.Slots)
                    {
                        if (Math.Abs(slotStruct1.pq.X - slotStruct2.pq.X) / 16 + Math.Abs(slotStruct1.pq.Y - slotStruct2.pq.Y) / 16 <= (int)slotStruct1.module.PowerRadius)
                            slotStruct2.Powered = true;
                    }
                    if ((int)slotStruct1.module.XSIZE > 1 || (int)slotStruct1.module.YSIZE > 1)
                    {
                        for (int index1 = 0; index1 < (int)slotStruct1.module.YSIZE; ++index1)
                        {
                            for (int index2 = 0; index2 < (int)slotStruct1.module.XSIZE; ++index2)
                            {
                                if (!(index2 == 0 & index1 == 0))
                                {
                                    foreach (SlotStruct slotStruct2 in this.Slots)
                                    {
                                        if (slotStruct2.pq.Y == slotStruct1.pq.Y + 16 * index1 && slotStruct2.pq.X == slotStruct1.pq.X + 16 * index2)
                                        {
                                            foreach (SlotStruct slotStruct3 in this.Slots)
                                            {
                                                if (Math.Abs(slotStruct2.pq.X - slotStruct3.pq.X) / 16 + Math.Abs(slotStruct2.pq.Y - slotStruct3.pq.Y) / 16 <= (int)slotStruct1.module.PowerRadius)
                                                    slotStruct3.Powered = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (SlotStruct slotStruct in this.Slots)
            {
                if (slotStruct.Powered)
                {
                    if (slotStruct.module != null && slotStruct.module.ModuleType != ShipModuleType.PowerConduit)
                        slotStruct.module.Powered = true;
                    if (slotStruct.parent != null && slotStruct.parent.module != null)
                        slotStruct.parent.module.Powered = true;                    
                }
                if (!slotStruct.Powered && slotStruct.module != null && slotStruct.module.IndirectPower)
                        slotStruct.module.Powered = true;
            }
        }

		public void ResetLists()
		{
			this.Reset = true;
			this.weaponSL.indexAtTop = 0;
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
            this.ActiveHull.ModuleSlotList.Clear();
			this.ActiveHull.Name = name;
			ShipData toSave = this.ActiveHull.GetClone();
			foreach (SlotStruct slot in this.Slots)
			{
				if (slot.isDummy)
				{
					ModuleSlotData data = new ModuleSlotData()
					{
						Position = slot.slotReference.Position,
						Restrictions = slot.Restrictions,
						InstalledModuleUID = "Dummy"
					};
					toSave.ModuleSlotList.Add(data);
				}
				else
				{
					ModuleSlotData data = new ModuleSlotData()
					{
						InstalledModuleUID = slot.ModuleUID,
						Position = slot.slotReference.Position,
						Restrictions = slot.Restrictions
					};
					if (slot.module != null)
					{
						data.facing = slot.module.facing;
					}
					toSave.ModuleSlotList.Add(data);
					if (slot.module != null && slot.module.ModuleType == ShipModuleType.Hangar)
					{
						data.SlotOptions = slot.module.hangarShipUID;
					}
					data.state = slot.state;
				}
			}
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			Ship_Game.Gameplay.CombatState combatState = toSave.CombatState;
			toSave.CombatState = this.CombatState;
			toSave.Name = name;

            //Cases correspond to the 5 options in the drop-down menu; default exists for... Propriety, mainly. The option selected when saving will always be the Category saved, pretty straightforward.
            switch (this.CategoryList.Options[this.CategoryList.ActiveIndex].@value)
            {
                case 1:
                    {
                        this.ActiveHull.ShipCategory = ShipData.Category.Unclassified;
                        break;
                    }
                case 2:
                    {
                        this.ActiveHull.ShipCategory = ShipData.Category.Civilian;
                        break;
                    }
                case 3:
                    {
                        this.ActiveHull.ShipCategory = ShipData.Category.Recon;
                        break;
                    }
                case 4:
                    {
                        this.ActiveHull.ShipCategory = ShipData.Category.Fighter;
                        break;
                    }
                case 5:
                    {
                        this.ActiveHull.ShipCategory = ShipData.Category.Bomber;
                        break;
                    }
                default:
                    {
                        this.ActiveHull.ShipCategory = ShipData.Category.Unclassified;
                        break;
                    }
            }

            //Adds the category determined by the case from the dropdown to the 'toSave' ShipData.
            toSave.ShipCategory = this.ActiveHull.ShipCategory;

            //Adds the boolean derived from the checkbox boolean (CarrierOnly) to the ShipData. Defaults to 'false'.
            toSave.CarrierShip = this.CarrierOnly;

			XmlSerializer Serializer = new XmlSerializer(typeof(ShipData));
			TextWriter WriteFileStream = new StreamWriter(string.Concat(path, "/StarDrive/Saved Designs/", name, ".xml"));
			Serializer.Serialize(WriteFileStream, toSave);
			WriteFileStream.Close();
			this.ShipSaved = true;
			if (Ship_Game.ResourceManager.ShipsDict.ContainsKey(name))
			{
				Ship newShip = Ship.CreateShipFromShipData(toSave);
				newShip.SetShipData(toSave);
				newShip.InitForLoad();
				newShip.InitializeStatus();
				Ship_Game.ResourceManager.ShipsDict[name] = newShip;
				Ship_Game.ResourceManager.ShipsDict[name].IsPlayerDesign = true;
			}
			else
			{
				Ship newShip = Ship.CreateShipFromShipData(toSave);
				newShip.SetShipData(toSave);
				newShip.InitForLoad();
				newShip.InitializeStatus();
				Ship_Game.ResourceManager.ShipsDict.Add(name, newShip);
				Ship_Game.ResourceManager.ShipsDict[name].IsPlayerDesign = true;
			}
			EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).UpdateShipsWeCanBuild();
			this.ActiveHull.CombatState = this.CombatState;
			this.ChangeHull(this.ActiveHull);
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
				ThrusterList = this.ActiveHull.ThrusterList,
				ModuleSlotList = new List<ModuleSlotData>()
			};
			foreach (SlotStruct slot in this.Slots)
			{
				if (!slot.isDummy)
				{
					ModuleSlotData data = new ModuleSlotData()
					{
						InstalledModuleUID = slot.ModuleUID,
						Position = slot.slotReference.Position,
						Restrictions = slot.Restrictions
					};
					if (slot.module != null)
					{
						data.facing = slot.module.facing;
						data.state = slot.state;
					}
					savedShip.ModuleSlotList.Add(data);
					if (slot.module == null || slot.module.ModuleType != ShipModuleType.Hangar)
					{
						continue;
					}
					data.SlotOptions = slot.module.hangarShipUID;
				}
				else if (!slot.isDummy)
				{
					ModuleSlotData data = new ModuleSlotData()
					{
						Position = slot.slotReference.Position,
						Restrictions = slot.Restrictions,
						InstalledModuleUID = ""
					};
					savedShip.ModuleSlotList.Add(data);
				}
				else
				{
					ModuleSlotData data = new ModuleSlotData()
					{
						Position = slot.slotReference.Position,
						Restrictions = slot.Restrictions,
						InstalledModuleUID = "Dummy"
					};
					savedShip.ModuleSlotList.Add(data);
				}
			}
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			Ship_Game.Gameplay.CombatState defaultstate = this.ActiveHull.CombatState;
			savedShip.CombatState = this.CombatState;
			string filename = string.Format("{0:yyyy-MM-dd}__{1}", DateTime.Now, this.ActiveHull.Name);
			savedShip.Name = filename;
			XmlSerializer Serializer = new XmlSerializer(typeof(ShipData));
			TextWriter WriteFileStream = new StreamWriter(string.Concat(path, "/StarDrive/WIP/", filename, ".xml"));
			Serializer.Serialize(WriteFileStream, savedShip);
			WriteFileStream.Close();
			savedShip.CombatState = defaultstate;
			this.ShipSaved = true;
		}

		private void SaveWIPThenChangeHull(object sender, EventArgs e)
		{
			this.SaveWIP(sender, e);
			this.ChangeHull(this.changeto);
		}

		private void SaveWIPThenExitToFleets(object sender, EventArgs e)
		{
			this.SaveWIP(sender, e);
			base.ScreenManager.AddScreen(new FleetDesignScreen(this.EmpireUI));
			this.ReallyExit();
		}

		private void SaveWIPThenExitToShipsList(object sender, EventArgs e)
		{
			this.SaveWIP(sender, e);
			base.ScreenManager.AddScreen(new ShipListScreen(base.ScreenManager, this.EmpireUI));
			this.ReallyExit();
		}

		private void SaveWIPThenLaunchScreen(object sender, EventArgs e)
		{
			this.SaveWIP(sender, e);
			string str = this.screenToLaunch;
			string str1 = str;
			if (str != null)
			{
				if (str1 == "Research")
				{
					AudioManager.PlayCue("echo_affirm");
					base.ScreenManager.AddScreen(new ResearchScreenNew(this.EmpireUI));
				}
				else if (str1 == "Budget")
				{
					AudioManager.PlayCue("echo_affirm");
					base.ScreenManager.AddScreen(new BudgetScreen(ShipDesignScreen.screen));
				}
			}
			string str2 = this.screenToLaunch;
			string str3 = str2;
			if (str2 != null)
			{
				if (str3 == "Main Menu")
				{
					AudioManager.PlayCue("echo_affirm");
					ShipDesignScreen.screen.ScreenManager.AddScreen(new GameplayMMScreen(ShipDesignScreen.screen));
				}
				else if (str3 == "Shipyard")
				{
					AudioManager.PlayCue("echo_affirm");
				}
				else if (str3 == "Empire")
				{
					ShipDesignScreen.screen.ScreenManager.AddScreen(new EmpireScreen(ShipDesignScreen.screen.ScreenManager, this.EmpireUI));
					AudioManager.PlayCue("echo_affirm");
				}
				else if (str3 == "Diplomacy")
				{
					ShipDesignScreen.screen.ScreenManager.AddScreen(new MainDiplomacyScreen(ShipDesignScreen.screen));
					AudioManager.PlayCue("echo_affirm");
				}
				else if (str3 == "?")
				{
					AudioManager.PlayCue("sd_ui_tactical_pause");
					InGameWiki wiki = new InGameWiki(new Rectangle(0, 0, 750, 600))
					{
						TitleText = "StarDrive Help",
						MiddleText = "This help menu contains information on all of the gameplay systems contained in StarDrive. You can also watch one of several tutorial videos for a developer-guided introduction to StarDrive."
					};
				}
			}
			this.ReallyExit();
		}

		public void SetActiveModule(ShipModule mod)
		{
			AudioManager.GetCue("smallservo").Play();
			mod.SetAttributesNoParent();
			this.ActiveModule = mod;
			this.ResetModuleState();
			foreach (SlotStruct s in this.Slots)
			{
				s.ShowInvalid = false;
				s.ShowValid = false;
				if (Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].Restrictions == Restrictions.I && (s.Restrictions == Restrictions.I || s.Restrictions == Restrictions.IO))
				{
					s.ShowValid = true;
				}
				else if (Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].Restrictions == Restrictions.IO && (s.Restrictions == Restrictions.I || s.Restrictions == Restrictions.IO || s.Restrictions == Restrictions.O))
				{
					s.ShowValid = true;
				}
                else if (Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].Restrictions == Restrictions.IE && (s.Restrictions == Restrictions.I || s.Restrictions == Restrictions.E))
                {
                    s.ShowValid = true;
                }
                else if (Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].Restrictions == Restrictions.OE && (s.Restrictions == Restrictions.O || s.Restrictions == Restrictions.E))
                {
                    s.ShowValid = true;
                }
                else if (Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].Restrictions == Restrictions.O && (s.Restrictions == Restrictions.O || s.Restrictions == Restrictions.IO))
                {
                    s.ShowValid = true;
                }
                else if (Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].Restrictions == Restrictions.E && s.Restrictions == Restrictions.E)
                {
                    s.ShowValid = true;
                }
                else if (Ship_Game.ResourceManager.ShipModulesDict[this.ActiveModule.UID].Restrictions != Restrictions.IOE || s.Restrictions != Restrictions.I && s.Restrictions != Restrictions.IO && s.Restrictions != Restrictions.O && s.Restrictions != Restrictions.E)
                {
                    s.ShowInvalid = true;
                }

                else
                {
                    s.ShowValid = true;
                }
			}
			if (this.ActiveModule.ModuleType == ShipModuleType.Hangar)
			{
				this.ChooseFighterSL.Entries.Clear();
				this.ChooseFighterSL.Copied.Clear();
				foreach (string shipname in EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).ShipsWeCanBuild)
				{
					if (!this.ActiveModule.PermittedHangarRoles.Contains(Ship_Game.ResourceManager.ShipsDict[shipname].Role) || Ship_Game.ResourceManager.ShipsDict[shipname].Size >= this.ActiveModule.MaximumHangarShipSize)
					{
						continue;
					}
					this.ChooseFighterSL.AddItem(Ship_Game.ResourceManager.ShipsDict[shipname]);
				}
                if (this.HangarShipUIDLast != "Undefined" && this.ActiveModule.PermittedHangarRoles.Contains(Ship_Game.ResourceManager.ShipsDict[HangarShipUIDLast].Role) && this.ActiveModule.MaximumHangarShipSize >= Ship_Game.ResourceManager.ShipsDict[HangarShipUIDLast].Size)
                {
                    this.ActiveModule.hangarShipUID = this.HangarShipUIDLast;
                }
				else if (this.ChooseFighterSL.Entries.Count > 0)
				{
					this.ActiveModule.hangarShipUID = (this.ChooseFighterSL.Entries[0].item as Ship).Name;
				}
			}
			this.HighlightedModule = null;
			this.HoveredModule = null;
			this.ResetModuleState();
		}

        public void UpdateHangarOptions(ShipModule mod)
        {
            if (mod.ModuleType == ShipModuleType.Hangar)
            {
                this.ChooseFighterSL.Entries.Clear();
                this.ChooseFighterSL.Copied.Clear();
                foreach (string shipname in EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).ShipsWeCanBuild)
                {
                    if (!mod.PermittedHangarRoles.Contains(Ship_Game.ResourceManager.ShipsDict[shipname].Role) || Ship_Game.ResourceManager.ShipsDict[shipname].Size >= mod.MaximumHangarShipSize)
                    {
                        continue;
                    }
                    this.ChooseFighterSL.AddItem(Ship_Game.ResourceManager.ShipsDict[shipname]);
                }
            }
        }

		private void SetupSlots()
		{
			this.Slots.Clear();
			foreach (ModuleSlotData slot in this.ActiveHull.ModuleSlotList)
			{
				SlotStruct ss = new SlotStruct();
				PrimitiveQuad pq = new PrimitiveQuad(slot.Position.X + this.offset.X - 8f, slot.Position.Y + this.offset.Y - 8f, 16f, 16f);
				ss.pq = pq;
				ss.Restrictions = slot.Restrictions;
				ss.facing = slot.facing;
				ss.ModuleUID = slot.InstalledModuleUID;
				ss.state = slot.state;
				ss.slotReference = slot;
				ss.SlotOptions = slot.SlotOptions;
				this.Slots.Add(ss);
			}
			foreach (SlotStruct slot in this.Slots)
			{
				if (slot.ModuleUID == null)
				{
					continue;
				}
				this.ActiveModule = Ship_Game.ResourceManager.GetModule(slot.ModuleUID);
				this.ChangeModuleState(slot.state);
				this.InstallModuleFromLoad(slot);
				if (slot.module == null || slot.module.ModuleType != ShipModuleType.Hangar)
				{
					continue;
				}
				slot.module.hangarShipUID = slot.SlotOptions;
			}
			this.ActiveModule = null;
			this.ActiveModState = ShipDesignScreen.ActiveModuleState.Normal;
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			float DesiredZ = MathHelper.SmoothStep(this.camera.Zoom, this.TransitionZoom, 0.2f);
			this.camera.Zoom = DesiredZ;
			if (this.camera.Zoom < 0.3f)
			{
				this.camera.Zoom = 0.3f;
			}
			if (this.camera.Zoom > 2.65f)
			{
				this.camera.Zoom = 2.65f;
			}
			this.cameraPosition.Z = this.OriginalZ / this.camera.Zoom;
			Vector3 camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
			this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f))) * Matrix.CreateRotationX(MathHelper.ToRadians(0f))) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

        //Added by McShooterz: modifies weapon stats to reflect weapon tag bonuses
        private float ModifiedWeaponStat(Weapon weapon, string stat)
        {
            float value=0;

            switch (stat)
            {
                case "damage":
                    value = weapon.DamageAmount;
                    break;
                case "range":
                    value = weapon.Range;
                    break;
                case "speed":
                    value = weapon.ProjectileSpeed;
                    break;
                case "firedelay":
                    value = weapon.fireDelay;
                    break;
                case "armor":
                    value = weapon.EffectVsArmor;
                    break;
                case "shield":
                    value = weapon.EffectVSShields;
                    break;
            }

            if (weapon.Tag_Missile)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Missile"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Missile"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Missile"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Missile"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Missile"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Missile"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Energy)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Energy"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Energy"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Energy"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Energy"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Energy"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Energy"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Torpedo)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Torpedo"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Torpedo"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Torpedo"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Torpedo"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Torpedo"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Torpedo"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Kinetic)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Kinetic"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Kinetic"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Kinetic"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Kinetic"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Kinetic"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Kinetic"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Hybrid)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Hybrid"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Hybrid"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Hybrid"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Hybrid"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Hybrid"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Hybrid"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Railgun)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Railgun"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Railgun"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Railgun"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Railgun"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Railgun"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Railgun"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Explosive)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Explosive"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Explosive"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Explosive"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Explosive"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Explosive"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Explosive"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Guided)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Guided"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Guided"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Guided"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Guided"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Guided"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Guided"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Intercept)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Intercept"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Intercept"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Intercept"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Intercept"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Intercept"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Intercept"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_PD)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["PD"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["PD"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["PD"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["PD"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["PD"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["PD"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_SpaceBomb)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Spacebomb"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Spacebomb"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Spacebomb"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Spacebomb"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Spacebomb"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Spacebomb"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_BioWeapon)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["BioWeapon"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["BioWeapon"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["BioWeapon"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["BioWeapon"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["BioWeapon"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["BioWeapon"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Drone)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Drone"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Drone"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Drone"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Drone"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Drone"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Drone"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Subspace)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Subspace"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Subspace"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Subspace"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Subspace"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Subspace"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Subspace"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Warp)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Warp"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Warp"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Warp"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Warp"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Warp"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Warp"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Cannon)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Cannon"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Cannon"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Cannon"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Cannon"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Cannon"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Cannon"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Beam)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Beam"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Beam"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Beam"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Beam"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Beam"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Beam"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Bomb)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Bomb"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Bomb"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Bomb"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Bomb"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Bomb"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.GetEmpireByName(this.EmpireUI.screen.PlayerLoyalty).data.WeaponTags["Bomb"].ShieldDamage;
                        break;
                }
            }
            return value;
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

		public enum ActiveModuleState
		{
			Normal,
			Left,
			Right,
			Rear
		}

		private enum Colors
		{
			Black,
			Red,
			Blue,
			Orange,
			Yellow,
			Green
		}

		private struct ModuleCatButton
		{
			public Rectangle mRect;

			public string Category;
		}

		private enum SlotModOperation
		{
			Delete,
			I,
			IO,
			O,
			Add,
			E
		}
	}
}