using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.IO;
using System.Xml.Serialization;
using Ship_Game.AI;

namespace Ship_Game
{
    public sealed class ShipToolScreen : GameScreen
    {
        private Matrix worldMatrix = Matrix.Identity;
        private Matrix view;
        private Matrix projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, 1f, 1f, 10000f);
        private SceneObject shipSO;

        private InputState designInputState;

        private Texture2D DottedLine;

        private Restrictions DesignState;

        private Rectangle SelectionBox = new Rectangle(-1, -1, 0, 0);

        private Texture2D moduleSlot;

        private int slotsize = 16;

        private DanButton LoadModelButton;

        private DanButton SaveHullButton;

        private string DescriptionOfState = "";

        private PrimitiveQuad border;

        private Vector2 aspect;

        private Vector2 mousePos = new Vector2(0f, 0f);

        private MouseState mouseStateCurrent;

        private MouseState mouseStatePrevious;

        public string TestTexture = "Textures/Modules/Armor";

        private Array<Ship> StartingShipList = new Array<Ship>();

        private bool ShowOverlay = true;

        private Vector3 cameraPosition = new Vector3(0f, 0f, 1300f);

        private UITextEntry ShipNameBox;

        private Array<ToggleButton> DesignStateButtons = new Array<ToggleButton>();

        private Vector2 Center;

        private string ModelPath;

        private Thruster thruster;

        private bool applyThruster;

        public Array<ShipModule> ModuleList = new Array<ShipModule>();

        private Array<SlotStruct> SlotList = new Array<SlotStruct>();

        private Rectangle what;

        private Vector2 tPos = Vector2.Zero;

        private float tscale = 30f;

        private float heat = 1f;

        private Array<ThrusterZone> TList = new Array<ThrusterZone>();

        private string HullName = "Hull Name";

        private int selectedShip = 0;

        private Rectangle RbBox = new Rectangle();

        private ShipModule ActiveModule;

        public ShipToolScreen(GameScreen parent) : base(parent)
        {
            base.TransitionOnTime = TimeSpan.FromSeconds(0);
            base.TransitionOffTime = TimeSpan.FromSeconds(0);
            this.designInputState = new InputState();
        }

        private void ConfigureSlots()
        {
            this.border = new PrimitiveQuad(this.aspect.X / 2f - 256f, this.aspect.Y / 2f - 256f, 512f, 512f);
            for (int x = -32; x < 32; x++)
            {
                for (int y = -32; y < 32; y++)
                {
                    PrimitiveQuad newCell = new PrimitiveQuad((float)((int)this.Center.X + 16 * x), (float)((int)this.Center.Y + 16 * y), (float)this.slotsize, (float)this.slotsize);
                    SlotStruct newslot = new SlotStruct()
                    {
                        PQ = newCell,
                        Restrictions = Restrictions.I
                    };
                    this.SlotList.Add(newslot);
                }
            }
        }

