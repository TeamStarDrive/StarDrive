using System;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

namespace Ship_Game.GameScreens.DiplomacyScreen {

    class DiplomacyItemsLayout
    {
        readonly ScrollList2<ItemToOffer> List;
        ItemToOffer Offer;
        Vector2 Cursor;

        public DiplomacyItemsLayout(ScrollList2<ItemToOffer> list, Rectangle rect)
        {
            List = list;
            Offer = null;
            Cursor = new Vector2((rect.X + 10), (rect.Y + Fonts.Pirulen12.LineSpacing + 2));
        }

        void AddItem(int tokenId, string response)
        {
            Offer = List.AddItem(new ItemToOffer(tokenId, response, Cursor));
            Cursor.Y += (Fonts.Arial12Bold.LineSpacing + 5);
        }

        public void AddSubItem(string name, string response, string inquiry)
        {
            var item = new ItemToOffer(name, response, Cursor) { SpecialInquiry = inquiry };
            Cursor.Y += (Fonts.Arial12Bold.LineSpacing + 5);
            Offer.AddSubItem(item);
        }

        public void AddCategory(int categoryId, Action populateSubItems)
        {
            AddItem(categoryId, "");
            Cursor.X += 10f;
            populateSubItems();
            Cursor.X -= 10f;
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