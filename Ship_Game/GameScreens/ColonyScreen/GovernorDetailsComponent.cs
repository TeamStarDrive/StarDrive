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
        UICheckBox GovOrbitals, AutoTroops, GovNoScrap, Quarantine;
        private FloatSlider Garrison;
        Submenu Title;

        public GovernorDetailsComponent(GameScreen screen, Planet p, in Rectangle rect) : base(rect)
        {
            Screen = screen;
            SetPlanetDetails(p);
            Title = Add(new Submenu(rect));
            Title.AddTab("Governor"); // "Assign Labor"
        }

        public void SetPlanetDetails(Planet p)
        {
            Log.Assert(p != null, "GovernorDetailsComponent Planet cannot be null");
            if (Planet == p || p == null)
                return;

            Planet = p;
            RemoveAll(); // delete all components

            // NOTE: Using RootContent here to avoid lag from resource unloading and reloading
            PortraitSprite = DrawableSprite.SubTex(ResourceManager.RootContent, $"Portraits/{Planet.Owner.data.PortraitName}");

            Portrait  = Add(new UIPanel(PortraitSprite));
            WorldType = Add(new UILabel(Planet.WorldType, Fonts.Arial12Bold));
            WorldDescription = Add(new UILabel(Fonts.Arial12Bold));
            
            GovOrbitals = Add(new UICheckBox(() => Planet.GovOrbitals, Fonts.Arial12Bold, title:1960, tooltip:1961));
            AutoTroops  = Add(new UICheckBox(() => Planet.AutoBuildTroops, Fonts.Arial12Bold, title:1956, tooltip:1957));
            GovNoScrap  = Add(new UICheckBox(() => Planet.DontScrapBuildings, Fonts.Arial12Bold, title:1941, tooltip:1942));
            Quarantine =  Add(new UICheckBox(() => Planet.Quarantine, Fonts.Arial12Bold, title: 1888, tooltip: 1887));

            Garrison = Slider(200, 200, 200, 40, "Garrison Size", 0, 25,Planet.GarrisonSize);
            Garrison.Tip = 1903;
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

            Quarantine.Pos  = new Vector2(Portrait.X, Bottom - 40);
            AutoTroops.Pos  = new Vector2(Portrait.X, Bottom - 24);
            GovOrbitals.Pos = new Vector2(TopRight.X - 250, Bottom - 40);
            GovNoScrap.Pos  = new Vector2(TopRight.X - 250, Bottom - 24);
            Garrison.Pos    = new Vector2(TopRight.X - 200, Portrait.Y);
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

        public override void Update(float fixedDeltaTime)
        {
            if (Planet.Owner != null)
            {
                GovOrbitals.Visible  = Planet.Owner.isPlayer && Planet.colonyType != Planet.ColonyType.Colony;
                Garrison.Visible     = Planet.Owner.isPlayer;
                Quarantine.Visible   = Planet.Owner.isPlayer;
                Planet.GarrisonSize  = (int)Garrison.AbsoluteValue;
                Quarantine.TextColor = Planet.Quarantine ? Color.Red : Color.White;

                // not for trade hubs, which do not build structures anyway
                GovNoScrap.Visible    = GovOrbitals.Visible && Planet.colonyType != Planet.ColonyType.TradeHub;
            }

            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);

            // Governor portrait overlay stuff
            Portrait.Color = Planet.colonyType == Planet.ColonyType.Colony ? new Color(64,64,64) : Color.White;
            Color borderColor;
            switch (Planet.colonyType)
            {
                default:                             borderColor = Color.White;           break;
                case Planet.ColonyType.TradeHub:     borderColor = Color.Yellow;          break;
                case Planet.ColonyType.Colony:       borderColor = new Color(64, 64, 64); break;
                case Planet.ColonyType.Industrial:   borderColor = Color.Orange;          break;
                case Planet.ColonyType.Agricultural: borderColor = Color.Green;           break;
                case Planet.ColonyType.Research:     borderColor = Color.CornflowerBlue;  break;
                case Planet.ColonyType.Military:     borderColor = Color.Red;             break;
            }

            Portrait.Border = borderColor;
            batch.Draw(PortraitShine, Portrait.Rect);
        }
    }
}
