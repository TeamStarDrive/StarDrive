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
        [StarData] public string Name;
        [StarData] public Vector2 VirtualSize;
        [StarData] public ElementInfo[] Elements = new ElementInfo[0];
    }

    [StarDataType]
    internal class ElementInfo // generic info for all elements (I'm just lazy)
    {
        [StarDataKeyName] public string Type;
        [StarData] public string Name;
        [StarData] public bool Visible = true; // Visible by default
        [StarData] public DrawDepth? DrawDepth;

        [StarData] public Vector2? AbsPos;
        [StarData] public Vector2? AbsSize;
        [StarData] public Vector2? LocalPos;
        [StarData] public Vector2? RelPos;
        [StarData] public Vector2? RelSize;

        // UIList
        [StarData] public Vector2? Padding;
        [StarData] public ListLayoutStyle? ListLayout;
        
        // UIButton
        [StarData] public ButtonStyle? ButtonStyle;
        [StarData] public string ClickSfx = null;

        public const string DefaultClickSfx = "echo_affirm";

        /**
         * Sets the auto-layout axis of the UIElement. Default is Align.TopLeft
         * Changing the axis will change the position and rotation axis of the object.
         *
         * And also sets the auto-layout alignment to parent container bounds.
         * By changing this value, you can align element Pos:[0,0] to parent BottomLeft
         * @example Align.Center will perfectly center to parent center
         */
        [StarData] public Align? AxisAlign; // sets both ParentAlign and LocalAxis
        [StarData] public Align? ParentAlign;
        [StarData] public Align? LocalAxis;

        // UIPanel
        [StarData] public string Texture;
        [StarData] public SpriteAnimInfo SpriteAnim = null;
        [StarData] public Color? Color;
        [StarData] public Color? BorderColor;
        
        // UILabel/UIButton
        [StarData] public LocalizedText? Title;
        [StarData] public LocalizedText? Tooltip;

        [StarData] public AnimInfo Animation = null;
        [StarData] public AnimInfo[] Animations = null;
        [StarData] public bool DebugDraw;

        [StarData] public ElementInfo[] Children = null;

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
        // Delay(0), Duration(1), LoopTime(0), FadeInTime(0.25), FadeOutTime(0.25)
        [StarData] public float[] Params;
        [StarData] public AnimPattern Pattern = AnimPattern.None;

        [StarData] public Color? MinColor;
        [StarData] public Color? MaxColor;
        [StarData] public Range? Alpha; // animation alpha range
        [StarData] public Range? CenterScale; // animation scale range

        [StarData] public Vector2? StartSize; // starting size of the animated UIElement
        [StarData] public Vector2? EndSize;

        [StarData] public Vector2? StartPos; // starting pos of the animated UIElement
        [StarData] public Vector2? EndPos;
    }

    [StarDataType]
    internal class SpriteAnimInfo
    {
        [StarData] public string Path;
        [StarData] public float Delay;
        [StarData] public float Duration;
        [StarData] public float StartAt;
        [StarData] public bool Looping;
        [StarData] public bool FreezeAtLastFrame;
        [StarData] public bool VisibleBeforeDelay;
    }
}
