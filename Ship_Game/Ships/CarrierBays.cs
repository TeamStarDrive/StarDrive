using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Ships
{
    public class CarrierBays
    {
        public ShipModule[] AllHangars { get; private set; }
        //public ShipModule[] AllFigherBays { get; private set; }
        //public ShipModule[] AllTroopBays { get; private set; }
        //private Array<ShipModule> AllHangars = new Array<ShipModule>();


        public static CarrierBays None { get; } = new CarrierBays
        {
            AllHangars = Empty<ShipModule>.Array,
        };

        //FB:  this is not the best coding, im not familiar with syntax or how to code this
        public CarrierBays Create(Ship ship, ShipModule[] moduleSlotList) 
        {
            if (!ship.HasHangars)
                return None;
            CarrierBays carrierBays = new CarrierBays();
            InitCarrier(moduleSlotList);
            return carrierBays; // FB: this doesnt work and i dont know why :( probably my lack of experience
        }

        private void InitCarrier(ShipModule[] moduleSlotList)
        {
            int hangarsNum = moduleSlotList.Count(module => module.Is(ShipModuleType.Hangar));
            AllHangars = new ShipModule[hangarsNum];
            int i = 0;
            foreach (ShipModule module in moduleSlotList)
            {
                if (module.Is(ShipModuleType.Hangar))
                {
                    AllHangars[i] = module;
                    ++i;
                }
            }
        }

        public bool HasHangars       => AllHangars.Any();
        public bool HasActiveHangars => AllActiveHangars.Any();
        public bool HasSupplyBays    => AllSupplyBays.Any();
        public bool HasFighterBays   => AllFighterHangars.Any();
        public bool HasTroopBays     => AllTroopBays.Any();

        public ShipModule[] AllTroopBays => AllHangars.FilterBy(module => module.IsTroopBay);

        public ShipModule[] AllSupplyBays => AllHangars.FilterBy(module => module.IsSupplyBay);

        public ShipModule[] AllActiveHangars => AllHangars.FilterBy(module => module.Active);

        public ShipModule[] AllFighterHangars
        {
            get
            {
                return AllHangars.FilterBy(module => !module.IsTroopBay && !module.IsSupplyBay && module.ModuleType != ShipModuleType.Transporter);
            }
        }

        public int AvailableAssaultShuttles
        {
            get
            {
                return AllTroopBays.Count(hangar => hangar.Active && hangar.hangarTimer <= 0 && hangar.GetHangarShip() == null);
            }
        }

        public int LaunchedAssaultShuttles
        {
            get
            {
                int i = 0;
                foreach (ShipModule hangar in AllTroopBays)
                {
                    Ship hangarship = hangar.GetHangarShip();
                    if (hangarship != null && hangarship.Active)
                        i += 1;
                }
                return i;
            }
        }

        public struct HangarInfo
        {
            public int Launched;
            public int Refitting;
            public int ReadyToLaunch;
        }

        public HangarInfo GrossHangarStatus
        {
            get
            {
                var info = new HangarInfo();
                foreach (ShipModule hangar in AllFighterHangars)
                {
                    if (hangar.FighterOut) ++info.Launched;
                    else if (hangar.hangarTimer > 0) ++info.Refitting;
                    else if (hangar.Active) ++info.ReadyToLaunch;
                }
                return info;
            }
        }
    }
}
