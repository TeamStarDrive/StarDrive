using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class PieMenu
    {
        Transition t;

        int HoveredIndex = -1;

        readonly Action hideDelegate;
        readonly Action newMenuDelegate;

        PieMenuNode newMenuNode;

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

        float SectorDegrees => 360f / RootNode.Children.Count;

        void UpdateHoverIndex(InputState input)
        {
            HoveredIndex = -1; // no selection

            Vector2 selectionOffset = (input.CursorPosition - Position) / Radius;
            float distance = selectionOffset.Length();

            if (distance > 3f)
            {
                HoveredIndex = -2; // way out of range!
                return;
            }

            if (0.3f <= distance && distance <= 1.5f)
            {
                float angle = selectionOffset.Normalized().ToDegrees() + SectorDegrees*0.5f;
                float relativeAngle = (angle.ToRadians() / RadMath.TwoPI);
                HoveredIndex = (int)(relativeAngle * RootNode.Children.Count);
            }
        }

        public void Draw(SpriteBatch spriteBatch, Graphics.Font font)
        {
            if (!Visible)
                return;

            float scale = t.CurrentPosition * ScaleFactor;
            for (int i = 0; i < RootNode.Children.Count; i++)
            {
                float relativeAngle = (float)i / RootNode.Children.Count;
                float angle = relativeAngle * RadMath.TwoPI;
                Vector2 imagePos = Position + (scale * Radius * angle.RadiansToDirection());
                int imageSize = (int)(scale * 30f);

                Color drawColor = (i == HoveredIndex) ? Color.Red : Color.White;

                spriteBatch.Draw(RootNode.Children[i].Icon, imagePos, drawColor, 0f, 
                        RootNode.Children[i].Icon.CenterF, scale, SpriteEffects.None, 1f);

                if (i == HoveredIndex)
                {
                    spriteBatch.DrawString(font, RootNode.Children[i].Text, 
                        imagePos + new Vector2(-font.MeasureString(RootNode.Children[i].Text).X / 2f, imageSize), 
                        Color.White);
                }
            }
        }

        public bool HandleInput(InputState input)
        {
            if (!Visible)
                return false;

            UpdateHoverIndex(input);

            if (input.InGameSelect) // click!
            {
                if (HoveredIndex >= 0)
                {
                    if (!RootNode.Children[HoveredIndex].IsLeaf)
                    {
                        ChangeTo(RootNode.Children[HoveredIndex]);
                        GameAudio.SubBassWhoosh();
                    }
                    else
                    {
                        RootNode.Children[HoveredIndex].Select();
                        ChangeTo(null);
                        GameAudio.SubBassWhoosh();
                    }
                }
                else if (HoveredIndex == -2)
                {
                    ChangeTo(null);
                    GameAudio.SubBassWhoosh();
                }
                else
                {
                    ChangeTo(RootNode.parent);
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

        void NewMenu()
        {
            RootNode = newMenuNode;
            t.Reset(Direction.Ascending);
            t.OnTransitionEnd = null;
        }

        void OnHide()
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

        public void Update(float deltaTime)
        {
            if (!Visible)
                return;
            t.Update(deltaTime);
        }
    }
}