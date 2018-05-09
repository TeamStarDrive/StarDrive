using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class ResearchScreenNew : GameScreen
    {
        public Camera2D camera = new Camera2D();

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

        public ResearchScreenNew(GameScreen parent, EmpireUIOverlay empireUi) : base(parent)
        {
            this.empireUI = empireUi;
            base.IsPopup = true;
            base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
            base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
        }

        protected override void Destroy()
        {
            qcomponent?.Dispose(ref qcomponent);
            base.Destroy();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            double totalSeconds = Game1.Instance.GameTime.ElapsedGameTime.TotalSeconds;
            this.ScreenManager.FadeBackBufferToBlack((int)this.TransitionAlpha * 2 / 3);
            this.ScreenManager.SpriteBatch.Begin();
            this.ScreenManager.SpriteBatch.FillRectangle(new Rectangle(0, 0, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, this.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight), Color.Black);
            this.MainMenu.Draw();
            this.ScreenManager.SpriteBatch.End();
            this.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, camera.Transform);
            foreach (KeyValuePair<string, Node> keyValuePair in TechTree)
            {
                RootNode rootNode = keyValuePair.Value as RootNode;
                if (rootNode?.nodeState != NodeState.Press) continue;
                Vector2 vector21 = new Vector2(rootNode.RootRect.X + rootNode.RootRect.Width - 10
                    , rootNode.RootRect.Y + rootNode.RootRect.Height / 2);
                Vector2 vector22 = MainMenuOffset + new Vector2(ColumnOffset, 0.0f);
                vector22.Y = vector21.Y;
                vector22.X -= Vector2.Distance(vector21, vector22) / 2f;
                Technology technology = ResourceManager.TechTree[keyValuePair.Key];
                for (int index = 0; index < technology.LeadsTo.Count; ++index)
                {
                    Technology.LeadsToTech leadsToTech = technology.LeadsTo[index];
                    TechEntry techEntry = EmpireManager.Player.GetTDict()[leadsToTech.UID];
                    techEntry = techEntry.FindNextDiscoveredTech(EmpireManager.Player);
                    if (techEntry == null)
                    {
                       continue;
                    }
                    bool complete = techEntry.Unlocked;
                    var treeNode = SubNodes[techEntry.UID] as TreeNode;
                    if (treeNode == null) continue;
                    var vector23 = new Vector2(vector22.X, treeNode.BaseRect.Y + treeNode.BaseRect.Height / 2 - 10);
                    --vector23.Y;
                    if (vector23.Y > vector22.Y)
                        ScreenManager.SpriteBatch.DrawResearchLineVertical(vector22, vector23, complete);
                    else
                        ScreenManager.SpriteBatch.DrawResearchLineVertical(vector23, vector22, complete);
                }
            }
            foreach (KeyValuePair<string, Node> keyValuePair in this.SubNodes)
            {
                if (!(keyValuePair.Value is TreeNode)) continue;

                Vector2 vector21 = new Vector2((float)((keyValuePair.Value as TreeNode).BaseRect.X + (keyValuePair.Value as TreeNode).BaseRect.Width - 25), (float)((keyValuePair.Value as TreeNode).BaseRect.Y + (keyValuePair.Value as TreeNode).BaseRect.Height / 2 - 10));
                Vector2 vector22 = vector21 + new Vector2(ColumnOffset / 2f, 0.0f);
                vector22.Y = vector21.Y;
                Array<Technology.LeadsToTech> leadsTo = ResourceManager.TechTree[keyValuePair.Key].LeadsTo;
                for (int index = 0; index < leadsTo.Count; ++index)
                {
                    Technology.LeadsToTech leadsToTech1 = leadsTo[index];                    
                    TechEntry techEntry1 = EmpireManager.Player.GetTDict()[leadsToTech1.UID];
                    techEntry1 = techEntry1.FindNextDiscoveredTech(EmpireManager.Player);
                    if (techEntry1 == null)
                    {
                        continue;
                    }
                    

                    bool complete = techEntry1.Unlocked;
                    TreeNode treeNode1 = SubNodes[techEntry1.UID] as TreeNode;
                    var vector23 = new Vector2(vector22.X, treeNode1.BaseRect.Y + treeNode1.BaseRect.Height / 2 - 10);
                    if (vector23.Y > vector22.Y)
                        ScreenManager.SpriteBatch.DrawResearchLineVertical(vector22, vector23, complete);
                    else
                        ScreenManager.SpriteBatch.DrawResearchLineVertical(vector23, vector22, complete);
                }
            }
            foreach (KeyValuePair<string, Node> keyValuePair in this.SubNodes)
            {
                Technology technology2 = ResourceManager.TechTree[keyValuePair.Key];
                if (keyValuePair.Value is TreeNode && technology2.LeadsTo.Count > 0)
                {
                    TreeNode treeNode2 = (keyValuePair.Value as TreeNode);
                    Vector2 LeftPoint1 = new Vector2(treeNode2.BaseRect.X + treeNode2.BaseRect.Width - 25, treeNode2.BaseRect.Y + treeNode2.BaseRect.Height / 2 - 10);
                    Vector2 RightPoint1 = LeftPoint1 + new Vector2((float)(this.ColumnOffset / 2), 0.0f);
                    RightPoint1.Y = LeftPoint1.Y;
                    bool Complete1 = false;
                    for (int index = 0; index < technology2.LeadsTo.Count; ++index)
                    {
                        Technology.LeadsToTech leadsToTech2 = technology2.LeadsTo[index];
                        Technology technology3 = ResourceManager.TechTree[leadsToTech2.UID];
                        TechEntry techEntry2 = EmpireManager.Player.GetTDict()[leadsToTech2.UID];
                        techEntry2 = techEntry2.FindNextDiscoveredTech(EmpireManager.Player);
                        if (techEntry2 != null)                     
                        {
                            bool complete2 = false;
                            if (techEntry2.Unlocked)
                            {
                                complete2 = true;
                                Complete1 = true;
                            }
                            TreeNode treeNode3 = (this.SubNodes[techEntry2.UID] as TreeNode);
                            Vector2 LeftPoint2 = new Vector2(RightPoint1.X, (float)(treeNode3.BaseRect.Y + treeNode3.BaseRect.Height / 2 - 10));
                            Vector2 RightPoint2 = LeftPoint2 + new Vector2(Vector2.Distance(LeftPoint1, RightPoint1) + 13f, 0.0f);
                            this.ScreenManager.SpriteBatch.DrawResearchLineHorizontalGradient(LeftPoint2, RightPoint2, complete2);
                        }
                    }
                    this.ScreenManager.SpriteBatch.DrawResearchLineHorizontal(LeftPoint1, RightPoint1, Complete1);
                }
            }
            foreach (KeyValuePair<string, Node> keyValuePair in this.TechTree)
            {
                RootNode rootNode1 = (keyValuePair.Value as RootNode);
                if (rootNode1?.nodeState == NodeState.Press)
                {
                    Vector2 LeftPoint1 = new Vector2(rootNode1.RootRect.X + rootNode1.RootRect.Width - 10, rootNode1.RootRect.Y + rootNode1.RootRect.Height / 2);
                    Vector2 RightPoint1 = MainMenuOffset + new Vector2(ColumnOffset, 0.0f);
                    RightPoint1.Y = LeftPoint1.Y;
                    RightPoint1.X -= Vector2.Distance(LeftPoint1, RightPoint1) / 2f;
                    bool Complete1 = false;
                    Technology technology4 = ResourceManager.TechTree[keyValuePair.Key];
                    for (int index = 0; index < technology4.LeadsTo.Count; ++index)
                    {
                        Technology.LeadsToTech leadsToTech3 = technology4.LeadsTo[index];
                        TechEntry techEntry3 = EmpireManager.Player.GetTDict()[leadsToTech3.UID];
                        techEntry3 = techEntry3.FindNextDiscoveredTech(EmpireManager.Player);
                        if (techEntry3 != null)
                        {
                            bool Complete2 = false;
                            if (techEntry3.Unlocked)
                            {
                                Complete2 = true;
                                Complete1 = true;
                            }
                            TreeNode treeNode4 = (SubNodes[techEntry3.UID] as TreeNode);
                            Vector2 LeftPoint2 = new Vector2(RightPoint1.X, (float)(treeNode4.BaseRect.Y + treeNode4.BaseRect.Height / 2 - 10));
                            --LeftPoint2.Y;
                            Vector2 RightPoint2 = LeftPoint2 + new Vector2(Vector2.Distance(LeftPoint1, RightPoint1) + 13f, 0.0f);
                            ScreenManager.SpriteBatch.DrawResearchLineHorizontalGradient(LeftPoint2, RightPoint2, Complete2);
                        }
                    }
                    ScreenManager.SpriteBatch.DrawResearchLineHorizontal(LeftPoint1, RightPoint1, Complete1);
                }
            }
            foreach (KeyValuePair<string, Node> keyValuePair in TechTree)
            {
                if (keyValuePair.Value is RootNode rootNode)
                    rootNode.Draw(ScreenManager.SpriteBatch);
            }
            foreach (KeyValuePair<string, Node> keyValuePair in SubNodes)
            {
                if (keyValuePair.Value is TreeNode treeNode)
                    treeNode.Draw(ScreenManager);
            }
            ScreenManager.SpriteBatch.End();
            ScreenManager.SpriteBatch.Begin();
            MainMenu.DrawHollow();
            close.Draw(spriteBatch);
            qcomponent.Draw();
            ToolTip.Draw(ScreenManager.SpriteBatch);
            ScreenManager.SpriteBatch.End();
        }

        public override void ExitScreen()
        {
            GlobalStats.ResearchRootUIDToDisplay = this.UIDCurrentRoot;
            RightClicked = false;
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

        public override bool HandleInput(InputState input)
        {                
            if (input.MouseCurr.RightButton == ButtonState.Pressed && input.MousePrev.RightButton == ButtonState.Released)
            {
                this.StartDragPos = input.CursorPosition;
                this.cameraVelocity.X = 0f;
                this.cameraVelocity.Y = 0f;
            }
            if (input.MouseCurr.RightButton != ButtonState.Pressed || input.MousePrev.RightButton != ButtonState.Pressed || this.RightClicked) 
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
                camera.Pos = camera.Pos + new Vector2(-xDiff, -yDiff);
                this.StartDragPos = input.CursorPosition;
            }

            cameraVelocity = cameraVelocity.Clamped(-10f, 10f);
            int backBufferWidth = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth;
            int backBufferHeight = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight;
            camera.Pos = camera.Pos.Clamped(backBufferWidth / 2f, backBufferHeight / 2f, 3200f, 3200f);

            if (input.IsCtrlKeyDown && input.KeysCurr.IsKeyDown(Keys.F1) && input.KeysPrev.IsKeyUp(Keys.F1))
            {

                var playerTechs = EmpireManager.Player.GetTDict();
                foreach (var techEntry in playerTechs)
                {                    
                    techEntry.Value.Unlock(EmpireManager.Player);
                }

                //foreach (KeyValuePair<string, Technology> tech in ResourceManager.TechTree)
                //{
                //    UnlockTree(tech.Key);
                //    foreach(Technology.UnlockedMod unlockmod in tech.Value.ModulesUnlocked)
                //        EmpireManager.Player.UnlockModuleForEmpire(unlockmod.ModuleUID);
                //}
                //foreach (KeyValuePair<string, ShipData> hull in ResourceManager.HullsDict)
                //{
                //    EmpireManager.Player.GetHDict()[hull.Key] = true;
                //}

                //foreach (KeyValuePair<string, Building> building in ResourceManager.BuildingsDict)
                //{
                //    Building building1 = ResourceManager.BuildingsDict[building.Key];
                //    if (string.IsNullOrEmpty(building1.EventTriggerUID))
                //        EmpireManager.Player.GetBDict()[building.Key] = true;
                //}
                //EmpireManager.Player.UpdateShipsWeCanBuild();
            }
            //Added by McShooterz: new cheat to only unlock tech
            if (input.KeysCurr.IsKeyDown(Keys.RightControl) && input.KeysCurr.IsKeyDown(Keys.F2) && input.KeysPrev.IsKeyUp(Keys.F2))
            {
                foreach (KeyValuePair<string, Technology> tech in ResourceManager.TechTree)
                {
                    this.UnlockTree(tech.Key);
                }
                EmpireManager.Player.UpdateShipsWeCanBuild();
            }
            //Added by McShooterz: new cheat to only unlock tech
            if (input.KeysCurr.IsKeyDown(Keys.RightControl) && input.KeysCurr.IsKeyDown(Keys.F3) && input.KeysPrev.IsKeyUp(Keys.F3))
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
                return true;
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
                if (EmpireManager.Player.GetTechEntry(tech.Key).Unlocked)
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
                    var techToCheck = tech.Value.tech;
                    var techsToAdd = new Array<string>()
                    {
                        techToCheck.UID
                    };
                    if (techToCheck.Tech.RootNode != 1)
                    {
                        while (!techToCheck.Unlocked)
                        {
                            var preReq = techToCheck.GetPreReq(EmpireManager.Player);
                            if (preReq == null)
                            {
                                break;
                            }                            
                            if (!preReq.Unlocked )
                            {
                                techsToAdd.Add(preReq.UID);
                            }
                            techToCheck = preReq;
                        }
                    }
                    techsToAdd.Reverse();
                    foreach (string toAdd in techsToAdd)
                    {
                        var techEntry = EmpireManager.Player.GetTechEntry(toAdd);
                        if (!techEntry.Discovered)
                            continue;
                        qcomponent.AddToQueue(this.SubNodes[toAdd] as TreeNode);
                    }
                }
            }
            if (input.Escaped)
            {
                this.ExitScreen();
                return true;
            }
            if (input.ResearchExitScreen && !GlobalStats.TakingInput)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                this.ExitScreen();
                return true;
            }
            if (this.close.HandleInput(input))
            {
                this.ExitScreen();
                return true;
            }
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            camera              = new Camera2D();
            camera.Pos          = new Vector2(Viewport.Width / 2f, Viewport.Height / 2f);
            Rectangle main      = new Rectangle(0, 0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth
                , base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);
            this.MainMenu       = new Menu2(main);
            this.MainMenuOffset = new Vector2((float)(main.X + 20), (float)(main.Y + 30));
            this.close          = new CloseButton(this, new Rectangle(main.X + main.Width - 40, main.Y + 20, 20, 20));
            this.QueueContainer = new Rectangle(main.X + main.Width - 355, main.Y + 40, 330, main.Height - 100);
            this.qcomponent     = new QueueComponent(base.ScreenManager, this.QueueContainer, this);
            if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth < 1600)
            {
                qcomponent.SetInvisible();
            }
            int numRoots = 0;
            foreach (KeyValuePair<string, TechEntry> tech in EmpireManager.Player.GetTDict())
            {
                Technology technology = ResourceManager.TechTree[tech.Value.UID];
                var playerTech = EmpireManager.Player.GetTechEntry(technology.UID);
                if (technology.RootNode != 1)
                {
                    continue;
                }
                if (playerTech.Discovered)// !technology.Secret)
                {
                    numRoots++;
                }
                else
                {
                    TechEntry techEntry = EmpireManager.Player.GetTDict()[tech.Value.UID];
                    if (techEntry.Discovered)
                    {
                        numRoots++;
                    }
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
                Technology technology1 = ResourceManager.TechTree[tech.Value.UID];
                if (technology1.RootNode != 1)
                {
                    continue;
                }
                if (!tech.Value.Discovered)
                {
                    continue;
                }
                this.Cursor.X = 0f;
                this.Cursor.Y = (float)(this.FindDeepestY() + 1);
                this.SetNode(tech.Value);                
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
                this.CompleteSubNodeTree.TryGetValue(resTop, out var resTopNode);
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
            Technology technology = ResourceManager.TechTree[treenode.tech.UID];
            for (int i = 0; i < technology.LeadsTo.Count; i++)
            {
                TechEntry techEntry = EmpireManager.Player.GetTDict()[technology.LeadsTo[i].UID];
                 //!ResourceManager.TechTree[technology.LeadsTo[i].UID].Secret || 
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
                    TechEntry te = techEntry;
                    TreeNode newNode = new TreeNode(this.MainMenuOffset + new Vector2((float)(this.ColumnOffset * (int)this.Cursor.X), (float)(this.RowOffset * (int)this.Cursor.Y)), te, this)
                    {
                        NodePosition = this.Cursor
                    };
                    if (EmpireManager.Player.GetTDict()[te.UID].Unlocked)
                    {
                        newNode.complete = true;
                    }
                    if (techEntry.Discovered)
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
            Technology technology = ResourceManager.TechTree[Root.tech.UID];            
            for (int i = 0; i < technology.LeadsTo.Count; i++)
            {
                TechEntry techEntry = EmpireManager.Player.GetTDict()[technology.LeadsTo[i].UID];
                //!ResourceManager.TechTree[technology.LeadsTo[i].UID].Secret || 

                TechEntry te = techEntry;
                TreeNode newNode =
                    new TreeNode(
                        this.MainMenuOffset + new Vector2((float) (this.ColumnOffset * (int) this.Cursor.X),
                            (float) (this.RowOffset * (int) this.Cursor.Y)), te, this)
                    {
                        NodePosition = this.Cursor
                    };
                if (EmpireManager.Player.GetTDict()[te.UID].Unlocked)
                {
                    newNode.complete = true;
                }
                if (techEntry.Discovered)
                    this.CompleteSubNodeTree.Add(newNode.tech.UID, newNode);
                this.PopulateAllTechs(newNode);
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
            Technology technology = ResourceManager.TechTree[Root.tech.UID];
            for (int i = 0; i < technology.LeadsTo.Count; i++)
            {
                TechEntry techEntry = EmpireManager.Player.GetTDict()[technology.LeadsTo[i].UID].FindNextDiscoveredTech(EmpireManager.Player);
                if (techEntry == null) continue;
                //!ResourceManager.TechTree[technology.LeadsTo[i].UID].Secret || 
                {
                    if (!first)
                    {
                        Cursor.Y = FindDeepestYSubNodes() + 1;
                    }
                    else
                    {
                        this.Cursor.Y = FindDeepestYSubNodes();
                        first = false;
                    }
                    this.Cursor.X = Root.NodePosition.X + 1f;
                    TechEntry te = techEntry;
                    TreeNode newNode = new TreeNode(MainMenuOffset + new Vector2(ColumnOffset * (int)Cursor.X, 
                        RowOffset * (int)Cursor.Y), te, this)
                    {
                        NodePosition = Cursor
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
            Vector2 Position = new Vector2(Cursor.X, Cursor.Y);
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
                Cursor.Y = this.Cursor.Y + 1f;
            }
            else if (treenode.tech.Discovered)
            {
                ClaimedSpots.Add(Position);
            }

            Technology technology = ResourceManager.TechTree[treenode.tech.UID];
            for (int i = 0; i < technology.LeadsTo.Count; i++)
            {
                TechEntry techEntry = EmpireManager.Player.GetTDict()[technology.LeadsTo[i].UID];
                 //!ResourceManager.TechTree[technology.LeadsTo[i].UID].Secret ||
                {
                    if (i != 0)
                    {
                        Cursor.Y = FindDeepestYSubNodes() + 1;
                    }
                    else
                    {
                        Cursor.Y = FindDeepestYSubNodes();
                    }
                    Cursor.X = treenode.NodePosition.X + 1f;
                    TechEntry te = techEntry;
                    float x = ColumnOffset * (int)Cursor.X;
                    float y = RowOffset * (int)Cursor.Y;
                    TreeNode newNode = new TreeNode(MainMenuOffset + new Vector2(x, y), te, this)
                    {
                        NodePosition = Cursor
                    };
                    if (EmpireManager.Player.GetTDict()[te.UID].Unlocked)
                    {
                        newNode.complete = true;
                    }
                    if (techEntry.Discovered)
                        SubNodes.Add(newNode.tech.UID, newNode);
                    PopulateNodesFromSubNode(newNode);
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
            this.UnlocksSubMenu = new Submenu(this.DetailPopUpRect);
            this.UnlockSL = new ScrollList(this.UnlocksSubMenu, 96)
            {
                indexAtTop = 0
            };
            this.DetailUnlocks.Clear();
            TechEntry techEntry = EmpireManager.Player.GetTDict()[techUID];
            foreach (Technology.UnlockedMod unlockMod in unlockedTech.ModulesUnlocked)
            {
                if (EmpireManager.Player.data.Traits.ShipType == unlockMod.Type || unlockMod.Type == null || unlockMod.Type == techEntry.AcquiredFrom)
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
                if (troop.Type == EmpireManager.Player.data.Traits.ShipType || troop.Type == null || troop.Type == "ALL" || troop.Type == techEntry.AcquiredFrom)
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
                if (EmpireManager.Player.data.Traits.ShipType == hull.ShipType || hull.ShipType == null || hull.ShipType == techEntry.AcquiredFrom)
                {
                    ShipData shipData = ResourceManager.HullsDict[hull.Name];
                    UnlockItem unlock = new UnlockItem
                    {
                        Type = "HULL",
                        privateName = hull.Name,
                        HullUnlocked = shipData.Name,
                        Description = string.Concat(Localizer.Token(4042), " ",
                            Localizer.GetRole(shipData.Role, EmpireManager.Player))
                    };
                    this.UnlockSL.AddItem(unlock);
                }
            }
            foreach (Technology.UnlockedBuilding unlockedB in unlockedTech.BuildingsUnlocked)
            {
                if (EmpireManager.Player.data.Traits.ShipType == unlockedB.Type || unlockedB.Type == null || unlockedB.Type == techEntry.AcquiredFrom)
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
                if (EmpireManager.Player.data.Traits.ShipType == ub.Type || ub.Type == null || ub.Type == techEntry.AcquiredFrom)
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
            var techd      = ResourceManager.TechTree[key];
            var playerDict = EmpireManager.Player.GetTDict()[key];
            if (!playerDict.Discovered || !EmpireManager.Player.HavePreReq(key)) return;

            /* Fat Bastard: The line below was missing the "if" statement. without the if to check if the tech is unlocked already,
                it will unlock bonus techs again and again, giving you massive amount of bonuses when you press the Ctrl F2.
                I added the same if to the UnlockTreeNoBonus method below 
                This code is called only when cheating and not in a regular game */

            if (!playerDict.Unlocked) EmpireManager.Player.UnlockTech(key);
            foreach (Technology.LeadsToTech tech in techd.LeadsTo)
                UnlockTree(tech.UID);
        }

        private void UnlockTreeNoBonus(string key)
        {
            Technology technology2 = ResourceManager.TechTree[key];
            Technology technology  = technology2;
            Technology technology1 = technology2;
            var playerDict = EmpireManager.Player.GetTDict()[key];
            if (!playerDict.Discovered || !EmpireManager.Player.HavePreReq(key)) return;
            if(technology2.BonusUnlocked.Count == 0)
                if (!playerDict.Unlocked)  EmpireManager.Player.UnlockTech(key);
            foreach (Technology.LeadsToTech tech in technology2.LeadsTo)
                UnlockTreeNoBonus(tech.UID);
        }

        //Added by McShooterz: find size of tech tree before it is built
        private static int CalculateTreeDemensionsFromRoot(string UID, ref int rows, int cols, int colmax)
        {
            int max = 0;
            int rowCount = 0;
            cols++;
            if (cols > colmax)
                colmax = cols;
            Technology technology = ResourceManager.TechTree[UID];
            if (technology.LeadsTo.Count > 1)
            {
                foreach (Technology.LeadsToTech tech in technology.LeadsTo)
                {
                    TechEntry techEntry1 = EmpireManager.Player.GetTDict()[tech.UID];
                    if (!techEntry1.Discovered) //techEntry1.Tech.Secret &&
                    {
                        continue;
                    }
                    rowCount++;
                }
                if(rowCount > 1)
                    rows += rowCount - 1;
            }
            foreach (Technology.LeadsToTech tech in technology.LeadsTo)
            {
                TechEntry techEntry = EmpireManager.Player.GetTDict()[tech.UID];
                if (!techEntry.Discovered) //techEntry.Tech.Secret && 
                {
                    CalculateTreeDemensionsFromRoot(tech.UID, ref rows, cols, colmax);
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
            var tech = qcomponent?.CurrentResearch;
            if (tech != null && tech.Node.tech.Progress >= tech.Node.tech.TechCost)
                qcomponent.CurrentResearch.Node.complete = true;
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