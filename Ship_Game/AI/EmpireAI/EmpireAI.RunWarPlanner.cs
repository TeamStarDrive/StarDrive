using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
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
                    if (OwnerEmpire.data.DiplomaticPersonality != null &&
                        OwnerEmpire.data.DiplomaticPersonality.Name == "Honorable")
                    {
                        anger = 60f;
                        offer.RejectDL  = "HelpUS_War_No_BreakAlliance";
                        ally.GetRelations(OwnerEmpire).Treaty_Alliance = false;
                        ourRelationToAlly.Treaty_Alliance    = false;
                        ourRelationToAlly.Treaty_OpenBorders = false;
                        ourRelationToAlly.Treaty_NAPact      = false;
                    }

                    ourRelationToAlly.Trust -= anger;
                    ourRelationToAlly.Anger_DiplomaticConflict += anger;
                })
            };

            if (ally == Empire.Universe.PlayerEmpire)
            {
                DiplomacyScreen.Show(OwnerEmpire, dialogue, ourOffer, offer, enemy);
            }
        }

        public void DeclareWarFromEvent(Empire them, WarType wt)
        {
            Relationship ourRelationToThem = OwnerEmpire.GetRelations(them);
            ourRelationToThem.AtWar     = true;
            ourRelationToThem.Posture   = Posture.Hostile;
            ourRelationToThem.ActiveWar = new War(OwnerEmpire, them, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (ourRelationToThem.Trust > 0f)
            {
                ourRelationToThem.Trust = 0f;
            }
            ourRelationToThem.Treaty_OpenBorders = false;
            ourRelationToThem.Treaty_NAPact      = false;
            ourRelationToThem.Treaty_Trade       = false;
            ourRelationToThem.Treaty_Alliance    = false;
            ourRelationToThem.Treaty_Peace       = false;
            them.GetEmpireAI().GetWarDeclaredOnUs(OwnerEmpire, wt);
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
                ourRelations.Treaty_NAPact = false;
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

            ourRelations.AtWar   = true;
            ourRelations.Posture = Posture.Hostile;
            ourRelations.ActiveWar = new War(OwnerEmpire, them, Empire.Universe.StarDate) {WarType = wt};
            if (ourRelations.Trust > 0f)
                ourRelations.Trust = 0.0f;
            ourRelations.Treaty_OpenBorders = false;
            ourRelations.Treaty_NAPact      = false;
            ourRelations.Treaty_Trade       = false;
            ourRelations.Treaty_Alliance    = false;
            ourRelations.Treaty_Peace       = false;
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
                        aiRelationToPlayer.Treaty_NAPact = false;
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
                ourRelationToThem.Treaty_NAPact     = false;
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
            ourRelationToThem.Posture   = Posture.Hostile;
            ourRelationToThem.ActiveWar = new War(OwnerEmpire, them, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (ourRelationToThem.Trust > 0f)
            {
                ourRelationToThem.Trust = 0f;
            }
            ourRelationToThem.Treaty_OpenBorders = false;
            ourRelationToThem.Treaty_NAPact      = false;
            ourRelationToThem.Treaty_Trade       = false;
            ourRelationToThem.Treaty_Alliance    = false;
            ourRelationToThem.Treaty_Peace       = false;
            them.GetEmpireAI().GetWarDeclaredOnUs(OwnerEmpire, wt);
        }

        public void EndWarFromEvent(Empire them)
        {
            OwnerEmpire.GetRelations(them).AtWar = false;
            them.GetRelations(OwnerEmpire).AtWar = false;

            foreach (MilitaryTask task in TaskList.AtomicCopy())
            {
                if (OwnerEmpire.GetFleetsDict().Get(task.WhichFleet, out Fleet fleet) &&
                    OwnerEmpire.data.Traits.Name == "Corsairs")
                {
                    bool foundhome = false;
                    foreach (Ship ship in OwnerEmpire.GetShips())
                    {
                        if (ship.IsPlatformOrStation)
                        {
                            foundhome = true;
                            foreach (Ship fship in fleet.Ships)
                            {
                                fship.AI.ClearOrders();
                                fship.DoEscort(ship);
                            }
                            break;
                        }
                    }

                    if (!foundhome)
                    {
                        foreach (Ship ship in fleet.Ships)
                            ship.AI.ClearOrders();
                    }
                }
                task.EndTaskWithMove();
            }
        }

        public void GetWarDeclaredOnUs(Empire warDeclarant, WarType wt)
        {
            Relationship relations = OwnerEmpire.GetRelations(warDeclarant);
            relations.AtWar     = true;
            relations.FedQuest  = null;
            relations.Posture   = Posture.Hostile;
            relations.ActiveWar = new War(OwnerEmpire, warDeclarant, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (Empire.Universe.PlayerEmpire != OwnerEmpire)
            {
                if (OwnerEmpire.data.DiplomaticPersonality.Name == "Pacifist")
                {
                    relations.ActiveWar.WarType = relations.ActiveWar.StartingNumContestedSystems <= 0
                        ? WarType.DefensiveWar
                        : WarType.BorderConflict;
                }
            }
            if (relations.Trust > 0f)
                relations.Trust          = 0f;
            relations.Treaty_Alliance    = false;
            relations.Treaty_NAPact      = false;
            relations.Treaty_OpenBorders = false;
            relations.Treaty_Trade       = false;
            relations.Treaty_Peace       = false;
        }

        public void OfferPeace(KeyValuePair<Empire, Relationship> relationship, string whichPeace)
        {
            var offerPeace = new Offer
            {
                PeaceTreaty = true,
                AcceptDL = "OFFERPEACE_ACCEPTED",
                RejectDL = "OFFERPEACE_REJECTED"
            };
            Relationship value = relationship.Value;
            offerPeace.ValueToModify = new Ref<bool>(() => false, x => value.SetImperialistWar());
            string dialogue = whichPeace;
            var ourOffer = new Offer { PeaceTreaty = true };
            if (relationship.Key == Empire.Universe.PlayerEmpire)
            {
                DiplomacyScreen.Show(OwnerEmpire, dialogue, ourOffer, offerPeace);
            }
            else
            {
                relationship.Key.GetEmpireAI()
                    .AnalyzeOffer(ourOffer, offerPeace, OwnerEmpire, Offer.Attitude.Respectful);
            }
        }


        private void RunWarPlanner()
        {
            if (OwnerEmpire.isPlayer)
                return;

            foreach(var kv in OwnerEmpire.AllRelations.Sorted
                (r=> (int?)r.Value.ActiveWar?.GetWarScoreState() ?? (int)WarState.NotApplicable))
            {
                var relation = kv.Value;
                
                    
                var warState = relation.ActiveWar?.ConductWar() ?? WarState.NotApplicable;
                if (!relation.PreparingForWar)
                    if (relation.ActiveWar?.TasksForThisWar().Length > 0)
                        break;
            }
        }
    }
}