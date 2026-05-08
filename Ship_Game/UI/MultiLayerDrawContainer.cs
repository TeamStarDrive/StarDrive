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

        // Phase 3.7 / §4.6 #1.c: BackAdditive layer (planet limb, Dust, Aurora,
        // city-lights) needs a true additive composite over the planet panel.
        // Pre-Phase-3.7 used plain AlphaBlend, which collapsed the highlights
        // into an unblended core. Phase 3.7 swapped to a custom SoftAdditive
        // (`src*(1-dst) + dst`) — that preserved the planet but attenuated the
        // puff contribution so far that Dust + Aurora became invisible against
        // a bright Mars limb. Post-§4.6 #1.c we use canonical
        // `BlendState.Additive` (`src*srcA + dst*1`): direct add over dark
        // space, controlled lift over the planet (Color multipliers in
        // MMenu.Mars.yaml dial back per-panel intensity if needed).
        static void BeginAdditive(SpriteBatch batch, bool saveState = false)
        {
            batch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
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
