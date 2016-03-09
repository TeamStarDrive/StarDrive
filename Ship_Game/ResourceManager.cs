using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using SgMotion;
using SgMotion.Controllers;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System.Runtime;
namespace Ship_Game
{
	public sealed class ResourceManager
	{
		public static Dictionary<string, Texture2D> TextureDict;

		public static XmlSerializer weapon_serializer;

		public static XmlSerializer serializer_shipdata;

		public static Dictionary<string, Ship> ShipsDict;

		public static Dictionary<int, Model> RoidsModels;

		public static Dictionary<int, Model> JunkModels;

		public static Dictionary<string, Technology> TechTree;

		public static List<Encounter> Encounters;

		public static Dictionary<string, Building> BuildingsDict;

		public static Dictionary<string, Good> GoodsDict;

		public static Dictionary<string, Weapon> WeaponsDict;

		public static Dictionary<string, ShipModule> ShipModulesDict;

		public static Dictionary<int, ToolTip> ToolTips;

		public static Dictionary<string, Texture2D> ProjTextDict;

		public static Dictionary<string, ModelMesh> ProjectileMeshDict;

		public static Dictionary<string, Model> ProjectileModelDict;

		public static bool Initialized;

		public static string WhichModPath;

		public static List<RandomItem> RandomItemsList;

		public static Dictionary<string, Troop> TroopsDict;

		public static Dictionary<string, DiplomacyDialog> DDDict;

		public static Dictionary<string, LocalizationFile> LanguageDict;

		public static LocalizationFile LanguageFile;

		public static Dictionary<string, Artifact> ArtifactsDict;

		public static Dictionary<string, ExplorationEvent> EventsDict;

		public static List<Texture2D> BigNebulas;

		public static List<Texture2D> MedNebulas;

		public static List<Texture2D> SmallNebulas;

		public static List<Texture2D> SmallStars;

		public static List<Texture2D> MediumStars;

		public static List<Texture2D> LargeStars;

		private static RacialTraits rt;

		public static List<EmpireData> Empires;

		public static XmlSerializer HeaderSerializer;

		public static XmlSerializer ModSerializer;

		public static Dictionary<string, Model> ModelDict;

		public static XmlSerializer EconSerializer;

		public static Dictionary<string, ShipData> HullsDict;

		public static UniverseScreen universeScreen;

		public static List<KeyValuePair<string, Texture2D>> FlagTextures;

        public static Dictionary<string, SoundEffect> SoundEffectDict;
        public static int OffSet;

        //Added by McShooterz
        public static HostileFleets HostileFleets;
        public static ShipNames ShipNames;
        public static AgentMissionData AgentMissionData;
        public static MainMenuShipList MainMenuShipList;
        public static Dictionary<ShipData.RoleName, ShipRole> ShipRoles;
        public static Dictionary<string, HullBonus> HullBonuses;
        public static Dictionary<string, PlanetEdict> PlanetaryEdicts;
        //public static Dictionary<string, string> DefaultStrings;
        
        //private static void AddDefaultString(string addme)
        //{
        //    if(!DefaultStrings.ContainsKey(addme))
        //    {
        //        DefaultStrings.Add()
        //    }
        //}
		static ResourceManager()
		{
			Ship_Game.ResourceManager.TextureDict = new Dictionary<string, Texture2D>();
			Ship_Game.ResourceManager.weapon_serializer = new XmlSerializer(typeof(Weapon));
			Ship_Game.ResourceManager.serializer_shipdata = new XmlSerializer(typeof(ShipData));
            Ship_Game.ResourceManager.ShipsDict = new Dictionary<string, Ship>();
			Ship_Game.ResourceManager.RoidsModels = new Dictionary<int, Model>();
			Ship_Game.ResourceManager.JunkModels = new Dictionary<int, Model>();
            Ship_Game.ResourceManager.TechTree = new Dictionary<string, Technology>(StringComparer.InvariantCultureIgnoreCase);
			Ship_Game.ResourceManager.Encounters = new List<Encounter>();
			Ship_Game.ResourceManager.BuildingsDict = new Dictionary<string, Building>();
			Ship_Game.ResourceManager.GoodsDict = new Dictionary<string, Good>();
			Ship_Game.ResourceManager.WeaponsDict = new Dictionary<string, Weapon>();
			Ship_Game.ResourceManager.ShipModulesDict = new Dictionary<string, ShipModule>();
			Ship_Game.ResourceManager.ToolTips = new Dictionary<int, ToolTip>();
			Ship_Game.ResourceManager.ProjTextDict = new Dictionary<string, Texture2D>();
			Ship_Game.ResourceManager.ProjectileMeshDict = new Dictionary<string, ModelMesh>();
			Ship_Game.ResourceManager.ProjectileModelDict = new Dictionary<string, Model>();
			Ship_Game.ResourceManager.Initialized = false;
			Ship_Game.ResourceManager.WhichModPath = "";
			Ship_Game.ResourceManager.RandomItemsList = new List<RandomItem>();
			Ship_Game.ResourceManager.TroopsDict = new Dictionary<string, Troop>();
			Ship_Game.ResourceManager.DDDict = new Dictionary<string, DiplomacyDialog>();
			Ship_Game.ResourceManager.LanguageDict = new Dictionary<string, LocalizationFile>();
			Ship_Game.ResourceManager.LanguageFile = new LocalizationFile();
			Ship_Game.ResourceManager.ArtifactsDict = new Dictionary<string, Artifact>();
			Ship_Game.ResourceManager.EventsDict = new Dictionary<string, ExplorationEvent>();
			Ship_Game.ResourceManager.BigNebulas = new List<Texture2D>();
			Ship_Game.ResourceManager.MedNebulas = new List<Texture2D>();
			Ship_Game.ResourceManager.SmallNebulas = new List<Texture2D>();
			Ship_Game.ResourceManager.SmallStars = new List<Texture2D>();
			Ship_Game.ResourceManager.MediumStars = new List<Texture2D>();
			Ship_Game.ResourceManager.LargeStars = new List<Texture2D>();
			Ship_Game.ResourceManager.rt = new RacialTraits();
			Ship_Game.ResourceManager.Empires = new List<EmpireData>();
			Ship_Game.ResourceManager.HeaderSerializer = new XmlSerializer(typeof(HeaderData));
			Ship_Game.ResourceManager.ModSerializer = new XmlSerializer(typeof(ModInformation));
			Ship_Game.ResourceManager.ModelDict = new Dictionary<string, Model>();
			Ship_Game.ResourceManager.EconSerializer = new XmlSerializer(typeof(EconomicResearchStrategy));
            Ship_Game.ResourceManager.HullsDict = new Dictionary<string, ShipData>(StringComparer.InvariantCultureIgnoreCase);
			Ship_Game.ResourceManager.FlagTextures = new List<KeyValuePair<string, Texture2D>>();
            //Added by McShooterz
            Ship_Game.ResourceManager.HostileFleets = new HostileFleets();
            Ship_Game.ResourceManager.ShipNames = new ShipNames();
            Ship_Game.ResourceManager.SoundEffectDict = new Dictionary<string, SoundEffect>();
            Ship_Game.ResourceManager.AgentMissionData = new AgentMissionData();
            Ship_Game.ResourceManager.MainMenuShipList = new MainMenuShipList();
            Ship_Game.ResourceManager.ShipRoles = new Dictionary<ShipData.RoleName, ShipRole>();
            Ship_Game.ResourceManager.HullBonuses = new Dictionary<string, HullBonus>();
            Ship_Game.ResourceManager.PlanetaryEdicts = new Dictionary<string, PlanetEdict>();
            Ship_Game.ResourceManager.OffSet = 0;
            
		}

		public ResourceManager()
		{
		}
        public static void MarkShipDesignsUnlockable()
        {
            int x = 0;

            Dictionary<Technology, List<string>> ShipTechs = new Dictionary<Technology, List<string>>();
            //foreach (KeyValuePair<string, TechEntry> TechTreeItem in this.TechnologyDict)
            foreach (KeyValuePair<string, Technology> TechTreeItem in ResourceManager.TechTree)
            {
                if (TechTreeItem.Value.ModulesUnlocked.Count <= 0 && TechTreeItem.Value.HullsUnlocked.Count <= 0)
                    continue;
                ShipTechs.Add(TechTreeItem.Value, ResourceManager.FindPreviousTechs( TechTreeItem.Value, new List<string>()));
            }



            ShipData shipData;
            HashSet<string> purge = new HashSet<string>();
            //if(shipData.EmpiresThatCanUseThis.ContainsKey(this.data.Traits.ShipType))
            foreach (ShipData hull in ResourceManager.HullsDict.Values)
            {
                if (hull.Role == ShipData.RoleName.disabled)
                    continue;
                if (hull.Role < ShipData.RoleName.gunboat)
                    hull.unLockable = true;
                foreach (Technology hulltech2 in ShipTechs.Keys)
                {
                    foreach (Technology.UnlockedHull hulls in hulltech2.HullsUnlocked)
                    {
                        if (hulls.Name == hull.Hull)
                        {
                            foreach (string tree in ShipTechs[hulltech2])
                            {
                                hull.techsNeeded.Add(tree);
                                hull.unLockable = true;
                            }
                            break;
                        }
                    }
                    if (hull.unLockable)
                        break;
                }

            }
            foreach (KeyValuePair<string, Ship> ship in ResourceManager.ShipsDict)
            {

                shipData = ship.Value.shipData;
                if (shipData == null)
                    continue;
                if (shipData.HullRole == ShipData.RoleName.disabled)
                    continue;
                //bool empirehulldict;
                //if (shipData.ShipStyle != this.data.Traits.ShipType && (!this.GetHDict().TryGetValue(shipData.Hull, out empirehulldict) || !empirehulldict))
                //    continue;
                //if (shipData.HullRole < ShipData.RoleName.gunboat || shipData.Role == ShipData.RoleName.prototype)
                //    shipData.hullUnlockable = true;
                List<string> techsFound = new List<string>();
                if (shipData.HullData != null && shipData.HullData.unLockable)
                {
                    foreach (string str in shipData.HullData.techsNeeded)
                        shipData.techsNeeded.Add(str);
                    shipData.hullUnlockable = true;
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine(" no hull tech");
                    shipData.allModulesUnlocakable = false;
                    shipData.hullUnlockable = false;
                    //shipData.techsNeeded.Clear();
                    purge.Add(ship.Key);
                }



                bool flag = false;
                if (shipData.hullUnlockable)
                {
                    shipData.allModulesUnlocakable = true;
                    foreach (ModuleSlotData module in ship.Value.shipData.ModuleSlotList)
                    {


                        if (module.InstalledModuleUID == "Dummy")
                            continue;
                        bool modUnlockable = false;
                        //if (!modUnlockable)
                        foreach (Technology technology in ShipTechs.Keys)
                        {

                            foreach (Technology.UnlockedMod mods in technology.ModulesUnlocked)
                            {
                                if (mods.ModuleUID == module.InstalledModuleUID)
                                {
                                    modUnlockable = true;

                                    shipData.techsNeeded.Add(technology.UID);
                                    foreach (string tree in ShipTechs[technology])
                                        shipData.techsNeeded.Add(tree);

                                    break;
                                }
                            }
                            if (modUnlockable)
                                break;
                        }
                        if (!modUnlockable)
                        {
                            shipData.allModulesUnlocakable = false;
                            //shipData.hullUnlockable = false;
                            //shipData.techsNeeded.Clear();
                           // purge.Add(ship.Key);
                            break;
                        }

                    }
                }

                if (shipData.allModulesUnlocakable)
                    foreach (string techname in shipData.techsNeeded)
                    {
                        shipData.TechScore += (int)ResourceManager.TechTree[techname].Cost;
                        x++;
                        if (shipData.BaseStrength == 0)
                        {
                            ResourceManager.CalculateBaseStrength(ship.Value);
                        }
                    }
                else
                {                    
                    //shipData.allModulesUnlocakable = false;
                    shipData.unLockable = false;
                    shipData.techsNeeded.Clear();
                    purge.Add(shipData.Name);
                    shipData.BaseStrength = 0;
                    //System.Diagnostics.Debug.WriteLine(shipData.Name);
                }

            }
            
            System.Diagnostics.Debug.WriteLine("Designs Bad: " + purge.Count + " : ShipDesigns OK : " + x);
            foreach (string purger in purge)
            {
                System.Diagnostics.Debug.WriteLine("These are Designs" + purger);
            }
            

        }
		public static Microsoft.Xna.Framework.Content.ContentManager localContentManager;
		public static bool ignoreLoadingErrors = false;
		/// <summary>
		/// This one used for reporting resource loading errors.
		/// </summary>
		/// <param name="FI"></param>
		/// <param name="where"></param>
		public static void ReportLoadingError(FileInfo FI, string where, Exception e)
		{
			if (!ignoreLoadingErrors)
			{
				e.Data.Add("Failing File: ", FI.FullName);
				e.Data.Add("Fail Info: ", e.InnerException.Message);
				throw (e);
			}
		}
		/// <summary>
		///  All references to Game1.Instance.Content were replaced by this function
		/// </summary>
		/// <returns></returns>
		public static Microsoft.Xna.Framework.Content.ContentManager GetContentManager()
		{
			if (Game1.Instance != null)
				return Game1.Instance.Content;
			return localContentManager;
		}

		public static Troop CopyTroop(Troop t)
		{
			Troop troop = new Troop()
			{
				Class = t.Class,
				Cost = t.Cost,
				Name = t.Name
			};
			troop.SetOwner(t.GetOwner());
			troop.Range = t.Range;
			troop.Description = t.Description;
			troop.HardAttack = t.HardAttack;
            //troop.Initiative = t.Initiative;          //Not referenced in code, removing to save memory -Gretman
            troop.SoftAttack = t.SoftAttack;
			troop.Strength = t.Strength;
			troop.StrengthMax = t.StrengthMax;
			troop.TargetType = t.TargetType;
			troop.TexturePath = t.TexturePath;
			troop.Experience = t.Experience;
			troop.Icon = t.Icon;
			troop.animated = t.animated;
			troop.idle_path = t.idle_path;
			troop.idle_x_offset = t.idle_x_offset;
			troop.idle_y_offset = t.idle_y_offset;
			troop.num_attack_frames = t.num_attack_frames;
			troop.num_idle_frames = t.num_idle_frames;
			troop.attack_width = t.attack_width;
			troop.attack_path = t.attack_path;
			troop.WhichFrame = (int)RandomMath.RandomBetween(1f, (float)(t.num_idle_frames - 1));
			troop.first_frame = t.first_frame;
			troop.MovementCue = t.MovementCue;
			troop.sound_attack = t.sound_attack;
			troop.MoveTimerBase = t.MoveTimerBase;
			troop.AttackTimerBase = t.AttackTimerBase;
			troop.Level = t.Level;
			troop.Kills = t.Kills;
            troop.BoardingStrength = t.BoardingStrength;
			return troop;
		}

       

