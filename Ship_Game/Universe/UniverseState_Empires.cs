﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Gameplay;

namespace Ship_Game.Universe;

/// <summary>
/// Contains everything related to Empires state
/// Except for the serialized state variables which are kept in UniverseState.cs
/// </summary>

public partial class UniverseState
{
    public IReadOnlyList<Empire> Empires => EmpireList;
    public int NumEmpires => EmpireList.Count;

    public Empire[] NonPlayerMajorEmpires =>
        EmpireList.Filter(empire => !empire.IsFaction && !empire.isPlayer);

    public Empire[] NonPlayerEmpires =>
        EmpireList.Filter(empire => !empire.isPlayer);

    public Empire[] ActiveNonPlayerMajorEmpires =>
        EmpireList.Filter(empire => !empire.IsFaction && !empire.isPlayer && !empire.data.Defeated);

    public Empire[] ActiveMajorEmpires =>
        EmpireList.Filter(empire => !empire.IsFaction && !empire.data.Defeated);

    public Empire[] ActiveEmpires =>
        EmpireList.Filter(empire => !empire.data.Defeated);

    public Empire[] MajorEmpires => EmpireList.Filter(empire => !empire.IsFaction);
    public Empire[] Factions => EmpireList.Filter(empire => empire.IsFaction);
    public Empire[] PirateFactions => EmpireList.Filter(empire => empire.WeArePirates);

    void InitializeEmpiresFromSave()
    {
        foreach (Empire e in EmpireList)
        {
            if (e.data.AbsorbedBy != null)
            {
                Empire masterEmpire = GetEmpireByName(e.data.AbsorbedBy);
                masterEmpire.AssimilateTech(e);
            }
        }

        foreach (Empire empire in MajorEmpires)
            empire.UpdateDefenseShipBuildingOffense();

        foreach (Empire empire in EmpireList.Filter(e => !e.data.Defeated))
            empire.UpdatePopulation();
    }

    public Empire CreateEmpire(IEmpireData readOnlyData, bool isPlayer, GameDifficulty difficulty = GameDifficulty.Normal)
    {
        if (GetEmpireByName(readOnlyData.Name) != null)
            throw new InvalidOperationException($"BUG: Empire already created! {readOnlyData.Name}");
        Empire e = CreateEmpireFromEmpireData(readOnlyData, isPlayer);
        AddEmpire(e);
        InitRelationships(e, difficulty);
        return e;
    }

    void InitRelationships(Empire us, GameDifficulty difficulty)
    {
        foreach (Empire them in Empires)
        {
            if (us != them)
            {
                Relationship usToThem = us.AddRelation(them);
                them.AddRelation(us);

                // TODO see if this increased anger bit can be removed
                if (them.isPlayer && difficulty > GameDifficulty.Hard)
                {
                    float difficultyRatio = (int) difficulty / 10f;
                    float trust = (100 - us.data.DiplomaticPersonality.Trustworthiness).LowerBound(0);

                    // this makes AI trust the player less and hate him because of an unknown
                    // territorial grievance
                    // TODO: do we really want this? maybe just make them easier to anger or something
                    usToThem.Trust -= difficultyRatio * trust;
                    usToThem.AddAngerTerritorialConflict(difficultyRatio * trust);
                }

                Empire.UpdateBilateralRelations(us, them);
            }
        }
    }

    public Empire CreateTestEmpire(string name)
    {
        var e = new Empire(this)
        {
            data = new EmpireData
            {
                Traits = new RacialTrait { Name = name }
            }
        };
        return AddEmpire(e);
    }

    public Empire AddEmpire(Empire e)
    {
        if (e.Universe == null)
            throw new ArgumentNullException(nameof(e.Universe));

        if (FindDuplicateEmpire(e) != null)
            throw new InvalidOperationException("Empire already added");

        EmpireList.Add(e);
        e.Id = EmpireList.Count;

        if (e.isPlayer)
        {
            if (Player != null)
                throw new InvalidOperationException($"Duplicate Player empire! previous={Player}  new={e}");
            Player = e;
        }

        switch (e.data.Traits.Name)
        {
            case "Cordrazine Collective":
                Cordrazine = e;
                break;
            case "The Remnant":
                Remnants = e;
                break;
            case "Unknown":
                Unknown = e;
                break;
            case "Corsairs":
                Corsairs = e;
                break;
        }

        return e;
    }

    void ClearEmpires()
    {
        foreach (Empire e in EmpireList)
            e.Dispose();
        EmpireList.Clear();

        Player = null;
        Cordrazine = null;
        Remnants = null;
        Unknown = null;
        Corsairs = null;
    }

    public Empire GetEmpire(int empireId)
    {
        for (int i = 0; i < EmpireList.Count; ++i)
            if (EmpireList[i].Id == empireId)
                return EmpireList[i];
        return null;
    }

    public Empire[] MajorEmpiresAtWarWith(Empire empire)
        => ActiveMajorEmpires.Filter(e => e.IsAtWarWith(empire));

    public Empire FindDuplicateEmpire(Empire empire)
    {
        if (EmpireList.ContainsRef(empire))
            return empire;
        return GetEmpireByName(empire.data.Traits.Name);
    }

    public Empire GetEmpireById(int empireId)
    {
        return empireId == 0 ? null : EmpireList[empireId - 1];
    }

    public Empire GetEmpireByName(string name)
    {
        if (name.IsEmpty())
            return null;
        foreach (Empire empire in EmpireList)
            if (empire.data.Traits.Name == name)
                return empire;
        return null;
    }

