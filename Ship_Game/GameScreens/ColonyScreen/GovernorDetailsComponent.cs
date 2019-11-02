using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.SpriteSystem;

namespace Ship_Game
{
    class GovernorDetailsComponent : UIElementContainer
    {
        Planet Planet;
        SubTexture PortraitTex;
        readonly SubTexture PortraitShine = ResourceManager.Texture("Portraits/portrait_shine");
        readonly SubTexture PortraitRedX  = ResourceManager.Texture("NewUI/x_red");
        UIPanel Portrait;
        UILabel WorldType, WorldDescription;
        DropOptions<Planet.ColonyType> ColonyTypeList;
        UICheckBox GovOrbitals, GovMilitia, GovNoScrap;

        public GovernorDetailsComponent(Planet p, in Rectangle rect) : base(rect)
        {
            SetPlanetDetails(p);
        }

        public void SetPlanetDetails(Planet p)
        {
            Log.Assert(p != null, "GovernorDetailsComponent Planet cannot be null");
            if (Planet == p)
                return;

            Planet = p;
            RemoveAll(); // delete all components

            PortraitTex   = ResourceManager.Texture($"Portraits/{Planet.Owner.data.PortraitName}");

            Portrait = Add(new UIPanel(new DrawableSprite(PortraitTex)) {Border = Color.Orange});
            WorldType = Add(new UILabel(Planet.WorldType, Fonts.Arial12Bold));
            WorldDescription = Add(new UILabel(Fonts.Arial12Bold));
            
            GovOrbitals = Add(new UICheckBox(() => Planet.GovOrbitals, Fonts.Arial12Bold, title:1960, tooltip:1961));
            GovMilitia  = Add(new UICheckBox(() => Planet.GovMilitia,  Fonts.Arial12Bold, title:1956, tooltip:1957));
            GovNoScrap  = Add(new UICheckBox(() => Planet.DontScrapBuildings, Fonts.Arial12Bold, title:1941, tooltip:1942));

            // Dropdown will go on top of everything else
            ColonyTypeList = Add(new DropOptions<Planet.ColonyType>(100, 18));
            ColonyTypeList.AddOption(option:"--", Planet.ColonyType.Colony);
            ColonyTypeList.AddOption(option:4064, Planet.ColonyType.Core);
            ColonyTypeList.AddOption(option:4065, Planet.ColonyType.Industrial);
            ColonyTypeList.AddOption(option:4066, Planet.ColonyType.Agricultural);
            ColonyTypeList.AddOption(option:4067, Planet.ColonyType.Research);
            ColonyTypeList.AddOption(option:4068, Planet.ColonyType.Military);
            ColonyTypeList.AddOption(option:5087, Planet.ColonyType.TradeHub);
            ColonyTypeList.ActiveValue = Planet.colonyType;
            ColonyTypeList.OnValueChange = OnColonyTypeChanged;

            base.PerformLayout();
        }

        public override void PerformLayout()
        {
            Portrait.Pos = Pos + new Vector2(10, 30);
            Portrait.Size = new Vector2(124, 148);
            while (Portrait.Bottom > Bottom) // make it fit!
            {
                Portrait.Height -= (int)(0.1f * Portrait.Height);
                Portrait.Width  -= (int)(0.1f * Portrait.Width);
            }

            WorldType.Pos         = new Vector2(Portrait.Right + 10, Portrait.Y);
            ColonyTypeList.Pos    = new Vector2(WorldType.X, Portrait.Y + 16);
            WorldDescription.Pos  = new Vector2(WorldType.X, Portrait.Y + 40);
            WorldDescription.Text = GetParsedDescription();

            GovOrbitals.Pos = new Vector2(Portrait.X, Portrait.Bottom + 4);
            GovMilitia.Pos  = new Vector2(Portrait.X, Portrait.Bottom + 20);
            GovNoScrap.Pos  = new Vector2(Portrait.X + 240, Portrait.Bottom + 4);

            base.PerformLayout(); // update all the sub-elements, like checkbox rects
        }

        string GetParsedDescription()
        {
            float maxWidth = (Right - 10 - WorldType.X);
            return Fonts.Arial12Bold.ParseText(Planet.ColonyTypeInfoText.Text, maxWidth);
        }

        void OnColonyTypeChanged(Planet.ColonyType type)
        {
            Planet.colonyType = type;
            WorldType.Text = Planet.WorldType;
            WorldDescription.Text = GetParsedDescription();
        }

        public override void Update(float deltaTime)
        {
            GovOrbitals.Visible = Planet.Owner.isPlayer && Planet.colonyType != Planet.ColonyType.Colony;
            GovMilitia.Visible = GovOrbitals.Visible;

            // not for trade hubs, which do not build structures anyway
            GovNoScrap.Visible = GovOrbitals.Visible && Planet.colonyType != Planet.ColonyType.TradeHub;


            base.Update(deltaTime);
        }

        public override void Draw(SpriteBatch batch)
        {
            base.Draw(batch);

            // Governor portrait overlay stuff
            batch.Draw(PortraitShine, Portrait.Rect);
            if (Planet.colonyType == Planet.ColonyType.Colony)
                batch.Draw(PortraitRedX, Portrait.Rect);
        }
    }
}
