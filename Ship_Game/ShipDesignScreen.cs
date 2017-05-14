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
using System.Linq;
using Ship_Game.AI;
using System.Text;

namespace Ship_Game
{
    public sealed class ShipDesignScreen : GameScreen
    {
        private Matrix worldMatrix = Matrix.Identity;
        private Matrix view;
        private Matrix projection;
        public Camera2d camera;
        public Array<ToggleButton> CombatStatusButtons = new Array<ToggleButton>();
        public bool Debug;
        public ShipData ActiveHull;
        public EmpireUIOverlay EmpireUI;
        private Menu1 ModuleSelectionMenu;
        private Model ActiveModel;
        private SceneObject shipSO;
        private Vector3 cameraPosition = new Vector3(0f, 0f, 1300f);
        public Array<SlotStruct> Slots = new Array<SlotStruct>();
        private Vector2 offset;
        private CombatState CombatState = CombatState.AttackRuns;
        private bool ShipSaved = true;
        private Array<ShipData> AvailableHulls = new Array<ShipData>();
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
        private Array<ShipDesignScreen.ModuleCatButton> ModuleCatButtons = new Array<ShipDesignScreen.ModuleCatButton>();
        private Array<ModuleButton> ModuleButtons = new Array<ModuleButton>();
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
        private ShipModule ActiveHangarModule;
        private ShipDesignScreen.ActiveModuleState ActiveModState;
        private Selector selector;
        public bool ToggleOverlay = true;
        private Vector2 starfieldPos = Vector2.Zero;
        private int scrollPosition;
        private DropOptions CategoryList;
        private Rectangle dropdownRect;
        private Vector2 classifCursor;
        public Stack<DesignAction> DesignStack = new Stack<DesignAction>();
        private string lastActiveUID = "";                                      //Gretman - To Make the Ctrl-Z much more responsive
        private Vector2 lastDesignActionPos = Vector2.Zero;
        private Vector2 COBoxCursor;
        private Checkbox CarrierOnlyBox;
        private bool fml = false;
        private bool fmlevenmore = false;
        public bool CarrierOnly;
        private ShipData.Category LoadCategory;
        public string HangarShipUIDLast = "Undefined";
        private float HoldTimer = .50f;
        private HashSet<string> techs = new HashSet<string>();

#if SHIPYARD
        short TotalI, TotalO, TotalE, TotalIO, TotalIE, TotalOE, TotalIOE = 0;        //For Gretman's debug shipyard
#endif


        public ShipDesignScreen(GameScreen parent, EmpireUIOverlay EmpireUI) : base(parent)
        {
            this.EmpireUI = EmpireUI;
            base.TransitionOnTime = TimeSpan.FromSeconds(2);
#if SHIPYARD
            Debug = true;
#endif
        }

        private void AddToTechList(HashSet<string> techlist)
        {
            foreach (string tech in techlist)
                this.techs.Add(tech);
        }

