using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.AI.Budget;
using Ship_Game.Commands.Goals;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace Ship_Game
{
    public partial class Empire
    {
        [XmlIgnore] [JsonIgnore] public int FreighterCap         => OwnedPlanets.Count * 3 + ResearchStrategy.ExpansionPriority;
        [XmlIgnore] [JsonIgnore] public int FreightersBeingBuilt => EmpireAI.Goals.Count(goal => goal is IncreaseFreighters);
        [XmlIgnore] [JsonIgnore] public int MaxFreightersInQueue => 1 + ResearchStrategy.IndustryPriority;
        [XmlIgnore] [JsonIgnore] public int TotalFreighters      => OwnedShips.Count(s => s.IsFreighter);
        [XmlIgnore] [JsonIgnore] public Ship[] IdleFreighters    => OwnedShips.Filter(s => s.IsIdleFreighter);
    }
}
