using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.SpriteSystem;

namespace Ship_Game
{
    class GovernorDetailsComponent : UIElementContainer
    {
        readonly GameScreen Screen;
        Planet Planet;
        DrawableSprite PortraitSprite;
        readonly SubTexture PortraitShine = ResourceManager.Texture("Portraits/portrait_shine");
        readonly SubTexture PortraitRedX  = ResourceManager.Texture("NewUI/x_red");
        UIPanel Portrait;
        UILabel WorldType, WorldDescription;
        DropOptions<Planet.ColonyType> ColonyTypeList;
        UICheckBox GovOrbitals, GovMilitia, GovNoScrap;
        readonly bool UseVideo;

        public GovernorDetailsComponent(GameScreen screen, 
            Planet p, in Rectangle rect, bool governorVideo) : base(rect)
        {
            Screen = screen;
            // Memory usage is too intensive
            UseVideo = false; // governorVideo;
            SetPlanetDetails(p);
        }

        public void SetPlanetDetails(Planet p)
        {
            Log.Assert(p != null, "GovernorDetailsComponent Planet cannot be null");
            if (Planet == p || p == null)
                return;

            Planet = p;
            RemoveAll(); // delete all components

            // NOTE: Using RootContent here to avoid lag from resource unloading and reloading
            PortraitSprite = UseVideo && p.Owner.data.Traits.VideoPath.NotEmpty()
                ? DrawableSprite.Video(ResourceManager.RootContent, p.Owner.data.Traits.VideoPath, looping:true)
                : DrawableSprite.SubTex(ResourceManager.RootContent, $"Portraits/{Planet.Owner.data.PortraitName}");

            Portrait  = Add(new UIPanel(PortraitSprite) {Border = Color.Orange});
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
            float aspect = PortraitSprite.Size.X / PortraitSprite.Size.Y;
            float height = (float)Math.Round(Height * 0.6f);
            Portrait.Size = new Vector2((float)Math.Round(aspect*height), height);
            Portrait.Pos = new Vector2(X + 10, Y + 30);

            WorldType.Pos         = new Vector2(Portrait.Right + 10, Portrait.Y);
            ColonyTypeList.Pos    = new Vector2(WorldType.X, Portrait.Y + 16);
            WorldDescription.Pos  = new Vector2(WorldType.X, Portrait.Y + 40);
            WorldDescription.Text = GetParsedDescription();

            GovOrbitals.Pos = new Vector2(Portrait.X, Bottom - 40);
            GovMilitia.Pos  = new Vector2(Portrait.X, Bottom - 24);
            GovNoScrap.Pos  = new Vector2(Portrait.X + 240, Bottom - 40);

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
