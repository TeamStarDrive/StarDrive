using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using System;
using Ship_Game.GameScreens.Universe.Debug;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using System.Linq;

namespace Ship_Game
{
    public sealed class ResearchScreenNew : GameScreen
    {
        public readonly UniverseScreen Universe;
        public readonly Empire Player;
        public Camera2D camera = new();

        readonly Map<string, RootNode> RootNodes = new(StringComparer.OrdinalIgnoreCase);
        public Map<string, TreeNode> SubNodes = new(StringComparer.OrdinalIgnoreCase);

        CloseButton Close;
        UIButton Search;
        Menu2 MainMenu;
        public EmpireUIOverlay empireUI;

        Vector2 MainMenuOffset;

        public ResearchQueueUIComponent Queue;

        int GridWidth  = 175;
        int GridHeight = 100;

        readonly Array<Vector2> ClaimedSpots = new();

        ResearchDebugUnlocks DebugUnlocks;

        public Color ApplyCurrentAlphaColor(Color color)
        {
            color = ApplyCurrentAlphaToColor(color);
            return new Color(color, color.A.LowerBound(100));
        }

        public ResearchScreenNew(GameScreen parent, UniverseScreen u, EmpireUIOverlay empireUi)
            : base(parent, toPause: u)
        {
            Universe = u;
            Player = u.Player;
            empireUI = empireUi;
            IsPopup = false;
            CanEscapeFromScreen = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        public override void LoadContent()
        {
            camera = new Camera2D { Pos = new Vector2(Viewport.Width, Viewport.Height) / 2f };
            var main = new Rectangle(0, 0, ScreenWidth, ScreenHeight);
            MainMenu = new Menu2(main);
            MainMenuOffset = new Vector2(main.X + 20, main.Y + 30);
            Close = Add(new CloseButton(main.Right - 40, main.Y + 20));

            RootNodes.Clear();
            SubNodes.Clear();

            int numDiscoveredRoots = Player.TechEntries.Count(t => t.IsRoot && t.Discovered);

            GridHeight = (main.Height - 40) / Math.Max(1, numDiscoveredRoots);
            MainMenuOffset.Y = main.Y + 12 + GridHeight / 3;
            if (ScreenHeight <= 720)
            {
                MainMenuOffset.Y += 8f;
            }

            Vector2 nodePos = Vector2.Zero;

            var rootTechs = Player.TechEntries.Filter(t => t.IsRoot && t.Discovered);
            // sort the techs
            rootTechs = rootTechs.Sorted(t => t.Tech.RootNode);

            foreach (TechEntry tech in rootTechs)
            {
                nodePos.X = 0f;
                nodePos.Y = FindDeepestY() + 1;
                SetRootNode(tech, ref nodePos);
            }

            GridHeight = (main.Height - 40) / 6;

            if (!RootNodes.TryGetValue(Universe.UState.ResearchRootUIDToDisplay, out RootNode root))
                root = RootNodes.Values.FirstOrDefault() ?? throw new("ResearchScreen has no RootNodes");

            PopulateNodesFromRoot(root);

            // Create queue once all techs are populated
            var queue = new Rectangle(main.X + main.Width - 355, main.Y + 40, 330, main.Height - 100);
            Queue = Add(new ResearchQueueUIComponent(this, queue));
            Vector2 searchPos = new(main.X + main.Width - 360, main.Height - 55);
            Search = Add(new UIButton(ButtonStyle.BigDip, searchPos, "Search"));
            Search.OnClick = OnSearchButtonClicked;

            DebugUnlocks = Add(new ResearchDebugUnlocks(Universe, () =>
            {
                Universe.UState.ResearchRootUIDToDisplay = GetCurrentlySelectedRootNode().Entry.UID;
                ReloadContent();
            }));
            DebugUnlocks.AxisAlign = Align.BottomRight;
            DebugUnlocks.SetLocalPos(-Queue.Width - 50, -25);

            base.LoadContent();
        }

        public override void Update(float fixedDeltaTime)
        {
            DebugUnlocks.Visible = Universe.Debug || Universe is DeveloperUniverse;
            base.Update(fixedDeltaTime);
        }

        public void OnSearchButtonClicked(UIButton button)
        {
            ScreenManager.AddScreen(new SearchTechScreen(this));
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);

            batch.SafeBegin();
            batch.FillRectangle(new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.Black);
            MainMenu.Draw(batch, elapsed);
            batch.SafeEnd();

            batch.SafeBegin(SpriteBlendMode.AlphaBlend, sortImmediate:false, saveState:false, camera.Transform);
            {
                DrawConnectingLines(batch);

                foreach (RootNode rootNode in RootNodes.Values)
                {
                    rootNode.Draw(batch);
                }

                foreach (TreeNode treeNode in SubNodes.Values)
                {
                    treeNode.Draw(batch);
                }
            }
            batch.SafeEnd();

            batch.SafeBegin();
            base.Draw(batch, elapsed);
            batch.SafeEnd();
        }

