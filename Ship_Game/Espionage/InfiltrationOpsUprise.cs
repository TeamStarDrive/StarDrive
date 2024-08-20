using Microsoft.Xna.Framework;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{

    [StarDataType]
    public class InfiltrationOpsUprise : InfiltrationOperation
    {
        [StarData] readonly Empire Owner;
        [StarData] readonly Empire Them;
        public const float PercentOfLevelCost = 0.5f;
        const int SuccessTargetNumber  = 40; // need to get 40 and above in a roll of d100)
        const float BaseRelationDamage = 5;
        public const int BaseRampUpTurns = 40;

        [StarDataConstructor]
        public InfiltrationOpsUprise() { }

        public InfiltrationOpsUprise(Empire owner, Empire them, int levelCost) :
            base((int)(levelCost * PercentOfLevelCost), InfiltrationOpsType.Uprise, BaseRampUpTurns, owner)
        {
            Owner = owner;
            Them = them;
        }

        public override void CompleteOperation()
        {
            var result = RollMissionResult(Owner, Them, Owner.IsAlliedWith(Them) ? (int)(SuccessTargetNumber * 0.75f) : SuccessTargetNumber);
            InfiltrationOpsResolve aftermath = new InfiltrationOpsResolve(Owner, Them, result);
            Espionage espionage = Owner.GetEspionage(Them);
            int planetsToTake = 20;
            int damageDieBonus = 0;
            switch (result)
            {
                case InfiltrationOpsResult.Phenomenal:
                    planetsToTake = 5;
                    damageDieBonus = 4;
                    break;
                case InfiltrationOpsResult.GreatSuccess:
                    planetsToTake = 10;
                    damageDieBonus = 2;
                    break; 
                case InfiltrationOpsResult.Success:
                    break;
                case InfiltrationOpsResult.Fail:
                    aftermath.Message = GameText.NewFailedToInciteUprise;
                    break;
                case InfiltrationOpsResult.MiserableFail:
                    aftermath.Message = GameText.NewFailedToInciteUpriseMiserable;
                    aftermath.MessageToVictim = Localizer.Token(GameText.InfiltrationUpriseMiserableFailVictim);
                    espionage.ReduceInfiltrationLevel();
                    break;
                case InfiltrationOpsResult.CriticalFail:
                    aftermath.Message = GameText.NewFailedToInciteUpriseDetected;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.InfiltrationUpriseCriticalFailVictim)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage);
                    espionage.ReduceInfiltrationLevel();
                    aftermath.DamageReason = "Caught Spying";
                    break;
                case InfiltrationOpsResult.Disaster:
                    aftermath.Message = GameText.FailedToInciteUpriseWipedOut;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.NewWipedOutNetworkUprise)}" +
                                                $"{Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}\n" +
                                                $"{Localizer.Token(GameText.TheirInfiltrationLevelWas)} {espionage.EffectiveLevel}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage, withLevelMultiplier: true);
                    aftermath.DamageReason = "Caught Spying Failed";
                    espionage.WipeoutInfiltration();
                    break;
            }

            if (aftermath.GoodResult)
            {
                Planet[] potentials = potentials = Them.GetPlanets().SortedDescending(p => p.ProdHere + p.FoodHere).TakeItems(planetsToTake);
                Planet targetPlanet = Them.Random.Item(potentials);
                UpriseBuildingType typeToDestroy = UpriseBuildingType.None;
                bool removeProd = true;
                bool removeFood = false;
                float fertilityReduction = 1f;
                int damageResult = Owner.Random.RollDie(4) + damageDieBonus;

                if (damageResult >= 2) typeToDestroy = UpriseBuildingType.Random;
                if (damageResult >= 3) typeToDestroy = UpriseBuildingType.Storage;
                if (damageResult >= 4) fertilityReduction = 0.75f;
                if (damageResult >= 5) removeFood = true;
                if (damageResult >= 6) typeToDestroy = UpriseBuildingType.HighestPrice;
                if (damageResult >= 7) typeToDestroy = UpriseBuildingType.AllMilitary;
                if (damageResult >= 8) fertilityReduction = 0.5f;

                targetPlanet.DestroyBuildingInUprise(typeToDestroy, out string buildingDestroyed);

                string assetsLost = "";
                if (removeProd && targetPlanet.ProdHere > 1)
                {
                    assetsLost += $"{targetPlanet.ProdHere.String(0)} {Localizer.Token(GameText.Production)}. ";
                    targetPlanet.ProdHere = 0;
                }

                if (removeFood && Them.NonCybernetic && targetPlanet.FoodHere > 1)
                {
                    assetsLost += $"{targetPlanet.FoodHere.String(0)} {Localizer.Token(GameText.Food)}. ";
                    targetPlanet.FoodHere = 0;
                }

                if (buildingDestroyed.NotEmpty())
                {
                    assetsLost += $"{buildingDestroyed}. ";
                }

                if (targetPlanet.Fertility > 0 && fertilityReduction.NotEqual(1f))
                {
                    float fertilityLost = targetPlanet.Fertility * fertilityReduction;
                    assetsLost += $"{(fertilityLost).String(2)} {Localizer.Token(GameText.Fertility)}. ";
                    targetPlanet.AddBaseFertility(-fertilityLost);
                }

                aftermath.MessageToVictim = $"{targetPlanet.Name}: {Localizer.Token(GameText.IncitedUpriseOn)} {assetsLost}";
                aftermath.CustomMessage = $"{Localizer.Token(GameText.WeIncitedUprise)} {targetPlanet.Name} " +
                    $"{Localizer.Token(GameText.NtheAgentWasNotDetected)}\n " +
                    $"{Localizer.Token(GameText.UpriseAssetsTheyLost)} " +
                    $"{assetsLost}";
            }

            aftermath.SendNotifications(Owner.Universe);
        }
    }

    public enum UpriseBuildingType
    {
        HighestPrice,
        Storage,
        Random,
        AllMilitary,
        None, // skip destruction
    }
}
