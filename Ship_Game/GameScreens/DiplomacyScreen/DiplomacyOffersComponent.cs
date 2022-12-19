using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Gameplay;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.GameScreens.DiplomacyScreen
{
    // Contains Scroll list and actions for ItemToOffer such as "Open Borders" etc
    class DiplomacyOffersComponent : UIElementContainer
    {
        readonly ScrollList<ItemToOffer> List;
        DiplomacyOffersComponent Theirs;
        readonly Empire Us;
        readonly Empire Them;
        Offer OurOffer;
        Offer TheirOffer;

        // EVT: offer has changed (probably)
        public Action OnOfferChanged;

        public DiplomacyOffersComponent(Empire empire, Empire other,
                                        Rectangle rect, SubTexture background) : base(rect)
        {
            Visible = false;
            Us = empire;
            Them = other;
            
            int backW = background.Width + 40;
            int backH = (int)background.GetHeightFromWidthAspect(backW);
            var back = new Rectangle(rect.X-5, rect.Y-5, backW, backH);
            base.Add(new UIPanel(back, background));

            RectF list = new(rect.X + 10, rect.Y + 30, rect.Width - 10, rect.Height - 40);
            List = base.Add(new ScrollList<ItemToOffer>(list));

            GameText title = empire.isPlayer ? GameText.WeHave : GameText.TheyHave;
            base.Add(new UILabel(new Vector2(rect.X + 26, rect.Y + 14), title, Fonts.Pirulen12));
        }

        public void StartNegotiation(DiplomacyOffersComponent theirs, Offer ourOffer, Offer theirOffer)
        {
            Visible = true;
            Theirs = theirs;
            OurOffer = ourOffer;
            TheirOffer = theirOffer;
            List.OnClick = OnDiplomacyItemClicked;
            Reset();
        }

        void SelectTheirItem(string response, bool selected)
        {
            foreach (ItemToOffer theirs in Theirs.List.AllEntries)
            {
                if (theirs.Response == response)
                {
                    theirs.Selected = selected;
                    break;
                }
            }
        }

        void OnDiplomacyItemClicked(ItemToOffer ourItem)
        {
            ourItem.Selected = !ourItem.Selected;
            bool selected = ourItem.Selected;
            switch (ourItem.Response)
            {
                case "NAPact":
                    OurOffer.NAPact = TheirOffer.NAPact = selected;
                    SelectTheirItem(ourItem.Response, selected);
                    break;
                case "We Declare War":
                    OurOffer.NAPact = TheirOffer.NAPact = selected;
                    SelectTheirItem("NAPact", selected);
                    break;
                case "Peace Treaty":
                    OurOffer.PeaceTreaty = TheirOffer.PeaceTreaty = selected;
                    SelectTheirItem(ourItem.Response, selected);
                    break;
                case "OfferAlliance":
                    OurOffer.Alliance = TheirOffer.Alliance = selected;
                    SelectTheirItem(ourItem.Response, selected);
                    break;
                case "OpenBorders": OurOffer.OpenBorders = selected; break;
                case "Declare War": ourItem.ChangeSpecialInquiry(OurOffer.EmpiresToWarOn);      break;
                case "Tech":        ourItem.ChangeSpecialInquiry(OurOffer.TechnologiesOffered); break;
                case "Artifacts":   ourItem.ChangeSpecialInquiry(OurOffer.ArtifactsOffered);    break;
                case "Colony":      ourItem.ChangeSpecialInquiry(OurOffer.ColoniesOffered);     break;
                case "TradeTreaty":
                    OurOffer.TradeTreaty = TheirOffer.TradeTreaty = selected;
                    SelectTheirItem("TradeTreaty", selected);
                    break;
            }

            OnOfferChanged?.Invoke();
        }

        void Reset()
        {
            List.Reset();
            AddRelationItems(Us.GetRelations(Them));
            var theirDesigns = Them.AllFactionShipDesigns;
            ItemToOffer techs = AddHeader(GameText.Technology);
            Us.AI.TradableTechs(Them, out Array<TechEntry> tradeAbleTechs);
            foreach (TechEntry entry in tradeAbleTechs)
            {
                Technology tech = entry.Tech;
                // FB - Do not trade ship tech that the AI cannot use due to lack of pre-made designs
                if (!Them.isPlayer && !Them.WeCanUseThisTech(entry, theirDesigns))
                    continue;
                
                var text = LocalizedText.Parse($"{{{tech.NameIndex}}}: {(int)tech.ActualCost}");
                techs.AddSubItem(new ItemToOffer(text, "Tech") { SpecialInquiry = entry.UID });
            }

            ItemToOffer artifacts = AddHeader(GameText.Artifacts);
            foreach (Artifact artifact in Us.data.OwnedArtifacts)
            {
                artifacts.AddSubItem(new ItemToOffer(artifact.NameText, "Artifacts") { SpecialInquiry = artifact.Name });
            }

            ItemToOffer colonies = AddHeader(GameText.Colonies);
            foreach (Planet p in Us.GetPlanets())
            {
                colonies.AddSubItem(new ItemToOffer(p.Name, "Colony") { SpecialInquiry = p.Name });
            }
        }

        void AddItem(in LocalizedText text, string response)
        {
            List.AddItem(new ItemToOffer(text, response));
        }

        ItemToOffer AddHeader(in LocalizedText text)
        {
            return List.AddItem(new ItemToOffer(text, isHeader:true));;
        }

        void AddRelationItems(Relationship relations)
        {
            if (!relations.AtWar)
            {
                if (!relations.Treaty_NAPact) AddItem(GameText.NonaggressionPact, "NAPact");
                if (!relations.Treaty_Trade) AddItem(GameText.TradeTreaty, "TradeTreaty");
                if (!relations.Treaty_OpenBorders) AddItem(GameText.OpenBordersTreaty, "OpenBorders");

                if (relations.Treaty_Trade && relations.Treaty_NAPact && !relations.Treaty_Alliance)
                    AddItem(GameText.Alliance, "OfferAlliance");
            }
            else
            {
                AddItem(GameText.PeaceTreaty, "Peace Treaty");
            }
        }
    }
}