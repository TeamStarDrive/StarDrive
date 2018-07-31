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
using Ship_Game.Ships;
using Ship_Game.UI;

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
                    var newCell = new PrimitiveQuad(((int)Center.X + 16 * x), ((int)Center.Y + 16 * y), slotsize, slotsize);
                    var newslot = new SlotStruct()
                    {
                        PQ = newCell,
                        Restrictions = Restrictions.I
                    };
                    this.SlotList.Add(newslot);
                }
            }
        }

        public override void Draw(SpriteBatch batch)
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
            batch.Begin();
            Vector2 TitlePos = new Vector2(20f, 20f);
            HelperFunctions.DrawDropShadowText(base.ScreenManager, "Ship Mod Tools", TitlePos, Fonts.Arial20Bold);
            TitlePos.Y = TitlePos.Y + (float)(Fonts.Arial20Bold.LineSpacing + 3);
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Use this tool to create module patterns for your ships", TitlePos, Color.White);
            if (this.shipSO != null)
            {
                TitlePos = new Vector2((float)this.what.X, 20f);
                SpriteFont arial12Bold = Fonts.Arial12Bold;
                float radius = this.shipSO.WorldBoundingSphere.Radius;
                batch.DrawString(arial12Bold, string.Concat("Radius: ", radius.ToString()), TitlePos, Color.White);
                TitlePos.Y = TitlePos.Y + 20f;
                string text = "If you can't see your model then your radius is likely too big or too small. A radius of 512 will fit snugly inside the box. Change the scale when you compile the model. If it is rotated oddly change the X, Y, and Z axis. If the model is off-center then you will need to re-export the 3D model from Blender, making sure to Set Origin to the desired pivot point of your model";
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, HelperFunctions.ParseText(Fonts.Arial12, text, 600f), TitlePos, Color.White);
            }
            var whichSelectionPos = new Vector2(what.X, what.Y - Fonts.Arial20Bold.LineSpacing);
            batch.DrawString(Fonts.Arial20Bold, string.Concat(DesignState, " - ", GetDesignStateText()), whichSelectionPos, Color.Orange);
            whichSelectionPos.X = whichSelectionPos.X + 150f;
            whichSelectionPos.Y = whichSelectionPos.Y + (float)Fonts.Arial20Bold.LineSpacing;
            whichSelectionPos.Y = whichSelectionPos.Y - Fonts.Arial12Bold.MeasureString(HelperFunctions.ParseText(Fonts.Arial12Bold, this.DescriptionOfState, 512f)).Y;
            base.ScreenManager.SpriteBatch.DrawRectangle(this.what, Color.White);
            foreach (SlotStruct slot in this.SlotList)
            {
                if (!this.applyThruster && slot.PQ.Filled)
                {
                    slot.Draw(batch, moduleSlot, Color.White);
                    batch.DrawString(Fonts.Arial20Bold, " "+slot.Restrictions, 
                        slot.PosVec2, Color.Navy, 0f, Vector2.Zero, 0.4f, SpriteEffects.None, 1f);
                }
                if (this.applyThruster || slot.ModuleUID == null)
                {
                    continue;
                }
                if (slot.Module.XSIZE > 1 || slot.Module.YSIZE > 1)
                {
                    batch.Draw(slot.Tex, slot.ModuleRect, Color.White);
                }
                else
                {
                    slot.Draw(batch, slot.Tex, Color.White);
                }
            }
            this.DrawHorizontalLine(batch, this.SelectionBox.Y);
            this.DrawHorizontalLine(batch, this.SelectionBox.Y + this.SelectionBox.Height);
            this.DrawVerticalLine(batch, this.SelectionBox.X);
            this.DrawVerticalLine(batch, this.SelectionBox.X + this.SelectionBox.Width);
            foreach (ToggleButton button in this.DesignStateButtons)
            {
                button.Draw(base.ScreenManager);
            }
            if (this.ActiveModule != null)
            {
                batch.Draw(ResourceManager.TextureDict[ResourceManager.GetModuleTemplate(ActiveModule.UID).IconTexturePath], new Rectangle(Input.MouseCurr.X, Input.MouseCurr.Y, 16 * this.ActiveModule.XSIZE, 16 * this.ActiveModule.YSIZE), Color.White);
                
                
                for (int i = 0; i < ActiveModule.XSIZE; i++)
                for (int j = 0; j < ActiveModule.YSIZE; j++)
                {
                    var pq = new PrimitiveQuad(new Rectangle(Input.MouseCurr.X + i * 16, Input.MouseCurr.Y + j * 16, 16, 16));
                    pq.Draw(batch, Color.White);
                }

            }
            Vector2 InfoPos = new Vector2((float)(this.SaveHullButton.r.X - 50), (float)(this.SaveHullButton.r.Y - 20));
            base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, "Hulls are saved to StarDrive/Ship Tools", InfoPos, Color.White);
            this.ShipNameBox.Draw(Fonts.Arial20Bold, base.ScreenManager.SpriteBatch, new Vector2((float)this.ShipNameBox.ClickableArea.X, (float)this.ShipNameBox.ClickableArea.Y), gameTime, Color.Orange);
            this.SaveHullButton.Draw(base.ScreenManager);
            this.LoadModelButton.Draw(base.ScreenManager);
            batch.End();
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
                if (!button.Rect.HitTest(input.CursorPosition))
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
            if (input.ScrollIn)  tscale += 1f;
            if (input.ScrollOut) tscale -= 1f;
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
            if (Input.LeftMouseClick)
            {
                this.SelectionBox = new Rectangle(Input.MouseCurr.X, Input.MouseCurr.Y, 0, 0);
            }
            if (Input.LeftMouseDown)
            {
                this.SelectionBox = new Rectangle(this.SelectionBox.X, this.SelectionBox.Y, Input.MouseCurr.X - this.SelectionBox.X, Input.MouseCurr.Y - this.SelectionBox.Y);
            }
            else if (Input.LeftMouseClick)
            {
                foreach (SlotStruct slot in this.SlotList)
                {
                    if (!slot.Intersects(SelectionBox) || this.ActiveModule != null)
                    {
                        continue;
                    }
                    slot.PQ.Filled = !slot.PQ.Filled;
                    slot.Restrictions = this.DesignState;
                }
            }
            if (Input.LeftMouseUp)
            {
                this.SelectionBox = new Rectangle(-1, -1, 0, 0);
            }
            if (this.applyThruster)
            {
                this.tPos = new Vector2(Input.MouseCurr.X - ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2, 
                                        Input.MouseCurr.Y - ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2);
            }
        }

        public void HandleMouseInput()
        {
            if (Input.LeftMouseClick)
            {
                this.ShowOverlay = !this.ShowOverlay;
                GameAudio.PlaySfxAsync("analogue_click2");
            }
        }

        public override void LoadContent()
        {
            ScreenManager.RemoveAllObjects();
            int screenWidth  = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            int screenHeight = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;

            PrimitiveQuad.Device = ScreenManager.GraphicsDevice;
            aspect = new Vector2(screenWidth, screenHeight);
            border = new PrimitiveQuad(aspect.X / 2f - 512f, aspect.Y / 2f - 512f, 1024f, 1024f);
            what = border.Rect;

            var designPos = new Vector2(what.X - 32f, what.Y + 5f);
            void AddDesignBtn(string icon)
            {
                var button = new ToggleButton(designPos, ToggleButtonStyle.Formation, icon) {Action = icon};
                DesignStateButtons.Add(button);
                designPos.Y += 29f;
            }
            AddDesignBtn("I");
            AddDesignBtn("IO");
            AddDesignBtn("O");
            AddDesignBtn("E");

            LoadModelButton = new DanButton(new Vector2(20f, (screenHeight - 50)), "Load Model");
            SaveHullButton = new DanButton(new Vector2((screenWidth - 200), (screenHeight - 50)), "Save Hull");
            ShipNameBox = new UITextEntry()
            {
                ClickableArea = new Rectangle(screenWidth - 200, screenHeight - 115, 180, 20),
                Text = this.HullName
            };
            AssignLightRig("example/ShipyardLightrig");
            ScreenManager.environment = TransientContent.Load<SceneEnvironment>("example/scene_environment");
            float aspectRatio = Viewport.Width / (float)Viewport.Height;
            Vector3 camPos = cameraPosition * new Vector3(-1f, 1f, 1f);
            view = Matrix.CreateRotationY(180f.ToRadians())
                 * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
            projection = Matrix.CreatePerspectiveFieldOfView(0.7853982f, aspectRatio, 1f, 10000f);
            moduleSlot = TransientContent.Load<Texture2D>("Textures/Ships/singlebox");
            DottedLine = TransientContent.Load<Texture2D>("Textures/UI/DottedLine");
            Center = new Vector2((int)(screenWidth / 2), (int)(screenHeight / 2));
            ConfigureSlots();
            thruster = new Thruster();
            thruster.LoadAndAssignDefaultEffects(TransientContent);
            base.LoadContent();
        }

        public void LoadModel(string modelPath)
        {
            if (shipSO != null)
                RemoveObject(shipSO);
            try
            {
                shipSO = ResourceManager.GetSceneMesh(TransientContent, modelPath);

            }
            catch (Exception)
            {
                shipSO = ResourceManager.GetSceneMesh(TransientContent, modelPath, animated:true);
            }
            shipSO.World = worldMatrix;
            ModelPath = modelPath;
            AddObject(shipSO);
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
            switch (DesignState)
            {
                case Restrictions.I:  DesignState = Restrictions.O; return;
                case Restrictions.IO: DesignState = Restrictions.E; return;
                case Restrictions.IOE: return;
                case Restrictions.O:  DesignState = Restrictions.IO; return;
                case Restrictions.E:  DesignState = Restrictions.I;  return;
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
                if (!slot.PQ.Filled)
                    continue;

                filledModules.Add(new ModuleSlotData
                {
                    Position           = slot.ModuleCenter - border.Position,
                    InstalledModuleUID = slot.ModuleUID,
                    Restrictions       = slot.Restrictions
                });
            }
            data.ModuleSlots    = filledModules.ToArray();
            data.DefaultAIState = AIState.AwaitingOrders;
            data.ThrusterList   = TList;
            var ser = new XmlSerializer(typeof(ShipData));
            using (var wfs = new StreamWriter($"Ship Tool/{HullName}.xml"))
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
            thruster.tscale = tscale;
            thruster.WorldPos = new Vector3(tPos.X, tPos.Y, 30f);
            this.thruster.Update(new Vector3(0f, -1f, 0f), this.heat, 0.002f, camPos);
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public struct ThrusterZone
        {
            public Vector2 Position;
            [XmlElement(ElementName = "scale")]
            public float Scale;
        }
    }
}