using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.UI
{
    public class MultiLayerDrawContainer : UIElementContainer
    {
        readonly Array<UIElementV2> BackElements = new Array<UIElementV2>();
        readonly Array<UIElementV2> BackAdditive = new Array<UIElementV2>();
        readonly Array<UIElementV2> ForeElements = new Array<UIElementV2>();
        readonly Array<UIElementV2> ForeAdditive = new Array<UIElementV2>();
        
        protected MultiLayerDrawContainer(in Rectangle rect) : base(rect)
        {
        }

        void ClearLayers()
        {
            BackElements.Clear();
            BackAdditive.Clear();
            ForeElements.Clear();
            ForeAdditive.Clear();
        }

        void GatherDrawLayers(UIElementContainer parent)
        {
            // HACK: This enables Multi-Layered Draw Mode on the UIElementContainer,
            //       which prevents recursive Draw
            parent.NewMultiLayeredDrawMode = true;

            int count = parent.GetInternalElementsUnsafe(out UIElementV2[] elements);
            for (int i = 0; i < count; ++i)
            {
                UIElementV2 child = elements[i];
                if (child.Visible)
                {
                    switch (child.DrawDepth)
                    {
                        default:
                        case DrawDepth.Foreground:   ForeElements.Add(child); break;
                        case DrawDepth.Background:   BackElements.Add(child); break;
                        case DrawDepth.ForeAdditive: ForeAdditive.Add(child); break;
                        case DrawDepth.BackAdditive: BackAdditive.Add(child); break;
                    }
                    
                    if (child is UIElementContainer container) // gather recursively:
                    {
                        GatherDrawLayers(container);
                    }
                }
            }
        }

        public void DrawMulti(ScreenManager manager, UIElementContainer root, bool draw3D,
                              GameTime time, ref Matrix view, ref Matrix projection)
        {
            GatherDrawLayers(root);

            if (draw3D) manager.BeginFrameRendering(time, ref view, ref projection);

            SpriteBatch batch = manager.SpriteBatch;

            if (BackElements.NotEmpty) BatchDrawSimple(batch, BackElements);
            if (BackAdditive.NotEmpty) BatchDrawAdditive(batch, BackAdditive);

            if (draw3D) manager.RenderSceneObjects();

            // @note Foreground is the default layer
            if (ForeElements.NotEmpty) BatchDrawSimple(batch, ForeElements);
            if (ForeAdditive.NotEmpty) BatchDrawAdditive(batch, ForeAdditive);

            if (draw3D) manager.EndFrameRendering();

            ClearLayers();
        }

        static void BatchDrawSimple(SpriteBatch batch, Array<UIElementV2> elements)
        {
            batch.Begin();

            int count = elements.Count;
            UIElementV2[] items = elements.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                items[i].Draw(batch);
            }

            batch.End();
        }

        static void BeginAdditive(SpriteBatch batch, bool saveState = false)
        {
            // NOTE: SaveState restores graphics device settings
            //       just in case we mix 3D rendering with 2D rendering

            batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, 
                saveState ? SaveStateMode.SaveState : SaveStateMode.None);
            batch.GraphicsDevice.RenderState.SourceBlend      = Blend.InverseDestinationColor;
            batch.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
            batch.GraphicsDevice.RenderState.BlendFunction    = BlendFunction.Add;
        }
        
        static void BatchDrawAdditive(SpriteBatch batch, Array<UIElementV2> elements)
        {
            BeginAdditive(batch);

            int count = elements.Count;
            UIElementV2[] items = elements.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                items[i].Draw(batch);
            }

            batch.End();
        }
    }
}
