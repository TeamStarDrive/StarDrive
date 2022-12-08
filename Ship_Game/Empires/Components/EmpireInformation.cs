using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;

namespace Ship_Game.Empires.Components
{
    [StarDataType]
    public class EmpireInformation
    {
        public enum InformationLevel
        {
            None,
            Minimal,
            Normal,
            High,
            Full
        }

        [StarData] public float EconomicStrength { get; private set; }
        [StarData] public float OffensiveStrength{ get; private set; }


        [StarData] public float AllianceEconomicStrength { get; private set; }
        [StarData] public float AllianceTotalStrength { get; private set; }


        Empire Them => Relation.Them;
        [StarData] Relationship Relation;

        [StarDataConstructor]
        public EmpireInformation(Relationship relation)
        {
            Relation = relation;
        }

        public void Update(InformationLevel knowledge)
        {
            switch(knowledge)
            {
                case InformationLevel.None:
                    break;
                case InformationLevel.Minimal:
                    break;
                case InformationLevel.Normal:
                    break;
                case InformationLevel.High:
                    break;
                case InformationLevel.Full:

                    EconomicStrength  = Them.AI.BuildCapacity;
                    OffensiveStrength = Them.OffensiveStrength;

                    AllianceEconomicStrength = EconomicStrength;
                    AllianceTotalStrength = OffensiveStrength;
                    var array = Them.Universe.GetAllies(Them);
                    for (int i = 0; i < array.Count; i++)
                    {
                        var empire = array[i];
                        AllianceTotalStrength += empire.CurrentMilitaryStrength;
                        AllianceEconomicStrength += empire.AI.BuildCapacity;
                    }


                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(knowledge), knowledge, null);
            }
        }
    }
}
