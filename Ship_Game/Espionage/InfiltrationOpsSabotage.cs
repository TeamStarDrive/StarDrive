using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{

    [StarDataType]
    public class InfiltrationOpsSabotage : InfiltrationOperation
    {
        [StarData] readonly Empire Owner;
        [StarData] readonly Empire Them;
        const float PercentOfLevelCost = 0.2f;
        const int SuccessTargetNumber = 40; // need to get 40 and above in a roll of d100)
        const float BaseRelationDamage = 15;
        const int BaseRampUpTurns = 30;

        public InfiltrationOpsSabotage(Empire owner, Empire them, int levelCost, byte level) :
            base((int)(levelCost * PercentOfLevelCost), level, InfiltrationOpsType.PlantMole, BaseRampUpTurns, owner)
        {
            Owner = owner;
            Them = them;
        }

        public override void CompleteOperation()
        {
            InfiltrationOpsResolve aftermath = new InfiltrationOpsResolve(Owner, Them);
            var result = RollMissionResult(Owner, Them, Owner.IsAlliedWith(Them) ? (int)(SuccessTargetNumber * 0.75f) : SuccessTargetNumber, Level);
            Espionage espionage = Owner.GetEspionage(Them);
            Planet targetPlanet = null;
            int crippledTurns = (int)(25 * Them.Universe.ProductionPace);

            switch (result)
            {
                case InfiltrationOpsResult.Phenomenal:
                    aftermath.GoodResult = true;
                    targetPlanet  = Them.Random.Item(Them.GetPlanets().SortedDescending(p => p.Prod.GrossMaxPotential).TakeItems(5));
                    crippledTurns*= 2;
                    break;
                case InfiltrationOpsResult.GreatSuccess:
                    aftermath.GoodResult = true;
                    targetPlanet  = Them.Random.Item(Them.GetPlanets().SortedDescending(p => p.Prod.GrossMaxPotential).TakeItems(10));
                    crippledTurns = (int)(crippledTurns * 1.5f);
                    break;
                case InfiltrationOpsResult.Success:
                    aftermath.GoodResult = true;
                    targetPlanet = Them.Random.Item(Them.GetPlanets());
                    break;
                case InfiltrationOpsResult.Fail:
                    aftermath.Message = GameText.InfiltrationSabotageMessageFail;
                    break;
                case InfiltrationOpsResult.MiserableFail:
                    aftermath.CustomMessage   = $"{Localizer.Token(GameText.InfiltrationSabotageMessageFailMiserable)}";
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.InfiltrationSabotageMessageFailMiserableVictim)}";
                    espionage.ReduceInfiltrationLevel();
                    break;
                case InfiltrationOpsResult.CriticalFail:
                    aftermath.CustomMessage = $"{Localizer.Token(GameText.InfiltrationSabotageMessageFailCritical)}";
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.InfiltrationSabotageMessageFailMiserableVictim)} " +
                        $"{Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage);
                    espionage.ReduceInfiltrationLevel();
                    aftermath.DamageReason = "Caught Spying";
                    break;
                case InfiltrationOpsResult.Disaster:
                    aftermath.CustomMessage = $"{Localizer.Token(GameText.InfiltrationSabotageMessageFailDisaster)}";
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.InfiltrationSabotageMessageFailDisasterVictim)}\n" +
                                                $"{Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}\n" +
                                                $"{Localizer.Token(GameText.TheirInfiltrationLevelWas)} {espionage.Level}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage, withLevelMultiplier: true);
                    aftermath.DamageReason = "Caught Spying Failed";
                    espionage.WipeoutInfiltration();
                    break;
            }

            if (aftermath.GoodResult)
            {
                aftermath.Planet = targetPlanet;
                aftermath.MessageUseTheirName = true;
                aftermath.CustomMessage = $"{Localizer.Token(GameText.InfiltrationSabotageSuccessMessage)} {targetPlanet.Name} ({Them.data.Traits.Name})" +
                    $"\nfor {crippledTurns} {Localizer.Token(GameText.Turns3)}";
                aftermath.MessageToVictim = $"{targetPlanet.Name} {Localizer.Token(GameText.InfiltrationSabotageSuccessMessageVictim)} " +
                    $"{crippledTurns} {Localizer.Token(GameText.Turns3)}.";
                targetPlanet.AddCrippledTurns(crippledTurns);
            }
            else
            {
                aftermath.MessageUseTheirName = true;
            }

            aftermath.SendNotifications(Owner.Universe);
        }
    }
}