        protected override void Destroy()
        {
            border?.Dispose(ref border);
            base.Destroy();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            GameTime gameTime = Game1.Instance.GameTime;
            ScreenManager.BeginFrameRendering(gameTime, ref view, ref projection);

            base.ScreenManager.GraphicsDevice.Clear(Color.Black);
            if (this.applyThruster)
            {
                this.thruster.Draw(ref this.view, ref this.projection);
            }
            ScreenManager.RenderSceneObjects();
            Rectangle rectangle = new Rectangle(this.border.X, this.border.Y, 512, 512);
            spriteBatch.Begin();
            Vector2 TitlePos = new Vector2(20f, 20f);
            HelperFunctions.DrawDropShadowText(base.ScreenManager, "Ship Mod Tools", TitlePos, Fonts.Arial20Bold);
            TitlePos.Y = TitlePos.Y + (float)(Fonts.Arial20Bold.LineSpacing + 3);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Use this tool to create module patterns for your ships", TitlePos, Color.White);
            if (this.shipSO != null)
            {
                TitlePos = new Vector2((float)this.what.X, 20f);
                SpriteFont arial12Bold = Fonts.Arial12Bold;
                float radius = this.shipSO.WorldBoundingSphere.Radius;
                spriteBatch.DrawString(arial12Bold, string.Concat("Radius: ", radius.ToString()), TitlePos, Color.White);
                TitlePos.Y = TitlePos.Y + 20f;
                string text = "If you can't see your model then your radius is likely too big or too small. A radius of 512 will fit snugly inside the box. Change the scale when you compile the model. If it is rotated oddly change the X, Y, and Z axis. If the model is off-center then you will need to re-export the 3D model from Blender, making sure to Set Origin to the desired pivot point of your model";
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, HelperFunctions.ParseText(Fonts.Arial12, text, 600f), TitlePos, Color.White);
            }
            Vector2 WhichSelectionPos = new Vector2((float)this.what.X, (float)(this.what.Y - Fonts.Arial20Bold.LineSpacing));
            spriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(this.DesignState, " - ", this.GetDesignStateText()), WhichSelectionPos, Color.Orange);
            WhichSelectionPos.X = WhichSelectionPos.X + 150f;
            WhichSelectionPos.Y = WhichSelectionPos.Y + (float)Fonts.Arial20Bold.LineSpacing;
            WhichSelectionPos.Y = WhichSelectionPos.Y - Fonts.Arial12Bold.MeasureString(HelperFunctions.ParseText(Fonts.Arial12Bold, this.DescriptionOfState, 512f)).Y;
            base.ScreenManager.SpriteBatch.DrawRectangle(this.what, Color.White);
            foreach (SlotStruct slot in this.SlotList)
            {
                if (!this.applyThruster && slot.PQ.isFilled)
                {
                    spriteBatch.Draw(this.moduleSlot, slot.PQ.enclosingRect, Color.White);
                    spriteBatch.DrawString(Fonts.Arial20Bold, string.Concat(" ", slot.Restrictions), new Vector2((float)slot.PQ.enclosingRect.X, (float)slot.PQ.enclosingRect.Y), Color.Navy, 0f, Vector2.Zero, 0.4f, SpriteEffects.None, 1f);
                }
                if (this.applyThruster || slot.ModuleUID == null)
                {
                    continue;
                }
                if (slot.Module.XSIZE > 1 || slot.Module.YSIZE > 1)
                {
                    spriteBatch.Draw(slot.Tex, new Rectangle(slot.PQ.enclosingRect.X, slot.PQ.enclosingRect.Y, 16 * slot.Module.XSIZE, 16 * slot.Module.YSIZE), Color.White);
                }
                else
                {
                    spriteBatch.Draw(slot.Tex, slot.PQ.enclosingRect, Color.White);
                }
            }
            this.DrawHorizontalLine(spriteBatch, this.SelectionBox.Y);
            this.DrawHorizontalLine(spriteBatch, this.SelectionBox.Y + this.SelectionBox.Height);
            this.DrawVerticalLine(spriteBatch, this.SelectionBox.X);
            this.DrawVerticalLine(spriteBatch, this.SelectionBox.X + this.SelectionBox.Width);
            foreach (ToggleButton button in this.DesignStateButtons)
            {
                button.Draw(base.ScreenManager);
            }
            if (this.ActiveModule != null)
            {
                spriteBatch.Draw(Ship_Game.ResourceManager.TextureDict[ResourceManager.GetModuleTemplate(ActiveModule.UID).IconTexturePath], new Rectangle(this.mouseStateCurrent.X, this.mouseStateCurrent.Y, 16 * this.ActiveModule.XSIZE, 16 * this.ActiveModule.YSIZE), Color.White);
                for (int i = 0; i < this.ActiveModule.XSIZE; i++)
                {
                    for (int j = 0; j < this.ActiveModule.YSIZE; j++)
                    {
                        PrimitiveQuad pq = new PrimitiveQuad(new Rectangle(this.mouseStateCurrent.X + i * 16, this.mouseStateCurrent.Y + j * 16, 16, 16));
                        pq.Draw(spriteBatch, Color.White);
                    }
                }
            }
            Vector2 InfoPos = new Vector2((float)(this.SaveHullButton.r.X - 50), (float)(this.SaveHullButton.r.Y - 20));
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Hulls are saved to StarDrive/Ship Tools", InfoPos, Color.White);
            this.ShipNameBox.Draw(Fonts.Arial20Bold, base.ScreenManager.SpriteBatch, new Vector2((float)this.ShipNameBox.ClickableArea.X, (float)this.ShipNameBox.ClickableArea.Y), gameTime, Color.Orange);
            this.SaveHullButton.Draw(base.ScreenManager);
            this.LoadModelButton.Draw(base.ScreenManager);
            spriteBatch.End();
            ScreenManager.EndFrameRendering();
        }

        private void DrawHorizontalLine(SpriteBatch spriteBatch, int thePositionY)
        {
            if (this.SelectionBox.Width > 0)
            {
                for (int aCounter = 0; aCounter <= this.SelectionBox.Width - 10; aCounter = aCounter + 10)
                {
                    if (this.SelectionBox.Width - aCounter >= 0)
                    {
                        spriteBatch.Draw(this.DottedLine, new Rectangle(this.SelectionBox.X + aCounter, thePositionY, 10, 5), Color.White);
                    }
                }
                return;
            }
            if (this.SelectionBox.Width < 0)
            {
                for (int aCounter = -10; aCounter >= this.SelectionBox.Width; aCounter = aCounter - 10)
                {
                    if (this.SelectionBox.Width - aCounter <= 0)
                    {
                        spriteBatch.Draw(this.DottedLine, new Rectangle(this.SelectionBox.X + aCounter, thePositionY, 10, 5), Color.White);
                    }
                }
            }
        }

        private void DrawVerticalLine(SpriteBatch spriteBatch, int thePositionX)
        {
            if (this.SelectionBox.Height <= 0)
            {
                if (this.SelectionBox.Height < 0)
                {
                    for (int aCounter = 0; aCounter >= this.SelectionBox.Height; aCounter = aCounter - 10)
                    {
                        if (this.SelectionBox.Height - aCounter <= 0)
                        {
                            spriteBatch.Draw(this.DottedLine, new Rectangle(thePositionX - 10, this.SelectionBox.Y + aCounter, 10, 5), Color.White);
                        }
                    }
                }
                return;
            }
            for (int aCounter = -2; aCounter <= this.SelectionBox.Height; aCounter = aCounter + 10)
            {
                if (this.SelectionBox.Height - aCounter >= 0)
                {
                    spriteBatch.Draw(this.DottedLine, new Rectangle(thePositionX, this.SelectionBox.Y + aCounter, 10, 5), new Rectangle?(new Rectangle(0, 0, this.DottedLine.Width, this.DottedLine.Height)), Color.White, 90f.ToRadians(), new Vector2(0f, 0f), SpriteEffects.None, 0f);
                }
            }
        }

        public override void ExitScreen()
        {
            base.ScreenManager.AddScreen(new MainMenuScreen());
            base.ExitScreen();
        }


        private string GetDesignStateText()
        {
            switch (this.DesignState)
            {
                case Restrictions.I:
                {
                    this.DescriptionOfState = "Internal hull slots are important to the health of your ship. If these slots are 70% destroyed or more, then your ship will die. There are many modules, such as power plants, that can only be placed in Internal slots";
                    return "Internal";
                }
                case Restrictions.IO:
                {
                    this.DescriptionOfState = "IO slots are dual-purpose slots that can be equipped with modules bearing either the I or the O restriction";
                    return "Internal / Outside";
                }
                case Restrictions.IOE:
                {
                    return "";
                }
                case Restrictions.O:
                {
                    this.DescriptionOfState = "O slots are slots that are on the outside of your ship. Typically weapons and armor go in external slots";
                    return "Outside";
                }
                case Restrictions.E:
                {
                    this.DescriptionOfState = "Engine slots may only be equipped with engine modules";
                    return "Engine";
                }
                default:
                {
                    return "";
                }
            }
        }

        public Ship GetDisplayedShip()
        {
            return this.StartingShipList[this.selectedShip];
        }

        public Restrictions GetRestrictionFromText(string text)
        {
            string str = text;
            string str1 = str;
            if (str != null)
            {
                if (str1 == "I")
                {
                    return Restrictions.I;
                }
                if (str1 == "O")
                {
                    return Restrictions.O;
                }
                if (str1 == "IO")
                {
                    return Restrictions.IO;
                }
                if (str1 == "E")
                {
                    return Restrictions.E;
                }
            }
            return Restrictions.I;
        }

        public override bool HandleInput(InputState input)
        {
            if (!base.IsActive)
            {
                return false;
            }
            if (this.LoadModelButton.HandleInput(input))
            {
                base.ScreenManager.AddScreen(new LoadModelScreen(this));
            }
            if (input.Escaped)
            {
                this.ActiveModule = null;
                this.ExitScreen();
            }
            if (this.ShipNameBox.HandlingInput)
            {
                this.ShipNameBox.HandleTextInput(ref this.HullName, input);
                this.ShipNameBox.Text = this.HullName;
            }
            if (!this.ShipNameBox.ClickableArea.HitTest(input.CursorPosition))
            {
                this.ShipNameBox.Hover = false;
                if (input.InGameSelect)
                {
                    this.ShipNameBox.HandlingInput = false;
                }
            }
            else
            {
                this.ShipNameBox.Hover = true;
                if (input.InGameSelect)
                {
                    this.ShipNameBox.HandlingInput = true;
                }
            }
            foreach (ToggleButton button in this.DesignStateButtons)
            {
                if (!button.r.HitTest(input.CursorPosition))
                {
                    button.Hover = false;
                }
                else
                {
                    if (!button.Hover)
                    {
                        GameAudio.PlaySfxAsync("sd_ui_mouseover");
                    }
                    button.Hover = true;
                    if (button.HasToolTip)
                    {
                        ToolTip.CreateTooltip(button.WhichToolTip);
                    }
                    if (input.InGameSelect)
                    {
                        GameAudio.PlaySfxAsync("sd_ui_accept_alt3");
                        this.SetRestrictionFromText(button.Action);
                    }
                }
                if (this.GetRestrictionFromText(button.Action) != this.DesignState)
                {
                    button.Active = false;
                }
                else
                {
                    button.Active = true;
                }
            }
            if (input.C)
            {
                this.MarkThruster();
            }
            if (this.SaveHullButton.HandleInput(input))
            {
                this.SaveShipData("New Ship");
            }
            if (input.ScrollIn)
            {
                tscale = tscale + 1f;
            }
            if (input.ScrollOut)
            {
                tscale = tscale - 1f;
            }
            if (input.Right)
            {
                this.heat = 1f;
            }
            if (input.Left)
            {
                this.heat = 0.01f;
            }
            if (input.Up)
            {
                this.applyThruster = true;
            }
            if (input.Down)
            {
                this.applyThruster = false;
            }
            if (input.YButtonDown || input.Right)
            {
                this.NextDesignState();
                return true;
            }
            if (input.BButtonDown)
            {
                this.ExitScreen();
                return true;
            }
            return false;
        }

        public void HandleInput()
        {
            if (!base.IsActive)
            {
                return;
            }
            this.mouseStateCurrent = Mouse.GetState();
            this.mousePos = Vector2.Zero;
            if (this.mouseStateCurrent.LeftButton == ButtonState.Pressed && this.mouseStatePrevious.LeftButton == ButtonState.Released)
            {
                this.SelectionBox = new Rectangle(this.mouseStateCurrent.X, this.mouseStateCurrent.Y, 0, 0);
            }
            if (this.mouseStateCurrent.LeftButton == ButtonState.Pressed)
            {
                this.SelectionBox = new Rectangle(this.SelectionBox.X, this.SelectionBox.Y, this.mouseStateCurrent.X - this.SelectionBox.X, this.mouseStateCurrent.Y - this.SelectionBox.Y);
            }
            else if (this.mouseStateCurrent.LeftButton == ButtonState.Released && this.mouseStatePrevious.LeftButton == ButtonState.Pressed)
            {
                foreach (SlotStruct slot in this.SlotList)
                {
                    if (!slot.PQ.enclosingRect.Intersects(this.SelectionBox) || this.ActiveModule != null)
                    {
                        continue;
                    }
                    slot.PQ.isFilled = !slot.PQ.isFilled;
                    slot.Restrictions = this.DesignState;
                }
            }
            if (this.mouseStateCurrent.LeftButton == ButtonState.Released)
            {
                this.SelectionBox = new Rectangle(-1, -1, 0, 0);
            }
            if (this.mouseStateCurrent.RightButton == ButtonState.Pressed)
            {
                ButtonState rightButton = this.mouseStatePrevious.RightButton;
            }
            this.mouseStatePrevious = this.mouseStateCurrent;
            if (this.applyThruster)
            {
                this.tPos = new Vector2((float)(this.mouseStateCurrent.X - base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2), (float)(this.mouseStateCurrent.Y - base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2));
            }
        }

        public void HandleMouseInput()
        {
            this.mouseStateCurrent = Mouse.GetState();
            this.mousePos = new Vector2((float)this.mouseStateCurrent.X, (float)this.mouseStateCurrent.Y);
            if (this.mouseStateCurrent.LeftButton == ButtonState.Pressed && this.mouseStatePrevious.LeftButton == ButtonState.Released && this.mousePos.X > (float)this.RbBox.X && this.mousePos.Y > (float)this.RbBox.Y && this.mousePos.X < (float)(this.RbBox.X + this.RbBox.Width) && this.mousePos.Y < (float)(this.RbBox.Y + this.RbBox.Height))
            {
                this.ShowOverlay = !this.ShowOverlay;
                GameAudio.PlaySfxAsync("analogue_click2");
            }
            this.mouseStatePrevious = this.mouseStateCurrent;
        }

        public override void LoadContent()
        {
            ScreenManager.RemoveAllObjects();
            PrimitiveQuad.graphicsDevice = base.ScreenManager.GraphicsDevice;
            this.aspect = new Vector2((float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, (float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);
            this.border = new PrimitiveQuad(this.aspect.X / 2f - 512f, this.aspect.Y / 2f - 512f, 1024f, 1024f);
            this.what = this.border.enclosingRect;
            ToggleButton Internal = new ToggleButton(new Rectangle(this.what.X - 32, this.what.Y + 5, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "I");
            this.DesignStateButtons.Add(Internal);
            Internal.Action = "I";
            ToggleButton InternalO = new ToggleButton(new Rectangle(this.what.X - 32, this.what.Y + 5 + 29, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "IO");
            this.DesignStateButtons.Add(InternalO);
            InternalO.Action = "IO";
            ToggleButton External = new ToggleButton(new Rectangle(this.what.X - 32, this.what.Y + 5 + 58, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "O");
            this.DesignStateButtons.Add(External);
            External.Action = "O";
            ToggleButton Engines = new ToggleButton(new Rectangle(this.what.X - 32, this.what.Y + 5 + 87, 24, 24), "SelectionBox/button_formation_active", "SelectionBox/button_formation_inactive", "SelectionBox/button_formation_hover", "SelectionBox/button_formation_press", "E");
            this.DesignStateButtons.Add(Engines);
            Engines.Action = "E";
            this.LoadModelButton = new DanButton(new Vector2(20f, (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 50)), "Load Model");
            this.SaveHullButton = new DanButton(new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 200), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 50)), "Save Hull");
            this.ShipNameBox = new UITextEntry()
            {
                ClickableArea = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 200, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 115, 180, 20),
                Text = this.HullName
            };
            AssignLightRig("example/ShipyardLightrig");
            base.ScreenManager.environment = TransientContent.Load<SceneEnvironment>("example/scene_environment");
            float width = (float)base.Viewport.Width;
            Viewport viewport = base.Viewport;
            float aspectRatio = width / (float)viewport.Height;
            Vector3 camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
            this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) * Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
            this.projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 10000f);
            this.moduleSlot = TransientContent.Load<Texture2D>("Textures/Ships/singlebox");
            this.DottedLine = TransientContent.Load<Texture2D>("Textures/UI/DottedLine");
            this.Center = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2));
            this.ConfigureSlots();
            this.thruster = new Thruster();
            this.thruster.LoadAndAssignDefaultEffects(TransientContent);
            base.LoadContent();
        }

        public void LoadModel(string modelPath)
        {
            if (shipSO != null)
                RemoveObject(shipSO);
            try
            {
                shipSO = ResourceManager.GetSceneMesh(modelPath);
                shipSO.World = worldMatrix;
                ModelPath = modelPath;
                AddObject(shipSO);
            }
            catch (Exception)
            {
                shipSO = ResourceManager.GetSceneMesh(modelPath, animated:true);
                shipSO.World = worldMatrix;
                ModelPath = modelPath;
                AddObject(shipSO);
            }
        }


        private void MarkThruster()
        {
            ThrusterZone z = new ThrusterZone();
            Vector2 thrPos = (this.tPos + new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2))) - new Vector2((float)this.border.X, (float)this.border.Y);
            z.Position = thrPos;
            z.Scale = this.tscale;
            this.TList.Add(z);
        }

        private void NextDesignState()
        {
            switch (this.DesignState)
            {
                case Restrictions.I:
                {
                    this.DesignState = Restrictions.O;
                    return;
                }
                case Restrictions.IO:
                {
                    this.DesignState = Restrictions.E;
                    return;
                }
                case Restrictions.IOE:
                {
                    return;
                }
                case Restrictions.O:
                {
                    this.DesignState = Restrictions.IO;
                    return;
                }
                case Restrictions.E:
                {
                    this.DesignState = Restrictions.I;
                    return;
                }
                default:
                {
                    return;
                }
            }
        }

        public void SaveShipData(string name)
        {
            var data = new ShipData()
            {
                Name      = HullName,
                ModelPath = Path.GetFileNameWithoutExtension(ModelPath),
                Role      = ShipData.RoleName.carrier,
                Hull      = HullName,
                IconPath  = "ShipIcons/hunter"
            };

            var filledModules = new Array<ModuleSlotData> { Capacity = SlotList.Count };
            for (int i = 0; i < SlotList.Count; ++i)
            {
                SlotStruct slot = SlotList[i];
                if (!slot.PQ.isFilled)
                    continue;

                var pos = new Vector2(slot.PQ.X + slot.PQ.W / 2 - border.X, slot.PQ.Y + slot.PQ.H / 2 - border.Y);
                filledModules.Add(new ModuleSlotData
                {
                    Position           = pos,
                    InstalledModuleUID = slot.ModuleUID,
                    Restrictions       = slot.Restrictions
                });
            }
            data.ModuleSlots    = filledModules.ToArray();
            data.DefaultAIState = AIState.AwaitingOrders;
            data.ThrusterList   = TList;
            var ser = new XmlSerializer(typeof(ShipData));
            using (var wfs = new StreamWriter("Ship Tool/" + HullName + ".xml"))
                ser.Serialize(wfs, data);
        }

        public void SetActiveModule(ShipModule mod)
        {
            GameAudio.PlaySfxAsync("smallservo");
            ActiveModule = mod;
        }

        public void SetRestrictionFromText(string text)
        {
            switch (text)
            {
                case "I":  DesignState = Restrictions.I;  return;
                case "O":  DesignState = Restrictions.O;  return;
                case "IO": DesignState = Restrictions.IO; return;
                case "E":  DesignState = Restrictions.E;  return;
            }
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.ScreenManager.editor.Update(gameTime);
            Vector3 camPos = this.cameraPosition * new Vector3(-1f, 1f, 1f);
            this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(180f.ToRadians())) * Matrix.CreateRotationX(0f.ToRadians())) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
            this.designInputState.Update(gameTime);
            this.HandleInput();
            this.thruster.Update(new Vector3(this.tPos.X, this.tPos.Y, 30f), new Vector3(0f, -1f, 0f), new Vector3(this.tscale, this.tscale, this.tscale), this.heat, 0.002f, Color.OrangeRed, Color.Blue, camPos);
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public struct ThrusterZone
        {
            public Vector2 Position;
            public float Scale;
        }
    }
}