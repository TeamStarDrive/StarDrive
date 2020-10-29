using System;
using System.Text;

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
        public Empire Them;

        string TechOffer(int i) => Localizer.Token(ResourceManager.TechTree[TechnologiesOffered[i]].NameIndex);
        string ArtifactOffer(int i) => Localizer.Token(ResourceManager.ArtifactsDict[ArtifactsOffered[i]].NameIndex);
        string ColonyOffer(int i) => ColoniesOffered[i];

        public string DoPleadingText(Attitude a, Offer TheirOffer)
        {
            var text = new StringBuilder();
            if (PeaceTreaty)
            {
                text.Append(Localizer.Token(3022), "\n\n");
            }
            if (Alliance)
            {
                text.Append(Localizer.Token(3023), "\n\n");
            }
            if (OpenBorders)
            {
                text.Append(TheirOffer.OpenBorders ? Localizer.Token(3024) : Localizer.Token(3025));
                if (NAPact)
                {
                    text.Append(Localizer.Token(3026));
                }
            }
            else if (TheirOffer.OpenBorders)
            {
                text.Append(Localizer.Token(3027));
                if (NAPact)
                {
                    text.Append(Localizer.Token(3028));
                }
            }
            else if (NAPact)
            {
                text.Append(Localizer.Token(3029));
            }
            if (ArtifactsOffered.Count > 0)
            {
                text.Append(Localizer.Token(3030));
                text.Append(ArtifactStringsToText());
            }
            if (TheirOffer.ArtifactsOffered.Count > 0)
            {
                text.Append(Localizer.Token(3031));
                text.Append(TheirOffer.ArtifactStringsToText());
                //text.Append(Localizer.Token(3032)
            }
            if (TradeTreaty)
            {
                if (NAPact || OpenBorders || TheirOffer.OpenBorders)
                {
                    text.Append("\n\n", Localizer.Token(3033));
                }
                else
                {
                    text.Append(Localizer.Token(3034));
                }
            }
            if (TradeTreaty || OpenBorders || TheirOffer.OpenBorders || NAPact)
            {
                text.Append("\n\n");
            }
            if (TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count == 0)
            {
                text.Append(Localizer.Token(3035));
                text.Append(TechStringsToText());
            }
            else if (TechnologiesOffered.Count == 0 && TheirOffer.TechnologiesOffered.Count > 0)
            {
                text.Append(Localizer.Token(3036));
                text.Append(TheirOffer.TechStringsToText());
            }
            else if (TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count > 0)
            {
                text.Append(Localizer.Token(3037));
                text.Append(TheirOffer.TechStringsToText());

                text.Append(Localizer.Token(3038));
                text.Append(TechStringsToText());
            }
            if (TheirOffer.ColoniesOffered.Count > 0 && ColoniesOffered.Count == 0)
            {
                text.Append(Localizer.Token(3039));
                text.Append(TheirOffer.ColonyStringsToText());
            }
            else if (TheirOffer.ColoniesOffered.Count > 0 && ColoniesOffered.Count > 0)
            {
                text.Append(Localizer.Token(3040));
                text.Append(TheirOffer.ColonyStringsToText());

                text.Append(Localizer.Token(3041));
                text.Append(ColonyStringsToText());
            }
            else if (ColoniesOffered.Count > 0)
            {
                text.Append(Localizer.Token(3042));
                text.Append(ColonyStringsToText());
            }
            if (TheirOffer.EmpiresToWarOn.Count > 0)
            {
                if (!EmpireManager.Player.IsAlliedWith(TheirOffer.Them))
                {
                    if (GetNumberOfDemands(this) > 0 && GetNumberOfDemands(TheirOffer) == 1)
                    {
                        text.Append("In exchange for our leavings, and to avoid your own certain doom, you must declare war upon: ");
                    }
                    else if (GetNumberOfDemands(TheirOffer) + GetNumberOfDemands(this) > 2)
                    {
                        text.Append("Finally, we will crush you and your pathetic empire unless you declare war upon: ");
                    }
                    else if (GetNumberOfDemands(TheirOffer) <= 1)
                    {
                        text.Append("Unless you wish for us to crush your pathetic empire, you will declare war upon: ");
                    }
                    else
                    {
                        text.Append("Furthermore, we will crush you and your pathetic empire unless you declare war upon: ");
                    }
                    if (TheirOffer.EmpiresToWarOn.Count == 1)
                    {
                        text.Append(TheirOffer.EmpiresToWarOn[0]);
                    }
                    else if (TheirOffer.EmpiresToWarOn.Count == 2)
                    {
                        text.Append(TheirOffer.EmpiresToWarOn[0], " and ", TheirOffer.EmpiresToWarOn[1]);
                    }
                    else if (TheirOffer.EmpiresToWarOn.Count > 2)
                    {
                        for (int i = 0; i < TheirOffer.EmpiresToWarOn.Count; i++)
                        {
                            if (i >= TheirOffer.EmpiresToWarOn.Count - 1)
                            {
                                text.Append(TheirOffer.EmpiresToWarOn[i], " and ");
                            }
                            else
                            {
                                text.Append(TheirOffer.EmpiresToWarOn[i], ", ");
                            }
                        }
                    }
                }
                else
                {
                    if (GetNumberOfDemands(this) > 1 && GetNumberOfDemands(TheirOffer) == 1)
                    {
                        text.Append("We offer you these gifts in the hope that you might join us in war against: ");
                    }
                    else if (GetNumberOfDemands(TheirOffer) + GetNumberOfDemands(this) > 2)
                    {
                        text.Append("Finally, we should not have to remind you that we can crush you like a bug. But we can. Therefore, to avoid annihilation, you must declare war on: ");
                    }
                    else if (GetNumberOfDemands(TheirOffer) <= 1)
                    {
                        text.Append("The time to do our bidding has come, ally. You must declare war upon: ");
                    }
                    else
                    {
                        text.Append("Furthermore, we should not have to remind you that we can crush you like a bug. But we can. Therefore, to avoid annihilation, you must declare war on: ");
                    }
                    if (TheirOffer.EmpiresToWarOn.Count == 1)
                    {
                        text.Append(TheirOffer.EmpiresToWarOn[0]);
                    }
                    else if (TheirOffer.EmpiresToWarOn.Count == 2)
                    {
                        text.Append(TheirOffer.EmpiresToWarOn[0], " and ", TheirOffer.EmpiresToWarOn[1]);
                    }
                    else if (TheirOffer.EmpiresToWarOn.Count > 2)
                    {
                        for (int i = 0; i < TheirOffer.EmpiresToWarOn.Count; i++)
                        {
                            if (i >= TheirOffer.EmpiresToWarOn.Count - 1)
                            {
                                text.Append(TheirOffer.EmpiresToWarOn[i], " and ");
                            }
                            else
                            {
                                text.Append(TheirOffer.EmpiresToWarOn[i], ", ");
                            }
                        }
                    }
                }
            }
            return text.ToString();
        }

        public string DoRespectfulText(Attitude a, Offer TheirOffer)
        {
            var text = new StringBuilder();
            if (PeaceTreaty)
            {
                text.Append(Localizer.Token(3000), "\n\n");
            }
            if (Alliance)
            {
                text.Append(Localizer.Token(3001), "\n\n");
            }
            if (OpenBorders)
            {
                text.Append(TheirOffer.OpenBorders ? Localizer.Token(3002) : Localizer.Token(3003));
                if (NAPact)
                {
                    text.Append(Localizer.Token(3004));
                }
            }
            else if (TheirOffer.OpenBorders)
            {
                text.Append(Localizer.Token(3005));
                if (NAPact)
                {
                    text.Append(Localizer.Token(3006));
                }
            }
            else if (NAPact)
            {
                text.Append(Localizer.Token(3007));
            }
            if (TradeTreaty)
            {
                if (NAPact || OpenBorders || TheirOffer.OpenBorders)
                {
                    text.Append("\n", Localizer.Token(3008));
                }
                else
                {
                    text.Append(Localizer.Token(3009));
                }
            }
            if (TradeTreaty || OpenBorders || TheirOffer.OpenBorders || NAPact)
            {
                text.Append("\n");
            }
            if (ArtifactsOffered.Count > 0)
            {
                text.Append(Localizer.Token(3010));
                text.Append(ArtifactStringsToText());
            }
            if (TheirOffer.ArtifactsOffered.Count > 0)
            {
                text.Append(Localizer.Token(3012));
                text.Append(TheirOffer.ArtifactStringsToText());
               
            }
            if (TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count == 0)
            {
                text.Append(Localizer.Token(3014));
                text.Append(TechStringsToText());
            }
            else if (TechnologiesOffered.Count == 0 && TheirOffer.TechnologiesOffered.Count > 0)
            {
                text.Append(Localizer.Token(3015));
                text.Append(TheirOffer.TechStringsToText());
            }
            else if (TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count > 0)
            {
                text.Append(Localizer.Token(3016));
                text.Append(TheirOffer.TechStringsToText());

                text.Append(Localizer.Token(3017));
                text.Append(TechStringsToText());

            }
            if (TheirOffer.ColoniesOffered.Count > 0 && ColoniesOffered.Count == 0)
            {
                text.Append(Localizer.Token(3018));
                text.Append(TheirOffer.ColonyStringsToText());
            }
            else if (TheirOffer.ColoniesOffered.Count > 0 && ColoniesOffered.Count > 0)
            {
                text.Append(Localizer.Token(3019));
                text.Append(TheirOffer.ColonyStringsToText());

                text.Append(Localizer.Token(3020));
                text.Append(ColonyStringsToText());
            }
            else if (ColoniesOffered.Count > 0)
            {
                text.Append(Localizer.Token(3021));
                text.Append(ColonyStringsToText());
            }
            if (TheirOffer.EmpiresToWarOn.Count > 0)
            {
                if (!EmpireManager.Player.IsAlliedWith(TheirOffer.Them))
                {
                    if (GetNumberOfDemands(this) > 0 && GetNumberOfDemands(TheirOffer) == 1)
                    {
                        text.Append("In exchange for this, we want you to declare war upon: ");
                    }
                    else if (GetNumberOfDemands(TheirOffer) + GetNumberOfDemands(this) > 2)
                    {
                        text.Append("Finally, we are requesting that you declare war upon: ");
                    }
                    else if (GetNumberOfDemands(TheirOffer) <= 1)
                    {
                        text.Append("We believe it would be prudent of you to declare war upon: ");
                    }
                    else
                    {
                        text.Append("Furthermore, we are requesting that you declare war upon: ");
                    }
                    if (TheirOffer.EmpiresToWarOn.Count == 1)
                    {
                        text.Append(TheirOffer.EmpiresToWarOn[0]);
                    }
                    else if (TheirOffer.EmpiresToWarOn.Count == 2)
                    {
                        text.Append(TheirOffer.EmpiresToWarOn[0], " and ", TheirOffer.EmpiresToWarOn[1]);
                    }
                    else if (TheirOffer.EmpiresToWarOn.Count > 2)
                    {
                        for (int i = 0; i < TheirOffer.EmpiresToWarOn.Count; i++)
                        {
                            if (i >= TheirOffer.EmpiresToWarOn.Count - 1)
                            {
                                text.Append(TheirOffer.EmpiresToWarOn[i], " and ");
                            }
                            else
                            {
                                text.Append(TheirOffer.EmpiresToWarOn[i], ", ");
                            }
                        }
                    }
                }
                else
                {
                    if (GetNumberOfDemands(this) > 1 && GetNumberOfDemands(TheirOffer) == 1)
                    {
                        text.Append("We give you this gift, friend, and now call upon our alliance in requesting that you declare war upon: ");
                    }
                    else if (GetNumberOfDemands(TheirOffer) + GetNumberOfDemands(this) > 2)
                    {
                        text.Append("Finally, we call upon our alliance and request that you declare war upon: ");
                    }
                    else if (GetNumberOfDemands(TheirOffer) <= 1)
                    {
                        text.Append("Friend, it is time for us to call upon our allies to join us in war. You must declare war upon: ");
                    }
                    else
                    {
                        text.Append("Furthermore, we call upon our alliance and request that you declare war upon: ");
                    }
                    if (TheirOffer.EmpiresToWarOn.Count == 1)
                    {
                        text.Append(TheirOffer.EmpiresToWarOn[0]);
                    }
                    else if (TheirOffer.EmpiresToWarOn.Count == 2)
                    {
                        text.Append(TheirOffer.EmpiresToWarOn[0], " and ", TheirOffer.EmpiresToWarOn[1]);
                    }
                    else if (TheirOffer.EmpiresToWarOn.Count > 2)
                    {
                        for (int i = 0; i < TheirOffer.EmpiresToWarOn.Count; i++)
                        {
                            if (i >= TheirOffer.EmpiresToWarOn.Count - 1)
                            {
                                text.Append(TheirOffer.EmpiresToWarOn[i], " and ");
                            }
                            else
                            {
                                text.Append(TheirOffer.EmpiresToWarOn[i], ", ");
                            }
                        }
                    }
                }
            }
            return text.ToString();
        }

        public StringBuilder TechStringsToText() => StringBuilderCommaDelimitedList(TechnologiesOffered, TechOffer);
        public StringBuilder ArtifactStringsToText() => StringBuilderCommaDelimitedList(ArtifactsOffered, ArtifactOffer);
        public StringBuilder ColonyStringsToText() => StringBuilderCommaDelimitedList(ColoniesOffered, ColonyOffer);

        private StringBuilder StringBuilderCommaDelimitedList(Array<string> stringList, Func<int,string> localization)
        {
            StringBuilder text = new StringBuilder();
            if (stringList.Count == 1)
            {
                text.Append(localization(0), ". ");
            }
            else if (stringList.Count > 2)
            {
                for (int i = 0; i < stringList.Count; i++)
                {
                    if (i >= stringList.Count - 1)
                    {
                        text.Append(Localizer.Token(3013), localization(i), ". ");
                    }
                    else
                    {
                        text.Append(localization(i), ", ");
                    }
                }
            }
            else if (stringList.Count == 2)
            {
                text.Append(localization(0), Localizer.Token(3011), localization(1), ". ");
            }
            return text;
        }

        public string DoThreateningText(Attitude a, Offer TheirOffer)
        {
            var text = new StringBuilder();
            if (PeaceTreaty)
            {
                text.Append(Localizer.Token(3043), "\n\n");
            }
            if (Alliance)
            {
                text.Append(Localizer.Token(3044), "\n\n");
            }
            if (OpenBorders)
            {
                text.Append(TheirOffer.OpenBorders ? Localizer.Token(3045) : Localizer.Token(3046));
                if (NAPact)
                {
                    text.Append(Localizer.Token(3047));
                }
            }
            else if (TheirOffer.OpenBorders)
            {
                text.Append(Localizer.Token(3048));
                if (NAPact)
                {
                    text.Append(Localizer.Token(3049));
                }
            }
            else if (NAPact)
            {
                text.Append(Localizer.Token(3050));
            }
            if (TradeTreaty)
            {
                if (NAPact || OpenBorders || TheirOffer.OpenBorders)
                {
                    text.Append("\n\n", Localizer.Token(3051));
                }
                else
                {
                    text.Append(Localizer.Token(3052));
                }
            }
            if (TradeTreaty || OpenBorders || TheirOffer.OpenBorders || NAPact)
            {
                text.Append("\n\n");
            }
            if (ArtifactsOffered.Count > 0)
            {
                text.Append(Localizer.Token(3053));
                text.Append(ArtifactStringsToText());
            }
            if (TheirOffer.ArtifactsOffered.Count > 0)
            {
                text.Append(Localizer.Token(3054));
                text.Append(TheirOffer.ArtifactStringsToText());
            }
            if (TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count == 0)
            {
                text.Append(Localizer.Token(3055));
                text.Append(TechStringsToText());
                text.Append(Localizer.Token(3056));
            }
            else if (TechnologiesOffered.Count == 0 && TheirOffer.TechnologiesOffered.Count > 0)
            {
                text.Append(Localizer.Token(3057));
                text.Append(TheirOffer.TechStringsToText());
            }
            else if (TechnologiesOffered.Count > 0 && TheirOffer.TechnologiesOffered.Count > 0)
            {
                text.Append(Localizer.Token(3058));
                text.Append(TheirOffer.TechStringsToText());

                text.Append(Localizer.Token(3059));
                text.Append(TechStringsToText());

            }
            if (TheirOffer.ColoniesOffered.Count > 0 && ColoniesOffered.Count == 0)
            {
                text.Append(Localizer.Token(3060));
                text.Append(TheirOffer.ColonyStringsToText());
            }
            else if (TheirOffer.ColoniesOffered.Count > 0 && ColoniesOffered.Count > 0)
            {
                text.Append(Localizer.Token(3061));
                text.Append(TheirOffer.ColonyStringsToText());

                text.Append(Localizer.Token(3062));
                text.Append(ColonyStringsToText());
            }
            else if (ColoniesOffered.Count > 0)
            {
                text.Append(Localizer.Token(3063));
                text.Append(ColonyStringsToText());
            }
            if (TheirOffer.EmpiresToWarOn.Count > 0)
            {
                if (!EmpireManager.Player.IsAlliedWith(TheirOffer.Them))
                {
                    if (GetNumberOfDemands(this) > 0 && GetNumberOfDemands(TheirOffer) == 1)
                    {
                        text.Append("In exchange for our leavings, and to avoid your own certain doom, you must declare war upon: ");
                    }
                    else if (GetNumberOfDemands(TheirOffer) + GetNumberOfDemands(this) > 2)
                    {
                        text.Append("Finally, we will crush you and your pathetic empire unless you declare war upon: ");
                    }
                    else if (GetNumberOfDemands(TheirOffer) <= 1)
                    {
                        text.Append("Unless you wish for us to crush your pathetic empire, you will declare war upon: ");
                    }
                    else
                    {
                        text.Append("Furthermore, we will crush you and your pathetic empire unless you declare war upon: ");
                    }
                    if (TheirOffer.EmpiresToWarOn.Count == 1)
                    {
                        text.Append(TheirOffer.EmpiresToWarOn[0]);
                    }
                    else if (TheirOffer.EmpiresToWarOn.Count == 2)
                    {
                        text.Append(TheirOffer.EmpiresToWarOn[0], " and ", TheirOffer.EmpiresToWarOn[1]);
                    }
                    else if (TheirOffer.EmpiresToWarOn.Count > 2)
                    {
                        for (int i = 0; i < TheirOffer.EmpiresToWarOn.Count; i++)
                        {
                            if (i >= TheirOffer.EmpiresToWarOn.Count - 1)
                            {
                                text.Append(TheirOffer.EmpiresToWarOn[i], " and ");
                            }
                            else
                            {
                                text.Append(TheirOffer.EmpiresToWarOn[i], ", ");
                            }
                        }
                    }
                }
                else
                {
                    if (GetNumberOfDemands(this) > 1 && GetNumberOfDemands(TheirOffer) == 1)
                    {
                        text.Append("Now, take these leavings and declare war on our enemies lest you become one! You must war upon: ");
                    }
                    else if (GetNumberOfDemands(TheirOffer) + GetNumberOfDemands(this) > 2)
                    {
                        text.Append("Finally, we should not have to remind you that we can crush you like a bug. But we can. Therefore, to avoid annihilation, you must declare war on: ");
                    }
                    else if (GetNumberOfDemands(TheirOffer) <= 1)
                    {
                        text.Append("The time to do our bidding has come, ally. You must declare war upon: ");
                    }
                    else
                    {
                        text.Append("Furthermore, we should not have to remind you that we can crush you like a bug. But we can. Therefore, to avoid annihilation, you must declare war on: ");
                    }
                    if (TheirOffer.EmpiresToWarOn.Count == 1)
                    {
                        text.Append(TheirOffer.EmpiresToWarOn[0]);
                    }
                    else if (TheirOffer.EmpiresToWarOn.Count == 2)
                    {
                        text.Append(TheirOffer.EmpiresToWarOn[0], " and ", TheirOffer.EmpiresToWarOn[1]);
                    }
                    else if (TheirOffer.EmpiresToWarOn.Count > 2)
                    {
                        for (int i = 0; i < TheirOffer.EmpiresToWarOn.Count; i++)
                        {
                            if (i >= TheirOffer.EmpiresToWarOn.Count - 1)
                            {
                                text.Append(TheirOffer.EmpiresToWarOn[i], " and ");
                            }
                            else
                            {
                                text.Append(TheirOffer.EmpiresToWarOn[i], ", ");
                            }
                        }
                    }
                }
            }
            return text.ToString();
        }

        public string FormulateOfferText(Attitude a, Offer TheirOffer)
        {
            switch (a)
            {
                case Attitude.Pleading:   return DoPleadingText(a, TheirOffer);
                case Attitude.Respectful: return DoRespectfulText(a, TheirOffer);
                case Attitude.Threaten:   return DoThreateningText(a, TheirOffer);
            }
            return "";
        }

        public int GetNumberOfDemands(Offer which)
        {
            int num = 0;
            if (which.NAPact) num++;
            if (which.PeaceTreaty) num++;
            if (which.OpenBorders) num++;
            if (which.TradeTreaty) num++;
            if (which.TechnologiesOffered.Count > 0) num++;
            if (which.ColoniesOffered.Count > 0)     num++;
            if (which.ArtifactsOffered.Count > 0)    num++;
            if (which.EmpiresToMakePeaceWith.Count > 0) num++;
            if (which.EmpiresToWarOn.Count > 0)         num++;
            return num;
        }

        public bool IsBlank()
        {
            return !NAPact && !PeaceTreaty
                && !OpenBorders && !TradeTreaty
                && TechnologiesOffered.Count <= 0
                && ColoniesOffered.Count <= 0
                && ArtifactsOffered.Count <= 0
                && EmpiresToMakePeaceWith.Count <= 0
                && EmpiresToWarOn.Count <= 0;
        }

        public enum Attitude
        {
            Pleading,
            Respectful,
            Threaten
        }
    }
}