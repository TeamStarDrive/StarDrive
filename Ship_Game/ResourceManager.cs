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
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Fasterflect;
using Microsoft.Xna.Framework.Media;

namespace Ship_Game
{
    public sealed class ResourceManager
    {
        public static Dictionary<string, Texture2D> TextureDict          = new Dictionary<string, Texture2D>();
        public static XmlSerializer WeaponSerializer                     = new XmlSerializer(typeof(Weapon));
        public static Dictionary<string, Ship> ShipsDict                 = new Dictionary<string, Ship>();
        private static readonly List<Model> RoidsModels                  = new List<Model>();
        private static readonly List<Model> JunkModels                   = new List<Model>();
        public static Dictionary<string, Technology> TechTree            = new Dictionary<string, Technology>(StringComparer.InvariantCultureIgnoreCase);
        public static List<Encounter> Encounters                         = new List<Encounter>();
        public static Dictionary<string, Building> BuildingsDict         = new Dictionary<string, Building>();
        public static Dictionary<string, Good> GoodsDict                 = new Dictionary<string, Good>();
        public static Dictionary<string, Weapon> WeaponsDict             = new Dictionary<string, Weapon>();
        public static Dictionary<string, ShipModule> ShipModulesDict     = new Dictionary<string, ShipModule>();
        private static readonly List<ToolTip> ToolTips                   = new List<ToolTip>();
        public static Dictionary<string, Texture2D> ProjTextDict         = new Dictionary<string, Texture2D>();
        public static Dictionary<string, ModelMesh> ProjectileMeshDict   = new Dictionary<string, ModelMesh>();
        public static Dictionary<string, Model> ProjectileModelDict      = new Dictionary<string, Model>();
        public static bool Initialized                                   = false;
        public static string WhichModPath                                = "";

        public static List<RandomItem> RandomItemsList                   = new List<RandomItem>();
        public static Dictionary<string, Troop> TroopsDict               = new Dictionary<string, Troop>();
        public static Dictionary<string, DiplomacyDialog> DDDict         = new Dictionary<string, DiplomacyDialog>();
        public static Dictionary<string, LocalizationFile> LanguageDict  = new Dictionary<string, LocalizationFile>();

        public static LocalizationFile LanguageFile                      = new LocalizationFile();
        public static Dictionary<string, Artifact> ArtifactsDict         = new Dictionary<string, Artifact>();
        public static Dictionary<string, ExplorationEvent> EventsDict    = new Dictionary<string, ExplorationEvent>();
        public static List<Texture2D> BigNebulas                         = new List<Texture2D>();
        public static List<Texture2D> MedNebulas                         = new List<Texture2D>();
        public static List<Texture2D> SmallNebulas                       = new List<Texture2D>();
        public static List<Texture2D> SmallStars                         = new List<Texture2D>();
        public static List<Texture2D> MediumStars                        = new List<Texture2D>();
        public static List<Texture2D> LargeStars                         = new List<Texture2D>();
        private static RacialTraits rt                                   = new RacialTraits();
        public static List<EmpireData> Empires                           = new List<EmpireData>();
        public static XmlSerializer HeaderSerializer                     = new XmlSerializer(typeof(HeaderData));
        public static XmlSerializer ModSerializer                        = new XmlSerializer(typeof(ModInformation));
        public static Dictionary<string, Model> ModelDict                = new Dictionary<string, Model>();

        public static Dictionary<string, ShipData> HullsDict             = new Dictionary<string, ShipData>(StringComparer.InvariantCultureIgnoreCase);
        public static UniverseScreen universeScreen;
        public static List<KeyValuePair<string, Texture2D>> FlagTextures = new List<KeyValuePair<string, Texture2D>>();
        public static Dictionary<string, SoundEffect> SoundEffectDict    = new Dictionary<string, SoundEffect>();

        // Added by McShooterz
        public static HostileFleets HostileFleets                        = new HostileFleets();
        public static ShipNames ShipNames                                = new ShipNames();
        public static AgentMissionData AgentMissionData                  = new AgentMissionData();
        public static MainMenuShipList MainMenuShipList                  = new MainMenuShipList();
        public static Dictionary<ShipData.RoleName, ShipRole> ShipRoles  = new Dictionary<ShipData.RoleName, ShipRole>();
        public static Dictionary<string, HullBonus> HullBonuses          = new Dictionary<string, HullBonus>();
        public static Dictionary<string, PlanetEdict> PlanetaryEdicts    = new Dictionary<string, PlanetEdict>();
        public static XmlSerializer EconSerializer                       = new XmlSerializer(typeof(EconomicResearchStrategy));
        public static Dictionary<string, EconomicResearchStrategy> EconStrats = new Dictionary<string, EconomicResearchStrategy>();

        public static int OffSet;


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

