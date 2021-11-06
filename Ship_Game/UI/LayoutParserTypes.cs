using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.Data.Serialization;

namespace Ship_Game.UI
{
    [StarDataType]
    internal class RootElementInfo
    {
        #pragma warning disable 649
        [StarData] public readonly string Name;
        [StarData] public readonly Vector2 VirtualSize;
        [StarData] public readonly ElementInfo[] Elements = new ElementInfo[0];
        #pragma warning restore 649
    }

    [StarDataType]
    internal class ElementInfo // generic info for all elements (I'm just lazy)
    {
        #pragma warning disable 649
        [StarDataKeyName] public readonly string Type;
        [StarData] public readonly string Name;
        [StarData] public readonly string Texture;
        [StarData] public readonly bool Visible = true; // Visible by default
        [StarData] public readonly DrawDepth? DrawDepth;

        [StarData] public readonly Vector2? AbsPos;
        [StarData] public readonly Vector2? AbsSize;
        [StarData] public readonly Vector2? LocalPos;
        [StarData] public readonly Vector2? RelPos;
        [StarData] public readonly Vector2? RelSize;

        [StarData] public readonly Vector2 Padding = new Vector2(5f, 5f);
        [StarData] public readonly ListLayoutStyle ListLayout = ListLayoutStyle.ResizeList;
        [StarData] public readonly ButtonStyle ButtonStyle = ButtonStyle.Default;
        [StarData] public readonly string ClickSfx = "echo_affirm";
        /**
         * Sets the auto-layout axis of the UIElement. Default is Align.TopLeft
         * Changing the axis will change the position and rotation axis of the object.
         *
         * And also sets the auto-layout alignment to parent container bounds.
         * By changing this value, you can align element Pos:[0,0] to parent BottomLeft
         * @example Align.Center will perfectly center to parent center
         */
        [StarData] public readonly Align? AxisAlign; // sets both ParentAlign and LocalAxis
        [StarData] public readonly Align? ParentAlign;
        [StarData] public readonly Align? LocalAxis;

        [StarData] public readonly Color? Color;
        [StarData] public readonly LocalizedText Title;
        [StarData] public readonly LocalizedText Tooltip;
        [StarData] public readonly AnimInfo Animation = null;
        [StarData] public readonly AnimInfo[] Animations = null;
        [StarData] public readonly SpriteAnimInfo SpriteAnim = null;
        [StarData] public readonly bool DebugDraw;
        [StarData] public readonly ElementInfo[] Children = new ElementInfo[0];
        #pragma warning restore 649

        // these are initialized after parsing:
        public SubTexture Tex;
        public SpriteAnimation Spr;
        public string ElementName => Name ?? Texture ?? "";
        public override string ToString() => $"{ElementName} {PosString} {SizeString}";

        string PosString
        {
            get
            {
                if (AbsPos != null)   return $"AbsPos X:{AbsPos.Value.X} Y:{AbsPos.Value.Y}";
                if (LocalPos != null) return $"LocalPos X:{LocalPos.Value.X} Y:{LocalPos.Value.Y}";
                if (RelPos != null)   return $"RelPos X:{RelPos.Value.X} Y:{RelPos.Value.Y}";
                return "RelPos X:0 Y:0";
            }
        }

        string SizeString
        {
            get
            {
                if (AbsSize != null) return $"AbsSize W:{AbsSize.Value.X} H:{AbsSize.Value.Y}";
                if (RelSize != null) return $"RelSize W:{RelSize.Value.X} H:{RelSize.Value.Y}";
                return "AbsSize X:? Y:?";
            }
        }
    }

    [StarDataType]
    internal class AnimInfo
    {
        #pragma warning disable 649
        // Delay(0), Duration(1), LoopTime(0), FadeInTime(0.25), FadeOutTime(0.25)
        [StarData] public readonly float[] Params;
        [StarData] public readonly AnimPattern Pattern = AnimPattern.None;

        [StarData] public readonly Color? MinColor;
        [StarData] public readonly Color? MaxColor;
        [StarData] public readonly Range? Alpha; // animation alpha range
        [StarData] public readonly Range? CenterScale; // animation scale range

        [StarData] public readonly Vector2? StartSize; // starting size of the animated UIElement
        [StarData] public readonly Vector2? EndSize;

        [StarData] public readonly Vector2? StartPos; // starting pos of the animated UIElement
        [StarData] public readonly Vector2? EndPos;
        #pragma warning restore 649
    }

    [StarDataType]
    internal class SpriteAnimInfo
    {
        #pragma warning disable 649
        [StarData] public readonly string Path;
        [StarData] public readonly float Delay;
        [StarData] public readonly float Duration;
        [StarData] public readonly float StartAt;
        [StarData] public readonly bool Looping;
        [StarData] public readonly bool FreezeAtLastFrame;
        [StarData] public readonly bool VisibleBeforeDelay;
        #pragma warning restore 649
    }
}
