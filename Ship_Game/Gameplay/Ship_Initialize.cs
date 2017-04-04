using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Ship_Game.Gameplay
{
    public sealed partial class Ship
    {

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
                    Log.Warning($"Failed to load ship {Name} due to invalid Module {uid}!");
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
    }
}
