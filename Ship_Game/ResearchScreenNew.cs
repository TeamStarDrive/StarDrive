using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class ResearchScreenNew : GameScreen
	{
		public Camera2d camera;

        private Map<string, Node> TechTree = new Map<string, Node>(StringComparer.OrdinalIgnoreCase);

        public Map<string, Node> CompleteSubNodeTree = new Map<string, Node>(StringComparer.OrdinalIgnoreCase);

        public Map<string, Node> SubNodes = new Map<string, Node>(StringComparer.OrdinalIgnoreCase);

		private Vector2 Cursor = Vector2.Zero;

		private CloseButton close;

		private Menu2 MainMenu;

		public EmpireUIOverlay empireUI;

		private Vector2 MainMenuOffset;

		private Rectangle QueueContainer;

		public QueueComponent qcomponent;

		private string UIDCurrentRoot = "";

		private int ColumnOffset = 175;

		private int RowOffset = 100;

		private Array<Vector2> ClaimedSpots = new Array<Vector2>();

		private Vector2 cameraVelocity = Vector2.Zero;

		public bool RightClicked;

		private Vector2 StartDragPos = Vector2.Zero;

		//private bool BuyHover;

		//private bool ShowingDetailPopUp;

		private Rectangle DetailPopUpRect = new Rectangle();

		private Node DetailInfo;

		private Rectangle DetailDestinationRect = new Rectangle(0, 0, 600, 600);

		private Rectangle AbsoluteDestination = new Rectangle(0, 0, 600, 600);

        //private float TimerDelay = 0.25f;

        private float ClickTimer;

		//private float transitionElapsedTime;

		private Color needToBuy = new Color(255, 165, 0, 50);

		private Array<ResearchScreenNew.UnlockItem> DetailUnlocks = new Array<ResearchScreenNew.UnlockItem>();

		private ScrollList UnlockSL;

		private Submenu UnlocksSubMenu;

		public ResearchScreenNew(GameScreen parent, EmpireUIOverlay empireUI) : base(parent)
		{
			this.empireUI = empireUI;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			this.camera = new Camera2d();
		}

        protected override void Dispose(bool disposing)
        {
            qcomponent?.Dispose(ref qcomponent);
            base.Dispose(disposing);
        }

        public override void Draw(GameTime gameTime)
        {
            double totalSeconds = gameTime.ElapsedGameTime.TotalSeconds;
            this.ScreenManager.FadeBackBufferToBlack((int)this.TransitionAlpha * 2 / 3);
            this.ScreenManager.SpriteBatch.Begin();
            this.ScreenManager.SpriteBatch.FillRectangle(new Rectangle(0, 0, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight), Color.Black);
            this.MainMenu.Draw();
            this.ScreenManager.SpriteBatch.End();
            this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, this.camera.get_transformation(this.ScreenManager.GraphicsDevice));
            foreach (KeyValuePair<string, Node> keyValuePair in this.TechTree)
            {
                if (keyValuePair.Value is RootNode && (keyValuePair.Value as RootNode).nodeState == NodeState.Press)
                {                    
                    Vector2 vector2_1 = new Vector2((float)((keyValuePair.Value as RootNode).RootRect.X + (keyValuePair.Value as RootNode).RootRect.Width - 10), (float)((keyValuePair.Value as RootNode).RootRect.Y + (keyValuePair.Value as RootNode).RootRect.Height / 2));
                    Vector2 vector2_2 = this.MainMenuOffset + new Vector2((float)this.ColumnOffset, 0.0f);
                    vector2_2.Y = vector2_1.Y;
                    vector2_2.X -= Vector2.Distance(vector2_1, vector2_2) / 2f;
                    for (int index = 0; index < ResourceManager.TechTree[keyValuePair.Key].LeadsTo.Count; ++index)
                    {
                        if (!ResourceManager.TechTree[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Secret || EmpireManager.Player.GetTDict()[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Discovered)
                        {
                            bool Complete = false;
                            if (EmpireManager.Player.GetTDict()[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Unlocked)
                                Complete = true;
                            Vector2 vector2_3 = new Vector2(vector2_2.X, (float)((this.SubNodes[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID] as TreeNode).BaseRect.Y + (this.SubNodes[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID] as TreeNode).BaseRect.Height / 2 - 10));
                            --vector2_3.Y;
                            if ((double)vector2_3.Y > (double)vector2_2.Y)
                                this.ScreenManager.SpriteBatch.DrawResearchLineVertical(vector2_2, vector2_3, Complete);
                            else
                                this.ScreenManager.SpriteBatch.DrawResearchLineVertical(vector2_3, vector2_2, Complete);
                        }
                    }
                }
            }
            foreach (KeyValuePair<string, Node> keyValuePair in this.SubNodes)
            {
                if (keyValuePair.Value is TreeNode)
                {
                    Vector2 vector2_1 = new Vector2((float)((keyValuePair.Value as TreeNode).BaseRect.X + (keyValuePair.Value as TreeNode).BaseRect.Width - 25), (float)((keyValuePair.Value as TreeNode).BaseRect.Y + (keyValuePair.Value as TreeNode).BaseRect.Height / 2 - 10));
                    Vector2 vector2_2 = vector2_1 + new Vector2((float)(this.ColumnOffset / 2), 0.0f);
                    vector2_2.Y = vector2_1.Y;
                    for (int index = 0; index < ResourceManager.TechTree[keyValuePair.Key].LeadsTo.Count; ++index)
                    {
                        if ((!ResourceManager.TechTree[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Secret || EmpireManager.Player.GetTDict()[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Discovered) && (!ResourceManager.TechTree[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Secret || EmpireManager.Player.GetTDict()[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Discovered))
                        {
                            bool Complete = false;
                            if (EmpireManager.Player.GetTDict()[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Unlocked)
                                Complete = true;
                            Vector2 vector2_3 = new Vector2(vector2_2.X, (float)((this.SubNodes[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID] as TreeNode).BaseRect.Y + (this.SubNodes[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID] as TreeNode).BaseRect.Height / 2 - 10));
                            if ((double)vector2_3.Y > (double)vector2_2.Y)
                                this.ScreenManager.SpriteBatch.DrawResearchLineVertical(vector2_2, vector2_3, Complete);
                            else
                                this.ScreenManager.SpriteBatch.DrawResearchLineVertical(vector2_3, vector2_2, Complete);
                        }
                    }
                }
            }
            foreach (KeyValuePair<string, Node> keyValuePair in this.SubNodes)
            {
                if (keyValuePair.Value is TreeNode && ResourceManager.TechTree[keyValuePair.Key].LeadsTo.Count > 0)
                {
                    Vector2 LeftPoint1 = new Vector2((float)((keyValuePair.Value as TreeNode).BaseRect.X + (keyValuePair.Value as TreeNode).BaseRect.Width - 25), (float)((keyValuePair.Value as TreeNode).BaseRect.Y + (keyValuePair.Value as TreeNode).BaseRect.Height / 2 - 10));
                    Vector2 RightPoint1 = LeftPoint1 + new Vector2((float)(this.ColumnOffset / 2), 0.0f);
                    RightPoint1.Y = LeftPoint1.Y;
                    bool Complete1 = false;
                    for (int index = 0; index < ResourceManager.TechTree[keyValuePair.Key].LeadsTo.Count; ++index)
                    {
                        if ((!ResourceManager.TechTree[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Secret || EmpireManager.Player.GetTDict()[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Discovered) && (!ResourceManager.TechTree[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Secret || EmpireManager.Player.GetTDict()[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Discovered))
                        {
                            bool Complete2 = false;
                            if (EmpireManager.Player.GetTDict()[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Unlocked)
                            {
                                Complete2 = true;
                                Complete1 = true;
                            }
                            Vector2 LeftPoint2 = new Vector2(RightPoint1.X, (float)((this.SubNodes[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID] as TreeNode).BaseRect.Y + (this.SubNodes[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID] as TreeNode).BaseRect.Height / 2 - 10));
                            Vector2 RightPoint2 = LeftPoint2 + new Vector2(Vector2.Distance(LeftPoint1, RightPoint1) + 13f, 0.0f);
                            this.ScreenManager.SpriteBatch.DrawResearchLineHorizontalGradient(LeftPoint2, RightPoint2, Complete2);
                        }
                    }
                    this.ScreenManager.SpriteBatch.DrawResearchLineHorizontal(LeftPoint1, RightPoint1, Complete1);
                }
            }
            foreach (KeyValuePair<string, Node> keyValuePair in this.TechTree)
            {
                if (keyValuePair.Value is RootNode && (keyValuePair.Value as RootNode).nodeState == NodeState.Press)
                {
                    Vector2 LeftPoint1 = new Vector2((float)((keyValuePair.Value as RootNode).RootRect.X + (keyValuePair.Value as RootNode).RootRect.Width - 10), (float)((keyValuePair.Value as RootNode).RootRect.Y + (keyValuePair.Value as RootNode).RootRect.Height / 2));
                    Vector2 RightPoint1 = this.MainMenuOffset + new Vector2((float)this.ColumnOffset, 0.0f);
                    RightPoint1.Y = LeftPoint1.Y;
                    RightPoint1.X -= Vector2.Distance(LeftPoint1, RightPoint1) / 2f;
                    bool Complete1 = false;
                    for (int index = 0; index < ResourceManager.TechTree[keyValuePair.Key].LeadsTo.Count; ++index)
                    {
                        if (!ResourceManager.TechTree[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Secret || EmpireManager.Player.GetTDict()[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Discovered)
                        {
                            bool Complete2 = false;
                            if (EmpireManager.Player.GetTDict()[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID].Unlocked)
                            {
                                Complete2 = true;
                                Complete1 = true;
                            }
                            Vector2 LeftPoint2 = new Vector2(RightPoint1.X, (float)((this.SubNodes[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID] as TreeNode).BaseRect.Y + (this.SubNodes[ResourceManager.TechTree[keyValuePair.Key].LeadsTo[index].UID] as TreeNode).BaseRect.Height / 2 - 10));
                            --LeftPoint2.Y;
                            Vector2 RightPoint2 = LeftPoint2 + new Vector2(Vector2.Distance(LeftPoint1, RightPoint1) + 13f, 0.0f);
                            this.ScreenManager.SpriteBatch.DrawResearchLineHorizontalGradient(LeftPoint2, RightPoint2, Complete2);
                        }
                    }
                    this.ScreenManager.SpriteBatch.DrawResearchLineHorizontal(LeftPoint1, RightPoint1, Complete1);
                }
            }
            foreach (KeyValuePair<string, Node> keyValuePair in this.TechTree)
            {
                if (keyValuePair.Value is RootNode)
                    (keyValuePair.Value as RootNode).Draw(this.ScreenManager.SpriteBatch);
            }
            foreach (KeyValuePair<string, Node> keyValuePair in this.SubNodes)
            {
                if (keyValuePair.Value is TreeNode)
                    (keyValuePair.Value as TreeNode).Draw(this.ScreenManager);
            }
            this.ScreenManager.SpriteBatch.End();
            this.ScreenManager.SpriteBatch.Begin();
            this.MainMenu.DrawHollow();
            this.close.Draw(this.ScreenManager);
            this.qcomponent.Draw();
            ToolTip.Draw(this.ScreenManager);
            this.ScreenManager.SpriteBatch.End();
        }

		public override void ExitScreen()
		{
			GlobalStats.ResearchRootUIDToDisplay = this.UIDCurrentRoot;
			base.ExitScreen();
		}

	

		private int FindDeepestY()
		{
			int deepest = 0;
			foreach (KeyValuePair<string, Node> node in this.TechTree)
			{
				if (node.Value.NodePosition.Y <= (float)deepest)
				{
					continue;
				}
				deepest = (int)node.Value.NodePosition.Y;
			}
			return deepest;
		}

		private int FindDeepestYSubNodes()
		{
			int deepest = 0;
			foreach (KeyValuePair<string, Node> node in this.SubNodes)
			{
				if (node.Value.NodePosition.Y <= (float)deepest)
				{
					continue;
				}
				deepest = (int)node.Value.NodePosition.Y;
			}
			return deepest;
		}

		public override void HandleInput(InputState input)
		{
            if (input.CurrentKeyboardState.IsKeyDown(Keys.R) && !input.LastKeyboardState.IsKeyDown(Keys.R) && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                this.ExitScreen();
                return;
            }
			if (this.close.HandleInput(input))
			{
				this.ExitScreen();
				return;
			}
            if (input.CurrentMouseState.RightButton == ButtonState.Pressed && input.LastMouseState.RightButton == ButtonState.Released)
			{
				this.StartDragPos = input.CursorPosition;
				this.cameraVelocity.X = 0f;
				this.cameraVelocity.Y = 0f;
			}
            if (input.CurrentMouseState.RightButton != ButtonState.Pressed || input.LastMouseState.RightButton != ButtonState.Pressed || this.RightClicked) 
			{
				this.cameraVelocity.X = 0f;
				this.cameraVelocity.Y = 0f;
                if (this.RightClicked)  //fbedard: prevent screen scroll
                    this.StartDragPos = input.CursorPosition;
			}
			else
			{
				float xDiff = input.CursorPosition.X - this.StartDragPos.X;
				float yDiff = input.CursorPosition.Y - this.StartDragPos.Y;
				Camera2d vector2 = this.camera;
				vector2._pos = vector2._pos + new Vector2(-xDiff, -yDiff);
				this.StartDragPos = input.CursorPosition;
			}
			this.cameraVelocity.X = MathHelper.Clamp(this.cameraVelocity.X, -10f, 10f);
			this.cameraVelocity.Y = MathHelper.Clamp(this.cameraVelocity.Y, -10f, 10f);
			this.camera._pos.X = MathHelper.Clamp(this.camera._pos.X, (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2), 3200f);
			this.camera._pos.Y = MathHelper.Clamp(this.camera._pos.Y, (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2), 3200f);
			if (input.CurrentKeyboardState.IsKeyDown(Keys.RightControl) && input.CurrentKeyboardState.IsKeyDown(Keys.F1) && input.LastKeyboardState.IsKeyUp(Keys.F1))
			{
               
                foreach (KeyValuePair<string, Technology> tech in ResourceManager.TechTree)
                {
                    this.UnlockTree(tech.Key);
                    foreach(Technology.UnlockedMod unlockmod in tech.Value.ModulesUnlocked)
                    {
                        EmpireManager.Player.GetMDict()[unlockmod.ModuleUID] = true;
                    }
                }
                foreach (KeyValuePair<string, ShipData> hull in ResourceManager.HullsDict)
                {
                    EmpireManager.Player.GetHDict()[hull.Key] = true;
                }
                //foreach (KeyValuePair<string, ShipModule> Module in ResourceManager.ShipModulesDict)
                //{
                    
                //    EmpireManager.Player.GetMDict()[Module.Key] = true;
                //}


                foreach (KeyValuePair<string, Ship_Game.Building> Building in ResourceManager.BuildingsDict)
                {
                    //if (ResourceManager.BuildingsDict[Building.Key].EventTriggerUID == null || ResourceManager.BuildingsDict[Building.Key].EventTriggerUID == "")
                    if (string.IsNullOrEmpty(ResourceManager.BuildingsDict[Building.Key].EventTriggerUID))
                        EmpireManager.Player.GetBDict()[Building.Key] = true;
                }
                EmpireManager.Player.UpdateShipsWeCanBuild();
			}
            //Added by McShooterz: new cheat to only unlock tech
            if (input.CurrentKeyboardState.IsKeyDown(Keys.RightControl) && input.CurrentKeyboardState.IsKeyDown(Keys.F2) && input.LastKeyboardState.IsKeyUp(Keys.F2))
            {
                foreach (KeyValuePair<string, Technology> tech in ResourceManager.TechTree)
                {
                    this.UnlockTree(tech.Key);
                }
                EmpireManager.Player.UpdateShipsWeCanBuild();
            }
            //Added by McShooterz: new cheat to only unlock tech
            if (input.CurrentKeyboardState.IsKeyDown(Keys.RightControl) && input.CurrentKeyboardState.IsKeyDown(Keys.F3) && input.LastKeyboardState.IsKeyUp(Keys.F3))
            {
                foreach (KeyValuePair<string, Technology> tech in ResourceManager.TechTree)
                {
                    this.UnlockTreeNoBonus(tech.Key);
                }
                EmpireManager.Player.UpdateShipsWeCanBuild();
            }
			this.qcomponent.HandleInput(input);
			if (this.qcomponent.Visible && this.qcomponent.container.HitTest(input.CursorPosition))
			{
				return;
			}
			foreach (KeyValuePair<string, Node> tech in this.TechTree)
			{
				if (!tech.Value.HandleInput(input) || !(tech.Value is RootNode))
				{
					continue;
				}
				this.PopulateNodesFromRoot(tech.Value as RootNode);
				GameAudio.PlaySfxAsync("sd_ui_research_select");
			}
			this.RightClicked = false;
			foreach (KeyValuePair<string, Node> tech in this.SubNodes)
			{
				if (!(tech.Value as TreeNode).HandleInput(input, base.ScreenManager, this.camera))
				{
                    if ((tech.Value as TreeNode).screen.RightClicked)  //fbedard: popup open
                        this.RightClicked = true;
					continue;
				}
				if (EmpireManager.Player.GetTDict()[tech.Key].Unlocked)
				{
					GameAudio.PlaySfxAsync("UI_Misc20");
				}
				else if (EmpireManager.Player.HavePreReq(tech.Key))
				{
					this.qcomponent.SetVisible();
					this.qcomponent.AddToQueue(tech.Value as TreeNode);
					GameAudio.PlaySfxAsync("sd_ui_research_select");
				}
				else if (EmpireManager.Player.HavePreReq(tech.Key))
				{
					GameAudio.PlaySfxAsync("UI_Misc20");
				}
				else
				{
					this.qcomponent.SetVisible();
					GameAudio.PlaySfxAsync("sd_ui_research_select");
					string techToCheck = tech.Key;
					Array<string> TechsToAdd = new Array<string>()
					{
						techToCheck
					};
					if (ResourceManager.TechTree[techToCheck].RootNode != 1)
					{
						while (!EmpireManager.Player.GetTDict()[techToCheck].Unlocked)
						{
							string prereq = EmpireManager.Player.GetPreReq(techToCheck);
							if (string.IsNullOrEmpty(prereq))
							{
								break;
							}
							if (!EmpireManager.Player.GetTDict()[prereq].Unlocked)
							{
								TechsToAdd.Add(prereq);
							}
							techToCheck = prereq;
						}
					}
					TechsToAdd.Reverse();
					foreach (string toAdd in TechsToAdd)
					{
						this.qcomponent.AddToQueue(this.SubNodes[toAdd] as TreeNode);
					}
				}
			}
			if (input.Escaped)
			{
				this.ExitScreen();
			}
			Vector2 vector21 = new Vector2((float)input.CurrentMouseState.X, (float)input.CurrentMouseState.Y);
			if (!this.RightClicked)
			{
				bool rightMouseClick = input.RightMouseClick;
			}
		}

		public override void LoadContent()
		{
			this.camera = new Camera2d();
			Camera2d vector2 = this.camera;
			Viewport viewport = base.Viewport;
			float width = (float)viewport.Width / 2f;
			Viewport viewport1 = base.Viewport;
			vector2.Pos = new Vector2(width, (float)viewport1.Height / 2f);
			Rectangle main = new Rectangle(0, 0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);
			this.MainMenu = new Menu2(base.ScreenManager, main);
			this.MainMenuOffset = new Vector2((float)(main.X + 20), (float)(main.Y + 30));
			this.close = new CloseButton(new Rectangle(main.X + main.Width - 40, main.Y + 20, 20, 20));
			this.QueueContainer = new Rectangle(main.X + main.Width - 355, main.Y + 40, 330, main.Height - 100);
			this.qcomponent = new QueueComponent(base.ScreenManager, this.QueueContainer, this);
			if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth < 1600)
			{
				this.qcomponent.SetInvisible();
			}
			int numRoots = 0;
			foreach (KeyValuePair<string, TechEntry> tech in EmpireManager.Player.GetTDict())
			{
				if (ResourceManager.TechTree[tech.Value.UID].RootNode != 1)
				{
					continue;
				}
				if (!ResourceManager.TechTree[tech.Value.UID].Secret)
				{
					numRoots++;
				}
                else if (EmpireManager.Player.GetTDict()[tech.Value.UID].Discovered)
				{
					numRoots++;
				}
			}
			this.RowOffset = (main.Height - 40) / numRoots;
			this.MainMenuOffset.Y = (float)(main.Y + this.RowOffset / 3);
			if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight <= 720)
			{
				this.MainMenuOffset.Y = this.MainMenuOffset.Y + 8f;
			}
			foreach (KeyValuePair<string, TechEntry> tech in EmpireManager.Player.GetTDict())
			{
				if (ResourceManager.TechTree[tech.Value.UID].RootNode != 1)
				{
					continue;
				}
				if (ResourceManager.TechTree[tech.Value.UID].Secret)
				{
					if (!EmpireManager.Player.GetTDict()[tech.Value.UID].Discovered)
					{
						continue;
					}
					this.Cursor.X = 0f;
					this.Cursor.Y = (float)(this.FindDeepestY() + 1);
					this.SetNode(tech.Value);
				}
				else
				{
					this.Cursor.X = 0f;
					this.Cursor.Y = (float)(this.FindDeepestY() + 1);
					this.SetNode(tech.Value);
				}
			}
            this.RowOffset = (main.Height - 40) / 6;
			foreach (KeyValuePair<string, Node> entry in this.TechTree)
			{
				this.PopulateAllTechsFromRoot(entry.Value as RootNode);
			}
			this.PopulateNodesFromRoot(this.TechTree[GlobalStats.ResearchRootUIDToDisplay] as RootNode);
			string resTop = EmpireManager.Player.ResearchTopic;
			if (!string.IsNullOrEmpty(resTop))
			{
                Node resTopNode = null;
                this.CompleteSubNodeTree.TryGetValue(resTop, out resTopNode);
                if(resTopNode != null)
                this.qcomponent.LoadQueue(resTopNode as TreeNode);
                else
                {
                    resTop = string.Empty;
                    EmpireManager.Player.ResearchTopic = string.Empty;
                }
			}
			foreach (string uid in EmpireManager.Player.data.ResearchQueue)
			{
				this.qcomponent.LoadQueue(this.CompleteSubNodeTree[uid] as TreeNode);
			}
			base.LoadContent();
		}

		public void PopulateAllTechs(Node treenode)
		{
			for (int i = 0; i < ResourceManager.TechTree[treenode.tech.UID].LeadsTo.Count; i++)
			{
				if (!ResourceManager.TechTree[ResourceManager.TechTree[treenode.tech.UID].LeadsTo[i].UID].Secret || EmpireManager.Player.GetTDict()[ResourceManager.TechTree[treenode.tech.UID].LeadsTo[i].UID].Discovered)
				{
					if (i != 0)
					{
						this.Cursor.Y = (float)(this.FindDeepestYSubNodes() + 1);
					}
					else
					{
						this.Cursor.Y = (float)this.FindDeepestYSubNodes();
					}
					this.Cursor.X = treenode.NodePosition.X + 1f;
					TechEntry te = EmpireManager.Player.GetTDict()[ResourceManager.TechTree[treenode.tech.UID].LeadsTo[i].UID];
					TreeNode newNode = new TreeNode(this.MainMenuOffset + new Vector2((float)(this.ColumnOffset * (int)this.Cursor.X), (float)(this.RowOffset * (int)this.Cursor.Y)), te, this)
					{
						NodePosition = this.Cursor
					};
					if (EmpireManager.Player.GetTDict()[te.UID].Unlocked)
					{
						newNode.complete = true;
					}
					this.CompleteSubNodeTree.Add(newNode.tech.UID, newNode);
					this.PopulateAllTechs(newNode);
				}
			}
		}

		public void PopulateAllTechsFromRoot(RootNode Root)
		{
			foreach (KeyValuePair<string, Node> node in this.TechTree)
			{
				(node.Value as RootNode).nodeState = NodeState.Normal;
			}
			Root.nodeState = NodeState.Press;
			this.Cursor = new Vector2(1f, 1f);
			for (int i = 0; i < ResourceManager.TechTree[Root.tech.UID].LeadsTo.Count; i++)
			{
				if (!ResourceManager.TechTree[ResourceManager.TechTree[Root.tech.UID].LeadsTo[i].UID].Secret || EmpireManager.Player.GetTDict()[ResourceManager.TechTree[Root.tech.UID].LeadsTo[i].UID].Discovered)
				{
					TechEntry te = EmpireManager.Player.GetTDict()[ResourceManager.TechTree[Root.tech.UID].LeadsTo[i].UID];
					TreeNode newNode = new TreeNode(this.MainMenuOffset + new Vector2((float)(this.ColumnOffset * (int)this.Cursor.X), (float)(this.RowOffset * (int)this.Cursor.Y)), te, this)
					{
						NodePosition = this.Cursor
					};
					if (EmpireManager.Player.GetTDict()[te.UID].Unlocked)
					{
						newNode.complete = true;
					}
					this.CompleteSubNodeTree.Add(newNode.tech.UID, newNode);
					this.PopulateAllTechs(newNode);
				}
			}
		}

		public void PopulateNodesFromRoot(RootNode Root)
		{
			this.UIDCurrentRoot = Root.tech.UID;
			this.SubNodes.Clear();
            int Rows = 1;
            int Cols = CalculateTreeDemensionsFromRoot(Root.tech.UID, ref Rows, 0, 0);
            if (Rows < 9)
                this.RowOffset = (MainMenu.Menu.Height - 40) / Rows;
            else
                this.RowOffset = (MainMenu.Menu.Height - 40) / 9;
            if (Cols > 0 && Cols < 9)
                this.ColumnOffset = (MainMenu.Menu.Width - 350) / Cols + 1;
            else
                this.ColumnOffset = 165;
			foreach (KeyValuePair<string, Node> node in this.TechTree)
			{
				(node.Value as RootNode).nodeState = NodeState.Normal;
			}
			Root.nodeState = NodeState.Press;
			this.ClaimedSpots.Clear();
			this.Cursor = new Vector2(1f, 1f);
            bool first = true;
			for (int i = 0; i < ResourceManager.TechTree[Root.tech.UID].LeadsTo.Count; i++)
			{
				if (!ResourceManager.TechTree[ResourceManager.TechTree[Root.tech.UID].LeadsTo[i].UID].Secret || EmpireManager.Player.GetTDict()[ResourceManager.TechTree[Root.tech.UID].LeadsTo[i].UID].Discovered)
				{
					if (!first)
					{
						this.Cursor.Y = (float)(this.FindDeepestYSubNodes() + 1);
					}
					else
					{
						this.Cursor.Y = (float)this.FindDeepestYSubNodes();
                        first = false;
					}
					this.Cursor.X = Root.NodePosition.X + 1f;
					TechEntry te = EmpireManager.Player.GetTDict()[ResourceManager.TechTree[Root.tech.UID].LeadsTo[i].UID];
					TreeNode newNode = new TreeNode(this.MainMenuOffset + new Vector2((float)(this.ColumnOffset * (int)this.Cursor.X), (float)(this.RowOffset * (int)this.Cursor.Y)), te, this)
					{
						NodePosition = this.Cursor
					};
					if (EmpireManager.Player.GetTDict()[te.UID].Unlocked)
					{
						newNode.complete = true;
					}
					this.SubNodes.Add(newNode.tech.UID, newNode);
					this.PopulateNodesFromSubNode(newNode);
				}
			}
		}

		public void PopulateNodesFromSubNode(Node treenode)
		{
			Vector2 Position = new Vector2(this.Cursor.X, this.Cursor.Y);
			bool SeatTaken = false;
			foreach (Vector2 v in this.ClaimedSpots)
			{
				if (v.X != Position.X || v.Y != Position.Y)
				{
					continue;
				}
				SeatTaken = true;
			}
			if (SeatTaken)
			{
				this.Cursor.Y = this.Cursor.Y + 1f;
			}
			else
			{
				this.ClaimedSpots.Add(Position);
			}
			for (int i = 0; i < ResourceManager.TechTree[treenode.tech.UID].LeadsTo.Count; i++)
			{
				if (!ResourceManager.TechTree[ResourceManager.TechTree[treenode.tech.UID].LeadsTo[i].UID].Secret || EmpireManager.Player.GetTDict()[ResourceManager.TechTree[treenode.tech.UID].LeadsTo[i].UID].Discovered)
				{
					if (i != 0)
					{
						this.Cursor.Y = (float)(this.FindDeepestYSubNodes() + 1);
					}
					else
					{
						this.Cursor.Y = (float)this.FindDeepestYSubNodes();
					}
					this.Cursor.X = treenode.NodePosition.X + 1f;
					TechEntry te = EmpireManager.Player.GetTDict()[ResourceManager.TechTree[treenode.tech.UID].LeadsTo[i].UID];
					TreeNode newNode = new TreeNode(this.MainMenuOffset + new Vector2((float)(this.ColumnOffset * (int)this.Cursor.X), (float)(this.RowOffset * (int)this.Cursor.Y)), te, this)
					{
						NodePosition = this.Cursor
					};
					if (EmpireManager.Player.GetTDict()[te.UID].Unlocked)
					{
						newNode.complete = true;
					}
					this.SubNodes.Add(newNode.tech.UID, newNode);
					this.PopulateNodesFromSubNode(newNode);
				}
			}
		}

		private void SetNode(TechEntry tech)
		{
			Vector2 Position = new Vector2(this.Cursor.X, this.Cursor.Y);
			bool SeatTaken = false;
			foreach (Vector2 v in this.ClaimedSpots)
			{
				if (v.X != Position.X || v.Y != Position.Y)
				{
					continue;
				}
				SeatTaken = true;
			}
			if (SeatTaken)
			{
				this.Cursor.Y = this.Cursor.Y + 1f;
			}
			else
			{
				this.ClaimedSpots.Add(Position);
			}
			RootNode newNode = new RootNode(this.MainMenuOffset + new Vector2((float)(this.ColumnOffset * (int)this.Cursor.X), (float)(this.RowOffset * ((int)this.Cursor.Y - 1))), tech)
			{
				NodePosition = this.Cursor
			};
			if (EmpireManager.Player.GetTDict()[tech.UID].Unlocked)
			{
				newNode.isResearched = true;
			}
			this.TechTree.Add(tech.UID, newNode);
		}

		public void ShowDetailPop(Node node, string techUID)
		{
			Technology unlockedTech = ResourceManager.TechTree[techUID];
			//this.ShowingDetailPopUp = true;
			this.DetailInfo = node;
			this.DetailPopUpRect = node.NodeRect;
			this.UnlocksSubMenu = new Submenu(base.ScreenManager, this.DetailPopUpRect);
			this.UnlockSL = new ScrollList(this.UnlocksSubMenu, 96)
			{
				indexAtTop = 0
			};
			this.DetailUnlocks.Clear();
			foreach (Technology.UnlockedMod unlockMod in unlockedTech.ModulesUnlocked)
			{
                if (EmpireManager.Player.data.Traits.ShipType == unlockMod.Type || unlockMod.Type == null || unlockMod.Type == EmpireManager.Player.GetTDict()[techUID].AcquiredFrom)
                {
                    UnlockItem unlock = new UnlockItem()
                    {
                        Type = "SHIPMODULE",
                        module = ResourceManager.GetModuleTemplate(unlockMod.ModuleUID)
                    };
                    this.UnlockSL.AddItem(unlock);
                }
			}
			foreach (Technology.UnlockedTroop troop in unlockedTech.TroopsUnlocked)
			{
                if (troop.Type == EmpireManager.Player.data.Traits.ShipType || troop.Type == null || troop.Type == "ALL" || troop.Type == EmpireManager.Player.GetTDict()[techUID].AcquiredFrom)
                {
                    UnlockItem unlock = new UnlockItem()
                    {
                        Type = "TROOP",
                        troop = ResourceManager.GetTroopTemplate(troop.Name)
                    };
                    this.UnlockSL.AddItem(unlock);
                }
			}
			foreach (Technology.UnlockedHull hull in unlockedTech.HullsUnlocked)
			{
                if (EmpireManager.Player.data.Traits.ShipType == hull.ShipType || hull.ShipType == null || hull.ShipType == EmpireManager.Player.GetTDict()[techUID].AcquiredFrom)
                {
                    UnlockItem unlock = new UnlockItem()
                    {
                        Type = "HULL",
                        privateName = hull.Name,
                        HullUnlocked = ResourceManager.HullsDict[hull.Name].Name
                    };
                    unlock.Description = string.Concat(Localizer.Token(4042), " ", Localizer.GetRole(ResourceManager.HullsDict[hull.Name].Role, EmpireManager.Player));
                    this.UnlockSL.AddItem(unlock);
                }
			}
			foreach (Technology.UnlockedBuilding unlockedB in unlockedTech.BuildingsUnlocked)
			{
                if (EmpireManager.Player.data.Traits.ShipType == unlockedB.Type || unlockedB.Type == null || unlockedB.Type == EmpireManager.Player.GetTDict()[techUID].AcquiredFrom)
                {
                    UnlockItem unlock = new UnlockItem()
                    {
                        Type = "BUILDING",
                        building = ResourceManager.BuildingsDict[unlockedB.Name]
                    };
                    this.UnlockSL.AddItem(unlock);
                }
			}
			foreach (Technology.UnlockedBonus ub in unlockedTech.BonusUnlocked)
			{
                if (EmpireManager.Player.data.Traits.ShipType == ub.Type || ub.Type == null || ub.Type == EmpireManager.Player.GetTDict()[techUID].AcquiredFrom)
                {
                    UnlockItem unlock = new UnlockItem()
                    {
                        Type = "ADVANCE",
                        privateName = ub.Name,
                    };
                    this.UnlockSL.AddItem(unlock);
                }
			}
		}

		private void UnlockTree(string key)
		{
            if (EmpireManager.Player.HavePreReq(key) && (!ResourceManager.TechTree[key].Secret || (ResourceManager.TechTree[key].Secret && EmpireManager.Player.GetTDict()[key].Discovered)))
            {
                EmpireManager.Player.UnlockTech(key);
                foreach (Technology.LeadsToTech tech in ResourceManager.TechTree[key].LeadsTo)
                {
                    this.UnlockTree(tech.UID);
                }
            }
		}

        private void UnlockTreeNoBonus(string key)
        {
            if (EmpireManager.Player.HavePreReq(key) && (!ResourceManager.TechTree[key].Secret || (ResourceManager.TechTree[key].Secret && EmpireManager.Player.GetTDict()[key].Discovered)))
            {
                if(ResourceManager.TechTree[key].BonusUnlocked.Count == 0)
                    EmpireManager.Player.UnlockTech(key);
                foreach (Technology.LeadsToTech tech in ResourceManager.TechTree[key].LeadsTo)
                {
                    this.UnlockTreeNoBonus(tech.UID);
                }
            }
        }

        //Added by McShooterz: find size of tech tree before it is built
        private int CalculateTreeDemensionsFromRoot(string UID, ref int rows, int cols, int colmax)
        {
            int max = 0;
            int rowCount = 0;
            cols++;
            if (cols > colmax)
                colmax = cols;
            if (ResourceManager.TechTree[UID].LeadsTo.Count > 1)
            {
                foreach (Technology.LeadsToTech tech in ResourceManager.TechTree[UID].LeadsTo)
                {
                    if (EmpireManager.Player.GetTDict()[tech.UID].Tech.Secret && !EmpireManager.Player.GetTDict()[tech.UID].Discovered)
                    {
                        continue;
                    }
                    rowCount++;
                }
                if(rowCount > 1)
                    rows += rowCount - 1;
            }
            foreach (Technology.LeadsToTech tech in ResourceManager.TechTree[UID].LeadsTo)
            {
                if (EmpireManager.Player.GetTDict()[tech.UID].Tech.Secret && !EmpireManager.Player.GetTDict()[tech.UID].Discovered)
                {
                    continue;
                }
                max = CalculateTreeDemensionsFromRoot(tech.UID, ref rows, cols, colmax);
                if (max > colmax)
                    colmax = max;
            }
            return colmax;
        }

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			ResearchScreenNew clickTimer = this;
			clickTimer.ClickTimer = clickTimer.ClickTimer + elapsedTime;
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		public class UnlockItem
		{
			public string Type;

			public string HullUnlocked;

			public string privateName;

			public Building building;

			public ShipModule module;

			public Troop troop;

			public string Description;

			public UnlockItem()
			{
			}
		}
	}
}