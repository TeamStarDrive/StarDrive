using Ship_Game;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.Gameplay
{
	public class Relationship
	{
		public FederationQuest FedQuest;

		public Ship_Game.Gameplay.Posture Posture = Ship_Game.Gameplay.Posture.Neutral;

		public string Name;

		public bool Known;

		public float IntelligenceBudget;

		public float IntelligencePenetration;

		public int turnsSinceLastContact;

		public bool WarnedAboutShips;

		public bool WarnedAboutColonizing;

		public int EncounterStep;

		public float Anger_FromShipsInOurBorders;

		public float Anger_TerritorialConflict;

		public float Anger_MilitaryConflict;

		public float Anger_DiplomaticConflict;

		public int SpiesDetected;

		public int TimesSpiedOnAlly;

		public int SpiesKilled;

		public float TotalAnger;

		public bool Treaty_OpenBorders;

		public bool Treaty_NAPact;

		public bool Treaty_Trade;

		public int Treaty_Trade_TurnsExisted;

		public bool Treaty_Alliance;

		public bool Treaty_Peace;

		public int PeaceTurnsRemaining;

		public float Threat;

		public float Trust;

		public War ActiveWar;

		public List<War> WarHistory = new List<War>();

		public bool haveRejectedNAPact;

		public bool HaveRejected_TRADE;

		public bool haveRejectedDemandTech;

		public bool HaveRejected_OpenBorders;

		public bool HaveRejected_Alliance;

		public int NumberStolenClaims;

		public List<Guid> StolenSystems = new List<Guid>();

		public bool HaveInsulted_Military;

		public bool HaveComplimented_Military;

		public bool XenoDemandedTech;

		public List<Guid> WarnedSystemsList = new List<Guid>();

		public bool HaveWarnedTwice;

		public bool HaveWarnedThrice;

		public Guid contestedSystemGuid;

		private SolarSystem contestedSystem;

		public bool AtWar;

		public bool PreparingForWar;

		public WarType PreparingForWarType = WarType.ImperialistWar;

		public int DefenseFleet = -1;

		public bool HasDefenseFleet;

		public float InvasiveColonyPenalty;

		public float AggressionAgainstUsPenalty;

		public float InitialStrength;

		public int TurnsKnown;

		public int TurnsAbove95;

		public int TurnsAllied;

		public BatchRemovalCollection<TrustEntry> TrustEntries = new BatchRemovalCollection<TrustEntry>();

		public float TrustUsed;

		public BatchRemovalCollection<FearEntry> FearEntries = new BatchRemovalCollection<FearEntry>();

		public float FearUsed;

		public float TheyOweUs;

		public float WeOweThem;

		public bool HaveRejected_Demand_Tech
		{
			get
			{
				return this.haveRejectedDemandTech;
			}
			set
			{
				this.haveRejectedDemandTech = value;
				if (this.haveRejectedDemandTech)
				{
					Relationship trust = this;
					trust.Trust = trust.Trust - 20f;
					Relationship angerDiplomaticConflict = this;
					angerDiplomaticConflict.Anger_DiplomaticConflict = angerDiplomaticConflict.Anger_DiplomaticConflict + 20f;
					Relationship totalAnger = this;
					totalAnger.TotalAnger = totalAnger.TotalAnger + 20f;
				}
			}
		}

		public bool HaveRejected_NAPACT
		{
			get
			{
				return this.haveRejectedNAPact;
			}
			set
			{
				this.haveRejectedNAPact = value;
				if (this.haveRejectedNAPact)
				{
					Relationship trust = this;
					trust.Trust = trust.Trust - 20f;
				}
			}
		}

		public Relationship(string Name)
		{
			this.Name = Name;
		}

		public Relationship()
		{
		}

		public void DamageRelationship(Empire Us, Empire Them, string why, float Amount, Planet p)
		{
			if (Us.data.DiplomaticPersonality == null)
			{
				return;
			}
			string str = why;
			string str1 = str;
			if (str != null)
			{
				if (str1 == "Caught Spying")
				{
					Relationship angerDiplomaticConflict = this;
					angerDiplomaticConflict.Anger_DiplomaticConflict = angerDiplomaticConflict.Anger_DiplomaticConflict + Amount;
					Relationship totalAnger = this;
					totalAnger.TotalAnger = totalAnger.TotalAnger + Amount;
					Relationship trust = this;
					trust.Trust = trust.Trust - Amount;
					Relationship spiesDetected = this;
					spiesDetected.SpiesDetected = spiesDetected.SpiesDetected + 1;
					if (Us.data.DiplomaticPersonality.Name == "Honorable" || Us.data.DiplomaticPersonality.Name == "Xenophobic")
					{
						Relationship relationship = this;
						relationship.Anger_DiplomaticConflict = relationship.Anger_DiplomaticConflict + Amount;
						Relationship totalAnger1 = this;
						totalAnger1.TotalAnger = totalAnger1.TotalAnger + Amount;
						Relationship trust1 = this;
						trust1.Trust = trust1.Trust - Amount;
					}
					if (this.Treaty_Alliance)
					{
						Relationship timesSpiedOnAlly = this;
						timesSpiedOnAlly.TimesSpiedOnAlly = timesSpiedOnAlly.TimesSpiedOnAlly + 1;
						if (this.TimesSpiedOnAlly == 1)
						{
							if (EmpireManager.GetEmpireByName(Us.GetUS().PlayerLoyalty) == Them && !Us.isFaction)
							{
								Us.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(Us, Them, "Caught_Spying_Ally_1", true));
								return;
							}
						}
						else if (this.TimesSpiedOnAlly > 1)
						{
							if (EmpireManager.GetEmpireByName(Us.GetUS().PlayerLoyalty) == Them && !Us.isFaction)
							{
								Us.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(Us, Them, "Caught_Spying_Ally_2", true));
							}
							this.Treaty_Alliance = false;
							this.Treaty_NAPact = false;
							this.Treaty_OpenBorders = false;
							this.Treaty_Trade = false;
							this.Posture = Ship_Game.Gameplay.Posture.Hostile;
							return;
						}
					}
					else if (this.SpiesDetected == 1 && !this.AtWar && EmpireManager.GetEmpireByName(Us.GetUS().PlayerLoyalty) == Them && !Us.isFaction)
					{
						if (this.SpiesDetected == 1)
						{
							if (EmpireManager.GetEmpireByName(Us.GetUS().PlayerLoyalty) == Them && !Us.isFaction)
							{
								Us.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(Us, Them, "Caught_Spying_1", true));
								return;
							}
						}
						else if (this.SpiesDetected == 2)
						{
							if (EmpireManager.GetEmpireByName(Us.GetUS().PlayerLoyalty) == Them && !Us.isFaction)
							{
								Us.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(Us, Them, "Caught_Spying_2", true));
								return;
							}
						}
						else if (this.SpiesDetected >= 3)
						{
							if (EmpireManager.GetEmpireByName(Us.GetUS().PlayerLoyalty) == Them && !Us.isFaction)
							{
								Us.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(Us, Them, "Caught_Spying_3", true));
							}
							this.Treaty_Alliance = false;
							this.Treaty_NAPact = false;
							this.Treaty_OpenBorders = false;
							this.Treaty_Trade = false;
							this.Posture = Ship_Game.Gameplay.Posture.Hostile;
							return;
						}
					}
				}
				else if (str1 == "Caught Spying Failed")
				{
					Relationship angerDiplomaticConflict1 = this;
					angerDiplomaticConflict1.Anger_DiplomaticConflict = angerDiplomaticConflict1.Anger_DiplomaticConflict + Amount;
					Relationship relationship1 = this;
					relationship1.TotalAnger = relationship1.TotalAnger + Amount;
					Relationship trust2 = this;
					trust2.Trust = trust2.Trust - Amount;
					if (Us.data.DiplomaticPersonality.Name == "Honorable" || Us.data.DiplomaticPersonality.Name == "Xenophobic")
					{
						Relationship angerDiplomaticConflict2 = this;
						angerDiplomaticConflict2.Anger_DiplomaticConflict = angerDiplomaticConflict2.Anger_DiplomaticConflict + Amount;
						Relationship totalAnger2 = this;
						totalAnger2.TotalAnger = totalAnger2.TotalAnger + Amount;
						Relationship relationship2 = this;
						relationship2.Trust = relationship2.Trust - Amount;
					}
					Relationship spiesKilled = this;
					spiesKilled.SpiesKilled = spiesKilled.SpiesKilled + 1;
					if (this.Treaty_Alliance)
					{
						Relationship timesSpiedOnAlly1 = this;
						timesSpiedOnAlly1.TimesSpiedOnAlly = timesSpiedOnAlly1.TimesSpiedOnAlly + 1;
						if (this.TimesSpiedOnAlly == 1)
						{
							if (EmpireManager.GetEmpireByName(Us.GetUS().PlayerLoyalty) == Them && !Us.isFaction)
							{
								Us.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(Us, Them, "Caught_Spying_Ally_1", true));
								return;
							}
						}
						else if (this.TimesSpiedOnAlly > 1)
						{
							if (EmpireManager.GetEmpireByName(Us.GetUS().PlayerLoyalty) == Them && !Us.isFaction)
							{
								Us.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(Us, Them, "Caught_Spying_Ally_2", true));
							}
							this.Treaty_Alliance = false;
							this.Treaty_NAPact = false;
							this.Treaty_OpenBorders = false;
							this.Treaty_Trade = false;
							this.Posture = Ship_Game.Gameplay.Posture.Hostile;
							return;
						}
					}
					else if (EmpireManager.GetEmpireByName(Us.GetUS().PlayerLoyalty) == Them && !Us.isFaction)
					{
						Us.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(Us, Them, "Killed_Spy_1", true));
						return;
					}
				}
				else if (str1 == "Insulted")
				{
					Relationship angerDiplomaticConflict3 = this;
					angerDiplomaticConflict3.Anger_DiplomaticConflict = angerDiplomaticConflict3.Anger_DiplomaticConflict + Amount;
					Relationship totalAnger3 = this;
					totalAnger3.TotalAnger = totalAnger3.TotalAnger + Amount;
					Relationship trust3 = this;
					trust3.Trust = trust3.Trust - Amount;
					if (Us.data.DiplomaticPersonality.Name == "Honorable" || Us.data.DiplomaticPersonality.Name == "Xenophobic")
					{
						Relationship relationship3 = this;
						relationship3.Anger_DiplomaticConflict = relationship3.Anger_DiplomaticConflict + Amount;
						Relationship totalAnger4 = this;
						totalAnger4.TotalAnger = totalAnger4.TotalAnger + Amount;
						Relationship trust4 = this;
						trust4.Trust = trust4.Trust - Amount;
						return;
					}
				}
				else if (str1 == "Colonized Owned System")
				{
					List<Planet> OurTargetPlanets = new List<Planet>();
					List<Planet> TheirTargetPlanets = new List<Planet>();
					foreach (Goal g in Us.GetGSAI().Goals)
					{
						if (g.type != GoalType.Colonize)
						{
							continue;
						}
						OurTargetPlanets.Add(g.GetMarkedPlanet());
					}
					foreach (Planet theirp in Them.GetPlanets())
					{
						TheirTargetPlanets.Add(theirp);
					}
					bool MatchFound = false;
					SolarSystem sharedSystem = null;
					foreach (Planet planet in OurTargetPlanets)
					{
						foreach (Planet other in TheirTargetPlanets)
						{
							if (p == null || other == null || p.system != other.system)
							{
								continue;
							}
							sharedSystem = p.system;
							MatchFound = true;
							break;
						}
						if (!MatchFound || !Us.GetRelations()[Them].WarnedSystemsList.Contains(sharedSystem.guid))
						{
							continue;
						}
						return;
					}
					Relationship angerTerritorialConflict = this;
					angerTerritorialConflict.Anger_TerritorialConflict = angerTerritorialConflict.Anger_TerritorialConflict + Amount;
					Relationship relationship4 = this;
					relationship4.Trust = relationship4.Trust - Amount;
					if (this.Anger_TerritorialConflict < (float)Us.data.DiplomaticPersonality.Territorialism && !this.AtWar)
					{
						if (this.AtWar)
						{
							return;
						}
						if (EmpireManager.GetEmpireByName(Us.GetUS().PlayerLoyalty) == Them && !Us.isFaction)
						{
							if (!this.WarnedAboutShips)
							{
								Us.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(Us, Them, "Colonized Warning", p));
							}
							else if (!this.AtWar)
							{
								Us.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(Us, Them, "Warning Ships then Colonized", p));
							}
							this.turnsSinceLastContact = 0;
							this.WarnedAboutColonizing = true;
							this.contestedSystem = p.system;
							this.contestedSystemGuid = p.system.guid;
							return;
						}
					}
				}
				else
				{
					if (str1 != "Destroyed Ship")
					{
						return;
					}
					if (this.Anger_MilitaryConflict == 0f && !this.AtWar)
					{
						Relationship angerMilitaryConflict = this;
						angerMilitaryConflict.Anger_MilitaryConflict = angerMilitaryConflict.Anger_MilitaryConflict + Amount;
						Relationship trust5 = this;
						trust5.Trust = trust5.Trust - Amount;
						if (EmpireManager.GetEmpireByName(Us.GetUS().PlayerLoyalty) == Them && !Us.isFaction)
						{
							if (this.Anger_MilitaryConflict < 2f)
							{
								Us.GetUS().ScreenManager.AddScreen(new DiplomacyScreen(Us, Them, "Aggression Warning"));
							}
							Relationship relationship5 = this;
							relationship5.Trust = relationship5.Trust - Amount;
						}
					}
					Relationship angerMilitaryConflict1 = this;
					angerMilitaryConflict1.Anger_MilitaryConflict = angerMilitaryConflict1.Anger_MilitaryConflict + Amount;
				}
			}
		}

		public SolarSystem GetContestedSystem()
		{
			return Ship.universeScreen.SolarSystemDict[this.contestedSystemGuid];
		}

		public float GetStrength()
		{
			float Strength = this.InitialStrength - this.Anger_FromShipsInOurBorders - this.Anger_TerritorialConflict - this.Anger_MilitaryConflict - this.Anger_DiplomaticConflict + this.Trust;
			return Strength;
		}

		public void ImproveRelations(float TrustEarned, float Diplo_Anger_minus)
		{
			Relationship angerDiplomaticConflict = this;
			angerDiplomaticConflict.Anger_DiplomaticConflict = angerDiplomaticConflict.Anger_DiplomaticConflict - Diplo_Anger_minus;
			Relationship totalAnger = this;
			totalAnger.TotalAnger = totalAnger.TotalAnger - Diplo_Anger_minus;
			Relationship trust = this;
			trust.Trust = trust.Trust + TrustEarned;
			if (this.Trust > 100f && !this.Treaty_Alliance)
			{
				this.Trust = 100f;
				return;
			}
			if (this.Trust > 150f && this.Treaty_Alliance)
			{
				this.Trust = 150f;
			}
		}

		public void SetImperialistWar()
		{
			if (this.ActiveWar != null)
			{
				this.ActiveWar.WarType = WarType.ImperialistWar;
			}
		}

		public void SetInitialStrength(float n)
		{
			this.Trust = n;
			this.InitialStrength = 50f + n;
		}

		private void UpdateIntelligence(Empire us, Empire them)
		{
			if (us.Money > this.IntelligenceBudget && this.IntelligencePenetration < 100f)
			{
				Empire money = us;
				money.Money = money.Money - this.IntelligenceBudget;
				int molecount = 0;
				foreach (Mole mole in us.data.MoleList)
				{
					foreach (Planet p in them.GetPlanets())
					{
						if (p.guid != mole.PlanetGuid)
						{
							continue;
						}
						molecount++;
					}
				}
				Relationship intelligencePenetration = this;
				intelligencePenetration.IntelligencePenetration = intelligencePenetration.IntelligencePenetration + (this.IntelligenceBudget + this.IntelligenceBudget * (0.1f * (float)molecount + us.data.SpyModifier)) / 30f;
				if (this.IntelligencePenetration > 100f)
				{
					this.IntelligencePenetration = 100f;
				}
			}
		}

		public void UpdatePlayerRelations(Empire us, Empire them)
		{
			this.UpdateIntelligence(us, them);
			if (this.Treaty_Trade)
			{
				Relationship treatyTradeTurnsExisted = this;
				treatyTradeTurnsExisted.Treaty_Trade_TurnsExisted = treatyTradeTurnsExisted.Treaty_Trade_TurnsExisted + 1;
			}
			if (this.Treaty_Peace)
			{
				Relationship peaceTurnsRemaining = this;
				peaceTurnsRemaining.PeaceTurnsRemaining = peaceTurnsRemaining.PeaceTurnsRemaining - 1;
				if (this.PeaceTurnsRemaining <= 0)
				{
					this.Treaty_Peace = false;
					us.GetRelations()[them].Treaty_Peace = false;
					us.GetUS().NotificationManager.AddPeaceTreatyExpiredNotification(them);
				}
			}
		}

		public void UpdateRelationship(Empire us, Empire them)
		{
			if (us.data.Defeated)
			{
				return;
			}
			if (this.FedQuest != null)
			{
				if (this.FedQuest.type == QuestType.DestroyEnemy && EmpireManager.GetEmpireByName(this.FedQuest.EnemyName).data.Defeated)
				{
					DiplomacyScreen ds = new DiplomacyScreen(us, EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty), "Federation_YouDidIt_KilledEnemy", true)
					{
						empToDiscuss = EmpireManager.GetEmpireByName(this.FedQuest.EnemyName)
					};
					us.GetUS().ScreenManager.AddScreen(ds);
					EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).AbsorbEmpire(us);
					this.FedQuest = null;
					return;
				}
				if (this.FedQuest.type == QuestType.AllyFriend)
				{
					if (EmpireManager.GetEmpireByName(this.FedQuest.EnemyName).data.Defeated)
					{
						this.FedQuest = null;
					}
					else if (EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetRelations()[EmpireManager.GetEmpireByName(this.FedQuest.EnemyName)].Treaty_Alliance)
					{
						DiplomacyScreen ds = new DiplomacyScreen(us, EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty), "Federation_YouDidIt_AllyFriend", true)
						{
							empToDiscuss = EmpireManager.GetEmpireByName(this.FedQuest.EnemyName)
						};
						us.GetUS().ScreenManager.AddScreen(ds);
						EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).AbsorbEmpire(us);
						this.FedQuest = null;
						return;
					}
				}
			}
			if (this.Posture == Ship_Game.Gameplay.Posture.Hostile && this.Trust > 50f && this.TotalAnger < 10f)
			{
				this.Posture = Ship_Game.Gameplay.Posture.Neutral;
			}
			if (them.isFaction)
			{
				this.AtWar = false;
			}
			this.UpdateIntelligence(us, them);
			if (this.AtWar && this.ActiveWar != null)
			{
				War activeWar = this.ActiveWar;
				activeWar.TurnsAtWar = activeWar.TurnsAtWar + 1f;
			}
			foreach (TrustEntry te in this.TrustEntries)
			{
				TrustEntry turnsInExistence = te;
				turnsInExistence.TurnsInExistence = turnsInExistence.TurnsInExistence + 1;
				if (te.TurnTimer == 0 || te.TurnsInExistence <= 250)
				{
					continue;
				}
				this.TrustEntries.QueuePendingRemoval(te);
			}
			this.TrustEntries.ApplyPendingRemovals();
			foreach (FearEntry te in this.FearEntries)
			{
				FearEntry fearEntry = te;
				fearEntry.TurnsInExistence = fearEntry.TurnsInExistence + 1f;
				if (te.TurnTimer == 0 || te.TurnsInExistence <= 250f)
				{
					continue;
				}
				this.FearEntries.QueuePendingRemoval(te);
			}
			this.FearEntries.ApplyPendingRemovals();
			if (!this.Treaty_Alliance)
			{
				this.TurnsAllied = 0;
			}
			else
			{
				Relationship turnsAllied = this;
				turnsAllied.TurnsAllied = turnsAllied.TurnsAllied + 1;
			}
			DTrait dt = us.data.DiplomaticPersonality;
			if (this.Posture == Ship_Game.Gameplay.Posture.Friendly)
			{
				Relationship trust = this;
				trust.Trust = trust.Trust + dt.TrustGainedAtPeace;
				if (this.Trust > 100f && !us.GetRelations()[them].Treaty_Alliance)
				{
					this.Trust = 100f;
				}
				else if (this.Trust > 150f && us.GetRelations()[them].Treaty_Alliance)
				{
					this.Trust = 150f;
				}
			}
			else if (this.Posture == Ship_Game.Gameplay.Posture.Hostile)
			{
				Relationship relationship = this;
				relationship.Trust = relationship.Trust - dt.TrustGainedAtPeace;
			}
			if (this.Treaty_NAPact)
			{
				Relationship trust1 = this;
				trust1.Trust = trust1.Trust + 0.0125f;
			}
			if (this.Treaty_OpenBorders)
			{
				Relationship relationship1 = this;
				relationship1.Trust = relationship1.Trust + 0.0125f;
			}
			if (this.Treaty_Trade)
			{
				Relationship trust2 = this;
				trust2.Trust = trust2.Trust + 0.0125f;
				Relationship treatyTradeTurnsExisted = this;
				treatyTradeTurnsExisted.Treaty_Trade_TurnsExisted = treatyTradeTurnsExisted.Treaty_Trade_TurnsExisted + 1;
			}
			if (this.Treaty_Peace)
			{
				Relationship peaceTurnsRemaining = this;
				peaceTurnsRemaining.PeaceTurnsRemaining = peaceTurnsRemaining.PeaceTurnsRemaining - 1;
				if (this.PeaceTurnsRemaining <= 0)
				{
					this.Treaty_Peace = false;
					us.GetRelations()[them].Treaty_Peace = false;
				}
				Relationship angerDiplomaticConflict = this;
				angerDiplomaticConflict.Anger_DiplomaticConflict = angerDiplomaticConflict.Anger_DiplomaticConflict - 0.1f;
				Relationship angerFromShipsInOurBorders = this;
				angerFromShipsInOurBorders.Anger_FromShipsInOurBorders = angerFromShipsInOurBorders.Anger_FromShipsInOurBorders - 0.1f;
				Relationship angerMilitaryConflict = this;
				angerMilitaryConflict.Anger_MilitaryConflict = angerMilitaryConflict.Anger_MilitaryConflict - 0.1f;
				Relationship angerTerritorialConflict = this;
				angerTerritorialConflict.Anger_TerritorialConflict = angerTerritorialConflict.Anger_TerritorialConflict - 0.1f;
			}
			if (this.Trust <= 95f)
			{
				this.TurnsAbove95 = 0;
			}
			else
			{
				Relationship turnsAbove95 = this;
				turnsAbove95.TurnsAbove95 = turnsAbove95.TurnsAbove95 + 1;
			}
			this.TrustUsed = 0f;
			foreach (TrustEntry te in this.TrustEntries)
			{
				Relationship trustUsed = this;
				trustUsed.TrustUsed = trustUsed.TrustUsed + te.TrustCost;
			}
			foreach (FearEntry te in this.FearEntries)
			{
				Relationship fearUsed = this;
				fearUsed.FearUsed = fearUsed.FearUsed + te.FearCost;
			}
            //foreach (Ship ship in us.GetShipsInOurBorders())
            //{
            //    if (ship.loyalty != them || them.GetRelations()[us].Treaty_OpenBorders || this.Treaty_Alliance)
            //    {
            //        continue;
            //    }
            //    if (!this.Treaty_NAPact)
            //    {
            //        Relationship angerFromShipsInOurBorders1 = this;
            //        angerFromShipsInOurBorders1.Anger_FromShipsInOurBorders = angerFromShipsInOurBorders1.Anger_FromShipsInOurBorders + (100f - this.Trust) / 100f * (float)ship.Size / 150f;
            //    }
            //    else
            //    {
            //        Relationship angerFromShipsInOurBorders2 = this;
            //        angerFromShipsInOurBorders2.Anger_FromShipsInOurBorders = angerFromShipsInOurBorders2.Anger_FromShipsInOurBorders + (100f - this.Trust) / 100f * (float)ship.Size / 300f;
            //    }
            //}

            foreach (Ship shipsInOurBorder in us.GetShipsInOurBorders().Where(ship => ship.loyalty != null && ship.loyalty != us && !ship.loyalty.isFaction))
            {
                //shipsInOurBorder.WeaponCentered = false;
                //added by gremlin: maintenance in enemy space
                if (shipsInOurBorder.loyalty != them || them.GetRelations()[us].Treaty_OpenBorders || this.Treaty_Alliance)
                {
                    if (shipsInOurBorder.loyalty == them && (them.GetRelations()[us].Treaty_OpenBorders))
                    {
                        shipsInOurBorder.isCloaking = true;
                        if (this.Treaty_Alliance)
                        {
                            shipsInOurBorder.isCloaked = true;
                        }

                    }
                    continue;

                }

                if (!this.Treaty_NAPact)
                {

                    Relationship angerFromShipsInOurBorders1 = this;
                    angerFromShipsInOurBorders1.Anger_FromShipsInOurBorders = angerFromShipsInOurBorders1.Anger_FromShipsInOurBorders + (100f - this.Trust) / 100f * (float)shipsInOurBorder.Size / 150f;
                    shipsInOurBorder.isDecloaking = true;
                }
                else
                {
                    Relationship angerFromShipsInOurBorders2 = this;
                    angerFromShipsInOurBorders2.Anger_FromShipsInOurBorders = angerFromShipsInOurBorders2.Anger_FromShipsInOurBorders + (100f - this.Trust) / 100f * (float)shipsInOurBorder.Size / 300f;

                }
            }

			float OurMilScore = 230f + us.MilitaryScore;
			float TheirMilScore = 230f + them.MilitaryScore;
			this.Threat = (1f - OurMilScore / TheirMilScore) * 100f;
			if (this.Threat > 100f)
			{
				this.Threat = 100f;
			}
			if (us.MilitaryScore < 1000f)
			{
				this.Threat = 0f;
			}
			if (this.Trust > 100f && !us.GetRelations()[them].Treaty_Alliance)
			{
				this.Trust = 100f;
			}
			else if (this.Trust > 150f && us.GetRelations()[them].Treaty_Alliance)
			{
				this.Trust = 150f;
			}
			Relationship initialStrength = this;
			initialStrength.InitialStrength = initialStrength.InitialStrength + dt.NaturalRelChange;
			if (this.Anger_TerritorialConflict > 0f)
			{
				Relationship angerTerritorialConflict1 = this;
				angerTerritorialConflict1.Anger_TerritorialConflict = angerTerritorialConflict1.Anger_TerritorialConflict - dt.AngerDissipation;
			}
			if (this.Anger_TerritorialConflict < 0f)
			{
				this.Anger_TerritorialConflict = 0f;
			}
			if (this.Anger_FromShipsInOurBorders > 100f)
			{
				this.Anger_FromShipsInOurBorders = 100f;
			}
			if (this.Anger_FromShipsInOurBorders > 0f)
			{
				Relationship relationship2 = this;
				relationship2.Anger_FromShipsInOurBorders = relationship2.Anger_FromShipsInOurBorders - dt.AngerDissipation;
			}
			if (this.Anger_FromShipsInOurBorders < 0f)
			{
				this.Anger_FromShipsInOurBorders = 0f;
			}
			if (this.Anger_MilitaryConflict > 0f)
			{
				Relationship angerTerritorialConflict2 = this;
				angerTerritorialConflict2.Anger_TerritorialConflict = angerTerritorialConflict2.Anger_TerritorialConflict - dt.AngerDissipation;
			}
			if (this.Anger_MilitaryConflict < 0f)
			{
				this.Anger_MilitaryConflict = 0f;
			}
			if (this.Anger_DiplomaticConflict > 0f)
			{
				Relationship angerDiplomaticConflict1 = this;
				angerDiplomaticConflict1.Anger_DiplomaticConflict = angerDiplomaticConflict1.Anger_DiplomaticConflict - dt.AngerDissipation;
			}
			if (this.Anger_DiplomaticConflict < 0f)
			{
				this.Anger_DiplomaticConflict = 0f;
			}
			this.TotalAnger = 0f;
			Relationship totalAnger = this;
			totalAnger.TotalAnger = totalAnger.TotalAnger + this.Anger_DiplomaticConflict;
			Relationship totalAnger1 = this;
			totalAnger1.TotalAnger = totalAnger1.TotalAnger + this.Anger_FromShipsInOurBorders;
			Relationship totalAnger2 = this;
			totalAnger2.TotalAnger = totalAnger2.TotalAnger + this.Anger_MilitaryConflict;
			Relationship totalAnger3 = this;
			totalAnger3.TotalAnger = totalAnger3.TotalAnger + this.Anger_TerritorialConflict;
			Relationship turnsKnown = this;
			turnsKnown.TurnsKnown = turnsKnown.TurnsKnown + 1;
			Relationship relationship3 = this;
			relationship3.turnsSinceLastContact = relationship3.turnsSinceLastContact + 1;
		}
	}
}