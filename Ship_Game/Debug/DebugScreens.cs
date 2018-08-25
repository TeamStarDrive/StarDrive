using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Debug
{
    public class DebugPage : UIElementContainer
    {

        public DebugPage(GameScreen parent, DebugModes mode) : base(parent, parent.Rect)
        {
            DebugMode = mode;
        }
        protected Array<UILabel> DebugText;
        public void HideAllDebugText()
        {
            if (DebugText == null) return;
            for (int i = 0; i < DebugText.Count; i++)
            {
                var column = DebugText[i];
                column.Hide();
            }
        }
        public void HideDebugGameInfo(int column)
        {
            DebugText?[column].Hide();
        }
        public DebugModes DebugMode { get; private set; }

        public void ShowDebugGameInfo(int column, DebugTextBlock lines, float x, float y)
        {
            if (DebugText == null)
                DebugText = new Array<UILabel>();

            if (DebugText.Count <= column)
                DebugText.Add(Label(x, y, ""));


            DebugText[column].Show();
            DebugText[column].MultilineText = lines.GetFormattedLines();

        }
        public virtual void Update(float deltaTime, DebugModes mode)
        {
            if (mode != DebugMode) return;
            base.Update(deltaTime);
        }
    }
    public class TradeDebug : DebugPage
    {
        private UniverseScreen Screen;
        private DebugInfoScreen Parent;
        private Rectangle DrawArea;
        public TradeDebug(UniverseScreen screen, DebugInfoScreen parent) : base(parent, DebugModes.Trade)
        {
            Screen = screen;
            Parent = parent;
            DrawArea = parent.Rect;
        }

        public override void Update(float deltaTime, DebugModes mode)
        {

            Planet planet = Screen.SelectedPlanet;

            Array<DebugTextBlock> text;
            if (planet?.Owner == null)
            {
                text = new Array<DebugTextBlock>();
                foreach (Empire empire in EmpireManager.Empires)
                {
                    if (empire.isFaction || empire.data.Defeated) continue;

                    var block = empire.DebugEmpireTradeInfo();
                    block.Header = empire.Name;
                    block.HeaderColor = empire.EmpireColor;

                    text.Add(block);

                }
                for (int i = 0; i < text.Count; i++)
                {
                    var lines = text[i];
                    ShowDebugGameInfo(i, lines, Rect.X + 10 + 300 * i, Rect.Y + 250);
                }
                return;
            }


            HideAllDebugText();

            text = planet?.TradeAI?.DebugText();
            if (text == null)
                return;
            if (text?.IsEmpty == true) return;
            for (int i = 0; i < text.Count; i++)
            {
                DebugTextBlock lines = text[i];
                ShowDebugGameInfo(i, lines, Rect.X + 10 + 300 * i, Rect.Y + 250);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible)
            {
                base.Draw(spriteBatch);
                return;
            }
            Planet planet = Screen.SelectedPlanet;
            int totalFreighters = 0;
            foreach (Empire e in EmpireManager.Empires)
            {
                foreach (Ship ship in e.GetShips())
                {
                    if (ship?.Active != true) continue;
                    ShipAI ai = ship.AI;
                    if (ai.State != AIState.SystemTrader) continue;
                    if (ai.OrderQueue.Count == 0) continue;

                    switch (ai.OrderQueue.PeekLast.Plan)
                    {
                        case ShipAI.Plan.DropOffGoods:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, ai.IsFood ? Color.GreenYellow : Color.SteelBlue, 6);
                            if (planet == ship.AI.end) totalFreighters++;

                            break;
                        case ShipAI.Plan.PickupGoods:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, ai.IsFood ? Color.GreenYellow : Color.SteelBlue, 3);
                            break;
                        case ShipAI.Plan.PickupPassengers:
                        case ShipAI.Plan.DropoffPassengers:
                            Screen.DrawCircleProjectedZ(ship.Center, 50f, e.EmpireColor, 32);
                            break;
                    }
                }

            }

            base.Draw(spriteBatch);
        }

    }
    public class PathDebug : DebugPage
    {
        private UniverseScreen Screen;
        private DebugInfoScreen Parent;
        private Rectangle DrawArea;
        private int EmpireID =1;
        public PathDebug(UniverseScreen screen, DebugInfoScreen parent) : base(parent, DebugModes.Pathing)
        {
            Screen = screen;
            Parent = parent;
            DrawArea = parent.Rect;
            if (DebugText == null)
                DebugText = new Array<UILabel>();
            if (DebugText.Count <= 1)
                DebugText.Add(Label(Rect.X, Rect.Y + 300, $""));
        }


        public override void Update(float deltaTime, DebugModes mode)
        {

            base.Update(deltaTime);
        }

        public override void Draw(SpriteBatch batch)
        {
            if (!Visible)
            {
                base.Draw(batch);
                return;
            }

            PathingInfo();
            base.Draw(batch);
        }
        public override bool HandleInput(InputState input)
        {
            if (input.ArrowUp)
                ChangeEmpireID(true);

            if (input.ArrowDown)
                ChangeEmpireID(false);
            return base.HandleInput(input);
        }

        private void ChangeEmpireID(bool increase)
        {
            EmpireID = EmpireID + (increase ? 1 : -1);
            if (EmpireID > EmpireManager.NumEmpires)
                EmpireID = 1;
            if (EmpireID < 1)
                EmpireID = EmpireManager.NumEmpires;

            var e = EmpireManager.GetEmpireById(EmpireID);

            DebugText[0].Text = $"Empire: {e.Name}";
            DebugText[0].Color = e.EmpireColor;
        }

        public void PathingInfo()
        {
            var e = EmpireManager.GetEmpireById(EmpireID);
            for (int x = 0; x < e.grid.GetLength(0); x++)
            for (int y = 0; y < e.grid.GetLength(1); y++)
            {
                var weight = e.grid[x, y];
                if (weight == 80)
                    continue;
                var translated = new Vector2((x - e.granularity) * Screen.PathMapReducer, (y - e.granularity) * Screen.PathMapReducer);
                Screen.DrawCircleProjected(translated, Screen.PathMapReducer, weight + 3, e.EmpireColor);
            }
        }
    }
}

