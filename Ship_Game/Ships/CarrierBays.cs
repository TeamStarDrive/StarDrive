using System.Linq;

namespace Ship_Game.Ships
{
    public class CarrierBays  // Created by Fat Bastard in to better deal with hangars
    {
        public ShipModule[] AllHangars { get; private set; }
        public ShipModule[] AllTroopBays { get; }
        public ShipModule[] AllSupplyBays { get; }
        public ShipModule[] AllFighterHangars { get; }
        public bool HasHangars;
        public bool HasSupplyBays;
        public bool HasFighterBays;
        public bool HasTroopBays;

        private CarrierBays(ShipModule[] slots) // this is a constructor, initialize everything in here
        {
            int hangarsCount = slots.Count(module => module.Is(ShipModuleType.Hangar));
            AllHangars       = new ShipModule[hangarsCount];
            int i            = 0;
            foreach (ShipModule module in slots)
            {
                if (module.Is(ShipModuleType.Hangar))
                {
                    AllHangars[i] = module;
                    ++i;
                }
            }
            AllTroopBays      = AllHangars.FilterBy(module => module.IsTroopBay);
            AllSupplyBays     = AllHangars.FilterBy(module => module.IsSupplyBay);
            AllFighterHangars = AllHangars.FilterBy(module => !module.IsTroopBay 
                                                              && !module.IsSupplyBay 
                                                              && module.ModuleType != ShipModuleType.Transporter);
            HasHangars        = AllHangars.Any();
            HasSupplyBays     = AllSupplyBays.Any();
            HasFighterBays    = AllFighterHangars.Any();
            HasTroopBays      = AllTroopBays.Any();
        }

        public static CarrierBays None { get; } = new CarrierBays() // Returns NIL object
        {
            AllHangars = Empty<ShipModule>.Array,
        };

        public static CarrierBays Create(ShipModule[] slots)
        {
            return slots.Any(m => m.ModuleType == ShipModuleType.Hangar) ? new CarrierBays(slots) : None;
        }


        public ShipModule[] AllActiveHangars => AllHangars.FilterBy(module => module.Active);

        public bool HasActiveHangars         => AllActiveHangars.Any(); // FB: this changes dynamically

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

        public HangarInfo GrossHangarStatus // FB: needed to display hangar status to the player
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

        public struct HangarInfo
        {
            public int Launched;
            public int Refitting;
            public int ReadyToLaunch;
        }
    }
}
