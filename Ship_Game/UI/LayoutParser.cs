using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.Data.Yaml;
using Ship_Game.Data.YamlSerializer;
using Ship_Game.SpriteSystem;

namespace Ship_Game.UI
{
    public class LayoutParser
    {
        // Loads GameScreen and registers it for HotLoading
        public static void LoadLayout(GameScreen screen, string layoutFile, bool clearElements)
        {
            FileInfo file = ResourceManager.GetModOrVanillaFile(layoutFile);
            LoadLayout(screen, screen.ScreenArea, file, clearElements);

            // trigger ReloadContent when layout file is modified
            ScreenManager.Instance.AddHotLoadTarget(screen, layoutFile, file.FullName);
        }

        // Loads a generic UI container -- does not register for HotLoading
        // because container might have been destroyed
        public static void LoadLayout(UIElementContainer container, Vector2 size, string layoutFile, 
                                      bool clearElements, bool required)
        {
            FileInfo file = ResourceManager.GetModOrVanillaFile(layoutFile);
            if (file == null && required)
                throw new FileNotFoundException($"Missing required layout {layoutFile}");

            if (file != null)
                LoadLayout(container, size, file, clearElements);
        }

        public static void LoadLayout(UIElementContainer container, Vector2 size, FileInfo file, bool clearElements)
        {
            if (clearElements)
                container.RemoveAll();

            var layoutParser = new LayoutParser(container, size, file);
            layoutParser.CreateElements();
        }

        readonly UIElementContainer MainContainer;
        readonly GameContentManager Content;
        readonly YamlNode Root;
        readonly RootElementInfo RootInfo;
        readonly string Name;
        readonly Vector2 VirtualXForm; // multiplier to transform virtual coordinates to actual coordinates

        LayoutParser(UIElementContainer mainContainer, Vector2 size, FileInfo file)
        {
            MainContainer = mainContainer;
            Content = mainContainer.ContentManager;
            using (var parser = new YamlParser(file))
            {
                Root = parser.Root;
            }

            MainContainer.Size = size;

            RootInfo = (RootElementInfo)new YamlSerializer(typeof(RootElementInfo)).Deserialize(Root);
            Name = RootInfo.Name;
            Vector2 virtualSize = RootInfo.VirtualSize;
            VirtualXForm = size / virtualSize;
        }

        void CreateElements()
        {
            MainContainer.Name = Name; // override the name
            CreateChildElements(MainContainer, RootInfo.Elements);
        }

        void CreateChildElements(UIElementContainer parent, ElementInfo[] children)
        {
            for (int i = 0; i < children.Length; ++i)
            {
                CreateElement(parent, children[i]);
            }
        }

        static Point GetTextureSize(ElementInfo info)
        {
            if (info.Spr != null)
                return new Point((int)info.Spr.Size.X, (int)info.Spr.Size.Y);
            if (info.Tex != null)
                return new Point(info.Tex.Width, info.Tex.Height);
            return default;
        }

        static Vector2 GetAutoAspectSize(UIElementV2 element, Vector2 size, ElementInfo info, bool abs)
        {
            if (!abs && size.AlmostZero())
            {
                Log.Error($"Element {element.Name} RelSize cannot be [0,0] using default [0.5,0.5]");
                return new Vector2(0.5f, 0.5f);
            }
            
            Point texSize = GetTextureSize(info);
            if (texSize.X != 0 && texSize.Y != 0)
            {
                if (size.AlmostZero())
                {
                    // always abs value
                    size.X = texSize.X;
                    size.Y = texSize.Y;
                }
                else if (size.Y.AlmostZero()) // only X SIZE provided
                {
                    float aspectRatio = (texSize.Y / (float)texSize.X);
                    if (abs)
                    {
                        size.Y = (float)Math.Round(size.X * aspectRatio);
                    }
                    else
                    {
                        Vector2 parentSize = element.ParentSize;
                        // this is absolute size in screen coordinates, not virtual size
                        float absSizeX = size.X * parentSize.X;
                        float absSizeY = (float)Math.Round(absSizeX * aspectRatio);
                        size.Y = absSizeY / parentSize.Y;
                    }
                }
                else if (size.X.AlmostZero()) // only Y SIZE provided
                {
                    float aspectRatio = (texSize.X / (float)texSize.Y);
                    if (abs)
                    {
                        size.X = (float)Math.Round(size.Y * aspectRatio);
                    }
                    else
                    {
                        Vector2 parentSize = element.ParentSize;
                        // this is absolute size in screen coordinates, not virtual size
                        float absSizeY = size.Y * parentSize.Y;
                        float absSizeX = (float)Math.Round(absSizeY * aspectRatio);
                        size.X = absSizeX / parentSize.X;
                    }
                }
            }
            return size;
        }

        void SetPosAndSize(UIElementV2 element, ElementInfo info)
        {
            if (info.Type != "Override") // set defaults
            {
                element.SetRelPos(0, 0);
                element.SetAbsSize(0, 0);
            }

            if (info.AbsPos != null)
                element.SetAbsPos(info.AbsPos.Value * VirtualXForm);
            
            if (info.AbsSize != null)
                element.SetAbsSize(GetAutoAspectSize(element, info.AbsSize.Value, info, abs:true) * VirtualXForm);

            if (info.LocalPos != null)
                element.SetLocalPos(info.LocalPos.Value * VirtualXForm);

            if (info.RelPos != null)
                element.SetRelPos(info.RelPos.Value);

            if (info.RelSize != null)
                element.SetRelSize(GetAutoAspectSize(element, info.RelSize.Value, info, abs:false));

            if (info.AxisAlign != null)
            {
                element.ParentAlign = info.AxisAlign.Value;
                element.LocalAxis = info.AxisAlign.Value;
            }

            if (info.ParentAlign != null)
                element.ParentAlign = info.ParentAlign.Value;

            if (info.LocalAxis != null)
                element.LocalAxis = info.LocalAxis.Value;

            element.UpdatePosAndSize(); // calculate current absolute Pos & Size
        }

