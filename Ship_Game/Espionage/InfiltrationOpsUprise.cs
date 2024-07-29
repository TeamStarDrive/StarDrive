﻿using Microsoft.Xna.Framework;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{

    [StarDataType]
    public class InfiltrationOpsUprise : InfiltrationOperation
    {
        [StarData] readonly Empire Owner;
        [StarData] readonly Empire Them;
        const float PercentOfLevelCost = 0.2f;
        const int SuccessTargetNumber  = 40; // need to get 40 and above in a roll of d100)
        const float BaseRelationDamage = 10;
        const int BaseRampUpTurns = 30;

        public InfiltrationOpsUprise(Empire owner, Empire them, int levelCost, byte level) :
            base((int)(levelCost * PercentOfLevelCost), level, InfiltrationOpsType.PlantMole, BaseRampUpTurns, owner)
        {
            Owner = owner;
            Them = them;
        }

        public override void CompleteOperation()
        {
            InfiltrationOpsResolve aftermath = new InfiltrationOpsResolve(Owner, Them);
            var result = RollMissionResult(Owner, Them, Owner.IsAlliedWith(Them) ? SuccessTargetNumber / 2 : SuccessTargetNumber, Level);
            Espionage espionage = Owner.GetEspionage(Them);
            var potentials      = Them.GetPlanets().Sorted(p => p.PopulationBillion).TakeItems(5);
            Planet targetPlanet = Them.Random.Item(potentials);
            bool addRebellion   = false;
            int numRebels       = 5;

            switch (result)
            {
                case InfiltrationOpsResult.Phenomenal:
                    aftermath.GoodResult = addRebellion = true;
                    numRebels += 7;
                    break;
                case InfiltrationOpsResult.GreatSuccess:
                    aftermath.GoodResult = addRebellion = true;
                    numRebels += 3;
                    break;
                case InfiltrationOpsResult.Success:
                    aftermath.GoodResult = addRebellion = true;
                    break;
                case InfiltrationOpsResult.Fail:
                    aftermath.CustomMessage = $"{Them.data.Traits.Name}: {Localizer.Token(GameText.NewFailedToInciteUprise)}";
                    break;
                case InfiltrationOpsResult.MiserableFail:
                    aftermath.CustomMessage = $"{Them.data.Traits.Name}: {Localizer.Token(GameText.NewFailedToInciteUpriseMiserable)}";
                    espionage.ReduceInfiltrationLevel();
                    break;
                case InfiltrationOpsResult.CriticalFail:
                    aftermath.CustomMessage = $"{Them.data.Traits.Name}: {Localizer.Token(GameText.NewFailedToInciteUpriseDetected)}";
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.AnEnemyAgentWasFoiled)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage);
                    espionage.ReduceInfiltrationLevel();
                    aftermath.DamageReason = "Caught Spying";
                    break;
                case InfiltrationOpsResult.Disaster:
                    aftermath.CustomMessage = $"{Them.data.Traits.Name}: {Localizer.Token(GameText.FailedToInciteUpriseWipedOut)}";
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.NewWipedOutNetworkUprise)}\n" +
                                                $"{Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}\n" +
                                                $"{Localizer.Token(GameText.TheirInfiltrationLevelWas)} {espionage.Level}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage, withLevelMultiplier: true);
                    aftermath.DamageReason = "Caught Spying Failed";
                    espionage.WipeoutInfiltration();
                    break;
            }

            if (addRebellion)
            {
                aftermath.MessageToVictim = $"{Localizer.Token(GameText.IncitedUpriseOn)} {targetPlanet.Name}";
                aftermath.CustomMessage = $"{Localizer.Token(GameText.WeIncitedUprise)} {targetPlanet.Name} {Localizer.Token(GameText.NtheAgentWasNotDetected)}";
                Them.AddRebellion(targetPlanet, numRebels);
            }
            aftermath.SendNotifications(Owner.Universe);
        }
    }
}
