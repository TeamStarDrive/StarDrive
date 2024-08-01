using Microsoft.Xna.Framework;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game
{

    [StarDataType]
    public class InfiltrationOpsRebellion : InfiltrationOperation
    {
        [StarData] readonly Empire Owner;
        [StarData] readonly Empire Them;
        public const float PercentOfLevelCost = 0.2f;
        const int SuccessTargetNumber = 50; // need to get 40 and above in a roll of d100)
        const float BaseRelationDamage = 20;
        public const int BaseRampUpTurns = 40;

        [StarDataConstructor]
        public InfiltrationOpsRebellion() { }

        public InfiltrationOpsRebellion(Empire owner, Empire them, int levelCost) :
            base((int)(levelCost * PercentOfLevelCost), InfiltrationOpsType.Rebellion, BaseRampUpTurns, owner)
        {
            Owner = owner;
            Them = them;
        }

        public override void CompleteOperation()
        {
            var result = RollMissionResult(Owner, Them, Owner.IsAlliedWith(Them) ? (int)(SuccessTargetNumber * 0.75f) : SuccessTargetNumber);
            InfiltrationOpsResolve aftermath = new InfiltrationOpsResolve(Owner, Them, result);
            Espionage espionage = Owner.GetEspionage(Them);
            var potentials = Them.GetPlanets().Sorted(p => p.PopulationBillion).TakeItems(5);
            Planet targetPlanet = Them.Random.Item(potentials);
            int numRebels = 5 + targetPlanet.GetDefendingTroopCount() + targetPlanet.NumMilitaryBuildings;
            float takeoverOrbitalChance = 50;

            switch (result)
            {
                case InfiltrationOpsResult.Phenomenal:
                    numRebels += 7;
                    takeoverOrbitalChance = 100;
                    break;
                case InfiltrationOpsResult.GreatSuccess:
                    numRebels += 3;
                    takeoverOrbitalChance = 75;
                    break;
                case InfiltrationOpsResult.Success:
                    break;
                case InfiltrationOpsResult.Fail:
                    aftermath.Message = GameText.NewFailedToInciteRebellion;
                    break;
                case InfiltrationOpsResult.MiserableFail:
                    aftermath.Message = GameText.NewFailedToInciteRebellionMiserable;
                    aftermath.MessageToVictim = Localizer.Token(GameText.InfiltrationRebellionMiserableFailVictim);
                    espionage.ReduceInfiltrationLevel();
                    break;
                case InfiltrationOpsResult.CriticalFail:
                    aftermath.Message = GameText.NewFailedToInciteRebellionDetected;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.InfiltrationRebellionCriticalFailVictim)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage);
                    espionage.ReduceInfiltrationLevel();
                    aftermath.DamageReason = "Caught Spying";
                    break;
                case InfiltrationOpsResult.Disaster:
                    aftermath.Message = GameText.FailedToInciteRebellionWipedOut;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.FailedToInciteRebellionWipedOut)}\n" +
                                                $"{Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}\n" +
                                                $"{Localizer.Token(GameText.TheirInfiltrationLevelWas)} {espionage.EffectiveLevel}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage, withLevelMultiplier: true);
                    aftermath.DamageReason = "Caught Spying Failed";
                    espionage.WipeoutInfiltration();
                    break;
            }

            if (aftermath.GoodResult)
            {
                aftermath.MessageToVictim = $"{Localizer.Token(GameText.IncitedUpriseOn)} {targetPlanet.Name}";
                aftermath.CustomMessage = $"{Localizer.Token(GameText.WeIncitedUprise)} {targetPlanet.Name} {Localizer.Token(GameText.NtheAgentWasNotDetected)}";
                Them.AddRebellion(targetPlanet, numRebels);
                TakeOverOrbitals(targetPlanet, takeoverOrbitalChance);
            }
            else
            {
                aftermath.MessageUseTheirName = true;
            }

            aftermath.SendNotifications(Owner.Universe);
        }

        public void TakeOverOrbitals(Planet targetPlanet, float takeoverOrbitalChance)
        {
            if (Them.TryGetRebels(out Empire rebels))
            {
                foreach (Ship orbital in targetPlanet.OrbitalStations)
                {
                    if (Them.Random.RollDice(takeoverOrbitalChance))
                    {
                        var troops = orbital.GetOurTroops();
                        foreach (Troop troop in troops)
                            troop.ChangeLoyalty(rebels);

                        orbital.LoyaltyChangeFromBoarding(rebels, false);
                    }
                }
            }
        }
    }
}
