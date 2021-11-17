using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Yaml;
using Ship_Game.Data.YamlSerializer;
using Ship_Game.SpriteSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.UI
{
    /// <summary>
    /// Utility for taking an active GameScreen
    /// and converting it into a reusable YAML file
    /// 
    /// Also generates the necessary C# binding boilerplate code
    /// </summary>
    public class LayoutExporter
    {
        public static void Export(GameScreen screen, string layoutFile)
        {
            var exporter = new LayoutExporter(screen, layoutFile);
            exporter.SaveLayout();
        }

        GameScreen Screen;
        FileInfo OutFile;
        RootElementInfo Root;

        LayoutExporter(GameScreen screen, string layoutFile)
        {
            Screen = screen;
            OutFile = ResourceManager.ContentInfo(layoutFile);

            var elements = new Array<ElementInfo>();

            foreach (UIElementV2 rootElement in Screen.GetElements())
                elements.Add(CreateElement(rootElement));

            Root = new RootElementInfo
            {
                Name = Screen.Name,
                VirtualSize = GameBase.ScreenSize,
                Elements = elements.ToArray()
            };
        }

        void SaveLayout()
        {
            var serializer = new YamlSerializer(typeof(RootElementInfo));
            using (var writer = new StreamWriter(OutFile.FullName, append:false, Encoding.UTF8))
            {
                var root = new YamlNode();
                serializer.Serialize(root, Root);
                root.SerializeTo(writer, depth:-2, noSpacePrefix:true);
            }
        }

        ElementInfo CreateElement(UIElementV2 element)
        {
            var info = new ElementInfo();

            // UIList is a specialized UIPanel
            if (element is UIList list)
            {
                info.Type = "List";
                info.Padding = list.Padding;
                info.ListLayout = list.LayoutStyle;
                SetPanelInfo(info, list);
            }
            else if (element is UIPanel panel)
            {
                info.Type = "Panel";
                SetPanelInfo(info, panel);
            }
            else if(element is UILabel label)
            {
                info.Type = "Label";
                info.Title = label.Text;
                if (label.Tooltip.NotEmpty)
                    info.Tooltip = label.Tooltip;
                if (label.Color != Color.White)
                    info.Color = label.Color;
            }
            else if (element is UIButton button)
            {
                info.Type = "Button";
                info.ButtonStyle = button.Style;
                info.Title = button.Text;
                if (button.Tooltip.NotEmpty)
                    info.Tooltip = button.Tooltip;
                if (button.ClickSfx != ElementInfo.DefaultClickSfx)
                    info.ClickSfx = button.ClickSfx;
            }
            else if (element is UICheckBox checkBox)
            {
                info.Type = "Checkbox";
                info.Title = checkBox.Text;
                if (checkBox.Tooltip.NotEmpty)
                    info.Tooltip = checkBox.Tooltip;
                if (checkBox.TextColor != Color.White)
                    info.Color = checkBox.TextColor;
            }
            else
            {
                Log.Warning($"Unsupported UIElement Type: {element}");
                info.Type = "Panel";
            }

            SetCommonFields(info, element);

            if (element is UIElementContainer container)
            {
                info.Children = container.GetElements().Select(CreateElement);
            }
            return info;
        }

        void SetPanelInfo(ElementInfo info, UIPanel panel)
        {
            info.Color = panel.Color;
            info.BorderColor = panel.Border;
            info.Tooltip = panel.Tooltip;

            if (panel.Sprite?.Tex != null)
            {
                info.Texture = panel.Sprite.Tex.TexturePath;
            }
            else if (panel.Sprite?.Anim != null)
            {
                SpriteAnimation a = panel.Sprite.Anim;
                info.SpriteAnim = new SpriteAnimInfo
                {
                    Path = a.Name,
                    Delay = a.Delay,
                    Duration = a.Duration,
                    StartAt = a.CurrentTime,
                    Looping = a.Looping,
                    FreezeAtLastFrame = a.FreezeAtLastFrame,
                    VisibleBeforeDelay = a.VisibleBeforeDelay,
                };
            }
        }

        void SetCommonFields(ElementInfo info, UIElementV2 e)
        {
            info.Name = e.Name;
            info.Visible = e.Visible;
            info.DrawDepth = e.DrawDepth;
            info.DebugDraw = (e as UIElementContainer)?.DebugDraw ?? false;

            if (e.ParentAlign == e.LocalAxis)
            {
                if (e.ParentAlign != Align.TopLeft)
                    info.AxisAlign = e.ParentAlign;
            }
            else
            {
                if (e.ParentAlign != Align.TopLeft)
                    info.ParentAlign = e.ParentAlign;
                if (e.LocalAxis != Align.TopLeft)
                    info.LocalAxis = e.LocalAxis;
            }

            // POS
            if (e.UseRelPos)
                info.RelPos = new Vector2(e.RelPos.X, e.RelPos.Y);
            else if (e.UseLocalPos)
                info.LocalPos = new Vector2(e.LocalPos.X, e.LocalPos.Y);
            else
                info.AbsPos = e.Pos;

            // SIZE
            if (e.UseRelSize)
                info.RelSize = new Vector2(e.RelSize.W, e.RelSize.H);
            else
                info.AbsSize = e.Size;

            var fx = e.GetEffects();
            if (fx != null)
            {
                var anims = fx.FilterSelect(f => f is UIBasicAnimEffect, f => f as UIBasicAnimEffect);
                if (anims.Length == 1)
                    info.Animation = GetAnimInfo(anims[0]);
                else
                    info.Animations = anims.Select(GetAnimInfo);
            }
        }

        AnimInfo GetAnimInfo(UIBasicAnimEffect a)
        {
            var ai = new AnimInfo()
            {
                Params = new float[] { a.Delay, a.Duration, a.Looping ? a.EndTime : 0, a.DurationIn, a.DurationOut },
                Pattern = a.AnimPattern,
            };

            if (a.AnimateColor)
            {
                ai.MinColor = a.MinColor;
                ai.MaxColor = a.MaxColor;
            }

            if (a.AnimateAlpha)
                ai.Alpha = a.AlphaRange;

            if (a.AnimateScale)
                ai.CenterScale = a.CenterScaleRange;

            if (a.AnimateSize)
            {
                ai.StartSize = a.StartSize;
                ai.EndSize = a.EndSize;
            }

            if (a.AnimatePosition)
            {
                ai.StartPos = a.StartPos;
                ai.EndPos = a.EndPos;
            }

            return ai;
        }
    }
}