        public void ChangeHull(ShipData hull)       //Mer
        {
        #if SHIPYARD
            TotalI = TotalO = TotalE = TotalIO = TotalIE = TotalOE = TotalIOE = 0;
        #endif
            Reset = true;
            DesignStack.Clear();
            lastDesignActionPos = Vector2.Zero;
            lastActiveUID = "";

            lock (GlobalStats.ObjectManagerLocker)
            {
                if (shipSO != null)
                {
                    ScreenManager.inter.ObjectManager.Remove(shipSO);
                }
            }
            ActiveHull = new ShipData()
            {
                Animated     = hull.Animated,
                CombatState  = hull.CombatState,
                Hull         = hull.Hull,
                IconPath     = hull.IconPath,
                ModelPath    = hull.ModelPath,
                Name         = hull.Name,
                Role         = hull.Role,
                ShipStyle    = hull.ShipStyle,
                ThrusterList = hull.ThrusterList,
                ShipCategory = hull.ShipCategory,
                CarrierShip  = hull.CarrierShip
            };
            techs.Clear();
            AddToTechList(ActiveHull.HullData.techsNeeded);
            CarrierOnly  = hull.CarrierShip;
            LoadCategory = hull.ShipCategory;
            fml = true;
            fmlevenmore = true;

            ActiveHull.ModuleSlots = new ModuleSlotData[hull.ModuleSlots.Length];
            for (int i = 0; i < hull.ModuleSlots.Length; ++i)
            {
                ModuleSlotData hullSlot = hull.ModuleSlots[i];
                ModuleSlotData data = new ModuleSlotData()
                {
                    Position           = hullSlot.Position,
                    Restrictions       = hullSlot.Restrictions,
                    Facing             = hullSlot.Facing,
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
            foreach (ToggleButton button in CombatStatusButtons)
            {
                switch (button.Action)
                {
                    default: continue;
                    case "attack":          button.Active = CombatState == CombatState.AttackRuns;   break;
                    case "arty":            button.Active = CombatState == CombatState.Artillery;    break;
                    case "hold":            button.Active = CombatState == CombatState.HoldPosition; break;
                    case "orbit_left":      button.Active = CombatState == CombatState.OrbitLeft;    break;
                    case "broadside_left":  button.Active = CombatState == CombatState.BroadsideLeft; break;
                    case "broadside_right": button.Active = CombatState == CombatState.BroadsideRight; break;
                    case "short":           button.Active = CombatState == CombatState.ShortRange;     break;
                    case "evade":           button.Active = CombatState == CombatState.Evade;          break;
                    case "orbit_right":     button.Active = CombatState == CombatState.OrbitRight;     break;
                }
            }
            SetupSlots();
        }

        private void ChangeModuleState(ActiveModuleState state)
        {
            if (ActiveModule == null)
                return;
            ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(ActiveModule.UID);
            int x = moduleTemplate.XSIZE;
            int y = moduleTemplate.YSIZE;
            switch (state)
            {
                case ActiveModuleState.Normal:
                {
                    this.ActiveModule.XSIZE = moduleTemplate.XSIZE;
                    this.ActiveModule.YSIZE = moduleTemplate.YSIZE;
                    this.ActiveModState = ActiveModuleState.Normal;
                    return;
                }
                case ActiveModuleState.Left:
                {
                    this.ActiveModule.XSIZE = y; // @todo Why are these swapped? Please comment.
                    this.ActiveModule.YSIZE = x; // These are swapped because if the module is facing left or right, then the length is now the height, and vice versa
                    this.ActiveModState = ActiveModuleState.Left;
                    this.ActiveModule.Facing = 270f;
                    return;
                }
                case ActiveModuleState.Right:
                {
                    this.ActiveModule.XSIZE = y; // @todo Why are these swapped? Please comment.
                    this.ActiveModule.YSIZE = x; // These are swapped because if the module is facing left or right, then the length is now the height, and vice versa
                    this.ActiveModState = ActiveModuleState.Right;
                    this.ActiveModule.Facing = 90f;
                    return;
                }
                case ActiveModuleState.Rear:
                {
                    this.ActiveModule.XSIZE = moduleTemplate.XSIZE;
                    this.ActiveModule.YSIZE = moduleTemplate.YSIZE;
                    this.ActiveModState = ActiveModuleState.Rear;
                    this.ActiveModule.Facing = 180f;
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
            slot.Module.Powered = true;
            slot.CheckedConduits = true;
            foreach (SlotStruct ss in Slots)
            {
                if (ss == slot || Math.Abs(slot.PQ.X - ss.PQ.X) / 16 + Math.Abs(slot.PQ.Y - ss.PQ.Y) / 16 != 1 || ss.Module == null || ss.Module.ModuleType != ShipModuleType.PowerConduit || ss.CheckedConduits)
                    continue;
                CheckAndPowerConduit(ss);
            }
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
            if (!hasBridge && ActiveHull.Role != ShipData.RoleName.platform && ActiveHull.Role != ShipData.RoleName.station || !emptySlots)
                return false;
            return true;
        }

        private bool FindStructFromOffset(SlotStruct offsetBase, int x, int y, out SlotStruct found)
        {
            found = null;
            if (x == 0 && y == 0)
                return false; // ignore self, {0,0} is offsetBase

            int sx = offsetBase.PQ.X + 16 * x;
            int sy = offsetBase.PQ.Y + 16 * y;
            for (int i = 0; i < Slots.Count; ++i)
            {
                SlotStruct s = Slots[i];
                if (s.PQ.X == sx && s.PQ.Y == sy)
                {
                    found = s;
                    return true;
                }
            }
            return false;
        }

        // @todo This is all broken. Redo everything.
        private void ClearDestinationSlots(SlotStruct slot, bool addToAlteredSlots = true)
        {
            for (int y = 0; y < ActiveModule.YSIZE; y++)
            {
                for (int x = 0; x < ActiveModule.XSIZE; x++)
                {
                    if (!FindStructFromOffset(slot, x, y, out SlotStruct slot2))
                        continue;

                    if (slot2.Module != null)
                        ClearParentSlot(slot2, addToAlteredSlots);

                    slot2.ModuleUID = null;
                    slot2.Tex       = null;
                    slot2.Module    = null;
                    slot2.Parent    = slot;
                    slot2.State     = ActiveModuleState.Normal;
                }
            }
        }

        private void ClearDestinationSlotsNoStack(SlotStruct slot) => ClearDestinationSlots(slot, false);

        // @todo This is all broken. Redo everything.
        private void ClearParentSlot(SlotStruct parent, bool addToAlteredSlots = true)
        {
            //actually supposed to clear ALL slots of a module, not just the parent
            if (addToAlteredSlots && DesignStack.Count > 0)
            {
                SlotStruct slot1 = new SlotStruct()
                {
                    PQ            = parent.PQ,
                    Restrictions  = parent.Restrictions,
                    Facing        = parent.Facing,
                    ModuleUID     = parent.ModuleUID,
                    Module        = parent.Module,
                    State         = parent.State,
                    SlotReference = parent.SlotReference
                };
                DesignStack.Peek().AlteredSlots.Add(slot1);
            }

            for (int y = 0; y < parent.Module.YSIZE; ++y)
            {
                for (int x = 0; x < parent.Module.XSIZE; ++x)
                {
                    if (!FindStructFromOffset(parent, x, y, out SlotStruct slot2))
                        continue;
                    slot2.ModuleUID = null;
                    slot2.Tex       = null;
                    slot2.Module    = null;
                    slot2.Parent    = null;
                    slot2.State     = ActiveModuleState.Normal;
                }
            }
            //clear parent slot
            parent.ModuleUID = null;
            parent.Tex       = null;
            parent.Module    = null;
            parent.Parent    = null;
            parent.State     = ActiveModuleState.Normal;
        }
        private void ClearParentSlotNoStack(SlotStruct parent) => ClearParentSlot(parent, false);   //Unused

        private void ClearSlot(SlotStruct slot, bool addToAlteredSlots = true)
        {   //this is the clearslot function actually used atm
            //only called from installmodule atm, not from manual module removal
            if (slot.Module != null)
            {
                ClearParentSlot(slot, addToAlteredSlots);
            }
            else
            {   //this requires not being a child slot and not containing a module
                //only empty parent slots can trigger this
                //why would we want to clear an empty slot?
                //might be used on initial load instead of a proper slot constructor
                slot.ModuleUID = null;
                slot.Tex       = null;
                slot.Parent    = null;
                slot.Module    = null;
                slot.State     = ActiveModuleState.Normal;
            }
        }
        private void ClearSlotNoStack(SlotStruct slot) => ClearSlot(slot, false);

        public void CreateShipModuleSelectionWindow()
        {
            this.upArrow = new Rectangle(this.ModuleSelectionArea.X + this.ModuleSelectionArea.Width - 22, this.ModuleSelectionArea.Y, 22, 30);
            this.downArrow = new Rectangle(this.ModuleSelectionArea.X + this.ModuleSelectionArea.Width - 22, this.ModuleSelectionArea.Y + this.ModuleSelectionArea.Height - 32, 20, 30);
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
                    World = this.worldMatrix
                };
                base.ScreenManager.inter.ObjectManager.Submit(this.shipSO);
                this.SetupSlots();
            }
        }

        private ModuleSlotData FindModuleSlotAtPos(Vector2 slotPos)
        {
            ModuleSlotData[] slots = ActiveHull.ModuleSlots;
            for (int i = 0; i < slots.Length; ++i)
                if (slots[i].Position == slotPos)
                    return slots[i];
            return null;
        }

        private void DebugAlterSlot(Vector2 SlotPos, SlotModOperation op)
        {
            ModuleSlotData toRemove = FindModuleSlotAtPos(SlotPos);
            if (toRemove == null)
                return;

            switch (op)
            {
                default:
                case SlotModOperation.Normal: return;
                case SlotModOperation.Delete: ActiveHull.ModuleSlots.Remove(toRemove, out ActiveHull.ModuleSlots); break;
                case SlotModOperation.I:      toRemove.Restrictions = Restrictions.I;  break;
                case SlotModOperation.O:      toRemove.Restrictions = Restrictions.O;  break;
                case SlotModOperation.E:      toRemove.Restrictions = Restrictions.E;  break;
                case SlotModOperation.IO:     toRemove.Restrictions = Restrictions.IO; break;
                case SlotModOperation.IE:     toRemove.Restrictions = Restrictions.IE; break;
                case SlotModOperation.OE:     toRemove.Restrictions = Restrictions.OE; break;
                case SlotModOperation.IOE:    toRemove.Restrictions = Restrictions.IOE; break;
            }
            ChangeHull(ActiveHull);
        }

        protected override void Dispose(bool disposing)
        {
            hullSL?.Dispose(ref hullSL);
            weaponSL?.Dispose(ref weaponSL);
            ChooseFighterSL?.Dispose(ref ChooseFighterSL);
            base.Dispose(disposing);
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

        private void DoExitToShipList(object sender, EventArgs e)   //Unused
        {
            ReallyExit();
        }

        private void DoExitToShipsList(object sender, EventArgs e) //Unused
        {
            ScreenManager.AddScreen(new ShipListScreen(this, EmpireUI));
            ReallyExit();
        }

        public override void Draw(GameTime gameTime)
        {
            Color unpoweredColored;
            Color activeColor;
            lock (GlobalStats.ObjectManagerLocker)
            {
                base.ScreenManager.sceneState.BeginFrameRendering(this.view, this.projection, gameTime, base.ScreenManager.environment, true);
                base.ScreenManager.editor.BeginFrameRendering(base.ScreenManager.sceneState);
                base.ScreenManager.inter.BeginFrameRendering(base.ScreenManager.sceneState);
                Empire.Universe.bg.Draw(Empire.Universe, Empire.Universe.starfield);
                base.ScreenManager.inter.RenderManager.Render();
            }
            base.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None
                , this.camera.get_transformation(base.ScreenManager.GraphicsDevice));
            if (this.ToggleOverlay)
            {
                foreach (SlotStruct slot in this.Slots)
                {
                    if (slot.Module != null)
                    {
                        base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.Texture("Modules/tile_concreteglass_1x1")
                            , new Rectangle(slot.PQ.enclosingRect.X, slot.PQ.enclosingRect.Y
                            , 16 * slot.Module.XSIZE, 16 * slot.Module.YSIZE), Color.Gray);
                    }
                    else
                    {
                        if (this.ActiveModule != null)
                        {
                            SpriteBatch spriteBatch = base.ScreenManager.SpriteBatch;
                            Texture2D item = Ship_Game.ResourceManager.Texture("Modules/tile_concreteglass_1x1");
                            Rectangle rectangle = slot.PQ.enclosingRect;
                            if (slot.ShowValid)
                            {
                                activeColor = Color.LightGreen;
                            }
                            else
                            {
                                activeColor = (slot.ShowInvalid ? Color.Red : Color.White);
                            }
                            spriteBatch.Draw(item, rectangle, activeColor);
                            if (slot.Powered)
                            {
                                base.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Modules/tile_concreteglass_1x1")
                                    , slot.PQ.enclosingRect, new Color(255, 255, 0, 150));
                            }
                        }
                        else if (slot.Powered)
                        {
                            base.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("Modules/tile_concreteglass_1x1")
                                , slot.PQ.enclosingRect, Color.Yellow);
                        }
                        else
                        {
                            SpriteBatch spriteBatch1 = base.ScreenManager.SpriteBatch;
                            Texture2D texture2D = ResourceManager.Texture("Modules/tile_concreteglass_1x1");
                            Rectangle rectangle1 = slot.PQ.enclosingRect;
                            if (slot.ShowValid)
                            {
                                unpoweredColored = Color.LightGreen;
                            }
                            else
                            {
                                unpoweredColored = (slot.ShowInvalid ? Color.Red : Color.White);
                            }
                            spriteBatch1.Draw(texture2D, rectangle1, unpoweredColored);
                        }
                    }
                    if (slot.Module != null)
                        continue;
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(" ", slot.Restrictions)
                        , new Vector2((float)slot.PQ.enclosingRect.X, (float)slot.PQ.enclosingRect.Y)
                        , Color.Navy, 0f, Vector2.Zero, 0.4f, SpriteEffects.None, 1f);
                }
                foreach (SlotStruct slot in this.Slots)
                {
                    if (slot.ModuleUID == null || slot.Tex == null)
                    {
                        continue;
                    }
                    if (slot.State != ActiveModuleState.Normal)
                    {
                        Rectangle r = new Rectangle(
                            slot.PQ.enclosingRect.X, 
                            slot.PQ.enclosingRect.Y, 
                            16 * slot.Module.XSIZE, 
                            16 * slot.Module.YSIZE);

                        // @todo Simplify this
                        switch (slot.State)
                        {
                            case ActiveModuleState.Left:
                            {
                                int h = slot.Module.YSIZE * 16;
                                int w = slot.Module.XSIZE * 16;
                                r.Width  = h; // swap width & height
                                r.Height = w;
                                r.Y += h;
                                ScreenManager.SpriteBatch.Draw(slot.Tex, r, null, Color.White, -1.57079637f, Vector2.Zero
                                    , SpriteEffects.None, 1f);
                                break;
                            }
                            case ActiveModuleState.Right:
                            {
                                int w = slot.Module.YSIZE * 16;
                                int h = slot.Module.XSIZE * 16;
                                r.Width = w;
                                r.Height = h;
                                r.X += h;
                                ScreenManager.SpriteBatch.Draw(slot.Tex, r, null, Color.White, 1.57079637f, Vector2.Zero
                                    , SpriteEffects.None, 1f);
                                break;
                            }
                            case ActiveModuleState.Rear:
                            {
                                ScreenManager.SpriteBatch.Draw(slot.Tex, r, null, Color.White, 0f, Vector2.Zero
                                    , SpriteEffects.FlipVertically, 1f);
                                break;
                            }
                        }
                    }
                    else if (slot.Module.XSIZE <= 1 && slot.Module.YSIZE <= 1)
                    {
                        if (slot.Module.ModuleType != ShipModuleType.PowerConduit)
                        {
                            ScreenManager.SpriteBatch.Draw(slot.Tex, slot.PQ.enclosingRect, Color.White);
                        }
                        else
                        {
                            string graphic = GetConduitGraphic(slot);
                            var conduitTex = ResourceManager.Texture("Conduits/" + graphic);
                            ScreenManager.SpriteBatch.Draw(conduitTex, slot.PQ.enclosingRect, Color.White);
                            if (slot.Module.Powered)
                            {
                                var poweredTex = ResourceManager.Texture("Conduits/" + graphic + "_power");
                                ScreenManager.SpriteBatch.Draw(poweredTex, slot.PQ.enclosingRect, Color.White);
                            }
                        }
                    }
                    else if (slot.SlotReference.Position.X <= 256f)
                    {
                        ScreenManager.SpriteBatch.Draw(slot.Tex, new Rectangle(slot.PQ.enclosingRect.X, slot.PQ.enclosingRect.Y
                            , 16 * slot.Module.XSIZE, 16 * slot.Module.YSIZE), Color.White);
                    }
                    else
                    {
                        ScreenManager.SpriteBatch.Draw(slot.Tex, new Rectangle(slot.PQ.enclosingRect.X, slot.PQ.enclosingRect.Y
                            , 16 * slot.Module.XSIZE, 16 * slot.Module.YSIZE), null, Color.White, 0f, Vector2.Zero
                            , SpriteEffects.FlipHorizontally, 1f);
                    }
                    if (slot.Module != HoveredModule)
                    {
                        continue;
                    }
                    Primitives2D.DrawRectangle(ScreenManager.SpriteBatch, new Rectangle(slot.PQ.enclosingRect.X, slot.PQ.enclosingRect.Y
                        , 16 * slot.Module.XSIZE, 16 * slot.Module.YSIZE), Color.White, 2f);
                }
                foreach (SlotStruct slot in this.Slots)
                {
                    if (slot.ModuleUID == null || slot.Tex == null || slot.Module != this.HighlightedModule && !this.ShowAllArcs)
                    {
                        continue;
                    }
                    if (slot.Module.shield_power_max > 0f)
                    {
                        Vector2 Center = new Vector2((float)(slot.PQ.enclosingRect.X + 16 * slot.Module.XSIZE / 2)
                            , (float)(slot.PQ.enclosingRect.Y + 16 * slot.Module.YSIZE / 2));
                        DrawCircle(Center, slot.Module.shield_radius, 50, Color.LightGreen);
                    }


                    // @todo Use this to fix the 'original' code below :)))
                    var arcTexture = Empire.Universe.GetArcTexture(slot.Module.FieldOfFire);

                    //Original by The Doctor, modified by McShooterz
                    if (slot.Module.FieldOfFire == 90f)
                    {
                        Vector2 Center = new Vector2((float)(slot.PQ.enclosingRect.X + 16 * slot.Module.XSIZE / 2)
                            , (float)(slot.PQ.enclosingRect.Y + 16 * slot.Module.YSIZE / 2));
                        Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.Module.InstalledWeapon.Tag_Cannon && !slot.Module.InstalledWeapon.Tag_Energy)
                        {
                            Color drawcolor = new Color(255, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable4 = null;
                            base.ScreenManager.SpriteBatch.Draw(arcTexture, toDraw, nullable4, drawcolor
                                , (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Railgun || slot.Module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc90"]
                                , toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Cannon)
                        {
                            Color drawcolor = new Color(0, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc90"], toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }                       
                        else if (!slot.Module.InstalledWeapon.isBeam)
                        {
                            Color drawcolor = new Color(255, 0, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable6 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc90"], toDraw, nullable6, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Color drawcolor = new Color(0, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable7 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc90"], toDraw, nullable7, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                    }
                    //Original by The Doctor, modified by McShooterz
                    else if (slot.Module.FieldOfFire == 15f && Ship_Game.ResourceManager.TextureDict.ContainsKey("Arcs/Arc15"))
                    {
                        Vector2 Center = new Vector2((float)(slot.PQ.enclosingRect.X + 16 * slot.Module.XSIZE / 2), (float)(slot.PQ.enclosingRect.Y + 16 * slot.Module.YSIZE / 2));
                        Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.Module.InstalledWeapon.Tag_Cannon && !slot.Module.InstalledWeapon.Tag_Energy)
                        {
                            Color drawcolor = new Color(255, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable4 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc15"], toDraw, nullable4, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Railgun || slot.Module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc15"], toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Cannon)
                        {
                            Color drawcolor = new Color(0, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc15"], toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (!slot.Module.InstalledWeapon.isBeam)
                        {
                            Color drawcolor = new Color(255, 0, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable6 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc15"], toDraw, nullable6, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Color drawcolor = new Color(0, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable7 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc15"], toDraw, nullable7, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                    }
                    //Original by The Doctor, modified by McShooterz
                    else if (slot.Module.FieldOfFire == 20f && ResourceManager.TextureDict.ContainsKey("Arcs/Arc20"))
                    {
                        Vector2 Center = new Vector2((float)(slot.PQ.enclosingRect.X + 16 * slot.Module.XSIZE / 2), (float)(slot.PQ.enclosingRect.Y + 16 * slot.Module.YSIZE / 2));
                        Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.Module.InstalledWeapon.Tag_Cannon && !slot.Module.InstalledWeapon.Tag_Energy)
                        {
                            Color drawcolor = new Color(255, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable4 = null;
                            base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc20"], toDraw, nullable4, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Railgun || slot.Module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc20"], toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Cannon)
                        {
                            Color drawcolor = new Color(0, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc20"], toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (!slot.Module.InstalledWeapon.isBeam)
                        {
                            Color drawcolor = new Color(255, 0, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable6 = null;
                            base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc20"], toDraw, nullable6, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Color drawcolor = new Color(0, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable7 = null;
                            base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Arcs/Arc20"], toDraw, nullable7, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                    }
                    //Original by The Doctor, modified by McShooterz
                    else if (slot.Module.FieldOfFire == 45f && ResourceManager.TextureDict.ContainsKey("Arcs/Arc45"))
                    {
                        Vector2 Center = new Vector2((float)(slot.PQ.enclosingRect.X + 16 * slot.Module.XSIZE / 2), (float)(slot.PQ.enclosingRect.Y + 16 * slot.Module.YSIZE / 2));
                        Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.Module.InstalledWeapon.Tag_Cannon && !slot.Module.InstalledWeapon.Tag_Energy)
                        {
                            Color drawcolor = new Color(255, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable4 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc45"], toDraw, nullable4, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Railgun || slot.Module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc45"], toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Cannon)
                        {
                            Color drawcolor = new Color(0, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc45"], toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (!slot.Module.InstalledWeapon.isBeam)
                        {
                            Color drawcolor = new Color(255, 0, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable6 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc45"], toDraw, nullable6, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Color drawcolor = new Color(0, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable7 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc45"], toDraw, nullable7, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                    }
                    //Original by The Doctor, modified by McShooterz
                    else if (slot.Module.FieldOfFire == 120f && Ship_Game.ResourceManager.TextureDict.ContainsKey("Arcs/Arc120"))
                    {
                        Vector2 Center = new Vector2((float)(slot.PQ.enclosingRect.X + 16 * slot.Module.XSIZE / 2), (float)(slot.PQ.enclosingRect.Y + 16 * slot.Module.YSIZE / 2));
                        Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.Module.InstalledWeapon.Tag_Cannon && !slot.Module.InstalledWeapon.Tag_Energy)
                        {
                            Color drawcolor = new Color(255, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable4 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc120"], toDraw, nullable4, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Railgun || slot.Module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc120"], toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Cannon)
                        {
                            Color drawcolor = new Color(0, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc120"], toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (!slot.Module.InstalledWeapon.isBeam)
                        {
                            Color drawcolor = new Color(255, 0, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable6 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc120"], toDraw, nullable6, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Color drawcolor = new Color(0, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable7 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc120"], toDraw, nullable7, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                    }
                    //Original by The Doctor, modified by McShooterz
                    else if (slot.Module.FieldOfFire == 60f && Ship_Game.ResourceManager.TextureDict.ContainsKey("Arcs/Arc60"))
                    {
                        Vector2 Center = new Vector2((float)(slot.PQ.enclosingRect.X + 16 * slot.Module.XSIZE / 2), (float)(slot.PQ.enclosingRect.Y + 16 * slot.Module.YSIZE / 2));
                        Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.Module.InstalledWeapon.Tag_Cannon && !slot.Module.InstalledWeapon.Tag_Energy)
                        {
                            Color drawcolor = new Color(255, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable4 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc60"], toDraw, nullable4, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Railgun || slot.Module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc60"], toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Cannon)
                        {
                            Color drawcolor = new Color(0, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc60"], toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (!slot.Module.InstalledWeapon.isBeam)
                        {
                            Color drawcolor = new Color(255, 0, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable6 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc60"], toDraw, nullable6, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Color drawcolor = new Color(0, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable7 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc60"], toDraw, nullable7, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                    }
                    //Original by The Doctor, modified by McShooterz
                    else if (slot.Module.FieldOfFire == 360f && Ship_Game.ResourceManager.TextureDict.ContainsKey("Arcs/Arc360"))
                    {
                        Vector2 Center = new Vector2((float)(slot.PQ.enclosingRect.X + 16 * slot.Module.XSIZE / 2), (float)(slot.PQ.enclosingRect.Y + 16 * slot.Module.YSIZE / 2));
                        Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.Module.InstalledWeapon.Tag_Cannon && !slot.Module.InstalledWeapon.Tag_Energy)
                        {
                            Color drawcolor = new Color(255, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable4 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc360"], toDraw, nullable4, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Railgun || slot.Module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc360"], toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Cannon)
                        {
                            Color drawcolor = new Color(0, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc360"], toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (!slot.Module.InstalledWeapon.isBeam)
                        {
                            Color drawcolor = new Color(255, 0, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable6 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc360"], toDraw, nullable6, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Color drawcolor = new Color(0, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable7 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc360"], toDraw, nullable7, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                    }
                    //Original by The Doctor, modified by McShooterz
                    else if (slot.Module.FieldOfFire == 180f && Ship_Game.ResourceManager.TextureDict.ContainsKey("Arcs/Arc180"))
                    {
                        Vector2 Center = new Vector2((float)(slot.PQ.enclosingRect.X + 16 * slot.Module.XSIZE / 2), (float)(slot.PQ.enclosingRect.Y + 16 * slot.Module.YSIZE / 2));
                        Vector2 Origin = new Vector2(250f, 250f);
                        if (slot.Module.InstalledWeapon.Tag_Cannon && !slot.Module.InstalledWeapon.Tag_Energy)
                        {
                            Color drawcolor = new Color(255, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable4 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc180"], toDraw, nullable4, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Railgun || slot.Module.InstalledWeapon.Tag_Subspace)
                        {
                            Color drawcolor = new Color(255, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc180"], toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (slot.Module.InstalledWeapon.Tag_Cannon)
                        {
                            Color drawcolor = new Color(0, 255, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable5 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc180"], toDraw, nullable5, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else if (!slot.Module.InstalledWeapon.isBeam)
                        {
                            Color drawcolor = new Color(255, 0, 0, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable6 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc180"], toDraw, nullable6, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                        else
                        {
                            Color drawcolor = new Color(0, 0, 255, 255);
                            Rectangle toDraw = new Rectangle((int)Center.X, (int)Center.Y, 500, 500);
                            Rectangle? nullable7 = null;
                            base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["Arcs/Arc180"], toDraw, nullable7, drawcolor, (float)slot.Module.Facing.ToRadians(), Origin, SpriteEffects.None, 1f);
                        }
                    }
                    //Original by The Doctor, modified by McShooterz
                    else
                    {
                        if (slot.Module.FieldOfFire == 0f)
                        {
                            continue;
                        }
                        float halfArc = slot.Module.FieldOfFire / 2f;
                        Vector2 Center = new Vector2((float)(slot.PQ.enclosingRect.X + 16 * slot.Module.XSIZE / 2), (float)(slot.PQ.enclosingRect.Y + 16 * slot.Module.YSIZE / 2));
                        Vector2 leftArc  = Center.PointFromAngle(slot.Module.Facing + -halfArc, 300f);
                        Vector2 rightArc = Center.PointFromAngle(slot.Module.Facing + halfArc, 300f);
                        Color arc = new Color(255, 165, 0, 100);
                        Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, Center, leftArc, arc, 3f);
                        Primitives2D.DrawLine(base.ScreenManager.SpriteBatch, Center, rightArc, arc, 3f);
                    }
                }
                foreach (SlotStruct ss in this.Slots)
                {
                    if (ss.Module == null)
                    {
                        continue;
                    }
                    Vector2 Center = new Vector2((float)(ss.PQ.X + 16 * ss.Module.XSIZE / 2), (float)(ss.PQ.Y + 16 * ss.Module.YSIZE / 2));
                    Vector2 lightOrigin = new Vector2(8f, 8f);
                    if (ss.Module.PowerDraw <= 0f || ss.Module.Powered || ss.Module.ModuleType == ShipModuleType.PowerConduit)
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
                ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(mb.ModuleUID);
                Rectangle modRect = new Rectangle(0, 0, moduleTemplate.XSIZE * 16, moduleTemplate.YSIZE * 16);
                //{
                    modRect.X = mb.moduleRect.X + 64 - modRect.Width / 2;
                    modRect.Y = mb.moduleRect.Y + 64 - modRect.Height / 2;
                //};
                base.ScreenManager.SpriteBatch.Draw(ResourceManager.Texture(moduleTemplate.IconTexturePath), modRect, Color.White);
                float nWidth = Fonts.Arial12.MeasureString(Localizer.Token(moduleTemplate.NameIndex)).X;
                Vector2 nameCursor = new Vector2((float)(mb.moduleRect.X + 64) - nWidth / 2f, (float)(mb.moduleRect.Y + 128 - Fonts.Arial12.LineSpacing - 2));
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, Localizer.Token(moduleTemplate.NameIndex), nameCursor, Color.White);
            }
            float single = (float)Mouse.GetState().X;
            MouseState state = Mouse.GetState();
            Vector2 MousePos = new Vector2(single, (float)state.Y);
            if (this.ActiveModule != null && !HelperFunctions.CheckIntersection(this.activeModSubMenu.Menu, MousePos) && !HelperFunctions.CheckIntersection(this.modSel.Menu, MousePos) && (!HelperFunctions.CheckIntersection(this.choosefighterrect, MousePos) || this.ActiveModule.ModuleType != ShipModuleType.Hangar || this.ActiveModule.IsSupplyBay || this.ActiveModule.IsTroopBay))
            {
                ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(ActiveModule.UID);

                Rectangle r = new Rectangle(this.mouseStateCurrent.X, this.mouseStateCurrent.Y, (int)((float)(16 * this.ActiveModule.XSIZE) * this.camera.Zoom), (int)((float)(16 * this.ActiveModule.YSIZE) * this.camera.Zoom));
                switch (this.ActiveModState)
                {
                    case ActiveModuleState.Normal:
                    {
                        base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[moduleTemplate.IconTexturePath], r, Color.White);
                        break;
                    }
                    case ActiveModuleState.Left:
                    {
                        r.Y = r.Y + (int)((16 * moduleTemplate.XSIZE) * camera.Zoom);
                        int h = r.Height;
                        int w = r.Width;
                        r.Width = h;
                        r.Height = w;
                        base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[moduleTemplate.IconTexturePath], r, null, Color.White, -1.57079637f, Vector2.Zero, SpriteEffects.None, 1f);
                        break;
                    }
                    case ActiveModuleState.Right:
                    {
                        r.X = r.X + (int)((16 * moduleTemplate.YSIZE) * camera.Zoom);
                        int h = r.Height;
                        int w = r.Width;
                        r.Width  = h;
                        r.Height = w;
                        base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[moduleTemplate.IconTexturePath], r, null, Color.White, 1.57079637f, Vector2.Zero, SpriteEffects.None, 1f);
                        break;
                    }
                    case ActiveModuleState.Rear:
                    {
                        base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[moduleTemplate.IconTexturePath], r, null, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 1f);
                        break;
                    }
                }
                if (this.ActiveModule.shield_power_max > 0f)
                {
                    Vector2 center = new Vector2((float)this.mouseStateCurrent.X, (float)this.mouseStateCurrent.Y) + new Vector2((float)(moduleTemplate.XSIZE * 16 / 2), (float)(moduleTemplate.YSIZE * 16 / 2));
                    DrawCircle(center, this.ActiveModule.shield_radius * this.camera.Zoom, 50, Color.LightGreen);
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
                Vector2 Pos = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Arial20Bold.MeasureString("Debug").X / 2, 120f);
                HelperFunctions.DrawDropShadowText(base.ScreenManager, "Debug", Pos, Fonts.Arial20Bold);
                Pos = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Arial20Bold.MeasureString(this.operation.ToString()).X / 2, 140f);
                HelperFunctions.DrawDropShadowText(base.ScreenManager, this.operation.ToString(), Pos, Fonts.Arial20Bold);
#if SHIPYARD
                string Ratios = "I: " + TotalI + "      O: " + TotalO + "      E: " + TotalE + "      IO: " + TotalIO + "      IE: " + TotalIE + "      OE: " + TotalOE + "      IOE: " + TotalIOE;
                Pos = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - Fonts.Arial20Bold.MeasureString(Ratios).X / 2, 180f);
                HelperFunctions.DrawDropShadowText(base.ScreenManager, Ratios, Pos, Fonts.Arial20Bold);
#endif
            }			this.close.Draw(base.ScreenManager);
            base.ScreenManager.SpriteBatch.End();
            lock (GlobalStats.ObjectManagerLocker)
            {
                base.ScreenManager.inter.EndFrameRendering();
                base.ScreenManager.editor.EndFrameRendering();
                base.ScreenManager.sceneState.EndFrameRendering();
            }
        }

        private void DrawString(ref Vector2 cursorPos, string text, SpriteFont font = null)
        {
            if (font == null) font = Fonts.Arial8Bold;
            ScreenManager.SpriteBatch.DrawString(font, text, cursorPos, Color.SpringGreen);
            cursorPos.X = cursorPos.X + Fonts.Arial8Bold.MeasureString(text).X;
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
                mod.HealthMax = ResourceManager.GetModuleTemplate(mod.UID).HealthMax;
                 
            }
            if (!activeModSubMenu.Tabs[0].Selected || mod == null)
                return;

            ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(mod.UID);

            //Added by McShooterz: Changed how modules names are displayed for allowing longer names
            Vector2 modTitlePos = new Vector2((float)(this.activeModSubMenu.Menu.X + 10), (float)(this.activeModSubMenu.Menu.Y + 35));
            if (Fonts.Arial20Bold.MeasureString(Localizer.Token(moduleTemplate.NameIndex)).X + 16 < this.activeModSubMenu.Menu.Width)
            {
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, Localizer.Token(moduleTemplate.NameIndex), modTitlePos, Color.White);
                modTitlePos.Y = modTitlePos.Y + (float)(Fonts.Arial20Bold.LineSpacing + 6);
            }
            else
            {
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial14Bold, Localizer.Token(moduleTemplate.NameIndex), modTitlePos, Color.White);
                modTitlePos.Y = modTitlePos.Y + (float)(Fonts.Arial14Bold.LineSpacing + 4);
            }
            string rest = "";
            if (moduleTemplate.Restrictions == Restrictions.IO)
            {
                rest = "Any Slot except E";
            }
            else if (moduleTemplate.Restrictions == Restrictions.I)
            {
                rest = "I, IO, IE or IOE";
            }
            else if (moduleTemplate.Restrictions == Restrictions.O)
            {
                rest = "O, IO, OE, or IOE";
            }
            else if (moduleTemplate.Restrictions == Restrictions.E)
            {
                rest = "E, IE, OE, or IOE";
            }
            else if (moduleTemplate.Restrictions == Restrictions.IOE)
            {
                rest = "Any Slot";
            }
            else if (moduleTemplate.Restrictions == Restrictions.IE)
            {
                rest = "Any Slot except O";
            }
            else if (moduleTemplate.Restrictions == Restrictions.OE)
            {
                rest = "Any Slot except I";
            }

            // Concat ship class restrictions
            string shipRest = "";
            bool specialString = false;

            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useDrones && GlobalStats.ActiveModInfo.useDestroyers)
            {
                if (!mod.FightersOnly && mod.DroneModule && mod.FighterModule && mod.CorvetteModule && mod.FrigateModule && mod.DestroyerModule && mod.CruiserModule && mod.CruiserModule && mod.CarrierModule && mod.CarrierModule && mod.PlatformModule && mod.StationModule && mod.FreighterModule)
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
            if (moduleTemplate.IsWeapon && moduleTemplate.BombType == null)
            {
                var weaponTemplate = ResourceManager.GetWeaponTemplate(moduleTemplate.WeaponType);

                var sb = new StringBuilder();
                if (weaponTemplate.Tag_Guided)    sb.Append("GUIDED ");
                if (weaponTemplate.Tag_Intercept) sb.Append("INTERCEPTABLE ");
                if (weaponTemplate.Tag_Energy)    sb.Append("ENERGY ");
                if (weaponTemplate.Tag_Hybrid)    sb.Append("HYBRID ");
                if (weaponTemplate.Tag_Kinetic)   sb.Append("KINETIC ");
                if (weaponTemplate.Tag_Explosive && !weaponTemplate.Tag_Flak) sb.Append("EXPLOSIVE ");
                if (weaponTemplate.Tag_Subspace)  sb.Append("SUBSPACE ");
                if (weaponTemplate.Tag_Warp)      sb.Append("WARP ");
                if (weaponTemplate.Tag_PD)        sb.Append("POINT DEFENSE ");
                if (weaponTemplate.Tag_Flak)      sb.Append("FLAK ");

                if (GlobalStats.HasMod && GlobalStats.ActiveModInfo.expandedWeaponCats && (weaponTemplate.Tag_Missile && !weaponTemplate.Tag_Guided))
                    sb.Append("ROCKET ");
                else if (weaponTemplate.Tag_Missile)
                    sb.Append("MISSILE ");

                if (weaponTemplate.Tag_Tractor)   sb.Append("TRACTOR ");
                if (weaponTemplate.Tag_Beam)      sb.Append("BEAM ");
                if (weaponTemplate.Tag_Array)     sb.Append("ARRAY ");
                if (weaponTemplate.Tag_Railgun)   sb.Append("RAILGUN ");
                if (weaponTemplate.Tag_Torpedo)   sb.Append("TORPEDO ");
                if (weaponTemplate.Tag_Bomb)      sb.Append("BOMB ");
                if (weaponTemplate.Tag_BioWeapon) sb.Append("BIOWEAPON ");
                if (weaponTemplate.Tag_SpaceBomb) sb.Append("SPACEBOMB ");
                if (weaponTemplate.Tag_Drone)     sb.Append("DRONE ");
                if (weaponTemplate.Tag_Cannon)    sb.Append("CANNON ");
                DrawString(ref modTitlePos, sb.ToString(), Fonts.Arial8Bold);

                modTitlePos.Y = modTitlePos.Y + (Fonts.Arial8Bold.LineSpacing + 5);
                modTitlePos.X = startx;
            }

            string txt = this.parseText(Localizer.Token(moduleTemplate.DescriptionIndex), (float)(this.activeModSubMenu.Menu.Width - 20), Fonts.Arial12);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, txt, modTitlePos, Color.White);
            modTitlePos.Y = modTitlePos.Y + (Fonts.Arial12Bold.MeasureString(txt).Y + 8f);
            float starty = modTitlePos.Y;
            float strength = ResourceManager.CalculateModuleOffenseDefense(mod, ActiveHull.ModuleSlots.Length);                
            if (strength > 0)
            {
                this.DrawStat(ref modTitlePos, "Offense", (float)strength, 227);
                modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
            }
            if (!mod.isWeapon || mod.InstalledWeapon == null)
            {
                if (mod.Cost != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(128), (float)mod.Cost * UniverseScreen.GamePaceStatic, 84);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.Mass != 0)
                {
                    float MassMod = (float)EmpireManager.Player.data.MassModifier;
                    float ArmourMassMod = (float)EmpireManager.Player.data.ArmourMassModifier;

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
                    this.DrawStat(ref modTitlePos, Localizer.Token(124), (float)mod.HealthMax + mod.HealthMax * (float)EmpireManager.Player.data.Traits.ModHpModifier, 80);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.ModuleType != ShipModuleType.PowerPlant)
                {
                    powerDraw = -(float)mod.PowerDraw;
                }
                else
                {
                    powerDraw = (mod.PowerDraw > 0f ? (float)(-mod.PowerDraw) : mod.PowerFlowMax + mod.PowerFlowMax * EmpireManager.Player.data.PowerFlowMod);
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
                    this.DrawStat(ref modTitlePos, string.Concat(Localizer.Token(135), "+"), (float)((mod.BonusRepairRate + mod.BonusRepairRate * EmpireManager.Player.data.Traits.RepairMod) * (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull) ? 1f + Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].RepairBonus : 1)), 97);
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
                    this.DrawStat(ref modTitlePos, Localizer.Token(132), mod.shield_power_max * (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useHullBonuses && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull) ? 1f + Ship_Game.ResourceManager.HullBonuses[this.ActiveHull.Hull].ShieldBonus : 1f) + EmpireManager.Player.data.ShieldPowerMod * mod.shield_power_max, 93);
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
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6162), (float)mod.shield_kinetic_resist, 209, Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_energy_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6163), (float)mod.shield_energy_resist, 210, Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_explosive_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6164), (float)mod.shield_explosive_resist, 211, Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_missile_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6165), (float)mod.shield_missile_resist, 212, Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_flak_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6166), (float)mod.shield_flak_resist, 213, Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_hybrid_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6167), (float)mod.shield_hybrid_resist, 214, Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_railgun_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6168), (float)mod.shield_railgun_resist, 215, Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_subspace_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6169), (float)mod.shield_subspace_resist, 216, Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_warp_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6170), (float)mod.shield_warp_resist, 217, Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_beam_resist != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6171), (float)mod.shield_beam_resist, 218, Color.LightSkyBlue, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.shield_threshold != 0)
                {
                    this.DrawStatColor(ref modTitlePos, Localizer.Token(6176), (float)mod.shield_threshold, 222, Color.LightSkyBlue, isPercent:true);
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
                    this.DrawStat(ref modTitlePos, Localizer.Token(2235), (float)(mod.PowerStoreMax + mod.PowerStoreMax * EmpireManager.Player.data.FuelCellModifier), 145);
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
                    this.DrawStat(ref modTitlePos, Localizer.Token(6004), (float)mod.ECM, 154, isPercent:true);
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
                    this.DrawStat(ref modTitlePos, Localizer.Token(6142), (float)mod.KineticResist, 189, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.EnergyResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6143), (float)mod.EnergyResist, 190, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.GuidedResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6144), (float)mod.GuidedResist, 191, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.MissileResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6145), (float)mod.MissileResist, 192, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.HybridResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6146), (float)mod.HybridResist, 193, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.BeamResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6147), (float)mod.BeamResist, 194, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.ExplosiveResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6148), (float)mod.ExplosiveResist, 195, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.InterceptResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6149), (float)mod.InterceptResist, 196, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.RailgunResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6150), (float)mod.RailgunResist, 197, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.SpaceBombResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6151), (float)mod.SpaceBombResist, 198, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.BombResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6152), (float)mod.BombResist, 199, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.BioWeaponResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6153), (float)mod.BioWeaponResist, 200, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.DroneResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6154), (float)mod.DroneResist, 201, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.WarpResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6155), (float)mod.WarpResist, 202, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TorpedoResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6156), (float)mod.TorpedoResist, 203, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.CannonResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6157), (float)mod.CannonResist, 204, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.SubspaceResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6158), (float)mod.SubspaceResist, 205, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.PDResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6159), (float)mod.PDResist, 206, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.FlakResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6160), (float)mod.FlakResist, 207, isPercent: true);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.APResist != 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6161), (float)mod.APResist, 208, isPercent: true);
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
                if (mod.FixedTracking > 0)
                {
                    this.DrawStat(ref modTitlePos, Localizer.Token(6187), (float)mod.FixedTracking, 231);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }
                if (mod.TargetTracking > 0)
                {
                    this.DrawStat(ref modTitlePos, "+" + Localizer.Token(6186), (float)mod.TargetTracking, 226);
                    modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                }


                if (mod.PermittedHangarRoles.Length > 0)
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
                this.DrawStat(ref modTitlePos, Localizer.Token(123), (float)EmpireManager.Player.data.MassModifier * mod.Mass, 79);
                modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                this.DrawStat(ref modTitlePos, Localizer.Token(124), (float)mod.HealthMax + EmpireManager.Player.data.Traits.ModHpModifier * mod.HealthMax, 80);
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
                    this.DrawStat(ref modTitlePos, Localizer.Token(127), (float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() + EmpireManager.Player.data.OrdnanceEffectivenessBonus * mod.InstalledWeapon.DamageAmount, 83);
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
                            float dps = 1f / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) * ((float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() + EmpireManager.Player.data.OrdnanceEffectivenessBonus * mod.InstalledWeapon.DamageAmount);
                            dps = dps * (float)mod.InstalledWeapon.ProjectileCount;
                            this.DrawStat(ref modTitlePos, "DPS", dps, 86);
                            modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                        }
                        else
                        {
                            float dps = (float)mod.InstalledWeapon.SalvoCount / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) * ((float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() + EmpireManager.Player.data.OrdnanceEffectivenessBonus * mod.InstalledWeapon.DamageAmount);
                            dps = dps * (float)mod.InstalledWeapon.ProjectileCount;
                            this.DrawStat(ref modTitlePos, "DPS", dps, 86);
                            modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                            this.DrawStat(ref modTitlePos, "Salvo", (float)mod.InstalledWeapon.SalvoCount, 182);
                            modTitlePos.Y += (float)Fonts.Arial12Bold.LineSpacing;
                        }
                    }
                    else if (mod.InstalledWeapon.SalvoCount <= 1)
                    {
                        float dps = 1f / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) * ((float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() + (float)mod.InstalledWeapon.DamageAmount * EmpireManager.Player.data.Traits.EnergyDamageMod);
                        dps = dps * (float)mod.InstalledWeapon.ProjectileCount;
                        this.DrawStat(ref modTitlePos, "DPS", dps, 86);
                        modTitlePos.Y = modTitlePos.Y + (float)Fonts.Arial12Bold.LineSpacing;
                    }
                    else
                    {
                        float dps = (float)mod.InstalledWeapon.SalvoCount / (ModifiedWeaponStat(mod.InstalledWeapon, "firedelay") * GetHullFireRateBonus()) * ((float)ModifiedWeaponStat(mod.InstalledWeapon, "damage") * GetHullDamageBonus() + (float)mod.InstalledWeapon.DamageAmount * EmpireManager.Player.data.Traits.EnergyDamageMod);
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
                    this.DrawStat(ref modTitlePos, Localizer.Token(6005), (float)mod.InstalledWeapon.ECMResist, 155, isPercent:true);
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
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, Localizer.GetRole((e.item as ShipData).Role, EmpireManager.Player), tCursor, Color.Orange);
                    if (HelperFunctions.CheckIntersection(e.clickRect, MousePos))
                    {
                        if (e.clickRectHover == 0)
                        {
                            GameAudio.PlaySfxAsync("sd_ui_mouseover");
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
                else if (e.item is ShipModule mod)
                {
                    bCursor.X += 5f;
                    ShipModule moduleTemplate = ResourceManager.GetModuleTemplate(mod.UID);
                    Rectangle modRect = new Rectangle((int)bCursor.X, (int)bCursor.Y, Ship_Game.ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Width, Ship_Game.ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Height);
                    Vector2 vector2 = new Vector2(bCursor.X + 15f, bCursor.Y + 15f);
                    Vector2 vector21 = new Vector2((float)(Ship_Game.ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Width / 2), (float)(Ship_Game.ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Height / 2));
                    float aspectRatio = (float)Ship_Game.ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Width / (float)Ship_Game.ResourceManager.TextureDict[moduleTemplate.IconTexturePath].Height;
                    float w = (float)modRect.Width;
                    for (h = (float)modRect.Height; w > 30f || h > 30f; h = h - 1.6f)
                    {
                        w = w - aspectRatio * 1.6f;
                    }
                    modRect.Width = (int)w;
                    modRect.Height = (int)h;
                    base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[moduleTemplate.IconTexturePath], modRect, Color.White);
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
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial8Bold, moduleTemplate.Restrictions.ToString(), tCursor, Color.Orange);
                    tCursor.X = tCursor.X + Fonts.Arial8Bold.MeasureString(moduleTemplate.Restrictions.ToString()).X;
                    if (moduleTemplate.InstalledWeapon != null && moduleTemplate.ModuleType != ShipModuleType.Turret || moduleTemplate.XSIZE != moduleTemplate.YSIZE)
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
                            GameAudio.PlaySfxAsync("sd_ui_mouseover");
                        }
                        e.clickRectHover = 1;
                    }
                }
            }
        }

        private bool RestrictedModCheck(ShipData.RoleName Role, ShipModule Mod)
        {

            if (Mod.FighterModule || Mod.CorvetteModule || Mod.FrigateModule || Mod.StationModule || Mod.DestroyerModule || Mod.CruiserModule
             || Mod.CarrierModule || Mod.CapitalModule || Mod.FreighterModule || Mod.PlatformModule || Mod.DroneModule)
            {  
                if (Role == ShipData.RoleName.drone && Mod.DroneModule == false) return true;
                if (Role == ShipData.RoleName.scout && Mod.FighterModule == false) return true;
                if (Role == ShipData.RoleName.fighter && Mod.FighterModule == false) return true;
                if (Role == ShipData.RoleName.corvette && Mod.CorvetteModule == false) return true;
                if (Role == ShipData.RoleName.gunboat && Mod.CorvetteModule == false) return true;
                if (Role == ShipData.RoleName.frigate && Mod.FrigateModule == false) return true;
                if (Role == ShipData.RoleName.destroyer && Mod.DestroyerModule == false) return true;
                if (Role == ShipData.RoleName.cruiser && Mod.CruiserModule == false) return true;
                if (Role == ShipData.RoleName.carrier && Mod.CarrierModule == false) return true;
                if (Role == ShipData.RoleName.capital && Mod.CapitalModule == false) return true;
                if (Role == ShipData.RoleName.freighter && Mod.FreighterModule == false) return true;
                if (Role == ShipData.RoleName.platform && Mod.PlatformModule == false) return true;
                if (Role == ShipData.RoleName.station && Mod.StationModule == false) return true;
            }
            else if (Mod.FightersOnly)
            {
                if (Role == ShipData.RoleName.fighter) return true;
                if (Role == ShipData.RoleName.scout) return true;
                if (Role == ShipData.RoleName.corvette) return true;
                if (Role == ShipData.RoleName.gunboat) return true;
            }

            return false;
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
                    Array<string> WeaponCategories = new Array<string>();
                    foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                    {
                        if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                        {
                            continue;
                        }
                        module.Value.ModuleType.ToString();
                        ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                        tmp.SetAttributesNoParent();

                        if (RestrictedModCheck(this.ActiveHull.Role, tmp)) continue;

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
                        foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                        {
                            if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                            {
                                continue;
                            }
                            ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                            tmp.SetAttributesNoParent();                            
                            bool restricted = tmp.FighterModule || tmp.CorvetteModule || tmp.FrigateModule || tmp.StationModule || tmp.DestroyerModule || tmp.CruiserModule
                            || tmp.CarrierModule || tmp.CapitalModule || tmp.FreighterModule || tmp.PlatformModule || tmp.DroneModule;
                            if (restricted)
                            {
                                //mer
                            }
                            // if not using new tags, ensure original <FightersOnly> still functions as in vanilla.
                            else if (!restricted && tmp.FightersOnly && this.ActiveHull.Role != ShipData.RoleName.fighter && this.ActiveHull.Role != ShipData.RoleName.scout && this.ActiveHull.Role != ShipData.RoleName.corvette && this.ActiveHull.Role != ShipData.RoleName.gunboat)
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
                    Array<string> ModuleCategories = new Array<string>();
                    foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                    {
                        if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                        {
                            continue;
                        }
                        module.Value.ModuleType.ToString();
                        ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                        tmp.SetAttributesNoParent();

                        if (RestrictedModCheck(this.ActiveHull.Role, tmp)) continue;

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
                        foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                        {
                            if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                            {
                                continue;
                            }
                            ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                            tmp.SetAttributesNoParent();

                            if (RestrictedModCheck(this.ActiveHull.Role, tmp)) continue;

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
                    Array<string> ModuleCategories = new Array<string>();
                    foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                    {
                        if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                        {
                            continue;
                        }
                        module.Value.ModuleType.ToString();
                        ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                        tmp.SetAttributesNoParent();

                        if (RestrictedModCheck(this.ActiveHull.Role, tmp)) continue;

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
                        foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                        {
                            if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                            {
                                continue;
                            }
                            ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                            tmp.SetAttributesNoParent();

                            if (RestrictedModCheck(this.ActiveHull.Role, tmp)) continue;

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
                    Array<string> ModuleCategories = new Array<string>();
                    foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                    {
                        if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                        {
                            continue;
                        }
                        module.Value.ModuleType.ToString();
                        ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                        tmp.SetAttributesNoParent();

                        if (RestrictedModCheck(this.ActiveHull.Role, tmp)) continue;

                        if ((tmp.ModuleType == ShipModuleType.Troop || tmp.ModuleType == ShipModuleType.Colony || tmp.ModuleType == ShipModuleType.Command || tmp.ModuleType == ShipModuleType.Storage || tmp.ModuleType == ShipModuleType.Hangar || tmp.ModuleType == ShipModuleType.Sensors || tmp.ModuleType == ShipModuleType.Special || tmp.ModuleType == ShipModuleType.Transporter || tmp.ModuleType == ShipModuleType.Ordnance || tmp.ModuleType == ShipModuleType.Construction) && !ModuleCategories.Contains(tmp.ModuleType.ToString()))
                        {
                            ModuleCategories.Add(tmp.ModuleType.ToString());
                            ModuleHeader type = new ModuleHeader(tmp.ModuleType.ToString(), 240f);
                            this.weaponSL.AddItem(type);
                        }
                        tmp = null;
                    }
                    foreach (ScrollList.Entry e in this.weaponSL.Entries)
                    {
                        foreach (KeyValuePair<string, ShipModule> module in ResourceManager.ShipModules)
                        {
                            if (!EmpireManager.Player.GetMDict()[module.Key] || module.Value.UID == "Dummy")
                            {
                                continue;
                            }
                            ShipModule tmp = ShipModule.CreateNoParent(module.Key);
                            tmp.SetAttributesNoParent();

                            if (RestrictedModCheck(this.ActiveHull.Role, tmp)) continue;

                            if ((tmp.ModuleType == ShipModuleType.Troop || tmp.ModuleType == ShipModuleType.Colony || tmp.ModuleType == ShipModuleType.Command || tmp.ModuleType == ShipModuleType.Storage || tmp.ModuleType == ShipModuleType.Hangar || tmp.ModuleType == ShipModuleType.Sensors || tmp.ModuleType == ShipModuleType.Special || tmp.ModuleType == ShipModuleType.Transporter || tmp.ModuleType == ShipModuleType.Ordnance || tmp.ModuleType == ShipModuleType.Construction) && (e.item as ModuleHeader).Text == tmp.ModuleType.ToString())
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
            if (GlobalStats.IsGermanFrenchOrPolish)
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
            float HitPoints           = 0f;
            float Mass                = 0f;
            float PowerDraw           = 0f;
            float PowerCapacity       = 0f;
            float OrdnanceCap         = 0f;
            float PowerFlow           = 0f;
            float ShieldPower         = 0f;
            float Thrust              = 0f;
            float AfterThrust         = 0f;
            float CargoSpace          = 0f;
            int TroopCount            = 0;
            float Size                = 0f;
            float Cost                = 0f;
            float WarpThrust          = 0f;
            float TurnThrust          = 0f;
            float WarpableMass        = 0f;
            float WarpDraw            = 0f;
            float FTLCount            = 0f;
            float FTLSpeed            = 0f;
            float RepairRate          = 0f;
            float sensorRange         = 0f;
            float sensorBonus         = 0f;
            float BeamLongestDuration = 0f;
            float OrdnanceUsed        =0f;
            float OrdnanceRecoverd    = 0f;
            float WeaponPowerNeeded   = 0f;
            float Upkeep              = 0f;
            float FTLSpoolTimer       = 0f;
            float EMPResist           = 0f;
            bool bEnergyWeapons       = false;
            float Off                 = 0f;
            float Def                 = 0;
            float strength            = 0;
            float targets             = 0;
            int fixedtargets          = 0;
            float TotalECM            = 0f;

            // bonuses are only available in mods
            ResourceManager.HullBonuses.TryGetValue(ActiveHull.Hull, out HullBonus bonus);

            foreach (SlotStruct slot in this.Slots)
            {
                Size = Size + 1f;
                if (slot.Module == null)
                {
                    continue;
                }
                HitPoints = HitPoints + (slot.Module.Health + EmpireManager.Player.data.Traits.ModHpModifier * slot.Module.Health);
                if (slot.Module.Mass < 0f && slot.Powered)
                {
                    if (slot.Module.ModuleType == ShipModuleType.Armor)
                    {
                        Mass += slot.Module.Mass * EmpireManager.Player.data.ArmourMassModifier;
                    }
                    else
                        Mass += slot.Module.Mass;
                }
                else if (slot.Module.Mass > 0f)
                {
                    if (slot.Module.ModuleType == ShipModuleType.Armor)
                    {
                        Mass += slot.Module.Mass * EmpireManager.Player.data.ArmourMassModifier;
                    }
                    else
                        Mass += slot.Module.Mass;
                }
                TroopCount += slot.Module.TroopCapacity;
                PowerCapacity += slot.Module.PowerStoreMax + slot.Module.PowerStoreMax * EmpireManager.Player.data.FuelCellModifier; 
                OrdnanceCap = OrdnanceCap + (float)slot.Module.OrdinanceCapacity;
                PowerFlow += slot.Module.PowerFlowMax + slot.Module.PowerFlowMax * EmpireManager.Player.data.PowerFlowMod;
                if (slot.Module.Powered)
                {
                    EMPResist += slot.Module.EMP_Protection;
                    WarpableMass = WarpableMass + slot.Module.WarpMassCapacity;
                    PowerDraw = PowerDraw + slot.Module.PowerDraw;
                    WarpDraw = WarpDraw + slot.Module.PowerDrawAtWarp;
                    if (slot.Module.ECM > TotalECM)
                        TotalECM = slot.Module.ECM;
                    if (slot.Module.InstalledWeapon != null && slot.Module.InstalledWeapon.PowerRequiredToFire > 0)
                        bEnergyWeapons = true;
                    if (slot.Module.InstalledWeapon != null && slot.Module.InstalledWeapon.BeamPowerCostPerSecond > 0)
                        bEnergyWeapons = true;
                    if (slot.Module.FTLSpeed > 0f)
                    {
                        FTLCount = FTLCount + 1f;
                        FTLSpeed = FTLSpeed + slot.Module.FTLSpeed;
                    }
                    if (slot.Module.FTLSpoolTime * EmpireManager.Player.data.SpoolTimeModifier > FTLSpoolTimer)
                    {
                        FTLSpoolTimer = slot.Module.FTLSpoolTime * EmpireManager.Player.data.SpoolTimeModifier;
                    }
                    ShieldPower += slot.Module.shield_power_max + EmpireManager.Player.data.ShieldPowerMod * slot.Module.shield_power_max;
                    Thrust = Thrust + slot.Module.thrust;
                    WarpThrust = WarpThrust + slot.Module.WarpThrust;
                    TurnThrust = TurnThrust + slot.Module.TurnThrust;

                    RepairRate += ((slot.Module.BonusRepairRate + slot.Module.BonusRepairRate * 
                        EmpireManager.Player.data.Traits.RepairMod) * (1f + bonus?.RepairBonus??0));
                    OrdnanceRecoverd += slot.Module.OrdnanceAddedPerSecond;
                    if (slot.Module.SensorRange > sensorRange)
                    {
                        sensorRange = slot.Module.SensorRange;
                    }
                    if (slot.Module.SensorBonus > sensorBonus)
                        sensorBonus = slot.Module.SensorBonus;
                    
                    //added by gremlin collect weapon stats                  
                    if (slot.Module.isWeapon || slot.Module.BombType != null)
                    {
                        Weapon weapon;
                        if (slot.Module.BombType == null)
                            weapon = slot.Module.InstalledWeapon;
                        else
                            weapon = ResourceManager.WeaponsDict[slot.Module.BombType];
                        OrdnanceUsed += weapon.OrdinanceRequiredToFire / weapon.fireDelay * weapon.SalvoCount;
                        WeaponPowerNeeded += weapon.PowerRequiredToFire / weapon.fireDelay * weapon.SalvoCount;
                        if(weapon.isBeam)
                            WeaponPowerNeeded += weapon.BeamPowerCostPerSecond * weapon.BeamDuration / weapon.fireDelay;
                        if(BeamLongestDuration < weapon.BeamDuration)
                            BeamLongestDuration = weapon.BeamDuration; 
                        
                    }
                    //end
                    if (slot.Module.FixedTracking > fixedtargets)
                        fixedtargets = slot.Module.FixedTracking;

                    targets += slot.Module.TargetTracking;

                }
                Cost = Cost + slot.Module.Cost * UniverseScreen.GamePaceStatic;
                CargoSpace = CargoSpace + slot.Module.Cargo_Capacity;

                

                
            }

            targets += fixedtargets;
            
            Mass = Mass + (float)(ActiveHull.ModuleSlots.Length / 2);
            Mass = Mass * EmpireManager.Player.data.MassModifier;
            if (Mass < (float)(ActiveHull.ModuleSlots.Length / 2))
            {
                Mass = (float)(ActiveHull.ModuleSlots.Length / 2);
            }
            float Speed = 0f;
            float WarpSpeed = WarpThrust / (Mass + 0.1f);
            //Added by McShooterz: hull bonus speed
            WarpSpeed *= EmpireManager.Player.data.FTLModifier * (1f + bonus?.SpeedBonus??0);
            float single = WarpSpeed / 1000f;
            string WarpString = string.Concat(single.ToString("#.0"), "k");
            float Turn = 0f;
            if (Mass > 0f)
            {
                Speed = Thrust / Mass;
                Turn = TurnThrust / Mass / 700f;
            }
            float AfterSpeed = AfterThrust / (Mass + 0.1f);
            AfterSpeed = AfterSpeed * EmpireManager.Player.data.SubLightModifier;
            Turn = (float)MathHelper.ToDegrees(Turn);
            Vector2 Cursor = new Vector2((float)(this.statsSub.Menu.X + 10), (float)(this.ShipStats.Menu.Y + 33));
            
            if (bonus != null) //Added by McShooterz: Draw Hull Bonuses
            {
               Vector2 LCursor = new Vector2(this.HullSelectionRect.X - 145, HullSelectionRect.Y + 31);
               if (bonus.ArmoredBonus  != 0 || bonus.ShieldBonus != 0 || bonus.SensorBonus != 0 ||
                   bonus.SpeedBonus    != 0 || bonus.CargoBonus  != 0 || bonus.DamageBonus != 0 ||
                   bonus.FireRateBonus != 0 || bonus.RepairBonus != 0 || bonus.CostBonus != 0)
                {
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Verdana14Bold ,Localizer.Token(6015), LCursor, Color.Orange);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Verdana14Bold.LineSpacing + 2);
                }
                if (bonus.ArmoredBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6016), bonus.ArmoredBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.ShieldBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, "Shield Strength", bonus.ShieldBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.SensorBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6017), bonus.SensorBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.SpeedBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6018), bonus.SpeedBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.CargoBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6019), bonus.CargoBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.DamageBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, "Weapon Damage", bonus.DamageBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.FireRateBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6020), bonus.FireRateBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.RepairBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6013), bonus.RepairBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
                if (bonus.CostBonus != 0)
                {
                    this.DrawHullBonus(ref LCursor, Localizer.Token(6021), bonus.CostBonus);
                    LCursor.Y = LCursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
                }
            }
            //Added by McShooterz: hull bonus starting cost
            DrawStat(ref Cursor, Localizer.Token(109)+":", ((int)Cost + (bonus?.StartingCost ?? 0)) * (1f - bonus?.CostBonus ?? 0), 99);
            Cursor.Y += Fonts.Arial12Bold.LineSpacing + 2;

            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.useProportionalUpkeep)
            {
                Upkeep = GetMaintCostShipyardProportional(this.ActiveHull, Cost, EmpireManager.Player);
            }
            else
            {
                Upkeep = GetMaintCostShipyard(this.ActiveHull, Size, EmpireManager.Player);
            }

            this.DrawStat(ref Cursor, "Upkeep Cost:", -Upkeep, 175);
            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);   //Gretman (so we can see how many total slots are on the ships)
            this.DrawStat(ref Cursor, "Ship UniverseRadius:", (float)ActiveHull.ModuleSlots.Length, 230);
            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);

            this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(110), ":"), PowerCapacity, 100, Color.LightSkyBlue);
            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(111), ":"), (PowerFlow - PowerDraw), 101, Color.LightSkyBlue);
            
            //added by McShooterz: Allow Warp draw and after burner values be displayed in ship info
            float fDrawAtWarp = 0;
            if (WarpDraw != 0)
            {
                fDrawAtWarp = (PowerFlow - (WarpDraw / 2 * EmpireManager.Player.data.FTLPowerDrainModifier + (PowerDraw * EmpireManager.Player.data.FTLPowerDrainModifier)));
                if (WarpSpeed > 0)
                {
                    Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(112), ":"), fDrawAtWarp, 102, Color.LightSkyBlue);
                }

            }
            else
            {
                fDrawAtWarp = (PowerFlow - PowerDraw * EmpireManager.Player.data.FTLPowerDrainModifier);
                if (WarpSpeed > 0)
                {
                    Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(112), ":"), fDrawAtWarp, 102, Color.LightSkyBlue);
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
                    this.DrawStatColor(ref Cursor, "Power Time:", EnergyDuration, 163, Color.LightSkyBlue);
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
            this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(113), ":"), HitPoints, 103, Color.Goldenrod);
            //Added by McShooterz: draw total repair
            if (RepairRate > 0)
            {
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(6013), ":"), RepairRate, 236, Color.Goldenrod);                
            }
            if (ShieldPower > 0)
            {
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(114), ":"), ShieldPower, 104, Color.Goldenrod);                
            }
            if (EMPResist > 0)
            {
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(6177), ":"), EMPResist, 220, Color.Goldenrod);
            }
            if (TotalECM > 0)
            {
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(6189), ":"), TotalECM, 234, Color.Goldenrod, isPercent:true);
            }

            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
            

            // The Doctor: removed the mass display. It's a meaningless value to the player, and it takes up a valuable line in the limited space.
            //this.DrawStat(ref Cursor, string.Concat(Localizer.Token(115), ":"), (int)Mass, 79);
            //Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);

