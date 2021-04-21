using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
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
        readonly Array<ArtifactEffect> ArtifactEffects = new Array<ArtifactEffect>();

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
        }
        
        public void AddArtifactEffect(ArtifactEffect effect)
        {
            ArtifactEffects.Add(effect);
        }
        
        public override void LoadContent()
        {
            if (Planet != null)
                TitleText = $"{Outcome.LocalizedTitle} at {Planet.Name}";
            else
                TitleText = $"{Outcome.LocalizedTitle} in Deep Space";

            base.LoadContent();

            if (Planet != null)
            {
                Empire.Universe.NotificationManager.AddAnomalyInvestigated(Planet, TitleText);
            }

            const string defaultImage = "Encounters/CrashedShip.png";
            string image = Outcome.Image.NotEmpty() ? Outcome.Image : defaultImage;
            Image = TransientContent.LoadSubTexture("Textures/" + image);
            if (Image == null)
            {
                Log.Error($"Failed to load image: {Outcome.Image}, using default");
                Image = TransientContent.LoadTextureOrDefault(defaultImage);
            }

            Rectangle imgRect = new RectF(MidContainer.X, MidContainer.Bottom + 2,
                                          MidContainer.Width, MidContainer.Width/Image.AspectRatio);
            MidSepBot = new Rectangle(MidContainer.X, imgRect.Bottom, MidContainer.Width, 2);
            Panel(imgRect, Image);

            Close.Visible = false; // the X just confuses people, a big OK button is better

            string confirm = Outcome.ConfirmText.NotEmpty() ? Outcome.ConfirmText : "Great!";
            var btn = Button(ButtonStyle.EventConfirm, Vector2.Zero, confirm, OnDismissClicked);
            btn.SetPosToCenterOf(this).SetDistanceFromBottomOf(this, 24);
            
            float textBoxBottom = btn.Y - 2;
            Rectangle textArea = new RectF(X + 8, imgRect.Bottom - 16, Width - 24, textBoxBottom - imgRect.Bottom);
            TextBox = Add(new UITextBox(new Submenu(textArea)));
            CreateTextBoxContent(TextBox);
        }

        void CreateTextBoxContent(UITextBox textBox)
        {
            textBox.AddLines(Outcome.LocalizedDescr, Fonts.Verdana10, Color.White);

            if (Outcome.SelectRandomPlanet && Outcome.GetPlanet() != null)
            {
                textBox.AddLine($"Relevant Planet: {Outcome.GetPlanet().Name}", Fonts.Arial12Bold, Color.LightGreen);
            }

            if (Outcome.GetArtifact() != null)
            {
                textBox.AddElement(new ArtifactItem(TransientContent, Outcome.GetArtifact(), ArtifactEffects, textBox.ItemsRect.Width));
            }

            if (Outcome.UnlockTech != null)
            {
                AddUnlockedTechToTextBox(textBox, Outcome.UnlockTech);
            }

            if (Outcome.MoneyGranted > 0)
            {
                textBox.AddLine($"Money Granted: {Outcome.MoneyGranted}", Fonts.Arial12Bold, Color.Green);
            }

            if (Outcome.ScienceBonus > 0f)
            {
                int scienceBonus = (int)(Outcome.ScienceBonus * 100f);
                textBox.AddLine($"Research Bonus Granted: {scienceBonus}%", Fonts.Arial12Bold, Color.Blue);
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

            batch.End();
        }

        class TechItem : UIElementV2
        {
            readonly GameContentManager Content;
            readonly Technology Tech;
            readonly ShipModule Mod;
            readonly SubTexture IconTex;
            public TechItem(GameContentManager content, Technology tech, float width)
            {
                Content = content;
                Tech = tech;
                Width = width;
                Mod = ResourceManager.GetModuleTemplate(Tech.ModulesUnlocked[0].ModuleUID);
                IconTex = Content.LoadSubTexture("Textures/" + Mod.IconTexturePath);
            }
            public override void Draw(SpriteBatch batch, DrawTimes elapsed)
            {
                var iconRect = new Rectangle((int)X, (int)Y, 16 * Mod.XSIZE, 16 * Mod.YSIZE);
                iconRect.X += 48 - iconRect.Width / 2;
                iconRect.Y += 48 - iconRect.Height / 2;
                while (iconRect.Height > 96)
                {
                    iconRect.Height -= Mod.YSIZE;
                    iconRect.Width  -= Mod.XSIZE;
                    iconRect.X += 48 - iconRect.Width / 2;
                    iconRect.Y += 48 - iconRect.Height / 2;
                }

                batch.Draw(IconTex, iconRect, Color.White);

                var pos = new Vector2(X + 100f, Y);
                batch.DrawString(Fonts.Arial20Bold, Mod.NameText, pos, Color.Orange);
                pos.Y += Fonts.Arial20Bold.LineSpacing;

                string desc = Fonts.Arial12Bold.ParseText(Mod.DescriptionText, Width - 120);
                batch.DrawString(Fonts.Arial12Bold, desc, pos, Color.White);
            }
            public override bool HandleInput(InputState input)
            {
                return false;
            }
        }

        class ArtifactItem : UIElementContainer
        {
            public ArtifactItem(GameContentManager content, Artifact art, 
                                Array<ArtifactEffect> effects, float width)
            {
                SubTexture artTex = content.LoadSubTexture("Textures/Artifact Icons/" + art.Name);

                float y = 0;
                LabelRel($"Artifact Granted: {art.Name}", Fonts.Arial12Bold, Color.LightGreen, 0, y);
                y += Fonts.Arial12Bold.LineSpacing;
                PanelRel(new RectF(0, y, 64, 64), artTex);
                y += 64;
                foreach (string line in Fonts.Arial12.ParseTextToLines(art.Description, width))
                {
                    LabelRel(line, Fonts.Arial12, 0, y);
                    y += Fonts.Arial12.LineSpacing;
                }
                foreach (ArtifactEffect effect in effects)
                {
                    LabelRel(effect.Text, Fonts.Arial12Bold, Color.Orange, 0, y);
                    y += Fonts.Arial12Bold.LineSpacing;
                }
            }
        }

        void AddUnlockedTechToTextBox(UITextBox textBox, string unlockTech)
        {
            if (!ResourceManager.TryGetTech(unlockTech, out Technology tech))
            {
                textBox.AddLine($"Missing Technology: {unlockTech}", Fonts.Arial12Bold, Color.Red);
                return;
            }

            if (Outcome.WeHadIt)
            {
                textBox.AddLine($"We found some {tech.Name.Text}, but we already possessed this knowledge.",
                                Fonts.Arial12Bold, Color.LightYellow);
                return;
            }

            textBox.AddLine($"New Technology Acquired: {tech.Name.Text}", Fonts.Arial12Bold, Color.AliceBlue);

            if (tech.ModulesUnlocked.Count > 0)
            {
                textBox.AddElement(new TechItem(TransientContent, tech, textBox.Width));
            }
        }

        public override bool HandleInput(InputState input)
        {
            if (input.RightMouseClick)
                return false; // Don't let right click exit screen and make players miss the event.

            return base.HandleInput(input);
        }

        public struct ArtifactEffect
        {
            public string Text;
            public int Value;
            public ArtifactEffect(string text, float value, bool percent)
            {
                Value = percent ? (int)(value * 100f) : (int)value;
                Text  = text + Value + (percent ? "%" : "");
            }
        }
    }
}