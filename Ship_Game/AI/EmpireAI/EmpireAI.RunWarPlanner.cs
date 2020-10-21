using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.Fleets;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        public War EmpireDefense;
        public float TotalWarValue { get; private set; }
        public float WarStrength = 0;
        
        public void SetTotalWarValue()
        {
            float value = 0;
            foreach ((Empire them, Relationship rel) in OwnerEmpire.AllRelations)
            {
                if (rel.AtWar)
                    value += them.GetOwnedSystems().Sum(s => s.WarValueTo(OwnerEmpire)).LowerBound(1);
            }
            TotalWarValue = value;
        }

        public static void ShowWarDeclaredNotification(Empire us, Empire them)
        {
            if (us.isPlayer || them.isPlayer ||
                (EmpireManager.Player.IsKnown(us) && EmpireManager.Player.IsKnown(them)))
            {
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(us, them);
            }

        }

        public void CallAllyToWar(Empire ally, Empire enemy)
        {
            var offer = new Offer
            {
                AcceptDL = "HelpUS_War_Yes",
                RejectDL = "HelpUS_War_No"
            };

            const string dialogue = "HelpUS_War";
            var ourOffer = new Offer
            {
                ValueToModify = new Ref<bool>(() => ally.IsAtWarWith(enemy), x =>
                {
                    if (x)
                    {
                        ally.GetEmpireAI().DeclareWarOnViaCall(enemy, WarType.ImperialistWar);
                        return;
                    }

                    Relationship ourRelationToAlly = OwnerEmpire.GetRelations(ally);

                    float anger = 30f;
                    if (OwnerEmpire.IsHonorable)
                    {
                        anger = 60f;
                        offer.RejectDL  = "HelpUS_War_No_BreakAlliance";
                        OwnerEmpire.BreakAllianceWith(ally);
                    }

                    ourRelationToAlly.Trust -= anger;
                    ourRelationToAlly.AddAngerDiplomaticConflict(anger);
                })
            };

            if (ally.isPlayer)
                DiplomacyScreen.Show(OwnerEmpire, dialogue, ourOffer, offer, enemy);
        }

        public void DeclareWarFromEvent(Empire them, WarType wt)
        {
            Relationship ourRelationToThem = OwnerEmpire.GetRelations(them);
            ourRelationToThem.AtWar     = true;
            ourRelationToThem.ChangeToHostile();
            ourRelationToThem.ActiveWar = War.CreateInstance(OwnerEmpire, them, wt);
            ourRelationToThem.Trust = 0f;

            OwnerEmpire.BreakAllTreatiesWith(them, includingPeace: true);
            them.GetEmpireAI().GetWarDeclaredOnUs(OwnerEmpire, wt);

            // FB - we are Resetting Pirate timers here since status change can be done via communication by the player
            if (OwnerEmpire.WeArePirates)
                OwnerEmpire.Pirates.ResetPaymentTimerFor(them);
            else if (them.WeArePirates)
                them.Pirates.ResetPaymentTimerFor(OwnerEmpire);
        }

        public void DeclareWarOn(Empire them, WarType warType)
        {
            Empire us = OwnerEmpire;
            Relationship usToThem = us.GetRelations(them);
            Relationship themToUs = them.GetRelations(OwnerEmpire);
            usToThem.PreparingForWar = false;
            if (us.isFaction || us.data.Defeated || them.isFaction || them.data.Defeated)
                return;

            usToThem.FedQuest = null;
            if (us.isPlayer && usToThem.Treaty_NAPact)
            {
                foreach ((Empire other, Relationship rel) in us.AllRelations)
                {
                    // damage our relations with other factions that are not
                    // already at war with them
                    if (other != them && !other.IsAtWarWith(them))
                    {
                        Relationship otherRelationToUs = other.GetRelations(us);
                        otherRelationToUs.Trust -= 50f;
                        otherRelationToUs.AddAngerDiplomaticConflict(20f);
                        otherRelationToUs.UpdateRelationship(other, us);
                    }
                }
                themToUs.Trust -= 50f;
                themToUs.AddAngerDiplomaticConflict(50f);
                themToUs.UpdateRelationship(them, us);
            }

            if (them.isPlayer && !usToThem.AtWar)
            {
                AIDeclaresWarOnPlayer(them, warType, usToThem);
            }

            ShowWarDeclaredNotification(us, them);

            usToThem.AtWar     = true;
            usToThem.ChangeToHostile();
            usToThem.ActiveWar = War.CreateInstance(us, them, warType);
            usToThem.Trust     = 0f;
            us.BreakAllTreatiesWith(them, includingPeace: true);

            them.GetEmpireAI().GetWarDeclaredOnUs(us, warType);
        }

        void AIDeclaresWarOnPlayer(Empire player, WarType warType, Relationship aiRelationToPlayer)
        {
            switch (warType)
            {
                case WarType.BorderConflict:
                    if (aiRelationToPlayer.GetContestedSystem(out SolarSystem contested))
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War BC TarSys", contested);
                    }
                    else
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War BC");
                    }
                    break;
                case WarType.ImperialistWar:
                    if (aiRelationToPlayer.Treaty_NAPact)
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War Imperialism Break NA");
                        foreach ((Empire other, Relationship rel) in OwnerEmpire.AllRelations)
                        {
                            if (other != player)
                            {
                                rel.Trust -= 50f;
                                rel.AddAngerDiplomaticConflict(20f);
                            }
                        }
                    }
                    else
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War Imperialism");
                    }
                    break;
                case WarType.DefensiveWar:
                    if (aiRelationToPlayer.Treaty_NAPact)
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War Defense BrokenNA");
                        OwnerEmpire.BreakTreatyWith(player, TreatyType.NonAggression);
                        aiRelationToPlayer.Trust -= 50f;
                        aiRelationToPlayer.AddAngerDiplomaticConflict(50);
                        foreach ((Empire other, Relationship rel) in OwnerEmpire.AllRelations)
                        {
                            if (other != player)
                            {
                                rel.Trust -= 50f;
                                rel.AddAngerDiplomaticConflict(20);
                            }
                        }
                    }
                    else
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War Defense");
                        aiRelationToPlayer.AddAngerDiplomaticConflict(25);
                        aiRelationToPlayer.Trust -= 25f;
                    }
                    break;
                case WarType.GenocidalWar:
                    break;
                case WarType.SkirmishWar:
                    break;
            }
        }

        public void DeclareWarOnViaCall(Empire them, WarType wt)
        {
            Empire us = OwnerEmpire;
            Relationship usToThem = us.GetRelations(them);
            usToThem.PreparingForWar = false;
            if (us.isFaction || us.data.Defeated || them.data.Defeated || them.isFaction)
                return;

            usToThem.FedQuest = null;
            if (us.isPlayer && usToThem.Treaty_NAPact)
            {
                Relationship themToUs = them.GetRelations(us);
                themToUs.Trust -= 50f;
                themToUs.AddAngerDiplomaticConflict(50);
            }

            if (them.isPlayer && !usToThem.AtWar)
            {
                AIDeclaresWarOnPlayer(them, wt, usToThem);
            }

            ShowWarDeclaredNotification(OwnerEmpire, them);

            usToThem.AtWar     = true;
            usToThem.ChangeToHostile();
            usToThem.ActiveWar = War.CreateInstance(OwnerEmpire, them, wt);
            usToThem.Trust     = 0f;
            OwnerEmpire.BreakAllTreatiesWith(them, includingPeace: true);
            them.GetEmpireAI().GetWarDeclaredOnUs(OwnerEmpire, wt);
        }

        public void EndWarFromEvent(Empire them)
        {
            OwnerEmpire.GetRelations(them).AtWar = false;
            them.GetRelations(OwnerEmpire).AtWar = false;

            // FB - we are Resetting Pirate timers here since status change can be done via communication by the player
            if (OwnerEmpire.WeArePirates)
                OwnerEmpire.Pirates.ResetPaymentTimerFor(them);
            else if (them.WeArePirates)
                them.Pirates.ResetPaymentTimerFor(OwnerEmpire);

            Empire.UpdateBilateralRelations(OwnerEmpire, them);
        }

        public void GetWarDeclaredOnUs(Empire declaredBy, WarType warType)
        {
            Relationship relations = OwnerEmpire.GetRelations(declaredBy);
            relations.AtWar     = true;
            relations.FedQuest  = null;
            relations.ChangeToHostile();
            relations.ActiveWar = War.CreateInstance(OwnerEmpire, declaredBy, warType);

            if (OwnerEmpire.IsPacifist && !OwnerEmpire.isPlayer)
            {
                relations.ActiveWar.WarType = relations.ActiveWar.StartingNumContestedSystems <= 0
                    ? WarType.DefensiveWar
                    : WarType.BorderConflict;
            }

            relations.Trust = 0f;
            Empire.UpdateBilateralRelations(OwnerEmpire, declaredBy);
        }

        void UpdateEmpireDefense()
        {
            if (OwnerEmpire.isPlayer || OwnerEmpire.isFaction) 
                return;
            if (EmpireDefense == null)
            {
                var newWar = War.CreateInstance(OwnerEmpire, OwnerEmpire, WarType.EmpireDefense);
                EmpireDefense = newWar;
            }
            EmpireDefense.ConductWar();
        }

        private void RunWarPlanner()
        {
            if (OwnerEmpire.data.Defeated) return;
            if (OwnerEmpire.GetPlanets().Count == 0)
                return;

            SetTotalWarValue();
            UpdateEmpireDefense();

            if (OwnerEmpire.isPlayer)
                return;

            WarState worstWar = WarState.NotApplicable;
            bool preparingForWar = false;

            var activeWars = new Array<War>();

            foreach((Empire other, Relationship rel) in OwnerEmpire.AllRelations)
            {
                if (GlobalStats.RestrictAIPlayerInteraction && other.isPlayer) 
                    continue;

                if (other.data.Defeated && rel.ActiveWar != null)
                {
                    rel.AtWar = false;
                    rel.PreparingForWar = false;
                    rel.ActiveWar.EndStarDate = Empire.Universe.StarDate;
                    rel.WarHistory.Add(rel.ActiveWar);
                    rel.Posture = Posture.Neutral;
                    rel.ActiveWar = null;
                    continue;
                }

                preparingForWar |= rel.PreparingForWar;
                if (rel.ActiveWar == null) 
                    continue;

                activeWars.Add(rel.ActiveWar);
            }

            // Process wars by their success.
            foreach(War war in activeWars.SortedDescending(w=> w.GetPriority()))
            {
                var currentWar = war.ConductWar();
                worstWar       = worstWar > currentWar ? currentWar : worstWar;
            }

            WarStrength = OwnerEmpire.Pool.EmpireReadyFleets.AccumulatedStrength;
            // start a new war by military strength
            if (worstWar > WarState.EvenlyMatched)
            {
                float currentTaskStrength    = GetStrengthNeededByTasks( f=>
                {
                    return f.GetTaskCategory().HasFlag(MilitaryTask.TaskCategory.FleetNeeded);
                });

                if (WarStrength > currentTaskStrength)
                {
                    WarStrength = OwnerEmpire.Pool.EmpireReadyFleets.AccumulatedStrength;

                    foreach ((Empire them, Relationship rel) in OwnerEmpire.AllRelations)
                    {
                        if (them.data.Defeated || them.isPlayer && GlobalStats.RestrictAIPlayerInteraction)
                            continue;
                        if (rel.Treaty_Peace || !rel.PreparingForWar || rel.AtWar) continue;

                        float minDistanceToThem = OwnerEmpire.MinDistanceToNearestOwnedSystemIn(them.GetOwnedSystems(), out SolarSystem nearestSystem);

                        if (minDistanceToThem < 0) continue;
                        float distanceMultiplier = (minDistanceToThem / (Empire.Universe.UniverseSize / 2f)).LowerBound(1);
                        float enemyStrength      = 0;//kv.Key.Pool.EmpireReadyFleets.AccumulatedStrength;

                        float anger = (rel.TotalAnger  / 100) * OwnerEmpire.GetWarOffensiveRatio();

                        if (enemyStrength * distanceMultiplier > WarStrength * anger - currentTaskStrength ) continue;

                        // all out war
                        if (rel.PreparingForWarType == WarType.ImperialistWar || rel.PreparingForWarType == WarType.GenocidalWar)
                        {
                            DeclareWarOn(them, rel.PreparingForWarType);
                        }
                        // they have planets in our AO
                        else if (rel.PreparingForWarType == WarType.BorderConflict)
                        {
                            bool theirBorderSystems = them.GetBorderSystems(OwnerEmpire, true)
                                .Any(s => IsInOurAOs(s.Position));
                            if (theirBorderSystems)
                                DeclareWarOn(them, rel.PreparingForWarType);
                        }
                        // we have planets in their AO. Skirmish War.
                        else if (rel.PreparingForWarType != WarType.DefensiveWar)
                        {
                            bool ourBorderSystem = OwnerEmpire.GetBorderSystems(them, false)
                                .Any(s => them.GetEmpireAI().IsInOurAOs(s.Position));
                            if (ourBorderSystem)
                            {
                                DeclareWarOn(them, rel.PreparingForWarType);
                            }
                        }
                        // We share a solar system
                        else if (OwnerEmpire.GetOwnedSystems().Any(s => s.OwnerList.Contains(them)))
                        {
                            DeclareWarOn(them, rel.PreparingForWarType);
                        }

                        break;
                    }
                }
            }
        }
    }
}