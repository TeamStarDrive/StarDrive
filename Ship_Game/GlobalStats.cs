using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace Ship_Game
{
	public class GlobalStats
	{
        public static int ComparisonCounter;

		public static int Comparisons;

		public static bool HardcoreRuleset;

		public static bool TakingInput;

		public static bool WarpInSystem;

		public static float FTLInSystemModifier;

        public static float EnemyFTLInSystemModifier;

		public static object ShieldLocker;

		public static object ClickableSystemsLock;

		public static object SensorNodeLocker;

		public static object BorderNodeLocker;

		public static object BombLock;

		public static object ObjectManagerLocker;

		public static object ExplosionLocker;

		public static object KnownShipsLock;

		public static object AddShipLocker;

		public static object BucketLock;

		public static object OwnedPlanetsLock;

		public static object DeepSpaceLock;

		public static object WayPointLock;

		public static object ClickableItemLocker;

		public static object NotificationLocker;

		public static object TaskLocker;

		public static object FleetButtonLocker;

		public static object BeamEffectLocker;

		public static Ship_Game.Config Config;

		public static System.Configuration.Configuration Configuration;

		public static bool ShowAllDesigns;

		public static int ModulesMoved;

		public static int DSCombatScans;

		public static int BeamTests;

		public static int ModuleUpdates;

		public static int WeaponArcChecks;

		public static int CombatScans;

		public static int DistanceCheckTotal;

		public static bool LimitSpeed;

		public static float GravityWellRange;

		public static bool PlanetaryGravityWells;

		public static bool AutoCombat;

		public static int TimesPlayed;

		public static ModEntry ActiveMod;

		public static string ResearchRootUIDToDisplay;

		public static bool ForceFullSim;

		public static int RemnantKills;

        public static int RemnantActivation;

		public static bool RemnantArmageddon;

		public static int CordrazinePlanetsCaptured;

        public static bool ExtraNotiofications;
        //PauseOnNotification
        public static bool PauseOnNotification;
        public static int ExtraPlanets;
        //OptionIncreaseShipMaintenance
        public static float OptionIncreaseShipMaintenance;
        public static float MinimumWarpRange;

        public static float MemoryLimiter;

        public static float StartingPlanetRichness;
        public static string ExtendedVersion;
        public static int IconSize;

        public static bool preventFederations;
        public static bool EliminationMode;

        public static int ShipCountLimit;
        public static float spaceroadlimit = .025f;
        public static float freighterlimit = 50f;
        public static int ScriptedTechWithin = 6;
        public static bool perf;
		static GlobalStats()
		{       
			GlobalStats.ComparisonCounter = 1;
			GlobalStats.Comparisons = 0;
			GlobalStats.HardcoreRuleset = false;
			GlobalStats.TakingInput = false;
			GlobalStats.WarpInSystem = true;
			GlobalStats.FTLInSystemModifier = 1f;
            GlobalStats.EnemyFTLInSystemModifier = 1f;
			GlobalStats.ShieldLocker = new object();
			GlobalStats.ClickableSystemsLock = new object();
			GlobalStats.SensorNodeLocker = new object();
			GlobalStats.BorderNodeLocker = new object();
			GlobalStats.BombLock = new object();
			GlobalStats.ObjectManagerLocker = new object();
			GlobalStats.ExplosionLocker = new object();
			GlobalStats.KnownShipsLock = new object();
			GlobalStats.AddShipLocker = new object();
			GlobalStats.BucketLock = new object();
			GlobalStats.OwnedPlanetsLock = new object();
			GlobalStats.DeepSpaceLock = new object();
			GlobalStats.WayPointLock = new object();
			GlobalStats.ClickableItemLocker = new object();
			GlobalStats.NotificationLocker = new object();
			GlobalStats.TaskLocker = new object();
			GlobalStats.FleetButtonLocker = new object();
			GlobalStats.BeamEffectLocker = new object();
			GlobalStats.ShowAllDesigns = true;
			GlobalStats.ModulesMoved = 0;
			GlobalStats.DSCombatScans = 0;
			GlobalStats.BeamTests = 0;
			GlobalStats.ModuleUpdates = 0;
			GlobalStats.WeaponArcChecks = 0;
			GlobalStats.CombatScans = 0;
			GlobalStats.DistanceCheckTotal = 0;
			GlobalStats.LimitSpeed = true;
            GlobalStats.GravityWellRange = float.Parse(ConfigurationManager.AppSettings["GravityWellRange"]); // 8000f;
			GlobalStats.PlanetaryGravityWells = true;
			GlobalStats.AutoCombat = true;
			GlobalStats.TimesPlayed = 0;
			GlobalStats.ResearchRootUIDToDisplay = "Colonization";
			GlobalStats.ForceFullSim = true;
			GlobalStats.RemnantKills = 0;
            GlobalStats.RemnantActivation = 0;
			GlobalStats.RemnantArmageddon = false;
			GlobalStats.CordrazinePlanetsCaptured = 0;
            GlobalStats.ExtraNotiofications = bool.Parse(ConfigurationManager.AppSettings["ExtraNotifications"]);
            GlobalStats.PauseOnNotification = bool.Parse(ConfigurationManager.AppSettings["PauseOnNotification"]);
            GlobalStats.ExtraPlanets = int.Parse(ConfigurationManager.AppSettings["ExtraPlanets"]);

            GlobalStats.MemoryLimiter = int.Parse(ConfigurationManager.AppSettings["MemoryLimiter"]);
            GlobalStats.MinimumWarpRange = int.Parse(ConfigurationManager.AppSettings["MinimumWarpRange"]);
            GlobalStats.StartingPlanetRichness = int.Parse(ConfigurationManager.AppSettings["StartingPlanetRichness"]);
            GlobalStats.OptionIncreaseShipMaintenance = int.Parse(ConfigurationManager.AppSettings["OptionIncreaseShipMaintenance"]);
            GlobalStats.ExtendedVersion = ConfigurationManager.AppSettings["ExtendedVersion"];
            GlobalStats.IconSize = int.Parse(ConfigurationManager.AppSettings["IconSize"]);
            GlobalStats.preventFederations = bool.Parse(ConfigurationManager.AppSettings["preventFederations"]);
            GlobalStats.ShipCountLimit = int.Parse(ConfigurationManager.AppSettings["shipcountlimit"]);
            GlobalStats.freighterlimit = int.Parse(ConfigurationManager.AppSettings["freighterlimit"]);
            GlobalStats.perf = bool.Parse(ConfigurationManager.AppSettings["perf"]);
		}
        public static void Statreset()
        {
            GlobalStats.ExtraNotiofications = bool.Parse(ConfigurationManager.AppSettings["ExtraNotifications"]);
            GlobalStats.PauseOnNotification = bool.Parse(ConfigurationManager.AppSettings["PauseOnNotification"]);
            GlobalStats.ExtraPlanets = int.Parse(ConfigurationManager.AppSettings["ExtraPlanets"]);

            GlobalStats.MemoryLimiter = int.Parse(ConfigurationManager.AppSettings["MemoryLimiter"]);
            GlobalStats.MinimumWarpRange = int.Parse(ConfigurationManager.AppSettings["MinimumWarpRange"]);
            GlobalStats.StartingPlanetRichness = int.Parse(ConfigurationManager.AppSettings["StartingPlanetRichness"]);
            GlobalStats.OptionIncreaseShipMaintenance = int.Parse(ConfigurationManager.AppSettings["OptionIncreaseShipMaintenance"]);
            GlobalStats.ExtendedVersion = ConfigurationManager.AppSettings["ExtendedVersion"];
            GlobalStats.IconSize = int.Parse(ConfigurationManager.AppSettings["IconSize"]);
            GlobalStats.preventFederations = bool.Parse(ConfigurationManager.AppSettings["preventFederations"]);
            GlobalStats.ShipCountLimit = int.Parse(ConfigurationManager.AppSettings["shipcountlimit"]);
            GlobalStats.EliminationMode = bool.Parse(ConfigurationManager.AppSettings["EliminationMode"]);
        }
		public GlobalStats()
		{
		}

		public static void IncrementCordrazineCapture()
		{
			GlobalStats.CordrazinePlanetsCaptured = GlobalStats.CordrazinePlanetsCaptured + 1;
			if (GlobalStats.CordrazinePlanetsCaptured == 1)
			{
				Ship.universeScreen.NotificationManager.AddEventNotification(ResourceManager.EventsDict["OwlwokFreedom"]);
			}
		}

		public static void IncrementRemnantKills()
		{
			GlobalStats.RemnantKills = GlobalStats.RemnantKills + 1;
            if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.mi.RemnantTechCount > 0)
            {
                if (GlobalStats.RemnantKills == 5 && GlobalStats.RemnantActivation < GlobalStats.ActiveMod.mi.RemnantTechCount)
                {
                    GlobalStats.RemnantActivation += 1;
                    Ship.universeScreen.NotificationManager.AddEventNotification(ResourceManager.EventsDict["RemnantTech1"]);
                    GlobalStats.RemnantKills = 0;
                }
            }
            else
            {
                if (GlobalStats.RemnantKills == 5)
                {
                    Ship.universeScreen.NotificationManager.AddEventNotification(ResourceManager.EventsDict["RemnantTech1"]);
                }
            }
		}
	}
}