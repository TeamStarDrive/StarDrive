using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
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
        readonly StarDataNode Root;
        readonly string Name;
        readonly Vector2 VirtualXForm; // multiplier to transform virtual coordinates to actual coordinates
        readonly StarDataSerializer ElementSerializer = new StarDataSerializer(typeof(ElementInfo));

        LayoutParser(UIElementContainer mainContainer, Vector2 size, FileInfo file)
        {
            Vector2 virtualSize;
            MainContainer = mainContainer;
            Content = mainContainer.ContentManager;
            using (var parser = new StarDataParser(file))
            {
                Root = parser.Root;
            }

            MainContainer.Size = size;
            if (Root.FindChild("Screen", out StarDataNode screen))
            {
                var info = (ScreenInfo)new StarDataSerializer(typeof(ScreenInfo)).Deserialize(screen);
                Name = info.Name;
                virtualSize = info.VirtualSize;
            }
            else
            {
                Name = Root.Key;
                virtualSize = size; // default to current screen size
            }
            VirtualXForm = size / virtualSize;
        }

        void CreateElements()
        {
            CreateElements(MainContainer, Root);
            MainContainer.Name = Name; // override the name
        }

        void CreateElements(UIElementContainer parent, StarDataNode node)
        {
            if (!node.HasItems)
                return;
            for (int i = 0; i < node.Items.Count; ++i)
            {
                CreateElement(parent, node.Items[i]);
            }
        }

        ElementInfo DeserializeElementInfo(StarDataNode node)
        {
            try
            {
                return (ElementInfo)ElementSerializer.Deserialize(node);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to parse StarData: {e.Message}\n{node}");
                return null;
            }
        }

        Vector2 AbsoluteSize(ElementInfo info, Vector2 size, Vector2 parentSize)
        {
            if (size.X < 0f)
            {
                Log.Error($"Element {info.ElementName} Width cannot be negative: {size.X} ! Using default value 64.");
                size.X = 64;
            }
            if (size.Y < 0f)
            {
                Log.Error($"Element {info.ElementName} Height cannot be negative: {size.Y} ! Using default value 64.");
                size.Y = 64;
            }
            Vector2 result = size;
            if (size.X <= 1f) result.X *= parentSize.X;
            else              result.X *= VirtualXForm.X;
            if (size.Y <= 1f) result.Y *= parentSize.Y;
            else              result.Y *= VirtualXForm.Y;
            return result;
        }

        static Vector2 AlignValue(Align align)
        {
            switch (align)
            {
                default:
                case Align.TopLeft:      return new Vector2(0.0f, 0.0f);
                case Align.TopCenter:    return new Vector2(0.5f, 0.0f);
                case Align.TopRight:     return new Vector2(1.0f, 0.0f);
                case Align.CenterLeft:   return new Vector2(0.0f, 0.5f);
                case Align.Center:       return new Vector2(0.5f, 0.5f);
                case Align.CenterRight:  return new Vector2(1.0f, 0.5f);
                case Align.BottomLeft:   return new Vector2(0.0f, 1.0f);
                case Align.BottomCenter: return new Vector2(0.5f, 1.0f);
                case Align.BottomRight:  return new Vector2(1.0f, 1.0f);
            }
        }

        Vector2 AbsolutePos(Vector2 pos, Vector2 absSize, Vector2 parent, Vector2 parentSize, Align axisAlign)
        {
            // @note parent size is already transformed, so we only need to transform non-relative positions
            Vector2 p = pos;
            if (-1f <= pos.X && pos.X <= 1f) p.X *= absSize.X;
            else                            p.X *= VirtualXForm.X;
            if (-1f <= pos.Y && pos.Y <= 1f) p.Y *= absSize.Y;
            else                            p.Y *= VirtualXForm.Y;

            Vector2 align = AlignValue(axisAlign);
            p -= align * absSize;
            return parent + align*parentSize + p;
        }


        Rectangle ParseRect(UIElementV2 parent, ElementInfo info)
        {
            Vector2 pos = default, size = default;

            bool hasRectDefinition = info.Rect.LengthSquared().NotZero();
            if (hasRectDefinition)
            {
                pos = new Vector2(info.Rect.X, info.Rect.Y);
                size = new Vector2(info.Rect.Z, info.Rect.W);
            }
            if (info.Pos.NotZero())
            {
                if (hasRectDefinition)
                    Log.Warning($"Attribute 'Pos' ignored in '{info.ElementName}' because 'Rect' overrides it!");
                else
                    pos = info.Pos;
            }
            if (info.Size.NotZero())
            {
                if (hasRectDefinition)
                    Log.Warning($"Attribute 'Size' ignored in '{info.ElementName}' because 'Rect' overrides it!");
                else
                    size = info.Size;
            }

            Vector2 absSize = AbsoluteSize(info, size, parent.Size);

            int texWidth = 0, texHeight = 0;
            if (info.Spr != null)
            {
                texWidth  = info.Spr.Width;
                texHeight = info.Spr.Height;
            }
            else if (info.Tex != null)
            {
                texWidth  = info.Tex.Width;
                texHeight = info.Tex.Height;
            }

            if (texWidth != 0 && texHeight != 0)
            {
                if (absSize.AlmostZero())
                {
                    absSize.X = (float)Math.Round(texWidth  * VirtualXForm.X);
                    absSize.Y = (float)Math.Round(texHeight * VirtualXForm.Y);
                }
                else if (absSize.Y.AlmostZero())
                {
                    float aspectRatio = (texHeight / (float)texWidth);
                    absSize.Y = (float)Math.Round(absSize.X * aspectRatio);
                }
                else if (absSize.X.AlmostZero())
                {
                    float aspectRatio = (texWidth / (float)texHeight);
                    absSize.X = (float)Math.Round(absSize.Y * aspectRatio);
                }
            }

            Vector2 absPos = AbsolutePos(pos, absSize, parent.Pos, parent.Size, info.AxisAlign);
            return new Rectangle
            {
                X = (int) absPos.X,
                Y = (int) absPos.Y,
                Width  = (int) absSize.X,
                Height = (int) absSize.Y
            };
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

        ElementInfo ParseInfo(UIElementV2 parent, StarDataNode node)
        {
            ElementInfo info = DeserializeElementInfo(node);
            if (info != null)
            {
                info.Tex = LoadTexture(info.Texture);
                info.Spr = LoadSpriteAnim(info.SpriteAnim);

                // init texture for size information, so buttons can be auto-resized
                if (info.Tex == null && node.Key == "Button")
                {
                    info.Tex = UIButton.StyleTexture(info.ButtonStyle);
                }

                info.R = ParseRect(parent, info);
            }
            return info;
        }

        void CreateElement(UIElementContainer parent, StarDataNode node)
        {
            if (node.Key == "Screen")
                return; // ignore layout descriptor

            bool newElement = true;
            UIElementV2 element;
            ElementInfo info = ParseInfo(parent, node);
            if (info == null)
                return; // meh
            
            if (node.Key == "Panel")
            {
                element = new UIPanel();
            }
            else if (node.Key == "List")
            {
                element = new UIList
                {
                    Padding = info.Padding,
                    LayoutStyle = info.ListLayout,
                };
            }
            else if (node.Key == "Label")
            {
                element = new UILabel(info.Title.Text);
            }
            else if (node.Key == "Button")
            {
                element = new UIButton(info.ButtonStyle)
                {
                    Text = info.Title.Text,
                    Tooltip = info.Tooltip.Text,
                    ClickSfx = info.ClickSfx,
                };
            }
            else if (node.Key == "Checkbox")
            {
                bool dummy = false;
                element = new UICheckBox(() => dummy, Fonts.Arial12Bold, info.Title.Text, info.Tooltip.Text);
            }
            else if (node.Key == "Override")
            {
                if (!parent.Find(info.ElementName, out element))
                {
                    Log.Warning($"Override '{info.ElementName}' failed. Element not found in '{parent.Name}'!");
                    return;
                }
                newElement = false;
            }
            else
            {
                Log.Warning($"Unrecognized UIElement {node.Key}");
                return; // meh
            }

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

            if (newElement)
            {
                element.Name = info.ElementName;
                element.Rect = info.R;
                parent.Add(element);
            }
            else // OVERRIDE
            {
                if (info.R.IsEmpty == false)
                    element.Rect = info.R;
            }

            if (info.DrawDepth != null)
                element.DrawDepth = info.DrawDepth.Value;
            element.Visible = info.Visible;
            ParseAnimation(element, info);

            var container = element as UIElementContainer;
            if (container != null)
            {
                container.DebugDraw = info.DebugDraw;
            }

            if (node.FindChild("Children", out StarDataNode children))
            {
                if (container != null)
                {
                    CreateElements(container, children);
                }
                else
                {
                    Log.Warning($"UI {element} cannot contain 'Children': {children}");
                }
            }
        }

        static void ParseAnimation(UIElementV2 element, ElementInfo info)
        {
            AnimInfo data = info.Animation;
            if (data != null)
            {
                element.ClearEffects();

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

                if (data.Alpha.Min.NotEqual(1f) || data.Alpha.Max.NotEqual(1f))
                    a.Alpha(data.Alpha.Min, data.Alpha.Max);

                if (data.MinColor != null || data.MaxColor != null)
                {
                    Color min = data.MinColor ?? Color.Black;
                    Color max = data.MaxColor ?? Color.White;
                    a.Color(min, max);
                }
            }
        }
    }
}