		public static Ship CreateShipAt(string key, Empire Owner, Planet p, bool DoOrbit)
		{
            Ship newShip;
            //if (universeScreen.MasterShipList.pendingRemovals.TryPop(out newShip))
            //{
            //    newShip.ShipRecreate();
            //    newShip.shipData.Role = Ship_Game.ResourceManager.ShipsDict[key].Role;
            //    newShip.Name = Ship_Game.ResourceManager.ShipsDict[key].Name;
            //    newShip.BaseStrength = Ship_Game.ResourceManager.ShipsDict[key].BaseStrength;
            //    newShip.BaseCanWarp = Ship_Game.ResourceManager.ShipsDict[key].BaseCanWarp;
            //}
            //else 
            if (!Ship_Game.ResourceManager.ShipsDict.TryGetValue(key, out newShip))
            {
                Ship_Game.ResourceManager.ShipsDict.TryGetValue(Owner.data.StartingScout, out newShip);
                newShip = new Ship()
                {
                    shipData = newShip.shipData,
                    Name = newShip.Name,
                    BaseStrength = newShip.BaseStrength,
                    BaseCanWarp = newShip.BaseCanWarp,
                    VanityName = "I am a bug"

                };
            }
            else
            {
                newShip = new Ship()
                    {
                        shipData = newShip.shipData,
                        Name = newShip.Name,
                        BaseStrength = newShip.BaseStrength,
                        BaseCanWarp = newShip.BaseCanWarp

                    };
            }
			newShip.LoadContent(GetContentManager());
			SceneObject newSO = new SceneObject();
			if (!Ship_Game.ResourceManager.ShipsDict[key].GetShipData().Animated)
			{
				newSO = new SceneObject(Ship_Game.ResourceManager.GetModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
				newShip.SetSO(newSO);
			}
			else
			{
				SkinnedModel model = Ship_Game.ResourceManager.GetSkinnedModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			newSO.ObjectType = ObjectType.Dynamic;
			newShip.SetSO(newSO);
			foreach (Thruster t in Ship_Game.ResourceManager.ShipsDict[key].GetTList())
			{
				Thruster thr = new Thruster()
				{
					Parent = newShip,
					tscale = t.tscale,
					XMLPos = t.XMLPos
				};
				newShip.GetTList().Add(thr);
			}
			foreach (ModuleSlot slot in Ship_Game.ResourceManager.ShipsDict[key].ModuleSlotList)
			{
				ModuleSlot newSlot = new ModuleSlot();
				newSlot.SetParent(newShip);
				newSlot.SlotOptions = slot.SlotOptions;
				newSlot.Restrictions = slot.Restrictions;
				newSlot.Position = slot.Position;
				newSlot.facing = slot.facing;
				newSlot.state = slot.state;
				newSlot.InstalledModuleUID = slot.InstalledModuleUID;
				newShip.ModuleSlotList.AddLast(newSlot);
			}
			newShip.Position = p.Position;
			newShip.loyalty = Owner;
            
			newShip.Initialize();
            //Added by McShooterz: add automatic ship naming
            if (GlobalStats.ActiveMod != null && Ship_Game.ResourceManager.ShipNames.CheckForName(Owner.data.Traits.ShipType, newShip.shipData.Role))
                newShip.VanityName = Ship_Game.ResourceManager.ShipNames.GetName(Owner.data.Traits.ShipType, newShip.shipData.Role);
			newShip.GetSO().World = Matrix.CreateTranslation(new Vector3(newShip.Center, 0f));
			lock (GlobalStats.ObjectManagerLocker)
			{
				Ship_Game.ResourceManager.universeScreen.ScreenManager.inter.ObjectManager.Submit(newShip.GetSO());
			}
			foreach (Thruster t in newShip.GetTList())
			{
				t.load_and_assign_effects(Ship_Game.ResourceManager.universeScreen.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", Ship_Game.ResourceManager.universeScreen.ThrusterEffect);
				t.InitializeForViewing();
			}
            if (newShip.shipData.Role == ShipData.RoleName.fighter)
			{
				Ship level = newShip;
				level.Level = level.Level + Owner.data.BonusFighterLevels;
			}
            if(universeScreen.GameDifficulty > UniverseData.GameDifficulty.Normal)
            {
                newShip.Level += (int)universeScreen.GameDifficulty;
            }
			Owner.AddShip(newShip);
			newShip.GetAI().State = AIState.AwaitingOrders;
            
			return newShip;
		}

        //Added by McShooterz: for refit to keep name
        public static Ship CreateShipAt(string key, Empire Owner, Planet p, bool DoOrbit, string RefitName, byte RefitLevel)
        {
            Ship newShip;
            //if (universeScreen.MasterShipList.pendingRemovals.TryPop(out newShip))
            //{
            //    newShip.ShipRecreate();
            //    newShip.shipData.Role = Ship_Game.ResourceManager.ShipsDict[key].Role;
            //    newShip.Name = Ship_Game.ResourceManager.ShipsDict[key].Name;
            //    newShip.BaseStrength = Ship_Game.ResourceManager.ShipsDict[key].BaseStrength;
            //    newShip.BaseCanWarp = Ship_Game.ResourceManager.ShipsDict[key].BaseCanWarp;
            //}
            //else
            newShip = new Ship()
            {
                shipData = Ship_Game.ResourceManager.ShipsDict[key].shipData,
                Name = Ship_Game.ResourceManager.ShipsDict[key].Name,
                BaseStrength = Ship_Game.ResourceManager.ShipsDict[key].BaseStrength,
                BaseCanWarp = Ship_Game.ResourceManager.ShipsDict[key].BaseCanWarp

            };
            newShip.LoadContent(GetContentManager());
            SceneObject newSO = new SceneObject();
            if (!Ship_Game.ResourceManager.ShipsDict[key].GetShipData().Animated)
            {
                newSO = new SceneObject(Ship_Game.ResourceManager.GetModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath).Meshes[0])
                {
                    ObjectType = ObjectType.Dynamic
                };
                newShip.SetSO(newSO);
            }
            else
            {
                SkinnedModel model = Ship_Game.ResourceManager.GetSkinnedModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath);
                newSO = new SceneObject(model.Model);
                newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
            }
            newSO.ObjectType = ObjectType.Dynamic;
            newShip.SetSO(newSO);
            foreach (Thruster t in Ship_Game.ResourceManager.ShipsDict[key].GetTList())
            {
                Thruster thr = new Thruster()
                {
                    Parent = newShip,
                    tscale = t.tscale,
                    XMLPos = t.XMLPos
                };
                newShip.GetTList().Add(thr);
            }
            foreach (ModuleSlot slot in Ship_Game.ResourceManager.ShipsDict[key].ModuleSlotList)
            {
                ModuleSlot newSlot = new ModuleSlot();
                newSlot.SetParent(newShip);
                newSlot.SlotOptions = slot.SlotOptions;
                newSlot.Restrictions = slot.Restrictions;
                newSlot.Position = slot.Position;
                newSlot.facing = slot.facing;
                newSlot.state = slot.state;
                newSlot.InstalledModuleUID = slot.InstalledModuleUID;
                newShip.ModuleSlotList.AddLast(newSlot);
            }
            newShip.Position = p.Position;
            newShip.loyalty = Owner;

            newShip.Initialize();
            //Added by McShooterz: add automatic ship naming
            newShip.VanityName = RefitName;
            newShip.Level = RefitLevel;
            newShip.GetSO().World = Matrix.CreateTranslation(new Vector3(newShip.Center, 0f));
            lock (GlobalStats.ObjectManagerLocker)
            {
                Ship_Game.ResourceManager.universeScreen.ScreenManager.inter.ObjectManager.Submit(newShip.GetSO());
            }
            foreach (Thruster t in newShip.GetTList())
            {
                t.load_and_assign_effects(Ship_Game.ResourceManager.universeScreen.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", Ship_Game.ResourceManager.universeScreen.ThrusterEffect);
                t.InitializeForViewing();
            }
            if (newShip.shipData.Role == ShipData.RoleName.fighter)
            {
                Ship level = newShip;
                level.Level += Owner.data.BonusFighterLevels;
            }
            Owner.AddShip(newShip);
            newShip.GetAI().State = AIState.AwaitingOrders;
            return newShip;
        }

		//fbedard: do not use, cannot change role of shipdata !
        public static Ship CreateShipAt(string key, Empire Owner, Planet p, bool DoOrbit, ShipData.RoleName role, List<Troop> Troops)
		{
			Ship newShip = new Ship()
			{
                shipData = Ship_Game.ResourceManager.ShipsDict[key].shipData,
                BaseStrength = Ship_Game.ResourceManager.ShipsDict[key].BaseStrength,
                BaseCanWarp = Ship_Game.ResourceManager.ShipsDict[key].BaseCanWarp
			};
            //newShip.shipData.Role = role;
			if (role == ShipData.RoleName.troop)
			{
				if (Troops.Count <= 0)
				{
					newShip.VanityName = "Troop Shuttle";
				}
				else
				{
					newShip.VanityName = Troops[0].Name;
				}
			}
			newShip.Name = Ship_Game.ResourceManager.ShipsDict[key].Name;
			newShip.LoadContent(GetContentManager());
			SceneObject newSO = new SceneObject();
			if (!Ship_Game.ResourceManager.ShipsDict[key].GetShipData().Animated)
			{
				newSO = new SceneObject(Ship_Game.ResourceManager.GetModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
			}
			else
			{
				SkinnedModel model = Ship_Game.ResourceManager.GetSkinnedModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			newShip.SetSO(newSO);
			newShip.Position = p.Position;
			foreach (Thruster t in Ship_Game.ResourceManager.ShipsDict[key].GetTList())
			{
				Thruster thr = new Thruster()
				{
					Parent = newShip,
					tscale = t.tscale,
					XMLPos = t.XMLPos
				};
				newShip.GetTList().Add(thr);
			}
			foreach (ModuleSlot slot in Ship_Game.ResourceManager.ShipsDict[key].ModuleSlotList)
			{
				ModuleSlot newSlot = new ModuleSlot();
				newSlot.SetParent(newShip);
				newSlot.SlotOptions = slot.SlotOptions;
				newSlot.Restrictions = slot.Restrictions;
				newSlot.Position = slot.Position;
				newSlot.facing = slot.facing;
				newSlot.state = slot.state;
				newSlot.InstalledModuleUID = slot.InstalledModuleUID;
				newShip.ModuleSlotList.AddLast(newSlot);
			}
            if (newShip.shipData.Role == ShipData.RoleName.fighter)
			{
				Ship level = newShip;
				level.Level = level.Level + Owner.data.BonusFighterLevels;
			}
			newShip.loyalty = Owner;
			newShip.Initialize();
            //Added by McShooterz: add automatic ship naming
			if (GlobalStats.ActiveModInfo != null && Ship_Game.ResourceManager.ShipNames.CheckForName(Owner.data.Traits.ShipType, newShip.shipData.Role))
                newShip.VanityName = Ship_Game.ResourceManager.ShipNames.GetName(Owner.data.Traits.ShipType, newShip.shipData.Role);
			newShip.GetSO().World = Matrix.CreateTranslation(new Vector3(newShip.Center, 0f));
			lock (GlobalStats.ObjectManagerLocker)
			{
				Ship_Game.ResourceManager.universeScreen.ScreenManager.inter.ObjectManager.Submit(newShip.GetSO());
			}
			foreach (Thruster t in newShip.GetTList())
			{
				t.load_and_assign_effects(Ship_Game.ResourceManager.universeScreen.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", Ship_Game.ResourceManager.universeScreen.ThrusterEffect);
				t.InitializeForViewing();
			}
			if (DoOrbit)
			{
				newShip.DoOrbit(p);
			}
			foreach (Troop t in Troops)
			{
				newShip.TroopList.Add(Ship_Game.ResourceManager.CopyTroop(t));
			}
			Owner.AddShip(newShip);
			return newShip;
		}

		public static Ship CreateShipAtPoint(string key, Empire Owner, Vector2 p)
		{
            if (!Ship_Game.ResourceManager.ShipsDict.ContainsKey(key))
            {
                return null;
            }
            Ship newShip;
            //if(universeScreen.MasterShipList.pendingRemovals.TryPop(out newShip))
            //{
            //    newShip.ShipRecreate();
            //}
            //else
            newShip = new Ship();

            newShip.shipData = Ship_Game.ResourceManager.ShipsDict[key].shipData;
			newShip.Name = Ship_Game.ResourceManager.ShipsDict[key].Name;
            newShip.BaseStrength = Ship_Game.ResourceManager.ShipsDict[key].BaseStrength;
            newShip.BaseCanWarp = Ship_Game.ResourceManager.ShipsDict[key].BaseCanWarp;
			newShip.LoadContent(GetContentManager());
			SceneObject newSO = new SceneObject();
			if (!Ship_Game.ResourceManager.ShipsDict[key].GetShipData().Animated)
			{
				newSO = new SceneObject(Ship_Game.ResourceManager.GetModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
				newShip.SetSO(newSO);
			}
			else
			{
				SkinnedModel model = Ship_Game.ResourceManager.GetSkinnedModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			newSO.ObjectType = ObjectType.Dynamic;
			newShip.SetSO(newSO);
            if (newShip.shipData.Role == ShipData.RoleName.fighter && Owner.data != null)
			{
				Ship level = newShip;
				level.Level = level.Level + Owner.data.BonusFighterLevels;
			}
			foreach (Thruster t in Ship_Game.ResourceManager.ShipsDict[key].GetTList())
			{
				Thruster thr = new Thruster()
				{
					Parent = newShip,
					tscale = t.tscale,
					XMLPos = t.XMLPos
				};
				newShip.GetTList().Add(thr);
			}
			foreach (ModuleSlot slot in Ship_Game.ResourceManager.ShipsDict[key].ModuleSlotList)
			{
				ModuleSlot newSlot = new ModuleSlot();
				newSlot.SetParent(newShip);
				newSlot.SlotOptions = slot.SlotOptions;
				newSlot.Restrictions = slot.Restrictions;
				newSlot.Position = slot.Position;
				newSlot.facing = slot.facing;
				newSlot.state = slot.state;
				newSlot.InstalledModuleUID = slot.InstalledModuleUID;
				newShip.ModuleSlotList.AddLast(newSlot);
			}
			newShip.Position = p;
			newShip.loyalty = Owner;
			newShip.Initialize();
            //Added by McShooterz: add automatic ship naming
            if (GlobalStats.ActiveMod != null && Ship_Game.ResourceManager.ShipNames.CheckForName(Owner.data.Traits.ShipType, newShip.shipData.Role))
                newShip.VanityName = Ship_Game.ResourceManager.ShipNames.GetName(Owner.data.Traits.ShipType, newShip.shipData.Role);
			newShip.GetSO().World = Matrix.CreateTranslation(new Vector3(newShip.Center, 0f));
			lock (GlobalStats.ObjectManagerLocker)
			{
				Ship_Game.ResourceManager.universeScreen.ScreenManager.inter.ObjectManager.Submit(newShip.GetSO());
			}
			newShip.isInDeepSpace = true;
			foreach (Thruster t in newShip.GetTList())
			{
				t.load_and_assign_effects(Ship_Game.ResourceManager.universeScreen.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", Ship_Game.ResourceManager.universeScreen.ThrusterEffect);
				t.InitializeForViewing();
			}
			Owner.AddShip(newShip);
			return newShip;
		}

		public static Ship CreateShipAtPoint(string key, Empire Owner, Vector2 p, float facing)
		{
						Ship newShip;
            //if(universeScreen.MasterShipList.pendingRemovals.TryPop(out newShip))
            //{
            //    newShip.ShipRecreate();
            //    newShip.Rotation = facing;
            //    newShip.shipData.Role = Ship_Game.ResourceManager.ShipsDict[key].Role;
            //    newShip.Name = Ship_Game.ResourceManager.ShipsDict[key].Name;
            //    newShip.BaseStrength = Ship_Game.ResourceManager.ShipsDict[key].BaseStrength;
            //    newShip.BaseCanWarp = Ship_Game.ResourceManager.ShipsDict[key].BaseCanWarp;
            //}
            //else
            newShip = new Ship()
			{
				Rotation = facing,
                shipData = Ship_Game.ResourceManager.ShipsDict[key].shipData,
                Name = Ship_Game.ResourceManager.ShipsDict[key].Name,
                BaseStrength = Ship_Game.ResourceManager.ShipsDict[key].BaseStrength,
                BaseCanWarp = Ship_Game.ResourceManager.ShipsDict[key].BaseCanWarp
                
			};
			newShip.LoadContent(GetContentManager());
            if (newShip.shipData.Role == ShipData.RoleName.fighter)
			{
				Ship level = newShip;
				level.Level = level.Level + Owner.data.BonusFighterLevels;
			}
			SceneObject newSO = new SceneObject();
			if (!Ship_Game.ResourceManager.ShipsDict[key].GetShipData().Animated)
			{
				newSO = new SceneObject(Ship_Game.ResourceManager.GetModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
				newShip.SetSO(newSO);
			}
			else
			{
				SkinnedModel model = Ship_Game.ResourceManager.GetSkinnedModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			newShip.SetSO(newSO);
			foreach (Thruster t in Ship_Game.ResourceManager.ShipsDict[key].GetTList())
			{
				Thruster thr = new Thruster()
				{
					Parent = newShip,
					tscale = t.tscale,
					XMLPos = t.XMLPos
				};
				newShip.GetTList().Add(thr);
			}
			foreach (ModuleSlot slot in Ship_Game.ResourceManager.ShipsDict[key].ModuleSlotList)
			{
				ModuleSlot newSlot = new ModuleSlot();
				newSlot.SetParent(newShip);
				newSlot.SlotOptions = slot.SlotOptions;
				newSlot.Restrictions = slot.Restrictions;
				newSlot.Position = slot.Position;
				newSlot.facing = slot.facing;
				newSlot.state = slot.state;
				newSlot.InstalledModuleUID = slot.InstalledModuleUID;
				newShip.ModuleSlotList.AddLast(newSlot);
			}
			newShip.Position = p;
			newShip.loyalty = Owner;
			newShip.Initialize();
            //Added by McShooterz: add automatic ship naming
            if (GlobalStats.ActiveMod != null && Ship_Game.ResourceManager.ShipNames.CheckForName(Owner.data.Traits.ShipType, newShip.shipData.Role))
                newShip.VanityName = Ship_Game.ResourceManager.ShipNames.GetName(Owner.data.Traits.ShipType, newShip.shipData.Role);
			newShip.GetSO().World = Matrix.CreateTranslation(new Vector3(newShip.Center, 0f));
			lock (GlobalStats.ObjectManagerLocker)
			{
				Ship_Game.ResourceManager.universeScreen.ScreenManager.inter.ObjectManager.Submit(newShip.GetSO());
			}
			newShip.isInDeepSpace = true;
			foreach (Thruster t in newShip.GetTList())
			{
				t.load_and_assign_effects(Ship_Game.ResourceManager.universeScreen.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", Ship_Game.ResourceManager.universeScreen.ThrusterEffect);
				t.InitializeForViewing();
			}
			Owner.AddShip(newShip);
			return newShip;
		}

		public static Ship CreateShipAtPointNow(string key, Empire Owner, Vector2 p)
		{
			Ship newShip = new Ship();
			if (!Ship_Game.ResourceManager.ShipsDict.ContainsKey(key))
			{
				return null;
			}
            newShip.shipData = Ship_Game.ResourceManager.ShipsDict[key].shipData;
			newShip.Name = Ship_Game.ResourceManager.ShipsDict[key].Name;
            newShip.BaseStrength = Ship_Game.ResourceManager.ShipsDict[key].BaseStrength;
            newShip.BaseCanWarp = Ship_Game.ResourceManager.ShipsDict[key].BaseCanWarp;
			newShip.LoadContent(GetContentManager());
			SceneObject newSO = new SceneObject();
			if (!Ship_Game.ResourceManager.ShipsDict[key].GetShipData().Animated)
			{
				newSO = new SceneObject(Ship_Game.ResourceManager.GetModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
				newShip.SetSO(newSO);
			}
			else
			{
				SkinnedModel model = Ship_Game.ResourceManager.GetSkinnedModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			newSO.ObjectType = ObjectType.Dynamic;
			newShip.SetSO(newSO);
            if (newShip.shipData.Role == ShipData.RoleName.fighter && Owner.data != null)
			{
				Ship level = newShip;
				level.Level = level.Level + Owner.data.BonusFighterLevels;
			}
			foreach (Thruster t in Ship_Game.ResourceManager.ShipsDict[key].GetTList())
			{
				Thruster thr = new Thruster()
				{
					Parent = newShip,
					tscale = t.tscale,
					XMLPos = t.XMLPos
				};
				newShip.GetTList().Add(thr);
			}
			foreach (ModuleSlot slot in Ship_Game.ResourceManager.ShipsDict[key].ModuleSlotList)
			{
				ModuleSlot newSlot = new ModuleSlot();
				newSlot.SetParent(newShip);
				newSlot.SlotOptions = slot.SlotOptions;
				newSlot.Restrictions = slot.Restrictions;
				newSlot.Position = slot.Position;
				newSlot.facing = slot.facing;
				newSlot.state = slot.state;
				newSlot.InstalledModuleUID = slot.InstalledModuleUID;
				newShip.ModuleSlotList.AddLast(newSlot);
			}
			newShip.Position = p;
			newShip.loyalty = Owner;
			newShip.Initialize();
            //Added by McShooterz: add automatic ship naming
            if (GlobalStats.ActiveMod != null && Ship_Game.ResourceManager.ShipNames.CheckForName(Owner.data.Traits.ShipType, newShip.shipData.Role))
                newShip.VanityName = Ship_Game.ResourceManager.ShipNames.GetName(Owner.data.Traits.ShipType, newShip.shipData.Role);
			newShip.GetSO().World = Matrix.CreateTranslation(new Vector3(newShip.Center, 0f));
			lock (GlobalStats.ObjectManagerLocker)
			{
				Ship_Game.ResourceManager.universeScreen.ScreenManager.inter.ObjectManager.Submit(newShip.GetSO());
			}
			newShip.isInDeepSpace = true;
			foreach (Thruster t in newShip.GetTList())
			{
				t.load_and_assign_effects(Ship_Game.ResourceManager.universeScreen.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", Ship_Game.ResourceManager.universeScreen.ThrusterEffect);
				t.InitializeForViewing();
			}
			Owner.AddShip(newShip);
			return newShip;
		}

		public static Ship CreateShipForBattleMode(string key, Empire Owner, Vector2 p)
		{
			Ship newShip = new Ship()
			{
                shipData = Ship_Game.ResourceManager.ShipsDict[key].shipData,
                Name = Ship_Game.ResourceManager.ShipsDict[key].Name,
                BaseStrength = Ship_Game.ResourceManager.ShipsDict[key].BaseStrength,
                BaseCanWarp = Ship_Game.ResourceManager.ShipsDict[key].BaseCanWarp
			};
			newShip.LoadContent(GetContentManager());
			SceneObject newSO = new SceneObject();
			if (!Ship_Game.ResourceManager.ShipsDict[key].GetShipData().Animated)
			{
				newSO = new SceneObject(Ship_Game.ResourceManager.GetModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
				newShip.SetSO(newSO);
			}
			else
			{
				SkinnedModel model = Ship_Game.ResourceManager.GetSkinnedModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			foreach (Thruster t in Ship_Game.ResourceManager.ShipsDict[key].GetTList())
			{
				Thruster thr = new Thruster()
				{
					Parent = newShip,
					tscale = t.tscale,
					XMLPos = t.XMLPos
				};
				newShip.GetTList().Add(thr);
			}
			foreach (ModuleSlot slot in Ship_Game.ResourceManager.ShipsDict[key].ModuleSlotList)
			{
				ModuleSlot newSlot = new ModuleSlot();
				newSlot.SetParent(newShip);
				newSlot.SlotOptions = slot.SlotOptions;
				newSlot.Restrictions = slot.Restrictions;
				newSlot.Position = slot.Position;
				newSlot.facing = slot.facing;
				newSlot.state = slot.state;
				newSlot.InstalledModuleUID = slot.InstalledModuleUID;
				newShip.ModuleSlotList.AddLast(newSlot);
			}
			newShip.Position = p;
			newShip.loyalty = Owner;
			newShip.Initialize();
			newShip.GetSO().World = Matrix.CreateTranslation(new Vector3(newShip.Center, 0f));
			newShip.isInDeepSpace = true;
			Owner.AddShip(newShip);
			return newShip;
		}

		public static Ship CreateShipFromHangar(string key, Empire Owner, Vector2 p, Ship Parent)
		{
			Ship s = Ship_Game.ResourceManager.CreateShipAtPoint(key, Owner, p);
			if (s != null)
			{
				s.Mothership = Parent;
				s.Velocity = Parent.Velocity;                
			}
			return s;
		}


		public static Troop CreateTroop(Troop t, Empire Owner)
		{
			Troop troop = new Troop()
			{
				Class = t.Class,
				Cost = t.Cost,
				Name = t.Name,
				Description = t.Description,
				HardAttack = t.HardAttack,
                //Initiative = t.Initiative,          //Not referenced in code, removing to save memory -Gretman
                SoftAttack = t.SoftAttack,
				Strength = t.Strength,
                StrengthMax = t.StrengthMax > 0 ? t.StrengthMax : t.Strength,
				Icon = t.Icon,
                BoardingStrength = t.BoardingStrength
			};

			if (Owner != null)
			{
				Troop strength = troop;
				strength.Strength = strength.Strength + (int)(Owner.data.Traits.GroundCombatModifier * (float)troop.Strength);
			}
			troop.TargetType = t.TargetType;
			troop.TexturePath = t.TexturePath;
			troop.Range = t.Range;
			troop.Experience = t.Experience;
			troop.SetOwner(Owner);
			troop.animated = t.animated;
			troop.idle_path = t.idle_path;
			troop.idle_x_offset = t.idle_x_offset;
			troop.idle_y_offset = t.idle_y_offset;
			troop.num_attack_frames = t.num_attack_frames;
			troop.num_idle_frames = t.num_idle_frames;
			troop.attack_width = t.attack_width;
			troop.attack_path = t.attack_path;
			troop.WhichFrame = (int)RandomMath.RandomBetween(1f, (float)(t.num_idle_frames - 1));
			troop.first_frame = t.first_frame;
			troop.sound_attack = t.sound_attack;
			troop.MovementCue = t.MovementCue;
			troop.MoveTimerBase = t.MoveTimerBase;
			troop.AttackTimerBase = t.AttackTimerBase;
			troop.Level = t.Level;
			troop.Kills = t.Kills;
			return troop;
		}

		public static Ship CreateTroopShipAtPoint(string key, Empire Owner, Vector2 point, Troop troop)
		{
			Ship Ship =null;
            if(!Ship_Game.ResourceManager.ShipsDict.TryGetValue(key, out Ship) ) //|| Ship.Size > Ship_Game.ResourceManager.ShipsDict[Owner.data.DefaultSmallTransport].Size)
            {
                Ship_Game.ResourceManager.ShipsDict.TryGetValue("Default Troop", out Ship);
                //key = "Default Troop";
            }
            Ship newShip = new Ship()
			{
                shipData = Ship.shipData,
                Name = Ship.Name,
				VanityName = troop.Name
			};
            newShip.LoadContent(GetContentManager());
			SceneObject newSO = new SceneObject();
            if (!Ship.GetShipData().Animated)
			{
                newSO = new SceneObject(Ship_Game.ResourceManager.GetModel(Ship.ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
			}
			else
			{
                SkinnedModel model = Ship_Game.ResourceManager.GetSkinnedModel(Ship.ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			newSO.ObjectType = ObjectType.Dynamic;
			newShip.SetSO(newSO);
			newShip.Position = point;
            foreach (Thruster t in Ship.GetTList())
			{
				Thruster thr = new Thruster()
				{
					Parent = newShip,
					tscale = t.tscale,
					XMLPos = t.XMLPos
				};
				newShip.GetTList().Add(thr);
			}
			foreach (ModuleSlot slot in Ship.ModuleSlotList)
			{
				ModuleSlot newSlot = new ModuleSlot();
				newSlot.SetParent(newShip);
				newSlot.SlotOptions = slot.SlotOptions;
				newSlot.Restrictions = slot.Restrictions;
				newSlot.Position = slot.Position;
				newSlot.facing = slot.facing;
				newSlot.InstalledModuleUID = slot.InstalledModuleUID;
				newShip.ModuleSlotList.AddLast(newSlot);
			}
			newShip.loyalty = Owner;
			newShip.Initialize();
			newShip.GetSO().World = Matrix.CreateTranslation(new Vector3(newShip.Center, 0f));
			newShip.VanityName = troop.Name;
			lock (GlobalStats.ObjectManagerLocker)
			{
				Ship_Game.ResourceManager.universeScreen.ScreenManager.inter.ObjectManager.Submit(newShip.GetSO());
			}
			foreach (Thruster t in newShip.GetTList())
			{
				t.load_and_assign_effects(Ship_Game.ResourceManager.universeScreen.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", Ship_Game.ResourceManager.universeScreen.ThrusterEffect);
				t.InitializeForViewing();
			}
			newShip.TroopList.Add(Ship_Game.ResourceManager.CopyTroop(troop));
            if (newShip.shipData.Role == ShipData.RoleName.troop) // && newShip.shipData.ShipCategory == ShipData.Category.Civilian)
                newShip.shipData.ShipCategory = ShipData.Category.Combat;  //fbedard
            Owner.AddShip(newShip);
			return newShip;
		}

		public static void DeleteShip(string Name)
		{
			FileInfo toDelete = null;
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/StarterShips");
			XmlSerializer serializer0 = new XmlSerializer(typeof(ShipData));
			FileInfo[] fileInfoArray = textList;
			int num = 0;
			while (num < (int)fileInfoArray.Length)
			{
				FileInfo FI = fileInfoArray[num];
				FileStream stream = FI.OpenRead();
				ShipData newShipData = (ShipData)serializer0.Deserialize(stream);
				//stream.Close();
				stream.Dispose();
				if (newShipData.Name != Name)
				{
					num++;
				}
				else
				{
					toDelete = FI;
					break;
				}
			}
			FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/SavedDesigns");
			int num1 = 0;
			while (num1 < (int)filesFromDirectory.Length)
			{
				FileInfo FI = filesFromDirectory[num1];
				FileStream stream = FI.OpenRead();
				ShipData newShipData = (ShipData)serializer0.Deserialize(stream);
				//stream.Close();
				stream.Dispose();
				if (newShipData.Name != Name)
				{
					num1++;
				}
				else
				{
					toDelete = FI;
					break;
				}
			}
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			FileInfo[] filesFromDirectory1 = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(path, "/StarDrive/Saved Designs"));
			int num2 = 0;
			while (num2 < (int)filesFromDirectory1.Length)
			{
				FileInfo FI = filesFromDirectory1[num2];
				FileStream stream = FI.OpenRead();
                if (FI.Extension != "XML")
                {
                    num2++;
                    continue;
                }
                ShipData newShipData  =null;
                try
                {
                    newShipData = (ShipData)serializer0.Deserialize(stream);
                }
                catch
                {
                    num2++;
                    continue;
                    
                }
				//stream.Close();
				stream.Dispose();
				if (newShipData.Name != Name)
				{
					num2++;
				}
				else
				{
					toDelete = FI;
					break;
				}
			}
			FileInfo[] fileInfoArray1 = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(path, "/StarDrive/WIP"));
			int num3 = 0;
			while (num3 < (int)fileInfoArray1.Length)
			{
				FileInfo FI = fileInfoArray1[num3];
				FileStream stream = FI.OpenRead();
				ShipData newShipData = (ShipData)serializer0.Deserialize(stream);
				//stream.Close();
				stream.Dispose();
				if (newShipData.Name != Name)
				{
					num3++;
				}
				else
				{
					toDelete = FI;
					break;
				}
			}
			textList = new FileInfo[0];
			if (toDelete != null)
			{
				toDelete.Delete();
			}
			foreach (Empire e in EmpireManager.EmpireList)
			{
				e.UpdateShipsWeCanBuild();
			}
		}

		private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);
			DirectoryInfo[] dirs = dir.GetDirectories();
			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(string.Concat("Source directory does not exist or could not be found: ", sourceDirName));
			}
			if (!Directory.Exists(destDirName))
			{
				Directory.CreateDirectory(destDirName);
			}
			FileInfo[] files = dir.GetFiles();
			for (int i = 0; i < (int)files.Length; i++)
			{
				FileInfo file = files[i];
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, true);
			}
			if (copySubDirs)
			{
				DirectoryInfo[] directoryInfoArray = dirs;
				for (int j = 0; j < (int)directoryInfoArray.Length; j++)
				{
					DirectoryInfo subdir = directoryInfoArray[j];
					string temppath = Path.Combine(destDirName, subdir.Name);
					Ship_Game.ResourceManager.DirectoryCopy(subdir.FullName, temppath, copySubDirs);
				}
			}
		}

		public static Building GetBuilding(string whichBuilding)
		{
			Building newB = new Building()
			{
				PlanetaryShieldStrengthAdded = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].PlanetaryShieldStrengthAdded,
				MinusFertilityOnBuild = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].MinusFertilityOnBuild,
				CombatStrength = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].CombatStrength,
				PlusProdPerRichness = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].PlusProdPerRichness,
				Name = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].Name,
                IsSensor = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].IsSensor,
                IsProjector = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].IsProjector,
				PlusResearchPerColonist = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].PlusResearchPerColonist,
				StorageAdded = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].StorageAdded,
				Unique = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].Unique,
				Icon = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].Icon,
				PlusTaxPercentage = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].PlusTaxPercentage,
				Strength = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].Strength,
				HardAttack = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].HardAttack,
				SoftAttack = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].SoftAttack,
				Defense = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].Defense,
				Maintenance = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].Maintenance,
				AllowInfantry = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].AllowInfantry,
				CanBuildAnywhere = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].CanBuildAnywhere,
				PlusFlatPopulation = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].PlusFlatPopulation,
				Weapon = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].Weapon,
				isWeapon = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].isWeapon,
				PlusTerraformPoints = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].PlusTerraformPoints,
                SensorRange = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].SensorRange,
                ProjectorRange = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].ProjectorRange,
                Category = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].Category
			};
            //comp fix to ensure functionality of vanilla buildings
            if ((newB.Name == "Outpost" || newB.Name =="Capital City") && newB.IsProjector==false && newB.ProjectorRange==0.0f)
            {
                newB.ProjectorRange = Empire.ProjectorRadius;
                newB.IsProjector = true;
            }
            if ((newB.Name == "Outpost" || newB.Name == "Capital City") && newB.IsSensor == false && newB.SensorRange == 0.0f)
            {
                newB.SensorRange = 20000.0f;
                newB.IsSensor = true;
            }
			if (newB.isWeapon)
			{
				newB.theWeapon = Ship_Game.ResourceManager.GetWeapon(newB.Weapon);
			}
			newB.PlusFlatFoodAmount = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].PlusFlatFoodAmount;
			newB.PlusFlatProductionAmount = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].PlusFlatProductionAmount;
			newB.PlusFlatResearchAmount = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].PlusFlatResearchAmount;
			newB.EventTriggerUID = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].EventTriggerUID;
			newB.EventWasTriggered = false;
			newB.NameTranslationIndex = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].NameTranslationIndex;
			newB.DescriptionIndex = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].DescriptionIndex;
			newB.ShortDescriptionIndex = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].ShortDescriptionIndex;
			newB.Unique = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].Unique;
			newB.CreditsPerColonist = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].CreditsPerColonist;
			newB.PlusProdPerColonist = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].PlusProdPerColonist;
			newB.Scrappable = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].Scrappable;
			newB.PlusFoodPerColonist = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].PlusFoodPerColonist;
			newB.WinsGame = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].WinsGame;
			newB.EventOnBuild = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].EventOnBuild;
			newB.NoRandomSpawn = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].NoRandomSpawn;
			newB.Cost = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].Cost * UniverseScreen.GamePaceStatic;
			newB.MaxPopIncrease = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].MaxPopIncrease;
			newB.AllowShipBuilding = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].AllowShipBuilding;
			newB.BuildOnlyOnce = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].BuildOnlyOnce;
			newB.IsCommodity = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].IsCommodity;
			newB.CommodityBonusType = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].CommodityBonusType;
			newB.CommodityBonusAmount = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].CommodityBonusAmount;
			newB.ResourceCreated = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].ResourceCreated;
			newB.ResourceConsumed = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].ResourceConsumed;
			newB.ConsumptionPerTurn = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].ConsumptionPerTurn;
			newB.OutputPerTurn = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].OutputPerTurn;
			newB.CommodityRequired = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].CommodityRequired;
            newB.ShipRepair = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].ShipRepair;
			return newB;
		}

        public static EmpireData GetEmpireByName(string name)
        {
            foreach (EmpireData empireData in ResourceManager.Empires)
            {
                if (empireData.Traits.Name == name)
                    return empireData;
            }
            return (EmpireData)null;
        }

		private static FileInfo[] GetFilesFromDirectory(string DirPath)
		{
			FileInfo[] files;
			DirectoryInfo Dir = new DirectoryInfo(DirPath);
			try
			{
				files = Dir.GetFiles("*.*", SearchOption.AllDirectories);
			}
			catch
			{
				files = new FileInfo[0];
			}
			return files;
		}

		public static FileInfo[] GetFilesFromDirectoryNoSub(string DirPath)
		{
			FileInfo[] files;
			DirectoryInfo Dir = new DirectoryInfo(DirPath);
			try
			{
				files = Dir.GetFiles("*.*", SearchOption.TopDirectoryOnly);
			}
			catch
			{
				files = new FileInfo[0];
			}
			return files;
		}
        public static Model GetModel(string path)
        {
            return GetModel(path, false);
        }
        public static Model GetModel(string path, bool NoException)
		{
			Model item =null;
//#if !DEBUG			
//            try
//#endif
            {
                Exception t = null;
                string loaderror = string.Empty;
                bool loaded = false;
                lock (Ship_Game.ResourceManager.ModelDict)
                    if (!Ship_Game.ResourceManager.ModelDict.TryGetValue(path, out item))
                    {
                        if (GlobalStats.ActiveMod != null ) //&& GlobalStats.ActiveModInfo != null)
                        {
                            try
                            {
                                item = GetContentManager().Load<Model>(string.Concat("Mod Models/", path));
                                loaded = true;
                            }
                            catch(Microsoft.Xna.Framework.Content.ContentLoadException ex)
                            {
                                
                            }
                            catch(OutOfMemoryException)
                            {
                                throw;
                            }
                        }
                        if (!loaded)
                        {
                            try
                            {
                                item = GetContentManager().Load<Model>(path);
                            }
                            catch
                            {
                                if (!NoException)
                                    throw;
                            }
                        }
                        Ship_Game.ResourceManager.ModelDict.Add(path, item);
                    }
                   
                return item;
                if (!Ship_Game.ResourceManager.ModelDict.TryGetValue(path, out item))
				{
                    
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                   
                        item = GetContentManager().Load<Model>(path);
                        Ship_Game.ResourceManager.ModelDict.Add(path, item);
                    
				}
                
                else
                {
                    item = Ship_Game.ResourceManager.ModelDict[path];
                }
			}
//#if !DEBUG
//            catch
//#endif
            {
                if (item == null)
                {
                    item = GetContentManager().Load<Model>(string.Concat("Mod Models/", path)) as Model;
                    //item = GetContentManager().Load<SkinnedModel>(string.Concat("Mod Models/", path)) ;
                    Ship_Game.ResourceManager.ModelDict.Add(path, item);
                    //item = model;
                }
            }

            return item;
		}

		public static ShipModule GetModule(string uid)
		{
			
            ShipModule module = new ShipModule()
			{
                //All of the commented out properties here have been replaced by this single reference to 'ShipModule_Advanced' which now contains them all - Gretman
                Advanced = Ship_Game.ResourceManager.ShipModulesDict[uid].Advanced,

                //BombType = Ship_Game.ResourceManager.ShipModulesDict[uid].BombType,
                //HealPerTurn = Ship_Game.ResourceManager.ShipModulesDict[uid].HealPerTurn,
                //BonusRepairRate = Ship_Game.ResourceManager.ShipModulesDict[uid].BonusRepairRate,
                //Cargo_Capacity = Ship_Game.ResourceManager.ShipModulesDict[uid].Cargo_Capacity,
                //Cost = Ship_Game.ResourceManager.ShipModulesDict[uid].Cost,
                DescriptionIndex = Ship_Game.ResourceManager.ShipModulesDict[uid].DescriptionIndex,
                //ECM = Ship_Game.ResourceManager.ShipModulesDict[uid].ECM,
				//EMP_Protection = Ship_Game.ResourceManager.ShipModulesDict[uid].EMP_Protection,
				//explodes = Ship_Game.ResourceManager.ShipModulesDict[uid].explodes,
				FieldOfFire = Ship_Game.ResourceManager.ShipModulesDict[uid].FieldOfFire,
				hangarShipUID = Ship_Game.ResourceManager.ShipModulesDict[uid].hangarShipUID,
				hangarTimer = Ship_Game.ResourceManager.ShipModulesDict[uid].hangarTimer,
				//hangarTimerConstant = Ship_Game.ResourceManager.ShipModulesDict[uid].hangarTimerConstant,
				Health = Ship_Game.ResourceManager.ShipModulesDict[uid].HealthMax,
				//IsSupplyBay = Ship_Game.ResourceManager.ShipModulesDict[uid].IsSupplyBay,
				HealthMax = Ship_Game.ResourceManager.ShipModulesDict[uid].HealthMax,
				isWeapon = Ship_Game.ResourceManager.ShipModulesDict[uid].isWeapon,
				//IsTroopBay = Ship_Game.ResourceManager.ShipModulesDict[uid].IsTroopBay,
				Mass = Ship_Game.ResourceManager.ShipModulesDict[uid].Mass,
				//MechanicalBoardingDefense = Ship_Game.ResourceManager.ShipModulesDict[uid].MechanicalBoardingDefense,
				ModuleType = Ship_Game.ResourceManager.ShipModulesDict[uid].ModuleType,
				NameIndex = Ship_Game.ResourceManager.ShipModulesDict[uid].NameIndex,
				//numberOfColonists = Ship_Game.ResourceManager.ShipModulesDict[uid].numberOfColonists,
				//numberOfEquipment = Ship_Game.ResourceManager.ShipModulesDict[uid].numberOfEquipment,
				//numberOfFood = Ship_Game.ResourceManager.ShipModulesDict[uid].numberOfFood,
				OrdinanceCapacity = Ship_Game.ResourceManager.ShipModulesDict[uid].OrdinanceCapacity,
				//OrdnanceAddedPerSecond = Ship_Game.ResourceManager.ShipModulesDict[uid].OrdnanceAddedPerSecond,
				//PowerDraw = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerDraw,
				//PowerFlowMax = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerFlowMax,
				//PowerRadius = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerRadius,
				//PowerStoreMax = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerStoreMax,
				//SensorRange = Ship_Game.ResourceManager.ShipModulesDict[uid].SensorRange,
				shield_power = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_power_max,    //Hmmm... This one is strange -Gretman
				//shield_power_max = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_power_max,
				//shield_radius = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_radius,
				//shield_recharge_delay = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_recharge_delay,
				//shield_recharge_rate = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_recharge_rate,
				//TechLevel = Ship_Game.ResourceManager.ShipModulesDict[uid].TechLevel,
				//thrust = Ship_Game.ResourceManager.ShipModulesDict[uid].thrust,
                //TroopBoardingDefense = Ship_Game.ResourceManager.ShipModulesDict[uid].TroopBoardingDefense,    //Not referenced in code, removing to save memory -Gretman
                //TroopCapacity = Ship_Game.ResourceManager.ShipModulesDict[uid].TroopCapacity,
				//TroopsSupplied = Ship_Game.ResourceManager.ShipModulesDict[uid].TroopsSupplied,
				UID = Ship_Game.ResourceManager.ShipModulesDict[uid].UID,
				//WarpThrust = Ship_Game.ResourceManager.ShipModulesDict[uid].WarpThrust,
				XSIZE = Ship_Game.ResourceManager.ShipModulesDict[uid].XSIZE,
				YSIZE = Ship_Game.ResourceManager.ShipModulesDict[uid].YSIZE,
				//InhibitionRadius = Ship_Game.ResourceManager.ShipModulesDict[uid].InhibitionRadius,
				//FightersOnly = Ship_Game.ResourceManager.ShipModulesDict[uid].FightersOnly,
                //DroneModule = Ship_Game.ResourceManager.ShipModulesDict[uid].DroneModule,
                //FighterModule = Ship_Game.ResourceManager.ShipModulesDict[uid].FighterModule,
                //CorvetteModule = Ship_Game.ResourceManager.ShipModulesDict[uid].CorvetteModule,
                //FrigateModule = Ship_Game.ResourceManager.ShipModulesDict[uid].FrigateModule,
                //DestroyerModule = Ship_Game.ResourceManager.ShipModulesDict[uid].DestroyerModule,
                //CruiserModule = Ship_Game.ResourceManager.ShipModulesDict[uid].CruiserModule,
                //CarrierModule = Ship_Game.ResourceManager.ShipModulesDict[uid].CarrierModule,
                //CapitalModule = Ship_Game.ResourceManager.ShipModulesDict[uid].CapitalModule,
                //FreighterModule = Ship_Game.ResourceManager.ShipModulesDict[uid].FreighterModule,
                //PlatformModule = Ship_Game.ResourceManager.ShipModulesDict[uid].PlatformModule,
                //StationModule = Ship_Game.ResourceManager.ShipModulesDict[uid].StationModule,
				//TurnThrust = Ship_Game.ResourceManager.ShipModulesDict[uid].TurnThrust,
				//DeployBuildingOnColonize = Ship_Game.ResourceManager.ShipModulesDict[uid].DeployBuildingOnColonize,
				PermittedHangarRoles = Ship_Game.ResourceManager.ShipModulesDict[uid].PermittedHangarRoles,
				//MaximumHangarShipSize = Ship_Game.ResourceManager.ShipModulesDict[uid].MaximumHangarShipSize,
				//IsRepairModule = Ship_Game.ResourceManager.ShipModulesDict[uid].IsRepairModule,
                //MountLeft = Ship_Game.ResourceManager.ShipModulesDict[uid].MountLeft,    //Not referenced in code, removing to save memory -Gretman
                //MountRight = Ship_Game.ResourceManager.ShipModulesDict[uid].MountRight,    //Not referenced in code, removing to save memory -Gretman
                //MountRear = Ship_Game.ResourceManager.ShipModulesDict[uid].MountRear,    //Not referenced in code, removing to save memory -Gretman
                //WarpMassCapacity = Ship_Game.ResourceManager.ShipModulesDict[uid].WarpMassCapacity,
				//PowerDrawAtWarp = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerDrawAtWarp,
				//FTLSpeed = Ship_Game.ResourceManager.ShipModulesDict[uid].FTLSpeed,
				//ResourceStored = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourceStored,
                //ResourceRequired = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourceRequired,    //Not referenced in code, removing to save memory -Gretman
                //ResourcePerSecond = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourcePerSecond,    //Not referenced in code, removing to save memory -Gretman
                //ResourcePerSecondWarp = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourcePerSecondWarp,    //Not referenced in code, removing to save memory -Gretman
                //ResourceStorageAmount = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourceStorageAmount,
				//IsCommandModule = Ship_Game.ResourceManager.ShipModulesDict[uid].IsCommandModule,
				//shield_recharge_combat_rate = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_recharge_combat_rate,
                //FTLSpoolTime = Ship_Game.ResourceManager.ShipModulesDict[uid].FTLSpoolTime,
                shieldsOff = Ship_Game.ResourceManager.ShipModulesDict[uid].shieldsOff,
                //SensorBonus = Ship_Game.ResourceManager.ShipModulesDict[uid].SensorBonus,
                //TransporterOrdnance = Ship_Game.ResourceManager.ShipModulesDict[uid].TransporterOrdnance,
                //TransporterPower = Ship_Game.ResourceManager.ShipModulesDict[uid].TransporterPower,
                //TransporterRange = Ship_Game.ResourceManager.ShipModulesDict[uid].TransporterRange,
                //TransporterTimerConstant = Ship_Game.ResourceManager.ShipModulesDict[uid].TransporterTimerConstant,
                //TransporterTroopLanding = Ship_Game.ResourceManager.ShipModulesDict[uid].TransporterTroopLanding,
                //TransporterTroopAssault = Ship_Game.ResourceManager.ShipModulesDict[uid].TransporterTroopAssault,
                //KineticResist = Ship_Game.ResourceManager.ShipModulesDict[uid].KineticResist,
                //EnergyResist = Ship_Game.ResourceManager.ShipModulesDict[uid].EnergyResist,
                //GuidedResist = Ship_Game.ResourceManager.ShipModulesDict[uid].GuidedResist,
                //MissileResist = Ship_Game.ResourceManager.ShipModulesDict[uid].MissileResist,
                //HybridResist = Ship_Game.ResourceManager.ShipModulesDict[uid].HybridResist,
                //BeamResist = Ship_Game.ResourceManager.ShipModulesDict[uid].BeamResist,
                //ExplosiveResist = Ship_Game.ResourceManager.ShipModulesDict[uid].ExplosiveResist,
                //InterceptResist = Ship_Game.ResourceManager.ShipModulesDict[uid].InterceptResist,
                //RailgunResist = Ship_Game.ResourceManager.ShipModulesDict[uid].RailgunResist,
                //SpaceBombResist = Ship_Game.ResourceManager.ShipModulesDict[uid].SpaceBombResist,
                //BombResist = Ship_Game.ResourceManager.ShipModulesDict[uid].BombResist,
                //BioWeaponResist = Ship_Game.ResourceManager.ShipModulesDict[uid].BioWeaponResist,
                //DroneResist = Ship_Game.ResourceManager.ShipModulesDict[uid].DroneResist,
                //WarpResist = Ship_Game.ResourceManager.ShipModulesDict[uid].WarpResist,
                //TorpedoResist = Ship_Game.ResourceManager.ShipModulesDict[uid].TorpedoResist,
                //CannonResist = Ship_Game.ResourceManager.ShipModulesDict[uid].CannonResist,
                //SubspaceResist = Ship_Game.ResourceManager.ShipModulesDict[uid].SubspaceResist,
                //PDResist = Ship_Game.ResourceManager.ShipModulesDict[uid].PDResist,
                //FlakResist = Ship_Game.ResourceManager.ShipModulesDict[uid].FlakResist,
                //APResist = Ship_Game.ResourceManager.ShipModulesDict[uid].APResist,
                //DamageThreshold = Ship_Game.ResourceManager.ShipModulesDict[uid].DamageThreshold,
                //shield_threshold = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_threshold,
                //shield_energy_resist = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_energy_resist,
                //shield_kinetic_resist = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_kinetic_resist,
                //shield_explosive_resist = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_explosive_resist,
                //shield_flak_resist = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_flak_resist,
                //shield_hybrid_resist = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_hybrid_resist,
                //shield_missile_resist = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_missile_resist,
                //shield_railgun_resist = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_railgun_resist,
                //shield_subspace_resist = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_subspace_resist,
                //shield_warp_resist = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_warp_resist,
                //shield_beam_resist = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_beam_resist,
                //IndirectPower = Ship_Game.ResourceManager.ShipModulesDict[uid].IndirectPower,
                //isPowerArmour = Ship_Game.ResourceManager.ShipModulesDict[uid].isPowerArmour,
                //isBulkhead = Ship_Game.ResourceManager.ShipModulesDict[uid].isBulkhead,
                //TargetTracking = Ship_Game.ResourceManager.ShipModulesDict[uid].TargetTracking

			};

            #region TargetWeight
            module.TargetValue += module.ModuleType == ShipModuleType.Armor ? -1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Bomb ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Command ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Countermeasure ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Drone ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Engine ? 2 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.FuelCell ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Hangar ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.MainGun ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.MissileLauncher ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Ordnance ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.PowerPlant ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Sensors ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Shield ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Spacebomb ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Special ? 1 : 0;
            module.TargetValue += module.ModuleType == ShipModuleType.Turret ? 1 : 0;
            module.TargetValue += module.explodes ? 2 : 0;
            module.TargetValue += module.isWeapon ? 1 : 0;

            #endregion

            return module;
		}

		public static Ship GetPlayerShip(string key)
		{
			Ship newShip = new Ship()
			{
				PlayerShip = true
				//Role = Ship_Game.ResourceManager.ShipsDict[key].Role
			};
            newShip.shipData = Ship_Game.ResourceManager.ShipsDict[key].shipData;
			newShip.LoadContent(GetContentManager());
			SceneObject newSO = new SceneObject();
			if (!Ship_Game.ResourceManager.ShipsDict[key].GetShipData().Animated)
			{
				newSO = new SceneObject(Ship_Game.ResourceManager.GetModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
				newShip.SetSO(newSO);
			}
			else
			{
				SkinnedModel model = Ship_Game.ResourceManager.GetSkinnedModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			newSO.ObjectType = ObjectType.Dynamic;
			newShip.SetSO(newSO);
			foreach (Thruster t in Ship_Game.ResourceManager.ShipsDict[key].GetTList())
			{
				Thruster thr = new Thruster()
				{
					Parent = newShip,
					XMLPos = t.XMLPos,
					tscale = t.tscale
				};
				newShip.GetTList().Add(thr);
			}
			if (newShip.FirstNode == null)
			{
				(new ShipModuleNode()).Next = new ShipModuleNode();
			}
			foreach (ModuleSlot slot in Ship_Game.ResourceManager.ShipsDict[key].ModuleSlotList)
			{
				ModuleSlot newSlot = new ModuleSlot()
				{
					Restrictions = slot.Restrictions,
					Position = slot.Position,
					facing = slot.facing,
					InstalledModuleUID = slot.InstalledModuleUID
				};
				newShip.ModuleSlotList.AddLast(newSlot);
			}
			return newShip;
		}

		public static RacialTraits GetRaceTraits()
		{
            //Added by McShooterz: mod folder support for RacialTraits folder
            FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/RacialTraits")) ? string.Concat(Ship_Game.ResourceManager.WhichModPath, "/RacialTraits") : "Content/RacialTraits");
			XmlSerializer serializer1 = new XmlSerializer(typeof(RacialTraits));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
                RacialTraits data = null;
                try
                {
                     data = (RacialTraits)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
					ReportLoadingError(fileInfoArray[i], "GetRaceTraits", e);
                }
				//stream.Close();
				stream.Dispose();
				if (data == null)
					continue;
                
				foreach(RacialTrait trait in data.TraitList)
                {
                    if (Localizer.LocalizerDict.ContainsKey(trait.TraitName + OffSet))
                    {
                        trait.TraitName += OffSet;
                        Localizer.used[trait.TraitName] = true;
                    }
                    if (Localizer.LocalizerDict.ContainsKey(trait.Description + OffSet))
                    {
                        trait.Description += OffSet;
                        Localizer.used[trait.Description] = true;
                    }
                }
                Ship_Game.ResourceManager.rt = data;
			}
			textList = null;
			return Ship_Game.ResourceManager.rt;
		}

		public static Ship GetShip(string key)
		{
            Ship newShip = new Ship()
            {
                shipData = Ship_Game.ResourceManager.ShipsDict[key].shipData,
                BaseStrength = Ship_Game.ResourceManager.ShipsDict[key].BaseStrength,
                BaseCanWarp = Ship_Game.ResourceManager.ShipsDict[key].BaseCanWarp
            };
			newShip.LoadContent(GetContentManager());
			newShip.Name = Ship_Game.ResourceManager.ShipsDict[key].Name;
			SceneObject newSO = new SceneObject();
			if (!Ship_Game.ResourceManager.ShipsDict[key].GetShipData().Animated)
			{
				newSO = new SceneObject(Ship_Game.ResourceManager.GetModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
				newShip.SetSO(newSO);
			}
			else
			{
				SkinnedModel model = Ship_Game.ResourceManager.GetSkinnedModel(Ship_Game.ResourceManager.ShipsDict[key].ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			newSO.ObjectType = ObjectType.Dynamic;
			newShip.SetSO(newSO);
			foreach (Thruster t in Ship_Game.ResourceManager.ShipsDict[key].GetTList())
			{
				Thruster thr = new Thruster()
				{
					Parent = newShip,
					tscale = t.tscale,
					XMLPos = t.XMLPos
				};
				newShip.GetTList().Add(thr);
			}
            foreach (ModuleSlot slot in Ship_Game.ResourceManager.ShipsDict[key].ModuleSlotList)
            {
                ModuleSlot newSlot = new ModuleSlot();
                newSlot.SetParent(newShip);
                newSlot.SlotOptions = slot.SlotOptions;
                newSlot.Restrictions = slot.Restrictions;
                newSlot.Position = slot.Position;
                newSlot.facing = slot.facing;
                newSlot.state = slot.state;
                newSlot.InstalledModuleUID = slot.InstalledModuleUID;
                newShip.ModuleSlotList.AddLast(newSlot);
            }
         
			return newShip;
		}

		public static SkinnedModel GetSkinnedModel(string path)
		{
			return GetContentManager().Load<SkinnedModel>(path);
		}

		public static Weapon GetWeapon(string uid)
		{
			Weapon w = new Weapon()
			{
				FakeExplode = Ship_Game.ResourceManager.WeaponsDict[uid].FakeExplode,
				Animated = Ship_Game.ResourceManager.WeaponsDict[uid].Animated,
				AnimationPath = Ship_Game.ResourceManager.WeaponsDict[uid].AnimationPath,
				BeamPowerCostPerSecond = Ship_Game.ResourceManager.WeaponsDict[uid].BeamPowerCostPerSecond,
				BeamThickness = Ship_Game.ResourceManager.WeaponsDict[uid].BeamThickness,
                BeamDuration = Ship_Game.ResourceManager.WeaponsDict[uid].BeamDuration,
				BombPopulationKillPerHit = Ship_Game.ResourceManager.WeaponsDict[uid].BombPopulationKillPerHit,
				BombTroopDamage_Max = Ship_Game.ResourceManager.WeaponsDict[uid].BombTroopDamage_Max,
				BombTroopDamage_Min = Ship_Game.ResourceManager.WeaponsDict[uid].BombTroopDamage_Min,
				DamageAmount = Ship_Game.ResourceManager.WeaponsDict[uid].DamageAmount,
				DamageRadius = Ship_Game.ResourceManager.WeaponsDict[uid].DamageRadius,
				EMPDamage = Ship_Game.ResourceManager.WeaponsDict[uid].EMPDamage,
				ExpColor = Ship_Game.ResourceManager.WeaponsDict[uid].ExpColor,
				explodes = Ship_Game.ResourceManager.WeaponsDict[uid].explodes,
				FireArc = Ship_Game.ResourceManager.WeaponsDict[uid].FireArc,
				FireCone = Ship_Game.ResourceManager.WeaponsDict[uid].FireCone,
				fireCueName = Ship_Game.ResourceManager.WeaponsDict[uid].fireCueName,
				fireDelay = Ship_Game.ResourceManager.WeaponsDict[uid].fireDelay,
				Frames = Ship_Game.ResourceManager.WeaponsDict[uid].Frames,
				HitPoints = Ship_Game.ResourceManager.WeaponsDict[uid].HitPoints,
				InFlightCue = Ship_Game.ResourceManager.WeaponsDict[uid].InFlightCue,
				isBeam = Ship_Game.ResourceManager.WeaponsDict[uid].isBeam,
				isMainGun = Ship_Game.ResourceManager.WeaponsDict[uid].isMainGun,
				isTurret = Ship_Game.ResourceManager.WeaponsDict[uid].isTurret,
				Light = Ship_Game.ResourceManager.WeaponsDict[uid].Light,
				LoopAnimation = Ship_Game.ResourceManager.WeaponsDict[uid].LoopAnimation,
				MassDamage = Ship_Game.ResourceManager.WeaponsDict[uid].MassDamage,
				ModelPath = Ship_Game.ResourceManager.WeaponsDict[uid].ModelPath,
				MuzzleFlash = Ship_Game.ResourceManager.WeaponsDict[uid].MuzzleFlash,
				Name = Ship_Game.ResourceManager.WeaponsDict[uid].Name,
				OrdinanceRequiredToFire = Ship_Game.ResourceManager.WeaponsDict[uid].OrdinanceRequiredToFire,
				particleDelay = Ship_Game.ResourceManager.WeaponsDict[uid].particleDelay,
				PowerDamage = Ship_Game.ResourceManager.WeaponsDict[uid].PowerDamage,
				PowerRequiredToFire = Ship_Game.ResourceManager.WeaponsDict[uid].PowerRequiredToFire,
				ProjectileCount = Ship_Game.ResourceManager.WeaponsDict[uid].ProjectileCount,
				ProjectileRadius = Ship_Game.ResourceManager.WeaponsDict[uid].ProjectileRadius,
				ProjectileSpeed = Ship_Game.ResourceManager.WeaponsDict[uid].ProjectileSpeed,
				ProjectileTexturePath = Ship_Game.ResourceManager.WeaponsDict[uid].ProjectileTexturePath,
				Range = Ship_Game.ResourceManager.WeaponsDict[uid].Range,
				RepulsionDamage = Ship_Game.ResourceManager.WeaponsDict[uid].RepulsionDamage,
				Scale = Ship_Game.ResourceManager.WeaponsDict[uid].Scale,
				ShieldPenChance = Ship_Game.ResourceManager.WeaponsDict[uid].ShieldPenChance,
				SiphonDamage = Ship_Game.ResourceManager.WeaponsDict[uid].SiphonDamage,
				ToggleSoundName = Ship_Game.ResourceManager.WeaponsDict[uid].ToggleSoundName,
				TroopDamageChance = Ship_Game.ResourceManager.WeaponsDict[uid].TroopDamageChance,
				UID = Ship_Game.ResourceManager.WeaponsDict[uid].UID,
				WeaponEffectType = Ship_Game.ResourceManager.WeaponsDict[uid].WeaponEffectType,
				WeaponType = Ship_Game.ResourceManager.WeaponsDict[uid].WeaponType,
				IsRepairDrone = Ship_Game.ResourceManager.WeaponsDict[uid].IsRepairDrone,
				HitsFriendlies = Ship_Game.ResourceManager.WeaponsDict[uid].HitsFriendlies,
				BombHardDamageMax = Ship_Game.ResourceManager.WeaponsDict[uid].BombHardDamageMax,
				BombHardDamageMin = Ship_Game.ResourceManager.WeaponsDict[uid].BombHardDamageMin,
				HardCodedAction = Ship_Game.ResourceManager.WeaponsDict[uid].HardCodedAction,
				TruePD = Ship_Game.ResourceManager.WeaponsDict[uid].TruePD,
				SalvoCount = Ship_Game.ResourceManager.WeaponsDict[uid].SalvoCount,
				SalvoTimer = Ship_Game.ResourceManager.WeaponsDict[uid].SalvoTimer,
				PlaySoundOncePerSalvo = Ship_Game.ResourceManager.WeaponsDict[uid].PlaySoundOncePerSalvo,
				EffectVsArmor = Ship_Game.ResourceManager.WeaponsDict[uid].EffectVsArmor,
				EffectVSShields = Ship_Game.ResourceManager.WeaponsDict[uid].EffectVSShields,
				RotationRadsPerSecond = Ship_Game.ResourceManager.WeaponsDict[uid].RotationRadsPerSecond,
				Tag_Beam = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Beam,
				Tag_Energy = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Energy,
				Tag_Explosive = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Explosive,
				Tag_Guided = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Guided,
				Tag_Hybrid = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Hybrid,
				Tag_Intercept = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Intercept,
				Tag_Kinetic = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Kinetic,
				Tag_Missile = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Missile,
				Tag_Railgun = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Railgun,
				Tag_Warp = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Warp,
				Tag_Torpedo = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Torpedo,
				Tag_Subspace = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Subspace,
				Tag_Cannon = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Cannon,
				Tag_Bomb = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Bomb,
				Tag_Drone = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Drone,
				Tag_BioWeapon = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_BioWeapon,
				Tag_PD = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_PD,
                Tag_Array = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Array,
                Tag_Flak = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Flak,
                Tag_Tractor = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_Tractor,
                Tag_SpaceBomb = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_SpaceBomb,
                ECMResist = Ship_Game.ResourceManager.WeaponsDict[uid].ECMResist,
                Excludes_Fighters = Ship_Game.ResourceManager.WeaponsDict[uid].Excludes_Fighters,
                Excludes_Corvettes = Ship_Game.ResourceManager.WeaponsDict[uid].Excludes_Corvettes,
                Excludes_Capitals = Ship_Game.ResourceManager.WeaponsDict[uid].Excludes_Capitals,
                Excludes_Stations = Ship_Game.ResourceManager.WeaponsDict[uid].Excludes_Stations,
                isRepairBeam = Ship_Game.ResourceManager.WeaponsDict[uid].isRepairBeam,
                ExplosionRadiusVisual = Ship_Game.ResourceManager.WeaponsDict[uid].ExplosionRadiusVisual,
                TerminalPhaseAttack = Ship_Game.ResourceManager.WeaponsDict[uid].TerminalPhaseAttack,
                TerminalPhaseDistance = Ship_Game.ResourceManager.WeaponsDict[uid].TerminalPhaseDistance,
                TerminalPhaseSpeedMod = Ship_Game.ResourceManager.WeaponsDict[uid].TerminalPhaseSpeedMod,
                ArmourPen = Ship_Game.ResourceManager.WeaponsDict[uid].ArmourPen,
                RangeVariance = Ship_Game.ResourceManager.WeaponsDict[uid].RangeVariance,
                //ExplosionFlash = Ship_Game.ResourceManager.WeaponsDict[uid].ExplosionFlash,          //Not referenced in code, removing to save memory -Gretman
                AltFireMode = Ship_Game.ResourceManager.WeaponsDict[uid].AltFireMode,
                AltFireTriggerFighter = Ship_Game.ResourceManager.WeaponsDict[uid].AltFireTriggerFighter,
                SecondaryFire = Ship_Game.ResourceManager.WeaponsDict[uid].SecondaryFire
			};
			return w;
		}

		public static void Initialize(ContentManager c)
		{
        
            Ship_Game.ResourceManager.WhichModPath = "Content";
			Ship_Game.ResourceManager.LoadItAll();
		}

		private static void LoadArtifacts()
		{
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Artifacts"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(List<Artifact>));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
                List<Artifact> data = null;
                try
                {
                    data = (List<Artifact>)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
					ReportLoadingError(fileInfoArray[i], "LoadArtifacts", e);
                }
				//stream.Close();
				stream.Dispose();
				if (data == null)
					continue;

				foreach (Artifact art in data)
				{
                    art.DescriptionIndex += OffSet;
                    art.NameIndex += OffSet;
                    string name = String.Intern(art.Name);
                    if (Ship_Game.ResourceManager.ArtifactsDict.ContainsKey(name))
					{
						Ship_Game.ResourceManager.ArtifactsDict[art.Name] = art;
					}
					else
					{
                        Ship_Game.ResourceManager.ArtifactsDict.Add(name, art);
					}
				}
			}
		}

		private static void LoadBuildings()
		{
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Buildings"));
			XmlSerializer serializer0 = new XmlSerializer(typeof(Building));
			FileInfo[] fileInfoArray = textList;
			
            for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
                Building newB = null;
                try
                {
                    newB = (Building)serializer0.Deserialize(stream);
                }
                catch (Exception e)
                {
					ReportLoadingError(fileInfoArray[i], "LoadBuildings", e);
                }
				//stream.Close();
				stream.Dispose();
				if (newB == null)
					continue;
                try
                {
                    if (Localizer.LocalizerDict.ContainsKey(newB.DescriptionIndex + OffSet))
                    {
                        newB.DescriptionIndex += OffSet;
                        Localizer.used[newB.DescriptionIndex] =true;
                    }
                    if (Localizer.LocalizerDict.ContainsKey(newB.NameTranslationIndex + OffSet))
                    {
                        newB.NameTranslationIndex += OffSet;
                        Localizer.used[newB.NameTranslationIndex] = true;
                    }
                    if (Localizer.LocalizerDict.ContainsKey(newB.ShortDescriptionIndex + OffSet))
                    {
                        newB.ShortDescriptionIndex += OffSet;
                        Localizer.used[newB.ShortDescriptionIndex] = true;
                    }
                    
                    if (Ship_Game.ResourceManager.BuildingsDict.ContainsKey(newB.Name))
                    {
                        Ship_Game.ResourceManager.BuildingsDict[String.Intern(newB.Name)] = newB;
                    }
                    else
                    {
                        Ship_Game.ResourceManager.BuildingsDict.Add(String.Intern(newB.Name), newB);
                    }
                }
                catch(NullReferenceException ex)
                {
                    ex.Data["Building Lookup"] = newB.Name;
					ReportLoadingError(fileInfoArray[i], "LoadBuildings", ex);
                }
			}
			textList = null;
		}

		private static void LoadDialogs()
		{
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/DiplomacyDialogs/", GlobalStats.Config.Language, "/"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(DiplomacyDialog));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileInfo FI = fileInfoArray[i];
				FileStream stream = FI.OpenRead();
				DiplomacyDialog data = null;
                try
                {
                    data = (DiplomacyDialog)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
					ReportLoadingError(fileInfoArray[i], "LoadDialogs", e);
                }
				//stream.Close();
				stream.Dispose();
				if (data == null)
					continue;

                if (Ship_Game.ResourceManager.DDDict.ContainsKey(Path.GetFileNameWithoutExtension(FI.Name)))
				{
					Ship_Game.ResourceManager.DDDict[string.Intern(Path.GetFileNameWithoutExtension(FI.Name))] = data;
				}
				else
				{
                    Ship_Game.ResourceManager.DDDict.Add(string.Intern(Path.GetFileNameWithoutExtension(FI.Name)), data);
				}
			}
		}

		public static void LoadEmpires()
		{
			Ship_Game.ResourceManager.Empires.Clear();
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Races"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(EmpireData));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
                EmpireData data = null;
                try
                {
                    data = (EmpireData)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
					ReportLoadingError(fileInfoArray[i], "LoadEmpires", e);
                }
                data.TroopDescriptionIndex = +OffSet;
                data.TroopNameIndex += OffSet;
                //stream.Close();
				stream.Dispose();
				Ship_Game.ResourceManager.Empires.Add(data);
			}
			textList = null;
		}

		public static void LoadEncounters()
		{
			Ship_Game.ResourceManager.Encounters.Clear();
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Encounter Dialogs"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(Encounter));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
                Encounter data = null;
                try
                {
                     data = (Encounter)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
					ReportLoadingError(fileInfoArray[i], "LoadEncounters", e);
                }				
                //stream.Close();
				stream.Dispose();
				Ship_Game.ResourceManager.Encounters.Add(data);
			}
		}

		private static void LoadExpEvents()
		{
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Exploration Events"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(ExplorationEvent));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileInfo FI = fileInfoArray[i];
				FileStream stream = FI.OpenRead();
                ExplorationEvent data = null;
                try
                {
                     data = (ExplorationEvent)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
					ReportLoadingError(fileInfoArray[i], "LoadExpEvents", e);
                }
				//stream.Close();
				stream.Dispose();				
                if (Ship_Game.ResourceManager.EventsDict.ContainsKey(Path.GetFileNameWithoutExtension(FI.Name)))
				{
					Ship_Game.ResourceManager.EventsDict[Path.GetFileNameWithoutExtension(FI.Name)] = data;
				}
				else
				{
					Ship_Game.ResourceManager.EventsDict.Add(Path.GetFileNameWithoutExtension(FI.Name), data);
				}
			}
		}

		private static void LoadFlagTextures()
		{
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Flags"));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileInfo FI = fileInfoArray[i];
				if (Path.GetFileNameWithoutExtension(FI.Name) != "Thumbs")
				{
					Texture2D tex = GetContentManager().Load<Texture2D>((Ship_Game.ResourceManager.WhichModPath == "Content" ? string.Concat("Flags/", Path.GetFileNameWithoutExtension(FI.Name)) : string.Concat(".../", Ship_Game.ResourceManager.WhichModPath, "/Flags/", Path.GetFileNameWithoutExtension(FI.Name))));
					KeyValuePair<string, Texture2D> Flag = new KeyValuePair<string, Texture2D>(Path.GetFileNameWithoutExtension(FI.Name), tex);
					Ship_Game.ResourceManager.FlagTextures.Add(Flag);
				}
			}
		}

		private static void LoadGoods()
		{
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/Goods");
			XmlSerializer serializer1 = new XmlSerializer(typeof(Good));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileInfo FI = fileInfoArray[i];
				FileStream stream = FI.OpenRead();
                Good data = null;
                try
                {
                     data = (Good)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
                    ReportLoadingError(fileInfoArray[i], "LoadGoods", e);
                }
				data.UID = String.Intern(Path.GetFileNameWithoutExtension(FI.Name));
				//stream.Close();
				stream.Dispose();
                //noLocalization
				if (Ship_Game.ResourceManager.GoodsDict.ContainsKey(data.UID))
				{
					Ship_Game.ResourceManager.GoodsDict[data.UID] = data;
				}
				else
				{
					Ship_Game.ResourceManager.GoodsDict.Add(data.UID, data);
				}
			}
			textList = null;
		}

		public static void LoadHardcoreTechTree()
		{
			Ship_Game.ResourceManager.TechTree.Clear();
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Technology_HardCore"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(Technology));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileInfo FI = fileInfoArray[i];
				FileStream stream = FI.OpenRead();
				Technology data = (Technology)serializer1.Deserialize(stream);
				//stream.Close();
				stream.Dispose();
                string.Intern(data.UID);
				if (Ship_Game.ResourceManager.TechTree.ContainsKey(Path.GetFileNameWithoutExtension(FI.Name)))
				{
					Ship_Game.ResourceManager.TechTree[Path.GetFileNameWithoutExtension(FI.Name)] = data;
				}
				else
				{
					//Ship_Game.ResourceManager.TechTree.Add(Path.GetFileNameWithoutExtension(FI.Name), data);
                    Ship_Game.ResourceManager.TechTree.Add(data.UID, data);
				}
			}
			textList = null;
		}

		public static List<ShipData> LoadHullData()
		{
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Hulls"));
			List<ShipData> retList = new List<ShipData>();
			XmlSerializer serializer0 = new XmlSerializer(typeof(ShipData));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileInfo FI = fileInfoArray[i];
				FileStream stream = FI.OpenRead();
                ShipData newShipData = null;
                try
                {
                    newShipData = (ShipData)serializer0.Deserialize(stream);
                }
                catch (Exception e)
                {
					ReportLoadingError(fileInfoArray[i], "LoadHullData", e);
                }
				if (newShipData == null)
					continue;
				
                //stream.Close();
				stream.Dispose();
				newShipData.Hull = String.Intern(string.Concat(FI.Directory.Name, "/", newShipData.Hull));
                if (!string.IsNullOrEmpty(newShipData.EventOnDeath) && string.IsNullOrEmpty(string.IsInterned(newShipData.EventOnDeath)))
                    string.Intern(newShipData.EventOnDeath);
                if (!string.IsNullOrEmpty(newShipData.ModelPath) && string.IsNullOrEmpty(string.IsInterned(newShipData.ModelPath)))
                    string.Intern(newShipData.ModelPath);
                if (!string.IsNullOrEmpty(newShipData.ShipStyle) && string.IsNullOrEmpty(string.IsInterned(newShipData.ShipStyle)))
                    string.Intern(newShipData.ShipStyle);
                if (!string.IsNullOrEmpty(newShipData.Name) && string.IsNullOrEmpty(string.IsInterned(newShipData.Name)))
                    string.Intern(newShipData.Name);
                if (!string.IsNullOrEmpty(newShipData.IconPath))
                    string.Intern(newShipData.IconPath);
                if (!string.IsNullOrEmpty(newShipData.Hull))
                    string.Intern(newShipData.Hull);
                if (!string.IsNullOrEmpty(newShipData.SelectionGraphic))
                    string.Intern(newShipData.SelectionGraphic);
				newShipData.ShipStyle = String.Intern(FI.Directory.Name);
				if (Ship_Game.ResourceManager.HullsDict.ContainsKey(newShipData.Hull))
				{
					Ship_Game.ResourceManager.HullsDict[newShipData.Hull] = newShipData;
				}
				else
				{
					Ship_Game.ResourceManager.HullsDict.Add(newShipData.Hull, newShipData);
				}
				retList.Add(newShipData);
			}
			return retList;
		}

		private static void LoadItAll()
		{
            ResourceManager.OffSet = 0;
            Ship_Game.ResourceManager.LoadLanguage();
            Ship_Game.ResourceManager.LoadTroops();
			Ship_Game.ResourceManager.LoadTextures();
			Ship_Game.ResourceManager.LoadToolTips();
			Ship_Game.ResourceManager.LoadHullData();
			Ship_Game.ResourceManager.LoadWeapons();
			Ship_Game.ResourceManager.LoadShipModules();
			Ship_Game.ResourceManager.LoadGoods();
			Ship_Game.ResourceManager.LoadShips();
			Ship_Game.ResourceManager.LoadJunk();
			Ship_Game.ResourceManager.LoadProjTexts();
			Ship_Game.ResourceManager.LoadBuildings();
			Ship_Game.ResourceManager.LoadProjectileMeshes();
			Ship_Game.ResourceManager.LoadTechTree();
			Ship_Game.ResourceManager.LoadRandomItems();
			Ship_Game.ResourceManager.LoadFlagTextures();
			Ship_Game.ResourceManager.LoadNebulas();
			Ship_Game.ResourceManager.LoadSmallStars();
			Ship_Game.ResourceManager.LoadMediumStars();
			Ship_Game.ResourceManager.LoadLargeStars();
			Ship_Game.ResourceManager.LoadEmpires();
			Ship_Game.ResourceManager.LoadDialogs();
			Ship_Game.ResourceManager.LoadEncounters();
			Ship_Game.ResourceManager.LoadExpEvents();
            Ship_Game.ResourceManager.LoadArtifacts();			
            Ship_Game.ResourceManager.LoadShipRoles();
            Ship_Game.ResourceManager.LoadPlanetEdicts();
            //Ship_Game.ResourceManager.MarkShipDesignsUnlockable();
            
		}

		private static void LoadJunk()
		{
			FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/Model/SpaceJunk");
			for (int num = 0; num < (int)filesFromDirectory.Length; num++)
			{
				FileInfo FI = filesFromDirectory[num];
				for (int i = 1; i < 15; i++)
				{
					try
					{
						if (Path.GetFileNameWithoutExtension(FI.Name) == string.Concat("spacejunk", i))
						{
							Model junk = GetContentManager().Load<Model>(string.Concat("Model/SpaceJunk/", Path.GetFileNameWithoutExtension(FI.Name)));
							Ship_Game.ResourceManager.JunkModels[i] = junk;
						}
					}
					catch (Exception e) 
					{  
						ReportLoadingError(FI, "LoadJunk", e);
					} 
				}
			}
		}

		private static void LoadLanguage()
		{



            FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Localization/English/"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(LocalizationFile));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
				LocalizationFile data = (LocalizationFile)serializer1.Deserialize(stream);
				//stream.Close();
				stream.Dispose();				
                Ship_Game.ResourceManager.LanguageFile = data;
			}
			Localizer.FillLocalizer();
			if (GlobalStats.Config.Language != "English")
			{
				textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Localization/", GlobalStats.Config.Language, "/"));
				FileInfo[] fileInfoArray1 = textList;
				for (int j = 0; j < (int)fileInfoArray1.Length; j++)
				{
					FileStream stream = fileInfoArray1[j].OpenRead();
					LocalizationFile data = (LocalizationFile)serializer1.Deserialize(stream);
					//stream.Close();
					stream.Dispose();
					Ship_Game.ResourceManager.LanguageFile = data;
				}
			}
			Localizer.FillLocalizer();
		}
        private static void LoadLanguageMods()
        {
            FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Localization/English/"));
            XmlSerializer serializer1 = new XmlSerializer(typeof(LocalizationFile));
            FileInfo[] fileInfoArray = textList;
            for (int i = 0; i < (int)fileInfoArray.Length; i++)
            {
                FileStream stream = fileInfoArray[i].OpenRead();
                LocalizationFile data = (LocalizationFile)serializer1.Deserialize(stream);
                //stream.Close();
                stream.Dispose();
                
                Ship_Game.ResourceManager.LanguageFile = data;
            }
            Localizer.FillLocalizer();
            if (GlobalStats.Config.Language != "English")
            {
                textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Localization/", GlobalStats.Config.Language, "/"));
                FileInfo[] fileInfoArray1 = textList;
                for (int j = 0; j < (int)fileInfoArray1.Length; j++)
                {
                    FileStream stream = fileInfoArray1[j].OpenRead();
                    LocalizationFile data = (LocalizationFile)serializer1.Deserialize(stream);
                    //stream.Close();
                    stream.Dispose();
                    Ship_Game.ResourceManager.LanguageFile = data;
                }
            }
            Localizer.FillLocalizer();
        }

		private static void LoadLargeStars()
		{
			FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/LargeStars");
			for (int i = 0; i < (int)filesFromDirectory.Length; i++)
			{
				FileInfo FI = filesFromDirectory[i];
				try
				{
					if (Path.GetFileNameWithoutExtension(FI.Name) != "Thumbs")
					{
						Texture2D tex = GetContentManager().Load<Texture2D>(string.Concat("LargeStars/", Path.GetFileNameWithoutExtension(FI.Name)));
						Ship_Game.ResourceManager.LargeStars.Add(tex);
					}
				}
				catch (Exception e)
				{
					ReportLoadingError(FI, "LoadLargeStars", e);
				}
			}
		}

		private static void LoadMediumStars()
		{
			FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/MediumStars");
			for (int i = 0; i < (int)filesFromDirectory.Length; i++)
			{
				FileInfo FI = filesFromDirectory[i];
				try
				{
					if (Path.GetFileNameWithoutExtension(FI.Name) != "Thumbs")
					{
						Texture2D tex = GetContentManager().Load<Texture2D>(string.Concat("MediumStars/", Path.GetFileNameWithoutExtension(FI.Name)));
						Ship_Game.ResourceManager.MediumStars.Add(tex);
					}
				}
				catch (Exception e)
				{
					ReportLoadingError(FI, "LoadMediumStars", e);
				}
			}
		}

		public static void LoadModdedEmpires()
		{
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Races"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(EmpireData));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
                EmpireData data = null;
                try
                {
                     data = (EmpireData)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
					ReportLoadingError(fileInfoArray[i], "LoadModdedEmpires", e);
                }
				//stream.Close();
				stream.Dispose();
				if (data == null)
					continue;
                if (Localizer.LocalizerDict.ContainsKey(data.TroopDescriptionIndex + OffSet))
                {
                    data.TroopDescriptionIndex += ResourceManager.OffSet;
                    Localizer.used[data.TroopDescriptionIndex] = true;
                }
                if (Localizer.LocalizerDict.ContainsKey(data.TroopNameIndex + OffSet))
                {
                    data.TroopNameIndex += ResourceManager.OffSet;
                    Localizer.used[data.TroopNameIndex] = true;
                }
                
                
                //ResourceManager.Empires.RemoveAll(x => x.PortraitName == data.PortraitName);
                EmpireData remove = ResourceManager.Empires.Find(x => x.PortraitName == data.PortraitName);
                if(remove != null)
                ResourceManager.Empires.Remove(remove);
                
                    
                Ship_Game.ResourceManager.Empires.Add(data);
			}
			textList = null;
		}

		public static void LoadMods(string ModPath)
		{		
            Ship_Game.ResourceManager.WhichModPath = ModPath;
            if (ModPath == "Mods/SD_Extended")
                ResourceManager.OffSet = 0;
            else
                ResourceManager.OffSet = 32000;
            Ship_Game.ResourceManager.LoadLanguage();
			Ship_Game.ResourceManager.LoadTroops();
			Ship_Game.ResourceManager.LoadTextures();
			Ship_Game.ResourceManager.LoadToolTips();
			Ship_Game.ResourceManager.LoadHullData();
			Ship_Game.ResourceManager.LoadWeapons();
			Ship_Game.ResourceManager.LoadShipModules();
			Ship_Game.ResourceManager.LoadGoods();
			Ship_Game.ResourceManager.LoadBuildings();
			Ship_Game.ResourceManager.LoadTechTree();
			Ship_Game.ResourceManager.LoadFlagTextures();
			Ship_Game.ResourceManager.LoadModdedEmpires();
			Ship_Game.ResourceManager.LoadDialogs();
			Ship_Game.ResourceManager.LoadEncounters();
			Ship_Game.ResourceManager.LoadExpEvents();
			Ship_Game.ResourceManager.LoadArtifacts();		
			Ship_Game.ResourceManager.LoadShips();
            Ship_Game.ResourceManager.LoadRandomItems();
            Ship_Game.ResourceManager.LoadProjTexts();
            Ship_Game.ResourceManager.LoadModsProjectileMeshes();
            
			if (Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Mod Models")))
			{
				Ship_Game.ResourceManager.DirectoryCopy(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Mod Models"), "Content/Mod Models", true);
			}
			if (Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Video")))
			{
				Ship_Game.ResourceManager.DirectoryCopy(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Video"), "Content/ModVideo", true);
			}
            //Added by McShooterz
            Ship_Game.ResourceManager.LoadHostileFleets();
            Ship_Game.ResourceManager.LoadShipNames();
            Ship_Game.ResourceManager.LoadAgentMissions();
            Ship_Game.ResourceManager.LoadMainMenuShipList();
            Ship_Game.ResourceManager.LoadSoundEffects();
            Ship_Game.ResourceManager.LoadShipRoles();
            Ship_Game.ResourceManager.LoadHullBonuses();
            Ship_Game.ResourceManager.LoadPlanetEdicts();
            Localizer.cleanLocalizer();
            ResourceManager.OffSet = 0;
		}

		private static void LoadNebulas()
		{
			FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/Nebulas");
			for (int i = 0; i < (int)filesFromDirectory.Length; i++)
			{
				FileInfo FI = filesFromDirectory[i];
				if (Path.GetFileNameWithoutExtension(FI.Name) != "Thumbs")
				{
					Texture2D tex = GetContentManager().Load<Texture2D>(string.Concat("Nebulas/", Path.GetFileNameWithoutExtension(FI.Name)));
					if (tex.Width == 2048)
					{
						Ship_Game.ResourceManager.BigNebulas.Add(tex);
					}
					else if (tex.Width != 1024)
					{
						Ship_Game.ResourceManager.SmallNebulas.Add(tex);
					}
					else
					{
						Ship_Game.ResourceManager.MedNebulas.Add(tex);
					}
				}
			}
		}

		private static void LoadProjectileMeshes()
		{
			var Content = GetContentManager();
			try
			{
			Model projLong = GetContentManager().Load<Model>("Model/Projectiles/projLong");
			ModelMesh projMesh = GetContentManager().Load<Model>("Model/Projectiles/projLong").Meshes[0];
			Ship_Game.ResourceManager.ProjectileMeshDict["projLong"] = projMesh;
			Ship_Game.ResourceManager.ProjectileModelDict["projLong"] = projLong;
			Model projTear = GetContentManager().Load<Model>("Model/Projectiles/projTear");
			projMesh = projTear.Meshes[0];
			Ship_Game.ResourceManager.ProjectileMeshDict["projTear"] = projMesh;
			Ship_Game.ResourceManager.ProjectileModelDict["projTear"] = projTear;
			Model projBall = GetContentManager().Load<Model>("Model/Projectiles/projBall");
			projMesh = projBall.Meshes[0];
			Ship_Game.ResourceManager.ProjectileMeshDict["projBall"] = projMesh;
			Ship_Game.ResourceManager.ProjectileModelDict["projBall"] = projBall;
			Model torpedo = GetContentManager().Load<Model>("Model/Projectiles/torpedo");
			projMesh = torpedo.Meshes[0];
			Ship_Game.ResourceManager.ProjectileMeshDict["torpedo"] = projMesh;
			Ship_Game.ResourceManager.ProjectileModelDict["torpedo"] = torpedo;
			Model missile = GetContentManager().Load<Model>("Model/Projectiles/missile");
			projMesh = missile.Meshes[0];
			Ship_Game.ResourceManager.ProjectileMeshDict["missile"] = projMesh;
			Ship_Game.ResourceManager.ProjectileModelDict["missile"] = missile;
			Model drone = GetContentManager().Load<Model>("Model/Projectiles/spacemine");
			projMesh = drone.Meshes[0];
			Ship_Game.ResourceManager.ProjectileMeshDict["spacemine"] = projMesh;
			Ship_Game.ResourceManager.ProjectileModelDict["spacemine"] = missile;
			}
			catch (Exception) { }

             
            //Added by McShooterz: failed attempt at loading projectile models
            //modified by gremlin
            /*
            if (Ship_Game.ResourceManager.WhichModPath != "Content")
            {
                FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Model/Projectiles"));
                for (int i = 0; i < (int)filesFromDirectory.Length; i++)
                {
                    string name = Path.GetFileNameWithoutExtension(filesFromDirectory[i].Name);
                    if (name != "Thumbs")
                    {
                        Model projModel = GetContentManager().Load<Model>(string.Concat("../", Ship_Game.ResourceManager.WhichModPath, "/Model/Projectiles/", name));
                        //try
                        //{
                        //    Model projModel = GetContentManager().Load<Model>(string.Concat("../", Ship_Game.ResourceManager.WhichModPath, "/Model/Projectiles/", name));
                        //}
                        
                        //catch
                        //{

                        //}
                        ModelMesh projMesh2 = projModel.Meshes[0];
                        Ship_Game.ResourceManager.ProjectileMeshDict[name] = projMesh2;
                        Ship_Game.ResourceManager.ProjectileModelDict[name] = projModel;
                    }
                }
            }*/
		}

        //Added by McShooterz: Load projectile models for mods
        private static void LoadModsProjectileMeshes()
        {
            FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectoryNoSub(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Model/Projectiles"));
            for (int i = 0; i < (int)filesFromDirectory.Length; i++)
            {
                string name = Path.GetFileNameWithoutExtension(filesFromDirectory[i].Name);
                if (name != "Thumbs" && (filesFromDirectory[i].GetType() ==  typeof(Model)))
                {
                    Model projModel = GetContentManager().Load<Model>(string.Concat("../", Ship_Game.ResourceManager.WhichModPath, "/Model/Projectiles/", name));
                    ModelMesh projMesh2 = projModel.Meshes[0];
                    Ship_Game.ResourceManager.ProjectileMeshDict[name] = projMesh2;
                    Ship_Game.ResourceManager.ProjectileModelDict[name] = projModel;
                }
            }
        }

      


		private static void LoadProjTexts()
		{

            ////Added by McShooterz: mod folder support /Model/Projectiles/textures
            //FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat("/Model/Projectiles/textures"));
            //for (int i = 0; i < (int)filesFromDirectory.Length; i++)
            //{
            //    string name = Path.GetFileNameWithoutExtension(filesFromDirectory[i].Name);
            //    if (name != "Thumbs")
            //    {
            //        Texture2D tex = GetContentManager().Load<Texture2D>(string.Concat("/Model/Projectiles/textures/", name));
            //        Ship_Game.ResourceManager.ProjTextDict[name] = tex;
            //    }
            //}
            //Added by McShooterz: mod folder support /Model/Projectiles/textures
            
            {
                FileInfo[] filesFrommodDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Model/Projectiles/textures"));
                for (int i = 0; i < (int)filesFrommodDirectory.Length; i++)
                {
                    string name = Path.GetFileNameWithoutExtension(filesFrommodDirectory[i].Name);
                    if (name != "Thumbs")
                    {
                        Texture2D tex = GetContentManager().Load<Texture2D>(string.Concat("../",Ship_Game.ResourceManager.WhichModPath, "/Model/Projectiles/textures/", name));
                        Ship_Game.ResourceManager.ProjTextDict[name] = tex;
                    }
                }
            }

		}


		private static void LoadRandomItems()
		{
			Ship_Game.ResourceManager.RandomItemsList.Clear();
            //Added by McShooterz: mod folder support RandomStuff
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/RandomStuff")) ? string.Concat(Ship_Game.ResourceManager.WhichModPath, "/RandomStuff") : "Content/RandomStuff");
			XmlSerializer serializer1 = new XmlSerializer(typeof(RandomItem));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
                RandomItem data = null;
                try
                {
                     data = (RandomItem)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
					ReportLoadingError(fileInfoArray[i], "LoadRandomItems", e);
                }
				if (data == null)
					continue;
                //stream.Close();
				stream.Dispose();
				Ship_Game.ResourceManager.RandomItemsList.Add(data);
			}
		}

		private static void LoadRoids()
		{
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/Model/Asteroids");
			int i = 1;
			FileInfo[] fileInfoArray = textList;
			for (int num = 0; num < (int)fileInfoArray.Length; num++)
			{
				FileInfo FI = fileInfoArray[num];
				Ship_Game.ResourceManager.RoidsModels[i] = GetContentManager().Load<Model>(string.Concat("Model/Asteroids/", Path.GetFileNameWithoutExtension(FI.Name)));
				i++;
			}
		}

		private static void LoadShipModules()
		{
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/ShipModules"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(ShipModule_Deserialize));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				
                FileInfo FI = fileInfoArray[i];
                //added by gremlin support techlevel disabled folder.
                if(FI.DirectoryName.IndexOf("disabled", StringComparison.OrdinalIgnoreCase)  >0)
                    continue;
				FileStream stream = FI.OpenRead();
                ShipModule_Deserialize data = null;
                try
                {
                     data = (ShipModule_Deserialize)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
                    ReportLoadingError(fileInfoArray[i], "LoadShipModules", e); 
                }
				//stream.Close();
				stream.Dispose();
				if (data == null)
					continue;
                if (Localizer.LocalizerDict.ContainsKey(data.DescriptionIndex + OffSet))
                {
                    data.DescriptionIndex += (ushort)OffSet;
                    Localizer.used[data.DescriptionIndex]=true;
                }
                if (Localizer.LocalizerDict.ContainsKey(data.NameIndex + OffSet))
                {
                    data.NameIndex += (ushort)OffSet;
                    Localizer.used[data.NameIndex] = true;
                }
                
                //if ( data.hangerTimerConstant >0 )
                //    data.hangarTimerConstant = data.hangerTimerConstant;                                    
                data.UID = String.Intern( Path.GetFileNameWithoutExtension(FI.Name));
                if (data.IconTexturePath != null && String.IsInterned(data.IconTexturePath) != null)
                    string.Intern(data.IconTexturePath);
                if (!string.IsNullOrEmpty(data.WeaponType) && string.IsNullOrEmpty(String.IsInterned(data.WeaponType)))
                    string.Intern(data.WeaponType);
                if(data.IsCommandModule  && data.TargetTracking ==0)
                {
                    data.TargetTracking = Convert.ToSByte((data.XSIZE * data.YSIZE) / 3);
                }
                if (Ship_Game.ResourceManager.ShipModulesDict.ContainsKey(data.UID))
				{
                    Ship_Game.ResourceManager.ShipModulesDict[data.UID] = data.ConvertToShipModule();
                    System.Diagnostics.Debug.WriteLine("ShipModule UID already found. Conflicting name:  " + data.UID);
                }
				else
				{
                    Ship_Game.ResourceManager.ShipModulesDict.Add(data.UID, data.ConvertToShipModule());
                }
                
			}
			foreach (KeyValuePair<string, ShipModule> entry in Ship_Game.ResourceManager.ShipModulesDict)
			{
				entry.Value.SetAttributesNoParent();
			}
			textList = null;
		}

		public static void LoadShips()
		{
            Ship_Game.ResourceManager.ShipsDict.Clear();// = new Dictionary<string, Ship>();
            //Added by McShooterz: Changed how StarterShips loads from mod if folder exists
            XmlSerializer serializer0 = new XmlSerializer(typeof(ShipData));
            FileInfo[] textList; //"Mods/", 
            
            if (GlobalStats.ActiveMod != null && Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/StarterShips")))
            {
                Ship_Game.ResourceManager.ShipsDict.Clear();
                textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/StarterShips/"));
            }
            else
            {
                textList = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/StarterShips");           
            }

            FileInfo[] fileInfoArray = textList;
            for (int i = 0; i < (int)fileInfoArray.Length; i++)
            {
                //added by gremlin support techlevel disabled folder.
                if (fileInfoArray[i].DirectoryName.IndexOf("disabled", StringComparison.OrdinalIgnoreCase) > 0)
                    continue;
                FileStream stream = fileInfoArray[i].OpenRead();

                ShipData newShipData = (ShipData)serializer0.Deserialize(stream);
                //stream.Close();
                stream.Dispose();
                Ship newShip = Ship.CreateShipFromShipData(newShipData);
                if (newShipData.Role != ShipData.RoleName.disabled)
                {
                    newShip.SetShipData(newShipData);
                    newShip.reserved = true;
                    if (newShip.InitForLoad())
                    {
                        newShip.InitializeStatus();
                        if (!string.IsNullOrEmpty(newShipData.EventOnDeath) && string.IsNullOrEmpty(string.IsInterned(newShipData.EventOnDeath)))
                            string.Intern(newShipData.EventOnDeath);
                        if (!string.IsNullOrEmpty(newShipData.ModelPath) && string.IsNullOrEmpty(string.IsInterned(newShipData.ModelPath)))
                            string.Intern(newShipData.ModelPath);
                        if (!string.IsNullOrEmpty(newShipData.ShipStyle) && string.IsNullOrEmpty(string.IsInterned(newShipData.ShipStyle)))
                            string.Intern(newShipData.ShipStyle);
                        if (!string.IsNullOrEmpty(newShipData.Name) && string.IsNullOrEmpty(string.IsInterned(newShipData.Name)))
                            string.Intern(newShipData.Name);
                        if (!string.IsNullOrEmpty(newShipData.IconPath) )
                            string.Intern(newShipData.IconPath);
                        if (!string.IsNullOrEmpty(newShipData.Hull))
                            string.Intern(newShipData.Hull);
                        if (!string.IsNullOrEmpty(newShipData.SelectionGraphic))
                            string.Intern(newShipData.SelectionGraphic);
                        Ship_Game.ResourceManager.ShipsDict[newShipData.Name] = newShip;
                    }
                }
            }
			FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/SavedDesigns");
			for (int j = 0; j < (int)filesFromDirectory.Length; j++)
			{
				FileStream stream = filesFromDirectory[j].OpenRead();
                ShipData newShipData = null;
                try
                {
                     newShipData = (ShipData)serializer0.Deserialize(stream);
                }
                catch (Exception e)
                {
                    ReportLoadingError(fileInfoArray[j], "LoadShips", e);
                }
				//stream.Close();
				stream.Dispose();
				Ship newShip = Ship.CreateShipFromShipData(newShipData);
                if (newShipData.Role != ShipData.RoleName.disabled)
                {
                    newShip.SetShipData(newShipData);
                    newShip.reserved = true;
                    if (newShip.InitForLoad())
                    {
                        newShip.InitializeStatus();
                        Ship_Game.ResourceManager.ShipsDict[String.Intern(newShipData.Name)] = newShip;
                    }
                    if (!string.IsNullOrEmpty(newShipData.EventOnDeath) && string.IsNullOrEmpty(string.IsInterned(newShipData.EventOnDeath)))
                        string.Intern(newShipData.EventOnDeath);
                    if (!string.IsNullOrEmpty(newShipData.ModelPath) && string.IsNullOrEmpty(string.IsInterned(newShipData.ModelPath)))
                        string.Intern(newShipData.ModelPath);
                    if (!string.IsNullOrEmpty(newShipData.ShipStyle) && string.IsNullOrEmpty(string.IsInterned(newShipData.ShipStyle)))
                        string.Intern(newShipData.ShipStyle);
                    if (!string.IsNullOrEmpty(newShipData.Name) && string.IsNullOrEmpty(string.IsInterned(newShipData.Name)))
                        string.Intern(newShipData.Name);   
                                            if (!string.IsNullOrEmpty(newShipData.IconPath) )
                            string.Intern(newShipData.IconPath);
                        if (!string.IsNullOrEmpty(newShipData.Hull))
                            string.Intern(newShipData.Hull);
                        if (!string.IsNullOrEmpty(newShipData.SelectionGraphic))
                            string.Intern(newShipData.SelectionGraphic);

                }
			}
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            FileInfo[] filesFromDirectory1 = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(path, "/StarDrive/Saved Designs"));
			for (int k = 0; k < (int)filesFromDirectory1.Length; k++)
			{
				FileInfo FI = filesFromDirectory1[k];
                if (String.Compare(FI.Extension, ".XML",StringComparison.OrdinalIgnoreCase)!=0)
                {
                    continue;
                }
				
#if !DEBUG
                try
#endif
				{
					FileStream stream = FI.OpenRead();
                    ShipData newShipData = null;
                    try
                    {
                         newShipData = (ShipData)serializer0.Deserialize(stream);
                    }
                    catch (Exception e)
                    {
                    	ReportLoadingError(FI, "LoadShips", e);
                    }
					//stream.Close();
					stream.Dispose();
					Ship newShip = Ship.CreateShipFromShipData(newShipData);
                    if (newShipData.Role != ShipData.RoleName.disabled)
                    {
                        newShip.IsPlayerDesign = true;
                        newShip.SetShipData(newShipData);
                        if (newShip.InitForLoad())
                        {
                            newShip.InitializeStatus();
                            Ship_Game.ResourceManager.ShipsDict[String.Intern(newShipData.Name)] = newShip;
                        }
                        if (!string.IsNullOrEmpty(newShipData.EventOnDeath) && string.IsNullOrEmpty(string.IsInterned(newShipData.EventOnDeath)))
                            string.Intern(newShipData.EventOnDeath);
                        if (!string.IsNullOrEmpty(newShipData.ModelPath) && string.IsNullOrEmpty(string.IsInterned(newShipData.ModelPath)))
                            string.Intern(newShipData.ModelPath);
                        if (!string.IsNullOrEmpty(newShipData.ShipStyle) && string.IsNullOrEmpty(string.IsInterned(newShipData.ShipStyle)))
                            string.Intern(newShipData.ShipStyle);
                        if (!string.IsNullOrEmpty(newShipData.Name) && string.IsNullOrEmpty(string.IsInterned(newShipData.Name)))
                            string.Intern(newShipData.Name); 
                                                if (!string.IsNullOrEmpty(newShipData.IconPath) )
                            string.Intern(newShipData.IconPath);
                        if (!string.IsNullOrEmpty(newShipData.Hull))
                            string.Intern(newShipData.Hull);
                        if (!string.IsNullOrEmpty(newShipData.SelectionGraphic))
                            string.Intern(newShipData.SelectionGraphic);

                    }
                    
				}
#if !DEBUG
				catch
				{
				}
#endif
			}
			if (GlobalStats.ActiveMod != null)
			{
				textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat("Mods/", GlobalStats.ActiveMod.ModPath, "/ShipDesigns/"));
				FileInfo[] fileInfoArray1 = textList;
				for (int l = 0; l < (int)fileInfoArray1.Length; l++)
				{
					FileInfo FI = fileInfoArray1[l];
					
                //try

					{
						FileStream stream = FI.OpenRead();
						ShipData newShipData = (ShipData)serializer0.Deserialize(stream);
						//stream.Close();
						stream.Dispose();
						Ship newShip = Ship.CreateShipFromShipData(newShipData);
                        if (newShipData.Role != ShipData.RoleName.disabled)
                        {
                            newShip.IsPlayerDesign = true;
                            newShip.SetShipData(newShipData);
                            newShip.reserved = true;
                            if (newShip.InitForLoad())
                            {
                                newShip.InitializeStatus();
                                Ship_Game.ResourceManager.ShipsDict[String.Intern(newShipData.Name)] = newShip;
                            }
                            if (!string.IsNullOrEmpty(newShipData.EventOnDeath) && string.IsNullOrEmpty(string.IsInterned(newShipData.EventOnDeath)))
                                string.Intern(newShipData.EventOnDeath);
                            if (!string.IsNullOrEmpty(newShipData.ModelPath) && string.IsNullOrEmpty(string.IsInterned(newShipData.ModelPath)))
                                string.Intern(newShipData.ModelPath);
                            if (!string.IsNullOrEmpty(newShipData.ShipStyle) && string.IsNullOrEmpty(string.IsInterned(newShipData.ShipStyle)))
                                string.Intern(newShipData.ShipStyle);  
                            if (!string.IsNullOrEmpty(newShipData.Name) && string.IsNullOrEmpty(string.IsInterned(newShipData.Name)))
                            string.Intern(newShipData.Name); 
                                                    if (!string.IsNullOrEmpty(newShipData.IconPath) )
                            string.Intern(newShipData.IconPath);
                        if (!string.IsNullOrEmpty(newShipData.Hull))
                            string.Intern(newShipData.Hull);
                        if (!string.IsNullOrEmpty(newShipData.SelectionGraphic))
                            string.Intern(newShipData.SelectionGraphic);

                        }
					}
      
			
                //catch
                //    {
                //    }  
	
				}
			}
            /*
            //fbedard: Create a copy of Unarmed Scout for Assault Ship
            Ship scout = null;
            Ship_Game.ResourceManager.ShipsDict.TryGetValue("Unarmed Scout", out scout);
            ShipData newScoutData = scout.shipData.GetClone();
            newScoutData.Name = "Assault_Ship";
            newScoutData.Role = ShipData.RoleName.troop;
            newScoutData.ShipCategory = ShipData.Category.Unclassified;
            Ship newscout = Ship.CreateShipFromShipData(newScoutData);
            newscout.IsPlayerDesign = false;
            newscout.SetShipData(newScoutData);
            newscout.Name = "Assault_Ship";
            newscout.VanityName = "Assault Ship";
            newscout.InitForLoad();
            newscout.InitializeStatus();
            Ship_Game.ResourceManager.ShipsDict[String.Intern("Assault_Ship")] = newscout;
            */
            #region old strength calculator
            //foreach (KeyValuePair<string, Ship> entry in Ship_Game.ResourceManager.ShipsDict)
            //{
            //    foreach (ModuleSlot slot in entry.Value.ModuleSlotList)
            //    {
            //        if (Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].isWeapon)
            //        {
            //            if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WeaponType].explodes)
            //            {
            //                Ship value = entry.Value;
            //                value.BaseStrength = value.BaseStrength + Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WeaponType].DamageAmount * (1f / Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WeaponType].fireDelay) * 0.75f;
            //            }
            //            else if (!Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WeaponType].isBeam)
            //            {
            //                Ship baseStrength = entry.Value;
            //                baseStrength.BaseStrength = baseStrength.BaseStrength + Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WeaponType].DamageAmount * (1f / Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WeaponType].fireDelay);
            //            }
            //            else
            //            {
            //                Ship ship = entry.Value;
            //                ship.BaseStrength = ship.BaseStrength + Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WeaponType].DamageAmount * 180f;
            //            }
            //        }
            //        if (Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WarpThrust <= 0)
            //        {
            //            continue;
            //        }
            //        entry.Value.BaseCanWarp = true;
            //    }
            //    Ship value1 = entry.Value;
            //    value1.BaseStrength = value1.BaseStrength / (float)entry.Value.ModuleSlotList.Count;
            //} 
            #endregion

            //added by gremlin : Base strength Calculator
            foreach (KeyValuePair<string, Ship> entry in ResourceManager.ShipsDict)
            {
                //if (entry.Value.BaseStrength != 0)
                //    continue;

                float Str = 0f;
                float def = 0f;
                int slotCount = entry.Value.Size;



                bool fighters = false;
                bool weapons = false;

                foreach (ModuleSlot slot in entry.Value.ModuleSlotList.Where(dummy => dummy.InstalledModuleUID != "Dummy"))
                {

                    ShipModule module = ResourceManager.ShipModulesDict[slot.InstalledModuleUID];
                    float offRate = 0;
                    if (module.InstalledWeapon != null)
                    {
                        weapons = true;
                        Weapon w = module.InstalledWeapon;
                        if (!w.explodes)
                        {
                            offRate += (!w.isBeam ? (w.DamageAmount *w.SalvoCount) * (1f / w.fireDelay) : w.DamageAmount * 18f);
                        }
                        else
                        {
                            offRate += (w.DamageAmount * w.SalvoCount) * (1f / w.fireDelay) * 0.75f;

                        }
                        if (offRate > 0 && (w.TruePD || w.Range < 1000))
                        {
                            float range = 0f;
                            if (w.Range < 1000)
                            {
                                range = (1000f - w.Range) * .01f;
                            }
                            offRate /= (2 + range);
                        }
                        if (w.EMPDamage > 0) offRate += w.EMPDamage * (1f / w.fireDelay) * .2f;
                        Str += offRate;
                    }


                    if (module.hangarShipUID != null && !module.IsSupplyBay && !module.IsTroopBay)
                    {

                        fighters = true;
                        Ship hangarship;// = new Ship();
                        ResourceManager.ShipsDict.TryGetValue(module.hangarShipUID, out hangarship);

                        if (hangarship != null)
                        {
                            if (hangarship.BaseStrength == 0)
                            {
                                try
                                {
                                    CalculateBaseStrength(hangarship);
                                }
                                catch
                                {
                                    Str += 300;
                                }
                                Str += hangarship.BaseStrength;
                            }
                            else 
                                Str += hangarship.BaseStrength;
                            
                        }
                    }
                    def += module.shield_power_max * ((module.shield_radius * .05f) / slotCount);
                    //(module.shield_power_max+  module.shield_radius +module.shield_recharge_rate) / slotCount ;
                    def += module.HealthMax * ((module.ModuleType == ShipModuleType.Armor ? (module.XSIZE) : 1f) / (slotCount * 4));

                    if (ResourceManager.ShipModulesDict[module.UID].WarpThrust > 0)
                    {
                        entry.Value.BaseCanWarp = true;
                    }

                }
                if (!fighters && !weapons) Str = 0;
                if (def > Str) def = Str;
                entry.Value.shipData.BaseStrength = Str + def;
                entry.Value.BaseStrength = entry.Value.shipData.BaseStrength;
            }
		}

        public static float CalculateBaseStrength(Ship ship)
        {
            
            foreach (KeyValuePair<string, Ship> entry in ResourceManager.ShipsDict)
            {
                if (entry.Key != ship.Name )
                    continue;
                if (entry.Value.BaseStrength > 0)
                {
                    ship.BaseStrength = entry.Value.BaseStrength;
                    ship.BaseCanWarp = entry.Value.BaseCanWarp;
                    return entry.Value.BaseStrength;
                    
                }
                //KeyValuePair<string, Ship> entry = ResourceManager.ShipsDict[ship.DesignUID]

                float Str = 0f;
                float def = 0f;
                int slotCount = entry.Value.Size;



                bool fighters = false;
                bool weapons = false;

                foreach (ModuleSlot slot in entry.Value.ModuleSlotList.Where(dummy => dummy.InstalledModuleUID != "Dummy"))
                {

                    ShipModule module = ResourceManager.ShipModulesDict[slot.InstalledModuleUID];
                    float offRate = 0;
                    if (module.InstalledWeapon != null)
                    {
                        weapons = true;
                        Weapon w = module.InstalledWeapon;
                        if (!w.explodes)
                        {
                            offRate += (!w.isBeam ? (w.DamageAmount * w.SalvoCount) * (1f / w.fireDelay) : w.DamageAmount * 18f);
                        }
                        else
                        {
                            offRate += (w.DamageAmount * w.SalvoCount) * (1f / w.fireDelay) * 0.75f;

                        }
                        if (offRate > 0 && (w.TruePD || w.Range < 1000))
                        {
                            float range = 0f;
                            if (w.Range < 1000)
                            {
                                range = (1000f - w.Range) * .01f;
                            }
                            offRate /= (2 + range);
                        }
                        if (w.EMPDamage > 0) offRate += w.EMPDamage * (1f / w.fireDelay) * .2f;
                        Str += offRate;
                    }


                    if (module.hangarShipUID != null && !module.IsSupplyBay && !module.IsTroopBay)
                    {

                        fighters = true;
                        Ship hangarship;// = new Ship();
                        ResourceManager.ShipsDict.TryGetValue(module.hangarShipUID, out hangarship);

                        if (hangarship != null)
                        {
                            Str += 100;
                        }
                    }
                    def += module.shield_power_max * ((module.shield_radius * .05f) / slotCount);
                    //(module.shield_power_max+  module.shield_radius +module.shield_recharge_rate) / slotCount ;
                    def += module.HealthMax * ((module.ModuleType == ShipModuleType.Armor ? (module.XSIZE) : 1f) / (slotCount * 4));

                    if (ResourceManager.ShipModulesDict[module.UID].WarpThrust > 0)
                    {
                        entry.Value.BaseCanWarp = true;
                        ship.BaseCanWarp = entry.Value.BaseCanWarp;
                    }

                }
                if (!fighters && !weapons) Str = 0;
                if (def > Str) def = Str;
                entry.Value.shipData.BaseStrength = Str + def;
                entry.Value.BaseStrength = entry.Value.shipData.BaseStrength;
                ship.BaseStrength = entry.Value.shipData.BaseStrength;
                return ship.BaseStrength;
                



            }
            return 0;
        }

        public static float CalculateModuleStrength(ShipModule moduleslot, string OffDefBoth, int slotCount)
        {

            
                float Str = 0f;
                float def = 0f;
            //int slotCount = moduleslot.XSIZE*moduleslot.YSIZE;



            //bool fighters = false;          //Not referenced in code, removing to save memory -Gretman
            //bool weapons = false;          //Not referenced in code, removing to save memory -Gretman
            //ModuleSlot slot = moduleslot;
            //foreach (ModuleSlot slot in entry.Value.ModuleSlotList.Where(dummy => dummy.InstalledModuleUID != "Dummy"))
            {

                ShipModule module = moduleslot;
                    float offRate = 0;
                    if (module.InstalledWeapon != null)
                    {
                        //weapons = true;
                        Weapon w = module.InstalledWeapon;
                        if (!w.explodes)
                        {
                            offRate += (!w.isBeam ? (w.DamageAmount * w.SalvoCount) * (1f / w.fireDelay) : w.DamageAmount * 18f);
                        }
                        else
                        {
                            offRate += (w.DamageAmount * w.SalvoCount) * (1f / w.fireDelay) * 0.75f;

                        }
                        if (offRate > 0 && (w.TruePD || w.Range < 1000))
                        {
                            float range = 0f;
                            if (w.Range < 1000)
                            {
                                range = (1000f - w.Range) * .01f;
                            }
                            offRate /= (2 + range);
                        }
                        if (w.EMPDamage > 0) offRate += w.EMPDamage * (1f / w.fireDelay) * .2f;
                        Str += offRate;
                    }


                    if (module.hangarShipUID != null && !module.IsSupplyBay && !module.IsTroopBay)
                    {

                        //fighters = true;
                        Ship hangarship;// = new Ship();
                        ResourceManager.ShipsDict.TryGetValue(module.hangarShipUID, out hangarship);

                        if (hangarship != null)
                        {
                            Str+= hangarship.BaseStrength;
                            
                        }
                        else Str += 100;
                    }
                    if (slotCount > 0)
                    {
                        def += module.shield_power_max * ((module.shield_radius * .05f) / slotCount);
                        //(module.shield_power_max+  module.shield_radius +module.shield_recharge_rate) / slotCount ;
                        def += module.HealthMax * ((module.ModuleType == ShipModuleType.Armor ? (module.XSIZE) : 1f) / (slotCount * 4));
                    }

                }
                //if (!fighters && !weapons) Str = 0;
                //if (def > Str) def = Str;

            if(OffDefBoth == "Off")
                return Str;
            if (OffDefBoth == "Def")
                return def;
            return Str + def;

            }

		private static void LoadSmallStars()
		{
			FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/SmallStars");
			for (int i = 0; i < (int)filesFromDirectory.Length; i++)
			{
				FileInfo FI = filesFromDirectory[i];
				if (Path.GetFileNameWithoutExtension(FI.Name) != "Thumbs")
				{
					Texture2D tex = GetContentManager().Load<Texture2D>(string.Concat("SmallStars/", Path.GetFileNameWithoutExtension(FI.Name)));
					Ship_Game.ResourceManager.SmallStars.Add(tex);
				}
			}
		}

		public static void LoadSubsetEmpires()
		{
			//Ship_Game.ResourceManager.Empires.Clear();
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Races"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(EmpireData));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
                EmpireData data = null;
                try
                {
                     data = (EmpireData)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
					ReportLoadingError(fileInfoArray[i], "LoadSubsetEmpires", e);
                }
				//stream.Close();
				stream.Dispose();
				if (data.Faction == 1)
				{
					Ship_Game.ResourceManager.Empires.Add(data);
				}
			}
			textList = null;
		}

		private static void LoadTechTree()
		{
			if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.clearVanillaTechs)
                Ship_Game.ResourceManager.TechTree.Clear();
            FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Technology"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(Technology));
			FileInfo[] fileInfoArray = textList;
            for (int i = 0; i < (int)fileInfoArray.Length; i++)
            {
                FileInfo FI = fileInfoArray[i];
                FileStream stream = FI.OpenRead();
                Technology data = null;
                try
                {
                    data = (Technology)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
                    ReportLoadingError(FI, "LoadTechTree", e);
                }
                //stream.Close();
                stream.Dispose();
                if (Localizer.LocalizerDict.ContainsKey(data.DescriptionIndex + OffSet))
                {
                    data.DescriptionIndex += OffSet;
                    Localizer.used[data.DescriptionIndex] = true;
                }
                if (Localizer.LocalizerDict.ContainsKey(data.NameIndex + OffSet))
                {
                    data.NameIndex += OffSet;
                    Localizer.used[data.NameIndex] = true;
                }
                foreach (Technology.UnlockedBonus bonus in data.BonusUnlocked)
                {
                    if (Localizer.LocalizerDict.ContainsKey(bonus.BonusIndex + OffSet))
                    {
                        bonus.BonusIndex += OffSet;
                        Localizer.used[bonus.BonusIndex] = true;
                    }
                    if (Localizer.LocalizerDict.ContainsKey(bonus.BonusNameIndex + OffSet))
                    {
                        bonus.BonusNameIndex += OffSet;
                        Localizer.used[bonus.BonusNameIndex] = true;
                    }
                }
                data.UID = Path.GetFileNameWithoutExtension(FI.Name);
                data.UID = string.IsInterned(data.UID);
                if (data.UID == null)
                {
                    data.UID = string.Intern(Path.GetFileNameWithoutExtension(FI.Name));
                }


                if (Ship_Game.ResourceManager.TechTree.ContainsKey(Path.GetFileNameWithoutExtension(FI.Name)))
                {

                    Ship_Game.ResourceManager.TechTree[Path.GetFileNameWithoutExtension(FI.Name)] = data;

                }
                else
                {

                    //Ship_Game.ResourceManager.TechTree.Add(Path.GetFileNameWithoutExtension(FI.Name), data);
                    Ship_Game.ResourceManager.TechTree.Add(data.UID, data);
                }
                //catagorize uncatagoried techs
                {
                    if (data.TechnologyType == TechnologyType.General)
                    {
                        if (data.BuildingsUnlocked.Count > 0)
                        {
                            foreach (Technology.UnlockedBuilding buildingU in data.BuildingsUnlocked)
                            {
                                Building building;
                                if (ResourceManager.BuildingsDict.TryGetValue(buildingU.Name, out building))
                                {
                                    if (building.AllowInfantry || building.PlanetaryShieldStrengthAdded > 0 || building.CombatStrength > 0
                                        || building.isWeapon || building.Strength > 0 || building.IsSensor)
                                        data.TechnologyType = TechnologyType.GroundCombat;
                                    else if (building.AllowShipBuilding || building.PlusFlatProductionAmount > 0 || building.PlusProdPerRichness > 0
                                        || building.StorageAdded > 0 || building.PlusFlatProductionAmount > 0)
                                        data.TechnologyType = TechnologyType.Industry;
                                    else if (building.PlusTaxPercentage > 0 || building.CreditsPerColonist > 0)
                                        data.TechnologyType = TechnologyType.Economic;
                                    else if (building.PlusFlatResearchAmount > 0 || building.PlusResearchPerColonist > 0)
                                        data.TechnologyType = TechnologyType.Research;
                                    else if (building.PlusFoodPerColonist > 0 || building.PlusFlatFoodAmount > 0 || building.PlusFoodPerColonist > 0
                                        || building.MaxPopIncrease > 0 || building.PlusFlatPopulation > 0 || building.Name == "Biosspheres"
                                        || building.PlusTerraformPoints > 0
                                        )
                                        data.TechnologyType = TechnologyType.Colonization;
                                }

                            }


                        }
                        else if (data.TroopsUnlocked.Count > 0)
                        {
                            data.TechnologyType = TechnologyType.GroundCombat;
                        }
                        else if (data.TechnologyType == TechnologyType.General && data.BonusUnlocked.Count > 0)
                        {
                            foreach (Technology.UnlockedBonus bonus in data.BonusUnlocked)
                            {
                                if (bonus.Type == "SHIPMODULE" || bonus.Type == "HULL")
                                    data.TechnologyType = TechnologyType.ShipGeneral;
                                else if (bonus.Type == "TROOP")
                                    data.TechnologyType = TechnologyType.GroundCombat;
                                else if (bonus.Type == "BUILDING")
                                    data.TechnologyType = TechnologyType.Colonization;
                                else if (bonus.Type == "ADVANCE")
                                    data.TechnologyType = TechnologyType.ShipGeneral;
                            }
                        }
                        else if (data.ModulesUnlocked.Count > 0)
                        {
                            foreach (Technology.UnlockedMod moduleU in data.ModulesUnlocked)
                            {
                                ShipModule module;
                                if (ResourceManager.ShipModulesDict.TryGetValue(moduleU.ModuleUID, out module))
                                {
                                    if (module.InstalledWeapon != null || module.MaximumHangarShipSize > 0
                                        || module.ModuleType == ShipModuleType.Hangar
                                        )
                                        data.TechnologyType = TechnologyType.ShipWeapons;
                                    else if (module.shield_power > 0 || module.ModuleType == ShipModuleType.Armor
                                        || module.ModuleType == ShipModuleType.Countermeasure
                                        || module.ModuleType == ShipModuleType.Shield

                                        )
                                        data.TechnologyType = TechnologyType.ShipDefense;


                                    else
                                        data.TechnologyType = TechnologyType.ShipGeneral;
                                    
                                }
                            }
                        }

                        else data.TechnologyType = TechnologyType.General;

                        if (data.HullsUnlocked.Count > 0)
                        {
                            data.TechnologyType = TechnologyType.ShipHull;
                            foreach (Technology.UnlockedHull hull in data.HullsUnlocked)
                            {
                                ShipData.RoleName role = ResourceManager.HullsDict[hull.Name].Role;
                                if (role == ShipData.RoleName.freighter || role == ShipData.RoleName.platform
                                    || role == ShipData.RoleName.construction || role == ShipData.RoleName.station)
                                    data.TechnologyType = TechnologyType.Industry;
                            }

                        }


                    }
                }

            }
			textList = null;
		}

		private static void LoadTextures()
		{
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Textures"));
			if (Ship_Game.ResourceManager.WhichModPath != "Content")
			{
				FileInfo[] fileInfoArray = textList;
				for (int i = 0; i < (int)fileInfoArray.Length; i++)
				{
					FileInfo FI = fileInfoArray[i];
					if (FI.Directory.Name != "Textures")
					{
						string name = Path.GetFileNameWithoutExtension(FI.Name);
                        if(string.IsInterned(name) == null)
                        {
                            string.Intern(name);
                        }
                        if (string.IsInterned(name) == null)
                        {
                            string.Intern(name);
                        }

						if (name != "Thumbs")
						{
							ContentManager content = GetContentManager();
							string[] whichModPath = new string[] { "../", Ship_Game.ResourceManager.WhichModPath, "/Textures/", FI.Directory.Name, "/", name };
							Texture2D tex = content.Load<Texture2D>(string.Concat(whichModPath));
							Ship_Game.ResourceManager.TextureDict[string.Intern(string.Concat(FI.Directory.Name, "/", name))] = tex;
						}
					}
					else if (Path.GetFileNameWithoutExtension(FI.Name) != "Thumbs")
					{
						Texture2D tex = GetContentManager().Load<Texture2D>(string.Concat("../", Ship_Game.ResourceManager.WhichModPath, "/Textures/", Path.GetFileNameWithoutExtension(FI.Name)));
						Ship_Game.ResourceManager.TextureDict[string.Intern(string.Concat(FI.Directory.Name, "/", Path.GetFileNameWithoutExtension(FI.Name)))] = tex;
					}
				}
				return;
			}
			FileInfo[] fileInfoArray1 = textList;
			for (int j = 0; j < (int)fileInfoArray1.Length; j++)
			{
				FileInfo FI = fileInfoArray1[j];
				if (FI.Directory.Name == "Textures")
				{
					string name = string.Intern(Path.GetFileNameWithoutExtension(FI.Name));
					if (name != "Thumbs")
					{
						Texture2D tex = GetContentManager().Load<Texture2D>(string.Concat("Textures/", Path.GetFileNameWithoutExtension(FI.Name)));
						if (!Ship_Game.ResourceManager.TextureDict.ContainsKey(string.Concat(FI.Directory.Name, "/", name)))
						{
							Ship_Game.ResourceManager.TextureDict[string.Intern(string.Concat(FI.Directory.Name, "/", Path.GetFileNameWithoutExtension(FI.Name)))] = tex;
						}
					}
				}
				else
				{
					string name = string.Intern(Path.GetFileNameWithoutExtension(FI.Name));
					if (name != "Thumbs")
					{
						Texture2D tex = GetContentManager().Load<Texture2D>(string.Concat("Textures/", FI.Directory.Name, "/", name));
						if (!Ship_Game.ResourceManager.TextureDict.ContainsKey(string.Concat(FI.Directory.Name, "/", name)))
						{
							Ship_Game.ResourceManager.TextureDict[string.Intern(string.Concat(FI.Directory.Name, "/", name))] = tex;
						}
					}
				}
			}
		}

		private static void LoadToolTips()
		{
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Tooltips"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(Tooltips));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
                Tooltips data = null;
                try
                {
                    data = (Tooltips)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
					ReportLoadingError(fileInfoArray[i], "LoadToolTips", e);
                }
				//stream.Close();
				stream.Dispose();
				foreach (ToolTip tip in data.ToolTipsList)
				{
					
                    if (Ship_Game.ResourceManager.ToolTips.ContainsKey(tip.TIP_ID))
					{
						
                        Ship_Game.ResourceManager.ToolTips[tip.TIP_ID] = tip;
					}
					else
					{
						Ship_Game.ResourceManager.ToolTips.Add(tip.TIP_ID, tip);
					}
				}
			}
		}

		private static void LoadTroops()
		{
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Troops"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(Troop));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileInfo FI = fileInfoArray[i];
				FileStream stream = FI.OpenRead();
                Troop data = null;
                try
                {
                    data = (Troop)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
					ReportLoadingError(FI, "LoadTroops", e);
                }
				//stream.Close();
				stream.Dispose();
				//no localization
                data.Name = String.Intern(Path.GetFileNameWithoutExtension(FI.Name));
                if (Ship_Game.ResourceManager.TroopsDict.ContainsKey(Path.GetFileNameWithoutExtension(FI.Name)))
				{
					Ship_Game.ResourceManager.TroopsDict[Path.GetFileNameWithoutExtension(FI.Name)] = data;
				}
				else
				{
					Ship_Game.ResourceManager.TroopsDict.Add(Path.GetFileNameWithoutExtension(FI.Name), data);
				}
                
                Troop troop = Ship_Game.ResourceManager.TroopsDict[Path.GetFileNameWithoutExtension(FI.Name)];
                if(troop.StrengthMax <= 0)
                {
                    troop.StrengthMax = troop.Strength;
                }
                //if (troop.attack_path !=null)
                //string.Intern(troop.attack_path);
                //if (!string.IsNullOrEmpty(troop.Class))
                //string.Intern(troop.Class);
                //if (!string.IsNullOrEmpty(troop.Description))
                //string.Intern(troop.Description);
                //if (!string.IsNullOrEmpty(troop.Icon))
                //string.Intern(troop.Icon);
                //if (!string.IsNullOrEmpty(troop.idle_path))
                //string.Intern(troop.idle_path);
                //if (!string.IsNullOrEmpty(troop.MovementCue))
                //string.Intern(troop.MovementCue);
                //if (troop.OwnerString !=null)
                //string.Intern(troop.OwnerString);
                //if (!string.IsNullOrEmpty(troop.RaceType))
                //string.Intern(troop.RaceType);
                //if (!string.IsNullOrEmpty(troop.sound_attack))
                //string.Intern(troop.sound_attack);
                //if (string.IsNullOrEmpty(troop.TexturePath))
                //string.Intern(troop.TexturePath);
                //if (string.IsNullOrEmpty(troop.TargetType))
                //string.Intern(troop.TargetType);                
			}
		}

		private static void LoadWeapons()
		{
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Weapons"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(Weapon));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileInfo FI = fileInfoArray[i];
				FileStream stream = FI.OpenRead();
                Weapon data = null;
                try
                {
                    data = (Weapon)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
					ReportLoadingError(FI, "LoadWeapons", e);
                }
				//stream.Close();
				stream.Dispose();
                //no localization
                data.UID = String.Intern(Path.GetFileNameWithoutExtension(FI.Name));
                //if (data.AnimationPath != null)
                //String.Intern(data.AnimationPath);
                //if (data.BeamTexture !=null)
                //String.Intern(data.BeamTexture);
                //if (data.dieCue !=null)
                //String.Intern(data.dieCue);
                //if (data.SecondaryFire != null)
                //String.Intern(data.SecondaryFire);
				if (Ship_Game.ResourceManager.WeaponsDict.ContainsKey(Path.GetFileNameWithoutExtension(FI.Name)))
				{
					Ship_Game.ResourceManager.WeaponsDict[Path.GetFileNameWithoutExtension(FI.Name)] = data;
				}
				else
				{
					Ship_Game.ResourceManager.WeaponsDict.Add(Path.GetFileNameWithoutExtension(FI.Name), data);
				}
                
                
			}
			textList = null;
		}

        //Added by McShooterz: Load ship roles
        private static void LoadShipRoles()
        {
            FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/ShipRoles"));
            XmlSerializer serializer1 = new XmlSerializer(typeof(ShipRole));
            FileInfo[] fileInfoArray = textList;
            ShipData.RoleName key = new ShipData.RoleName();

            for (int i = 0; i < (int)fileInfoArray.Length; i++)
            {
                FileInfo FI = fileInfoArray[i];
                FileStream stream = FI.OpenRead();
                ShipRole data = null;
                try
                {
                    data = (ShipRole)serializer1.Deserialize(stream);
                }
                catch (Exception e)
                {
                    ReportLoadingError(FI, "LoadShipRoles", e);
                }
                //stream.Close();
                stream.Dispose();
                if (Localizer.LocalizerDict.ContainsKey(data.Localization + ResourceManager.OffSet))
                {
                    data.Localization += ResourceManager.OffSet;
                    Localizer.used[data.Localization] = true;
                }
                switch (data.Name)  //fbedard: translate string into enum
                {
                    case "platform":
				    {
					    key = ShipData.RoleName.platform;
					    break;
				    }
                    case "station":
                    {
                        key = ShipData.RoleName.station;
                        break;
                    }
                    case "construction":
                    {
                        key = ShipData.RoleName.construction;
                        break;
                    }
                    case "supply":
                    {
                        key = ShipData.RoleName.supply;
                        break;
                    }
                    case "freighter":
                    {
                        key = ShipData.RoleName.freighter;
                        break;
                    }
                    case "troop":
                    {
                        key = ShipData.RoleName.troop;
                        break;
                    }
                    case "fighter":
                    {
                        key = ShipData.RoleName.fighter;
                        break;
                    }
                    case "scout":
                    {
                        key = ShipData.RoleName.scout;
                        break;
                    }
                    case "gunboat":
                    {
                        key = ShipData.RoleName.gunboat;
                        break;
                    }
                    case "drone":
                    {
                        key = ShipData.RoleName.drone;
                        break;
                    }
                    case "corvette":
                    {
                        key = ShipData.RoleName.corvette;
                        break;
                    }
                    case "frigate":
                    {
                        key = ShipData.RoleName.frigate;
                        break;
                    }
                    case "destroyer":
                    {
                        key = ShipData.RoleName.destroyer;
                        break;
                    }
                    case "cruiser":
                    {
                        key = ShipData.RoleName.cruiser;
                        break;
                    }
                    case "carrier":
                    {
                        key = ShipData.RoleName.carrier;
                        break;
                    }
                    case "capital":
                    {
                        key = ShipData.RoleName.capital;
                        break;
                    }
                    case "prototype":
                    {
                        key = ShipData.RoleName.prototype;
                        break;
                    }
                    default:
                    {
                        key = ShipData.RoleName.disabled;
                        break;
                    }
                }
                for (int j = 0; j < data.RaceList.Count(); j++)
                {
                    if (Localizer.LocalizerDict.ContainsKey(data.RaceList[j].Localization + ResourceManager.OffSet))
                    {
                        data.RaceList[j].Localization += ResourceManager.OffSet;
                        Localizer.used[data.RaceList[j].Localization] = true;
                    }
                }
                if (Ship_Game.ResourceManager.ShipRoles.ContainsKey(key))
                {
                    Ship_Game.ResourceManager.ShipRoles[key] = data;
                }
                else
                {
                    Ship_Game.ResourceManager.ShipRoles.Add(key, data);
                }

            }
            textList = null;
        }

        //Added by McShooterz: Load hull bonuses
        private static void LoadHullBonuses()
        {
            if (Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/HullBonuses")) && GlobalStats.ActiveModInfo.useHullBonuses)
            {
                FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/HullBonuses"));
                XmlSerializer serializer1 = new XmlSerializer(typeof(HullBonus));
                FileInfo[] fileInfoArray = textList;
                for (int i = 0; i < (int)fileInfoArray.Length; i++)
                {
                    FileInfo FI = fileInfoArray[i];
                    FileStream stream = FI.OpenRead();
                    HullBonus data = null;
                    try
                    {
                         data = (HullBonus)serializer1.Deserialize(stream);
                    }
                    catch (Exception e)
                    {
						ReportLoadingError(FI, "LoadHullBonuses", e);
                    }
                    //stream.Close();
                    stream.Dispose();
                    String.Intern(data.Hull);
                    if (Ship_Game.ResourceManager.HullBonuses.ContainsKey(data.Hull))
                    {
                        Ship_Game.ResourceManager.HullBonuses[data.Hull] = data;
                    }
                    else
                    {
                        Ship_Game.ResourceManager.HullBonuses.Add(data.Hull, data);
                    }
                    
                }
                textList = null;
            }
            if (Ship_Game.ResourceManager.HullBonuses.Count == 0)
                GlobalStats.ActiveModInfo.useHullBonuses = false;
        }

        //Added by McShooterz: Load planetary edicts
        private static void LoadPlanetEdicts()
        {
            if (Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/PlanetEdicts")))
            {
                FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/PlanetEdicts"));
                XmlSerializer serializer1 = new XmlSerializer(typeof(PlanetEdict));
                FileInfo[] fileInfoArray = textList;
                for (int i = 0; i < (int)fileInfoArray.Length; i++)
                {
                    FileInfo FI = fileInfoArray[i];
                    FileStream stream = FI.OpenRead();
                    PlanetEdict data = null;
                    try
                    {
                         data = (PlanetEdict)serializer1.Deserialize(stream);
                    }
                    catch (Exception e)
                    {
                        ReportLoadingError(FI, "LoadPlanetEdicts", e);
                    }
                    //stream.Close();
                    stream.Dispose();
                    if (Ship_Game.ResourceManager.PlanetaryEdicts.ContainsKey(data.Name))
                    {
                        Ship_Game.ResourceManager.PlanetaryEdicts[data.Name] = data;
                    }
                    else
                    {
                        Ship_Game.ResourceManager.PlanetaryEdicts.Add(data.Name, data);
                    }
                }
                textList = null;
            }
        }

        //Added by McShooterz: Load hostileFleets.xml
        private static void LoadHostileFleets()
        {
            if (File.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/HostileFleets/HostileFleets.xml")))
            {
                Ship_Game.ResourceManager.HostileFleets = (HostileFleets)new XmlSerializer(typeof(HostileFleets)).Deserialize((Stream)new FileInfo(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/HostileFleets/HostileFleets.xml")).OpenRead());
            }
            else
            {
                return;
            }
        }

        //Added by McShooterz: Load ShipNames.xml
        private static void LoadShipNames()
        {
            if (File.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/ShipNames/ShipNames.xml")))
            {
                Ship_Game.ResourceManager.ShipNames = (ShipNames)new XmlSerializer(typeof(ShipNames)).Deserialize((Stream)new FileInfo(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/ShipNames/ShipNames.xml")).OpenRead());
            }
            else
            {
                return;
            }
            

        }

        //Added by McShooterz: Load AgentMissionData.xml
        private static void LoadAgentMissions()
        {
            if (File.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/AgentMissions/AgentMissionData.xml")))
            {
                Ship_Game.ResourceManager.AgentMissionData = (AgentMissionData)new XmlSerializer(typeof(AgentMissionData)).Deserialize((Stream)new FileInfo(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/AgentMissions/AgentMissionData.xml")).OpenRead());

            
            }
            else
            {
                return;
            }
            
        }

        //Added by McShooterz: Load AgentMissionData.xml
        private static void LoadMainMenuShipList()
        {
            if (File.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/MainMenu/MainMenuShipList.xml")))
            {
                Ship_Game.ResourceManager.MainMenuShipList = (MainMenuShipList)new XmlSerializer(typeof(MainMenuShipList)).Deserialize((Stream)new FileInfo(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/MainMenu/MainMenuShipList.xml")).OpenRead());
            }
            else
            {
                return;
            }
            foreach(string name in Ship_Game.ResourceManager.MainMenuShipList .ModelPaths )
            {
                String.Intern(name);
            }
        }

        //Added by McShooterz: load sound effects
        private static void LoadSoundEffects()
        {
            FileInfo[] fileInfoArray1 = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/SoundEffects"));
            for (int j = 0; j < (int)fileInfoArray1.Length; j++)
            {
                FileInfo FI = fileInfoArray1[j];
                string name = Path.GetFileNameWithoutExtension(FI.Name);
                String.Intern(name);
                if (name != "Thumbs")
                {
                    SoundEffect se = GetContentManager().Load<SoundEffect>(string.Concat("..\\", Ship_Game.ResourceManager.WhichModPath, "\\SoundEffects\\", name));
                    if (!Ship_Game.ResourceManager.SoundEffectDict.ContainsKey(name))
                    {
                        Ship_Game.ResourceManager.SoundEffectDict[name] = se;
                    }
                }
            }
        }

		public static void Reset()
		{
            try
            {
                DirectoryInfo di = new DirectoryInfo("Content/Mod Models");
                di.Delete(true);
                di.Create();
                di = new DirectoryInfo("Content/ModVideo");
                di.Delete(true);
                di.Create();
            }
            catch
            {
 
            }
			Ship_Game.ResourceManager.HullsDict.Clear();
			Ship_Game.ResourceManager.WeaponsDict.Clear();
			Ship_Game.ResourceManager.TroopsDict.Clear();
			Ship_Game.ResourceManager.BuildingsDict.Clear();
			Ship_Game.ResourceManager.ShipModulesDict.Clear();
			Ship_Game.ResourceManager.FlagTextures.Clear();
			Ship_Game.ResourceManager.TechTree.Clear();
			Ship_Game.ResourceManager.ArtifactsDict.Clear();
			Ship_Game.ResourceManager.ShipsDict.Clear();
            Ship_Game.ResourceManager.HostileFleets = new HostileFleets(); ;
            Ship_Game.ResourceManager.ShipNames = new ShipNames(); ;
            Ship_Game.ResourceManager.SoundEffectDict.Clear();


            Ship_Game.ResourceManager.TextureDict.Clear();
            Ship_Game.ResourceManager.ToolTips.Clear();
            Ship_Game.ResourceManager.GoodsDict.Clear();         
            //Ship_Game.ResourceManager.LoadDialogs();
            Ship_Game.ResourceManager.Encounters.Clear();
            Ship_Game.ResourceManager.EventsDict.Clear();
            
            //Ship_Game.ResourceManager.LoadLanguage();

            Ship_Game.ResourceManager.RandomItemsList.Clear();
            Ship_Game.ResourceManager.ProjectileMeshDict.Clear();
            Ship_Game.ResourceManager.ProjTextDict.Clear();
            
            //Game1.Instance.screenManager.AddScreen(new GameLoadingScreen());
            //Game1.Instance.IsLoaded = true;
            //if (Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Mod Models")))
            //{
            //    Ship_Game.ResourceManager.DirectoryCopy(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Mod Models"), "Content/Mod Models", true);
            //}
            //if (Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Video")))
            //{
            //    Ship_Game.ResourceManager.DirectoryCopy(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Video"), "Content/ModVideo", true);
            //}
          

		}
        public static List<string> FindPreviousTechs( Technology target, List<string> alreadyFound)
        {
            bool found = false;
            //this is supposed to reverse walk through the tech tree.
            foreach (KeyValuePair<string, Technology> TechTreeItem in ResourceManager.TechTree)
            {
                
                foreach (Technology.LeadsToTech leadsto in TechTreeItem.Value.LeadsTo)
                {
                    //if if it finds a tech that leads to the target tech then find the tech that leads to it. 
                    if (leadsto.UID == target.UID)
                    {
                        alreadyFound.Add(target.UID);
                        alreadyFound= FindPreviousTechs( TechTreeItem.Value, alreadyFound);
                        //alreadyFound.AddRange(FindPreviousTechs(empire, TechTreeItem.Value.GetTech(), alreadyFound));
                        found = true;
                        break;
                    }
                }
                if (found)
                    break;
            }
            return alreadyFound;
        }
		public static void Start()
		{
		}
	}
}