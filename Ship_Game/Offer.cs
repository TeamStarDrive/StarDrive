using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Offer
	{
		public List<string> TechnologiesOffered = new List<string>();

		public List<string> ArtifactsOffered = new List<string>();

		public Ref<bool> ValueToModify;

		public bool PeaceTreaty;

		public bool Alliance;

		public string AcceptDL;

		public string RejectDL;

		public List<string> ColoniesOffered = new List<string>();

		public bool NAPact;

		public List<string> EmpiresToWarOn = new List<string>();

		public List<string> EmpiresToMakePeaceWith = new List<string>();

		public bool OpenBorders;

		public bool TradeTreaty;

		private string OfferText = "";

		public Empire Them;

		public Offer()
		{
		}

		public string DoPleadingText(Offer.Attitude a, Offer TheirOffer)
		{
			this.OfferText = "";
			if (this.PeaceTreaty)
			{
				Offer offer = this;
				offer.OfferText = string.Concat(offer.OfferText, Localizer.Token(3022));
				Offer offer1 = this;
				offer1.OfferText = string.Concat(offer1.OfferText, "\n\n");
			}
			if (this.Alliance)
			{
				Offer offer2 = this;
				offer2.OfferText = string.Concat(offer2.OfferText, Localizer.Token(3023));
				Offer offer3 = this;
				offer3.OfferText = string.Concat(offer3.OfferText, "\n\n");
			}
			if (this.OpenBorders)
			{
				if (!TheirOffer.OpenBorders)
				{
					Offer offer4 = this;
					offer4.OfferText = string.Concat(offer4.OfferText, Localizer.Token(3025));
				}
				else
				{
					Offer offer5 = this;
					offer5.OfferText = string.Concat(offer5.OfferText, Localizer.Token(3024));
				}
				if (this.NAPact)
				{
					Offer offer6 = this;
					offer6.OfferText = string.Concat(offer6.OfferText, Localizer.Token(3026));
				}
			}
			else if (TheirOffer.OpenBorders)
			{
				Offer offer7 = this;
				offer7.OfferText = string.Concat(offer7.OfferText, Localizer.Token(3027));
				if (this.NAPact)
				{
					Offer offer8 = this;
					offer8.OfferText = string.Concat(offer8.OfferText, Localizer.Token(3028));
				}
			}
			else if (this.NAPact)
			{
				Offer offer9 = this;
				offer9.OfferText = string.Concat(offer9.OfferText, Localizer.Token(3029));
			}
			if (this.ArtifactsOffered.Count > 0)
			{
				Offer offer10 = this;
				offer10.OfferText = string.Concat(offer10.OfferText, Localizer.Token(3030));
				if (this.ArtifactsOffered.Count == 1)
				{
					Offer offer11 = this;
					offer11.OfferText = string.Concat(offer11.OfferText, this.ArtifactsOffered[0], ". ");
				}
				else if (this.ArtifactsOffered.Count != 2)
				{
					for (int i = 0; i < this.ArtifactsOffered.Count; i++)
					{
						if (i >= this.ArtifactsOffered.Count - 1)
						{
							Offer offer12 = this;
							offer12.OfferText = string.Concat(offer12.OfferText, Localizer.Token(3013));
							Offer offer13 = this;
							offer13.OfferText = string.Concat(offer13.OfferText, this.ArtifactsOffered[i], ". ");
						}
						else
						{
							Offer offer14 = this;
							offer14.OfferText = string.Concat(offer14.OfferText, this.ArtifactsOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer15 = this;
					offer15.OfferText = string.Concat(offer15.OfferText, this.ArtifactsOffered[0], Localizer.Token(3011));
					Offer offer16 = this;
					offer16.OfferText = string.Concat(offer16.OfferText, this.ArtifactsOffered[1], ". ");
				}
			}
			if (TheirOffer.ArtifactsOffered.Count > 0)
			{
				Offer offer17 = this;
				offer17.OfferText = string.Concat(offer17.OfferText, Localizer.Token(3031));
				if (TheirOffer.ArtifactsOffered.Count == 1)
				{
					Offer offer18 = this;
					offer18.OfferText = string.Concat(offer18.OfferText, TheirOffer.ArtifactsOffered[0], ". ");
				}
				else if (TheirOffer.ArtifactsOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.ArtifactsOffered.Count; i++)
					{
						if (i >= TheirOffer.ArtifactsOffered.Count - 1)
						{
							Offer offer19 = this;
							offer19.OfferText = string.Concat(offer19.OfferText, Localizer.Token(3013));
							Offer offer20 = this;
							offer20.OfferText = string.Concat(offer20.OfferText, TheirOffer.ArtifactsOffered[i], ". ");
						}
						else
						{
							Offer offer21 = this;
							offer21.OfferText = string.Concat(offer21.OfferText, TheirOffer.ArtifactsOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer22 = this;
					offer22.OfferText = string.Concat(offer22.OfferText, TheirOffer.ArtifactsOffered[0], Localizer.Token(3032));
					Offer offer23 = this;
					offer23.OfferText = string.Concat(offer23.OfferText, TheirOffer.ArtifactsOffered[1], ". ");
				}
			}
			if (this.TradeTreaty)
			{
				if (this.NAPact || this.OpenBorders || TheirOffer.OpenBorders)
				{
					Offer offer24 = this;
					offer24.OfferText = string.Concat(offer24.OfferText, "\n\n");
					Offer offer25 = this;
					offer25.OfferText = string.Concat(offer25.OfferText, Localizer.Token(3033));
				}
				else
				{
					Offer offer26 = this;
					offer26.OfferText = string.Concat(offer26.OfferText, Localizer.Token(3034));
				}
			}
			if (this.TradeTreaty || this.OpenBorders || TheirOffer.OpenBorders || this.NAPact)
			{
				Offer offer27 = this;
				offer27.OfferText = string.Concat(offer27.OfferText, "\n\n");
			}
			if (this.TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count == 0)
			{
				Offer offer28 = this;
				offer28.OfferText = string.Concat(offer28.OfferText, Localizer.Token(3035));
				if (this.TechnologiesOffered.Count == 1)
				{
					Offer offer29 = this;
					offer29.OfferText = string.Concat(offer29.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[0]].NameIndex), ". ");
				}
				else if (this.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < this.TechnologiesOffered.Count; i++)
					{
						if (i >= this.TechnologiesOffered.Count - 1)
						{
							Offer offer30 = this;
							offer30.OfferText = string.Concat(offer30.OfferText, Localizer.Token(3013));
							Offer offer31 = this;
							offer31.OfferText = string.Concat(offer31.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[i]].NameIndex), ". ");
						}
						else
						{
							Offer offer32 = this;
							offer32.OfferText = string.Concat(offer32.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[i]].NameIndex), ", ");
						}
					}
				}
				else
				{
					Offer offer33 = this;
					offer33.OfferText = string.Concat(offer33.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[0]].NameIndex), Localizer.Token(3011));
					Offer offer34 = this;
					offer34.OfferText = string.Concat(offer34.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[1]].NameIndex), ". ");
				}
			}
			else if (this.TechnologiesOffered.Count == 0 && TheirOffer.TechnologiesOffered.Count > 0)
			{
				Offer offer35 = this;
				offer35.OfferText = string.Concat(offer35.OfferText, Localizer.Token(3036));
				if (TheirOffer.TechnologiesOffered.Count == 1)
				{
					Offer offer36 = this;
					offer36.OfferText = string.Concat(offer36.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[0]].NameIndex), "? ");
				}
				else if (TheirOffer.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.TechnologiesOffered.Count; i++)
					{
						if (i >= this.TechnologiesOffered.Count - 1)
						{
							Offer offer37 = this;
							offer37.OfferText = string.Concat(offer37.OfferText, Localizer.Token(3013));
							Offer offer38 = this;
							offer38.OfferText = string.Concat(offer38.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[i]].NameIndex), "? ");
						}
						else
						{
							Offer offer39 = this;
							offer39.OfferText = string.Concat(offer39.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[i]].NameIndex), ", ");
						}
					}
				}
				else
				{
					Offer offer40 = this;
					offer40.OfferText = string.Concat(offer40.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[0]].NameIndex), Localizer.Token(3011));
					Offer offer41 = this;
					offer41.OfferText = string.Concat(offer41.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[1]].NameIndex), "? ");
				}
			}
			else if (this.TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count > 0)
			{
				Offer offer42 = this;
				offer42.OfferText = string.Concat(offer42.OfferText, Localizer.Token(3037));
				if (TheirOffer.TechnologiesOffered.Count == 1)
				{
					Offer offer43 = this;
					offer43.OfferText = string.Concat(offer43.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[0]].NameIndex), ". ");
				}
				else if (TheirOffer.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.TechnologiesOffered.Count; i++)
					{
						if (i >= this.TechnologiesOffered.Count - 1)
						{
							Offer offer44 = this;
							offer44.OfferText = string.Concat(offer44.OfferText, Localizer.Token(3013));
							Offer offer45 = this;
							offer45.OfferText = string.Concat(offer45.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[i]].NameIndex), ". ");
						}
						else
						{
							Offer offer46 = this;
							offer46.OfferText = string.Concat(offer46.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[i]].NameIndex), ", ");
						}
					}
				}
				else
				{
					Offer offer47 = this;
					offer47.OfferText = string.Concat(offer47.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[0]].NameIndex), Localizer.Token(3011));
					Offer offer48 = this;
					offer48.OfferText = string.Concat(offer48.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[1]].NameIndex), ". ");
				}
				Offer offer49 = this;
				offer49.OfferText = string.Concat(offer49.OfferText, Localizer.Token(3038));
				if (this.TechnologiesOffered.Count == 1)
				{
					Offer offer50 = this;
					offer50.OfferText = string.Concat(offer50.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[0]].NameIndex), ". ");
				}
				else if (this.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < this.TechnologiesOffered.Count; i++)
					{
						if (i >= this.TechnologiesOffered.Count - 1)
						{
							Offer offer51 = this;
							offer51.OfferText = string.Concat(offer51.OfferText, Localizer.Token(3013));
							Offer offer52 = this;
							offer52.OfferText = string.Concat(offer52.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[i]].NameIndex), ". ");
						}
						else
						{
							Offer offer53 = this;
							offer53.OfferText = string.Concat(offer53.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[i]].NameIndex), ", ");
						}
					}
				}
				else
				{
					Offer offer54 = this;
					offer54.OfferText = string.Concat(offer54.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[0]].NameIndex), Localizer.Token(3011));
					Offer offer55 = this;
					offer55.OfferText = string.Concat(offer55.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[1]].NameIndex), ". ");
				}
			}
			if (TheirOffer.ColoniesOffered.Count > 0 && this.ColoniesOffered.Count == 0)
			{
				Offer offer56 = this;
				offer56.OfferText = string.Concat(offer56.OfferText, Localizer.Token(3039));
				if (TheirOffer.ColoniesOffered.Count == 1)
				{
					Offer offer57 = this;
					offer57.OfferText = string.Concat(offer57.OfferText, TheirOffer.ColoniesOffered[0], ". ");
				}
				else if (TheirOffer.ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.ColoniesOffered.Count; i++)
					{
						if (i >= TheirOffer.ColoniesOffered.Count - 1)
						{
							Offer offer58 = this;
							offer58.OfferText = string.Concat(offer58.OfferText, Localizer.Token(3013));
							Offer offer59 = this;
							offer59.OfferText = string.Concat(offer59.OfferText, TheirOffer.ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer60 = this;
							offer60.OfferText = string.Concat(offer60.OfferText, TheirOffer.ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer61 = this;
					offer61.OfferText = string.Concat(offer61.OfferText, TheirOffer.ColoniesOffered[0], Localizer.Token(3011));
					Offer offer62 = this;
					offer62.OfferText = string.Concat(offer62.OfferText, TheirOffer.ColoniesOffered[1], ". ");
				}
			}
			else if (TheirOffer.ColoniesOffered.Count > 0 && this.ColoniesOffered.Count > 0)
			{
				Offer offer63 = this;
				offer63.OfferText = string.Concat(offer63.OfferText, Localizer.Token(3040));
				if (TheirOffer.ColoniesOffered.Count == 1)
				{
					Offer offer64 = this;
					offer64.OfferText = string.Concat(offer64.OfferText, TheirOffer.ColoniesOffered[0], ". ");
				}
				else if (TheirOffer.ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.ColoniesOffered.Count; i++)
					{
						if (i >= TheirOffer.ColoniesOffered.Count - 1)
						{
							Offer offer65 = this;
							offer65.OfferText = string.Concat(offer65.OfferText, Localizer.Token(3013));
							Offer offer66 = this;
							offer66.OfferText = string.Concat(offer66.OfferText, TheirOffer.ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer67 = this;
							offer67.OfferText = string.Concat(offer67.OfferText, TheirOffer.ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer68 = this;
					offer68.OfferText = string.Concat(offer68.OfferText, TheirOffer.ColoniesOffered[0], Localizer.Token(3011));
					Offer offer69 = this;
					offer69.OfferText = string.Concat(offer69.OfferText, TheirOffer.ColoniesOffered[1], ". ");
				}
				Offer offer70 = this;
				offer70.OfferText = string.Concat(offer70.OfferText, Localizer.Token(3041));
				if (this.ColoniesOffered.Count == 1)
				{
					Offer offer71 = this;
					offer71.OfferText = string.Concat(offer71.OfferText, this.ColoniesOffered[0], ". ");
				}
				else if (this.ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < this.ColoniesOffered.Count; i++)
					{
						if (i >= this.ColoniesOffered.Count - 1)
						{
							Offer offer72 = this;
							offer72.OfferText = string.Concat(offer72.OfferText, Localizer.Token(3013));
							Offer offer73 = this;
							offer73.OfferText = string.Concat(offer73.OfferText, this.ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer74 = this;
							offer74.OfferText = string.Concat(offer74.OfferText, this.ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer75 = this;
					offer75.OfferText = string.Concat(offer75.OfferText, this.ColoniesOffered[0], Localizer.Token(3011));
					Offer offer76 = this;
					offer76.OfferText = string.Concat(offer76.OfferText, this.ColoniesOffered[1], ". ");
				}
			}
			else if (this.ColoniesOffered.Count > 0)
			{
				Offer offer77 = this;
				offer77.OfferText = string.Concat(offer77.OfferText, Localizer.Token(3042));
				if (this.ColoniesOffered.Count == 1)
				{
					Offer offer78 = this;
					offer78.OfferText = string.Concat(offer78.OfferText, this.ColoniesOffered[0], ". ");
				}
				else if (this.ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < this.ColoniesOffered.Count; i++)
					{
						if (i >= this.ColoniesOffered.Count - 1)
						{
							Offer offer79 = this;
							offer79.OfferText = string.Concat(offer79.OfferText, Localizer.Token(3013));
							Offer offer80 = this;
							offer80.OfferText = string.Concat(offer80.OfferText, this.ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer81 = this;
							offer81.OfferText = string.Concat(offer81.OfferText, this.ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer82 = this;
					offer82.OfferText = string.Concat(offer82.OfferText, this.ColoniesOffered[0], Localizer.Token(3011));
					Offer offer83 = this;
					offer83.OfferText = string.Concat(offer83.OfferText, this.ColoniesOffered[1], ". ");
				}
			}
			if (TheirOffer.EmpiresToWarOn.Count > 0)
			{
				if (!EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetRelations()[TheirOffer.Them].Treaty_Alliance)
				{
					if (this.GetNumberOfDemands(this) > 0 && this.GetNumberOfDemands(TheirOffer) == 1)
					{
						Offer offer84 = this;
						offer84.OfferText = string.Concat(offer84.OfferText, "In exchange for our leavings, and to avoid your own certain doom, you must declare war upon: ");
					}
					else if (this.GetNumberOfDemands(TheirOffer) + this.GetNumberOfDemands(this) > 2)
					{
						Offer offer85 = this;
						offer85.OfferText = string.Concat(offer85.OfferText, "Finally, we will crush you and your pathetic empire unless you declare war upon: ");
					}
					else if (this.GetNumberOfDemands(TheirOffer) <= 1)
					{
						this.OfferText = "Unless you wish for us to crush your pathetic empire, you will declare war upon: ";
					}
					else
					{
						Offer offer86 = this;
						offer86.OfferText = string.Concat(offer86.OfferText, "Furthermore, we will crush you and your pathetic empire unless you declare war upon: ");
					}
					if (TheirOffer.EmpiresToWarOn.Count == 1)
					{
						Offer offer87 = this;
						offer87.OfferText = string.Concat(offer87.OfferText, TheirOffer.EmpiresToWarOn[0]);
					}
					else if (TheirOffer.EmpiresToWarOn.Count == 2)
					{
						Offer offer88 = this;
						offer88.OfferText = string.Concat(offer88.OfferText, TheirOffer.EmpiresToWarOn[0], " and ", TheirOffer.EmpiresToWarOn[1]);
					}
					else if (TheirOffer.EmpiresToWarOn.Count > 2)
					{
						for (int i = 0; i < TheirOffer.EmpiresToWarOn.Count; i++)
						{
							if (i >= TheirOffer.EmpiresToWarOn.Count - 1)
							{
								Offer offer89 = this;
								offer89.OfferText = string.Concat(offer89.OfferText, " and ", TheirOffer.EmpiresToWarOn[i]);
							}
							else
							{
								Offer offer90 = this;
								offer90.OfferText = string.Concat(offer90.OfferText, TheirOffer.EmpiresToWarOn[i]);
								Offer offer91 = this;
								offer91.OfferText = string.Concat(offer91.OfferText, ", ");
							}
						}
					}
				}
				else
				{
					if (this.GetNumberOfDemands(this) > 1 && this.GetNumberOfDemands(TheirOffer) == 1)
					{
						Offer offer92 = this;
						offer92.OfferText = string.Concat(offer92.OfferText, "We offer you these gifts in the hope that you might join us in war against: ");
					}
					else if (this.GetNumberOfDemands(TheirOffer) + this.GetNumberOfDemands(this) > 2)
					{
						Offer offer93 = this;
						offer93.OfferText = string.Concat(offer93.OfferText, "Finally, we should not have to remind you that we can crush you like a bug. But we can. Therefore, to avoid annihilation, you must declare war on: ");
					}
					else if (this.GetNumberOfDemands(TheirOffer) <= 1)
					{
						Offer offer94 = this;
						offer94.OfferText = string.Concat(offer94.OfferText, "The time to do our bidding has come, ally. You must declare war upon: ");
					}
					else
					{
						Offer offer95 = this;
						offer95.OfferText = string.Concat(offer95.OfferText, "Furthermore, we should not have to remind you that we can crush you like a bug. But we can. Therefore, to avoid annihilation, you must declare war on: ");
					}
					if (TheirOffer.EmpiresToWarOn.Count == 1)
					{
						Offer offer96 = this;
						offer96.OfferText = string.Concat(offer96.OfferText, TheirOffer.EmpiresToWarOn[0]);
					}
					else if (TheirOffer.EmpiresToWarOn.Count == 2)
					{
						Offer offer97 = this;
						offer97.OfferText = string.Concat(offer97.OfferText, TheirOffer.EmpiresToWarOn[0], " and ", TheirOffer.EmpiresToWarOn[1]);
					}
					else if (TheirOffer.EmpiresToWarOn.Count > 2)
					{
						for (int i = 0; i < TheirOffer.EmpiresToWarOn.Count; i++)
						{
							if (i >= TheirOffer.EmpiresToWarOn.Count - 1)
							{
								Offer offer98 = this;
								offer98.OfferText = string.Concat(offer98.OfferText, " and ", TheirOffer.EmpiresToWarOn[i]);
							}
							else
							{
								Offer offer99 = this;
								offer99.OfferText = string.Concat(offer99.OfferText, TheirOffer.EmpiresToWarOn[i]);
								Offer offer100 = this;
								offer100.OfferText = string.Concat(offer100.OfferText, ", ");
							}
						}
					}
				}
			}
			return this.OfferText;
		}

		public string DoRespectfulText(Offer.Attitude a, Offer TheirOffer)
		{
			this.OfferText = "";
			if (this.PeaceTreaty)
			{
				Offer offer = this;
				offer.OfferText = string.Concat(offer.OfferText, Localizer.Token(3000));
				Offer offer1 = this;
				offer1.OfferText = string.Concat(offer1.OfferText, "\n\n");
			}
			if (this.Alliance)
			{
				Offer offer2 = this;
				offer2.OfferText = string.Concat(offer2.OfferText, Localizer.Token(3001));
				Offer offer3 = this;
				offer3.OfferText = string.Concat(offer3.OfferText, "\n\n");
			}
			if (this.OpenBorders)
			{
				if (!TheirOffer.OpenBorders)
				{
					Offer offer4 = this;
					offer4.OfferText = string.Concat(offer4.OfferText, Localizer.Token(3003));
				}
				else
				{
					Offer offer5 = this;
					offer5.OfferText = string.Concat(offer5.OfferText, Localizer.Token(3002));
				}
				if (this.NAPact)
				{
					Offer offer6 = this;
					offer6.OfferText = string.Concat(offer6.OfferText, Localizer.Token(3004));
				}
			}
			else if (TheirOffer.OpenBorders)
			{
				Offer offer7 = this;
				offer7.OfferText = string.Concat(offer7.OfferText, Localizer.Token(3005));
				if (this.NAPact)
				{
					Offer offer8 = this;
					offer8.OfferText = string.Concat(offer8.OfferText, Localizer.Token(3006));
				}
			}
			else if (this.NAPact)
			{
				Offer offer9 = this;
				offer9.OfferText = string.Concat(offer9.OfferText, Localizer.Token(3007));
			}
			if (this.TradeTreaty)
			{
				if (this.NAPact || this.OpenBorders || TheirOffer.OpenBorders)
				{
					Offer offer10 = this;
					offer10.OfferText = string.Concat(offer10.OfferText, "\n");
					Offer offer11 = this;
					offer11.OfferText = string.Concat(offer11.OfferText, Localizer.Token(3008));
				}
				else
				{
					Offer offer12 = this;
					offer12.OfferText = string.Concat(offer12.OfferText, Localizer.Token(3009));
				}
			}
			if (this.TradeTreaty || this.OpenBorders || TheirOffer.OpenBorders || this.NAPact)
			{
				Offer offer13 = this;
				offer13.OfferText = string.Concat(offer13.OfferText, "\n");
			}
			if (this.ArtifactsOffered.Count > 0)
			{
				Offer offer14 = this;
				offer14.OfferText = string.Concat(offer14.OfferText, Localizer.Token(3010));
				if (this.ArtifactsOffered.Count == 1)
				{
					Offer offer15 = this;
					offer15.OfferText = string.Concat(offer15.OfferText, this.ArtifactsOffered[0], ". ");
				}
				else if (this.ArtifactsOffered.Count != 2)
				{
					for (int i = 0; i < this.ArtifactsOffered.Count; i++)
					{
						if (i >= this.ArtifactsOffered.Count - 1)
						{
							Offer offer16 = this;
							offer16.OfferText = string.Concat(offer16.OfferText, "and ");
							Offer offer17 = this;
							offer17.OfferText = string.Concat(offer17.OfferText, this.ArtifactsOffered[i], ". ");
						}
						else
						{
							Offer offer18 = this;
							offer18.OfferText = string.Concat(offer18.OfferText, this.ArtifactsOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer19 = this;
					offer19.OfferText = string.Concat(offer19.OfferText, this.ArtifactsOffered[0], Localizer.Token(3011));
					Offer offer20 = this;
					offer20.OfferText = string.Concat(offer20.OfferText, this.ArtifactsOffered[1], ". ");
				}
			}
			if (TheirOffer.ArtifactsOffered.Count > 0)
			{
				Offer offer21 = this;
				offer21.OfferText = string.Concat(offer21.OfferText, Localizer.Token(3012));
				if (TheirOffer.ArtifactsOffered.Count == 1)
				{
					Offer offer22 = this;
					offer22.OfferText = string.Concat(offer22.OfferText, TheirOffer.ArtifactsOffered[0], ". ");
				}
				else if (TheirOffer.ArtifactsOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.ArtifactsOffered.Count; i++)
					{
						if (i >= TheirOffer.ArtifactsOffered.Count - 1)
						{
							Offer offer23 = this;
							offer23.OfferText = string.Concat(offer23.OfferText, Localizer.Token(3013));
							Offer offer24 = this;
							offer24.OfferText = string.Concat(offer24.OfferText, TheirOffer.ArtifactsOffered[i], ". ");
						}
						else
						{
							Offer offer25 = this;
							offer25.OfferText = string.Concat(offer25.OfferText, TheirOffer.ArtifactsOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer26 = this;
					offer26.OfferText = string.Concat(offer26.OfferText, TheirOffer.ArtifactsOffered[0], Localizer.Token(3011));
					Offer offer27 = this;
					offer27.OfferText = string.Concat(offer27.OfferText, TheirOffer.ArtifactsOffered[1], ". ");
				}
			}
			if (this.TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count == 0)
			{
				Offer offer28 = this;
				offer28.OfferText = string.Concat(offer28.OfferText, Localizer.Token(3014));
				if (this.TechnologiesOffered.Count == 1)
				{
					Offer offer29 = this;
					offer29.OfferText = string.Concat(offer29.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[0]].NameIndex), ". ");
				}
				else if (this.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < this.TechnologiesOffered.Count; i++)
					{
						if (i >= this.TechnologiesOffered.Count - 1)
						{
							Offer offer30 = this;
							offer30.OfferText = string.Concat(offer30.OfferText, Localizer.Token(3013));
							Offer offer31 = this;
							offer31.OfferText = string.Concat(offer31.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[i]].NameIndex), ". ");
						}
						else
						{
							Offer offer32 = this;
							offer32.OfferText = string.Concat(offer32.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[i]].NameIndex), ", ");
						}
					}
				}
				else
				{
					Offer offer33 = this;
					offer33.OfferText = string.Concat(offer33.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[0]].NameIndex), Localizer.Token(3011));
					Offer offer34 = this;
					offer34.OfferText = string.Concat(offer34.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[1]].NameIndex), ". ");
				}
			}
			else if (this.TechnologiesOffered.Count == 0 && TheirOffer.TechnologiesOffered.Count > 0)
			{
				Offer offer35 = this;
				offer35.OfferText = string.Concat(offer35.OfferText, Localizer.Token(3015));
				if (TheirOffer.TechnologiesOffered.Count == 1)
				{
					Offer offer36 = this;
					offer36.OfferText = string.Concat(offer36.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[0]].NameIndex), ". ");
				}
				else if (TheirOffer.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.TechnologiesOffered.Count; i++)
					{
						if (i >= this.TechnologiesOffered.Count - 1)
						{
							Offer offer37 = this;
							offer37.OfferText = string.Concat(offer37.OfferText, Localizer.Token(3013));
							Offer offer38 = this;
							offer38.OfferText = string.Concat(offer38.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[i]].NameIndex), ". ");
						}
						else
						{
							Offer offer39 = this;
							offer39.OfferText = string.Concat(offer39.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[i]].NameIndex), ", ");
						}
					}
				}
				else
				{
					Offer offer40 = this;
					offer40.OfferText = string.Concat(offer40.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[0]].NameIndex), Localizer.Token(3011));
					Offer offer41 = this;
					offer41.OfferText = string.Concat(offer41.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[1]].NameIndex), ". ");
				}
			}
			else if (this.TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count > 0)
			{
				Offer offer42 = this;
				offer42.OfferText = string.Concat(offer42.OfferText, Localizer.Token(3016));
				if (TheirOffer.TechnologiesOffered.Count == 1)
				{
					Offer offer43 = this;
					offer43.OfferText = string.Concat(offer43.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[0]].NameIndex), ". ");
				}
				else if (TheirOffer.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.TechnologiesOffered.Count; i++)
					{
						if (i >= this.TechnologiesOffered.Count - 1)
						{
							Offer offer44 = this;
							offer44.OfferText = string.Concat(offer44.OfferText, Localizer.Token(3013));
							Offer offer45 = this;
							offer45.OfferText = string.Concat(offer45.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[i]].NameIndex), ". ");
						}
						else
						{
							Offer offer46 = this;
							offer46.OfferText = string.Concat(offer46.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[i]].NameIndex), ", ");
						}
					}
				}
				else
				{
					Offer offer47 = this;
					offer47.OfferText = string.Concat(offer47.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[0]].NameIndex), Localizer.Token(3011));
					Offer offer48 = this;
					offer48.OfferText = string.Concat(offer48.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[1]].NameIndex), ". ");
				}
				Offer offer49 = this;
				offer49.OfferText = string.Concat(offer49.OfferText, Localizer.Token(3017));
				if (this.TechnologiesOffered.Count == 1)
				{
					Offer offer50 = this;
					offer50.OfferText = string.Concat(offer50.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[0]].NameIndex), ". ");
				}
				else if (this.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < this.TechnologiesOffered.Count; i++)
					{
						if (i >= this.TechnologiesOffered.Count - 1)
						{
							Offer offer51 = this;
							offer51.OfferText = string.Concat(offer51.OfferText, Localizer.Token(3013));
							Offer offer52 = this;
							offer52.OfferText = string.Concat(offer52.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[i]].NameIndex), ". ");
						}
						else
						{
							Offer offer53 = this;
							offer53.OfferText = string.Concat(offer53.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[i]].NameIndex), ", ");
						}
					}
				}
				else
				{
					Offer offer54 = this;
					offer54.OfferText = string.Concat(offer54.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[0]].NameIndex), Localizer.Token(3011));
					Offer offer55 = this;
					offer55.OfferText = string.Concat(offer55.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[1]].NameIndex), ". ");
				}
			}
			if (TheirOffer.ColoniesOffered.Count > 0 && this.ColoniesOffered.Count == 0)
			{
				Offer offer56 = this;
				offer56.OfferText = string.Concat(offer56.OfferText, Localizer.Token(3018));
				if (TheirOffer.ColoniesOffered.Count == 1)
				{
					Offer offer57 = this;
					offer57.OfferText = string.Concat(offer57.OfferText, TheirOffer.ColoniesOffered[0], ". ");
				}
				else if (TheirOffer.ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.ColoniesOffered.Count; i++)
					{
						if (i >= TheirOffer.ColoniesOffered.Count - 1)
						{
							Offer offer58 = this;
							offer58.OfferText = string.Concat(offer58.OfferText, Localizer.Token(3013));
							Offer offer59 = this;
							offer59.OfferText = string.Concat(offer59.OfferText, TheirOffer.ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer60 = this;
							offer60.OfferText = string.Concat(offer60.OfferText, TheirOffer.ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer61 = this;
					offer61.OfferText = string.Concat(offer61.OfferText, TheirOffer.ColoniesOffered[0], Localizer.Token(3011));
					Offer offer62 = this;
					offer62.OfferText = string.Concat(offer62.OfferText, TheirOffer.ColoniesOffered[1], ". ");
				}
			}
			else if (TheirOffer.ColoniesOffered.Count > 0 && this.ColoniesOffered.Count > 0)
			{
				Offer offer63 = this;
				offer63.OfferText = string.Concat(offer63.OfferText, Localizer.Token(3019));
				if (TheirOffer.ColoniesOffered.Count == 1)
				{
					Offer offer64 = this;
					offer64.OfferText = string.Concat(offer64.OfferText, TheirOffer.ColoniesOffered[0], ". ");
				}
				else if (TheirOffer.ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.ColoniesOffered.Count; i++)
					{
						if (i >= TheirOffer.ColoniesOffered.Count - 1)
						{
							Offer offer65 = this;
							offer65.OfferText = string.Concat(offer65.OfferText, Localizer.Token(3013));
							Offer offer66 = this;
							offer66.OfferText = string.Concat(offer66.OfferText, TheirOffer.ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer67 = this;
							offer67.OfferText = string.Concat(offer67.OfferText, TheirOffer.ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer68 = this;
					offer68.OfferText = string.Concat(offer68.OfferText, TheirOffer.ColoniesOffered[0], Localizer.Token(3011));
					Offer offer69 = this;
					offer69.OfferText = string.Concat(offer69.OfferText, TheirOffer.ColoniesOffered[1], ". ");
				}
				Offer offer70 = this;
				offer70.OfferText = string.Concat(offer70.OfferText, Localizer.Token(3020));
				if (this.ColoniesOffered.Count == 1)
				{
					Offer offer71 = this;
					offer71.OfferText = string.Concat(offer71.OfferText, this.ColoniesOffered[0], ". ");
				}
				else if (this.ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < this.ColoniesOffered.Count; i++)
					{
						if (i >= this.ColoniesOffered.Count - 1)
						{
							Offer offer72 = this;
							offer72.OfferText = string.Concat(offer72.OfferText, Localizer.Token(3013));
							Offer offer73 = this;
							offer73.OfferText = string.Concat(offer73.OfferText, this.ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer74 = this;
							offer74.OfferText = string.Concat(offer74.OfferText, this.ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer75 = this;
					offer75.OfferText = string.Concat(offer75.OfferText, this.ColoniesOffered[0], Localizer.Token(3011));
					Offer offer76 = this;
					offer76.OfferText = string.Concat(offer76.OfferText, this.ColoniesOffered[1], ". ");
				}
			}
			else if (this.ColoniesOffered.Count > 0)
			{
				Offer offer77 = this;
				offer77.OfferText = string.Concat(offer77.OfferText, Localizer.Token(3021));
				if (this.ColoniesOffered.Count == 1)
				{
					Offer offer78 = this;
					offer78.OfferText = string.Concat(offer78.OfferText, this.ColoniesOffered[0], ". ");
				}
				else if (this.ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < this.ColoniesOffered.Count; i++)
					{
						if (i >= this.ColoniesOffered.Count - 1)
						{
							Offer offer79 = this;
							offer79.OfferText = string.Concat(offer79.OfferText, Localizer.Token(3013));
							Offer offer80 = this;
							offer80.OfferText = string.Concat(offer80.OfferText, this.ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer81 = this;
							offer81.OfferText = string.Concat(offer81.OfferText, this.ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer82 = this;
					offer82.OfferText = string.Concat(offer82.OfferText, this.ColoniesOffered[0], Localizer.Token(3011));
					Offer offer83 = this;
					offer83.OfferText = string.Concat(offer83.OfferText, this.ColoniesOffered[1], ". ");
				}
			}
			if (TheirOffer.EmpiresToWarOn.Count > 0)
			{
				if (!EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetRelations()[TheirOffer.Them].Treaty_Alliance)
				{
					if (this.GetNumberOfDemands(this) > 0 && this.GetNumberOfDemands(TheirOffer) == 1)
					{
						Offer offer84 = this;
						offer84.OfferText = string.Concat(offer84.OfferText, "In exchange for this, we want you to declare war upon: ");
					}
					else if (this.GetNumberOfDemands(TheirOffer) + this.GetNumberOfDemands(this) > 2)
					{
						Offer offer85 = this;
						offer85.OfferText = string.Concat(offer85.OfferText, "Finally, we are requesting that you declare war upon: ");
					}
					else if (this.GetNumberOfDemands(TheirOffer) <= 1)
					{
						this.OfferText = "We believe it would be prudent of you to declare war upon: ";
					}
					else
					{
						Offer offer86 = this;
						offer86.OfferText = string.Concat(offer86.OfferText, "Furthermore, we are requesting that you declare war upon: ");
					}
					if (TheirOffer.EmpiresToWarOn.Count == 1)
					{
						Offer offer87 = this;
						offer87.OfferText = string.Concat(offer87.OfferText, TheirOffer.EmpiresToWarOn[0]);
					}
					else if (TheirOffer.EmpiresToWarOn.Count == 2)
					{
						Offer offer88 = this;
						offer88.OfferText = string.Concat(offer88.OfferText, TheirOffer.EmpiresToWarOn[0], " and ", TheirOffer.EmpiresToWarOn[1]);
					}
					else if (TheirOffer.EmpiresToWarOn.Count > 2)
					{
						for (int i = 0; i < TheirOffer.EmpiresToWarOn.Count; i++)
						{
							if (i >= TheirOffer.EmpiresToWarOn.Count - 1)
							{
								Offer offer89 = this;
								offer89.OfferText = string.Concat(offer89.OfferText, " and ", TheirOffer.EmpiresToWarOn[i]);
							}
							else
							{
								Offer offer90 = this;
								offer90.OfferText = string.Concat(offer90.OfferText, TheirOffer.EmpiresToWarOn[i]);
								Offer offer91 = this;
								offer91.OfferText = string.Concat(offer91.OfferText, ", ");
							}
						}
					}
				}
				else
				{
					if (this.GetNumberOfDemands(this) > 1 && this.GetNumberOfDemands(TheirOffer) == 1)
					{
						Offer offer92 = this;
						offer92.OfferText = string.Concat(offer92.OfferText, "We give you this gift, friend, and now call upon our alliance in requesting that you declare war upon: ");
					}
					else if (this.GetNumberOfDemands(TheirOffer) + this.GetNumberOfDemands(this) > 2)
					{
						Offer offer93 = this;
						offer93.OfferText = string.Concat(offer93.OfferText, "Finally, we call upon our alliance and request that you declare war upon: ");
					}
					else if (this.GetNumberOfDemands(TheirOffer) <= 1)
					{
						Offer offer94 = this;
						offer94.OfferText = string.Concat(offer94.OfferText, "Friend, it is time for us to call upon our allies to join us in war. You must declare war upon: ");
					}
					else
					{
						Offer offer95 = this;
						offer95.OfferText = string.Concat(offer95.OfferText, "Furthermore, we call upon our alliance and request that you declare war upon: ");
					}
					if (TheirOffer.EmpiresToWarOn.Count == 1)
					{
						Offer offer96 = this;
						offer96.OfferText = string.Concat(offer96.OfferText, TheirOffer.EmpiresToWarOn[0]);
					}
					else if (TheirOffer.EmpiresToWarOn.Count == 2)
					{
						Offer offer97 = this;
						offer97.OfferText = string.Concat(offer97.OfferText, TheirOffer.EmpiresToWarOn[0], " and ", TheirOffer.EmpiresToWarOn[1]);
					}
					else if (TheirOffer.EmpiresToWarOn.Count > 2)
					{
						for (int i = 0; i < TheirOffer.EmpiresToWarOn.Count; i++)
						{
							if (i >= TheirOffer.EmpiresToWarOn.Count - 1)
							{
								Offer offer98 = this;
								offer98.OfferText = string.Concat(offer98.OfferText, " and ", TheirOffer.EmpiresToWarOn[i]);
							}
							else
							{
								Offer offer99 = this;
								offer99.OfferText = string.Concat(offer99.OfferText, TheirOffer.EmpiresToWarOn[i]);
								Offer offer100 = this;
								offer100.OfferText = string.Concat(offer100.OfferText, ", ");
							}
						}
					}
				}
			}
			return this.OfferText;
		}

		public string DoThreateningText(Offer.Attitude a, Offer TheirOffer)
		{
			this.OfferText = "";
			if (this.PeaceTreaty)
			{
				Offer offer = this;
				offer.OfferText = string.Concat(offer.OfferText, Localizer.Token(3043));
				Offer offer1 = this;
				offer1.OfferText = string.Concat(offer1.OfferText, "\n\n");
			}
			if (this.Alliance)
			{
				Offer offer2 = this;
				offer2.OfferText = string.Concat(offer2.OfferText, Localizer.Token(3044));
				Offer offer3 = this;
				offer3.OfferText = string.Concat(offer3.OfferText, "\n\n");
			}
			if (this.OpenBorders)
			{
				if (!TheirOffer.OpenBorders)
				{
					Offer offer4 = this;
					offer4.OfferText = string.Concat(offer4.OfferText, Localizer.Token(3046));
				}
				else
				{
					Offer offer5 = this;
					offer5.OfferText = string.Concat(offer5.OfferText, Localizer.Token(3045));
				}
				if (this.NAPact)
				{
					Offer offer6 = this;
					offer6.OfferText = string.Concat(offer6.OfferText, Localizer.Token(3047));
				}
			}
			else if (TheirOffer.OpenBorders)
			{
				Offer offer7 = this;
				offer7.OfferText = string.Concat(offer7.OfferText, Localizer.Token(3048));
				if (this.NAPact)
				{
					Offer offer8 = this;
					offer8.OfferText = string.Concat(offer8.OfferText, Localizer.Token(3049));
				}
			}
			else if (this.NAPact)
			{
				Offer offer9 = this;
				offer9.OfferText = string.Concat(offer9.OfferText, Localizer.Token(3050));
			}
			if (this.TradeTreaty)
			{
				if (this.NAPact || this.OpenBorders || TheirOffer.OpenBorders)
				{
					Offer offer10 = this;
					offer10.OfferText = string.Concat(offer10.OfferText, "\n\n");
					Offer offer11 = this;
					offer11.OfferText = string.Concat(offer11.OfferText, Localizer.Token(3051));
				}
				else
				{
					Offer offer12 = this;
					offer12.OfferText = string.Concat(offer12.OfferText, Localizer.Token(3052));
				}
			}
			if (this.TradeTreaty || this.OpenBorders || TheirOffer.OpenBorders || this.NAPact)
			{
				Offer offer13 = this;
				offer13.OfferText = string.Concat(offer13.OfferText, "\n\n");
			}
			if (this.ArtifactsOffered.Count > 0)
			{
				Offer offer14 = this;
				offer14.OfferText = string.Concat(offer14.OfferText, Localizer.Token(3053));
				if (this.ArtifactsOffered.Count == 1)
				{
					Offer offer15 = this;
					offer15.OfferText = string.Concat(offer15.OfferText, this.ArtifactsOffered[0], ". ");
				}
				else if (this.ArtifactsOffered.Count != 2)
				{
					for (int i = 0; i < this.ArtifactsOffered.Count; i++)
					{
						if (i >= this.ArtifactsOffered.Count - 1)
						{
							Offer offer16 = this;
							offer16.OfferText = string.Concat(offer16.OfferText, Localizer.Token(3013));
							Offer offer17 = this;
							offer17.OfferText = string.Concat(offer17.OfferText, this.ArtifactsOffered[i], ". ");
						}
						else
						{
							Offer offer18 = this;
							offer18.OfferText = string.Concat(offer18.OfferText, this.ArtifactsOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer19 = this;
					offer19.OfferText = string.Concat(offer19.OfferText, this.ArtifactsOffered[0], Localizer.Token(3011));
					Offer offer20 = this;
					offer20.OfferText = string.Concat(offer20.OfferText, this.ArtifactsOffered[1], ". ");
				}
			}
			if (TheirOffer.ArtifactsOffered.Count > 0)
			{
				Offer offer21 = this;
				offer21.OfferText = string.Concat(offer21.OfferText, Localizer.Token(3054));
				if (TheirOffer.ArtifactsOffered.Count == 1)
				{
					Offer offer22 = this;
					offer22.OfferText = string.Concat(offer22.OfferText, TheirOffer.ArtifactsOffered[0], ". ");
				}
				else if (TheirOffer.ArtifactsOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.ArtifactsOffered.Count; i++)
					{
						if (i >= TheirOffer.ArtifactsOffered.Count - 1)
						{
							Offer offer23 = this;
							offer23.OfferText = string.Concat(offer23.OfferText, Localizer.Token(3013));
							Offer offer24 = this;
							offer24.OfferText = string.Concat(offer24.OfferText, TheirOffer.ArtifactsOffered[i], ". ");
						}
						else
						{
							Offer offer25 = this;
							offer25.OfferText = string.Concat(offer25.OfferText, TheirOffer.ArtifactsOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer26 = this;
					offer26.OfferText = string.Concat(offer26.OfferText, TheirOffer.ArtifactsOffered[0], Localizer.Token(3011));
					Offer offer27 = this;
					offer27.OfferText = string.Concat(offer27.OfferText, TheirOffer.ArtifactsOffered[1], ". ");
				}
			}
			if (this.TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count == 0)
			{
				Offer offer28 = this;
				offer28.OfferText = string.Concat(offer28.OfferText, Localizer.Token(3055));
				if (this.TechnologiesOffered.Count == 1)
				{
					Offer offer29 = this;
					offer29.OfferText = string.Concat(offer29.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[0]].NameIndex), ". ");
				}
				else if (this.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < this.TechnologiesOffered.Count; i++)
					{
						if (i >= this.TechnologiesOffered.Count - 1)
						{
							Offer offer30 = this;
							offer30.OfferText = string.Concat(offer30.OfferText, Localizer.Token(3013));
							Offer offer31 = this;
							offer31.OfferText = string.Concat(offer31.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[i]].NameIndex), ". ");
						}
						else
						{
							Offer offer32 = this;
							offer32.OfferText = string.Concat(offer32.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[i]].NameIndex), ", ");
						}
					}
				}
				else
				{
					Offer offer33 = this;
					offer33.OfferText = string.Concat(offer33.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[0]].NameIndex), Localizer.Token(3011));
					Offer offer34 = this;
					offer34.OfferText = string.Concat(offer34.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[1]].NameIndex), ". ");
				}
				Offer offer35 = this;
				offer35.OfferText = string.Concat(offer35.OfferText, Localizer.Token(3056));
			}
			else if (this.TechnologiesOffered.Count == 0 && TheirOffer.TechnologiesOffered.Count > 0)
			{
				Offer offer36 = this;
				offer36.OfferText = string.Concat(offer36.OfferText, Localizer.Token(3057));
				if (TheirOffer.TechnologiesOffered.Count == 1)
				{
					Offer offer37 = this;
					offer37.OfferText = string.Concat(offer37.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[0]].NameIndex), ". ");
				}
				else if (TheirOffer.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.TechnologiesOffered.Count; i++)
					{
						if (i >= this.TechnologiesOffered.Count - 1)
						{
							Offer offer38 = this;
							offer38.OfferText = string.Concat(offer38.OfferText, Localizer.Token(3013));
							Offer offer39 = this;
							offer39.OfferText = string.Concat(offer39.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[i]].NameIndex), ". ");
						}
						else
						{
							Offer offer40 = this;
							offer40.OfferText = string.Concat(offer40.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[i]].NameIndex), ", ");
						}
					}
				}
				else
				{
					Offer offer41 = this;
					offer41.OfferText = string.Concat(offer41.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[0]].NameIndex), Localizer.Token(3011));
					Offer offer42 = this;
					offer42.OfferText = string.Concat(offer42.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[1]].NameIndex), ". ");
				}
			}
			else if (this.TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count > 0)
			{
				Offer offer43 = this;
				offer43.OfferText = string.Concat(offer43.OfferText, Localizer.Token(3058));
				if (TheirOffer.TechnologiesOffered.Count == 1)
				{
					Offer offer44 = this;
					offer44.OfferText = string.Concat(offer44.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[0]].NameIndex), ". ");
				}
				else if (TheirOffer.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.TechnologiesOffered.Count; i++)
					{
						if (i >= this.TechnologiesOffered.Count - 1)
						{
							Offer offer45 = this;
							offer45.OfferText = string.Concat(offer45.OfferText, Localizer.Token(3013));
							Offer offer46 = this;
							offer46.OfferText = string.Concat(offer46.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[i]].NameIndex), ". ");
						}
						else
						{
							Offer offer47 = this;
							offer47.OfferText = string.Concat(offer47.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[i]].NameIndex), ", ");
						}
					}
				}
				else
				{
					Offer offer48 = this;
					offer48.OfferText = string.Concat(offer48.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[0]].NameIndex), Localizer.Token(3011));
					Offer offer49 = this;
					offer49.OfferText = string.Concat(offer49.OfferText, Localizer.Token(ResourceManager.TechTree[TheirOffer.TechnologiesOffered[1]].NameIndex), ". ");
				}
				Offer offer50 = this;
				offer50.OfferText = string.Concat(offer50.OfferText, Localizer.Token(3059));
				if (this.TechnologiesOffered.Count == 1)
				{
					Offer offer51 = this;
					offer51.OfferText = string.Concat(offer51.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[0]].NameIndex), ". ");
				}
				else if (this.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < this.TechnologiesOffered.Count; i++)
					{
						if (i >= this.TechnologiesOffered.Count - 1)
						{
							Offer offer52 = this;
							offer52.OfferText = string.Concat(offer52.OfferText, Localizer.Token(3013));
							Offer offer53 = this;
							offer53.OfferText = string.Concat(offer53.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[i]].NameIndex), ". ");
						}
						else
						{
							Offer offer54 = this;
							offer54.OfferText = string.Concat(offer54.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[i]].NameIndex), ", ");
						}
					}
				}
				else
				{
					Offer offer55 = this;
					offer55.OfferText = string.Concat(offer55.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[0]].NameIndex), Localizer.Token(3011));
					Offer offer56 = this;
					offer56.OfferText = string.Concat(offer56.OfferText, Localizer.Token(ResourceManager.TechTree[this.TechnologiesOffered[1]].NameIndex), ". ");
				}
			}
			if (TheirOffer.ColoniesOffered.Count > 0 && this.ColoniesOffered.Count == 0)
			{
				Offer offer57 = this;
				offer57.OfferText = string.Concat(offer57.OfferText, Localizer.Token(3060));
				if (TheirOffer.ColoniesOffered.Count == 1)
				{
					Offer offer58 = this;
					offer58.OfferText = string.Concat(offer58.OfferText, TheirOffer.ColoniesOffered[0], ". ");
				}
				else if (TheirOffer.ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.ColoniesOffered.Count; i++)
					{
						if (i >= TheirOffer.ColoniesOffered.Count - 1)
						{
							Offer offer59 = this;
							offer59.OfferText = string.Concat(offer59.OfferText, Localizer.Token(3013));
							Offer offer60 = this;
							offer60.OfferText = string.Concat(offer60.OfferText, TheirOffer.ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer61 = this;
							offer61.OfferText = string.Concat(offer61.OfferText, TheirOffer.ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer62 = this;
					offer62.OfferText = string.Concat(offer62.OfferText, TheirOffer.ColoniesOffered[0], Localizer.Token(3011));
					Offer offer63 = this;
					offer63.OfferText = string.Concat(offer63.OfferText, TheirOffer.ColoniesOffered[1], ". ");
				}
			}
			else if (TheirOffer.ColoniesOffered.Count > 0 && this.ColoniesOffered.Count > 0)
			{
				Offer offer64 = this;
				offer64.OfferText = string.Concat(offer64.OfferText, Localizer.Token(3061));
				if (TheirOffer.ColoniesOffered.Count == 1)
				{
					Offer offer65 = this;
					offer65.OfferText = string.Concat(offer65.OfferText, TheirOffer.ColoniesOffered[0], ". ");
				}
				else if (TheirOffer.ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.ColoniesOffered.Count; i++)
					{
						if (i >= TheirOffer.ColoniesOffered.Count - 1)
						{
							Offer offer66 = this;
							offer66.OfferText = string.Concat(offer66.OfferText, Localizer.Token(3013));
							Offer offer67 = this;
							offer67.OfferText = string.Concat(offer67.OfferText, TheirOffer.ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer68 = this;
							offer68.OfferText = string.Concat(offer68.OfferText, TheirOffer.ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer69 = this;
					offer69.OfferText = string.Concat(offer69.OfferText, TheirOffer.ColoniesOffered[0], Localizer.Token(3011));
					Offer offer70 = this;
					offer70.OfferText = string.Concat(offer70.OfferText, TheirOffer.ColoniesOffered[1], ". ");
				}
				Offer offer71 = this;
				offer71.OfferText = string.Concat(offer71.OfferText, Localizer.Token(3062));
				if (this.ColoniesOffered.Count == 1)
				{
					Offer offer72 = this;
					offer72.OfferText = string.Concat(offer72.OfferText, this.ColoniesOffered[0], ". ");
				}
				else if (this.ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < this.ColoniesOffered.Count; i++)
					{
						if (i >= this.ColoniesOffered.Count - 1)
						{
							Offer offer73 = this;
							offer73.OfferText = string.Concat(offer73.OfferText, Localizer.Token(3013));
							Offer offer74 = this;
							offer74.OfferText = string.Concat(offer74.OfferText, this.ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer75 = this;
							offer75.OfferText = string.Concat(offer75.OfferText, this.ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer76 = this;
					offer76.OfferText = string.Concat(offer76.OfferText, this.ColoniesOffered[0], Localizer.Token(3011));
					Offer offer77 = this;
					offer77.OfferText = string.Concat(offer77.OfferText, this.ColoniesOffered[1], ". ");
				}
			}
			else if (this.ColoniesOffered.Count > 0)
			{
				Offer offer78 = this;
				offer78.OfferText = string.Concat(offer78.OfferText, Localizer.Token(3063));
				if (this.ColoniesOffered.Count == 1)
				{
					Offer offer79 = this;
					offer79.OfferText = string.Concat(offer79.OfferText, this.ColoniesOffered[0], ". ");
				}
				else if (this.ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < this.ColoniesOffered.Count; i++)
					{
						if (i >= this.ColoniesOffered.Count - 1)
						{
							Offer offer80 = this;
							offer80.OfferText = string.Concat(offer80.OfferText, Localizer.Token(3013));
							Offer offer81 = this;
							offer81.OfferText = string.Concat(offer81.OfferText, this.ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer82 = this;
							offer82.OfferText = string.Concat(offer82.OfferText, this.ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer83 = this;
					offer83.OfferText = string.Concat(offer83.OfferText, this.ColoniesOffered[0], Localizer.Token(3011));
					Offer offer84 = this;
					offer84.OfferText = string.Concat(offer84.OfferText, this.ColoniesOffered[1], ". ");
				}
			}
			if (TheirOffer.EmpiresToWarOn.Count > 0)
			{
				if (!EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty).GetRelations()[TheirOffer.Them].Treaty_Alliance)
				{
					if (this.GetNumberOfDemands(this) > 0 && this.GetNumberOfDemands(TheirOffer) == 1)
					{
						Offer offer85 = this;
						offer85.OfferText = string.Concat(offer85.OfferText, "In exchange for our leavings, and to avoid your own certain doom, you must declare war upon: ");
					}
					else if (this.GetNumberOfDemands(TheirOffer) + this.GetNumberOfDemands(this) > 2)
					{
						Offer offer86 = this;
						offer86.OfferText = string.Concat(offer86.OfferText, "Finally, we will crush you and your pathetic empire unless you declare war upon: ");
					}
					else if (this.GetNumberOfDemands(TheirOffer) <= 1)
					{
						this.OfferText = "Unless you wish for us to crush your pathetic empire, you will declare war upon: ";
					}
					else
					{
						Offer offer87 = this;
						offer87.OfferText = string.Concat(offer87.OfferText, "Furthermore, we will crush you and your pathetic empire unless you declare war upon: ");
					}
					if (TheirOffer.EmpiresToWarOn.Count == 1)
					{
						Offer offer88 = this;
						offer88.OfferText = string.Concat(offer88.OfferText, TheirOffer.EmpiresToWarOn[0]);
					}
					else if (TheirOffer.EmpiresToWarOn.Count == 2)
					{
						Offer offer89 = this;
						offer89.OfferText = string.Concat(offer89.OfferText, TheirOffer.EmpiresToWarOn[0], " and ", TheirOffer.EmpiresToWarOn[1]);
					}
					else if (TheirOffer.EmpiresToWarOn.Count > 2)
					{
						for (int i = 0; i < TheirOffer.EmpiresToWarOn.Count; i++)
						{
							if (i >= TheirOffer.EmpiresToWarOn.Count - 1)
							{
								Offer offer90 = this;
								offer90.OfferText = string.Concat(offer90.OfferText, " and ", TheirOffer.EmpiresToWarOn[i]);
							}
							else
							{
								Offer offer91 = this;
								offer91.OfferText = string.Concat(offer91.OfferText, TheirOffer.EmpiresToWarOn[i]);
								Offer offer92 = this;
								offer92.OfferText = string.Concat(offer92.OfferText, ", ");
							}
						}
					}
				}
				else
				{
					if (this.GetNumberOfDemands(this) > 1 && this.GetNumberOfDemands(TheirOffer) == 1)
					{
						Offer offer93 = this;
						offer93.OfferText = string.Concat(offer93.OfferText, "Now, take these leavings and declare war on our enemies lest you become one! You must war upon: ");
					}
					else if (this.GetNumberOfDemands(TheirOffer) + this.GetNumberOfDemands(this) > 2)
					{
						Offer offer94 = this;
						offer94.OfferText = string.Concat(offer94.OfferText, "Finally, we should not have to remind you that we can crush you like a bug. But we can. Therefore, to avoid annihilation, you must declare war on: ");
					}
					else if (this.GetNumberOfDemands(TheirOffer) <= 1)
					{
						Offer offer95 = this;
						offer95.OfferText = string.Concat(offer95.OfferText, "The time to do our bidding has come, ally. You must declare war upon: ");
					}
					else
					{
						Offer offer96 = this;
						offer96.OfferText = string.Concat(offer96.OfferText, "Furthermore, we should not have to remind you that we can crush you like a bug. But we can. Therefore, to avoid annihilation, you must declare war on: ");
					}
					if (TheirOffer.EmpiresToWarOn.Count == 1)
					{
						Offer offer97 = this;
						offer97.OfferText = string.Concat(offer97.OfferText, TheirOffer.EmpiresToWarOn[0]);
					}
					else if (TheirOffer.EmpiresToWarOn.Count == 2)
					{
						Offer offer98 = this;
						offer98.OfferText = string.Concat(offer98.OfferText, TheirOffer.EmpiresToWarOn[0], " and ", TheirOffer.EmpiresToWarOn[1]);
					}
					else if (TheirOffer.EmpiresToWarOn.Count > 2)
					{
						for (int i = 0; i < TheirOffer.EmpiresToWarOn.Count; i++)
						{
							if (i >= TheirOffer.EmpiresToWarOn.Count - 1)
							{
								Offer offer99 = this;
								offer99.OfferText = string.Concat(offer99.OfferText, " and ", TheirOffer.EmpiresToWarOn[i]);
							}
							else
							{
								Offer offer100 = this;
								offer100.OfferText = string.Concat(offer100.OfferText, TheirOffer.EmpiresToWarOn[i]);
								Offer offer101 = this;
								offer101.OfferText = string.Concat(offer101.OfferText, ", ");
							}
						}
					}
				}
			}
			return this.OfferText;
		}

		public string FormulateOfferText(Offer.Attitude a, Offer TheirOffer)
		{
			switch (a)
			{
				case Offer.Attitude.Pleading:
				{
					return this.DoPleadingText(a, TheirOffer);
				}
				case Offer.Attitude.Respectful:
				{
					return this.DoRespectfulText(a, TheirOffer);
				}
				case Offer.Attitude.Threaten:
				{
					return this.DoThreateningText(a, TheirOffer);
				}
			}
			return this.OfferText;
		}

		public int GetNumberOfDemands(Offer which)
		{
			int num = 0;
			if (which.NAPact)
			{
				num++;
			}
			if (which.PeaceTreaty)
			{
				num++;
			}
			if (which.OpenBorders)
			{
				num++;
			}
			if (which.TradeTreaty)
			{
				num++;
			}
			if (which.TechnologiesOffered.Count > 0)
			{
				num++;
			}
			if (which.ColoniesOffered.Count > 0)
			{
				num++;
			}
			if (which.ArtifactsOffered.Count > 0)
			{
				num++;
			}
			if (which.EmpiresToMakePeaceWith.Count > 0)
			{
				num++;
			}
			if (which.EmpiresToWarOn.Count > 0)
			{
				num++;
			}
			return num;
		}

		public bool IsBlank()
		{
			if (!this.NAPact && !this.PeaceTreaty && !this.OpenBorders && !this.TradeTreaty && this.TechnologiesOffered.Count <= 0 && this.ColoniesOffered.Count <= 0 && this.ArtifactsOffered.Count <= 0 && this.EmpiresToMakePeaceWith.Count <= 0 && this.EmpiresToWarOn.Count <= 0)
			{
				return true;
			}
			return false;
		}

		public enum Attitude
		{
			Pleading,
			Respectful,
			Threaten
		}
	}
}