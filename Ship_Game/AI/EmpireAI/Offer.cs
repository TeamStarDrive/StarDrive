namespace Ship_Game
{
	public sealed class Offer
	{
		public Array<string> TechnologiesOffered = new Array<string>();

		public Array<string> ArtifactsOffered = new Array<string>();

		public Ref<bool> ValueToModify;

		public bool PeaceTreaty;

		public bool Alliance;

		public string AcceptDL;

		public string RejectDL;

		public Array<string> ColoniesOffered = new Array<string>();

		public bool NAPact;

		public Array<string> EmpiresToWarOn = new Array<string>();

		public Array<string> EmpiresToMakePeaceWith = new Array<string>();

		public bool OpenBorders;

		public bool TradeTreaty;

		private string OfferText = "";

		public Empire Them;

        string TechOffer(int i) => Localizer.Token(ResourceManager.TechTree[TechnologiesOffered[i]].NameIndex);

	    public string DoPleadingText(Attitude a, Offer TheirOffer)
		{
			OfferText = "";
			if (PeaceTreaty)
			{
                OfferText += Localizer.Token(3022);
                OfferText += "\n\n";
            }
			if (Alliance)
			{
                OfferText += Localizer.Token(3023);
                OfferText += "\n\n";
            }
			if (OpenBorders)
			{
				if (!TheirOffer.OpenBorders)
				{
                    OfferText += Localizer.Token(3025);
                }
				else
				{
                    OfferText += Localizer.Token(3024);
                }
				if (NAPact)
				{
                    OfferText += Localizer.Token(3026);
                }
			}
			else if (TheirOffer.OpenBorders)
			{
                OfferText += Localizer.Token(3027);
                if (NAPact)
				{
                    OfferText += Localizer.Token(3028);
                }
			}
			else if (NAPact)
			{
                OfferText += Localizer.Token(3029);
            }
			if (ArtifactsOffered.Count > 0)
			{
                OfferText += Localizer.Token(3030);
                if (ArtifactsOffered.Count == 1)
				{
					Offer offer11 = this;
					offer11.OfferText = string.Concat(offer11.OfferText, ArtifactsOffered[0], ". ");
				}
				else if (ArtifactsOffered.Count != 2)
				{
					for (int i = 0; i < ArtifactsOffered.Count; i++)
					{
						if (i >= ArtifactsOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            Offer offer13 = this;
							offer13.OfferText = string.Concat(offer13.OfferText, ArtifactsOffered[i], ". ");
						}
						else
						{
							Offer offer14 = this;
							offer14.OfferText = string.Concat(offer14.OfferText, ArtifactsOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer15 = this;
					offer15.OfferText = string.Concat(offer15.OfferText, ArtifactsOffered[0], Localizer.Token(3011));
					Offer offer16 = this;
					offer16.OfferText = string.Concat(offer16.OfferText, ArtifactsOffered[1], ". ");
				}
			}
			if (TheirOffer.ArtifactsOffered.Count > 0)
			{
                OfferText += Localizer.Token(3031);
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
                            OfferText += Localizer.Token(3013);
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
			if (TradeTreaty)
			{
				if (NAPact || OpenBorders || TheirOffer.OpenBorders)
				{
                    OfferText += "\n\n";
                    OfferText += Localizer.Token(3033);
                }
				else
				{
                    OfferText += Localizer.Token(3034);
                }
			}
			if (TradeTreaty || OpenBorders || TheirOffer.OpenBorders || NAPact)
			{
                OfferText += "\n\n";
            }
			if (TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count == 0)
			{
                OfferText += Localizer.Token(3035);
                if (TechnologiesOffered.Count == 1)
				{
                    OfferText += TechOffer(0) + ". ";
                }
				else if (TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TechnologiesOffered.Count; i++)
					{
						if (i >= TechnologiesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            OfferText += TechOffer(i) + ". ";
                        }
						else
						{
							Offer offer32 = this;
							offer32.OfferText = string.Concat(offer32.OfferText, TechOffer(i), ", ");
						}
					}
				}
				else
				{
					Offer offer33 = this;
					offer33.OfferText = string.Concat(offer33.OfferText, TechOffer(0), Localizer.Token(3011));
                    OfferText += TechOffer(1) + ". ";
                }
			}
			else if (TechnologiesOffered.Count == 0 && TheirOffer.TechnologiesOffered.Count > 0)
			{
                OfferText += Localizer.Token(3036);
                if (TheirOffer.TechnologiesOffered.Count == 1)
				{
					Offer offer36 = this;
					offer36.OfferText = string.Concat(offer36.OfferText, TechOffer(0), "? ");
				}
				else if (TheirOffer.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.TechnologiesOffered.Count; i++)
					{
						if (i >= TechnologiesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            Offer offer38 = this;
							offer38.OfferText = string.Concat(offer38.OfferText, TechOffer(i), "? ");
						}
						else
						{
							Offer offer39 = this;
							offer39.OfferText = string.Concat(offer39.OfferText, TechOffer(i), ", ");
						}
					}
				}
				else
				{
					Offer offer40 = this;
					offer40.OfferText = string.Concat(offer40.OfferText, TechOffer(0), Localizer.Token(3011));
					Offer offer41 = this;
					offer41.OfferText = string.Concat(offer41.OfferText, TechOffer(1), "? ");
				}
			}
			else if (TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count > 0)
			{
                OfferText += Localizer.Token(3037);
                if (TheirOffer.TechnologiesOffered.Count == 1)
				{
                    OfferText += TechOffer(0) + ". ";
                }
				else if (TheirOffer.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.TechnologiesOffered.Count; i++)
					{
						if (i >= TechnologiesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            OfferText += TechOffer(i) + ". ";
                        }
						else
						{
							Offer offer46 = this;
							offer46.OfferText = string.Concat(offer46.OfferText, TechOffer(i), ", ");
						}
					}
				}
				else
				{
					Offer offer47 = this;
					offer47.OfferText = string.Concat(offer47.OfferText, TechOffer(0), Localizer.Token(3011));
                    OfferText += TechOffer(1) + ". ";
                }

                OfferText += Localizer.Token(3038);
                if (TechnologiesOffered.Count == 1)
				{
                    OfferText += TechOffer(0) + ". ";
                }
				else if (TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TechnologiesOffered.Count; i++)
					{
						if (i >= TechnologiesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            OfferText += TechOffer(i) + ". ";
                        }
						else
						{
							Offer offer53 = this;
							offer53.OfferText = string.Concat(offer53.OfferText, TechOffer(i), ", ");
						}
					}
				}
				else
				{
					Offer offer54 = this;
					offer54.OfferText = string.Concat(offer54.OfferText, TechOffer(0), Localizer.Token(3011));
                    OfferText += TechOffer(1) + ". ";
                }
			}
			if (TheirOffer.ColoniesOffered.Count > 0 && ColoniesOffered.Count == 0)
			{
                OfferText += Localizer.Token(3039);
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
                            OfferText += Localizer.Token(3013);
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
			else if (TheirOffer.ColoniesOffered.Count > 0 && ColoniesOffered.Count > 0)
			{
                OfferText += Localizer.Token(3040);
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
                            OfferText += Localizer.Token(3013);
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

                OfferText += Localizer.Token(3041);
                if (ColoniesOffered.Count == 1)
				{
					Offer offer71 = this;
					offer71.OfferText = string.Concat(offer71.OfferText, ColoniesOffered[0], ". ");
				}
				else if (ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < ColoniesOffered.Count; i++)
					{
						if (i >= ColoniesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            Offer offer73 = this;
							offer73.OfferText = string.Concat(offer73.OfferText, ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer74 = this;
							offer74.OfferText = string.Concat(offer74.OfferText, ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer75 = this;
					offer75.OfferText = string.Concat(offer75.OfferText, ColoniesOffered[0], Localizer.Token(3011));
					Offer offer76 = this;
					offer76.OfferText = string.Concat(offer76.OfferText, ColoniesOffered[1], ". ");
				}
			}
			else if (ColoniesOffered.Count > 0)
			{
                OfferText += Localizer.Token(3042);
                if (ColoniesOffered.Count == 1)
				{
					Offer offer78 = this;
					offer78.OfferText = string.Concat(offer78.OfferText, ColoniesOffered[0], ". ");
				}
				else if (ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < ColoniesOffered.Count; i++)
					{
						if (i >= ColoniesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            Offer offer80 = this;
							offer80.OfferText = string.Concat(offer80.OfferText, ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer81 = this;
							offer81.OfferText = string.Concat(offer81.OfferText, ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer82 = this;
					offer82.OfferText = string.Concat(offer82.OfferText, ColoniesOffered[0], Localizer.Token(3011));
					Offer offer83 = this;
					offer83.OfferText = string.Concat(offer83.OfferText, ColoniesOffered[1], ". ");
				}
			}
			if (TheirOffer.EmpiresToWarOn.Count > 0)
			{
				if (!EmpireManager.Player.GetRelations(TheirOffer.Them).Treaty_Alliance)
				{
					if (GetNumberOfDemands(this) > 0 && GetNumberOfDemands(TheirOffer) == 1)
					{
                        OfferText += "In exchange for our leavings, and to avoid your own certain doom, you must declare war upon: ";
                    }
					else if (GetNumberOfDemands(TheirOffer) + GetNumberOfDemands(this) > 2)
					{
                        OfferText += "Finally, we will crush you and your pathetic empire unless you declare war upon: ";
                    }
					else if (GetNumberOfDemands(TheirOffer) <= 1)
					{
						OfferText = "Unless you wish for us to crush your pathetic empire, you will declare war upon: ";
					}
					else
					{
                        OfferText += "Furthermore, we will crush you and your pathetic empire unless you declare war upon: ";
                    }
					if (TheirOffer.EmpiresToWarOn.Count == 1)
					{
                        OfferText += TheirOffer.EmpiresToWarOn[0];
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
                                OfferText += TheirOffer.EmpiresToWarOn[i];
                                OfferText += ", ";
                            }
						}
					}
				}
				else
				{
					if (GetNumberOfDemands(this) > 1 && GetNumberOfDemands(TheirOffer) == 1)
					{
                        OfferText += "We offer you these gifts in the hope that you might join us in war against: ";
                    }
					else if (GetNumberOfDemands(TheirOffer) + GetNumberOfDemands(this) > 2)
					{
                        OfferText += "Finally, we should not have to remind you that we can crush you like a bug. But we can. Therefore, to avoid annihilation, you must declare war on: ";
                    }
					else if (GetNumberOfDemands(TheirOffer) <= 1)
					{
                        OfferText += "The time to do our bidding has come, ally. You must declare war upon: ";
                    }
					else
					{
                        OfferText += "Furthermore, we should not have to remind you that we can crush you like a bug. But we can. Therefore, to avoid annihilation, you must declare war on: ";
                    }
					if (TheirOffer.EmpiresToWarOn.Count == 1)
					{
                        OfferText += TheirOffer.EmpiresToWarOn[0];
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
                                OfferText += TheirOffer.EmpiresToWarOn[i];
                                OfferText += ", ";
                            }
						}
					}
				}
			}
			return OfferText;
		}

		public string DoRespectfulText(Attitude a, Offer TheirOffer)
		{
			OfferText = "";
			if (PeaceTreaty)
			{
                OfferText += Localizer.Token(3000);
                OfferText += "\n\n";
            }
			if (Alliance)
			{
                OfferText += Localizer.Token(3001);
                OfferText += "\n\n";
            }
			if (OpenBorders)
			{
				if (!TheirOffer.OpenBorders)
				{
                    OfferText += Localizer.Token(3003);
                }
				else
				{
                    OfferText += Localizer.Token(3002);
                }
				if (NAPact)
				{
                    OfferText += Localizer.Token(3004);
                }
			}
			else if (TheirOffer.OpenBorders)
			{
                OfferText += Localizer.Token(3005);
                if (NAPact)
				{
                    OfferText += Localizer.Token(3006);
                }
			}
			else if (NAPact)
			{
                OfferText += Localizer.Token(3007);
            }
			if (TradeTreaty)
			{
				if (NAPact || OpenBorders || TheirOffer.OpenBorders)
				{
                    OfferText += "\n";
                    OfferText += Localizer.Token(3008);
                }
				else
				{
                    OfferText += Localizer.Token(3009);
                }
			}
			if (TradeTreaty || OpenBorders || TheirOffer.OpenBorders || NAPact)
			{
                OfferText += "\n";
            }
			if (ArtifactsOffered.Count > 0)
			{
                OfferText += Localizer.Token(3010);
                if (ArtifactsOffered.Count == 1)
				{
					Offer offer15 = this;
					offer15.OfferText = string.Concat(offer15.OfferText, ArtifactsOffered[0], ". ");
				}
				else if (ArtifactsOffered.Count != 2)
				{
					for (int i = 0; i < ArtifactsOffered.Count; i++)
					{
						if (i >= ArtifactsOffered.Count - 1)
						{
                            OfferText += "and ";
                            Offer offer17 = this;
							offer17.OfferText = string.Concat(offer17.OfferText, ArtifactsOffered[i], ". ");
						}
						else
						{
							Offer offer18 = this;
							offer18.OfferText = string.Concat(offer18.OfferText, ArtifactsOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer19 = this;
					offer19.OfferText = string.Concat(offer19.OfferText, ArtifactsOffered[0], Localizer.Token(3011));
					Offer offer20 = this;
					offer20.OfferText = string.Concat(offer20.OfferText, ArtifactsOffered[1], ". ");
				}
			}
			if (TheirOffer.ArtifactsOffered.Count > 0)
			{
                OfferText += Localizer.Token(3012);
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
                            OfferText += Localizer.Token(3013);
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
			if (TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count == 0)
			{
                OfferText += Localizer.Token(3014);
                if (TechnologiesOffered.Count == 1)
				{
                    OfferText += TechOffer(0) + ". ";
                }
				else if (TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TechnologiesOffered.Count; i++)
					{
						if (i >= TechnologiesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            OfferText += TechOffer(i) + ". ";
                        }
						else
						{
							Offer offer32 = this;
							offer32.OfferText = string.Concat(offer32.OfferText, TechOffer(i), ", ");
						}
					}
				}
				else
				{
					Offer offer33 = this;
					offer33.OfferText = string.Concat(offer33.OfferText, TechOffer(0), Localizer.Token(3011));
                    OfferText += TechOffer(1) + ". ";
                }
			}
			else if (TechnologiesOffered.Count == 0 && TheirOffer.TechnologiesOffered.Count > 0)
			{
                OfferText += Localizer.Token(3015);
                if (TheirOffer.TechnologiesOffered.Count == 1)
				{
                    OfferText += TechOffer(0) + ". ";
                }
				else if (TheirOffer.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.TechnologiesOffered.Count; i++)
					{
						if (i >= TechnologiesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            OfferText += TechOffer(i) + ". ";
                        }
						else
						{
							Offer offer39 = this;
							offer39.OfferText = string.Concat(offer39.OfferText, TechOffer(i), ", ");
						}
					}
				}
				else
				{
					Offer offer40 = this;
					offer40.OfferText = string.Concat(offer40.OfferText, TechOffer(0), Localizer.Token(3011));
                    OfferText += TechOffer(1) + ". ";
                }
			}
			else if (TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count > 0)
			{
                OfferText += Localizer.Token(3016);
                if (TheirOffer.TechnologiesOffered.Count == 1)
				{
                    OfferText += TechOffer(0) + ". ";
                }
				else if (TheirOffer.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.TechnologiesOffered.Count; i++)
					{
						if (i >= TechnologiesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            OfferText += TechOffer(i) + ". ";
                        }
						else
						{
							Offer offer46 = this;
							offer46.OfferText = string.Concat(offer46.OfferText, TechOffer(i), ", ");
						}
					}
				}
				else
				{
					Offer offer47 = this;
					offer47.OfferText = string.Concat(offer47.OfferText, TechOffer(0), Localizer.Token(3011));
                    OfferText += TechOffer(1) + ". ";
                }

                OfferText += Localizer.Token(3017);
                if (TechnologiesOffered.Count == 1)
				{
                    OfferText += TechOffer(0) + ". ";
                }
				else if (TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TechnologiesOffered.Count; i++)
					{
						if (i >= TechnologiesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            OfferText += TechOffer(i) + ". ";
                        }
						else
						{
							Offer offer53 = this;
							offer53.OfferText = string.Concat(offer53.OfferText, TechOffer(i), ", ");
						}
					}
				}
				else
				{
					Offer offer54 = this;
					offer54.OfferText = string.Concat(offer54.OfferText, TechOffer(0), Localizer.Token(3011));
                    OfferText += TechOffer(1) + ". ";
                }
			}
			if (TheirOffer.ColoniesOffered.Count > 0 && ColoniesOffered.Count == 0)
			{
                OfferText += Localizer.Token(3018);
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
                            OfferText += Localizer.Token(3013);
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
			else if (TheirOffer.ColoniesOffered.Count > 0 && ColoniesOffered.Count > 0)
			{
                OfferText += Localizer.Token(3019);
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
                            OfferText += Localizer.Token(3013);
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

                OfferText += Localizer.Token(3020);
                if (ColoniesOffered.Count == 1)
				{
					Offer offer71 = this;
					offer71.OfferText = string.Concat(offer71.OfferText, ColoniesOffered[0], ". ");
				}
				else if (ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < ColoniesOffered.Count; i++)
					{
						if (i >= ColoniesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            Offer offer73 = this;
							offer73.OfferText = string.Concat(offer73.OfferText, ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer74 = this;
							offer74.OfferText = string.Concat(offer74.OfferText, ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer75 = this;
					offer75.OfferText = string.Concat(offer75.OfferText, ColoniesOffered[0], Localizer.Token(3011));
					Offer offer76 = this;
					offer76.OfferText = string.Concat(offer76.OfferText, ColoniesOffered[1], ". ");
				}
			}
			else if (ColoniesOffered.Count > 0)
			{
                OfferText += Localizer.Token(3021);
                if (ColoniesOffered.Count == 1)
				{
					Offer offer78 = this;
					offer78.OfferText = string.Concat(offer78.OfferText, ColoniesOffered[0], ". ");
				}
				else if (ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < ColoniesOffered.Count; i++)
					{
						if (i >= ColoniesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            Offer offer80 = this;
							offer80.OfferText = string.Concat(offer80.OfferText, ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer81 = this;
							offer81.OfferText = string.Concat(offer81.OfferText, ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer82 = this;
					offer82.OfferText = string.Concat(offer82.OfferText, ColoniesOffered[0], Localizer.Token(3011));
					Offer offer83 = this;
					offer83.OfferText = string.Concat(offer83.OfferText, ColoniesOffered[1], ". ");
				}
			}
			if (TheirOffer.EmpiresToWarOn.Count > 0)
			{
				if (!EmpireManager.Player.GetRelations(TheirOffer.Them).Treaty_Alliance)
				{
					if (GetNumberOfDemands(this) > 0 && GetNumberOfDemands(TheirOffer) == 1)
					{
                        OfferText += "In exchange for this, we want you to declare war upon: ";
                    }
					else if (GetNumberOfDemands(TheirOffer) + GetNumberOfDemands(this) > 2)
					{
                        OfferText += "Finally, we are requesting that you declare war upon: ";
                    }
					else if (GetNumberOfDemands(TheirOffer) <= 1)
					{
						OfferText = "We believe it would be prudent of you to declare war upon: ";
					}
					else
					{
                        OfferText += "Furthermore, we are requesting that you declare war upon: ";
                    }
					if (TheirOffer.EmpiresToWarOn.Count == 1)
					{
                        OfferText += TheirOffer.EmpiresToWarOn[0];
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
                                OfferText += TheirOffer.EmpiresToWarOn[i];
                                OfferText += ", ";
                            }
						}
					}
				}
				else
				{
					if (GetNumberOfDemands(this) > 1 && GetNumberOfDemands(TheirOffer) == 1)
					{
                        OfferText += "We give you this gift, friend, and now call upon our alliance in requesting that you declare war upon: ";
                    }
					else if (GetNumberOfDemands(TheirOffer) + GetNumberOfDemands(this) > 2)
					{
                        OfferText += "Finally, we call upon our alliance and request that you declare war upon: ";
                    }
					else if (GetNumberOfDemands(TheirOffer) <= 1)
					{
                        OfferText += "Friend, it is time for us to call upon our allies to join us in war. You must declare war upon: ";
                    }
					else
					{
                        OfferText += "Furthermore, we call upon our alliance and request that you declare war upon: ";
                    }
					if (TheirOffer.EmpiresToWarOn.Count == 1)
					{
                        OfferText += TheirOffer.EmpiresToWarOn[0];
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
                                OfferText += TheirOffer.EmpiresToWarOn[i];
                                OfferText += ", ";
                            }
						}
					}
				}
			}
			return OfferText;
		}

		public string DoThreateningText(Attitude a, Offer TheirOffer)
		{
			OfferText = "";
			if (PeaceTreaty)
			{
                OfferText += Localizer.Token(3043);
                OfferText += "\n\n";
            }
			if (Alliance)
			{
                OfferText += Localizer.Token(3044);
                OfferText += "\n\n";
            }
			if (OpenBorders)
			{
				if (!TheirOffer.OpenBorders)
				{
                    OfferText += Localizer.Token(3046);
                }
				else
				{
                    OfferText += Localizer.Token(3045);
                }
				if (NAPact)
				{
                    OfferText += Localizer.Token(3047);
                }
			}
			else if (TheirOffer.OpenBorders)
			{
                OfferText += Localizer.Token(3048);
                if (NAPact)
				{
                    OfferText += Localizer.Token(3049);
                }
			}
			else if (NAPact)
			{
                OfferText += Localizer.Token(3050);
            }
			if (TradeTreaty)
			{
				if (NAPact || OpenBorders || TheirOffer.OpenBorders)
				{
                    OfferText += "\n\n";
                    OfferText += Localizer.Token(3051);
                }
				else
				{
                    OfferText += Localizer.Token(3052);
                }
			}
			if (TradeTreaty || OpenBorders || TheirOffer.OpenBorders || NAPact)
			{
                OfferText += "\n\n";
            }
			if (ArtifactsOffered.Count > 0)
			{
                OfferText += Localizer.Token(3053);
                if (ArtifactsOffered.Count == 1)
				{
					Offer offer15 = this;
					offer15.OfferText = string.Concat(offer15.OfferText, ArtifactsOffered[0], ". ");
				}
				else if (ArtifactsOffered.Count != 2)
				{
					for (int i = 0; i < ArtifactsOffered.Count; i++)
					{
						if (i >= ArtifactsOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            Offer offer17 = this;
							offer17.OfferText = string.Concat(offer17.OfferText, ArtifactsOffered[i], ". ");
						}
						else
						{
							Offer offer18 = this;
							offer18.OfferText = string.Concat(offer18.OfferText, ArtifactsOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer19 = this;
					offer19.OfferText = string.Concat(offer19.OfferText, ArtifactsOffered[0], Localizer.Token(3011));
					Offer offer20 = this;
					offer20.OfferText = string.Concat(offer20.OfferText, ArtifactsOffered[1], ". ");
				}
			}
			if (TheirOffer.ArtifactsOffered.Count > 0)
			{
                OfferText += Localizer.Token(3054);
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
                            OfferText += Localizer.Token(3013);
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
			if (TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count == 0)
			{
                OfferText += Localizer.Token(3055);
                if (TechnologiesOffered.Count == 1)
				{
                    OfferText += TechOffer(0) + ". ";
                }
				else if (TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TechnologiesOffered.Count; i++)
					{
						if (i >= TechnologiesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            OfferText += TechOffer(i) + ". ";
                        }
						else
						{
							Offer offer32 = this;
							offer32.OfferText = string.Concat(offer32.OfferText, TechOffer(i), ", ");
						}
					}
				}
				else
				{
					Offer offer33 = this;
					offer33.OfferText = string.Concat(offer33.OfferText, TechOffer(0), Localizer.Token(3011));
                    OfferText += TechOffer(1) + ". ";
                }

                OfferText += Localizer.Token(3056);
            }
			else if (TechnologiesOffered.Count == 0 && TheirOffer.TechnologiesOffered.Count > 0)
			{
                OfferText += Localizer.Token(3057);
                if (TheirOffer.TechnologiesOffered.Count == 1)
				{
                    OfferText += TechOffer(0) + ". ";
                }
				else if (TheirOffer.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.TechnologiesOffered.Count; i++)
					{
						if (i >= TechnologiesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            OfferText += TechOffer(i) + ". ";
                        }
						else
						{
							Offer offer40 = this;
							offer40.OfferText = string.Concat(offer40.OfferText, TechOffer(i), ", ");
						}
					}
				}
				else
				{
					Offer offer41 = this;
					offer41.OfferText = string.Concat(offer41.OfferText, TechOffer(0), Localizer.Token(3011));
                    OfferText += TechOffer(1) + ". ";
                }
			}
			else if (TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count > 0)
			{
                OfferText += Localizer.Token(3058);
                if (TheirOffer.TechnologiesOffered.Count == 1)
				{
                    OfferText += TechOffer(0) + ". ";
                }
				else if (TheirOffer.TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TheirOffer.TechnologiesOffered.Count; i++)
					{
						if (i >= TechnologiesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            OfferText += TechOffer(i) + ". ";
                        }
						else
						{
							Offer offer47 = this;
							offer47.OfferText = string.Concat(offer47.OfferText, TechOffer(i), ", ");
						}
					}
				}
				else
				{
					Offer offer48 = this;
					offer48.OfferText = string.Concat(offer48.OfferText, TechOffer(0), Localizer.Token(3011));
                    OfferText += TechOffer(1) + ". ";
                }

                OfferText += Localizer.Token(3059);
                if (TechnologiesOffered.Count == 1)
				{
                    OfferText += TechOffer(0) + ". ";
                }
				else if (TechnologiesOffered.Count != 2)
				{
					for (int i = 0; i < TechnologiesOffered.Count; i++)
					{
						if (i >= TechnologiesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            OfferText += TechOffer(i) + ". ";
                        }
						else
						{
							Offer offer54 = this;
							offer54.OfferText = string.Concat(offer54.OfferText, TechOffer(i), ", ");
						}
					}
				}
				else
				{
					Offer offer55 = this;
					offer55.OfferText = string.Concat(offer55.OfferText, TechOffer(0), Localizer.Token(3011));
                    OfferText += TechOffer(1) + ". ";
                }
			}
			if (TheirOffer.ColoniesOffered.Count > 0 && ColoniesOffered.Count == 0)
			{
                OfferText += Localizer.Token(3060);
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
                            OfferText += Localizer.Token(3013);
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
			else if (TheirOffer.ColoniesOffered.Count > 0 && ColoniesOffered.Count > 0)
			{
                OfferText += Localizer.Token(3061);
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
                            OfferText += Localizer.Token(3013);
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

                OfferText += Localizer.Token(3062);
                if (ColoniesOffered.Count == 1)
				{
					Offer offer72 = this;
					offer72.OfferText = string.Concat(offer72.OfferText, ColoniesOffered[0], ". ");
				}
				else if (ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < ColoniesOffered.Count; i++)
					{
						if (i >= ColoniesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            Offer offer74 = this;
							offer74.OfferText = string.Concat(offer74.OfferText, ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer75 = this;
							offer75.OfferText = string.Concat(offer75.OfferText, ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer76 = this;
					offer76.OfferText = string.Concat(offer76.OfferText, ColoniesOffered[0], Localizer.Token(3011));
					Offer offer77 = this;
					offer77.OfferText = string.Concat(offer77.OfferText, ColoniesOffered[1], ". ");
				}
			}
			else if (ColoniesOffered.Count > 0)
			{
                OfferText += Localizer.Token(3063);
                if (ColoniesOffered.Count == 1)
				{
					Offer offer79 = this;
					offer79.OfferText = string.Concat(offer79.OfferText, ColoniesOffered[0], ". ");
				}
				else if (ColoniesOffered.Count != 2)
				{
					for (int i = 0; i < ColoniesOffered.Count; i++)
					{
						if (i >= ColoniesOffered.Count - 1)
						{
                            OfferText += Localizer.Token(3013);
                            Offer offer81 = this;
							offer81.OfferText = string.Concat(offer81.OfferText, ColoniesOffered[i], ". ");
						}
						else
						{
							Offer offer82 = this;
							offer82.OfferText = string.Concat(offer82.OfferText, ColoniesOffered[i], ", ");
						}
					}
				}
				else
				{
					Offer offer83 = this;
					offer83.OfferText = string.Concat(offer83.OfferText, ColoniesOffered[0], Localizer.Token(3011));
					Offer offer84 = this;
					offer84.OfferText = string.Concat(offer84.OfferText, ColoniesOffered[1], ". ");
				}
			}
			if (TheirOffer.EmpiresToWarOn.Count > 0)
			{
				if (!EmpireManager.Player.GetRelations(TheirOffer.Them).Treaty_Alliance)
				{
					if (GetNumberOfDemands(this) > 0 && GetNumberOfDemands(TheirOffer) == 1)
					{
                        OfferText += "In exchange for our leavings, and to avoid your own certain doom, you must declare war upon: ";
                    }
					else if (GetNumberOfDemands(TheirOffer) + GetNumberOfDemands(this) > 2)
					{
                        OfferText += "Finally, we will crush you and your pathetic empire unless you declare war upon: ";
                    }
					else if (GetNumberOfDemands(TheirOffer) <= 1)
					{
						OfferText = "Unless you wish for us to crush your pathetic empire, you will declare war upon: ";
					}
					else
					{
                        OfferText += "Furthermore, we will crush you and your pathetic empire unless you declare war upon: ";
                    }
					if (TheirOffer.EmpiresToWarOn.Count == 1)
					{
                        OfferText += TheirOffer.EmpiresToWarOn[0];
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
                                OfferText += TheirOffer.EmpiresToWarOn[i];
                                OfferText += ", ";
                            }
						}
					}
				}
				else
				{
					if (GetNumberOfDemands(this) > 1 && GetNumberOfDemands(TheirOffer) == 1)
					{
                        OfferText += "Now, take these leavings and declare war on our enemies lest you become one! You must war upon: ";
                    }
					else if (GetNumberOfDemands(TheirOffer) + GetNumberOfDemands(this) > 2)
					{
                        OfferText += "Finally, we should not have to remind you that we can crush you like a bug. But we can. Therefore, to avoid annihilation, you must declare war on: ";
                    }
					else if (GetNumberOfDemands(TheirOffer) <= 1)
					{
                        OfferText += "The time to do our bidding has come, ally. You must declare war upon: ";
                    }
					else
					{
                        OfferText += "Furthermore, we should not have to remind you that we can crush you like a bug. But we can. Therefore, to avoid annihilation, you must declare war on: ";
                    }
					if (TheirOffer.EmpiresToWarOn.Count == 1)
					{
                        OfferText += TheirOffer.EmpiresToWarOn[0];
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
                                OfferText += TheirOffer.EmpiresToWarOn[i];
                                OfferText += ", ";
                            }
						}
					}
				}
			}
			return OfferText;
		}

		public string FormulateOfferText(Attitude a, Offer TheirOffer)
		{
			switch (a)
			{
				case Attitude.Pleading:
				{
					return DoPleadingText(a, TheirOffer);
				}
				case Attitude.Respectful:
				{
					return DoRespectfulText(a, TheirOffer);
				}
				case Attitude.Threaten:
				{
					return DoThreateningText(a, TheirOffer);
				}
			}
			return OfferText;
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
			if (!NAPact && !PeaceTreaty && !OpenBorders && !TradeTreaty && TechnologiesOffered.Count <= 0 && ColoniesOffered.Count <= 0 && ArtifactsOffered.Count <= 0 && EmpiresToMakePeaceWith.Count <= 0 && EmpiresToWarOn.Count <= 0)
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