using Microsoft.Xna.Framework.Content;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Processors
{
    /// <summary />
    public class LightRigReader_Pro : ContentTypeReader<LightRig>
    {
        /// <summary />
        protected override LightRig Read(ContentReader input, LightRig instance)
        {
            var lightRig = new LightRig
            {
                Name         = input.ReadString(),
                LightRigFile = input.ReadString(),
                ProjectFile  = input.ReadString()
            };
            lightRig.InitFromXml(input.ReadString());
            BlockUtil.SkipBlock(input);
            return lightRig;
        }
    }
}
