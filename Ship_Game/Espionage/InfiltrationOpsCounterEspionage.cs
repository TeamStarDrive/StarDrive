using Microsoft.Xna.Framework;
using SDUtils;
using Ship_Game.Data.Serialization;
using System.Linq;

namespace Ship_Game
{

    [StarDataType]
    public class InfiltrationOpsCounterEspionage : InfiltrationOperation
    {
        [StarData] readonly Empire Owner;
        [StarData] readonly Empire Them;
        const float PercentOfLevelCost = 0.2f;
        const int SuccessTargetNumber = 40; // need to get 40 and above in a roll of d100)
        const float BaseRelationDamage = 10;
        const int BaseRampUpTurns = 20;

        public InfiltrationOpsCounterEspionage(Empire owner, Empire them, int levelCost, byte level) :
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
            Espionage theirEspionage = Them.GetEspionage(Owner);
            var potentialMoles = Them.data.MoleList.Filter(m => !m.Sticky && Owner.GetPlanets().Any(p => p.Id == m.PlanetId));

            switch (result)
            {
                case InfiltrationOpsResult.Phenomenal:
                    aftermath.GoodResult = true;
                    if (theirEspionage.Level > 0)
                    {
                        theirEspionage.WipeoutInfiltration();
                        aftermath.CustomMessage = $"{Them.data.Traits.Name}: {Localizer.Token(GameText.CounterEspioangeOpsExposedWeWipedOut)}\n" +
                                                  $"{Localizer.Token(GameText.TheirInfiltrationLevelWas)} {theirEspionage.Level}";
                        aftermath.MessageToVictim = $"{Owner.data.Traits.Name}: {Localizer.Token(GameText.CounterEspioangeOpsExposedAndWipedOut)}";
                    }
                    else if (potentialMoles.Length > 0)
                    {
                        RemoveMole();
                    }

                    break;
                case InfiltrationOpsResult.GreatSuccess:
                    aftermath.GoodResult = true;
                    if (theirEspionage.Level > 0)
                    {
                        aftermath.Message = GameText.CounterEspioangeOpsWeExposedPartially;
                        aftermath.MessageToVictim = $"{Owner.data.Traits.Name}: {Localizer.Token(GameText.CounterEspioangeOpsWeExposedPartially)}";
                        theirEspionage.ReduceInfiltrationLevel();
                    }
                    else if (potentialMoles.Length > 0)
                    {
                        RemoveMole();
                    }

                    break;
                case InfiltrationOpsResult.Success:
                    aftermath.GoodResult = true;
                    if (potentialMoles.Length > 0)
                        RemoveMole();
                    break;
                case InfiltrationOpsResult.Fail:
                    aftermath.Message = Localizer.Token(GameText.CounterEspioangeOpsFailed);
                    break;
                case InfiltrationOpsResult.MiserableFail:
                    aftermath.Message = GameText.CounterEspioangeOpsFailedMiserably;
                    aftermath.MessageToVictim = Localizer.Token(GameText.InfiltrationCounterEspionageMiserableFailVictim);
                    espionage.ReduceInfiltrationLevel();
                    break;
                case InfiltrationOpsResult.CriticalFail:
                    aftermath.Message = GameText.CounterEspioangeOpsFailedDetected;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.CounterEspioangeOpsFailedAgentCaught)}\n{Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage);
                    espionage.ReduceInfiltrationLevel();
                    aftermath.DamageReason = "Caught Spying";
                    break;
                case InfiltrationOpsResult.Disaster:
                    aftermath.Message = GameText.CounterEspioangeOpsFailedWipeout;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.CounterEspioangeOpsFailedWipeoutVictim)}\n" +
                                                $"{Localizer.Token(GameText.NtheAgentWasSentBy)} {Owner.data.Traits.Name}\n" +
                                                $"{Localizer.Token(GameText.TheirInfiltrationLevelWas)} {espionage.Level}.";
                    aftermath.RelationDamage = CalcRelationDamage(BaseRelationDamage, espionage, withLevelMultiplier: true);
                    aftermath.DamageReason = "Caught Spying Failed";
                    espionage.WipeoutInfiltration();
                    break;
            }

            aftermath.SendNotifications(Owner.Universe);

            void RemoveMole()
            {
                Mole mole = Them.Random.Item(potentialMoles);
                aftermath.Planet = Them.Universe.GetPlanet(mole.PlanetId);
                aftermath.CustomMessage = $"{Localizer.Token(GameText.EliminatedMole)} {aftermath.Planet.Name}\n({Them.data.Traits.Name})";
                aftermath.MessageToVictim = $"{Localizer.Token(GameText.LostMole)} {aftermath.Planet.Name}";
                Them.RemoveMole(mole);
            }
        }
    }
}
