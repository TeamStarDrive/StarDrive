using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDGraphics;
using Ship_Game.GameScreens.NewGame;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public class SelectOpponentListItem : ScrollListItem<SelectOpponentListItem>
    {
        public SelectOpponentsScreen Screen;
        public IEmpireData EmpireData;
        public SubTexture Portrait;

        public SelectOpponentListItem(SelectOpponentsScreen screen, IEmpireData empireData)
        {
            Screen = screen;
            EmpireData = empireData;
            Portrait = ResourceManager.Texture("Races/" + empireData.VideoPath);
        }

        bool Chosen => Screen.Params.SelectedOpponents.Contains(EmpireData);

        Rectangle FlagRect => new((int)Right - 120, (int)CenterY - 40, 80, 80);
        static Rectangle LeftArrow(in Rectangle flag) => new(flag.X - 20, flag.CenterY() - 10, 20, 20);
        static Rectangle RightArrow(in Rectangle flag) => new(flag.Right, flag.CenterY() - 10, 20, 20);

        public override bool HandleInput(InputState input)
        {
            if (Chosen && input.LeftMouseClick)
            {
                Rectangle flag = FlagRect;
                if (flag.HitTest(input.CursorPosition))
                {
                    Screen.PickColor(EmpireData);
                    return true;
                }
                if (LeftArrow(flag).HitTest(input.CursorPosition))
                {
                    Screen.StepFlag(EmpireData, -1);
                    return true;
                }
                if (RightArrow(flag).HitTest(input.CursorPosition))
                {
                    Screen.StepFlag(EmpireData, +1);
                    return true;
                }
            }
            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);

            int height = (int)Height;
            int width = (int)Portrait.GetWidthFromHeightAspect(height);
            var portrait = new Rectangle((int)X +10, (int)Y, width, height);
            bool selected = Chosen;
            float alpha = selected ? 1f : 0.3f;
            batch.Draw(Portrait, portrait, Color.White.Alpha(alpha));
            if (!selected)
                return;

            (Color color, int flagIndex) = Screen.LookOf(EmpireData);
            batch.DrawRectangle(portrait, color, thickness: 2);
            Rectangle flag = FlagRect;
            batch.Draw(ResourceManager.Flag(flagIndex), flag, color);
            batch.Draw(ResourceManager.Texture("UI/leftArrow"), LeftArrow(flag), Color.BurlyWood);
            batch.Draw(ResourceManager.Texture("UI/rightArrow"), RightArrow(flag), Color.BurlyWood);
        }
    }
}
