using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
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

namespace Ship_Game
{
	public class ResourceManager
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

		static ResourceManager()
		{
			Ship_Game.ResourceManager.TextureDict = new Dictionary<string, Texture2D>();
			Ship_Game.ResourceManager.weapon_serializer = new XmlSerializer(typeof(Weapon));
			Ship_Game.ResourceManager.serializer_shipdata = new XmlSerializer(typeof(ShipData));
			Ship_Game.ResourceManager.ShipsDict = new Dictionary<string, Ship>();
			Ship_Game.ResourceManager.RoidsModels = new Dictionary<int, Model>();
			Ship_Game.ResourceManager.JunkModels = new Dictionary<int, Model>();
			Ship_Game.ResourceManager.TechTree = new Dictionary<string, Technology>();
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
			Ship_Game.ResourceManager.HullsDict = new Dictionary<string, ShipData>();
			Ship_Game.ResourceManager.FlagTextures = new List<KeyValuePair<string, Texture2D>>();
		}

		public ResourceManager()
		{
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
			troop.Initiative = t.Initiative;
			troop.SoftAttack = t.SoftAttack;
			troop.Strength = t.Strength;
			troop.StrengthMax = t.StrengthMax;
			troop.TargetType = t.TargetType;
			troop.TexturePath = t.TexturePath;
			troop.Kills = t.Kills;
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
			return troop;
		}

		public static Ship CreateShipAt(string key, Empire Owner, Planet p, bool DoOrbit)
		{
			Ship newShip = new Ship()
			{
				Role = Ship_Game.ResourceManager.ShipsDict[key].Role,
				Name = Ship_Game.ResourceManager.ShipsDict[key].Name
			};
			newShip.LoadContent(Game1.Instance.Content);
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
			if (newShip.Role == "fighter")
			{
				Ship level = newShip;
				level.Level = level.Level + Owner.data.BonusFighterLevels;
			}
			Owner.AddShip(newShip);
			newShip.GetAI().State = AIState.AwaitingOrders;
			return newShip;
		}

