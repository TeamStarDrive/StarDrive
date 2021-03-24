using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class EventPopup : PopupWindow
    {
        public ExplorationEvent ExpEvent;
        readonly Outcome Outcome;
        readonly Planet Planet;
        Rectangle TextArea;
        SubTexture Image;
        Rectangle ImageRect;

        public Map<Packagetypes, Array<DrawPackage>> DrawPackages = new Map<Packagetypes, Array<DrawPackage>>();

        public EventPopup(UniverseScreen s, Empire playerEmpire, ExplorationEvent e, 
            Outcome outcome, bool triggerNow, Planet p = null) : base(s, 600, 720)
        {
            if (triggerNow)
                e.TriggerOutcome(playerEmpire, outcome);

            Outcome           = outcome;
            Planet            = p;
            ExpEvent          = e;
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0f;

            foreach (Packagetypes packagetype in Enum.GetValues(typeof(Packagetypes)))
            {
                DrawPackages.Add(packagetype,new Array<DrawPackage>());
            }
        }
        
        public override void LoadContent()
        {
            if (Planet != null)
                TitleText = $"{Outcome.TitleText} at {Planet.Name}";
            else
                TitleText = $"{Outcome.TitleText} in Deep Space";

            base.LoadContent();
            TextArea = new Rectangle(TitleRect.X - 4, TitleRect.Bottom + MidContainer.Height + 10, 
                                     TitleRect.Width, 600 - (TitleRect.Height + MidContainer.Height));

            if (Planet != null)
            {
                Empire.Universe.NotificationManager.AddAnomalyInvestigated(Planet, TitleText);
            }

            string image = Outcome.Image.NotEmpty() ? Outcome.Image : "Encounters/CrashedShip.png";
            Image = TransientContent.LoadSubTexture("Textures/" + image);
            ImageRect = new RectF(MidContainer.X, MidContainer.Bottom + 2,
                                  MidContainer.Width, MidContainer.Width/Image.AspectRatio);
            MidSepBot = new Rectangle(MidContainer.X, ImageRect.Bottom, MidContainer.Width, 2);

            Panel(ImageRect, Image);

            string confirm = Outcome."Great.";
            Button(ButtonStyle.DanButton, new Vector2(Right-200, Bottom - 50), "Beam them up, Scotty!", OnDismissClicked);
        }

        void OnDismissClicked(UIButton btn)
        {
            ExitScreen();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);

            base.Draw(batch, elapsed);

            batch.Begin();
            
            var textPos = new Vector2(TextArea.X + 10, ImageRect.Bottom + 10);

            string description = Fonts.Verdana10.ParseText(Outcome.DescriptionText, TextArea.Width - 40);
            DrawString(batch, Fonts.Verdana10, description, ref textPos, Color.White);

            if (Outcome.SelectRandomPlanet && Outcome.GetPlanet() != null)
            {
                DrawString(batch, Fonts.Arial12Bold, "Relevant Planet: "+Outcome.GetPlanet().Name, ref textPos, Color.LightGreen);				
            }

            Artifact art = Outcome.GetArtifact();
            if (art != null)
            {
                DrawString(batch, Fonts.Arial12Bold, $"Artifact Granted: {art.Name}", ref textPos, Color.LightGreen);
                
                var iconRect = new Rectangle((int)textPos.X, (int)textPos.Y, 64, 64);
                SubTexture artTex = TransientContent.LoadSubTexture("Textures/Artifact Icons/"+art.Name);
                batch.Draw(artTex, iconRect, Color.White);
                textPos.Y += iconRect.Height;
                
                string artDescr = Fonts.Arial12.ParseText(art.Description, TextArea.Width - 40);
                batch.DrawString(Fonts.Arial12, artDescr, textPos, Color.White);
                textPos.Y += Fonts.Arial12.MeasureString(artDescr).Y;

                foreach (DrawPackage artifactDrawPackage in DrawPackages[Packagetypes.Artifact])
                {
                    textPos.Y += artifactDrawPackage.Font.LineSpacing;
                    batch.DrawString(artifactDrawPackage.Font, artifactDrawPackage.Text, textPos, artifactDrawPackage.Color);
                }
            }

            if (Outcome.UnlockTech != null)
            {
                DrawUnlockedTech(batch, ref textPos);
            }

            if (Outcome.MoneyGranted > 0)
            {
                DrawString(batch, Fonts.Arial12Bold, $"Money Granted: {Outcome.MoneyGranted}", ref textPos, Color.White);
            }

            if (Outcome.ScienceBonus > 0f)
            {
                int scienceBonus = (int)(Outcome.ScienceBonus * 100f);
                DrawString(batch, Fonts.Arial12Bold, $"Research Bonus Granted: {scienceBonus}%", ref textPos, Color.White);			
            }

            batch.End();
        }

        void DrawString(SpriteBatch batch, SpriteFont font, string text, ref Vector2 textPos, Color color)
        {
            textPos.Y += font.LineSpacing;
            batch.DrawString(font, text, textPos, color);
            textPos.Y += font.MeasureString(text).Y;
            textPos.Y += font.LineSpacing;
        }

        void DrawUnlockedTech(SpriteBatch batch, ref Vector2 textPos)
        {
            if (Outcome.WeHadIt)
            {
                string alreadyPosess = "We found some alien technology, but we already possessed this knowledge.";
                DrawString(batch, Fonts.Arial12Bold, alreadyPosess, ref textPos, Color.White);
                return;
            }

            if (!ResourceManager.TryGetTech(Outcome.UnlockTech, out Technology tech))
            {
                DrawString(batch, Fonts.Arial12Bold, $"Missing Technology: {tech.UID}", ref textPos, Color.Red);
                return;
            }

            string text = "Technology Acquired: " + Localizer.Token(tech.NameIndex);
            DrawString(batch, Fonts.Arial12Bold, text, ref textPos, Color.White);

            if (tech.ModulesUnlocked.Count > 0)
            {
                ShipModule unlockedMod = ResourceManager.GetModuleTemplate(tech.ModulesUnlocked[0].ModuleUID);
                Rectangle IconRect = new Rectangle((int)textPos.X, (int)textPos.Y, 
                                                   16 * unlockedMod.XSIZE, 16 * unlockedMod.YSIZE);

                IconRect.X = IconRect.X + 48 - IconRect.Width / 2;
                IconRect.Y = IconRect.Y + 48 - IconRect.Height / 2;

                while (IconRect.Height > 96)
                {
                    IconRect.Height = IconRect.Height - unlockedMod.YSIZE;
                    IconRect.Width = IconRect.Width - unlockedMod.XSIZE;
                    IconRect.X = IconRect.X + 48 - IconRect.Width / 2;
                    IconRect.Y = IconRect.Y + 48 - IconRect.Height / 2;
                }

                batch.Draw(ResourceManager.Texture(ResourceManager.GetModuleTemplate(unlockedMod.UID).IconTexturePath), IconRect, Color.White);
                string moduleName = Localizer.Token(unlockedMod.NameIndex);

                var moduleNamePos = new Vector2(textPos.X + 100f, textPos.Y);
                DrawString(batch, Fonts.Arial20Bold, moduleName, ref moduleNamePos, Color.Orange);

                string desc = Fonts.Arial12Bold.ParseText(Localizer.Token(unlockedMod.DescriptionIndex), TextArea.Width - 120);
                var moduleDescrPos = new Vector2(textPos.X + 100f, textPos.Y + 22f);
                DrawString(batch, Fonts.Arial12Bold, desc, ref moduleDescrPos, Color.White);
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (input.RightMouseClick)
                return false; // Don't let right click exit screen and make players miss the event.

            return base.HandleInput(input);
        }

        public enum Packagetypes
        {
            Artifact,
            Technology,
            Planet
        }

        public class DrawPackage
        {
            public string Text;
            public SpriteFont Font;
            public int Value;
            public Texture2D Icon;
            public Color Color;

            public DrawPackage()
            {
            }

            public DrawPackage(string text, SpriteFont font, int value,
                Color color)
            {
                Text = text;
                Font = font;
                Value = value;
                Color = color;
            }

            public DrawPackage(string text, SpriteFont font, float value,
                Color color, string postFix)
            {
                Value = postFix == "%" ? (int)(value * 100f) : (int)value;
                Text  = text + Value + postFix;
                Font  = font;
                Color = color;
            }
        }
    }
}