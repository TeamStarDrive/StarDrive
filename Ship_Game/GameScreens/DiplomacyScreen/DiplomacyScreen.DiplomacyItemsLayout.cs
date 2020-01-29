using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.GameScreens.DiplomacyScreen {

    class DiplomacyItemsLayout
    {
        readonly ScrollList2<ItemToOffer> List;
        ItemToOffer Offer;

        public DiplomacyItemsLayout(ScrollList2<ItemToOffer> list, Rectangle rect)
        {
            List = list;
            Offer = null;
        }

        void AddItem(in LocalizedText text, string response)
        {
            Offer = List.AddItem(new ItemToOffer(text, response));
        }

        public void AddSubItem(in LocalizedText text, string response, string inquiry)
        {
            var item = new ItemToOffer(text.Text, response) { SpecialInquiry = inquiry };
            Offer.AddSubItem(item);
        }

        public void AddCategory(GameText text, Action populateSubItems)
        {
            Offer = List.AddItem(new ItemToOffer(text, true));
            populateSubItems();
        }

        public void AddRelationItems(Relationship relations)
        {
            if (!relations.AtWar)
            {
                if (!relations.Treaty_NAPact) AddItem(1214, "NAPact");
                if (!relations.Treaty_Trade) AddItem(1215, "TradeTreaty");
                if (!relations.Treaty_OpenBorders) AddItem(1216, "OpenBorders");

                if (relations.Treaty_Trade && relations.Treaty_NAPact && !relations.Treaty_Alliance)
                    AddItem(2045, "OfferAlliance");
            }
            else
            {
                AddItem(1213, "Peace Treaty");
            }
        }
    }
}