		public static Ship CreateShipAt(string key, Empire Owner, Planet p, bool DoOrbit, string role, List<Troop> Troops)
		{
			Ship newShip = new Ship()
			{
				Role = role
			};
			if (role == "troop")
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
			newShip.LoadContent(Game1.Instance.Content);
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
			newShip.SetHome(p);
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
			if (newShip.Role == "fighter")
			{
				Ship level = newShip;
				level.Level = level.Level + Owner.data.BonusFighterLevels;
			}
			newShip.loyalty = Owner;
			newShip.Initialize();
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
			Ship newShip = new Ship();
			if (!Ship_Game.ResourceManager.ShipsDict.ContainsKey(key))
			{
				return null;
			}
			newShip.Role = Ship_Game.ResourceManager.ShipsDict[key].Role;
			newShip.Name = Ship_Game.ResourceManager.ShipsDict[key].Name;
			newShip.LoadContent(Game1.Instance.Content);
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
			if (newShip.Role == "fighter" && Owner.data != null)
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
			Ship newShip = new Ship()
			{
				Rotation = facing,
				Role = Ship_Game.ResourceManager.ShipsDict[key].Role,
				Name = Ship_Game.ResourceManager.ShipsDict[key].Name
			};
			newShip.LoadContent(Game1.Instance.Content);
			if (newShip.Role == "fighter")
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
			newShip.Role = Ship_Game.ResourceManager.ShipsDict[key].Role;
			newShip.Name = Ship_Game.ResourceManager.ShipsDict[key].Name;
			newShip.LoadContent(Game1.Instance.Content);
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
			if (newShip.Role == "fighter" && Owner.data != null)
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
				Role = Ship_Game.ResourceManager.ShipsDict[key].Role,
				Name = Ship_Game.ResourceManager.ShipsDict[key].Name
			};
			newShip.LoadContent(Game1.Instance.Content);
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
				Initiative = t.Initiative,
				SoftAttack = t.SoftAttack,
				Strength = t.Strength,
				StrengthMax = t.StrengthMax,
				Icon = t.Icon
			};
			if (Owner != null)
			{
				Troop strength = troop;
				strength.Strength = strength.Strength + (int)(Owner.data.Traits.GroundCombatModifier * (float)troop.Strength);
				Troop strengthMax = troop;
				strengthMax.StrengthMax = strengthMax.StrengthMax + (int)(Owner.data.Traits.GroundCombatModifier * (float)troop.Strength);
			}
			troop.TargetType = t.TargetType;
			troop.TexturePath = t.TexturePath;
			troop.Range = t.Range;
			troop.Kills = t.Kills;
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
			Ship newShip = new Ship()
			{
				Role = "troop",
				Name = key,
				VanityName = troop.Name
			};
			newShip.LoadContent(Game1.Instance.Content);
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
			newSO.ObjectType = ObjectType.Dynamic;
			newShip.SetSO(newSO);
			newShip.Position = point;
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
				stream.Close();
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
				stream.Close();
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
				ShipData newShipData = (ShipData)serializer0.Deserialize(stream);
				stream.Close();
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
				stream.Close();
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
                ProjectorRange = Ship_Game.ResourceManager.BuildingsDict[whichBuilding].ProjectorRange
			};
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
			Model item;
			try
			{
				if (!Ship_Game.ResourceManager.ModelDict.ContainsKey(path))
				{
					Model model = Game1.Instance.Content.Load<Model>(path);
					Ship_Game.ResourceManager.ModelDict.Add(path, model);
					item = model;
				}
				else
				{
					item = Ship_Game.ResourceManager.ModelDict[path];
				}
			}
			catch
			{
				Model model = Game1.Instance.Content.Load<Model>(string.Concat("Mod Models/", path));
				item = model;
			}
			return item;
		}

		public static ShipModule GetModule(string uid)
		{
			ShipModule module = new ShipModule()
			{
				BombType = Ship_Game.ResourceManager.ShipModulesDict[uid].BombType,
				HealPerTurn = Ship_Game.ResourceManager.ShipModulesDict[uid].HealPerTurn,
				BonusRepairRate = Ship_Game.ResourceManager.ShipModulesDict[uid].BonusRepairRate,
                CanRotate = Ship_Game.ResourceManager.ShipModulesDict[uid].CanRotate,
				Cargo_Capacity = Ship_Game.ResourceManager.ShipModulesDict[uid].Cargo_Capacity,
				Cost = Ship_Game.ResourceManager.ShipModulesDict[uid].Cost,
				DescriptionIndex = Ship_Game.ResourceManager.ShipModulesDict[uid].DescriptionIndex,
				EMP_Protection = Ship_Game.ResourceManager.ShipModulesDict[uid].EMP_Protection,
				explodes = Ship_Game.ResourceManager.ShipModulesDict[uid].explodes,
				FieldOfFire = Ship_Game.ResourceManager.ShipModulesDict[uid].FieldOfFire,
				hangarShipUID = Ship_Game.ResourceManager.ShipModulesDict[uid].hangarShipUID,
				hangarTimer = Ship_Game.ResourceManager.ShipModulesDict[uid].hangarTimer,
				hangarTimerConstant = Ship_Game.ResourceManager.ShipModulesDict[uid].hangarTimerConstant,
				Health = Ship_Game.ResourceManager.ShipModulesDict[uid].HealthMax,
				IsSupplyBay = Ship_Game.ResourceManager.ShipModulesDict[uid].IsSupplyBay,
				HealthMax = Ship_Game.ResourceManager.ShipModulesDict[uid].HealthMax,
				isWeapon = Ship_Game.ResourceManager.ShipModulesDict[uid].isWeapon,
				IsTroopBay = Ship_Game.ResourceManager.ShipModulesDict[uid].IsTroopBay,
				Mass = Ship_Game.ResourceManager.ShipModulesDict[uid].Mass,
				MechanicalBoardingDefense = Ship_Game.ResourceManager.ShipModulesDict[uid].MechanicalBoardingDefense,
				ModuleType = Ship_Game.ResourceManager.ShipModulesDict[uid].ModuleType,
				NameIndex = Ship_Game.ResourceManager.ShipModulesDict[uid].NameIndex,
				numberOfColonists = Ship_Game.ResourceManager.ShipModulesDict[uid].numberOfColonists,
				numberOfEquipment = Ship_Game.ResourceManager.ShipModulesDict[uid].numberOfEquipment,
				numberOfFood = Ship_Game.ResourceManager.ShipModulesDict[uid].numberOfFood,
				OrdinanceCapacity = Ship_Game.ResourceManager.ShipModulesDict[uid].OrdinanceCapacity,
				OrdnanceAddedPerSecond = Ship_Game.ResourceManager.ShipModulesDict[uid].OrdnanceAddedPerSecond,
				PowerDraw = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerDraw,
				PowerFlowMax = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerFlowMax,
				PowerRadius = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerRadius,
				PowerStoreMax = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerStoreMax,
				SensorRange = Ship_Game.ResourceManager.ShipModulesDict[uid].SensorRange,
				shield_power = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_power_max,
				shield_power_max = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_power_max,
				shield_radius = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_radius,
				shield_recharge_delay = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_recharge_delay,
				shield_recharge_rate = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_recharge_rate,
				TechLevel = Ship_Game.ResourceManager.ShipModulesDict[uid].TechLevel,
				thrust = Ship_Game.ResourceManager.ShipModulesDict[uid].thrust,
				TroopBoardingDefense = Ship_Game.ResourceManager.ShipModulesDict[uid].TroopBoardingDefense,
				TroopCapacity = Ship_Game.ResourceManager.ShipModulesDict[uid].TroopCapacity,
				TroopsSupplied = Ship_Game.ResourceManager.ShipModulesDict[uid].TroopsSupplied,
				UID = Ship_Game.ResourceManager.ShipModulesDict[uid].UID,
				WarpThrust = Ship_Game.ResourceManager.ShipModulesDict[uid].WarpThrust,
				XSIZE = Ship_Game.ResourceManager.ShipModulesDict[uid].XSIZE,
				YSIZE = Ship_Game.ResourceManager.ShipModulesDict[uid].YSIZE,
				InhibitionRadius = Ship_Game.ResourceManager.ShipModulesDict[uid].InhibitionRadius,
				FightersOnly = Ship_Game.ResourceManager.ShipModulesDict[uid].FightersOnly,
				TurnThrust = Ship_Game.ResourceManager.ShipModulesDict[uid].TurnThrust,
				DeployBuildingOnColonize = Ship_Game.ResourceManager.ShipModulesDict[uid].DeployBuildingOnColonize,
				PermittedHangarRoles = Ship_Game.ResourceManager.ShipModulesDict[uid].PermittedHangarRoles,
				MaximumHangarShipSize = Ship_Game.ResourceManager.ShipModulesDict[uid].MaximumHangarShipSize,
				IsRepairModule = Ship_Game.ResourceManager.ShipModulesDict[uid].IsRepairModule,
				MountLeft = Ship_Game.ResourceManager.ShipModulesDict[uid].MountLeft,
				MountRight = Ship_Game.ResourceManager.ShipModulesDict[uid].MountRight,
				MountRear = Ship_Game.ResourceManager.ShipModulesDict[uid].MountRear,
				WarpMassCapacity = Ship_Game.ResourceManager.ShipModulesDict[uid].WarpMassCapacity,
				PowerDrawAtWarp = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerDrawAtWarp,
				PowerDrawWithAfterburner = Ship_Game.ResourceManager.ShipModulesDict[uid].PowerDrawWithAfterburner,
				AfterburnerThrust = Ship_Game.ResourceManager.ShipModulesDict[uid].AfterburnerThrust,
				FTLSpeed = Ship_Game.ResourceManager.ShipModulesDict[uid].FTLSpeed,
				ResourceStored = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourceStored,
				ResourceRequired = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourceRequired,
				ResourcePerSecond = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourcePerSecond,
				ResourcePerSecondAfterburner = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourcePerSecondAfterburner,
				ResourcePerSecondWarp = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourcePerSecondWarp,
				ResourceStorageAmount = Ship_Game.ResourceManager.ShipModulesDict[uid].ResourceStorageAmount,
				IsCommandModule = Ship_Game.ResourceManager.ShipModulesDict[uid].IsCommandModule,
				shield_recharge_combat_rate = Ship_Game.ResourceManager.ShipModulesDict[uid].shield_recharge_combat_rate
			};
			return module;
		}

		public static Ship GetPlayerShip(string key)
		{
			Ship newShip = new Ship()
			{
				PlayerShip = true,
				Role = Ship_Game.ResourceManager.ShipsDict[key].Role
			};
			newShip.LoadContent(Game1.Instance.Content);
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
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/RacialTraits");
			XmlSerializer serializer1 = new XmlSerializer(typeof(RacialTraits));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
				RacialTraits data = (RacialTraits)serializer1.Deserialize(stream);
				stream.Close();
				stream.Dispose();
				Ship_Game.ResourceManager.rt = data;
			}
			textList = null;
			return Ship_Game.ResourceManager.rt;
		}

		public static Ship GetShip(string key)
		{
			Ship newShip = new Ship()
			{
				Role = Ship_Game.ResourceManager.ShipsDict[key].Role
			};
			newShip.LoadContent(Game1.Instance.Content);
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
				newSlot.Restrictions = slot.Restrictions;
				newSlot.Position = slot.Position;
				newSlot.facing = slot.facing;
				newSlot.InstalledModuleUID = slot.InstalledModuleUID;
				newShip.ModuleSlotList.AddLast(newSlot);
			}
			return newShip;
		}

		public static SkinnedModel GetSkinnedModel(string path)
		{
			return Game1.Instance.Content.Load<SkinnedModel>(path);
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
				requiresOrdinance = Ship_Game.ResourceManager.WeaponsDict[uid].requiresOrdinance,
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
				Tag_PD = Ship_Game.ResourceManager.WeaponsDict[uid].Tag_PD
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
				List<Artifact> data = (List<Artifact>)serializer1.Deserialize(stream);
				stream.Close();
				stream.Dispose();
				foreach (Artifact art in data)
				{
					if (Ship_Game.ResourceManager.ArtifactsDict.ContainsKey(art.Name))
					{
						Ship_Game.ResourceManager.ArtifactsDict[art.Name] = art;
					}
					else
					{
						Ship_Game.ResourceManager.ArtifactsDict.Add(art.Name, art);
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
				Building newB = (Building)serializer0.Deserialize(stream);
				stream.Close();
				stream.Dispose();
				if (Ship_Game.ResourceManager.BuildingsDict.ContainsKey(newB.Name))
				{
					Ship_Game.ResourceManager.BuildingsDict[newB.Name] = newB;
				}
				else
				{
					Ship_Game.ResourceManager.BuildingsDict.Add(newB.Name, newB);
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
				DiplomacyDialog data = (DiplomacyDialog)serializer1.Deserialize(stream);
				stream.Close();
				stream.Dispose();
				if (Ship_Game.ResourceManager.DDDict.ContainsKey(Path.GetFileNameWithoutExtension(FI.Name)))
				{
					Ship_Game.ResourceManager.DDDict[Path.GetFileNameWithoutExtension(FI.Name)] = data;
				}
				else
				{
					Ship_Game.ResourceManager.DDDict.Add(Path.GetFileNameWithoutExtension(FI.Name), data);
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
				EmpireData data = (EmpireData)serializer1.Deserialize(stream);
				stream.Close();
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
				Encounter data = (Encounter)serializer1.Deserialize(stream);
				stream.Close();
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
				ExplorationEvent data = (ExplorationEvent)serializer1.Deserialize(stream);
				stream.Close();
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
					Texture2D tex = Game1.Instance.Content.Load<Texture2D>((Ship_Game.ResourceManager.WhichModPath == "Content" ? string.Concat("Flags/", Path.GetFileNameWithoutExtension(FI.Name)) : string.Concat(".../", Ship_Game.ResourceManager.WhichModPath, "/Flags/", Path.GetFileNameWithoutExtension(FI.Name))));
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
				Good data = (Good)serializer1.Deserialize(stream);
				data.UID = Path.GetFileNameWithoutExtension(FI.Name);
				stream.Close();
				stream.Dispose();
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
				stream.Close();
				stream.Dispose();
				if (Ship_Game.ResourceManager.TechTree.ContainsKey(Path.GetFileNameWithoutExtension(FI.Name)))
				{
					Ship_Game.ResourceManager.TechTree[Path.GetFileNameWithoutExtension(FI.Name)] = data;
				}
				else
				{
					Ship_Game.ResourceManager.TechTree.Add(Path.GetFileNameWithoutExtension(FI.Name), data);
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
				ShipData newShipData = (ShipData)serializer0.Deserialize(stream);
				stream.Close();
				stream.Dispose();
				newShipData.Hull = string.Concat(FI.Directory.Name, "/", newShipData.Hull);
				newShipData.ShipStyle = FI.Directory.Name;
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
			Ship_Game.ResourceManager.LoadLanguage();
		}

		private static void LoadJunk()
		{
			FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/Model/SpaceJunk");
			for (int num = 0; num < (int)filesFromDirectory.Length; num++)
			{
				FileInfo FI = filesFromDirectory[num];
				for (int i = 1; i < 15; i++)
				{
					if (Path.GetFileNameWithoutExtension(FI.Name) == string.Concat("spacejunk", i))
					{
						Model junk = Game1.Instance.Content.Load<Model>(string.Concat("Model/SpaceJunk/", Path.GetFileNameWithoutExtension(FI.Name)));
						Ship_Game.ResourceManager.JunkModels[i] = junk;
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
				stream.Close();
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
					stream.Close();
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
				if (Path.GetFileNameWithoutExtension(FI.Name) != "Thumbs")
				{
					Texture2D tex = Game1.Instance.Content.Load<Texture2D>(string.Concat("LargeStars/", Path.GetFileNameWithoutExtension(FI.Name)));
					Ship_Game.ResourceManager.LargeStars.Add(tex);
				}
			}
		}

		private static void LoadMediumStars()
		{
			FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/MediumStars");
			for (int i = 0; i < (int)filesFromDirectory.Length; i++)
			{
				FileInfo FI = filesFromDirectory[i];
				if (Path.GetFileNameWithoutExtension(FI.Name) != "Thumbs")
				{
					Texture2D tex = Game1.Instance.Content.Load<Texture2D>(string.Concat("MediumStars/", Path.GetFileNameWithoutExtension(FI.Name)));
					Ship_Game.ResourceManager.MediumStars.Add(tex);
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
				EmpireData data = (EmpireData)serializer1.Deserialize(stream);
				stream.Close();
				stream.Dispose();
				Ship_Game.ResourceManager.Empires.Add(data);
			}
			textList = null;
		}

		public static void LoadMods(string ModPath)
		{
			Ship_Game.ResourceManager.WhichModPath = ModPath;
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
			Ship_Game.ResourceManager.LoadLanguage();
			Ship_Game.ResourceManager.LoadShips();
			if (Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Mod Models")))
			{
				Ship_Game.ResourceManager.DirectoryCopy(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Mod Models"), "Content/Mod Models", true);
			}
			if (Directory.Exists(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Video")))
			{
				Ship_Game.ResourceManager.DirectoryCopy(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Video"), "Content/ModVideo", true);
			}
		}

		private static void LoadNebulas()
		{
			FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/Nebulas");
			for (int i = 0; i < (int)filesFromDirectory.Length; i++)
			{
				FileInfo FI = filesFromDirectory[i];
				if (Path.GetFileNameWithoutExtension(FI.Name) != "Thumbs")
				{
					Texture2D tex = Game1.Instance.Content.Load<Texture2D>(string.Concat("Nebulas/", Path.GetFileNameWithoutExtension(FI.Name)));
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
			Model projLong = Game1.Instance.Content.Load<Model>("Model/Projectiles/projLong");
			ModelMesh projMesh = Game1.Instance.Content.Load<Model>("Model/Projectiles/projLong").Meshes[0];
			Ship_Game.ResourceManager.ProjectileMeshDict["projLong"] = projMesh;
			Ship_Game.ResourceManager.ProjectileModelDict["projLong"] = projLong;
			Model projTear = Game1.Instance.Content.Load<Model>("Model/Projectiles/projTear");
			projMesh = projTear.Meshes[0];
			Ship_Game.ResourceManager.ProjectileMeshDict["projTear"] = projMesh;
			Ship_Game.ResourceManager.ProjectileModelDict["projTear"] = projTear;
			Model projBall = Game1.Instance.Content.Load<Model>("Model/Projectiles/projBall");
			projMesh = projBall.Meshes[0];
			Ship_Game.ResourceManager.ProjectileMeshDict["projBall"] = projMesh;
			Ship_Game.ResourceManager.ProjectileModelDict["projBall"] = projBall;
			Model torpedo = Game1.Instance.Content.Load<Model>("Model/Projectiles/torpedo");
			projMesh = torpedo.Meshes[0];
			Ship_Game.ResourceManager.ProjectileMeshDict["torpedo"] = projMesh;
			Ship_Game.ResourceManager.ProjectileModelDict["torpedo"] = torpedo;
			Model missile = Game1.Instance.Content.Load<Model>("Model/Projectiles/missile");
			projMesh = missile.Meshes[0];
			Ship_Game.ResourceManager.ProjectileMeshDict["missile"] = projMesh;
			Ship_Game.ResourceManager.ProjectileModelDict["missile"] = missile;
			Model drone = Game1.Instance.Content.Load<Model>("Model/Projectiles/spacemine");
			projMesh = drone.Meshes[0];
			Ship_Game.ResourceManager.ProjectileMeshDict["spacemine"] = projMesh;
			Ship_Game.ResourceManager.ProjectileModelDict["spacemine"] = missile;
		}

		private static void LoadProjTexts()
		{
			FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/Model/Projectiles/textures");
			for (int i = 0; i < (int)filesFromDirectory.Length; i++)
			{
				string name = Path.GetFileNameWithoutExtension(filesFromDirectory[i].Name);
				if (name != "Thumbs")
				{
					Texture2D tex = Game1.Instance.Content.Load<Texture2D>(string.Concat("Model/Projectiles/textures/", name));
					Ship_Game.ResourceManager.ProjTextDict[name] = tex;
				}
			}
		}

		private static void LoadRandomItems()
		{
			Ship_Game.ResourceManager.RandomItemsList.Clear();
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/RandomStuff"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(RandomItem));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
				RandomItem data = (RandomItem)serializer1.Deserialize(stream);
				stream.Close();
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
				Ship_Game.ResourceManager.RoidsModels[i] = Game1.Instance.Content.Load<Model>(string.Concat("Model/Asteroids/", Path.GetFileNameWithoutExtension(FI.Name)));
				i++;
			}
		}

		private static void LoadShipModules()
		{
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/ShipModules"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(ShipModule));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileInfo FI = fileInfoArray[i];
				FileStream stream = FI.OpenRead();
				ShipModule data = (ShipModule)serializer1.Deserialize(stream);
				stream.Close();
				stream.Dispose();
				if (Ship_Game.ResourceManager.ShipModulesDict.ContainsKey(Path.GetFileNameWithoutExtension(FI.Name)))
				{
					Ship_Game.ResourceManager.ShipModulesDict[Path.GetFileNameWithoutExtension(FI.Name)] = data;
				}
				else
				{
					Ship_Game.ResourceManager.ShipModulesDict.Add(Path.GetFileNameWithoutExtension(FI.Name), data);
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
			Ship_Game.ResourceManager.ShipsDict.Clear();
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/StarterShips");
			XmlSerializer serializer0 = new XmlSerializer(typeof(ShipData));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
				ShipData newShipData = (ShipData)serializer0.Deserialize(stream);
				stream.Close();
				stream.Dispose();
				Ship newShip = Ship.CreateShipFromShipData(newShipData);
				newShip.SetShipData(newShipData);
				newShip.reserved = true;
				if (newShip.InitForLoad())
				{
					newShip.InitializeStatus();
					Ship_Game.ResourceManager.ShipsDict[newShipData.Name] = newShip;
				}
			}
			FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/SavedDesigns");
			for (int j = 0; j < (int)filesFromDirectory.Length; j++)
			{
				FileStream stream = filesFromDirectory[j].OpenRead();
				ShipData newShipData = (ShipData)serializer0.Deserialize(stream);
				stream.Close();
				stream.Dispose();
				Ship newShip = Ship.CreateShipFromShipData(newShipData);
				newShip.SetShipData(newShipData);
				if (newShip.InitForLoad())
				{
					newShip.InitializeStatus();
					Ship_Game.ResourceManager.ShipsDict[newShipData.Name] = newShip;
				}
			}
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			FileInfo[] filesFromDirectory1 = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(path, "/StarDrive/Saved Designs"));
			for (int k = 0; k < (int)filesFromDirectory1.Length; k++)
			{
				FileInfo FI = filesFromDirectory1[k];
				try
				{
					FileStream stream = FI.OpenRead();
					ShipData newShipData = (ShipData)serializer0.Deserialize(stream);
					stream.Close();
					stream.Dispose();
					Ship newShip = Ship.CreateShipFromShipData(newShipData);
					newShip.IsPlayerDesign = true;
					newShip.SetShipData(newShipData);
					if (newShip.InitForLoad())
					{
						newShip.InitializeStatus();
						Ship_Game.ResourceManager.ShipsDict[newShipData.Name] = newShip;
					}
				}
				catch
				{
				}
			}
			if (GlobalStats.ActiveMod != null)
			{
				textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat("Mods/", GlobalStats.ActiveMod.ModPath, "/ShipDesigns/"));
				FileInfo[] fileInfoArray1 = textList;
				for (int l = 0; l < (int)fileInfoArray1.Length; l++)
				{
					FileInfo FI = fileInfoArray1[l];
					try
					{
						FileStream stream = FI.OpenRead();
						ShipData newShipData = (ShipData)serializer0.Deserialize(stream);
						stream.Close();
						stream.Dispose();
						Ship newShip = Ship.CreateShipFromShipData(newShipData);
						newShip.IsPlayerDesign = true;
						newShip.SetShipData(newShipData);
						if (newShip.InitForLoad())
						{
							newShip.InitializeStatus();
							Ship_Game.ResourceManager.ShipsDict[newShipData.Name] = newShip;
						}
					}
					catch
					{
					}
				}
			}
			foreach (KeyValuePair<string, Ship> entry in Ship_Game.ResourceManager.ShipsDict)
			{
				foreach (ModuleSlot slot in entry.Value.ModuleSlotList)
				{
					if (Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].isWeapon)
					{
						if (Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WeaponType].explodes)
						{
							Ship value = entry.Value;
							value.BaseStrength = value.BaseStrength + Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WeaponType].DamageAmount * (1f / Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WeaponType].fireDelay) * 0.75f;
						}
						else if (!Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WeaponType].isBeam)
						{
							Ship baseStrength = entry.Value;
							baseStrength.BaseStrength = baseStrength.BaseStrength + Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WeaponType].DamageAmount * (1f / Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WeaponType].fireDelay);
						}
						else
						{
							Ship ship = entry.Value;
							ship.BaseStrength = ship.BaseStrength + Ship_Game.ResourceManager.WeaponsDict[Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WeaponType].DamageAmount * 180f;
						}
					}
					if (Ship_Game.ResourceManager.ShipModulesDict[slot.InstalledModuleUID].WarpThrust <= 0)
					{
						continue;
					}
					entry.Value.BaseCanWarp = true;
				}
				Ship value1 = entry.Value;
				value1.BaseStrength = value1.BaseStrength / (float)entry.Value.ModuleSlotList.Count;
			}
		}

		private static void LoadSmallStars()
		{
			FileInfo[] filesFromDirectory = Ship_Game.ResourceManager.GetFilesFromDirectory("Content/SmallStars");
			for (int i = 0; i < (int)filesFromDirectory.Length; i++)
			{
				FileInfo FI = filesFromDirectory[i];
				if (Path.GetFileNameWithoutExtension(FI.Name) != "Thumbs")
				{
					Texture2D tex = Game1.Instance.Content.Load<Texture2D>(string.Concat("SmallStars/", Path.GetFileNameWithoutExtension(FI.Name)));
					Ship_Game.ResourceManager.SmallStars.Add(tex);
				}
			}
		}

		public static void LoadSubsetEmpires()
		{
			Ship_Game.ResourceManager.Empires.Clear();
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Races"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(EmpireData));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileStream stream = fileInfoArray[i].OpenRead();
				EmpireData data = (EmpireData)serializer1.Deserialize(stream);
				stream.Close();
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
			FileInfo[] textList = Ship_Game.ResourceManager.GetFilesFromDirectory(string.Concat(Ship_Game.ResourceManager.WhichModPath, "/Technology"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(Technology));
			FileInfo[] fileInfoArray = textList;
			for (int i = 0; i < (int)fileInfoArray.Length; i++)
			{
				FileInfo FI = fileInfoArray[i];
				FileStream stream = FI.OpenRead();
				Technology data = (Technology)serializer1.Deserialize(stream);
				stream.Close();
				stream.Dispose();
				if (Ship_Game.ResourceManager.TechTree.ContainsKey(Path.GetFileNameWithoutExtension(FI.Name)))
				{
					Ship_Game.ResourceManager.TechTree[Path.GetFileNameWithoutExtension(FI.Name)] = data;
				}
				else
				{
					Ship_Game.ResourceManager.TechTree.Add(Path.GetFileNameWithoutExtension(FI.Name), data);
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
						if (name != "Thumbs")
						{
							ContentManager content = Game1.Instance.Content;
							string[] whichModPath = new string[] { "../", Ship_Game.ResourceManager.WhichModPath, "/Textures/", FI.Directory.Name, "/", name };
							Texture2D tex = content.Load<Texture2D>(string.Concat(whichModPath));
							Ship_Game.ResourceManager.TextureDict[string.Concat(FI.Directory.Name, "/", name)] = tex;
						}
					}
					else if (Path.GetFileNameWithoutExtension(FI.Name) != "Thumbs")
					{
						Texture2D tex = Game1.Instance.Content.Load<Texture2D>(string.Concat("../", Ship_Game.ResourceManager.WhichModPath, "/Textures/", Path.GetFileNameWithoutExtension(FI.Name)));
						Ship_Game.ResourceManager.TextureDict[string.Concat(FI.Directory.Name, "/", Path.GetFileNameWithoutExtension(FI.Name))] = tex;
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
					string name = Path.GetFileNameWithoutExtension(FI.Name);
					if (name != "Thumbs")
					{
						Texture2D tex = Game1.Instance.Content.Load<Texture2D>(string.Concat("Textures/", Path.GetFileNameWithoutExtension(FI.Name)));
						if (!Ship_Game.ResourceManager.TextureDict.ContainsKey(string.Concat(FI.Directory.Name, "/", name)))
						{
							Ship_Game.ResourceManager.TextureDict[string.Concat(FI.Directory.Name, "/", Path.GetFileNameWithoutExtension(FI.Name))] = tex;
						}
					}
				}
				else
				{
					string name = Path.GetFileNameWithoutExtension(FI.Name);
					if (name != "Thumbs")
					{
						Texture2D tex = Game1.Instance.Content.Load<Texture2D>(string.Concat("Textures/", FI.Directory.Name, "/", name));
						if (!Ship_Game.ResourceManager.TextureDict.ContainsKey(string.Concat(FI.Directory.Name, "/", name)))
						{
							Ship_Game.ResourceManager.TextureDict[string.Concat(FI.Directory.Name, "/", name)] = tex;
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
				Tooltips data = (Tooltips)serializer1.Deserialize(stream);
				stream.Close();
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
				Troop data = (Troop)serializer1.Deserialize(stream);
				stream.Close();
				stream.Dispose();
				if (Ship_Game.ResourceManager.TroopsDict.ContainsKey(Path.GetFileNameWithoutExtension(FI.Name)))
				{
					Ship_Game.ResourceManager.TroopsDict[Path.GetFileNameWithoutExtension(FI.Name)] = data;
				}
				else
				{
					Ship_Game.ResourceManager.TroopsDict.Add(Path.GetFileNameWithoutExtension(FI.Name), data);
				}
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
				Weapon data = (Weapon)serializer1.Deserialize(stream);
				stream.Close();
				stream.Dispose();
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

		public static void Reset()
		{
			DirectoryInfo di = new DirectoryInfo("Content/Mod Models");
			di.Delete(true);
			di.Create();
			di = new DirectoryInfo("Content/ModVideo");
			di.Delete(true);
			di.Create();
			Ship_Game.ResourceManager.HullsDict.Clear();
			Ship_Game.ResourceManager.WeaponsDict.Clear();
			Ship_Game.ResourceManager.TroopsDict.Clear();
			Ship_Game.ResourceManager.BuildingsDict.Clear();
			Ship_Game.ResourceManager.ShipModulesDict.Clear();
			Ship_Game.ResourceManager.FlagTextures.Clear();
			Ship_Game.ResourceManager.TechTree.Clear();
			Ship_Game.ResourceManager.ArtifactsDict.Clear();
			Ship_Game.ResourceManager.ShipsDict.Clear();
		}

		public static void Start()
		{
		}
	}
}