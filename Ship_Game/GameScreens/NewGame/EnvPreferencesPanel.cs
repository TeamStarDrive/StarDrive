using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game.GameScreens.NewGame
{
    public class EnvPreferencesPanel : UIElementContainer
    {
        readonly RaceDesignScreen Screen;
        readonly UILabel Title;
        readonly UILabel BestType;
        UIPanel PlanetIcon;

        public PlanetCategory PreferredEnv = PlanetCategory.Terran;
        public float EnvTerran = 1;
        public float EnvOceanic = 1;
        public float EnvSteppe = 1;
        public float EnvTundra = 1;
        public float EnvSwamp = 1;
        public float EnvDesert = 1;
        public float EnvIce = 1;
        public float EnvBarren = 1;
        public float EnvVolcanic = 1;

        IEmpireData Data;

        public EnvPreferencesPanel(RaceDesignScreen parent, RacialTrait raceSummary, in Rectangle rect) : base(rect)
        {
            Screen = parent;
            Data = Screen.SelectedData;

            bool lo = parent.LowRes;
            Add(new Menu1(rect, withSub: false)).SetLocalPos(0,0);

            var font = lo ? Fonts.Arial8Bold : Fonts.Arial12Bold;
            Title = Add(new UILabel("Environment Preferences", font, Color.BurlyWood));
            Title.SetLocalPos(35, 15);
            Title.Tooltip = "Some races have modifiers to their Max Population and Fertility based on the planet type.";

            BestType = Add(new UILabel("Best Planet Type", font, Color.BurlyWood));
            BestType.SetLocalPos(35 + (lo ? 175 : 275), 15);
            BestType.Tooltip = "This is the best suited environment for this race, Terraforming a planet will transform it to this planet type.";
            

            UIList column1 = Add(new UIList(ListLayoutStyle.ResizeList));
            UIList column2 = Add(new UIList(ListLayoutStyle.ResizeList));
            column1.SetLocalPos(15, 35);
            column2.SetLocalPos(15 + (lo ? 60 : 140), 35);
            column1.Padding = column2.Padding = new Vector2(4, 4);

            UILabel AddEnvSplitter(UIList list, string title, Func<float> getValue)
            {
                var key = new UILabel(LocalizedText.Parse(title), font, Color.Wheat);
                var val = new UILabel(getValue().String(2), font);
                val.DynamicText = (l) => getValue().String(2);
                val.DynamicColor = (l) =>
                {
                    float value = getValue();
                    if (value > 1) return Color.Green;
                    if (value < 1) return Color.Red;
                    return Color.White;
                };
                list.AddSplit(key, val).Split = Screen.LowRes ? 50 : 80;
                return val;
            }

            AddEnvSplitter(column1, "{Terran}: ", () => EnvTerran);
            AddEnvSplitter(column1, "{Steppe}: ", () => EnvSteppe);
            AddEnvSplitter(column1, "{Oceanic}: ",() => EnvOceanic);
            AddEnvSplitter(column1, "{Swamp}: ",  () => EnvSwamp);
            AddEnvSplitter(column1, "{Volcanic}: ", () => EnvVolcanic);

            AddEnvSplitter(column2, "{Tundra}: ", () => EnvTundra);
            AddEnvSplitter(column2, "{Ice}: ",    () => EnvIce);
            AddEnvSplitter(column2, "{Desert}: ", () => EnvDesert);
            AddEnvSplitter(column2, "{Barren}: ", () => EnvBarren);
            UpdatePreferences(raceSummary);
        }

        public void UpdateArchetype(IEmpireData data, RacialTrait raceSummary)
        {
            Data = data;
            UpdatePlanetIcon();
            UpdatePreferences(raceSummary);
        }

        public void UpdatePreferences(RacialTrait raceSummary)
        {
            PreferredEnv = raceSummary.PreferredEnv;
            EnvTerran = raceSummary.EnvTerran;
            EnvOceanic = raceSummary.EnvOceanic;
            EnvSteppe = raceSummary.EnvSteppe;
            EnvTundra = raceSummary.EnvTundra;
            EnvSwamp = raceSummary.EnvSwamp;
            EnvDesert = raceSummary.EnvDesert;
            EnvIce = raceSummary.EnvIce;
            EnvBarren = raceSummary.EnvBarren;
            EnvVolcanic = raceSummary.EnvVolcanic;
            UpdatePlanetIcon();
            UpdateVisibility();
        }

        void UpdateVisibility()
        {
            bool wasVisibile = Visible;
            Visible = PreferredEnv != PlanetCategory.Terran
                || EnvTerran != 1
                || EnvOceanic != 1
                || EnvSteppe != 1
                || EnvTundra != 1
                || EnvSwamp != 1
                || EnvDesert != 1
                || EnvIce != 1
                || EnvBarren != 1
                || EnvVolcanic != 1;

            if (!wasVisibile && Visible)
                SlideInFromOffset(offset: new(-Width, 0), Screen.TransitionOffTime);
        }

        void UpdatePlanetIcon()
        {
            PlanetIcon?.RemoveFromParent(true);

            int size = Screen.LowRes ? 80 : 100;
            PlanetIcon = Add(new UIPanel(BestType.LocalPos.Add(0, 20), new Vector2(size),
                                         GetPlanetIcon())
            {
                Name = "EnvPref.PlanetIcon",
                Tooltip = Planet.TextCategory(PreferredEnv)
            });
        }

        SubTexture GetPlanetIcon()
        {
            string path;
            switch (PreferredEnv)
            {
                default:
                case PlanetCategory.Terran:  path = "Planets/25"; break;
                case PlanetCategory.Steppe:  path = "Planets/18"; break;
                case PlanetCategory.Oceanic: path = "Planets/21"; break;
                case PlanetCategory.Swamp:   path = "Planets/19"; break;
                case PlanetCategory.Tundra:  path = "Planets/11"; break;
                case PlanetCategory.Ice:     path = "Planets/17"; break;
                case PlanetCategory.Desert:  path = "Planets/14"; break;
                case PlanetCategory.Barren:  path = "Planets/16"; break;
            }

            return ResourceManager.Texture(path);
        }
    }
}