        static Vector2 CenterBetweenPoints(Vector2 left, Vector2 right)
        {
            return left.LerpTo(right, 0.5f).Rounded();
        }

        RootNode GetCurrentlySelectedRootNode()
        {
            foreach (RootNode root in RootNodes.Values)
                if (root.nodeState == NodeState.Press)
                    return root;
            return null;
        }

        // the center-right connector point of the parent node
        Vector2 GetParentConnectorPoint(Node parent)
        {
            return (parent is RootNode root) ? root.RightPoint : ((TreeNode)parent).RightPoint;
        }

        Vector2 GetBranchMidPoint(Node parent)
        {
            Vector2 parentNode = GetParentConnectorPoint(parent);

            // for the ROOT nodes, the midpoint is a bit closer
            if (parent is RootNode)
                return new(parentNode.X + (int)(GridWidth / 3), parentNode.Y);
            return new(parentNode.X + (int)(GridWidth / 2), parentNode.Y);
        }

        void DrawLinesFromParentToChild(SpriteBatch batch, Node parent, TechEntry child)
        {
            if (SubNodes.TryGetValue(child.UID, out TreeNode node))
            {
                Vector2 branchMidPoint = GetBranchMidPoint(parent);
                Vector2 verticalEnd = new(branchMidPoint.X, node.BaseRect.CenterY - 10);
                Vector2 endPos = new(node.BaseRect.X + 13f, verticalEnd.Y);

                // draw the vertical line which connects us from branch middle junction towards the child tech
                DrawResearchLineVertical(batch, branchMidPoint, verticalEnd, child.Unlocked);

                // draw the final horizontal connection from middle junction to endPos
                DrawResearchLineHorizontal(batch, verticalEnd, endPos, child.Unlocked, gradient: true);
            }
        }

        void DrawLineFromParentToBranchMiddle(SpriteBatch batch, Node parent, bool anyTechsComplete)
        {
            // from parent node to the middle of the branch junction
            Vector2 parentNode = GetParentConnectorPoint(parent);
            Vector2 branchMidPoint = GetBranchMidPoint(parent);
            DrawResearchLineHorizontal(batch, parentNode, branchMidPoint, anyTechsComplete, gradient:false);
        }

        void DrawConnectingLinesFromParentToChildren(SpriteBatch batch, Node parent)
        {
            bool anyTechsComplete = false;
            bool discoveredAny = false;
            foreach (TechEntry maybeUndiscovered in parent.Entry.Children)
            {
                // scan from `maybeUndiscovered` (inclusive) until we find a discovered tech
                // this would skip over any secret techs in the middle 
                TechEntry toTech = maybeUndiscovered.FindNextDiscoveredTech(Player);
                if (toTech != null)
                {
                    discoveredAny = true;
                    anyTechsComplete |= toTech.Unlocked;
                    DrawLinesFromParentToChild(batch, parent, toTech);
                }
            }

            // from parent tech to the middle of the branch junction
            if (discoveredAny)
            {
                DrawLineFromParentToBranchMiddle(batch, parent, anyTechsComplete);
            }
        }

        void DrawConnectingLines(SpriteBatch batch)
        {
            RootNode root = GetCurrentlySelectedRootNode();

            DrawConnectingLinesFromParentToChildren(batch, root);
            foreach (TreeNode from in SubNodes.Values)
            {
                DrawConnectingLinesFromParentToChildren(batch, from);
            }
        }

