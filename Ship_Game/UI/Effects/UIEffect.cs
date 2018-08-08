using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    /// <summary>
    /// Represents a simple UI Behaviour Effect
    /// such as transitioning
    /// </summary>
    public abstract class UIEffect
    {
        protected readonly UIElementV2 Element;

        /// <summary>
        /// Animation progress from 0f to 1f.
        /// Values above 1f may occurr for exaggerated animations.
        /// </summary>
        public float Animation { get; protected set; }

        protected UIEffect(UIElementV2 element)
        {
            Element = element;
        }

        /// <summary>
        /// Perform effect update
        /// </summary>
        /// <returns>TRUE if this Effect has finished</returns>
        public abstract bool Update(float deltaTime);
    }
}