    public Array<Empire> GetAllies(Empire e)
    {
        var allies = new Array<Empire>();
        if (e.IsFaction)
            return allies;
        for (int i = 0; i < EmpireList.Count; i++)
        {
            Empire empire = EmpireList[i];
            if (empire != e && e.IsAlliedWith(empire))
                allies.Add(empire);
        }

        return allies;
    }

    public Array<Empire> GetEnemies(Empire e)
    {
        var enemies = new Array<Empire>();
        for (int i = 0; i < EmpireList.Count; i++)
        {
            Empire empire = EmpireList[i];
            if (e.IsEmpireHostile(empire))
                enemies.Add(empire);
        }

        return enemies;
    }

    public Array<Empire> GetTradePartners(Empire e)
    {
        var allies = new Array<Empire>();
        if (e.IsFaction)
            return allies;
        foreach (Empire empire in EmpireList)
            if (!empire.isPlayer && e.IsTradeTreaty(empire))
                allies.Add(empire);
        return allies;
    }

    public Empire FindRebellion(string rebelName)
    {
        foreach (Empire e in EmpireList)
        {
            if (e.data.PortraitName == rebelName)
            {
                Log.Info($"Found Existing Rebel: {e.data.PortraitName}");
                return e;
            }
        }

        return null;
    }

    public Empire GetEmpireByShipType(string shipType)
    {
        if (shipType.IsEmpty())
            return null;
        foreach (Empire empire in EmpireList)
            if (empire.data.Traits.ShipType == shipType)
                return empire;
        return null;
    }

    public Troop CreateRebelTroop(Empire rebelEmpire)
    {
        foreach (string troopType in ResourceManager.TroopTypes)
        {
            if (rebelEmpire.WeCanBuildTroop(troopType) &&
                ResourceManager.TryCreateTroop(troopType, rebelEmpire, out Troop troop))
            {
                troop.Description = rebelEmpire.data.TroopDescription.Text;
                return troop;
            }
        }
        return null;
    }

    Empire CreateEmpireFromEmpireData(IEmpireData readOnlyData, bool isPlayer)
    {
        EmpireData data = readOnlyData.CreateInstance();
        DiplomaticTraits dt = ResourceManager.DiplomaticTraits;
        var empire = new Empire(us: this)
        {
            data = data, 
            isPlayer = isPlayer,
            IsFaction = data.IsFaction,
        };

        if      (data.IsFaction) Log.Info($"Creating Faction {data.Traits.Name}");
        else if (data.MinorRace) Log.Info($"Creating MinorRace {data.Traits.Name}");
        else                     Log.Info($"Creating MajorEmpire {data.Traits.Name}");

        DTrait[] dipTraits = dt.DiplomaticTraitsList.Filter(
            dip => !data.ExcludedDTraits.Any(trait => trait == dip.Name));
        data.DiplomaticPersonality = RandomMath.RandItem(dipTraits);

        ETrait[] ecoTraits = dt.EconomicTraitsList.Filter(
            eco => !data.ExcludedETraits.Any(trait => trait == eco.Name));
        data.EconomicPersonality = RandomMath.RandItem(ecoTraits);

        // Added by McShooterz: set values for alternate race file structure
        data.Traits.LoadTraitConstraints();
        empire.dd = ResourceManager.GetDiplomacyDialog(data.DiplomacyDialogPath);
        data.SpyModifier = data.Traits.SpyMultiplier;
        data.Traits = data.Traits;
        data.Traits.Spiritual = data.Traits.Spiritual;
        data.Traits.PassengerModifier += data.Traits.PassengerBonus;
        empire.EmpireColor = data.Traits.Color;
        empire.Initialize();
        return empire;
    }

    public Empire CreateRebelsFromEmpireData(IEmpireData readOnlyData, Empire parent)
    {
        EmpireData data = readOnlyData.CreateInstance();
        Empire rebelEmpire = GetEmpireByName(data.RebelName);
        if (rebelEmpire != null) return rebelEmpire;

        var empire = new Empire(parent.Universe, parent)
        {
            IsFaction = true,
            data = data
        };

        // Added by McShooterz: mod folder support
        DiplomaticTraits dt = ResourceManager.DiplomaticTraits;
        data.DiplomaticPersonality = RandomMath.RandItem(dt.DiplomaticTraitsList);
        data.DiplomaticPersonality = RandomMath.RandItem(dt.DiplomaticTraitsList);
        data.EconomicPersonality   = RandomMath.RandItem(dt.EconomicTraitsList);
        data.EconomicPersonality   = RandomMath.RandItem(dt.EconomicTraitsList);
        data.SpyModifier = data.Traits.SpyMultiplier;
        empire.EmpireColor = new Color(128, 128, 128, 255);

        data.IsRebelFaction  = true;
        data.Traits.Name     = data.RebelName;
        data.Traits.Singular = data.RebelSing;
        data.Traits.Plural   = data.RebelPlur;

        empire.Initialize();
        empire.UpdatePopulation();

        AddEmpire(empire);

        foreach (Empire otherEmpire in Empires)
        {
            if (otherEmpire != empire)
            {
                otherEmpire.AddRelation(empire);
                empire.AddRelation(otherEmpire);
                Empire.UpdateBilateralRelations(empire, otherEmpire);
            }
        }

        data.RebellionLaunched = true;
        return empire;
    }
}