        static void DrawResearchLineHorizontal(SpriteBatch batch, Vector2 left, Vector2 right, bool complete, bool gradient)
        {
            if (left.X > right.X) // top must have lower X
                Vectors.Swap(ref left, ref right);

            SubTexture texture;
            if (gradient)
            {
                texture = ResourceManager.Texture(complete
                        ? "ResearchMenu/grid_horiz_gradient_complete"
                        : "ResearchMenu/grid_horiz_gradient");
            }
            else
            {
                texture = ResourceManager.Texture(complete
                        ? "ResearchMenu/grid_horiz_complete"
                        : "ResearchMenu/grid_horiz");
            }

            RectF r = new(left.X + 5, left.Y - 2, (right.X - left.X) - 5, 5);
            //batch.Draw(texture, r, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1f);
            batch.Draw(texture, r, Color.White);

            // fill a small rectangle at the beginning of the research line
            // to cover up some stupid artifacts caused by XNA transparent sprite renderer
            batch.FillRectangle(new(left.X, left.Y, 5, 1), (complete ? new(110, 171, 227) : new(194, 194, 194)));
        }

        static void DrawResearchLineVertical(SpriteBatch batch, Vector2 top, Vector2 bottom, bool complete)
        {
            if (top.Y > bottom.Y) // top must have lower Y
                Vectors.Swap(ref top, ref bottom);

            SubTexture texture = ResourceManager.Texture(complete
                               ? "ResearchMenu/grid_vert_complete"
                               : "ResearchMenu/grid_vert");

            // shift the line down a bit to avoid overlapping transparency artifacts
            int offsetY = 1;
            RectF r = new(top.X - texture.CenterX, top.Y + offsetY, texture.Width, (bottom.Y - top.Y) - offsetY);
            //batch.Draw(texture, r, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 1f);
            batch.Draw(texture, r, Color.White);
        }


        public override void ExitScreen()
        {
            Universe.UState.ResearchRootUIDToDisplay = GetCurrentlySelectedRootNode().Entry.UID;
            base.ExitScreen();
        }

        int FindDeepestY()
        {
            int deepest = 0;
            foreach (RootNode root in RootNodes.Values)
                if (root.NodePosition.Y > deepest)
                    deepest = (int) root.NodePosition.Y;
            return deepest;
        }

        int FindDeepestYSubNodes()
        {
            int deepest = 0;
            foreach (TreeNode node in SubNodes.Values)
                if (node.NodePosition.Y > deepest)
                    deepest = (int)node.NodePosition.Y;
            return deepest;
        }

        public override bool HandleInput(InputState input)
        {
            if (input.ResearchExitScreen || input.RightMouseClick)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }

            if (input.MiddleMouseHeld())
                camera.MoveClamped(input.CursorVelocity, ScreenCenter, new Vector2(3200));

            foreach (RootNode root in RootNodes.Values)
            {
                if (root.HandleInput(input,camera))
                {
                    GameAudio.ResearchSelect();
                    PopulateNodesFromRoot(root);
                    return true;
                }
            }

            foreach (TreeNode node in SubNodes.Values)
            {
                if (node.HandleInput(input, ScreenManager, camera, Universe))
                {
                    if (input.LeftMouseClick && !input.RightMouseClick)
                    {
                        OnTechNodeClicked(node.Entry);
                    }
                    return true; // input captured
                }
            }

            return base.HandleInput(input);
        }

        void OnTechNodeClicked(TechEntry tech)
        {
            if (!tech.CanBeResearched)
            {
                // this tech cannot be researched
                GameAudio.NegativeClick();
                return;
            }
            
            bool added = false;
            
            if (!Player.Research.IsQueued(tech.UID))
            {
                GameAudio.ResearchSelect();
                Player.Research.AddTechToQueue(tech.UID);
                added = true;
            }
            
            // if ctrl is held down, move tech to top of queue ALWAYS, even if it was already in queue (imo good UX)
            if(GameBase.ScreenManager.input != null && GameBase.ScreenManager.input.IsCtrlKeyDown)
            {
                int index = Player.Research.IndexInQueue(tech.UID);
                int moved = Player.Research.MoveToTopWithPreReqs(index);
                if (moved == 0)
                {
                    GameAudio.NegativeClick();
                }
            }
            
            // if CTRL was not held down, and tech is in queue (but not added right now), remove it
            else
            {
                if (!added)
                {
                    Player.Research.RemoveTechFromQueue(tech.UID);
                }
            }
            
            Queue.ReloadResearchQueue();
        }

        Vector2 GridSize => new(GridWidth, GridHeight);

        Vector2 GetCurrentCursorOffset(in Vector2 cursorPos, float yOffset = 0)
        {
            var cursor = new Vector2(cursorPos.X, cursorPos.Y + yOffset);
            return (MainMenuOffset + cursor*GridSize).Rounded();
        }

