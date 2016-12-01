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

		static ResourceManager()
		{
			weapon_serializer   = new XmlSerializer(typeof(Weapon));
			serializer_shipdata = new XmlSerializer(typeof(ShipData));
            TextureDict         = new Dictionary<string, Texture2D>();
            ShipsDict           = new Dictionary<string, Ship>();
            RoidsModels         = new Dictionary<int, Model>();
            JunkModels          = new Dictionary<int, Model>();
            TechTree            = new Dictionary<string, Technology>(StringComparer.InvariantCultureIgnoreCase);
            Encounters          = new List<Encounter>();
            BuildingsDict       = new Dictionary<string, Building>();
            GoodsDict           = new Dictionary<string, Good>();
            WeaponsDict         = new Dictionary<string, Weapon>();
            ShipModulesDict     = new Dictionary<string, ShipModule>();
            ToolTips            = new Dictionary<int, ToolTip>();
            ProjTextDict        = new Dictionary<string, Texture2D>();
            ProjectileMeshDict  = new Dictionary<string, ModelMesh>();
            ProjectileModelDict = new Dictionary<string, Model>();
            Initialized         = false;
            WhichModPath        = "";
            RandomItemsList     = new List<RandomItem>();
            TroopsDict          = new Dictionary<string, Troop>();
            DDDict              = new Dictionary<string, DiplomacyDialog>();
            LanguageDict        = new Dictionary<string, LocalizationFile>();
            LanguageFile        = new LocalizationFile();
            ArtifactsDict       = new Dictionary<string, Artifact>();
            EventsDict          = new Dictionary<string, ExplorationEvent>();
            BigNebulas          = new List<Texture2D>();
            MedNebulas          = new List<Texture2D>();
            SmallNebulas        = new List<Texture2D>();
            SmallStars          = new List<Texture2D>();
            MediumStars         = new List<Texture2D>();
            LargeStars          = new List<Texture2D>();
            rt                  = new RacialTraits();
            Empires             = new List<EmpireData>();
            HeaderSerializer    = new XmlSerializer(typeof(HeaderData));
            ModSerializer       = new XmlSerializer(typeof(ModInformation));
            ModelDict           = new Dictionary<string, Model>();
            EconSerializer      = new XmlSerializer(typeof(EconomicResearchStrategy));
            HullsDict           = new Dictionary<string, ShipData>(StringComparer.InvariantCultureIgnoreCase);
            FlagTextures        = new List<KeyValuePair<string, Texture2D>>();
            //Added by McShooterz
            HostileFleets       = new HostileFleets();
            ShipNames           = new ShipNames();
            SoundEffectDict     = new Dictionary<string, SoundEffect>();
            AgentMissionData    = new AgentMissionData();
            MainMenuShipList    = new MainMenuShipList();
            ShipRoles           = new Dictionary<ShipData.RoleName, ShipRole>();
            HullBonuses         = new Dictionary<string, HullBonus>();
            PlanetaryEdicts     = new Dictionary<string, PlanetEdict>();
            OffSet              = 0;

        }

        public static void MarkShipDesignsUnlockable()
        {
            var shipTechs = new Dictionary<Technology, List<string>>();
            foreach (var techTreeItem in TechTree)
            {
                Technology tech = techTreeItem.Value;
                if (tech.ModulesUnlocked.Count <= 0 && tech.HullsUnlocked.Count <= 0)
                    continue;
                shipTechs.Add(tech, FindPreviousTechs(tech, new List<string>()));
            }


            foreach (ShipData hull in HullsDict.Values)
            {
                if (hull.Role == ShipData.RoleName.disabled)
                    continue;
                if (hull.Role < ShipData.RoleName.gunboat)
                    hull.unLockable = true;
                foreach (Technology hulltech2 in shipTechs.Keys)
                {
                    foreach (Technology.UnlockedHull hulls in hulltech2.HullsUnlocked)
                    {
                        if (hulls.Name == hull.Hull)
                        {
                            foreach (string tree in shipTechs[hulltech2])
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

            int x = 0;
            var purge = new HashSet<string>();
            foreach (var kv in ShipsDict)
            {

                ShipData shipData = kv.Value.shipData;
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
                    purge.Add(kv.Key);
                }



                if (shipData.hullUnlockable)
                {
                    shipData.allModulesUnlocakable = true;
                    foreach (ModuleSlotData module in kv.Value.shipData.ModuleSlotList)
                    {


                        if (module.InstalledModuleUID == "Dummy")
                            continue;
                        bool modUnlockable = false;
                        //if (!modUnlockable)
                        foreach (Technology technology in shipTechs.Keys)
                        {

                            foreach (Technology.UnlockedMod mods in technology.ModulesUnlocked)
                            {
                                if (mods.ModuleUID == module.InstalledModuleUID)
                                {
                                    modUnlockable = true;

                                    shipData.techsNeeded.Add(technology.UID);
                                    foreach (string tree in shipTechs[technology])
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
                        shipData.TechScore += (int)TechTree[techname].Cost;
                        x++;
                        if (shipData.BaseStrength == 0.0f)
                        {
                            CalculateBaseStrength(kv.Value);
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
		public static ContentManager LocalContentManager;
		public static bool IgnoreLoadingErrors = false;
		/// <summary>
		/// This one used for reporting resource loading errors.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="where"></param>
		public static void ReportLoadingError(FileInfo info, string where, Exception e)
		{
		    if (IgnoreLoadingErrors) return;
		    e.Data.Add("Failing File: ", info.FullName);
		    e.Data.Add("Fail Info: ", e.InnerException?.Message);
		    throw e;
		}
        /// <summary>
        ///  All references to Game1.Instance.Content were replaced by this function
        /// </summary>
        /// <returns></returns>
        public static ContentManager ContentManager => Game1.Instance != null ? Game1.Instance.Content : LocalContentManager;

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
            //troop.Initiative = t.Initiative;          //Not referenced in code, removing to save memory
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

       

		public static Ship CreateShipAt(string key, Empire Owner, Planet p, bool DoOrbit)   //Normal Shipyard ship creation
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
            if (!ShipsDict.TryGetValue(key, out newShip))
            {
                ShipsDict.TryGetValue(Owner.data.StartingScout, out newShip);
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
			newShip.LoadContent(ContentManager);
			SceneObject newSO = new SceneObject();
			if (!ShipsDict[key].GetShipData().Animated)
			{
				newSO = new SceneObject(GetModel(ShipsDict[key].ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
				newShip.SetSO(newSO);
			}
			else
			{
				SkinnedModel model = GetSkinnedModel(ShipsDict[key].ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			newSO.ObjectType = ObjectType.Dynamic;
			newShip.SetSO(newSO);
			foreach (Thruster t in ShipsDict[key].GetTList())
			{
				Thruster thr = new Thruster()
				{
					Parent = newShip,
					tscale = t.tscale,
					XMLPos = t.XMLPos
				};
				newShip.GetTList().Add(thr);
			}
			foreach (ModuleSlot slot in ShipsDict[key].ModuleSlotList)
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
            if (GlobalStats.ActiveMod != null && ShipNames.CheckForName(Owner.data.Traits.ShipType, newShip.shipData.Role))
                newShip.VanityName = ShipNames.GetName(Owner.data.Traits.ShipType, newShip.shipData.Role);
			newShip.GetSO().World = Matrix.CreateTranslation(new Vector3(newShip.Center, 0f));
			lock (GlobalStats.ObjectManagerLocker)
			{
				universeScreen.ScreenManager.inter.ObjectManager.Submit(newShip.GetSO());
			}
			foreach (Thruster t in newShip.GetTList())
			{
				t.load_and_assign_effects(universeScreen.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", universeScreen.ThrusterEffect);
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
        public static Ship CreateShipAt(string key, Empire Owner, Planet p, bool DoOrbit, string RefitName, byte RefitLevel)    //Refit Ship Creation
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
                shipData = ShipsDict[key].shipData,
                Name = ShipsDict[key].Name,
                BaseStrength = ShipsDict[key].BaseStrength,
                BaseCanWarp = ShipsDict[key].BaseCanWarp

            };
            newShip.LoadContent(ContentManager);
            SceneObject newSO = new SceneObject();
            if (!ShipsDict[key].GetShipData().Animated)
            {
                newSO = new SceneObject(GetModel(ShipsDict[key].ModelPath).Meshes[0])
                {
                    ObjectType = ObjectType.Dynamic
                };
                newShip.SetSO(newSO);
            }
            else
            {
                SkinnedModel model = GetSkinnedModel(ShipsDict[key].ModelPath);
                newSO = new SceneObject(model.Model);
                newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
            }
            newSO.ObjectType = ObjectType.Dynamic;
            newShip.SetSO(newSO);
            foreach (Thruster t in ShipsDict[key].GetTList())
            {
                Thruster thr = new Thruster()
                {
                    Parent = newShip,
                    tscale = t.tscale,
                    XMLPos = t.XMLPos
                };
                newShip.GetTList().Add(thr);
            }
            foreach (ModuleSlot slot in ShipsDict[key].ModuleSlotList)
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
                universeScreen.ScreenManager.inter.ObjectManager.Submit(newShip.GetSO());
            }
            foreach (Thruster t in newShip.GetTList())
            {
                t.load_and_assign_effects(universeScreen.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", universeScreen.ThrusterEffect);
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
        public static Ship CreateShipAt(string key, Empire Owner, Planet p, bool DoOrbit, ShipData.RoleName role, List<Troop> Troops) //Unused
        {
            Ship newShip = new Ship()
            {
                shipData = ShipsDict[key].shipData,
                BaseStrength = ShipsDict[key].BaseStrength,
                BaseCanWarp = ShipsDict[key].BaseCanWarp
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
            newShip.Name = ShipsDict[key].Name;
            newShip.LoadContent(ContentManager);
            SceneObject newSO;
            if (!ShipsDict[key].GetShipData().Animated)
            {
                newSO = new SceneObject(GetModel(ShipsDict[key].ModelPath).Meshes[0])
                {
                    ObjectType = ObjectType.Dynamic
                };
            }
            else
            {
                SkinnedModel model = GetSkinnedModel(ShipsDict[key].ModelPath);
                newSO = new SceneObject(model.Model);
                newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
            }
            newShip.SetSO(newSO);
            newShip.Position = p.Position;
            foreach (Thruster t in ShipsDict[key].GetTList())
            {
                Thruster thr = new Thruster()
                {
                    Parent = newShip,
                    tscale = t.tscale,
                    XMLPos = t.XMLPos
                };
                newShip.GetTList().Add(thr);
            }
            foreach (ModuleSlot slot in ShipsDict[key].ModuleSlotList)
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
            if (GlobalStats.ActiveModInfo != null && ShipNames.CheckForName(Owner.data.Traits.ShipType, newShip.shipData.Role))
                newShip.VanityName = ShipNames.GetName(Owner.data.Traits.ShipType, newShip.shipData.Role);
            newShip.GetSO().World = Matrix.CreateTranslation(new Vector3(newShip.Center, 0f));
            lock (GlobalStats.ObjectManagerLocker)
            {
                universeScreen.ScreenManager.inter.ObjectManager.Submit(newShip.GetSO());
            }
            foreach (Thruster t in newShip.GetTList())
            {
                t.load_and_assign_effects(universeScreen.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", universeScreen.ThrusterEffect);
                t.InitializeForViewing();
            }
            if (DoOrbit)
            {
                newShip.DoOrbit(p);
            }
            foreach (Troop t in Troops)
            {
                newShip.TroopList.Add(CopyTroop(t));
            }
            Owner.AddShip(newShip);
            return newShip;
        }

        public static Ship CreateShipAtPoint(string key, Empire owner, Vector2 p)       //Debug, Hanger Ship, and Platform creation
		{
            if (!ShipsDict.ContainsKey(key))
            {
                return null;
            }
            ;
            //if(universeScreen.MasterShipList.pendingRemovals.TryPop(out newShip))
            //{
            //    newShip.ShipRecreate();
            //}
            //else
            Ship newShip = new Ship();

            newShip.shipData = ShipsDict[key].shipData;
			newShip.Name = ShipsDict[key].Name;
            newShip.BaseStrength = ShipsDict[key].BaseStrength;
            newShip.BaseCanWarp = ShipsDict[key].BaseCanWarp;
			newShip.LoadContent(ContentManager);
			SceneObject newSO = new SceneObject();
			if (!ShipsDict[key].GetShipData().Animated)
			{
				newSO = new SceneObject(GetModel(ShipsDict[key].ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
				newShip.SetSO(newSO);
			}
			else
			{
				SkinnedModel model = GetSkinnedModel(ShipsDict[key].ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			newSO.ObjectType = ObjectType.Dynamic;
			newShip.SetSO(newSO);
            if (newShip.shipData.Role == ShipData.RoleName.fighter && owner.data != null)
			{
				Ship level = newShip;
				level.Level = level.Level + owner.data.BonusFighterLevels;
			}
			foreach (Thruster t in ShipsDict[key].GetTList())
			{
				Thruster thr = new Thruster()
				{
					Parent = newShip,
					tscale = t.tscale,
					XMLPos = t.XMLPos
				};
				newShip.GetTList().Add(thr);
			}
			foreach (ModuleSlot slot in ShipsDict[key].ModuleSlotList)
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
			newShip.loyalty = owner;
			newShip.Initialize();
            //Added by McShooterz: add automatic ship naming
            if (GlobalStats.ActiveMod != null && ShipNames.CheckForName(owner.data.Traits.ShipType, newShip.shipData.Role))
                newShip.VanityName = ShipNames.GetName(owner.data.Traits.ShipType, newShip.shipData.Role);
			newShip.GetSO().World = Matrix.CreateTranslation(new Vector3(newShip.Center, 0f));
			lock (GlobalStats.ObjectManagerLocker)
			{
				universeScreen.ScreenManager.inter.ObjectManager.Submit(newShip.GetSO());
			}
			newShip.isInDeepSpace = true;
			foreach (Thruster t in newShip.GetTList())
			{
				t.load_and_assign_effects(universeScreen.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", universeScreen.ThrusterEffect);
				t.InitializeForViewing();
			}
			owner.AddShip(newShip);
			return newShip;
		}

		public static Ship CreateShipAtPoint(string key, Empire owner, Vector2 p, float facing)     //unused   -- Called in fleet creation function, which is in turn not used
		{
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
            Ship newShip = new Ship()
			{
				Rotation = facing,
                shipData = ShipsDict[key].shipData,
                Name = ShipsDict[key].Name,
                BaseStrength = ShipsDict[key].BaseStrength,
                BaseCanWarp = ShipsDict[key].BaseCanWarp
                
			};
			newShip.LoadContent(ContentManager);
            if (newShip.shipData.Role == ShipData.RoleName.fighter)
			{
				Ship level = newShip;
				level.Level = level.Level + owner.data.BonusFighterLevels;
			}
			SceneObject newSO;
			if (!ShipsDict[key].GetShipData().Animated)
			{
				newSO = new SceneObject(GetModel(ShipsDict[key].ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
				newShip.SetSO(newSO);
			}
			else
			{
				SkinnedModel model = GetSkinnedModel(ShipsDict[key].ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			newShip.SetSO(newSO);
			foreach (Thruster t in ShipsDict[key].GetTList())
			{
				Thruster thr = new Thruster()
				{
					Parent = newShip,
					tscale = t.tscale,
					XMLPos = t.XMLPos
				};
				newShip.GetTList().Add(thr);
			}
			foreach (ModuleSlot slot in ShipsDict[key].ModuleSlotList)
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
			newShip.loyalty = owner;
			newShip.Initialize();
            //Added by McShooterz: add automatic ship naming
            if (GlobalStats.ActiveMod != null && ShipNames.CheckForName(owner.data.Traits.ShipType, newShip.shipData.Role))
                newShip.VanityName = ShipNames.GetName(owner.data.Traits.ShipType, newShip.shipData.Role);
			newShip.GetSO().World = Matrix.CreateTranslation(new Vector3(newShip.Center, 0f));
			lock (GlobalStats.ObjectManagerLocker)
			{
				universeScreen.ScreenManager.inter.ObjectManager.Submit(newShip.GetSO());
			}
			newShip.isInDeepSpace = true;
			foreach (Thruster t in newShip.GetTList())
			{
				t.load_and_assign_effects(universeScreen.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", universeScreen.ThrusterEffect);
				t.InitializeForViewing();
			}
			owner.AddShip(newShip);
			return newShip;
		}

		public static Ship CreateShipAtPointNow(string key, Empire owner, Vector2 p)        //Unused
		{
			Ship newShip = new Ship();
			if (!ShipsDict.ContainsKey(key))
			{
				return null;
			}
            newShip.shipData = ShipsDict[key].shipData;
			newShip.Name = ShipsDict[key].Name;
            newShip.BaseStrength = ShipsDict[key].BaseStrength;
            newShip.BaseCanWarp = ShipsDict[key].BaseCanWarp;
			newShip.LoadContent(ContentManager);
			SceneObject newSO;
			if (!ShipsDict[key].GetShipData().Animated)
			{
				newSO = new SceneObject(GetModel(ShipsDict[key].ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
				newShip.SetSO(newSO);
			}
			else
			{
				SkinnedModel model = GetSkinnedModel(ShipsDict[key].ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			newSO.ObjectType = ObjectType.Dynamic;
			newShip.SetSO(newSO);
            if (newShip.shipData.Role == ShipData.RoleName.fighter && owner.data != null)
			{
				Ship level = newShip;
				level.Level = level.Level + owner.data.BonusFighterLevels;
			}
			foreach (Thruster t in ShipsDict[key].GetTList())
			{
				Thruster thr = new Thruster()
				{
					Parent = newShip,
					tscale = t.tscale,
					XMLPos = t.XMLPos
				};
				newShip.GetTList().Add(thr);
			}
			foreach (ModuleSlot slot in ShipsDict[key].ModuleSlotList)
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
			newShip.loyalty = owner;
			newShip.Initialize();
            //Added by McShooterz: add automatic ship naming
            if (GlobalStats.ActiveMod != null && ShipNames.CheckForName(owner.data.Traits.ShipType, newShip.shipData.Role))
                newShip.VanityName = ShipNames.GetName(owner.data.Traits.ShipType, newShip.shipData.Role);
			newShip.GetSO().World = Matrix.CreateTranslation(new Vector3(newShip.Center, 0f));
			lock (GlobalStats.ObjectManagerLocker)
			{
				universeScreen.ScreenManager.inter.ObjectManager.Submit(newShip.GetSO());
			}
			newShip.isInDeepSpace = true;
			foreach (Thruster t in newShip.GetTList())
			{
				t.load_and_assign_effects(universeScreen.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", universeScreen.ThrusterEffect);
				t.InitializeForViewing();
			}
			owner.AddShip(newShip);
			return newShip;
		}

		public static Ship CreateShipForBattleMode(string key, Empire owner, Vector2 p)     //Unused... Battle mode, eh?
		{
		    Ship template = ShipsDict[key];
            Ship newShip = new Ship()
			{
                shipData = template.shipData,
                Name = template.Name,
                BaseStrength = template.BaseStrength,
                BaseCanWarp = template.BaseCanWarp
			};
			newShip.LoadContent(ContentManager);
			SceneObject newSO;
			if (!template.GetShipData().Animated)
			{
				newSO = new SceneObject(GetModel(template.ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
				newShip.SetSO(newSO);
			}
			else
			{
				SkinnedModel model = GetSkinnedModel(template.ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			foreach (Thruster t in template.GetTList())
			{
				Thruster thr = new Thruster()
				{
					Parent = newShip,
					tscale = t.tscale,
					XMLPos = t.XMLPos
				};
				newShip.GetTList().Add(thr);
			}
			foreach (ModuleSlot slot in template.ModuleSlotList)
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
			newShip.loyalty = owner;
			newShip.Initialize();
			newShip.GetSO().World = Matrix.CreateTranslation(new Vector3(newShip.Center, 0f));
			newShip.isInDeepSpace = true;
			owner.AddShip(newShip);
			return newShip;
		}

		public static Ship CreateShipFromHangar(string key, Empire owner, Vector2 p, Ship parent)       //Hanger Ship Creation (Actually just calls the one above)
		{
			Ship s = CreateShipAtPoint(key, owner, p);
		    if (s == null) return null;
		    s.Mothership = parent;
		    s.Velocity = parent.Velocity;
		    return s;
		}


		public static Troop CreateTroop(Troop t, Empire owner)
		{
			Troop troop = new Troop()
			{
				Class = t.Class,
				Cost = t.Cost,
				Name = t.Name,
				Description = t.Description,
				HardAttack = t.HardAttack,
                //Initiative = t.Initiative,          //Not referenced in code, removing to save memory
                SoftAttack = t.SoftAttack,
				Strength = t.Strength,
                StrengthMax = t.StrengthMax > 0 ? t.StrengthMax : t.Strength,
				Icon = t.Icon,
                BoardingStrength = t.BoardingStrength
			};

			if (owner != null)
			{
				Troop strength = troop;
				strength.Strength = strength.Strength + (int)(owner.data.Traits.GroundCombatModifier * (float)troop.Strength);
			}
			troop.TargetType = t.TargetType;
			troop.TexturePath = t.TexturePath;
			troop.Range = t.Range;
			troop.Experience = t.Experience;
			troop.SetOwner(owner);
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
			Ship Ship = null;
            if(!ShipsDict.TryGetValue(key, out Ship)) //|| Ship.Size > Ship_Game.ResourceManager.ShipsDict[Owner.data.DefaultSmallTransport].Size)
            {
                ShipsDict.TryGetValue("Default Troop", out Ship);
                //key = "Default Troop";
            }
            Ship newShip = new Ship
			{
                shipData = Ship.shipData,
                Name = Ship.Name,
				VanityName = troop.Name
			};
            newShip.LoadContent(ContentManager);
			SceneObject newSO;
            if (!Ship.GetShipData().Animated)
			{
                newSO = new SceneObject(GetModel(Ship.ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
			}
			else
			{
                SkinnedModel model = GetSkinnedModel(Ship.ModelPath);
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
				universeScreen.ScreenManager.inter.ObjectManager.Submit(newShip.GetSO());
			}
			foreach (Thruster t in newShip.GetTList())
			{
				t.load_and_assign_effects(universeScreen.ScreenManager.Content, "Effects/ThrustCylinderB", "Effects/NoiseVolume", universeScreen.ThrusterEffect);
				t.InitializeForViewing();
			}
			newShip.TroopList.Add(CopyTroop(troop));
            if (newShip.shipData.Role == ShipData.RoleName.troop) // && newShip.shipData.ShipCategory == ShipData.Category.Civilian)
                newShip.shipData.ShipCategory = ShipData.Category.Combat;  //fbedard
            Owner.AddShip(newShip);
			return newShip;
		}

        // Added by RedFox
        // Don't worry, yield syntax means only one file is opened at a time
        public static IEnumerable<KeyValuePair<FileInfo,T>> EnumerateEntitiesInDir<T>(string dir) where T : class
	    {
	        var serializer = new XmlSerializer(typeof(ShipData));
            foreach (FileInfo info in Dir.GetFiles(dir))
	        {
	            T item;
	            try {
	                using (FileStream stream = info.OpenRead())
	                    item = (T) serializer.Deserialize(stream);
	            } catch { continue; }
                yield return new KeyValuePair<FileInfo, T>(info, item);
            }
        }
	    public static FileInfo FindEntityFromDir<T>(string dir, out T entity, Func<T, bool> predicate) where T : class
	    {
	        foreach (var kv in EnumerateEntitiesInDir<T>(dir))
	            if (predicate(kv.Value)) {
	                entity = kv.Value;
	                return kv.Key;
	            }
            entity = null;
            return null;
	    }

        // Refactored by RedFox
        public static void DeleteShip(string shipName)
	    {
	        ShipData ship;
            FindEntityFromDir("Content/StarterShips", out ship, s => s.Name == shipName)?.Delete();
	        FindEntityFromDir("Content/SavedDesigns", out ship, s => s.Name == shipName)?.Delete();

	        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            FindEntityFromDir(appData + "/StarDrive/Saved Designs", out ship, s => s.Name == shipName)?.Delete();
            FindEntityFromDir(appData + "/StarDrive/WIP",           out ship, s => s.Name == shipName)?.Delete();

			foreach (Empire e in EmpireManager.EmpireList)
				e.UpdateShipsWeCanBuild();
		}

		public static Building GetBuilding(string whichBuilding)
		{
			Building newB = new Building()
			{
				PlanetaryShieldStrengthAdded = BuildingsDict[whichBuilding].PlanetaryShieldStrengthAdded,
				MinusFertilityOnBuild = BuildingsDict[whichBuilding].MinusFertilityOnBuild,
				CombatStrength = BuildingsDict[whichBuilding].CombatStrength,
				PlusProdPerRichness = BuildingsDict[whichBuilding].PlusProdPerRichness,
				Name = BuildingsDict[whichBuilding].Name,
                IsSensor = BuildingsDict[whichBuilding].IsSensor,
                IsProjector = BuildingsDict[whichBuilding].IsProjector,
				PlusResearchPerColonist = BuildingsDict[whichBuilding].PlusResearchPerColonist,
				StorageAdded = BuildingsDict[whichBuilding].StorageAdded,
				Unique = BuildingsDict[whichBuilding].Unique,
				Icon = BuildingsDict[whichBuilding].Icon,
				PlusTaxPercentage = BuildingsDict[whichBuilding].PlusTaxPercentage,
				Strength = BuildingsDict[whichBuilding].Strength,
				HardAttack = BuildingsDict[whichBuilding].HardAttack,
				SoftAttack = BuildingsDict[whichBuilding].SoftAttack,
				Defense = BuildingsDict[whichBuilding].Defense,
				Maintenance = BuildingsDict[whichBuilding].Maintenance,
				AllowInfantry = BuildingsDict[whichBuilding].AllowInfantry,
				CanBuildAnywhere = BuildingsDict[whichBuilding].CanBuildAnywhere,
				PlusFlatPopulation = BuildingsDict[whichBuilding].PlusFlatPopulation,
				Weapon = BuildingsDict[whichBuilding].Weapon,
				isWeapon = BuildingsDict[whichBuilding].isWeapon,
				PlusTerraformPoints = BuildingsDict[whichBuilding].PlusTerraformPoints,
                SensorRange = BuildingsDict[whichBuilding].SensorRange,
                ProjectorRange = BuildingsDict[whichBuilding].ProjectorRange,
                Category = BuildingsDict[whichBuilding].Category
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
				newB.theWeapon = GetWeapon(newB.Weapon);
			}
			newB.PlusFlatFoodAmount = BuildingsDict[whichBuilding].PlusFlatFoodAmount;
			newB.PlusFlatProductionAmount = BuildingsDict[whichBuilding].PlusFlatProductionAmount;
			newB.PlusFlatResearchAmount = BuildingsDict[whichBuilding].PlusFlatResearchAmount;
			newB.EventTriggerUID = BuildingsDict[whichBuilding].EventTriggerUID;
			newB.EventWasTriggered = false;
			newB.NameTranslationIndex = BuildingsDict[whichBuilding].NameTranslationIndex;
			newB.DescriptionIndex = BuildingsDict[whichBuilding].DescriptionIndex;
			newB.ShortDescriptionIndex = BuildingsDict[whichBuilding].ShortDescriptionIndex;
			newB.Unique = BuildingsDict[whichBuilding].Unique;
			newB.CreditsPerColonist = BuildingsDict[whichBuilding].CreditsPerColonist;
			newB.PlusProdPerColonist = BuildingsDict[whichBuilding].PlusProdPerColonist;
			newB.Scrappable = BuildingsDict[whichBuilding].Scrappable;
			newB.PlusFoodPerColonist = BuildingsDict[whichBuilding].PlusFoodPerColonist;
			newB.WinsGame = BuildingsDict[whichBuilding].WinsGame;
			newB.EventOnBuild = BuildingsDict[whichBuilding].EventOnBuild;
			newB.NoRandomSpawn = BuildingsDict[whichBuilding].NoRandomSpawn;
			newB.Cost = BuildingsDict[whichBuilding].Cost * UniverseScreen.GamePaceStatic;
			newB.MaxPopIncrease = BuildingsDict[whichBuilding].MaxPopIncrease;
			newB.AllowShipBuilding = BuildingsDict[whichBuilding].AllowShipBuilding;
			newB.BuildOnlyOnce = BuildingsDict[whichBuilding].BuildOnlyOnce;
			newB.IsCommodity = BuildingsDict[whichBuilding].IsCommodity;
			newB.CommodityBonusType = BuildingsDict[whichBuilding].CommodityBonusType;
			newB.CommodityBonusAmount = BuildingsDict[whichBuilding].CommodityBonusAmount;
			newB.ResourceCreated = BuildingsDict[whichBuilding].ResourceCreated;
			newB.ResourceConsumed = BuildingsDict[whichBuilding].ResourceConsumed;
			newB.ConsumptionPerTurn = BuildingsDict[whichBuilding].ConsumptionPerTurn;
			newB.OutputPerTurn = BuildingsDict[whichBuilding].OutputPerTurn;
			newB.CommodityRequired = BuildingsDict[whichBuilding].CommodityRequired;
            newB.ShipRepair = BuildingsDict[whichBuilding].ShipRepair;
			return newB;
		}

        public static EmpireData GetEmpireByName(string name)
        {
            foreach (EmpireData empireData in Empires)
                if (empireData.Traits.Name == name)
                    return empireData;
            return null;
        }

        private static Model LoadModel(string path, bool throwOnFailure)
        {
            try { return ContentManager.Load<Model>(path); }
            catch (ContentLoadException e) { if (throwOnFailure) throw e; }
            return null;
        }

        // Refactored by RedFox
        public static Model GetModel(string path, bool throwOnFailure = false)
		{
			Model item = null;

            // try to get cached value
            lock (ModelDict) if (ModelDict.TryGetValue(path, out item)) return item;

            if (GlobalStats.ActiveMod != null)
                item = LoadModel("Mod Models/" + path, false);

            if (item == null)
                item = LoadModel(path, throwOnFailure);

            // stick it into Model cache
            lock (ModelDict) ModelDict.Add(path, item);
            return item;
		}

		public static ShipModule GetModule(string uid)
		{
			
            ShipModule module = new ShipModule()
			{
                //All of the commented out properties here have been replaced by this single reference to 'ShipModule_Advanced' which now contains them all - Gretman
                Advanced = ShipModulesDict[uid].Advanced,

                //BombType = Ship_Game.ResourceManager.ShipModulesDict[uid].BombType,
                //HealPerTurn = Ship_Game.ResourceManager.ShipModulesDict[uid].HealPerTurn,
                //BonusRepairRate = Ship_Game.ResourceManager.ShipModulesDict[uid].BonusRepairRate,
                //Cargo_Capacity = Ship_Game.ResourceManager.ShipModulesDict[uid].Cargo_Capacity,
                //Cost = Ship_Game.ResourceManager.ShipModulesDict[uid].Cost,
                DescriptionIndex = ShipModulesDict[uid].DescriptionIndex,
                //ECM = Ship_Game.ResourceManager.ShipModulesDict[uid].ECM,
				//EMP_Protection = Ship_Game.ResourceManager.ShipModulesDict[uid].EMP_Protection,
				//explodes = Ship_Game.ResourceManager.ShipModulesDict[uid].explodes,
				FieldOfFire = ShipModulesDict[uid].FieldOfFire,
				hangarShipUID = ShipModulesDict[uid].hangarShipUID,
				hangarTimer = ShipModulesDict[uid].hangarTimer,
				//hangarTimerConstant = Ship_Game.ResourceManager.ShipModulesDict[uid].hangarTimerConstant,
				Health = ShipModulesDict[uid].HealthMax,
				//IsSupplyBay = Ship_Game.ResourceManager.ShipModulesDict[uid].IsSupplyBay,
				HealthMax = ShipModulesDict[uid].HealthMax,
				isWeapon = ShipModulesDict[uid].isWeapon,
				//IsTroopBay = Ship_Game.ResourceManager.ShipModulesDict[uid].IsTroopBay,
				Mass = ShipModulesDict[uid].Mass,
				//MechanicalBoardingDefense = Ship_Game.ResourceManager.ShipModulesDict[uid].MechanicalBoardingDefense,
				ModuleType = ShipModulesDict[uid].ModuleType,
				NameIndex = ShipModulesDict[uid].NameIndex,
				//numberOfColonists = Ship_Game.ResourceManager.ShipModulesDict[uid].numberOfColonists,
				//numberOfEquipment = Ship_Game.ResourceManager.ShipModulesDict[uid].numberOfEquipment,
				//numberOfFood = Ship_Game.ResourceManager.ShipModulesDict[uid].numberOfFood,
				OrdinanceCapacity = ShipModulesDict[uid].OrdinanceCapacity,
				//OrdnanceAddedPerSecond = Ship_Game.ResourceManager.ShipModulesDict[uid].OrdnanceAddedPerSecond,
				//PowerDraw = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerDraw,
				//PowerFlowMax = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerFlowMax,
				//PowerRadius = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerRadius,
				//PowerStoreMax = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerStoreMax,
				//SensorRange = Ship_Game.ResourceManager.ShipModulesDict[uid].SensorRange,
				shield_power = ShipModulesDict[uid].shield_power_max,    //Hmmm... This one is strange -Gretman
				//shield_power_max = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_power_max,
				//shield_radius = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_radius,
				//shield_recharge_delay = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_recharge_delay,
				//shield_recharge_rate = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_recharge_rate,
				//TechLevel = Ship_Game.ResourceManager.ShipModulesDict[uid].TechLevel,
				//thrust = Ship_Game.ResourceManager.ShipModulesDict[uid].thrust,
                //TroopBoardingDefense = Ship_Game.ResourceManager.ShipModulesDict[uid].TroopBoardingDefense,    //Not referenced in code, removing to save memory
                //TroopCapacity = Ship_Game.ResourceManager.ShipModulesDict[uid].TroopCapacity,
				//TroopsSupplied = Ship_Game.ResourceManager.ShipModulesDict[uid].TroopsSupplied,
				UID = ShipModulesDict[uid].UID,
				//WarpThrust = Ship_Game.ResourceManager.ShipModulesDict[uid].WarpThrust,
				XSIZE = ShipModulesDict[uid].XSIZE,
				YSIZE = ShipModulesDict[uid].YSIZE,
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
				PermittedHangarRoles = ShipModulesDict[uid].PermittedHangarRoles,
				//MaximumHangarShipSize = Ship_Game.ResourceManager.ShipModulesDict[uid].MaximumHangarShipSize,
				//IsRepairModule = Ship_Game.ResourceManager.ShipModulesDict[uid].IsRepairModule,
                //MountLeft = Ship_Game.ResourceManager.ShipModulesDict[uid].MountLeft,    //Not referenced in code, removing to save memory
                //MountRight = Ship_Game.ResourceManager.ShipModulesDict[uid].MountRight,    //Not referenced in code, removing to save memory
                //MountRear = Ship_Game.ResourceManager.ShipModulesDict[uid].MountRear,    //Not referenced in code, removing to save memory
                //WarpMassCapacity = Ship_Game.ResourceManager.ShipModulesDict[uid].WarpMassCapacity,
				//PowerDrawAtWarp = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerDrawAtWarp,
				//FTLSpeed = Ship_Game.ResourceManager.ShipModulesDict[uid].FTLSpeed,
				//ResourceStored = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourceStored,
                //ResourceRequired = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourceRequired,    //Not referenced in code, removing to save memory
                //ResourcePerSecond = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourcePerSecond,    //Not referenced in code, removing to save memory
                //ResourcePerSecondWarp = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourcePerSecondWarp,    //Not referenced in code, removing to save memory
                //ResourceStorageAmount = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourceStorageAmount,
				//IsCommandModule = Ship_Game.ResourceManager.ShipModulesDict[uid].IsCommandModule,
				//shield_recharge_combat_rate = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_recharge_combat_rate,
                //FTLSpoolTime = Ship_Game.ResourceManager.ShipModulesDict[uid].FTLSpoolTime,
                shieldsOff = ShipModulesDict[uid].shieldsOff,
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
		    Ship template = ShipsDict[key];
		    Ship newShip = new Ship
		    {
		        PlayerShip = true,
		        shipData = template.shipData
		        //Role = Ship_Game.ResourceManager.ShipsDict[key].Role
		    };
		    newShip.LoadContent(ContentManager);
			SceneObject newSO;
			if (!template.GetShipData().Animated)
			{
				newSO = new SceneObject(GetModel(template.ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
				newShip.SetSO(newSO);
			}
			else
			{
				SkinnedModel model = GetSkinnedModel(template.ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			newSO.ObjectType = ObjectType.Dynamic;
			newShip.SetSO(newSO);
			foreach (Thruster t in template.GetTList())
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
			foreach (ModuleSlot slot in template.ModuleSlotList)
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

        // Refactored by RedFox
		public static RacialTraits GetRaceTraits()
		{
            // Added by McShooterz: mod folder support for RacialTraits folder
            string modTraits = WhichModPath + "/RacialTraits";
		    string traitsDir = Directory.Exists(modTraits) ? modTraits : "Content/RacialTraits";

			var s = new XmlSerializer(typeof(RacialTraits));

		    foreach (var traits in LoadEntities<RacialTraits>(traitsDir, "GetRaceTraits"))
		    {
                foreach (RacialTrait trait in traits.TraitList)
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
                rt = traits;
            }
			return rt;
		}

		public static Ship GetShip(string key)
		{
		    Ship ship2 = ShipsDict[key];
            Ship newShip = new Ship()
            {
                shipData = ship2.shipData,
                BaseStrength = ship2.BaseStrength,
                BaseCanWarp = ship2.BaseCanWarp
            };
			newShip.LoadContent(ContentManager);
			newShip.Name = ship2.Name;
			SceneObject newSO;
			if (!ship2.GetShipData().Animated)
			{
				newSO = new SceneObject(GetModel(ship2.ModelPath).Meshes[0])
				{
					ObjectType = ObjectType.Dynamic
				};
				newShip.SetSO(newSO);
			}
			else
			{
				SkinnedModel model = GetSkinnedModel(ship2.ModelPath);
				newSO = new SceneObject(model.Model);
				newShip.SetAnimationController(new AnimationController(model.SkeletonBones), model);
			}
			newSO.ObjectType = ObjectType.Dynamic;
			newShip.SetSO(newSO);
			foreach (Thruster t in ship2.GetTList())
			{
				Thruster thr = new Thruster()
				{
					Parent = newShip,
					tscale = t.tscale,
					XMLPos = t.XMLPos
				};
				newShip.GetTList().Add(thr);
			}
            foreach (ModuleSlot slot in ship2.ModuleSlotList)
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
			return ContentManager.Load<SkinnedModel>(path);
		}

        // ?? Is this supposed to be Weapon.Clone() ?
		public static Weapon GetWeapon(string uid)
		{
		    Weapon w2 = WeaponsDict[uid];
            Weapon w = new Weapon
			{
				FakeExplode = w2.FakeExplode,
				Animated = w2.Animated,
				AnimationPath = w2.AnimationPath,
				BeamPowerCostPerSecond = w2.BeamPowerCostPerSecond,
				BeamThickness = w2.BeamThickness,
                BeamDuration = w2.BeamDuration,
				BombPopulationKillPerHit = w2.BombPopulationKillPerHit,
				BombTroopDamage_Max = w2.BombTroopDamage_Max,
				BombTroopDamage_Min = w2.BombTroopDamage_Min,
				DamageAmount = w2.DamageAmount,
				DamageRadius = w2.DamageRadius,
				EMPDamage = w2.EMPDamage,
				ExpColor = w2.ExpColor,
				explodes = w2.explodes,
				FireArc = w2.FireArc,
				FireCone = w2.FireCone,
				fireCueName = w2.fireCueName,
				fireDelay = w2.fireDelay,
				Frames = w2.Frames,
				HitPoints = w2.HitPoints,
				InFlightCue = w2.InFlightCue,
				isBeam = w2.isBeam,
				isMainGun = w2.isMainGun,
				isTurret = w2.isTurret,
				Light = w2.Light,
				LoopAnimation = w2.LoopAnimation,
				MassDamage = w2.MassDamage,
				ModelPath = w2.ModelPath,
				MuzzleFlash = w2.MuzzleFlash,
				Name = w2.Name,
				OrdinanceRequiredToFire = w2.OrdinanceRequiredToFire,
				particleDelay = w2.particleDelay,
				PowerDamage = w2.PowerDamage,
				PowerRequiredToFire = w2.PowerRequiredToFire,
				ProjectileCount = w2.ProjectileCount,
				ProjectileRadius = w2.ProjectileRadius,
				ProjectileSpeed = w2.ProjectileSpeed,
				ProjectileTexturePath = w2.ProjectileTexturePath,
				Range = w2.Range,
				RepulsionDamage = w2.RepulsionDamage,
				Scale = w2.Scale,
				ShieldPenChance = w2.ShieldPenChance,
				SiphonDamage = w2.SiphonDamage,
				ToggleSoundName = w2.ToggleSoundName,
				TroopDamageChance = w2.TroopDamageChance,
				UID = w2.UID,
				WeaponEffectType = w2.WeaponEffectType,
				WeaponType = w2.WeaponType,
				IsRepairDrone = w2.IsRepairDrone,
				HitsFriendlies = w2.HitsFriendlies,
				BombHardDamageMax = w2.BombHardDamageMax,
				BombHardDamageMin = w2.BombHardDamageMin,
				HardCodedAction = w2.HardCodedAction,
				TruePD = w2.TruePD,
				SalvoCount = w2.SalvoCount,
				SalvoTimer = w2.SalvoTimer,
				PlaySoundOncePerSalvo = w2.PlaySoundOncePerSalvo,
				EffectVsArmor = w2.EffectVsArmor,
				EffectVSShields = w2.EffectVSShields,
				RotationRadsPerSecond = w2.RotationRadsPerSecond,
				Tag_Beam = w2.Tag_Beam,
				Tag_Energy = w2.Tag_Energy,
				Tag_Explosive = w2.Tag_Explosive,
				Tag_Guided = w2.Tag_Guided,
				Tag_Hybrid = w2.Tag_Hybrid,
				Tag_Intercept = w2.Tag_Intercept,
				Tag_Kinetic = w2.Tag_Kinetic,
				Tag_Missile = w2.Tag_Missile,
				Tag_Railgun = w2.Tag_Railgun,
				Tag_Warp = w2.Tag_Warp,
				Tag_Torpedo = w2.Tag_Torpedo,
				Tag_Subspace = w2.Tag_Subspace,
				Tag_Cannon = w2.Tag_Cannon,
				Tag_Bomb = w2.Tag_Bomb,
				Tag_Drone = w2.Tag_Drone,
				Tag_BioWeapon = w2.Tag_BioWeapon,
				Tag_PD = w2.Tag_PD,
                Tag_Array = w2.Tag_Array,
                Tag_Flak = w2.Tag_Flak,
                Tag_Tractor = w2.Tag_Tractor,
                Tag_SpaceBomb = w2.Tag_SpaceBomb,
                ECMResist = w2.ECMResist,
                Excludes_Fighters = w2.Excludes_Fighters,
                Excludes_Corvettes = w2.Excludes_Corvettes,
                Excludes_Capitals = w2.Excludes_Capitals,
                Excludes_Stations = w2.Excludes_Stations,
                isRepairBeam = w2.isRepairBeam,
                ExplosionRadiusVisual = w2.ExplosionRadiusVisual,
                TerminalPhaseAttack = w2.TerminalPhaseAttack,
                TerminalPhaseDistance = w2.TerminalPhaseDistance,
                TerminalPhaseSpeedMod = w2.TerminalPhaseSpeedMod,
                ArmourPen = w2.ArmourPen,
                RangeVariance = w2.RangeVariance,
                //ExplosionFlash = Ship_Game.ResourceManager.w2.ExplosionFlash,          //Not referenced in code, removing to save memory
                AltFireMode = w2.AltFireMode,
                AltFireTriggerFighter = w2.AltFireTriggerFighter,
                SecondaryFire = w2.SecondaryFire,
                OffPowerMod = w2.OffPowerMod
			};
			return w;
		}

		public static void Initialize(ContentManager c)
		{
            WhichModPath = "Content";
			LoadItAll();
		}


        // Added by RedFox - Generic entity loading, less typing == more fun
        private static bool LoadEntity<T>(XmlSerializer s, FileInfo info, string id, out T entity) where T : class
	    {
	        try
	        {
                using (FileStream stream = info.OpenRead())
                    entity = (T)s.Deserialize(stream);
	            return true;
	        }
            catch (Exception e)
            {
                ReportLoadingError(info, id, e);
                entity = null;
                return false;
            }
        }
        private static IEnumerable<T> LoadEntities<T>(string dir, string id) where T : class
        {
            var s = new XmlSerializer(typeof(T));
            foreach (FileInfo info in Dir.GetFiles(WhichModPath + dir)) {
                if (LoadEntity(s, info, id, out T entity))
                    yield return entity;
            }
        }
	    private static IEnumerable<KeyValuePair<FileInfo, T>> LoadEntitiesWithInfo<T>(string dir, string id) where T : class
	    {
            var s = new XmlSerializer(typeof(T));
            foreach (FileInfo info in Dir.GetFiles(WhichModPath + dir)) {
                if (LoadEntity(s, info, id, out T entity))
                    yield return new KeyValuePair<FileInfo, T>(info, entity);
            }
        }

        private static void LoadArtifacts() // Refactored by RedFox
        {
		    foreach (var arts in LoadEntities<List<Artifact>>("/Artifacts", "LoadArtifacts"))
		    {
                foreach (Artifact art in arts)
                {
                    art.DescriptionIndex += OffSet;
                    art.NameIndex += OffSet;
                    ArtifactsDict[string.Intern(art.Name)] = art;
                }
            }
		}

		private static void LoadBuildings() // Refactored by RedFox
        {
		    foreach (var kv in LoadEntitiesWithInfo<Building>("/Buildings", "LoadBuildings"))
		    {
                Building newB = kv.Value;
                try
                {
                    if (Localizer.LocalizerDict.ContainsKey(newB.DescriptionIndex + OffSet))
                    {
                        newB.DescriptionIndex += OffSet;
                        Localizer.used[newB.DescriptionIndex] = true;
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

                    BuildingsDict[string.Intern(newB.Name)] = newB;
                }
                catch (NullReferenceException ex)
                {
                    ex.Data["Building Lookup"] = newB.Name;
                    ReportLoadingError(kv.Key, "LoadBuildings", ex);
                }
            }
		}

		private static void LoadDialogs() // Refactored by RedFox
        {
		    string dir = "/DiplomacyDialogs/" + GlobalStats.Config.Language + "/";
            foreach (var kv in LoadEntitiesWithInfo<DiplomacyDialog>(dir, "LoadDialogs"))
		    {
                string nameNoExt = Path.GetFileNameWithoutExtension(kv.Key.Name);
                DDDict[string.Intern(nameNoExt)] = kv.Value;
            }
		}

		public static void LoadEmpires() // Refactored by RedFox
        {
            Empires.Clear();
		    foreach (var empireData in LoadEntities<EmpireData>("/Races", "LoadEmpires"))
		    {
                empireData.TroopDescriptionIndex += OffSet; // @todo Is += correct here? Old version had =+
                empireData.TroopNameIndex += OffSet;
                Empires.Add(empireData);
            }
		}

		public static void LoadEncounters() // Refactored by RedFox
        {
            Encounters.Clear();
		    foreach (var encounter in LoadEntities<Encounter>("/Encounter Dialogs", "LoadEncounters"))
		    {
                Encounters.Add(encounter);
            }
		}

		private static void LoadExpEvents() // Refactored by RedFox
        {
			foreach (var kv in LoadEntitiesWithInfo<ExplorationEvent>("/Exploration Events", "LoadExpEvents"))
			{
			    string nameNoExt = Path.GetFileNameWithoutExtension(kv.Key.Name);
                EventsDict[nameNoExt] = kv.Value;
			}
		}

		private static void LoadFlagTextures() // Refactored by RedFox
        {
			foreach (FileInfo info in Dir.GetFiles(WhichModPath + "/Flags"))
			{
			    string nameNoExt = Path.GetFileNameWithoutExtension(info.Name);
			    if (nameNoExt == "Thumbs")
			        continue;

			    string path = WhichModPath == "Content"
			        ? ("Flags/" + nameNoExt) : ("../" + WhichModPath + "/Flags/" + nameNoExt);

                Texture2D tex = ContentManager.Load<Texture2D>(path);
			    FlagTextures.Add(new KeyValuePair<string, Texture2D>(nameNoExt, tex));
			}
		}

        private static void LoadGoods() // Refactored by RedFox
        {
            foreach (var kv in LoadEntitiesWithInfo<Good>("/Goods", "LoadGoods"))
            {
                Good good = kv.Value;
                good.UID = string.Intern(Path.GetFileNameWithoutExtension(kv.Key.Name));
                GoodsDict[good.UID] = good;
            }
        }

        public static void LoadHardcoreTechTree() // Refactored by RedFox
        {
			TechTree.Clear();
			foreach (var kv in LoadEntitiesWithInfo<Technology>("/Technology_HardCore", "LoadTechnologyHardcore"))
			{
			    string nameNoExt = Path.GetFileNameWithoutExtension(kv.Key.Name);
                TechTree[nameNoExt] = kv.Value;
			}
		}

		public static List<ShipData> LoadHullData() // Refactored by RedFox
        {
            var retList = new List<ShipData>();

            foreach (var kv in LoadEntitiesWithInfo<ShipData>("/Hulls", "LoadHullData"))
			{
			    string dirName = kv.Key.Directory?.Name ?? "";
                ShipData shipData = kv.Value;
                shipData.Hull = string.Intern(dirName + "/" + shipData.Hull);

			    if (!string.IsNullOrEmpty(shipData.EventOnDeath)) string.Intern(shipData.EventOnDeath);
			    if (!string.IsNullOrEmpty(shipData.ModelPath))    string.Intern(shipData.ModelPath);
			    if (!string.IsNullOrEmpty(shipData.ShipStyle))    string.Intern(shipData.ShipStyle);
			    if (!string.IsNullOrEmpty(shipData.Name))         string.Intern(shipData.Name);
			    if (!string.IsNullOrEmpty(shipData.IconPath))     string.Intern(shipData.IconPath);
			    if (!string.IsNullOrEmpty(shipData.Hull))         string.Intern(shipData.Hull);
			    if (!string.IsNullOrEmpty(shipData.SelectionGraphic)) string.Intern(shipData.SelectionGraphic);
                shipData.ShipStyle = string.Intern(dirName);

			    HullsDict[shipData.Hull] = shipData;
			    retList.Add(shipData);
			}
			return retList;
		}

		private static void LoadItAll()
		{
            OffSet = 0;
            LoadLanguage();
            LoadTroops();
			LoadTextures();
			LoadToolTips();
			LoadHullData();
			LoadWeapons();
			LoadShipModules();
			LoadGoods();
			LoadShips();
			LoadJunk();
			LoadProjTexts();
			LoadBuildings();
			LoadProjectileMeshes();
			LoadTechTree();
			LoadRandomItems();
			LoadFlagTextures();
			LoadNebulas();
			LoadSmallStars();
			LoadMediumStars();
			LoadLargeStars();
			LoadEmpires();
			LoadDialogs();
			LoadEncounters();
			LoadExpEvents();
            LoadArtifacts();			
            LoadShipRoles();
            LoadPlanetEdicts();
            //Ship_Game.ResourceManager.MarkShipDesignsUnlockable();
            
		}

		private static void LoadJunk() // Refactored by RedFox
        {
		    foreach (FileInfo info in Dir.GetFiles("Content/Model/SpaceJunk"))
		    {
                string nameNoExt = Path.GetFileNameWithoutExtension(info.Name);
		        if (!nameNoExt.StartsWith("spacejunk") || !int.TryParse(nameNoExt.Substring(9), out int idx))
                    continue;
		        try
		        {
		            Model junk = ContentManager.Load<Model>("Model/SpaceJunk/" + nameNoExt);
		            JunkModels[idx] = junk;
		        }
		        catch (Exception e)
		        {
		            ReportLoadingError(info, "LoadJunk", e);
		        }
		    }
		}

		private static void LoadLanguage() // Refactored by RedFox
        {
		    foreach (var localization in LoadEntities<LocalizationFile>("/Localization/English/", "LoadLanguage"))
		    {
			    LanguageFile = localization;
            }
            Localizer.FillLocalizer();

			if (GlobalStats.Config.Language != "English")
			{
                foreach (var localization in LoadEntities<LocalizationFile>("/Localization/" + GlobalStats.Config.Language + "/", "LoadLanguage"))
                {
                    LanguageFile = localization;
                }
			}
			Localizer.FillLocalizer();
		}

		private static void LoadLargeStars() // Refactored by RedFox
        {
		    foreach (FileInfo info in Dir.GetFiles("Content/LargeStars"))
		    {
		        try
		        {
		            string nameNoExt = Path.GetFileNameWithoutExtension(info.Name);
		            if (nameNoExt == "Thumbs") continue;

		            LargeStars.Add(ContentManager.Load<Texture2D>("LargeStars/" + nameNoExt));
		        }
		        catch (Exception e)
		        {
		            ReportLoadingError(info, "LoadLargeStars", e);
		        }
		    }
		}

		private static void LoadMediumStars() // Refactored by RedFox
        {
		    foreach (FileInfo info in Dir.GetFiles("Content/MediumStars"))
		    {
		        try
		        {
                    string nameNoExt = Path.GetFileNameWithoutExtension(info.Name);
                    if (nameNoExt == "Thumbs") continue;

		            MediumStars.Add(ContentManager.Load<Texture2D>("MediumStars/" + nameNoExt));
		        }
		        catch (Exception e)
		        {
		            ReportLoadingError(info, "LoadMediumStars", e);
		        }
		    }
		}

        public static void LoadModdedEmpires() // Refactored by RedFox
        {
		    foreach (var empireData in LoadEntities<EmpireData>("/Races", "LoadModdedEmpires"))
		    {
                if (Localizer.LocalizerDict.ContainsKey(empireData.TroopDescriptionIndex + OffSet))
                {
                    empireData.TroopDescriptionIndex += OffSet;
                    Localizer.used[empireData.TroopDescriptionIndex] = true;
                }
                if (Localizer.LocalizerDict.ContainsKey(empireData.TroopNameIndex + OffSet))
                {
                    empireData.TroopNameIndex += OffSet;
                    Localizer.used[empireData.TroopNameIndex] = true;
                }

		        int index = Empires.FindIndex(x => x.PortraitName == empireData.PortraitName);
		        if (index == -1) Empires.Add(empireData);
		        else             Empires[index] = empireData;
            }
		}

		public static void LoadMods(string modPath)
		{		
            WhichModPath = modPath;
            OffSet = modPath == "Mods/SD_Extended" ? 0 : 32000;
            LoadLanguage();
			LoadTroops();
			LoadTextures();
			LoadToolTips();
			LoadHullData();
			LoadWeapons();
			LoadShipModules();
			LoadGoods();
			LoadBuildings();
			LoadTechTree();
			LoadFlagTextures();
			LoadModdedEmpires();
			LoadDialogs();
			LoadEncounters();
			LoadExpEvents();
			LoadArtifacts();		
			LoadShips();
            LoadRandomItems();
            LoadProjTexts();
            LoadModsProjectileMeshes();
            
			if (Directory.Exists(WhichModPath + "/Mod Models"))
			{
				Dir.CopyDir(WhichModPath + "/Mod Models", "Content/Mod Models", true);
			}
			if (Directory.Exists(WhichModPath + "/Video"))
			{
                Dir.CopyDir(WhichModPath + "/Video", "Content/ModVideo", true);
			}
            // Added by McShooterz
            LoadBlackboxSpecific();
            LoadShipRoles();
            Localizer.cleanLocalizer();
            OffSet = 0;
		}


	    // Refactored by RedFox
        private static void LoadNebulas()
		{
		    foreach (FileInfo info in Dir.GetFiles("Content/Nebulas"))
		    {
		        string nameNoExt = Path.GetFileNameWithoutExtension(info.Name);
                if (nameNoExt == "Thumbs") continue;
		        Texture2D tex = ContentManager.Load<Texture2D>("Nebulas/" + nameNoExt);
		        if      (tex.Width == 2048) BigNebulas.Add(tex);
                else if (tex.Width == 1024) MedNebulas.Add(tex);
		        else                        SmallNebulas.Add(tex);
		    }
		}

        // Refactored by RedFox
		private static void LoadProjectileMeshes()
		{
			var content = ContentManager;
			try
			{
                Model projLong = content.Load<Model>("Model/Projectiles/projLong");
                Model projTear = content.Load<Model>("Model/Projectiles/projTear");
                Model projBall = content.Load<Model>("Model/Projectiles/projBall");
                Model torpedo = content.Load<Model>("Model/Projectiles/torpedo");
                Model missile = content.Load<Model>("Model/Projectiles/missile");
                Model drone = content.Load<Model>("Model/Projectiles/spacemine");

                ProjectileMeshDict["projLong"] = projLong.Meshes[0];
                ProjectileMeshDict["projTear"] = projTear.Meshes[0];
                ProjectileMeshDict["projBall"] = projBall.Meshes[0];
                ProjectileMeshDict["torpedo"] = torpedo.Meshes[0];
                ProjectileMeshDict["missile"] = missile.Meshes[0];
                ProjectileMeshDict["spacemine"] = drone.Meshes[0];

                ProjectileModelDict["projLong"] = projLong;
			    ProjectileModelDict["projTear"] = projTear;
			    ProjectileModelDict["projBall"] = projBall;
			    ProjectileModelDict["torpedo"] = torpedo;
			    ProjectileModelDict["missile"] = missile;
			    ProjectileModelDict["spacemine"] = missile;
			}
			catch (Exception) { }
		}

        
        private static void LoadModsProjectileMeshes() // Added by McShooterz: Load projectile models for mods
        {
            var content = ContentManager;
            foreach (FileInfo info in Dir.GetFilesNoSub(WhichModPath + "/Model/Projectiles"))
            {
                string nameNoExt = Path.GetFileNameWithoutExtension(info.Name);
                if (nameNoExt == "Thumbs")
                    continue;
                Model projModel = content.Load<Model>("../" + WhichModPath + "/Model/Projectiles/" + nameNoExt);
                ProjectileMeshDict[nameNoExt] = projModel.Meshes[0];
                ProjectileModelDict[nameNoExt] = projModel;
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
                FileInfo[] filesFrommodDirectory = Dir.GetFiles(string.Concat(WhichModPath, "/Model/Projectiles/textures"));
                for (int i = 0; i < (int)filesFrommodDirectory.Length; i++)
                {
                    string name = Path.GetFileNameWithoutExtension(filesFrommodDirectory[i].Name);
                    if (name != "Thumbs")
                    {
                        Texture2D tex = ContentManager.Load<Texture2D>(string.Concat("../",WhichModPath, "/Model/Projectiles/textures/", name));
                        ProjTextDict[name] = tex;
                    }
                }
            }

		}


		private static void LoadRandomItems()
		{
			RandomItemsList.Clear();
            //Added by McShooterz: mod folder support RandomStuff
			FileInfo[] textList = Dir.GetFiles(Directory.Exists(string.Concat(WhichModPath, "/RandomStuff")) ? string.Concat(WhichModPath, "/RandomStuff") : "Content/RandomStuff");
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
				RandomItemsList.Add(data);
			}
		}

		private static void LoadRoids()
		{
			FileInfo[] textList = Dir.GetFiles("Content/Model/Asteroids");
			int i = 1;
			FileInfo[] fileInfoArray = textList;
			for (int num = 0; num < (int)fileInfoArray.Length; num++)
			{
				FileInfo FI = fileInfoArray[num];
				RoidsModels[i] = ContentManager.Load<Model>(string.Concat("Model/Asteroids/", Path.GetFileNameWithoutExtension(FI.Name)));
				i++;
			}
		}

		private static void LoadShipModules()
		{
			FileInfo[] textList = Dir.GetFiles(string.Concat(WhichModPath, "/ShipModules"));
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
                if(data.IsCommandModule  && data.TargetTracking ==0 && data.FixedTracking == 0)
                {
                    data.TargetTracking = Convert.ToSByte((data.XSIZE * data.YSIZE) / 3);
                }
                if (ShipModulesDict.ContainsKey(data.UID))
				{
                    ShipModulesDict[data.UID] = data.ConvertToShipModule();
                    System.Diagnostics.Debug.WriteLine("ShipModule UID already found. Conflicting name:  " + data.UID);
                }
				else
				{
                    ShipModulesDict.Add(data.UID, data.ConvertToShipModule());
                }
                
			}
			foreach (KeyValuePair<string, ShipModule> entry in ShipModulesDict)
			{
				entry.Value.SetAttributesNoParent();
			}
			textList = null;
		}

		public static void LoadShips()
		{
            ShipsDict.Clear();// = new Dictionary<string, Ship>();
            //Added by McShooterz: Changed how StarterShips loads from mod if folder exists
            XmlSerializer serializer0 = new XmlSerializer(typeof(ShipData));
            FileInfo[] textList; //"Mods/", 
            
            if (GlobalStats.ActiveMod != null && Directory.Exists(string.Concat(WhichModPath, "/StarterShips")))
            {
                ShipsDict.Clear();
                textList = Dir.GetFiles(string.Concat(WhichModPath, "/StarterShips/"));
            }
            else
            {
                textList = Dir.GetFiles("Content/StarterShips");           
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
                        ShipsDict[newShipData.Name] = newShip;
                    }
                }
            }
			FileInfo[] filesFromDirectory = Dir.GetFiles("Content/SavedDesigns");
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
                        ShipsDict[String.Intern(newShipData.Name)] = newShip;
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
            FileInfo[] filesFromDirectory1 = Dir.GetFiles(string.Concat(path, "/StarDrive/Saved Designs"));
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
                            ShipsDict[String.Intern(newShipData.Name)] = newShip;
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
				textList = Dir.GetFiles(string.Concat("Mods/", GlobalStats.ActiveMod.ModPath, "/ShipDesigns/"));
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
                                ShipsDict[String.Intern(newShipData.Name)] = newShip;
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
            foreach (KeyValuePair<string, Ship> entry in ShipsDict)
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

                    ShipModule module = ShipModulesDict[slot.InstalledModuleUID];
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
                        ShipsDict.TryGetValue(module.hangarShipUID, out hangarship);

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

                    if (ShipModulesDict[module.UID].WarpThrust > 0)
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
            
            foreach (KeyValuePair<string, Ship> entry in ShipsDict)
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

                    ShipModule module = ShipModulesDict[slot.InstalledModuleUID];
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
                        ShipsDict.TryGetValue(module.hangarShipUID, out hangarship);

                        if (hangarship != null)
                        {
                            Str += 100;
                        }
                    }
                    def += module.shield_power_max * ((module.shield_radius * .05f) / slotCount);
                    //(module.shield_power_max+  module.shield_radius +module.shield_recharge_rate) / slotCount ;
                    def += module.HealthMax * ((module.ModuleType == ShipModuleType.Armor ? (module.XSIZE) : 1f) / (slotCount * 4));

                    if (ShipModulesDict[module.UID].WarpThrust > 0)
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



            //bool fighters = false;          //Not referenced in code, removing to save memory
            //bool weapons = false;          //Not referenced in code, removing to save memory
            //ModuleSlot slot = moduleslot;
            //foreach (ModuleSlot slot in entry.Value.ModuleSlotList.Where(dummy => dummy.InstalledModuleUID != "Dummy"))
            {

                ShipModule module = moduleslot;
                    float offRate = 0;
                    if (module.InstalledWeapon != null)
                    {
                        //weapons = true;
                        Weapon w = module.InstalledWeapon;

                    //Doctor: The 25% penalty to explosive weapons is presumably to note that not all the damage is applied to a single module - this isn't really weaker overall, though
                    //and unfairly penalises weapons with explosive damage and makes them appear falsely weaker.

                        //if (!w.explodes)
                        //{
                            offRate += (!w.isBeam ? (w.DamageAmount * w.SalvoCount) * (1f / w.fireDelay) : w.DamageAmount * 18f);
                       // }
                        //else
                        //{
                         //   offRate += (w.DamageAmount * w.SalvoCount) * (1f / w.fireDelay) * 0.75f;

                       // }

                    //Doctor: Guided weapons attract better offensive rating than unguided - more likely to hit. Setting at flat 25% currently.
                    if (w.Tag_Guided)
                    {
                        offRate *= 1.25f;
                    }

                    //Doctor: Higher range on a weapon attracts a small bonus to offensive rating. E.g. a range 2000 weapon gets 5% uplift vs a 5000 range weapon 12.5% uplift. 
                    offRate *= (1 + (w.Range / 40000));


                    //Doctor: Here follows multipliers which modify the perceived offensive value of weapons based on any modifiers they may have against armour and shields
                    //Previously if e.g. a rapid-fire cannon only did 20% damage to armour, it could have amuch higher off rating than a railgun that had less technical DPS but did double armour damage.
                    if (w.EffectVsArmor < 1)
                    {
                        if (w.EffectVsArmor > 0.75)
                            offRate *= 0.9f;
                        else if (w.EffectVsArmor > 0.5)
                            offRate *= 0.85f;
                        else if (w.EffectVsArmor > 0.25)
                            offRate *= 0.8f;
                        else
                            offRate *= 0.75f;
                    }
                    if (w.EffectVsArmor > 1)
                    {
                        if (w.EffectVsArmor > 2.0)
                            offRate *= 1.5f;
                        else if (w.EffectVsArmor > 1.5)
                            offRate *= 1.3f;
                        else
                            offRate *= 1.1f;
                    }
                    if (w.EffectVSShields < 1)
                    {
                        if (w.EffectVSShields > 0.75)
                            offRate *= 0.9f;
                        else if (w.EffectVSShields > 0.5)
                            offRate *= 0.85f;
                        else if (w.EffectVSShields > 0.25)
                            offRate *= 0.8f;
                        else
                            offRate *= 0.75f;
                    }
                    if (w.EffectVSShields > 1)
                    {
                        if (w.EffectVSShields > 2)
                            offRate *= 1.5f;
                        else if (w.EffectVSShields > 1.5)
                            offRate *= 1.3f;
                        else
                            offRate *= 1.1f;
                    }


                    //Doctor: If there are manual XML override modifiers to a weapon for manual balancing, apply them.
                    offRate *= w.OffPowerMod;

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
                        ShipsDict.TryGetValue(module.hangarShipUID, out hangarship);

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
			FileInfo[] filesFromDirectory = Dir.GetFiles("Content/SmallStars");
			for (int i = 0; i < (int)filesFromDirectory.Length; i++)
			{
				FileInfo FI = filesFromDirectory[i];
				if (Path.GetFileNameWithoutExtension(FI.Name) != "Thumbs")
				{
					Texture2D tex = ContentManager.Load<Texture2D>(string.Concat("SmallStars/", Path.GetFileNameWithoutExtension(FI.Name)));
					SmallStars.Add(tex);
				}
			}
		}

		public static void LoadSubsetEmpires()
		{
			//Ship_Game.ResourceManager.Empires.Clear();
			FileInfo[] textList = Dir.GetFiles(string.Concat(WhichModPath, "/Races"));
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
					Empires.Add(data);
				}
			}
			textList = null;
		}

		private static void LoadTechTree()
		{
			if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.clearVanillaTechs)
                TechTree.Clear();
            FileInfo[] textList = Dir.GetFiles(string.Concat(WhichModPath, "/Technology"));
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


                if (TechTree.ContainsKey(Path.GetFileNameWithoutExtension(FI.Name)))
                {

                    TechTree[Path.GetFileNameWithoutExtension(FI.Name)] = data;

                }
                else
                {

                    //Ship_Game.ResourceManager.TechTree.Add(Path.GetFileNameWithoutExtension(info.Name), data);
                    TechTree.Add(data.UID, data);
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
                                if (BuildingsDict.TryGetValue(buildingU.Name, out building))
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
                                if (ShipModulesDict.TryGetValue(moduleU.ModuleUID, out module))
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
                                ShipData.RoleName role = HullsDict[hull.Name].Role;
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
            ContentManager content = ContentManager;

            FileInfo[] textList = Dir.GetFiles(WhichModPath + "/Textures");
			if (WhichModPath != "Content")
			{
				foreach (FileInfo info in textList)
				{
                    string nameNoExt = Path.GetFileNameWithoutExtension(info.Name);
                    if (nameNoExt == "Thumbs") continue;

                    string directory = info.Directory?.Name ?? "";
				    string texPath;
				    if (directory == "Textures")
                        texPath = "../" + WhichModPath + "/Textures/" + nameNoExt;
                    else
                        texPath = "../" + WhichModPath + "/Textures/" + directory + "/" + nameNoExt;

				    Texture2D tex = content.Load<Texture2D>(texPath);
				    TextureDict[directory + "/" + nameNoExt] = tex;
				}
				return;
			}
			foreach (FileInfo FI in textList)
			{
			    if (FI.Directory.Name == "Textures")
			    {
			        string name = string.Intern(Path.GetFileNameWithoutExtension(FI.Name));
			        if (name != "Thumbs")
			        {
			            Texture2D tex = content.Load<Texture2D>(string.Concat("Textures/", Path.GetFileNameWithoutExtension(FI.Name)));
			            if (!TextureDict.ContainsKey(string.Concat(FI.Directory.Name, "/", name)))
			            {
			                TextureDict[string.Intern(string.Concat(FI.Directory.Name, "/", Path.GetFileNameWithoutExtension(FI.Name)))] = tex;
			            }
			        }
			    }
			    else
			    {
			        string name = string.Intern(Path.GetFileNameWithoutExtension(FI.Name));
			        if (name != "Thumbs")
			        {
			            Texture2D tex = ContentManager.Load<Texture2D>(string.Concat("Textures/", FI.Directory.Name, "/", name));
			            if (!TextureDict.ContainsKey(string.Concat(FI.Directory.Name, "/", name)))
			            {
			                TextureDict[string.Intern(string.Concat(FI.Directory.Name, "/", name))] = tex;
			            }
			        }
			    }
			}
		}

		private static void LoadToolTips()
		{
			foreach (var tooltips in LoadEntities<Tooltips>("/Tooltips", "LoadToolTips"))
			{
			    foreach (ToolTip tip in tooltips.ToolTipsList)
			        ToolTips[tip.TIP_ID] = tip;
			}
		}

		private static void LoadTroops()
		{
			foreach (var kv in LoadEntitiesWithInfo<Troop>("/Troops", "LoadTroops"))
			{
			    Troop troop = kv.Value;
                troop.Name = string.Intern((Path.GetFileNameWithoutExtension(kv.Key.Name)));
                TroopsDict[troop.Name] = troop;

			    if (troop.StrengthMax <= 0)
                    troop.StrengthMax = troop.Strength;
			}
		}

        
		private static void LoadWeapons() // Refactored by RedFox
        {
			foreach (var kv in LoadEntitiesWithInfo<Weapon>("/Weapons", "LoadWeapons"))
			{
			    Weapon wep = kv.Value;
			    wep.UID = string.Intern(Path.GetFileNameWithoutExtension(kv.Key.Name));
			    WeaponsDict[wep.UID] = wep;
			}
		}

        //Added by McShooterz: Load ship roles
        private static void LoadShipRoles()
        {
            foreach (var shipRole in LoadEntities<ShipRole>("/ShipRoles", "LoadShipRoles"))
            {
                if (Localizer.LocalizerDict.ContainsKey(shipRole.Localization + OffSet))
                {
                    shipRole.Localization += OffSet;
                    Localizer.used[shipRole.Localization] = true;
                }

                foreach (var race in shipRole.RaceList)
                {
                    if (!Localizer.LocalizerDict.ContainsKey(race.Localization + OffSet))
                        continue;
                    race.Localization += OffSet;
                    Localizer.used[race.Localization] = true;
                }

                Enum.TryParse(shipRole.Name, out ShipData.RoleName key);
                ShipRoles[key] = shipRole;
            }
        }

        // Added by RedFox: only deseralize to ref entity IF the file exists
	    private static void DeserializeIfExists<T>(string dir, ref T entity) where T : class
	    {
            string path = WhichModPath + dir;
	        if (!File.Exists(path))
	            return;
            using (Stream stream = new FileInfo(path).OpenRead())
                entity = (T)new XmlSerializer(typeof(T)).Deserialize(stream);
	    }

	    private static void LoadPlanetEdicts()
	    {
            foreach (var planetEdict in LoadEntities<PlanetEdict>("/PlanetEdicts", "LoadPlanetEdicts"))
                PlanetaryEdicts[planetEdict.Name] = planetEdict;
        }

        // Added by RedFox
        private static void LoadBlackboxSpecific()
        {
            if (GlobalStats.ActiveModInfo.useHullBonuses)
            {
                foreach (var hullBonus in LoadEntities<HullBonus>("/HullBonuses", "LoadHullBonuses"))
                    HullBonuses[hullBonus.Hull] = hullBonus;
                GlobalStats.ActiveModInfo.useHullBonuses = HullBonuses.Count != 0;
            }

            DeserializeIfExists("/HostileFleets/HostileFleets.xml", ref HostileFleets);
            DeserializeIfExists("/ShipNames/ShipNames.xml", ref ShipNames);
            DeserializeIfExists("/AgentMissions/AgentMissionData.xml", ref AgentMissionData);
            DeserializeIfExists("/MainMenu/MainMenuShipList.xml", ref MainMenuShipList);

            foreach (FileInfo info in Dir.GetFiles(WhichModPath + "/SoundEffects"))
            {
                string nameNoExt = Path.GetFileNameWithoutExtension(info.Name);
                if (nameNoExt == "Thumbs") continue;

                SoundEffect se = ContentManager.Load<SoundEffect>("..\\" + WhichModPath + "\\SoundEffects\\" + nameNoExt);
                SoundEffectDict[nameNoExt] = se;
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
			HullsDict.Clear();
			WeaponsDict.Clear();
			TroopsDict.Clear();
			BuildingsDict.Clear();
			ShipModulesDict.Clear();
			FlagTextures.Clear();
			TechTree.Clear();
			ArtifactsDict.Clear();
			ShipsDict.Clear();
            HostileFleets = new HostileFleets(); ;
            ShipNames = new ShipNames(); ;
            SoundEffectDict.Clear();


            TextureDict.Clear();
            ToolTips.Clear();
            GoodsDict.Clear();         
            //Ship_Game.ResourceManager.LoadDialogs();
            Encounters.Clear();
            EventsDict.Clear();
            
            //Ship_Game.ResourceManager.LoadLanguage();

            RandomItemsList.Clear();
            ProjectileMeshDict.Clear();
            ProjTextDict.Clear();
		}
        public static List<string> FindPreviousTechs(Technology target, List<string> alreadyFound)
        {
            //this is supposed to reverse walk through the tech tree.
            foreach (var techTreeItem in TechTree)
            {
                Technology tech = techTreeItem.Value;
                foreach (Technology.LeadsToTech leadsto in tech.LeadsTo)
                {
                    //if if it finds a tech that leads to the target tech then find the tech that leads to it. 
                    if (leadsto.UID == target.UID)
                    {
                        alreadyFound.Add(target.UID);
                        return FindPreviousTechs(tech, alreadyFound);
                    }
                }
            }
            return alreadyFound;
        }
	}
}