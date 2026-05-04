using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;
using SDGraphics;
using SDUtils;
using Rectangle = SDGraphics.Rectangle;
using Matrix = SDGraphics.Matrix;

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

        public void DrawMulti(ScreenManager manager, SpriteBatch batch, DrawTimes elapsed,
                              UIElementContainer root, bool draw3D,
                              ref Matrix view, ref Matrix projection)
        {
            GatherDrawLayers(root);

            if (draw3D) manager.BeginFrameRendering(elapsed, ref view, ref projection);

            if (BackElements.NotEmpty) BatchDrawSimple(batch, elapsed, BackElements);
            if (BackAdditive.NotEmpty) BatchDrawAdditive(batch, elapsed, BackAdditive);

            if (draw3D) manager.RenderSceneObjects();

            // @note Foreground is the default layer
            if (ForeElements.NotEmpty) BatchDrawSimple(batch, elapsed, ForeElements);
            if (ForeAdditive.NotEmpty) BatchDrawAdditive(batch, elapsed, ForeAdditive);

            if (draw3D) manager.EndFrameRendering();

            ClearLayers();
        }

        static void BatchDrawSimple(SpriteBatch batch, DrawTimes elapsed, Array<UIElementV2> elements)
        {
            batch.SafeBegin();

            int count = elements.Count;
            UIElementV2[] items = elements.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                items[i].Draw(batch, elapsed);
            }

            batch.SafeEnd();
        }

        // Phase 3.7: pass the additive BlendState directly to SpriteBatch.Begin so it
        // survives Immediate-mode batch flushes. The legacy "Begin AlphaBlend, then
        // override device.BlendState" pattern only worked under XNA 3.1 because the
        // device.RenderState was a free-standing API; under MonoGame the SpriteBatch
        // re-applies its own blend state per flush. Without this fix, the MainMenu
        // BackAdditive overlays (Lights_edge / Dust / Lights_center / Aurora) draw
        // with plain alpha-blend and the planet's limb / city-lights / aurora effects
        // collapse into a narrow, unblended core.
        static readonly BlendState SoftAdditive = new()
        {
            Name = "SoftAdditive",
            ColorSourceBlend = Blend.InverseDestinationColor,
            AlphaSourceBlend = Blend.InverseDestinationColor,
            ColorDestinationBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
            ColorBlendFunction = BlendFunction.Add,
            AlphaBlendFunction = BlendFunction.Add,
        };

        static void BeginAdditive(SpriteBatch batch, bool saveState = false)
        {
            batch.Begin(SpriteSortMode.Immediate, SoftAdditive);
        }
        
        static void BatchDrawAdditive(SpriteBatch batch, DrawTimes elapsed, Array<UIElementV2> elements)
        {
            BeginAdditive(batch);

            int count = elements.Count;
            UIElementV2[] items = elements.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                items[i].Draw(batch, elapsed);
            }

            batch.SafeEnd();
        }
    }
}
