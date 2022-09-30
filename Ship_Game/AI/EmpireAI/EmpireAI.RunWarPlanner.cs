using Ship_Game.Gameplay;
using System.Linq;
using SDGraphics;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.DiplomacyScreen;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        public static void ShowWarDeclaredNotification(Empire us, Empire them)
        {
            if (us.isPlayer || them.isPlayer ||
                (us.Universe.Player.IsKnown(us) && us.Universe.Player.IsKnown(them)))
            {
                us.Universe.Notifications?.AddWarDeclaredNotification(us, them);
            }
        }

        public void CallAllyToWar(Empire ally, Empire enemy)
        {
            if (!ally.isPlayer)
            {
                if (ally.ProcessAllyCallToWar(OwnerEmpire, enemy, out _))
                    ally.AI.DeclareWarOnViaCall(enemy, WarType.ImperialistWar, OwnerEmpire);

                return;
            }

            // For player allies
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
                        ally.AI.DeclareWarOnViaCall(enemy, WarType.ImperialistWar, OwnerEmpire);
                        return;
                    }

                    Relationship ourRelationToAlly = OwnerEmpire.GetRelations(ally);
                    float anger = OwnerEmpire.IsHonorable ? 60f : 30f;
                    ourRelationToAlly.Trust -= anger;
                    ourRelationToAlly.AddAngerDiplomaticConflict(anger);
                    if (ourRelationToAlly.Anger_DiplomaticConflict.GreaterOrEqual(60))
                    {
                        offer.RejectDL = "HelpUS_War_No_BreakAlliance";
                        OwnerEmpire.BreakAllianceWith(ally);
                        if (!OwnerEmpire.IsPacifist && !OwnerEmpire.IsCunning)
                            ourRelationToAlly.PrepareForWar(WarType.ImperialistWar, OwnerEmpire);
                    }
                })
            };

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
            them.AI.GetWarDeclaredOnUs(OwnerEmpire, wt);

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
            usToThem.CancelPrepareForWar();
            if (us.IsFaction || us.data.Defeated || them.IsFaction || them.data.Defeated)
                return;

            usToThem.FedQuest = null;
            if (us.isPlayer && usToThem.Treaty_NAPact)
            {
                foreach (Relationship rel in us.AllRelations)
                {
                    // damage our relations with other factions that are not
                    // already at war with them
                    if (rel.Them != them && !rel.Them.IsAtWarWith(them))
                    {
                        Relationship otherRelationToUs = rel.Them.GetRelations(us);
                        otherRelationToUs.Trust -= 50f;
                        otherRelationToUs.AddAngerDiplomaticConflict(20f);
                        otherRelationToUs.UpdateRelationship(rel.Them, us);
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

            them.AI.GetWarDeclaredOnUs(us, warType);
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
                        foreach (Relationship rel in OwnerEmpire.AllRelations)
                        {
                            if (rel.Them != player)
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
                case WarType.GenocidalWar:
                case WarType.DefensiveWar:
                    if (aiRelationToPlayer.Treaty_NAPact)
                    {
                        DiplomacyScreen.Show(OwnerEmpire, player, "Declare War Defense BrokenNA");
                        OwnerEmpire.BreakTreatyWith(player, TreatyType.NonAggression);
                        aiRelationToPlayer.Trust -= 50f;
                        aiRelationToPlayer.AddAngerDiplomaticConflict(50);
                        foreach (Relationship rel in OwnerEmpire.AllRelations)
                        {
                            if (rel.Them != player)
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
                case WarType.SkirmishWar: // no diplo for player. Pirates use skirmish wars
                    break;
            }
        }

        public void DeclareWarOnViaCall(Empire them, WarType wt, Empire requestingEmpire)
        {
            Empire us = OwnerEmpire;
            Relationship usToThem = us.GetRelations(them);
            usToThem.CancelPrepareForWar();
            if (us.IsFaction || us.data.Defeated || them.data.Defeated || them.IsFaction)
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
                us.Universe.Notifications.AddDeclareWarViaAllyCall(OwnerEmpire, requestingEmpire);
            }

            ShowWarDeclaredNotification(OwnerEmpire, them);

            usToThem.AtWar     = true;
            usToThem.ChangeToHostile();
            usToThem.ActiveWar = War.CreateInstance(OwnerEmpire, them, wt);
            usToThem.Trust     = 0f;
            OwnerEmpire.BreakAllTreatiesWith(them, includingPeace: true);
            them.AI.GetWarDeclaredOnUs(OwnerEmpire, wt);
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

        /// <summary>
        /// Ensures The AI will always have an EmpireDefense Goal
        /// </summary>
        void UpdateEmpireDefense()
        {
            if (OwnerEmpire.IsFaction) 
                return;

            if (OwnerEmpire.NoEmpireDefenseGoal())
                OwnerEmpire.AI.AddGoal(new EmpireDefense(OwnerEmpire));
        }

        private void RunWarPlanner()
        {
            if (OwnerEmpire.isPlayer
                || OwnerEmpire.data.Defeated
                || OwnerEmpire.GetPlanets().Count == 0)
            {
                return;
            }

            UpdateEmpireDefense();
            foreach (Relationship rel in OwnerEmpire.AllRelations)
            {
                if (rel.Them.data.Defeated && rel.ActiveWar != null)
                {
                    rel.AtWar = false;
                    rel.CancelPrepareForWar();
                    rel.ActiveWar.EndStarDate = rel.Them.Universe.StarDate;
                    rel.WarHistory.Add(rel.ActiveWar);
                    rel.Posture = Posture.Neutral;
                }
            }
        }
    }
}