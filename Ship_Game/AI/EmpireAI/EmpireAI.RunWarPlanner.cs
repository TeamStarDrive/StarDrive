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
            foreach (var rel in OwnerEmpire.AllRelations)
            {
                var them = rel.Key;
                var war = rel.Value.ActiveWar;
                if (!rel.Value.AtWar) continue;
                value = them.GetOwnedSystems().Sum(s => s.WarValueTo(OwnerEmpire)).LowerBound(1);
            }
            TotalWarValue = value;
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
                ValueToModify = new Ref<bool>(() => ally.GetRelations(enemy).AtWar, x =>
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

                    ourRelationToAlly.Trust                    -= anger;
                    ourRelationToAlly.Anger_DiplomaticConflict += anger;
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

        public void DeclareWarOn(Empire them, WarType wt)
        {
            Relationship ourRelations = OwnerEmpire.GetRelations(them);
            Relationship theirRelationToUs = them.GetRelations(OwnerEmpire);
            ourRelations.PreparingForWar = false;
            if (OwnerEmpire.isFaction || OwnerEmpire.data.Defeated || (them.data.Defeated || them.isFaction))
                return;

            ourRelations.FedQuest = null;
            if (OwnerEmpire == Empire.Universe.PlayerEmpire && ourRelations.Treaty_NAPact)
            {
                foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
                {
                    if (kv.Key != them)
                    {
                        Relationship otherRelationToUs = kv.Key.GetRelations(OwnerEmpire);
                        otherRelationToUs.Trust                    -= 50f;
                        otherRelationToUs.Anger_DiplomaticConflict += 20f;
                        otherRelationToUs.UpdateRelationship(kv.Key, OwnerEmpire);
                    }
                }
                theirRelationToUs.Trust                    -= 50f;
                theirRelationToUs.Anger_DiplomaticConflict += 50f;
                theirRelationToUs.UpdateRelationship(them, OwnerEmpire);
            }

            if (them == Empire.Universe.PlayerEmpire && !ourRelations.AtWar)
            {
                AIDeclaresWarOnPlayer(them, wt, ourRelations);
            }

            if (them == Empire.Universe.PlayerEmpire || OwnerEmpire == Empire.Universe.PlayerEmpire)
            {
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(OwnerEmpire, them);
            }
            else if (Empire.Universe.PlayerEmpire.GetRelations(them).Known &&
                     Empire.Universe.PlayerEmpire.GetRelations(OwnerEmpire).Known)
            {
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(OwnerEmpire, them);
            }

            ourRelations.AtWar     = true;
            ourRelations.ChangeToHostile();
            ourRelations.ActiveWar = War.CreateInstance(OwnerEmpire, them, wt);
            ourRelations.Trust     = 0f;
            OwnerEmpire.BreakAllTreatiesWith(them, includingPeace: true);
            them.GetEmpireAI().GetWarDeclaredOnUs(OwnerEmpire, wt);
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
                        foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
                        {
                            if (kv.Key != player)
                            {
                                kv.Value.Trust -= 50f;
                                kv.Value.Anger_DiplomaticConflict += 20f;
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
                        aiRelationToPlayer.Anger_DiplomaticConflict += 50f;
                        foreach (KeyValuePair<Empire, Relationship> kv in OwnerEmpire.AllRelations)
                        {
                            if (kv.Key != player)
                            {
                                kv.Value.Trust -= 50f;
                                kv.Value.Anger_DiplomaticConflict += 20f;
                            }
                        }
                    }
                    else
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War Defense");
                        aiRelationToPlayer.Anger_DiplomaticConflict += 25f;
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
            Relationship ourRelationToThem = OwnerEmpire.GetRelations(them);
            ourRelationToThem.PreparingForWar = false;
            if (OwnerEmpire.isFaction || OwnerEmpire.data.Defeated || them.data.Defeated || them.isFaction)
                return;

            ourRelationToThem.FedQuest = null;
            if (OwnerEmpire == Empire.Universe.PlayerEmpire && ourRelationToThem.Treaty_NAPact)
            {
                Relationship item                                = them.GetRelations(OwnerEmpire);
                item.Trust                                       = item.Trust - 50f;
                Relationship angerDiplomaticConflict             = them.GetRelations(OwnerEmpire);
                angerDiplomaticConflict.Anger_DiplomaticConflict =
                    angerDiplomaticConflict.Anger_DiplomaticConflict + 50f;
                them.GetRelations(OwnerEmpire).UpdateRelationship(them, OwnerEmpire);
            }

            if (them == Empire.Universe.PlayerEmpire && !ourRelationToThem.AtWar)
            {
                AIDeclaresWarOnPlayer(them, wt, ourRelationToThem);
            }
            if (them == Empire.Universe.PlayerEmpire || OwnerEmpire == Empire.Universe.PlayerEmpire)
            {
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(OwnerEmpire, them);
            }
            else if (Empire.Universe.PlayerEmpire.GetRelations(them).Known &&
                     Empire.Universe.PlayerEmpire.GetRelations(OwnerEmpire).Known)
            {
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(OwnerEmpire, them);
            }

            ourRelationToThem.AtWar     = true;
            ourRelationToThem.ChangeToHostile();
            ourRelationToThem.ActiveWar = War.CreateInstance(OwnerEmpire, them, wt);
            ourRelationToThem.Trust     = 0f;
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
        }

        public void GetWarDeclaredOnUs(Empire warDeclarant, WarType wt)
        {
            Relationship relations = OwnerEmpire.GetRelations(warDeclarant);
            relations.AtWar     = true;
            relations.FedQuest  = null;
            relations.ChangeToHostile();
            relations.ActiveWar = War.CreateInstance(OwnerEmpire, warDeclarant, wt);

            if (Empire.Universe.PlayerEmpire != OwnerEmpire)
            {
                if (OwnerEmpire.IsPacifist)
                {
                    relations.ActiveWar.WarType = relations.ActiveWar.StartingNumContestedSystems <= 0
                        ? WarType.DefensiveWar
                        : WarType.BorderConflict;
                }
            }

            relations.Trust = 0f;
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
            SetTotalWarValue();
            UpdateEmpireDefense();

            if (OwnerEmpire.isPlayer || OwnerEmpire.data.Defeated)
                return;
            WarState worstWar = WarState.NotApplicable;
            bool preparingForWar = false;
            foreach(var kv in OwnerEmpire.AllRelations)
            {
                if (GlobalStats.RestrictAIPlayerInteraction && kv.Key.isPlayer) 
                    continue;

                if (kv.Key.data.Defeated && kv.Value.ActiveWar != null)
                {
                    var relationThem = kv.Value;
                    relationThem.AtWar = false;
                    relationThem.PreparingForWar = false;
                    relationThem.ActiveWar.EndStarDate = Empire.Universe.StarDate;
                    relationThem.WarHistory.Add(relationThem.ActiveWar);
                    relationThem.Posture = Posture.Neutral;
                    relationThem.ActiveWar = null;
                    continue;
                }

                Relationship rel = kv.Value;
                preparingForWar |= rel.PreparingForWar;
                if (rel.ActiveWar == null) 
                    continue;


                var currentWar = rel.ActiveWar.ConductWar();
                worstWar = worstWar > currentWar ? currentWar : worstWar;
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

                    foreach (var kv in OwnerEmpire.AllRelations)
                    {
                        if (kv.Key.data.Defeated || GlobalStats.RestrictAIPlayerInteraction && kv.Key.isPlayer) continue;
                        Relationship rel = kv.Value;
                        Empire them      = kv.Key;
                        if (rel.Treaty_Peace || !rel.PreparingForWar || rel.AtWar) continue;

                        float minDistanceToThem = OwnerEmpire.MinDistanceToNearestOwnedSystemIn(them.GetOwnedSystems(), out SolarSystem nearestSystem);

                        if (minDistanceToThem < 0) continue;
                        float projectorRadius    = OwnerEmpire.GetProjectorRadius();
                        float distanceMultiplier = (minDistanceToThem / (Empire.Universe.UniverseSize / 4f)).LowerBound(1);
                        float enemyStrength      = kv.Key.Pool.EmpireReadyFleets.AccumulatedStrength;

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