        void PopulateNodesFromRoot(RootNode root)
        {
            foreach (RootNode node in RootNodes.Values)
                node.nodeState = (node == root) ? NodeState.Press : NodeState.Normal;

            SubNodes.Clear();
            ClaimedSpots.Clear();

            int rows = 1;
            int cols = CalculateTreeDimensionsFromRoot(root.Entry, ref rows, 0, 0);
            if (rows < 9) GridHeight = (MainMenu.Menu.Height - 40) / rows;
            else          GridHeight = (MainMenu.Menu.Height - 40) / 9;

            if (cols > 0 && cols < 9) GridWidth = (MainMenu.Menu.Width - 350) / cols;
            else                      GridWidth = 165;

            var nodePos = new Vector2(1f, 1f);
            bool first = true;

            foreach (TechEntry child in root.Entry.Children)
            {
                if (!child.Discovered)
                    continue;

                nodePos.X = root.NodePosition.X + 1f;
                nodePos.Y = FindDeepestYSubNodes() + (first ? 0 : 1);
                if (first) first = false;

                if (!SubNodes.ContainsKey(child.UID)) // only ever add unique entries
                {
                    var newNode = new TreeNode(GetCurrentCursorOffset(nodePos), child, this) { NodePosition = nodePos };
                    SubNodes[newNode.Entry.UID] = newNode;
                    PopulateNodesFromSubNode(newNode, ref nodePos);
                }
            }
        }

        void PopulateNodesFromSubNode(Node node, ref Vector2 nodePos)
        {
            UpdateCursorAndClaimedSpots(ref nodePos, node.Entry.Discovered);

            bool first = true;
            foreach (TechEntry child in node.Entry.Children)
            {
                nodePos.X = node.NodePosition.X + 1f;
                nodePos.Y = FindDeepestYSubNodes() + (first ? 0 : 1);
                if (first) first = false;

                if (child.Discovered && !SubNodes.ContainsKey(child.UID))
                {
                    var newNode = new TreeNode(GetCurrentCursorOffset(nodePos), child, this) { NodePosition = nodePos };
                    SubNodes[newNode.Entry.UID] = newNode;
                    PopulateNodesFromSubNode(newNode, ref nodePos);
                }
            }
        }

        void SetRootNode(TechEntry tech, ref Vector2 nodePos)
        {
            UpdateCursorAndClaimedSpots(ref nodePos, true);

            RootNodes[tech.UID] = new RootNode(GetCurrentCursorOffset(nodePos, -1), tech)
            {
                NodePosition = nodePos,
                isResearched = tech.Unlocked
            };
        }
        
        void UpdateCursorAndClaimedSpots(ref Vector2 nodePos, bool addToClaimed)
        {
            if (PositionIsClaimed(nodePos))
                nodePos.Y += 1f;
            else if (addToClaimed)
                ClaimedSpots.Add(nodePos);
        }
        
        bool PositionIsClaimed(Vector2 position) => ClaimedSpots.Any(p => p.AlmostEqual(position));

        //Added by McShooterz: find size of tech tree before it is built
        int CalculateTreeDimensionsFromRoot(TechEntry techEntry, ref int rows, int cols, int colmax)
        {
            cols++;
            if (cols > colmax)
                colmax = cols;

            TechEntry[] children = techEntry.Children;

            // look for branches and make space for them
            if (children.Length > 0)
            {
                int rowCount = 0;
                // don't count the main branch. use the branch that starts here.
                for (int i = 1; i < children.Length; i++)
                {
                    var discovered = children[i].FindNextDiscoveredTech(Player);
                    if (discovered != null)
                        rowCount++;
                }
                rows += rowCount;
            }

            foreach (TechEntry maybeUndiscovered in children)
            {
                // TODO: not sure why this pattern is used here?
                // scan from `maybeUndiscovered` (inclusive) until we find a discovered tech
                var discovered = maybeUndiscovered.FindNextDiscoveredTech(Player);
                if (discovered != null)
                {
                    int max = CalculateTreeDimensionsFromRoot(discovered, ref rows, cols, colmax);
                    if (max > colmax)
                        colmax = max;
                }
                else
                {
                    CalculateTreeDimensionsFromRoot(maybeUndiscovered, ref rows, cols, colmax);
                }
            }
            return colmax;
        }
    }
}