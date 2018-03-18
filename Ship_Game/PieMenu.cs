using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
    public delegate void SimpleDelegate(object sender);

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
            this.t = new Transition(Direction.Ascending, TransitionCurve.Linear, 0.15f);
            this.hideDelegate = this.OnHide;
            this.newMenuDelegate = this.NewMenu;
        }

        public void ChangeTo(PieMenuNode newNode)
        {
            if (newNode == null)
            {
                this.t.OnTransitionEnd = this.hideDelegate;
                this.t.Reset(Direction.Descending);
                return;
            }
            this.t.OnTransitionEnd = this.newMenuDelegate;
            this.newMenuNode = newNode;
            this.t.Reset(Direction.Descending);
        }

        private void ComputeSelected(Vector2 selectionVector)
        {
            this.selectionIndex = -1;
            if (selectionVector.Length() > 3f)
            {
                this.selectionIndex = -2;
                return;
            }
            if (selectionVector.Length() > 1.5f)
            {
                return;
            }
            if (selectionVector.Length() > 0.3f)
            {
                float angleDivision = 1f / (float)this.RootNode.Children.Count;
                float angle = (float)Math.Atan2((double)selectionVector.Y, (double)selectionVector.X);
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
                this.selectionIndex = 0;
                while ((float)this.selectionIndex * angleDivision < angle)
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
            if (!this.Visible)
            {
                return;
            }
            Vector2 center = this.Position;
            float scale = this.t.CurrentPosition * this.ScaleFactor;
            float currentAngle = 1.57079637f;
            float angleIncrement = 6.28318548f / (float)this.RootNode.Children.Count;
            for (int i = 0; i < this.RootNode.Children.Count; i++)
            {
                Vector2 imagePos = center + (scale * this.Radius * new Vector2((float)Math.Cos((double)currentAngle), -(float)Math.Sin((double)currentAngle)));
                int imageSize = (int)(scale * 30f);
                Rectangle rectangle = new Rectangle((int)imagePos.X - imageSize, (int)imagePos.Y - imageSize, 2 * imageSize, 2 * imageSize);
                Color drawColor = Color.White;
                if (currentAngle <= 0f)
                {
                    currentAngle = currentAngle + 6.28318548f;
                }
                if (i == this.selectionIndex)
                {
                    drawColor = Color.Red;
                }
                Rectangle? nullable = null;
                spriteBatch.Draw(this.RootNode.Children[i].Icon, new Vector2(imagePos.X, imagePos.Y), nullable, drawColor, 0f, new Vector2((float)(this.RootNode.Children[i].Icon.Width / 2), (float)(this.RootNode.Children[i].Icon.Height / 2)), scale, SpriteEffects.None, 1f);
                if (i == this.selectionIndex)
                {
                    spriteBatch.DrawString(font, this.RootNode.Children[i].Text, imagePos + new Vector2(-font.MeasureString(this.RootNode.Children[i].Text).X / 2f, (float)imageSize), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
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
                        GameAudio.PlaySfxAsync("sub_bass_whoosh");
                    }
                    else
                    {
                        RootNode.Children[selectionIndex].Select();
                        ChangeTo(null);
                        GameAudio.PlaySfxAsync("sub_bass_whoosh");
                    }
                }
                else if (this.selectionIndex != -2)
                {
                    ChangeTo(RootNode.parent);
                    GameAudio.PlaySfxAsync("sub_bass_whoosh");
                }
                else
                {
                    ChangeTo(null);
                    GameAudio.PlaySfxAsync("sub_bass_whoosh");
                }
            }
            if (input.MenuCancel)
            {
                ChangeTo(null);
                GameAudio.PlaySfxAsync("sub_bass_whoosh");
            }
            return true;
        }

        public bool HandleInput(InputState input)
        {
            return this.HandleInput(input, input.GamepadCurr.ThumbSticks.Left);
        }

        private void NewMenu()
        {
            this.RootNode = this.newMenuNode;
            this.t.Reset(Direction.Ascending);
            this.t.OnTransitionEnd = null;
        }

        private void OnHide()
        {
            this.Visible = false;
            this.t.OnTransitionEnd = null;
        }

        public void Show(Vector2 position)
        {
            this.t.Reset(Direction.Ascending);
            this.t.OnTransitionEnd = null;
            this.Visible = true;
            this.Position = position;
        }

        public void Show(PieMenuNode rootNode, Vector2 position)
        {
            this.RootNode = rootNode;
            this.Show(position);
        }

        public void Update(GameTime gameTime)
        {
            if (!Visible)
                return;
            t.Update(gameTime.ElapsedGameTime.TotalSeconds);
        }
    }
}