using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Outcome
	{
		private Planet SelectedPlanet;

		public bool BeginArmageddon;

		public int Chance;

		private Artifact grantedArtifact;

		public List<string> TroopsToSpawn;

		public List<string> FriendlyShipsToSpawn;

		public List<string> RemnantShipsToSpawn;

		public bool UnlockSecretBranch;

		public string SecretTechDiscovered;

		public string TitleText;

		public string UnlockTech;

		public bool WeHadIt;

		public bool GrantArtifact;

		public bool RemoveTrigger;

		public string ReplaceWith = "";

		public string DescriptionText;

		public int MoneyGranted;

		public List<string> TroopsGranted;

		public float FoodProductionBonus;

		public float IndustryBonus;

		public float ScienceBonus;

		public bool SelectRandomPlanet;

		public string SpawnBuildingOnPlanet;

		public string SpawnFleetInOrbitOfPlanet;

        public bool onlyTriggerOnce;

        public bool alreadyTriggered;

		public Outcome()
		{
		}

		public Artifact GetArtifact()
		{
			return this.grantedArtifact;
		}

		public Planet GetPlanet()
		{
			return this.SelectedPlanet;
		}

		public void SetArtifact(Artifact art)
		{
			this.grantedArtifact = art;
		}

		public void SetPlanet(Planet p)
		{
			this.SelectedPlanet = p;
		}
	}
}