#region HardcoreRule info
            if (GlobalStats.HardcoreRuleset)
            {
                string massstring = GetNumberString(Mass);
                string wmassstring = GetNumberString(WarpableMass);
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
                    this.DrawStat(ref Cursor, string.Concat(Localizer.Token(2170), ":"), speed, 135);
                    Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                }
            }
#endregion
            else if (WarpSpeed <= 0f)
            {
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(2170), ":"), 0, 135, Color.DarkSeaGreen);
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            }
            else
            {
                this.DrawStatPropulsion(ref Cursor, string.Concat(Localizer.Token(2170), ":"), WarpString, 135);
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            }
            if (WarpSpeed > 0 && FTLSpoolTimer > 0)
            {
                this.DrawStatColor(ref Cursor, "FTL Spool:", FTLSpoolTimer, 177, Color.DarkSeaGreen);
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            }
            this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(116), ":"), (Speed * EmpireManager.Player.data.SubLightModifier * (GlobalStats.ActiveMod != null && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull) ? 1f + bonus.SpeedBonus : 1)), 105, Color.DarkSeaGreen);
            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            //added by McShooterz: afterburn speed
            if (AfterSpeed != 0)
            {
                this.DrawStatColor(ref Cursor, "Afterburner Speed:", AfterSpeed, 105, Color.DarkSeaGreen);
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            }
            this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(117), ":"), Turn, 107, Color.DarkSeaGreen);
            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);
            if (OrdnanceCap > 0)
            {
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(118), ":"), OrdnanceCap, 108, Color.IndianRed);
                
            }
            if (OrdnanceRecoverd > 0)
            {
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                this.DrawStatColor(ref Cursor, "Ordnance Created / s:", OrdnanceRecoverd, 162, Color.IndianRed);
                
            }
            if (OrdnanceCap > 0)
            {
                float AmmoTime = 0f;
                if (OrdnanceUsed - OrdnanceRecoverd > 0)
                {
                    AmmoTime = OrdnanceCap / (OrdnanceUsed - OrdnanceRecoverd);
                    Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
                    this.DrawStatColor(ref Cursor, "Ammo Time:", AmmoTime, 164, Color.IndianRed);
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
                this.DrawStatColor(ref Cursor, string.Concat(Localizer.Token(6132), ":"), (float)TroopCount, 180, Color.IndianRed);                
            }

            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 10);

            if (CargoSpace > 0)
            {
                this.DrawStat(ref Cursor, string.Concat(Localizer.Token(119), ":"), (CargoSpace + (GlobalStats.ActiveMod != null && GlobalStats.ActiveModInfo.useHullBonuses && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull) ? CargoSpace * bonus.CargoBonus : 0)), 109);
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            }
            if (sensorRange != 0)
            {
                this.DrawStat(ref Cursor, string.Concat(Localizer.Token(6130), ":"), ((sensorRange + sensorBonus) + (GlobalStats.ActiveMod != null && GlobalStats.ActiveModInfo.useHullBonuses && ResourceManager.HullBonuses.ContainsKey(this.ActiveHull.Hull) ? (sensorRange + sensorBonus) * bonus.SensorBonus : 0)), 235);
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            }
            if (targets > 0)
            {
                this.DrawStat(ref Cursor, string.Concat(Localizer.Token(6188), ":"), ((targets + 1f)), 232);
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            }

            Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing);
            bool hasBridge = false;
            bool EmptySlots = true;
            foreach (SlotStruct slot in this.Slots)
            {
                if (slot.ModuleUID == null)
                    EmptySlots = false;

                if (slot.Module != null)
                {
                    Off += ResourceManager.CalculateModuleOffense(slot.Module);
                    Def += ResourceManager.CalculateModuleDefense(slot.Module, (int)Size);
                }
                if (slot.ModuleUID == null || !ResourceManager.GetModuleTemplate(slot.ModuleUID).IsCommandModule)
                    continue;

                hasBridge = true;
            }
            strength = (Def > Off ? Off * 2 : Def + Off);
            if (strength > 0)
            {
                this.DrawStat(ref Cursor, string.Concat(Localizer.Token(6190), ":"), strength, 227);
                Cursor.Y = Cursor.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            }
            Vector2 CursorReq = new Vector2((float)(this.statsSub.Menu.X - 180), (float)(this.ShipStats.Menu.Y + (Fonts.Arial12Bold.LineSpacing * 2) + 45));
            if (this.ActiveHull.Role != ShipData.RoleName.platform)
            {
                this.DrawRequirement(ref CursorReq, Localizer.Token(120), hasBridge);
                CursorReq.Y = CursorReq.Y + (float)(Fonts.Arial12Bold.LineSpacing + 2);
            }
            this.DrawRequirement(ref CursorReq, Localizer.Token(121), EmptySlots);
        }

        private float GetMaintCostShipyard(ShipData ship, float Size, Empire empire)
        {
            float maint = 0f;
            //string role = ship.Role;
            //string str = role;
            //bool nonCombat = false;
            //added by gremlin: Maintenance changes
            float maintModReduction = 1;

            //Get Maintenance of ship role
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

            //Modify Maintenance by freighter size
            if (ship.Role == ShipData.RoleName.freighter)
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

            if ((ship.Role == ShipData.RoleName.freighter || ship.Role == ShipData.RoleName.platform) && empire.data.CivMaintMod != 1.0)
            {
                maint *= empire.data.CivMaintMod;
            }

            //Apply Privatization
            if ((ship.Role == ShipData.RoleName.freighter || ship.Role == ShipData.RoleName.platform) && empire.data.Privatization)
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
            float OptionIncreaseShipMaintenance = GlobalStats.ShipMaintenanceMulti;
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

            // Calculate maintenance by proportion of ship cost, Duh.
            if (ship.Role == ShipData.RoleName.fighter || ship.Role == ShipData.RoleName.scout)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepFighter;
            else if (ship.Role == ShipData.RoleName.corvette || ship.Role == ShipData.RoleName.gunboat)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepCorvette;
            else if (ship.Role == ShipData.RoleName.frigate || ship.Role == ShipData.RoleName.destroyer)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepFrigate;
            else if (ship.Role == ShipData.RoleName.cruiser)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepCruiser;
            else if (ship.Role == ShipData.RoleName.carrier)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepCarrier;
            else if (ship.Role == ShipData.RoleName.capital)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepCapital;
            else if (ship.Role == ShipData.RoleName.freighter)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepFreighter;
            else if (ship.Role == ShipData.RoleName.platform)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepPlatform;
            else if (ship.Role == ShipData.RoleName.station)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepStation;
            else if (ship.Role == ShipData.RoleName.drone && GlobalStats.ActiveModInfo.useDrones)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepDrone;
            else
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepBaseline;
            if (maint == 0f && GlobalStats.ActiveModInfo.UpkeepBaseline > 0)
                maint = fCost * GlobalStats.ActiveModInfo.UpkeepBaseline;
            else if (maint == 0f && GlobalStats.ActiveModInfo.UpkeepBaseline == 0)
                maint = fCost * 0.004f;


            // Modifiers below here  

            if ((ship.Role == ShipData.RoleName.freighter || ship.Role == ShipData.RoleName.platform) && empire != null && !empire.isFaction && empire.data.CivMaintMod != 1.0)
            {
                maint *= empire.data.CivMaintMod;
            }

            if ((ship.Role == ShipData.RoleName.freighter || ship.Role == ShipData.RoleName.platform) && empire != null && !empire.isFaction && empire.data.Privatization)
            {
                maint *= 0.5f;
            }

            if (GlobalStats.ShipMaintenanceMulti > 1)
            {
                maint *= GlobalStats.ShipMaintenanceMulti;
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

        private void DrawStatColor(ref Vector2 Cursor, string words, float stat, int Tooltip_ID, Color color, bool doGoodBadTint = true, bool isPercent = false)
        {
            float amount = 120f;
            if (GlobalStats.IsGermanFrenchOrPolish) amount = amount + 20f;
            Vector2 MousePos = new Vector2((float)Mouse.GetState().X, (float)Mouse.GetState().Y);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, words, Cursor, color);
            string numbers = "0.0";
            if (isPercent) numbers = stat.ToString("p1");
            else numbers = GetNumberString(stat);
            if (stat == 0f) numbers = "0";
            Cursor.X = Cursor.X + (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            if (doGoodBadTint) base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, (stat > 0f ? Color.LightGreen : Color.LightPink));
            else base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, numbers, Cursor, Color.White);
            Cursor.X = Cursor.X - (amount - Fonts.Arial12Bold.MeasureString(numbers).X);
            if (HelperFunctions.CheckIntersection(new Rectangle((int)Cursor.X, (int)Cursor.Y, (int)Fonts.Arial12Bold.MeasureString(words).X + (int)Fonts.Arial12Bold.MeasureString(numbers).X, Fonts.Arial12Bold.LineSpacing), MousePos))
            {
                ToolTip.CreateTooltip(Tooltip_ID, base.ScreenManager);
            }
        }

        private void DrawStat(ref Vector2 Cursor, string words, float stat, int Tooltip_ID, bool doGoodBadTint = true, bool isPercent = false)
        {
            DrawStatColor(ref Cursor, words, stat, Tooltip_ID, Color.White, doGoodBadTint, isPercent);
        }
        //Mer - Gretman left off here
        private void DrawStat(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            float amount = 165f;
            if (GlobalStats.IsGermanFrenchOrPolish)
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
            if (GlobalStats.IsGermanFrenchOrPolish)
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
            if (GlobalStats.IsGermanFrenchOrPolish)
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
            if (GlobalStats.IsGermanFrenchOrPolish)
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
            if (GlobalStats.IsGermanFrenchOrPolish)
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
            if (GlobalStats.IsGermanFrenchOrPolish)
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

        private void DrawStatBad(ref Vector2 Cursor, string words, string stat, int Tooltip_ID)
        {
            float amount = 165f;
            if (GlobalStats.IsGermanFrenchOrPolish)
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
            if (GlobalStats.IsGermanFrenchOrPolish)
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
            if (this.ActiveHull.Role == ShipData.RoleName.freighter && this.fml)
            {
                this.CategoryList.ActiveIndex = 1;
                this.fml = false;
            }
            //Scout hull type defaults to Recon behaviour. Not really important, as the 'Recon' tag is going to supplant the notion of having 'Fighter' class hulls automatically be scouts, but it makes things easier when working with scout hulls without existing categorisation.
            else if (this.ActiveHull.Role == ShipData.RoleName.scout && this.fml)
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
            this.screenToLaunch = launches;
            MessageBoxScreen message;
            if (this.ShipSaved && this.CheckDesign())
            {
                this.LaunchScreen(null, null);
                this.ReallyExit();
                return;
            }
            else if(!this.ShipSaved && this.CheckDesign())
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

        private string GetConduitGraphic(SlotStruct ss)
        {
            bool right = false;
            bool left = false;
            bool up = false;
            bool down = false;
            int numNear = 0;
            foreach (SlotStruct slot in this.Slots)
            {
                if (slot.Module == null || slot.Module.ModuleType != ShipModuleType.PowerConduit || slot == ss)
                {
                    continue;
                }
                int totalDistanceX = Math.Abs(slot.PQ.X - ss.PQ.X) / 16;
                int totalDistanceY = Math.Abs(slot.PQ.Y - ss.PQ.Y) / 16;
                if (totalDistanceX == 1 && totalDistanceY == 0)
                {
                    if (slot.PQ.X <= ss.PQ.X)
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
                if (slot.PQ.Y <= ss.PQ.Y)
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

        private static FileInfo[] GetFilesFromDirectory(string DirPath)     //Unused
        {
            return (new DirectoryInfo(DirPath)).GetFiles("*.*", SearchOption.AllDirectories);
        }

        private void GoHullLeft()       //Unused
        {
            ShipDesignScreen hullIndex = this;
            hullIndex.HullIndex = hullIndex.HullIndex - 1;
            if (this.HullIndex < 0)
            {
                this.HullIndex = this.AvailableHulls.Count - 1;
            }
            this.ChangeHull(this.AvailableHulls[this.HullIndex]);
        }

        private void GoHullRight()      //Unused
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
            if (HelperFunctions.CheckIntersection(dropdownRect, input.CursorPosition))  //fbedard: add tooltip for CategoryList
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
                            ToolTip.CreateTooltip("Can be used as Freighter.\nEvade when enemy.\nRepair when damaged at 15%", this.ScreenManager);
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
                && this.ActiveModule.ModuleType != ShipModuleType.Turret || this.ActiveModule.XSIZE != this.ActiveModule.YSIZE))
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
            if (this.close.HandleInput(input))
                this.ExitScreen();
            else if (input.CurrentKeyboardState.IsKeyDown(Keys.Z) && input.LastKeyboardState.IsKeyUp(Keys.Z)
                && input.CurrentKeyboardState.IsKeyDown(Keys.LeftControl))
            {
                if (this.DesignStack.Count <= 0)
                    return;
                lastActiveUID = "";
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
                if (!HelperFunctions.CheckIntersection(this.ModuleSelectionMenu.Menu, input.CursorPosition)
                    && !HelperFunctions.CheckIntersection(this.HullSelectionRect, input.CursorPosition)
                    && !HelperFunctions.CheckIntersection(this.ChooseFighterSub.Menu, input.CursorPosition))
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

                if (Debug)
                {
                    if (input.CurrentKeyboardState.IsKeyDown(Keys.Enter) && input.LastKeyboardState.IsKeyUp(Keys.Enter))
                    {
                        foreach (ModuleSlotData moduleSlotData in ActiveHull.ModuleSlots)
                            moduleSlotData.InstalledModuleUID = null;
                        new XmlSerializer(typeof(ShipData)).Serialize(new StreamWriter("Content/Hulls/" + ActiveHull.ShipStyle + "/" + ActiveHull.Name + ".xml"), ActiveHull);
                    }
                    if (input.Right)
                        ++operation;
                    if (operation > SlotModOperation.Normal)
                        operation = SlotModOperation.Delete;
                }

                this.HoveredModule = null;
                this.mouseStateCurrent = Mouse.GetState();
                Vector2 vector2 = new Vector2(mouseStateCurrent.X, mouseStateCurrent.Y);
                this.selector = null;
                this.EmpireUI.HandleInput(input, this);
                this.activeModSubMenu.HandleInputNoReset(this);
                this.hullSL.HandleInput(input);
                for (int index = hullSL.indexAtTop;
                    index < hullSL.Copied.Count && index < hullSL.indexAtTop + hullSL.entriesToDisplay; ++index)
                {
                    ScrollList.Entry e = hullSL.Copied[index];
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
                            GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                            if (!this.ShipSaved && !this.CheckDesign())
                            {
                                MessageBoxScreen messageBoxScreen = new MessageBoxScreen(this, Localizer.Token(2121), "Save", "No");
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
                    if (this.ActiveModule.ModuleType == ShipModuleType.Hangar && !this.ActiveModule.IsTroopBay
                        && !this.ActiveModule.IsSupplyBay)
                    {
                        this.UpdateHangarOptions(this.ActiveModule);
                        this.ChooseFighterSL.HandleInput(input);
                        for (int index = this.ChooseFighterSL.indexAtTop; index < this.ChooseFighterSL.Copied.Count
                            && index < this.ChooseFighterSL.indexAtTop + this.ChooseFighterSL.entriesToDisplay; ++index)
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
                    for (int index = this.ChooseFighterSL.indexAtTop; index < this.ChooseFighterSL.Copied.Count
                        && index < this.ChooseFighterSL.indexAtTop + this.ChooseFighterSL.entriesToDisplay; ++index)
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
                                GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                                return;
                            }
                        }
                    }
                }
                for (int index = this.weaponSL.indexAtTop; index < this.weaponSL.Copied.Count
                    && index < this.weaponSL.indexAtTop + this.weaponSL.entriesToDisplay; ++index)
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
                            this.SetActiveModule(ShipModule.CreateNoParent((e.item as ShipModule).UID));
                            this.ResetModuleState();
                            return;
                        }
                    }
                    else
                        e.clickRectHover = 0;
                }
                this.weaponSL.HandleInput(input);
                if (HelperFunctions.CheckIntersection(this.HullSelectionRect, input.CursorPosition)
                    && input.CurrentMouseState.LeftButton == ButtonState.Pressed
                    || HelperFunctions.CheckIntersection(this.modSel.Menu, input.CursorPosition)
                    && input.CurrentMouseState.LeftButton == ButtonState.Pressed
                    || HelperFunctions.CheckIntersection(this.activeModSubMenu.Menu, input.CursorPosition)
                    && input.CurrentMouseState.LeftButton == ButtonState.Pressed)
                    return;
                if (HelperFunctions.CheckIntersection(this.modSel.Menu, vector2))
                {
                    if (this.mouseStateCurrent.ScrollWheelValue > this.mouseStatePrevious.ScrollWheelValue
                        && this.weaponSL.indexAtTop > 0)
                        --this.weaponSL.indexAtTop;
                    if (this.mouseStateCurrent.ScrollWheelValue < this.mouseStatePrevious.ScrollWheelValue
                        && this.weaponSL.indexAtTop + this.weaponSL.entriesToDisplay < this.weaponSL.Entries.Count)
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
                        Vector2 spaceFromWorldSpace = this.camera.GetScreenSpaceFromWorldSpace(new Vector2((float)slotStruct.PQ.enclosingRect.X, (float)slotStruct.PQ.enclosingRect.Y));
                        if (HelperFunctions.CheckIntersection(new Rectangle((int)spaceFromWorldSpace.X, (int)spaceFromWorldSpace.Y, (int)(16.0 * (double)this.camera.Zoom), (int)(16.0 * (double)this.camera.Zoom)), vector2))
                        {
                            if (slotStruct.Module != null)
                                this.HoveredModule = slotStruct.Module;
                            if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
                            {
                                GameAudio.PlaySfxAsync("simple_beep");
                                if (this.Debug)
                                {
                                    this.DebugAlterSlot(slotStruct.SlotReference.Position, this.operation);
                                    return;
                                }
                                else if (slotStruct.Module != null)
                                    this.HighlightedModule = slotStruct.Module;
                            }
                        }
                    }
                }
                if (HelperFunctions.CheckIntersection(this.upArrow, vector2) && this.mouseStateCurrent.LeftButton == ButtonState.Released && (this.mouseStatePrevious.LeftButton == ButtonState.Pressed && this.scrollPosition > 0))
                {
                    --this.scrollPosition;
                    GameAudio.PlaySfxAsync("blip_click");
                    foreach (ModuleButton moduleButton in this.ModuleButtons)
                        moduleButton.moduleRect.Y += 128;
                }
                if (HelperFunctions.CheckIntersection(this.downArrow, vector2) && input.LeftMouseClick)
                {
                    ++this.scrollPosition;
                    GameAudio.PlaySfxAsync("blip_click");
                    foreach (ModuleButton moduleButton in this.ModuleButtons)
                        moduleButton.moduleRect.Y -= 128;
                }
                if (HelperFunctions.CheckIntersection(this.ModuleSelectionArea, vector2))
                {
                    if (input.ScrollIn && this.scrollPosition > 0)
                    {
                        --this.scrollPosition;
                        GameAudio.PlaySfxAsync("blip_click");
                        foreach (ModuleButton moduleButton in this.ModuleButtons)
                            moduleButton.moduleRect.Y += 128;
                    }
                    if (input.ScrollOut)
                    {
                        ++this.scrollPosition;
                        GameAudio.PlaySfxAsync("blip_click");
                        foreach (ModuleButton moduleButton in this.ModuleButtons)
                            moduleButton.moduleRect.Y -= 128;
                    }
                }
                if (input.RightMouseClick)
                {
                    //this should actually clear slots
                    this.ActiveModule = (ShipModule)null;
                    foreach (SlotStruct slot in this.Slots)
                    {
                        slot.SetValidity(null);
                        Vector2 spaceFromWorldSpace = this.camera.GetScreenSpaceFromWorldSpace(
                            new Vector2((float)slot.PQ.enclosingRect.X, (float)slot.PQ.enclosingRect.Y));
                        Rectangle rect = new Rectangle((int)spaceFromWorldSpace.X, (int)spaceFromWorldSpace.Y
                            , (int)(16.0 * (double)this.camera.Zoom), (int)(16.0 * (double)this.camera.Zoom));
                        if (slot.Module != null && HelperFunctions.CheckIntersection(rect, vector2)) //if clicked at this slot
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
                    if (HelperFunctions.CheckIntersection(this.ModuleSelectionArea, new Vector2((float)(moduleButton.moduleRect.X + 30), (float)(moduleButton.moduleRect.Y + 30))))
                    {
                        if (HelperFunctions.CheckIntersection(moduleButton.moduleRect, vector2))
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
                        Vector2 spaceFromWorldSpace = this.camera.GetScreenSpaceFromWorldSpace(new Vector2((float)slot.PQ.enclosingRect.X
                            , (float)slot.PQ.enclosingRect.Y));
                        if (HelperFunctions.CheckIntersection(new Rectangle((int)spaceFromWorldSpace.X, (int)spaceFromWorldSpace.Y
                            , (int)(16.0 * (double)this.camera.Zoom), (int)(16.0 * (double)this.camera.Zoom)), vector2))
                        {
                            GameAudio.PlaySfxAsync("sub_bass_mouseover");

                            if (slot.PQ.X != this.lastDesignActionPos.X || slot.PQ.Y != this.lastDesignActionPos.Y
                                || ActiveModule.UID != this.lastActiveUID)
                            {
                                this.InstallModule(slot);                       //This will make the Ctrl+Z functionality in the shipyard a lot more responsive -Gretman
                                this.lastDesignActionPos.X = slot.PQ.X;
                                this.lastDesignActionPos.Y = slot.PQ.Y;
                                this.lastActiveUID = ActiveModule.UID;
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
                    if (slotStruct.ModuleUID != null && this.HighlightedModule != null && (slotStruct.Module == this.HighlightedModule && (double)slotStruct.Module.FieldOfFire != 0.0) && slotStruct.Module.ModuleType == ShipModuleType.Turret)
                    {
                        float num1 = slotStruct.Module.FieldOfFire / 2f;
                        Vector2 spaceFromWorldSpace = this.camera.GetScreenSpaceFromWorldSpace(new Vector2((float)(slotStruct.PQ.enclosingRect.X + 16 * (int)slotStruct.Module.XSIZE / 2), (float)(slotStruct.PQ.enclosingRect.Y + 16 * (int)slotStruct.Module.YSIZE / 2)));
                        float num2 = spaceFromWorldSpace.AngleToTarget(vector2);
                        float num3 = this.HighlightedModule.Facing;
                        float num4 = Math.Abs(num2 - num3);
                        if ((double)num4 > (double)num1)
                        {
                            if ((double)num2 > 180.0)
                                num2 = (float)(-1.0 * (360.0 - (double)num2));
                            if ((double)num3 > 180.0)
                                num3 = (float)(-1.0 * (360.0 - (double)num3));
                            num4 = Math.Abs(num2 - num3);
                        }

                        if (GlobalStats.AltArcControl)
                        {
                            //The Doctor: ALT (either) + LEFT CLICK to pick and move arcs. This way, it's impossible to accidentally pick the wrong arc, while it's just as responsive and smooth as the original method when you are trying to.                    
                            if ((double)num4 < (double)num1 && (this.mouseStateCurrent.LeftButton == ButtonState.Pressed && this.mouseStatePrevious.LeftButton == ButtonState.Pressed && ((input.CurrentKeyboardState.IsKeyDown(Keys.LeftAlt) || input.LastKeyboardState.IsKeyDown(Keys.LeftAlt)) || (input.CurrentKeyboardState.IsKeyDown(Keys.RightAlt) || input.LastKeyboardState.IsKeyDown(Keys.RightAlt)))))
                            {

                                this.HighlightedModule.Facing = spaceFromWorldSpace.AngleToTarget(vector2);
                            }
                        }
                        else
                        {
                            //Delay method
                            if ((this.mouseStateCurrent.LeftButton == ButtonState.Pressed && this.mouseStatePrevious.LeftButton == ButtonState.Pressed && this.HoldTimer < 0))
                            {
                                this.HighlightedModule.Facing = spaceFromWorldSpace.AngleToTarget(vector2);
                            }

                        }



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
                                    this.ScreenManager.AddScreen((GameScreen)new LoadDesigns(this));
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
                        if (HelperFunctions.CheckIntersection(toggleButton.r, input.CursorPosition))
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
                this.mouseStatePrevious = this.mouseStateCurrent;
                base.HandleInput(input);
            }
        }


        private bool SlotStructFits(SlotStruct slot)
        {
            int numFreeSlots = 0;
            int sx = slot.PQ.X, sy = slot.PQ.Y;
            for (int y = 0; y < ActiveModule.YSIZE; ++y)
            {
                for (int x = 0; x < ActiveModule.XSIZE; ++x)
                {
                    for (int i = 0; i < Slots.Count; ++i)
                    {
                        SlotStruct ss = Slots[i];
                        if (ss.ShowValid && ss.PQ.Y == (sy + 16 * y) && ss.PQ.X == (sx + 16 * x))
                        {
                            if (ss.Module == null && ss.Parent == null)
                                ++numFreeSlots;
                        }
                    }
                }
            }
            return numFreeSlots == (ActiveModule.XSIZE * ActiveModule.YSIZE);
        }
        
        private void InstallModule(SlotStruct slot)
        {
            if (SlotStructFits(slot))
            {
                DesignAction designAction = new DesignAction();
                designAction.clickedSS = new SlotStruct();
                designAction.clickedSS.PQ = slot.PQ;
                designAction.clickedSS.Restrictions  = slot.Restrictions;
                designAction.clickedSS.Facing        = slot.Module != null ? slot.Module.Facing : 0.0f;
                designAction.clickedSS.ModuleUID     = slot.ModuleUID;
                designAction.clickedSS.Module        = slot.Module;
                designAction.clickedSS.Tex           = slot.Tex;
                designAction.clickedSS.SlotReference = slot.SlotReference;
                designAction.clickedSS.State         = slot.State;
                DesignStack.Push(designAction);
                ClearSlot(slot);
                ClearDestinationSlots(slot);

                slot.ModuleUID            = ActiveModule.UID;
                slot.Module               = ShipModule.CreateNoParent(ActiveModule.UID);
                slot.Module.XSIZE         = ActiveModule.XSIZE;
                slot.Module.YSIZE         = ActiveModule.YSIZE;
                slot.Module.XMLPosition   = ActiveModule.XMLPosition;
                slot.State                = ActiveModState;
                slot.Module.hangarShipUID = ActiveModule.hangarShipUID;
                slot.Module.Facing        = ActiveModule.Facing;
                slot.Tex = ResourceManager.Texture(ActiveModule.IconTexturePath);
                slot.Module.SetAttributesNoParent();

                RecalculatePower();
                ShipSaved = false;
                if (ActiveModule.ModuleType != ShipModuleType.Hangar)
                {
                    ActiveModule = ShipModule.CreateNoParent(ActiveModule.UID);
                }
                ChangeModuleState(ActiveModState);
            }
            else PlayNegativeSound();
        }

        private void InstallModuleFromLoad(SlotStruct slot)
        {
            if (SlotStructFits(slot))
            {
                ActiveModuleState activeModuleState = slot.State;
                ClearSlot(slot);
                ClearDestinationSlotsNoStack(slot);
                slot.ModuleUID = ActiveModule.UID;
                slot.Module    = ActiveModule; 
                slot.State     = activeModuleState;
                slot.Module.Facing = slot.Facing;
                slot.Tex = ResourceManager.TextureDict[ActiveModule.IconTexturePath];
                slot.Module.SetAttributesNoParent();

                RecalculatePower();
            }
            else PlayNegativeSound();
        }

        private void InstallModuleNoStack(SlotStruct slot)
        {
            if (SlotStructFits(slot))
            {
                ClearSlotNoStack(slot);
                ClearDestinationSlotsNoStack(slot);
                slot.ModuleUID            = ActiveModule.UID;
                slot.Module               = ActiveModule;
                slot.State                = ActiveModState;
                slot.Module.hangarShipUID = ActiveModule.hangarShipUID;
                slot.Module.Facing        = ActiveModule.Facing;
                slot.Tex = ResourceManager.TextureDict[ResourceManager.GetModuleTemplate(ActiveModule.UID).IconTexturePath];
                slot.Module.SetAttributesNoParent();

                RecalculatePower();
                ShipSaved = false;
                if (ActiveModule.ModuleType != ShipModuleType.Hangar)
                {
                    ActiveModule = ShipModule.CreateNoParent(ActiveModule.UID);
                }
                //grabs a fresh copy of the same module type to cursor 
                ChangeModuleState(ActiveModState);
                //set rotation for new module at cursor
            }
            else PlayNegativeSound();
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
                    GameAudio.PlaySfxAsync("echo_affirm");
                    ScreenManager.AddScreen(new ResearchScreenNew(this, EmpireUI));
                }
                else if (str1 == "Budget")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    ScreenManager.AddScreen(new BudgetScreen(Empire.Universe));
                }
            }
            string str2 = this.screenToLaunch;
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
                        MiddleText = "This help menu contains information on all of the gameplay systems contained in StarDrive. You can also watch one of several tutorial videos for a developer-guided introduction to StarDrive."
                    };
                }
            }
            this.ReallyExit();
        }

        public override void LoadContent()
        {
            LightRig rig = TransientContent.Load<LightRig>("example/ShipyardLightrig");
            rig.AssignTo(this);
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
            this.choosefighterrect = new Rectangle(acsub.X + acsub.Width + 5, acsub.Y-90, 240, 270);
            if (this.choosefighterrect.Y + this.choosefighterrect.Height > base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
            {
                int diff = this.choosefighterrect.Y + this.choosefighterrect.Height - base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
                this.choosefighterrect.Height = this.choosefighterrect.Height - (diff + 10);
            }
            this.choosefighterrect.Height = acsub.Height;
            this.ChooseFighterSub = new Submenu(base.ScreenManager, this.choosefighterrect);
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
            float width = (float)base.Viewport.Width;
            Viewport viewport = base.Viewport;
            float aspectRatio = width / (float)viewport.Height;
            this.offset = new Vector2();
            Viewport viewport1 = base.Viewport;
            this.offset.X = (float)(viewport1.Width / 2 - 256);
            Viewport viewport2 = base.Viewport;
            this.offset.Y = (float)(viewport2.Height / 2 - 256);
            this.camera = new Camera2d();
            Camera2d vector2 = this.camera;
            Viewport viewport3 = base.Viewport;
            float single = (float)viewport3.Width / 2f;
            Viewport viewport4 = base.Viewport;
            vector2.Pos = new Vector2(single, (float)viewport4.Height / 2f);
            Vector3 camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
            this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) * Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
            this.projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 20000f);
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
                        World = this.worldMatrix
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
            Vector3 pScreenSpace = viewport5.Project(Vector3.Zero, this.projection, this.view, Matrix.Identity);
            Vector2 pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
            Vector2 radialPos = MathExt.PointOnCircle(90f, xDistance);
            Viewport viewport6 = base.Viewport;
            Vector3 insetRadialPos = viewport6.Project(new Vector3(radialPos, 0f), this.projection, this.view, Matrix.Identity);
            Vector2 insetRadialSS = new Vector2(insetRadialPos.X, insetRadialPos.Y);
            float Radius = Vector2.Distance(insetRadialSS, pPos) + 10f;
            if (Radius >= xDistance)
            {
                while (Radius > xDistance)
                {
                    camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
                    this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) * Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
                    bs = this.shipSO.WorldBoundingSphere;
                    Viewport viewport7 = base.Viewport;
                    pScreenSpace = viewport7.Project(Vector3.Zero, this.projection, this.view, Matrix.Identity);
                    pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    radialPos = MathExt.PointOnCircle(90f, xDistance);
                    Viewport viewport8 = base.Viewport;
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
                    this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) * Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
                    bs = this.shipSO.WorldBoundingSphere;
                    Viewport viewport9 = base.Viewport;
                    pScreenSpace = viewport9.Project(Vector3.Zero, this.projection, this.view, Matrix.Identity);
                    pPos = new Vector2(pScreenSpace.X, pScreenSpace.Y);
                    radialPos = MathExt.PointOnCircle(90f, xDistance);
                    Viewport viewport10 = base.Viewport;
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
            this.classifCursor = new Vector2(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * .5f, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_132px"].Height + 10 );
            Cursor = new Vector2((float)(this.classifCursor.X), (float)(this.classifCursor.Y));
            Vector2 OrdersBarPos = new Vector2(Cursor.X, (float)((int)Cursor.Y+20 ));
            OrdersBarPos.X = OrdersBarPos.X - 15;
            ToggleButton AttackRuns = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_formation_headon");
            this.CombatStatusButtons.Add(AttackRuns);
            AttackRuns.Action = "attack";
            AttackRuns.HasToolTip = true;
            AttackRuns.WhichToolTip = 1;
            
            OrdersBarPos.X = OrdersBarPos.X + 29f;
            ToggleButton ShortRange = new ToggleButton(new Rectangle((int)OrdersBarPos.X, (int)OrdersBarPos.Y, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "SelectionBox/icon_grid");
            this.CombatStatusButtons.Add(ShortRange);
            ShortRange.Action = "short";
            ShortRange.HasToolTip = true;
            ShortRange.WhichToolTip = 228;

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
                this.hullSL.AddItem(type);
            }
            foreach (ScrollList.Entry e in this.hullSL.Entries)
            {
                foreach (KeyValuePair<string, ShipData> hull in Ship_Game.ResourceManager.HullsDict)
                {
                    if (!EmpireManager.Player.GetHDict()[hull.Key] || !((e.item as ModuleHeader).Text == Localizer.GetRole(hull.Value.Role, EmpireManager.Player)))
                    {
                        continue;
                    }
                    e.AddItem(hull.Value);
                }
            }
            Rectangle ShipStatsPanel = new Rectangle(this.HullSelectionRect.X + 50, this.HullSelectionRect.Y + this.HullSelectionRect.Height - 20, 280, 320);

            
            //base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth
            dropdownRect = new Rectangle((int)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth * .25f), (int) OrdersBarPos.Y, 100, 18);
            //dropdownRect = new Rectangle((int)ShipStatsPanel.X, (int)ShipStatsPanel.Y + ShipStatsPanel.Height + 118, 100, 18);
                        
            this.CategoryList = new DropOptions(dropdownRect);
            //this.CategoryList.AddOption("Unclassified", 1);
            //this.CategoryList.AddOption("Civilian", 2);
            //this.CategoryList.AddOption("Recon", 3);
            //this.CategoryList.AddOption("Combat", 4);
            //this.CategoryList.AddOption("Kamikaze", 5);
            foreach(Ship_Game.ShipData.Category item in Enum.GetValues(typeof(Ship_Game.ShipData.Category)).Cast<Ship_Game.ShipData.Category>())
            {
                this.CategoryList.AddOption(item.ToString(),(int)item +1);

            }

            CarrierOnly = ActiveHull.CarrierShip;
            COBoxCursor = new Vector2(dropdownRect.X + 106, dropdownRect.Y);
            CarrierOnlyBox = new Checkbox(COBoxCursor.X, COBoxCursor.Y, () => CarrierOnly, Fonts.Arial12Bold, "Carrier Only", 0); 

            this.ShipStats = new Menu1(base.ScreenManager, ShipStatsPanel);
            this.statsSub = new Submenu(base.ScreenManager, ShipStatsPanel);
            this.statsSub.AddTab(Localizer.Token(108));
            this.ArcsButton = new GenericButton(new Vector2((float)(this.HullSelectionRect.X- 32), 97f), "Arcs", Fonts.Pirulen20, Fonts.Pirulen16);//new GenericButton(new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 32), 97f), "Arcs", Fonts.Pirulen20, Fonts.Pirulen16);
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

        public void PlayNegativeSound() => GameAudio.PlaySfxAsync("UI_Misc20");

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

        private void RecalculatePower()
        {
            foreach (SlotStruct slotStruct in this.Slots)
            {
                slotStruct.Powered = false;
                slotStruct.CheckedConduits = false;
                if (slotStruct.Module != null)
                    slotStruct.Module.Powered = false;
            }
            foreach (SlotStruct slotStruct in this.Slots)
            {
                //System.Diagnostics.Debug.Assert(slotStruct.parent != null, "parent is null");                   
                if (slotStruct.Module != null && slotStruct.Module.ModuleType == ShipModuleType.PowerPlant)
                {
                    foreach (SlotStruct slot in this.Slots)
                    {
                        if (slot.Module != null && slot.Module.ModuleType == ShipModuleType.PowerConduit && (Math.Abs(slot.PQ.X - slotStruct.PQ.X) / 16 + Math.Abs(slot.PQ.Y - slotStruct.PQ.Y) / 16 == 1 && slot.Module != null))
                            this.CheckAndPowerConduit(slot);
                    }
                }                
                else if (slotStruct.Parent != null)               
                {
                    //System.Diagnostics.Debug.Assert(slotStruct.parent.module != null, "parent is fine, module is null");
                    if (slotStruct.Parent.Module != null)
                    {
                        //System.Diagnostics.Debug.Assert(slotStruct.parent.module.ModuleType != null, "parent is fine, module is fine, moduletype is null");
                        if (slotStruct.Parent.Module.ModuleType == ShipModuleType.PowerPlant)
                        {
                            foreach (SlotStruct slot in this.Slots)
                            {
                                if (slot.Module != null && slot.Module.ModuleType == ShipModuleType.PowerConduit && (Math.Abs(slot.PQ.X - slotStruct.PQ.X) / 16 + Math.Abs(slot.PQ.Y - slotStruct.PQ.Y) / 16 == 1 && slot.Module != null))
                                    this.CheckAndPowerConduit(slot);
                            }
                        }
                    }
                }
            }
            foreach (SlotStruct slotStruct1 in Slots)
            {
                if (slotStruct1.Module != null && slotStruct1.Module.PowerRadius > 0 && (slotStruct1.Module.ModuleType != ShipModuleType.PowerConduit || slotStruct1.Module.Powered))
                {
                    foreach (SlotStruct slotStruct2 in Slots)
                    {
                        if (Math.Abs(slotStruct1.PQ.X - slotStruct2.PQ.X) / 16 + Math.Abs(slotStruct1.PQ.Y - slotStruct2.PQ.Y) / 16 <= (int)slotStruct1.Module.PowerRadius)
                            slotStruct2.Powered = true;
                    }
                    if (slotStruct1.Module.XSIZE <= 1 && slotStruct1.Module.YSIZE <= 1)
                        continue;

                    for (int y = 0; y < slotStruct1.Module.YSIZE; ++y)
                    {
                        for (int x = 0; x < slotStruct1.Module.XSIZE; ++x)
                        {
                            if (x == 0 && y == 0) continue;
                            foreach (SlotStruct slotStruct2 in Slots)
                            {
                                if (slotStruct2.PQ.Y == slotStruct1.PQ.Y + 16 * y && slotStruct2.PQ.X == slotStruct1.PQ.X + 16 * x)
                                {
                                    foreach (SlotStruct slotStruct3 in Slots)
                                    {
                                        if (Math.Abs(slotStruct2.PQ.X - slotStruct3.PQ.X) / 16 + Math.Abs(slotStruct2.PQ.Y - slotStruct3.PQ.Y) / 16 <= (int)slotStruct1.Module.PowerRadius)
                                            slotStruct3.Powered = true;
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
                    if (slotStruct.Module != null && slotStruct.Module.ModuleType != ShipModuleType.PowerConduit)
                        slotStruct.Module.Powered = true;
                    if (slotStruct.Parent != null && slotStruct.Parent.Module != null)
                        slotStruct.Parent.Module.Powered = true;                    
                }
                if (!slotStruct.Powered && slotStruct.Module != null && slotStruct.Module.IndirectPower)
                        slotStruct.Module.Powered = true;
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
                    Position           = slot.SlotReference.Position,
                    Restrictions       = slot.Restrictions,
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
            TextWriter WriteFileStream = new StreamWriter(string.Concat(path, "/StarDrive/Saved Designs/", name, ".xml"));
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
                Animated       = this.ActiveHull.Animated,
                CombatState    = this.ActiveHull.CombatState,
                Hull           = this.ActiveHull.Hull,
                IconPath       = this.ActiveHull.IconPath,
                ModelPath      = this.ActiveHull.ModelPath,
                Name           = this.ActiveHull.Name,
                Role           = this.ActiveHull.Role,
                ShipStyle      = this.ActiveHull.ShipStyle,
                ThrusterList   = this.ActiveHull.ThrusterList
            };

            savedShip.ModuleSlots = new ModuleSlotData[Slots.Count];
            for (int i = 0; i < Slots.Count; ++i)
            {
                SlotStruct slot = Slots[i];
                ModuleSlotData data = new ModuleSlotData
                {
                    InstalledModuleUID = slot.ModuleUID,
                    Position           = slot.SlotReference.Position,
                    Restrictions       = slot.Restrictions
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
            TextWriter WriteFileStream = new StreamWriter(string.Concat(path, "/StarDrive/WIP/", savedShip.Name, ".xml"));
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

        private void SaveWIPThenExitToFleets(object sender, EventArgs e)        //Unused
        {
            this.SaveWIP(sender, e);
            base.ScreenManager.AddScreen(new FleetDesignScreen(this, EmpireUI));
            this.ReallyExit();
        }

        private void SaveWIPThenExitToShipsList(object sender, EventArgs e)     //Unused
        {
            this.SaveWIP(sender, e);
            base.ScreenManager.AddScreen(new ShipListScreen(this, EmpireUI));
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
                    GameAudio.PlaySfxAsync("echo_affirm");
                    base.ScreenManager.AddScreen(new ResearchScreenNew(this, EmpireUI));
                }
                else if (str1 == "Budget")
                {
                    GameAudio.PlaySfxAsync("echo_affirm");
                    base.ScreenManager.AddScreen(new BudgetScreen(Empire.Universe));
                }
            }
            string str2 = this.screenToLaunch;
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
                        MiddleText = "This help menu contains information on all of the gameplay systems contained in StarDrive. You can also watch one of several tutorial videos for a developer-guided introduction to StarDrive."
                    };
                }
            }
            this.ReallyExit();
        }

        public void SetActiveModule(ShipModule mod)
        {

            GameAudio.PlaySfxAsync("smallservo");
            mod.SetAttributesNoParent();
            this.ActiveModule = mod;
            this.ResetModuleState();
            foreach (SlotStruct s in this.Slots)                                    
                s.SetValidity(ActiveModule);
            
            if (this.ActiveHangarModule != this.ActiveModule && this.ActiveModule.ModuleType == ShipModuleType.Hangar)
            {
                this.ActiveHangarModule = this.ActiveModule;
                this.ChooseFighterSL.Entries.Clear();
                this.ChooseFighterSL.Copied.Clear();
                foreach (string shipname in EmpireManager.Player.ShipsWeCanBuild)
                {
                    if (!this.ActiveModule.PermittedHangarRoles.Contains(Ship_Game.ResourceManager.ShipsDict[shipname].shipData.GetRole()) || Ship_Game.ResourceManager.ShipsDict[shipname].Size >= this.ActiveModule.MaximumHangarShipSize)
                    {
                        continue;
                    }
                    this.ChooseFighterSL.AddItem(Ship_Game.ResourceManager.ShipsDict[shipname]);
                }
                if (this.HangarShipUIDLast != "Undefined" && this.ActiveModule.PermittedHangarRoles.Contains(Ship_Game.ResourceManager.ShipsDict[HangarShipUIDLast].shipData.GetRole()) && this.ActiveModule.MaximumHangarShipSize >= Ship_Game.ResourceManager.ShipsDict[HangarShipUIDLast].Size)
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
            if (this.ActiveHangarModule != mod &&  mod.ModuleType == ShipModuleType.Hangar)
            {
                this.ActiveHangarModule = mod;
                this.ChooseFighterSL.Entries.Clear();
                this.ChooseFighterSL.Copied.Clear();
                foreach (string shipname in EmpireManager.Player.ShipsWeCanBuild)
                {
                    if (!mod.PermittedHangarRoles.Contains(ResourceManager.ShipsDict[shipname].shipData.GetRole()) || ResourceManager.ShipsDict[shipname].Size >= mod.MaximumHangarShipSize)
                    {
                        continue;
                    }
                    ChooseFighterSL.AddItem(ResourceManager.ShipsDict[shipname]);
                }
            }
        }

        private void SetupSlots()
        {
            this.Slots.Clear();
            foreach (ModuleSlotData slot in this.ActiveHull.ModuleSlots)
            {
                PrimitiveQuad pq = new PrimitiveQuad(slot.Position.X + this.offset.X - 8f, slot.Position.Y + this.offset.Y - 8f, 16f, 16f);
                SlotStruct ss = new SlotStruct()
                {
                    PQ            = pq,
                    Restrictions  = slot.Restrictions,
                    Facing        = slot.Facing,
                    ModuleUID     = slot.InstalledModuleUID,
                    SlotReference = slot,
                    SlotOptions   = slot.SlotOptions
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
            this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) * Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
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
                        value += value * EmpireManager.Player.data.WeaponTags["Missile"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Missile"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Missile"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Missile"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Missile"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Missile"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Energy)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Energy"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Energy"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Energy"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Energy"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Energy"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Energy"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Torpedo)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Torpedo"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Torpedo"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Torpedo"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Torpedo"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Torpedo"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Torpedo"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Kinetic)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Kinetic"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Kinetic"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Kinetic"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Kinetic"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Kinetic"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Kinetic"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Hybrid)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Hybrid"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Hybrid"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Hybrid"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Hybrid"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Hybrid"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Hybrid"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Railgun)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Railgun"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Railgun"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Railgun"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Railgun"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Railgun"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Railgun"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Explosive)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Explosive"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Explosive"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Explosive"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Explosive"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Explosive"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Explosive"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Guided)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Guided"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Guided"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Guided"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Guided"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Guided"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Guided"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Intercept)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Intercept"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Intercept"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Intercept"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Intercept"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Intercept"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Intercept"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_PD)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["PD"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["PD"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["PD"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["PD"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["PD"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["PD"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_SpaceBomb)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Spacebomb"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Spacebomb"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Spacebomb"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Spacebomb"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Spacebomb"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Spacebomb"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_BioWeapon)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["BioWeapon"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["BioWeapon"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["BioWeapon"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["BioWeapon"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["BioWeapon"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["BioWeapon"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Drone)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Drone"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Drone"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Drone"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Drone"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Drone"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Drone"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Subspace)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Subspace"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Subspace"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Subspace"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Subspace"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Subspace"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Subspace"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Warp)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Warp"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Warp"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Warp"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Warp"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Warp"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Warp"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Cannon)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Cannon"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Cannon"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Cannon"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Cannon"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Cannon"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Cannon"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Beam)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Beam"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Beam"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Beam"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Beam"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Beam"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Beam"].ShieldDamage;
                        break;
                }
            }
            if (weapon.Tag_Bomb)
            {
                switch (stat)
                {
                    case "damage":
                        value += value * EmpireManager.Player.data.WeaponTags["Bomb"].Damage;
                        break;
                    case "range":
                        value += value * EmpireManager.Player.data.WeaponTags["Bomb"].Range;
                        break;
                    case "speed":
                        value += value * EmpireManager.Player.data.WeaponTags["Bomb"].Speed;
                        break;
                    case "firedelay":
                        value -= value * EmpireManager.Player.data.WeaponTags["Bomb"].Rate;
                        break;
                    case "armor":
                        value += value * EmpireManager.Player.data.WeaponTags["Bomb"].ArmorDamage;
                        break;
                    case "shield":
                        value += value * EmpireManager.Player.data.WeaponTags["Bomb"].ShieldDamage;
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

        private enum Colors     //Unused
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
            O,
            E,
            IO,
            IE,
            OE,
            IOE,
            Normal

        }
    }
}