        SubTexture LoadTexture(string texturePath)
        {
            if (texturePath.IsEmpty()) return null;
            return Content.LoadTextureOrDefault("Textures/" + texturePath);
        }

        SpriteAnimation LoadSpriteAnim(SpriteAnimInfo sprite)
        {
            if (sprite == null || sprite.Path.IsEmpty()) return null;
            var sa = new SpriteAnimation(Content, "Textures/" + sprite.Path, autoStart: false)
            {
                Looping = sprite.Looping,
                FreezeAtLastFrame  = sprite.FreezeAtLastFrame,
                VisibleBeforeDelay = sprite.VisibleBeforeDelay,
            };
            sa.Start(sprite.Duration, sprite.StartAt, sprite.Delay);
            return sa;
        }

        void LoadElementResources(ElementInfo info)
        {
            info.Tex = LoadTexture(info.Texture);
            info.Spr = LoadSpriteAnim(info.SpriteAnim);

            // init texture for size information, so buttons can be auto-resized
            if (info.Tex == null && info.Type == "Button")
                info.Tex = UIButton.StyleTexture(info.ButtonStyle);
        }

        static UIElementV2 GetOrCreateElement(UIElementContainer parent, ElementInfo info)
        {
            switch (info.Type)
            {
                case "Panel":
                    return new UIPanel();
                case "List":
                    return new UIList
                    {
                        Padding = info.Padding,
                        LayoutStyle = info.ListLayout,
                    };
                case "Label":
                    return new UILabel(info.Title);
                case "Button":
                    return new UIButton(info.ButtonStyle, info.Title)
                    {
                        Tooltip = info.Tooltip,
                        ClickSfx = info.ClickSfx,
                    };
                case "Checkbox":
                    {
                        bool dummy = false;
                        return new UICheckBox(() => dummy, Fonts.Arial12Bold, info.Title, info.Tooltip);
                    }
                case "Override":
                    if (parent.Find(info.ElementName, out UIElementV2 element))
                        return element;
                    Log.Warning($"Override '{info.ElementName}' failed. Element not found in '{parent.Name}'!");
                    break;
                default:
                    Log.Warning($"Unrecognized UIElement {info.Type}");
                    break;
            }
            return null;
        }

        void CreateElement(UIElementContainer parent, ElementInfo info)
        {
            UIElementV2 element = GetOrCreateElement(parent, info);
            if (element == null)
                return;

            if (info.Type != "Override")
                parent.Add(element);

            element.Name = info.ElementName;
            LoadElementResources(info); // need texture info for Pos and Size

            SetPosAndSize(element, info);

            if (element is ISpriteElement sprite)
            {
                if (info.Spr != null)
                    sprite.Sprite = new DrawableSprite(info.Spr);
                else if (info.Tex != null)
                    sprite.Sprite = new DrawableSprite(info.Tex);
            }

            if (element is IColorElement colorElement && info.Color != null)
            {
                colorElement.Color = info.Color.Value;
            }

            if (info.DrawDepth != null)
            {
                element.DrawDepth = info.DrawDepth.Value;
            }

            element.Visible = info.Visible;

            if (info.Animation != null || info.Animations != null)
            {
                if (info.Type == "Override")
                    element.ClearEffects();

                if (info.Animation != null)
                    ParseAnimation(element, info.Animation);

                if (info.Animations != null)
                    foreach (var anim in info.Animations)
                        ParseAnimation(element, anim);
            }

            var container = element as UIElementContainer;
            if (container != null)
            {
                container.DebugDraw = info.DebugDraw;
            }

            if (info.Children != null && info.Children.Length != 0)
            {
                if (container != null)
                {
                    CreateChildElements(container, info.Children);
                }
                else
                {
                    Log.Warning($"UI {element} cannot contain 'Children', YAML Type:{info.Type} Name:{info.Name}");
                }
            }
        }

        static void ParseAnimation(UIElementV2 element, AnimInfo data)
        {
            // Delay(0), Duration(1), LoopTime(0), FadeInTime(0.25), FadeOutTime(0.25)
            float[] p = data.Params ?? Empty<float>.Array;

            float delay    = p.Length >= 1 ? p[0] : 0f;
            float duration = p.Length >= 2 ? p[1] : 1f;
            float loop     = p.Length >= 3 ? p[2] : 0f;
            float fadeIn   = p.Length >= 4 ? p[3] : 0.25f;
            float fadeOut  = p.Length >= 5 ? p[4] : 0.25f;

            UIBasicAnimEffect a = element.Anim(delay, duration, fadeIn, fadeOut);

            a.AnimPattern = data.Pattern;

            if (loop.NotZero())
                a.Loop(loop);

            if (data.Alpha != null)
                a.Alpha(data.Alpha.Value);

            if (data.CenterScale != null)
                a.CenterScale(data.CenterScale.Value);

            if (data.MinColor != null || data.MaxColor != null)
            {
                Color min = data.MinColor ?? Color.Black;
                Color max = data.MaxColor ?? Color.White;
                a.Color(min, max);
            }

            if (data.StartPos != null && data.EndPos != null)
                a.Pos(data.StartPos.Value, data.EndPos.Value);

            if (data.StartSize != null && data.EndSize != null)
                a.Size(data.StartSize.Value, data.EndSize.Value);

            Log.Info($"Add {element.Name} {a}");
        }
    }
}
