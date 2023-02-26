using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    public sealed class PieMenu
    {
        Transition t;
        int HoveredIndex = -1;
        
        PieMenuNode RootNode;
        Vector2 Position;
        readonly float Radius = 75f;
        readonly float ScaleFactor = 1f;
        public bool Visible { get; private set; }

        public PieMenu()
        {
            t = new Transition(Direction.Ascending, TransitionCurve.Linear, 0.15f);
        }

        void ChangeTo(PieMenuNode newNode)
        {
            if (newNode == null) // transition to hidden
            {
                t.Reset(Direction.Descending);
                t.OnTransitionEnd = () =>
                {
                    Visible = false;
                    t.OnTransitionEnd = null;
                };
            }
            else
            {
                t.Reset(Direction.Descending);
                t.OnTransitionEnd = () =>
                {
                    RootNode = newNode;
                    t.Reset(Direction.Ascending);
                    t.OnTransitionEnd = null;
                };
            }
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

        public void DrawAt(SpriteBatch batch, Vector2 position, Graphics.Font font)
        {
            if (!Visible)
                return;

            Position = position;

            float scale = t.CurrentPosition * ScaleFactor;
            for (int i = 0; i < RootNode.Children.Count; i++)
            {
                PieMenuNode node = RootNode.Children[i];
                float relativeAngle = (float)i / RootNode.Children.Count;
                float angle = relativeAngle * RadMath.TwoPI;
                Vector2 imagePos = Position + (scale * Radius * angle.RadiansToDirection());
                int imageSize = (int)(scale * 30f);

                Color drawColor = (i == HoveredIndex) ? Color.Red : Color.White;

                batch.Draw(node.Icon, imagePos, drawColor, 0f, node.Icon.CenterF, scale, SpriteEffects.None, 1f);

                if (i == HoveredIndex)
                {
                    var pos = imagePos + new Vector2(-font.TextWidth(node.Text) / 2f, imageSize);
                    batch.DrawString(font, node.Text, pos, Color.White);
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

        public void Show(PieMenuNode rootNode)
        {
            RootNode = rootNode;
            t.Reset(Direction.Ascending);
            t.OnTransitionEnd = null;
            Visible = true;
        }

        public void Hide()
        {
            // begin transition to OnHide()
            ChangeTo(null);
        }

        public void Update(float deltaTime)
        {
            if (!Visible)
                return;
            t.Update(deltaTime);
        }
    }
}