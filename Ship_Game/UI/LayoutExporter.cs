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
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game.UI
{
    /// <summary>
    /// Utility for taking an active GameScreen/UIElementContainer
    /// and converting it into a reusable YAML file
    /// 
    /// Also generates the necessary C# binding boilerplate code
    /// </summary>
    public class LayoutExporter
    {
        public static void Export(UIElementContainer container, string layoutFile)
        {
            var exporter = new LayoutExporter(container, layoutFile);
            exporter.SaveLayout();
        }

        readonly FileInfo OutFile;
        readonly RootElementInfo Root;

        LayoutExporter(UIElementContainer container, string layoutFile)
        {
            OutFile = ResourceManager.ContentInfo(layoutFile);

            var elements = new Array<ElementInfo>();

            foreach (UIElementV2 rootElement in container.GetElements())
                elements.Add(CreateElement(rootElement));

            Root = new RootElementInfo
            {
                Name = container.Name,
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

        static ElementInfo CreateElement(UIElementV2 element)
        {
            var info = new ElementInfo();

            // UIList is a specialized UIPanel
            if (element is UIList list)
            {
                info.Type = "List";
                info.Padding = list.Padding;
                info.ListLayout = list.LayoutStyle;
                if (list.Color != Color.TransparentBlack)
                    info.Color = list.Color;
                SetPanelInfo(info, list);
            }
            else if (element is UIPanel panel)
            {
                info.Type = "Panel";
                if (panel.Color != Color.White)
                    info.Color = panel.Color;
                SetPanelInfo(info, panel);
            }
            else if(element is UILabel label)
            {
                info.Type = "Label";
                if (label is VersionLabel)
                    info.Type = "VersionLabel";
                info.Title = label.Text;
                if (label.Font != Fonts.Arial12Bold)
                    info.Font = label.Font.Name;
                if (label.Tooltip.NotEmpty)
                    info.Tooltip = label.Tooltip;
                if (label.Color != Color.White)
                    info.Color = label.Color;
            }
            else if (element is UIButton button)
            {
                info.Type = "Button";
                info.Title = button.Text;
                if (button.Font != Fonts.Arial12Bold)
                    info.Font = button.Font.Name;
                if (button.Style != ButtonStyle.Default)
                    info.ButtonStyle = button.Style;
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
                var elements = container.GetElements();
                if (elements.Count > 0) // only set if there are children
                    info.Children = elements.Select(CreateElement);
            }
            return info;
        }

        static void SetPanelInfo(ElementInfo info, UIPanel panel)
        {

            if (panel.Border != Color.TransparentBlack)
                info.BorderColor = panel.Border;

            if (panel.Tooltip.NotEmpty)
                info.Tooltip = panel.Tooltip;

            if (panel.Sprite?.Tex != null)
            {
                info.Texture = GetTexturePath(panel.Sprite.Tex.TexturePath);
            }
            else if (panel.Sprite?.Anim != null)
            {
                SpriteAnimation a = panel.Sprite.Anim;
                var sa = new SpriteAnimInfo
                {
                    Path = GetTexturePath(a.Name),
                    Duration = a.Duration,
                };

                if (a.Delay > 0)
                    sa.Delay = a.Delay;

                if (a.CurrentTime > 0)
                    sa.StartAt = a.CurrentTime;

                if (a.Looping)
                    sa.Looping = a.Looping;

                if (a.FreezeAtLastFrame)
                    sa.FreezeAtLastFrame = a.FreezeAtLastFrame;

                if (a.VisibleBeforeDelay)
                    sa.VisibleBeforeDelay = a.VisibleBeforeDelay;

                info.SpriteAnim = sa;
            }
        }

        static void SetCommonFields(ElementInfo info, UIElementV2 e)
        {
            info.Name = e.Name;

            if (!e.Visible)
                info.Visible = e.Visible;

            if (e.DrawDepth != DrawDepth.Foreground)
                info.DrawDepth = e.DrawDepth;

            if ((e as UIElementContainer)?.DebugDraw == true)
                info.DebugDraw = true;

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
            if (!(e.Parent is UIList)) // UIList sets pos automatically
            {
                if (e.UseRelPos)
                    info.RelPos = new Vector2(e.RelPos.X, e.RelPos.Y);
                else if (e.UseLocalPos)
                    info.LocalPos = new Vector2(e.LocalPos.X, e.LocalPos.Y);
                else
                    info.AbsPos = e.Pos;
            }

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

        static AnimInfo GetAnimInfo(UIBasicAnimEffect a)
        {
            var ai = new AnimInfo()
            {
                Params = new [] { a.Delay, a.Duration, (a.Looping ? a.EndTime : 0), a.DurationIn, a.DurationOut },
            };

            if (a.AnimPattern != AnimPattern.None)
                ai.Pattern = a.AnimPattern;

            if (a.AnimateAlpha)
                ai.Alpha = a.AlphaRange;

            if (a.AnimateScale)
                ai.CenterScale = a.CenterScaleRange;

            if (a.AnimateColor)
            {
                ai.MinColor = a.MinColor;
                ai.MaxColor = a.MaxColor;
            }

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

        static string GetTexturePath(string path)
        {
            if (path.StartsWith("Textures/"))
                return path.Substring("Textures/".Length);
            return path;
        }
    }
}
