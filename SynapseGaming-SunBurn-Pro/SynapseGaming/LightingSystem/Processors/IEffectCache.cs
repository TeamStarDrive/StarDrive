using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Processors
{
    public interface IEffectCache
    {
        bool TryGetEffect<T>(string assetName, out T asset) where T : Effect;
        void AddEffect(string assetName, Effect effect);

        void LogEffectError(Exception ex, string error);
    }
}
