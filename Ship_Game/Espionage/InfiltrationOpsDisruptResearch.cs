﻿using Ship_Game.Data.Serialization;

namespace Ship_Game
{

    [StarDataType]
    public class InfiltrationOpsDisruptResearch : InfiltrationOperation
    {
        [StarData] readonly Empire Owner;
        [StarData] readonly Empire Them;
        const float PercentOfLevelCost = 0.2f;
        const int SuccessTargetNumber = 40; // need to get 40 and above in a roll of d100)
        const float BaseRelationDamage = 15;
        const int BaseRampUpTurns = 40;

        public InfiltrationOpsDisruptResearch(Empire owner, Empire them, int levelCost, byte level) :
            base((int)(levelCost * PercentOfLevelCost), level, InfiltrationOpsType.PlantMole, BaseRampUpTurns, owner)
        {
            Owner = owner;
            Them = them;
        }

        public override void Update(float progressToUdpate)
        {
            base.Update(progressToUdpate);
            Owner.GetEspionage(Them).DecreaseSlowResearchChance();
        }

        public override void CompleteOperation()
        {
            InfiltrationOpsResolve aftermath = new InfiltrationOpsResolve(Owner, Them);
            var result = RollMissionResult(Owner, Them, Owner.IsAlliedWith(Them) ? (int)(SuccessTargetNumber * 0.75f) : SuccessTargetNumber, Level);
            Espionage espionage = Owner.GetEspionage(Them);
            switch (result)
            {
                case InfiltrationOpsResult.Phenomenal:
                    aftermath.GoodResult = true;
                    espionage.SetSlowResearchChance(125);
                    aftermath.Message = GameText.InfiltrationDisruptPhenomenalSuccess;
                    break;
                case InfiltrationOpsResult.GreatSuccess:
                    aftermath.GoodResult = true;
                    espionage.SetSlowResearchChance(100);
                    aftermath.Message = GameText.InfiltrationDisruptGreatSuccess;
                    break;
                case InfiltrationOpsResult.Success:
                    aftermath.GoodResult = true;
                    espionage.SetSlowResearchChance(80);
                    aftermath.Message = GameText.InfiltrationDisruptSuccess;
                    break;
                case InfiltrationOpsResult.Fail:
                    aftermath.Message = GameText.InfiltrationDisruptFail;
                    break;
                case InfiltrationOpsResult.MiserableFail:
                    aftermath.Message = GameText.InfiltrationDisruptMiserableFail;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.InfiltrationDisruptMiserableFailVictim)}";
                    espionage.ReduceInfiltrationLevel();
                    break;
                case InfiltrationOpsResult.CriticalFail:
                    aftermath.Message = GameText.InfiltrationDisruptCriticalFail;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.InfiltrationDisruptCriticalFailVictim)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage);
                    espionage.ReduceInfiltrationLevel();
                    aftermath.DamageReason = "Caught Spying";
                    break;
                case InfiltrationOpsResult.Disaster:
                    aftermath.Message = GameText.InfiltrationDisruptDisaster;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.InfiltrationDisruptDisasterVictim)}\n" +
                                                $"{Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}\n" +
                                                $"{Localizer.Token(GameText.TheirInfiltrationLevelWas)} {espionage.Level}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage, withLevelMultiplier: true);
                    aftermath.DamageReason = "Caught Spying Failed";
                    espionage.WipeoutInfiltration();
                    break;
            }

            if (aftermath.GoodResult)
                aftermath.MessageToVictim = Localizer.Token(GameText.InfiltrationDisruptSuccessVictim);

            aftermath.SendNotifications(Owner.Universe);
        }
    }
}
