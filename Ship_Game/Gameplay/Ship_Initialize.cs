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

        public void CreateModuleSlotsFromData(ModuleSlotData[] templateSlots, bool fromSave)
        {
            int count = 0;
            for (int i = 0; i < templateSlots.Length; ++i)
                if (ShipModule.CanCreate(templateSlots[i].InstalledModuleUID))
                    ++count; // @note Backwards savegame compatibility, dummy modules are deprecated

            ModuleSlotList = new ShipModule[count];

            count = 0;
            for (int i = 0; i < templateSlots.Length; ++i)
            {
                ModuleSlotData slotData = templateSlots[i];
                if (!ShipModule.CanCreate(slotData.InstalledModuleUID))
                    continue;

                ShipModule module = ShipModule.Create(slotData.InstalledModuleUID, this, slotData.Position, slotData.Facing);
                if (fromSave)
                {
                    module.Health      = slotData.Health;
                    module.ShieldPower = slotData.ShieldPower;
                }
                module.HangarShipGuid = slotData.HangarshipGuid;
                module.hangarShipUID  = slotData.SlotOptions;
                ModuleSlotList[count++] = module;
            }
        }

        public static Ship CreateShipFromShipData(ShipData data, bool fromSave)
        {
            var ship = new Ship
            {
                Position = new Vector2(200f, 200f),
                Name = data.Name,
                Level = data.Level,
                experience = data.experience,
                shipData = data,
                ModelPath = data.ModelPath
            };

            ship.CreateModuleSlotsFromData(data.ModuleSlots, fromSave);

            foreach (ShipToolScreen.ThrusterZone thrusterZone in data.ThrusterList)
                ship.ThrusterList.Add(new Thruster
                {
                    tscale = thrusterZone.Scale,
                    XMLPos = thrusterZone.Position,
                    Parent = ship
                });


            return ship;
        }
    }
}
