using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SgMotion;
using SgMotion.Controllers;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Gameplay
{
    public sealed partial class Ship
    {
        // The only way to spawn instances of Ship is to call Ship.CreateShip... overloads
        private Ship()
        {
        }

        public bool CreateModuleSlotsFromData(ModuleSlotData[] templateSlots, bool fromSave)
        {
            int count = 0;
            for (int i = 0; i < templateSlots.Length; ++i)
            {
                string uid = templateSlots[i].InstalledModuleUID;
                if (uid == "Dummy") // @note Backwards savegame compatibility for ship designs, dummy modules are deprecated
                    continue;
                if (!ResourceManager.ShipModules.ContainsKey(uid))
                {
                    Log.Warning($"Failed to load ship '{Name}' due to invalid Module '{uid}'!");
                    return false;
                }
                ++count;
            }

            ModuleSlotList = new ShipModule[count];

            count = 0;
            for (int i = 0; i < templateSlots.Length; ++i)
            {
                ModuleSlotData slotData = templateSlots[i];
                string uid = slotData.InstalledModuleUID;
                if (uid == "Dummy")
                    continue;
                ShipModule module = ShipModule.Create(uid, this, slotData.Position, slotData.Facing);
                if (fromSave)
                {
                    module.Health      = slotData.Health;
                    module.ShieldPower = slotData.ShieldPower;
                }
                module.HangarShipGuid = slotData.HangarshipGuid;
                module.hangarShipUID  = slotData.SlotOptions;
                ModuleSlotList[count++] = module;
            }

            CreateModuleGrid();
            return true;
        }

        public static Ship CreateShipFromShipData(ShipData data, bool fromSave)
        {
            var ship = new Ship
            {
                Position   = new Vector2(200f, 200f),
                Name       = data.Name,
                Level      = data.Level,
                experience = data.experience,
                shipData   = data,
                ModelPath  = data.ModelPath
            };

            if (!ship.CreateModuleSlotsFromData(data.ModuleSlots, fromSave))
                return null;

            foreach (ShipToolScreen.ThrusterZone thrusterZone in data.ThrusterList)
            {
                ship.ThrusterList.Add(new Thruster
                {
                    tscale = thrusterZone.Scale,
                    XMLPos = thrusterZone.Position,
                    Parent = ship
                });
            }
            return ship;
        }

        // Added by RedFox - Debug, Hangar Ship, and Platform creation
        public static Ship CreateShipAtPoint(string shipName, Empire owner, Vector2 position)
        {
            if (!ResourceManager.ShipsDict.TryGetValue(shipName, out Ship template))
            {
                var stackTrace = new Exception();
                MessageBox.Show($"Failed to create new ship '{shipName}'. This is a bug caused by mismatched or missing ship designs\n\n{stackTrace.StackTrace}",
                                 "Ship spawn failed!", MessageBoxButtons.OK);
                return null;
            }

            var ship = new Ship
            {
                shipData     = template.shipData,
                Name         = template.Name,
                BaseStrength = template.BaseStrength,
                BaseCanWarp  = template.BaseCanWarp,
                loyalty      = owner,
                Position     = position
            };

            if (!ship.CreateModuleSlotsFromData(template.shipData.ModuleSlots, fromSave: false))
            {
                Log.Error($"Unexpected failure while spawning ship '{shipName}'. Is the module list corrupted??");
                return null; // return and crash again...
            }

            ship.ThrusterList.Capacity = template.ThrusterList.Count;
            foreach (Thruster t in template.ThrusterList)
                ship.AddThruster(t);

            if (!template.shipData.Animated)
            {
                ship.SetSO(new SceneObject(ResourceManager.GetModel(template.ModelPath).Meshes[0])
                { ObjectType = ObjectType.Dynamic });
            }
            else
            {
                SkinnedModel model = ResourceManager.GetSkinnedModel(template.ModelPath);
                ship.SetSO(new SceneObject(model.Model) { ObjectType = ObjectType.Dynamic });
                ship.SetAnimationController(new AnimationController(model.SkeletonBones), model);
            }

            // Added by McShooterz: add automatic ship naming
            if (GlobalStats.HasMod)
                ship.VanityName = ResourceManager.ShipNames.GetName(owner.data.Traits.ShipType, ship.shipData.Role);

            if (ship.shipData.Role == ShipData.RoleName.fighter)
                ship.Level += owner.data.BonusFighterLevels;

            // during new game creation, universeScreen can still be null
            if (Empire.Universe != null && Empire.Universe.GameDifficulty > UniverseData.GameDifficulty.Normal)
                ship.Level += (int)Empire.Universe.GameDifficulty;

            ship.Initialize();

            var so = ship.GetSO();
            so.World = Matrix.CreateTranslation(new Vector3(ship.Center, 0f));

            var screenManager = Empire.Universe?.ScreenManager ?? ResourceManager.ScreenManager;
            lock (GlobalStats.ObjectManagerLocker)
            {
                screenManager.inter.ObjectManager.Submit(so);
            }

            GameContentManager content = ResourceManager.ContentManager;
            var thrustCylinder = content.Load<Model>("Effects/ThrustCylinderB");
            var noiseVolume    = content.Load<Texture3D>("Effects/NoiseVolume");
            var thrusterEffect = content.Load<Effect>("Effects/Thrust");
            foreach (Thruster t in ship.GetTList())
            {
                t.load_and_assign_effects(content, thrustCylinder, noiseVolume, thrusterEffect);
                t.InitializeForViewing();
            }

            owner.AddShip(ship);
            return ship;
        }

        //@bug #1002  cant add a ship to a system in readlock. 
        public static Ship CreateShipAt(string shipName, Empire owner, Planet p, Vector2 deltaPos, bool doOrbit)
        {
            Ship ship = CreateShipAtPoint(shipName, owner, p.Position + deltaPos);
            if (doOrbit)
                ship.DoOrbit(p);

            ship.SetSystem(p.system);
            return ship;
        }

        // Refactored by RedFox - Normal Shipyard ship creation
        public static Ship CreateShipAt(string shipName, Empire owner, Planet p, bool doOrbit)
        {
            return CreateShipAt(shipName, owner, p, Vector2.Zero, doOrbit);
        }

        // Added by McShooterz: for refit to keep name
        // Refactored by RedFox
        public static Ship CreateShipAt(string shipName, Empire owner, Planet p, bool doOrbit, string refitName, int refitLevel)
        {
            Ship ship = CreateShipAt(shipName, owner, p, doOrbit);

            // Added by McShooterz: add automatic ship naming
            ship.VanityName = refitName;
            ship.Level      = refitLevel;
            return ship;
        }

        // Hangar Ship Creation
        public static Ship CreateShipFromHangar(string key, Empire owner, Vector2 p, Ship parent)
        {
            Ship ship = CreateShipAtPoint(key, owner, p);
            if (ship == null) return null;
            ship.Mothership = parent;
            ship.Velocity = parent.Velocity;
            return ship;
        }

        public static Ship CreateTroopShipAtPoint(string shipName, Empire owner, Vector2 point, Troop troop)
        {
            Ship ship = CreateShipAtPoint(shipName, owner, point);
            ship.VanityName = troop.Name;
            ship.TroopList.Add(ResourceManager.CopyTroop(troop));
            if (ship.shipData.Role == ShipData.RoleName.troop)
                ship.shipData.ShipCategory = ShipData.Category.Combat;
            return ship;
        }
    }
}