                if (shipData.HullData != null && shipData.HullData.unLockable)
                {
                    foreach (string str in shipData.HullData.techsNeeded)
                        shipData.techsNeeded.Add(str);
                    shipData.hullUnlockable = true;
                }
                else
                {
                    shipData.allModulesUnlocakable = false;
                    shipData.hullUnlockable = false;
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
                            break;
                        }

                    }
                }

                if (shipData.allModulesUnlocakable)
                    foreach (string techname in shipData.techsNeeded)
                    {
                        shipData.TechScore += (int)TechTree[techname].Cost;
                        x++;
                        if (!(shipData.BaseStrength > 0f))
                            CalculateBaseStrength(kv.Value);
                    }
                else
                {                    
                    shipData.unLockable = false;
                    shipData.techsNeeded.Clear();
                    purge.Add(shipData.Name);
                    shipData.BaseStrength = 0;
                }

            }
            
            System.Diagnostics.Debug.WriteLine("Designs Bad: " + purge.Count + " : ShipDesigns OK : " + x);
            foreach (string purger in purge)
                System.Diagnostics.Debug.WriteLine("These are Designs" + purger);
        }
        public static bool IgnoreLoadingErrors = false;
        // Used for reporting resource loading errors.
        public static void ReportLoadingError(FileInfo info, string where, Exception e)
        {
            if (IgnoreLoadingErrors) return;
            e.Data.Add("Failing File: ", info.FullName);
            e.Data.Add("Fail Info: ", e.InnerException?.Message);
            throw e;
        }
        public static void ReportLoadingError(string fileName, string where, Exception e)
        {
            if (IgnoreLoadingErrors) return;
            e.Data.Add("Failing File: ", fileName);
            e.Data.Add("Fail Info: ", e.InnerException?.Message);
            throw e;
        }

        // All references to Game1.Instance.Content were replaced by this property
        public static ContentManager ContentManager => Game1.Instance.Content;

        public static Troop CopyTroop(Troop t)
        {
            Troop troop = t.Clone();
            troop.StrengthMax = t.StrengthMax > 0 ? t.StrengthMax : t.Strength;
            troop.WhichFrame  = (int)RandomMath.RandomBetween(1, t.num_idle_frames - 1);
            troop.SetOwner(t.GetOwner());
            return troop;
        }

        public static Ship GetShipTemplate(string shipName)
        {
            return ShipsDict[shipName];
        }
        public static string GetShipHull(string shipName)
        {
            return ShipsDict[shipName].GetShipData().Hull;
        }

        // Added by RedFox
        // Debug, Hangar Ship, and Platform creation
        public static Ship CreateShipAtPoint(string shipName, Empire owner, Vector2 position)
        {
            if (!ShipsDict.TryGetValue(shipName, out Ship template))
            {
                Exception stackTrace = new Exception();
                MessageBox.Show($"Failed to create new ship '{shipName}'. "+
                    $"This is a bug caused by mismatched or missing ship designs\n\n{stackTrace.StackTrace}", 
                    "Ship spawn failed!", MessageBoxButtons.OK);
                return null;
            }

            Ship ship = new Ship
            {
                shipData     = template.shipData,
                Name         = template.Name,
                BaseStrength = template.BaseStrength,
                BaseCanWarp  = template.BaseCanWarp,
                loyalty      = owner,
                Position     = position
            };

            if (!template.shipData.Animated)
            {
                ship.SetSO(new SceneObject(GetModel(template.ModelPath).Meshes[0])
                { ObjectType = ObjectType.Dynamic });
            }
            else
            {
                SkinnedModel model = GetSkinnedModel(template.ModelPath);
                ship.SetSO(new SceneObject(model.Model) { ObjectType = ObjectType.Dynamic });
                ship.SetAnimationController(new AnimationController(model.SkeletonBones), model);
            }

            ship.GetTList().Capacity = template.GetTList().Count;
            foreach (Thruster t in template.GetTList())
                ship.AddThruster(t);

            ship.ModuleSlotList.Capacity = template.ModuleSlotList.Count;
            foreach (ModuleSlot slot in template.ModuleSlotList)
            {
                ModuleSlot newSlot = new ModuleSlot();
                newSlot.SetParent(ship);
                newSlot.SlotOptions  = slot.SlotOptions;
                newSlot.Restrictions = slot.Restrictions;
                newSlot.Position     = slot.Position;
                newSlot.facing       = slot.facing;
                newSlot.state        = slot.state;
                newSlot.InstalledModuleUID = slot.InstalledModuleUID;
                ship.ModuleSlotList.Add(newSlot);
            }

            // Added by McShooterz: add automatic ship naming
            if (GlobalStats.ActiveMod != null)
                ship.VanityName = ShipNames.GetName(owner.data.Traits.ShipType, ship.shipData.Role);

            if (ship.shipData.Role == ShipData.RoleName.fighter)
                ship.Level += owner.data.BonusFighterLevels;

            if (universeScreen.GameDifficulty > UniverseData.GameDifficulty.Normal)
                ship.Level += (int)universeScreen.GameDifficulty;

            ship.Initialize();

            var so = ship.GetSO();
            so.World = Matrix.CreateTranslation(new Vector3(ship.Center, 0f));
            lock (GlobalStats.ObjectManagerLocker)
            {
                universeScreen.ScreenManager.inter.ObjectManager.Submit(so);
            }

            var content = universeScreen.ScreenManager.Content;
            var thrustCylinder = content.Load<Model>("Effects/ThrustCylinderB");
            var noiseVolume    = content.Load<Texture3D>("Effects/NoiseVolume");
            foreach (Thruster t in ship.GetTList())
            {
                t.load_and_assign_effects(content, thrustCylinder, noiseVolume, universeScreen.ThrusterEffect);
                t.InitializeForViewing();
            }

            owner.AddShip(ship);
            return ship;
        }

        public static Ship CreateShipAt(string shipName, Empire owner, Planet p, Vector2 deltaPos, bool doOrbit)
        {
            Ship ship = CreateShipAtPoint(shipName, owner, p.Position + deltaPos);
            ship.isInDeepSpace = false; // Planet p implies we're not in deep space
            if (doOrbit)
                ship.DoOrbit(p);

            ship.SetSystem(p.system);
            p.system.ShipList.Add(ship);
            p.system.spatialManager.CollidableObjects.Add(ship);
            return ship;
        }

        // Refactored by RedFox
        public static Ship CreateShipAt(string shipName, Empire owner, Planet p, bool doOrbit)   //Normal Shipyard ship creation
        {
            return CreateShipAt(shipName, owner, p, Vector2.Zero, doOrbit);
        }

        // Added by McShooterz: for refit to keep name
        // Refactored by RedFox
        public static Ship CreateShipAt(string shipName, Empire owner, Planet p, bool doOrbit, string refitName, byte refitLevel)
        {
            Ship ship = CreateShipAt(shipName, owner, p, doOrbit);

            // Added by McShooterz: add automatic ship naming
            ship.VanityName = refitName;
            ship.Level      = refitLevel;
            return ship;
        }

        // unused   -- Called in fleet creation function, which is in turn not used
        public static Ship CreateShipAtPoint(string shipName, Empire owner, Vector2 p, float facing)
        {
            Ship ship = CreateShipAtPoint(shipName, owner, p);
            ship.Rotation = facing;
            return ship;
        }

        public static Ship CreateShipForBattleMode(string shipName, Empire owner, Vector2 p) //Unused... Battle mode, eh?
        {
            Ship ship = CreateShipAtPoint(shipName, owner, p);
            ship.isInDeepSpace = true;
            return ship;
        }

        // Hangar Ship Creation
        public static Ship CreateShipFromHangar(string key, Empire owner, Vector2 p, Ship parent)
        {
            Ship s = CreateShipAtPoint(key, owner, p);
            if (s == null) return null;
            s.Mothership = parent;
            s.Velocity = parent.Velocity;
            return s;
        }


        public static Troop CreateTroop(Troop template, Empire forOwner)
        {
            Troop troop = CopyTroop(template);
            if (forOwner != null)
                troop.Strength += (int)(forOwner.data.Traits.GroundCombatModifier * troop.Strength);
            troop.SetOwner(forOwner);
            return troop;
        }

        public static Ship CreateTroopShipAtPoint(string shipName, Empire owner, Vector2 point, Troop troop)
        {
            Ship ship = CreateShipAtPoint(shipName, owner, point);
            ship.VanityName = troop.Name;
            ship.TroopList.Add(CopyTroop(troop));
            if (ship.shipData.Role == ShipData.RoleName.troop)
                ship.shipData.ShipCategory = ShipData.Category.Combat;
            return ship;
        }

        // Added by RedFox
        public static void DeleteFirstShipFromDir(string dir, string shipName)
        {
            foreach (FileInfo info in Dir.GetFiles(dir))
            {
                // @note ship.Name is always the same as fileNameNoExt 
                //       part of "shipName.xml", so we can skip parsing the XML's
                string fShipName = info.NameNoExt();
                if (fShipName != shipName)
                    continue;

                ShipsDict.Remove(shipName);
                info.Delete();
                return;
            }
        }

        // Refactored by RedFox
        public static void DeleteShip(string shipName)
        {
            DeleteFirstShipFromDir("Content/StarterShips", shipName);
            DeleteFirstShipFromDir("Content/SavedDesigns", shipName);

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            DeleteFirstShipFromDir(appData + "/StarDrive/Saved Designs", shipName);
            DeleteFirstShipFromDir(appData + "/StarDrive/WIP", shipName);

            foreach (Empire e in EmpireManager.EmpireList)
                e.UpdateShipsWeCanBuild();
        }

        public static Building GetBuilding(string whichBuilding)
        {
            Building template = BuildingsDict[whichBuilding];
            Building newB = template.Clone();
            newB.Cost *= UniverseScreen.GamePaceStatic;

            // comp fix to ensure functionality of vanilla buildings
            if (newB.Name == "Outpost" || newB.Name =="Capital City")
            {
                // @todo What is going on here? Is this correct?
                if (!newB.IsProjector && !(newB.ProjectorRange > 0f))
                {
                    newB.ProjectorRange = Empire.ProjectorRadius;
                    newB.IsProjector    = true;
                }
                if (!newB.IsSensor && !(newB.SensorRange > 0.0f))
                {
                    newB.SensorRange = 20000.0f;
                    newB.IsSensor = true;
                }
            }
            if (template.isWeapon)
            {
                newB.theWeapon = GetWeapon(template.Weapon);
            }
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
            catch (ContentLoadException) { if (throwOnFailure) throw; }
            return null;
        }

        // Refactored by RedFox
        public static Model GetModel(string path, bool throwOnFailure = false)
        {
            Model item;

            // try to get cached value
            lock (ModelDict) if (ModelDict.TryGetValue(path, out item)) return item;

            if (GlobalStats.ActiveMod != null)
            {
                string modModel = WhichModPath + "/" + path;
                if (File.Exists(modModel))
                    item = LoadModel(modModel, false);
            }

            if (item == null)
                item = LoadModel(path, throwOnFailure);

            // stick it into Model cache
            lock (ModelDict) ModelDict.Add(path, item);
            return item;
        }

        public static float GetModuleCost(string uid)
        {
            ShipModule template = ShipModulesDict[uid];
            return template.Cost;
        }
        public static ShipModule GetModule(string uid)
        {
            ShipModule template = ShipModulesDict[uid];
            ShipModule module = new ShipModule
            {
                // All complex properties here have been replaced by this single reference to 'ShipModule_Advanced' which now contains them all - Gretman
                Advanced             = template.Advanced,
                DescriptionIndex     = template.DescriptionIndex,
                FieldOfFire          = template.FieldOfFire,
                hangarShipUID        = template.hangarShipUID,
                hangarTimer          = template.hangarTimer,
                Health               = template.HealthMax,
                HealthMax            = template.HealthMax,
                isWeapon             = template.isWeapon,
                Mass                 = template.Mass,
                ModuleType           = template.ModuleType,
                NameIndex            = template.NameIndex,
                OrdinanceCapacity    = template.OrdinanceCapacity,
                shield_power         = template.shield_power_max,    //Hmmm... This one is strange -Gretman
                UID                  = template.UID,
                XSIZE                = template.XSIZE,
                YSIZE                = template.YSIZE,
                PermittedHangarRoles = template.PermittedHangarRoles,
                shieldsOff           = template.shieldsOff
            };
            // @todo This might need to be updated with latest ModuleType logic?
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
            return module;
        }

        // Refactored by RedFox
        public static RacialTraits GetRaceTraits()
        {
            // Added by McShooterz: mod folder support for RacialTraits folder
            string modTraits = WhichModPath + "/RacialTraits";
            string traitsDir = Directory.Exists(modTraits) ? modTraits : "Content/RacialTraits";

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

        public static SkinnedModel GetSkinnedModel(string path)
        {
            return ContentManager.Load<SkinnedModel>(path);
        }

        // Refactored by RedFox, gets a new weapon instance based on weapon UID
        public static Weapon GetWeapon(string uid)
        {
            Weapon template = WeaponsDict[uid];
            Weapon wep = template.Clone();
            return wep;
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
            foreach (FileInfo info in Dir.GetFiles(WhichModPath + dir, "xml")) {
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
                string nameNoExt = kv.Key.NameNoExt();
                DDDict[nameNoExt] = kv.Value;
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
                string nameNoExt = kv.Key.NameNoExt();
                EventsDict[nameNoExt] = kv.Value;
            }
        }

        private static void LoadFlagTextures() // Refactored by RedFox
        {
            foreach (FileInfo info in Dir.GetFiles(WhichModPath + "/Flags", "xnb"))
            {
                string nameNoExt = info.NameNoExt();
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
                good.UID = string.Intern(kv.Key.NameNoExt());
                GoodsDict[good.UID] = good;
            }
        }

        public static void LoadHardcoreTechTree() // Refactored by RedFox
        {
            TechTree.Clear();
            foreach (var kv in LoadEntitiesWithInfo<Technology>("/Technology_HardCore", "LoadTechnologyHardcore"))
            {
                string nameNoExt = kv.Key.NameNoExt();
                TechTree[nameNoExt] = kv.Value;
            }
        }

        public static bool TryGetHull(string shipHull, out ShipData hullData)
        {
            return HullsDict.TryGetValue(shipHull, out hullData);
        }

        public static List<ShipData> LoadHullData() // Refactored by RedFox
        {
            var retList = new List<ShipData>();
            Parallel.ForEach(Dir.GetFiles(WhichModPath + "/Hulls", "xml"), info =>
            {
                try
                {
                    string dirName = info.Directory?.Name ?? "";
                    ShipData shipData = ShipData.Parse(info);
                    shipData.Hull      = string.Intern(dirName + "/" + shipData.Hull);
                    shipData.ShipStyle = string.Intern(dirName);

                    lock (retList)
                    {
                        HullsDict[shipData.Hull] = shipData;
                        retList.Add(shipData);
                    }
                }
                catch (Exception e)
                {
                    ReportLoadingError(info, "LoadHullData", e);
                }
            });
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
            LoadAsteroids();
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
            LoadEconomicResearchStrats();
            //Ship_Game.ResourceManager.MarkShipDesignsUnlockable();
            
        }

        public static Model GetJunkModel(int idx)
        {
            return JunkModels[idx];
        }
        public static int NumJunkModels => JunkModels.Count;

        public static int NumAsteroidModels => RoidsModels.Count;
        public static Model GetAsteroidModel(int roidId)
        {
            return RoidsModels[roidId];
        }

        // loads models from a model folder that match "modelPrefixNNN.xnb" format, where N is an integer
        private static void LoadNumberedModels(List<Model> models, string modelFolder, string modelPrefix, string id)
        {
            var files = Dir.GetFiles(WhichModPath + modelFolder, "xnb");
            if (files.Length == 0)
                return;

            string loaddir = "../"+WhichModPath+modelFolder;
            models.Clear();
            foreach (FileInfo info in files)
            {
                string nameNoExt = info.NameNoExt();
                try
                {
                    // only accept "prefixNN" format, because there are a bunch of textures in the asteroids folder
                    if (!nameNoExt.StartsWith(modelPrefix) || !int.TryParse(nameNoExt.Substring(modelPrefix.Length), out int _))
                        continue;
                    models.Add(ContentManager.Load<Model>(loaddir + nameNoExt));
                }
                catch (Exception e)
                {
                    ReportLoadingError(info, id, e);
                }
            }
        }

        private static void LoadJunk() // Refactored by RedFox
        {
            LoadNumberedModels(JunkModels, "/Model/SpaceJunk/", "spacejunk", "LoadJunk");
        }
        private static void LoadAsteroids()
        {
            LoadNumberedModels(RoidsModels, "/Model/Asteroids/", "asteroid", "LoadAsteroids");
            //LoadNumberedModels(RoidsModels, "/Model/SpaceJunk/", "spacejunk", "LoadJunk");
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
            foreach (FileInfo info in Dir.GetFilesNoThumbs("Content/LargeStars"))
            {
                try
                {
                    string nameNoExt = info.NameNoExt();
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
            foreach (FileInfo info in Dir.GetFilesNoThumbs("Content/MediumStars"))
            {
                try
                {
                    string nameNoExt = info.NameNoExt();
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
            LoadBlackboxSpecific();
            LoadShipRoles();
            LoadPlanetEdicts();
            LoadEconomicResearchStrats();

            Localizer.cleanLocalizer();
            OffSet = 0;
        }


        // Refactored by RedFox
        private static void LoadNebulas()
        {
            foreach (FileInfo info in Dir.GetFilesNoThumbs("Content/Nebulas"))
            {
                string nameNoExt = info.NameNoExt();
                Texture2D tex = ContentManager.Load<Texture2D>("Nebulas/" + nameNoExt);
                if      (tex.Width == 2048) BigNebulas.Add(tex);
                else if (tex.Width == 1024) MedNebulas.Add(tex);
                else                        SmallNebulas.Add(tex);
            }
        }

        // Refactored by RedFox
        private static void LoadProjectileMesh(string projectileDir, string nameNoExt)
        {
            string fullPath = projectileDir + nameNoExt;
            try
            {
                Model projModel = ContentManager.Load<Model>(projectileDir + nameNoExt);
                ProjectileMeshDict[nameNoExt]  = projModel.Meshes[0];
                ProjectileModelDict[nameNoExt] = projModel;
            }
            catch (Exception e)
            {
                ReportLoadingError(fullPath, "LoadProjectile", e);
            }
        }

        private static void LoadProjectileMeshes()
        {
            const string projectileDir = "Model/Projectiles/";
            LoadProjectileMesh(projectileDir, "projLong");
            LoadProjectileMesh(projectileDir, "projTear");
            LoadProjectileMesh(projectileDir, "projBall");
            LoadProjectileMesh(projectileDir, "torpedo");
            LoadProjectileMesh(projectileDir, "missile");
            LoadProjectileMesh(projectileDir, "spacemine");
        }
        
        private static void LoadModsProjectileMeshes() // Added by McShooterz: Load projectile models for mods
        {
            string projectileDir = "../" + WhichModPath + "/Model/Projectiles/";
            foreach (FileInfo info in Dir.GetFilesNoSub(WhichModPath + "/Model/Projectiles", "xnb"))
                LoadProjectileMesh(projectileDir, info.NameNoExt());
        }


        private static void LoadProjTexts()
        {
            string root = "../"+WhichModPath+"/Model/Projectiles/textures/";
            foreach (FileInfo info in Dir.GetFiles(WhichModPath + "/Model/Projectiles/textures", "xnb"))
            {
                string nameNoExt = info.NameNoExt();
                Texture2D tex = ContentManager.Load<Texture2D>(root + nameNoExt);
                ProjTextDict[nameNoExt] = tex;
            }
        }


        private static void LoadRandomItems()
        {
            string dir = WhichModPath + "/RandomStuff";
            if (!Directory.Exists(dir))
                return;

            RandomItemsList.Clear();
            foreach (var kv in LoadEntitiesWithInfo<RandomItem>(dir, "LoadRandomItems"))
                RandomItemsList.Add(kv.Value);
        }

        private static void LoadShipModules()
        {
            foreach (var kv in LoadEntitiesWithInfo<ShipModule_Deserialize>("/ShipModules", "LoadShipModules"))
            {
                // Added by gremlin support techlevel disabled folder.
                if (kv.Key.DirectoryName.IndexOf("disabled", StringComparison.OrdinalIgnoreCase) > 0)
                    continue;
                ShipModule_Deserialize data = kv.Value;

                if (Localizer.LocalizerDict.ContainsKey(data.DescriptionIndex + OffSet))
                {
                    data.DescriptionIndex += (ushort)OffSet;
                    Localizer.used[data.DescriptionIndex] = true;
                }
                if (Localizer.LocalizerDict.ContainsKey(data.NameIndex + OffSet))
                {
                    data.NameIndex += (ushort)OffSet;
                    Localizer.used[data.NameIndex] = true;
                }
                
                data.UID = string.Intern(kv.Key.NameNoExt());
                data.IconTexturePath = string.Intern(data.IconTexturePath);
                if (data.WeaponType != null)
                    data.WeaponType = string.Intern(data.WeaponType);

                if (data.IsCommandModule  && data.TargetTracking == 0 && data.FixedTracking == 0)
                {
                    data.TargetTracking = Convert.ToSByte((data.XSIZE*data.YSIZE) / 3);
                }

            #if DEBUG
                if (ShipModulesDict.ContainsKey(data.UID))
                    System.Diagnostics.Debug.WriteLine("ShipModule UID already found. Conflicting name:  {0}", data.UID);
            #endif
                ShipModulesDict[data.UID] = data.ConvertToShipModule();
            }

            foreach (var entry in ShipModulesDict)
                entry.Value.SetAttributesNoParent();
        }

        private static List<Ship> LoadShipsFromDirectory(string dir)
        {
            var ships = new List<Ship>();
            Parallel.ForEach(Dir.GetFiles(dir, "xml"), info => {
            //foreach (var info in Dir.GetFiles(dir, "xml")) { 
                //added by gremlin support techlevel disabled folder.
                if (info.DirectoryName.IndexOf("disabled", StringComparison.OrdinalIgnoreCase) != -1)
                    return; // continue PFor

                try
                {
                    ShipData shipData = ShipData.Parse(info);
                    //ShipData shipData = ShipDataSerializer.Deserialize<ShipData>(info);
                    if (shipData.Role == ShipData.RoleName.disabled)
                        return; // continue PFor

                    Ship newShip = Ship.CreateShipFromShipData(shipData);
                    newShip.SetShipData(shipData);
                    if (!newShip.InitForLoad())
                        return; // continue PFor

                    newShip.InitializeStatus();

                    lock (ships)
                    {
                        ShipsDict[shipData.Name] = newShip;
                        ships.Add(newShip);
                    }
                }
                catch (Exception e)
                {
                    ReportLoadingError(info, "LoadShips", e);
                }
            });

            return ships;
        }

        // Refactored by RedFox
        // This is a hotpath during loading and ~50% of time is spent here
        public static void LoadShips()
        {
            ShipsDict.Clear();

            string starterShips = WhichModPath + "/StarterShips/";
            starterShips = Directory.Exists(starterShips) ? starterShips : "Content/StarterShips/";

            foreach (Ship ship in LoadShipsFromDirectory(starterShips))
                ship.reserved = true;

            foreach (Ship ship in LoadShipsFromDirectory(WhichModPath + "/SavedDesigns"))
                ship.reserved = true;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            foreach (Ship ship in LoadShipsFromDirectory(appData + "/StarDrive/Saved Designs"))
                ship.IsPlayerDesign = true;

            if (GlobalStats.ActiveMod != null)
            {
                foreach (Ship ship in LoadShipsFromDirectory("Mods/" + GlobalStats.ActiveMod.ModPath + "/ShipDesigns/"))
                {
                    ship.reserved = true;
                    ship.IsPlayerDesign = true;
                }
            }

            // Added by gremlin : Base strength Calculator
            foreach (var entry in ShipsDict)
            {
                CalculateBaseStrength(entry.Value);
            }
        }

        // @todo Move this into Ship class and autocalculate during ship instance init
        public static float CalculateBaseStrength(Ship ship)
        {
            float offense = 0f;
            float defense = 0f;
            bool fighters = false;
            bool weapons = false;

            foreach (ModuleSlot slot in ship.ModuleSlotList.Where(dummy => dummy.InstalledModuleUID != "Dummy"))
            {
                ShipModule module = ShipModulesDict[slot.InstalledModuleUID];
                weapons  |=  module.InstalledWeapon != null;
                fighters |=  module.hangarShipUID   != null && !module.IsSupplyBay && !module.IsTroopBay;

                offense += CalculateModuleOffense(module);
                defense += CalculateModuleDefense(module, ship.Size);

                if (ShipModulesDict[module.UID].WarpThrust > 0)
                    ship.BaseCanWarp = true;
            }

            if (!fighters && !weapons) offense = 0f;
            if (defense > offense) defense = offense;

            return ship.BaseStrength = ship.shipData.BaseStrength = offense + defense;
        }

        // @todo Move this to ShipModule class
        public static float CalculateModuleOffenseDefense(ShipModule module, int slotCount)
        {
            return CalculateModuleDefense(module, slotCount) + CalculateModuleOffense(module);
        }

        // @todo Move this to ShipModule class
        public static float CalculateModuleDefense(ShipModule module, int slotCount)
        {
            if (slotCount <= 0)
                return 0f;

            float def = 0f;
            def += module.shield_power_max * ((module.shield_radius * .05f) / slotCount);
            //(module.shield_power_max+  module.shield_radius +module.shield_recharge_rate) / slotCount ;
            def += module.HealthMax * ((module.ModuleType == ShipModuleType.Armor ? (module.XSIZE) : 1f) / (slotCount * 4));
            return def;
        }

        // @todo Move this to ShipModule class
        public static float CalculateModuleOffense(ShipModule module)
        {
            float off = 0f;
            if (module.InstalledWeapon != null)
            {
                //weapons = true;
                Weapon w = module.InstalledWeapon;

                //Doctor: The 25% penalty to explosive weapons was presumably to note that not all the damage is applied to a single module - this isn't really weaker overall, though
                //and unfairly penalises weapons with explosive damage and makes them appear falsely weaker.
                off += (!w.isBeam ? (w.DamageAmount * w.SalvoCount) * (1f / w.fireDelay) : w.DamageAmount * 18f);

                //Doctor: Guided weapons attract better offensive rating than unguided - more likely to hit. Setting at flat 25% currently.
                if (w.Tag_Guided)
                    off *= 1.25f;

                //Doctor: Higher range on a weapon attracts a small bonus to offensive rating. E.g. a range 2000 weapon gets 5% uplift vs a 5000 range weapon 12.5% uplift. 
                off *= (1 + (w.Range / 40000));

                //Doctor: Here follows multipliers which modify the perceived offensive value of weapons based on any modifiers they may have against armour and shields
                //Previously if e.g. a rapid-fire cannon only did 20% damage to armour, it could have amuch higher off rating than a railgun that had less technical DPS but did double armour damage.
                if (w.EffectVsArmor < 1)
                {
                    if (w.EffectVsArmor > 0.75f)      off *= 0.9f;
                    else if (w.EffectVsArmor > 0.5f)  off *= 0.85f;
                    else if (w.EffectVsArmor > 0.25f) off *= 0.8f;
                    else                              off *= 0.75f;
                }
                if (w.EffectVsArmor > 1)
                {
                    if (w.EffectVsArmor > 2.0f)      off *= 1.5f;
                    else if (w.EffectVsArmor > 1.5f) off *= 1.3f;
                    else                             off *= 1.1f;
                }
                if (w.EffectVSShields < 1)
                {
                    if (w.EffectVSShields > 0.75f)      off *= 0.9f;
                    else if (w.EffectVSShields > 0.5f)  off *= 0.85f;
                    else if (w.EffectVSShields > 0.25f) off *= 0.8f;
                    else                                off *= 0.75f;
                }
                if (w.EffectVSShields > 1)
                {
                    if (w.EffectVSShields > 2f)        off *= 1.5f;
                    else if (w.EffectVSShields > 1.5f) off *= 1.3f;
                    else                               off *= 1.1f;
                }

                //Doctor: If there are manual XML override modifiers to a weapon for manual balancing, apply them.
                off *= w.OffPowerMod;

                if (off > 0f && (w.TruePD || w.Range < 1000))
                {
                    float range = 0f;
                    if (w.Range < 1000)
                        range = (1000f - w.Range) * .01f;
                    off /= (2 + range);
                }
                if (w.EMPDamage > 0) off += w.EMPDamage * (1f / w.fireDelay) * .2f;
            }
            if (module.hangarShipUID != null && !module.IsSupplyBay && !module.IsTroopBay)
            {
                if (ShipsDict.TryGetValue(module.hangarShipUID, out Ship hangarShip))
                {
                    off += (hangarShip.BaseStrength > 0f) ? hangarShip.BaseStrength : CalculateBaseStrength(hangarShip);
                }
                else off += 100f;
            }
            return off;
        }

        private static void LoadSmallStars()
        {
            foreach (FileInfo info in Dir.GetFiles("Content/SmallStars", "xnb"))
            {
                Texture2D tex = ContentManager.Load<Texture2D>("SmallStars/" + info.NameNoExt());
                SmallStars.Add(tex);
            }
        }

        public static void LoadSubsetEmpires()
        {
            foreach (var data in LoadEntities<EmpireData>("/Races", "LoadSubsetEmpires"))
            {
                if (data.Faction == 1)
                    Empires.Add(data);
            }
        }

        private static void LoadTechTree()
        {
            if (GlobalStats.ActiveModInfo != null && GlobalStats.ActiveModInfo.clearVanillaTechs)
                TechTree.Clear();

            foreach (var kv in LoadEntitiesWithInfo<Technology>("/Technology", "LoadTechTree"))
            {
                var tech = kv.Value;
                if (Localizer.LocalizerDict.ContainsKey(tech.DescriptionIndex + OffSet))
                {
                    tech.DescriptionIndex += OffSet;
                    Localizer.used[tech.DescriptionIndex] = true;
                }
                if (Localizer.LocalizerDict.ContainsKey(tech.NameIndex + OffSet))
                {
                    tech.NameIndex += OffSet;
                    Localizer.used[tech.NameIndex] = true;
                }
                foreach (Technology.UnlockedBonus bonus in tech.BonusUnlocked)
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

                tech.UID = string.Intern(kv.Key.NameNoExt());
                TechTree[tech.UID] = tech;

                // categorize uncategorized techs
                if (tech.TechnologyType != TechnologyType.General)
                    continue;

                if (tech.BuildingsUnlocked.Count > 0)
                {
                    foreach (Technology.UnlockedBuilding buildingU in tech.BuildingsUnlocked)
                    {
                        if (!BuildingsDict.TryGetValue(buildingU.Name, out Building building))
                            continue;
                        if (building.AllowInfantry || building.PlanetaryShieldStrengthAdded > 0 
                                 || building.CombatStrength > 0 || building.isWeapon 
                                 || building.Strength > 0  || building.IsSensor)
                            tech.TechnologyType = TechnologyType.GroundCombat;
                        else if (building.AllowShipBuilding || building.PlusFlatProductionAmount > 0 
                                 || building.PlusProdPerRichness > 0 || building.StorageAdded > 0 
                                 || building.PlusFlatProductionAmount > 0)
                            tech.TechnologyType = TechnologyType.Industry;
                        else if (building.PlusTaxPercentage > 0 || building.CreditsPerColonist > 0)
                            tech.TechnologyType = TechnologyType.Economic;
                        else if (building.PlusFlatResearchAmount > 0 || building.PlusResearchPerColonist > 0)
                            tech.TechnologyType = TechnologyType.Research;
                        else if (building.PlusFoodPerColonist > 0 || building.PlusFlatFoodAmount > 0 
                                 || building.PlusFoodPerColonist > 0 || building.MaxPopIncrease > 0 
                                 || building.PlusFlatPopulation > 0  || building.Name == "Biosspheres" || building.PlusTerraformPoints > 0)
                            tech.TechnologyType = TechnologyType.Colonization;
                    }
                }
                else if (tech.TroopsUnlocked.Count > 0)
                {
                    tech.TechnologyType = TechnologyType.GroundCombat;
                }
                else if (tech.TechnologyType == TechnologyType.General && tech.BonusUnlocked.Count > 0)
                {
                    foreach (Technology.UnlockedBonus bonus in tech.BonusUnlocked)
                    {
                        if (bonus.Type == "SHIPMODULE" || bonus.Type == "HULL")
                            tech.TechnologyType = TechnologyType.ShipGeneral;
                        else if (bonus.Type == "TROOP")
                            tech.TechnologyType = TechnologyType.GroundCombat;
                        else if (bonus.Type == "BUILDING")
                            tech.TechnologyType = TechnologyType.Colonization;
                        else if (bonus.Type == "ADVANCE")
                            tech.TechnologyType = TechnologyType.ShipGeneral;
                    }
                }
                else if (tech.ModulesUnlocked.Count > 0)
                {
                    foreach (Technology.UnlockedMod moduleU in tech.ModulesUnlocked)
                    {
                        if (!ShipModulesDict.TryGetValue(moduleU.ModuleUID, out ShipModule module))
                            continue;

                        if (module.InstalledWeapon != null || module.MaximumHangarShipSize > 0
                            || module.ModuleType == ShipModuleType.Hangar)
                            tech.TechnologyType = TechnologyType.ShipWeapons;
                        else if (module.shield_power > 0 
                                 || module.ModuleType == ShipModuleType.Armor
                                 || module.ModuleType == ShipModuleType.Countermeasure
                                 || module.ModuleType == ShipModuleType.Shield)
                            tech.TechnologyType = TechnologyType.ShipDefense;
                        else
                            tech.TechnologyType = TechnologyType.ShipGeneral;
                    }
                }
                else tech.TechnologyType = TechnologyType.General;

                if (tech.HullsUnlocked.Count > 0)
                {
                    tech.TechnologyType = TechnologyType.ShipHull;
                    foreach (Technology.UnlockedHull hull in tech.HullsUnlocked)
                    {
                        ShipData.RoleName role = HullsDict[hull.Name].Role;
                        if (role == ShipData.RoleName.freighter 
                            || role == ShipData.RoleName.platform
                            || role == ShipData.RoleName.construction 
                            || role == ShipData.RoleName.station)
                            tech.TechnologyType = TechnologyType.Industry;
                    }

                }
            }
        }

        // This method is a hot path during Loading and accounts for ~25% of time spent
        private static void LoadTextures()
        {
            ContentManager content = ContentManager;

            string rootDir = WhichModPath != "Content" ? "../"+WhichModPath+"/Textures/" : "Textures/";
            Parallel.ForEach(Dir.GetFilesNoThumbs(WhichModPath + "/Textures"), info =>
            {
                string nameNoExt = info.NameNoExt();
                string directory = info.Directory?.Name ?? "";
                string loadPath = $"{rootDir}{(directory == "Textures" ? "" : directory + '/')}{nameNoExt}";

                // 90% of this methods time is spent inside content::Load
                Texture2D tex = content.Load<Texture2D>(loadPath);

                lock (TextureDict)
                {
                    TextureDict[directory + "/" + nameNoExt] = tex;
                }
            });
        }

        // Gets a loaded texture using the given abstract texture path
        public static Texture2D GetTexture(string texturePath)
        {
            return TextureDict[texturePath];
        }

        private static void LoadToolTips()
        {
            foreach (var tooltips in LoadEntities<Tooltips>("/Tooltips", "LoadToolTips"))
            {
                ToolTips.Capacity = tooltips.ToolTipsList.Count;
                foreach (ToolTip tip in tooltips.ToolTipsList)
                {
                    int idx = tip.TIP_ID - 1;
                    while (ToolTips.Count <= idx) ToolTips.Add(null); // sparse List
                    ToolTips[idx] = tip;
                }
            }
        }
        public static ToolTip GetToolTip(int tipId)
        {
            return ToolTips[tipId - 1];
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

        private static void LoadEconomicResearchStrats()
        {
            foreach (var strat in LoadEntities<EconomicResearchStrategy>("/EconomicResearchStrategy", "LoadEconResearchStrats"))
                EconStrats[strat.Name] = strat;
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

            foreach (FileInfo info in Dir.GetFilesNoThumbs(WhichModPath + "/SoundEffects"))
            {
                string nameNoExt = info.NameNoExt();
                SoundEffect se = ContentManager.Load<SoundEffect>("..\\" + WhichModPath + "\\SoundEffects\\" + nameNoExt);
                SoundEffectDict[nameNoExt] = se;
            }
        }

        public static void Reset()
        {
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

        public static Video LoadVideo(ContentManager content, string videoPath)
        {
            string vanillaVideo = "Video/" + videoPath;
            string moddedVideo  = WhichModPath + "/" + vanillaVideo;
            string path         = File.Exists(moddedVideo) ? moddedVideo : vanillaVideo;
            return content.Load<Video>(path);
        }
    }
}