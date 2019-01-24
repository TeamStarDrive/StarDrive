using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class PieMenu
    {
        private Transition t;

        private int selectionIndex = -1;

        private Action hideDelegate;

        private Action newMenuDelegate;

        private PieMenuNode newMenuNode;

        public Vector2 Position { get; set; }

        public float Radius { get; set; } = 100f;

        public PieMenuNode RootNode { get; set; }

        public float ScaleFactor { get; set; }

        public bool Visible { get; set; }

        public PieMenu()
        {
            t = new Transition(Direction.Ascending, TransitionCurve.Linear, 0.15f);
            hideDelegate = OnHide;
            newMenuDelegate = NewMenu;
        }

        public void ChangeTo(PieMenuNode newNode)
        {
            if (newNode == null)
            {
                t.OnTransitionEnd = hideDelegate;
                t.Reset(Direction.Descending);
                return;
            }
            t.OnTransitionEnd = newMenuDelegate;
            newMenuNode = newNode;
            t.Reset(Direction.Descending);
        }

        private void ComputeSelected(Vector2 selectionVector)
        {
            selectionIndex = -1;
            if (selectionVector.Length() > 3f)
            {
                selectionIndex = -2;
                return;
            }
            if (selectionVector.Length() > 1.5f)
            {
                return;
            }
            if (selectionVector.Length() > 0.3f)
            {
                float angleDivision = 1f / RootNode.Children.Count;
                float angle = (float)Math.Atan2(selectionVector.Y, selectionVector.X);
                if (angle < 0f)
                {
                    angle = angle + 6.28318548f;
                }
                angle = angle / 6.28318548f;
                angle = 1f - angle;
                float rotationBegins = 0.75f - angleDivision / 2f;
                if (angle <= rotationBegins)
                {
                    angle = angle + 1f;
                }
                angle = angle - rotationBegins;
                selectionIndex = 0;
                while (selectionIndex * angleDivision < angle)
                {
                    PieMenu pieMenu = this;
                    pieMenu.selectionIndex = pieMenu.selectionIndex + 1;
                }
                PieMenu pieMenu1 = this;
                pieMenu1.selectionIndex = pieMenu1.selectionIndex - 1;
            }
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            if (!Visible)
            {
                return;
            }
            Vector2 center = Position;
            float scale = t.CurrentPosition * ScaleFactor;
            float currentAngle = 1.57079637f;
            float angleIncrement = 6.28318548f / RootNode.Children.Count;
            for (int i = 0; i < RootNode.Children.Count; i++)
            {
                Vector2 imagePos = center + (scale * Radius * new Vector2((float)Math.Cos(currentAngle), -(float)Math.Sin(currentAngle)));
                int imageSize = (int)(scale * 30f);
                Color drawColor = Color.White;
                if (currentAngle <= 0f)
                {
                    currentAngle = currentAngle + 6.28318548f;
                }
                if (i == selectionIndex)
                {
                    drawColor = Color.Red;
                }
                spriteBatch.Draw(RootNode.Children[i].Icon,
                    new Vector2(imagePos.X, imagePos.Y), drawColor, 0f, 
                    new Vector2(RootNode.Children[i].Icon.Width / 2, RootNode.Children[i].Icon.Height / 2),
                    scale, SpriteEffects.None, 1f);
                if (i == selectionIndex)
                {
                    spriteBatch.DrawString(font, RootNode.Children[i].Text, imagePos + new Vector2(-font.MeasureString(RootNode.Children[i].Text).X / 2f, imageSize), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }
                currentAngle = currentAngle - angleIncrement;
            }
        }

        public bool HandleInput(InputState input, Vector2 selectionVector)
        {
            if (!Visible)
            {
                return false;
            }
            ComputeSelected(selectionVector);
            if (input.InGameSelect)
            {
                if (selectionIndex >= 0)
                {
                    if (!RootNode.Children[selectionIndex].IsLeaf)
                    {
                        ChangeTo(RootNode.Children[selectionIndex]);
                        GameAudio.SubBassWhoosh();
                    }
                    else
                    {
                        RootNode.Children[selectionIndex].Select();
                        ChangeTo(null);
                        GameAudio.SubBassWhoosh();
                    }
                }
                else if (selectionIndex != -2)
                {
                    ChangeTo(RootNode.parent);
                    GameAudio.SubBassWhoosh();
                }
                else
                {
                    ChangeTo(null);
                    GameAudio.SubBassWhoosh();
                }
            }
            if (input.MenuCancel)
            {
                ChangeTo(null);
                GameAudio.SubBassWhoosh();
            }
            return true;
        }

        public bool HandleInput(InputState input)
        {
            return HandleInput(input, input.GamepadCurr.ThumbSticks.Left);
        }

        private void NewMenu()
        {
            RootNode = newMenuNode;
            t.Reset(Direction.Ascending);
            t.OnTransitionEnd = null;
        }

        private void OnHide()
        {
            Visible = false;
            t.OnTransitionEnd = null;
        }

        public void Show(Vector2 position)
        {
            t.Reset(Direction.Ascending);
            t.OnTransitionEnd = null;
            Visible = true;
            Position = position;
        }

        public void Show(PieMenuNode rootNode, Vector2 position)
        {
            RootNode = rootNode;
            Show(position);
        }

        public void Update(GameTime gameTime)
        {
            if (!Visible)
                return;
            t.Update(gameTime.ElapsedGameTime.TotalSeconds);
        }
    }
}