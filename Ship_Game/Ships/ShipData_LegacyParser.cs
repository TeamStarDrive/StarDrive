using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    // NOTE: public variables are SERIALIZED
    public partial class ShipData
    {
        // Added by RedFox - manual parsing of ShipData, because this is the slowest part
        // in loading, the brunt work is offloaded to C++ and then copied back into C#
        static unsafe ShipData ParseXML(FileInfo info, bool isHullDefinition)
        {
            CShipDataParser* s = null;
            try
            {
                s = CreateShipDataParser(info.FullName); // @note This will never throw
                if (!s->ErrorMessage.Empty)
                {
                    Log.Error($"Ship Load error in {info.FullName} : {s->ErrorMessage.AsString}");
                    throw new InvalidDataException(s->ErrorMessage.AsString);
                }

                // if this design belongs to a specific Mod, then make sure current ModName matches
                string modName = s->ModName.AsString;
                if (modName.NotEmpty() && modName != GlobalStats.ModName)
                    return null; // ignore this design

                var ship = new ShipData
                {
                    Animated       = s->Animated != 0,
                    ShipStyle      = s->ShipStyle.AsInternedOrNull,
                    EventOnDeath   = s->EventOnDeath.AsInternedOrNull,
                    experience     = s->Experience,
                    Level          = s->Level,
                    Name           = s->Name.AsString,
                    ModName        = modName,
                    FixedCost      = s->FixedCost,
                    FixedUpkeep    = s->FixedUpkeep,
                    IsShipyard     = s->IsShipyard != 0,
                    IconPath       = s->IconPath.AsString,
                    Hull           = s->Hull.AsString,
                    ModelPath      = s->ModelPath.AsString,
                    CarrierShip    = s->CarrierShip != 0,
                    BaseStrength   = s->BaseStrength,
                    HullUnlockable = s->HullUnlockable != 0,
                    UnLockable     = s->UnLockable != 0,
                    TechScore      = s->TechScore,
                    IsOrbitalDefense          = s->IsOrbitalDefense != 0,
                    SelectionGraphic          = s->SelectionGraphic.AsString,
                    AllModulesUnlockable     = s->AllModulesUnlockable != 0,
                    MechanicalBoardingDefense = s->MechanicalBoardingDefense
                };
                Enum.TryParse(s->Role.AsString,              out ship.Role);
                Enum.TryParse(s->CombatState.AsString,       out ship.CombatState);
                Enum.TryParse(s->ShipCategory.AsString,      out ship.ShipCategory);
                Enum.TryParse(s->HangarDesignation.AsString, out ship.HangarDesignation);
                Enum.TryParse(s->DefaultAIState.AsString,    out ship.DefaultAIState);

                // @todo Remove SDNative.ModuleSlot conversion
                // @todo Optimize CModuleSlot -- we don't need string data for everything
                //       GUID should be byte[16]
                //       Orientation should be int
                //       
                ship.ModuleSlots = new ModuleSlotData[s->ModuleSlotsLen];
                for (int i = 0; i < s->ModuleSlotsLen; ++i)
                {
                    CModuleSlot* msd = &s->ModuleSlots[i];
                    Enum.TryParse(msd->Restrictions.AsString, out Restrictions restrictions);
                    var slot = new ModuleSlotData(
                        xmlPos: new Vector2(msd->PosX, msd->PosY),
                        restrictions: restrictions,
                        // @note Interning the strings saves us roughly 70MB of RAM across all UID-s
                        moduleUid: msd->InstalledModuleUID.AsInternedOrNull, // must be interned
                        facing: msd->Facing,
                        orientation: msd->State.AsInterned,
                        // slot options can be:
                        // "NotApplicable", "Ftr-Plasma Tentacle", "Vulcan Scout", ... etc.
                        // It's a general purpose "whatever" sink, however it's used very frequently
                        slotOptions: msd->SlotOptions.AsInterned
                    );
                    slot.Health      = msd->Health;
                    slot.ShieldPower = msd->ShieldPower;
                    if (msd->HangarshipGuid.NotEmpty)
                    {
                        string guid = msd->HangarshipGuid.AsString;
                        if (guid != "00000000-0000-0000-0000-000000000000")
                            slot.HangarshipGuid = Guid.Parse(guid);
                    }
                    ship.ModuleSlots[i] = slot;
                }

                ship.ThrusterList = new ThrusterZone[s->ThrustersLen];
                for (int i = 0; i < s->ThrustersLen; ++i)
                {
                    CThrusterZone* zone = &s->Thrusters[i];
                    ship.ThrusterList[i] = new ThrusterZone
                    {
                        Position = new Vector3(zone->X, zone->Y, zone->Z),
                        Scale = zone->Scale
                    };
                }

                // @todo Remove conversion to HashSet
                ship.TechsNeeded = new HashSet<string>();
                for (int i = 0; i < s->TechsLen; ++i)
                    ship.TechsNeeded.Add(s->Techs[i].AsInterned);

                ship.FinalizeAfterLoad(info, isHullDefinition);
                return ship;
            }
            finally
            {
                DisposeShipDataParser(s);
            }
        }


        ///// C++ Interface /////
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct CThrusterZone
        {
            public readonly float X, Y, Z, Scale;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct CModuleSlot
        {
            public readonly float PosX, PosY, Health, ShieldPower, Facing;
            public readonly CStrView InstalledModuleUID;
            public readonly CStrView HangarshipGuid;
            public readonly CStrView State;
            public readonly CStrView Restrictions;
            public readonly CStrView SlotOptions;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        unsafe struct CShipDataParser
        {
            public readonly CStrView Name;
            public readonly CStrView Hull;
            public readonly CStrView ShipStyle;
            public readonly CStrView EventOnDeath;
            public readonly CStrView SelectionGraphic;
            public readonly CStrView IconPath;
            public readonly CStrView ModelPath;
            public readonly CStrView DefaultAIState;
            public readonly CStrView Role;
            public readonly CStrView CombatState;
            public readonly CStrView ShipCategory;
            public readonly CStrView HangarDesignation;
            public readonly CStrView ModName;

            public readonly int TechScore;
            public readonly float BaseStrength;
            public readonly float FixedUpkeep;
            public readonly float MechanicalBoardingDefense;
            public readonly byte Experience;
            public readonly byte Level;
            public readonly short FixedCost;
            public readonly byte Animated;
            public readonly byte HasFixedCost;
            public readonly byte HasFixedUpkeep;
            public readonly byte IsShipyard;
            public readonly byte CarrierShip;
            public readonly byte BaseCanWarp;
            public readonly byte IsOrbitalDefense;
            public readonly byte HullUnlockable;
            public readonly byte UnLockable;
            public readonly byte AllModulesUnlockable;

            public readonly CThrusterZone* Thrusters;
            public readonly int ThrustersLen;
            public readonly CModuleSlot* ModuleSlots;
            public readonly int ModuleSlotsLen;
            public readonly CStrView* Techs;
            public readonly int TechsLen;

            public readonly CStrView ErrorMessage;
        }

        [DllImport("SDNative.dll")]
        static extern unsafe CShipDataParser* CreateShipDataParser(
                                [MarshalAs(UnmanagedType.LPWStr)] string filename);

        [DllImport("SDNative.dll")]
        static extern unsafe void DisposeShipDataParser(CShipDataParser* parser);

        /////////////////////////

    }
}
