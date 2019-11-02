using System.Linq;
using Ship_Game.AI;

namespace Ship_Game
{
    public partial class ColonyScreen
    {
        
        public override void Update(float elapsedTime)
        {
            P.UpdateIncomes(false);
            UpdateBuildAndConstructLists(elapsedTime);
            UpdateButtonTimer(elapsedTime);

            if (ShipInfoOverlay.Visible && BuildableList.HighlightedIndex == -1)
                ShipInfoOverlay.Visible = false;

            GovOrbitals.Visible = P.Owner.isPlayer && GovernorDropdown.ActiveIndex != 0;
            GovMilitia.Visible = GovOrbitals.Visible;

            // not for trade hubs, which do not build structures anyway
            DontScrapBuildings.Visible = GovOrbitals.Visible && P.colonyType != Planet.ColonyType.TradeHub;

            base.Update(elapsedTime);
        }
        
        void UpdateButtonTimer(float elapsedTime)
        {
            ButtonUpdateTimer -= elapsedTime;
            if (ButtonUpdateTimer > 0f)
                return;

            ButtonUpdateTimer = 1;
            UpdateButtons();
            UpdateGovOrbitalStats();
        }
        
        void UpdateGovOrbitalStats()
        {
            if (P.Owner != Empire.Universe.player || !P.GovOrbitals || P.colonyType == Planet.ColonyType.Colony)
                return;

            Planet.WantedOrbitals wantedOrbitals = P.GovernorWantedOrbitals();
            PlatformsStats = $"Platforms: {P.NumPlatforms}/{wantedOrbitals.Platforms}";
            StationsStats  = $"Stations: {P.NumStations}/{wantedOrbitals.Stations}";
            ShipyardsStats = $"Shipyards: {P.NumShipyards}/{wantedOrbitals.Shipyards}";
        }

        void UpdateButtons()
        {
            // fbedard: Display button
            if (P.Owner == Empire.Universe.player)
            {
                int troopsLanding = P.Owner.GetShips()
                    .Filter(s => s.TroopList.Count > 0 && s.AI.State != AIState.Resupply && s.AI.State != AIState.Orbit)
                    .Count(troopAI => troopAI.AI.OrderQueue.Any(goal => goal.TargetPlanet != null && goal.TargetPlanet == P));

                if (troopsLanding > 0)
                {
                    CallTroops.Text = $"Incoming Troops: {troopsLanding}";
                    CallTroops.Style = ButtonStyle.Military;
                }
                else
                {
                    CallTroops.Text = "Call Troops";
                    CallTroops.Style = ButtonStyle.Default;
                }

                UpdateButtonText(LaunchAllTroops, P.TroopsHere.Count(t => t.CanMove), "Launch All Troops");
                UpdateButtonText(BuildPlatform, P.NumPlatforms, "Build Platform");
                UpdateButtonText(BuildStation, P.NumStations, "Build Station");
                UpdateButtonText(BuildShipyard, P.NumShipyards, "Build Shipyard");
            }

            CallTroops.Visible        = P.Owner == Empire.Universe.player;
            int numTroopsCanLaunch    = P.TroopsHere.Count(t => t.Loyalty == EmpireManager.Player && t.CanMove);
            LaunchSingleTroop.Visible = CallTroops.Visible && numTroopsCanLaunch > 0;
            LaunchAllTroops.Visible   = CallTroops.Visible && numTroopsCanLaunch > 1;
        }

        void UpdateButtonText(UIButton button, int value, string defaultText)
        {
            button.Text = value > 0 ? $"{defaultText} ({value})" : defaultText;
        }
    }
}