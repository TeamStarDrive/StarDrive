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
        UITextBox TextBox;
        SubTexture Image;

        public Map<Packagetypes, Array<DrawPackage>> DrawPackages = new Map<Packagetypes, Array<DrawPackage>>();

        public EventPopup(UniverseScreen s, Empire playerEmpire, ExplorationEvent e,
                          Outcome outcome, bool triggerNow, Planet p = null)
            : base(s, 600, 720)
        {
            if (triggerNow)
                e.TriggerOutcome(playerEmpire, outcome);

            Outcome = outcome;
            Planet  = p;
            ExpEvent = e;
            IsPopup  = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0f;

            foreach (Packagetypes packagetype in Enum.GetValues(typeof(Packagetypes)))
            {
                DrawPackages.Add(packagetype, new Array<DrawPackage>());
            }
        }
        
        public override void LoadContent()
        {
            if (Planet != null)
                TitleText = $"{Outcome.TitleText} at {Planet.Name}";
            else
                TitleText = $"{Outcome.TitleText} in Deep Space";

            base.LoadContent();

            if (Planet != null)
            {
                Empire.Universe.NotificationManager.AddAnomalyInvestigated(Planet, TitleText);
            }

            string image = Outcome.Image.NotEmpty() ? Outcome.Image : "Encounters/CrashedShip.png";
            Image = TransientContent.LoadSubTexture("Textures/" + image);
            Rectangle imgRect = new RectF(MidContainer.X, MidContainer.Bottom + 2,
                                          MidContainer.Width, MidContainer.Width/Image.AspectRatio);
            MidSepBot = new Rectangle(MidContainer.X, imgRect.Bottom, MidContainer.Width, 2);

            Panel(imgRect, Image);

            Close.Visible = false; // the X just confuses people, a big OK button is better

            string confirm = Outcome.ConfirmText.NotEmpty() ? Outcome.ConfirmText : "Great!";
            var btn = Button(ButtonStyle.DanButtonBrownWide, Vector2.Zero, confirm, OnDismissClicked);
            btn.SetPosToCenterOf(this).SetDistanceFromBottomOf(this, 24);
            
            var textArea = new Rectangle(TitleRect.X - 4, imgRect.Bottom + 10,  TitleRect.Width, 
                                         (int)(Bottom - imgRect.Bottom - 32));
            TextBox = Add(new UITextBox(new Submenu(textArea)));
            CreateTextBoxContent();
        }

        void CreateTextBoxContent()
        {
            TextBox.AddLines(Outcome.DescriptionText, Fonts.Verdana10, Color.White);

            if (Outcome.SelectRandomPlanet && Outcome.GetPlanet() != null)
            {
                TextBox.AddLine($"Relevant Planet: {Outcome.GetPlanet().Name}", Fonts.Arial12Bold, Color.LightGreen);
            }

            if (Outcome.UnlockTech != null)
            {
                AddUnlockedTechToTextBox(TextBox, Outcome.UnlockTech);
            }

            if (Outcome.MoneyGranted > 0)
            {
                TextBox.AddLine($"Money Granted: {Outcome.MoneyGranted}", Fonts.Arial12Bold, Color.Green);
            }

            if (Outcome.ScienceBonus > 0f)
            {
                int scienceBonus = (int)(Outcome.ScienceBonus * 100f);
                TextBox.AddLine($"Research Bonus Granted: {scienceBonus}%", Fonts.Arial12Bold, Color.Blue);
            }
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
            
            //var textPos = new Vector2(TextArea.X + 10, ImageRect.Bottom + 10);

            //string description = Fonts.Verdana10.ParseText(Outcome.DescriptionText, TextArea.Width - 40);
            //DrawString(batch, Fonts.Verdana10, description, ref textPos, Color.White);

            //if (Outcome.SelectRandomPlanet && Outcome.GetPlanet() != null)
            //{
            //    DrawString(batch, Fonts.Arial12Bold, "Relevant Planet: "+Outcome.GetPlanet().Name, ref textPos, Color.LightGreen);
            //}

            //Artifact art = Outcome.GetArtifact();
            //if (art != null)
            //{
            //    DrawString(batch, Fonts.Arial12Bold, $"Artifact Granted: {art.Name}", ref textPos, Color.LightGreen);
                
            //    var iconRect = new Rectangle((int)textPos.X, (int)textPos.Y, 64, 64);
            //    SubTexture artTex = TransientContent.LoadSubTexture("Textures/Artifact Icons/"+art.Name);
            //    batch.Draw(artTex, iconRect, Color.White);
            //    textPos.Y += iconRect.Height;
                
            //    string artDescr = Fonts.Arial12.ParseText(art.Description, TextArea.Width - 40);
            //    batch.DrawString(Fonts.Arial12, artDescr, textPos, Color.White);
            //    textPos.Y += Fonts.Arial12.MeasureString(artDescr).Y;

            //    foreach (DrawPackage artifactDrawPackage in DrawPackages[Packagetypes.Artifact])
            //    {
            //        textPos.Y += artifactDrawPackage.Font.LineSpacing;
            //        batch.DrawString(artifactDrawPackage.Font, artifactDrawPackage.Text, textPos, artifactDrawPackage.Color);
            //    }
            //}

            //if (Outcome.UnlockTech != null)
            //{
            //    DrawUnlockedTech(batch, ref textPos);
            //}

            //if (Outcome.MoneyGranted > 0)
            //{
            //    DrawString(batch, Fonts.Arial12Bold, $"Money Granted: {Outcome.MoneyGranted}", ref textPos, Color.White);
            //}

            //if (Outcome.ScienceBonus > 0f)
            //{
            //    int scienceBonus = (int)(Outcome.ScienceBonus * 100f);
            //    DrawString(batch, Fonts.Arial12Bold, $"Research Bonus Granted: {scienceBonus}%", ref textPos, Color.White);			
            //}

            batch.End();
        }

        class TechItem : UIElementV2
        {
            readonly Technology Tech;
            public TechItem(Technology tech, float width)
            {
                Tech = tech;
                Width = width;
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                ShipModule mod = ResourceManager.GetModuleTemplate(Tech.ModulesUnlocked[0].ModuleUID);
                var iconRect = new Rectangle((int)X, (int)Y, 16 * mod.XSIZE, 16 * mod.YSIZE);
                iconRect.X += 48 - iconRect.Width / 2;
                iconRect.Y += 48 - iconRect.Height / 2;
                while (iconRect.Height > 96)
                {
                    iconRect.Height -= mod.YSIZE;
                    iconRect.Width  -= mod.XSIZE;
                    iconRect.X += 48 - iconRect.Width / 2;
                    iconRect.Y += 48 - iconRect.Height / 2;
                }

                batch.Draw(ResourceManager.Texture(mod.IconTexturePath), iconRect, Color.White);

                var pos = new Vector2(X + 100f, Y);
                batch.DrawString(Fonts.Arial20Bold, mod.NameText, pos, Color.Orange);
                pos.Y += Fonts.Arial20Bold.LineSpacing;

                string desc = Fonts.Arial12Bold.ParseText(mod.DescriptionText, Width - 120);
                batch.DrawString(Fonts.Arial12Bold, desc, pos, Color.White);
            }
            public override bool HandleInput(InputState input)
            {
                return false;
            }
        }

        void AddUnlockedTechToTextBox(UITextBox textBox, string unlockTech)
        {
            if (Outcome.WeHadIt)
            {
                textBox.AddLine("We found some alien technology, but we already possessed this knowledge.",
                                Fonts.Arial12Bold, Color.LightYellow);
                return;
            }

            if (!ResourceManager.TryGetTech(unlockTech, out Technology tech))
            {
                textBox.AddLine($"Missing Technology: {tech.UID}", Fonts.Arial12Bold, Color.Red);
                return;
            }

            textBox.AddLine($"Technology Acquired: {Localizer.Token(tech.NameIndex)}");

            if (tech.ModulesUnlocked.Count > 0)
            {
                textBox.AddElement(new TechItem(tech, textBox.Width));
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